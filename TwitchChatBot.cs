using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;

using Newtonsoft.Json;

using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Auth.OAuth2;

using VkNet;
using VkNet.Model;
using VkNet.Enums.Filters;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Support.Extensions;
using System.Threading;

namespace SegaTwitchBot
{
    class TwitchChatBot
    {
        static TwitchClient client;
        static readonly TwitchPubSub pubsub = new TwitchPubSub();
        static readonly JoinedChannel joinedChannel = new JoinedChannel(TwitchInfo.ChannelName);
        static readonly HttpClient HTTPClient = new HttpClient();
        //static readonly VkApi vk_api = new VkApi();
        //static readonly Random rand = new Random();

        static readonly string ApplicationName = "Google Sheets API .NET Quickstart";
        static SheetsService sheets_service;
        static readonly string[] scopes = { SheetsService.Scope.Spreadsheets };
        const string linkToHOF = "https://docs.google.com/spreadsheets/d/19RwGl1i79-3ZuVYyytfyvsg_wVprvozMSyooAy3HaU8";
        const string spreadsheetId_HOF = "19RwGl1i79-3ZuVYyytfyvsg_wVprvozMSyooAy3HaU8";
        const string spreadsheetId_Anon = "1IoXknFKw-f_FmMrxB9-ni5wgSg5JFCJSt0Gq06m1KEM";

        int massGifts = 0;
        const int TIMEOUTTIME = 10;
        static bool timeToPolling = false;
        static bool toTimeoutUserBelow = false;
        static Dictionary<string, int> votes;   
        static readonly HashSet<string> usersWithShield = new HashSet<string>();
        static readonly Regex regex_botsPlusToChat = new Regex(@".*?[Бб]оты?,? \+ в ча[тй].*", RegexOptions.Compiled);
        static readonly Regex regex_hiToBot = new Regex(@".+?NortagesBot.+?([Пп]ривет|[Зз]дравствуй|[Дд]аров|kupaSubHype|kupaPrivet|KonCha|VoHiYo|PrideToucan|HeyGuys|basilaHi|[Qq]{1,2}).*", RegexOptions.Compiled);

        public void Connect()
        {            
            HTTPClient.DefaultRequestHeaders.Add("secret-key", TwitchInfo.JsonBinSecret);
            HTTPClient.DefaultRequestHeaders.Add("versioning", "false");

            // TwitchClient
            ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, TwitchInfo.ChannelName);

            //client.OnLog += Client_OnLog;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnConnected += Client_OnConnected;
            client.OnError += Client_OnError;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnReSubscriber += Client_OnReSubscriber;
            client.OnGiftedSubscription += Client_OnGiftedSubscription;
            client.OnCommunitySubscription += Client_OnCommunitySubscription;
            
            client.Connect();

            // PubSub
            pubsub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            pubsub.OnListenResponse += OnListenResponse;
            pubsub.OnRewardRedeemed += OnRewardRedeemed;
            pubsub.OnStreamUp += OnStreamUp;

            pubsub.ListenToRewards(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));

            pubsub.Connect();

            GoogleCredential credential;
            if (File.Exists("credentials.json"))
            {
                // Put your credentials json file in the root of the solution and make sure copy to output dir property is set to always copy 
                using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);
                credential = GoogleCredential.FromStream(stream).CreateScoped(scopes);
            }
            else
            {
                credential = GoogleCredential.FromJson(Environment.GetEnvironmentVariable("credentials.json")).CreateScoped(scopes);
            }            

            // Create Google Sheets API service.
            sheets_service = new SheetsService(new BaseClientService.Initializer()
            {                
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // VK API

            //vk_api.Authorize(new ApiAuthParams
            //{
            //    AccessToken = "43a54afd43a54afd43a54afd0043d79f00443a543a54afd1d5f2479d149db02ebfef170"
            //});

            // Check GetViewers method
            if (GetViewers().ToList().Count > 0) Console.WriteLine("GetViewers works");
            else Console.WriteLine("GetViewers doesn't work :(");
        }

        // TWITCH CLIENT SUBSCRIBERS

        private void Client_OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
        {
            massGifts = e.GiftedSubscription.MsgParamMassGiftCount;
            if (e.GiftedSubscription.MsgParamMassGiftCount == 1)
            {
                client.SendMessage(joinedChannel, $"{e.GiftedSubscription.DisplayName}, спасибо за подарочную подписку! PrideFlower");
            }
            else
            {
                client.SendMessage(joinedChannel, $"{e.GiftedSubscription.DisplayName}, спасибо за подарочные подписки! peepoLove peepoLove peepoLove");
            }

            if (e.GiftedSubscription.IsAnonymous)
            {
                FindAnonymousGifter();
            }
        }

        private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            if (massGifts > 0)
            {
                massGifts--;
            }
            else
            {
                client.SendMessage(joinedChannel, $"{e.GiftedSubscription.DisplayName}, спасибо за подарочную подписку для {e.GiftedSubscription.MsgParamRecipientDisplayName}! peepoLove");
            }

            if (e.GiftedSubscription.IsAnonymous)
            {
                FindAnonymousGifter();
            }
        }

        private void Client_OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            client.SendMessage(joinedChannel, $"{e.ReSubscriber.DisplayName}, спасибо за обновление подписки! Poooound");
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            client.SendMessage(joinedChannel, $"{e.Subscriber.DisplayName}, спасибо за подписку! bleedPurple Давайте сюда Ваш паспорт FBCatch");
        }

        private void Client_OnError(object sender, OnErrorEventArgs e)
        {
            Console.WriteLine($"ERROR: {e.Exception.Message}\n{e.Exception.StackTrace}");
        }

        private async void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (false)
            {
                if (timeToPolling && (e.Command.CommandText == "ммр" || e.Command.CommandText == "mmr"))
                {
                    if (int.TryParse(e.Command.ArgumentsAsString, out int vote))
                    {
                        votes[e.Command.ChatMessage.DisplayName] = vote;
                        Console.WriteLine($"{e.Command.ChatMessage.DisplayName} votes for {vote}");
                    }
                }
                else if (e.Command.CommandText == "залславы")
                {
                    var winners = GetHallOfFame();
                    if (e.Command.ArgumentsAsString == "фулл")
                    {
                        var msg = "Полный список лучших ванг стрима этого месяца Pog\n";
                        client.SendMessage(joinedChannel, msg + linkToHOF);
                    }
                    else
                    {
                        var msg = "Топ-3 ванг стрима Pog\n";
                        int i = 0;
                        foreach (var winner in winners)
                        {
                            msg += $"{winner.Key} - {winner.Value}; ";
                            if (i == 3) break;
                            i++;
                        }
                        msg = msg.Remove(msg.Length - 2, 2) + ".";
                        client.SendMessage(joinedChannel, msg);
                    }
                }
                else if (e.Command.CommandText == "начатьголосование" && (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
                {
                    timeToPolling = true;
                    votes = new Dictionary<string, int>();
                    client.SendMessage(joinedChannel, "Голосование началось! TwitchVotes Пишите !ммр и свою ставку. Под конец стрима будет определён победитель, удачи! TakeNRG");
                    Console.WriteLine("Polling just started!");
                }
                else if (timeToPolling && e.Command.CommandText == "закончитьголосование" && (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
                {
                    timeToPolling = false;
                    var bin_id = "5ede5401655d87580c463af7";
                    var url = $"https://api.jsonbin.io/b/{bin_id}";
                    var content = new StringContent(
                      JsonConvert.SerializeObject(votes),
                      Encoding.UTF8,
                      "application/json"
                      );
                    var response = await HTTPClient.PutAsync(url, content);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine("Smth went wrong...");
                        Console.WriteLine(response.StatusCode);
                        Console.WriteLine(response.ReasonPhrase);
                    }

                    Console.WriteLine("Polling just closed!");
                    client.SendMessage(joinedChannel, "Голосование закончилось! FBtouchdown Ожидайте конца стрима, чтобы узнать результат TwitchLit");
                }
                else if (e.Command.CommandText == "показатьрезультат" && (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
                {
                    if (int.TryParse(e.Command.ArgumentsAsString, out int result))
                    {
                        Console.WriteLine($"The result is {result}");

                        var bin_id = "5ede5401655d87580c463af7";
                        var url = $"https://api.jsonbin.io/b/{bin_id}";

                        var response = await HTTPClient.GetAsync(url);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var votes = JsonConvert.DeserializeObject<Dictionary<string, int>>(await response.Content.ReadAsStringAsync());
                            string winner = votes.OrderBy(item => Math.Abs(result - item.Value)).First().Key;
                            client.SendMessage(joinedChannel, $"PorscheWIN Победитель - {winner}. Его ставка - {votes[winner]}. Поздравляем! EZY Clap");
                            UpdateHallOfFame(winner);
                            Console.WriteLine($"The winner is {winner}! His bet was {votes[winner]}");
                        }
                        else
                        {
                            Console.WriteLine("Smth went wrong...");
                            Console.WriteLine(response.ReasonPhrase);
                        }
                    }
                }
            }
            
            else if (new string[] { "song", "music", "песня", "музыка" }.Contains(e.Command.CommandText))
            {
                //var group = vk_api.Groups.GetByIdAsync(null, "120235040", GroupsFields.Status).Result.FirstOrDefault();
                //var result = group.StatusAudio != null ? $"{group.StatusAudio.Artist} - {group.StatusAudio.Title}" : "Сейчас у стримера в вк ничего не играет :(";
                //Console.WriteLine("Current song: " + result);
                //client.SendMessage(joinedChannel, result);
                var prefix = string.IsNullOrEmpty(e.Command.ArgumentsAsString) ? e.Command.ArgumentsAsString + ", " : "";
                client.SendMessage(joinedChannel, $"{prefix}Все песни, кроме тех, что с ютуба, транслируются у стримера в группе вк, заходи GivePLZ https://vk.com/k_i_ra_group TakeNRG");
            }
            else if (e.Command.CommandText == "промокод")
            {
                client.SendMessage(joinedChannel, "KIRA - лучший промокод на MYCSGOO.NET MrDestructoid");
            }
        }

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
            // TODO: Once in a while save logs to a file
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (toTimeoutUserBelow && !e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
            {
                if (usersWithShield.Contains(e.ChatMessage.DisplayName))
                {
                    Console.WriteLine($"{e.ChatMessage.DisplayName} lose a shield!");
                    client.SendMessage(joinedChannel, $"@{e.ChatMessage.DisplayName}, Твой бабл лопнул CurseLit Теперь будь осторожнее Keepo");
                    usersWithShield.Remove(e.ChatMessage.DisplayName);
                    return;
                }
                client.TimeoutUser(joinedChannel, e.ChatMessage.DisplayName, TimeSpan.FromMinutes(TIMEOUTTIME));                
                toTimeoutUserBelow = false;
                Console.WriteLine($"{e.ChatMessage.DisplayName} is banned on {TIMEOUTTIME} minutes!");
                return;
            }
            if (regex_botsPlusToChat.Matches(e.ChatMessage.Message).Count > 0)
            {
                client.SendMessage(joinedChannel, "+");
            }
            else if (regex_hiToBot.Matches(e.ChatMessage.Message).Count > 0)
            {
                client.SendMessage(joinedChannel, $"{e.ChatMessage.DisplayName} Приветствую MrDestructoid");
            }
            else if (e.ChatMessage.Message.Contains("selphy"))
            {
                Console.WriteLine("\nSelphy sub - " + e.ChatMessage.DisplayName + '\n');
            }
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine(e.Error);
        }

        private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Disconnect();
        }

        // PUBSUB SUBSCRIBERS

        private void OnStreamUp(object sender, OnStreamUpArgs e)
        {
            client.SendMessage(joinedChannel, "Привет всем и хорошего стрима!");
            Console.WriteLine("The stream just has started");
        }

        private static void OnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
        {
            if (e.Status == "UNFULFILLED") {
                Console.WriteLine("\nSomeone redeemed a reward!");
                Console.WriteLine($"Name: {e.DisplayName},\nStatus: {e.Status},\nTitle: {e.RewardTitle},\nMessage: {e.Message},\nPrompt: {e.RewardPrompt}\n");

                var rewardTestName = "Сдать дань"; // TODO: Change rewardTestName

                if (e.RewardTitle.Contains("Таймач самому себе"))
                {
                    client.TimeoutUser(joinedChannel, e.DisplayName, TimeSpan.FromMinutes(10));
                }
                else if (e.RewardTitle.Contains("Таймач человеку снизу"))
                {
                    toTimeoutUserBelow = true;
                }
                else if (false && e.RewardTitle.Contains(rewardTestName))
                {
                    Console.WriteLine($"{e.DisplayName} took a shield!");
                    usersWithShield.Add(e.DisplayName);
                }
            }            
        }

        private static void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            // SendTopics accepts an oauth optionally, which is necessary for some topics
            Console.WriteLine("PubSub Service is Connected");

            pubsub.SendTopics(TwitchInfo.BotToken);
        }

        private static void FindAnonymousGifter()
        {
            var viewers = GetViewers();
                        
            var response = sheets_service.Spreadsheets.Values.Get(spreadsheetId_Anon, "A:A").Execute();
            var old_records = response.Values;
            var old_viewers = old_records != null ? old_records.Select(n => n.First().ToString()).ToList() : new List<string>();

            var upd_values = new List<IList<object>>();
            var viewers_intersection = old_viewers.Count != 0 ? viewers.Intersect(old_viewers) : viewers;
            foreach (var viewer in viewers_intersection)
            {
                upd_values.Add(new List<object> { viewer });
            }

            ValueRange body = new ValueRange { Values = upd_values };            
            sheets_service.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId_Anon, "A:A").Execute();
            var upd_request = sheets_service.Spreadsheets.Values.Update(body, spreadsheetId_Anon, "A1");
            upd_request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            upd_request.Execute();
        }

        private static IList<string> GetViewers()
        {
            // Get a chrome driver and navigate to the stream's chat page.
            var driver = new ChromeDriver(".");
            driver.Navigate().GoToUrl("https://www.twitch.tv/k_i_ra/chat");

            // Find the element that shows users in chat and click on it.
            driver.FindElement(By.XPath("//button[@aria-label='Users in Chat']")).Click();
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Find the scrollable div with chat users and scrolls through them till the end.
            var scroll_div = driver.FindElement(By.XPath("//div[@data-test-selector='scrollable-area-wrapper']/div[@class='simplebar-scroll-content chat-viewers__scroll-container']"));
            driver.ExecuteJavaScript(@" var prevHeight = 0;
                                        var target = arguments[0];
                                        var theInterval = setInterval(
                                        function () {
                                            if (prevHeight != target.scrollHeight) {
                                                target.scrollTop = target.scrollHeight;
                                                prevHeight = target.scrollHeight;
                                            }
                                            else { clearInterval(theInterval); }
                                        }, 500);", scroll_div);
            // Time can be found if get the current viewers count on the stream page.
            Thread.Sleep(TimeSpan.FromSeconds(8));

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var selector = By.CssSelector("p[class='tw-capcase']");
            var viewers = new List<IWebElement>();

            // Add moderators
            var path1 = "//div[@aria-labelledby='chat-viewers-list-header-Moderators']";
            var moderators_block = wait2.Until(ExpectedConditions.ElementIsVisible(By.XPath(path1)));
            var moders = moderators_block.FindElements(selector);
            viewers.AddRange(moders);

            try
            {
                // Add VIPs if they are.
                var path2 = "//div[@aria-labelledby='chat-viewers-list-header-VIPs']";
                var vips_block = driver.FindElement(By.XPath(path2));
                var vips = vips_block.FindElements(selector);
                viewers.AddRange(vips);
            }
            catch (NoSuchElementException) { }

            // Add others.
            var path3 = "//div[@aria-labelledby='chat-viewers-list-header-Users']";
            var users_block = driver.FindElement(By.XPath(path3));
            var users = users_block.FindElements(selector);
            viewers.AddRange(users);

            // Quit the driver and return viewers.
            var viewers_nicks = viewers.Select(n => n.Text).ToList();
            driver.Quit();
            return viewers_nicks;
        }

        private static void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
                Console.WriteLine($"Successfully verified listening to topic: {e.Topic}");
            else
                Console.WriteLine($"Failed to listen! Error: {e.Response.Error}");
        }

        // CUSTOM

        Dictionary<string, int> GetHallOfFame()
        {
            string rangeToRead = "HallOfFame!A2:B";
            var request = sheets_service.Spreadsheets.Values.Get(spreadsheetId_HOF, rangeToRead);

            ValueRange response = request.Execute();
            var values = response.Values;

            if (values != null && values.Count > 0)
            {
                Dictionary<string, int> d = new Dictionary<string, int>();
                foreach (var row in values)
                {
                    d.Add(row[0].ToString(), Convert.ToInt32(row[1]));
                }
                return d;
            }
            else
            {
                Console.WriteLine("No data found.");
                return new Dictionary<string, int>();
            }
        }

        void UpdateHallOfFame(string winner)
        {
            string rangeToWrite = "HallOfFame!A2";

            var d = GetHallOfFame();

            var key_name = winner;
            if (!d.ContainsKey(key_name))
            {
                d.Add(key_name, 0);
            }
            d[key_name]++;

            var upd_values = new List<IList<object>>();

            foreach (var item in d.OrderByDescending(key => key.Value))
            {
                upd_values.Add(new List<object> { item.Key, item.Value });
            }

            ValueRange body = new ValueRange { Values = upd_values };
            var upd_request = sheets_service.Spreadsheets.Values.Update(body, spreadsheetId_HOF, rangeToWrite);
            upd_request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            upd_request.Execute();
        }

        private void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
            //client.SendMessage(joinedChannel, "The plug has been pulled. My time is up. Until next time. ResidentSleeper");

            client.OnConnectionError -= Client_OnConnectionError; // will complain of a fatal network error if not disconnected. Is this something to fix?
            client.LeaveChannel(joinedChannel.Channel);

            pubsub.Disconnect();
            //client.Disconnect();
        }
    }
}
