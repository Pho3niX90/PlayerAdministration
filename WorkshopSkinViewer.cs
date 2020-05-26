using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins {
    [Info("Workshop Skin Viewer", "Pho3niX90", "0.0.1")]
    [Description("Allows you to check item skins from workshop")]
    public class WorkshopSkinViewer : RustPlugin {
        #region Vars

        private const string permUse = "workshopskinviewer.use";

        #endregion

        #region Oxide Hooks

        private void Init() {
            cmd.AddChatCommand(config.command, this, nameof(cmdGiveSkinnedItem));
            permission.RegisterPermission(permUse, this);
        }

        #endregion

        #region Commands

        private void cmdGiveSkinnedItem(BasePlayer player, string command, string[] args) {
            if (permission.UserHasPermission(player.UserIDString, permUse) == false) {
                Message(player, "Permission");
                return;
            }

            if (args == null || args?.Length < 1) {
                Message(player, "Usage");
                return;
            }

            GiveItem(player, args[0]);
        }

        #endregion

        #region Core

        private void GiveItem(BasePlayer player, string skinIDString) {
            try {
                var skinID = 0UL;
                if (ulong.TryParse(skinIDString, out skinID) == false) {
                    Message(player, "Error");
                    return;
                }
                string itemShortname = player.GetActiveItem().info.shortname;
                Item activeItem = player.GetActiveItem();
                int  itemPos = activeItem.position;

                Item item = CopyIem(activeItem, skinID);
                player.GetActiveItem().DoRemove();

                player.GiveItem(item);
                item.SetHeldEntity(item.GetHeldEntity());
                item.MoveToContainer(activeItem.GetRootContainer(), itemPos);
                item.MarkDirty();
                player.SendNetworkUpdateImmediate();

                Message(player, "Received", item.info.displayName.english, skinID);
            } catch {
                Message(player, "Error");
            }
        }

        private Item CopyIem(Item item, ulong skin) {

            var duplicate = ItemManager.Create(item.info, item.amount, skin);
            if (item.hasCondition) {
                duplicate._maxCondition = item._maxCondition;
                duplicate._condition = item._condition;
            }

            if (item.contents != null) {
                duplicate.contents.capacity = item.contents.capacity;
            }

            var projectile = item.GetHeldEntity() as BaseProjectile;
            if (projectile == null) {
                return duplicate;
            }
            
            var projectileDuplicate = duplicate.GetHeldEntity() as BaseProjectile;
            if (projectileDuplicate == null) {
                return duplicate;
            }

            projectileDuplicate.primaryMagazine.Load(projectile.primaryMagazine.Save());
            return duplicate;
        }

        #endregion

        #region Configuration 1.1.0

        private static ConfigData config;

        private class ConfigData {
            [JsonProperty(PropertyName = "Command")]
            public string command;
        }

        private ConfigData GetDefaultConfig() {
            return new ConfigData {
                command = "skin",
            };
        }

        protected override void LoadConfig() {
            base.LoadConfig();

            try {
                config = Config.ReadObject<ConfigData>();

                if (config == null) {
                    LoadDefaultConfig();
                }
            } catch {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                Interface.Oxide.RootPluginManager.RemovePlugin(this);
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig() {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization 1.1.1

        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Usage", "Usage:\n/wskin skinID"},
                {"Permission", "You don't have permission to use that!"},
                {"Received", "You received {0} with skin #{1}"},
                {"Error", "Looks as you made a mistake!"}
            }, this);
        }

        private void Message(BasePlayer player, string messageKey, params object[] args) {
            if (player == null) {
                return;
            }

            var message = GetMessage(messageKey, player.UserIDString, args);
            player.SendConsoleCommand("chat.add", (object)0, (object)message);
        }

        private string GetMessage(string messageKey, string playerID, params object[] args) {
            return string.Format(lang.GetMessage(messageKey, this, playerID), args);
        }

        #endregion
    }
}
