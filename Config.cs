using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace NortagesTwitchBot
{
    static class Config
    {
        static string GetJsonFromStorage(string jsonId)
        {
            string pathToConfig = "configs.json";
            string JSONbinURLKey = "JSONbinURL";
            string securityKeyKey = "securityKey";
            string apiKeyKey = "JSONstorageAPIkey";

            string apiUrl, id, securityKey, apiKey;

            if (File.Exists(pathToConfig))
            {
                using StreamReader r = new StreamReader(pathToConfig);
                string json = r.ReadToEnd();
                var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                id = items[jsonId];
                apiKey = items[apiKeyKey];
                apiUrl = items[JSONbinURLKey];
                securityKey = items[securityKeyKey];
            }
            else
            {
                id = Environment.GetEnvironmentVariable(jsonId);
                apiKey = Environment.GetEnvironmentVariable(apiKeyKey);
                apiUrl = Environment.GetEnvironmentVariable(JSONbinURLKey);
                securityKey = Environment.GetEnvironmentVariable(securityKeyKey);
            }

            var url = $"{apiUrl}{id}";

            var request = WebRequest.Create(url);
            request.Headers.Add("Api-key", apiKey);
            request.Headers.Add("Security-key", securityKey);
            var response = request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            response.Close();
            return responseString;
        }

        static string GetConfigJSON()
        {
            string configIdKey = "configId";
            return GetJsonFromStorage(configIdKey);
        }

        public static string GetGoogleCredJSON()
        {
            string configIdKey = "googleCredentialsId";
            return GetJsonFromStorage(configIdKey);
        }

        static Config()
        {
            var json = GetConfigJSON();
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            ChannelName = config["ChannelName"];
            BotUsername = config["BotUsername"];
            BotPassword = config["BotPassword"];
            GmailEmail = config["GmailEmail"];
            GmailPassword = config["GmailPassword"];
            BotToken = config["BotToken"];
            ClientID = config["ClientID"];
            RefreshToken = config["RefreshToken"];
            ChannelToken = config["ChannelToken"];
            JsonBinSecret = config["JsonBinSecret"];
        }

        public static string ChannelName { get; private set; }
        public static string BotUsername { get; private set; }
        public static string BotPassword { get; private set; }
        public static string GmailEmail { get; private set; }
        public static string GmailPassword { get; private set; }
        public static string BotToken { get; private set; }
        public static string ChannelToken { get; private set; }
        public static string ClientID { get; private set; }
        public static string RefreshToken { get; private set; }
        public static string JsonBinSecret { get; private set; }
        public static string GoogleCredentials { get; internal set; }
    }
}
