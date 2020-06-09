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
    [Info("Scrim Deathmatch", "Pho3niX90", "1.4.1")]
    [Description("")]
    public class KingGamesDeathmatch : RustPlugin
    {
        [PluginReference] Plugin Spawns;
        #region Vars
        private const int autoBalanceMax = 1;
        private static KingGamesDeathmatch plugin;
        static KingGamesDeathmatch _instance;
        string panelColor = "0 0 0 0.5";//"1 0.5 0 0";
        string panelColor2 = "1.0 0.8 0 0.8";//"1 0.5 0 1";
        private static List<string> teamColors = new List<string>();

        public enum MemberType
        {
            Spectator,
            Playing,
            Destroying,
            NoTeam
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

            //colors must be hex
            teamColors.Add("#ff0000");
            teamColors.Add("#0000ff");
            teamColors.Add("#00ff00");
            teamColors.Add("#ff00ff");
            teamColors.Add("#00ffff");
        }

        private void OnServerInitialized() {
            CreateGames();
        }

        private void Unload() {
            RemoveAllGames();
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info) {
            BasePlayer attacker = info?.InitiatorPlayer;
            Puts($"Player {player.displayName} has died. Killed by {attacker?.displayName} {attacker == null}");
            MarkDead(player);
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
                    if (def.enabled) {
                        var obj = new GameObject().AddComponent<GameController>();
                        obj.Setup(def);
                    }
                }
            });
        }

        private void JoinGame(BasePlayer player, string name, int teamNumber) {
            Puts($"Tryint to join {name} for team {teamNumber}");
            var obj = Interface.CallHook(nameof(CanJoinGame), player);
            player.ConsoleMessage($"[Games] Can join game '{name}' : {obj == null} (Current game: {obj})");
            if (obj != null) return;

            foreach (var game in UnityEngine.Object.FindObjectsOfType<GameController>()) {
                if (string.Equals(game.definition.shortname, name, StringComparison.OrdinalIgnoreCase)) {
                    game.PlayerJoin(player, teamNumber);
                }
            }
        }

        private void LeaveGame(BasePlayer player) {
            var obj = plugin.GetPlayer(player);
            if (obj != null) {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private static void MarkReady(BasePlayer player, bool ar = false) {
            var obj = plugin.GetPlayer(player);
            if (obj != null && !obj.isReady) {
                obj.isReady = true;
                plugin.SendAll(obj.team.controller, ar ? Message.BecameReadyAuto : Message.BecameReady, player.displayName);
            }
        }

        private static void MarkDead(BasePlayer player) {
            var obj = plugin.GetPlayer(player);
            if (obj != null) obj.Died();
        }

        private static void CleanArena(GameController gControl) {
            Vector3 t1 = plugin.GetFirstSpawnPoint(plugin.LoadSpawnpoints(gControl.definition.team1Spawn));
            Vector3 t2 = plugin.GetFirstSpawnPoint(plugin.LoadSpawnpoints(gControl.definition.team2Spawn));

            Vector3 midPoint = (t1 + t2) / 2;
            float distance = Vector3.Distance(t1, t2) + 10;

            var entities = new List<BaseEntity>();
            Vis.Entities(midPoint, distance, entities);

            foreach (var entity in entities) {
                if (entity.IsValid() && entity.OwnerID != 0) {
                    BasePlayer owner = BasePlayer.FindByID(entity.OwnerID);
                    GamePlayer player = plugin.GetPlayer(owner);
                    if (owner == null || player == null || gControl.allPlayersIncSpec.Contains(player) || !owner.IsConnected)
                        if (!(entity is BasePlayer)) entity?.Kill();
                }
            }
        }

        private void SendAll(GameController controller, Message key, params object[] args) {
            foreach (var player in controller.allPlayersIncSpec) {
                RefreshTeamUIPanel(player, controller);
                SendMessage(player.original, key, args);
            }
        }

        #endregion

        #region Scores
        private void OnEntityTakeDamage(BasePlayer player, HitInfo info) {
            if (info == null || info.damageTypes.Total() < 1) return;
            var initiator = plugin.GetPlayer(info?.InitiatorPlayer);
            if (initiator != null) initiator.damage += Convert.ToInt32(info.damageTypes.Total());
        }

        private void OnEntityDeath(BasePlayer player, HitInfo info) {
            if (info == null) return;

            var initiator = plugin.GetPlayer(info?.InitiatorPlayer);
            var victim = plugin.GetPlayer(player);

            if (initiator != null) initiator.kills++;
            if (victim != null) victim.death++;

            if (initiator != null && victim != null) {
                string iniColor = plugin.TeamColor(initiator.team.teamNumber);
                string vicColor = plugin.TeamColor(victim.team.teamNumber);

                foreach (GamePlayer pl in initiator.currentGameContr.allPlayersIncSpec) {
                    SendReply(pl.original, $"<color={iniColor}>{initiator.original.displayName}</color> killed <color={vicColor}>{victim.original.displayName}</color>");
                }
            }
        }

        #endregion

        #region UI 2.0 General

        private const string elemMainTop = "KingGamesDeathmatch.UI.Main.Top";
        private const string elemMainBottom = "KingGamesDeathmatch.UI.Main.Bottom";
        private const string elemMainLeft = "KingGamesDeathmatch.UI.Main.Bottom.Left";
        private const string elemMainRight = "KingGamesDeathmatch.UI.Main.Bottom.Right";
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
            var containerTop = new CuiElementContainer();
            var containerBottomLeft = new CuiElementContainer();
            var containerBottomRight = new CuiElementContainer();

            // Main score panel
            containerTop.Add(new CuiElement {
                Name = elemMainTop,
                Parent = "Hud",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMax = "1.0 1.0",
                        AnchorMin = "0.0 0.0"
                    },
                    //new CuiNeedsCursorComponent()
                }
            });

            AddTeamPanel(containerTop, controller.team1);
            AddTeamPanel(containerTop, controller.team2);

            // Main panel
            containerBottomLeft.Add(new CuiElement {
                Name = elemMainLeft,
                Parent = "Hud.Menu",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMax = "0.293 0.07",
                        AnchorMin = "0.185 0.04"
                    },
                    //new CuiNeedsCursorComponent()
                }
            });

            // Change team button
            containerBottomLeft.Add(new CuiButton {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = player.team.teamNumber == 0 ? "JOIN TEAM" : "CHANGE TEAM",
                    FontSize = 17,
                },
                Button =
                {
                    Command = "deathmatch.changeteam",
                    Color = "0.5 0.5 0.5 0.7"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, elemMainLeft);


            containerBottomRight.Add(new CuiElement {
                Name = elemMainRight,
                Parent = "Hud.Menu",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMax = "0.848 0.09",
                        AnchorMin = "0.632 0.02"
                    },
                    //new CuiNeedsCursorComponent()
                }
            });

            //Leave button
            containerBottomRight.Add(new CuiButton {
                Text =
                 {
                     Align = TextAnchor.MiddleCenter,
                     Text = "LEAVE",
                     FontSize = 17,
                 },
                Button =
                 {
                     Command = "chat.say /leave",
                     Color = "0.5 0.5 0.5 0.7"
                 },
                RectTransform =
                 {
                    AnchorMin = "0.15 0",
                    AnchorMax = "0.49 0.45"
                 }
            }, elemMainRight);

            if (controller.owner == player) {
                // Settings button
                containerBottomRight.Add(new CuiButton {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "SETTINGS",
                        FontSize = 17,
                    },
                    Button =
                    {
                        Command = "api.deathmatch",
                        Color = "0.5 0.5 0.5 0.7"
                    },
                    RectTransform =
                    {
                    AnchorMin = "0.51 0.00",
                    AnchorMax = "0.85 0.45"
                    }
                }, elemMainRight);
            }

            // Owner Name
            containerBottomRight.Add(new CuiElement {
                Parent = elemMainRight,
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
                        AnchorMin = "0 0.50",
                        AnchorMax = "1 1.00",
                    },
                }
            });

            CuiHelper.DestroyUi(player.original, elemMainTop);
            CuiHelper.DestroyUi(player.original, elemMainLeft);
            CuiHelper.DestroyUi(player.original, elemMainRight);
            CuiHelper.AddUi(player.original, containerTop);
            CuiHelper.AddUi(player.original, containerBottomLeft);
            CuiHelper.AddUi(player.original, containerBottomRight);
        }

        private void AddTeamPanel(CuiElementContainer container, GameTeam team) {
            var teamID = team.teamNumber;
            var values = team.getPlayers;
            var parent = elemMainTop + ".team." + teamID;
            var transform = teamID == 2 ? team2Position : team1Position;
            var teamChar = teamID == 2 ? "B" : "A";
            string teamColor = plugin.TeamColor(teamID);

            // Team panel
            container.Add(new CuiElement {
                Name = parent,
                Parent = elemMainTop,
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
                            Color = !value.isPlaying ? "1 0 0 1" : "0 1 0 1",
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
                layer = "SPECTATORS",
                textON = "ALLOWED",
                textOFF = "FORBIDDEN",
                command = "api.deathmatch visibility spectators"
            },
            new ButtonComplex // 4
            {
                layer = "HS ONLY",
                colorOFF = "0 0.8 0 0.8",
                colorON = "0.8 0 0 0.8",
                command = "api.deathmatch hsonly"
            },
            new ButtonComplex // 5
            {
                layer = "AUTOBALANCE",
                command = "api.deathmatch autobalance"
            },
            new ButtonComplex // 6
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
                    Parent = "Overlay",
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
                        new CuiImageComponent {Color = panelColor2,},
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
            buttons[3].isOn = controller.definition.spectatorsAllowed;
            buttons[4].isOn = controller.definition.headshotOnly;
            buttons[5].isOn = controller.definition.autoBalance;
            buttons[6].isOn = controller.definition.friendlyFire;

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
                        Color = panelColor2,
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
                        Color = panelColor2,
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
                        Text = $"<color={plugin.TeamColor(controller.team1.teamNumber)}>Team A:</color> {controller.team1.kitName}",
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

                        Command = $"api.deathmatch kit team1 {name}",
                        Color = panelColor2
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{lastX} {lastY - sizeY / 2}",
                        OffsetMax = $"{lastX + sizeX} {lastY + sizeY / 2}"
                    }
                }, elemMainSettings);


                if (++i % 5 == 0) {
                    lastY -= sizeY + offsetY;
                    lastX = startX;
                } else {
                    lastX += sizeX + offsetX;
                }
            }

            startX = 200;
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
                        Text = $"<color={plugin.TeamColor(controller.team2.teamNumber)}>Team B:</color> {controller.team2.kitName}",
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
                        OffsetMin = $"{lastX - sizeX} {lastY + sizeY}",
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
                        Command = $"api.deathmatch kit team2 {name}",
                        Color = panelColor2
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{lastX - sizeX} {lastY - sizeY / 2}",
                        OffsetMax = $"{lastX} {lastY + sizeY / 2}"
                    }
                }, elemMainSettings);


                if (++i % 5 == 0) {
                    lastY -= sizeY + offsetY;
                    lastX = startX;
                } else {
                    lastX += sizeX + offsetX;
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
                        Color = panelColor2
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

        #region Commands
        [ConsoleCommand("api.deathmatch")]
        private void cmdAPICommand(ConsoleSystem.Arg arg) {
            var bPlayer = arg.Player();
            var player = GetPlayer(bPlayer);
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

                    if (arg2 == "spectators") {
                        def.spectatorsAllowed = !def.spectatorsAllowed;
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

                case "changeteam":
                    var targetChange = controller.allPlayers.FirstOrDefault(x => x.original.UserIDString == arg2);
                    if (targetChange != null) {
                        controller.ChangeTeam(targetChange, true);
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
            var player = GetPlayer(bPlayer);
            if (player == null) {
                return;
            }

            player.team.controller.ChangeTeam(player);
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
            [JsonProperty(PropertyName = "[Kit] Team 1 Clothing")]
            public string kitTeam1 = "clothing_red";
            [JsonProperty(PropertyName = "[Kit] Team 2 Clothing")]
            public string kitTeam2 = "clothing_blue";

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
            [JsonIgnore] public bool spectatorsAllowed = true;
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
            {Message.MissingPlayers, "Team {0} missing players! Match will start when filled!"},
            {Message.NotEnoughReady, "Use <color=#00ffff>/r</color> to mark yourself as ready, Ready players: {0}, Not ready: {1}"},
            {Message.GameStarting, "Most of players are ready! Game is starting in 5 seconds!"},
            {Message.StartingIn, "Game is starting in {0} seconds!"},
            {Message.TeamWon, "<color={0}>Team {1}</color> won!"},
            {Message.TeamWonClean, "9"},
            {Message.PlayerJoined, "Player {0} joined team {1}!"},
            {Message.AutoBalance, "You can't join that team, auto-balance is ON!"},
            {Message.JoinedTeam, "You joined team {0}!"},
            {Message.FloatingTextLobbyInfo, "<color=#00ffff>{0}</color>\n\n{1}\n\n Current Players\n{2}\n\n{3} Players\n Arena is {5}"},
            {Message.UILeftTop, "{0}\n{1}\n{2} Current Players"},
            {Message.UICenterTop, "Team 1 : {0} | Team 2 : {1}"},
            {Message.UIRightTop, "Players:\n{0}\n{1}"},
            {Message.BecameReady, "Player {0} is ready!"},
            {Message.BecameReadyAuto, "Player {0} is ready."},
            {Message.PlayersLimit, "Arena full {0} {1}!"},
            {Message.PlayersLimitChangingToSpectator, "Arena full {0} {1}! Changing to Spectators" },
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
            TeamWonClean,
            PlayersLimit,
            PlayersLimitChangingToSpectator,
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

        string GetMessage(Message key, params object[] args) {
            var msg = lang.GetMessage(key.ToString(), this);
            return string.Format(msg, args);
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
            SendMessage(receiver, GetMessage(key, args));
        }

        #endregion

        #region Hook
        private void OnPlayerDisconnected(BasePlayer player, string reason) {
            Puts("Player disconnected");
            try {
                LeaveGame(player);
            } catch (Exception e) {
                plugin.Puts(e.Message);
            }
        }

        string TeamColor(int num) => teamColors[Math.Min(teamColors.Count, num) - 1];
        #endregion

        #region Scripts
        public class GameController : MonoBehaviour
        {
            public GameDefinition definition;
            public GameTeam team1 = new GameObject().AddComponent<GameTeam>();
            public GameTeam team2 = new GameObject().AddComponent<GameTeam>();
            public GameTeam noteam = new GameObject().AddComponent<GameTeam>();

            public int countSpectators => noteam.countTotal;
            public int countReady => team1.countReady + team2.countReady;
            public int countTotal => team1.countTotal + team2.countTotal;
            public int freeSlots => definition.maxPlayers - countTotal;
            public GamePlayer[] allPlayers => team1.getPlayers.Concat(team2.getPlayers).ToArray();
            public GamePlayer[] allPlayersIncSpec => allPlayers.Concat(noteam.getPlayers).ToArray();
            public bool gameInProgress;
            public GamePlayer owner;

            // Core

            public void Setup(GameDefinition iDefinition) {
                definition = iDefinition;
            }

            private void Start() {
                noteam.Setup(this, definition.spectatorsSpawn, 0);
                team1.Setup(this, definition.team1Spawn, 1);
                team2.Setup(this, definition.team2Spawn, 2);
                InvokeRepeating(nameof(CheckGameStatus), 5, 5);
            }

            public void Restart() {
                gameInProgress = false;

                CleanArena(this);

                team1.ResetPlayers(true);
                team2.ResetPlayers(true);

                foreach (GamePlayer pl in allPlayers) UIMsg(pl.original, pl.currentGame);

                Invoke(nameof(StartMatch), 5f);
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
                if (team1.owner == null) {
                    team1.owner = team1.getPlayers.FirstOrDefault();
                }
                if (team2.owner == null) {
                    team2.owner = team2.getPlayers.FirstOrDefault();
                }

                if (gameInProgress) {
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
                plugin.timer.Once(0.1f, () => {
                    SendAll(Message.TeamWon, plugin.TeamColor(num));

                    foreach (GamePlayer pl in allPlayersIncSpec) {
                        var msg = plugin.GetMessage(Message.TeamWon, plugin.TeamColor(num), num);
                        UIMsgNotice(pl.original, msg);
                    }

                });
            }

            public void CheckForReady(bool ignore = false) {

                if (team1.countTotal < 1 || team2.countTotal < 1) {
                    var num = team1.countTotal < 1 ? 1 : 2;
                    SendAll(Message.MissingPlayers, num);
                    return;
                }

                var notReady = countTotal - countReady;
                if (!ignore && countReady < notReady) {
                    SendAll(Message.NotEnoughReady, countReady, notReady);
                    return;
                }

                if (!IsInvoking(nameof(StartMatch))) {
                    SendAll(Message.GameStarting);
                    float seconds = 5;

                    Invoke(nameof(StartMatch), seconds);

                    foreach (GamePlayer pl in allPlayersIncSpec) {
                        UIMsg(pl.original, pl.currentGame);
                    }
                }
            }


            void UIMsg(BasePlayer player, string arena) {
                Interface.CallHook("CountDown", arena, player);
            }
            void UIMsgNotice(BasePlayer player, string msg) {
                Interface.CallHook("UICoreMessage", player, msg, 2f);
            }

            private void StartMatch() {
                if (gameInProgress) return;

                CleanArena(this);
                gameInProgress = true;
                team1.ResetPlayers(false);
                team2.ResetPlayers(false);

                foreach (var player in allPlayersIncSpec)
                    plugin.RefreshTeamUIPanel(player, this);
            }

            private void OnDestroy() {
                DestroyImmediate(team1);
                DestroyImmediate(team2);
            }

            public void PlayerJoin(BasePlayer player, int teamNumber) {

                if (definition.banned.Contains(player.UserIDString)) {
                    player.ChatMessage("You are banned :(");
                    return;
                }

                if (!definition.arenaPublic || !(definition.team1Public && definition.team2Public && definition.spectatorsAllowed)) {
                    plugin.SendMessage(player, Message.Private);
                    return;
                }

                if (freeSlots < 1) {
                    if (teamNumber > 0) {
                        if (definition.spectatorsAllowed) {
                            plugin.SendMessage(player, Message.PlayersLimitChangingToSpectator, definition.maxPlayers, countTotal);
                            teamNumber = 0;
                        } else {
                            plugin.SendMessage(player, Message.PlayersLimit, definition.maxPlayers, countTotal);
                            return;
                        }
                    }
                }

                var member = plugin.GetPlayer(player);
                if (member != null) {
                    plugin.SendMessage(player, Message.InOtherGame);
                    return;
                }

                member = player.gameObject.AddComponent<GamePlayer>();
                var team = ChooseTeam(member, teamNumber);
                team.JoinPlayer(member);
                SendAll(Message.PlayerJoined, player.displayName, team.teamNumber);
            }

            public void ChangeTeam(GamePlayer player, bool force = false) {
                var team = ChooseTeam(player, player.team.teamNumber == 1 ? 2 : 1);
                if (team != player.team || force) {
                    player.team.LeavePlayer(player);
                    team.JoinPlayer(player);
                    if (!force) player.original.ChatMessage("Team will change after this round!");
                } else {
                    player.original.ChatMessage("Can't change team");
                }
            }

            // Utils

            private void SendAll(Message key, params object[] args) {
                plugin.SendAll(this, key, args);
            }

            private GameTeam ChooseTeam(GamePlayer player, int teamNumber) {
                plugin.Puts($"[Scrim,  {definition.shortname}] Team 1: {team1.countTotal}, Team 2: {team2.countTotal} [balance: {definition.autoBalance}]");
                GameTeam desiredTeam = teamNumber == 1 ? team1 : team2;
                GameTeam balancedTeam = teamNumber == 1 ? team1 : team2;
                if (teamNumber == 0) return noteam;
                if (!definition.autoBalance) return desiredTeam;

                plugin.SendMessage(player.original, Message.AutoBalance);
                if (Math.Abs(team1.countTotal - team2.countTotal) >= autoBalanceMax) {

                    if (team1.countTotal > team2.countTotal && definition.team2Public) {
                        balancedTeam = team2;
                    } else if (definition.team1Public) {
                        balancedTeam = team1;
                    } else {
                        balancedTeam = noteam;
                    }

                    if (desiredTeam != balancedTeam) {
                        plugin.SendMessage(player.original, Message.AutoBalance);
                    }

                    return balancedTeam;
                } else {
                    return teamNumber == 1 ? team1 : team2;
                }
            }
        }

        public class GameTeam : MonoBehaviour
        {
            public GameController controller;
            private List<GamePlayer> players = new List<GamePlayer>();
            public RespawnInfo infoSpectator = new RespawnInfo();
            public GamePlayer owner;
            [JsonIgnore] public List<string> banned = new List<string>();

            public RespawnInfo infoPlayer => new RespawnInfo {
                kitName = kitName,
                kitNameClothing = kitNameClothing,
                spawnFile = spawnFile
            };


            public RelationshipManager.PlayerTeam gameTeam;

            public int teamNumber;
            public int wins;
            public string kitName = config.kitsWeapons[0];
            public string kitNameClothing = config.kitTeam1 ?? config.kitTeam2 ?? "";
            public string spawnFile;
            public int countTotal => players.Count;
            public int countAlive => players.Count(x => x.isPlaying);
            public int countReady => players.Count(x => x.isReady);
            public GamePlayer[] getPlayers => players.ToArray();

            public void Setup(GameController iController, string iSpawnFile, int teamID) {
                controller = iController;
                teamNumber = teamID;
                kitNameClothing = teamNumber == 1 ? config.kitTeam1 : config.kitTeam2;
                infoSpectator = new RespawnInfo {
                    kitName = config.kitSpectators,
                    spawnFile = controller.definition.spectatorsSpawn
                };
                spawnFile = iSpawnFile;
                gameTeam = RelationshipManager.Instance.CreateTeam();
            }

            public void JoinPlayer(GamePlayer player) {
                players.Add(player);
                player.ToChat(Message.JoinedTeam, teamNumber);
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
                    player.type = spectators || player.team.teamNumber == 0 ? MemberType.Spectator : MemberType.Playing;
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
                var color = plugin.TeamColor(teamNumber);

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
            public GameController currentGameContr => team.controller;
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
                CuiHelper.DestroyUi(original, elemMainTop);
                CuiHelper.DestroyUi(original, elemMainLeft);
                CuiHelper.DestroyUi(original, elemMainRight);
                type = MemberType.Destroying;
                Reset();
            }

            public void Died() {
                type = MemberType.Spectator;
                team.controller.CheckGameStatus();
            }

            public RespawnInfo GetRespawnInfo() {
                switch (type) {
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
                ToConsole($"[Games Deathmatch, {currentGame}] ResetPlayer");
                Interface.CallHook("RefreshPlayer", player);
            }
        }

        #endregion

        #region Games API 1.0.0
        GamePlayer GetPlayer(BasePlayer bPlayer) => bPlayer?.GetComponent<GamePlayer>();
        private object GetCustomSpawnPosition(BasePlayer player) // Return position if needed
        {
            var obj = GetPlayer(player);
            string spawnFile = obj?.GetRespawnInfo()?.spawnFile;
            List<Vector3> spawns = (spawnFile != null) ? LoadSpawnpoints(spawnFile) : LoadSpawnpoints("start_area");
            return GetRandomSpawnPoint(spawns);// obj != null && spawns != null ? GetRandomSpawnPoint(spawns) : GetRandomSpawnPoint(spawns2);
        }

        private string GetCustomKitName(BasePlayer player) // Return kit name if needed
        {
            GamePlayer gp = GetPlayer(player);
            return gp != null ? gp.GetRespawnInfo()?.kitName : "";
        }

        private string GetCustomKitNameClothing(BasePlayer player) // Return kit name if needed
        {
            GamePlayer gp = plugin.GetPlayer(player);
            return gp != null ? gp?.GetRespawnInfo()?.kitNameClothing : "";
        }

        private object CanGetDamageFrom(BasePlayer victim, HitInfo info) // Return text why cant get damage
        {

            if (info == null || info.InitiatorPlayer == null) {
                return "There is no attacker??";
            }

            BasePlayer initiator = info?.InitiatorPlayer;

            if (victim == initiator) {
                //plugin.Puts("[KGC] Can get damage because: victim == initiator");
                return null;// "No Suicide";
            }

            var obj1 = GetPlayer(victim);
            var obj2 = GetPlayer(initiator);

            if (obj1 == null && obj2 == null) return null;

            if (!obj1.isPlaying && !obj2.isPlaying) {
                Puts("Non of the players are playing scrim");
                return null;
            }

            if (!obj1.isPlaying || !obj2.isPlaying) {
                return "Only ONE of the players are playing scrim";
            }

            if (obj1.team == obj2.team) {
                return obj1.team.controller.definition.friendlyFire ? "In same team, Friendly Fire ON" : null;
            }

            if (obj1.team.controller.definition.headshotOnly) {
                return !info.isHeadshot ? "Not Headshot" : null;
            }

            return null;
        }

        private object CanJoinGame(BasePlayer player) // Return text with existing game name
        {
            var obj = GetPlayer(player);
            Puts("CAnJoin?? " + (obj == null));
            return obj != null ? obj.currentGame : (object)null;
        }

        private string PlayersString(GamePlayer[] gplayers) {
            StringBuilder builder = new StringBuilder();

            List<string> players = new List<string>();

            foreach (GamePlayer pl in gplayers) {
                string color = plugin.TeamColor(pl.team.teamNumber);
                players.Add($"<color={color}>{pl.original.displayName}</color>");
            }
            if (gplayers.Count() == 0) {
                players.Add("<color=#ff0000ff>Empty</color>");
            }
            builder.Append(String.Join(", ", players));
            return builder.ToString();
        }

        private void ReadyToGame(BasePlayer player, bool ar) => MarkReady(player, ar);

        #endregion

        #region API

        private Dictionary<string, string> GetAllGames() {
            var dic = new Dictionary<string, string>();

            foreach (var value in UnityEngine.Object.FindObjectsOfType<GameController>()) {
                var key = value.definition.shortname;
                var text = GetMessage(Message.FloatingTextLobbyInfo,

                    key,
                     value.definition.description,
                     PlayersString(value.allPlayers),
                     value.countTotal.ToString(),
                     value.definition.maxPlayers.ToString(),
                    (value.definition.arenaPublic ? "Public" : "Private")

                    );
                dic.Add(key, text);
                // {Message.FloatingTextLobbyInfo, "<color=#00ffff>{0}</color>\n\n{1}Current Players\n{2}\n\n{3}/{4} Free slots\n Arena is {5}"},
            }

            return dic;
        }
        private Dictionary<string, string> GetAllTeams() {
            var dic = new Dictionary<string, string>();

            foreach (var value in UnityEngine.Object.FindObjectsOfType<GameController>()) {
                var key = value.definition.shortname;
                var text = value.team1.countTotal + "|" + value.team2.countTotal;
                dic.Add(key, text);
                // {Message.FloatingTextLobbyInfo, "<color=#00ffff>{0}</color>\n\n{1}Current Players\n{2}\n\n{3}/{4} Free slots\n Arena is {5}"},
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
            } else {
                PrintError($"Unable to load the specified spawnfile: {spawnFile}");
                return null;
            }
            return new List<Vector3>(spawnPoints);
        }

        Vector3 GetRandomSpawnPoint(List<Vector3> spawns) => spawns.GetRandom();
        Vector3 GetFirstSpawnPoint(List<Vector3> spawns) => spawns.FirstOrDefault();

        #endregion
    }
}