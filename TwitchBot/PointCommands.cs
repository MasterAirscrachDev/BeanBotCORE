using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBot
{
    class PointCommands
    {
        bool resetAsked = false;
        public PointCommands()
        {
            resetAsked = false;
        }
        public async Task<ProcessData> GivePoints(ProcessData data)
        {
            try {
                //string will look like this
                //!give{Program.config.currencies} @user 100
                //split the string into an array
                string[] messageArray = data.message.content.Split(' ');
                //get the user and the amount of {Program.config.currencies}
                string userToGive = messageArray[1].Replace("@", "");
                if (messageArray.Length > 1)
                {
                    int amount = CommandManager.PointsFromString(messageArray[2], data.user);
                    //check if the user has enough {Program.config.currencies}
                    if (CommandManager.CanAfford(messageArray[2], data.user))
                    {
                        User userToGiveTo = await SaveSystem.GetUser(userToGive);
                        if (userToGiveTo.name == data.user.name|| userToGiveTo.name == "AwesomeBean_BOT") {
                            data.returnMessage = $"@{data.message.sender} you can't give {Program.config.currencies} to yourself";
                            return data;
                        }
                        //remove the {Program.config.currencies} from the user
                        data.user.points -= amount;
                        //give tax is a number between 0 and 100
                        int tax = (int)(amount * (Program.config.giveTaxPercent / 100.0f));
                        //add the {Program.config.currencies} to the user
                        amount -= tax;
                        userToGiveTo.points += amount;
                        await SaveSystem.SaveUser(userToGiveTo);
                        if(tax != 0){
                            User bot = await SaveSystem.GetUser("AwesomeBean_BOT");
                            bot.points += tax;
                            await SaveSystem.SaveUser(bot);
                        }
                        //save the user
                        //set the message
                        data.returnMessage = $"@{data.message.sender} gave @{userToGive} {amount.ToString("N0")} {Program.config.currencies}!";
                        return data;
                    }
                    else if (Program.config.warnIfBadSyntax)
                    { data.returnMessage = $"@{data.message.sender} Not Enough {Program.config.currencies}"; }
                }
                else if (Program.config.warnIfBadSyntax)
                { data.returnMessage = $"@{data.message.sender} !Give{Program.config.currencies} @user num"; }

                return data;
            }
            catch { return data; }
            
        }
        public async Task<ProcessData> EditPoints(ProcessData data)
        {
            if (data.message.sender == "MasterAirscrach" || data.message.sender.ToLower() == data.message.channel)
            {
                //string will look like this: editbeans{Program.config.currencies} @user 100 [optional guilded]
                //split the string into an array
                string[] messageArray = data.message.content.Split(' ');
                
                //get the user and the amount of {Program.config.currencies}
                bool full = messageArray.Length > 2;
                string userToGive = full ? messageArray[1] : data.message.sender;
                bool guilded = false;
                try { guilded = messageArray[3].ToLower() == "gold"; } catch { }
                userToGive = userToGive.Replace("@", "");
                int amount = 0;
                try { if(full){amount = int.Parse(messageArray[2]);}else{amount = int.Parse(messageArray[1]);} }
                catch { return data; }
            
                User userToGiveTo = await SaveSystem.GetUser(userToGive);
                //add the {Program.config.currencies} to the user
                if (!guilded) { userToGiveTo.points += amount; }
                else { userToGiveTo.goldPoints += amount; }
                await SaveSystem.SaveUser(userToGiveTo);
                //save the user set the message
                data.returnMessage = $"@{userToGiveTo.name} now has {userToGiveTo.points.ToString("N0")} ({userToGiveTo.goldPoints}) {Program.config.currencies}!";
            }
            else if(Program.config.warnIfBadSyntax)
            { data.returnMessage = $"I'm Sorry {data.user.name}, I'm afraid I can't do that."; }

            return data;
        }
        public async Task<ProcessData> ScoreBoard(ProcessData data)
        {
            List<User> users = new List<User>();
            users.AddRange(await SaveSystem.GetAllUsers());
            //remove awesomebean_bot from the list
            users.RemoveAll(x => x.name == "AwesomeBean_BOT");

            //sort users by user.points
            users.Sort((x, y) => (y.points + y.goldPoints).CompareTo(x.points + y.goldPoints));
            data.returnMessage = $"{Program.config.currency} Board:";
            int count = users.Count;
            if(count > 10) { count = 10; }
            for (int i = 0; i < count; i++)
            { data.returnMessage += $" {i + 1}. {users[i].name} - {users[i].points.ToString("N0")} |"; }
            return data;
        }
        public async Task<ProcessData> CheckFloor(ProcessData data){
            //get the user awesomebean_bot
            User user = await SaveSystem.GetUser("AwesomeBean_BOT");
            //get a random % between 5 and 30 of the user.points 
            int percent = new Random().Next(5, Program.GetMaxMultipliedPoints(30, data.user));
            int amount = (int)(user.points * (percent / 100.0));
            //remove the amount from the user
            user.points -= amount;
            Program.Log($"Floor has {user.points} points left");
            //save the user
            await SaveSystem.SaveUser(user);
            //set the message
            data.user.points += amount;
            data.returnMessage = $"@{data.message.sender} you scavenged {amount.ToString("N0")} {Program.config.currencies} from the floor!";
            return data;
        }
        public async Task<ProcessData> UseGold(ProcessData data, List<UserWithTimer> activeUsers){
            //convert activeUsers to a string list
            List<string> activeChat = new List<string>();
            for (int i = 0; i < activeUsers.Count; i++)
            { activeChat.Add(activeUsers[i].username); }
            Random random = new Random();
            //random bool
            bool isEvil = random.Next(0, 4) == 1;
            bool shared = random.Next(0, 3) == 1;
            //data.returnMessage = $"isEvil: {isEvil}, shared: {shared}";
            //return data;
            //bool isEvil = false;
            //bool shared = true;
            //if evil and shared
            if(isEvil && shared){
                int option = 1;
                if(option == 1){
                    //reduce all active users points by 10%
                    for (int i = 0; i < activeChat.Count; i++)
                    {
                        User user = await SaveSystem.GetUser(activeChat[i]);
                        //Console.WriteLine($"Loaded user {user.name}, with {user.points}");
                        user.points -= (int)(user.points * 0.1);
                        //Console.WriteLine($"Saving user {user.name}, with {user.points}");
                        await SaveSystem.SaveUser(user);
                    }
                    //set the message
                    data.returnMessage = $"@{data.message.sender} used a gold {Program.config.currency} and everyone lost 10% of their {Program.config.currencies}!";
                }
            }
            //if evil and not shared
            else if(isEvil && !shared){
                int option = 1;
                if(option == 1){
                    //reduce this users points by 10%
                    data.user.points -= (int)(data.user.points * 0.1);
                    data.returnMessage = $"@{data.message.sender} used a gold {Program.config.currency} and lost 10% of their {Program.config.currencies}!";
                }

            }
            //if not evil and shared
            else if(!isEvil && shared){
                int option = new Random().Next(1, 3);
                if(option == 1){
                    //get the wealiest user in active chat
                    int maxWealth = 0;
                    for(int i = 0; i < activeChat.Count; i++){
                        User user = await SaveSystem.GetUser(activeChat[i]);
                        if(user.points > maxWealth){ maxWealth = user.points; }
                    }
                    if(maxWealth < 10000){ maxWealth = 10000; }

                    int pointsperchatter = (int)Math.Floor((double)(maxWealth / activeChat.Count));
                    for (int i = 0; i < activeChat.Count; i++)
                    {
                        User user = await SaveSystem.GetUser(activeChat[i]);
                        //Console.WriteLine($"Loaded user {user.name}, with {user.points}");
                        user.points += pointsperchatter;
                        //Console.WriteLine($"Saving user {user.name}, with {user.points}");
                        await SaveSystem.SaveUser(user);
                    }
                    data.returnMessage = $"@{data.message.sender} you have blessed the chat with {pointsperchatter.ToString("N0")} {Program.config.currencies} each!";
                }
                else if(option == 2){
                    Program.TempMulti(2, 5);
                    data.returnMessage = $"@{data.message.sender} you have blessed the chat with a +{Program.globalMultiplier}x multiplier for 5 minutes!";
                }
            }
            //if not evil and not shared
            else if(!isEvil && !shared){
                int option = 1;
                if(option == 1){
                    
                    SaveSystem.AddUserTempMulti(data.user.name, 1, 5);
                    data.returnMessage = $"@{data.message.sender} been blessed with +1x multiplier for 5 minutes!";
                }
            }
            return data;
        }

        public async Task<ProcessData> ResetEconomy(ProcessData data){
            if(!resetAsked){
                resetAsked = true;
                data.returnMessage = $"@{data.message.sender} this action is non-reversable, type the command again to confirm.";
                return data;
            }
            //message will be ResetEconomy 100 100
            try{
                string[] args = data.message.content.Split(' ');
                int pointCap = int.Parse(args[1]);
                int goldCap = int.Parse(args[2]);
                int ttsCap = int.Parse(args[3]);
                User[] users = await SaveSystem.GetAllUsers();
                bool e = false;
                for (int i = 0; i < users.Length; i++)
                {
                    e = false;
                    if(users[i].points > pointCap){
                        users[i].points = pointCap; e = true;
                    }
                    if(users[i].goldPoints > goldCap){
                        users[i].goldPoints = goldCap; e = true;
                    }
                    if(e){ SaveSystem.SaveUser(users[i]); }
                }
                data.returnMessage = $"@{data.message.sender} economy reset!";
                resetAsked = false;
                return data;
            }
            catch{
                data.returnMessage = $"@{data.message.sender} invalid syntax, please use the format: {Program.config.prefix}bot.ResetEconomy ({Program.config.currencies}Max) (gold{Program.config.currencies}max) (ttsTokensmax)";
                return data;
            }
            

        }
    }
}
