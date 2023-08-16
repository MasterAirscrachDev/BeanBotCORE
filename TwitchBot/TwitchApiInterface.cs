using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using AsyncAwaitBestPractices;

namespace TwitchBot
{
    public class TwitchTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public List<string> Scopes { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }
    class TwitchApiInterface
    {
        private const string RedirectUri = "http://localhost:3000";
        private static readonly HttpClient HttpClient = new HttpClient();

        GitHubConnector gitHubConnector = new GitHubConnector();

        public async Task<string> GetAccessToken(string code, bool bot = false)
        {
            var requestParams = new Dictionary<string, string>
            {
                {"client_id", SpecialDat.clientID},
                {"client_secret", SpecialDat.clientSecret},
                {"code", code},
                {"grant_type", "authorization_code"},
                {"redirect_uri", RedirectUri}
            };

            var requestContent = new FormUrlEncodedContent(requestParams);

            var response = await HttpClient.PostAsync("https://id.twitch.tv/oauth2/token", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                Program.Log($"Failed to get access token, Error: {response.ReasonPhrase}", MessageType.Error);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TwitchTokenResponse>(responseBody);
            //Console.WriteLine("Token Info");
            //Console.WriteLine($"Access Token: {tokenResponse.AccessToken}");
            //Console.WriteLine($"Refresh Token: {tokenResponse.RefreshToken}");
            //Console.WriteLine($"Expires In: {tokenResponse.ExpiresIn}");
            //Console.WriteLine($"Scopes: {string.Join(",", tokenResponse.Scopes)}");
            //Console.WriteLine($"Token Type: {tokenResponse.TokenType}");

            FileSuper fileSuper = new FileSuper("BeanBot", "ReplayStudios");
            fileSuper.SetEncryption(true, SpecialDat.TokenEnc);
            Save save = new Save();
            save.SetString("AccessToken", tokenResponse.AccessToken);
            save.SetString("RefreshToken", tokenResponse.RefreshToken);
            save.SetInt("ExpiresIn", tokenResponse.ExpiresIn);
            save.SetString("OriginTime", DateTime.Now.ToBinary().ToString());
            save.SetString("Scopes", string.Join(",", tokenResponse.Scopes));
            if(bot){ await fileSuper.SaveFile("BotToken.key", save);}
            else{await fileSuper.SaveFile("Token.key", save);}
            return tokenResponse.AccessToken;
        }
        public async Task<string> RefreshAccessToken(string refreshToken, bool bot = false)
        {
            var requestParams = new Dictionary<string, string>
            {
                {"client_id", SpecialDat.clientID},
                {"client_secret", SpecialDat.clientSecret},
                {"refresh_token", refreshToken},
                {"grant_type", "refresh_token"},
                {"redirect_uri", RedirectUri}
            };

            var requestContent = new FormUrlEncodedContent(requestParams);

            var response = await HttpClient.PostAsync("https://id.twitch.tv/oauth2/token", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                Program.Log($"Failed to refresh access token, Error: {response.ReasonPhrase}", MessageType.Error);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TwitchTokenResponse>(responseBody);
            //Console.WriteLine("Token Info");
            //Console.WriteLine($"Access Token: {tokenResponse.AccessToken}");
            //Console.WriteLine($"Refresh Token: {tokenResponse.RefreshToken}");
            //Console.WriteLine($"Expires In: {tokenResponse.ExpiresIn}");
            //Console.WriteLine($"Scopes: {string.Join(",", tokenResponse.Scopes)}");
            //Console.WriteLine($"Token Type: {tokenResponse.TokenType}");
            //save this info
            FileSuper fileSuper = new FileSuper("BeanBot", "ReplayStudios");
            fileSuper.SetEncryption(true, SpecialDat.TokenEnc);
            Save save = new Save();
            save.SetString("AccessToken", tokenResponse.AccessToken);
            save.SetString("RefreshToken", tokenResponse.RefreshToken);
            save.SetInt("ExpiresIn", tokenResponse.ExpiresIn);
            save.SetString("OriginTime", System.DateTime.Now.ToBinary().ToString());
            save.SetString("Scopes", string.Join(",", tokenResponse.Scopes));
            if(bot){ await fileSuper.SaveFile("BotToken.key", save);}
            else{await fileSuper.SaveFile("Token.key", save);}
            return tokenResponse.AccessToken;
        }

        public async Task<bool> IsAccessTokenValid(string accessToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);

            var response = await httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

            if (responseObject.status == 401)
            {
                // Invalid token
                return false;
            }

            // Valid token
            return true;
        }

        public string GetAuthCodeFromUrl(string url)
        {
            var uri = new Uri(url);
            var queryParams = uri.Query.TrimStart('?').Split('&');
            foreach (var queryParam in queryParams)
            {
                var pair = queryParam.Split('=');
                if (pair.Length == 2 && pair[0] == "code")
                {
                    string c = WebUtility.UrlDecode(pair[1]);
                    Program.Log($"Code: {c}");
                    return c;
                }
            }
            return null;
        }
        public async Task<string> GetBotToken(){
            string token = await gitHubConnector.GetAccessToken();
            if(await IsAccessTokenValid(token)){
                return token;
            }
            //Console.BackgroundColor = ConsoleColor.Red;
            //Console.WriteLine("Bot Token Is Dead, if this persists longer than 10 minutes, please contact the developer");
            return null;
        }
        public void UpdateGitHubCommandList(string commands){
            gitHubConnector.UploadHelp(commands);
        }
    }
}