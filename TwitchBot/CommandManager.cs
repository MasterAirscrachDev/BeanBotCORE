using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace TwitchBot
{
    public class CommandManager
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        List<string> chatDecay = new List<string>(), hasFreebeans = new List<string>();
        List<UserWithTimer> activeChat = new List<UserWithTimer>();
        List<CommandCooldown> commandCooldowns = new List<CommandCooldown>();
        string CouterName = "Counter";
        string[] botsToIgnore = new string[0];
        //Bot bot;
        public CommandManager()
        { Ticker(); GetBlacklist(); }
        PointCommands beanCommands = new PointCommands();
        public MinigameCommands minigames = new MinigameCommands();
        TextToSpeech TTSmanager = new TextToSpeech();
        Utility utility = new Utility();
        bool lockCustom = false;
        public async Task ProcessMessage(Message message)
        {
            //check if the message is from a bot
            if (botsToIgnore.Contains(message.sender.ToLower()) || !Program.authConfirmed) { return; }
            GetPoints(message);
            //get the user from the savesystem getUser task
            ProcessData data = new ProcessData();
            data.user = await SaveSystem.GetUser(message.sender);
            data.user.name = message.sender;
            data.message = message;
            if(message.bits > 0){ data.user.points += message.bits * 8; Program.customCommands.BitsCommand(data, message.bits); }
            //load tempmultiplier if the user has one
            //BASE COMMANDS ========================================================================
            if (CheckMessage(message, "hi"))
            { await Program.SendMessage($"Howdy @{message.sender}, type !help to get started"); return; }
            else if (CheckMessage(message, "help", 0, false, 1))
            {
                data = Program.helpSystem.Help(data);
                await Program.SendMessage(data.returnMessage, data.message.isWhisper ? data.message.sender : null); return;
            }
            else if(message.sender.ToLower() == Program.config.channel && CheckMessage(message, $"bot.commands")){
                Program.helpSystem.ListAllCommands(); return;
            }
            //BEAN COMMANDS ========================================================================
            else if (CheckMessage(message, Program.config.currencies, 0, false, 1))
            {
                await Program.SendMessage($"@{data.message.sender} you have {data.user.points.ToString("N0")} ({data.user.goldPoints.ToString("N0")}) {Program.config.currencies.ToLower()} and a {data.user.multiplier + data.user.TempMultiplier}x Multi! ({data.user.ttsTokens} TTS)", data.message.isWhisper ? data.message.sender : null);
                return;
            }
            else if (CheckMessage(message, $"give{Program.config.currencies}"))
            {
                data = await beanCommands.GivePoints(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if (CheckMessage(message, $"edit{Program.config.currencies}"))
            {
                data = await beanCommands.EditPoints(data);
                Program.SendMessage(data.returnMessage); return;
            }
            else if (CheckMessage(message, $"{Program.config.currency}board", 200, true))
            {
                Program.SendMessage($"Getting the {Program.config.currency}board, this may take a moment...");
                data = await beanCommands.ScoreBoard(data);
                await Program.SendMessage(data.returnMessage); SaveSystem.UpdateAllUsers(); return;
            }
            else if (CheckMessage(message, $"free{Program.config.currencies}"))
            {
                if (!hasFreebeans.Contains(message.sender))
                {
                    hasFreebeans.Add(message.sender);
                    int points = Program.GetMaxMultipliedPoints(Program.config.dailyPoints, data.user);
                    data.user.points += points;
                    data.user.ttsTokens += Program.config.baseTTSTokens;
                    await Program.SendMessage($"@{message.sender} you have been given {points} {Program.config.currencies.ToLower()}!");
                    DelaySave(data.user);
                }
                else
                { await Program.SendMessage($"@{message.sender} you have already claimed your free {Program.config.currencies.ToLower()} for this stream!"); }
                return;
            }
            else if (CheckMessage(message, "prestige"))
            {
                if (data.user.points == 1000000000)
                {
                    data.user.points = 0;
                    data.user.goldPoints++;
                    DelaySave(data.user);
                    await Program.SendMessage($"@{message.sender} you have prestiged! You now have {data.user.goldPoints} Golden {Program.config.currencies}!");
                }
                else
                { await Program.SendMessage($"@{message.sender} you need 1,000,000,000 {Program.config.currencies} to prestige!"); }
            }
            else if (CheckMessage(message, $"golden{Program.config.currency.ToLower()}blessing", 300))
            {
                if (data.user.goldPoints > 0)
                {
                    data.user.goldPoints--;
                    data = await beanCommands.UseGold(data, activeChat);
                    DelaySave(data.user);
                    await Program.SendMessage(data.returnMessage); return;
                }
                else
                { await Program.SendMessage($"@{message.sender} you need at least 1 Golden {Program.config.currencies} to use this command!"); }
            }
            else if (!Program.config.noFloor && CheckMessage(message, "floor", 120)){
                data = await beanCommands.CheckFloor(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage); return;
            }
            //MINIGAME COMMANDS ====================================================================
            else if(CheckMessage(message, $"coinflip", 45, false, 1))
            {
                data = minigames.CoinFlip(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage, data.message.isWhisper ? data.message.sender : null); return;
            }
            else if(CheckMessage(message, "quickmath", 120)){
                data = minigames.QuickMath(data, 100);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(CheckMessage(message, "answer")){
                data = minigames.MathAnswer(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if (CheckMessage(message, $"oneups", 300)){
                data = minigames.OneUps(data);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if (CheckMessage(message, $"playwithfire", 240)){
                data = minigames.PlayWithFire(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if (CheckMessage(message, $"picknum", 0, false, 2)){
                //split the string on the spaces
                string[] substrings = data.message.content.Split(' ');
                int num = 0;
                if(substrings.Length > 1){
                    try { num = int.Parse(substrings[1]); }
                    catch { return; }
                }
                await minigames.PickOneUpsNum(data.user.name, num); return;
            }
            else if((Program.config.minigamesCost > 0) && CheckMessage(message, $"openminigames", Program.config.minigamesCooldown, Program.config.noMinigamesStack)){
                data = minigames.OpenMinigames(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage); return;
            }
            //EVENT COMMANDS =======================================================================
            else if(CheckMessage(message, $"drop", 0, false, 1))
            {
                data = await Program.eventSystem.Collect(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(CheckMessage(message, "steal", 60)){
               data = await Program.eventSystem.Steal(data, activeChat.ToArray());
               DelaySave(data.user);
               await Program.SendMessage(data.returnMessage); return;
            }
            else if(CheckMessage(message, "catch", 0, false, 1)){
               Program.eventSystem.CancelSteal(data); return;
            }
            else if(CheckMessage(message, "buypadlock", 0, false, 1)){
                data = Program.eventSystem.BuyPadlock(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage, data.message.isWhisper ? data.message.sender : null); return;
            }
            else if(CheckMessage(message, "padlock", 0, false, 1)){
                data = Program.eventSystem.PadlockInfo(data);
                await Program.SendMessage(data.returnMessage, data.message.isWhisper ? data.message.sender : null); return;
            }
            //PREDICTION COMMANDS ==================================================================
            else if(message.usermod && CheckMessage(message, $"startprediction"))
            {
                data = Program.eventSystem.StartPrediction(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(message.usermod && CheckMessage(message, $"endprediction"))
            {
                data = await Program.eventSystem.EndPrediction(data);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(message.usermod && CheckMessage(message, $"lockprediction")){
                data = Program.eventSystem.LockPrediction(data);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(message.usermod && CheckMessage(message, $"cancelprediction")){
                data = await Program.eventSystem.CancelPrediction(data);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(CheckMessage(message, $"vote", 0 , false, 1))
            {
                data = Program.eventSystem.PredictionVote(data);
                DelaySave(data.user);
                await Program.SendMessage(data.returnMessage, data.message.isWhisper ? data.message.sender : null); return;
            }
            else if(CheckMessage(message, $"prediction"))
            {
                data = Program.eventSystem.ViewPrediction(data);
                await Program.SendMessage(data.returnMessage); return;
            }
            //MOD COMMANDS =========================================================================
            else if(message.usermod && CheckMessage(message, $"reload", 0, false, 1)){
                Program.customCommands.ReloadCommands();
                TTSmanager.ReloadTTS();
                Program.RefreshConfig();
                await Program.SendMessage($"@{message.sender} config and commands reloaded", data.message.isWhisper ? data.message.sender : null); 
                GetBlacklist();
                Program.helpSystem.ReloadHelp(); return;
            }
            //UTILITY COMMANDS =====================================================================
            else if(message.usermod && CheckMessage(message, $"setcounter")){
                data = utility.SetName(data);
                CouterName = data.returnMessage.Split(':')[0];
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(message.usermod && CheckMessage(message, $"clearcounter")){
                utility.ClearCounter(); return;
            }
            else if(message.usermod && CheckMessage(message, CouterName)){
                data = utility.ChangeCounter(data);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(CheckMessage(message, CouterName)){
                data = utility.GetCounter(data);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(message.sender.ToLower() == message.channel && CheckMessage(message, "bot.config")){
                utility.OpenConfigFolder(); return;
            }
            else if(message.sender.ToLower() == message.channel && CheckMessage(message, "bot.reseteconomy")){
                data = await beanCommands.ResetEconomy(data);
                await Program.SendMessage(data.returnMessage); return;
            }
            else if(message.sender.ToLower() == message.channel && CheckMessage(message, "bot.processes")){
                ExeFocusChecker.ListAllApplications(); return;
            }
            else if(message.sender.ToLower() == message.channel && CheckMessage(message, "auth:", 0, false, 2)){
                //get the authkey
                string authKey = data.message.content.Split(':')[1];
                string key = Program.GenerateCode(Program.config.channel);
                //check if the key is valid
                if(authKey != key){ Program.Log($"Wrong AuthKey, Use AUTH:{key}", MessageType.Warning); return; }
                Program.Log($"AuthKey: {authKey}", MessageType.Success);
                SaveAuthKey(authKey);
            }
            else if(message.sender.ToLower() == message.channel && CheckMessage(message, "key:", 0, false, 2)){
                //remove the KEY: part
                int v = 4; if (data.message.content[4] == ' ') { v = 5; }
                
                string token = data.message.content.Remove(0, v);
                if(token.StartsWith("http://localhost:3000/?code=")){
                    Program.twitchLibInterface.bot.GetAccessToken(token); //is actaully the url
                }
            }
            else if(message.usermod && CheckMessage(message, "bot.title")){
                Program.twitchLibInterface.bot.SetTitle(data.message.content.Remove(0, 10)); return;
            }
            else if(message.usermod && CheckMessage(message, "bot.lock")){
                lockCustom = !lockCustom; Program.SendMessage($"CustomCommandsLocked: {lockCustom}"); return;
            }
            //DEV COMMANDS ==========================================================================
            else if(message.sender == "MasterAirscrach" && CheckMessage(message, "dev.getfollowers")){
                //split message on space
                List<string> users = await Program.twitchLibInterface.bot.GetFollowers(); 
                Program.Log($"users: {users.Count}", MessageType.Debug);
                foreach(string user in users){
                    Program.Log(user, MessageType.Debug);
                }
                return;
            }
            else if(message.sender == "MasterAirscrach" && CheckMessage(message, "dev.getsubs")){
                //split message on space
                List<UserSub> users = await Program.twitchLibInterface.bot.GetSubscribers();
                foreach(UserSub user in users){
                    Program.Log($"{user.username} {user.plan}", MessageType.Debug);
                }
                return;
            }
            else if(message.sender == "MasterAirscrach" && CheckMessage(message, "ekey:", 0, false, 2)){
                //remove the KEY: part
                int v = 5; if (data.message.content[5] == ' ') { v = 6; }
                string token = data.message.content.Remove(0, v);
                //Console.WriteLine($"Token: {token}");
                if(token.StartsWith("http://localhost:3000/?code=")){
                    Program.twitchLibInterface.bot.GetAccessToken(token, true); //is actaully the url
                }
            }
            else if(message.sender == "MasterAirscrach" && CheckMessage(message, "fakebits")){
                //split message on space
                string[] split = message.content.Split(' ');
                int bits2 = int.Parse(split[1]);
                Program.customCommands.BitsCommand(data, bits2);
                return;
            }
            else if(message.sender == "MasterAirscrach" && CheckMessage(message, "return", 0, false, 1))
            { await Program.SendMessage($"{message.content.Remove(0,7)}"); return; }
            else if(message.sender == "MasterAirscrach" && CheckMessage(message, "EmergencyToken")){
                Program.twitchLibInterface.bot.GetBotAuthURL(); return;
            }
            else if(message.sender == "MasterAirscrach" && CheckMessage(message, "surl", 0, false, 1)){
                Task.Run(() => utility.PlayAudioFromUrl(message.content.Remove(0,5))); return;
            }
            else if(message.sender == "MasterAirscrach" && CheckMessage(message, "curl", 0, false, 1)){
                Task.Run(() => Program.customCommands.ReadTextFileFromUrl(message.content.Remove(0,5))); return;
            }
            //TTS COMMANDS =========================================================================
            else if (CheckMessage(message, $"tts", 0, false, 1) && Program.config.ttsCost != -1)
            { RunTTS(data); return; } //this has its own function to prevent asnyc weirdness
            else if(CheckMessage(message, $"buytts", 0, false, 1) && Program.config.ttsCost != -1){
                data = TTSmanager.BuyTTS(data); DelaySave(data.user); Program.SendMessage(data.returnMessage, data.message.isWhisper ? data.message.sender : null); return;
            }
            else if(message.usermod && CheckMessage(message, $"edittts") && Program.config.ttsCost != -1){
                await TTSmanager.EditTTS(data); Program.SendMessage(data.returnMessage); return;
            }
            else if(message.usermod && (CheckMessage(message, $"stopsound") || CheckMessage(message, $"ss")))
            { TTSmanager.StopTTS(); Program.customCommands.StopAllSounds(); return; }
            else if(message.sender.ToLower() == Program.config.channel && CheckMessage(message, "bot.voices")){
                TTSmanager.ListVoices(); return;
            }
            //CUSTOM COMMANDS ======================================================================
            else if(!lockCustom) { Program.customCommands.CustomCommand(data); }
        }
        public int GetActiveChatters() { return activeChat.Count; }
        public bool UserInChat(string user) { 
            for(int i = 0; i < activeChat.Count; i++){
                if(activeChat[i].username == user){ return true; }
            }
            return false;
        }
        async Task RunTTS(ProcessData data){
            data = await TTSmanager.Say(data);
            DelaySave(data.user);
            await Program.SendMessage(data.returnMessage, data.message.isWhisper ? data.message.sender : null); return;
        }
        public async Task GetPoints(Message message)
        {
            if (botsToIgnore.Contains(message.sender.ToLower())) { return; }
            AddUserToActiveChat(message.sender);
            SetEventDropCount();
            if (!chatDecay.Contains(message.sender))
            { 
                User user = await SaveSystem.GetUser(message.sender);
                chatDecay.Add(message.sender);
                //add 1 to 3 beans to user
                int max = Program.GetMaxMultipliedPoints(3, user);
                user.points += new Random().Next(1, max + 1);
                ReAddUser(message.sender);
                Program.Log($"{message.sender} is on cooldown", MessageType.Warning);
                await SaveSystem.SaveUser(user);
            }
        }
        void SetEventDropCount()
        {
            try
            {
                float dropcount = activeChat.Count * (Program.config.dropPercent / 100f);
                if (dropcount < 1) { dropcount = 1; }
                //round dropcount up
                int send = (int)Math.Ceiling(dropcount);
                //Console.WriteLine($"dropf{dropcount} | drops{send}");
                Program.eventSystem.newDrops = send;
            }
            catch
            { Program.eventSystem.newDrops = 1; }
            
        }
        public static async Task DelaySave(User user)
        {
            await Task.Delay(100);
            await SaveSystem.SaveUser(user);
        }
        async Task ReAddUser(string username)
        {
            //wait for 1 minute
            await Task.Delay(60000);
            chatDecay.Remove(username);
            Program.Log($"{username} is off cooldown", MessageType.Warning);
        }
        void AddUserToActiveChat(string username)
        {
            //if the user is not in the active chat list
            //add them to the list with 5 Time to inactive
            for(int i = 0; i < activeChat.Count; i++)
            {
                if(activeChat[i].username == username)
                { activeChat[i].Timer = 8; return; }
            }
            activeChat.Add(new UserWithTimer(username, 8));
        }
        async Task Ticker()
        {
            int count = 0;
            while(true){
                count++;
                //wait for 1 second
                await Task.Delay(1000);
                //loop through the command cooldown list and reduce the cooldown by 1
                for (int i = 0; i < commandCooldowns.Count; i++)
                { commandCooldowns[i].timeLeft--; }
                //remove any commands that have a cooldown of 0
                for (int i = 0; i < commandCooldowns.Count; i++)
                {
                    if (commandCooldowns[i].timeLeft < 1)
                    { commandCooldowns.RemoveAt(i); i--; }
                }
                if(count == 60){
                    count = 0;
                    //loop through the active chat list and reduce the time to inactive by 1
                    for (int i = 0; i < activeChat.Count; i++)
                    { 
                        activeChat[i].Timer --;
                        if (activeChat[i].Timer < 1)
                        { activeChat.RemoveAt(i); i--; }
                    }
                }
            }
        }
        public bool CheckMessage(Message message, string command, int cooldown = 0, bool global = false, int allowWhisper = 0)
        {
            //split the string on the spaces
            //string[] substrings = message.Content.Split(' ');
            if (message.content.ToLower().StartsWith(command.ToLower())) {
                //0 = no whisper, 1 = whisper, 2 = only whisper
                
                if (cooldown > 0) {
                    for(int i = 0; i < commandCooldowns.Count; i++) {
                        if ((commandCooldowns[i].username == message.sender && commandCooldowns[i].command == command) || (commandCooldowns[i].username == "@" && commandCooldowns[i].command == command))
                        {
                            //convert the time left to a string of m:s
                            TimeSpan t = TimeSpan.FromSeconds(commandCooldowns[i].timeLeft);
                            string timeLeft = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
                            string reply = global ? $"@{message.sender} this command is being used to much. wait {timeLeft}" : $"@{message.sender} your using this command to much. wait {timeLeft}";

                            Program.SendMessage(reply, message.isWhisper ? message.sender : null);
                            return false;
                        }
                    }
                    string send = global ? "@" : message.sender;
                    commandCooldowns.Add(new CommandCooldown(command, send, cooldown));
                }
                if (allowWhisper == 1 && message.isWhisper) { return true; }
                else if (allowWhisper == 2 && !message.isWhisper) { return false; }
                else if (allowWhisper == 0 && message.isWhisper) { return false; }
                else { return true; }//should never get here
            }
            else { return false; }
        }
        public void ResetCooldown(string name, string command){
            for(int i = 0; i < commandCooldowns.Count; i++){
                if(commandCooldowns[i].username == name && commandCooldowns[i].command == command){
                    commandCooldowns.RemoveAt(i);
                    return;
                }
            }
        }
        async Task SaveAuthKey(string authKey)
        {
            FileSuper fileSuper = new FileSuper("BeanBot", "ReplayStudios");
            fileSuper.SetEncryption(true, SpecialDat.AuthEnc);
            Save save = new Save();
            save.SetString("AuthKey", authKey);
            await fileSuper.SaveFile("Auth.key", save);
            await Program.SendMessage($"Bot Authenticated Successfully");
            //lauch the bot and close this one
            string appPath = Process.GetCurrentProcess().MainModule.FileName;
            // Launch a new instance of the application
            Process.Start(appPath);
            // Close the current instance of the application
            Environment.Exit(0);
        }
        public static bool CanAfford(string bet, User user)
        {
            try{
                int betInt = int.Parse(bet);
                if (user.points >= betInt && betInt > 0)
                { return true; }
                else { return false; }
            }
            catch{
                if (bet.Contains("all"))
                {
                    if (user.points > 0) { return true; }
                    else { return false; }
                }
                else { return false; }
            }
        }
        public static int PointsFromString(string bet, User user)
        {
            try
            {
                int betInt = int.Parse(bet);
                return betInt;
            }
            catch
            {
                if (bet.Contains("all"))
                { return user.points; }
                else
                { return 0; }
            }
        }
        async Task GetBlacklist(){
            string[] content = SaveSystem.GetPlaintextFile("userBlacklist.txt");
            if(content == null){
                await SaveSystem.CreatePlaintextFile("userBlacklist.txt", "users that cannot use the bot, one entry per line");
                return;
            }
            //loop through the content and add each line to the blacklist noting that blacklist is an array
            List <string> blacklist = new List<string>();
            for(int i = 0; i < content.Length; i++){
                blacklist.Add(content[i].ToLower());
            }
            
            blacklist.AddRange(SpecialDat.botsBlacklist);
            botsToIgnore = blacklist.ToArray();
        }
        public MinigameCommands GetMinigameCommands() { return minigames; }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
}
public struct ProcessData
{
    public Message message;
    public string returnMessage;
    public User user;
}
class UserWithTimer
{
    public string username;
    public int Timer;
    public UserWithTimer(string username, int Timer)
    {
        this.username = username;
        this.Timer = Timer;
    }
}
class CommandCooldown{
    public string command, username;
    public int timeLeft;
    public CommandCooldown(string command, string username, int timeLeft)
    {
        this.command = command;
        this.username = username;
        this.timeLeft = timeLeft;
    }
}