using Facepunch;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins {
    [Info("IFN Stash warning system", "Pho3niX90", "1.1.1")]
    [Description("")]

    class IcefuseStashWarning : RustPlugin {

        #region Plugin References
        [PluginReference] Plugin DiscordMessages;
        #endregion

        #region Global Vars
        const string PermissionStash = "icefusestashwarning.admin";
        ulong IcefuseSteamID = 76561198795921619;
        private IFNStashWarningConfig cfg;
        public List<ulong> StashUsers = new List<ulong>();
        #endregion

        #region Init Hooks

        void Init() {
            cfg = new IFNStashWarningConfig(this);
            if (!permission.PermissionExists(PermissionStash, this)) permission.RegisterPermission(PermissionStash, this);
            if (covalence.Server.Name.Contains("King")) {
                this.IcefuseSteamID = 76561199044451528L;
            }
        }

        #endregion

        #region Chat Commands
        [ChatCommand("stash")]
        private void StashCommand(BasePlayer player, string command, string[] args) {
            if (!HasPermission(player)) {
                SendReplyWithIcon(player, "Permission Error");
                return;
            }
            GiveStash(player);
            ToggleStashPlacement(player);
        }
        #endregion

        #region Game Hooks

        object CanSeeStash(BasePlayer player, StashContainer stash) {
            if (StashUsers.Contains(player.userID)) return "Dont show";

            IPlayer iplayer = covalence.Players.FindPlayerById(stash.OwnerID.ToString());
            Puts("Stash found " + stash.OwnerID);
            bool deleteContents = false;

            if (stash.OwnerID == player.userID && cfg.DiscordIgnoreOwnStash) return null;

            if (stash.OwnerID.Equals(IcefuseSteamID) && cfg.ShowServerStashWarning) {
                SendReplyWithIcon(player, GetMsg("Stash Found Warning"));
                deleteContents = true;
            }

            List<EmbedFieldList> content = new List<EmbedFieldList>();
            content.Add(
                    new EmbedFieldList() {
                        name = "Location",
                        value = stash.transform.position.ToString() + " Grid " + GridReference(stash.transform.position),
                        inline = false
                    }
                );
            int cnt = 0;
            var contents = new StringBuilder();
            while (cnt < stash.inventorySlots) {
                Item slot = stash.inventory.GetSlot(cnt);
                if (slot != null) {
                    if (deleteContents) slot.DoRemove();
                }
                cnt++;
            }
            content.Add(
                    new EmbedFieldList() {
                        name = "Owner of stash",
                        value = iplayer != null ? iplayer?.Name : stash.OwnerID.ToString(),
                        inline = false
                    }
                );
            Facepunch.RCon.Broadcast(RCon.LogType.Chat, new ConVar.Chat.ChatEntry {
                Message = "Stash Found, original owner is  " + iplayer.Name + " ["+ iplayer.Id+"], \n Location: " + stash.transform.position.ToString() + " Grid " + GridReference(stash.transform.position),
                UserId = player.UserIDString,
                Username = player.displayName,
                Time = Facepunch.Math.Epoch.Current
            });
            DiscordSend(player, content);
            return null;
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject) {
            var stash = gameObject.GetComponent<StashContainer>();
            var player = planner.GetOwnerPlayer();
            if (player == null || stash == null || !StashUsers.Contains(player.userID) || !HasPermission(player)) return;

            SendReplyWithIcon(player, "Stash Placed");
            stash.SetFlag(BaseEntity.Flags.Reserved5, true);
            stash.OwnerID = IcefuseSteamID;
            var contents = new StringBuilder();

            for (var i = 0; i < Random.Range(1, 6); i++) {
                var keyValuePair = cfg.FillerItems.ElementAt(Random.Range(0, cfg.FillerItems.Count - 1));

                Item item = ItemManager.CreateByName(keyValuePair.Key);
                int qty = Random.Range(1, Math.Min(Convert.ToInt32(keyValuePair.Value), item.MaxStackable()));
                item.amount = Math.Min(item.MaxStackable(), qty);

                item.MoveToContainer(stash.inventory);
                contents.Append(keyValuePair.Key).Append("(").Append(qty).Append(")").Append("\n");
            }

            GiveStash(player);

            string location = stash.transform.position.ToString() + " Grid " + GridReference(stash.transform.position);
            contents.Length -= 1;

            List<EmbedFieldList> content = new List<EmbedFieldList>();
            content.Add(
                    new EmbedFieldList() {
                        name = "Location Placed",
                        value = location,
                        inline = false
                    }
                );
            content.Add(
                    new EmbedFieldList() {
                        name = "Content",
                        value = contents.ToString(),
                        inline = false
                    }
                );
            Facepunch.RCon.Broadcast(RCon.LogType.Chat, new ConVar.Chat.ChatEntry {
                Message = "Stash Location Placed: " + location.ToString() + ",\n Stash Content: " + contents.ToString(),
                UserId = player.UserIDString,
                Username = player.displayName,
                Time = Facepunch.Math.Epoch.Current
            });
            DiscordSend(player, content);
        }

        #endregion

        #region Helpers
        void DiscordSend(BasePlayer player, List<EmbedFieldList> content) {
            if (cfg.DiscordWebhookURL.Length == 0 || cfg.DiscordWebhookURL.Equals("")) return;

            var str = new StringBuilder();
            List<EmbedFieldList> fields = new List<EmbedFieldList>();
            string connectString = $"[steam://connect/{covalence.Server.Address}:{covalence.Server.Port}](steam://connect/{covalence.Server.Address}:{covalence.Server.Port})";
            fields.Add(new EmbedFieldList() {
                name = cfg.DiscordWebhookServer.Length > 0 ? cfg.DiscordWebhookServer : covalence.Server.Name,
                value = connectString,
                inline = false
            });
            fields.Add(new EmbedFieldList() {
                name = player.IsAdmin ? "Admin" : "Player ",
                value = $"[{player.displayName}\n{player.UserIDString}](https://www.battlemetrics.com/rcon/players?filter[search]={player.UserIDString})",
                inline = false
            });
            str.Append(cfg.DiscordWebhookServer.Length > 0 ? cfg.DiscordWebhookServer : covalence.Server.Name + "\n" + player.displayName + "\n" + connectString);
            foreach (EmbedFieldList it in content) {
                fields.Add(it);
                str.Append("\n" + it.name + ": " + it.value);
            }
            Puts(str.ToString());

            var fieldsObject = fields.Cast<object>().ToArray();
            string json = JsonConvert.SerializeObject(fieldsObject);
            DiscordMessages?.Call("API_SendFancyMessage", cfg.DiscordWebhookURL, cfg.DiscordWebhookTitle, cfg.DiscordWebhookColor, json);
        }

        bool HasPermission(BasePlayer player, string permissionName = PermissionStash) {
            return permission.UserHasPermission(player.UserIDString, permissionName);
        }

        void SendReplyWithIcon(BasePlayer player, string format, params object[] args) {
            int cnt = 0;
            string msg = GetMsg(format);
            foreach (var arg in args) {
                msg = msg.Replace("{" + cnt + "}", arg.ToString());
                cnt++;
            }
            Player.Reply(player, msg, IcefuseSteamID);
        }

        void ToggleStashPlacement(BasePlayer player) {
            if (StashUsers.Contains(player.userID)) {
                StashUsers.Remove(player.userID);
            } else {
                StashUsers.Add(player.userID);
            }

            string status = StashUsers.Contains(player.userID) ? "enabled" : "disabled";

            if (cfg.DiscordSendStashToggle) {
                List<EmbedFieldList> content = new List<EmbedFieldList>();
                content.Add(new EmbedFieldList() {
                    name = "Status",
                    value = status,
                    inline = false
                });
                DiscordSend(player, content);
            }

            SendReplyWithIcon(player, "Stash Status", status);
        }

        private static string GridReference(Vector3 pos) {
            int worldSize = ConVar.Server.worldsize;
            float gridWidth = (worldSize * 0.0066666666666667f);
            float scale = worldSize / gridWidth;
            float translate = worldSize / 2f;
            float x = pos.x + translate;
            float z = pos.z + translate;
            var lat = (int)(x / scale);
            var latChar = (char)('A' + lat);
            var lon = (int)(worldSize / scale - z / scale);
            return $"{latChar}{lon}";
        }

        void GiveStash(BasePlayer player) {
            timer.Once(0.1f,
            () => player.inventory.GiveItem(ItemManager.CreateByName("stash.small", 1)));
        }
        #endregion

        #region Configuration

        private class IFNStashWarningConfig {
            // Config default vars
            public bool Debug = false;
            public string DiscordWebhookURL = "";
            public string DiscordWebhookTitle = "Icefuse: Stash Warning!";
            public string DiscordWebhookServer = "";
            public int DiscordWebhookColor = 13459797;
            public bool DiscordSendStashGive = true;
            public bool DiscordSendStashToggle = true;
            public bool DiscordIgnoreOwnStash = true;
            public bool ShowServerStashWarning = false;
            public Dictionary<string, object> FillerItems = new Dictionary<string, object>
            {
                {"explosive.timed", 10},
                {"lmg.m249", 3}
            };

            private IcefuseStashWarning plugin;
            public IFNStashWarningConfig(IcefuseStashWarning plugin) {
                this.plugin = plugin;
                /**
                 * Load all saved config values
                 * */
                GetConfig(ref Debug, "Debug: Show additional debug console logs");
                GetConfig(ref DiscordWebhookURL, "Discord: Webhook URL");
                GetConfig(ref DiscordWebhookTitle, "Discord: Embed Title");
                GetConfig(ref DiscordWebhookColor, "Discord: Color");
                GetConfig(ref DiscordWebhookServer, "Discord: Webhook Server Name");
                GetConfig(ref DiscordSendStashToggle, "Discord: Send Sash Toggle Msgs");
                GetConfig(ref DiscordSendStashGive, "Discord: Send Stash Give Msgs");
                GetConfig(ref DiscordIgnoreOwnStash, "Discord: Ignore Own Stashes");
                GetConfig(ref ShowServerStashWarning, "Stash: Show Server Stash Warning");
                GetConfig(ref FillerItems, "Stash Items");

                plugin.SaveConfig();
            }

            private void GetConfig<T>(ref T variable, params string[] path) {
                if (path.Length == 0) return;

                if (plugin.Config.Get(path) == null) {
                    SetConfig(ref variable, path);
                    plugin.PrintWarning($"Added new field to config: {string.Join("/", path)}");
                }

                variable = (T)Convert.ChangeType(plugin.Config.Get(path), typeof(T));
            }

            private void SetConfig<T>(ref T variable, params string[] path) => plugin.Config.Set(path.Concat(new object[] { variable }).ToArray());
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new configuration file.");

        #endregion

        #region Localization
        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                ["Stash Placed"] = "Stash has been placed",
                ["Stash Status"] = "Stash placement mode sucessfully [#ADD8E6]{0}[/#].",
                ["Permission Error"] = "You do not have permission.",
                ["Argument Error"] = "Err: The amount arg must be a number",
                ["Stashes Given"] = "You were sucessfully given [#ADD8E6]{0}[/#] stashes.",
                ["Stash Found Warning"] = "Permission restricted."

            }, this);
        }

        string GetMsg(string msg, params object[] args) {
            msg = lang.GetMessage(msg, this);
            if (args.Length > 0) {
                Puts("args " + args.Length);
                int cnt = 0;
                foreach (var arg in args) {
                    msg = msg.Replace("{" + cnt + "}", arg.ToString());
                    cnt++;
                }
            }
            return msg;
        }
        #endregion

        #region Classes

        public class EmbedFieldList {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
        }

        #endregion
    }
}
