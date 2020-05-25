using System;
using System.Collections.Generic;

using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.V5.Models.Channels;

namespace SegaTwitchBot
{
    public static class TwitchHelpers
    {
        private static readonly TwitchAPI twitchAPI = new TwitchAPI();

        static TwitchHelpers()
        {
            // TwitchAPI
            twitchAPI.Settings.ClientId = TwitchInfo.ClientID;
            twitchAPI.Settings.AccessToken = TwitchInfo.BotToken;
            twitchAPI.Settings.Secret = "Twitch"; // Need to not hard code this
        }

        public static TimeSpan? GetUpTime()
        {
            string userId = GetUserId(TwitchInfo.ChannelName);

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

        public static Channel GetChannel(string userName)
        {
            string userId = GetUserId(userName);

            if (!string.IsNullOrEmpty(userId))
            {
                Channel channel = twitchAPI.V5.Channels.GetChannelByIDAsync(GetUserId(userName)).Result;
                if (channel != null)
                    return channel;
            }

            return null;
        }
    }
}