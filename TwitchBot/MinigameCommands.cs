using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TwitchBot
{
    public class MinigameCommands
    {
        int open = 0;
        OneUpsGame oneUpsGame = null;
        List<QuickMathQuestion> quickMathQuestions = new List<QuickMathQuestion>();
        public MinigameCommands()
        { ActiveTimer(); if(Program.config.minigamesCost == 0){open = int.MaxValue;} }
        
        public ProcessData QuickMath(ProcessData data, int cost = 100){
            if(open == 0){data.returnMessage = $"@{data.message.sender} Minigames are closed at the moment"; return data;}
            if (!CommandManager.CanAfford(cost.ToString(), data.user)){ data.returnMessage = $"@{data.message.sender} Not Enough {Program.config.currencies}"; return data;}
            //Picking a random number between 1 and 100"
            int diff = new Random().Next(1, 100);
            // Generating problem");
            string problem = GenerateMathProblem(diff);
            //Problem: {problem}
            //split the message into an array on |
            string[] substr = problem.Split('|');
            string question = substr[0];
            //Question: {question}
            float answer = float.Parse(substr[1]);
            //Answer: {answer}
            //Generateing QuickMathQuestion struct
            //get the time to solve
            // if diff is 1-10, time is 10 seconds, if d
            string time = "";
            int winValue = 0;
            if(diff <= 10){time = "10s"; winValue = 10;}
            else if(diff <= 20){time = "20s"; winValue = 15;}
            else if(diff <= 30){time = "30s"; winValue = 30;}
            else if(diff <= 40){time = "40s"; winValue = 40;}
            else if(diff <= 50){time = "50s"; winValue = 50;}
            else if(diff <= 60){time = "1m"; winValue = 60;}
            else if(diff <= 70){time = "1m 10s"; winValue = 70;}
            else if(diff <= 80){time = "1m 20s"; winValue = 90;}
            else if(diff <= 90){time = "1m 30s"; winValue = 110;}
            else if(diff <= 100){time = "1m 40s"; winValue = 150;}
            
            winValue += cost;
            QuickMathQuestion quickMathQuestion = new QuickMathQuestion(data.user.name, answer, diff, winValue);
            //Console.WriteLine($"Sending question to chat");
            question = question.Replace("1", "𝟭").Replace("2", "𝟮").Replace("3", "𝟯").Replace("4", "𝟰").Replace("5", "𝟱").Replace("6", "𝟲").Replace("7", "𝟳").Replace("8", "𝟴").Replace("9", "𝟵").Replace("0", "𝟬");
            //question = question.Replace("+", "➕").Replace("-", "➖").Replace("*", "✖️").Replace("/", "➗");
            question = question.Replace(")","❩").Replace("(","❨");
            //add "​" at random points in the string to prevent the bot from auto detecting the question
            // for(int i = 0; i < 10; i++){
            //     int index = new Random().Next(0, question.Length);
            //     question = question.Insert(index, "​");
            // }
            //Console.WriteLine($"Formatted problem: {question}");
            data.returnMessage = $"@{data.message.sender} Solve: {question} In {time} for {winValue} {Program.config.currencies.ToLower()} submit with !answer (answer to 1 decimal place)";
            quickMathQuestions.Add(quickMathQuestion);
            RemoveQuestion(quickMathQuestion);
            data.user.points -= cost;
            //Console.WriteLine($"Returning data");
            return data;
        }
        public ProcessData MathAnswer(ProcessData data){
            //Console.WriteLine($"MathAnswer: {data.message.content}");
            //split the message into an array on " "
            string[] substr = data.message.content.Split(' ');
            //if the array is 2 long
            if(substr.Length == 2){
                try{
                    float answer = float.Parse(substr[1]);
                    foreach(QuickMathQuestion question in quickMathQuestions){
                        if(question.user == data.message.sender){
                            if(answer == question.answer){
                                int points = Program.GetMaxMultipliedPoints(question.prise, data.user);
                                data.user.points += points;
                                data.returnMessage = $"@{data.message.sender} Correct! You won {points} {Program.config.currencies.ToLower()}";
                                quickMathQuestions.Remove(question);
                            }
                            else{
                                data.returnMessage = $"@{data.message.sender} Incorrect, the answer was {question.answer}";
                            }
                            quickMathQuestions.Remove(question); return data;
                        }
                    }
                }
                catch{
                    data.returnMessage = $"@{data.message.sender} Invalid answer";
                }
            }
            return data;
        }
        async Task RemoveQuestion(QuickMathQuestion question){
            //round the diff to the highest 10
            int diff = question.diff;
            if(diff % 10 != 0){diff += 10 - (diff % 10);}
            await Task.Delay(1000 * diff);
            if(quickMathQuestions.Remove(question)){
                await Program.SendMessage($"@{question.user} You ran out of time to answer the question, the answer was {question.answer}");
            }
        }
        public ProcessData CoinFlip(ProcessData data)
        {
            
            Random random = new Random();
            //message.content will look like this: !coinflip || !coinflip heads 100
            //split the message into an array
            string[] substr = data.message.content.Split(' ');
            if(substr.Length > 2)
            {
                if(open == 0){data.returnMessage = $"@{data.message.sender} Minigames are closed at the moment"; return data;}
                int team = -1;
                if (substr[1] == "heads"){ team = 1; }
                else if (substr[1] == "tails"){ team = 2; }
                if (team != -1){
                    try{
                        int bet = CommandManager.PointsFromString(substr[2], data.user);
                        if(bet > 1000){bet = 1000;}
                        if (CommandManager.CanAfford(substr[2], data.user)){
                            int randomNumber = random.Next(1, 4);
                            if (team == randomNumber){
                                bet = Program.GetMaxMultipliedPoints(bet, data.user);
                                data.user.points += bet;
                                data.returnMessage = $"@{data.message.sender} guessed the coin flip and won {bet.ToString("N0")} {Program.config.currencies.ToLower()}!";
                            }
                            else{
                                data.user.points -= bet;
                                AddToFloor((int)Math.Round(bet * 0.1));
                                string coin = team == 2 ? "Heads" : "Tails";
                                data.returnMessage = $"@{data.message.sender} guessed the coin flip wrong! It was {coin}";
                            }
                        }
                        else{
                            if (Program.config.warnIfBadSyntax)
                            { data.returnMessage = $"@{data.message.sender} Not Enough {Program.config.currencies}"; }
                            Program.commandManager.ResetCooldown(data.message.sender, "coinflip");
                        } 
                    }
                    catch {
                        if (Program.config.warnIfBadSyntax)
                        { data.returnMessage = $"@{data.message.sender} use !coinflip (heads/tails) (bet)"; }
                        Program.commandManager.ResetCooldown(data.message.sender, "coinflip");
                    }
                }
                else if (Program.config.warnIfBadSyntax)
                { data.returnMessage = $"@{data.message.sender} use !coinflip (heads/tails) (bet)"; }
            }
            else
            {
                //pick a number between 0 - 1
                int randomNumber = random.Next(0, 1);
                data.returnMessage = randomNumber == 0 ? $"@{data.message.sender} it's heads" : $"@{data.message.sender} it's tails";
            }
            return data;
        }
        public ProcessData PlayWithFire(ProcessData data){
            //get the bet
            if(open == 0){data.returnMessage = $"@{data.message.sender} Minigames are closed at the moment"; return data;}
            //split the message on the spaces
            string[] substr = data.message.content.Split(' ');
            if(substr.Length == 2){
                int bet = CommandManager.PointsFromString(substr[1], data.user);
                //if bet is more than 1000, set it to 1000
                if(bet > 1000){bet = 1000;}
                //roll a random number between 1 - 20
                Random random = new Random();
                int randomNumber = random.Next(1, 20);
                if(randomNumber == 11){
                    //if the number is 11, the user wins 10x the bet
                    bet = Program.GetUserMultipliedPoints(bet * 10, data.user);
                    data.user.points += bet;
                    data.returnMessage = $"@{data.message.sender} played with fire and won {bet.ToString("N0")} {Program.config.currencies.ToLower()}!";
                    return data;
                }
                else{
                    //if the number is 1, the user loses 10x the bet
                    data.user.points -= bet;
                    AddToFloor((int)Math.Round(bet * 0.1));
                    data.returnMessage = $"@{data.message.sender} played with fire and got banished from chat for 120 seconds!";
                    Program.twitchLibInterface.bot.AddUserToTempBanned(data.user.name, 2);
                    return data;
                }
            }
            else{
                if (Program.config.warnIfBadSyntax)
                { data.returnMessage = $"@{data.message.sender} use !playwithfire (bet)"; }
                Program.commandManager.ResetCooldown(data.message.sender, "playwithfire");
                return data;
            }
        }
        public ProcessData OneUps(ProcessData data){
            if(open == 0){data.returnMessage = $"@{data.message.sender} Minigames are closed at the moment"; return data;}
            //message.content should look like this: !oneups (bet)
            //split the message into an array
            string[] substr = data.message.content.Split(' ');
            bool valid = true;
            if(substr.Length > 1){
                try{
                    int bet = CommandManager.PointsFromString(substr[1], data.user);
                    if (CommandManager.CanAfford(substr[1], data.user)){
                        data.returnMessage = $"@{data.message.sender} has started a game of One Ups at {bet} {Program.config.currencies}! use /w awesomebean_bot PICKNUM 1-10 to join, winner will be drawn in 3 minutes";
                        oneUpsGame = new OneUpsGame();
                        oneUpsGame.bet = bet;
                        DecideOneUps();
                    }
                    else if (Program.config.warnIfBadSyntax)
                    { data.returnMessage = $"@{data.message.sender} Not Enough {Program.config.currencies}"; }
                }
                catch{
                    if (Program.config.warnIfBadSyntax) { valid = false; }
                }
            }
            else if (Program.config.warnIfBadSyntax) { valid = false; }
            if(!valid){ data.returnMessage = $"@{data.message.sender} use !oneups (bet)"; }
            return data;
        }
        async Task DecideOneUps(){
            //wait 1 min
            await Task.Delay(60000 * 3);
            int totalpoints = 0;
            for(int i = 0; i < oneUpsGame.choices.Count; i++){
                totalpoints += oneUpsGame.bet;
            }
            OneUpsChoice winner = PickUserWithHighestUniqueNumber(oneUpsGame.choices);
            if(winner.choice == 0){
                //no one won
                oneUpsGame = null;
                Program.SendMessage("No one won the One Ups game"); return;
            }
            //add the points to the winner

            User winnerUser = await SaveSystem.GetUser(winner.name);
            int points = totalpoints;
            winnerUser.points += points;
            await SaveSystem.SaveUser(winnerUser);
            oneUpsGame = null;
            Program.SendMessage($"@{winnerUser.name} won the One Ups game with {winner.choice}! They won {points.ToString("N0")} {Program.config.currencies.ToLower()}!");
        }
        public async Task PickOneUpsNum(string user, int pick){
            //check if the game is open
            if(oneUpsGame != null){
                //check if the user has already picked a number
                if(pick > 0 && pick < 11 && !oneUpsGame.choices.Exists(x => x.name == user)){
                    //add the user to the list
                    OneUpsChoice choice = new OneUpsChoice();
                    choice.name = user;
                    choice.choice = pick;
                    oneUpsGame.choices.Add(choice);
                    await Program.SendMessage($"{oneUpsGame.choices.Count} users have joined the One Ups game"); 
                }
            }
        }
        public ProcessData OpenMinigames(ProcessData data){
            int minigamesCost = Program.config.minigamesCost;
            if(data.user.points >= minigamesCost){
                data.user.points -= minigamesCost;
                open += Program.config.minigamesDuration;
                data.returnMessage = $"@{data.message.sender} has opened the minigames for {open} Minutes";
            }
            else{
                data.returnMessage = $"@{data.message.sender} you need {minigamesCost.ToString("N0")} {Program.config.currencies.ToLower()} to open the minigames";
                Program.commandManager.ResetCooldown(data.message.sender, "openminigames");
            }
            return data;
        }
        OneUpsChoice PickUserWithHighestUniqueNumber(List<OneUpsChoice> choices)
        {
            OneUpsChoice selectedUser = new OneUpsChoice();
            selectedUser.choice = 0;
            Dictionary<int, int> choicesCount = new Dictionary<int, int>();
            foreach (OneUpsChoice choice in choices)
            {
                if (!choicesCount.ContainsKey(choice.choice))
                { choicesCount[choice.choice] = 1; }
                else
                { choicesCount[choice.choice]++; }
            }

            foreach (OneUpsChoice choice in choices)
            {
                if (choicesCount[choice.choice] == 1 && choice.choice > selectedUser.choice)
                { selectedUser = choice; }
            }
            return selectedUser;
        }
        //write a function that takes a number 1-100 and returns a string math problem where the number represents the difficulty 1=easy 100=hard
        public string GenerateMathProblem(int difficulty)
        {
            Random random = new Random();
            int numSteps = 2 + difficulty / 20; // Determine the number of steps based on difficulty

            string problem = $"{GenerateNumber(random)} ";
            bool hasOpeningBracket = false;

            for (int i = 1; i <= numSteps; i++)
            {
                char operatorSymbol = GetRandomOperator(random);
                bool addBracket = random.Next(0, 101) < difficulty;
                if(hasOpeningBracket) { addBracket = false;}
                string br = "";
                if (addBracket)
                {
                    br = "(";
                    hasOpeningBracket = true;
                }
                problem += $"{operatorSymbol} {br}{GenerateNumber(random)} ";
                if(hasOpeningBracket && !addBracket){
                    //roll to see if we close the bracket
                    if(random.Next(0, 2) == 0){
                        //close the bracket
                        problem = problem.Substring(0, problem.Length - 1);
                        problem += ") ";
                        hasOpeningBracket = false;
                    }
                }
            }
            if(hasOpeningBracket){
                //remove the last space
                problem = problem.Substring(0, problem.Length - 1);
                problem += ") ";
                hasOpeningBracket = false;
            }

            problem += $"= ?|{CalculateAnswer(problem)}";

            return problem;
        }

        char GetRandomOperator(Random r)
        {
            char[] operators = { '+', '-', '*', '/' };
            return operators[r.Next(operators.Length)];
        }
        string GenerateNumber(Random r)
        { return r.Next(1, 11).ToString(); }

        private float CalculateAnswer(string problem)
        {
            string a = new DataTable().Compute(problem.Replace("?", ""), null).ToString();
            float answer = float.Parse(a);
            //if float has decimal places, round to 1 decimal place
            if (answer % 1 != 0)
            { answer = (float)Math.Round(answer, 1); }
            return answer;
        }
        async Task AddToFloor(int randomDrop){
            //add the random drop to the floor
            //get the user awesomebean_bot
            User bot = await SaveSystem.GetUser("awesomebean_bot");
            //add the random drop to the floor
            bot.points += randomDrop;
            //save the user
            await SaveSystem.SaveUser(bot);
        }
        async Task ActiveTimer(){
            //wait a min then reduce the timer
            while(true){
                await Task.Delay(60000);
                if(open > 0){
                    open--;
                    if(open == 0){
                        Program.SendMessage("Minigames are now closed");
                    }
                }
            }
        }
        class OneUpsGame{
            public int bet;
            public List<OneUpsChoice> choices = new List<OneUpsChoice>();
        }
        struct OneUpsChoice{
            public int choice;
            public string name;
            public OneUpsChoice(int choice, string name){
                this.choice = choice;
                this.name = name;
            }
        }
        struct QuickMathQuestion{
            public string user;
            public int diff, prise;
            public float answer;
            public QuickMathQuestion(string user, float answer, int diff, int prise){
                this.user = user;
                this.answer = answer;
                this.diff = diff;
                this.prise = prise;
            }
        }
    }
}

