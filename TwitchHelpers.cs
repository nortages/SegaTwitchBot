﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Math.EC.Rfc7748;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Core.Models.Undocumented.RecentMessages;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Entitlements.GetCodeStatus;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.ThirdParty.UsernameChange;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace NortagesTwitchBot
{
    public static class TwitchHelpers
    {
        private static readonly TwitchAPI twitchAPI = new TwitchAPI();

        static TwitchHelpers()
        {
            // TwitchAPI
            twitchAPI.Settings.ClientId = Config.ClientID;
            twitchAPI.Settings.AccessToken = Config.BotToken;
            twitchAPI.Settings.Secret = "Twitch"; // Need to not hard code this
        }

        public static bool IsSubscribeToChannel(string broadcasterId, string userId, string accessToken = null)
        {
            if (accessToken == null) accessToken = twitchAPI.Settings.AccessToken;
            var url = @"https://api.twitch.tv/helix/subscriptions";

            var webClient = new WebClient();
            webClient.QueryString.Add("broadcaster_id", broadcasterId);
            webClient.QueryString.Add("user_id", userId);
            webClient.Headers.Add("Authorization", $"Bearer {accessToken}");
            webClient.Headers.Add("Client-Id", Config.ClientID);

            string result;
            try
            {
                result = webClient.DownloadString(url);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Response);
                return false;
            }
            dynamic parsedData = JObject.Parse(result);
            JArray jArray = parsedData.data;
            return jArray.Count != 0;
        }

        public static void SendMessageWithDelay(this TwitchClient client, JoinedChannel channel, string message, TimeSpan delay)
        {
            Task.Delay(delay).ContinueWith(t => client.SendMessage(channel, message));
        }

        public static void SendMessageWithDelay(this TwitchClient client, string channel, string message, TimeSpan delay)
        {
            Task.Delay(delay).ContinueWith(t => client.SendMessage(channel, message));
        }

        public static bool GetOnlineStatus(string channelId)
        {
            return twitchAPI.V5.Streams.BroadcasterOnlineAsync(channelId).Result;
        }

        public static Subscription[] GetSubscribers(string channelId)
        {
            return twitchAPI.Helix.Subscriptions.GetBroadcasterSubscriptions(channelId, Config.ClientID).Result.Data;
        }

        public static TimeSpan? GetUpTime()
        {
            string userId = GetUserId(Config.ChannelName);

            if (userId == null || string.IsNullOrEmpty(userId))
                return null;
            return twitchAPI.V5.Streams.GetUptimeAsync(userId).Result;
        }

        public static string GetUserId(string userName)
        {
            List<string> list = new List<string>() { userName };
            User[] users = twitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0].Id;
        }

        public static User GetUser(string userName)
        {
            if (userName == string.Empty)
                return null;

            List<string> list = new List<string>() { userName };
            User[] users = twitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0];
        }

        public static User[] GetUsersAsync(List<string> userNames)
        {
            if (userNames.Count == 0)
                return null;

            User[] users = twitchAPI.Helix.Users.GetUsersAsync(null, userNames).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users;
        }

        public static Channel GetChannel(string userName)
        {
            string userId = GetUserId(userName);

            if (!string.IsNullOrEmpty(userId))
            {
                Channel channel = twitchAPI.V5.Channels.GetChannelByIDAsync(userId).Result;
                if (channel != null)
                    return channel;
            }

            return null;
        }

        public static User[] GetChanneSubscribers(string userName)
        {
            string userId = GetUserId(userName);

            if (!string.IsNullOrEmpty(userId))
            {
                var subs = twitchAPI.Helix.Subscriptions.GetBroadcasterSubscriptions(userId).Result;
                var userNames = new List<string>();
                foreach (var sub in subs.Data)
                {
                    userNames.Add(sub.UserName);
                }
                return GetUsersAsync(userNames);
            }

            return null;
        }

        public static List<UsernameChangeListing> GetUsernameChangesAsync(string userName)
        {
            return twitchAPI.ThirdParty.UsernameChange.GetUsernameChangesAsync(userName).Result;
        }

        public static List<ChatterFormatted> GetChatters(string channelName)
        {
            return twitchAPI.Undocumented.GetChattersAsync(channelName).Result;
        }
    }
}