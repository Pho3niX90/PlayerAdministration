using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using UnityEngine;
using Anchor = Oxide.Plugins.KingdomUI.Anchor;
using Rgba = Oxide.Plugins.KingdomUI.Rgba;
using UI = Oxide.Plugins.KingdomUI.UIMethods;

// TODO: Add QuickSmelt functionality
// TODO: Add quicksmelt configuration options

namespace Oxide.Plugins {
    [Info("Kingdom Wars", "Pho3niX90", "0.0.1")]
    [Description("Custom gamemode that changes values of gathering/smelting/crafting based on a current phase.")]
    public class KingdomWars : RustPlugin {
        #region Declaration

        // Config, Data
        private static ConfigFile CFile;
        private static DataFile DFile;

        // Permissions
        private const string AdminPerm = "kingdomwars.admin";

        // Timers
        private Timer UiTimer;
        private Timer HeliTimer;
        private Timer DropTimer;

        // Temp storage
        private Dictionary<string, float> DefaultBlueprints = new Dictionary<string, float>();

        // UI stuff
        private const string MainContainer = "Main.Container";
        private const string CountdownContainer = "Countdown.Container";
        private const string CountdownTimer = "Countdown.Timer";
        private const string PhaseContainer = "Phase.Container";
        private const string PhaseContent = "Phase.Content";
        private CuiElementContainer CachedContainer;
        private Dictionary<ulong, BasePlayer> UiPlayers = new Dictionary<ulong, BasePlayer>();

        #endregion

        #region Configuration

        private class ConfigFile {
            public TimeSettings TimeSettings;

            public UiSettings UiSettings;

            public static ConfigFile DefaultConfig() {
                return new ConfigFile {
                    TimeSettings = new TimeSettings(),
                    UiSettings = new UiSettings()
                };
            }
        }

        private class UiSettings {
            public bool Enabled { get; set; }
            public Rgba PrimaryColor { get; set; }
            public Rgba SecondaryColor { get; set; }
            public ConfigAnchor CountdownAnchor { get; set; }
            public ConfigAnchor PhaseAnchor { get; set; }

            public UiSettings() {
                Enabled = true;
                PrimaryColor = new Rgba(0, 0, 0, 0.3f);
                SecondaryColor = new Rgba(255, 255, 255, 255);
                CountdownAnchor = new ConfigAnchor(new Anchor(0.01f, 0.93f), new Anchor(0.1f, 0.99f));
                PhaseAnchor = new ConfigAnchor(new Anchor(0.01f, 0.88f), new Anchor(0.1f, 0.92f));
            }
        }

        private class ConfigAnchor {
            public Anchor Min { get; set; }
            public Anchor Max { get; set; }

            public ConfigAnchor() {
            }

            public ConfigAnchor(Anchor min, Anchor max) {
                Min = min;
                Max = max;
            }
        }

        private class TimeSettings {
            [JsonProperty("Build phase length (minutes)")]
            public float Build;

            [JsonProperty("Fight phrase length (minutes)")]
            public float Fight;

            public TimeSettings() {
                Build = 1;
                Fight = 1;
            }
        }

        private class Rotor {
            public float MainHealth;
            public float TailHealth;

            public Rotor() {
                MainHealth = 75;
                TailHealth = 100;
            }
        }

        protected override void LoadDefaultConfig() {
            PrintWarning("Generating default configuration file...");
            CFile = ConfigFile.DefaultConfig();
        }

        protected override void LoadConfig() {
            base.LoadConfig();
            try {
                CFile = Config.ReadObject<ConfigFile>();
                if (CFile == null) Regenerate();
            } catch { Regenerate(); }
        }

        protected override void SaveConfig() => Config.WriteObject(CFile);

        private void Regenerate() {
            PrintWarning($"Configuration file at 'oxide/config/{Name}.json' seems to be corrupt! Regenerating...");
            CFile = ConfigFile.DefaultConfig();
            SaveConfig();
        }

        #endregion

        #region Data

        private class DataFile {
            public PhaseInfo PhaseInfo = new PhaseInfo();
        }

        private class PhaseInfo {
            public bool BuildPhase;
            public bool FightPhase;
            public DateTime StartTime;

            public bool KingWarActive { get { return BuildPhase || FightPhase; } }

            public PhaseInfo() {
                BuildPhase = false;
                StartTime = DateTime.MinValue;
            }

            public bool ShouldEnd(float period) => StartTime.AddMinutes(period) < DateTime.UtcNow;

            public double GetSeconds(float period) => (StartTime.AddMinutes(period) - DateTime.UtcNow).TotalSeconds;
        }

        #endregion

        #region Lang

        private struct Msg {
            public const string TimeLeft = "TimeLeft";
            public const string BuildPhase = "BuildPhase";
            public const string FightPhase = "FightPhase";
            public const string BuildPhaseStart = "BuildPhaseStart";
            public const string BuildPhaseEnd = "BuildPhaseEnd";
            public const string FightPhaseStart = "FightPhaseStart";
            public const string FightPhaseEnd = "FightPhaseEnd";
            public const string NoPerm = "NoPerm";
            public const string NoKingWarActive = "NoKingWarActive";
            public const string HellStarted = "HellStarted";
            public const string HellInvalidArgs = "HellInvalidArgs";
            public const string HellNoInt = "HellNoInt";
            public const string InfoCraft = "InfoCraft";
            public const string InfoSmelt = "InfoSmelt";
            public const string InfoGather = "InfoGather";
        }

        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                ["DayFormat"] = "{0}D {1}H",
                ["DaysFormat"] = "{0}D {1}H",
                ["HourFormat"] = "{0}H {1}M",
                ["HoursFormat"] = "{0}H {1}M",
                ["MinFormat"] = "{0}M {1}S",
                ["MinsFormat"] = "{0}M {1}S",
                ["SecsFormat"] = "{0}S",
                [Msg.TimeLeft] = "TIME LEFT",
                [Msg.BuildPhase] = "BUILD PHASE",
                [Msg.FightPhase] = "FIGHT PHASE",
                [Msg.BuildPhaseEnd] = "A build phase has ended!",
                [Msg.FightPhaseEnd] = "A fight phase has ended!",
                [Msg.NoPerm] = "You don't have permission to use that command",
                [Msg.NoKingWarActive] = "There's currently no kingwar active to make hell!",
                [Msg.HellStarted] = "It's turned to hell, {0} helicopters and {1} airdrops incoming!",
                [Msg.HellInvalidArgs] = "Invalid arguments. Usage: /hell <amount>",
                [Msg.HellNoInt] = "The input '<color=orange>{0}</color>' doesn't seem to be a valid integer",
                [Msg.InfoCraft] = "<color=orange>Crafting rate</color> - {0}x",
                [Msg.InfoSmelt] = "<color=orange>Smelting rate</color> - {0}x",
                [Msg.InfoGather] = "<color=orange>Gathering rate</color> - {0}x",
            }, this);
        }

        #endregion

        #region Methods

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private void DestroyTimers() {
            UiTimer?.Destroy();
            HeliTimer?.Destroy();
            DropTimer?.Destroy();
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info) {
            if (info != null && info.InitiatorPlayer != null && entity != null && DFile.PhaseInfo.BuildPhase) {
                BasePlayer player = info.InitiatorPlayer;
                if ((entity is BasePlayer)
                                //|| (entity is BaseNpc)
                                //|| (entity is BaseAnimalNPC)
                                //|| (entity is BuildingBlock)
                                || (entity.GetComponent<Deployable>() != null)
                                //|| entity.PrefabName.Contains("barrel")
                                || (entity is BaseHelicopter)
                                || (entity is MiniCopter || entity is MotorBoat)
                                ) {
                    info.damageTypes.ScaleAll(0);
                }
            }
        }

        private void InitializeBuildPhase(DateTime? startTime = null) {
            DestroyTimers();

            DFile.PhaseInfo.StartTime = startTime ?? DateTime.UtcNow;
            DFile.PhaseInfo.BuildPhase = true;

            Interface.Call("OnBuildPhaseInitiated");
            foreach (BasePlayer player in BasePlayer.activePlayerList) SendReplyWithIcon(player, "Build phase has started, all PVP damage has been turned off.");
            if (CFile.UiSettings.Enabled) {
                InitializeUi();
                UiTimer = timer.Every(1f, RefreshTimer);
            }
        }

        private void EndBuildPhase() {
            DestroyTimers();

            DFile.PhaseInfo.StartTime = DateTime.MinValue;
            DFile.PhaseInfo.BuildPhase = false;

            Interface.Call("OnBuildPhaseEnded");

            DestroyUi();
        }

        private void InitializeFightPhase(DateTime? startTime = null) {
            DestroyTimers();

            DFile.PhaseInfo.StartTime = startTime ?? DateTime.UtcNow;
            DFile.PhaseInfo.FightPhase = true;

            foreach (BasePlayer player in BasePlayer.activePlayerList) SendReplyWithIcon(player, "Fight phase has started, all PVP damage has been turned on.");

            Interface.Call("OnFightPhaseInitiated");

            if (CFile.UiSettings.Enabled) {
                InitializeUi();
                UiTimer = timer.Every(1f, RefreshTimer);
            }
        }

        private void EndFightPhase() {
            DestroyTimers();

            DFile.PhaseInfo.StartTime = DateTime.MinValue;
            DFile.PhaseInfo.FightPhase = false;

            foreach (var player in BasePlayer.activePlayerList)
                PrintToChat(player, Lang(Msg.FightPhaseEnd, player.UserIDString));

            Interface.Call("OnFightPhaseEnded");

            DestroyUi();
        }

        private void CheckPhase() {
            if (DFile.PhaseInfo.KingWarActive) {
                if (DFile.PhaseInfo.BuildPhase && DFile.PhaseInfo.ShouldEnd(CFile.TimeSettings.Build)) {
                    EndBuildPhase();
                    InitializeFightPhase();
                }
                if (DFile.PhaseInfo.FightPhase && DFile.PhaseInfo.ShouldEnd(CFile.TimeSettings.Fight))
                    EndFightPhase();
            }
        }

        #region UI Methods

        private void InitializeUi() {
            foreach (var player in BasePlayer.activePlayerList)
                AddUi(player);
        }

        private void AddUi(BasePlayer player) {
            if (!UiPlayers.ContainsKey(player.userID))
                UiPlayers.Add(player.userID, player);
            CuiHelper.DestroyUi(player, MainContainer);
            CuiHelper.AddUi(player, CachedContainer);
            RefreshTimer();
            RefreshPhase();
        }

        private void DestroyUi() {
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, MainContainer);
        }

        private void ConstructUi() {
            CachedContainer = UI.Container(MainContainer, "0 0 0 0", new Anchor(0, 0), new Anchor(1, 1), "Under");
            UI.Element(CountdownContainer, MainContainer, ref CachedContainer, CFile.UiSettings.CountdownAnchor.Min, CFile.UiSettings.CountdownAnchor.Max, CFile.UiSettings.PrimaryColor.Format());
            UI.Border(new Anchor(0.01f, 0.93f), new Anchor(0.1f, 0.99f), ref CachedContainer, 0.0001f, CFile.UiSettings.SecondaryColor.Format(), MainContainer);
            UI.Text("", CountdownContainer, ref CachedContainer, TextAnchor.UpperCenter, CFile.UiSettings.SecondaryColor.Format(), 13, Lang(Msg.TimeLeft),
                new Anchor(0.1f, 0.5f), new Anchor(0.9f, 0.85f));

            UI.Element(PhaseContainer, MainContainer, ref CachedContainer, CFile.UiSettings.PhaseAnchor.Min, CFile.UiSettings.PhaseAnchor.Max, CFile.UiSettings.PrimaryColor.Format());
            UI.Border(CFile.UiSettings.PhaseAnchor.Min, CFile.UiSettings.PhaseAnchor.Max, ref CachedContainer, 0.0001f, CFile.UiSettings.SecondaryColor.Format(), MainContainer);
        }

        private void RefreshTimer() {
            var period = DFile.PhaseInfo.KingWarActive ? (DFile.PhaseInfo.BuildPhase ? CFile.TimeSettings.Build : CFile.TimeSettings.Fight) : 1;
            if (string.IsNullOrEmpty(GetFormattedTime(DFile.PhaseInfo.GetSeconds(period)))) {
                CheckPhase();
                return;
            }
            var container = DrawTimer();
            foreach (var player in UiPlayers) {
                CuiHelper.DestroyUi(player.Value, CountdownTimer);
                CuiHelper.AddUi(player.Value, container);
            }
        }

        private CuiElementContainer DrawTimer() {
            var period = DFile.PhaseInfo.KingWarActive ? (DFile.PhaseInfo.BuildPhase ? CFile.TimeSettings.Build : CFile.TimeSettings.Fight) : 1;
            var container = UI.Container(CountdownTimer, "0 0 0 0", new Anchor(0.1f, 0.15f), new Anchor(0.9f, 0.5f), CountdownContainer);
            UI.Text("", CountdownTimer, ref container, TextAnchor.LowerCenter, CFile.UiSettings.SecondaryColor.Format(), 11, GetFormattedTime(DFile.PhaseInfo.GetSeconds(period)),
                new Anchor(0, 0), new Anchor(1, 1));
            return container;
        }

        private void RefreshPhase() {
            var container = DrawPhase();
            foreach (var player in UiPlayers) {
                CuiHelper.DestroyUi(player.Value, PhaseContent);
                CuiHelper.AddUi(player.Value, container);
            }
        }

        private CuiElementContainer DrawPhase() {
            var phase = DFile.PhaseInfo.KingWarActive ? (DFile.PhaseInfo.BuildPhase ? Lang(Msg.BuildPhase) : Lang(Msg.FightPhase)) : "N/A";
            var container = UI.Container(PhaseContent, "0 0 0 0", new Anchor(0, 0), new Anchor(1, 1), PhaseContainer);
            UI.Text("", PhaseContent, ref container, TextAnchor.MiddleCenter, CFile.UiSettings.SecondaryColor.Format(), 12, phase, new Anchor(0, 0), new Anchor(1, 1));
            return container;
        }

        #endregion

        private string GetFormattedTime(double time) {
            TimeSpan timeSpan = TimeSpan.FromSeconds(time);
            if (timeSpan.TotalSeconds < 1) return null;

            if (Math.Floor(timeSpan.TotalDays) >= 1)
                return string.Format(timeSpan.Days > 1 ? Lang("DaysFormat", null, timeSpan.Days, timeSpan.Hours) : Lang("DayFormat", null, timeSpan.Days, timeSpan.Hours));
            if (Math.Floor(timeSpan.TotalMinutes) >= 60)
                return string.Format(timeSpan.Hours > 1 ? Lang("HoursFormat", null, timeSpan.Hours, timeSpan.Minutes) : Lang("HourFormat", null, timeSpan.Hours, timeSpan.Minutes));
            if (Math.Floor(timeSpan.TotalSeconds) >= 60)
                return string.Format(timeSpan.Minutes > 1 ? Lang("MinsFormat", null, timeSpan.Minutes, timeSpan.Seconds) : Lang("MinFormat", null, timeSpan.Minutes, timeSpan.Seconds));
            return Lang("SecsFormat", null, timeSpan.Seconds);
        }

        #endregion

        #region Hooks

        private void Init() {
            DFile = Interface.Oxide.DataFileSystem.ReadObject<DataFile>(Name);
            permission.RegisterPermission(AdminPerm, this);
        }

        private void OnServerInitialized() {
            ConstructUi();

            foreach (var defaultBlueprint in ItemManager.bpList) {
                if (!DefaultBlueprints.ContainsKey(defaultBlueprint.targetItem.shortname))
                    DefaultBlueprints.Add(defaultBlueprint.targetItem.shortname, defaultBlueprint.time);
            }
            if (DFile.PhaseInfo.KingWarActive) {
                if (DFile.PhaseInfo.BuildPhase)
                    InitializeBuildPhase(DFile.PhaseInfo.StartTime);
                if (DFile.PhaseInfo.FightPhase)
                    InitializeFightPhase(DFile.PhaseInfo.StartTime);
            }
        }

        private void OnServerSave() => Interface.Oxide.DataFileSystem.WriteObject(Name, DFile);

        private void Unload() {
            DestroyTimers();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerDisconnect(player);
            foreach (var bp in ItemManager.bpList)
                bp.time = DefaultBlueprints[bp.targetItem.shortname];
        }

        private void OnPlayerConnected(BasePlayer player) {
            if (CFile.UiSettings.Enabled && DFile.PhaseInfo.KingWarActive)
                timer.Once(3f, () => AddUi(player));
        }

        private void OnPlayerDisconnect(BasePlayer player) {
            if (UiPlayers.ContainsKey(player.userID))
                UiPlayers.Remove(player.userID);
            CuiHelper.DestroyUi(player, MainContainer);
        }

        void SendReplyWithIcon(BasePlayer player, string format, params object[] args) {
            int cnt = 0;
            string msg = format;
            foreach (var arg in args) {
                msg = msg.Replace("{" + cnt + "}", arg.ToString());
                cnt++;
            }
            Player.Reply(player, msg, 76561199044451528L);
        }
        #endregion

        #region Commands

        [ConsoleCommand("kw.start")]
        private void StartCommand(ConsoleSystem.Arg arg) {
            var userId = arg.Connection?.userid ?? 0;
            if (userId.IsSteamId() && !permission.UserHasPermission(userId.ToString(), AdminPerm)) {
                arg.ReplyWith("You don't have permission to use that command");
                return;
            }
            if (DFile.PhaseInfo.KingWarActive) {
                arg.ReplyWith("You can't start a kingwar when there's already one active!");
                return;
            }
            arg.ReplyWith("You've started a king war!");
            InitializeBuildPhase();
        }

        [ConsoleCommand("kw.end")]
        private void EndCommand(ConsoleSystem.Arg arg) {
            var userId = arg.Connection?.userid ?? 0;
            if (userId.IsSteamId() && !permission.UserHasPermission(userId.ToString(), AdminPerm)) {
                arg.ReplyWith("You don't have permission to use that command");
                return;
            }
            if (!DFile.PhaseInfo.KingWarActive) {
                arg.ReplyWith("You can't end a kingwar when there's not one active!");
                return;
            }
            arg.ReplyWith("You've ended the kingwar!");
            if (DFile.PhaseInfo.BuildPhase)
                EndBuildPhase();
            if (DFile.PhaseInfo.FightPhase)
                EndFightPhase();
        }

        [ConsoleCommand("kw.build")]
        private void BuildPhaseCommand(ConsoleSystem.Arg arg) {
            var userId = arg.Connection?.userid ?? 0;
            if (userId.IsSteamId() && !permission.UserHasPermission(userId.ToString(), AdminPerm)) {
                arg.ReplyWith("You don't have permission to use that command");
                return;
            }
            if (DFile.PhaseInfo.KingWarActive && DFile.PhaseInfo.BuildPhase) {
                arg.ReplyWith("You can't start a build phase when there's already one active!");
                return;
            }
            arg.ReplyWith("You've started a build phase!");
            InitializeBuildPhase();
        }

        [ConsoleCommand("kw.fight")]
        private void FightPhaseCommand(ConsoleSystem.Arg arg) {
            var userId = arg.Connection?.userid ?? 0;
            if (userId.IsSteamId() && !permission.UserHasPermission(userId.ToString(), AdminPerm)) {
                arg.ReplyWith("You don't have permission to use that command");
                return;
            }
            if (DFile.PhaseInfo.KingWarActive && DFile.PhaseInfo.FightPhase) {
                arg.ReplyWith("You can't start a fight phase when there's already one active!");
                return;
            }
            arg.ReplyWith("You've started a fight phase!");
            //EndBuildPhase();
            InitializeFightPhase();
        }
        #endregion

    }
}

namespace Oxide.Plugins.KingdomUI {
    public class UIMethods {
        public static CuiElementContainer Container(string name, string bgColor, Anchor Min, Anchor Max,
            string parent = "Overlay", float fadeOut = 0f, float fadeIn = 0f) {
            var newElement = new CuiElementContainer()
            {
                new CuiElement()
                {
                    Name = name,
                    Parent = parent,
                    FadeOut = fadeOut,
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = bgColor,
                            FadeIn = fadeIn
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = $"{Min.X} {Min.Y}",
                            AnchorMax = $"{Max.X} {Max.Y}"
                        }
                    }
                },
            };
            return newElement;
        }

        public static void Panel(string name, string parent, ref CuiElementContainer container, string bgColor,
            Anchor Min, Anchor Max, bool cursor = false) {
            container.Add(new CuiPanel() {
                Image =
                {
                    Color = bgColor
                },
                CursorEnabled = cursor,
                RectTransform =
                {
                    AnchorMin = $"{Min.X} {Min.Y}",
                    AnchorMax = $"{Max.X} {Max.Y}"
                }
            }, parent, name);
        }

        public static void Label(string name, string parent, ref CuiElementContainer container, Anchor Min, Anchor Max,
            string text, string color = "1 1 1 1", int fontSize = 15, TextAnchor textAnchor = TextAnchor.MiddleCenter,
            string font = "robotocondensed-bold.ttf") {
            container.Add(new CuiLabel() {
                Text =
                {
                    Align = textAnchor,
                    Color = color,
                    Font = font,
                    FontSize = fontSize
                },
                RectTransform =
                {
                    AnchorMin = $"{Min.X} {Min.Y}",
                    AnchorMax = $"{Max.X} {Max.Y}"
                }
            }, parent, name);
        }

        public static void Button(string name, string parent, ref CuiElementContainer container, Anchor Min,
            Anchor Max, string command, string text, string textColor,
            int fontSize, string color = "1 1 1 1", TextAnchor anchor = TextAnchor.MiddleCenter, float fadeOut = 0f,
            float fadeIn = 0f, string font = "robotocondensed-bold.ttf") {
            container.Add(new CuiButton() {
                FadeOut = fadeOut,
                Button =
                {
                    Color = color,
                    Command = command,
                },
                RectTransform =
                {
                    AnchorMin = $"{Min.X} {Min.Y}",
                    AnchorMax = $"{Max.X} {Max.Y}"
                },
                Text =
                {
                    Text = text,
                    Color = textColor,
                    Align = anchor,
                    Font = font,
                    FontSize = fontSize,
                    FadeIn = fadeIn
                }
            }, parent, name);
        }

        public static void Text(string name, string parent, ref CuiElementContainer container, TextAnchor anchor,
            string color, int fontSize, string text,
            Anchor Min, Anchor Max, string font = "robotocondensed-bold.ttf", float fadeOut = 0f,
            float fadeIn = 0f) {
            container.Add(new CuiElement() {
                Name = name,
                Parent = parent,
                FadeOut = fadeOut,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = text,
                        Align = anchor,
                        FontSize = fontSize,
                        Font = font,
                        FadeIn = fadeIn,
                        Color = color
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = $"{Min.X} {Min.Y}",
                        AnchorMax = $"{Max.X} {Max.Y}"
                    }
                }
            });
        }

        public static void Element(string name, string parent, ref CuiElementContainer container, Anchor Min, Anchor Max,
            string bgColor, string material = "", float fadeOut = 0f, float fadeIn = 0f) {
            container.Add(new CuiElement() {
                Name = name,
                Parent = parent,
                FadeOut = fadeOut,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = bgColor,
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = $"{Min.X} {Min.Y}",
                        AnchorMax = $"{Max.X} {Max.Y}"
                    }
                }
            });
        }

        public static void Border(Anchor posMin, Anchor posMax, ref CuiElementContainer container, float borderSize = 0.001f, string color = "1 1 1 1", string parent = "Overlay") {
            Element("", parent, ref container, posMin, new Anchor(posMax.X, posMin.Y + (borderSize * 2)), "1 1 1 1");
            Element("", parent, ref container, new Anchor(posMin.X, posMax.Y - (borderSize * 2)), posMax, "1 1 1 1");
            Element("", parent, ref container, posMin, new Anchor(posMin.X + borderSize, posMax.Y), "1 1 1 1");
            Element("", parent, ref container, new Anchor(posMax.X, posMin.Y), new Anchor(posMax.X + borderSize, posMax.Y), "1 1 1 1");
        }
    }

    public class Anchor {
        public float X { get; set; }
        public float Y { get; set; }

        public Anchor() {
        }

        public Anchor(float x, float y) {
            X = x;
            Y = y;
        }

        public static Anchor operator +(Anchor first, Anchor second) {
            return new Anchor(first.X + second.X, first.Y + second.Y);
        }

        public static Anchor operator -(Anchor first, Anchor second) {
            return new Anchor(first.X - second.X, first.Y - second.Y);
        }
    }

    public class Rgba {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public Rgba() {
        }

        public Rgba(float r, float g, float b, float a) {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public string Format() {
            return $"{R / 255} {G / 255} {B / 255} {A}";
        }
    }
}