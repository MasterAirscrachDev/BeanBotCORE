using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.Core.Enums;
using System.Text.RegularExpressions;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Core;


namespace TwitchBot
{
    class TwitchLibInterface
    {
        public BotInterface bot;
        public TwitchLibInterface()
        { ConnectBackend(); }
        void ConnectBackend()
        { bot = new BotInterface(); }
    }
    class BotInterface
    {
        TwitchClient client;
        TwitchAPI api;
        bool ActiveBotAccessToken = false, hasMod = false, doChecks = false, chatConnected = false;
        string broadcasterID = null, botID, UserAccessToken = null, refresh = null;
        List<string> chatters = new List<string>(), bannedUsers = new List<string>();
        TwitchApiInterface apiInterface;
        List<AuthScopes> mainScopes = new List<AuthScopes> { AuthScopes.Helix_moderator_Manage_Chat_Messages, AuthScopes.Helix_Moderation_Read, AuthScopes.Helix_User_Read_Email, AuthScopes.Helix_Moderator_Read_Followers, AuthScopes.Helix_User_Manage_Whispers },
        streamerScopes = new List<AuthScopes> {AuthScopes.Helix_Moderation_Read, AuthScopes.Helix_Channel_Read_Subscriptions, AuthScopes.Helix_Channel_Manage_Broadcast};
        List<UserWithTimer> tempBanned = new List<UserWithTimer>();
        public BotInterface()
        {
            api = new TwitchAPI();
            apiInterface = new TwitchApiInterface();
            api.Settings.ClientId = SpecialDat.clientID;
            api.Settings.Secret = SpecialDat.clientSecret;
            ConnectionCredentials credentials = new ConnectionCredentials(Program.config.name, SpecialDat.oauth);
            var clientOptions = new ClientOptions
            { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, Program.config.channel);
            //client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnReSubscriber += Client_OnReSubscriber;
            client.OnGiftedSubscription += Client_OnGiftedSubscription;
            client.OnPrimePaidSubscriber += Client_OnPrimePaidSubscriber;
            client.OnContinuedGiftedSubscription += Client_OnContinuedGiftedSubscription;
            //create a follow listener
            chatters.Add(Program.config.channel);
            //client.OnConnected += Client_OnConnected;

            client.Connect();
            //do we have an access token?
            ServerSetup();   
        }
        async Task ServerSetup(){
            Program.Log("Waiting auth confirmation", MessageType.Warning);
            while(!doChecks){ await Task.Delay(100); }
            Program.Log("Auth confirmed", MessageType.Success);
            await GetSavedToken();
            Program.Log("Getting Bot Token");
            await RefreshBotToken();
            MinTicker();
            UpdateBannedUsers();
        }
        public void AuthConfirmed(){
            doChecks = true;
        }

        public async Task RefreshBotToken(){
            string t = await apiInterface.GetBotToken();
            int tries = 0;
            while (t == null){
                ActiveBotAccessToken = false;
                await Task.Delay(5000);
                if(tries == 1){
                    Program.Log("Failed to get the bot token from the server, retrying", MessageType.Warning);
                }
                else if(tries > 10){
                    Program.Log("Failed to get the bot token from the server (10) Something is wrong, please contact the developer", MessageType.Error);
                }
                t = await apiInterface.GetBotToken();
                tries++;
            }
            api.Settings.AccessToken = t;
            api.Settings.Scopes = mainScopes;
            ActiveBotAccessToken = true;
            Program.Log("Bot Token Obtained From the server", MessageType.Success);
            broadcasterID = await GetUserIDFromName(Program.config.channel);
            botID = await GetUserIDFromName("AwesomeBean_BOT");
            await IsBotMod();

        }
        async Task<string> GetUserIDFromName(string name){
            try{
                var userResponse = await api.Helix.Users.GetUsersAsync(logins: new List<string> { name });
                var user = userResponse.Users.FirstOrDefault();
                return user?.Id;
            }
            catch(Exception e){
                Program.Log($"Failed to get the user ID, Error: {e.Message}", MessageType.Error);
                return null;
            }
        }
        async Task GetSavedToken(){
            FileSuper fileSuper = new FileSuper("BeanBot", "ReplayStudios");
            fileSuper.SetEncryption(true, SpecialDat.TokenEnc);
            Save save = await fileSuper.LoadFile("Token.key");
            if(save == null){ 
                if(!Program.config.ignoreToken){
                    Program.Log("Token not found, generating new token", MessageType.Warning);
                    SendConsoleAuthMessage();
                    GetAuthURL();
                    return;
                }
            }
            else{
                if(Program.config.ignoreToken){ SaveSystem.DeleteFile("Token.key"); return; }
                //Console.WriteLine("Token found");
                //compare the scopes if the scopes are different, generate a new token
                int scopeCount = save.GetString("Scopes").Split(',').Length;
                if(scopeCount != streamerScopes.Count){
                    Program.Log("Scopes have changed, generating new token", MessageType.Warning);
                    //delete the old token
                    SaveSystem.DeleteFile("Token.key");
                    SendConsoleAuthMessage();
                    GetAuthURL();
                    return;
                }
                //check if the token is valid
                //if it is, use it
                //if it isn't, generate a new one
                //get the time in seconds between now and the last time the token was generated
                DateTime originTime = DateTime.FromBinary(long.Parse(save.GetString("OriginTime")));
                int limit = (int)save.GetInt("ExpiresIn");
                //if the time between now and the origin time is greater than the limit, generate a new token
                if((DateTime.Now - originTime).TotalSeconds > limit || !await apiInterface.IsAccessTokenValid(save.GetString("AccessToken"))){
                    Program.Log("Token has expired, generating new token", MessageType.Warning);
                    UserAccessToken = await apiInterface.RefreshAccessToken(save.GetString("RefreshToken"));
                    if(UserAccessToken == null){
                        Program.Log("Token Regeneration Failed, Please Authorize a new token", MessageType.Error);
                        SendConsoleAuthMessage();
                        GetAuthURL();
                        return;
                    }
                    return;
                }
                else{
                    //log the token
                    //Console.WriteLine("Token Info");
                    //Console.WriteLine($"Access Token: {save.GetString("AccessToken")}");
                    //Console.WriteLine($"Refresh Token: {save.GetString("RefreshToken")}");
                    //Console.WriteLine($"Expires In: {save.GetInt("ExpiresIn")}");
                    //Console.WriteLine($"Scopes: {save.GetString("Scopes")}");
                    //Console.WriteLine($"Token Type: {save.GetString("TokenType")}");
                    //if the token is valid, use it
                    //Console.WriteLine("Token is valid");
                    //check if token is valid
                    if(await apiInterface.IsAccessTokenValid(save.GetString("AccessToken"))){
                        Program.Log("Token is valid", MessageType.Success);
                        UserAccessToken = save.GetString("AccessToken");
                        return;
                    }
                    else{
                        UserAccessToken = await apiInterface.RefreshAccessToken(save.GetString("RefreshToken"));
                        if(UserAccessToken == null){
                            Program.Log("Token Regeneration Failed, Please Authorize a new token", MessageType.Error);
                            SendConsoleAuthMessage();
                            GetAuthURL();
                            return;
                        }
                        return;
                    }
                    //UserAccessToken = save.GetString("AccessToken");
                    //Console.WriteLine($"Token Confirmed");
                }
            }
        }
        void SendConsoleAuthMessage(){
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(@"-------------------------------------
An Authentication Page has opened
By agreeing you give the bot the ablity to do the following

--Check if the bot has moderator status in your channel--
This Allows the bot to do anti spam, and correctly set the RateLimit


--Read The Subscribers of your channel--
This will correctly update and remove user multipliers

--Edit Your Stream Details--
This allows the bot to change the title and category of your stream

If You agree to these terms Click Accept and Then send the following command followed by the website url (The text at the top)
If The website doesnt load thats fine
Send any message in chat to link the bot, then
Send This In Your Chat: /w AwesomeBean_BOT KEY:put url here (example: KEY:http://localhost:3000/)");
        }
        void GetAuthURL(){
            GetAuthUrl(streamerScopes);
        }
        public void GetBotAuthURL(){
            GetAuthUrl(mainScopes);
        }
        public async Task GetAccessToken(string authUrl, bool bot = false)
        {
            try
            {
                string test = await apiInterface.GetAccessToken(apiInterface.GetAuthCodeFromUrl(authUrl), bot);
                if(test == null){ Program.Log("Token Is null", MessageType.Error); return;}
                if(bot){ Program.Log("Bot Token Updated"); return;}
                UserAccessToken = test;
                //api.Settings.AccessToken = test3;
                //api.Settings.Scopes = mainScopes;
                Program.Log("Token Confirmed", MessageType.Success);
            }
            catch(Exception e)
            { Program.Log($"Failed to get the access token, Error: {e.Message}", MessageType.Error); }
            
        }
        public string GetAuthUrl(IEnumerable<AuthScopes> scopes)
        {
            // Generate the auth URL
            string r = api.Auth.GetAuthorizationCodeUrl("http://localhost:3000", scopes, true);
            //Console.WriteLine($"Auth URL: {r}");
            Process.Start(r);
            return r;
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        { Console.WriteLine(e.Data);  }
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        { Program.Log($"Connected to {e.AutoJoinChannel}", MessageType.Success); }
        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e){
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(@"BeanBot Terms of Use
1. You may not use this bot to spam or harass other users
2. You may not use this bot in another channel without permission from the owner
3. The bot is provided as is, and the devs is not responsible for any issues caused by the bot
4. Developers of this bot are not responsible for any issues caused by custom commands,
However if you think a Beanscript function is broken, please report it to masterairscrach666 on discord

By using this bot, you agree to these terms of use
This Bot Utilises TwitchLib C# Library https://github.com/TwitchLib/TwitchLib
And The Octokit C# Library https://github.com/octokit
Thanks For Using beanbot, Please Report Any Bugs To masterairscrach666 On Discord");
            Console.ForegroundColor = ConsoleColor.White;
            Program.Log($"AwesomeBean_BOT Conneted To Channel {Program.config.channel}", MessageType.Success);
            chatConnected = true;
            if(Program.config.notifyOnJoin && Program.authConfirmed){ Program.SendMessage($"I have awoken, and i bring {Program.config.currencies}");}
        }
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e){
            Message m = new Message();
            m.sender = e.ChatMessage.DisplayName;
            if (!chatters.Contains(m.sender.ToLower())){ chatters.Add(m.sender.ToLower()); }
            if(!doChecks){ return; }
            m.content = e.ChatMessage.Message;
            //Console.WriteLine($"Message Received from {e.ChatMessage.DisplayName}: {e.ChatMessage.UserId}");
            //Console.WriteLine($"Message Received: {m.content}, with ID: {e.ChatMessage.Id}");
            bool QuickDelete = false;
            float ratio = 1;
            string QuickDeleteReason = "Spam Filter:";
            if(m.content.Length > 50){
                //get the ratio of safe chars to total chars
                ratio = CalculateRatio(m.content);
                if(ratio < 0.3 && Program.config.autoDeleteSpam){ Program.Log($"message was likely spam with {ratio} score", MessageType.Error); QuickDelete = true; QuickDeleteReason += $" {m.content} | ratio: {ratio}"; }
            }
            m.usermod = (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster || e.ChatMessage.DisplayName == "MasterAirscrach");
            if((e.ChatMessage.Bits < 1 && tempBanned.Any(x => x.username == e.ChatMessage.Username)))
            { QuickDelete = true; QuickDeleteReason = "You are temp banned"; }
            if(QuickDelete && !m.usermod){ Program.twitchLibInterface.bot.DeleteMessage(e.ChatMessage.Id); return; }
            
            m.content = Regex.Replace(m.content, @"[^\u0000-\u007F]+", string.Empty);
            //if the user isnt in chatters, add them
            //if the last char is a space, remove it
            if(m.content.EndsWith(" ")){ m.content = m.content.Remove(m.content.Length - 1); }
            //chatters.Add(m.sender);

            if(m.content.StartsWith(Program.config.prefix)){
                Program.LogMessage(true, m, ratio);
                m.content = m.content.Remove(0, Program.config.prefix.Length);
            }else{ Program.LogMessage(false, m, ratio); Task.Run(() => Program.commandManager.GetPoints(m)); return; }
            m.channel = e.ChatMessage.Channel;
            m.color = e.ChatMessage.ColorHex;
            m.messageID = e.ChatMessage.Id;
            m.bits = e.ChatMessage.Bits;
            if(Program.allowDebug && m.bits != 0){ Program.Log($"Bits Received from {m.sender}: {m.bits}");}
            m.firstMessage = e.ChatMessage.IsFirstMessage;
            //get the bits
            Program.commandManager.ProcessMessage(m);
        }
        float CalculateRatio(string inputString)
        {
            int standardCount = 0;
            int nonStandardCount = 0;
            foreach (char c in inputString)
            {
                if ((c >= 32 && c <= 126) || (c >= 160 && c <= 255))
                { standardCount++; }
                else
                { nonStandardCount++; }
            }
            if (standardCount == 0 && nonStandardCount == 0)
            { return 0.0f; }
            return (float)standardCount / (standardCount + nonStandardCount);
        }
        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            //Console.WriteLine($"Whisper Received from {e.WhisperMessage.DisplayName}: {e.WhisperMessage.Message}");
            //check if the user is talking to this client
            if(chatters.Contains(e.WhisperMessage.Username.ToLower())){
                //Console.WriteLine("Whisper is from a chatter, processing");
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                
                //remove the /channelname
                Message message = new Message();
                //get the index of the first space
                message.content = e.WhisperMessage.Message;
                message.sender = e.WhisperMessage.DisplayName;
                if(IsUserBlocked(message.sender)){ return;}
                message.channel = Program.config.channel;
                message.isWhisper = true;
                Console.WriteLine($"Whisper: {message.content} from {message.sender}");
                //if it starts with the prefix, remove it
                if(message.content.StartsWith(Program.config.prefix)){
                    message.content = message.content.Remove(0, Program.config.prefix.Length);
                }
                Program.commandManager.ProcessMessage(message);
            }
            else if(e.WhisperMessage.DisplayName == "MasterAirscrach" && e.WhisperMessage.Message.ToLower() == "pong"){
                Program.SendMessage($"pong from {Program.config.channel}", "MasterAirscrach");
            }
        }
        public async Task<bool> SendWhisper(string message, string username)
        {
            string userID = await GetUserIDFromName(username);
            try{
                await api.Helix.Whispers.SendWhisperAsync(botID, userID, message, true);
                return true;
            }
            catch(Exception e){
                Program.Log($"Error sending whisper to {username}: {e.Message}", MessageType.Error);
                return false;
            }
        }
        //check if the user is banned or timeouted on the channel
        bool IsUserBlocked(string username)
        {
            if(bannedUsers.Any(x => x == username)){ return true; }
            return false;
        }
        async Task UpdateBannedUsers(){
            while(true){
                
                bannedUsers.Clear();
                try{
                    var banned = await api.Helix.Moderation.GetBannedUsersAsync(broadcasterID, first:100, accessToken:UserAccessToken);
                    foreach(var user in banned.Data){
                        //Program.Log($"Adding {user.UserName} to banned users");
                        bannedUsers.Add(user.UserName);
                    }
                }
                catch(Exception e){
                    Program.Log($"Error updating banned users: {e.Message}", MessageType.Error);
                }
                
                //var timeouted = await api.Helix.Moderation.Get
                await Task.Delay(60000);
            }
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e) { 
            int plan = 0;
            if(e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime){ plan = 4; }
            else if(e.Subscriber.SubscriptionPlan == SubscriptionPlan.Tier1){ plan = 1; }
            else if(e.Subscriber.SubscriptionPlan == SubscriptionPlan.Tier2){ plan = 2; }
            else if(e.Subscriber.SubscriptionPlan == SubscriptionPlan.Tier3){ plan = 3; }
            NewSub(e.Subscriber.DisplayName, plan); 
        }
        private void Client_OnReSubscriber(object sender, OnReSubscriberArgs e) { 
            int plan = 0;
            if(e.ReSubscriber.SubscriptionPlan == SubscriptionPlan.Prime){ plan = 4; }
            else if(e.ReSubscriber.SubscriptionPlan == SubscriptionPlan.Tier1){ plan = 1; }
            else if(e.ReSubscriber.SubscriptionPlan == SubscriptionPlan.Tier2){ plan = 2; }
            else if(e.ReSubscriber.SubscriptionPlan == SubscriptionPlan.Tier3){ plan = 3; }
            NewSub(e.ReSubscriber.DisplayName, plan);
        }
        private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e) {
            int plan = 0;
            if(e.GiftedSubscription.MsgParamSubPlan == SubscriptionPlan.Prime){ plan = 4; }
            else if(e.GiftedSubscription.MsgParamSubPlan == SubscriptionPlan.Tier1){ plan = 1; }
            else if(e.GiftedSubscription.MsgParamSubPlan == SubscriptionPlan.Tier2){ plan = 2; }
            else if(e.GiftedSubscription.MsgParamSubPlan == SubscriptionPlan.Tier3){ plan = 3; }
            NewSub(e.GiftedSubscription.MsgParamRecipientDisplayName, plan, e.GiftedSubscription.DisplayName);
        }
        private void Client_OnPrimePaidSubscriber(object sender, OnPrimePaidSubscriberArgs e)
        { NewSub(e.PrimePaidSubscriber.DisplayName, 4); }
        private void Client_OnContinuedGiftedSubscription(object sender, OnContinuedGiftedSubscriptionArgs e)
        { NewSub(e.ContinuedGiftedSubscription.DisplayName, 0); }
        public void SendMessage(string message){
            if(!chatConnected) return;
            client.SendMessage(Program.config.channel, message);
        }
        //write a function that deletes a message
        public async Task DeleteMessage(string messageID, string reason = "No Reason Provided"){
            Program.Log($"Deleting message {messageID} for reason {reason}", MessageType.Warning);
            try{
                if(!ActiveBotAccessToken || !hasMod) return;
                await api.Helix.Moderation.DeleteChatMessagesAsync(broadcasterID, "823308356", messageID);
            }
            catch(Exception e){ Program.Log($"Delete Error: {e.Message}", MessageType.Error); RefreshBotToken(); }
        }
        async Task IsBotMod(){
            //check if we are mod in the channel
            try{
                Console.BackgroundColor = ConsoleColor.Black;
                Program.Log("Checking if bot is mod");
                if(UserAccessToken == null) {while(UserAccessToken == null){ await Task.Delay(100); } }
                Program.Log("Bot has User access token", MessageType.Success);
                var mods = await api.Helix.Moderation.GetModeratorsAsync(broadcasterID, null, 20, null, UserAccessToken);
                foreach(var mod in mods.Data){
                    if(mod.UserName.ToLower() == "awesomebean_bot"){
                        Program.Log("Bot is mod, Ratelimit set to 100", MessageType.Success);
                        Program.ConfirmMod(); hasMod = true; return;
                    }
                }
                Program.Log("Bot is not mod, Ratelimit set to 20", MessageType.Warning);
            }
            catch(Exception e){ Program.Log(e.Message, MessageType.Error); }
        }
        async Task NewSub(string username, int plan, string gifter = null){
            User user = await SaveSystem.GetUser(username);
            if(plan == 1 || plan == 4){
                user.points += Program.config.t1points;
                user.goldPoints += Program.config.t1gold;
                user.multiplier = Program.config.t1Multiplier;
                user.ttsTokens += Program.config.t1TTS;
                await SaveSystem.SaveUser(user);
                string emb = plan == 1 ? "at Tier 1" : "with Prime";
                Program.SendMessage($"@{username} has Subscribed {emb}! Very epic, Enjoy a {user.multiplier}x multiplier!");
                Program.TempMulti(0.2f, 30);
                Program.SendMessage($"Hype Bonus: +{Program.globalMultiplier}x {Program.config.currencies} for the next half hour!");
            }
            else if(plan == 2){
                user.points += Program.config.t2points;
                user.goldPoints += Program.config.t2gold;
                user.multiplier = Program.config.t2Multiplier;
                user.ttsTokens += Program.config.t2TTS;
                await SaveSystem.SaveUser(user);
                Program.SendMessage($"@{username} has Subscribed at Tier 2! Very epic, Enjoy a {user.multiplier}x multiplier!");
                Program.TempMulti(0.5f, 30);
                Program.SendMessage($"Hype Bonus: +{Program.globalMultiplier}x {Program.config.currencies} for the next half hour!");
            }
            else if(plan == 3){
                user.points += Program.config.t3points;
                user.goldPoints += Program.config.t3gold;
                user.multiplier = Program.config.t3Multiplier;
                user.ttsTokens += Program.config.t3TTS;
                await SaveSystem.SaveUser(user);
                Program.SendMessage($"@{username} has Subscribed at Tier 3! Very epic, Enjoy a {user.multiplier}x multiplier!");
                Program.TempMulti(1, 30);
                Program.SendMessage($"Hype Bonus: +{Program.globalMultiplier}x {Program.config.currencies} for the next half hour!");
            }
            if(gifter != null){
                User gifterUser = await SaveSystem.GetUser(gifter);
                int gold = 0, points = 0, tts = 0;
                if(plan == 1 || plan == 4){ gold = Program.config.t1gold / 2; points = Program.config.t1points / 2; tts = Program.config.t1TTS / 2; }
                else if(plan == 2){ gold = Program.config.t2gold / 2; points = Program.config.t2points / 2; tts = Program.config.t2TTS / 2; }
                else if(plan == 3){ gold = Program.config.t3gold / 2; points = Program.config.t3points / 2; tts = Program.config.t3TTS / 2; }
                gifterUser.points += points;
                gifterUser.goldPoints += gold;
                gifterUser.ttsTokens += tts;
                await SaveSystem.SaveUser(gifterUser);
                Program.SendMessage($"@{gifter} has gifted a sub to @{username}! They have recieved {points} ({gold})!");
            }
        }
        public async Task CheckToken(){
            bool valid = await apiInterface.IsAccessTokenValid(UserAccessToken);
            //if(Program.allowDebug){ Console.WriteLine($"Token Valid: {valid}"); }
            if(!valid) { await GetSavedToken(); }
        }
        public async Task<List<string>> GetFollowers()
        {
            if (api == null || broadcasterID == null || !hasMod) { return null; } // safe check for public use
            
            List<string> followers = new List<string>();
            string cursor = null;
            while (true)
            {
                var response = await api.Helix.Channels.GetChannelFollowersAsync(broadcasterID, first: 80, after: cursor);
                followers.AddRange(response.Data.Select(f => f.UserLogin));
                cursor = response.Pagination.Cursor;
                if (cursor == null) break;
            }
            return followers;
        }
        public async Task<List<UserSub>> GetSubscribers()
        {
            if(string.IsNullOrEmpty(UserAccessToken)){ await Task.Delay(3000); }

            if (api == null || broadcasterID == null || !hasMod) { return null; } // safe check for public use
            if(string.IsNullOrEmpty(UserAccessToken)){ return null; }

            List<UserSub> subscribers = new List<UserSub>();
            var cursor = "";
            do
            {
                var subscribersResponse = await api.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(
                    broadcasterId: broadcasterID,
                    accessToken: UserAccessToken,
                    first: 100,
                    after: cursor
                );

                if (subscribersResponse != null && subscribersResponse.Data != null)
                {
                    foreach (var subscriber in subscribersResponse.Data)
                    {
                        //Console.WriteLine($"{subscriber.UserName} - {subscriber.Tier}");
                        int subPlan = 0;
                        if(subscriber.Tier == "1000"){ subPlan = 1; }
                        else if(subscriber.Tier == "2000"){ subPlan = 2; }
                        else if(subscriber.Tier == "3000"){ subPlan = 3; }
                        subscribers.Add(new UserSub(subscriber.UserName, subPlan));
                    }
                }
                cursor = subscribersResponse.Pagination.Cursor;
            } while (!string.IsNullOrEmpty(cursor));

            return subscribers;
        }
        //write a function to set the stream title
        public async Task SetTitle(string title){
            if(Program.allowDebug){ Program.Log($"Setting title to {title}"); }
            if (api == null || broadcasterID == null || !hasMod) { return; } // safe check for public use
            if(Program.allowDebug){ Program.Log($"Getting old data"); }
            //update the title
            GetChannelInformationResponse datIn = await api.Helix.Channels.GetChannelInformationAsync(broadcasterID, UserAccessToken);
            if(Program.allowDebug){ Program.Log($"Data Obtained"); }
            ModifyChannelInformationRequest request = new ModifyChannelInformationRequest();
            request.Title = title;
            request.GameId = datIn.Data[0].GameId;
            request.BroadcasterLanguage = datIn.Data[0].BroadcasterLanguage;
            request.Delay = datIn.Data[0].Delay;
            request.Tags = datIn.Data[0].Tags;
            if(Program.allowDebug){ Program.Log($"Setting New data"); }
            try{
                if(Program.allowDebug){ Program.Log($"BroadcasterID: {broadcasterID}, Token: {UserAccessToken}"); }
                await api.Helix.Channels.ModifyChannelInformationAsync(broadcasterID, request, UserAccessToken);
            }
            catch(Exception e){
                Program.Log($"Error setting title: {e}", MessageType.Error);
            }
            
            await Program.SendMessage($"Stream title updated successfully!");
        }
        public void AddUserToTempBanned(string username, int minutes)
        {
            for(int i = 0; i < tempBanned.Count; i++)
            {
                if(tempBanned[i].username == username.ToLower())
                { tempBanned[i].Timer = minutes; return; }
            }
            tempBanned.Add(new UserWithTimer(username.ToLower(), minutes));
            //Console.WriteLine($"{username} has been temp banned for {minutes} minutes");
        }
        async Task MinTicker()
        {
            //wait for 1 minute
            await Task.Delay(60000);
            //loop through the active chat list and reduce the time to inactive by 1
            for(int i = 0; i < tempBanned.Count; i++)
            {
                tempBanned[i].Timer--;
                if(tempBanned[i].Timer < 1)
                { tempBanned.RemoveAt(i); i--; }
            }
            MinTicker();
        }

        public void UpdateGitHubCommandList(string helplist){
            if(!Program.config.uploadFullCommandList){ return;}
            apiInterface.UpdateGitHubCommandList(helplist);
        }
    }
    class UserFollow{
        public string username, ID;
        public int followStatus;
        public UserFollow(string username, string ID){
            this.username = username;
            this.ID = ID;
        }
    }
    class UserSub{
        public string username;
        public int plan;
        public UserSub(string username, int plan){
            this.username = username;
            this.plan = plan;
        }
    }
}
