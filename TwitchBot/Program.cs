using System;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using System.Collections.Generic;

namespace TwitchBot
{
    class Program
    {
        public static List<string> sendList = new List<string>();
        public static BotSystem config;
        public static string version = "v1.2.8";
        public static EventSystem eventSystem;
        public static CommandManager commandManager;
        public static TwitchLibInterface twitchLibInterface;
        public static CustomCommands customCommands;
        public static HelpCommands helpSystem;
        public static float globalMultiplier = 0;
        static int rateLimit = 20, rate = 0;
        public static bool allowDebug = false, authConfirmed = false;
        static async Task Main(string[] args)
        {
            DisableConsoleQuickEdit.Go(); //disable quick edit (so the bot cant be paused by clicking on the console)
            await RefreshConfig(); //get the config
            if(config == null){Log("Error: Config Failed To Load", MessageType.Error); await Task.Delay(-1);}
            else if(config.channel.ToLower() == "channelgoeshere"){
                Log("Error: Config Failed To Load, Please Change The Channel Name", MessageType.Warning);
                Log("If your name is channelgoeshere, please contact the developer", MessageType.Warning);
                await Task.Delay(-1);
            }
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
            fileSuper.SetEncryption(true, SpecialDat.AuthEnc);
            Save save = await fileSuper.LoadFile("Auth.key");
            authConfirmed = false;
            if(save != null)
            {
                //confirm the authkey if it matches
                string authKey = save.GetString("AuthKey");
                if (authKey == realAuthKey) { authConfirmed = true; }
            }
            if(!authConfirmed){
                Log($@"Send Any Message in chat, then
Send The Following Whisper To The Bot to Authenticate
/w @AwesomeBean_BOT AUTH:{realAuthKey}
", MessageType.Debug);
                await Task.Delay(-1);
            }
            if(config.channel == "masterairscrach"){allowDebug = true;} //enable debugging for masterairscrach
            twitchLibInterface.bot.AuthConfirmed(); //alert the botcode that the authkey is confirmed
            //GlobalKeyListener.ListenForSS(); //this causes errors atm
            await Task.Delay(-1); //wait forever
        }
        static async Task Setup(){
            Log("Clearing Users", MessageType.Success);
            await SaveSystem.ClearOldUsers(); //clear old users
            Log("Doing Taxes", MessageType.Success);
            await SaveSystem.UpdateAllUsers(true); //update all users multipliers and do taxes
            EmptyRateBucket(); //start emptying the rate bucket
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
    }
}
public class Message
{
    public string sender, content, channel, messageID, color;
    public bool usermod, firstMessage, isWhisper;
    public int bits;
}
public enum MessageType
{
    Log, Warning, Error, Debug, Success
}