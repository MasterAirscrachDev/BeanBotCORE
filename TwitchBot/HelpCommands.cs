using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBot
{
    class HelpCommands
    {
        string helpList;
        string customHelp;
        public HelpCommands()
        {
            string link = Program.config.uploadFullCommandList ? $". or get a full list here: github.com/MasterAirscrachDev/BeanBotFullCommands/blob/main/{Program.config.channel}%20Commands.md" : "",
            floor = Program.config.noFloor ? $"{Program.config.prefix}Floor has disabled by the streamer" : "Check the floor for any {Program.config.currencies} | {Program.config.prefix}floor",
            tts = Program.config.ttsCost > -1 ? $"Get Text To Speech | {Program.config.prefix}tts (text) (costs 1 TTS Token per {Program.config.ttsPerToken} letters), | !buyTTS | !editTTS" : "TTS is disabled";
            helpList = $@"<The Help Command | Catagories: {Program.config.currencies}, Minigames, Events, About, TTS{link}
{Program.config.currencies.ToLower()}<You Gain 1-3 {Program.config.currencies} Every Minute Your Active in Chat | Commands: {Program.config.prefix}{Program.config.currencies} {Program.config.prefix}Give{Program.config.currencies}, {Program.config.prefix}{Program.config.currency}Board, {Program.config.prefix}Free{Program.config.currencies}, {Program.config.prefix}Floor, {Program.config.prefix}Steal
give{Program.config.currencies.ToLower()}<Give someone {Program.config.currencies} | {Program.config.prefix}give{Program.config.currencies.ToLower()} (@user) (amount)
{Program.config.currency.ToLower()}board<View The Leaderboard
free{Program.config.currencies.ToLower()}<Get {Program.config.dailyPoints} Free {Program.config.currencies} once per stream
floor<{floor}
edit{Program.config.currencies.ToLower()}<[STREAMER] Edit a users {Program.config.currencies} | {Program.config.prefix}edit{Program.config.currencies.ToLower()} (optional @user) (amount) | edit gold {Program.config.prefix}edit{Program.config.currencies.ToLower()} (@user) (amount) gold
minigames<Minigames to play in chat | Commands: {Program.config.prefix}OpenMinigames {Program.config.prefix}Coinflip  {Program.config.prefix}OneUps {Program.config.prefix}Quickmath 
openminigames<Unlocks the minigames for {Program.config.minigamesDuration} minutes, can be stacked | {Program.config.prefix}openminigames (costs {Program.config.minigamesCost.ToString("N0")} {Program.config.currencies})
coinflip<Flip a coin, if you bet you will get double your bet on correct guess | {Program.config.prefix}coinflip (optional:[heads/tails] [amount]) betting is capped at 1000 {Program.config.currencies}
quickmath<Answer a math question in a timelimit, your are awarded based on the complexity | {Program.config.prefix}quickmath | {Program.config.prefix}answer (answer rounded to 1 decimal place)
answer<Answer a math question in a timelimit, your are awarded based on the complexity | {Program.config.prefix}quickmath | {Program.config.prefix}answer (answer rounded to 1 decimal place)
oneups<Play a game of 1up, where you must pick the highest number not picked by anyone else | {Program.config.prefix}oneups (bet)
playwithfire<Play a game with fire, if you win you will get 10x your bet, if you lose you get banished from chat for 2 minutes | {Program.config.prefix}playwithfire (bet) betting is capped at 1000 {Program.config.currencies}
events<Events that happen in chat | Commands: {Program.config.prefix}Event {Program.config.prefix}Drop {Program.config.prefix}Prediction {Program.config.prefix}Steal {Program.config.prefix}Catch
event<View the current event | {Program.config.prefix}event (optional:name)
startprediction<[MOD]Start a Prediction | {Program.config.prefix}StartPrediction (prediction),(team 1),(team 2),(ect)
prediction<View the current prediction
lockprediction<[MOD]Lock the current prediction | {Program.config.prefix}Lockprediction
endprediction<[MOD]Ends the current prediction | {Program.config.prefix}Endprediction (winner)
vote<Vote for the prediction | {Program.config.prefix}vote (teamname) (bet)
steal<Attempt to steal from another user, They will have 5 seconds to try {Program.config.prefix}catch you | {Program.config.prefix}steal | {Program.config.prefix}steal (@target)
catch<Stop another user from stealing from you
buypadlock<buy a padlock to stop people stealing from you | {Program.config.prefix}buypadlock (tier[1-3]) 1: 10% {Program.config.currencies} for 10, 2: 20% {Program.config.currencies} for 20M, 3: 30% {Program.config.currencies} for 30M and notification on break
padlock<view your padlock, use {Program.config.prefix}buypadlock to buy one
join<Join an event you were invited to | {Program.config.prefix}join (optional:eventname) (optional:info)
drop<collect a drop of {Program.config.pointsFromDrop} {Program.config.currencies.ToLower()} | {Program.config.prefix}drop
setcounter<[MOD]Set the name of the counter and reset it to 0 | {Program.config.prefix}setcounter (name)
clearcounter<[MOD]Removes The counter from the capture, does not reset the counter | {Program.config.prefix}clearcounter
counter<View/Change the counter for an event |[MOD] {Program.config.prefix}Counter+ |[MOD] {Program.config.prefix}Counter- | Counter
about<Get some info | {Program.config.prefix}help about bot {Program.config.prefix}help about creator {Program.config.prefix}help about inspiration
about bot<(Running BeanBot.exe {Program.version}) Beanbot's goal is to replace twitch channel points with built-in games and fully customisable chat-to-game interactions
about creator<BeanBot was made by twitch.tv/MasterAirscrach maybe drop a cheeky follow, get it yourself: (masterairscrachdev.itch.io/beanbot)
about testers<Huge thanks to twitch.tv/5G_Greek, twitch.tv/xBlustone and twitch.tv/Elppa for letting me test this bot on their channels, and thanks to their viewers for helping me find all the bugs
about inspiration<This bot was inspired by the cool commands of twitch.tv/DrTreggles and the amazing chat interaction of twitch.tv/DougDougW
buytts<buy TTS Tokens, {Program.config.ttsCost.ToString("N0")} {Program.config.currencies}, 5% discount per token | {Program.config.prefix}buyTTS (amount)
tts<{tts}
edittts<[MOD] Edit a users TTS Tokens | {Program.config.prefix}edittts (@user) (amount)
stopsound<[MOD] Stops the tts and any other sounds the bot is playing | {Program.config.prefix}stopsound or {Program.config.prefix}ss
bot.voices<[STREAMER] Get a list of all the voices you can use for tts in console
bot.commands<[STREAMER] Get a list of all the commands in console
bot.config<[STREAMER] Opens the config folder for the bot
bot.processes<[STREAMER] Get a list of all the processes the bot can detect in console
bot.lock<[MOD] toggles the use of custom commands
golden{Program.config.currency}blessing<activates the power of a golden {Program.config.currency}
reload<[MOD] Reloads the config file and all custom commands | {Program.config.prefix}reload
prestige<Prestige your {Program.config.currencies} | {Program.config.prefix}prestige (costs 1,000,000,000 {Program.config.currencies})
cows<Nothing can save you now
picknum<[WHISPER] Pick a number between 1 and 10 for the OneUps Minigame | PICKNUM (number)
active<[WHISPER] All the chats your detected in | ACTIVE";
            GetCustomHelpLines();
        }
        public void ReloadHelp()
        { GetCustomHelpLines(); }
        async Task GetCustomHelpLines()
        { customHelp = Program.customCommands.GetCustomHelp(); UpdateGitHubCommandList(); }   
        public ProcessData Help(ProcessData data, bool isWhisper = false)
        {
            //remove the first 4 letters
            string command = "";
            if(data.message.content.Length > 4) { command = data.message.content.Remove(0, 5).ToLower(); }
            //Console.WriteLine($"`{command}`");
            string[] helpArray = $"{helpList}\n{customHelp}".Split('\n');
            
            for (int i = 0; i < helpArray.Length; i++) {
                //Console.WriteLine(helpArray[i]);
                string[] helpLine = helpArray[i].Split('<');
                if (helpLine[0] == command)
                { 
                    string user = !isWhisper ? $"@{data.message.sender} " : "";
                    data.returnMessage = $"{user}{helpLine[1]}"; break;
                }
            }
            return data;
        }
        public void ListAllCommands(){
            string[] helpArray = $"{helpList}\n{customHelp}".Split('\n');

            Console.BackgroundColor = ConsoleColor.Black;
            Program.Log("All Commands:", MessageType.Debug);
            for (int i = 0; i < helpArray.Length; i++) {
                string[] helpLine = helpArray[i].Split('<');
                Program.Log($"{helpLine[0]} - {helpLine[1]}", MessageType.Debug);
            }
        }
        async Task UpdateGitHubCommandList(){
            await Task.Delay(5000);
            //Console.WriteLine("Updating GitHub Command List");
            string help = $"### Base Commands\nhelp{helpList.Replace("\n", "\n <br>")}\n### Custom Commands\n{customHelp.Replace("\n", "\n <br>")}";
            string floor = Program.config.noFloor ? $"{Program.config.prefix}Floor Is Disabled" : $"{Program.config.prefix}Floor is enabled";
            string tts = Program.config.ttsCost > -1 ? $"Get Text To Speech | {Program.config.prefix}tts (text) (costs {Program.config.ttsCost} {Program.config.currencies})" : "TTS is disabled";
            string help2 = $@"{tts} <br>
{floor} <br>
{Program.config.prefix}Openminigames costs {Program.config.minigamesCost} {Program.config.currencies} and lasts {Program.config.minigamesDuration} minutes <br>
### Custom Commands <br>
```js
{customHelp}";
            try{
                Program.twitchLibInterface.bot.UpdateGitHubCommandList(help2);
            }
            catch{}
        }

    }
}
