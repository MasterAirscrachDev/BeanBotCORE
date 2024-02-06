using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace TwitchBot
{
    public static class SaveSystem
    {
        static List<TempMulti> userMultiCooldowns = new List<TempMulti>();
        public static async Task<User> GetUser(string username)
        {
            User user = new User();
            //check if the user exists
            //get the main folder
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //add the folder name
            dir = $"{dir}\\ReplayStudios\\BeanBot\\users\\";
            //check if the user exists
            if (File.Exists($"{dir}{username.ToLower()}.dat"))
            {
                //Console.WriteLine($"User {username} exists");
                FileSuper saveSystem = new FileSuper("BeanBot", "ReplayStudios", false);
                saveSystem.SetEncryption(SpecialDat.UserEnc);
                Save save = await saveSystem.LoadFile($"users\\{username}.dat");
                user.name = save.GetString("displayName");
                if (string.IsNullOrEmpty(user.name)) { user.name = username; }
                //Console.WriteLine(save.GetInt("points"));
                user.points = (int)save.GetInt("points");
                user.goldPoints = (int)save.GetInt("goldPoints");
                user.subTier = save.GetInt("subTier") == null ? -1 : (int)save.GetInt("subTier");
                user.ttsTokens = save.GetInt("ttsTokens") == null ? Program.config.baseTTSTokens : (int)save.GetInt("ttsTokens");
                
                user.dateSaved = save.GetString("dateSaved");
                if (user.dateSaved == null) { user.dateSaved = DateTime.Now.ToBinary().ToString(); }
                if(user.subTier == -1) { user.multiplier = Program.config.viewerMultiplier; }
                else if(user.subTier == 0){ user.multiplier = Program.config.followerMultiplier;}
                else if(user.subTier == 1){ user.multiplier = Program.config.t1Multiplier;}
                else if(user.subTier == 2){ user.multiplier = Program.config.t2Multiplier;}
                else if(user.subTier == 3){ user.multiplier = Program.config.t3Multiplier;}
                user.TempMultiplier = 0;
                foreach(TempMulti cooldown in userMultiCooldowns){
                    if(cooldown.name == user.name){
                        if(cooldown.expires > DateTime.Now){
                            user.TempMultiplier += cooldown.multiplier;
                        }
                        else{
                            userMultiCooldowns.Remove(cooldown);
                        }
                    }
                }
                //Console.WriteLine($"Loaded {user.name} with {user.points}p, {user.subTier}sub, {user.multiplier}x multi");
                return user;
            }
            else
            {
                //Console.WriteLine($"User {username} doesnt exist");
                //create the user
                user.name = username;
                user.points = 0;
                user.goldPoints = 0;
                user.multiplier = Program.config.viewerMultiplier;
                user.TempMultiplier = 0;
                user.subTier = -1;
                user.ttsTokens = Program.config.baseTTSTokens;
                return user;
            }
        }
        public static string[] GetAllFilesInFolder(string location) 
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //add the folder name
            dir = $"{dir}\\ReplayStudios\\BeanBot\\";
            dir = $"{dir}{location}";
            if(dir.EndsWith("\\")){ dir = dir.Remove(dir.Length - 1); }
            string[] files;
            //Console.WriteLine($"Getting all files in `{dir}`");
            try { files = Directory.GetFiles(dir); }
            catch { Directory.CreateDirectory(dir); files = null;  }
            return files;
        }
        public static string[] GetAllSubFolders(string folder){
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //add the folder name
            dir = $"{dir}\\ReplayStudios\\BeanBot\\";
            dir = $"{dir}{folder}";
            string[] dirs;
            try { dirs = Directory.GetDirectories(dir); }
            catch { dirs = null; }
            return dirs;
        }
        public static string[] GetPlaintextFile(string location)
        {
            //get the main folder
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //add the folder name
            dir = $"{dir}\\ReplayStudios\\BeanBot\\";
            dir = $"{dir}{location}";
            //open the file
            try
            {
                string data = File.ReadAllText(dir);
                return data.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch
            {
                Program.Log($"{location} not found", MessageType.Error);
                return null;
            }
        }
        public static void DeleteFile(string subPath){
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //add the folder name
            dir = $"{dir}\\ReplayStudios\\BeanBot\\";
            dir = $"{dir}\\{subPath}";
            File.Delete(dir);
        }
        public static async Task CreatePlaintextFile(string location, string content)
        {
            //get the main folder
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //add the folder name
            dir = $"{dir}\\ReplayStudios\\BeanBot\\";
            dir = $"{dir}{location}";
            //make sure the directory exists
            if (!Directory.Exists(Path.GetDirectoryName(dir))) { Directory.CreateDirectory(Path.GetDirectoryName(dir)); }
            //open the file
            using (FileStream sourceStream = new FileStream(dir, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(content);
                await sourceStream.WriteAsync(info, 0, info.Length);
            }
        }
        public static async Task<User[]> GetAllUsers()
        {
            //DateTime start = DateTime.Now;
            string[] names = GetAllFilesInFolder("users");
            if(names == null || names.Length == 0) { return null; }
            //get just the name of the file ignoreing location and extention
            int fTrim = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\ReplayStudios\\BeanBot\\users\\".Length;
            User[] users = new User[names.Length];
            for (int i = 0; i < names.Length; i++)
            { 
                names[i] = names[i].Remove(0, fTrim); 
                names[i] = names[i].Remove(names[i].Length - 4);
                users[i] = await GetUser(names[i]);
            }
            //Console.WriteLine($"Got {users.Length} users in {DateTime.Now.Subtract(start).TotalSeconds}s");
            return users;
        }
        public static async Task UpdateAllUsers(bool doTax = false, bool retry = false){
            DateTime start = DateTime.Now;
            User[] users = await GetAllUsers();
            List<string> followerNames;
            List<UserSub> subs;
            int taxMoney = 0, followCount = 0, subCount = 0;
            try{
                await Program.twitchLibInterface.bot.CheckToken();
                followerNames = await Program.twitchLibInterface.bot.GetFollowers();
                subs = await Program.twitchLibInterface.bot.GetSubscribers();
            }
            catch(Exception e){
                if(retry){ Program.Log($"Error updating all users: {e}", MessageType.Error); return;}
                await Program.twitchLibInterface.bot.RefreshBotToken();
                await Task.Delay(10000);
                UpdateAllUsers(doTax, true); return;
            }
            
            for (int i = 0; i < users.Length; i++)
            {
                if(users[i].name == "AwesomeBean_BOT"){ continue; }
                bool changed = false;
                //set the multi based on if the user is following
                
                int followRank = -1, oldrank = users[i].subTier;
                //check if we are in subs
                if(subs != null){
                    for(int j = 0; j < subs.Count; j++){
                        if(subs[j].username.ToLower() == users[i].name.ToLower()){
                            followRank = subs[j].plan;
                            subCount++;
                            break;
                        }
                    }
                }
                
                if(followRank == -1 && followerNames != null){
                    //check if we are in followers
                    for(int j = 0; j < followerNames.Count; j++){
                        if(followerNames[j].ToLower() == users[i].name.ToLower()){
                            followRank = 0;
                            followCount++;
                            break;
                        }
                    }
                }
                changed = (followRank != oldrank);
                users[i].subTier = followRank;
                
                if(doTax && Program.config.taxPercent > 0 && (users[i].points > Program.config.taxThreshold)){
                    taxMoney =+ (int)((users[i].points - Program.config.taxThreshold) * (Program.config.taxPercent / 100f));
                    users[i].points -= taxMoney;
                    changed = true; 
                }
                if(changed){
                    //Program.Log($"Changing {users[i].name} from {oldrank} to {followRank}", MessageType.Debug);
                    SaveUser(users[i]);
                }
            }
            if(taxMoney > 0){
                User bot = await GetUser("AwesomeBean_BOT");
                bot.points += (int)Math.Round(taxMoney * 0.1f);
                SaveUser(bot);
            }
            Program.Log($"Active Followers: {followCount}, Active Subs: {subCount}");
            Program.Log($"Updated {users.Length} users in {DateTime.Now.Subtract(start).TotalSeconds}s");
        }
        public static async Task SaveUser(User user)
        {
            //Console.WriteLine($"saving user: {user.name}, display name: {user.name}, points: {user.points}, gold points: {user.goldPoints}, multiplier: {user.multiplier}");
            FileSuper saveSystem = new FileSuper("BeanBot", "ReplayStudios", false);
            saveSystem.SetEncryption(SpecialDat.UserEnc);
            Save save = new Save();
            
            //clamp user points between 0 and 1B
            if (user.points < 0) user.points = 0; if (user.points > 1000000000) user.points = 1000000000;
            save.SetInt("points", user.points);
            save.SetInt("goldPoints", user.goldPoints);
            save.SetInt("subTier", user.subTier);
            save.SetInt("ttsTokens", user.ttsTokens);
            save.SetString("displayName", user.name);
            //note the date of the last save
            save.SetString("lastSave", DateTime.Now.ToBinary().ToString());
            await saveSystem.SaveFile($"users\\{user.name}.dat", save);
        }
        public static async Task ClearOldUsers()
        {
            User[] users = await GetAllUsers();
            if (users == null || users.Length < 101) return;
            //string[] names = new string[100];
            //delete all users that have not been saved in the last 30 days and arn't in the top 100 points
            //sort all the users by points
            int count = 0;
            Array.Sort(users, delegate(User user1, User user2) { return user1.points.CompareTo(user2.points); });
            for(int i = 1000; i < users.Length; i++){
                if(users[i].dateSaved == null) continue;
                DateTime date = DateTime.FromBinary(Convert.ToInt64(users[i].dateSaved));
                if(date.AddDays(30) < DateTime.Now){
                    //delete the user
                    count++;
                    File.Delete($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\ReplayStudios\\BeanBot\\users\\{users[i].name}.dat");
                }
            }
            if(count > 0) { Program.Log($"Deleted {count} users"); }
        }
        public static void AddUserTempMulti(string name, float multiplier, int mins){
            TempMulti temp = new TempMulti();
            temp.name = name;
            temp.multiplier = multiplier;
            temp.expires = DateTime.Now.AddMinutes(mins);
            userMultiCooldowns.Add(temp);
        }
    }
}
public struct User{
    public string name, dateSaved;
    public int points, goldPoints, subTier, ttsTokens;
    public float multiplier, TempMultiplier;
    public User(string name, int points){
        this.name = name;
        this.points = points;
        goldPoints = 0;
        multiplier = 1;
        TempMultiplier = 1;
        ttsTokens = 0;
        subTier = -1; //-1 = viewer, 0 = follower, 4 = prime, 1 = tier 1, 2 = tier 2, 3 = tier 3
        dateSaved = DateTime.Now.ToBinary().ToString();
    }
}
struct TempMulti{
    public string name;
    public float multiplier;
    public DateTime expires;
}
