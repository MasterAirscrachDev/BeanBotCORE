using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBot
{
    class EventSystem
    {
        Prediction activePrediction;
        List<Robbery> activeRobberies = new List<Robbery>();
        List<PadlockUser> activePadlocks = new List<PadlockUser>();
        //Bot bot;
        CommandManager commandManager;
        int dropsLeft;
        public int newDrops;
        bool goldDrop = false;
        List<string> hasDrop = new List<string>();
        public EventSystem(bool manageTickers, CommandManager commandManager)
        {
            //this.bot = bot;
            this.commandManager = commandManager;
            if (manageTickers)
            { StartDrop(); PadlockTicker(); }
            //Console.WriteLine("Event System Started");
            if(Program.config.autoChannelPadlock){
                activePadlocks.Add(new PadlockUser(Program.config.channel, int.MaxValue, true));
            }
        }
        async Task Drop()
        {
            //wait 10 minutes
            await Task.Delay(Program.config.dropTimer * 60000);
            //wait 30 seconds
            //await Task.Delay(30000);
            if (dropsLeft <= 0 && newDrops > 0)
            {
                hasDrop.Clear();
                string mesage = "";
                dropsLeft = newDrops;
                //pick a number between 1 and 100
                int chance = new System.Random().Next(1, 101);
                if(chance == 1){
                    goldDrop = true;
                    string multi = Program.config.pointsFromDrop > 1 ? $"These {dropsLeft} {Program.config.currencies}" : $"This {Program.config.currency}";
                    mesage = $"Stardust Converted {multi} Into Gold! Use !Drop to collect it!";
                }
                else{
                    goldDrop = false;
                    string multi = Program.config.pointsFromDrop > 1 ? $" {dropsLeft} " : $" ";
                    mesage = $"A Wild Drop Has Apeared! First{multi}to use !Drop will get {Program.GetGlobalMultipliedPoints(Program.config.pointsFromDrop)} {Program.config.currencies}!";
                }
                await Program.SendMessage(mesage);
            }
            else if(Program.config.notifyOnDropsRemaining && newDrops > 0)
            { 
                string mesage = "";
                if(goldDrop){ goldDrop = false; mesage = $"The Stardust has faded leaving {dropsLeft} regular !Drops"; }
                else{ mesage = $"There Is Still {dropsLeft} {Program.config.currency} !Drops Left"; }
                if(commandManager.GetActiveChatters() > 0) {
                    await Program.SendMessage(mesage);
                }
                hasDrop.Clear(); 
            }
            StartDrop();
        }
        void StartDrop()
        { Task.Run(() => Drop()); }
        public async Task<ProcessData> Collect(ProcessData data)
        {
            if (dropsLeft > 0 && !hasDrop.Contains(data.message.sender.ToLower()))
            {
                hasDrop.Add(data.message.sender.ToLower());
                dropsLeft--;
                if (goldDrop)
                {
                    data.user.goldPoints++;
                    data.returnMessage = $"@{data.message.sender} has claimed a gold drop! {dropsLeft} left!";
                    return data;
                }
                else{
                    data.user.points += Program.GetMaxMultipliedPoints(Program.config.pointsFromDrop, data.user);
                    data.returnMessage = $"@{data.message.sender} has claimed a {Program.config.currency} drop! {dropsLeft} left!";
                    return data;
                }
                
            }
            else if (hasDrop.Contains(data.message.sender.ToLower()))
            { data.returnMessage = $"@{data.message.sender} you have already claimed this drop!"; }
            else { data.returnMessage = $"@{data.message.sender} No drops left!"; }
            return data;
        }
        //a command to steal from the richest user
        public async Task<ProcessData> Steal(ProcessData data, UserWithTimer[] activeUsers)
        {
            //message will look like this "!steal @target" or just "!steal"
            //check if the user is already stealing
            if (activeRobberies.Any(x => x.theif == data.message.sender.ToLower()))
            { data.returnMessage = $"@{data.message.sender} you are already stealing!"; return data; }
            User targetUser;
            if (data.message.content == "steal") {
                //get the richest user
                //get a list of all users and sort them by points
                List<User> users = new List<User>();
                users.AddRange(await SaveSystem.GetAllUsers());
                users.RemoveAll(x => x.name == "AwesomeBean_BOT");
                users.Sort((x, y) => y.points.CompareTo(x.points));
                targetUser = users[0];
            }
            else
            {
                try{
                    //get the target
                    string target = data.message.content.Remove(0, 7);
                    targetUser = await SaveSystem.GetUser(target.ToLower());
                }
                catch{ data.returnMessage = $"@{data.message.sender} use !steal @target"; return data; }
            }
            //check if the target is the user
            if (targetUser.name == data.message.sender)
            { data.returnMessage = $"@{data.message.sender} you can't steal from yourself!"; return data; }
            //check if the target is in the active users
            if (!activeUsers.Any(x => x.username == targetUser.name))
            { data.returnMessage = $"@{data.message.sender} {targetUser.name} is not in chat!"; return data; }
            if (activePadlocks.Any(x => x.username.ToLower() == targetUser.name.ToLower()))
            { data.returnMessage = $"@{data.message.sender} @{targetUser.name} has a padlock, you can't steal from them!"; return data; }
            //check if the target has less beans than the user
            if (targetUser.points < data.user.points)
            { data.returnMessage = $"@{data.message.sender} it would be rude to steal from someone poorer than you!"; return data; }
            //create a new robbery
            Robbery robbery = new Robbery(data.message.sender, targetUser.name); activeRobberies.Add(robbery);
            //wait 5 seconds + 1 for delay
            await Task.Delay(6000);
            //check if the robbery was stopped
            if (robbery.stopped)
            {
                data.returnMessage = $"@{data.message.sender} Failed to steal from @{robbery.victim}!";
                activeRobberies.Remove(robbery);
                return data;
            }
            else {
                //transfer 10% of the victims points to the user
                targetUser = await SaveSystem.GetUser(robbery.victim.ToLower());
                int pointsToSteal = (int)(targetUser.points * 0.1);
                targetUser.points -= pointsToSteal; await SaveSystem.SaveUser(targetUser);
                data.user.points += pointsToSteal;
                data.returnMessage = $"@{data.message.sender} stole {pointsToSteal.ToString("N0")} {Program.config.currencies} from @{robbery.victim}!";
                activeRobberies.Remove(robbery);
                return data;
            }
        }
        public void CancelSteal(ProcessData data)
        {   //if the user is being robbed cancel the robbery
            for(int i = 0; i < activeRobberies.Count; i++) {
                if(activeRobberies[i].victim.ToLower() == data.message.sender.ToLower() && activeRobberies[i].stopped == false)
                { activeRobberies[i].stopped = true; }
            }
        }
        //padlock ticker
        async Task PadlockTicker()
        {
            //wait 1 min
            await Task.Delay(60000);
            //remove 1 min from all padlocks
            for(int i = 0; i < activePadlocks.Count; i++)
            {
                activePadlocks[i].time--;
                if(activePadlocks[i].time <= 0) {
                    if (activePadlocks[i].notifyOnEnd)
                    { Program.SendMessage($"@{activePadlocks[i].username} Your Padlock has broken"); }
                    activePadlocks.RemoveAt(i);
                    i--;
                }
            }
            //start the ticker again
            PadlockTicker();
        }
        //function for buying a padlock to prevent stealing
        public ProcessData BuyPadlock(ProcessData data)
        {
            //theres 3 tiers of padlocks
            //tier 1 costs 1,000 beans and lasts 10 mins
            //tier 2 costs 2,500 beans  and lasts 20 mins
            //tier 3 costs 5,000 beans and lasts 30 mins
            //a message should look like this "!buypadlock tier"
            try{
                //if the message is just !buypadlock then buy a tier 1 padlock
                //remove the buypadlock part
                string message = data.message.content.Remove(0, 11);
                //check if the user has enough beans
                //check if there is a tier
                int tier = -1;
                if(message.Length > 0){
                    //get the tier
                    tier = int.Parse(message);
                    //check if the tier is valid
                    if((tier < 1 || tier > 4) || (tier == 4 && !data.message.usermod)){
                        data.returnMessage = $"@{data.message.sender} that is not a valid tier!";
                        return data;
                    }
                }
                else{
                    //buy a tier 1 padlock
                    tier = 1;
                }
                //check if the user has enough beans
                int cost = 0;
                //cost is 10, 20 and 30% of the users beans
                if(tier == 1){ cost = (int)Math.Floor(data.user.points * 0.1); }
                else if(tier == 2){ cost = (int)Math.Floor(data.user.points * 0.2); }
                else if(tier == 3){ cost = (int)Math.Floor(data.user.points * 0.3); }
                else if(tier == 4){ cost = 0;}
                if(data.user.points < cost){
                    data.returnMessage = $"@{data.message.sender} you don't have enough {Program.config.currencies}!";
                    return data;
                }
                //check if the user already has a padlock
                if(activePadlocks.Any(x => x.username == data.message.sender)){
                    data.returnMessage = $"@{data.message.sender} you already have a padlock!";
                    return data;
                }
                //create a new padlock
                if(tier != 4){
                    activePadlocks.Add(new PadlockUser(data.message.sender, tier * 10, tier == 3));
                    //remove the beans from the user
                    data.user.points -= cost;
                    data.returnMessage = $"@{data.message.sender} bought a tier {tier} padlock for {cost.ToString("N0")} {Program.config.currencies}!";
                }
                else{
                    if(Program.config.allowModpadlock)
                    activePadlocks.Add(new PadlockUser(data.message.sender, int.MaxValue, true));
                    //if user == channel
                    if(data.user.name.ToLower() == Program.config.channel){
                        data.returnMessage = $"@{data.message.sender} equipped a channel padlock!";
                    }
                    else if(data.user.name == "MasterAirscrach"){
                        data.returnMessage = $"@{data.message.sender} take a break, you've earnt it!";
                    }
                    else{
                        data.returnMessage = $"@{data.message.sender} bit scummy of you";
                    }
                }
                return data;
            }
            catch{
                int cost1 = (int)Math.Floor(data.user.points * 0.1), cost2 = (int)Math.Floor(data.user.points * 0.2), cost3 = (int)Math.Floor(data.user.points * 0.3);
                data.returnMessage = $"@{data.message.sender} use !buypadlock (1,2,3) T1 = {cost1.ToString("N0")} {Program.config.currencies}, T2 = {cost2.ToString("N0")} {Program.config.currencies}, T3 = {cost3.ToString("N0")} {Program.config.currencies}";
                return data;
            }
        }
        public ProcessData PadlockInfo(ProcessData data)
        {
            //check if the user has a padlock
            if(activePadlocks.Any(x => x.username == data.message.sender)){
                //get the padlock
                PadlockUser padlock = activePadlocks.Find(x => x.username == data.message.sender);
                if(padlock.time > 30)
                { data.returnMessage = $"@{data.message.sender} if your padlock runs out you have more issues than being stolen from"; }
                else{ data.returnMessage = $"@{data.message.sender} your padlock will expire in {padlock.time} minutes!"; }
                return data;
            }
            else
            { data.returnMessage = $"@{data.message.sender} you don't have a padlock, use !buypadlock to buy one!"; return data; }
        }
        //function to remove a padlock taking an optional user
        public ProcessData RemovePadlock(ProcessData data)
        {
            //message will either be "removepadlock" or "removepadlock @user"
            //check if the user has a padlock
            string targetUser = "";
            if(data.message.content.Length > 13){
                //get the target
                targetUser = data.message.content.Remove(0, 14);
                targetUser = targetUser.Replace("@", "").ToLower();
                if(targetUser == "masterairscrach" || targetUser == Program.config.channel)
                { data.returnMessage = $"I'm sorry @{data.message.sender}, I'm afraid I can't do that."; return data; }
            }
            else{
                targetUser = data.message.sender;
            }
            if(activePadlocks.Any(x => x.username == targetUser)){
                //get the padlock
                PadlockUser padlock = activePadlocks.Find(x => x.username == targetUser);
                //remove the padlock
                activePadlocks.Remove(padlock);
                //return a message
                data.returnMessage = $"@{data.message.sender} removed @{targetUser}'s padlock!";
                return data;
            }
            else
            { data.returnMessage = $"@{data.message.sender} @{targetUser} does not have a padlock!"; return data; }
        }
        public ProcessData StartPrediction(ProcessData data){
            //message should look like this "startprediction prediction name, option 1, option 2, ect"
            try{
                //remove the startprediction part
                string message = data.message.content.Remove(0, 16);
                //split the message by commas
                string[] splitMessage = message.Split(',');
                //get the prediction name
                string predictionName = splitMessage[0];
                //create the teams
                List<PredictionTeam> teams = new List<PredictionTeam>();
                if(splitMessage.Length < 3){ data.returnMessage = $"Error Starting Prediction! You need at least 2 teams!"; return data; }
                for (int i = 1; i < splitMessage.Length; i++)
                {
                    //check if the first character is a space and remove it
                    if (splitMessage[i][0] == ' ')
                    { splitMessage[i] = splitMessage[i].Remove(0, 1); }
                    teams.Add(new PredictionTeam(splitMessage[i]));
                }
                //create the prediction
                Prediction prediction = new Prediction(predictionName);
                prediction.teams = teams;
                activePrediction = prediction;
                //return a message
                data.returnMessage = $"A Prediction Has Started Use !vote (teamname) (amount) to bet!";
                return data;
            }
            catch{
                data.returnMessage = $"Error Starting Prediction!";
                return data;
            }
            
        }
        public ProcessData ViewPrediction(ProcessData data){
            //check if there is an active prediction
            if (activePrediction != null)
            {
                //create a message
                string message = $"Prediction: {activePrediction.name} |";
                double totalVotes = activePrediction.GetTotalPoints();
                //the stats should include the team name, the amount of votes, and the percentage of the total votes and the top voter
                foreach (PredictionTeam team in activePrediction.teams)
                {
                    //get the total votes
                    int teamTotalVotes = team.GetPointSum();
                    //get the percentage of the total votes
                    double percentage = (double)teamTotalVotes / totalVotes;
                    //get the top voter
                    string topVoter = team.GetTopVoter();
                    //add the stats to the message
                    message += $" Team {team.name}: {teamTotalVotes.ToString("N0")} {Program.config.currencies} ({percentage.ToString("P")}) |";
                    if (topVoter != null)
                    { message += $"{topVoter} {Program.config.currencies} |"; }
                }
                //return the message
                data.returnMessage = message;
                return data;
            }
            else
            {
                data.returnMessage = $"There is no active prediction!";
                return data;
            }
        }
        public ProcessData PredictionVote(ProcessData data){
            //message should look like this "vote teamname amount"
            try{
                //remove the vote part
                string message = data.message.content.Remove(0, 5);
                //split the message on the last space because the team name could have spaces
                string[] splitMessage = message.Split(' ');
                //get the team name
                string teamName = "";
                for (int i = 0; i < splitMessage.Length - 1; i++)
                { teamName += splitMessage[i] + " "; }
                teamName = teamName.Remove(teamName.Length - 1);
                //get the amount
                int amount = CommandManager.PointsFromString(splitMessage[splitMessage.Length - 1], data.user);
                //check if the user has enough points
                if (data.user.points >= amount && amount > 0){
                    //check if there is an active prediction
                    if (activePrediction != null){
                        //check if the prediction is not locked
                        if (!activePrediction.locked){
                            //check if the team exists
                            //make sure the user has not voted on any other teams
                            for (int i = 0; i < activePrediction.teams.Count; i++){
                                if (activePrediction.teams[i].name != teamName && activePrediction.teams[i].HasVoter(data.message.sender)){
                                    //return a message
                                    data.returnMessage = $"@{data.message.sender} You have already voted on a team!";
                                    return data;
                                }
                            }
                            for (int i = 0; i < activePrediction.teams.Count; i++){
                                //Console.WriteLine($"'{activePrediction.teams[i].name}' == '{teamName}'");
                                if (activePrediction.teams[i].name.ToLower() == teamName.ToLower()){
                                    //add the vote
                                    activePrediction.teams[i].AddVote(data.message.sender, amount);
                                    //remove the points from the user
                                    data.user.points -= amount;
                                    //return a message
                                    data.returnMessage = $"@{data.message.sender} has voted {amount.ToString("N0")} {Program.config.currencies} on {teamName}!";
                                    return data;
                                }
                            }
                            //return a message
                            data.returnMessage = $"@{data.message.sender} Invalid team name!";
                            return data;
                        }
                        else{
                            data.returnMessage = $"@{data.message.sender} The prediction is locked!";
                            return data;
                        }
                    }
                    else{
                        data.returnMessage = $"@{data.message.sender} There is no active prediction!";
                        return data;
                    }
                }
                else{
                    data.returnMessage = $"@{data.message.sender} You do not have enough {Program.config.currencies}!";
                    return data;
                }
            }
            catch{
                data.returnMessage = $"@{data.message.sender} Invalid vote! Use !vote (teamname) (amount)";
                return data;
            }
        }
        public ProcessData LockPrediction(ProcessData data){
            //check if there is an active prediction
            if (activePrediction != null){
                activePrediction.locked = true;
                //return a message
                data.returnMessage = $"The Prediction Has Been Locked! Good Luck!";
                return data;
            }
            else{
                data.returnMessage = $"@{data.message.sender} There is no active prediction!";
                return data;
            }
        }
        public async Task<ProcessData> EndPrediction(ProcessData data){
            //check if there is an active prediction
            if (activePrediction != null){
                //message should look like this "endprediction teamname"
                try
                {
                    //remove the endprediction part
                    string teamName = data.message.content.Remove(0, 14);
                    //check if the team exists
                    for (int i = 0; i < activePrediction.teams.Count; i++)
                    {
                        if (activePrediction.teams[i].name.ToLower().Contains(teamName.ToLower()))
                        {
                            //get total points of all the other teams
                            int totalPoints = 0;
                            for (int j = 0; j < activePrediction.teams.Count; j++)
                            {
                                if (activePrediction.teams[j].name != teamName)
                                { totalPoints += activePrediction.teams[j].GetPointSum(); }
                            }
                            //get the names of the winners
                            string[] winners = activePrediction.teams[i].GetVoters();
                            //get the amount of points the winners get
                            int pointsPerWinner = totalPoints / winners.Length;
                            //give the winners the points
                            for (int j = 0; j < winners.Length; j++)
                            {
                                //get the user
                                User user = await SaveSystem.GetUser(winners[j]);
                                //add the points
                                user.points += pointsPerWinner + activePrediction.teams[i].GetVote(winners[j]);
                                //save the user
                                await SaveSystem.SaveUser(user);
                            }
                            //return a message
                            data.returnMessage = $"The Prediction Has Ended! Team {teamName} Has Won {pointsPerWinner.ToString("N0")} {Program.config.currencies} each!";
                            //reset the active prediction
                            activePrediction = null;
                            return data;
                        }
                    }
                    //return a message
                    data.returnMessage = $"@{data.message.sender} Invalid team name!";
                    return data;
                }
                catch
                {
                    data.returnMessage = $"@{data.message.sender} Invalid endprediction! Use !endprediction (teamname)";
                    return data;
                }
            }
            else{
                data.returnMessage = $"@{data.message.sender} There is no active prediction!";
                return data;
            }
        }
        //make a command to cancel a prediction and return all the points to the users
        public async Task<ProcessData> CancelPrediction(ProcessData data){
            //check if there is an active prediction
            if (activePrediction != null){
                //for each team
                for (int i = 0; i < activePrediction.teams.Count; i++){
                    activePrediction.teams[i].RefundPoints();
                }
                activePrediction = null;
                //return a message
                data.returnMessage = $"The Prediction Has Been Cancelled!";
                return data;
            }
            else{
                data.returnMessage = $"@{data.message.sender} There is no active prediction!";
                return data;
            }
        }
    }
    class Prediction
    {
        public string name;
        public bool locked;
        public List<PredictionTeam> teams = new List<PredictionTeam>();
        public int GetTotalPoints()
        {
            int totalPoints = 0;
            for (int i = 0; i < teams.Count; i++)
            { totalPoints += teams[i].GetPointSum(); }
            return totalPoints;
        }
        public Prediction(string name) { this.name = name; locked = false; }
    }
    class PredictionTeam
    {
        public string name;
        public PredictionTeam(string name) { this.name = name; }
        List<Vote> votes = new List<Vote>();
        public string[] GetVoters()
        {
            string[] voters = new string[votes.Count];
            for (int i = 0; i < votes.Count; i++)
            { voters[i] = votes[i].username; }
            return voters;
        }
        public int GetVote(string user)
        {
            for (int i = 0; i < votes.Count; i++)
            { if (votes[i].username == user) { return votes[i].bet; } }
            return 0;
        }
        public void AddVote(string username, int bet)
        {
            //add or increase the vote
            for (int i = 0; i < votes.Count; i++)
            {
                if (votes[i].username == username)
                { votes[i].bet += bet; return; }
            }
            votes.Add(new Vote(username, bet));
        }
        public int GetPointSum()
        {
            int sum = 0;
            for (int i = 0; i < votes.Count; i++)
            { sum += votes[i].bet; }
            return sum;
        }
        public bool HasVoter(string username)
        {
            for (int i = 0; i < votes.Count; i++)
            { if (votes[i].username == username) { return true; } }
            return false;
        }
        public string GetTopVoter()
        {
            //should be "username with x points"
            if (votes.Count == 0) { return null; }
            string topVoter = null;
            int topPoints = 0;
            for (int i = 0; i < votes.Count; i++)
            {
                if (votes[i].bet > topPoints)
                {
                    topVoter = votes[i].username;
                    topPoints = votes[i].bet;
                }
            }
            return $"Top Voter: @{topVoter} with {topPoints.ToString("N0")}";
        }
        public async Task RefundPoints()
        {
            //for each vote refund the points
            for (int i = 0; i < votes.Count; i++)
            {
                //get the user
                User user = await SaveSystem.GetUser(votes[i].username);
                //add the points
                user.points += votes[i].bet;
                //save the user
                await SaveSystem.SaveUser(user);
            }
        }
    }
    class Vote
    {
        public string username;
        public int bet;
        public Vote(string username, int bet)
        {
            this.username = username;
            this.bet = bet;
        }
    }
    class Robbery
    {
        public string theif, victim;
        public bool stopped = false;
        public Robbery(string theif, string victim)
        {
            this.theif = theif;
            this.victim = victim;
        }
    }
    class PadlockUser
    {
        public string username;
        public int time;
        public bool notifyOnEnd;
        public PadlockUser(string username, int time, bool notifyOnEnd = false)
        {
            this.username = username;
            this.time = time;
            this.notifyOnEnd = notifyOnEnd;
        }
    }
}
