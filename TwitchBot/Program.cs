using System;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using System.Collections.Generic;
using System.Linq;

namespace TwitchBot
{
    class Program
    {
        public static List<string> sendList = new List<string>();
        public static BotSystem config;
        public static string version = "v1.3.5",
        decodeString = null;
        public static EventSystem eventSystem;
        public static CommandManager commandManager;
        public static TwitchLibInterface twitchLibInterface;
        public static CustomCommands customCommands;
        public static HelpCommands helpSystem;
        public static WebRenderer webRenderer;
        public static float globalMultiplier = 0;
        static int rateLimit = 20, rate = 0;
        public static bool allowDebug = false, authConfirmed = false;
        public static Save serverData = null;
        static async Task Main(string[] args)
        {
            ToggleConsoleQuickEdit.NoEdit(); //disable quick edit (so the bot cant be paused by clicking on the console)
            await RefreshConfig(); //get the config
            if(config == null){Log("Error: Config Failed To Load", MessageType.Error); await Task.Delay(-1);}
            else if(config.channel.ToLower() == "channelgoeshere"){
                Core core = new Core(); //create a new core
                Log("SETUP 0/6", MessageType.Log);
                //Log("Please change the Channel Name in the config to your twitch name", MessageType.Log);
                //Log("After you have changed it restart the bot", MessageType.Log);
                //Log("", MessageType.Log);
                //Log("If your name is channelgoeshere, please contact the developer", MessageType.Warning);
                //Utility u = new Utility();
                //u.OpenConfig(); //open the config
                //await Task.Delay(-1);
                ToggleConsoleQuickEdit.EnableEdit(); //disable quick edit (so the bot cant be paused by clicking on the console)
                bool nameSet = false;
                while (!nameSet){
                    Log("Please Enter Your Twitch username (not caps sensitive)");
                    Console.ResetColor();
                    string name = Console.ReadLine();
                    //check that name is at between 4 and 25 chars
                    if(name.Length < 4 || name.Length > 25){ Log("Name must be between 4 and 25 chars", MessageType.Warning); continue; }
                    else{
                        //check that name is only letters and numbers
                        bool valid = true;
                        foreach(char c in name){
                            if(!char.IsLetterOrDigit(c)){ valid = false; break; }
                        }
                        if(valid){
                            bool sure = GetConsoleYN($"Is {name} correct?");
                            if(sure){
                                config.channel = name;
                                nameSet = true;
                            }
                        }
                    }
                }
                Log("SETUP 1/6", MessageType.Log);
                bool prefixSet = false;
                if(!GetConsoleYN($"Chat Prefix is {config.prefix}, keep this?")){
                    while(!prefixSet){
                        Log("Please Enter Your Desired Chat Prefix");
                        Console.ResetColor();
                        string prefix = Console.ReadLine();
                        //check that prefix is at between 1 and 5 chars
                        if(prefix.Length < 1 || prefix.Length > 5){ Log("Prefix must be between 1 and 5 chars", MessageType.Warning); continue; }
                        else{
                            //check that prefix is only letters and numbers
                            bool sure = GetConsoleYN($"Is {prefix} correct?");
                            if(sure){
                                config.prefix = prefix;
                                prefixSet = true;
                            }
                        }
                    }
                }
                Log("SETUP 2/6", MessageType.Log);
                bool currencyNameSet = false;
                if(!GetConsoleYN($"Currency Name is {config.currency}/{config.currencies}, keep this?")){
                    while(!currencyNameSet){
                        Log("Please Enter Your Desired Currency Name (singular, eg: bean)");
                        Console.ResetColor();
                        string currency = Console.ReadLine();
                        //check that currency is at between 1 and 5 chars
                        if(currency.Length < 3 || currency.Length > 20){ Log("Currency must be between 1 and 20 chars", MessageType.Warning); continue; }
                        else{
                            //check that currency is only letters and numbers
                            bool sure = GetConsoleYN($"Is {currency} correct?");
                            if(sure){
                                bool pluralSet = false;
                                Log("Please Enter Your Desired Currency Name (plural, eg: beans)");
                                Console.ResetColor();
                                string currencies = Console.ReadLine();
                                //check that currency is at between 2 and 20 chars
                                if(currencies.Length < 3 || currencies.Length > 20){ Log("Currency must be between 2 and 20 chars", MessageType.Warning); continue; }
                                else{
                                    //check that currency is only letters and numbers
                                    sure = GetConsoleYN($"Is {currency}/{currencies} correct?");
                                    if(sure){
                                        config.currency = currency;
                                        config.currencies = currencies;
                                        currencyNameSet = true;
                                    }
                                }

                            }
                        }
                    }
                }
                Log("SETUP 3/6", MessageType.Log);
                bool useTTS = GetConsoleYN($"Enable Text To Speech?");
                config.ttsCost = useTTS ? config.ttsCost : -1;
                if(useTTS){ Log("Feel Free to customize the TTS to your liking anytime in the confg", MessageType.Log); }
                else{
                    Log("TTS Disabled, you can change this anytime in the config", MessageType.Log);
                }
                Log("SETUP 4/6", MessageType.Log);
                Log("BeanBot allows the optional use of an online command list that will shows all your commands");
                Log("You can see an example here: https://github.com/MasterAirscrachDev/BeanBotFullCommands/blob/main/masterairscrach%20Commands.md");
                bool useCommands = GetConsoleYN($"Enable Online Command List?");
                config.uploadFullCommandList = useCommands;
                if(useCommands){ Log("Online Help enabled, disable anytime in the confg", MessageType.Log); }
                else{
                    Log("Online Help Disabled, you can change this anytime in the config", MessageType.Log);
                }
                ToggleConsoleQuickEdit.NoEdit(); //disable quick edit (so the bot cant be paused by clicking on the console)
                await core.SaveSettings(config); //save the config
            }
            LaunchServer(); //launch the server
            //The Order Of these matters a lot
            commandManager = new CommandManager();
            customCommands = new CustomCommands();
            helpSystem = new HelpCommands();
            eventSystem = new EventSystem(true, commandManager);
            twitchLibInterface = new TwitchLibInterface();

            Task.Run(() => Setup()); //start cleanup and setup
            string realAuthKey = GenerateCode(config.channel); //generate authkey
            //load the saved authkey for comparison
            FileSuper fileSuper = new FileSuper("BeanBot", "ReplayStudios");
            fileSuper.SetEncryption(SpecialDat.AuthEnc);
            Save save = await fileSuper.LoadFile("Auth.key");
            authConfirmed = false;
            if(save != null)
            {
                //confirm the authkey if it matches
                string authKey = save.GetString("AuthKey");
                if (authKey == realAuthKey) { authConfirmed = true; }
            }
            if(!authConfirmed){
                Log("SETUP 5/6", MessageType.Log);
                Log($@"Send Any Message in your twitch chat, then
Send The Following Whisper To The Bot to Authenticate
/w @AwesomeBean_BOT AUTH:{realAuthKey}
", MessageType.Debug);
                await Task.Delay(-1);
            }
            if(SpecialDat.devs.Contains(config.channel)){allowDebug = true;} //enable debugging for masterairscrach
            twitchLibInterface.bot.AuthConfirmed(); //alert the botcode that the authkey is confirmed
            //GlobalKeyListener.ListenForSS(); //this causes errors atm

            Log("Starting WebRenderer", MessageType.Log);
            webRenderer = new WebRenderer(); //create a new web renderer
            webRenderer.StartRenderer(); //start the web renderer


            await Task.Delay(-1); //wait forever
        }
        static async Task Setup(){
            Log("Clearing Users", MessageType.Success);
            await SaveSystem.ClearOldUsers(); //clear old users
            Log("Doing Taxes", MessageType.Success);
            EmptyRateBucket(); //start emptying the rate bucket
        }
        static async Task LaunchServer(){
            Save t = await GetServerData();
            if(t != null){
                string version = t.GetString("Version");
                Utility u = new Utility();
                if(version != Program.version){ await u.UpdateBot(version); }
                else{
                    FileSuper ss = new FileSuper("BeanBot","ReplayStudios");
                    Save s = await ss.LoadFile("UPDATE");
                    if(s != null){
                        //delete update file
                        SaveSystem.DeleteFile("UPDATE");
                    }
                    Log("", MessageType.Success);
                    Log($"You are on Latest version: {Program.version}", MessageType.Success);
                    Log("Thank you for using beanbot :)", MessageType.Success);
                    Log("", MessageType.Success);
                }
            }
            //Log($"Server Version: {t.GetString("Version")}", MessageType.Success);
            //Log($"Server AccessToken: {t.GetString("AccessToken")}", MessageType.Success);
            //Log($"Server GitHubToken: {t.GetString("GitHubToken")}", MessageType.Success);
        }
        public static async Task RefreshConfig(){
            Core core = new Core(); //create a new core
            config = await core.GetSettings(); //get the settings
            core = null; //destroy the core
        }
        static async Task EmptyRateBucket()
        {
            while (true){
                //wait for 30 seconds
                await Task.Delay(30000); rate = 0; //reset the rate
            }
        }
        public static void ConfirmMod(){
            rateLimit = 100; //increase the rate limit
        }
        public static async Task SendMessage(string message, string whisperTarget = null)
        { 
            //Console.WriteLine($"Sending Message: {message}");
            if (!string.IsNullOrEmpty(message)) { 
                if(whisperTarget != null){ await SendWhisper(message, whisperTarget); return; } //send a whisper if the target is not null
                if(rate < rateLimit){
                    twitchLibInterface.bot.SendMessage(message); rate++; //send the message
                }
                else{
                    sendList.Add(message); //add the message to the list
                    //notify the user that the bot is ratelimited
                    string emb = (rateLimit == 100) ? ", Consider giving the bot mod in chat to increase the rateLimit" : "";
                    Log($"Ratelimited, Please Wait{emb}", MessageType.Warning);
                    //wait until bucket is empty
                    while(rate >= rateLimit){ await Task.Delay(1000); } //this cound bug if there are somehow 2 on the same ms
                    //send all messages in list
                    twitchLibInterface.bot.SendMessage(message); rate++; //send the message
                }
            }
        }
        static async Task SendWhisper(string message, string user)
        {
            try{ //remove the user from the message if it is there
                if(message.StartsWith($"@{user}")){ message = message.Remove(0, user.Length + 1);}
                await twitchLibInterface.bot.SendWhisper(message, user); //send the whisper
            }
            catch(Exception e){
                Log($"Error Sending Whisper: {e.Message}", MessageType.Error); //log the error
            }
        }
        public static void LogMessage(bool command, Message message, float ratio)
        { //used for logging chat messages
            Console.BackgroundColor = command ? ConsoleColor.DarkBlue : ConsoleColor.DarkGray;
            Console.WriteLine($"{message.sender} said `{message.content}` ||spm:{ratio}, Ratelimits:{rate}/{rateLimit}");
        }
        public static void Log(string content, MessageType m = MessageType.Log){
            //used for logging other things
            if(m == MessageType.Log){ Console.BackgroundColor = ConsoleColor.DarkGray; }
            else if(m == MessageType.Error){ Console.BackgroundColor = ConsoleColor.DarkRed; }
            else if(m == MessageType.Warning){ Console.BackgroundColor = ConsoleColor.DarkYellow; }
            else if(m == MessageType.Success){ Console.BackgroundColor = ConsoleColor.DarkGreen; }
            else if(m == MessageType.Debug){ Console.BackgroundColor = ConsoleColor.Black; }
            Console.WriteLine(content);
        }
        public static async Task TempMulti(float multiplier, int time)
        {   //used for temp global multipliers
            globalMultiplier += multiplier;
            await Task.Delay(time * 60000);//time is in minutes
            globalMultiplier -= multiplier;
        }
        //all the multiplier grabbers
        public static int GetMaxMultipliedPoints(int points, User user)
        { return (int)Math.Floor(points * (user.multiplier + user.TempMultiplier + globalMultiplier)); }
        public static int GetUserMultipliedPoints(int points, User user)
        { return (int)Math.Floor(points * (user.multiplier + user.TempMultiplier)); }
        public static int GetGlobalMultipliedPoints(int points)
        { return (int)Math.Floor(points * (1 + globalMultiplier)); }
        
        public static string GenerateCode(string input)
        {
            // Get the hash code of the input string
            int hash = input.GetHashCode();
            // Get the absolute value of the hash code
            int code = Math.Abs(hash);
            // Get the last 6 digits of the code
            int shortCode = code % 1000000;
            // Return the code as a string
            return shortCode.ToString("D6");
        }
        public static async Task<Save> GetServerData(int recuse = 0){
            NetSys.Client client = new NetSys.Client();
            client.Connect(SpecialDat.GetServerIP(config.channel));
            //client.Connect("192.168.0.50");
            string exepath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(":","։");
            //Log(exepath);
            string secret = "";
            //generate a random 20-33 char string
            Random r = new Random();
            int length = r.Next(20, 33);
            for(int i = 0; i < length; i++){ secret += (char)r.Next(33, 126); }
            secret = secret.Replace(":", "");
            await client.SendData($"{exepath}:{secret}",2, true);
            int timeout = 0;
            while(decodeString == null){ 
                await Task.Delay(1000);
                timeout++;
                if(timeout > 10){ 
                    await client.Disconnect(); 
                    if(recuse < 5){ return await GetServerData(recuse + 1); }
                    else{Log("Failed To Contact Server after 5 attemts", MessageType.Error); return null; }
                }
            }
            //Log($"Got Data From Server: {decodeString}", MessageType.Success);
            string data = SpecialDat.DecodeString(decodeString, secret);
            FileSuper fs = new FileSuper("BeanBot", "ReplayStudios");
            //Log($"Decoded Data From Server: {data}", MessageType.Success);
            decodeString = null;
            Save save = fs.LoadSaveFromRaw(data);
            await client.Disconnect();
            serverData = save;
            if(save == null){ return null; }
            return save;
        }
        public static bool GetConsoleYN(string question){
            Log($"{question} (y/n)", MessageType.Debug);
            while(true){
                Console.ResetColor();
                string input = Console.ReadLine();
                if(input.ToLower() == "y"){ return true; }
                else if(input.ToLower() == "n"){ return false; }
                else{ Log($"{question} (y/n)", MessageType.Warning); }
            }
        }
    }
}
public class Message
{
    public string sender, content, channel, messageID, color;
    public bool usermod, firstMessage, isWhisper, userdev;
    public int bits;
}
public enum MessageType
{
    Log, Warning, Error, Debug, Success
}