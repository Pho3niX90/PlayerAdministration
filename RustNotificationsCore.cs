using Oxide.Core.Libraries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Rust Notifications Core", "Stephan E.G. Veenstra", "0.0.4")]
    [Description("Send notifications to the Rust Notifications App")]
    class RustNotificationsCore : CovalencePlugin
    {
        private const string DEFAULT_KEY_MESSAGE = "replace this with your key";
        private const string API_ROOT = "https://us-central1-rust-notifications-prd.cloudfunctions.net/api";

        private const int CODE_SUBSCRIPTION_ADDED = 0;
        private const int CODE_ALREADY_SUBSCRIBED = 1;
        private const int CODE_ALREADY_SUBSCRIBED_MUTED = 2;
        private const int CODE_NOT_USING_APP = 3;


        private RustNotificationsCoreConfig config;
        private Dictionary<string, string> headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };

        private void Init()
        {
            config = Config.ReadObject<RustNotificationsCoreConfig>();
            Puts("Rust Notifications Core Plugin Initialized");
        }

        protected override void LoadDefaultConfig()
        {
            var defaultConfig = new RustNotificationsCoreConfig();
            Config.WriteObject(defaultConfig, true);
        }

        [Command("subscribe")]
        private void SubscribeCommand(IPlayer player, string command, string[] args)
        {
            RequestSubscription(player);
        }

        [Command("notify"), Permission("notifications.send")]
        private void NotifyCommand(IPlayer player, string command, string[] args)
        {
            if (args.Count() == 0)
            {
                player.Reply(
                    "To notify all players that subscribed to the server use:\n" +
                    " /notify all <(i)nfo|(w)arning|(a)lert|(s)uccess> <title> <message>\n" +
                    "To notify specific players use:\n" +
                    " /notify <(i)nfo|(w)arning|(a)lert|(s)uccess> <title> <message> <playerId> ...");
                return;
            }

            if (args.Count() == 4 && args[0] == "all")
            {
                SendAll(args[1], args[2], args[3]);
                return;
            }

            Send(args[0], args[1], args[2], args.Skip(3).ToArray());
        }

        void OnUserConnected(IPlayer player)
        {
            RequestSubscription(player);
        }

        void RequestSubscription(IPlayer player)
        {
            webrequest.Enqueue(API_ROOT + "/subscription-request/" + player.Id + "?key=" + config.ApiKey, "", (code, response) =>
            {
                var res = JsonConvert.DeserializeObject<SubscriptionRequestResponse>(response);

                switch(res.Code)
                {
                    case CODE_NOT_USING_APP:
                        player.Reply("This server uses Rust Notifications which allows you to receive notifications in the Rust Notifications app."
                            + "\nPlease download the app if you'd like to receive notifications from this server."
                            + "\nOnce you logged in, use /subscribe to subscribe to this server.");
                        return;
                    case CODE_ALREADY_SUBSCRIBED:
                        player.Reply("You are receiving Rust Notifications from this server.");
                        return;
                    case CODE_ALREADY_SUBSCRIBED_MUTED:
                        player.Reply("You are subscribed to this server but the subscription has been muted.\nPlease unmute the subscription to receive Rust Notifications.");
                        return;
                    case CODE_SUBSCRIPTION_ADDED:
                        player.Reply("This server uses Rust Notifications.\nA subscription has been added for you in the Rust Notifications app.\nTo start receiving notifications, please unmute it.");
                        return;
                }

                Puts(response);
                player.Reply(response);
            }, this, RequestMethod.POST, headers);
        }

        private void SendAll(string type, string title, string message)
        {
            Send(type, title, message, null);
        }

        private void Send(string type, string title, string message, string[] userIds)
        {
            if (config.ApiKey == DEFAULT_KEY_MESSAGE)
            {
                Puts("Server Token missing! Request a token and add it to the configuration.");
                return;
            }

            var notification = new Notification()
            {
                Title = title,
                Message = message,
                Type = getType(type),
                Players = userIds
            };

            webrequest.Enqueue(API_ROOT + "/notification?key=" + config.ApiKey, JsonConvert.SerializeObject(notification), (code, response) =>
           {
               Puts(response);
           }, this, RequestMethod.POST, headers);
        }

        private string getType(string input)
        {
            switch (input)
            {
                case "i":
                case "info":
                    return "info";
                case "w":
                case "warning":
                    return "warning";
                case "a":
                case "alert":
                    return "alert";
                case "s":
                case "success":
                    return "success";
                default:
                    return input;
            }
        }

        private class RustNotificationsCoreConfig
        {
            [JsonProperty(PropertyName = "key")]
            public string ApiKey { get; set; } = DEFAULT_KEY_MESSAGE;
        }

        public class Notification
        {
            [JsonProperty(PropertyName = "type")]
            public String Type { get; set; }

            [JsonProperty(PropertyName = "title")]
            public String Title { get; set; }

            [JsonProperty(PropertyName = "message")]
            public String Message { get; set; }

            [JsonProperty(PropertyName = "players")]
            public String[] Players { get; set; }
        }

        private class SubscriptionRequestResponse {
            [JsonProperty(PropertyName = "code")]
            public int Code { get; set; }

            [JsonProperty(PropertyName = "message")]
            public string Message { get; set; }
        }
    }
}
