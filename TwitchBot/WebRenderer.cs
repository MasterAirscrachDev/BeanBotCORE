using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace TwitchBot
{
    public class WebRenderer
    {
        int timeOut = 5000;
        List<HTMLememnt> css = new List<HTMLememnt>();
        List<HTMLememnt> blocks = new List<HTMLememnt>();
        void AddElement(string name, string content, bool style = false){
            //add or update element
            if(style){
                for(int i = 0; i < css.Count; i++){
                    if(css[i].name == name){
                        css[i].content = content;
                        return;
                    }
                }
                css.Add(new HTMLememnt{name = name, content = content});
            }else{
                for(int i = 0; i < blocks.Count; i++){
                    if(blocks[i].name == name){
                        blocks[i].content = content;
                        return;
                    }
                }
                blocks.Add(new HTMLememnt{name = name, content = content});
            }
        }
        void RemoveElement(string name, bool style = false){
            //remove element
            if(style){
                for(int i = 0; i < css.Count; i++){
                    if(css[i].name == name){
                        css.RemoveAt(i);
                        return;
                    }
                }
            }else{
                for(int i = 0; i < blocks.Count; i++){
                    if(blocks[i].name == name){
                        blocks.RemoveAt(i);
                        return;
                    }
                }
            }
        }
        
        public async Task StartRenderer()
        {
            // Specify the URL where your site will be hosted (e.g., http://localhost:8080)
            string url = "http://localhost:8080/";

            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine($"Listening on {url}");

                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    string filename = "index.html"; // Replace with the path to your HTML file
                    string content = File.ReadAllText(filename);

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);

                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
            }
            await Task.CompletedTask;
        }
        public void SetPrediction(Prediction prediction){
            if(prediction == null){
                RemoveElement("prediction");
                return;
            }
            if(prediction.teams.Count != 2){
                return;
            }
            string col1 = "rgb(211, 109, 213)"; //tmp
            string col2 = "rgb(75, 75, 223)"; //tmp
            int team1percent = 50; //tmp
            team1percent -= 1;
            int team2percent = team1percent + 2;
            team1percent = Math.Max(team1percent, 0);
            team2percent = Math.Min(team2percent, 100);
            AddElement(".bar-container", "position: relative;", true);
            AddElement(".bar", $@"height: 30px;
background: linear-gradient(to right, {col1} {team1percent}%, {col2} {team2percent}%);
border-radius: 5px;
margin-bottom: 10px;", true);
            AddElement(".percentage", "position: absolute;top: 50%;transform: translateY(-50%);font-weight: bold;width: 50%;text-align: center;", true);
            AddElement(".percentage.left", "left: 0;padding-left: 3%;text-align: left;", true);
            AddElement(".percentage.right", "right: 0;padding-right: 3%;text-align: right;", true);
            AddElement(".team-name", "display: flex;justify-content: space-between;font-size: 18px;margin-bottom: 10px;", true);
            //add the prediction block
            AddElement("prediction",$@"<div style=""position: absolute; height: 100px; width: 300px; top: 10%; left: 50%; transform: translate(-50%, -50%);"">
<div class=""vote-container"">
<div class=""team-name"">
    <span>Team A</span>
    <span>Team B</span>
</div>
<div class=""bar-container"">
    <div class=""bar""></div>
    <div class=""percentage left"">{team1percent + 1}%</div>
    <div class=""percentage right"">{team2percent - 1}%</div>
</div></div></div>");
            UpdateWebFile();
        }
        void UpdateWebFile(){
            string file = GetWebFile();
            File.WriteAllText("index.html", file);
        }
        string GetWebFile(){
            string start = $@"<!DOCTYPE html>
<html>
<script>setTimeout(function(){{location.reload()}},{timeOut});</script>
<head>
<meta charset={"\""}UTF-8{"\""}>
<meta name={"\""}viewport{"\""} content={"\""}width=device-width, initial-scale=1.0{"\""}>
<style>
body {{
display: flex;
justify-content: center;
align-items: center;
height: 100vh;
margin: 0;
font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
color:ghostwhite;
}}";
            for(int i = 0; i < css.Count; i++){
                start += $"{css[i].name} {{\n{css[i].content}\n}}\n";
            }
            start += @"</style>
</head>
<body>";
            for(int i = 0; i < blocks.Count; i++){
                start += $"{blocks[i].content}";
            }
            start += "</body>\n</html>";
            return start;
        }
        class HTMLememnt{
            public string name;
            public string content;
        }
    }
}
