//Requires: Clans
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("King Score Board", "Pho3niX90", "0.1.96")]
    [Description("Scoring System")]
    public class KingScoreBoard : RustPlugin
    {
        [PluginReference] Plugin Clans;
        [PluginReference] Plugin DiscordMessages;

        Dictionary<string, ClanScoreboard> _clansScores;
        Dictionary<ulong, PlayerScoreboard> _playerScores;
        Dictionary<ulong, string> openPanels;
        Dictionary<ulong, HitInfo> _lastWounded;
        Dictionary<string, int> lineNum;
        Dictionary<string, uint> clanTcKiller;
        //List<uint> cratesFound = new List<uint>();
        Dictionary<uint, ulong> cratesLooted = new Dictionary<uint, ulong>();
        Dictionary<string, int> _tcs;
        string mapCreationDate;

        private void OnServerInitialized() {
            mapCreationDate = SaveRestore.SaveCreatedTime.ToLocalTime().ToString("yyyy_MM_dd");
        }

        void Loaded() {
            lineNum = new Dictionary<string, int>();
            _clansScores = new Dictionary<string, ClanScoreboard>();
            _playerScores = new Dictionary<ulong, PlayerScoreboard>();
            openPanels = new Dictionary<ulong, string>();
            _lastWounded = new Dictionary<ulong, HitInfo>();
            _tcs = new Dictionary<string, int>();

            LoadData();

            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                try {
                    InitPlayer(player.userID);
                    string playerClan = GetClan(player.userID);
                    if (playerClan != null && playerClan.Length > 0) InitClan(playerClan);
                } catch (Exception e) {
                }
            }

            if (config.tournamentMode) {
                DestroyAllMarkers();
                CreateAllMarkers();
            }
            SaveData();
        }

        void Unload() {

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) {
                try {
                    CuiHelper.DestroyUi(BasePlayer.activePlayerList[i], mainPanelClanName);
                    CuiHelper.DestroyUi(BasePlayer.activePlayerList[i], mainPanelPlayersName);
                } catch (Exception e) {

                }
            }

            if (config.tournamentMode)
                DestroyAllMarkers();


            SaveData();

            _playerScores.Clear();
            _clansScores.Clear();
        }

        #region Hooks
        private List<LootableCorpse> FindCorpses(Vector3 pos) {
            List<LootableCorpse> corpses = new List<LootableCorpse>();
            Vis.Entities(pos, 10f, corpses);
            return corpses;
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity) {
            ulong playerId = player.userID;
            if (playerId == 0) return;
            string clan = GetClan(playerId);

            int type = CrateType(entity);
            uint crateId = entity.net.ID;
            if (cratesLooted.ContainsKey(crateId)) return;
            if (!_playerScores.ContainsKey(playerId)) return;

            if (!_clansScores.ContainsKey(clan)) {
                ClanScoreboard newClan = new ClanScoreboard(clan);
                _clansScores.TryAdd(clan, newClan);
            }

            if (type == 0) {
                return;
            } else if (type == 1) {
                _playerScores[playerId].MilCrates++;
                _clansScores[clan].MilCrates++;
            } else if (type == 2) {
                _playerScores[playerId].Airdrops++;
                _clansScores[clan].Airdrops++;
            } else if (type == 3) {
                _playerScores[playerId].EliteCrates++;
                _clansScores[clan].EliteCrates++;
            } else if (type == 4) {
                _playerScores[playerId].HeliCrates++;
                _clansScores[clan].HeliCrates++;
            } else if (type == 5) {
                _playerScores[playerId].BradleyCrates++;
                _clansScores[clan].BradleyCrates++;
            }

            DiscordReport($"{playerId} opened a {entity.ShortPrefabName}");
            cratesLooted.TryAdd(crateId, player.userID);
        }

        int CrateType(BaseEntity entity) {
            switch (entity.ShortPrefabName) {
                case "crate_normal": return 0;
                case "codelockedhackablecrate": return 1;
                case "supply_drop": return 2;
                case "crate_elite": return 3;
                case "heli_crate": return 4;
                case "bradley_crate": return 5;
                default: return 0;
            }
        }

        void OnClanCreate(string tag) {
            Puts($"clan {tag} created");
            InitClan(tag);
        }

        void OnClanDestroy(string tag) {
            Puts($"clan {tag} disbanded, cleaning data");
            _clansScores.Remove(tag);
        }

        void OnServerSave() {
            SaveData();
        }

        private void OnNewSave(string filename) {
            _clansScores.Clear();
            _playerScores.Clear();
            _tcs.Clear();
        }

        void OnPlayerConnected(BasePlayer player) {
            InitPlayer(player.userID);
            string clanTag = GetClan(player.userID);
            InitClan(clanTag);
            if (config.tournamentMode) {
                DestroyAllMarkers();
                CreateAllMarkers();
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity victimEntity, HitInfo hitInfo) {
            if ((victimEntity is BasePlayer) && !(victimEntity is NPCPlayer)) {
                ulong userId = victimEntity.ToPlayer().userID;

                if (_lastWounded.ContainsKey(userId)) {
                    _lastWounded[userId] = hitInfo;
                } else {
                    _lastWounded.TryAdd(userId, hitInfo);
                }
            } else if (victimEntity is BaseHelicopter) {
                if (hitInfo?.InitiatorPlayer != null) {
                    if (_lastWounded.ContainsKey(victimEntity.net.ID)) {
                        _lastWounded[victimEntity.net.ID] = hitInfo;
                    } else {
                        _lastWounded.TryAdd(victimEntity.net.ID, hitInfo);
                    }
                }
            }
        }

        private void OnEntityDeath(BuildingPrivlidge cupboard, HitInfo info) {
            BasePlayer attacker = info?.InitiatorPlayer;
            if (attacker != null) {
                if (cupboard.OwnerID != 0 && !IsMemberOrAlly(attacker.UserIDString, cupboard.OwnerID.ToString())) {
                    AddTCKill(attacker.userID);
                    clanTcKiller.TryAdd(GetClan(attacker.userID), cupboard.net.ID);
                }
            }
        }

        void OnEntityKill(BuildingPrivlidge cupboard) {
            if (!config.tournamentMode) return;
            DestroyMarker(cupboard);
            Puts($"Owner is {cupboard.OwnerID}");
            string clan = GetClan(cupboard.OwnerID);
            Puts($"Clan is {clan}");
            if (clan != null && _tcs.ContainsKey(clan)) {
                int old = _tcs[clan];
                _tcs[clan]--;
                Puts($"Creating clan {clan} tc info, from {old} to {_tcs[clan]}");
                if (_tcs[clan] < 1) {
                    JArray members = GetClanMembers(clan);
                    if (members == null && members.Count == 0) {
                        BasePlayer Player = BasePlayer.FindByID(cupboard.OwnerID);
                        SendReply(Player, "You aren't in a clan");
                        MsgAdminsOnline($"Player `{Player.displayName} ({Player.userID})` is not part of a clan");
                    } else {
                        foreach (string userid in members) {
                            if (userid == null || userid.IsNullOrEmpty()) return;
                            BasePlayer.FindByID(ulong.Parse(userid))?.Kick("You have been eliminated!");
                            string clantagattack = GetClan((cupboard.lastAttacker as BasePlayer).UserIDString);
                            PrintToChat($"Team <color=red>{clan}</color> has been eliminated by <color=red>{clantagattack}</color>!");
                            permission.GrantUserPermission(userid, "whitelist.kicked", null);
                        }
                    }
                }
            }
        }

        JArray GetClanMembers(string clan) {
            JObject clanData = GetClanData(clan);
            Puts(clanData.ToString());
            return clanData?.GetValue("members") as JArray;
        }

        void OnEntityBuilt(Planner plan, GameObject go) {
            if (!config.tournamentMode) return;

            BasePlayer Player = plan.GetOwnerPlayer();
            BaseEntity Entity = go.ToBaseEntity();
            if (Entity.ShortPrefabName.Contains("cupboard.tool")) {
                BaseEntity TC = Entity as BuildingPrivlidge;
                string clan = GetClan(Player.userID);
                if (clan == null || clan.Trim().IsNullOrEmpty()) {
                    SendReply(Player, "You aren't in a clan");
                    MsgAdminsOnline($"Player `{Player.displayName} ({Player.userID})` is not part of a clan");
                }

                if (clan.Trim().IsNullOrEmpty()) {
                    permission.RevokeUserPermission(Player.UserIDString, "whitelist.allowed");
                    permission.GrantUserPermission(Player.UserIDString, "whitelist.kicked", null);
                    Puts($"Should kick {Player.displayName}");
                    Player?.Kick("You have been eliminated!"); ;
                    return;
                }

                if (_tcs.ContainsKey(clan)) {
                    Puts("Updating clan " + clan + " tc info");

                    if (_tcs[clan] < config.maxTcs) {
                        _tcs[clan]++;
                    } else {
                        SendReply(Player, $"Maximum of {config.maxTcs} allowed.");
                        _tcs[clan]++;
                        TC.Kill();
                        return;
                    }

                } else {
                    Puts("Creating clan " + clan + " tc info");
                    _tcs.TryAdd(clan, 1);
                }
                CreateMarker(Entity);
            }
        }

        void DestroyMarker(BaseEntity entity) {
            var TCs = BaseNetworkable.FindObjectsOfType<MapMarkerGenericRadius>().Where(tc => tc.net.ID == entity.net.ID);
            foreach (var tc in TCs) tc.Kill();
        }

        void CreateAllMarkers() {
            foreach (var x in BaseNetworkable.FindObjectsOfType<MapMarkerGenericRadius>().ToList()) {

                string clan = GetClan(x.OwnerID);
                if (!clan.IsNullOrEmpty()) {
                    if (_tcs.ContainsKey(clan)) {
                        Puts("Updating clan " + clan + " tc info");
                        _tcs[clan]++;
                    } else {
                        Puts("Creating clan " + clan + " tc info");
                        _tcs.TryAdd(clan, 1);
                    }
                }
                CreateMarker(x);
            }
        }

        void CreateMarker(BaseEntity entity) {
            MapMarkerGenericRadius marker2 = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", entity.transform.position) as MapMarkerGenericRadius;
            marker2.alpha = 0.8f;
            marker2.color1 = Color.red;
            marker2.color2 = Color.green;
            marker2.radius = 0.3f;
            marker2.enabled = true;
            marker2.Spawn();
            marker2.SendUpdate();
        }

        void DestroyAllMarkers() {
            foreach (var x in BaseNetworkable.FindObjectsOfType<MapMarkerGenericRadius>().ToList()) {
                x.Kill();
            }
        }

        List<uint> lastApc = new List<uint>();
        void OnEntityKill(BradleyAPC apc) {
            Puts("APC Killed");
            //FindCrates(apc.transform.position);
            if (apc.lastAttacker is BasePlayer && !lastApc.Contains(apc.net.ID)) {
                lastApc.Add(apc.net.ID);
                AddApcKill(((BasePlayer)apc.lastAttacker).userID);
            }
        }

        void OnEntityKill(BaseHelicopter heli) {
            //FindCrates(heli.transform.position);
            if (heli.lastAttacker is BasePlayer) {
                AddHeliKill(((BasePlayer)heli.lastAttacker).userID);
            } else if (_lastWounded.ContainsKey(heli.net.ID)) {
                HitInfo hitInfo = _lastWounded[heli.net.ID];
                if (hitInfo?.InitiatorPlayer != null) {
                    AddHeliKill(hitInfo.InitiatorPlayer.userID);
                }
            }
        }

        void DiscordReport(string msg) {
            if (!config.tournamentMode) return;
            foreach (string hook in config.discordHooks)
                DiscordMessages?.Call("API_SendTextMessage", hook, msg);
        }

        void OnEntityDeath(BasePlayer player) {
            if ((player is HTNPlayer || player is NPCPlayer) && player.lastAttacker != null && player.lastAttacker is BasePlayer) {
                AddScientistKill(((BasePlayer)player.lastAttacker).userID, player.ShortPrefabName.Equals("heavyscientist"));
            }
        }

        object OnPlayerDeath(BasePlayer victim, HitInfo info) {
            if (victim == null || !(victim is BasePlayer) || victim.userID < 76560000000000000) return null;
            BasePlayer attacker = info?.Initiator?.ToPlayer();
            if (victim.userID == attacker.userID) return null;
            if (attacker != null && attacker is BasePlayer && !(victim is HTNPlayer || victim is NPCPlayer)) {
                try {
                    if (!IsMemberOrAlly(attacker.UserIDString, victim.UserIDString))
                        AddKill(attacker.userID);
                } catch (Exception e) {
                    Puts("AddKill error");
                }
            } else if (_lastWounded.ContainsKey(victim.userID)) {
                HitInfo hitInfo = _lastWounded[victim.userID];
                if (hitInfo?.InitiatorPlayer != null && hitInfo?.InitiatorPlayer is BasePlayer && !(hitInfo?.InitiatorPlayer is NPCPlayer)) {
                    if (!IsMemberOrAlly(hitInfo?.InitiatorPlayer.UserIDString, victim.UserIDString))
                        AddKill(hitInfo.InitiatorPlayer.userID);
                    _lastWounded.Remove(victim.userID);
                }
            }

            if (victim != null && !(victim is HTNPlayer || victim is NPCPlayer)) {
                try {
                    AddDeath(victim.userID);
                } catch (Exception e) {
                    Puts("AddDeath error");
                }
            }
            return null;
        }

        object OnPlayerWound(BasePlayer player) {
            if (!(player is BasePlayer) || player is NPCPlayer) return null;
            AddDown(player.userID);
            return null;
        }

        object OnPlayerRevive(BasePlayer reviver, BasePlayer player) {
            if (!(player is BasePlayer) || player is NPCPlayer) return null;
            AddRevive(reviver.userID);
            return null;
        }
        #endregion

        string GetClan(ulong userID) => Clans?.Call<string>("GetClanOf", userID);
        string GetClan(string userID) => Clans?.Call<string>("GetClanOf", userID);
        JObject GetClanData(string tag) {
            return Clans?.Call<JObject>("GetClan", tag);
        }

        bool IsMemberOrAlly(string userid1, string userid2) {
            if (userid1.Equals(userid2)) return true;
            return Clans.Call<bool>("IsMemberOrAlly", userid1, userid2);
            // bool IsMemberOrAlly(string playerId, string otherId) // Check if 2 players are clan mates or clan allies
        }

        void AddKill(ulong playerId) {
            if (playerId == 0) return;
            string clan = GetClan(playerId);
            _AddPlayerKill(playerId);
            if (clan != null) {
                if (_clansScores.ContainsKey(clan)) {
                    _clansScores[clan].Kills++;
                } else {
                    ClanScoreboard newClan = new ClanScoreboard(clan);
                    newClan.Kills++;
                    _clansScores.TryAdd(clan, newClan);
                }
            } else {
                BasePlayer player = BasePlayer.FindByID(playerId);
                SendReply(player, "You aren't in a clan");
                MsgAdminsOnline($"Player `{player.displayName} ({playerId})` is not part of a clan");
            }
        }

        void AddApcKill(ulong playerId) {
            if (playerId == 0) return;
            string clan = GetClan(playerId);
            _AddApcKill(playerId);

            if (clan != null) {
                PrintToChat($"Team {clan} has destroy Bradley!");
                if (_clansScores.ContainsKey(clan)) {
                    _clansScores[clan].Bradley++;
                } else {
                    ClanScoreboard newClan = new ClanScoreboard(clan);
                    newClan.Bradley++;
                    _clansScores.TryAdd(clan, newClan);
                }
            }
            DiscordReport($"{playerId} killed an APC");
        }

        void AddHeliKill(ulong playerId) {
            if (playerId == 0) return;
            string clan = GetClan(playerId);
            _AddHeliKill(playerId);
            if (clan != null) {
                PrintToChat($"Team {clan} has destroy the Patrol Helicopter!");
                if (_clansScores.ContainsKey(clan)) {
                    _clansScores[clan].Helis++;
                } else {
                    ClanScoreboard newClan = new ClanScoreboard(clan);
                    newClan.Helis++;
                    _clansScores.TryAdd(clan, newClan);
                }
            }
            DiscordReport($"{playerId} killed a Heli");
        }

        void AddScientistKill(ulong playerId, bool isHeavy) {
            if (playerId == 0) return;
            string clan = GetClan(playerId);
            _AddNpcKill(playerId);
            if (clan != null) {
                if (_clansScores.ContainsKey(clan)) {
                    if (isHeavy) {
                        _clansScores[clan].HTNScientist++;
                    } else {
                        _clansScores[clan].Scientist++;
                    }
                } else {
                    ClanScoreboard newClan = new ClanScoreboard(clan);
                    if (isHeavy) {
                        newClan.HTNScientist++;
                    } else {
                        newClan.Scientist++;
                    }
                    _clansScores.TryAdd(clan, newClan);
                }
            }
            DiscordReport($"{playerId} killed a scientist");
        }

        void AddTCKill(ulong playerId) {
            if (playerId == 0) return;
            string clan = GetClan(playerId);
            _AddTCKill(playerId);
            if (clan != null) {
                if (_clansScores.ContainsKey(clan)) {
                    _clansScores[clan].TC++;
                } else {
                    ClanScoreboard newClan = new ClanScoreboard(clan);
                    newClan.TC++;
                    _clansScores.TryAdd(clan, newClan);
                }
            }
            DiscordReport($"{playerId} destroyed a TC");
        }

        void AddDeath(ulong playerId) {
            if (playerId == 0) return;
            string clan = GetClan(playerId);
            _AddPlayerDeath(playerId);
            if (clan != null) {
                if (_clansScores.ContainsKey(clan)) {
                    _clansScores[clan].Deaths++;
                } else {
                    ClanScoreboard newClan = new ClanScoreboard(clan);
                    newClan.Deaths++;
                    _clansScores.TryAdd(clan, newClan);
                }
            }
            DiscordReport($"{playerId} died");
        }

        void AddRevive(ulong playerId) {
            if (playerId == 0) return;
            string clan = GetClan(playerId);
            _AddPlayerRevive(playerId);
            if (clan != null) {
                if (_clansScores.ContainsKey(clan)) {
                    _clansScores[clan].Revives++;
                } else {
                    ClanScoreboard newClan = new ClanScoreboard(clan);
                    newClan.Revives++;
                    _clansScores.TryAdd(clan, newClan);
                }
            }
        }

        void AddDown(ulong playerId) {
            if (playerId == 0) return;
            string clan = GetClan(playerId);
            _AddPlayerDown(playerId);
            if (clan != null) {
                if (_clansScores.ContainsKey(clan)) {
                    _clansScores[clan].Downs++;
                } else {
                    ClanScoreboard newClan = new ClanScoreboard(clan);
                    newClan.Downs++;
                    _clansScores.TryAdd(clan, newClan);
                }
            }
        }

        void _AddPlayerKill(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].Kills++;
                _playerScores[playerId].clanTag = GetClan(playerId);
            }
        }

        void _AddApcKill(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].Bradley++;
            }
        }

        void _AddNpcKill(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].Scientist++;
            }
        }

        void _AddTCKill(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].TC++;
            }
        }

        void _AddHeliKill(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].Helis++;
            }
        }

        void _AddCrate(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].LockCrates++;
            }
        }

        void _AddPlayerDeath(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].Deaths++;
            }
        }

        void _AddPlayerDown(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].Downs++;
            }
        }

        void _AddPlayerRevive(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].Revives++;
            }
        }

        void InitPlayer(ulong playerId) {
            if (playerId == 0) return;
            string playerClan = GetClan(playerId);
            if (!_playerScores.ContainsKey(playerId)) {
                PlayerScoreboard pScore = new PlayerScoreboard(playerId, playerClan);
                _playerScores.TryAdd(playerId, pScore);
            }
            InitClan(playerClan);
        }

        void InitClan(string clanTag) {
            if (clanTag != null && !_clansScores.ContainsKey(clanTag)) {
                ClanScoreboard cScore = new ClanScoreboard(clanTag);
                _clansScores.TryAdd(clanTag, cScore);
            }
            if (clanTag != null && !_tcs.ContainsKey(clanTag)) {
                _tcs.TryAdd(clanTag, 0);
            }
        }

        ClanScoreboard GetClanStats(ulong playerId) => _clansScores[GetClan(playerId)];
        PlayerScoreboard GetPlayerStats(ulong playerId) => _playerScores[playerId];

        #region Helpers
        void MsgAdminsOnline(string msg) {
            foreach (var admin in BasePlayer.activePlayerList.Where(x => x.IsAdmin)) {
                SendReply(admin, msg);
            }
        }
        #endregion

        #region Chat Commands
        [ChatCommand("kdr")]
        private void cmdStats(BasePlayer player, string command, string[] args) {
            InitPlayer(player.userID);

            try {
                PlayerScoreboard pScore = GetPlayerStats(player.userID);
                SendReply(player, $"Players Stats\nKills: {pScore.Kills}\nDeaths: {pScore.Deaths}\nRevives: {pScore.Revives}\nDowns: {pScore.Downs}");

                ClanScoreboard cScore = GetClanStats(player.userID);
                SendReply(player, $"Clan Stats\nKills: {cScore.Kills}\nDeaths: {cScore.Deaths}\nRevives: {cScore.Revives}\nDowns: {cScore.Downs}");
            } catch (Exception e) {

            }
        }

        [ConsoleCommand("clantag")]
        void cmdClanTAG(ConsoleSystem.Arg arg) {
            if (arg.Args.Length == 0) return;
            JObject clanData = GetClanData(arg.Args[0]);
            if (clanData == null) {
                Puts($"Clan {arg.Args[0]} not found");
                return;
            }
            if (arg.IsAdmin || arg.IsServerside) Puts(GetClanData(arg.Args[0]).ToString());
        }

        [ConsoleCommand("getclan")]
        void cmdClan(ConsoleSystem.Arg arg) {
            //if (arg.IsAdmin || arg.IsServerside)
            Puts("Called");
            string search = arg.Args[0];
            Puts($"Search clan for {search}");
            string clan = GetClan(search);
            Puts($"Clan for {search} is {clan}");
        }

        [ConsoleCommand("cleartc")]
        void cmdClanTAG3(ConsoleSystem.Arg arg) {
            if (arg.IsAdmin || arg.IsServerside) {
                foreach (var clan in _clansScores.Values) {
                    clan.TC = 0;
                }
                SaveData();
            }
        }

        [ConsoleCommand("tcs")]
        void cmdClanTAG2(ConsoleSystem.Arg arg) {
            if (!config.tournamentMode) return;
            StringBuilder leftTc = new StringBuilder();
            foreach (KeyValuePair<string, int> clan in _tcs) {
                leftTc.Append($"Clan {clan.Key} has {clan.Value} TC's left\n");
                if (clan.Value < 1) {
                    JArray members = GetClanMembers(clan.Key);
                    foreach (string userid in members) {
                        BasePlayer player = BasePlayer.FindByID(ulong.Parse(userid));
                        if (player != null) {
                            if (player.IsConnected && !player.IsAdmin) {
                                permission.RevokeUserPermission(userid, "whitelist.allowed");
                                permission.GrantUserPermission(userid, "whitelist.kicked", null);
                                Puts($"Should kick {player.displayName}");
                                player?.Kick("You have been eliminated!"); ;
                                PrintToChat($"Team {clan} has been eliminated!");
                            }
                        }
                    }
                }
            }
            arg.ReplyWith(leftTc.ToString());
            Puts(leftTc.ToString());
        }

        [ConsoleCommand("kickall")]
        void cmdClanTAG4(ConsoleSystem.Arg arg) {
            if (!arg.IsAdmin || !config.tournamentMode) return;
            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                if (!player.IsAdmin && player.IsConnected) {
                    permission.RevokeUserPermission(player.UserIDString, "whitelist.allowed");
                    permission.GrantUserPermission(player.UserIDString, "whitelist.kicked", null);
                    player.Kick("The tournament has ended, thanks for participating.");
                }
            }
        }
        #endregion

        #region Data
        void SaveData() {
            try {
                Interface.Oxide.DataFileSystem.WriteObject<Dictionary<ulong, PlayerScoreboard>>($"ScoreBoard/allPlayers_{mapCreationDate}", _playerScores, true);
            } catch (Exception e) {

            }

            try {
                Interface.Oxide.DataFileSystem.WriteObject<Dictionary<string, ClanScoreboard>>($"ScoreBoard/allClans_{mapCreationDate}", _clansScores, true);
            } catch (Exception e) {

            }
        }

        void LoadData(bool reset = false) {
            try {
                _playerScores = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerScoreboard>>($"ScoreBoard/allPlayers_{mapCreationDate}");
            } catch (Exception e) {

            }

            try {
                _clansScores = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, ClanScoreboard>>($"ScoreBoard/allClans_{mapCreationDate}");
            } catch (Exception e) {

            }

            if (config.tournamentMode) {
                List<string> clansToRemove = new List<string>();
                foreach (ClanScoreboard c in _clansScores.Values) {
                    JObject clan = GetClanData(c.clanTag);
                    if (clan == null) Puts("Clan: " + c.clanTag + " is being reported as non existent");
                }
            }
            SaveData();
        }
        #endregion

        #region Classes
        public class EmbedFieldList
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
        }

        public class DiscordHeaders
        {
            public string headerName;
            public int length() {
                return headerName.Length;
            }
            public DiscordHeaders(string name) {
                headerName = name;
            }
            public int paddingLeft() {
                return (int)Math.Round(headerName.Length / 2d);
            }
            public int paddingRight() {
                return headerName.Length - paddingLeft();
            }
        }

        public class Points
        {
            public int TC;
            public int Bradley;
            public int Scientist;
            public int HeavyScientist;
            public int Kill;
            public int Death;
            public int Heli;
            public int LockCrate;
            public int BradleyCrate;
            public int HeliCrate;
            public int MilCrate;
            public int EliteCrate;
            public int Airdrop;
            public Points(int TC, int Bradley, int Scientist, int HeavyScientist, int Kill, int Death, int Heli, int LockCrate, int MilCrate, int EliteCrate, int Airdrop, int BradleyCrate, int HeliCrate) {
                this.TC = TC;
                this.Bradley = Bradley;
                this.Scientist = Scientist;
                this.HeavyScientist = HeavyScientist;
                this.Kill = Kill;
                this.Death = Death;
                this.Heli = Heli;
                this.LockCrate = LockCrate;
                this.BradleyCrate = BradleyCrate;
                this.HeliCrate = HeliCrate;
                this.MilCrate = MilCrate;
                this.EliteCrate = EliteCrate;
                this.Airdrop = Airdrop;
            }
            public Points() {
                TC = 40;
                Bradley = 20;
                Scientist = 1;
                HeavyScientist = 1;
                Kill = 2;
                Death = -1;
                Heli = 30;
                LockCrate = 5;
                BradleyCrate = 5;
                HeliCrate = 5;
                MilCrate = 5;
                EliteCrate = 5;
                Airdrop = 5;
            }
        }

        public class HSize
        {
            public double AnchoredXMin;
            public double AnchoredXMax;
            public double AnchoredYMin = 0.830;
            public double AnchoredYMax = 0.960;
            public string AnchoredMin = "";
            public string AnchoredMax = "";
            public HSize(double AnchoredXMin, double AnchoredXMax) {
                this.AnchoredMin = $"{AnchoredXMin} {AnchoredYMin}";
                this.AnchoredMax = $"{AnchoredXMax} {AnchoredYMax}";
                this.AnchoredXMin = AnchoredXMin;
                this.AnchoredXMax = AnchoredXMax;
            }
        }

        public class PlayerScoreboard
        {
            public string clanTag;
            public ulong playerId;
            public int Kills;
            public int Deaths;
            public int Revives;
            public int Downs;
            public int Helis;
            public int Scientist;
            public int HTNScientist;
            public int Bradley;
            public int TC;
            public int BradleyCrates;
            public int HeliCrates;
            public int LockCrates;
            public int MilCrates;
            public int EliteCrates;
            public int Airdrops;

            public PlayerScoreboard() {
            }

            public PlayerScoreboard(string clanTag, ulong playerId, int Kills, int Deaths, int Revives, int Downs, int Helis, int Bradley, int Scientist, int HTNScientist, int TC, int LockCrates, int BradleyCrates, int HeliCrates, int MilCrates, int EliteCrates, int Airdrops) {
                this.clanTag = clanTag;
                this.playerId = playerId;
                this.Kills = Kills;
                this.Deaths = Deaths;
                this.Revives = Revives;
                this.Downs = Downs;
                this.Helis = Helis;
                this.Scientist = Scientist;
                this.HTNScientist = HTNScientist;
                this.Bradley = Bradley;
                this.TC = TC;
                this.LockCrates = LockCrates;
                this.MilCrates = MilCrates;
                this.EliteCrates = EliteCrates;
                this.Airdrops = Airdrops;
                this.BradleyCrates = BradleyCrates;
                this.HeliCrates = HeliCrates;
            }

            public PlayerScoreboard(ulong id, string tag) {
                playerId = id;
                clanTag = tag;
                Kills = 0;
                Deaths = 0;
                Revives = 0;
                Downs = 0;
                Helis = 0;
                Scientist = 0;
                HTNScientist = 0;
                Bradley = 0;
                TC = 0;
                BradleyCrates = 0;
                HeliCrates = 0;
                LockCrates = 0;
                MilCrates = 0;
                EliteCrates = 0;
                Airdrops = 0;
            }

            public double kdr() {
                return (Kills > 0 && Deaths == 0) ? Kills : Math.Round((double)Kills / (double)Deaths, 1);
            }

            public double score() {
                int score = 0;
                score += Kills * config.points.Kill;
                score += Deaths * config.points.Death;
                score += Helis * config.points.Heli;
                score += Scientist * config.points.Scientist;
                score += HTNScientist * config.points.HeavyScientist;
                score += Bradley * config.points.Bradley;
                score += LockCrates * config.points.LockCrate;
                score += HeliCrates * config.points.HeliCrate;
                score += BradleyCrates * config.points.BradleyCrate;
                score += MilCrates * config.points.MilCrate;
                score += EliteCrates * config.points.EliteCrate;
                score += Airdrops * config.points.Airdrop;
                score += TC * config.points.TC;
                return score;
            }

            public void Reset() {
                Kills = 0;
                Deaths = 0;
                Revives = 0;
                Downs = 0;
                Helis = 0;
                Scientist = 0;
                HTNScientist = 0;
                Bradley = 0;
                TC = 0;
                BradleyCrates = 0;
                HeliCrates = 0;
                LockCrates = 0;
                MilCrates = 0;
                EliteCrates = 0;
                Airdrops = 0;
            }
        }

        public class ClanScoreboard
        {
            public string clanTag;
            public int Kills;
            public int Deaths;
            public int Revives;
            public int Downs;
            public int Helis;
            public int Scientist;
            public int HTNScientist;
            public int Bradley;
            public int TC;
            public int BradleyCrates;
            public int HeliCrates;
            public int LockCrates;
            public int MilCrates;
            public int EliteCrates;
            public int Airdrops;

            public ClanScoreboard() {
            }

            public ClanScoreboard(string tag, int Kills, int Deaths, int Revives, int Downs, int Helis, int Bradley, int Scientist, int HTNScientist, int TC, int LockCrates, int BradleyCrates, int HeliCrates, int MilCrates, int EliteCrates, int Airdrops) {
                clanTag = tag;
                this.Kills = Kills;
                this.Deaths = Deaths;
                this.Revives = Revives;
                this.Downs = Downs;
                this.Helis = Helis;
                this.Scientist = Scientist;
                this.HTNScientist = HTNScientist;
                this.Bradley = Bradley;
                this.TC = TC;
                this.LockCrates = LockCrates;
                this.MilCrates = MilCrates;
                this.EliteCrates = EliteCrates;
                this.Airdrops = Airdrops;
                this.BradleyCrates = BradleyCrates;
                this.HeliCrates = HeliCrates;
            }
            public ClanScoreboard(string tag) {
                clanTag = tag;
                Kills = 0;
                Deaths = 0;
                Revives = 0;
                Downs = 0;
                Helis = 0;
                Scientist = 0;
                HTNScientist = 0;
                Bradley = 0;
                TC = 0;
                BradleyCrates = 0;
                HeliCrates = 0;
                LockCrates = 0;
                MilCrates = 0;
                EliteCrates = 0;
                Airdrops = 0;

            }

            public double kdr() {
                return (Kills > 0 && Deaths == 0) ? Kills : Math.Round((double)Kills / (double)Deaths, 1);
            }

            public double score() {
                int score = 0;
                score += Kills * config.points.Kill;
                score += Deaths * config.points.Death;
                score += Helis * config.points.Heli;
                score += Scientist * config.points.Scientist;
                score += HTNScientist * config.points.HeavyScientist;
                score += Bradley * config.points.Bradley;
                score += LockCrates * config.points.LockCrate;
                score += MilCrates * config.points.MilCrate;
                score += EliteCrates * config.points.EliteCrate;
                score += Airdrops * config.points.Airdrop;
                score += TC * config.points.TC;
                return score;
            }

            public void Reset() {
                Kills = 0;
                Deaths = 0;
                Revives = 0;
                Downs = 0;
                Helis = 0;
                Scientist = 0;
                HTNScientist = 0;
                Bradley = 0;
                TC = 0;
                BradleyCrates = 0;
                HeliCrates = 0;
                LockCrates = 0;
                MilCrates = 0;
                EliteCrates = 0;
                Airdrops = 0;
            }
        }
        #endregion

        #region Helpers

        void DestroyUI(BasePlayer player) {
            if (openPanels.ContainsKey(player.userID)) {
                try {
                    CuiHelper.DestroyUi(player, mainPanelClanName);
                    CuiHelper.DestroyUi(player, mainPanelPlayersName);
                } catch (Exception e) {
                }
                openPanels.Remove(player.userID);
            }
        }

        #endregion

        static List<string> clans = new List<string>();

        #region UI

        string mainElementClan;
        string mainElementPlayers;
        string mainPanelClanName = "mainPanelClans";
        string mainPanelPlayersName = "mainPanelPlayers";

        [ConsoleCommand("top")]
        void cmdConsoleTop(ConsoleSystem.Arg arg) {
            bool sortByScore = arg.Args.Length > 0 ? arg.Args[0].Contains("s") : false;
            List<ClanScoreboard> cScoreSorted = _clansScores.Values.OrderByDescending(x => sortByScore ? x.score() : x.kdr()).Take(10).ToList();
            List<PlayerScoreboard> pScoreSorted = _playerScores.Values.OrderByDescending(x => sortByScore ? x.score() : x.kdr()).Take(10).ToList();
            StringBuilder str = new StringBuilder();
            str.Append("Top players are\n");
            foreach (PlayerScoreboard psc in pScoreSorted) {
                str.Append($"{psc.playerId}\n");
            }
            str.Append("\nTop clans are\n");
            foreach (ClanScoreboard csc in cScoreSorted) {
                str.Append($"{csc.clanTag}\n");
            }
        }


        [ChatCommand("all")]
        private void cmdCuiAll(BasePlayer player, string command, string[] args) {
            List<ClanScoreboard> cScoreSorted = _clansScores.Values.OrderByDescending(x => x.score()).ToList();
            List<PlayerScoreboard> pScoreSorted = _playerScores.Values.OrderByDescending(x => x.score()).ToList();
            Puts("Clan\tKills\tDeath\tScore");
            foreach (ClanScoreboard c in cScoreSorted) {
                Puts($"{c.clanTag}\t{c.Kills}\t\t{c.Deaths}\t\t{c.score()}");
            }
            Puts("Player\tKills\tDeath\tScore");
            foreach (PlayerScoreboard c in pScoreSorted) {
                Puts($"{covalence.Players.FindPlayer(c.playerId.ToString()).Name}\t{c.Kills}\t\t{c.Deaths}\t\t{c.score()}");
            }
        }


        [ChatCommand("top")]
        private void cmdCui(BasePlayer player, string command, string[] args) {
            bool isExpanded = false;
            bool sortByScore = false;

            if (config.tournamentMode) {
                isExpanded = true;
                sortByScore = true;
            }

            if (args.Length > 0) {
                if (args[0].Contains("e")) {
                    isExpanded = true;
                }
                if (args[0].Contains("s")) {
                    sortByScore = true;
                }
            }

            CuiElementContainer container = new CuiElementContainer();
            List<ClanScoreboard> cScoreSorted = _clansScores.Values.OrderByDescending(x => sortByScore ? x.score() : x.kdr()).Take(10).ToList();
            List<PlayerScoreboard> pScoreSorted = _playerScores.Values.OrderByDescending(x => sortByScore ? x.score() : x.kdr()).Take(10).ToList();

            string PanelAnchorMin = isExpanded ? "0.700 0.650" : "0.800 0.650";
            string PanelAnchorMax = isExpanded ? "0.995 0.960" : "0.995 0.960"; //left digit is viewheight from left, right digit is Y axis, viewheight from bottom

            Dictionary<string, HSize> headerSizes = new Dictionary<string, HSize>();
            headerSizes.TryAdd("#", isExpanded ? new HSize(0.05, 0.08) : new HSize(0.05, 0.12));
            headerSizes.TryAdd("Clan", isExpanded ? new HSize(0.12, 0.37) : new HSize(0.15, 0.46));

            if (isExpanded) {
                headerSizes.TryAdd("H", new HSize(0.39, 0.44));
                headerSizes.TryAdd("B", new HSize(0.46, 0.51));
                headerSizes.TryAdd("S", new HSize(0.53, 0.58));
            }

            headerSizes.TryAdd("K", isExpanded ? new HSize(0.60, 0.65) : new HSize(0.49, 0.64));
            headerSizes.TryAdd("D", isExpanded ? new HSize(0.67, 0.72) : new HSize(0.67, 0.80));
            headerSizes.TryAdd("KDR", isExpanded ? new HSize(0.74, 0.81) : new HSize(0.83, 1.00));

            if (isExpanded) {
                headerSizes.TryAdd("TC", new HSize(0.83, 0.88));
                headerSizes.TryAdd("Score", new HSize(0.90, 1.00));
            }

            if (cScoreSorted.Count > 0) {

                // headers
                mainElementClan = container.Add(new CuiPanel {
                    Image = { Color = "0 0 0 0.5" },
                    RectTransform = { AnchorMin = PanelAnchorMin, AnchorMax = PanelAnchorMax },
                    CursorEnabled = false,
                }, "Hud", mainPanelClanName);

                _UILineNum(mainElementClan);

                AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["#"], 12, mainElementClan, "#");
                AddCuiLabelHeader(container, TextAnchor.MiddleLeft, headerSizes["Clan"], 12, mainElementClan, "Clan");

                if (isExpanded) {
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["H"], 12, mainElementClan, "H");
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["B"], 12, mainElementClan, "B");
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["S"], 12, mainElementClan, "S");
                }

                AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["K"], 12, mainElementClan, "K");
                AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["D"], 12, mainElementClan, "D");
                AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["KDR"], 12, mainElementClan, "KDR");

                if (isExpanded) {
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["TC"], 12, mainElementClan, "TC");
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["Score"], 12, mainElementClan, "Score");
                }

                // rows
                for (var i = 0; i < cScoreSorted.Count; i++) {
                    ClanScoreboard team = cScoreSorted[i];
                    string clan = team.clanTag;
                    bool clanIsNotElim = config.tournamentMode ? clan != null && _tcs.ContainsKey(clan) && _tcs[clan] >= 1 : true;
                    string textColor = clanIsNotElim ? "" : "1 0.0 0.1 1";


                    AddCuiLabelVert(container, TextAnchor.MiddleLeft, headerSizes["#"], 12, mainElementClan, (i + 1).ToString(), false, textColor);
                    AddCuiLabelVert(container, TextAnchor.MiddleLeft, headerSizes["Clan"], 12, mainElementClan, team.clanTag, false, textColor);

                    if (isExpanded) {
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["H"], 12, mainElementClan, team.Helis.ToString(), false, textColor);
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["B"], 12, mainElementClan, team.Bradley.ToString(), false, textColor);
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["S"], 12, mainElementClan, team.Scientist.ToString(), false, textColor);
                    }

                    AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["K"], 12, mainElementClan, team.Kills.ToString(), false, textColor);
                    AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["D"], 12, mainElementClan, team.Deaths.ToString(), false, textColor);
                    AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["KDR"], 12, mainElementClan, team.kdr().ToString(), !isExpanded, textColor);

                    if (isExpanded) {
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["TC"], 12, mainElementClan, team.TC.ToString(), false, textColor);
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["Score"], 12, mainElementClan, team.score().ToString(), true, textColor);
                    }

                }
            }
            ////////////////////////////////////////////////////////////////////////////
            //Puts(pScoreSorted.Count.ToString());
            if (pScoreSorted.Count > 0) {

                string PPanelAnchorMin = isExpanded ? "0.700 0.330" : "0.800 0.330";// "0.700 0.650" : "0.800 0.650";
                string PPanelAnchorMax = isExpanded ? "0.995 0.640" : "0.995 0.640";//"0.995 0.960" : "0.995 0.960"; //left digit is viewheight from left, right digit is Y axis, viewheight from bottom

                mainElementPlayers = container.Add(new CuiPanel {
                    Image = { Color = "0 0 0 0.5" },
                    RectTransform = { AnchorMin = PPanelAnchorMin, AnchorMax = PPanelAnchorMax },
                    CursorEnabled = false,
                }, "Hud", mainPanelPlayersName);

                _UILineNum(mainElementPlayers);
                AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["#"], 12, mainElementPlayers, "#");
                AddCuiLabelHeader(container, TextAnchor.MiddleLeft, headerSizes["Clan"], 12, mainElementPlayers, "Player");

                if (isExpanded) {
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["H"], 12, mainElementPlayers, "H");
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["B"], 12, mainElementPlayers, "B");
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["S"], 12, mainElementPlayers, "S");
                }

                AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["K"], 12, mainElementPlayers, "K");
                AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["D"], 12, mainElementPlayers, "D");
                AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["KDR"], 12, mainElementPlayers, "KDR");

                if (isExpanded) {
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["TC"], 12, mainElementPlayers, "TC");
                    AddCuiLabelHeader(container, TextAnchor.MiddleCenter, headerSizes["Score"], 12, mainElementPlayers, "Score");
                }

                // rows
                for (var i = 0; i < pScoreSorted.Count; i++) {
                    PlayerScoreboard plyr = pScoreSorted[i];
                    string clan = plyr.clanTag;
                    bool clanIsNotElim = config.tournamentMode ? clan != null && _tcs.ContainsKey(clan) && _tcs[clan] >= 1 : true;
                    string textColor = clanIsNotElim ? "" : "1 0.0 0.1 1";
                    IPlayer fplayer = covalence.Players.FindPlayerById(plyr.playerId.ToString());

                    AddCuiLabelVert(container, TextAnchor.MiddleLeft, headerSizes["#"], 12, mainElementPlayers, (i + 1).ToString(), false, textColor);
                    AddCuiLabelVert(container, TextAnchor.MiddleLeft, headerSizes["Clan"], 12, mainElementPlayers, fplayer != null ? fplayer.Name : plyr.playerId.ToString(), false, textColor);

                    if (isExpanded) {
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["H"], 12, mainElementPlayers, plyr.Helis.ToString(), false, textColor);
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["B"], 12, mainElementPlayers, plyr.Bradley.ToString(), false, textColor);
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["S"], 12, mainElementPlayers, plyr.Scientist.ToString(), false, textColor);
                    }

                    AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["K"], 12, mainElementPlayers, plyr.Kills.ToString(), false, textColor);
                    AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["D"], 12, mainElementPlayers, plyr.Deaths.ToString(), false, textColor);
                    AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["KDR"], 12, mainElementPlayers, plyr.kdr().ToString(), !isExpanded, textColor);

                    if (isExpanded) {
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["TC"], 12, mainElementPlayers, plyr.TC.ToString(), false, textColor);
                        AddCuiLabelVert(container, TextAnchor.MiddleCenter, headerSizes["Score"], 12, mainElementPlayers, plyr.score().ToString(), true, textColor);
                    }
                }
            }
            Puts("Total count " + (cScoreSorted.Count + pScoreSorted.Count));
            if ((cScoreSorted.Count + pScoreSorted.Count) > 0 && !openPanels.ContainsKey(player.userID)) {
                openPanels.TryAdd(player.userID, "open");
                CuiHelper.AddUi(player, container);
                timer.Once(10, () => DestroyUI(player));
            }
        }

        void _UILineNum(string panel) {
            if (!lineNum.ContainsKey(panel)) lineNum.TryAdd(panel, 0);
            else lineNum[panel] = 0;
        }

        void AddCuiLabelHeader(CuiElementContainer container, TextAnchor alignm, HSize hSize, int fontSize, string ElementName, string pText) {
            //Puts($"{hSize.AnchoredXMin} {hSize.AnchoredXMax}");
            container.Add(new CuiLabel {
                Text = { Text = pText, FontSize = 12, Align = alignm },
                RectTransform = { AnchorMin = $"{hSize.AnchoredXMin} {hSize.AnchoredYMin}", AnchorMax = $"{hSize.AnchoredXMax} {hSize.AnchoredYMax}" }
            }, ElementName);
        }

        void AddCuiLabelVert(CuiElementContainer container, TextAnchor alignm, HSize hSize, int fontSize, string ElementName, string pText, bool last = false, string color = "") {
            double height = 0.075;
            double xMin = hSize.AnchoredYMin - (height * (lineNum[ElementName] + 1));
            double xMax = hSize.AnchoredYMin - (height * (lineNum[ElementName]));
            container.Add(new CuiLabel {
                Text = { Text = pText, FontSize = 12, Align = alignm, Color = color },
                RectTransform = {
                    AnchorMin = $"{hSize.AnchoredXMin} {xMin}",
                    AnchorMax = $"{hSize.AnchoredXMax} {xMax}" },
            }, ElementName);

            if (last) lineNum[ElementName]++;
        }
        #endregion

        #region Config
        private static ConfigData config = new ConfigData();
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Points")]
            public Points points = new Points();
            [JsonProperty(PropertyName = "Discord Hooks")]
            public List<string> discordHooks = new List<string>();
            [JsonProperty(PropertyName = "Tournament Mode")]
            public bool tournamentMode = false;
            [JsonProperty(PropertyName = "Max TC Per Clan")]
            public int maxTcs = 1;
        }
        protected override void LoadConfig() {
            base.LoadConfig();

            try {
                config = Config.ReadObject<ConfigData>();
                if (config == null) {
                    LoadDefaultConfig();
                }
            } catch {
                for (var i = 0; i < 3; i++) {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }

                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private static void ValidateConfig() {

        }

        protected override void LoadDefaultConfig() {
            config = new ConfigData();
        }

        protected override void SaveConfig() {
            Config.WriteObject(config);
        }
        #endregion
    }
}
