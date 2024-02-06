using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TwitchBot
{
    public class Core{
        public async Task<BotSystem> GetSettings(){
            //initialize the default settings
            BotSystem cfg = new BotSystem();
            cfg.channel = "ChannelGoesHere";
            cfg.name = "awesomebean_bot";
            cfg.prefix = "!";
            cfg.currency = "Bean";
            cfg.currencies = "Beans";
            cfg.warnIfBadSyntax = true;
            cfg.notifyOnJoin = true;
            cfg.notifyOnDropsRemaining = true;
            cfg.ttsCost = 50;
            cfg.baseTTSTokens = 5;
            cfg.ttsPerToken = 25;
            cfg.pointsFromDrop = 50;
            cfg.dailyPoints = 100;
            cfg.taxPercent = 3;
            cfg.taxThreshold = 50000;
            cfg.ttsVoice = "DEFAULT";
            cfg.t1TTS = 20; cfg.t2TTS = 40; cfg.t3TTS = 80;
            cfg.chatSayTTSCost = false;
            cfg.betaTTSFilter = false;
            cfg.dailyPoints = 100;
            cfg.pointsFromDrop = 50;
            cfg.viewerMultiplier = 1;
            cfg.followerMultiplier = 1.5f;
            cfg.t1points = 5000; cfg.t2points = 10000; cfg.t3points = 25000;
            cfg.t1Multiplier = 1.7f; cfg.t2Multiplier = 1.9f; cfg.t3Multiplier = 2.2f;
            cfg.t1gold = 5; cfg.t2gold = 10; cfg.t3gold = 20;
            cfg.noFloor = false;
            cfg.minigamesCost = 10000;
            cfg.minigamesDuration = 6;
            cfg.minigamesCooldown = 600;
            cfg.noMinigamesStack = false;
            cfg.uploadFullCommandList = true;
            cfg.dropTimer = 10; cfg.dropPercent = 60;
            cfg.giveTaxPercent = 0;
            cfg.ignoreToken = false;
            cfg.autoDeleteSpam = false;
            cfg.autoChannelPadlock = false;
            cfg.allowModpadlock = false;
            cfg.lurkCommands = false;
            cfg.noSteal = false;
            cfg.fullScriptDebugging = false;
            string writeConfig = GetConfigString(cfg);
            //check if the config file exists
            string[] lines = SaveSystem.GetPlaintextFile("config.dat");
            if(lines != null) {
                //go through each line
                for (int i = 0; i < lines.Length; i++)
                {
                    try{
                        if(lines[i].StartsWith("-")){ continue; }
                        string[] line = lines[i].Split('<');
                        string data = "";
                        if(line[1].Contains("/")){ //this handles compatibility with older versions
                            string[] line2 = line[1].Split('/');
                            data = line2[0];
                            //remove spaces from the end of the data
                            while(data.EndsWith(" ")){
                                data = data.Remove(data.Length - 1);
                            }
                        }
                        else{ data = line[1]; }
                        string key = line[0];
                        //Console.WriteLine($"`{key}` = `{data}`");

                        //read the config and set any values that are found
                        if (key == "ChannelName") { cfg.channel = data.ToLower(); }
                        else if (key == "Prefix") { cfg.prefix = data; }
                        else if (key == "Currency") { cfg.currency = data; }
                        else if (key == "Currencies") { cfg.currencies = data; }
                        else if (key == "WarnIfBadSyntax") { cfg.warnIfBadSyntax = Convert.ToBoolean(data); }
                        else if (key == "NotifyOnJoin") { cfg.notifyOnJoin = Convert.ToBoolean(data); }
                        else if (key == "NotifyOnDropsRemaining") { cfg.notifyOnDropsRemaining = Convert.ToBoolean(data); }
                        else if (key == "TextToSpeechCost") { cfg.ttsCost = Convert.ToInt32(data); }
                        else if (key == "PointsFromDrop") { cfg.pointsFromDrop = Convert.ToInt32(data); }
                        else if (key == "TaxPercent") { cfg.taxPercent = Convert.ToInt32(data); }
                        else if (key == "TaxThreshold") { cfg.taxThreshold = Convert.ToInt32(data); }
                        else if (key == "TTSVoice") { cfg.ttsVoice = data; }
                        else if (key == "ChatSayTTSCost") { cfg.chatSayTTSCost = Convert.ToBoolean(data); }
                        else if (key == "DailyPoints") { cfg.dailyPoints = Convert.ToInt32(data); }
                        else if (key == "ViewerMultiplier") { cfg.viewerMultiplier = (float)Math.Round(float.Parse(data),1); }
                        else if (key == "FollowerMultiplier") { cfg.followerMultiplier = (float)Math.Round(float.Parse(data),1); }
                        else if (key == "T1Points") { cfg.t1points = Convert.ToInt32(data); }
                        else if (key == "T2Points") { cfg.t2points = Convert.ToInt32(data); }
                        else if (key == "T3Points") { cfg.t3points = Convert.ToInt32(data); }
                        else if (key == "T1Multiplier") { cfg.t1Multiplier = (float)Math.Round(float.Parse(data),1); }
                        else if (key == "T2Multiplier") { cfg.t2Multiplier = (float)Math.Round(float.Parse(data),1); }
                        else if (key == "T3Multiplier") { cfg.t3Multiplier = (float)Math.Round(float.Parse(data),1); }
                        else if (key == "T1Gold") { cfg.t1gold = Convert.ToInt32(data); }
                        else if (key == "T2Gold") { cfg.t2gold = Convert.ToInt32(data); }
                        else if (key == "T3Gold") { cfg.t3gold = Convert.ToInt32(data); }
                        else if (key == "FilterTTS") { cfg.betaTTSFilter = Convert.ToBoolean(data); }
                        else if (key == "NoFloor") { cfg.noFloor = Convert.ToBoolean(data); }
                        else if (key == "GlobalMinigamesCooldown") { cfg.noMinigamesStack = Convert.ToBoolean(data); }
                        else if (key == "UploadFullCommandList") { cfg.uploadFullCommandList = Convert.ToBoolean(data); }
                        else if (key == "MinigamesCost") { cfg.minigamesCost = Convert.ToInt32(data); }
                        else if (key == "MinigamesDuration") { cfg.minigamesDuration = Convert.ToInt32(data); }
                        else if (key == "MinigamesCooldown") { cfg.minigamesCooldown = Convert.ToInt32(data); }
                        else if (key == "DropRate") { cfg.dropTimer = Convert.ToInt32(data); }
                        else if (key == "DropPercent") { cfg.dropPercent = Convert.ToInt32(data); }
                        else if (key == "GiveTax") { cfg.giveTaxPercent = Convert.ToInt32(data); }
                        else if (key == "FreeTokens") { cfg.baseTTSTokens = Convert.ToInt32(data); }
                        else if (key == "LettersPerToken") { cfg.ttsPerToken = Convert.ToInt32(data); }
                        else if (key == "Tier1SubTTSTokens") { cfg.t1TTS = Convert.ToInt32(data); }
                        else if (key == "Tier2SubTTSTokens") { cfg.t2TTS = Convert.ToInt32(data); }
                        else if (key == "Tier3SubTTSTokens") { cfg.t3TTS = Convert.ToInt32(data); }
                        else if (key == "FilterChatMessages") { cfg.autoDeleteSpam = Convert.ToBoolean(data); }
                        else if (key == "AutoPadlock") { cfg.autoChannelPadlock = Convert.ToBoolean(data); }
                        else if (key == "AllowModpadlock"){ cfg.allowModpadlock = Convert.ToBoolean(data);}
                        else if (key == "LurkCommands"){ cfg.lurkCommands = Convert.ToBoolean(data);}
                        else if (key == "NoSteal"){ cfg.noSteal = Convert.ToBoolean(data);}
                        else if (key == "FullScriptDebugging"){ cfg.fullScriptDebugging = Convert.ToBoolean(data);}
                        else if (key == "DontUseToken"){ cfg.ignoreToken = Convert.ToBoolean(data);}
                    }
                    catch{ }
                }
            }
            else
            {
                //the file doesnt exist, create it
                await SaveSystem.CreatePlaintextFile("config.dat", writeConfig);
                //string fPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\ReplayStudios\\BeanBot\\config.dat";
                //launch the file at fPAth in the default text editor
                //Process.Start("notepad.exe", fPath);
                //Console.WriteLine("Configure The Config File Then Restart The Bot");
                //await Task.Delay(-1);
            }
            //update the config file with any new values
            writeConfig = GetConfigString(cfg);
            await SaveSystem.CreatePlaintextFile("config.dat", writeConfig);
            return cfg;
        }
        public async Task SaveSettings(BotSystem cfg){
            string writeConfig = GetConfigString(cfg);
            await SaveSystem.CreatePlaintextFile("config.dat", writeConfig);
        }
        string GetConfigString(BotSystem cfg){
            string writeConfig = $@"---CORE---
ChannelName<{cfg.channel}  / your twitch channel name
Prefix<{cfg.prefix}   / the prefix for commands eg !command (default = ! cannot be /)
Currency<{cfg.currency}    / the singular name of your currency eg Bean (default = Bean)
Currencies<{cfg.currencies}     / the plural name of your currency eg Beans (default = Beans)
---TTS---
TextToSpeechCost<{cfg.ttsCost}    / the cost of a TTSToken (-1 = no tts, 0 = free) (default = 50)
FreeTokens<{cfg.baseTTSTokens}    / the amount tokens a new user gets, also given on !freepoints (default = 5)
LettersPerToken<{cfg.ttsPerToken}    / the amount of letters a token is worth (default = 25)
TTSVoice<{cfg.ttsVoice}     / the voice to use for text to speech | use !voices to see a list of avalible voices
ChatSayTTSCost<{cfg.chatSayTTSCost}   / tell a user the cost of their tts message [True | False] (default = False)
FilterTTS<{cfg.betaTTSFilter}   / [BETA] filter tts messages [True | False] (default = False)
---POINTS---
DailyPoints<{cfg.dailyPoints}    / the amount of points a user gets from the free daily command (default = 100)
PointsFromDrop<{cfg.pointsFromDrop}   / the base amount of points a user gets from a drop (default = 50)
DropRate<{cfg.dropTimer}    / the delay between drops in minutes (must be positive and more than 0) (default = 10)
DropPercent<{cfg.dropPercent}    / the Percentage drops spawned (based on users) (must be between 10-100) (default = 60)
NotifyOnDropsRemaining<{cfg.notifyOnDropsRemaining}    / notify the chat when drops are remaining [True | False] (default = True)
---MULTIPLIERS---
ViewerMultiplier<{cfg.viewerMultiplier}    / the multiplier for viewers (default = 1.0)
FollowerMultiplier<{cfg.followerMultiplier}    / the multiplier for followers (default = 1.5)
Tier1SubMultiplier<{cfg.t1Multiplier}    / the multiplier for Tier 1 subs and Prime subs (default = 1.7)
Tier2SubMultiplier<{cfg.t2Multiplier}    / the multiplier for Tier 2 subs (default = 1.9)
Tier3SubMultiplier<{cfg.t3Multiplier}    / the multiplier for Tier 3 subs (default = 2.2)
---SUB POINTS---
Tier1SubPoints<{cfg.t1points}    / the amount of points a Tier 1/Prime sub gets (default = 5000)
Tier2SubPoints<{cfg.t2points}    / the amount of points a Tier 2 sub gets (default = 10000)
Tier3SubPoints<{cfg.t3points}    / the amount of points a Tier 3 sub gets (default = 25000)
---SUB GOLD---
Tier1SubGold<{cfg.t1gold}    / the amount of gold a Tier 1/Prime sub gets (default = 5)
Tier2SubGold<{cfg.t2gold}    / the amount of gold a Tier 2 sub gets (default = 10)
Tier3SubGold<{cfg.t3gold}    / the amount of gold a Tier 3 sub gets (default = 20)
---SUB TTS---
Tier1SubTTSTokens<{cfg.t1TTS}    / the amount of TTSTokens a Tier 1/Prime sub gets (default = 20)
Tier2SubTTSTokens<{cfg.t2TTS}    / the amount of TTSTokens a Tier 2 sub gets (default = 40)
Tier3SubTTSTokens<{cfg.t3TTS}    / the amount of TTSTokens a Tier 3 sub gets (default = 80)
---TAX--- (taxes are done on launch and remove TaxPercent of the users currency over TaxThreshold)
TaxPercent<{cfg.taxPercent}    / the percent of tax to take from a user on launch (default = 3)
TaxThreshold<{cfg.taxThreshold}    / the amount of currency a user needs to have before tax is taken (default = 50000)
GiveTax<{cfg.giveTaxPercent}    / the percent of tax to take when users give exchange points (default = 0)
---MINIGAMES---
MinigamesCost<{cfg.minigamesCost}    / the cost to open the minigames (-1 = no minigames, 0 = free) (default = 10000)
MinigamesDuration<{cfg.minigamesDuration}    / the duration of the minigames in minutes (default = 6)
MinigamesCooldown<{cfg.minigamesCooldown}    / the cooldown of the minigames in seconds (default = 600)
GlobalMinigamesCooldown<{cfg.noMinigamesStack}    / Minigames cooldown applies to all users [True | False] (default = False)
---CHAT AND MODERATION---
WarnIfBadSyntax<{cfg.warnIfBadSyntax}    / warn if a command was typed wrong [True | False] (default = True)
AllowModpadlock<{cfg.allowModpadlock}    / allow mods to use padlock 4 [True | False] (default = False)
FilterChatMessages<{cfg.autoDeleteSpam}    / auto delete large non-english messages (eg ascii art) [True | False] (default = False)
---STARTUP---
DontUseToken<{cfg.ignoreToken}    / dont request a Token, this disables some features, not reccomended [True | False] (default = False)
NoFloor<{cfg.noFloor}    / Disable !Floor [True | False] (default = False)
NoSteal<{cfg.noSteal}    / Disable !Steal [True | False] (default = False)
AutoPadlock<{cfg.autoChannelPadlock}    / automatically gives the streamer a padlock [True | False] (default = False)
NotifyOnJoin<{cfg.notifyOnJoin}    / notify the chat when the bot joins [True | False] (default = True)
---OTHER---
LurkCommands<{cfg.lurkCommands}    / enable lurk and unlurk commands [True | False] (default = False)
FullScriptDebugging<{cfg.fullScriptDebugging}    / enable full script debugging [True | False] (default = False)
UploadFullCommandList<{cfg.uploadFullCommandList}    / upload the full command list for online viewing [True | False] (default = True)";
            return writeConfig;
        }
    }
}
public class BotSystem{
    public string name, channel, prefix, currency, currencies, ttsVoice;
    public bool warnIfBadSyntax, notifyOnJoin, notifyOnDropsRemaining, chatSayTTSCost, betaTTSFilter, noFloor, noSteal,
    noMinigamesStack, uploadFullCommandList, ignoreToken, autoDeleteSpam, autoChannelPadlock, allowModpadlock,
    lurkCommands, fullScriptDebugging;
    public int ttsCost, ttsPerToken, baseTTSTokens, pointsFromDrop, taxPercent, taxThreshold, dailyPoints,
    t1points, t2points, t3points, t1gold, t2gold, t3gold, minigamesCost, minigamesDuration, minigamesCooldown,
    dropTimer, dropPercent, giveTaxPercent, t1TTS, t2TTS, t3TTS;
    public float viewerMultiplier, followerMultiplier, t1Multiplier, t2Multiplier, t3Multiplier;
}