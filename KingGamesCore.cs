using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Games Core", "Pho3niX90", "0.1.1")]
    [Description("")]
    public class KingGamesCore : RustPlugin
    {
        #region Vars
        [PluginReference] Plugin Spawns;
        [PluginReference] Plugin NetGroupSwitch;
        private static KingGamesCore plugin;
        private static List<string> teamColors = new List<string>();
        private Dictionary<string, string> games;
        private Dictionary<string, string> teams;
        #endregion

        #region Oxide Hooks
        private void Init() {
            plugin = this;
            ConVar.Decay.tick = float.MaxValue;

            cmd.AddChatCommand("join", this, cmdJoin);
            cmd.AddChatCommand("joingui", this, cmdJoinGUI);
            cmd.AddChatCommand("leave", this, cmdLeave);
            cmd.AddChatCommand("lobby", this, cmdLeave);
            cmd.AddChatCommand("ready", this, cmdReady);
            cmd.AddChatCommand("r", this, cmdReady);

            //colors must be rgba, float
            teamColors.Add("1 0 0 1");
            teamColors.Add("0 0 1 1");
            teamColors.Add("0 1 0 1");
            teamColors.Add("1 0 1 1");
            teamColors.Add("0 1 1 1");
        }

        private void OnServerInitialized() {
            DisableItemCondition();
            ConfigureServer();

            timer.Once(1f, () => {
                MoveAllPlayers();
                RefreshFloatingTexts();
            });

            timer.Every(config.floatingTextRefreshRate, RefreshFloatingTexts);

            if (config.sleepersRemoveCycle > 0) {
                timer.Every(config.sleepersRemoveCycle, TickRemoveSleepers);
            }
        }

        private void OnEntitySpawned(LootableCorpse corpse) {
            if (corpse == null || corpse.IsDestroyed) return;
            foreach (var container in corpse.containers) {
                if (container == null) continue;
                foreach (var item in container.itemList.ToArray()) {
                    item.GetHeldEntity()?.Kill();
                    item.DoRemove();
                }
            }
            corpse.Kill();
        }

        private BasePlayer.SpawnPoint OnPlayerRespawn(BasePlayer player) => new BasePlayer.SpawnPoint { pos = GetSpawnPosition(player) };

        private void OnPlayerRespawned(BasePlayer player) => RefreshPlayer(player);

        private void OnPlayerDisconnected(BasePlayer player) => LeaveGame(player);

        object OnEntityTakeDamage(BasePlayer player, HitInfo info) => API_CanGetDamageFrom(player, info);

        private void OnPlayerDeath(BasePlayer player, HitInfo info) => AutoRespawn(player);

        private void OnPlayerSleep(BasePlayer player) => AutoWakeup(player);

        private object CanDropActiveItem(BasePlayer player) {
            return false;
        }

        //private object OnItemAction(Item item, string action, BasePlayer player)
        //{
        //    return action == "drop" ? true : (object) null;
        //}

        private object OnServerMessage(string m, string n) {
            return m.Contains("gave") && n == "SERVER" ? (object)true : null;
        }
        #endregion

        #region Commands
        [ConsoleCommand("join")]
        private void cmdConsoleJoin(ConsoleSystem.Arg arg) {
            BasePlayer player = arg.Player();
            Puts("throwing command");
            if (player == null) return;
            Puts("throwing command2");
            cmdJoin(player, "join", arg.Args);
        }
        private void cmdJoin(BasePlayer player, string command, string[] args) {
            plugin.Puts("Joining");
            var name = args?.Length > 0 ? args[0].ToLower() : "null";
            foreach (var x in args) {
                plugin.Puts(x.ToString());
            }
            BasePlayer joiner = player;
            int teamNumber = 0;
            if (args?.Length == 2) int.TryParse(args[1].ToString(), out teamNumber);
            if (args?.Length == 3) {
                BasePlayer bot = BasePlayer.FindBot(ulong.Parse(args[2]));
                if (bot != null) {
                    joiner = bot;
                }
            }
            plugin.NetGroupSwitch?.Call("SwitchDim", joiner, name);
            timer.Once(0.3f, () => {
                API_JoinGame(joiner, name, teamNumber);
            });
        }

        private void cmdJoinGUI(BasePlayer player, string command, string[] args) {
            ///create GUI
            ///
            if (args.Length == 0) return;
            string gameType = args[0];
            string arenaName = args[1];

            CreateJoinGUI(player, gameType, arenaName);
        }

        private void cmdLeave(BasePlayer player, string command, string[] args) => API_LeaveGame(player);

        private void cmdReady(BasePlayer player, string command, string[] args) => API_ReadyToGame(player);
        #endregion

        #region Helper Methods
        #region Player
        private static Vector3 GetLobbyPosition() {
            List<Vector3> spawnPoints = plugin.Spawns?.Call("LoadSpawnFile", "start_area") as List<Vector3>;
            return spawnPoints.GetRandom();
        }

        private static Vector3 GetSpawnPosition(BasePlayer player) {
            var position = API_GetCustomSpawnPosition(player);
            return position == new Vector3() ? GetLobbyPosition() : position;
        }

        private void AutoWakeup(BasePlayer player) {
            timer.Once(0.2f, () => {
                if (!player.IsValid() || !player.IsConnected || player.IsDead()) {
                    return;
                }

                if (player.IsReceivingSnapshot) {
                    timer.Once(0.5f, () => AutoWakeup(player));
                    return;
                }

                player.EndSleeping();
            });
        }

        private static void Teleport(BasePlayer player, Vector3 position) {
            if (player.IsAlive()) {
                player.ConsoleMessage($"Teleporting to {position}");
                player.EnsureDismounted();
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.Teleport(position);
                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate();
                //player.ClientRPCPlayer(null, player, "StartLoading");
                player.SendFullSnapshot();
            } else {
                player.RespawnAt(position, new Quaternion());
            }
        }

        private void RefreshPlayer(BasePlayer player) {
            if (!player.IsConnected || !player.IsAlive()) return;

            player.inventory.Strip();

            Teleport(player, GetSpawnPosition(player));

            // get kits
            var kitName = API_GetKitName(player);
            var kitNameClothing = API_GetKitClothing(player);
            if (string.IsNullOrEmpty(kitName)) {
                kitName = config.lobbyKit;
            }

            timer.Once(0.4f, () => {
                player.ConsoleMessage($"Giving player kit '{kitName}'");
                Interface.CallHook("GiveKit", player, kitName);
                if (kitNameClothing != null) {
                    Interface.CallHook("GiveKit", player, kitNameClothing);
                    player.ConsoleMessage($"Giving player clothing kit '{kitNameClothing}'");
                }
                ResetPlayerStats(player);
            });
            player.SendNetworkUpdateImmediate();
        }

        private void ResetPlayerStats(BasePlayer player) {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
            player.health = 100f;
            player.metabolism.hydration.max = 500;
            player.metabolism.hydration.value = 500;
            player.metabolism.calories.max = 500;
            player.metabolism.calories.value = 500;
            player.metabolism.bleeding.value = 0;
            player.metabolism.radiation_level.value = 0;
            player.metabolism.radiation_poison.value = 0;
            player.metabolism.SendChangesToClient();
        }

        private static void TickRemoveSleepers() {
            var sleepers = UnityEngine.Object.FindObjectsOfType<BasePlayer>().Where(x => x.IsSleeping() && !x.IsConnected);
            foreach (var sleeper in sleepers) {
                sleeper.inventory.Strip();
                sleeper.Kill();
            }
        }

        private void AutoRespawn(BasePlayer player) {
            timer.Once(0.1f, () => {
                if (player.IsValid() && player.IsDead() && player.IsConnected) {
                    player.Respawn();
                }
            });
        }

        private void MoveAllPlayers() {
            foreach (var player in BasePlayer.activePlayerList) RefreshPlayer(player);
        }
        #endregion

        #region Items
        private static void DisableItemCondition() {
            foreach (var item in ItemManager.itemList) item.condition.enabled = false;
        }
        #endregion

        #region Environment
        private void ConfigureServer() {
            var time = UnityEngine.Object.FindObjectOfType<TOD_Time>();
            if (time != null) {
                time.ProgressTime = false;
            }

            Server.Command("env.time 12");
            Server.Command("weather.fog 0");
            Server.Command("weather.rain 0");
            Server.Command("antihack.terrain_protection 0");
            Server.Command("antihack.terrain_kill false");
            Server.Command("writecfg");
        }

        private static void GetTeams() {
            var teams = Interface.CallHook("GetAllTeams");
        }
        private static void RefreshFloatingTexts() {
            var warps = Interface.CallHook("GetAllZonesForFloatingText") as Dictionary<Vector3, string> ?? new Dictionary<Vector3, string>();
            plugin.games = Interface.CallHook("GetAllGames") as Dictionary<string, string> ?? new Dictionary<string, string>();
            plugin.teams = Interface.CallHook("GetAllTeams") as Dictionary<string, string> ?? new Dictionary<string, string>();
            var result = new Dictionary<Vector3, string>();

            foreach (var value in warps) {
                var position = value.Key;
                var gameName = value.Value;
                var info = string.Empty;
                if (plugin.games.TryGetValue(gameName, out info)) {
                    result.Add(position, info);
                }
            }

            foreach (var value in result) {
                var position = value.Key + config.warpsTextOffset;
                var text = value.Value;

                foreach (var player in BasePlayer.activePlayerList) {
                    if (Vector3.Distance(player.transform.position, position) > config.maxFloatingTextDistance) {
                        continue;
                    }

                    if (player.Connection.authLevel == 0) {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendNetworkUpdateImmediate();
                    }

                    player.SendConsoleCommand("ddraw.text", config.floatingTextRefreshRate, Color.white, position, text);

                    if (player.Connection.authLevel == 0) {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                        player.SendNetworkUpdateImmediate();
                    }
                }
            }
        }
        #endregion

        #endregion

        #region Configuration

        private static ConfigData config = new ConfigData();
        private class RespawnInfo
        {
            public Vector3 position;
            public string kitName;
            public string kitNameClothing;
        }
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Sleepers remove cycle")]
            public int sleepersRemoveCycle = 300;

            [JsonProperty(PropertyName = "Floating text refresh rate")]
            public int floatingTextRefreshRate = 3;

            [JsonProperty(PropertyName = "Floating text maximal distance")]
            public int maxFloatingTextDistance = 100;

            [JsonProperty(PropertyName = "Warps text offset")]
            public Vector3 warpsTextOffset = new Vector3(0f, 0.5f, 0f);

            [JsonProperty(PropertyName = "Lobby position")]
            public Vector3 lobbyPosition = new Vector3();

            [JsonProperty(PropertyName = "Lobby size (m)")]
            public float lobbySize = 9f;

            [JsonProperty(PropertyName = "Lobby kit")]
            public string lobbyKit = "kitLobby";
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

        #region API
        private static Vector3 API_GetCustomSpawnPosition(BasePlayer player) {
            var obj = Interface.CallHook(nameof(GetCustomSpawnPosition), player);
            return (Vector3?)obj ?? new Vector3();
        }

        private static string API_GetKitName(BasePlayer player) {
            object obj = Interface.CallHook(nameof(GetCustomKitName), player);
            return ((string)obj);
        }

        private static string API_GetKitClothing(BasePlayer player) {
            object obj = Interface.CallHook(nameof(GetCustomKitNameClothing), player);
            return ((string)obj);
        }

        private static object API_CanGetDamageFrom(BasePlayer victim, HitInfo info) {
            // null is true

            //Below must be handled by event to check if players are participating
            if (info == null || info.InitiatorPlayer == null) {
                if (info != null) info.damageTypes.ScaleAll(0f);
                plugin.Puts("[KGC] Can't get damage because: initiator == null");
                return true;
            }
            //
            //if (info == null) {
            //    plugin.Puts("[KGC] Can't get damage because: info == null");
            //    return false;
            //}

            var obj = Interface.CallHook(nameof(CanGetDamageFrom), victim, info);
            if (obj != null) plugin.Puts(obj.ToString());
            return obj;
        }

        private static void API_JoinGame(BasePlayer player, string name, int teamNumber) {
            Interface.CallHook(nameof(JoinGame), player, name, teamNumber);
        }

        private static void API_LeaveGame(BasePlayer player) {
            Interface.CallHook(nameof(LeaveGame), player);
            plugin.NextTick(() => Interface.CallHook("RefreshPlayer", player));
        }

        private static void API_ReadyToGame(BasePlayer player) {
            Interface.CallHook(nameof(ReadyToGame), player);
        }

        private object GetCustomSpawnPosition(BasePlayer player) { return null; }
        private string GetCustomKitName(BasePlayer player) { return null; }
        private string GetCustomKitNameClothing(BasePlayer player) { return null; }
        private object CanGetDamageFrom(BasePlayer victim, BasePlayer initiator) { return null; }
        private object CanJoinGame(BasePlayer player) { return null; }
        private void JoinGame(BasePlayer player, string name, int teamNumber) // Join game with name
        {
            var obj = Interface.CallHook(nameof(CanJoinGame), player);
            //player.ConsoleMessage($"[Minigames] Can join game '{name}' : {obj == null} (Current game: {obj})");
            if (obj == null) {
                // Join game here exactly
            }
        }

        private void LeaveGame(BasePlayer player) { }
        private void ReadyToGame(BasePlayer player) { }

        #endregion

        #region UI
        UIColor shadowColor = new UIColor(0.1, 0.1, 0.1, 0.8);
        UIColor noticeColor = new UIColor(0.85, 0.85, 0.85, 0.1);
        int SimpleUI_FontSize = 150;
        float SimpleUI_Top = 0f;
        float SimpleUI_Left = 0f;
        float SimpleUI_MaxWidth = 1f;
        float SimpleUI_MaxHeight = 1f;
        const float SimpleUI_HideTimer = 0.9f;
        List<string> tags = new List<string> {
            "</color>",
            "</size>",
            "<i>",
            "</i>",
            "<b>",
            "</b>"
        };

        List<Regex> regexTags = new List<Regex> {
            new Regex(@"<color=.+?>", RegexOptions.Compiled),
            new Regex(@"<size=.+?>", RegexOptions.Compiled)
        };

        void UICoreMessage(BasePlayer player, string message, float timeout = SimpleUI_HideTimer) {
            float fadeIn = 0.1f;

            UIObject ui = new UIObject();

            ui.AddText("DeathNotice_DropShadow",
                SimpleUI_Left + 0.001, SimpleUI_Top + 0.001,
                SimpleUI_MaxWidth, SimpleUI_MaxHeight,
                shadowColor,
                StripTags(message),
                SimpleUI_FontSize,
                "Hud.Menu", 3, fadeIn, fadeIn);

            ui.AddText("DeathNotice", SimpleUI_Left, SimpleUI_Top, SimpleUI_MaxWidth, SimpleUI_MaxHeight, noticeColor, message, SimpleUI_FontSize, "Hud.Menu", 3, fadeIn, fadeIn);

            ui.Draw(player);
            timer.Once(timeout, () => {
                ui.Destroy(player);
            });
        }

        Dictionary<string, int> countdowns = new Dictionary<string, int>();
        void CountDown(string arena, BasePlayer player, bool end = false) {
            string key = $"{arena}_{player.net.ID}";
            if (countdowns.ContainsKey(key) && countdowns[key] < 1 && !end) {
                countdowns.Remove(key);
                timer.Once(0, () => CountDown(key, player, true));
                return;
            } else if (!countdowns.ContainsKey(key) && !end) {
                countdowns.Add(key, 5);
            }

            if (end) {
                UICoreMessage(player, $"<color=green>START</color>");
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", player.GetNetworkPosition());
                //timer.Once(1, () => CountDown(arena, player, end));
            } else {
                UICoreMessage(player, $"<color=red>{countdowns[key]--}</color>");
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.lock.prefab", player.GetNetworkPosition());
                timer.Once(1, () => CountDown(arena, player));
            }
        }

        string StripTags(string original) {
            foreach (string tag in tags)
                original = original.Replace(tag, "");

            foreach (Regex regexTag in regexTags)
                original = regexTag.Replace(original, "");

            return original;
        }
        class UIColor
        {
            string color;

            public UIColor(double red, double green, double blue, double alpha) {
                color = $"{red} {green} {blue} {alpha}";
            }

            public override string ToString() => color;
        }
        class UIObject
        {
            List<object> ui = new List<object>();
            List<string> objectList = new List<string>();

            public UIObject() {
            }

            string RandomString() {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                List<char> charList = chars.ToList();

                string random = "";

                for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
                    random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];

                return random;
            }

            public void Draw(BasePlayer player) {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine));
            }

            public void Destroy(BasePlayer player) {
                foreach (string uiName in objectList)
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", uiName);
            }

            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "Hud", int alignmode = 0, float fadeIn = 0f, float fadeOut = 0f) {
                //name = name + RandomString();
                text = text.Replace("\n", "{NEWLINE}");
                string align = "";

                switch (alignmode) {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; };
                }

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"fadeOut", fadeOut.ToString()},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.ToString()},
                                {"align", align},
                                {"fadeIn", fadeIn.ToString()}
                            },
                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left} {((1 - top) - height)}"},
                                {"anchormax", $"{(left + width)} {(1 - top)}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }
        }

        private const string elemMain = "KingGamesCore.UI.Main";
        void CreateJoinGUI(BasePlayer player, string gameType, string arenaName) {
            var containerTop = new CuiElementContainer();

            // Main panel
            containerTop.Add(new CuiElement {
                Name = elemMain,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0.8"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMax = "0.7 0.6",
                        AnchorMin = "0.3 0.4"
                    },
                    new CuiNeedsCursorComponent()
                }
            });
            //close button
            containerTop.Add(new CuiButton {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "X",
                    FontSize = 15,
                },
                Button =
                {
                    Close = elemMain,
                    Color = "1 0 0 1"
                },
                RectTransform =
                {
                    AnchorMin = "1 1",
                    AnchorMax = "1 1",
                    OffsetMin = "-25 -25",
                    OffsetMax = "-5 -5"
                }
            }, elemMain);

            //Teams Buttons
            int teamCount = 2; //hardcoded for now.
            float sidePadding = 0.03f;
            float spacing = 0.02f;
            float buttonWidth = ((1f - (sidePadding * 2) - (spacing * (teamCount - 1))) / teamCount);
            float startX = 0f + sidePadding;

            containerTop.Add(new CuiLabel {
                Text =
                    {
                        Text = arenaName.Length > 0 ? arenaName : "Err",
                        FontSize = 20,
                        Align = TextAnchor.MiddleLeft
                    },
                RectTransform = { AnchorMin = $"0.05 0.73", AnchorMax = $"0.95 0.95" }
            }, elemMain);

            string teamScores;
            plugin.teams.TryGetValue(arenaName, out teamScores);
            int team1 = 0;
            int team2 = 0;

            if (teamScores != null && teamScores.Length > 0) {
                int index = teamScores.IndexOf("|", StringComparison.Ordinal);
                int.TryParse(teamScores.Substring(0, index), out team1);
                int.TryParse(teamScores.Substring(index), out team2);
            }

            for (var i = 1; i <= teamCount; i++) {
                int players = i == 1 ? team1 : i == 2 ? team2 : 0;
                string color = teamColors[Math.Min(i, teamColors.Count) - 1];
                CreateButton(ref containerTop, elemMain, color, $"Team {i} ({players})", 20, $"{startX} 0.40", $"{startX + buttonWidth} 0.70", $"join {arenaName} {i}");
                startX += buttonWidth + spacing;
            }

            CreateButton(ref containerTop, elemMain, "0 15 0 1", "Spectate", 20, $"{0f + sidePadding} 0.05", $"{1f - sidePadding} 0.35", $"join {arenaName} 0");
            CuiHelper.AddUi(player, containerTop);
        }

        void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter) {
            container.Add(new CuiButton {
                Button = { Color = color, Command = command, FadeIn = 0f, Close = panel },
                RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                Text = { Text = text, FontSize = size, Align = align }
            }, panel);
        }
        #endregion
    }
}