using System;
using NAudio.Wave;
using NAudio.Vorbis;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace TwitchBot
{
    public class Utility
    {
        int CounterValue;
        string CounterName;
        public Utility()
        {
            CounterValue = 0;
            CounterName = "Counter";
            UpdateCounterFile(true);
        }
        public ProcessData SetName(ProcessData data)
        {
            //remove the command from the message
            data.message.content = data.message.content.Replace("setcounter ", "");
            CounterName = data.message.content;
            CounterValue = 0;
            data.returnMessage = $"{CounterName}: {CounterValue}";
            UpdateCounterFile();
            return data;
        }
        //change the counter value
        public ProcessData ChangeCounter(ProcessData data)
        {
            //command will either be counter- or counter+
            //remove the command from the message
            data.message.content = data.message.content.Replace(CounterName, "");
            bool edit = false;
            for(int i = 0; i < data.message.content.Length; i++)
            {
                if(data.message.content[i] == '+') { CounterValue++; edit = true; }
                else if(data.message.content[i] == '-') { CounterValue--; edit = true; }
            }
            data.returnMessage = $"{CounterName}: {CounterValue}";
            if(edit){
                UpdateCounterFile();
            }
            return data;
        }
        public void ClearCounter()
        { UpdateCounterFile(true); }
        async Task UpdateCounterFile(bool clear = false){
            string content = clear ? "" : $"{CounterName}: {CounterValue}";
            SaveSystem.CreatePlaintextFile("counter.txt", content);

        }
        //get the counter value
        public ProcessData GetCounter(ProcessData data)
        {
            data.returnMessage = $"{CounterName}: {CounterValue}";
            return data;
        }
        //open the config folder
        public void OpenConfigFolder()
        {
            //open the config folder
            // AppData\\Roaming\\ReplayStudios\\TwitchBot
            Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ReplayStudios\\BeanBot");
        }
        public void OpenConfig()
        {
            //open the config file
            // AppData\\Roaming\\ReplayStudios\\TwitchBot
            Process.Start("notepad.exe", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ReplayStudios\\BeanBot\\config.dat");
            //Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ReplayStudios\\BeanBot\\config.dat");
        }
        public void PlayAudioFromUrl(string url, float volume = 0.7f)
        {
            //check if there is a space in the url
            if(url.Contains(" "))
            {
                //if there is a space, split the url into a list of strings
                string[] urlList = url.Split(' ');
                url = urlList[0];
                volume = float.Parse(urlList[1]);
            }

            using (var webClient = new WebClient())
            {
                try
                {
                    // Download the audio file from the web URL
                    byte[] audioData = webClient.DownloadData(url);
                    // Create a WaveStream from the downloaded audio data
                    WaveStream waveStream;
                    if (url.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                        waveStream = new WaveFileReader(new System.IO.MemoryStream(audioData));
                    else if (url.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                        waveStream = new Mp3FileReader(new System.IO.MemoryStream(audioData));
                    else if (url.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                        //SUPPORT FOR OGG
                        using (var vorbisStream = new VorbisWaveReader(new System.IO.MemoryStream(audioData)))
                        {
                            // Convert Vorbis-compressed audio to PCM
                            waveStream = new WaveChannel32(vorbisStream);
                        }
                    else
                    {
                        Program.Log("SURL Unsupported audio format", MessageType.Error);
                        return;
                    }
                    // Adjust the volume
                    var volumeProvider = new VolumeWaveProvider16(waveStream);
                    volumeProvider.Volume = volume;

                    // Create a WaveOutEvent instance to play the audio
                    using (var waveOut = new WaveOutEvent())
                    {
                        waveOut.Init(volumeProvider);
                        waveOut.Play();

                        // Wait for the audio to finish playing
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            // You can add optional delay or perform other tasks here
                        }
                    }
                    // Dispose the WaveStream
                    waveStream.Dispose();
                }
                catch (Exception ex){
                    Program.Log("SURL error occurred: " + ex.Message, MessageType.Error); // Handle any exceptions that occurred during the process
                }
            }
        }
        public async Task UpdateBot(string svVersion){
            //compare versions to see if we are on a dev version
            //string will be v1.1.1
            //remove the v and the .
            string version = svVersion.Replace("v", "").Replace(".", "");
            string lVersion = Program.version.Replace("v", "").Replace(".", "");
            if(int.Parse(version) > int.Parse(lVersion)){
                //if the server version is greater than the local version, update
                Program.Log($"Server version ({svVersion}) is greater than local version ({Program.version}), updating", MessageType.Warning);
                //download the new version
                //go up a directory an check if there is an exe called BeanBotInstaller.exe
                string path = System.IO.Directory.GetCurrentDirectory();
                path = path.Substring(0, path.LastIndexOf('\\'));
                path += "\\BeanBotInstaller.exe";
                //Console.WriteLine(path);
                if(System.IO.File.Exists(path)){
                    FileSuper ss = new FileSuper("BeanBot","ReplayStudios");
                    Save s = await ss.LoadFile("UPDATE");
                    if(s != null){
                        //we failed to update
                        Program.Log("Still Out Of Date After Update, If This Continues Please Alet The Devs", MessageType.Error);
                        SaveSystem.DeleteFile("UPDATE");
                        return;
                    }
                    await ss.SaveFile("UPDATE", new Save());
                    Program.Log("BeanBotInstaller.exe found, auto updating", MessageType.Success);
                    await Task.Delay(3000);
                    //if the file exists, run it
                    Process p = new Process();
                    p.StartInfo.FileName = path;
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.WorkingDirectory = $"{path.Substring(0, path.LastIndexOf('\\'))}\\";
                    p.StartInfo.CreateNoWindow = false;
                    p.Start();
                    //close the current bot
                    Environment.Exit(0);
                }
                else{
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("");
                    Console.WriteLine("BeanBotInstaller.exe not found");
                    Console.WriteLine("Please Locate The Installer Or Download It From itch");
                    Console.WriteLine("");
                }
            }
            else{
                Program.Log("", MessageType.Success);
                Program.Log($"You are using Dev version {Program.version} (Server: {svVersion})", MessageType.Success);
                Program.Log("", MessageType.Success);
            }
        }
    }
    static class ToggleConsoleQuickEdit {
        const uint ENABLE_QUICK_EDIT = 0x0040;
        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        internal static bool NoEdit() {

            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode)) {
                // ERROR: Unable to get console mode.
                return false;
            }
            // Clear the quick edit bit in the mode flags
            consoleMode &= ~ENABLE_QUICK_EDIT;
            // set the new mode
            if (!SetConsoleMode(consoleHandle, consoleMode)) {
                // ERROR: Unable to set console mode
                return false;
            }
            return true;
        }
        internal static bool EnableEdit() {

            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode)) {
                // ERROR: Unable to get console mode.
                return false;
            }
            // Clear the quick edit bit in the mode flags
            consoleMode |= ENABLE_QUICK_EDIT;
            // set the new mode
            if (!SetConsoleMode(consoleHandle, consoleMode)) {
                // ERROR: Unable to set console mode
                return false;
            }
            return true;
        }
    }
    static class ExeFocusChecker
    {
        public static bool IsExeFocused(string exeName)
        {
            // Get the current active process
            IntPtr handle = GetForegroundWindow();
            int processId;
            GetWindowThreadProcessId(handle, out processId);

            // Get the process associated with the active window
            Process activeProcess = Process.GetProcessById(processId);

            // Check if the process executable name matches the provided exe name
            Program.Log($"Active process: {activeProcess.ProcessName}, Checking if {exeName} is focused");
            return activeProcess.ProcessName.ToLower().StartsWith(exeName.ToLower());
        }
        public static void ListAllApplications()
        {
            Process[] processlist = Process.GetProcesses();
            //sort by name
            Array.Sort(processlist, delegate(Process x, Process y)
            {
                return x.ProcessName.CompareTo(y.ProcessName);
            });

            Console.BackgroundColor = ConsoleColor.Black;
            List<string> names = new List<string>();
            foreach (Process theprocess in processlist)
            {
                if (!names.Contains(theprocess.ProcessName))
                {
                    names.Add(theprocess.ProcessName);
                    Program.Log($"Process: {theprocess.ProcessName}");
                }
            }
        }

        // Import Windows API functions
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);
    }
}