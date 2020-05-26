using Newtonsoft.Json;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins {
    [Info("Rust Notifications Core", "Stephan E.G. Veenstra", "0.0.3")]
    [Description("Send notifications to the Rust Notifications App")]
    class RustNotificationsCore : CovalencePlugin {
        private const string DEFAULT_KEY_MESSAGE = "replace this with your key";
        private RustNotificationsCoreConfig config;

        private void Init() {
            config = Config.ReadObject<RustNotificationsCoreConfig>();
            Puts("Rust Notifications Core Plugin Initialized");
        }

        protected override void LoadDefaultConfig() {
            var defaultConfig = new RustNotificationsCoreConfig();
            Config.WriteObject(defaultConfig, true);
        }

        [Command("notify"), Permission("notifications.send")]
        private void NotifyCommand(IPlayer player, string command, string[] args) {
            if (args.Count() == 0) {
                player.Reply(
                    "To notify all players that subscribed to the server use:\n" +
                    " /notify all <(i)nfo|(w)arning|(a)lert|(s)uccess> <title> <message>\n" +
                    "To notify specific players use:\n" +
                    " /notify <(i)nfo|(w)arning|(a)lert|(s)uccess> <title> <message> <playerId> ...");
                return;
            }

            if (args.Count() == 4 && args[0] == "all") {
                SendAll(args[1], args[2], args[3]);
                return;
            }

            Send(args[0], args[1], args[2], args.Skip(3).ToArray());
        }

        private void SendAll(string type, string title, string message) {
            Send(type, title, message, null);
        }

        private void Send(string type, string title, string message, string[] userIds) {
            Dictionary<string, string> headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };

            if (config.ApiKey == DEFAULT_KEY_MESSAGE) {
                Puts("Server Token missing! Request a token and add it to the configuration.");
                return;
            }

            var notification = new Notification() {
                Title = title,
                Message = message,
                Type = getType(type),
                Players = userIds
            };

            webrequest.Enqueue("https://us-central1-rust-notifications-prd.cloudfunctions.net/api/notification?key=" + config.ApiKey, JsonConvert.SerializeObject(notification), (code, response) => {
                Puts(response);
            }, this, RequestMethod.POST, headers);
        }

        private string getType(string input) {
            switch (input) {
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

        private class RustNotificationsCoreConfig {
            [JsonProperty(PropertyName = "key")]
            public string ApiKey { get; set; } = DEFAULT_KEY_MESSAGE;
        }

        public class Notification {
            [JsonProperty(PropertyName = "type")]
            public String Type { get; set; }

            [JsonProperty(PropertyName = "title")]
            public String Title { get; set; }

            [JsonProperty(PropertyName = "message")]
            public String Message { get; set; }

            [JsonProperty(PropertyName = "players")]
            public String[] Players { get; set; }
        }
    }
}
