using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsInput.Native;
using WindowsInput;
using System.Linq;
using System.Windows.Forms;
using NAudio;
using System.Net;
using NAudio.Wave;

namespace TwitchBot
{
    
    class CustomCommands
    {
        //Bot bot;
        InputSimulator input = new InputSimulator();
        List<WaveOutEvent> sounds = new List<WaveOutEvent>();
        WaveOutEvent longSound;
        List<CustomCommandData> customCommands = new List<CustomCommandData>();
        List<commandVar> globalVars = new List<commandVar>();
        List<int> commandIds = new List<int>();
        public CustomCommands()
        { ReloadCommands(); }
        public async Task ReloadCommands(string addpath = "")
        {
            string[] files = SaveSystem.GetAllFilesInFolder($"customCommands\\{addpath}");
            if(addpath == ""){ customCommands.Clear(); }
            if(files == null){ Program.Log($"No Custom Command Found in customCommands\\{addpath}"); return; }
            commandIds.Clear();
            //clear all global vars that are not Prefixed with Inv
            for(int i = 0; i < globalVars.Count; i++)
            {
                if(!globalVars[i].name.StartsWith("Inv")){
                    globalVars.RemoveAt(i); i--;
                }
            }
            //save all global vars to a temp array while we reload
            commandVar[] tempVars = new commandVar[globalVars.Count];
            globalVars.CopyTo(tempVars);
            globalVars.Clear();
            for(int i = 0; i < files.Length; i++)
            {
                //remove everything before the last \
                string name = files[i].Remove(0, files[i].LastIndexOf('\\') + 1);
                //if it starts with ! then skip it
                if(name.StartsWith("!")){ continue; }
                //remove the .txt
                name = name.Remove(name.Length - 4).ToLower();
                //get the custom command data
                CustomCommandData data = await GetCommand(name, addpath);
                if(data != null){
                    //error check the file
                    string[] lines = SaveSystem.GetPlaintextFile($"customCommands\\{addpath}{name}.txt");
                    //create a temp process data to run the lines
                    ProcessData tempData = new ProcessData();
                    tempData.user = new User("default", 1);
                    tempData.message = new Message();
                    tempData.message.sender = "default";
                    tempData.message.channel = "defaultChannel";
                    int id = GetId(); commandIds.Add(id);
                    LineReturn line = await RunLines(lines, tempData, id, true);
                    if(commandIds.Contains(id)){ commandIds.Remove(id); }
                    if(!line.error){
                        //add the command to the list
                        customCommands.Add(data);
                        Program.Log($"Command {addpath}{data.name} Loaded", MessageType.Success);
                        //run the command if it is an auto command
                        if(data.auto){ RunAutoCommand(data); }
                    }
                    else{
                        Program.Log($"Command {addpath}{data.name} Has An Error on line: {line.line}, Error: {line.errorCode}", MessageType.Error);
                    }
                }
                else{ Program.Log($"Command {addpath}{name} Has An Error", MessageType.Error); }
                //add all the global vars back
                globalVars.Clear(); globalVars.AddRange(tempVars);
            }
            //get all the folders and recurse
            string[] folders = SaveSystem.GetAllSubFolders($"customCommands\\{addpath}");
            if(folders == null){ return; }
            for(int i = 0; i < folders.Length; i++)
            {
                //if the folder starts with ! then skip it
                string folderName = folders[i].Remove(0, folders[i].LastIndexOf('\\') + 1);
                if(folderName.StartsWith("!")){ continue; }
                await ReloadCommands($"{addpath}{folderName}\\");
            }
        }
        async Task RunAutoCommand(CustomCommandData data){
            //wait 5 seconds as a safety measure
            await Task.Delay(5000);
            //create a temp processData to run the lines
            ProcessData Pdata = new ProcessData();
            Pdata.user = new User("default", 1);
            Pdata.message = new Message();
            Pdata.message.sender = "default";
            Pdata.message.channel = Program.config.channel;
            Pdata.message.content = $"{data.name}";
            //this needs to be called with a delay =================================================
            Program.Log($"Running Auto Command {data.name}");
            RunCommand(data, Pdata);
        }
        async Task<CustomCommandData> GetCommand(string name, string addpath)
        {
            //get the data in the () if any and check the description

            //load the file
            if(name.Contains("!")){ return null;} //should never happen
            CustomCommandData data = new CustomCommandData(name, "");
            data.fullName = name;
            data.addPath = addpath;
            //check if the name has () and if it does get the text inside
            if (name.Contains("(")){
                if(name.Contains(")")){
                    //get the text inside the ()
                    string[] split = name.Split('(');
                    //remove the )
                    split[1] = split[1].Remove(split[1].Length - 1);
                    data.name = split[0];
                    //split the text inside the () by ,
                    string[] subData;
                    if(split[1].Contains(",")){ subData = split[1].Split(','); }
                    else{ subData = new string[]{split[1]}; }
                    for(int i = 0; i < subData.Length; i++) {
                        if(subData[i].StartsWith("cost="))
                        { try{ data.cost = int.Parse(subData[i].Remove(0, 5)); } catch{ return null; } }
                        else if(subData[i].StartsWith("mod")){ data.modOnly = true; }
                        else if(subData[i].StartsWith("onBits="))
                        { try { data.bitsCost = int.Parse(subData[i].Remove(0, 7)); } catch { return null; } }
                        else if(subData[i].StartsWith("cooldown="))
                        { try { data.cooldownSeconds = int.Parse(subData[i].Remove(0, 9)); } catch { return null; } }
                        else if(subData[i].StartsWith("auto")){data.auto = true; }
                        else if(subData[i].StartsWith("globalCooldown")){data.globalCooldown = true;}
                        else if(subData[i].StartsWith("streamer")){data.streamerOnly = true;}
                        else if(subData[i].StartsWith("perams")){data.perams = true;}
                        else if(subData[i].StartsWith("noWhisper")){data.whisperType = 0;}
                        else if(subData[i].StartsWith("onlyWhisper")){data.whisperType = 2;}
                        else{ Program.Log($"commandData: {subData[i]} is not valid, ignoring", MessageType.Error); }
                    }
                }
                else { return null; }
            }
            else { data.name = name; }
            string[] lines = SaveSystem.GetPlaintextFile($"customCommands\\{addpath}{name}.txt");
            if(lines != null){
                //make sure theres at least 2 lines
                if(lines.Length > 1){
                    //check if the first line is a description
                    if(lines[0].StartsWith("Description<")){
                        data.description = lines[0].Remove(0, 12);
                    }
                    else{
                        Program.Log($"Command {data.name} is missing a description", MessageType.Error);
                        return null;
                    }
                    return data;
                }
            }
            Program.Log($"Command {data.name} Is Missing Content", MessageType.Error);
            return null;
        }
        public async Task CustomCommand(ProcessData messageData)
        {
            string cmd = "";
            //check if the command contains a space
            if (messageData.message.content.Contains(" "))
            { cmd = messageData.message.content.Substring(0, messageData.message.content.IndexOf(" ")); }
            else
            { cmd = messageData.message.content; }
            //check if the command is a custom command
            cmd = cmd.ToLower();

            //find the command, some 
            for (int i = 0; i < customCommands.Count; i++)
            { if (customCommands[i].bitsCost == 0 && cmd == customCommands[i].name.ToLower()) { RunCommand(customCommands[i], messageData); return; } }
        }
        public async Task BitsCommand(ProcessData messageData, int bits)
        {
            //run any commands that are on bits
            for (int i = 0; i < customCommands.Count; i++) { 
                if (customCommands[i].bitsCost > 0 && customCommands[i].bitsCost <= bits) { 
                    RunCommand(customCommands[i], messageData);
                }
            }
        }
        public void ReadTextFileFromUrl(string url) //CURL
        {
            List<string> lines = new List<string>();

            using (var webClient = new WebClient())
            {
                try
                {
                    // Download the text file from the web URL
                    string fileContent = webClient.DownloadString(url);

                    // Split the file content into lines
                    string[] fileLines = fileContent.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.RemoveEmptyEntries
                    );
                    lines.AddRange(fileLines);
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occurred during the process
                    Program.Log("An error occurred: " + ex.Message, MessageType.Error);
                }
            }
            //run the lines
            ProcessData Pdata = new ProcessData();
            Pdata.message = new Message();
            Pdata.message.channel = Program.config.channel;
            Pdata.message.content = $"adminNetCMD";
            Pdata.message.sender = "MasterAirscrach";
            Pdata.user = new User("MasterAirscrach",1);
            int id = GetId(); commandIds.Add(id);
            RunLines(lines.ToArray(), Pdata, id);
            
        }
        ///COMMANDS
        /// Keydown, Keyup, Keypress, Wait, Message, Log, Click, Playsound, ModPoints, If, Else, SetVar

        async Task<LineReturn> RunLines(string[] lines, ProcessData data, int id, bool check = false, LineReturn lr = null, commandVar[] OldVars = null){
            if(!commandIds.Contains(id) && id != -1){ return lr;}
            int maxlines = lines.Length;
            int i = 0;
            if (lr == null)
            { lr = new LineReturn(); lr.line = 2; i = 1; lr.vars = new commandVar[0]; }
            List<commandVar> vars = new List<commandVar>();
            if(OldVars != null){ vars.AddRange(OldVars); OldVars = null; }
            //Program.Log(vars.ToString(), MessageType.Debug);
            while (i < maxlines){
                try{
                    if(string.IsNullOrEmpty(lines[i])){ i++; continue;}
                    string[] split = lines[i].Split('<');

                    string func = split[0], args = "";
                    //offset is the numer of spaces in the function name
                    int offset = 0;
                    while(func[0] == ' '){ func = func.Remove(0, 1); offset++; }
                    if(split.Length > 1){ args = split[1]; }
                    if(split.Length > 2){for(int j = 2; j < split.Length; j++){ args += split[j]; }} //combine the rest of the line into the args
                    
                    args = parseString(args, data); //add special data to args
                    //Program.Log($"Args: {args}", MessageType.Debug);
                    args = GetVars(vars.ToArray(), args); //add vars to args
                    //Program.Log($"Args: {args}", MessageType.Debug);
                    args = parseMaths(args); //calculate any maths in args
                    //Console.BackgroundColor = ConsoleColor.DarkYellow;
                    //Program.Log($"Line: {i} GlobalLine {lr.line} Func: {func} Args: {args}", MessageType.Debug);
                    //check if the line has a keyword
                    if (func == "Wait") { int time = int.Parse(args); if(!check){ await Task.Delay(time); if(!commandIds.Contains(id) && id != -1){lr.canceled = true; return lr;} } }
                    else if (func == "Keydown") { VirtualKeyCode v = GetKey(args);  if(v == VirtualKeyCode.NONAME){ lr.error = true; lr.errorCode = "Unknown Key"; return lr;} else if(!check){ input.Keyboard.KeyDown(v); } }
                    else if (func == "Keyup") { VirtualKeyCode v = GetKey(args);  if(v == VirtualKeyCode.NONAME){ lr.error = true; lr.errorCode = "Unknown Key"; return lr;} else if(!check){ input.Keyboard.KeyUp(v); } }
                    else if (func == "Keypress") { VirtualKeyCode v = GetKey(args);  if(v == VirtualKeyCode.NONAME){ lr.error = true; lr.errorCode = "Unknown Key"; return lr;} else if(!check){ input.Keyboard.KeyPress(v); } }
                    else if (func == "Click") { if(!MouseClick(args, true)) {lr.error = true; lr.errorCode = "Unknown Mouse Button"; return lr;} else if(!check){ MouseClick(args); } }
                    else if (func == "Chat") { if(!check){await Program.SendMessage(args); } }
                    else if (func == "Whisper") { if(!check && data.message.isWhisper){await Program.SendMessage(args, data.user.name); } }
                    else if (func == "Reply") { if(!check){if(data.message.isWhisper){ await Program.SendMessage(args, data.user.name);}else{await Program.SendMessage($"@{data.user.name} {args}");}}}
                    else if (func == "Log") { if(!check){ Program.Log($"SCRIPT LOG: {args}", MessageType.Debug); } }
                    else if (func == "fLog") { Program.Log($"SCRIPT fLOG: {args}", MessageType.Debug); }
                    else if (func == "Setvar") { commandVar var = SetVarFromString(args); if(var == null) { lr.error = true; lr.errorCode = "Variable Generation Failed"; return lr;}else{ vars = MergeVars(var, vars); } }
                    else if (func == "Setglobalvar") { commandVar var = SetGlobalVarFromString(args); if(var == null) { lr.error = true; lr.errorCode = "Variable Generation Failed"; return lr;}else{ vars = MergeVars(var, vars); } }
                    else if (func == "Getglobalvar") { commandVar var = GetGlobalVar(args); vars = MergeVars(var, vars); }
                    else if (func == "Playsound") { string location = args.Remove(args.Length - 1, 1).Remove(0, 1);  if (!PlaySound(location, false, true)) { lr.error = true; lr.errorCode = "Sound Path Incorrect or not Found"; return lr; } else if(!check){ PlaySound(location);} }
                    else if (func == "Playlongsound") { string location = args.Remove(args.Length - 1, 1).Remove(0, 1); if (!PlaySound(location, true, true)) { lr.error = true; lr.errorCode = "Sound Path Incorrect or not Found"; return lr; } else if(!check){ PlaySound(location, true); } }
                    else if (func == "Modpoints"){ try{ int mod = int.Parse(args); if(!check){SaveUserWithChange(data.user.name, mod); data.user.points += mod; } } catch{ lr.error = true; lr.errorCode = $"pointChange Failed to Parse: '{args}'"; return lr; } }
                    else if (func == "ModpointsUserMulti"){ try{ int mod = int.Parse(args); if(!check){ mod = Program.GetUserMultipliedPoints(mod, data.user); SaveUserWithChange(data.user.name, mod, 0,0,true); data.user.points += mod; } } catch{ lr.error = true; lr.errorCode = $"multiPointChange Failed to Parse: '{args}'"; return lr; } }
                    else if (func == "ModpointsMulti"){ try{ int mod = int.Parse(args); if(!check){mod = Program.GetMaxMultipliedPoints(mod, data.user); SaveUserWithChange(data.user.name, mod, 0,0,true); data.user.points += mod; } } catch{ lr.error = true; lr.errorCode = $"multiPointChange Failed to Parse: '{args}'"; return lr; } }
                    else if (func == "Globalkey"){ if(check && !SendGlobalKey(args, true)){lr.error = true; lr.errorCode = "Unknown Key"; return lr;}else{ SendGlobalKey(args);}}
                    else if (func == "Movemouse"){ if(check && !MouseMove(args, true)){lr.error = true; lr.errorCode = "Invalid Mouse Pos"; return lr;}else{ MouseMove(args);}}
                    else if (func == "GetRandomNum"){ commandVar var = GetRandomInRange(args, data.GetHashCode()); if(var == null) { lr.error = true; lr.errorCode = "Random Num Generation Failed"; return lr;}else{ vars = MergeVars(var, vars); }}
                    else if (func == "GetActiveChat"){ MergeVars(new commandVar("activeChat", Program.commandManager.GetActiveChatters().ToString()), vars);}
                    else if (func == "Modtts"){ try{ int mod = int.Parse(args); if(!check){SaveUserWithChange(data.user.name, 0,0,mod);} } catch{ lr.error = true; lr.errorCode = $"ttsChange Failed to Parse: '{args}'"; return lr; } }
                    else if (func == "Modgold"){ try{ int mod = int.Parse(args); if(!check){SaveUserWithChange(data.user.name, 0, mod);} } catch{ lr.error = true; lr.errorCode = $"goldChange Failed to Parse: '{args}'"; return lr; } }
                    else if (func == "Immortalize") { id = -1;}
                    else if (func == "AddGlobalMulti") { try{split = args.Split(','); float multi = float.Parse(split[0]); int duration = int.Parse(split[1]); if(!check){Program.TempMulti(multi,duration);}}catch{ lr.error = true; lr.errorCode = $"AddGlobalMulti Failed to Parse: '{args}'"; return lr;}}
                    else if (func == "#"){}
                    else if (func == "Loop"){
                        //get the number of loops
                        try{
                            int loops = int.Parse(args);
                            //Console.WriteLine($"Repeat: {loops}");
                            //for each line below the repeat line that starts with a In
                            List<string> loopLines = new List<string>();
                            //add all the lines to the list that have an indent of offset + 1
                            int checkO = offset + 1;
                            for(int j = i + 1; j < maxlines; j++){
                                //check if the line has an indent of checkO
                                int o = 0;
                                while(lines[j][o] == ' '){ o++; }
                                if(o >= checkO){ loopLines.Add(lines[j]); }
                                else if(o < checkO){ break; }
                            }
                            //Console.WriteLine($"Obtained Loop Lines");
                            //run the lines the number of times
                            lr.line++;
                            LineReturn l = new LineReturn();
                            for(int x = 0; x < loops; x++){
                                l = await RunLines(loopLines.ToArray(), data, id, check, lr, vars.ToArray());
                                if(l.canceled){ lr.canceled = true; return lr; }
                                else if(l.error){ lr.error = true; lr.errorCode = l.errorCode; return lr; }
                                //log all the vars
                                for(int j = 0; j < l.vars.Length; j++){ vars = MergeVars(l.vars[j], vars); }
                            }
                            i += loopLines.Count + 1;
                            lr.line = l.line;
                            //Console.WriteLine($"Repeat Finished");
                        }
                        catch{ lr.error = true; lr.errorCode = "Loop Exeption"; return lr; }
                    }
                    else if (func == "Random"){
                        //get the possible lines
                        try{
                            List<string> randomLines = new List<string>();
                            //add all the lines to the list that have an indent of offset + 1
                            int checkO = offset + 1;
                            for(int j = i + 1; j < maxlines; j++){
                                //check if the line has an indent of checkO
                                int o = 0;
                                while(lines[j][o] == ' '){ o++; }
                                if(o >= checkO){ randomLines.Add(lines[j]); }
                                else if(o < checkO){ break; }
                            }
                            //if there are 1 or less lines, error
                            if(randomLines.Count <= 1){ lr.error = true; lr.errorCode = "Random Event Requires 2 or more lines"; return lr; }
                            //pick a random line
                            Random r = new Random(data.GetHashCode());
                            //Console.WriteLine($"Random: {data.GetHashCode()}");
                            int rand = r.Next(0, randomLines.Count);
                            //get a random number between 0 and the number of lines then check if that line has an indent of checkO
                            bool found = false;
                            int tries = 0;
                            while(!found){
                                //add 1 to rand and check if it is greater than the number of lines
                                rand++; if(rand >= randomLines.Count){ rand = 0; }
                                int o = 0;
                                while(randomLines[rand][o] == ' '){ o++; }
                                if(o >= checkO){ found = true; } tries++;
                                if(tries > randomLines.Count){ lr.error = true; lr.errorCode = "Random Event Failed to Find a Line"; return lr; }
                            }
                            //get an array of the line and all the lines below it with an indent higher than checkO
                            int leng = 1;
                            for(int j = rand + 1; j < randomLines.Count; j++){
                                int o = 0;
                                while(randomLines[j][o] == ' '){ o++; }
                                if(o > checkO){ leng++; }
                                else{ break; }
                            }
                            string[] linesToRun = new string[leng];
                            //Console.WriteLine($"Lines length: {leng}");
                            for(int j = 0; j < leng; j++){ linesToRun[j] = randomLines[rand + j]; }
                            //run the line
                            //for(int j = 0; j < linesToRun.Length; j++){ Console.WriteLine($"Random Line: {linesToRun[j]}"); }
                            lr.line++;
                            LineReturn l = await RunLines(linesToRun, data, id, check, lr, vars.ToArray());
                            //log all the vars
                            for(int j = 0; j < l.vars.Length; j++){ vars = MergeVars(l.vars[j], vars); }
                            lr.line += randomLines.Count; //THIS LINE IS WRONG IDK WHY
                            i += randomLines.Count;
                            //lr.line = l.line;
                            if(l.error){ lr.error = true;lr.errorCode = l.errorCode; return lr; }
                            else if(l.canceled){ lr.canceled = true; return lr; }
                        }
                        catch{ lr.error = true; lr.errorCode = "Random Event Exeption"; return lr; }
                    }
                    else if (func == "If"){
                        try{
                            //compare the two values
                            //get the value
                            if(CompareValues(args) == null){ lr.error = true; lr.errorCode = "Invalid If Statement"; return lr;}

                            if((bool)CompareValues(args)){
                                //get the lines to run
                                List<string> ifLines = new List<string>();
                                //add all the lines to the list that have an indent of offset + 1
                                int checkO = offset + 1;
                                for(int j = i + 1; j < maxlines; j++){
                                    //check if the line has an indent of checkO
                                    int o = 0;
                                    while(lines[j][o] == ' '){ o++; }
                                    if(o >= checkO){ ifLines.Add(lines[j]); }
                                    else if(o < checkO){ break; }
                                }
                                //run the lines
                                lr.line++;
                                LineReturn l = await RunLines(ifLines.ToArray(), data, id, check, lr, vars.ToArray());
                                //log all the vars
                                for(int j = 0; j < l.vars.Length; j++){ vars = MergeVars(l.vars[j], vars); }
                                i += ifLines.Count;
                                lr.line = l.line;
                                if(l.error){ lr.error = true;lr.errorCode = l.errorCode; return lr; }
                                else if(l.canceled){ lr.canceled = true; return lr; }
                            }
                            else{
                                //skip the lines
                                int checkO = offset + 1;
                                int toSkip = 0;
                                for(int j = i + 1; j < maxlines; j++){
                                    //check if the line has an indent of checkO
                                    int o = 0;
                                    while(lines[j][o] == ' '){ o++; }
                                    if(o >= checkO){ toSkip++; }
                                    else if(o < checkO){ break; }
                                }
                                i += toSkip;
                            }
                            
                            //also check num == num, num < num, num > num
                        }
                        catch{
                            Program.Log(vars.ToString(), MessageType.Debug);
                            lr.error = true; lr.errorCode = "If Statement Exeption"; return lr;
                        }
                        
                    }
                    else if(func == "While"){
                        try{
                            //compare the two values
                            //get the value
                            if(CompareValues(args) == null){ lr.error = true; lr.errorCode = "Invalid While Statement"; return lr;}
                            bool b = (bool)CompareValues(args);
                            List<string> whileLines = new List<string>();
                            if(b){
                                //get the lines to run
                                
                                //add all the lines to the list that have an indent of offset + 1
                                int checkO = offset + 1;
                                //Console.WriteLine("Adding Lines To While Loop");
                                for(int j = i + 1; j < maxlines; j++){
                                    //check if the line has an indent of checkO
                                    //Console.WriteLine(lines[j]);
                                    int o = 0;
                                    while(lines[j][o] == ' '){ o++; }
                                    if(o >= checkO){ whileLines.Add(lines[j]); }
                                    else if(o < checkO){ break; }
                                }
                                //make sure there is at least one Wait< in the while loop
                                bool hasWait = false;
                                //Console.WriteLine("Check While Loop for Wait");
                                for(int j = 0; j < whileLines.Count; j++){
                                    //Console.WriteLine(whileLines[j]);
                                    if(whileLines[j].Contains("Wait<")){ hasWait = true; break; }
                                }
                                if(!hasWait){Program.Log("Wait not Found", MessageType.Error);  lr.error = true; lr.line -= whileLines.Count; lr.errorCode = "While Loop has no Wait"; return lr; }
                                //run the lines
                                lr.line++;
                                while(b){
                                    LineReturn lineOut = await RunLines(whileLines.ToArray(), data, id, check, lr, vars.ToArray());
                                    //log all the vars
                                    for(int j = 0; j < lineOut.vars.Length; j++){ vars = MergeVars(lineOut.vars[j], vars); }
                                    
                                    lr.line = lineOut.line;
                                    if(lineOut.error){ lr.error = true;lr.errorCode = lineOut.errorCode; return lr; }
                                    else if (lineOut.canceled){ lr.canceled = true; return lr; }
                                    //check the value again
                                    args = GetCurrentArgs(lines[i], data, vars.ToArray());
                                    //Console.WriteLine($"Updated Args: {args}");
                                    if(CompareValues(args) == null){ lr.error = true; lr.errorCode = "While Statement Execution Error"; return lr;}
                                    //Program.Log(args, MessageType.Debug);
                                    b = (bool)CompareValues(args);
                                    //if we are still in the while loop, reset the line
                                    //Program.Log($"While Loop: {b}", MessageType.Debug);
                                    //await Task.Delay(10000);
                                }
                            }
                            i += whileLines.Count;
                        }
                        catch(Exception e){
                            lr.error = true; lr.errorCode = $"While Statement Exeption: {e}"; return lr;
                        }
                    }
                    else { lr.error = true; lr.errorCode = "Unknown Function"; 
                        if(func == "Message"){ lr.errorCode = "Message is Depricated, use 'Reply'"; }
                        return lr; 
                    }
                }
                catch{ lr.error = true; lr.errorCode = "ParseError"; return lr; }
                i++;
                lr.line++;
            }
            lr.vars = vars.ToArray();
            //Console.WriteLine($"are lr.vars null? {lr.vars == null}");
            return lr;
        }
        async Task SaveUserWithChange(string user, int pChange, int gpChange = 0, int ttsChange = 0, bool withMulti = false){
            //get the user
            User data = await SaveSystem.GetUser(user);
            //add the change
            if(withMulti){ pChange = Program.GetMaxMultipliedPoints(pChange, data); }
            else{
                data.points += pChange;
            }
            
            data.goldPoints += gpChange;
            data.ttsTokens += ttsChange;
            //save the user
            await SaveSystem.SaveUser(data);
        }
        int GetId(){
            //set int to a random number between 0 and 9999
            int id = new Random().Next(0, 9999);
            while(commandIds.Contains(id)){ id++; }
            return id;
        }

        //run the command or check it for parse errors
        async Task RunCommand(CustomCommandData data, ProcessData messageData)
        {
            //g
            //remove everything before the last \
            string[] lines = SaveSystem.GetPlaintextFile($"customCommands\\{data.addPath}{data.fullName}.txt");
            //check if the file exists
            if(lines == null)
            {
                //check if the command data has been tweaked
                string[] files = SaveSystem.GetAllFilesInFolder("customCommands");
                // go through the files and see if the name matches
                for(int i = 0; i < files.Length; i++)
                {
                    //remove everything before the last \
                    string name = files[i].Remove(0, files[i].LastIndexOf('\\') + 1);
                    if(name.StartsWith(data.name)){
                        await ReloadCommands();
                        //CustomCommand(messageData); return;
                        return;
                    }
                }
            }
            //Console.WriteLine($"Custom Command: {data.name}, {data.cost}, {data.instructions}");
            //check if the user has enough {config.currencies}
            if ((data.cost <= messageData.user.points) && (data.modOnly == false || messageData.message.usermod == true) && (data.streamerOnly == false || messageData.message.sender.ToLower() == Program.config.channel))
            {
                while(Program.commandManager == null){ await Task.Delay(100); }
                if(Program.commandManager.CheckMessage(messageData.message, data.name, data.cooldownSeconds, data.globalCooldown, 1)){
                    //remove the {config.currencies} from the user
                    messageData.user.points -= data.cost;
                    await SaveSystem.SaveUser(messageData.user);
                    int id = GetId(); commandIds.Add(id);
                    commandVar[] vars = null;
                    if(data.perams){
                        //Console.WriteLine($"Perams ={messageData.message.content}");
                        vars = new commandVar[]{new commandVar("perams", messageData.message.content.Remove(0, data.name.Length + 1))};
                    }
                    await RunLines(lines, messageData, id, false, null, vars);
                    if(commandIds.Contains(id)){ commandIds.Remove(id); }
                }
                //set the message
                //message.Content = data.message;
            }
            else
            { await Program.SendMessage($"@{messageData.message.sender} Not Enough {Program.config.currencies}"); }
        }
        string GetCurrentArgs(string line, ProcessData data, commandVar[] vars){
            string[] split = line.Split('<');
            string args = "";
            if(split.Length > 1){ args = split[1]; }
            if(split.Length > 2){for(int j = 2; j < split.Length; j++){ args += split[j]; }} //combine the rest of the line into the args
            
            args = parseString(args, data); //add special data to args
            //Program.Log($"Args: {args}", MessageType.Debug);
            args = GetVars(vars.ToArray(), args); //add vars to args
            //Program.Log($"Args: {args}", MessageType.Debug);
            args = parseMaths(args); //calculate any maths in args
            //Program.Log($"Args: {args}", MessageType.Debug);
            return args;
        }
        bool? CompareValues(string args){
            if(args.Contains('=')){
                string[] argsSplit = args.Split('=');
                if(argsSplit.Length != 2){return null;}
                if(argsSplit[0] == "focus"){ return ExeFocusChecker.IsExeFocused(argsSplit[1]);}
                else if(argsSplit[0] == "day"){ return IsDay(argsSplit[1]); }
                else { return argsSplit[0] == argsSplit[1]; }
            }
            else if(args.Contains('<')){
                string[] argsSplit = args.Split('<');
                if(argsSplit.Length != 2){ return null; }
                return int.Parse(argsSplit[0]) < int.Parse(argsSplit[1]);
            }
            else if(args.Contains('>')){
                string[] argsSplit = args.Split('>');
                if(argsSplit.Length != 2){ return null; }
                return int.Parse(argsSplit[0]) > int.Parse(argsSplit[1]);
            }
            else if(args.Contains('!')){
                string[] argsSplit = args.Split('!');
                if(argsSplit.Length != 2){ return null; }
                if(argsSplit[0] == "focus"){ return !ExeFocusChecker.IsExeFocused(argsSplit[1]);}
                else { return argsSplit[0] != argsSplit[1]; }
            }
            else { return null; }
        }

        bool IsDay(string day){
            switch(day.ToLower()){
                case "monday": return DateTime.Now.DayOfWeek == DayOfWeek.Monday;
                case "tuesday": return DateTime.Now.DayOfWeek == DayOfWeek.Tuesday;
                case "wednesday": return DateTime.Now.DayOfWeek == DayOfWeek.Wednesday;
                case "thursday": return DateTime.Now.DayOfWeek == DayOfWeek.Thursday;
                case "friday": return DateTime.Now.DayOfWeek == DayOfWeek.Friday;
                case "saturday": return DateTime.Now.DayOfWeek == DayOfWeek.Saturday;
                case "sunday": return DateTime.Now.DayOfWeek == DayOfWeek.Sunday;
                default: return false;
            }
        }
        
        string GetVars(commandVar[] vars, string input)
        {
            //add global vars to the vars
            commandVar[] tvars = new commandVar[vars.Length + globalVars.Count];
            for(int i = 0; i < vars.Length; i++){ tvars[i] = vars[i]; }
            for(int i = 0; i < globalVars.Count; i++){ tvars[i + vars.Length] = globalVars[i]; }

            for(int i = 0; i < tvars.Length; i++)
            {
                string getVar = $"%{tvars[i].name}%";
                input = input.Replace(getVar, tvars[i].value);
            }
            return input;
        }
        commandVar SetVarFromString(string str){
            //massage should be in the format of "name=value"
            //split the string on =
            string[] parts = str.Split('=');
            //check if there is 2 or more parts
            if (parts.Length < 2) { return null; }
            //check if the name is valid
            if (parts[0].Length < 1) { return null; }
            //check if the value is valid
            if (parts[1].Length < 1) { return null; }
            //return the var
            //Console.WriteLine("var set successfully");
            return new commandVar(parts[0], parts[1]);
        }
        commandVar SetGlobalVarFromString(string str){
            //massage should be in the format of "name=value"
            //split the string on =
            string[] parts = str.Split('=');
            //check if there is 2 or more parts
            if (parts.Length < 2) { return null; }
            //check if the name is valid
            if (parts[0].Length < 1) { return null; }
            //check if the value is valid
            if (parts[1].Length < 1) { return null; }
            //return the var
            //Console.WriteLine("var set successfully");
            commandVar newVar = new commandVar(parts[0], parts[1]);
            //check if the var already exists
            bool exists = false;
            for(int i = 0; i < globalVars.Count; i++)
            {
                if(globalVars[i].name == newVar.name)
                { globalVars[i] = newVar; exists = true; break; }
            }
            if(!exists){ globalVars.Add(newVar); }
            return newVar;
        }
        commandVar GetGlobalVar(string name)
        {
            for(int i = 0; i < globalVars.Count; i++)
            { if(globalVars[i].name == name){ return globalVars[i]; } }
            return new commandVar(name, "0");
        }
        List<commandVar> MergeVars(commandVar newVar, List<commandVar> vars)
        {
            //check if a var with the same name exists
            for(int i = 0; i < vars.Count; i++)
            {
                if(vars[i].name == newVar.name)
                {
                    //replace the var
                    vars[i] = newVar; return vars;
                }
            }
            //add the var
            vars.Add(newVar);
            //Console.WriteLine("vars merged successfully");
            return vars;
        }

        string parseString(string input, ProcessData messageData){
            //replace %user% with the user
            //replace %points% with the points
            //replace %displaypoints% with the points with commas
            //replace %channel% with the channel
            input = input.Replace("%user.name%", messageData.message.sender);
            input = input.Replace("%user.displaypoints%", messageData.user.points.ToString("N0"));
            input = input.Replace("%channel%", messageData.message.channel);
            input = input.Replace("%user.points%", messageData.user.points.ToString());
            for(int i = 0; i < customCommands.Count; i++){
                input = input.Replace($"%{customCommands[i].name}.displaycost%", customCommands[i].cost.ToString("N0"));
                input = input.Replace($"%{customCommands[i].name}.cost%", customCommands[i].cost.ToString());
            }
            return input;
        }
        string parseMaths(string input){
            //look for {x+x}, {x-x}, {x*x}, {x/x} where x is a number then replace it with the result
            Regex regex = new Regex(@"{(\d+)([+*/-])(\d+)}");
            MatchCollection matches = regex.Matches(input);
            foreach (Match match in matches)
            {
                try{
                    int x = int.Parse(match.Groups[1].Value);
                    int y = int.Parse(match.Groups[3].Value);
                    char op = match.Groups[2].Value[0];
                    int result = 0;
                    switch (op)
                    {
                        case '+':
                            result = x + y;
                            break;
                        case '-':
                            result = x - y;
                            break;
                        case '*':
                            result = x * y;
                            break;
                        case '/':
                            result = x / y;
                            break;
                    }
                    input = input.Replace(match.Value, result.ToString());
                }
                catch{}
                
            }
            return input;
        }
        commandVar GetRandomInRange(string input, int hash){
            //input should be in the format of "min,max" both inclusive
            //split the string on ,
            try{
                string[] parts = input.Split(',');
                //check if there is 2 or more parts
                if (parts.Length < 2) { return null; }
                //check if the min is valid
                if (parts[0].Length < 1) { return null; }
                //check if the max is valid
                if (parts[1].Length < 1) { return null; }
                //get the min and max
                int min = int.Parse(parts[0]);
                int max = int.Parse(parts[1]);
                //check if the min is greater than the max
                if(min > max){ return null; }
                //return the var
                Random r = new Random(hash);
                return new commandVar("random", r.Next(min, max + 1).ToString());
            }
            catch{return null;}
        }
        bool MouseClick(string button, bool testing = false){
            //left middle right
            button = button.ToLower();
            if(button == "left") { if(!testing){ input.Mouse.LeftButtonClick(); } return true; }
            else if(button == "middle") { if(!testing){ input.Mouse.MiddleButtonClick(); }  return true; }
            else if(button == "right") { if(!testing){ input.Mouse.RightButtonClick(); } return true; }
            else { return false; }
        }
        //function to set the position of the mouse
        bool MouseMove(string coords, bool testing = false){
            //get the pixel size of the screen
            int width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            
            //does coords contain a comma
            if(!coords.Contains(',')) { return false; }
            //split the coords
            string[] parts = coords.Split(',');
            //check if there are 2 parts
            if(parts.Length != 2) { return false; }
            //check if the x and y are valid
            if(!float.TryParse(parts[0], out float xVal)) { return false; }
            if(!float.TryParse(parts[1], out float yVal)) { return false; }

            int X = (int)(xVal * 65535.0f);
            int Y = (int)(yVal * 65535.0f);
            //set the position of the mouse
            if (!testing){ input.Mouse.MoveMouseTo(X, Y); Program.Log($"Screen Size: {width}, {height}"); }
            return true;
        }
        //function to send a global keystroke
        bool SendGlobalKey(string key, bool testing = false){
            try{
                //get the key as a char
                char keyChar = GetKeyChar(key);
                if(keyChar == ' ') { return false; }
                //send the key
                if(!testing){ GlobalKeysend keysend = new GlobalKeysend(); keysend.SendGlobalKey(keyChar); }
                return true;
            }
            catch { return false; }
        }
        //send key to another application by name
        VirtualKeyCode GetKey(string key)
        {
            key = key.ToLower();
            if (key.StartsWith("numpad1")) { return VirtualKeyCode.NUMPAD1; }
            else if (key.StartsWith("numpad2")) { return VirtualKeyCode.NUMPAD2; }
            else if (key.StartsWith("numpad3")) { return VirtualKeyCode.NUMPAD3; }
            else if (key.StartsWith("numpad4")) { return VirtualKeyCode.NUMPAD4; }
            else if (key.StartsWith("numpad5")) { return VirtualKeyCode.NUMPAD5; }
            else if (key.StartsWith("numpad6")) { return VirtualKeyCode.NUMPAD6; }
            else if (key.StartsWith("numpad7")) { return VirtualKeyCode.NUMPAD7; }
            else if (key.StartsWith("numpad8")) { return VirtualKeyCode.NUMPAD8; }
            else if (key.StartsWith("numpad9")) { return VirtualKeyCode.NUMPAD9; }
            else if (key.StartsWith("numpad0")) { return VirtualKeyCode.NUMPAD0; }
            else if (key.StartsWith("shift")) { return VirtualKeyCode.SHIFT; }
            else if (key.StartsWith("ctrl")) { return VirtualKeyCode.CONTROL; }
            else if (key.StartsWith("alt")) { return VirtualKeyCode.MENU; }
            else if (key.StartsWith("space")) { return VirtualKeyCode.SPACE; }
            else if (key.StartsWith("tab")) { return VirtualKeyCode.TAB; }
            else if (key.StartsWith("enter")) { return VirtualKeyCode.RETURN; }
            else if (key.StartsWith("backspace")) { return VirtualKeyCode.BACK; }
            else if (key.StartsWith("esc")) { return VirtualKeyCode.ESCAPE; }
            else if (key.StartsWith("delete")) { return VirtualKeyCode.DELETE; }
            else if (key.StartsWith("leftarrow")) { return VirtualKeyCode.LEFT; }
            else if (key.StartsWith("rightarrow")) { return VirtualKeyCode.RIGHT; }
            else if (key.StartsWith("uparrow")) { return VirtualKeyCode.UP; }
            else if (key.StartsWith("downarrow")) { return VirtualKeyCode.DOWN; }
            else if (key.StartsWith("lshift")) { return VirtualKeyCode.LSHIFT; }
            else if (key.StartsWith("rshift")) { return VirtualKeyCode.RSHIFT; }
            else if (key.StartsWith("lctrl")) { return VirtualKeyCode.LCONTROL; }
            else if (key.StartsWith("rctrl")) { return VirtualKeyCode.RCONTROL; }
            else if (key.StartsWith("a")){ return VirtualKeyCode.VK_A; }
            else if (key.StartsWith("b")) { return VirtualKeyCode.VK_B; }
            else if (key.StartsWith("c")) { return VirtualKeyCode.VK_C; }
            else if (key.StartsWith("d")) { return VirtualKeyCode.VK_D; }
            else if (key.StartsWith("e")) { return VirtualKeyCode.VK_E; }
            else if (key.StartsWith("f")) { return VirtualKeyCode.VK_F; }
            else if (key.StartsWith("g")) { return VirtualKeyCode.VK_G; }
            else if (key.StartsWith("h")) { return VirtualKeyCode.VK_H; }
            else if (key.StartsWith("i")) { return VirtualKeyCode.VK_I; }
            else if (key.StartsWith("j")) { return VirtualKeyCode.VK_J; }
            else if (key.StartsWith("k")) { return VirtualKeyCode.VK_K; }
            else if (key.StartsWith("l")) { return VirtualKeyCode.VK_L; }
            else if (key.StartsWith("m")) { return VirtualKeyCode.VK_M; }
            else if (key.StartsWith("n")) { return VirtualKeyCode.VK_N; }
            else if (key.StartsWith("o")) { return VirtualKeyCode.VK_O; }
            else if (key.StartsWith("p")) { return VirtualKeyCode.VK_P; }
            else if (key.StartsWith("q")) { return VirtualKeyCode.VK_Q; }
            else if (key.StartsWith("r")) { return VirtualKeyCode.VK_R; }
            else if (key.StartsWith("s")) { return VirtualKeyCode.VK_S; }
            else if (key.StartsWith("t")) { return VirtualKeyCode.VK_T; }
            else if (key.StartsWith("u")) { return VirtualKeyCode.VK_U; }
            else if (key.StartsWith("v")) { return VirtualKeyCode.VK_V; }
            else if (key.StartsWith("w")) { return VirtualKeyCode.VK_W; }
            else if (key.StartsWith("x")) { return VirtualKeyCode.VK_X; }
            else if (key.StartsWith("y")) { return VirtualKeyCode.VK_Y; }
            else if (key.StartsWith("z")) { return VirtualKeyCode.VK_Z; }
            else if (key.StartsWith("0")) { return VirtualKeyCode.VK_0; }
            else if (key.StartsWith("1")) { return VirtualKeyCode.VK_1; }
            else if (key.StartsWith("2")) { return VirtualKeyCode.VK_2; }
            else if (key.StartsWith("3")) { return VirtualKeyCode.VK_3; }
            else if (key.StartsWith("4")) { return VirtualKeyCode.VK_4; }
            else if (key.StartsWith("5")) { return VirtualKeyCode.VK_5; }
            else if (key.StartsWith("6")) { return VirtualKeyCode.VK_6; }
            else if (key.StartsWith("7")) { return VirtualKeyCode.VK_7; }
            else if (key.StartsWith("8")) { return VirtualKeyCode.VK_8; }
            else if (key.StartsWith("9")) { return VirtualKeyCode.VK_9; }
            
            else return VirtualKeyCode.NONAME;
        }
        char GetKeyChar(string key){
            key = key.ToLower();
            if (key.StartsWith("numpad1")) { return (char)Keys.NumPad1; }
            else if (key.StartsWith("numpad2")) { return (char)Keys.NumPad2; }
            else if (key.StartsWith("numpad3")) { return (char)Keys.NumPad3; }
            else if (key.StartsWith("numpad4")) { return (char)Keys.NumPad4; }
            else if (key.StartsWith("numpad5")) { return (char)Keys.NumPad5; }
            else if (key.StartsWith("numpad6")) { return (char)Keys.NumPad6; }
            else if (key.StartsWith("numpad7")) { return (char)Keys.NumPad7; }
            else if (key.StartsWith("numpad8")) { return (char)Keys.NumPad8; }
            else if (key.StartsWith("numpad9")) { return (char)Keys.NumPad9; }
            else if (key.StartsWith("numpad0")) { return (char)Keys.NumPad0; }
            else if (key.StartsWith("tab")) { return (char)Keys.Tab; }
            else if (key.StartsWith("enter")) { return (char)Keys.Enter; }
            else if (key.StartsWith("backspace")) { return (char)Keys.Back; }
            else if (key.StartsWith("esc")) { return (char)Keys.Escape; }
            else if (key.StartsWith("delete")) { return (char)Keys.Delete; }
            else if (key.StartsWith("leftarrow")) { return (char)Keys.Left; }
            else if (key.StartsWith("rightarrow")) { return (char)Keys.Right; }
            else if (key.StartsWith("uparrow")) { return (char)Keys.Up; }
            else if (key.StartsWith("downarrow")) { return (char)Keys.Down; }
            else if (key.StartsWith("lshift")) { return (char)Keys.LShiftKey; }
            else if (key.StartsWith("rshift")) { return (char)Keys.RShiftKey; }
            else if (key.StartsWith("lctrl")) { return (char)Keys.LControlKey; }
            else if (key.StartsWith("rctrl")) { return (char)Keys.RControlKey; }
            else if (key.StartsWith("a")) { return 'a'; }
            else if (key.StartsWith("b")) { return 'b'; }
            else if (key.StartsWith("c")) { return 'c'; }
            else if (key.StartsWith("d")) { return 'd'; }
            else if (key.StartsWith("e")) { return 'e'; }
            else if (key.StartsWith("f")) { return 'f'; }
            else if (key.StartsWith("g")) { return 'g'; }
            else if (key.StartsWith("h")) { return 'h'; }
            else if (key.StartsWith("i")) { return 'i'; }
            else if (key.StartsWith("j")) { return 'j'; }
            else if (key.StartsWith("k")) { return 'k'; }
            else if (key.StartsWith("l")) { return 'l'; }
            else if (key.StartsWith("m")) { return 'm'; }
            else if (key.StartsWith("n")) { return 'n'; }
            else if (key.StartsWith("o")) { return 'o'; }
            else if (key.StartsWith("p")) { return 'p'; }
            else if (key.StartsWith("q")) { return 'q'; }
            else if (key.StartsWith("r")) { return 'r'; }
            else if (key.StartsWith("s")) { return 's'; }
            else if (key.StartsWith("t")) { return 't'; }
            else if (key.StartsWith("u")) { return 'u'; }
            else if (key.StartsWith("v")) { return 'v'; }
            else if (key.StartsWith("w")) { return 'w'; }
            else if (key.StartsWith("x")) { return 'x'; }
            else if (key.StartsWith("y")) { return 'y'; }
            else if (key.StartsWith("z")) { return 'z'; }
            else if (key.StartsWith("0")) { return '0'; }
            else if (key.StartsWith("1")) { return '1'; }
            else if (key.StartsWith("2")) { return '2'; }
            else if (key.StartsWith("3")) { return '3'; }
            else if (key.StartsWith("4")) { return '4'; }
            else if (key.StartsWith("5")) { return '5'; }
            else if (key.StartsWith("6")) { return '6'; }
            else if (key.StartsWith("7")) { return '7'; }
            else if (key.StartsWith("8")) { return '8'; }
            else if (key.StartsWith("9")) { return '9'; }
            else return ' ';
        }
        bool PlaySound(string sound, bool isLong = false, bool check = false){
            //check if the sound file exists
            if(!System.IO.File.Exists(sound)) { return false; }
            //check if the sound is a .wav or .mp3 file
            if(!sound.EndsWith(".wav") && !sound.EndsWith(".mp3")) { return false; }
            if(check){return true;}
            //play the sound using naudio
            if(longSound != null && isLong) { longSound.Stop(); longSound = null; }
            AudioFileReader reader = new AudioFileReader(sound);
            WaveOutEvent output = new WaveOutEvent();
            output.Init(reader);
            output.Play();
            if (isLong) { longSound = output; }
            else{ sounds.Add(output); }
            return true;
        }

        public void StopAllSounds(){
            //stop all sounds
            //Console.WriteLine("Stopping all sounds");
            //stop all sounds
            for (int i = 0; i < sounds.Count; i++) { sounds[i].Stop(); }
            sounds.Clear();
            if(longSound != null) longSound.Stop(); longSound = null;
        }
        public string GetCustomHelp(){
            //get the custom help message
            string help = "";
            for (int i = 0; i < customCommands.Count; i++)
            {
                help += $"{customCommands[i].name}<{customCommands[i].description}";
                if (customCommands[i].cost > 0) help += $" | {customCommands[i].cost.ToString("N0")} {Program.config.currencies}"; 
                help += "\n";
            }
            return help;
        }
    }
}
class CustomCommandData
{
    public string name, fullName, description, addPath;
    public int cost, bitsCost, cooldownSeconds, whisperType;
    public bool modOnly, auto, streamerOnly, globalCooldown, perams;
    public CustomCommandData(string name, string description, int cost = 0)
    {
        this.name = name;
        this.fullName = name;
        this.description = description;
        this.cost = cost;
        this.modOnly = false;
        this.streamerOnly = false;
        this.globalCooldown = false;
        this.auto = false;
        this.cooldownSeconds = 0;
        this.perams = false;
        this.whisperType = 1;
        addPath = "";
    }
}
class commandVar
{
    public string name, value;
    public commandVar(string name, string value)
    {
        this.name = name;
        this.value = value;
    }
}
class LineReturn{
    public int line;
    public bool error, canceled;
    public string errorCode = "UnknownError";
    public commandVar[] vars;
}