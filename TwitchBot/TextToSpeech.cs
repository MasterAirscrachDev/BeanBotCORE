using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;

namespace TwitchBot
{
    class TextToSpeech
    {
        public SpeechSynthesizer voice = new SpeechSynthesizer();
        List<string> voices = new List<string>();
        List<TTSReplace> ttsReplaceList = new List<TTSReplace>();
        public TextToSpeech()
        {
            string voiceName = Program.config.ttsVoice;
            //add all the voices to the list
            for (int i = 0; i < voice.GetInstalledVoices().Count; i++)
            { voices.Add(voice.GetInstalledVoices()[i].VoiceInfo.Name); }
            if(voices.Count == 0) { Program.Log("No voices found, Please Install A Windows Voice", MessageType.Warning); return; }
            if(voices.Contains(voiceName)) { voice.SelectVoice(voiceName); }
            else if (voices.Contains("Microsoft David Desktop"))
            { voice.SelectVoice("Microsoft David Desktop"); }
            else { voice.SelectVoice(voices[0]); }
            GetReplaceList();
        }
        async Task GetReplaceList(){
            //Console.WriteLine("Loading TTS Replace List");
            string[] lines = SaveSystem.GetPlaintextFile("TTSReplaceList.txt");
            if(lines == null){
                Program.Log("TTS Replace List Not Found, Creating Default", MessageType.Success);
                await SaveSystem.CreatePlaintextFile("TTSReplaceList.txt", "Use this to correct name pronounciation and filter tts\nThis IS capitalisation sensitive\nkill<love\ndie<live\nlose<win");
                lines = new string[]{"kill<love", "die<live", "lose<win"};
            }
            ttsReplaceList.Clear();
            foreach(string line in lines){
                if(line.Contains('<')){
                    //Console.WriteLine($"TTS Replace List: {line}");
                    string[] split = line.Split('<');
                    if(split.Length == 2){
                        ttsReplaceList.Add(new TTSReplace(split[0], split[1]));
                        Program.Log($"TTS Replace: {split[0]} -> {split[1]}");
                    }
                }
                else{
                    //Console.WriteLine($"TTS Replace List Error: {line}");
                }
            }
            Program.Log("TTS Replace List Loaded", MessageType.Success);
        }
        public void ReloadTTS(){
            GetReplaceList();
        }
        public ProcessData BuyTTS(ProcessData data){
            //get the number of tokens to buy
            try{
                int tokens = int.Parse(data.message.content.Remove(0, 7));
                if(tokens < 1){ data.returnMessage = $"@{data.message.sender} Please enter a number of tokens to buy"; return data; }
                //add a 5% discount for every token bought capped at 50%
                int cost = Program.config.ttsCost * tokens;
                int discount = 0;
                if(tokens > 1){
                    discount = (int)(cost * (tokens * 0.05));
                    if(discount > (cost / 2)){ discount = cost / 2; }
                }
                cost -= discount;
                //check if the user has enough tokens
                //if cost is negative then auto return
                if(cost < 0){ data.returnMessage = $"@{data.message.sender} Naughty Naughty"; return data; }
                if(data.user.points < cost){
                    data.returnMessage = $"@{data.message.sender} You do not have enough {Program.config.currencies} to buy {tokens} TTS Tokens (You Need: {cost})";
                    return data;
                }
                //take the tokens from the user
                data.user.points -= cost;
                //add the tokens to the streamer
                data.user.ttsTokens += tokens;
                data.returnMessage = $"@{data.message.sender} bought {tokens} TTS Tokens for {cost} {Program.config.currencies}";
                return data;
            }
            catch{
                data.returnMessage = $"@{data.message.sender} Please enter a number of tokens to buy";
                return data;
            }
        }
        public async Task<ProcessData> EditTTS(ProcessData data){
            //message will be edittts @user amount
            //get the user to edit and the amount to edit by
            try{
                string[] split = data.message.content.Split(' ');
                if(split.Length != 3){ data.returnMessage = $"@{data.message.sender} use !editTTS @user amount"; return data; }
                string user = split[1].Replace("@", "");
                int amount = int.Parse(split[2]);
                //cap the amount between -9999 and 9999
                if(amount > 9999){ amount = 9999; }
                if(amount < -9999){ amount = -9999; }
                User u = await SaveSystem.GetUser(user);
                if(amount == 0){ u.ttsTokens = 0;}
                else{ u.ttsTokens += amount; }
                await SaveSystem.SaveUser(u);
                data.returnMessage = $"@{data.message.sender} @{user} now has {u.ttsTokens} TTS Tokens";
                return data;
            }
            catch{
                data.returnMessage = $"@{data.message.sender} use !editTTS @user amount";
                return data;
            }
            
            
        }
        public async Task SystemSay(string message, string user){
            int length = message.Length;
            if(Program.config.betaTTSFilter && (length > 10 || length == 1)){
                if(!FilterText(message)){ return; }
            }
            //split the message into words
            string[] words = message.Split(' ');
            for(int i = 0; i < words.Length; i++){
                foreach(TTSReplace r in ttsReplaceList){
                    if(words[i] == r.replace){
                        words[i] = r.with;
                    }
                }
            }
            message = string.Join(" ", words);
            message = $"{user} said {message}";
            voice.SpeakAsync(message);
        }
        public async Task<ProcessData> Say(ProcessData data)
        {
            string message;
            try { message = data.message.content.Remove(0, 4); } catch {
                data.returnMessage = $"@{data.message.sender} Please enter a message to say";
                return data;
            }
            //get the length of the message
            int length = message.Length;
            int maxAffordableLength = data.user.ttsTokens * Program.config.ttsPerToken;
            if(length > maxAffordableLength){
                data.returnMessage = $"@{data.message.sender} You do not have enough TTS Tokens. use !buyTTS to buy more";
                return data;
            }
            if(Program.config.betaTTSFilter && (length > 10 || length == 1)){
                if(!FilterText(message)){ return data; }
            }
            Program.Log($"Length of message: {length}", MessageType.Debug);
            int cost = length / Program.config.ttsPerToken;
            if(cost < 1){ cost = 1; }
            if (data.user.ttsTokens >= cost)
            {
                //split the message into words
                string[] words = message.Split(' ');
                for(int i = 0; i < words.Length; i++){
                    foreach(TTSReplace r in ttsReplaceList){
                        if(words[i] == r.replace){
                            words[i] = r.with;
                        }
                    }
                }
                message = string.Join(" ", words);
                if(data.message.isWhisper){ message = $"{data.user.name} said {message}"; }
                voice.SpeakAsync(message);
                Program.Log($"tts cost {cost} for: {message}", MessageType.Debug);
                if(Program.config.chatSayTTSCost){
                    data.returnMessage = $"@{data.user.name} used tts for {cost} TTS Tokens";
                }
                data.user.ttsTokens -= cost;
                return data;
            }
            else
            {
                data.returnMessage = $"@{data.user.name} you do not have enough TTS Tokens, buy some with !buyTTS";
                return data;
            }
            
        }



        bool FilterText(string message){
            if(message.Length == 1){ Program.Log("TTS Failed: single char message", MessageType.Warning); return false;}
            //do checks for the message to prevent spam
            //int spaces = message.Count(f => f == ' ');
            //if(spaces < 3){ Console.BackgroundColor = ConsoleColor.DarkYellow; Console.WriteLine("TTS Failed: not enough spaces"); return data; }
            string[] words = message.Split(' ');
            int wordCount = words.Length;
            if(wordCount == 1 && words[0].Length > 15){ Program.Log("TTS Failed: single long word", MessageType.Warning); return false; }
            //if any of the words is longer than 20 characters fail
            foreach(string word in words){ if(word.Length > 20){ Program.Log("TTS Failed: found word too long", MessageType.Warning); return false; } }
            if(wordCount > 2){
                //check the max number of duplicate words
                string filteredMessage = message.Replace("  ", " ");
                //replace any non-letter or number characters with spaces
                for(int i = 0; i < filteredMessage.Length; i++){
                    if(!char.IsLetterOrDigit(filteredMessage[i])){
                        filteredMessage = filteredMessage.Remove(i, 1);
                        filteredMessage = filteredMessage.Insert(i, " ");
                    }
                }
                while(filteredMessage.Contains("  ")){ filteredMessage = filteredMessage.Replace("  ", " "); }
                words = filteredMessage.Split(' ');
                int maxDuplicateWords = 0;
                string maxDuplicateWord = "";
                for(int i = 0; i < wordCount; i++){
                    int duplicateWords = 0;
                    for(int j = 0; j < wordCount; j++){ if(words[i] == words[j]){ duplicateWords++; } }
                    if(duplicateWords > maxDuplicateWords){
                        maxDuplicateWords = duplicateWords; maxDuplicateWord = words[i];
                    }
                }
                //get the ratio of duplicate words to total words
                float ratio = (float)maxDuplicateWords / (float)wordCount;
                Program.Log($"Duplicates: {ratio * 100}%", MessageType.Debug);
                if(ratio > 0.46f){ Program.Log("TTS Failed: too many duplicate words 0", MessageType.Warning); return false; }


                //bool isJibberish = IsJibberish(message);
                //if(isJibberish){ Program.Log("TTS Failed: jibberish", MessageType.Warning); return false; }
            }
            Program.Log("TTS Filter Passed", MessageType.Success);
            return true;
        }
        public void ListVoices()
        {
            Program.Log("Listing voices", MessageType.Debug);
            for (int i = 0; i < voices.Count; i++)
            { Program.Log($"{i}: {voices[i]}"); }
        }
        public void StopTTS(){
            voice.SpeakAsyncCancelAll();
            //voice.SpeakAsyncCancel
        }
        class TTSReplace{
            public string replace;
            public string with;
            public TTSReplace(string r, string w){
                replace = r;
                with = w;
            }
        }
    }
}
