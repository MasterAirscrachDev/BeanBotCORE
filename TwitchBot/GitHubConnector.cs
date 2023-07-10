using Octokit;
using System;
using System.Threading.Tasks;

namespace TwitchBot
{
    class GitHubConnector
    {
        bool first = true; //this makes it so the bot doesn't spam update
        async Task<string> GetServerBotToken()
        {
            string owner = "MasterAirscrachDev";
            string repo = "BotTokens";
            string filePath = "BotToken.key";
            var client = new GitHubClient(new ProductHeaderValue("BeanBotTokenManager"));
            client.Credentials = new Credentials(SpecialDat.githubLogin); //bot token

            var fileContents = await client.Repository.Content.GetAllContents(owner, repo, filePath);

            if (fileContents.Count == 0)
            {
                //Console.WriteLine($"File not found: {filePath}");
                return null;
            }

            var fileContent = fileContents[0].Content;
            //Console.WriteLine($"File content: {fileContent}");
            return fileContent;
        }
        public async Task<string> GetAccessToken()
        {
            string file = await GetServerBotToken();
            FileSuper fileSuper = new FileSuper("BeanBot", "ReplayStudios");
            fileSuper.SetEncryption(true, SpecialDat.TokenEnc);
            Save save = fileSuper.LoadSaveFromRaw(file, "BotToken.key");
            string token = save.GetString("AccessToken");
            string version = save.GetString("Version");
            if(version != Program.version && first)
            {
                Utility u = new Utility();
                u.UpdateBot(version);
                first = false;
            }
            else if(first){
                FileSuper ss = new FileSuper("BeanBot","ReplayStudios");
                Save s = await ss.LoadFile("UPDATE");
                if(s != null){
                    //delete update file
                    SaveSystem.DeleteFile("UPDATE");
                }
                Program.Log("", MessageType.Success);
                Program.Log($"You are on Latest version {Program.version}", MessageType.Success);
                Program.Log("Thank you for using beanbot :)", MessageType.Success);
                Program.Log("", MessageType.Success);
            }
            return token;
        }
        public async Task UploadHelp(string fullhelp){
            string owner = "MasterAirscrachDev";
            string repo = "BeanBotFullCommands";
            string filePath = $"{Program.config.channel} Commands.md";
            string title = $"{Program.config.channel} Commands (Last Updated {DateTime.Now})";

            string content = $"# {title}\n{fullhelp}";
            string commitMessage = $"Adding file at {DateTime.Now}";
            var client = new GitHubClient(new Octokit.ProductHeaderValue("BeanBotHelpManager"));
            client.Credentials = new Credentials(SpecialDat.githubLogin); //bot token

            // Check if the file exists in the repository
            try
            {
                var existingFile = await client.Repository.Content.GetAllContents(owner, repo, filePath);
                var existingContent = existingFile[0].Content;

                // If the content is the same, return without doing anything (becuase we change the time this does nothing TODO: FIX)
                if (existingContent == content)
                {
                    Program.Log("File content is the same, no update needed.", MessageType.Success);
                    return;
                }

                // Update the existing file with the new content
                var updateResult = await client.Repository.Content.UpdateFile(
                    owner,
                    repo,
                    filePath,
                    new UpdateFileRequest(commitMessage, content, existingFile[0].Sha)
                );
                Program.Log("File updated: " + updateResult.Content.DownloadUrl, MessageType.Success);
            }
            catch (NotFoundException)
            {
                // File doesn't exist, create a new file
                var createResult = await client.Repository.Content.CreateFile(
                    owner,
                    repo,
                    filePath,
                    new CreateFileRequest(commitMessage, content)
                );
                Program.Log("File created: " + createResult.Content.DownloadUrl, MessageType.Success);
            }
        }
    }
}
