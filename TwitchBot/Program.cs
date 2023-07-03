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
        public static string version = "v1.2.5";
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
            DisableConsoleQuickEdit.Go();
            //Console.WriteLine("Loading Config");
            await RefreshConfig();
            if(config == null){Log("Error: Config Failed To Load", MessageType.Error); await Task.Delay(-1);}
            else if(config.channel.ToLower() == "channelgoeshere"){
                Log("Error: Config Failed To Load, Please Change The Channel Name", MessageType.Warning);
                Log("If your name is channelgoeshere, please contact the developer", MessageType.Warning);
                await Task.Delay(-1);
            }
            //Console.WriteLine("Config Loaded");
            //The Order Of these matters a lot
            commandManager = new CommandManager();
            customCommands = new CustomCommands();
            helpSystem = new HelpCommands();
            eventSystem = new EventSystem(true, commandManager);
            twitchLibInterface = new TwitchLibInterface();

            Task.Run(() => Setup());
            //Console.WriteLine("Generating AuthKey");
            string realAuthKey = GenerateCode(config.channel);
            //check authkey
            //Console.WriteLine("Loading saved AuthKey");
            FileSuper fileSuper = new FileSuper("BeanBot", "ReplayStudios");
            fileSuper.SetEncryption(true, SpecialDat.AuthEnc);
            Save save = await fileSuper.LoadFile("Auth.key");
            authConfirmed = false;
            //Console.WriteLine("AuthKey Loaded");
            if(save != null)
            {
                //Console.WriteLine("AuthKey Found");
                string authKey = save.GetString("AuthKey");
                if (authKey == realAuthKey) { authConfirmed = true; }
            }
            if(!authConfirmed){
                Console.BackgroundColor = ConsoleColor.Black;
                Log($@"
Send The Following Whisper To The Bot to Authenticate
/w @AwesomeBean_BOT AUTH:{realAuthKey}
", MessageType.Debug);
                await Task.Delay(-1);
            }
            if(config.channel == "masterairscrach"){allowDebug = true;}
            //Console.WriteLine("AuthKey Confirmed");
            twitchLibInterface.bot.AuthConfirmed();
            await Task.Delay(-1);
        }
        static async Task Setup(){
            Log("Clearing Users", MessageType.Success);
            await SaveSystem.ClearOldUsers();
            Log("Doing Taxes", MessageType.Success);
            await SaveSystem.UpdateAllUsers(true);
            //Console.WriteLine("Starting RateBucket");
            EmptyRateBucket();
        }
        public static async Task RefreshConfig(){
            Core core = new Core();
            config = await core.GetSettings();
            core = null;
        }
        static async Task EmptyRateBucket()
        {
            while (true){
                //wait for 30 seconds
                await Task.Delay(30000); rate = 0;
            }
        }
        public static void ConfirmMod(){
            rateLimit = 100;
        }
        public static async Task SendMessage(string message, string whisperTarget = null)
        { 
            //Console.WriteLine($"Sending Message: {message}");
            if (!string.IsNullOrEmpty(message)) { 
                if(whisperTarget != null){ await SendWhisper(message, whisperTarget); return; }
                if(rate < rateLimit){
                    twitchLibInterface.bot.SendMessage(message); rate++;
                }
                else{
                    sendList.Add(message);
                    string emb = (rateLimit == 100) ? ", Consider giving the bot mod in chat to increase the rateLimit" : "";
                    Log($"Ratelimited, Please Wait{emb}", MessageType.Warning);
                    //wait until bucket is empty
                    while(rate >= rateLimit){ await Task.Delay(1000); }
                    //send all messages in list
                    twitchLibInterface.bot.SendMessage(message); rate++;
                }
            }
        }
        static async Task SendWhisper(string message, string user)
        { 
            try{
                if(message.StartsWith($"@{user}")){ message = message.Remove(0, user.Length + 1);}
                await twitchLibInterface.bot.SendWhisper(message, user);
            }
            catch(Exception e){
                Log($"Error Sending Whisper: {e.Message}", MessageType.Error);
            }
        }
        public static void LogMessage(bool command, Message message, float ratio)
        {
            Console.BackgroundColor = command ? ConsoleColor.DarkBlue : ConsoleColor.DarkGray;
            Console.WriteLine($"{message.sender} said `{message.content}` ||spm:{ratio}, Ratelimits:{rate}/{rateLimit}");
        }
        public static void Log(string content, MessageType m = MessageType.Log){
            if(m == MessageType.Log){ Console.BackgroundColor = ConsoleColor.DarkGray; }
            else if(m == MessageType.Error){ Console.BackgroundColor = ConsoleColor.DarkRed; }
            else if(m == MessageType.Warning){ Console.BackgroundColor = ConsoleColor.DarkYellow; }
            else if(m == MessageType.Success){ Console.BackgroundColor = ConsoleColor.DarkGreen; }
            else if(m == MessageType.Debug){ Console.BackgroundColor = ConsoleColor.Black; }
            Console.WriteLine(content);
        }
        public static async Task TempMulti(float multiplier, int time)
        {
            globalMultiplier += multiplier;
            //time is in minutes
            await Task.Delay(time * 60000);
            globalMultiplier -= multiplier;
        }

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