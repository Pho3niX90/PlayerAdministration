using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Zone TP to Spawn", "Pho3niX90", "0.0.3")]
    [Description("Teleports players to a spawnfile when entering a zone.")]
    public class ZoneTeleport : RustPlugin
    {
        [PluginReference] Plugin Spawns;
        [PluginReference] Plugin ZoneManager;

        List<ZSpawn> zoneToSpawn;

        public class ZSpawn
        {
            public bool isArena;
            public string zoneID;
            public string spawnName;
            public string arenaName;

            public ZSpawn(string zID, string name) {
                zoneID = zID;
                isArena = name.StartsWith("A:");
                arenaName = isArena ? name.Replace("A:", "") : "";
                spawnName = name;
            }
        }

        #region Lifecycle
        void Loaded() {
            zoneToSpawn = new List<ZSpawn>();
            LoadSpawnData();
        }

        void Unloaded() {
            SaveSpawnData();
        }
        #endregion

        #region Command Methods
        [ChatCommand("zonetospawn")]
        void cmdSpawns(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin) return;
            if (args.Length < 2) return;
            if (zoneToSpawn.FindAll(x => x.zoneID == args[0]).Count > 0) {
                SendReply(player, $"A spawn file already exists for ZoneID {args[0]}");
            } else {
                string spawnTo = args[1];
                bool toArena = spawnTo.StartsWith("A:");

                if (!toArena) {
                    List<Vector3> success = Spawns?.Call("LoadSpawnFile", args[1]) as List<Vector3>;
                    if (success != null && success.Count > 0) {
                        zoneToSpawn.Add(new ZSpawn(args[0], args[1]));
                        SendReply(player, "Link saved");
                    } else {
                        SendReply(player, $"Spawn file {args[1]} either contains no spawns, or doesn't exist.");
                    }
                } else {
                    zoneToSpawn.Add(new ZSpawn(args[0], args[1]));
                    SendReply(player, $"Arena spawn added");
                }
                SaveSpawnData();
            }
        }

        [ChatCommand("zonetospawn_remove")]
        void cmdSpawns2(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin) return;
            if (args.Length < 1) return;
            if (!ContainsZone(args[0])) {
                SendReply(player, $"No such link with ZoneID {args[0]}");
            } else {
                if (zoneToSpawn.RemoveAll(x => x.zoneID == args[0]) > 0) {
                    SendReply(player, "Linked Removed");
                    SaveSpawnData();
                } else {
                    SendReply(player, "An error occured");
                }
            }
        }

        [ChatCommand("zonetospawn_list")]
        void cmdSpawns3(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin) return;
            StringBuilder str = new StringBuilder();
            if (zoneToSpawn.Count == 0) {
                SendReply(player, "There are no links");
            } else {
                foreach (ZSpawn item in zoneToSpawn) {
                    if (item.isArena) str.Append($"{item.zoneID} links to {item.spawnName}, and is an arena\n");
                    else str.Append($"{item.zoneID} links to {item.spawnName}\n");
                }
                SendReply(player, str.ToString());
            }
        }
        #endregion

        #region Plugin Hooks
        void OnEnterZone(string ZoneID, BasePlayer player) {
            if (ContainsZone(ZoneID)) {
                ZSpawn spawnFile = zoneToSpawn.Find(x => x.zoneID == ZoneID);
                if (spawnFile.isArena) {
                    player.SendConsoleCommand($"chat.say \"/join {spawnFile.arenaName}\"");
                } else {
                    List<Vector3> spawns = LoadSpawnpoints(spawnFile.spawnName);

                    if (spawns != null) {
                        MoveToSpawn(player, spawns);
                    }
                }
            }
        }
        #endregion

        #region Helpers
        void MoveToSpawn(BasePlayer player, List<Vector3> spawns) {
            Vector3 position = GetSpawnPoint(spawns);
            if (position != null) {
                if (!BasePlayer.sleepingPlayerList.Contains(player)) BasePlayer.sleepingPlayerList.Add(player);

                Vector3 spawn = GetSpawnPoint(spawns);
                player.MovePosition(spawn);
                player.ClientRPCPlayer(null, player, "ForcePositionTo", spawn);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.UpdateNetworkGroup();

                player.SendNetworkUpdateImmediate(false);
                player.ClientRPCPlayer(null, player, "StartLoading");
                player.SendFullSnapshot();
            }
        }

        Vector3 GetSpawnPoint(List<Vector3> spawns) {
            return spawns.GetRandom();
        }

        private List<Vector3> LoadSpawnpoints(string spawnFile) {
            List<Vector3> spawnPoints = Spawns?.Call("LoadSpawnFile", spawnFile) as List<Vector3>;
            if (spawnPoints != null) {
                if (spawnPoints.Count == 0) {
                    PrintError("Loaded spawnfile contains no spawn points. Unable to continue");
                    return null;
                }
                PrintWarning($"Successfully loaded {spawnPoints.Count} spawn points");
            } else {
                PrintError($"Unable to load the specified spawnfile: {spawnFile}");
                return null;
            }
            return new List<Vector3>(spawnPoints);
        }

        bool ContainsZone(string ZoneID) => zoneToSpawn.FindAll(x => x.zoneID == ZoneID).Count > 0;

        private Dictionary<Vector3, string> GetAllZonesForFloatingText() {
            List<ZSpawn> spawns = zoneToSpawn.FindAll(x => x.isArena);
            Dictionary<Vector3, string> spawnsRet = new Dictionary<Vector3, string>();
            foreach (ZSpawn zs in zoneToSpawn) {
                Vector3 zoneLoc = (Vector3)ZoneManager.Call("GetZoneLocation", zs.zoneID);
                Vector3 zoneLocWithOff = zoneLoc + new Vector3(0f, 1f, 0f);
                spawnsRet.Add(zoneLocWithOff, zs.arenaName);
            }
            return spawnsRet;
        }
        #endregion

        #region Data
        void SaveSpawnData() {
            Interface.Oxide.DataFileSystem.WriteObject($"ZoneTeleport", zoneToSpawn, true);
        }

        void LoadSpawnData() {
            zoneToSpawn = Interface.Oxide.DataFileSystem.ReadObject<List<ZSpawn>>($"ZoneTeleport");
        }
        #endregion
    }
}
