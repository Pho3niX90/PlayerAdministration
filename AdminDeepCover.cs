using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("Admin Deep Cover", "Tricky", "2.0.2")]
    [Description("Hides admin original identify by using a different steam profile")]

    public class AdminDeepCover : RustPlugin {
        #region Plugin References
        [PluginReference]
        private Plugin BetterChat;
        #endregion

        #region Classes Stored Data
        private static readonly string Perm = "admindeepcover.use";
        private static readonly string RichTextFormat = "<color={1}><size={2}>{0}</size></color>";
        Dictionary<ulong, Identify.RestoreInfo> PlayerData = new Dictionary<ulong, Identify.RestoreInfo>();

        private class Identify {
            public string Name;
            public ulong UserID;
            public class RestoreInfo : Identify {
                public string RestoreName;
                public ulong RestoreUserID;
            }
        }
        #endregion

        #region Config
        Configuration config;

        class Configuration {
            [JsonProperty("Better Chat Group")]
            public string BetterChatGroup = "default";

            [JsonProperty("Identifies", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<Identify> Identifies = new List<Identify>
            {
                new Identify
                {
                    Name = "TheFlow92 skinbet.gg",
                    UserID = 76561198353995677
                },

                new Identify
                {
                    Name = "Pr@y magicalrust.ru",
                    UserID = 76561198444436710
                },

                new Identify
                {
                    Name = "Tricky",
                    UserID = 76561198976471280
                }
            };
        }

        protected override void LoadConfig() {
            base.LoadConfig();
            try {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            } catch {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Lang
        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                ["No Permission"] = "You don't have permission to use this command!",
                ["Deep Cover Enabled"] = "Deep Cover has been enabled!",
                ["Deep Cover Disabled"] = "Deep Cover has been disabled!",
            }, this);
        }
        #endregion

        #region Oxide Hooks
        private void Init()
            => permission.RegisterPermission(Perm, this);

        private void OnServerInitialized() {
            if (BetterChat != null)
                Unsubscribe(nameof(OnPlayerChat));
            else
                Unsubscribe(nameof(OnUserChat));
        }

        private void Unload() {
            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                var identify = GetIdentify(player);
                if (identify == null)
                    return;

                Rename(player, identify.RestoreName);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
            => OnPlayerRespawned(player);

        private void OnPlayerRespawned(BasePlayer player) {
            var identify = GetIdentify(player);
            if (identify == null)
                return;

            Rename(player, identify.Name);
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info) {
            if (info == null || info.Initiator == null)
                return;

            var attacker = info.Initiator.ToPlayer();
            if (attacker == null)
                return;

            var identify = GetIdentify(attacker);
            if (identify == null)
                return;

            attacker.userID = identify.UserID;
            timer.Once(0.2f, () => {
                if (attacker != null)
                    attacker.userID = identify.RestoreUserID;
            });
        }

        #region Chat Hooks
        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel) {
            if (channel == Chat.ChatChannel.Team)
                return null;

            var identify = GetIdentify(player);
            if (identify == null)
                return null;

            Server.Broadcast(message, $"<color=#5af>{identify.Name}</color>", identify.UserID);
            return true;
        }

        private object OnUserChat(IPlayer player, string message)
            => PlayerData.ContainsKey(Convert.ToUInt64(player.Id)) ? true : (object)null;

        private object OnBetterChat(Dictionary<string, object> data) {
            var chatChannel = (Chat.ChatChannel)data["ChatChannel"];
            if (chatChannel == Chat.ChatChannel.Team)
                return null;

            var player = (IPlayer)data["Player"];
            if (player == null)
                return null;

            var identify = GetIdentify((BasePlayer)player.Object);
            if (identify == null)
                return null;

            var groupFields = (Dictionary<string, object>)BetterChat?.Call("API_GetGroupFields", config.BetterChatGroup);

            var message = ((string)groupFields["ChatFormat"])
                .Replace("{Title}", string.Format(RichTextFormat, groupFields["Title"], groupFields["TitleColor"], groupFields["TitleSize"]))
                .Replace("{Username}", string.Format(RichTextFormat, identify.Name, groupFields["UsernameColor"], groupFields["UsernameSize"]))
                .Replace("{Message}", string.Format(RichTextFormat, data["Message"], groupFields["MessageColor"], groupFields["MessageSize"]));

            Server.Broadcast(message, identify.UserID);
            return true;
        }
        #endregion

        #endregion

        #region Commands
        [ConsoleCommand("deepcover")]
        private void ccmdDeepCover(ConsoleSystem.Arg arg)
            => cmdDeepCover((BasePlayer)arg.Connection.player, arg.cmd.FullName, arg.Args);

        [ChatCommand("deepcover")]
        private void cmdDeepCover(BasePlayer player, string command, string[] args) {
            if (!HasPermission(player, Perm)) {
                player.ChatMessage(Lang("No Permission"));
                return;
            }

            if (!PlayerData.ContainsKey(player.userID)) {
                var identify = config.Identifies.GetRandom();

                PlayerData.Add(player.userID, new Identify.RestoreInfo {
                    Name = identify.Name,
                    UserID = identify.UserID,
                    RestoreName = player.displayName,
                    RestoreUserID = player.userID
                });

                Rename(player, identify.Name);
                player.ChatMessage(Lang("Deep Cover Enabled"));
            } else {
                var identify = GetIdentify(player);

                PlayerData.Remove(player.userID);
                Rename(player, identify.RestoreName);
                player.ChatMessage(Lang("Deep Cover Disabled"));
            }
        }
        #endregion

        #region Methods
        private Identify.RestoreInfo GetIdentify(BasePlayer player) {
            Identify.RestoreInfo identify;
            if (PlayerData.TryGetValue(player.userID, out identify))
                return identify;

            return null;
        }

        private void Rename(BasePlayer player, string newName) {
            player.displayName = newName;
            player.IPlayer.Rename(newName);
        }
        #endregion

        #region API
        private bool API_IsDeepCovered(BasePlayer player)
            => PlayerData.ContainsKey(player.userID);
        #endregion

        #region Helpers
        private string Lang(string key, string id = null) => lang.GetMessage(key, this, id);

        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        #endregion 
    }
}
