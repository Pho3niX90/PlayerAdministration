using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Scrim Deathmatch", "Pho3niX90", "1.2.5")]
    [Description("")]
    public class KingGamesDeathmatch : RustPlugin
    {
        [PluginReference] Plugin Spawns;
        //[PluginReference] Plugin ZoneManager;
        #region Vars
        private const int autoBalanceMax = 1;
        private static KingGamesDeathmatch plugin;
        static KingGamesDeathmatch _instance;

        public enum MemberType
        {
            Spectator,
            Playing,
            Destroying
        }

        public class RespawnInfo
        {
            public string spawnFile;
            public string kitName;
            public string kitNameClothing;
        }

        #endregion

        #region Oxide Hooks

        private void Init() {
            plugin = this;
        }

        private void OnServerInitialized() {
            CreateGames();
        }

        private void Unload() {
            RemoveAllGames();
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info) {
            BasePlayer attacker = info?.InitiatorPlayer;
            Puts($"Player {player.displayName} has died. Killed  by {attacker?.displayName}");
            Puts($"Damage {info?.damageTypes}");
            MarkDead(player);
        }
        bool CanBeWounded(BasePlayer player, HitInfo info) {
            Puts($"Can player {player?.displayName} be wounded");
            return true;
        }
        #endregion

        #region Core

        private static void RemoveAllGames() {
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<GameController>()) {
                UnityEngine.Object.DestroyImmediate(entity);
            }

            foreach (var entity in UnityEngine.Object.FindObjectsOfType<GameTeam>()) {
                UnityEngine.Object.DestroyImmediate(entity);
            }

            foreach (var entity in UnityEngine.Object.FindObjectsOfType<GamePlayer>()) {
                UnityEngine.Object.DestroyImmediate(entity);
            }
        }

        private void CreateGames() {
            RemoveAllGames();

            timer.Once(1f, () => {
                foreach (var def in config.definitions) {
                    if (def.enabled == true) {
                        var obj = new GameObject().AddComponent<GameController>();
                        obj.Setup(def);
                    }
                }
            });
        }

        private static void JoinGame(BasePlayer player, string name, bool inFirstTeam = false) {
            foreach (var game in UnityEngine.Object.FindObjectsOfType<GameController>()) {
                if (string.Equals(game.definition.shortname, name, StringComparison.OrdinalIgnoreCase) == true) {
                    game.PlayerJoin(player, inFirstTeam);
                }
            }
        }

        private static void LeaveGame(BasePlayer player) {
            var obj = player?.GetComponent<GamePlayer>();
            if (obj != null) {
                plugin.Puts("Leaving Game " + player.displayName);
                UnityEngine.Object.DestroyImmediate(obj);
            } else {
                plugin.Puts("Player doesnt have gameplayer obj" + player.displayName);
            }
        }

        private static void MarkReady(BasePlayer player, bool ar = false) {
            var obj = player?.GetComponent<GamePlayer>();
            if (obj != null && !obj.isReady) {
                obj.isReady = true;
                plugin.SendAll(obj.team.controller, ar ? Message.BecameReadyAuto : Message.BecameReady, "{name}", player.displayName);
            }
            obj.team.controller.CheckGameStatus();
        }

        private static void MarkDead(BasePlayer player) {
            var obj = player.GetComponent<GamePlayer>();
            if (obj != null) {
                obj.Died();
            }
        }

        private static void CleanArena(string t1Spawn, string t2Spawn) {
            plugin.Puts("Cleaning arena");

            Vector3 t1 = plugin.GetFirstSpawnPoint(plugin.LoadSpawnpoints(t1Spawn));
            Vector3 t2 = plugin.GetFirstSpawnPoint(plugin.LoadSpawnpoints(t2Spawn));

            Vector3 midPoint = (t1 + t2) / 2;
            float distance = Vector3.Distance(t1, t2) + 10;

            var entities = new List<BaseEntity>();
            Vis.Entities(midPoint, distance, entities);

            foreach (var entity in entities) {
                if (entity.IsValid() == true && entity.OwnerID != 0) {
                    plugin.Puts("Killing entity");
                    entity?.Kill();
                }
            }
        }

        private void SendAll(GameController controller, Message key, params object[] args) {
            foreach (var player in controller.allPlayers) {
                RefreshTeamUIPanel(player, controller);
                SendMessage(player.original, key, args);
            }
        }

        #endregion

        #region Scores

        private void OnEntityTakeDamage(BasePlayer player, HitInfo info) {
            if (player != null && !player.displayName.Equals("NPC")) Puts($"Damage done to {player?.displayName}");
            if (info == null || info.damageTypes.Total() < 1) {
                return;
            }

            Puts($"Damage done to {player?.displayName}, by {info?.InitiatorPlayer?.name}, weapon used {info?.WeaponPrefab?.ShortPrefabName}");
            var initiator = info?.InitiatorPlayer?.GetComponent<GamePlayer>();
            if (initiator != null) {
                initiator.damage += Convert.ToInt32(info.damageTypes.Total());
            }
        }

        private void OnEntityDeath(BasePlayer player, HitInfo info) {
            if (info == null) {
                return;
            }

            var initiator = info?.InitiatorPlayer?.GetComponent<GamePlayer>();
            var victim = player.GetComponent<GamePlayer>();

            if (initiator != null) {
                initiator.kills++;
            }

            if (victim != null) {
                victim.death++;
            }
            if (initiator != null && victim != null) {
                // team1 is red ff0000
                // team2 is blue 0000ff
                GamePlayer[] allPlayers = initiator.team.getPlayers.Concat(victim.team.getPlayers).ToArray();
                bool p1 = initiator.team.kitNameClothing.Contains("red");

                string red = "#ff0000";
                string blue = "#0000ff";

                string col1 = p1 ? red : blue;
                string col2 = !p1 ? red : blue;

                foreach (GamePlayer pl in allPlayers) {
                    SendReply(pl.original, $"<color={col1}>{initiator.original.displayName}</color> killed <color={col2}>{victim.original.displayName}</color>");
                }
            }
        }

        #endregion

        #region UI 2.0 General

        private const string elemMain = "KingGamesDeathmatch.UI.Main";
        private CuiRectTransformComponent team1Position = new CuiRectTransformComponent {
            AnchorMin = "0 0.967",
            AnchorMax = "0 0.967",
            OffsetMin = "5 -30",
            OffsetMax = "305 -5"
        };

        private CuiRectTransformComponent team2Position = new CuiRectTransformComponent {
            AnchorMin = "1 0.967",
            AnchorMax = "1 0.967",
            OffsetMin = "-310 -30",
            OffsetMax = "-5 -5"
        };

        private void RefreshTeamUIPanel(GamePlayer player, GameController controller) {
            var container = new CuiElementContainer();

            // Main panel
            container.Add(new CuiElement {
                Name = elemMain,
                //Parent = "Under",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                    //new CuiNeedsCursorComponent()
                }
            });

            AddTeamPanel(container, controller.team1);
            AddTeamPanel(container, controller.team2);

            // Leave button
            container.Add(new CuiButton {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Leave",
                    FontSize = 20,
                },
                Button =
                {
                    Command = "chat.say /leave",
                    Color = "0.5 0.5 0.5 0.7"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0",
                    AnchorMax = "0.5 0",
                    OffsetMin = "200 17",
                    OffsetMax = "300 45"
                }
            }, elemMain);

            if (controller.owner == player) {
                // Settings button
                container.Add(new CuiButton {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "Settings",
                        FontSize = 20,
                    },
                    Button =
                    {
                        Command = "api.deathmatch",
                        Color = "0.5 0.5 0.5 0.7"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0",
                        AnchorMax = "0.5 0",
                        OffsetMin = "305 17",
                        OffsetMax = "405 45"
                    }
                }, elemMain);
            }

            // Change team button
            container.Add(new CuiButton {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Change team",
                    FontSize = 20,
                },
                Button =
                {
                    Command = "deathmatch.changeteam",
                    Color = "0.5 0.5 0.5 0.7"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0",
                    AnchorMax = "0.5 0",
                    OffsetMin = "-365 17",
                    OffsetMax = "-215 45"
                }
            }, elemMain);

            // Owner Name
            container.Add(new CuiElement {
                Parent = elemMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"Owner: {controller.owner?.original?.displayName ?? "No One" }",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0",
                        AnchorMax = "0.5 0",
                        OffsetMin = "200 47",
                        OffsetMax = "400 77"
                    },
                }
            });

            CuiHelper.DestroyUi(player.original, elemMain);
            CuiHelper.AddUi(player.original, container);
        }

        private void AddTeamPanel(CuiElementContainer container, GameTeam team) {
            var teamID = team.teamNumber;
            var values = team.getPlayers;
            var parent = elemMain + ".team." + teamID;
            var transform = teamID == 2 ? team2Position : team1Position;
            var teamChar = teamID == 2 ? "B" : "A";
            string red = "#ff0000";
            string blue = "#0000ff";
            string teamColor = teamID == 2 ? blue : red;

            // Team panel
            container.Add(new CuiElement {
                Name = parent,
                Parent = elemMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.5 0.5 0.5 0.7"
                    },
                    transform,
                }
            });

            container.Add(new CuiElement {
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"   <color={teamColor}>Team {teamChar}</color> ({team.getPlayers.Length:00})  Wins: {team.wins:00}       Kills     Deaths     Damage     Ready",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 12
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    },
                }
            });

            var startY = -20;
            var lastY = startY;
            var sizeY = 15;
            var offsetY = 5;

            foreach (var value in values) {
                // Name
                container.Add(new CuiElement {
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = value.original.displayName,
                            Color = "1 1 1 1",
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 12
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = $"10 {lastY}",
                            OffsetMax = $"120 {lastY + sizeY}"
                        },
                    }
                });

                // Kills
                container.Add(new CuiElement {
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = value.kills.ToString(),
                            Color = "1 1 1 1",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 12
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = $"120 {lastY}",
                            OffsetMax = $"155 {lastY + sizeY}"
                        },
                    }
                });

                // Death
                container.Add(new CuiElement {
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = value.death.ToString(),
                            Color = "1 1 1 1",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 12
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = $"160 {lastY}",
                            OffsetMax = $"200 {lastY + sizeY}"
                        },
                    }
                });

                // Damage
                container.Add(new CuiElement {
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = value.damage.ToString(),
                            Color = "1 1 1 1",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 12
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = $"205 {lastY}",
                            OffsetMax = $"250 {lastY + sizeY}"
                        },
                    }
                });

                // Ready
                container.Add(new CuiElement {
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = value.isReady.ToString(),
                            Color = "1 1 1 1",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 12
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = $"255 {lastY}",
                            OffsetMax = $"290 {lastY + sizeY}"
                        },
                    }
                });

                lastY -= sizeY + offsetY;
            }
        }

        #endregion

        #region UI 2.0 Settings

        private const string elemMainSettings = "test.ui.main";
        private const string elemPanelSettings = "test.ui.subpanel";

        private class ButtonSimple
        {
            public string layer = "Name";
            public string command = "Command";
        }

        private class ButtonComplex : ButtonSimple
        {
            public string textON = "ON";
            public string textOFF = "OFF";
            public bool isOn = true;
            public string colorON = "0 0.8 0 0.8";
            public string colorOFF = "0.8 0 0 0.8";
        }

        private ButtonComplex[] buttonsSettingsGeneral =
        {
            new ButtonComplex // 0
            {
                layer = "ARENA",
                textON = "PUBLIC",
                textOFF = "PRIVATE",
                command = "api.deathmatch visibility arena"
            },
            new ButtonComplex // 1
            {
                layer = "TEAM A",
                textON = "PUBLIC",
                textOFF = "PRIVATE",
                command = "api.deathmatch visibility team1"
            },
            new ButtonComplex // 2
            {
                layer = "TEAM B",
                textON = "PUBLIC",
                textOFF = "PRIVATE",
                command = "api.deathmatch visibility team2"
            },
            new ButtonComplex // 3
            {
                layer = "HS ONLY",
                colorOFF = "0 0.8 0 0.8",
                colorON = "0.8 0 0 0.8",
                command = "api.deathmatch hsonly"
            },
            new ButtonComplex // 4
            {
                layer = "AUTOBALANCE",
                command = "api.deathmatch autobalance"
            },
            new ButtonComplex // 5
            {
                layer = "FRIENDLY FIRE",
                colorOFF = "0 0.8 0 0.8",
                colorON = "0.8 0 0 0.8",
                command = "api.deathmatch friendlyfire"
            },
//            new ButtonComplex // 6
//            {
//                layer = "TIME",
//                textON = "DAY",
//                textOFF = "NIGHT",
//            },
        };

        private ButtonSimple[] buttonsSettingsAdditional =
        {
            new ButtonSimple
            {
                layer = "FORCE START",
                command = "api.deathmatch forcestart"
            },
            new ButtonSimple
            {
                layer = "RESTART ROUND",
                command = "api.deathmatch restartround"
            },
        };

        private ButtonSimple[] buttonsNavigation =
        {
            new ButtonSimple
            {
                layer = "GENERAL"
            },
            new ButtonSimple
            {
                layer = "KITS"
            },
            new ButtonSimple
            {
                layer = "PLAYERS"
            },
        };

        private void UIOpenSettings(GamePlayer player, int type) {
            var container = new CuiElementContainer
            {
                // Main panel
                new CuiElement
                {
                    Name = elemMainSettings,
                    Parent = "Hud.Menu",
                    Components =
                    {
                        new CuiImageComponent {Color = "0 0 0 0.9"},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
                        new CuiNeedsCursorComponent()
                    }
                },
                // Border
                new CuiElement
                {
                    Name = elemMainSettings + ".border",
                    Parent = elemMainSettings,
                    Components =
                    {
                        new CuiImageComponent {Color = "1 0.5 0 1",},
                        new CuiRectTransformComponent {AnchorMin = "0 0.93", AnchorMax = "1 1",},
                    }
                },
                // Sub panel
                new CuiElement
                {
                    Name = elemPanelSettings,
                    Parent = elemMainSettings,
                    Components =
                    {
                        new CuiImageComponent {Color = "0.2 0.2 0.2 0"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.334 0.5",
                            AnchorMax = "0.334 0.5",
                            OffsetMin = "-300 -200",
                            OffsetMax = "300 200"
                        },
                    }
                }
            };

            switch (type) {
                case 1:
                    AddGeneralPage(container, player.team.controller);
                    break;

                case 2:
                    AddKitsPage(container, player.team.controller);
                    break;

                case 3:
                    AddPlayersPage(container, player.team.controller);
                    break;
            }

            // Navigation
            var offsetX = 50;
            var sizeX = 150;
            var sizeY = 50;
            var lastY = 0;
            var lastX = -(buttonsNavigation.Length * (sizeX + offsetX)) / 2;
            var i = 0;
            foreach (var button in buttonsNavigation) {
                // Button
                container.Add(new CuiButton {
                    Text =
                        {
                            Text = button.layer,
                            Color = button.command,
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 25
                        },
                    Button =
                        {
                            Command = $"api.deathmatch {++i}",
                            Color = "1 0.5 0 0"
                        },
                    RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{lastX} {lastY - sizeY / 2}",
                            OffsetMax = $"{lastX + sizeX} {lastY + sizeY / 2}"
                        }
                }, elemMainSettings + ".border");

                lastX += sizeX + offsetX;
            }

            // Close button
            container.Add(new CuiButton {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "X",
                    FontSize = 30,
                },
                Button =
                {
                    Close = elemMainSettings,
                    Color = "1 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "1 1",
                    AnchorMax = "1 1",
                    OffsetMin = "-45 -45",
                    OffsetMax = "-5 -5"
                }
            }, elemMainSettings);

            CuiHelper.DestroyUi(player.original, elemMainSettings);
            CuiHelper.AddUi(player.original, container);
        }

        private void AddGeneralPage(CuiElementContainer container, GameController controller) {
            var startX = 0;
            var startY = 0;
            var offsetX = 5;
            var offsetY = 5;
            var sizeX = 250;
            var sizeY = 50;
            var lastX = startX;
            var lastY = startY;

            var buttons = buttonsSettingsGeneral.ToArray();
            buttons[0].isOn = controller.definition.arenaPublic;
            buttons[1].isOn = controller.definition.team1Public;
            buttons[2].isOn = controller.definition.team2Public;
            buttons[3].isOn = controller.definition.headshotOnly;
            buttons[4].isOn = controller.definition.autoBalance;
            buttons[5].isOn = controller.definition.friendlyFire;

            foreach (var button in buttonsSettingsGeneral) {
                // Name
                container.Add(new CuiButton {
                    Text =
                    {
                        Text = button.layer,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25
                    },
                    Button =
                    {
                        Command = button.command,
                        Color = "1 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = $"{lastX} {lastY - sizeY}",
                        OffsetMax = $"{lastX + sizeX} {lastY}"
                    }
                }, elemPanelSettings);

                lastX += sizeX + offsetX;

                // Button
                container.Add(new CuiButton {
                    Text =
                    {
                        Text = button.isOn ? button.textON : button.textOFF,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25
                    },
                    Button =
                    {
                        Command = button.command,
                        Color = button.isOn ? button.colorON : button.colorOFF,
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = $"{lastX} {lastY - sizeY}",
                        OffsetMax = $"{lastX + sizeX} {lastY}"
                    }
                }, elemPanelSettings);

                lastY -= sizeY + offsetY;
                lastX = startX;
            }

            // Border
            container.Add(new CuiElement {
                Parent = elemPanelSettings,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1 0.5 0 1",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = $"{sizeX * 2 + offsetX * 2} {lastY}",
                        OffsetMax = $"{sizeX * 2 + offsetX * 2 + 5} {startY + 5}"
                    }
                }
            });

            startX = sizeX * 2 + offsetX * 2 + 10;
            startY = 0;
            offsetX = 5;
            offsetY = 5;
            sizeX = 250;
            sizeY = 50;
            lastX = startX;
            lastY = startY;

            foreach (var button in buttonsSettingsAdditional) {
                // Button
                container.Add(new CuiButton {
                    Text =
                    {
                        Text = button.layer,
                        Color = button.command,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25
                    },
                    Button =
                    {
                        Command = button.command,
                        Color = "1 0.5 0 0.5"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = $"{lastX} {lastY - sizeY}",
                        OffsetMax = $"{lastX + sizeX} {lastY}"
                    }
                }, elemPanelSettings);

                lastY -= sizeY + offsetY;
                lastX = startX;
            }

            startX = 50;
            startY = 0;
            offsetX = 50;
            offsetY = 5;
            sizeX = 150;
            sizeY = 50;
            lastX = startX;
            lastY = startY;

            lastX = -(buttonsNavigation.Length * (sizeX + offsetX)) / 2;

            foreach (var button in buttonsNavigation) {
                // Button
                container.Add(new CuiButton {
                    Text =
                        {
                            Text = button.layer,
                            Color = button.command,
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 25
                        },
                    Button =
                        {
                            Command = button.command,
                            Color = "1 0.5 0 0"
                        },
                    RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{lastX} {lastY - sizeY / 2}",
                            OffsetMax = $"{lastX + sizeX} {lastY + sizeY / 2}"
                        }
                }, elemMainSettings + ".border");

                lastX += sizeX + offsetX;
            }
        }

        private void AddKitsPage(CuiElementContainer container, GameController controller) {
            // Border
            container.Add(new CuiElement {
                Parent = elemMainSettings,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1 0.5 0 1",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-3 -300",
                        OffsetMax = "3 250"
                    }
                }
            });

            var startX = -550;
            var startY = 120;
            var offsetX = 5;
            var offsetY = 5;
            var sizeX = 100;
            var sizeY = 25;
            var lastX = startX;
            var lastY = startY;
            var i = 0;

            // Team A
            container.Add(new CuiButton {
                Text =
                    {
                        Text = "Team A: " + controller.team1.kitName,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 25
                    },
                Button =
                    {

                        Color = "1 0.5 0 0"
                    },
                RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{lastX} {lastY + sizeY}",
                        OffsetMax = $"{lastX + sizeX * 4} {lastY + sizeY * 3}"
                    }
            }, elemMainSettings);

            foreach (var name in config.kitsWeapons) {
                if (i >= 50) {
                    break;
                }

                // Button
                container.Add(new CuiButton {
                    Text =
                    {
                        Text = name,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15
                    },
                    Button =
                    {

                        Command = "api.deathmatch kit team1 " + name,
                        Color = "1 0.5 0 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{lastX} {lastY - sizeY / 2}",
                        OffsetMax = $"{lastX + sizeX} {lastY + sizeY / 2}"
                    }
                }, elemMainSettings);

                lastX += sizeX + offsetX;

                if (++i % 5 == 0) {
                    lastY -= sizeY + offsetY;
                    lastX = startX;
                }
            }

            startX = 550;
            startY = 120;
            offsetX = 5;
            offsetY = 5;
            sizeX = 100;
            sizeY = 25;
            lastX = startX;
            lastY = startY;
            i = 0;

            // Team B
            container.Add(new CuiButton {
                Text =
                    {
                        Text = "Team B: " + controller.team2.kitName,
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 25
                    },
                Button =
                    {

                        Color = "1 0.5 0 0"
                    },
                RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{lastX - sizeX * 5 - 20} {lastY + sizeY}",
                        OffsetMax = $"{lastX} {lastY + sizeY * 3}"
                    }
            }, elemMainSettings);

            foreach (var name in config.kitsWeapons) {
                if (i >= 50) {
                    break;
                }

                // Button
                container.Add(new CuiButton {
                    Text =
                    {
                        Text = name,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15
                    },
                    Button =
                    {

                        Command = "api.deathmatch kit team2 " + name,
                        Color = "1 0.5 0 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{lastX - sizeX} {lastY - sizeY / 2}",
                        OffsetMax = $"{lastX} {lastY + sizeY / 2}"
                    }
                }, elemMainSettings);

                lastX -= sizeX + offsetX;

                if (++i % 5 == 0) {
                    lastY -= sizeY + offsetY;
                    lastX = startX;
                }
            }
        }

        private void AddPlayersPage(CuiElementContainer container, GameController controller) {
            var startX = 500;
            var startY = 220;
            var offsetX = 5;
            var offsetY = 5;
            var sizeX = 200;
            var sizeY = 50;
            var lastX = startX;
            var lastY = startY;
            var i = 0;

            foreach (var player in controller.allPlayers) {
                if (i >= 50) {
                    break;
                }

                // Button
                container.Add(new CuiButton {
                    Text =
                    {
                        Text = player.original.displayName,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20
                    },
                    Button =
                    {
                        Command = "api.deathmatch control " + player.original.userID,
                        Color = "1 0.5 0 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{lastX - sizeX} {lastY - sizeY / 2}",
                        OffsetMax = $"{lastX} {lastY + sizeY / 2}"
                    }
                }, elemMainSettings);

                lastX -= sizeX + offsetX;

                if (++i % 5 == 0) {
                    lastY -= sizeY + offsetY;
                    lastX = startX;
                }
            }
        }

        #endregion

        #region UI 2.0 Player Control


        private const string PLAYERCONTROLelemMain = "deathmatch.PLAYERCONTROL.Main";

        private void PLAYERCONTROLShowUI(BasePlayer player, string name = "Orange #1", string userID = "OrangeID") {
            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = PLAYERCONTROLelemMain,
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiImageComponent {Color = "1 1 1 0.2",},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-175 -75",
                            OffsetMax = "175 75"
                        }
                    }
                },
                new CuiElement
                {
                    Parent = PLAYERCONTROLelemMain,
                    Components =
                    {
                        new CuiTextComponent {Text = name, Align = TextAnchor.UpperCenter, FontSize = 15},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 0.9"}
                    }
                },
                {
                    new CuiButton
                    {
                        Text = {Text = "x", Align = TextAnchor.MiddleCenter, FontSize = 20},
                        Button = {Color = "0 0 0 0", Close = PLAYERCONTROLelemMain},
                        RectTransform =
                        {
                            AnchorMin = "1 1", AnchorMax = "1 1", OffsetMax = "0 0", OffsetMin = "-35 -35"
                        }
                    },
                    PLAYERCONTROLelemMain
                },
                {
                    new CuiButton
                    {
                        Text = {Text = "Ban", Align = TextAnchor.MiddleCenter, FontSize = 15},
                        Button = {Command = "api.deathmatch ban " + userID, Color = "1 0.5 0 1",},
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 5", OffsetMax = "-5 30",
                        }
                    },
                    PLAYERCONTROLelemMain
                },
                {
                    new CuiButton
                    {
                        Text = {Text = "Kick", Align = TextAnchor.MiddleCenter, FontSize = 15},
                        Button = {Command = "api.deathmatch kick " + userID, Color = "1 0.5 0 1",},
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "5 5", OffsetMax = "155 30",
                        }
                    },
                    PLAYERCONTROLelemMain
                },
                {
                    new CuiButton
                    {
                        Text = {Text = "Make Owner", Align = TextAnchor.MiddleCenter, FontSize = 15},
                        Button = {Command = "api.deathmatch owner " + userID, Color = "1 0.5 0 1",},
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 -35", OffsetMax = "-5 -5",
                        }
                    },
                    PLAYERCONTROLelemMain
                },
                {
                    new CuiButton
                    {
                        Text = {Text = "Change team", Align = TextAnchor.MiddleCenter, FontSize = 15},
                        Button = {Command = "api.deathmatch changeteam " + userID, Color = "1 0.5 0 1",},
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "5 -35", OffsetMax = "155 -5",
                        }
                    },
                    PLAYERCONTROLelemMain
                }
            };

            CuiHelper.DestroyUi(player, PLAYERCONTROLelemMain);
            CuiHelper.AddUi(player, container);
        }

        private void PLAYERCONTROLHideUi(BasePlayer player) {
            CuiHelper.DestroyUi(player, PLAYERCONTROLelemMain);
        }

        #endregion

        #region Commands

        [ConsoleCommand("api.deathmatch")]
        private void cmdAPICommand(ConsoleSystem.Arg arg) {
            var bPlayer = arg.Player();
            var player = bPlayer?.GetComponent<GamePlayer>();
            if (player == null) {
                return;
            }

            var controller = player.team.controller;
            if (controller.owner != player) {
                return;
            }

            var def = controller.definition;

            var action = arg.Args?.Length > 0 ? arg.Args[0] : "null";
            var arg2 = arg.Args?.Length > 1 ? arg.Args[1] : "null";
            var arg3 = arg.Args?.Length > 2 ? arg.Args[2] : "null";
            switch (action.ToLower()) {
                case "visibility":
                    if (arg2 == "team1") {
                        def.team1Public = !def.team1Public;
                    }

                    if (arg2 == "team2") {
                        def.team2Public = !def.team2Public;
                    }

                    if (arg2 == "arena") {
                        def.arenaPublic = !def.arenaPublic;
                    }
                    break;

                case "hsonly":
                    def.headshotOnly = !def.headshotOnly;
                    break;

                case "autobalance":
                    def.autoBalance = !def.autoBalance;
                    break;

                case "friendlyfire":
                    def.friendlyFire = !def.friendlyFire;
                    break;

                case "kit":
                    if (arg2 == "team1") {
                        controller.team1.kitName = arg3;
                        controller.team1.kitNameClothing = "clothing_red";
                    }

                    if (arg2 == "team2") {
                        controller.team2.kitName = arg3;
                        controller.team2.kitNameClothing = "clothing_blue";
                    }

                    break;

                case "forcestart":
                    controller.CheckForReady(true);
                    break;

                case "restartround":
                    controller.Restart();
                    break;

                case "1":
                    player.lastPage = 1;
                    break;

                case "2":
                    player.lastPage = 2;
                    break;

                case "3":
                    player.lastPage = 3;
                    break;

                case "kick":
                    var target = controller.allPlayers.FirstOrDefault(x => x.original.UserIDString == arg2);
                    if (target != null) {
                        UnityEngine.Object.DestroyImmediate(target);
                    }

                    break;

                case "ban":
                    var targetBan = controller.allPlayers.FirstOrDefault(x => x.original.UserIDString == arg2);
                    if (targetBan != null) {
                        UnityEngine.Object.DestroyImmediate(targetBan);
                        def.banned.Add(arg2);
                    }

                    break;

                case "control":
                    var targetControl = controller.allPlayers.FirstOrDefault(x => x.original.UserIDString == arg2);
                    if (targetControl != null) {
                        PLAYERCONTROLShowUI(player.original, targetControl.original.displayName, targetControl.original.UserIDString);
                    }
                    break;

                case "changeteam":
                    var targetChange = controller.allPlayers.FirstOrDefault(x => x.original.UserIDString == arg2);
                    if (targetChange != null) {
                        controller.ForceChangeTeam(targetChange);
                    }
                    break;

                case "owner":
                    var targetOwner = controller.allPlayers.FirstOrDefault(x => x.original.UserIDString == arg2);
                    if (targetOwner != null) {
                        controller.owner = targetOwner;
                    }
                    break;
            }

            if (player.lastPage == 0) {
                player.lastPage = 1;
            }

            UIOpenSettings(player, player.lastPage);
        }

        [ConsoleCommand("deathmatch.changeteam")]
        private void cmdCommand(ConsoleSystem.Arg arg) {
            var bPlayer = arg.Player();
            var player = bPlayer?.GetComponent<GamePlayer>();
            if (player == null) {
                return;
            }

            player.team.controller.ChangeTeam(player, player.team.teamNumber == 2);
        }

        #endregion

        #region Configuration | 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Definitions")]
            public GameDefinition[] definitions =
            {
                new GameDefinition
                {
                    shortname = "deathmatch.first",
                    description = "First deathmatch arena, flat, for 2 teams"
                },
            };

            [JsonProperty(PropertyName = "[Kit] Spectators")]
            public string kitSpectators = "kitSpectator";

            [JsonProperty(PropertyName = "Kits Weapons")]
            public string[] kitsWeapons =
            {
                "weapon",
                "kitSMG",
                "kitNoob",
            };
        }

        public class GameDefinition
        {
            [JsonProperty(PropertyName = "Shortname")]
            public string shortname = string.Empty;

            [JsonProperty(PropertyName = "Description")]
            public string description = string.Empty;

            [JsonProperty(PropertyName = "Enabled")]
            public bool enabled = true;

            [JsonProperty(PropertyName = "Max Players")]
            public int maxPlayers = 10;

            [JsonProperty(PropertyName = "[Position] Respawn for Team 1")]
            public string team1Spawn = "";

            [JsonProperty(PropertyName = "[Position] Respawn for Team 2")]
            public string team2Spawn = "";

            [JsonProperty(PropertyName = "[Position] Respawn for Spectators")]
            public string spectatorsSpawn = "";

            [JsonIgnore] public bool arenaPublic = true;
            [JsonIgnore] public bool team1Public = true;
            [JsonIgnore] public bool team2Public = true;
            [JsonIgnore] public bool headshotOnly = false;
            [JsonIgnore] public bool autoBalance = true;
            [JsonIgnore] public bool friendlyFire = true;
            [JsonIgnore] public List<string> banned = new List<string>();
        }

        protected override void LoadConfig() {
            base.LoadConfig();

            try {
                config = Config.ReadObject<ConfigData>();

                if (config == null) {
                    LoadDefaultConfig();
                }
            } catch {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");

                timer.Every(10f,
                    () => {
                        PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                    });
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() {
            config = new ConfigData();
        }

        protected override void SaveConfig() {
            Config.WriteObject(config);
        }

        #endregion

        #region Language | 2.0.0

        private Dictionary<object, string> langMessages = new Dictionary<object, string>
        {
            {Message.Usage, "Usage:\n"},
            {Message.Permission, "<color=#ff0000>You don't have permission to use that!</color>"},
            {Message.Cooldown, "Cooldown for <color=#00ffff>{time.seconds}</color> seconds!"},
            {Message.MissingPlayers, "Team {num} missing players! Match will start when filled!"},
            {Message.NotEnoughReady, "Use <color=#00ffff>/r</color> to mark yourself as ready, Ready players: {ready}, Not ready: {notReady}"},
            {Message.GameStarting, "Most of players are ready! Game is starting in 5 seconds!"},
            {Message.StartingIn, "Game is starting in {seconds} seconds!"},
            {Message.TeamWon, "<color={color}>Team {num}</color> won!"},
            {Message.PlayerJoined, "Player {name} joined team {num}!"},
            {Message.AutoBalance, "You can't join that team, auto-balance is ON!"},
            {Message.JoinedTeam, "You joined team {num}!"},
            {Message.FloatingTextLobbyInfo, "<color=#00ffff>{name}</color>\n\n{description}\n\n{players} Current Players\n{allPlayers}\n\n{free}/{max} Free slots\n Arena is {public}"},
            {Message.UILeftTop, "{name}\n{description}\n{players} Current Players"},
            {Message.UICenterTop, "Team 1 : {team.wins.1} | Team 2 : {team.wins.2}"},
            {Message.UIRightTop, "Players:\n{team.players.1}\n{team.players.2}"},
            {Message.BecameReady, "Player {name} is ready!"},
            {Message.BecameReadyAuto, "Player {name} is ready."},
            {Message.PlayersLimit, "Arena full {players} {limit}!"},
            {Message.Private, "This arena has been made private!"}
        };

        public enum Message
        {
            Usage,
            Permission,
            Cooldown,
            MissingPlayers,
            NotEnoughReady,
            GameStarting,
            TeamWon,
            PlayersLimit,
            Private,
            InOtherGame,
            PlayerJoined,
            AutoBalance,
            JoinedTeam,
            FloatingTextLobbyInfo,
            UILeftTop,
            UICenterTop,
            UIRightTop,
            BecameReady,
            BecameReadyAuto,
            StartingIn,
        }

        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(langMessages.ToDictionary(x => x.Key.ToString(), y => y.Value), this);
        }

        private string GetMessage(Message key, string playerID = null, params object[] args) {
            var message = lang.GetMessage(key.ToString(), this, playerID);
            var dic = OrganizeArgs(args);
            if (dic != null) {
                foreach (var pair in dic) {
                    var s0 = "{" + pair.Key + "}";
                    var s1 = pair.Key;
                    var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                    message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                    message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            return message;
        }

        private void SendMessage(object receiver, string message) {
            if (receiver == null) {
                Puts(message);
                return;
            }

            var console = receiver as ConsoleSystem.Arg;
            if (console != null) {
                SendReply(console, message);
                return;
            }

            var player = receiver as BasePlayer;
            if (player != null) {
                player.ChatMessage(message);
            }
        }

        private void SendMessage(object receiver, Message key, params object[] args) {
            var userID = (receiver as BasePlayer)?.UserIDString;
            var message = GetMessage(key, userID, args);
            SendMessage(receiver, message);
        }

        private static Dictionary<string, object> OrganizeArgs(object[] args) {
            var dic = new Dictionary<string, object>();
            for (var i = 0; i < args.Length; i += 2) {
                var value = args[i].ToString();
                var nextValue = i + 1 < args.Length ? args[i + 1] : null;
                dic.Add(value, nextValue);
            }

            return dic;
        }

        #endregion

        #region Hook
        private void OnPlayerDisconnected(BasePlayer player, string reason) {
            GamePlayer gp = player.GetComponent<GamePlayer>();
            Puts("Player disconnected");
            try {
                LeaveGame(player);
            } catch (Exception e) {
                plugin.Puts(e.Message);
            }
        }
        #endregion

        #region Scripts

        public class GameController : MonoBehaviour
        {
            public GameDefinition definition;
            public GameTeam team1 = new GameObject().AddComponent<GameTeam>();
            public GameTeam team2 = new GameObject().AddComponent<GameTeam>();

            public int countReady => team1.countReady + team2.countReady;
            public int countTotal => team1.countTotal + team2.countTotal;
            public int freeSlots => definition.maxPlayers - countTotal;
            public GamePlayer[] allPlayers => team1.getPlayers.Concat(team2.getPlayers).ToArray();
            public bool gameInProgress;
            public GamePlayer owner;

            // Core

            public void Setup(GameDefinition iDefinition) {
                definition = iDefinition;
            }

            private void Start() {
                team1.Setup(this, definition.team1Spawn, 1);
                team2.Setup(this, definition.team2Spawn, 2);
                InvokeRepeating(nameof(CheckGameStatus), 3, 3);
            }

            public void Restart() {
                gameInProgress = false;
                team1.ResetPlayers(true);
                team2.ResetPlayers(true);
                CleanArena(definition.team1Spawn, definition.team2Spawn);
                StartMatch();
            }

            public void CheckGameStatus() {
                if (countTotal < 1) {
                    team1.wins = 0;
                    team2.wins = 0;
                    definition.banned.Clear();
                    definition.arenaPublic = true;
                }

                if (owner == null) {
                    owner = allPlayers.FirstOrDefault();
                }

                if (gameInProgress == true) {
                    CheckForWinners();
                } else {
                    CheckForReady();
                }
            }

            private void CheckForWinners() {
                if (team1.countAlive > 0 && team2.countAlive > 0) return;


                gameInProgress = false;

                if (team1.countAlive > 0) {
                    team1.wins++;
                } else {
                    team2.wins++;
                }

                var num = team1.countAlive > 0 ? 1 : 2;
                string red = "#ff0000";
                string blue = "#0000ff";
                SendAll(Message.TeamWon, "{num}", num, "{color}", num == 1 ? red : blue);
            }

            public void CheckForReady(bool ignore = false) {

                if (team1.countTotal < 1 || team2.countTotal < 1) {
                    var num = team1.countTotal < 1 ? 1 : 2;
                    SendAll(Message.MissingPlayers, "{num}", num);
                    return;
                }

                var notReady = countTotal - countReady;
                if (ignore == false && countReady < notReady) {
                    SendAll(Message.NotEnoughReady, "{ready}", countReady, "{notReady}", notReady);
                    return;
                }

                if (!IsInvoking(nameof(StartMatch))) {
                    SendAll(Message.GameStarting);
                    float seconds = 5;

                    Invoke(nameof(StartMatch), seconds);

                    foreach (GamePlayer pl in allPlayers) {
                        UIMsg(pl.original, pl.currentGame);
                    }
                }
            }

            void UIMsg(BasePlayer player, string msg) {
                Interface.CallHook("CountDown", msg, player);
            }

            private void StartMatch() {
                if (gameInProgress == true) return;

                CleanArena(definition.team1Spawn, definition.team2Spawn);
                gameInProgress = true;
                team1.ResetPlayers(false);
                team2.ResetPlayers(false);
            }

            private void OnDestroy() {
                DestroyImmediate(team1);
                DestroyImmediate(team2);
            }

            public void PlayerJoin(BasePlayer player, bool wantInFirstTeam) {
                plugin.Puts($"Debug: \n definition.maxPlayers {definition.maxPlayers} \n countTotal {countTotal}");

                if (definition.banned.Contains(player.UserIDString) == true) {
                    player.ChatMessage("You are banned :(");
                    return;
                }

                if (freeSlots < 1) {
                    plugin.SendMessage(player, Message.PlayersLimit, "{limit}", definition.maxPlayers, "{players}", countTotal);
                    return;
                }

                if (definition.arenaPublic == false) {
                    plugin.SendMessage(player, Message.Private);
                    return;
                }

                var member = player.gameObject.GetComponent<GamePlayer>();
                if (member != null) {
                    plugin.SendMessage(player, Message.InOtherGame);
                    return;
                }

                member = player.gameObject.AddComponent<GamePlayer>();
                var team = GetIdealTeam(member, wantInFirstTeam);
                team.JoinPlayer(member);
                SendAll(Message.PlayerJoined, "{name}", player.displayName, "{num}", team.teamNumber);
            }

            public void ForceChangeTeam(GamePlayer player) {
                var other = player.team.teamNumber == 1 ? team2 : team1;
                player.team.LeavePlayer(player);
                other.JoinPlayer(player);
            }

            public void ChangeTeam(GamePlayer player, bool inFirst) {
                var team = GetIdealTeam(player, inFirst);
                if (team != player.team) {
                    player.team.LeavePlayer(player);
                    team.JoinPlayer(player);
                } else {
                    player.original.ChatMessage("Can't change team");
                }
            }

            // Utils

            private void SendAll(Message key, params object[] args) {
                plugin.SendAll(this, key, args);
            }

            private GameTeam GetIdealTeam(GamePlayer player, bool wantInFirstTeam) {
                plugin.Puts($"[Scrim,  {definition.shortname}] Team 1: {team1.countTotal}, Team 2: {team2.countTotal} [balance: {definition.autoBalance}]");

                if (!definition.autoBalance) return wantInFirstTeam ? team1 : team2;

                plugin.SendMessage(player.original, Message.AutoBalance);
                if (Math.Abs(team1.countTotal - team2.countTotal) >= 1) {
                    if (team1.countTotal > team2.countTotal) {
                        return team2;
                    } else {
                        return team1;
                    }
                } else {
                    return wantInFirstTeam ? team1 : team2;
                }
                /*
                var teamWant = wantInFirstTeam ? team1 : team2;
                var teamOther = wantInFirstTeam ? team2 : team1;
                var canTeam = definition.autoBalance == false || teamWant.countTotal - teamOther.countTotal < autoBalanceMax;
                var isPublic = wantInFirstTeam ? definition.team1Public : definition.team2Public;

                if (canTeam == true && isPublic) {
                    return teamWant;
                } else {
                    plugin.SendMessage(player.original, Message.AutoBalance);
                    return teamOther;
                }
                */
            }
        }

        public class GameTeam : MonoBehaviour
        {
            public GameController controller;
            private List<GamePlayer> players = new List<GamePlayer>();
            public RespawnInfo infoSpectator = new RespawnInfo();

            public RespawnInfo infoPlayer => new RespawnInfo {
                kitName = kitName,
                kitNameClothing = kitNameClothing,
                spawnFile = spawnFile
            };


            public RelationshipManager.PlayerTeam gameTeam;

            public int teamNumber;
            public int wins;
            public string kitName = config.kitsWeapons[0];
            public string kitNameClothing = "clothing_red";
            public string spawnFile;
            public int countTotal => players.Count;
            public int countAlive => players.Count(x => x.isPlaying);
            public int countReady => players.Count(x => x.isReady);
            public GamePlayer[] getPlayers => players.ToArray();

            public void Setup(GameController iController, string iSpawnFile, int teamID) {
                controller = iController;
                teamNumber = teamID;
                kitNameClothing = teamNumber == 1 ? "clothing_red" : "clothing_blue";
                infoSpectator = new RespawnInfo {
                    kitName = config.kitSpectators,
                    spawnFile = controller.definition.spectatorsSpawn
                };
                spawnFile = iSpawnFile;
                gameTeam = RelationshipManager.Instance.CreateTeam();
            }

            public void JoinPlayer(GamePlayer player) {
                players.Add(player);
                player.ToChat(Message.JoinedTeam, "{num}", teamNumber);
                plugin.Puts($"Scrim, {plugin.Version}] Joined team {teamNumber} at {controller.definition.shortname}");
                player.team = this;
                player.type = MemberType.Spectator;
                player.Reset();
                controller.CheckGameStatus();
                player.original?.Team?.Disband();
                gameTeam.AddPlayer(player.original);
            }

            public void LeavePlayer(GamePlayer player) {
                if (players.Contains(player)) {
                    players.Remove(player);
                    controller.CheckGameStatus();
                    player.original?.Team?.RemovePlayer(player.original.userID);
                }
            }

            public void ResetPlayers(bool spectators) {
                foreach (var player in players) {
                    player.isReady = false;// (_instance.autoReady.ContainsKey(GetComponent<BasePlayer>().userID)) ? true : false;
                    player.type = spectators ? MemberType.Spectator : MemberType.Playing;
                    player.Reset();
                }
            }

            public void OnWin() {
                wins++;
            }

            private void OnDestroy() {
                foreach (var player in players.ToArray()) {
                    DestroyImmediate(player);
                }
            }

            public string GetPlayers() {
                var text = string.Empty;
                var color = teamNumber == 1 ? "ff0000" : "0000ff";

                foreach (var player in players) {
                    text += $"<color=#{color}>{player.original.displayName}</color>\n";
                }

                return text;
            }
        }

        public class GamePlayer : MonoBehaviour
        {
            private BasePlayer player;
            public BasePlayer original => player;
            public int lastPage;

            public string currentGame => team.controller.definition.shortname;
            public GameTeam team;
            public MemberType type;
            public bool isReady;

            public int kills = 0;
            public int death = 0;
            public int damage = 0;

            public bool isPlaying => type == MemberType.Playing;

            private void Awake() {
                player = GetComponent<BasePlayer>();
            }

            private void OnDestroy() {
                player.ChatMessage("You are leaving your team");
                team.LeavePlayer(this);
                CuiHelper.DestroyUi(original, elemMain);
                type = MemberType.Destroying;
                Reset();
            }

            public void Died() {
                type = MemberType.Spectator;
                team.controller.CheckGameStatus();
            }

            public RespawnInfo GetRespawnInfo() {
                switch (type) {
                    case MemberType.Spectator:
                        return team.infoSpectator;

                    case MemberType.Playing:
                        return team.infoPlayer;

                    case MemberType.Destroying:
                        return null;

                    default:
                        return team.infoSpectator;
                }
            }

            public void ToChat(Message message, params object[] args) {
                plugin.SendMessage(player, message, args);
            }

            public void ToConsole(string msg) {
                player.ConsoleMessage(msg);
            }

            public void Reset() {
                ToConsole($"[Minigames Deathmatch, {currentGame}] ResetPlayer");
                Interface.CallHook("RefreshPlayer", player);
            }
        }

        #endregion

        #region Minigames API 1.0.0

        private object GetCustomSpawnPosition(BasePlayer player) // Return position if needed
        {
            var obj = player?.GetComponent<GamePlayer>();
            List<Vector3> spawns = LoadSpawnpoints(obj.GetRespawnInfo()?.spawnFile);
            List<Vector3> spawns2 = LoadSpawnpoints("start_area");
            return obj != null && spawns != null ? GetRandomSpawnPoint(spawns) : GetRandomSpawnPoint(spawns2);
        }

        private string GetCustomKitName(BasePlayer player) // Return kit name if needed
        {
            GamePlayer gp = player?.GetComponent<GamePlayer>();
            return gp != null ? gp.GetRespawnInfo()?.kitName : "";
        }

        private string GetCustomKitNameClothing(BasePlayer player) // Return kit name if needed
        {
            GamePlayer gp = player?.GetComponent<GamePlayer>();
            return gp != null ? gp?.GetRespawnInfo()?.kitNameClothing : "";
        }

        private object CanGetDamageFrom(BasePlayer victim, BasePlayer initiator, HitInfo info) // Return text why can get damage
        {
            var obj1 = victim?.GetComponent<GamePlayer>();
            var obj2 = initiator?.GetComponent<GamePlayer>();
            if (obj1 == null || obj2 == null) {
                return null;
            }

            // TODO: Checks for controller??

            if (!obj1.isPlaying && !obj2.isPlaying) {
                Puts("Non of the players are are playing scrim");
                return null;
            }

            if (!obj1.isPlaying || !obj2.isPlaying) {
                return "Only ONE of the players are are playing scrim";
            }

            if (obj1.team == obj2.team) {
                return obj1.team.controller.definition.friendlyFire ? "In same team, Friendly Fire ON" : null;
            }

            if (obj1.team.controller.definition.headshotOnly) {
                return info.isHeadshot ? "Is Headshot" : null;
            }

            return "In different teams, both in game";
        }

        private object CanJoinMiniGame(BasePlayer player) // Return text with existing game name
        {
            var obj = player?.GetComponent<GamePlayer>();
            return obj != null ? obj.currentGame : (object)null;
        }

        private void JoinMiniGame(BasePlayer player, string name) // Join game with name
        {
            var obj = Interface.CallHook(nameof(CanJoinMiniGame), player);
            player.ConsoleMessage($"[Minigames] Can join game '{name}' : {obj == null} (Current game: {obj})");
            if (obj == null) {
                JoinGame(player, name);
            }
        }

        private void LeaveMiniGame(BasePlayer player) // Leave game if possible
        {
            LeaveGame(player);
        }

        private string PlayersString(GamePlayer[] gplayers) {
            string red = "#ff0000";
            string blue = "#0000ff";
            StringBuilder builder = new StringBuilder();

            List<string> players = new List<string>();

            foreach (GamePlayer pl in gplayers) {
                string color = pl.team.teamNumber == 1 ? red : blue;
                players.Add($"<color={color}>{pl.original.displayName}</color>");
            }
            if (gplayers.Count() == 0) {
                players.Add("<color=#ff0000ff>Empty</color>");
            }
            builder.Append(String.Join(", ", players));
            return builder.ToString();
        }

        private void ReadyToMiniGame(BasePlayer player, bool ar) // Mark ready to game if possible
        {
            MarkReady(player, ar);
        }
        #endregion

        #region API

        private Dictionary<string, string> GetAllGames() {
            var dic = new Dictionary<string, string>();

            foreach (var value in UnityEngine.Object.FindObjectsOfType<GameController>()) {
                var key = value.definition.shortname;
                var text = GetMessage(Message.FloatingTextLobbyInfo, null, "{name}", key,
                     "{description}", value.definition.description,
                     "{public}", value.definition.arenaPublic ? "Public" : "Private",
                     "{players}", value.countTotal,
                     "{allPlayers}", PlayersString(value.allPlayers),
                     "{team1}", PlayersString(value.team1.getPlayers),
                     "{team2}", PlayersString(value.team2.getPlayers),
                     "{free}", value.freeSlots,
                     "{max}", value.definition.maxPlayers);
                dic.Add(key, text);
            }

            return dic;
        }

        #endregion

        #region Plugin Hooks
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

        Vector3 GetRandomSpawnPoint(List<Vector3> spawns) {
            return spawns.GetRandom();
        }
        Vector3 GetFirstSpawnPoint(List<Vector3> spawns) {
            return spawns.FirstOrDefault();
        }
        #endregion
    }
}