using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Scrim Core", "Pho3niX90", "0.0.9")]
    [Description("")]
    public class KingGamesCore : RustPlugin
    {
        [PluginReference] Plugin Spawns;
        #region Vars

        private static int getMask => LayerMask.GetMask("Construction", "Terrain");
        private static KingGamesCore plugin;

        #endregion

        #region Oxide Hooks

        private void Init() {
            plugin = this;
            cmd.AddChatCommand("lobby", this, nameof(cmdControlChat));
            cmd.AddChatCommand("join", this, nameof(cmdControlChat));
            cmd.AddChatCommand("leave", this, nameof(cmdControlChat));
            cmd.AddChatCommand("r", this, nameof(cmdControlChat));
            //cmd.AddChatCommand("ar", this, nameof(cmdControlChat));
            cmd.AddChatCommand("ready", this, nameof(cmdControlChat));
            ConVar.Decay.tick = float.MaxValue;
        }

        private void OnServerInitialized() {
            ModifyItems();
            FreezeTime();

            timer.Once(1f, () => {
                MoveAllPlayers();
                RefreshFloatingTexts();
            });

            timer.Every(config.floatingTextRefreshRate, RefreshFloatingTexts);

            if (config.sleepersRemoveCycle > 0) {
                timer.Every(config.sleepersRemoveCycle, TickRemoveSleepers);
            }
        }

        private BasePlayer.SpawnPoint OnPlayerRespawn(BasePlayer player) {
            return new BasePlayer.SpawnPoint {
                pos = GetSpawnPosition(player)
            };
        }

        private void OnEntitySpawned(LootableCorpse corpse) {
            if (corpse == null || corpse.IsDestroyed) return;
            foreach (var container in corpse.containers) {
                if (container == null) continue;
                CleanContainer(container);
            }
            corpse.Kill();
        }

        private void OnPlayerRespawned(BasePlayer player) {
            RefreshPlayer(player);
        }

        private void OnPlayerDisconnected(BasePlayer player) {
            LeaveMiniGame(player);
        }

        object OnEntityTakeDamage(BasePlayer player, HitInfo info) {
            return CheckDamage(player, info);
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info) {
            AutoRespawn(player);
        }

        private void OnPlayerSleep(BasePlayer player) {
            AutoWakeup(player);
        }

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

        private void OnServerSave() {
            Server.Command("writecfg");
        }

        #endregion

        #region Commands

        private void cmdControlChat(BasePlayer player, string command, string[] args) {
            switch (command.ToLower()) {
                case "join":
                    var name = args?.Length > 0 ? args[0].ToLower() : "null";
                    BasePlayer joiner = player;
                    if (args?.Length == 2) {
                        BasePlayer bot = BasePlayer.Find(args[1]);
                        if (bot != null) {
                            joiner = bot;
                        }
                    }
                    API_JoinMiniGame(joiner, name);
                    break;

                case "lobby":
                case "leave":
                    API_LeaveMiniGame(player);
                    break;


                case "r":
                case "ready":
                    API_ReadyToMiniGame(player);
                    break;
            }
        }

        #endregion

        #region Core

        private void MoveAllPlayers() {
            foreach (var player in BasePlayer.activePlayerList) {
                RefreshPlayer(player);
            }
        }

        private static object CheckDamage(BasePlayer player, HitInfo info) {
            var initiator = info?.InitiatorPlayer;
            //if (!API_CanGetDamageFrom(player, initiator, info)) {
            //    info.damageTypes.ScaleAll(0f);
            //}
            return API_CanGetDamageFrom(player, initiator, info);
        }

        private static void CleanContainer(ItemContainer container) {
            foreach (var item in container.itemList.ToArray()) {
                item.GetHeldEntity()?.Kill();
                item.DoRemove();
            }
        }

        private void AutoRespawn(BasePlayer player) {
            timer.Once(0.2f, () => {
                if (player.IsValid() && player.IsDead() && player.IsConnected) {
                    player.Respawn();
                }
            });
        }

        private void AutoWakeup(BasePlayer player) {
            timer.Once(0.2f, () => {
                if (!player.IsValid() || !player.IsConnected || player.IsDead()) {
                    return;
                }

                if (player.IsReceivingSnapshot) {
                    timer.Once(1f, () => AutoWakeup(player));
                    return;
                }

                player.EndSleeping();
            });
        }

        private static void TickRemoveSleepers() {
            var sleepers = UnityEngine.Object.FindObjectsOfType<BasePlayer>().Where(x => x.IsSleeping() && !x.IsConnected);
            foreach (var sleeper in sleepers) {
                sleeper.inventory.Strip();
                sleeper.Kill();
            }
        }

        private static void Teleport(BasePlayer player, Vector3 position) {
            player.ConsoleMessage($"Teleporting to {position}");
            player.EnsureDismounted();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.Teleport(position);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate();
            //player.ClientRPCPlayer(null, player, "StartLoading");
            player.SendFullSnapshot();
        }

        private static void ModifyItems() {
            foreach (var item in ItemManager.itemList) {
                item.condition.enabled = false;
            }
        }

        private void RefreshPlayer(BasePlayer player) {
            Puts($"Refreshing player {player.displayName}");
            if (!player.IsConnected) {
                Puts($"Player either not connected");
                return;
            }
            if (!player.IsAlive()) {
                Puts($"Player either not alive");
                return;
            }

            if (player.IsWounded()) {
                player.StopWounded();
            }

            // --- Params --- //
            player.health = 100f;
            player.metabolism.hydration.max = 5000;
            player.metabolism.hydration.value = 5000;
            player.metabolism.calories.max = 5000;
            player.metabolism.calories.value = 5000;
            player.metabolism.bleeding.max = 0;
            player.metabolism.temperature.max = 30;
            player.metabolism.temperature.min = 30;
            player.metabolism.temperature.value = 30;

            // --- Position --- //
            var position = GetSpawnPosition(player);
            if (player.IsAlive()) {
                Puts($"Teleporting {player.displayName} to {position}");
                Teleport(player, position);
            } else {
                Puts($"Respawning {player.displayName} at {position}");
                player.RespawnAt(position, new Quaternion());
            }

            // --- Items --- //
            var kitName = API_GetKitName(player);
            var kitNameClothing = API_GetKitClothing(player);
            if (string.IsNullOrEmpty(kitName)) {
                kitName = config.lobbyKit;
            }

            player.inventory.Strip();
            timer.Once(0.2f, () => {
                player.ConsoleMessage($"Giving player kit '{kitName}'");
                Interface.CallHook("GiveKit", player, kitName);
                if (kitNameClothing != null) {
                    Interface.CallHook("GiveKit", player, kitNameClothing);
                    player.ConsoleMessage($"Giving player clothing kit '{kitNameClothing}'");
                }
            });

            player.SendNetworkUpdateImmediate();
        }

        private static Vector3 GetSpawnPosition(BasePlayer player) {
            var position = API_GetCustomSpawnPosition(player);
            if (position == new Vector3()) {
                position = GetLobbyPosition();
            }
            return position;
        }

        private void FreezeTime() {
            var time = UnityEngine.Object.FindObjectOfType<TOD_Time>();
            if (time != null) {
                time.ProgressTime = false;
            }

            Server.Command("env.time 12");
            Server.Command("weather.fog 0");
            Server.Command("weather.rain 0");
            Server.Command("antihack.terrain_protection 0");
            Server.Command("antihack.terrain_kill false");
        }

        private static Vector3 GetLobbyPosition() {
            List<Vector3> spawnPoints = plugin.Spawns?.Call("LoadSpawnFile", "start_area") as List<Vector3>;
            return spawnPoints.GetRandom();
        }

        private static void RefreshFloatingTexts() {
            var warps = Interface.CallHook("GetAllZonesForFloatingText") as Dictionary<Vector3, string> ?? new Dictionary<Vector3, string>();
            var games = Interface.CallHook("GetAllGames") as Dictionary<string, string> ?? new Dictionary<string, string>();
            var result = new Dictionary<Vector3, string>();

            foreach (var value in warps) {
                var position = value.Key;
                var gameName = value.Value;
                var info = string.Empty;
                if (games.TryGetValue(gameName, out info)) {
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

        #region Configuration | 2.0.0

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

        #region Minigames API 1.0.0

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

        private static object API_CanGetDamageFrom(BasePlayer victim, BasePlayer initiator, HitInfo info) {
            // null is true


            //Below must be handled by event to check if players are participating
            //if (initiator == null) {
            //    plugin.Puts("[KGC] Can't get damage because: initiator == null");
            //    return false;
            //}
            //
            //if (info == null) {
            //    plugin.Puts("[KGC] Can't get damage because: info == null");
            //    return false;
            //}

            if (victim == initiator) {
                //plugin.Puts("[KGC] Can get damage because: victim == initiator");
                return null;
            }

            var obj = Interface.CallHook(nameof(CanGetDamageFrom), victim, initiator, info);
            return obj;
        }

        private static void API_JoinMiniGame(BasePlayer player, string name) {
            Interface.CallHook(nameof(JoinMiniGame), player, name);
        }

        private static void API_LeaveMiniGame(BasePlayer player) {
            Interface.CallHook(nameof(LeaveMiniGame), player);
            plugin.NextTick(() => {
                Interface.CallHook("RefreshPlayer", player);
            });
        }

        private static void API_ReadyToMiniGame(BasePlayer player) {
            Interface.CallHook(nameof(ReadyToMiniGame), player);
        }

        private object GetCustomSpawnPosition(BasePlayer player) // Return position if needed
        {
            return null;
        }

        private string GetCustomKitName(BasePlayer player) // Return kit name if needed
        {
            return null;
        }

        private string GetCustomKitNameClothing(BasePlayer player) // Return kit name if needed
        {
            return null;
        }

        private object CanGetDamageFrom(BasePlayer victim, BasePlayer initiator) // Return text why can get damage
        {
            return null;
        }

        private object CanJoinMiniGame(BasePlayer player) // Return text with existing game name
        {
            return null;
        }

        private void JoinMiniGame(BasePlayer player, string name) // Join game with name
        {
            var obj = Interface.CallHook(nameof(CanJoinMiniGame), player);
            //player.ConsoleMessage($"[Minigames] Can join game '{name}' : {obj == null} (Current game: {obj})");
            if (obj == null) {
                // Join game here exactly
            }
        }

        private void LeaveMiniGame(BasePlayer player) // Leave game if possible
        {

        }

        private void ReadyToMiniGame(BasePlayer player) // Mark ready to game if possible
        {

        }

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
        #endregion
    }
}