using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oxide.Plugins {
    [Info("Zone Health", "Pho3niX90", "0.0.1")]
    [Description("Teleports players to a spawnfile when entering a zone.")]
    public class ZoneAddHealth : RustPlugin {
        [PluginReference] Plugin ZoneManager;
        private DynamicConfigFile data;

        Dictionary<string, string> zoneKit;

        #region Lifecycle
        void Loaded() {
            zoneKit = new Dictionary<string, string>();
            LoadData();
        }
        void Unloaded() {
            SaveData();
        }
        #endregion

        #region Command Methods
        [ChatCommand("zonehealth")]
        void cmdSpawns(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin) return;
            if (args.Length < 2) return;
            if (zoneKit.ContainsKey(args[0])) {
                SendReply(player, $"A kit already exists for ZoneID {args[0]}");
            } else {
                object success = Interface.CallHook("isKit", args[1]);
                bool succ = false;
                if (success != null) {
                    succ = (bool) success;
                }

                if (succ) {
                    zoneKit.Add(args[0], args[1]);
                    SendReply(player, "Kit Link saved");
                    SaveData();
                } else {
                    SendReply(player, $"Kit doesn't exist.");
                }
            }
        }
        [ChatCommand("zonehealth_remove")]
        void cmdSpawns2(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin) return;
            if (args.Length < 1) return;
            if (!zoneKit.ContainsKey(args[0])) {
                SendReply(player, $"No such kit link with ZoneID {args[0]}");
            } else {
                if (zoneKit.Remove(args[0])) {
                    SendReply(player, "Kit Link Removed");
                    SaveData();
                } else {
                    SendReply(player, "An error occured");
                }
            }
        }
        [ChatCommand("zonehealth_list")]
        void cmdSpawns3(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin) return;
            StringBuilder str = new StringBuilder();
            if (zoneKit.Count == 0) {
                SendReply(player, "There are no links");
            } else {
                foreach (KeyValuePair<string, string> item in zoneKit) {
                    str.Append($"{item.Key} kit for {item.Value}\n");
                }
                SendReply(player, str.ToString());
            }
        }
        #endregion

        #region Plugin Hooks
        void OnEnterZone(string ZoneID, BasePlayer player) {
            if (zoneKit.ContainsKey(ZoneID)) {
                Interface.CallHook("TryGiveKit", player, zoneKit[ZoneID]);
            }
        }
        private void OnExitZone(string ZoneID, BasePlayer player) {
            if (zoneKit.ContainsKey(ZoneID)) {
                player.inventory.Strip();
            }
        }
        #endregion

        #region Helpers

        #endregion

        #region Data
        void SaveData() {
            try {
                Interface.Oxide.DataFileSystem.WriteObject<Dictionary<string, string>>($"ZoneKit", zoneKit, true);
            } catch (Exception e) {

            }
        }

        void LoadData() {
            try {
                zoneKit = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>($"ZoneKit");
            } catch (Exception e) {

            }
        }
        #endregion
    }
}
