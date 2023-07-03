```js
<The Help Command | Catagories: {INSERT CURRENCIES}, Minigames, Events, About, TTS

{INSERT CURRENCY}<You Gain 1-3 {INSERT CURRENCIES} Every Minute Your Active in Chat | Commands: <PREFIX>{INSERT CURRENCIES}  <PREFIX>Give{INSERT CURRENCIES}, <PREFIX>{INSERT CURRENCY}Board, <PREFIX>Free{INSERT CURRENCIES}, <PREFIX>Floor, <PREFIX>Steal
give{INSERT CURRENCY}<Give someone {INSERT CURRENCIES} | <PREFIX>give{INSERT CURRENCY} (@user) (amount)
{INSERT CURRENCY}board<View The Leaderboard
free{INSERT CURRENCY}<Get {CHECK STREAMER CONFIG} Free {INSERT CURRENCIES} once per stream
drop<collect a drop of {Program.config.pointsFromDrop} {INSERT CURRENCY} | <PREFIX>drop
golden{INSERT CURRENCY}<activates the power of a golden {INSERT CURRENCY}
prestige<Prestige your {INSERT CURRENCIES} | <PREFIX>prestige (costs 1,000,000,000 {INSERT CURRENCIES})
floor<Collect {INSERT CURRENCIES} if enabled by Streamer

minigames<Minigames to play in chat | Commands: <PREFIX>OpenMinigames <PREFIX>Coinflip  <PREFIX>OneUps <PREFIX>Quickmath
openminigames<Unlocks the minigames for {CHECK STREAMER CONFIG} minutes, can be stacked | <PREFIX>openminigames (costs {CHECK STREAMER CONFIG} {INSERT CURRENCIES})
coinflip<Flip a coin, if you bet you will get double your bet on correct guess | <PREFIX>coinflip (optional:[heads/tails] [amount]) betting is capped at 1000 {INSERT CURRENCIES}
quickmath<Answer a math question in a timelimit, your are awarded based on the complexity | <PREFIX>quickmath | <PREFIX>answer (answer rounded to 1 decimal place)
answer<Answer a math question in a timelimit, your are awarded based on the complexity | <PREFIX>quickmath | <PREFIX>answer (answer rounded to 1 decimal place)
oneups<Play a game of 1up, where you must pick the highest number not picked by anyone else | <PREFIX>oneups (bet)
playwithfire<Play a game with fire, if you win you will get 10x your bet, if you lose you get banished from chat for 2 minutes | <PREFIX>playwithfire (bet) betting is capped at 1000 {INSERT CURRENCIES}
events<Events that happen in chat | Commands: <PREFIX>Event <PREFIX>Drop <PREFIX>Prediction <PREFIX>Steal <PREFIX>Catch
event<View the current event | <PREFIX>event (optional:name)

startprediction<[MOD]Start a Prediction | <PREFIX>StartPrediction (prediction),(team 1),(team 2),(ect)
prediction<View the current prediction
lockprediction<[MOD]Lock the current prediction | <PREFIX>Lockprediction
endprediction<[MOD]Ends the current prediction | <PREFIX>Endprediction (winner)
vote<Vote for the prediction | <PREFIX>vote (teamname) (bet)

steal<Attempt to steal from another user, They will have 5 seconds to try <PREFIX>catch you | <PREFIX>steal | <PREFIX>steal (@target)
catch<Stop another user from stealing from you

buypadlock<buy a padlock to stop people stealing from you | <PREFIX>buypadlock (optional:tier[1-3]) 1: 1K {INSERT CURRENCIES} for 10M, 2: 2.5K {INSERT CURRENCIES} for 20M, 3: 5K {INSERT CURRENCIES} for 30M and notification on break
padlock<view your padlock, use <PREFIX>buypadlock to buy one

counter<View/Change the counter for an event |[MOD] <PREFIX>Counter+ |[MOD] <PREFIX>Counter- | Counter
about<Get some info | <PREFIX>help about bot <PREFIX>help about creator <PREFIX>help about inspiration
about bot<(Running BeanBot.exe) Beanbot's goal is to replace twitch channel points with built-in games and fully customisable chat-to-game interactions
about creator<BeanBot was made by twitch.tv/MasterAirscrach maybe drop a cheeky follow, get it yourself: (masterairscrachdev.itch.io/beanbot)
about testers<Huge thanks to twitch.tv/5G_Greek, twitch.tv/xBlustone and twitch.tv/Elppa for letting me test this bot on their channels, and thanks to their viewers for helping me find all the bugs
about inspiration<This bot was inspired by the cool commands of twitch.tv/DrTreggles and the amazing chat interaction of twitch.tv/DougDougW

tts<Get TTS, {CHECK STREAMER CONFIG}
buyTTS<buy TTS Tokens to use for TTS, 5% discount per token | <PREFIX>buyTTS (amount)

setCounter<[MOD]Set the name of the counter and reset it to 0 | <PREFIX>setcounter (name)
clearCounter<[MOD]Removes The counter from the capture, does not reset the counter | <PREFIX>clearcounter

Reload<[MOD] Reloads the config file and all custom commands | <PREFIX>reload
stopSound<[MOD] Stops the tts and any other sounds the bot is playing | <PREFIX>stopsound or <PREFIX>ss

Edit{INSERT CURRENCIES}<[STREAMER] Edit a users {INSERT CURRENCIES} | <PREFIX>edit{INSERT CURRENCY} (optional @user) (amount) | edit gold <PREFIX>edit{INSERT CURRENCY} (@user) (amount) gold
EditTTS<[MOD] Edit a users TTS Tokens | <PREFIX>edittts (@user) (amount)
bot.voices<[STREAMER] Get a list of all the voices you can use for tts in console
bot.commands<[STREAMER] Get a list of all the commands in console
bot.config<[STREAMER] Opens the config folder for the bot
bot.processes<[STREAMER] Get a list of all the processes the bot can detect
bot.lock<[MOD] toggles the use of custom commands