using Octokit;
using System;
using System.Threading.Tasks;

namespace TwitchBot
{
    class GitHubConnector
    {
        public async Task UploadHelp(string fullhelp){
            string owner = "MasterAirscrachDev";
            string repo = "BeanBotFullCommands";
            string filePath = $"{Program.config.channel} Commands.md";
            string title = $"{Program.config.channel} Commands";

            string content = $"# {title}\n{fullhelp}";
            string commitMessage = $"Setting {Program.config.channel} commands at {DateTime.Now}";
            if(Program.serverData == null){
                await Task.Delay(10000);
                if(Program.serverData == null){ return;}
            }
            var client = new GitHubClient(new ProductHeaderValue("BeanBotHelpManager"))
            { Credentials = new Credentials(Program.serverData.GetString("GitHubToken")) };
            // Check if the file exists in the repository
            try {
                var existingFile = await client.Repository.Content.GetAllContents(owner, repo, filePath);
                var existingContent = existingFile[0].Content;

                // If the content is the same, return without doing anything (becuase we change the time this does nothing TODO: FIX)
                if (existingContent == content) {
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
            catch (NotFoundException) {
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
