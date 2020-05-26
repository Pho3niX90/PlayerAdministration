// Requires: ZoneManager
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("Zone Perms", "Pho3niX90", "1.1.1")]
    [Description("Grant players permissions when entering a zone.")]
    class ZonePerms : RustPlugin {
        [PluginReference] Plugin ZoneManager;

        #region Data Related
        private Dictionary<ulong, ZonePermsData> _zonePermsData = new Dictionary<ulong, ZonePermsData>();

        private class ZonePermsData {
            public List<string> permission, groups;

            public ZonePermsData() {
                permission = new List<string>();
                groups = new List<string>();
            }
        }


        private void SaveData() {
            Interface.Oxide.DataFileSystem.WriteObject("ZonePermsData", _zonePermsData);
        }

        private void Unload() {
            foreach (var player in _zonePermsData) {
                foreach (var perm in player.Value.permission) {
                    permission.RevokeUserPermission(player.Key.ToString(), perm);
                }

                foreach (var group in player.Value.groups) {
                    permission.RemoveUserGroup(player.Key.ToString(), group);
                }
            }
            _zonePermsData.Clear();
            SaveData();
        }

        private void OnServerSave() {
            SaveData();
        }
        #endregion

        #region Hooks
        private void Init() {
            LoadVariables();
            _zonePermsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, ZonePermsData>>("ZonePermsData");
        }

        Dictionary<ulong, string> lastZone = new Dictionary<ulong, string>();

        void OnPlayerDisconnected(BasePlayer player, string reason) {
            if (!lastZone.ContainsKey(player.userID)) return;
            OnExitZone(lastZone[player.userID], player);
            lastZone.Remove(player.userID);
        }

        private void OnEnterZone(string ZoneID, BasePlayer player) {
            if (player is HumanNPC || player is HTNPlayer || player is NPCPlayer || player.displayName.Trim().Equals("NPC")) return;
            Puts($"ZoneID {ZoneID} for player {player?.displayName}");
            if (!configData.Enable) return;

            ZoneAddons zoneaddvalue;
            ZonePermsData zonesvalue;

            if (configData.Zones.TryGetValue(ZoneID, out zoneaddvalue)) {
                if (!zoneaddvalue.EnableZone) return;

                Puts("ZoneID " + ZoneID);
                if (!lastZone.ContainsKey(player.userID)) {
                    lastZone.Add(player.userID, ZoneID);
                } else {
                    lastZone[player.userID] = ZoneID;
                }

                if (!_zonePermsData.TryGetValue(player.userID, out zonesvalue)) {
                    _zonePermsData.Add(player.userID, new ZonePermsData());
                }

                for (var i = 0; i < zoneaddvalue.OnZoneEnter.Permissions.Count; i++) {
                    permission.GrantUserPermission(player.UserIDString, zoneaddvalue.OnZoneEnter.Permissions[i], null);
                }

                for (var i = 0; i < zoneaddvalue.OnZoneEnter.Groups.Count; i++) {
                    permission.AddUserGroup(player.UserIDString, zoneaddvalue.OnZoneEnter.Groups[i]);
                }

                _zonePermsData[player.userID].groups.AddRange(zoneaddvalue.OnZoneExit.Groups);
                _zonePermsData[player.userID].permission.AddRange(zoneaddvalue.OnZoneExit.Permissions);

                SaveData();

            }
        }

        private void OnExitZone(string ZoneID, BasePlayer player) {
            if (player is HumanNPC || player is HTNPlayer || player is NPCPlayer || player.displayName.Trim().Equals("NPC")) return;
            Puts("ZoneID " + ZoneID);
            if (!configData.Enable) return;

            ZoneAddons zoneaddvalue;
            ZonePermsData zonesvalue;

            if (configData.Zones.TryGetValue(ZoneID, out zoneaddvalue)) {
                if (!zoneaddvalue.EnableZone) {
                    return;
                }

                if (_zonePermsData.TryGetValue(player.userID, out zonesvalue)) {
                    for (var i = 0; i < zoneaddvalue.OnZoneExit.Permissions.Count; i++) {
                        permission.RevokeUserPermission(player.UserIDString, zoneaddvalue.OnZoneExit.Permissions[i]);
                        zonesvalue.permission.Remove(zoneaddvalue.OnZoneExit.Permissions[i]);
                    }

                    for (var i = 0; i < zoneaddvalue.OnZoneExit.Groups.Count; i++) {
                        permission.RemoveUserGroup(player.UserIDString, zoneaddvalue.OnZoneExit.Groups[i]);
                        zonesvalue.groups.Remove(zoneaddvalue.OnZoneExit.Groups[i]);
                    }
                }

                _zonePermsData.Remove(player.userID);
                SaveData();
            }
        }

        #endregion

        #region Config

        private class ZoneAddons {
            public bool EnableZone;
            public OnEnter OnZoneEnter;
            public OnExit OnZoneExit;
        }

        private class OnEnter {
            public List<string> Permissions;
            public List<string> Groups;
        }

        private class OnExit {
            public List<string> Permissions;
            public List<string> Groups;
        }

        private ConfigData configData;
        private class ConfigData {
            public bool Enable;
            public Dictionary<string, ZoneAddons> Zones;
        }

        private void LoadVariables() {
            LoadConfigVariables();
            SaveConfig();
        }

        protected override void LoadDefaultConfig() {
            var config = new ConfigData {
                Enable = false,
                Zones = new Dictionary<string, ZoneAddons> {
                    ["431235"] = new ZoneAddons() {
                        EnableZone = false,
                        OnZoneEnter = new OnEnter() {
                            Permissions = new List<string>
                            {
                                "permission1",
                                "permission2"
                            },
                            Groups = new List<string>
                            {
                                "group1",
                                "group2"
                            }
                        },
                        OnZoneExit = new OnExit() {
                            Permissions = new List<string>
                            {
                                "permission1",
                                "permission2"
                            },
                            Groups = new List<string>
                            {
                                "group1",
                                "group2"
                            }
                        }
                    },
                    ["749261"] = new ZoneAddons() {
                        EnableZone = false,
                        OnZoneEnter = new OnEnter() {
                            Permissions = new List<string>
                            {
                                "permission1",
                                "permission2"
                            },
                            Groups = new List<string>
                            {
                                "group1",
                                "group2"
                            }
                        },
                        OnZoneExit = new OnExit() {
                            Permissions = new List<string>
                            {
                                "permission1",
                                "permission2"
                            },
                            Groups = new List<string>
                            {
                                "group1",
                                "group2"
                            }
                        }
                    }
                }

            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}