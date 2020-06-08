using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("Accuracy", "Pho3niX90", "0.0.6")]
    [Description("Accuracy System")]
    public class Accuracy : RustPlugin {
        [PluginReference] Plugin ZoneManager;

        const string PERMISSION_NAME = "accuracy.use";
        new Dictionary<ulong, AccuracyData> _allData;
        AccuracyData _globalData;
        new Dictionary<ulong, string> openPanels;
        Dictionary<string, int> lineNum;

        #region Helpers
        void Init() {
            if (!permission.PermissionExists(PERMISSION_NAME, this)) permission.RegisterPermission(PERMISSION_NAME, this);
        }

        void Loaded() {
            lineNum = new Dictionary<string, int>();
            openPanels = new Dictionary<ulong, string>();
            _allData = new Dictionary<ulong, AccuracyData>();
            _globalData = new AccuracyData();
        }

        void Unload() {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestroyUI(player);
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo info) {
            if (!permission.UserHasPermission(attacker.UserIDString, PERMISSION_NAME)) return;

            attacker.GetActiveItem()._condition = attacker.GetActiveItem()._maxCondition;
            if (!_allData.ContainsKey(attacker.userID)) {
                _allData.Add(attacker.userID, new AccuracyData());
            }

            //Track shots
            _allData[attacker.userID].shots++;
            _globalData.shots++;

            // Track last projectile distance
            int distance = (int)Math.Round(info.ProjectileDistance);
            _globalData.distanceLast = distance;
            _allData[attacker.userID].distanceLast = distance;

            bool isLanded = false;
            bool isHeadshot = false;
            if (!info.boneName.Equals("N/A")) {
                isLanded = true;

                // Track landed shots, and total distance for average calc
                _allData[attacker.userID].shotsLanded++;
                _allData[attacker.userID].distanceTotal += Math.Round(info.ProjectileDistance);
                _globalData.shotsLanded++;
                _globalData.distanceTotal += Math.Round(info.ProjectileDistance);

                // Track body part hits
                switch (info.boneArea) {
                    case HitArea.Head:
                        _allData[attacker.userID].Head++;
                        _globalData.Head++;
                        isHeadshot = true;
                        break;
                    case HitArea.Chest:
                        _allData[attacker.userID].Chest++;
                        _globalData.Chest++;
                        break;
                    case HitArea.Stomach:
                        _allData[attacker.userID].Stomach++;
                        _globalData.Stomach++;
                        break;
                    case HitArea.Arm:
                        _allData[attacker.userID].Arm++;
                        _globalData.Arm++;
                        break;
                    case HitArea.Hand:
                        _allData[attacker.userID].Hand++;
                        _globalData.Hand++;
                        break;
                    case HitArea.Leg:
                        _allData[attacker.userID].Leg++;
                        _globalData.Leg++;
                        break;
                    case HitArea.Foot:
                        _allData[attacker.userID].Foot++;
                        _globalData.Foot++;
                        break;
                }
            }
            // Track projectile distance, along with body part hits
            if (distance <= 29) {
                _allData[attacker.userID].data_25m.shots++;
                _globalData.data_25m.shots++;
                if (isLanded) {
                    _allData[attacker.userID].data_25m.shotsLanded++;
                    _globalData.data_25m.shotsLanded++;
                }
                if (isHeadshot) {
                    _allData[attacker.userID].data_25m.Head++;
                    _globalData.data_25m.Head++;
                }

            } else if (distance > 29 && distance <= 54) { //50m
                _allData[attacker.userID].data_50m.shots++;
                _globalData.data_50m.shots++;
                if (isLanded) {
                    _allData[attacker.userID].data_50m.shotsLanded++;
                    _globalData.data_50m.shotsLanded++;
                }
                if (isHeadshot) {
                    _allData[attacker.userID].data_50m.Head++;
                    _globalData.data_50m.Head++;
                }
            } else if (distance > 54 && distance <= 110) { //100m
                _allData[attacker.userID].data_100m.shots++;
                _globalData.data_100m.shots++;
                if (isLanded) {
                    _allData[attacker.userID].data_100m.shotsLanded++;
                    _globalData.data_100m.shotsLanded++;
                }
                if (isHeadshot) {
                    _allData[attacker.userID].data_100m.Head++;
                    _globalData.data_100m.Head++;
                }
            } else if (distance > 110 && distance <= 154) { //150m
                _allData[attacker.userID].data_150m.shots++;
                _globalData.data_150m.shots++;
                if (isLanded) {
                    _allData[attacker.userID].data_150m.shotsLanded++;
                    _globalData.data_150m.shotsLanded++;
                }
                if (isHeadshot) {
                    _allData[attacker.userID].data_150m.Head++;
                    _globalData.data_150m.Head++;
                }

            } else if (distance > 154 && distance <= 204) { //200m
                _allData[attacker.userID].data_200m.shots++;
                _globalData.data_200m.shots++;
                if (isLanded) {
                    _allData[attacker.userID].data_200m.shotsLanded++;
                    _globalData.data_200m.shotsLanded++;
                }
                if (isHeadshot) {
                    _allData[attacker.userID].data_200m.Head++;
                    _globalData.data_200m.Head++;
                }

            } else if (distance > 204) { //250m +
                _allData[attacker.userID].data_250m.shots++;
                _globalData.data_250m.shots++;
                if (isLanded) {
                    _allData[attacker.userID].data_250m.shotsLanded++;
                    _globalData.data_250m.shotsLanded++;
                }
                if (isHeadshot) {
                    _allData[attacker.userID].data_250m.Head++;
                    _globalData.data_250m.Head++;
                }
            }

            // update UI
            NextTick(() => {
                UpdateUI(attacker);
            });
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("Accuracy_ResetStats")]
        private void cmdUI_ResetStats(ConsoleSystem.Arg arg) {
            var player = arg.Connection.player as BasePlayer;
            cmdStats(player, null, null);
        }
        [ConsoleCommand("Accuracy_ToggleExtraStats")]
        private void cmdUI_ExtraStats(ConsoleSystem.Arg arg) {
            var player = arg.Connection.player as BasePlayer;
            if (_allData.ContainsKey(player.userID)) {
                if (_allData[player.userID].showExtraInfo) DestroyUI(player);
                _allData[player.userID].showExtraInfo = !_allData[player.userID].showExtraInfo;
                UpdateUI(player);
            }
        }
        [ConsoleCommand("Accuracy_ToggleInfiniteAmmo")]
        private void cmdUI_InfiniteAmmo(ConsoleSystem.Arg arg) {
            var player = arg.Connection.player as BasePlayer;
            if (_allData.ContainsKey(player.userID)) {
                _allData[player.userID].infiniteAmmo = !_allData[player.userID].infiniteAmmo;
                UpdateUI(player);
            }
        }
        #endregion

        #region Chat Commands
        [ChatCommand("reset")]
        private void cmdStats(BasePlayer player, string command, string[] args) {
            ResetData(player.userID);
            SendReply(player, "Accuracy data reset");
            UpdateUI(player);
        }
        #endregion

        #region Game Hooks
        void OnPlayerDisconnected(BasePlayer player, string reason) {
            ResetData(player.userID);
        }
        #endregion

        #region Plugin Hooks
        void OnEnterZone(string ZoneID, BasePlayer player) {
            if (permission.UserHasPermission(player.UserIDString, PERMISSION_NAME)) {
                if (!openPanels.ContainsKey(player.userID)) {
                    if (!_allData.ContainsKey(player.userID)) {
                        _allData.Add(player.userID, new AccuracyData());
                    }
                    CreateUI(player);
                }
            } else {
                if (openPanels.ContainsKey(player.userID)) {
                    DestroyUI(player);
                    if (_allData.ContainsKey(player.userID)) {
                        _allData.Remove(player.userID);
                    }
                }
            }
        }
        #endregion

        #region Data
        AccuracyData GetAccData(ulong playerId) {
            AccuracyData data;
            if (_allData.ContainsKey(playerId)) {
                data = _allData[playerId];
            } else {
                data = new AccuracyData();
                _allData.Add(playerId, data);
            }
            return data;
        }

        void ResetData(ulong playerId) {
            AccuracyData data;
            if (_allData.ContainsKey(playerId)) {
                _allData[playerId].Reset();
            }
        }
        #endregion

        #region Classes
        public class AccuracyDataChild {
            public int shots;
            public int shotsLanded;
            public double distanceTotal;
            public int distanceLast;
            public double damageTotal;
            //Area
            public int Head = 0;
            public int Chest = 0;
            public int Stomach = 0;
            public int Arm = 0;
            public int Hand = 0;
            public int Leg = 0;
            public int Foot = 0;

            public int Missed() {
                return shots - shotsLanded;
            }

            public string MissedRate() {
                return Math.Round((1d - ((double)shotsLanded / (double)shots)) * 100) + "%";
            }

            public string Rate(HitArea area) {
                int landed = 0;
                switch (area) {
                    case HitArea.Head:
                        landed = Head;
                        break;
                    case HitArea.Chest:
                        landed = Chest;
                        break;
                    case HitArea.Stomach:
                        landed = Stomach;
                        break;
                    case HitArea.Arm:
                        landed = Arm;
                        break;
                    case HitArea.Hand:
                        landed = Hand;
                        break;
                    case HitArea.Leg:
                        landed = Leg;
                        break;
                    case HitArea.Foot:
                        landed = Foot;
                        break;
                    default:
                        landed = shotsLanded;
                        break;
                }
                return Math.Round(((double)landed / (double)shots) * 100) + "%";
            }

            public string Rate() {
                return Math.Round(((double)shotsLanded / (double)shots) * 100) + "%";
            }
        }
        public class AccuracyData {
            public int shots;
            public int shotsLanded;
            public double distanceTotal;
            public int distanceLast;
            public double damageTotal;
            //Area
            public int Head = 0;
            public int Chest = 0;
            public int Stomach = 0;
            public int Arm = 0;
            public int Hand = 0;
            public int Leg = 0;
            public int Foot = 0;

            public bool showExtraInfo = false;
            public bool infiniteAmmo = false;

            public AccuracyDataChild data_25m;
            public AccuracyDataChild data_50m;
            public AccuracyDataChild data_100m;
            public AccuracyDataChild data_150m;
            public AccuracyDataChild data_200m;
            public AccuracyDataChild data_250m;

            public AccuracyData() {
                distanceTotal = 0;
                distanceLast = 0;
                shots = 0;
                shotsLanded = 0;
                Head = 0;
                Chest = 0;
                Stomach = 0;
                Arm = 0;
                Hand = 0;
                Leg = 0;
                Foot = 0;
                data_25m = new AccuracyDataChild();
                data_50m = new AccuracyDataChild();
                data_100m = new AccuracyDataChild();
                data_150m = new AccuracyDataChild();
                data_200m = new AccuracyDataChild();
                data_250m = new AccuracyDataChild();
            }

            public void Reset() {
                distanceTotal = 0;
                distanceLast = 0;
                shots = 0;
                shotsLanded = 0;
                Head = 0;
                Chest = 0;
                Stomach = 0;
                Arm = 0;
                Hand = 0;
                Leg = 0;
                Foot = 0;
                data_25m = new AccuracyDataChild();
                data_50m = new AccuracyDataChild();
                data_100m = new AccuracyDataChild();
                data_150m = new AccuracyDataChild();
                data_200m = new AccuracyDataChild();
                data_250m = new AccuracyDataChild();
            }

            public string Rate(HitArea area) {
                int landed = 0;
                switch (area) {
                    case HitArea.Head:
                        landed = Head;
                        break;
                    case HitArea.Chest:
                        landed = Chest;
                        break;
                    case HitArea.Stomach:
                        landed = Stomach;
                        break;
                    case HitArea.Arm:
                        landed = Arm;
                        break;
                    case HitArea.Hand:
                        landed = Hand;
                        break;
                    case HitArea.Leg:
                        landed = Leg;
                        break;
                    case HitArea.Foot:
                        landed = Foot;
                        break;
                    default:
                        landed = shotsLanded;
                        break;
                }
                return Math.Round(((double)landed / (double)shots) * 100) + "%";
            }

            public int Missed() {
                return shots - shotsLanded;
            }

            public string MissedRate() {
                return Math.Round((1d - ((double)shotsLanded / (double)shots)) * 100) + "%";
            }

            public string Rate() {
                return Math.Round(((double)shotsLanded / (double)shots) * 100) + "%";
            }

            public double distanceAvg() {
                return Math.Round(distanceTotal / shotsLanded);
            }
        }
        #endregion

        #region Helpers

        #endregion

        static List<string> clans = new List<string>();

        string mainElementAccuracy;
        string mainElementAccuracy25;
        string mainElementAccuracy50;
        string mainElementAccuracy100;
        string mainElementAccuracy150;
        string mainElementAccuracy200;
        string mainElementAccuracy250;
        string mainPanelAccuracyNameHeader = "mainHeaderPanelAccuracy";
        string mainPanelAccuracyNameHeader25 = "mainHeaderPanelAccuracy25";
        string mainPanelAccuracyNameHeader50 = "mainHeaderPanelAccuracy50";
        string mainPanelAccuracyNameHeader100 = "mainHeaderPanelAccuracy100";
        string mainPanelAccuracyNameHeader150 = "mainHeaderPanelAccuracy150";
        string mainPanelAccuracyNameHeader200 = "mainHeaderPanelAccuracy200";
        string mainPanelAccuracyNameHeader250 = "mainHeaderPanelAccuracy250";
        string mainPanelAccuracyName = "mainPanelAccuracy";
        string mainPanelAccuracyName25 = "mainPanelAccuracy25";
        string mainPanelAccuracyName50 = "mainPanelAccuracy50";
        string mainPanelAccuracyName100 = "mainPanelAccuracy100";
        string mainPanelAccuracyName150 = "mainPanelAccuracy150";
        string mainPanelAccuracyName200 = "mainPanelAccuracy200";
        string mainPanelAccuracyName250 = "mainPanelAccuracy250";
        string mainPanelButtons = "mainPanelButtons";

        #region UI
        [ChatCommand("top")]
        private void cmdCui(BasePlayer player, string command, string[] args) {
            CreateUI(player);
        }

        void _UILineNum(string panel) {
            if (!lineNum.ContainsKey(panel)) {
                lineNum.Add(panel, 0);
            } else {
                lineNum[panel] = 0;
            }
        }

        void CreateUI(BasePlayer player) {

            CuiElementContainer containerMain = new CuiElementContainer();
            AccuracyData data = _allData[player.userID];

            #region main panel
            // Headers
            mainElementAccuracy = containerMain.Add(new CuiPanel {
                Image =
                {
                    Color = "0 0 0 0.5"
                },
                RectTransform =
                {
                    AnchorMin = "0.005 0.940",
                    AnchorMax = "0.200 0.960" //left digit is viewheight from left, right digit is Y axis, viewheight from bottom
                },
                CursorEnabled = false,
            }, "Hud", mainPanelAccuracyNameHeader);

            containerMain.Add(new CuiLabel {
                Text =
                                {
                        Text = "Overall Accuracy",
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter
                    },
                RectTransform =
                                {
                        AnchorMin = $"0.05 0.050",
                        AnchorMax = $"0.95 0.950"
                    }
            }, mainElementAccuracy);

            // Panels
            mainElementAccuracy = containerMain.Add(new CuiPanel {
                Image =
                {
                    Color = "0 0 0 0.5"
                },
                RectTransform =
                {
                    AnchorMin = "0.005 0.680",
                    AnchorMax = "0.200 0.937" //left digit is viewheight from left, right digit is Y axis, viewheight from bottom
                },
                CursorEnabled = false,
            }, "Hud", mainPanelAccuracyName);

            _UILineNum(mainElementAccuracy);
            double box1 = 0.070;
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Avg Distance", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.distanceAvg().ToString()}m", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Shots", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.shots.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Landed", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.shotsLanded.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Rate()}", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Missed", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Missed().ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.MissedRate()}", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Head", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Head.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Rate(HitArea.Head)}", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Chest", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Chest.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Rate(HitArea.Chest)}", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Stomach", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Stomach.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Rate(HitArea.Stomach)}", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Arm", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Arm.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Rate(HitArea.Arm)}", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Leg", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Leg.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Rate(HitArea.Leg)}", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Foot", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Foot.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Rate(HitArea.Foot)}", 3, box1);

            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"Hand", 1, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Hand.ToString()}", 2, box1);
            AddCuiLabel(containerMain, 12, mainElementAccuracy, $"{data.Rate(HitArea.Hand)}", 3, box1);
            #endregion

            #region Buttons
            mainElementAccuracy = containerMain.Add(new CuiPanel {
                Image =
                {
                    Color = "0 0 0 0.5"
                },
                RectTransform =
                {
                    AnchorMin = "0.005 0.637",
                    AnchorMax = "0.200 0.677" //left digit is viewheight from left, right digit is Y axis, viewheight from bottom
                },
                CursorEnabled = false,
            }, "Hud", mainPanelButtons);

            CreateButton(ref containerMain, mainElementAccuracy, UIColors["buttongrey"], "Reset", 12, "0.020 0.050", "0.326 0.950", "Accuracy_ResetStats");
            CreateButton(ref containerMain, mainElementAccuracy, ShowExtraStats(player) ? UIColors["buttongreen"] : UIColors["buttonred"], "Extra Stats", 12, "0.346 0.050", "0.672 0.950", "Accuracy_ToggleExtraStats");
            CreateButton(ref containerMain, mainElementAccuracy, InfiniteAmmo(player) ? UIColors["buttongreen"] : UIColors["buttonred"], "Infinite Ammo", 12, "0.692 0.050", "0.980 0.950", "Accuracy_ToggleInfiniteAmmo");

            #endregion

            if (ShowExtraStats(player)) {
                #region 25m panel
                // 25 meters   
                AddExtraPanel(containerMain, 1, 1, "<=25m Accuracy", mainPanelAccuracyNameHeader25, mainPanelAccuracyName25, out mainElementAccuracy25);
                box1 = 0.15;

                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"Shots", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"{data.data_25m.shots.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"Landed", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"{data.data_25m.shotsLanded.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"{data.data_25m.Rate()}", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"Missed", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"{data.data_25m.Missed().ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"{data.data_25m.MissedRate()}", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"Head", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"{data.data_25m.Head.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy25, $"{data.data_25m.Rate(HitArea.Head)}", 3, box1);
                #endregion
                #region 50m panel
                // 25 meters   
                AddExtraPanel(containerMain, 1, 2, "25-50m Accuracy", mainPanelAccuracyNameHeader50, mainPanelAccuracyName50, out mainElementAccuracy50);


                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"Shots", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"{data.data_50m.shots.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"Landed", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"{data.data_50m.shotsLanded.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"{data.data_50m.Rate()}", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"Missed", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"{data.data_50m.Missed().ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"{data.data_50m.MissedRate()}", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"Head", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"{data.data_50m.Head.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy50, $"{data.data_50m.Rate(HitArea.Head)}", 3, box1);
                #endregion
                #region 100m panel
                // 100 meters   
                AddExtraPanel(containerMain, 1, 3, "50-100m Accuracy", mainPanelAccuracyNameHeader100, mainPanelAccuracyName100, out mainElementAccuracy100);


                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"Shots", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"{data.data_100m.shots.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"Landed", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"{data.data_100m.shotsLanded.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"{data.data_100m.Rate()}", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"Missed", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"{data.data_100m.Missed().ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"{data.data_100m.MissedRate()}", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"", 3, box1);

                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"Head", 1, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"{data.data_100m.Head.ToString()}", 2, box1);
                AddCuiLabel(containerMain, 12, mainElementAccuracy100, $"{data.data_100m.Rate(HitArea.Head)}", 3, box1);
                #endregion
            }
            openPanels.Add(player.userID, mainElementAccuracy);

            CuiHelper.AddUi(player, containerMain);
        }
        void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter) {
            container.Add(new CuiButton {
                Button = { Color = color, Command = command, FadeIn = 0f },
                RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                Text = { Text = text, FontSize = size, Align = align }
            },
            panel);
        }
        void AddCuiLabel(CuiElementContainer container, int fontSize, string ElementName, string pText, int col, double height = 0.075) {

            string boxStart = col == 1 ? "0.05" : col == 2 ? "0.49" : "0.73";
            string boxEnd = col == 1 ? "0.47" : col == 2 ? "0.71" : "0.95";

            container.Add(new CuiLabel {
                Text =
                    {
                        Text = pText,
                        FontSize = fontSize,
                        Align = TextAnchor.MiddleLeft
                    },
                RectTransform =
                    {
                        AnchorMin = $"{boxStart} {0.95 - height*(lineNum[ElementName]+1)}",
                        AnchorMax = $"{boxEnd} {0.95 - (height*lineNum[ElementName]+0)}"
                    }
            }, ElementName);
            if (col == 3) {
                lineNum[ElementName]++;
            }
        }
        bool ShowExtraStats(BasePlayer player) {
            return _allData.ContainsKey(player.userID) && _allData[player.userID].showExtraInfo;
        }
        bool InfiniteAmmo(BasePlayer player) {
            return _allData.ContainsKey(player.userID) && _allData[player.userID].infiniteAmmo;
        }
        void AddExtraPanel(CuiElementContainer container, int col, int row, string headerText, string headerPanel, string contentPanel, out string element) {

            double headerSize = 0.020;
            double panelHeight = 0.120;
            double marginHeader = 0.005;
            double marginPanel = 0.003;

            double panelTotalSize = headerSize + panelHeight + marginHeader + marginPanel;

            double headerEnd = 0.960 - (((row - 1) * panelTotalSize));
            double headerStart = headerEnd - headerSize;

            double panelWidth = 0.195 * col;
            double panelWStart = col == 1 ? 0.205 : ((col - 1) * panelWidth);
            double panelWEnd = panelWStart + panelWidth;///col == 1 ? 0.310 : ((col - 1) * 0.310);


            double panelHEnd = headerStart - marginPanel;// panelHStart - panelHeight;
            double panelHStart = panelHEnd - panelHeight;


            element = container.Add(new CuiPanel {
                Image =
                {
                    Color = "0 0 0 0.5"
                },
                RectTransform =
                {
                    AnchorMin = $"{panelWStart} {headerStart}",
                    AnchorMax = $"{panelWEnd} {headerEnd}" //left digit is viewheight from left, right digit is Y axis, viewheight from bottom
                },
                CursorEnabled = false,
            }, "Hud", headerPanel);

            container.Add(new CuiLabel {
                Text =
                    {
                        Text = headerText,
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter
                    },
                RectTransform =
                    {
                        AnchorMin = $"0.050 0.050",
                        AnchorMax = $"0.950 0.950"
                    }
            }, element);

            element = container.Add(new CuiPanel {
                Image =
                {
                    Color = "0 0 0 0.5"
                },
                RectTransform =
                {
                    AnchorMin = $"{panelWStart} {panelHStart}",
                    AnchorMax = $"{panelWEnd} {panelHEnd}" //left digit is viewheight from left, right digit is Y axis, viewheight from bottom
                },
                CursorEnabled = false,
            }, "Hud", contentPanel);
            _UILineNum(element);
        }

        void UpdateUI(BasePlayer player) {
            DestroyUI(player);
            CreateUI(player);
        }

        void DestroyUI(BasePlayer player) {
            if (openPanels.ContainsKey(player.userID)) {
                try {
                    CuiHelper.DestroyUi(player, mainPanelAccuracyNameHeader);
                    if (ShowExtraStats(player)) {
                        CuiHelper.DestroyUi(player, mainPanelAccuracyNameHeader25);
                        CuiHelper.DestroyUi(player, mainPanelAccuracyNameHeader50);
                        CuiHelper.DestroyUi(player, mainPanelAccuracyNameHeader100);
                    }
                    CuiHelper.DestroyUi(player, mainPanelAccuracyName);
                    if (ShowExtraStats(player)) {
                        CuiHelper.DestroyUi(player, mainPanelAccuracyName25);
                        CuiHelper.DestroyUi(player, mainPanelAccuracyName50);
                        CuiHelper.DestroyUi(player, mainPanelAccuracyName100);
                    }
                    CuiHelper.DestroyUi(player, mainPanelButtons);
                } catch (Exception e) {
                }
                openPanels.Remove(player.userID);
            }
        }
        #endregion

        #region Infinite Ammo
        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player) {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_NAME) || !InfiniteAmmo(player)) return;
            if (projectile.primaryMagazine.contents > 0) return;
            projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
            projectile.SendNetworkUpdateImmediate();
        }
        #endregion

        #region Data
        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"black", "0 0 0 1.0" },
            {"dark", "0.1 0.1 0.1 0.98" },
            {"header", "1 1 1 0.3" },
            {"light", ".564 .564 .564 1.0" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"brown", "0.3 0.16 0.0 1.0" },
            {"yellow", "0.9 0.9 0.0 1.0" },
            {"orange", "1.0 0.65 0.0 1.0" },
            {"limegreen", "0.42 1.0 0 1.0" },
            {"blue", "0.2 0.6 1.0 1.0" },
            {"red", "1.0 0.1 0.1 1.0" },
            {"white", "1 1 1 1" },
            {"green", "0.28 0.82 0.28 1.0" },
            {"grey", "0.85 0.85 0.85 1.0" },
            {"lightblue", "0.6 0.86 1.0 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttongreen", "0.133 0.965 0.133 0.9" },
            {"buttonred", "0.964 0.133 0.133 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" },
        };
        #endregion
    }
}