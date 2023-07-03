///FileSuperSystem V1.8
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

public class Save
{
    public List<int> Ints = new List<int>();
    public List<bool> Bools = new List<bool>();
    public List<float> Floats = new List<float>();
    public List<string> Strings = new List<string>(), StringNames = new List<string>(), IntNames = new List<string>(), BoolNames = new List<string>(), FloatNames = new List<string>();
    ///Set an int using the name
    public void SetInt(string name, int value)
    {
        if (IntNames.Contains(name)) Ints[IntNames.IndexOf(name)] = value;
        else { IntNames.Add(name); Ints.Add(value); }
    }
    ///Set a bool using the name
    public void SetBool(string name, bool value)
    {
        if (BoolNames.Contains(name)) Bools[BoolNames.IndexOf(name)] = value;
        else { BoolNames.Add(name); Bools.Add(value); }
    }
    ///Set a float using the name
    public void SetFloat(string name, float value)
    {
        if (FloatNames.Contains(name)) Floats[FloatNames.IndexOf(name)] = value;
        else { FloatNames.Add(name); Floats.Add(value); }
    }
    ///Set a string using the name
    public void SetString(string name, string value)
    {
        if (StringNames.Contains(name)) Strings[StringNames.IndexOf(name)] = value;
        else { StringNames.Add(name); Strings.Add(value); }
    }
    ///Get an int using the name, if its not found return null
    public int? GetInt(string name)
    {
        if (IntNames.Contains(name)) return Ints[IntNames.IndexOf(name)];
        else return null;
    }
    ///Get a bool using the name, if its not found return null
    public bool? GetBool(string name)
    {
        if (BoolNames.Contains(name)) return Bools[BoolNames.IndexOf(name)];
        else return null;
    }
    ///Get a float using the name, if its not found return null
    public float? GetFloat(string name)
    {
        if (FloatNames.Contains(name)) return Floats[FloatNames.IndexOf(name)];
        else return null;
    }
    ///Get a string using the name, if its not found return null
    public string GetString(string name)
    {
        if (StringNames.Contains(name)) return Strings[StringNames.IndexOf(name)];
        else return null;
    }
}
public class FileSuper
{
    public bool working, debug, useEncryption;
    public string project, studio, encryptKey, fullpath;
    public FileSuper(string project, string studio, bool debug = false)
    {
        this.project = project;
        this.studio = studio;
        this.debug = debug;
        fullpath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{studio}\\{project}\\";
    }
    public void SetEncryption(bool enabled, string key = null)
    {
        useEncryption = enabled;
        encryptKey = key;
    }
    //This Handles the encryption
    string EncDecProcess(string jsonIn, string key)
    {
        string xorstring = "", input = jsonIn, enckey = key;
        for (int i = 0; i < input.Length; i++)
        {
            xorstring += (char)(input[i] ^ enckey[i % enckey.Length]);
        }
        return xorstring;
    }
    public async Task<bool> SaveFile(string file, Save save)
    {
        working = true;
        //the file may be prefixed with a path, so we need to check for that
        string path = fullpath;
        if (file.Contains("\\"))
        {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (debug)
        {
            //UnityEngine.Debug.Log($"Saving To {path}");
            Console.WriteLine($"Saving To {path}");
        }
        //check if the path exists, if not create it
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        //convert the save to serialised data
        string data = "";
        //why use json?
        if (save.StringNames.Count > 0)
        {
            //line should look like this
            //dasr.{stringname}="{stringvalue}"
            for (int i = 0; i < save.StringNames.Count; i++)
            { data += $"dasr.{save.StringNames[i]}=\"{save.Strings[i]}\"\n"; }
        }
        if (save.IntNames.Count > 0)
        {
            //line should look like this
            //dain.{intname}={intvalue}
            for (int i = 0; i < save.IntNames.Count; i++)
            { data += $"dain.{save.IntNames[i]}={save.Ints[i]}\n"; }
        }
        if (save.BoolNames.Count > 0)
        {
            //line should look like this
            //dabo.{boolname}={boolvalue}
            for (int i = 0; i < save.BoolNames.Count; i++)
            { data += $"dabo.{save.BoolNames[i]}={save.Bools[i]}\n"; }
        }
        if (save.FloatNames.Count > 0)
        {
            //line should look like this
            //dafl.{floatname}={floatvalue}
            for (int i = 0; i < save.FloatNames.Count; i++)
            { data += $"dafl.{save.FloatNames[i]}={save.Floats[i]}\n"; }
        }
        //encrypt the data if needed
        if (useEncryption)
        {
            if (encryptKey == null)
            {
                string newkey = ""; //uses an automatic encryption key
                for (int i = 0; i < file.Length; i++)
                {
                    try { newkey += project[i]; } catch { }
                    try { newkey += studio[i]; } catch { }
                }
                data = EncDecProcess(data, newkey);
            }
            else
            { data = EncDecProcess(data, encryptKey); }
        }
        //write the data to the file asynchronously
        using (StreamWriter outputFile = new StreamWriter(path + file))
        { await outputFile.WriteAsync(data); }
        if (debug)
        {
            //UnityEngine.Debug.Log($"Saved {file} to {path}");
            Console.WriteLine($"Saved {file} to {path}");
        }
        working = false;
        return true;
    }
    public string GetSaveAsRaw(string FileName, Save save)
    {
        working = true;
        //convert the save to serialised data
        string data = "";
        //why use json?
        if (save.StringNames.Count > 0)
        {
            //line should look like this
            //dasr.{stringname}="{stringvalue}"
            for (int i = 0; i < save.StringNames.Count; i++)
            { data += $"dasr.{save.StringNames[i]}=\"{save.Strings[i]}\"\n"; }
        }
        if (save.IntNames.Count > 0)
        {
            //line should look like this
            //dain.{intname}={intvalue}
            for (int i = 0; i < save.IntNames.Count; i++)
            { data += $"dain.{save.IntNames[i]}={save.Ints[i]}\n"; }
        }
        if (save.BoolNames.Count > 0)
        {
            //line should look like this
            //dabo.{boolname}={boolvalue}
            for (int i = 0; i < save.BoolNames.Count; i++)
            { data += $"dabo.{save.BoolNames[i]}={save.Bools[i]}\n"; }
        }
        if (save.FloatNames.Count > 0)
        {
            //line should look like this
            //dafl.{floatname}={floatvalue}
            for (int i = 0; i < save.FloatNames.Count; i++)
            { data += $"dafl.{save.FloatNames[i]}={save.Floats[i]}\n"; }
        }
        //encrypt the data if needed
        if (useEncryption)
        {
            if (encryptKey == null)
            {
                string newkey = ""; //uses an automatic encryption key
                for (int i = 0; i < FileName.Length; i++)
                {
                    try { newkey += project[i]; } catch { }
                    try { newkey += studio[i]; } catch { }
                }
                data = EncDecProcess(data, newkey);
            }
            else
            { data = EncDecProcess(data, encryptKey); }
        }
        working = false;
        return data;
    }

    public async Task<Save> LoadFile(string file)
    {
        working = true;
        //the file may be prefixed with a path, so we need to check for that
        string path = fullpath;
        if (file.Contains("\\"))
        {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        //check if the file exists, if it doesn't return null
        if (!File.Exists(path + file))
        {
            //if (debug) Debug.Log($"File {file} not found in {path}");
            working = false;
            return null;
        }
        if (debug)
        {
            //UnityEngine.Debug.Log($"Loading {file} from {path}");
            Console.WriteLine($"Loading {file} from {path}");
        }
        //read the file asynchronously
        //string jsonIn = await File.ReadAllTextAsync(path + file);
        string jsonIn = FrameworkReadAllText(path + file);
        //Console.WriteLine($"jsonIn: {jsonIn}");

        if (debug)
        {
            //UnityEngine.Debug.Log($"Content: {jsonIn}");
            Console.WriteLine($"Content: {jsonIn}");
        }
        //decrypt the data if needed
        if (useEncryption)
        {
            if (encryptKey == null)
            {
                string newkey = ""; //uses an automatic encryption key
                for (int i = 0; i < file.Length; i++)
                {
                    try { newkey += project[i]; } catch { }
                    try { newkey += studio[i]; } catch { }
                }
                jsonIn = EncDecProcess(jsonIn, newkey);
            }
            else
            { jsonIn = EncDecProcess(jsonIn, encryptKey); }
        }
        //Console.WriteLine($"jsonIn: {jsonIn}");
        if (debug)
        {
            //UnityEngine.Debug.Log($"{file} Decrypted");
            Console.WriteLine($"{file} Decrypted");
        }
        //convert the data to a save
        //the data is split into sections, so we need to split it up
        //check the content of the first line
        string[] lines = jsonIn.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (debug)
        {
            //UnityEngine.Debug.Log($"Parsing[{lines.Length}]");
            Console.WriteLine($"Parsing[{lines.Length}]");
        }
        Save save = new Save();
        for (int i = 0; i < lines.Length; i++)
        {
            try
            {
                if (lines[i].StartsWith("dasr."))
                {
                    string[] line = lines[i].Split('=');
                    string name = line[0].Substring(5);
                    string value = line[1].Substring(1, line[1].Length - 2);
                    save.SetString(name, value);
                }
                else if (lines[i].StartsWith("dain."))
                {
                    string[] line = lines[i].Split('=');
                    string name = line[0].Substring(5);
                    int value = int.Parse(line[1]);
                    save.SetInt(name, value);
                }
                else if (lines[i].StartsWith("dabo."))
                {
                    string[] line = lines[i].Split('=');
                    string name = line[0].Substring(5);
                    bool value = bool.Parse(line[1]);
                    save.SetBool(name, value);
                }
                else if (lines[i].StartsWith("dafl."))
                {
                    string[] line = lines[i].Split('=');
                    string name = line[0].Substring(5);
                    float value = float.Parse(line[1]);
                    save.SetFloat(name, value);
                }
            }
            catch
            {
                if (debug)
                {
                    //UnityEngine.Debug.Log($"ParseError on line: {i}");
                    Console.WriteLine($"ParseError on line: {i}");
                }
            }

        }
        working = false;
        if (debug)
        {
            //UnityEngine.Debug.Log($"Loaded {file}");
            Console.WriteLine($"Loaded {file}");
        }
        return save;
    }
    public Save LoadSaveFromRaw(string content, string FileName)
    {
        working = true;
        string jsonIn = content;

        if (debug)
        {
            //UnityEngine.Debug.Log($"Content: {jsonIn}");
            Console.WriteLine($"Content: {jsonIn}");
        }
        //decrypt the data if needed
        if (useEncryption)
        {
            if (encryptKey == null)
            {
                string newkey = ""; //uses an automatic encryption key
                for (int i = 0; i < FileName.Length; i++)
                {
                    try { newkey += project[i]; } catch { }
                    try { newkey += studio[i]; } catch { }
                }
                jsonIn = EncDecProcess(jsonIn, newkey);
            }
            else
            { jsonIn = EncDecProcess(jsonIn, encryptKey); }
        }
        if (debug)
        {
            //UnityEngine.Debug.Log($"{file} Decrypted");
            Console.WriteLine($"{FileName} Decrypted");
        }
        //convert the data to a save
        //the data is split into sections, so we need to split it up
        //check the content of the first line
        string[] lines = jsonIn.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (debug)
        {
            //UnityEngine.Debug.Log($"Parsing[{lines.Length}]");
            Console.WriteLine($"Parsing[{lines.Length}]");
        }
        Save save = new Save();
        for (int i = 0; i < lines.Length; i++)
        {
            try
            {
                if (lines[i].StartsWith("dasr."))
                {
                    string[] line = lines[i].Split('=');
                    string name = line[0].Substring(5);
                    string value = line[1].Substring(1, line[1].Length - 2);
                    save.SetString(name, value);
                }
                else if (lines[i].StartsWith("dain."))
                {
                    string[] line = lines[i].Split('=');
                    string name = line[0].Substring(5);
                    int value = int.Parse(line[1]);
                    save.SetInt(name, value);
                }
                else if (lines[i].StartsWith("dabo."))
                {
                    string[] line = lines[i].Split('=');
                    string name = line[0].Substring(5);
                    bool value = bool.Parse(line[1]);
                    save.SetBool(name, value);
                }
                else if (lines[i].StartsWith("dafl."))
                {
                    string[] line = lines[i].Split('=');
                    string name = line[0].Substring(5);
                    float value = float.Parse(line[1]);
                    save.SetFloat(name, value);
                }
            }
            catch
            {
                if (debug)
                {
                    //UnityEngine.Debug.Log($"ParseError on line: {i}");
                    Console.WriteLine($"ParseError on line: {i}");
                }
            }

        }
        working = false;
        if (debug)
        {
            //UnityEngine.Debug.Log($"Content Parsed");
            Console.WriteLine($"Content Parsed");
        }
        return save;
    }
    public async Task SaveText(string file, string text, bool forcePath = false)
    {
        string path = "";
        if (!forcePath) path = fullpath;
        if (file.Contains("\\"))
        {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        //encrypt the data if needed
        if (useEncryption)
        {
            if (encryptKey == null)
            {
                string newkey = ""; //uses an automatic encryption key
                for (int i = 0; i < file.Length; i++)
                {
                    try { newkey += project[i]; } catch { }
                    try { newkey += studio[i]; } catch { }
                }
                text = EncDecProcess(text, newkey);
            }
            else
            { text = EncDecProcess(text, encryptKey); }
        }
        using (StreamWriter outputFile = new StreamWriter(path + file))
        { await outputFile.WriteAsync(text); }
    }
    public async Task<string[]> LoadText(string file, bool forcePath = false)
    {
        string path = "";
        if (!forcePath) path = fullpath;
        if (file.Contains("\\"))
        {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (!File.Exists(path + file)) return null;
        //string content = await File.ReadAllTextAsync(path + file);
        string content = FrameworkReadAllText(path + file);
        if (useEncryption)
        {
            if (encryptKey == null)
            {
                string newkey = ""; //uses an automatic encryption key
                for (int i = 0; i < file.Length; i++)
                {
                    try { newkey += project[i]; } catch { }
                    try { newkey += studio[i]; } catch { }
                }
                content = EncDecProcess(content, newkey);
            }
            else
            { content = EncDecProcess(content, encryptKey); }
        }
        return content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
    }
    public string[] GetFilesInFolder(string folder, bool forcePath = false)
    {
        string path = "";
        if (!forcePath) path = fullpath;
        if (folder.Contains("\\"))
        {
            path += folder.Substring(0, folder.LastIndexOf("\\") + 1);
            folder = folder.Substring(folder.LastIndexOf("\\") + 1);
        }
        //UnityEngine.Debug.Log(path + folder);
        Console.WriteLine(path + folder);
        if (!Directory.Exists(path + folder)) return null;
        return Directory.GetFiles(path + folder);
    }
    string FrameworkReadAllText(string filePath)
    {
        string fileContents = string.Empty;
        try
        {
            // Open the file using a stream reader.
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                // Read the contents of the file into a string variable.
                fileContents = streamReader.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur while attempting to read the file.
            Console.WriteLine("An error occurred: " + ex.Message);
        }
        return fileContents;
    }
}