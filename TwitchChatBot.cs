using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Auth.OAuth2;

using VkNet;
using VkNet.Model;

using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Support.Extensions;
using MailKit.Net.Imap;
using MailKit;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NortagesTwitchBot
{
    public static class TwitchChatBot
    {
        public static TwitchClient client;
        public static SheetsService sheetsService;
        static readonly TwitchPubSub pubsub = new TwitchPubSub();
        static readonly JoinedChannel joinedChannel = new JoinedChannel(TwitchInfo.ChannelName);
        static readonly HttpClient HTTPClient = new HttpClient();
        static readonly VkApi vkApi = new VkApi();
        static readonly Random rand = new Random();

        static ChromeDriver driver;

        static int massGifts = 0;
        const int TIMEOUTTIME = 10;
        static (bool flag, int num) timeoutUserBelowData = (false, 0);
        static (bool isHitBySnowball, string userName) hitBySnowballData = (false, null);
        static readonly HashSet<string> usersWithShield = new HashSet<string>();

        static readonly RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        static readonly Regex regex_trimEndFromQuyaBot = new Regex(@"\[\d\]", regexOptions);
        static readonly Regex regex_botsPlusToChat = new Regex(@".*?Боты?,? \+ в ча[тй].*", regexOptions);
        static readonly Regex regex_hiToBot = new Regex(@".+?NortagesBot.+?(Привет|Здравствуй|Даров|kupaSubHype|kupaPrivet|KonCha|VoHiYo|PrideToucan|HeyGuys|basilaHi|Q{1,2}).*", regexOptions);
        static readonly Regex regex_botCheck = new Regex(@"@NortagesBot (Жив|Живой|Тут|Здесь)\?", regexOptions);
        static readonly Regex regex_botLox = new Regex(@"@NortagesBot (kupaLox|лох)", regexOptions);
        static readonly Regex regex_botWorryStick = new Regex(@"@NortagesBot( worryStick)+", regexOptions);

        static readonly Dictionary<string, string> GTAcodes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("gta_codes.json"));

        public static void Connect()
        {
            TwitchClientInitialize();
            PubSubInitialize();

            //VKApiInitialize();
            //JSONBinInitialize();
            GoogleSheetsServiceInitialize();

            if (false && Environment.GetEnvironmentVariable("DEPLOYED") != null)
            {
                NavigateToModersPanel(); 
            }
        }
    
        #region Initialization

        private static void JSONBinInitialize()
        {
            HTTPClient.DefaultRequestHeaders.Add("secret-key", TwitchInfo.JsonBinSecret);
            HTTPClient.DefaultRequestHeaders.Add("versioning", "false");
        }

        private static void VKApiInitialize()
        {
            vkApi.Authorize(new ApiAuthParams
            {
                AccessToken = "43a54afd43a54afd43a54afd0043d79f00443a543a54afd1d5f2479d149db02ebfef170"
            });
        }

        private static void GoogleSheetsServiceInitialize()
        {
            GoogleCredential credential;
            string[] scopes = { SheetsService.Scope.Spreadsheets };
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
            sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "ApplicationName",
            });
        }

        private static void PubSubInitialize()
        {
            pubsub.OnPubSubServiceConnected += PubSub_OnPubSubServiceConnected;
            pubsub.OnListenResponse += PubSub_OnListenResponse;
            pubsub.OnRewardRedeemed += PubSub_OnRewardRedeemed;
            pubsub.OnStreamUp += PubSub_OnStreamUp;
            pubsub.OnPubSubServiceClosed += Pubsub_OnPubSubServiceClosed;
            pubsub.OnPubSubServiceError += Pubsub_OnPubSubServiceError;

            pubsub.ListenToRewards(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));
            pubsub.ListenToVideoPlayback(joinedChannel.Channel);
            pubsub.Connect();
        }

        private static void Pubsub_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            Console.WriteLine("[PUBSUB_ERROR] " + e.Exception.Message);
        }

        private static void Pubsub_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            Console.WriteLine("[PUBSUB_CLOSED]");
        }

        private static void TwitchClientInitialize()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1,
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
        }

        #endregion Initialization

        #region TWITCH CLIENT SUBSCRIBERS

        private static async void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (new string[] { "song", "music", "песня", "музыка" }.Contains(e.Command.CommandText))
            {
                Commands.SongCommand(e);
            }
            else if (e.Command.CommandText == "промокод")
            {
                client.SendMessage(joinedChannel, "kira - лучший промокод на mycsgoo.net TakeNRG");
            }
            else if (new string[] { "bans", "баны" }.Contains(e.Command.CommandText))
            {
                //Commands.BansCommand(e);
            }
            else if (e.Command.CommandText == "снежок" &&
                     e.Command.ArgumentsAsString.TrimStart('@') == TwitchInfo.BotUsername)
            {
                Console.WriteLine("Someone just throw a snowball to the bot!");
                hitBySnowballData.isHitBySnowball = true;
                hitBySnowballData.userName = e.Command.ChatMessage.DisplayName;
            }
            #region Currently not used
            else if (new string[] { "mmr", "ммр" }.Contains(e.Command.CommandText))
            {
                Commands.MmrCommand(e);
            }
            else if (new string[] { "hof", "залславы" }.Contains(e.Command.CommandText))
            {
                Commands.HallOfFameCommand(e);
            }
            else if (new string[] { "startvoting", "начатьголосование" }.Contains(e.Command.CommandText) && (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
            {
                Commands.StartVotingCommand(e);
            }
            else if (new string[] { "stopvoting", "закончитьголосование" }.Contains(e.Command.CommandText) && (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
            {
                await Commands.StopVotingCommand(e);
            }
            else if (new string[] { "showresult", "показать результат" }.Contains(e.Command.CommandText) && (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
            {
                await Commands.ShowResult(e);
            }
            else if (e.Command.CommandText == "ban")
            {
                var userToBan = e.Command.ArgumentsAsString.TrimStart('@');
                client.SendMessage(joinedChannel, $"Пользователь {userToBan} был забанен.");
            }
            #endregion Currently not used
        }

        private static void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (timeoutUserBelowData.flag && !e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
            {
                if (usersWithShield.Contains(e.ChatMessage.DisplayName))
                {
                    Console.WriteLine($"{e.ChatMessage.DisplayName} lose a shield!");
                    client.SendMessage(joinedChannel, $"@{e.ChatMessage.DisplayName}, Твой бабл лопнул CurseLit Теперь будь осторожнее Keepo");
                    usersWithShield.Remove(e.ChatMessage.DisplayName);
                    return;
                }
                client.TimeoutUser(joinedChannel, e.ChatMessage.DisplayName, TimeSpan.FromMinutes(TIMEOUTTIME * timeoutUserBelowData.num));
                timeoutUserBelowData.flag = false;
                timeoutUserBelowData.num = 0;
                Console.WriteLine($"{e.ChatMessage.DisplayName} is banned on {TIMEOUTTIME} minutes!");
                return;
            }

            // Regexes
            if (regex_botsPlusToChat.IsMatch(e.ChatMessage.Message))
            {
                client.SendMessage(joinedChannel, "+");
            }
            else if (regex_hiToBot.IsMatch(e.ChatMessage.Message))
            {
                client.SendMessage(joinedChannel, $"{e.ChatMessage.DisplayName} Приветствую MrDestructoid");
            }
            else if (regex_botCheck.IsMatch(e.ChatMessage.Message))
            {
                var answer = regex_botCheck.Match(e.ChatMessage.Message).Groups[1].Value;
                client.SendMessage(joinedChannel, $"{e.ChatMessage.DisplayName} {answer}.");
            }
            else if (regex_botLox.IsMatch(e.ChatMessage.Message))
            {
                client.SendMessage(joinedChannel, $"{e.ChatMessage.DisplayName} сам {regex_botLox.Match(e.ChatMessage.Message).Groups[1].Value}");
            }
            else if (regex_botWorryStick.IsMatch(e.ChatMessage.Message))
            {
                client.SendMessage(joinedChannel, $"{e.ChatMessage.DisplayName} KEKWait");
            }
            else if (GTAcodes.ContainsKey(e.ChatMessage.Message.ToUpper()))
            {
                client.SendMessage(joinedChannel, string.Format(GTAcodes[e.ChatMessage.Message.ToUpper()], e.ChatMessage.Username));
            }
            else if (hitBySnowballData.isHitBySnowball && e.ChatMessage.DisplayName == "QuyaBot")
            {
                hitBySnowballData.isHitBySnowball = false;
                var message = e.ChatMessage.Message;
                message = regex_trimEndFromQuyaBot.Replace(message, "");
                var snowballSender = hitBySnowballData.userName;
                var commandCooldown = TimeSpan.FromSeconds(15);
                var messageCooldown = TimeSpan.FromSeconds(5);
                var botUsername = TwitchInfo.BotUsername.ToLower();
                if (message == $"Снежок прилетает прямо в {botUsername}, а {snowballSender}, задорно хохоча, скрывается с места преступления!")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " та за шо?(", messageCooldown);
                }
                else if (message == $"Снежок, запущенный {snowballSender} по невероятной траектории, попадает по жо... попадает ниже спины {botUsername}.")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " ах ты... ну, погоди! Kappa", messageCooldown);
                    client.SendMessageWithDelay(e.ChatMessage.Channel, $"!снежок @{snowballSender}", commandCooldown);
                }
                else if (message == $"{snowballSender} хватает камень и кидает его в {botUsername}. Ты вообще адекватен? Так делать нельзя!")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " ай! Вообще-то, очень больно было BibleThump", messageCooldown);
                }
                else if (message == $"{snowballSender} коварно подкрадывается со снежком к {botUsername} и засовывет пригорошню снега прямо за шиворот! Такой подлости никто не ждал!")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " Твою ж... Холодно-то как... Ну, ладно! Ща тоже снега попробуешь KappaClaus", messageCooldown);
                    client.SendMessageWithDelay(e.ChatMessage.Channel, $"!снежок @{snowballSender}", commandCooldown);
                }
                else if (message == $"{snowballSender} кидается с кулаками на {botUsername}. Кажется ему никто не объяснил правил!")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " ты чего дерёшься?? SMOrc", messageCooldown);
                }
                else if (message == $"Видимо {snowballSender} имеет небольшое косоглазие, потому что не попадает снежком в {botUsername}!")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " ха, мазила! PepeLaugh", messageCooldown);
                }
                else if (message == $"{snowballSender} метко попадает снежком в лицо {botUsername}. Ну что, вкусный снег в этом году?")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " *Пфу-пфу* Микросхемы мне корпус, ты что творишь??", messageCooldown);
                    client.SendMessageWithDelay(e.ChatMessage.Channel, $"!снежок @{snowballSender}", commandCooldown);
                }
                else if (message == $"{snowballSender} пытается кинуть снежок, но неклюже поскальзывается и падает прямо в сугроб. Видимо, сегодня неудачный день!")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " KEKW", messageCooldown);
                }
                else if (message == $"{snowballSender} кидает снежок, но {botUsername} мастерстки ловит его на лету и кидает в обратную сторону! Нет, ну вы это видели?")
                {
                    client.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " не в этот раз EZY", messageCooldown);
                }
            }
        }

        private static void SendMessageWithDelay(this TwitchClient client, string channel, string message, TimeSpan delay)
        {
            Task.Delay(delay).ContinueWith(t => client.SendMessage(channel, message));
        }

        private static void Client_OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
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

        private static void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
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

        private static void Client_OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            client.SendMessage(joinedChannel, $"{e.ReSubscriber.DisplayName}, спасибо за продление подписки! Poooound");
        }

        private static void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            client.SendMessage(joinedChannel, $"{e.Subscriber.DisplayName}, спасибо за подписку! bleedPurple Давайте сюда Ваш паспорт FBCatch kupaPasport");
        }


        private static void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
            // TODO: Once in a while save logs to a file
        }

        private static void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
        }

        private static void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private static void Client_OnError(object sender, OnErrorEventArgs e)
        {
            Console.WriteLine($"ERROR: {e.Exception.Message}\n{e.Exception.StackTrace}");
        }

        private static void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine(e.Error);
        }

        private static void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Disconnect();
        }

        #endregion TWITCH CLIENT SUBSCRIBERS

        #region PUBSUB SUBSCRIBERS

        private static void PubSub_OnStreamUp(object sender, OnStreamUpArgs e)
        {
            client.SendMessage(joinedChannel, "Привет всем и хорошего стрима! peepoLove");
            Console.WriteLine("The stream just has started");
        }

        private static void PubSub_OnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
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
                    timeoutUserBelowData.flag = true;
                    timeoutUserBelowData.num++;
                }
                else if (false && e.RewardTitle.Contains(rewardTestName))
                {
                    Console.WriteLine($"{e.DisplayName} took a shield!");
                    usersWithShield.Add(e.DisplayName);
                }
            }            
        }

        private static void PubSub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            // SendTopics accepts an oauth optionally, which is necessary for some topics
            Console.WriteLine("PubSub Service is Connected");

            pubsub.SendTopics(TwitchInfo.BotToken);
        }

        private static void PubSub_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
                Console.WriteLine($"Successfully verified listening to topic: {e.Topic}");
            else
                Console.WriteLine($"Failed to listen! Error: {e.Response.Error}");
        }

        #endregion PUBSUB SUBSCRIBERS

        private static void FindAnonymousGifter()
        {
            const string spreadsheetId_Anon = "1IoXknFKw-f_FmMrxB9-ni5wgSg5JFCJSt0Gq06m1KEM";

            var chatters = TwitchHelpers.GetChatters(joinedChannel.Channel).Select(n => n.Username);
                        
            var response = sheetsService.Spreadsheets.Values.Get(spreadsheetId_Anon, "A:A").Execute();
            var old_records = response.Values;
            var old_viewers = old_records != null ? old_records.Select(n => n.First().ToString()).ToList() : new List<string>();

            var upd_values = new List<IList<object>>();
            var viewers_intersection = old_viewers.Count != 0 ? chatters.Intersect(old_viewers) : chatters;
            foreach (var viewer in viewers_intersection)
            {
                upd_values.Add(new List<object> { viewer });
            }

            ValueRange body = new ValueRange { Values = upd_values };            
            sheetsService.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId_Anon, "A:A").Execute();
            var upd_request = sheetsService.Spreadsheets.Values.Update(body, spreadsheetId_Anon, "A1");
            upd_request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            upd_request.Execute();
        }

        private static Task<IList<string>> GetViewers(int wait_seconds = 8)
        {
            // Gets the chrome driver and navigate to the stream's chat page.
            ChromeDriver driver;
            var chrome_options = new ChromeOptions();
            if (Environment.GetEnvironmentVariable("DEPLOYED") != null)
            {
                Console.WriteLine("USING GOOGLE_CHROME_SHIM");
                chrome_options.BinaryLocation = Environment.GetEnvironmentVariable("GOOGLE_CHROME_SHIM");
            }

            driver = new ChromeDriver(chrome_options);

            driver.Navigate().GoToUrl("https://www.twitch.tv/k_i_ra/chat");
            //driver.Navigate().GoToUrl("https://www.twitch.tv/dinablin/chat");

            // Finds the element that shows users in chat and click on it.
            driver.FindElement(By.XPath("//button[@aria-label='Users in Chat']"), 5).Click();

            // Finds the scrollable div with chat users and scrolls through them till the end.
            var by = By.XPath("//div[@data-test-selector='scrollable-area-wrapper']/div[@class='simplebar-scroll-content chat-viewers__scroll-container']");
            var scroll_div = driver.FindElement(by, 5);

            //var newWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            //var scroll_div = newWait.Until(ExpectedConditions.ElementToBeClickable(by));
            //scroll_div.Click();

            //var prevHeight = 0;
            //while (prevHeight != scroll_div.Size.Height)
            //{
            //    prevHeight = scroll_div.Size.Height;
            //    scroll_div.SendKeys(Keys.End);
            //    Thread.Sleep(TimeSpan.FromSeconds(2));
            //}

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

            Thread.Sleep(TimeSpan.FromSeconds(wait_seconds));

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var selector = By.CssSelector("p[class='tw-capcase']");
            var viewers = new List<IWebElement>();

            // Adds moderators
            var path1 = "//div[@aria-labelledby='chat-viewers-list-header-Moderators']";
            var moderators_block = wait2.Until(ExpectedConditions.ElementIsVisible(By.XPath(path1)));
            var moders = moderators_block.FindElements(selector);
            viewers.AddRange(moders);

            try
            {
                // Adds VIPs if they are.
                var path2 = "//div[@aria-labelledby='chat-viewers-list-header-VIPs']";
                var vips_block = driver.FindElement(By.XPath(path2));
                var vips = vips_block.FindElements(selector);
                viewers.AddRange(vips);
            }
            catch (NoSuchElementException) { }

            // Adds others.
            var path3 = "//div[@aria-labelledby='chat-viewers-list-header-Users']";
            var users_block = driver.FindElement(By.XPath(path3));
            var users = users_block.FindElements(selector);
            viewers.AddRange(users);

            // Quits the driver and return viewers.
            IList<string> viewers_nicks = viewers.Select(n => n.Text).ToList();
            driver.Quit();
            return Task.FromResult(viewers_nicks);
        }

        private static void NavigateToModersPanel()
        {
            var chrome_options = new ChromeOptions();
            if (Environment.GetEnvironmentVariable("DEPLOYED") != null)
            {
                chrome_options.BinaryLocation = Environment.GetEnvironmentVariable("GOOGLE_CHROME_SHIM");
            }
            driver = new ChromeDriver(chrome_options);
            Commands.driver = driver;
            driver.Navigate().GoToUrl("https://www.twitch.tv/moderator/k_i_ra");

            SignInToTwitch(driver);
        }

        private static void SignInToTwitch(ChromeDriver driver)
        {
            var loginField = driver.FindElement(By.Id("login-username"), 5);
            loginField.SendKeys(TwitchInfo.BotUsername);

            var passwordField = driver.FindElement(By.Id("password-input"));
            passwordField.SendKeys(TwitchInfo.BotPassword);

            var loginButton = driver.FindElement(By.XPath("//button[@data-a-target='passport-login-button']"));
            loginButton.Click();

            Thread.Sleep(TimeSpan.FromSeconds(2));

            var header = driver.FindElement(By.XPath("//h4[@data-test-selector='auth-shell-header-header']"));

            if (header == null || header.Text != "Verify login code") Console.WriteLine("FCKNG CAPTCHA");

            var inputs = driver.FindElements(By.XPath("//div[@data-a-target='passport-modal']//input"));

            Thread.Sleep(TimeSpan.FromSeconds(3));
            string code = GetVerificationCode(driver);
            Console.WriteLine("Verification code: " + code);

            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].SendKeys(code[i].ToString());
            }
        }

        private static string GetVerificationCode(ChromeDriver driver)
        {
            using var imapClient = new ImapClient();

            var mailServer = "imap.gmail.com";
            int port = 993;
            imapClient.Connect(mailServer, port);

            // Note: since we don't have an OAuth2 token, disable
            // the XOAUTH2 authentication mechanism.
            imapClient.AuthenticationMechanisms.Remove("XOAUTH2");

            var login = TwitchInfo.GmailEmail;
            var password = TwitchInfo.GmailPassword;
            imapClient.Authenticate(login, password);

            var inbox = imapClient.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            var results = inbox.Search(SearchOptions.All, SearchQuery.HasGMailLabel("twitch-verification-codes"));
            var firstResultId = results.UniqueIds.LastOrDefault();
            var message = inbox.GetMessage(firstResultId);
            Console.WriteLine("Message date: " + message.Date);
            var path = "./verification_code.html";
            File.WriteAllText(path, message.HtmlBody);

            driver.OpenNewTab();
            // Opens the html-file with code.
            string formedUrl = "file:///" + Directory.GetCurrentDirectory() + path.TrimStart('.');
            Console.WriteLine("Formed Url: " + formedUrl);
            driver.Navigate().GoToUrl(formedUrl);
            // Gets that code.
            var xpath = By.XPath("/html/body/table/tbody/tr/td/center/table[2]/tbody/tr/td/table[5]/tbody/tr/th/table/tbody/tr/th/div/p");
            var code = driver.FindElement(xpath, 5).Text;
            // Closes the current tab.
            driver.CloseCurrentTab();

            imapClient.Disconnect(true);
            return code;
        }

        private static void Disconnect()
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