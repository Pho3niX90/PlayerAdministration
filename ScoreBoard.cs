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

namespace Oxide.Plugins {
    [Info("King Score Board", "Pho3niX90", "0.0.3")]
    [Description("Scoring System")]
    public class ScoreBoard : RustPlugin {
        [PluginReference]
        private Plugin Clans;

        new Dictionary<string, ClanScoreboard> _clansScores;
        new Dictionary<ulong, PlayerScoreboard> _playerScores;
        new Dictionary<ulong, string> openPanels;
        new Dictionary<ulong, HitInfo> _lastWounded;

        void Loaded() {
            _clansScores = new Dictionary<string, ClanScoreboard>();
            _playerScores = new Dictionary<ulong, PlayerScoreboard>();
            openPanels = new Dictionary<ulong, string>();
            _lastWounded = new Dictionary<ulong, HitInfo>();

            LoadData();

            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                try {
                    InitPlayer(player.userID);
                    string playerClan = GetClan(player.userID);
                    if (playerClan != null && playerClan.Length > 0) InitClan(playerClan);
                } catch (Exception e) {

                }
            }
        }

        void Unload() {

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) {
                try {
                    CuiHelper.DestroyUi(BasePlayer.activePlayerList[i], mainPanelClanName);
                    CuiHelper.DestroyUi(BasePlayer.activePlayerList[i], mainPanelPlayersName);
                } catch (Exception e) {

                }
            }

            SaveData();

            _playerScores.Clear();
            _clansScores.Clear();
        }

        #region Hooks
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
        }

        void OnPlayerConnected(BasePlayer player) {
            InitPlayer(player.userID);
            string clanTag = GetClan(player.userID);
            InitClan(clanTag);
        }

        private void OnEntityTakeDamage(BaseCombatEntity victimEntity, HitInfo hitInfo) {
            if (!(victimEntity is BasePlayer) || victimEntity is NPCPlayer) return;
            ulong userId = victimEntity.ToPlayer().userID;

            if (_lastWounded.ContainsKey(userId)) {
                _lastWounded[userId] = hitInfo;
            } else {
                _lastWounded.Add(userId, hitInfo);
            }
        }

        object OnPlayerDeath(BasePlayer victim, HitInfo info) {
            if (victim == null || !(victim is BasePlayer) || victim is NPCPlayer) return null;

            BasePlayer attacker = info?.Initiator?.ToPlayer();

            if (attacker != null && attacker is BasePlayer && !(attacker is NPCPlayer)) {
                try {
                    AddKill(attacker.userID);
                } catch (Exception e) {
                    Puts("AddKill error");
                }
            } else if (_lastWounded.ContainsKey(victim.userID)) {
                HitInfo hitInfo = _lastWounded[victim.userID];
                if (hitInfo?.InitiatorPlayer != null && hitInfo?.InitiatorPlayer is BasePlayer && !(hitInfo?.InitiatorPlayer is NPCPlayer)) {
                    AddKill(hitInfo.InitiatorPlayer.userID);
                    _lastWounded.Remove(victim.userID);
                }
            }

            if (victim != null) {
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
        JObject GetClan(string tag) {
            return Clans?.Call<JObject>("GetClan", tag);
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
                    _clansScores.Add(clan, newClan);
                }
            }
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
                    _clansScores.Add(clan, newClan);
                }
            }
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
                    _clansScores.Add(clan, newClan);
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
                    _clansScores.Add(clan, newClan);
                }
            }
        }

        void _AddPlayerKill(ulong playerId) {
            if (_playerScores.ContainsKey(playerId)) {
                _playerScores[playerId].Kills++;
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
                _playerScores.Add(playerId, pScore);
            }
            InitClan(playerClan);
        }

        void InitClan(string clanTag) {
            if (clanTag != null && !_clansScores.ContainsKey(clanTag)) {
                ClanScoreboard cScore = new ClanScoreboard(clanTag);
                _clansScores.Add(clanTag, cScore);
            }
        }

        ClanScoreboard GetClanStats(ulong playerId) => _clansScores[GetClan(playerId)];
        int GetClanKills(string clan) => _clansScores[clan].Kills;
        int GetClanDeaths(string clan) => _clansScores[clan].Deaths;
        int GetClanRevives(string clan) => _clansScores[clan].Revives;
        int GetClanDowns(string clan) => _clansScores[clan].Downs;
        int GetClanKills(ulong playerId) => GetClan(playerId) == null || GetClan(playerId).Length == 0 ? 0 : _clansScores[GetClan(playerId)].Kills;
        int GetClanDeaths(ulong playerId) => _clansScores[GetClan(playerId)].Deaths;
        int GetClanRevives(ulong playerId) => _clansScores[GetClan(playerId)].Revives;
        int GetClanDowns(ulong playerId) => _clansScores[GetClan(playerId)].Downs;

        PlayerScoreboard GetPlayerStats(ulong playerId) => _playerScores[playerId];
        int GetPlayerKills(ulong playerId) => _playerScores[playerId].Kills;
        int GetPlayerDeaths(ulong playerId) => _playerScores[playerId].Deaths;
        int GetPlayerRevives(ulong playerId) => _playerScores[playerId].Revives;
        int GetPlayerDowns(ulong playerId) => _playerScores[playerId].Downs;


        #region Helpers

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
            if (arg.IsAdmin || arg.IsServerside)
                Puts(GetClan(arg.Args[0]).ToString());
        }
        #endregion

        #region Data
        void SaveData() {
            try {
                Interface.Oxide.DataFileSystem.WriteObject<Dictionary<ulong, PlayerScoreboard>>($"ScoreBoard/allPlayers", _playerScores, true);
            } catch (Exception e) {

            }
            try {
                Interface.Oxide.DataFileSystem.WriteObject<Dictionary<string, ClanScoreboard>>($"ScoreBoard/allClans", _clansScores, true);
            } catch (Exception e) {

            }
        }

        void LoadData(bool reset = false) {
            try {
                _playerScores = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerScoreboard>>($"ScoreBoard/allPlayers");
            } catch (Exception e) {

            }

            try {
                _clansScores = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, ClanScoreboard>>($"ScoreBoard/allClans");
            } catch (Exception e) {

            }

            List<string> clansToRemove = new List<string>();
            foreach (ClanScoreboard c in _clansScores.Values) {
                JObject clan = GetClan(c.clanTag);
                if (clan == null) clansToRemove.Add(c.clanTag);
            }

            timer.Once(2f, () => {
                foreach (string clan in clansToRemove)
                    _clansScores.Remove(clan);

                clansToRemove.Clear();
            });

            SaveData();
        }
        #endregion

        #region Classes
        public class PlayerScoreboard {
            public string clanTag;
            public ulong playerId;
            public int Kills;
            public int Deaths;
            public int Revives;
            public int Downs;

            public PlayerScoreboard() {
            }
            public PlayerScoreboard(string clanTag, ulong playerId, int Kills, int Deaths, int Revives, int Downs) {
                this.clanTag = clanTag;
                this.playerId = playerId;
                this.Kills = Kills;
                this.Deaths = Deaths;
                this.Revives = Revives;
                this.Downs = Downs;
            }
            public PlayerScoreboard(ulong id, string clanTag) {
                playerId = id;
                Kills = 0;
                Deaths = 0;
                Revives = 0;
                Downs = 0;
            }

            public double kdr() {
                return (Kills > 0 && Deaths == 0) ? Kills : Math.Round((double)Kills / (double)Deaths, 1);
            }

            public void Reset() {
                Kills = 0;
                Deaths = 0;
                Revives = 0;
                Downs = 0;
            }
        }
        public class ClanScoreboard {
            public string clanTag;
            public int Kills;
            public int Deaths;
            public int Revives;
            public int Downs;

            public ClanScoreboard() {
            }
            public ClanScoreboard(string tag, int Kills, int Deaths, int Revives, int Downs) {
                clanTag = tag;
                this.Kills = Kills;
                this.Deaths = Deaths;
                this.Revives = Revives;
                this.Downs = Downs;
            }
            public ClanScoreboard(string tag) {
                clanTag = tag;
                Kills = 0;
                Deaths = 0;
                Revives = 0;
                Downs = 0;
            }

            public double kdr() {
                return (Kills > 0 && Deaths == 0) ? Kills : Math.Round((double)Kills / (double)Deaths, 1);
            }

            public void Reset() {
                Kills = 0;
                Deaths = 0;
                Revives = 0;
                Downs = 0;
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
            List<ClanScoreboard> cScoreSorted = _clansScores.Values.OrderByDescending(x => x.kdr()).Take(10).ToList();
            List<PlayerScoreboard> pScoreSorted = _playerScores.Values.OrderByDescending(x => x.kdr()).Take(10).ToList();
            StringBuilder str = new StringBuilder();
            str.Append("Top players are\n");
            foreach (PlayerScoreboard psc in pScoreSorted) {
                str.Append($"{psc.playerId}\n");
            }
            str.Append("\nTop clans are\n");
            foreach (ClanScoreboard csc in cScoreSorted) {
                str.Append($"{csc.clanTag}\n");
            }

            Puts(str.ToString());
        }
        [ChatCommand("top")]
        private void cmdCui(BasePlayer player, string command, string[] args) {

            CuiElementContainer container = new CuiElementContainer();
            List<ClanScoreboard> cScoreSorted = _clansScores.Values.OrderByDescending(x => x.kdr()).Take(10).ToList();
            List<PlayerScoreboard> pScoreSorted = _playerScores.Values.OrderByDescending(x => x.kdr()).Take(10).ToList();
            if (cScoreSorted.Count > 0) {
                mainElementClan = container.Add(new CuiPanel {
                    Image =
                    {
                    Color = "0 0 0 0.5"
                },
                    RectTransform =
                    {
                    AnchorMin = "0.800 0.650",
                    AnchorMax = "0.995 0.960" //left digit is viewheight from left, right digit is Y axis, viewheight from bottom
                },
                    CursorEnabled = false,
                }, "Hud", mainPanelClanName);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "#",
                        FontSize = 15,
                        Align = TextAnchor.MiddleLeft
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.05 0.80",
                        AnchorMax = $"0.12 0.95"
                    }
                }, mainElementClan);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "Clan",
                        FontSize = 15,
                        Align = TextAnchor.MiddleLeft
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.15 0.80",
                        AnchorMax = $"0.46 0.95"
                    }
                }, mainElementClan);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "Kills",
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.49 0.80",
                        AnchorMax = $"0.64 0.95"
                    }
                }, mainElementClan);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "Deaths",
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.67 0.80",
                        AnchorMax = $"0.85 0.95"
                    }
                }, mainElementClan);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "KDR",
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.85 0.80",
                        AnchorMax = $"1.00 0.95"
                    }
                }, mainElementClan);


                for (var i = 0; i < cScoreSorted.Count; i++) {
                    ClanScoreboard team = cScoreSorted[i];

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = (i+1).ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.05 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"0.12 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementClan);

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = team.clanTag,
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.15 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"0.30 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementClan);

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = team.Kills.ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.49 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"0.64 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementClan);

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = team.Deaths.ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.67 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"0.80 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementClan);

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = team.kdr().ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.85 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"1.00 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementClan);
                }
            }
            ////////////////////////////////////////////////////////////////////////////
            ///
            if (pScoreSorted.Count > 0) {
                mainElementPlayers = container.Add(new CuiPanel {
                    Image =
                    {
                    Color = "0 0 0 0.5"
                },
                    RectTransform =
                    {
                    AnchorMin = "0.800 0.335",
                    AnchorMax = "0.995 0.645" //left digit is viewheight from left, right digit is Y axis, viewheight from bottom
                },
                    CursorEnabled = false,
                }, "Hud", mainPanelPlayersName);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "#",
                        FontSize = 15,
                        Align = TextAnchor.MiddleLeft
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.05 0.80",
                        AnchorMax = $"0.12 0.95"
                    }
                }, mainElementPlayers);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "Players",
                        FontSize = 15,
                        Align = TextAnchor.MiddleLeft
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.15 0.80",
                        AnchorMax = $"0.46 0.95"
                    }
                }, mainElementPlayers);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "Kills",
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.49 0.80",
                        AnchorMax = $"0.64 0.95"
                    }
                }, mainElementPlayers);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "Deaths",
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.67 0.80",
                        AnchorMax = $"0.85 0.95"
                    }
                }, mainElementPlayers);

                container.Add(new CuiLabel {
                    Text =
                    {
                        Text = "KDR",
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.85 0.80",
                        AnchorMax = $"1.00 0.95"
                    }
                }, mainElementPlayers);


                for (var i = 0; i < pScoreSorted.Count; i++) {
                    PlayerScoreboard plyr = pScoreSorted[i];
                    IPlayer fplayer = covalence.Players.FindPlayerById(plyr.playerId.ToString());

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = (i+1).ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.05 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"0.12 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementPlayers);
                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = fplayer != null ? fplayer?.Name : plyr.playerId.ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.15 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"0.40 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementPlayers);

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = plyr.Kills.ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.49 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"0.64 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementPlayers);

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = plyr.Deaths.ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.67 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"0.80 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementPlayers);

                    container.Add(new CuiLabel {
                        Text =
                        {
                        Text = plyr.kdr().ToString(),
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter
                    },
                        RectTransform =
                        {
                        AnchorMin = $"0.85 {0.825 - 0.075*(i+1)}",
                        AnchorMax = $"1.00 {0.825 - (0.075*i+0)}"
                    }
                    }, mainElementPlayers);
                }
            }

            if ((cScoreSorted.Count + pScoreSorted.Count) > 0 && !openPanels.ContainsKey(player.userID)) {
                openPanels.Add(player.userID, "open");
                CuiHelper.AddUi(player, container);
                timer.Once(10, () => DestroyUI(player));
            }
        }
        #endregion
    }
}