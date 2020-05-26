// Requires: Arena
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System.IO;

namespace Oxide.Plugins
{
    [Info("ArenaUI", "k1lly0u", "0.1.69")]
    [Description("Creates all Arena UI")]
    class ArenaUI : RustPlugin
    {
        #region Fields
        [PluginReference] Arena Arena;
        [PluginReference] ArenaStatistics ArenaStatistics;

        [PluginReference] Plugin ImageLibrary, Kits, Spawns, ZoneManager;

        private static ArenaUI ins;

        private Dictionary<ulong, List<string>> openUi = new Dictionary<ulong, List<string>>();
        private Dictionary<ColorType, string> uiColors = new Dictionary<ColorType, string>();
        private Dictionary<string, string> itemNames = new Dictionary<string, string>();
        private Dictionary<ulong, Arena.ArenaData.EventConfig> eventCreator = new Dictionary<ulong, Arena.ArenaData.EventConfig>();
        private Dictionary<ulong, Arena.ArenaData.EventConfig> eventEditorBackup = new Dictionary<ulong, Arena.ArenaData.EventConfig>();

        private string dataDirectory = $"file://{Interface.Oxide.DataDirectory}{Path.DirectorySeparatorChar}Arena{Path.DirectorySeparatorChar}Images{Path.DirectorySeparatorChar}";

        private bool hasLogo;

        public enum UIPanel { Clock, Menu, Death, Respawn, Class, Scores, Popup, Help, Statistics }
        private enum ColorType { Button_Selected, Button_Deselected, BG_Main_Transparent, BG_Main_Solid, Panel_Main_Solid, Panel_Main_Transparent, Panel_Alt_Solid, Panel_Alt_Transparent, BG_Main_PartialTransparent, Panel_Event_Full, Panel_Event_Open }
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            Interface.Oxide.DataFileSystem.SaveDatafile("Arena/Images/foldercreator");
            lang.RegisterMessages(Messages, this);
        }

        private void OnServerInitialized()
        {
            ins = this;
            Arena.UI = this;
            LoadColors();
            LoadDefaultImages();

            foreach (var item in ItemManager.itemList)
            {
                if (!itemNames.ContainsKey(item.itemid.ToString()))
                    itemNames.Add(item.itemid.ToString(), item.displayName.translated);
            }
        }

        private void Unload()
        {
            if (Arena != null)
                Arena.isUnloading = true;

            foreach (var player in BasePlayer.activePlayerList)
                DestroyAllUI(player);
                        
            Arena.EventManager[] eventGames = UnityEngine.Object.FindObjectsOfType<Arena.EventManager>();
            if (eventGames != null)
            {
                foreach (var eventGame in eventGames)
                    UnityEngine.Object.DestroyImmediate(eventGame);
            }

            Arena.EventPlayer[] eventPlayers = UnityEngine.Object.FindObjectsOfType<Arena.EventPlayer>();
            if (eventPlayers != null)
            {
                foreach (var eventPlayer in eventPlayers)
                    UnityEngine.Object.DestroyImmediate(eventPlayer);
            }
        }
        #endregion

        #region UI Management
        public void AddUI(BasePlayer player, UIPanel panel, CuiElementContainer container)
        {
            if (!openUi.ContainsKey(player.userID))
                openUi.Add(player.userID, new List<string>());
            openUi[player.userID].Add($"arena.{panel}");
            CuiHelper.AddUi(player, container);
        }

        public void AddUI(BasePlayer player, string panel, CuiElementContainer container)
        {
            if (!openUi.ContainsKey(player.userID))
                openUi.Add(player.userID, new List<string>());
            openUi[player.userID].Add(panel);
            CuiHelper.AddUi(player, container);
        }

        public void DestroyUI(BasePlayer player, UIPanel panel)
        {
            if (openUi.ContainsKey(player.userID))
                openUi[player.userID].Remove($"arena.{panel}");
            CuiHelper.DestroyUi(player, $"arena.{panel}");
        }

        public void DestroyUI(BasePlayer player, string panel)
        {
            if (openUi.ContainsKey(player.userID))
                openUi[player.userID].Remove(panel);
            CuiHelper.DestroyUi(player, panel);
        }

        public void DestroyAllUI(BasePlayer player)
        {
            if (openUi.ContainsKey(player.userID))
            {
                foreach (var element in openUi[player.userID])
                    CuiHelper.DestroyUi(player, element);
                openUi[player.userID].Clear();
            }
            else
            {
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Class}");
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Clock}");
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Death}");
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Help}");
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Menu}");
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Respawn}");
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Scores}");
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Statistics}");
                CuiHelper.DestroyUi(player, $"arena.{UIPanel.Popup}");

                for (int i = 0; i < 20; i++)                
                    CuiHelper.DestroyUi(player, $"arena.{UIPanel.Popup} {i}");
            }
        }
        #endregion

        #region Functions
        private void LoadColors()
        {            
            foreach (var color in configData.UISettings.Colors)
                uiColors.Add(ParseType<ColorType>(color.Key), UI.Color(color.Value.Hex, color.Value.Alpha));
            UI.styleColor = uiColors[ColorType.Button_Selected];
        }

        public string StripTags(string str)
        {
            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                str = str.Substring(str.IndexOf("]") + 1).Trim();

            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                StripTags(str);

            return str;
        }

        public string TrimToSize(string str, int size = 18)
        {
            if (str.Length > size)
                str = str.Substring(0, size);
            return str;
        }

        private bool HasEventOfType(string type)
        {
            foreach (var eventGame in Arena.events)
                if (eventGame.Value.config.eventType == type)
                    return true;
            return false;
        }

        private float GetHeight(int num) => 0.89f - (0.06f * num);

        private T ParseType<T>(string type) => (T)Enum.Parse(typeof(T), type, true);        
        #endregion

        #region UI     
        public static class UI
        {
            static public string styleColor;

            static public CuiElementContainer ElementContainer(UIPanel panel, string color, UI4 dimensions, bool useCursor = false, string parent = "Overlay")
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        $"arena.{panel}"
                    }
                };
                return NewElement;
            }
            static public CuiElementContainer Popup(string panelName, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel { Image = {Color = "0 0 0 0" }, RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()} },
                        new CuiElement().Parent = parent,
                        panelName
                    }
                };
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                },
                panelName);
                return container;
            }
            static public void Panel(ref CuiElementContainer container, UIPanel panel, string color, UI4 dimensions, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    CursorEnabled = cursor
                },
                $"arena.{panel}");
            }
            static public void Label(ref CuiElementContainer container, UIPanel panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                $"arena.{panel}");

            }
            static public void OutlineLabel(ref CuiElementContainer container, UIPanel panel, string color, string text, int size, string distance, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, string parent = "Overlay")
            {
                CuiElement textElement = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = $"arena.{panel}",
                    FadeOut = 0.2f,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = text,
                            FontSize = size,
                            Align = TextAnchor.MiddleCenter,
                            FadeIn = 0.2f
                        },
                        new CuiOutlineComponent
                        {
                            Distance = distance,
                            Color = color
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax
                        }
                    }
                };
                container.Add(textElement);
            }
            static public void Button(ref CuiElementContainer container, UIPanel panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                $"arena.{panel}");                
            }
            static public void StyledButton(ref CuiElementContainer container, UIPanel panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = $"{dimensions.xMin} {dimensions.yMin}", AnchorMax = $"{dimensions.xMax} {dimensions.yMax}" },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                $"arena.{panel}");
                container.Add(new CuiPanel
                {
                    Image = { Color = styleColor },
                    RectTransform = { AnchorMin = $"{dimensions.xMin} {dimensions.yMax + 0.0005f}", AnchorMax = $"{dimensions.xMax} {dimensions.yMax + 0.0015f}" }
                },
                $"arena.{panel}");
            }
            static public void Image(ref CuiElementContainer container, UIPanel panel, string png, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = $"arena.{panel}",
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }
            static public void Input(ref CuiElementContainer container, UIPanel panel, string text, int size, string command, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = $"arena.{panel}",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            CharsLimit = 300,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        public class UI4
        {
            public float xMin, yMin, xMax, yMax;
            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }
            public string GetMin() => $"{xMin} {yMin}";
            public string GetMax() => $"{xMax} {yMax}";
        }
        #endregion

        #region User Interface 
         
        #region Main Menu Elements     
        public void OpenMainMenu(BasePlayer player, string eventType, int eventPage = 0)
        {
            CuiElementContainer container = UI.ElementContainer(UIPanel.Menu, uiColors[ColorType.BG_Main_Solid], new UI4(0, 0, 1, 1), true);
            AddMenuHeader(ref container, player.userID, eventType);

            UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], Arena.configData.ServerSettings.EventOnly ? msg("returntolobby", player.userID) : msg("returntogame", player.userID), 16, new UI4(0.01f, 0.94f, 0.13f, 0.98f), "aui.change.element destroy");
            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Main_Solid], new UI4(0.01f, 0.01f, 0.99f, 0.79f));

            var eventList = Arena.events.Where(x => x.Value.config.eventType == eventType).ToArray();
            if (eventList.Length > 10)
            {
                var maxpages = (eventList.Length - 1) / 10 + 1;
                if (eventPage < maxpages - 1)
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], ">\n>\n>", 10, new UI4(0.975f, 0.36f, 0.99f, 0.44f), $"aui.change.element {eventType.Replace(" ", "<><>")} {eventPage + 1}");
                if (eventPage > 0)
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "<\n<\n<", 10, new UI4(0.01f, 0.36f, 0.025f, 0.44f), $"aui.change.element {eventType.Replace(" ", "<><>")} {eventPage - 1}");
            }
            int maxCount = (10 * (eventPage + 1));
            if (maxCount > eventList.Length)
                maxCount = eventList.Length;
            int i = 0;
            int entryCount = 10 * eventPage;
            for (int n = entryCount; n < maxCount; n++)
            {
                Arena.EventManager manager = eventList.ElementAt(n).Value;
                CreateEventEntry(ref container, manager, i, player.userID);
                i++;
            }
            DestroyUI(player, UIPanel.Menu);
            AddUI(player, UIPanel.Menu, container);
        }

        private void AddMenuHeader(ref CuiElementContainer container, ulong playerId, string eventType)
        {
            if (hasLogo)
                UI.Image(ref container, UIPanel.Menu, GetImage("logo"), new UI4(0.005f, 0.85f, 0.995f, 0.99f));

            List<string> eventTypes = Arena.eventTypes.Where(x => HasEventOfType(x)).ToList();

            for (int j = 0; j < eventTypes.Count; j++)
            {
                float xMin = 0.01f + (0.1205f * j);
                float xMax = xMin + 0.12f;
                                
                UI.StyledButton(ref container, UIPanel.Menu, eventType == eventTypes[j] ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], msg(eventTypes[j], playerId), 15, new UI4(xMin, 0.8f, xMax, 0.84f), eventType == eventTypes[j] ? "" : $"aui.change.element {eventTypes[j].Replace(" ", "<><>")} 0");
            }
        }

        void CreateEventEntry(ref CuiElementContainer container, Arena.EventManager manager, int number, ulong playerId)
        {
            var xPos = number > 4 ? 0.2f * (number - 5) : 0.2f * number;
            var yPos = number > 4 ? 0.03f : 0.42f;

            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(xPos + 0.03f, yPos + 0.32f, xPos + 0.17f, yPos + 0.36f));
            UI.Label(ref container, UIPanel.Menu, manager.eventName, 15, new UI4(xPos + 0.03f, yPos + 0.325f, xPos + 0.17f, yPos + 0.355f));

            UI.Image(ref container, UIPanel.Menu, GetImage(string.IsNullOrEmpty(manager.config.eventIcon) ? "placeholder" : manager.config.eventIcon), new UI4(xPos + 0.03f, yPos + 0.065f, xPos + 0.17f, yPos + 0.32f));

            UI.Panel(ref container, UIPanel.Menu, manager.eventPlayers.Count == manager.config.maximumPlayers ? uiColors[ColorType.Panel_Event_Full] : manager.eventPlayers.Count < manager.config.maximumPlayers && manager.eventPlayers.Count > 0 ? uiColors[ColorType.Panel_Event_Open] : uiColors[ColorType.Panel_Alt_Solid], new UI4(xPos + 0.03f, yPos + 0.025f, xPos + 0.17f, yPos + 0.065f));
            UI.Label(ref container, UIPanel.Menu, manager.status == Arena.EventStatus.Finished || manager.status == Arena.EventStatus.Pending ? $"{msg("waitforplayers", playerId)} ({manager.eventPlayers.Count}/{manager.config.maximumPlayers})" : $"{msg("inprogress", playerId)} ({manager.eventPlayers.Count}/{manager.config.maximumPlayers})", 10, new UI4(xPos + 0.03f, yPos + 0.035f, xPos + 0.17f, yPos + 0.065f));

            bool hasPermission = !string.IsNullOrEmpty(manager.config.permission) ? permission.UserHasPermission(playerId.ToString(), manager.config.permission) : true;

            UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], !hasPermission ? msg("donatoronly", playerId) : msg("viewevent", playerId), 15, new UI4(xPos + 0.03f, yPos - 0.00505f, xPos + 0.17f, yPos + 0.03495f), !hasPermission ? "aui.donator.popup" : $"aui.player.controls view {manager.eventName.Replace(" ", "<><>")}");            
        }

        private void ShowEventDetails(BasePlayer player, Arena.EventManager manager, Arena.Team team = Arena.Team.A)
        {   
            CuiElementContainer container = UI.ElementContainer(UIPanel.Menu, uiColors[ColorType.BG_Main_Solid], new UI4(0, 0, 1, 1), true);
            AddMenuHeader(ref container, player.userID, manager.config.eventType);

            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Main_Solid], new UI4(0.01f, 0.01f, 0.99f, 0.79f));
            UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], Arena.configData.ServerSettings.EventOnly ? msg("returntolobby", player.userID) : msg("returntogame", player.userID), 16, new UI4(0.01f, 0.94f, 0.13f, 0.98f), "aui.change.element destroy");
            UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], msg("returntoevents", player.userID), 16, new UI4(0.01f, 0.8985f, 0.13f, 0.9385f), $"aui.change.element {manager.config.eventType.Replace(" ", "<><>")} 0");

            UI.Label(ref container, UIPanel.Menu, manager.eventName, 18, new UI4(0.02f, 0.73f, 0.5f, 0.78f), TextAnchor.MiddleLeft);

            int i = 0;
            AddInformationEntry(ref container, msg("gamestatus", player.userID), manager.status == Arena.EventStatus.Finished || manager.status == Arena.EventStatus.Pending ? msg("waitforplayers", player.userID) : msg("inprogress", player.userID), i); i++;            
            AddInformationEntry(ref container, msg("players", player.userID), $"{manager.eventPlayers.Count} / {manager.config.maximumPlayers}", i); i++;
            if (!Arena.Events.IgnoreKillLimit(manager.config.eventType))
            { AddInformationEntry(ref container, msg("scorelimit", player.userID), manager.config.killLimit == 0 ? msg("unlimited", player.userID) : manager.config.killLimit.ToString(), i); i++; }
            if (manager.config.timeLimit > 0)
            { AddInformationEntry(ref container, msg("timelimit", player.userID), $"{manager.config.timeLimit} {msg("minutes", player.userID)}", i); i++; }
            AddInformationEntry(ref container, msg("gear", player.userID), manager.config.useClassSelector ? msg("classselect", player.userID) : msg("eventspec", player.userID), i); i++;

            foreach (var addition in manager.GetAdditionalInformation())
            {
                AddInformationEntry(ref container, addition.Key, addition.Value, i);
                i++;
            }

            if (!string.IsNullOrEmpty(manager.config.description))            
                AddDescriptionEntry(ref container, msg("description", player.userID), manager.config.description, i);            

            CreateScoreboard(ref container, manager);
            CreateScoreList(ref container, manager);
            AddTeamSelection(ref container, manager, team, player.userID);

            DestroyUI(player, UIPanel.Menu);
            AddUI(player, UIPanel.Menu, container);
        }

        private void AddInformationEntry(ref CuiElementContainer container, string key, string value, int number)
        {
            float yMin = 0.68f - (number * 0.045f);
            float yMax = yMin + 0.04f;
            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.3f, yMax));
            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], new UI4(0.3005f, yMin, 0.4f, yMax));
            UI.Label(ref container, UIPanel.Menu, key, 15, new UI4(0.025f, yMin, 0.3f, yMax), TextAnchor.MiddleLeft);
            UI.Label(ref container, UIPanel.Menu, value, 15, new UI4(0.3005f, yMin, 0.395f, yMax), TextAnchor.MiddleRight);
        }
        private void AddDescriptionEntry(ref CuiElementContainer container, string key, string description, int number)
        {
            float yMin = 0.68f - (number * 0.045f);
            float yMax = yMin + 0.04f;

            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.3f, yMax));
            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], new UI4(0.3005f, yMin, 0.4f, yMax));
            UI.Label(ref container, UIPanel.Menu, key, 15, new UI4(0.025f, yMin, 0.3f, yMax), TextAnchor.MiddleLeft);
            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], new UI4(0.02f, yMin - 0.005f, 0.4f, yMin - 0.0005f));

            yMin = 0.68f - ((description.Length > 60 ? number + 2 : number + 1) * 0.045f);
            yMax = yMin + (description.Length > 60 ? 0.085f : 0.04f);

            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.4f, yMax));
            UI.Label(ref container, UIPanel.Menu, description, 15, new UI4(0.025f, yMin, 0.4f, yMax - 0.003f), TextAnchor.UpperLeft);
        }
        private void CreateScoreboard(ref CuiElementContainer container, Arena.EventManager manager, float xMin = 0.517f, string color = "")
        {
            if (string.IsNullOrEmpty(color))
                color = uiColors[ColorType.Button_Selected];

            UI.Panel(ref container, UIPanel.Menu, color, new UI4(xMin, 0.725f, xMin + 0.03f, 0.765f));

            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(xMin + 0.033f, 0.725f, xMin + 0.327f, 0.765f));
            UI.Label(ref container, UIPanel.Menu, msg("player"), 15, new UI4(xMin + 0.043f, 0.725f, xMin + 317f, 0.765f), TextAnchor.MiddleLeft);

            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(xMin + 0.33f, 0.725f, xMin + 0.38f, 0.765f));
            UI.Label(ref container, UIPanel.Menu, manager.GetScoreName(true), 15, new UI4(xMin + 0.33f, 0.725f, xMin + 0.38f, 0.765f));

            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(xMin + 0.383f, 0.725f, xMin + 0.433f, 0.765f));
            UI.Label(ref container, UIPanel.Menu, manager.GetScoreName(false), 15, new UI4(xMin + 0.383f, 0.725f, xMin + 0.433f, 0.765f));

            UI.Panel(ref container, UIPanel.Menu, color, new UI4(xMin, 0.7225f, xMin + 0.433f, 0.7245f));            
        }
        private void CreateScoreList(ref CuiElementContainer container, Arena.EventManager manager)
        {
            if (Arena.Events.IsTeamEvent(manager.config.eventType))
            {
                int count = AddScoreboardEntries(ref container, manager.GetGameScores(Arena.Team.A), 1, true, $"{(!string.IsNullOrEmpty(manager.config.teamA.name) ? manager.config.teamA.GetFormattedName() : msg("Team A"))} - ");
                AddScoreboardEntries(ref container, manager.GetGameScores(Arena.Team.B), count, true, $"{(!string.IsNullOrEmpty(manager.config.teamB.name) ? manager.config.teamB.GetFormattedName() : msg("Team B"))} - ");
            }
            else AddScoreboardEntries(ref container, manager.GetGameScores());
        }

        private int AddScoreboardEntries(ref CuiElementContainer container, List<Arena.ScoreEntry> data, int i = 1, bool limit = false, string prefix = "", float xMin = 0.517f)
        {           
            foreach (var eventPlayer in limit ? data.Take(7) : data.Take(15))
            {           
                float yMin = 0.725f - (i * 0.045f);
                float yMax = yMin + 0.04f;
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(xMin, yMin, xMin + 0.03f, yMax));
                UI.Label(ref container, UIPanel.Menu, eventPlayer.position.ToString(), 15, new UI4(xMin, yMin, xMin + 0.03f, yMax));

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(xMin + 0.033f, yMin, xMin + 0.327f, yMax));
                UI.Label(ref container, UIPanel.Menu, $"{prefix}{StripTags(eventPlayer.displayName)}", 15, new UI4(xMin + 0.043f, yMin, xMin + 317f, yMax), TextAnchor.MiddleLeft);

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(xMin + 0.33f, yMin, xMin + 0.38f, yMax));
                UI.Label(ref container, UIPanel.Menu, eventPlayer.value1.ToString(), 15, new UI4(xMin + 0.34f, yMin, xMin + 0.37f, yMax), TextAnchor.MiddleRight);

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(xMin + 0.383f, yMin, xMin + 0.433f, yMax));
                UI.Label(ref container, UIPanel.Menu, eventPlayer.value2.ToString(), 15, new UI4(xMin + 0.393f, yMin, xMin + 0.423f, yMax), TextAnchor.MiddleRight);

                i++;
            }
            return i;
        }

        private void AddTeamSelection(ref CuiElementContainer container, Arena.EventManager manager, Arena.Team team, ulong playerId)
        {
            bool isGameFull = manager.eventPlayers.Count >= manager.config.maximumPlayers;
                       
            if (Arena.Events.IsTeamEvent(manager.config.eventType))
            {
                string nameA = string.IsNullOrEmpty(manager.config.teamA.name) ? msg("Team A", playerId) : manager.config.teamA.name;
                string nameB = string.IsNullOrEmpty(manager.config.teamB.name) ? msg("Team B", playerId) : manager.config.teamB.name;

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.0295f, 0.091f, 0.3905f, 0.131f));
                UI.Label(ref container, UIPanel.Menu, isGameFull ? msg("maxcapacity", playerId) : !manager.CanJoinEvent() ? msg("cantjoinstarted", playerId) : msg("selectteam", playerId), 15, new UI4(0.0295f, 0.091f, 0.3905f, 0.131f));

                UI.StyledButton(ref container, UIPanel.Menu, team == Arena.Team.A ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], nameA, 15, new UI4(0.0295f, 0.05f, 0.1495f, 0.09f), team == Arena.Team.A ? "" : $"aui.change.team a {manager.eventName.Replace(" ", "<><>")}");
                UI.StyledButton(ref container, UIPanel.Menu, team == Arena.Team.B ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], nameB, 15, new UI4(0.15f, 0.05f, 0.27f, 0.09f), team == Arena.Team.B ? "" : $"aui.change.team b {manager.eventName.Replace(" ", "<><>")}");                
                UI.StyledButton(ref container, UIPanel.Menu, isGameFull ? uiColors[ColorType.Panel_Alt_Solid] : uiColors[ColorType.Button_Deselected], msg("enterevent", playerId), 15, new UI4(0.2705f, 0.05f, 0.3905f, 0.09f), isGameFull || !manager.CanJoinEvent() ? "" : $"aui.event.enter {team} {manager.eventName.Replace(" ", "<><>")}");
            }
            else
            {
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, 0.05f, 0.3f, 0.09f));
                UI.Label(ref container, UIPanel.Menu, isGameFull ? msg("maxcapacity", playerId) : !manager.CanJoinEvent() ? msg("cantjoinstarted", playerId) : msg("canjoinevent", playerId), 15, new UI4(0.025f, 0.05f, 0.3f, 0.09f), TextAnchor.MiddleLeft);

                UI.StyledButton(ref container, UIPanel.Menu, isGameFull || !manager.CanJoinEvent() ? uiColors[ColorType.Panel_Alt_Solid] : uiColors[ColorType.Button_Deselected], msg("enter", playerId), 15, new UI4(0.3005f, 0.05f, 0.4f, 0.09f), isGameFull || !manager.CanJoinEvent() ? "" : $"aui.event.enter {team} {manager.eventName.Replace(" ", "<><>")}");
            }            
        }
        #endregion

        #region Respawn Screen       
        public void ShowDeathScreen(BasePlayer player, string message, int time)
        {
            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            Arena.EventManager manager = Arena.GetEvent(eventPlayer.currentEvent);
                    
            CuiElementContainer container = UI.ElementContainer(UIPanel.Death, uiColors[ColorType.BG_Main_PartialTransparent], new UI4(0, 0, 1, 1), true);

            UI.Label(ref container, UIPanel.Death, message, 22, new UI4(0.2f, 0.7f, 0.8f, 0.85f));
            UI.Image(ref container, UIPanel.Death, GetImage("skullicon"), new UI4(0.475f, 0.46f, 0.525f, 0.54f));

            if (manager.config.useClassSelector && !eventPlayer.isEliminated)            
                UI.StyledButton(ref container, UIPanel.Death, uiColors[ColorType.Button_Deselected], msg("changeclass", player.userID), 15, new UI4(0.44f, 0.21f, 0.56f, 0.25f), "aui.player.class");
                  
            DestroyAllUI(player);
            AddUI(player, UIPanel.Death, container);

            if (!eventPlayer.isEliminated)
                AddRespawnButton(eventPlayer, time + 1);
            else timer.In(3, () => DestroyUI(player, UIPanel.Death));
        }

        public void ShowSpectateScreen(BasePlayer player, string message)
        {
            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            Arena.EventManager manager = Arena.GetEvent(eventPlayer.currentEvent);

            CuiElementContainer container = UI.ElementContainer(UIPanel.Death, uiColors[ColorType.BG_Main_PartialTransparent], new UI4(0, 0, 1, 1), true);

            UI.Label(ref container, UIPanel.Death, message, 20, new UI4(0.2f, 0.7f, 0.8f, 0.85f));
            UI.Image(ref container, UIPanel.Death, GetImage("skullicon"), new UI4(0.475f, 0.46f, 0.525f, 0.54f));
            UI.StyledButton(ref container, UIPanel.Death, uiColors[ColorType.Button_Deselected], msg("leaveevent", eventPlayer.player.userID), 15, new UI4(0.44f, 0.26f, 0.56f, 0.3f), "aui.player.controls leave");

            DestroyAllUI(player);
            AddUI(player, UIPanel.Death, container);            
        }        

        private void AddRespawnButton(Arena.EventPlayer eventPlayer, int elapsed)
        {
            --elapsed;
            CuiElementContainer container = UI.ElementContainer(UIPanel.Respawn, "0 0 0 0", new UI4(0.44f, 0.26f, 0.56f, 0.3f));
            UI.Panel(ref container, UIPanel.Respawn, uiColors[ColorType.Button_Selected], new UI4(0, 1, 1, 1.05f));
            UI.Button(ref container, UIPanel.Respawn, uiColors[ColorType.Button_Deselected], elapsed == 0 ? msg("respawn", eventPlayer.player.userID) : $"{msg("respawn", eventPlayer.player.userID)} ({elapsed})", 15, new UI4(0, 0, 1, 1), elapsed == 0 ? "aui.player.respawn" : "");
            DestroyUI(eventPlayer.player, UIPanel.Respawn);
            if (!eventPlayer.isDead || eventPlayer.isEliminated) return;
            AddUI(eventPlayer.player, UIPanel.Respawn, container);
            if (elapsed > 0)
                timer.In(1, () => AddRespawnButton(eventPlayer, elapsed));
        }
        #endregion

        #region Class Selector
        public void ShowSpawnScreen(BasePlayer player)
        {
            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            Arena.EventManager manager = Arena.GetEvent(eventPlayer.currentEvent);

            CuiElementContainer container = UI.ElementContainer(UIPanel.Death, uiColors[ColorType.BG_Main_PartialTransparent], new UI4(0, 0, 1, 1), true);

            UI.Label(ref container, UIPanel.Death, msg("selectclass", player.userID), 22, new UI4(0.2f, 0.7f, 0.8f, 0.85f));
            UI.Image(ref container, UIPanel.Death, GetImage("skullicon"), new UI4(0.475f, 0.46f, 0.525f, 0.54f));

            UI.StyledButton(ref container, UIPanel.Death, uiColors[ColorType.Button_Deselected], msg("spawn", eventPlayer.player.userID), 15, new UI4(0.44f, 0.26f, 0.56f, 0.3f), "aui.player.classchosen");

            DestroyAllUI(player);
            AddUI(player, UIPanel.Death, container);
            AddClassSelector(eventPlayer, manager);
        }
        
        private void AddClassSelector(Arena.EventPlayer eventPlayer, Arena.EventManager manager)
        {            
            if (manager.config.useClassSelector)
            {
                CuiElementContainer container = UI.ElementContainer(UIPanel.Class, "0 0 0 0", new UI4(0.02f, 0, 0.14f, 1));
                int i = 0;
                foreach (var kit in manager.config.eventKits)
                {
                    float yMin = 0.03f + (i * 0.05f);
                    float yMax = yMin + 0.04f;

                    UI.StyledButton(ref container, UIPanel.Class, i == eventPlayer.currentKit ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], kit, 15, new UI4(0, yMin, 1, yMax), i == eventPlayer.currentKit ? "" : $"aui.player.class {i}");
                    i++;
                }

                DestroyUI(eventPlayer.player, UIPanel.Class);
                AddUI(eventPlayer.player, UIPanel.Class, container);
            }
        }
        #endregion

        #region Game Menu  
        public void ShowHelpText(BasePlayer player)
        {
            CuiElementContainer container = UI.ElementContainer(UIPanel.Help, uiColors[ColorType.BG_Main_Transparent], new UI4(0.343f, 0, 0.641f, 0.015f));
            UI.Label(ref container, UIPanel.Help, string.Format(msg("menuhelp", player.userID), $"/{Arena.configData.ServerSettings.ChatCommand}"), 10, new UI4(0, 0, 1, 1));
            DestroyUI(player, UIPanel.Help);
            AddUI(player, UIPanel.Help, container);
        }
        public void OpenPlayerMenu(BasePlayer player)
        {
            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            Arena.EventManager manager = Arena.GetEvent(eventPlayer.currentEvent);

            CuiElementContainer container = null;
            if (Arena.Events.IsTeamEvent(manager.config.eventType))
            {
                container = UI.ElementContainer(UIPanel.Menu, uiColors[ColorType.BG_Main_Transparent], new UI4(0.01f, 0.75f, 0.12f, 0.99f), true);
                UI.Image(ref container, UIPanel.Menu, GetImage("logosmall"), new UI4(0, 0.5f, 1, 1));
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], msg("leaveevent", player.userID), 15, new UI4(0, 0.3334f, 1, 0.5f), "aui.player.controls leave");
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], msg("switchteam", player.userID), 15, new UI4(0, 0.1666f, 1, 0.3334f), $"aui.change.team {eventPlayer.team} {manager.config.eventName.Replace(" ", "<><>")}");
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], msg("cancel", player.userID), 15, new UI4(0, 0, 1, 0.1666f), "aui.player.controls cancel");
            }
            else
            {
                container = UI.ElementContainer(UIPanel.Menu, uiColors[ColorType.BG_Main_Transparent], new UI4(0.01f, 0.79f, 0.12f, 0.99f), true);
                UI.Image(ref container, UIPanel.Menu, GetImage("logosmall"), new UI4(0, 0.4f, 1, 1));
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], msg("leaveevent", player.userID), 15, new UI4(0, 0.2f, 1, 0.4f), "aui.player.controls leave");
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], msg("cancel", player.userID), 15, new UI4(0, 0, 1, 0.2f), "aui.player.controls cancel");
            }
            DestroyUI(player, UIPanel.Menu);
            AddUI(player, UIPanel.Menu, container);
        }
        #endregion
       
        #region Scoreboards
        public void ShowEventResults(Arena.EventManager manager)
        {
            CuiElementContainer container = UI.ElementContainer(UIPanel.Menu, uiColors[ColorType.BG_Main_PartialTransparent], new UI4(0, 0, 1, 1), true);

            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Main_Transparent], new UI4(0.4f, 0.94f, 0.6f, 1));
            UI.Label(ref container, UIPanel.Menu, $"{manager.eventName} ({manager.config.eventType})", 15, new UI4(0.4f, 0.94f, 0.6f, 1));
            UI.Panel(ref container, UIPanel.Menu, UI.Color("#ffffff", 0.98f), new UI4(0.4f, 0.9385f, 0.6f, 0.9395f));

            UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], Arena.configData.ServerSettings.EventOnly ? msg("returntolobby") : msg("returntogame"), 16, new UI4(0.01f, 0.94f, 0.13f, 0.98f), "aui.player.controls leave");

            if (manager.isContinual)
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], msg("continueplaying"), 16, new UI4(0.135f, 0.94f, 0.265f, 0.98f), "aui.player.controls continue");

            if (Arena.Events.IsTeamEvent(manager.config.eventType))
                AddTeamScoreboard(ref container, manager);
            else AddSingleScoreboard(ref container, manager);

            foreach(var eventPlayer in manager.eventPlayers)
            {
                DestroyAllUI(eventPlayer.player);
                AddUI(eventPlayer.player, UIPanel.Menu, container);
            }
        }
        private void AddSingleScoreboard(ref CuiElementContainer container, Arena.EventManager manager)
        {
            CreateScoreboard(ref container, manager, 0.2835f);
            AddScoreboardEntries(ref container, manager.GetGameScores(), 1, false, "", 0.2835f);
        }
        private void AddTeamScoreboard(ref CuiElementContainer container, Arena.EventManager manager)
        {
            string aColor = string.IsNullOrEmpty(manager.config.teamA.color) ? uiColors[ColorType.Button_Selected] : UI.Color(manager.config.teamA.color, 1);
            string bColor = string.IsNullOrEmpty(manager.config.teamB.color) ? uiColors[ColorType.Button_Selected] : UI.Color(manager.config.teamB.color, 1);

            UI.Panel(ref container, UIPanel.Menu, aColor, new UI4(0.362f, 0.77f, 0.4665f, 0.83f));
            UI.Label(ref container, UIPanel.Menu, $"{(string.IsNullOrEmpty(manager.config.teamA.name) ? msg("Team A") : manager.config.teamA.name)}  -  {manager.GetTeamScore(Arena.Team.A)}", 16, new UI4(0.362f, 0.77f, 0.455f, 0.83f), TextAnchor.MiddleRight);
            CreateScoreboard(ref container, manager, 0.0335f, aColor);
            AddScoreboardEntries(ref container, manager.GetGameScores(Arena.Team.A), 1, false, "", 0.0335f);

            UI.Panel(ref container, UIPanel.Menu, bColor, new UI4(0.5335f, 0.77f, 0.64f, 0.83f));
            UI.Label(ref container, UIPanel.Menu, $"{manager.GetTeamScore(Arena.Team.B)}  -  {(string.IsNullOrEmpty(manager.config.teamB.name) ? msg("Team B") : manager.config.teamB.name)}", 16, new UI4(0.545f, 0.77f, 0.64f, 0.83f), TextAnchor.MiddleLeft);
            CreateScoreboard(ref container, manager, 0.5335f, bColor);
            AddScoreboardEntries(ref container, manager.GetGameScores(Arena.Team.B), 1, false, "", 0.5335f);
        }
        public CuiElementContainer ShowGameScores(Arena.EventManager manager)
        {
            CuiElementContainer container = null;

            string[] scoreTypes = manager.GetScoreType();
            int count = 0;
            float size = 0.0992f;

            if (Arena.Events.IsTeamEvent(manager.config.eventType))
            {                
                container = UI.ElementContainer(UIPanel.Scores, "0 0 0 0", new UI4(0.87f, 0.655f, 0.997f, 0.85f));

                UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Button_Selected], new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                UI.Label(ref container, UIPanel.Scores, $"{manager.eventName} ({msg(manager.config.eventType)})", 10, new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                count++;

                UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Panel_Main_Transparent], new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                UI.Label(ref container, UIPanel.Scores, manager.GetScoreString(), 10, new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                count++;

                string additionalScore = manager.GetAdditionalScoreString();
                if (!string.IsNullOrEmpty(additionalScore))
                {
                    UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Panel_Main_Transparent], new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                    UI.Label(ref container, UIPanel.Scores, additionalScore, 10, new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                    count++;
                }

                UI.Panel(ref container, UIPanel.Scores, string.IsNullOrEmpty(manager.config.teamA.color) ? uiColors[ColorType.Panel_Main_Transparent] : UI.Color(manager.config.teamA.color, 1f), new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                UI.Label(ref container, UIPanel.Scores, $"{manager.config.teamA.GetFormattedName() ?? msg("Team A")} ({manager.GetTeamScore(Arena.Team.A)})", 10, new UI4(0.05f, 1 - (0.1f * count), 0.6f, 1 - (0.1f * count) + size), TextAnchor.MiddleLeft);
                UI.Label(ref container, UIPanel.Scores, scoreTypes[0], 10, new UI4(0.6f, 1 - (0.1f * count), 0.8f, 1 - (0.1f * count) + size));
                UI.Label(ref container, UIPanel.Scores, scoreTypes[1], 10, new UI4(0.8f, 1 - (0.1f * count), 1, 1 - (0.1f * count) + size));
                count++;
                     
                var scoresA = manager.GetGameScores(Arena.Team.A);
                for (int i = 0; i < scoresA.Count; i++)
                {
                    if (i >= 5) break;
                    float yMin = 1 - (0.1f * count);
                    float yMax = yMin + size;
                    UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Panel_Main_Transparent], new UI4(0, yMin, 1, yMax));
                    UI.Label(ref container, UIPanel.Scores, TrimToSize(scoresA[i].displayName, 20), 10, new UI4(0.05f, yMin, 0.6f, yMax), TextAnchor.MiddleLeft);
                    UI.Label(ref container, UIPanel.Scores, scoresA[i].value1.ToString(), 10, new UI4(0.6f, yMin, 0.8f, yMax));
                    UI.Label(ref container, UIPanel.Scores, scoresA[i].value2.ToString(), 10, new UI4(0.8f, yMin, 1, yMax));
                    count++;
                }
                
                UI.Panel(ref container, UIPanel.Scores, string.IsNullOrEmpty(manager.config.teamB.color) ? uiColors[ColorType.Panel_Main_Transparent] : UI.Color(manager.config.teamB.color, 1f), new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                UI.Label(ref container, UIPanel.Scores, $"{manager.config.teamB.GetFormattedName() ?? msg("Team B")} ({manager.GetTeamScore(Arena.Team.B)})", 10, new UI4(0.05f, 1 - (0.1f * count), 0.6f, 1 - (0.1f * count) + size), TextAnchor.MiddleLeft);
                UI.Label(ref container, UIPanel.Scores, scoreTypes[0], 10, new UI4(0.6f, 1 - (0.1f * count), 0.8f, 1 - (0.1f * count) + size));
                UI.Label(ref container, UIPanel.Scores, scoreTypes[1], 10, new UI4(0.8f, 1 - (0.1f * count), 1, 1 - (0.1f * count) + size));
                count++;

                var scoresB = manager.GetGameScores(Arena.Team.B);
                for (int i = 0; i < scoresB.Count; i++)
                {
                    if (i >= 5) break;
                    float yMin = 1 - (0.1f * count);
                    float yMax = yMin + size;
                    UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Panel_Main_Transparent], new UI4(0, yMin, 1, yMax));
                    UI.Label(ref container, UIPanel.Scores, TrimToSize(scoresB[i].displayName, 20), 10, new UI4(0.05f, yMin, 0.6f, yMax), TextAnchor.MiddleLeft);
                    UI.Label(ref container, UIPanel.Scores, scoresB[i].value1.ToString(), 10, new UI4(0.6f, yMin, 0.8f, yMax));
                    UI.Label(ref container, UIPanel.Scores, scoresB[i].value2.ToString(), 10, new UI4(0.8f, yMin, 1, yMax));
                    count++;
                }
            }
            else
            {
                var scores = manager.GetGameScores();
                container = UI.ElementContainer(UIPanel.Scores, "0 0 0 0", new UI4(0.87f, 0.655f, 0.997f, 0.85f));

                UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Button_Selected], new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                UI.Label(ref container, UIPanel.Scores, $"{manager.eventName} ({msg(manager.config.eventType)})", 10, new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                count++;

                UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Panel_Main_Transparent], new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                UI.Label(ref container, UIPanel.Scores, manager.GetScoreString(), 10, new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                count++;

                string additionalScore = manager.GetAdditionalScoreString();
                if (!string.IsNullOrEmpty(additionalScore))
                {
                    UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Panel_Main_Transparent], new UI4(0, (1 - (0.1f * count)) + 0.002f, 1, (1 - (0.1f * count) + size) - 0.002f));
                    UI.Label(ref container, UIPanel.Scores, additionalScore, 10, new UI4(0, (1 - (0.1f * count)) + 0.002f, 1, (1 - (0.1f * count) + size) - 0.002f));
                    count++;
                }

                UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Button_Selected], new UI4(0, (1 - (0.1f * count)) + 0.002f , 1, (1 - (0.1f * count) + size) - 0.002f));
                UI.Label(ref container, UIPanel.Scores, scoreTypes[0], 10, new UI4(0.6f, 1 - (0.1f * count), 0.8f, 1 - (0.1f * count) + size));
                UI.Label(ref container, UIPanel.Scores, scoreTypes[1], 10, new UI4(0.8f, 1 - (0.1f * count), 1, 1 - (0.1f * count) + size));
                count++;

                for (int i = 0; i < scores.Count; i++)
                {
                    if (i >= 10) break;
                    float yMin = 1 - (0.1f * count);
                    float yMax = 1 - (0.1f * count) + size;
                    UI.Panel(ref container, UIPanel.Scores, uiColors[ColorType.Panel_Main_Transparent], new UI4(0, yMin, 1, yMax));
                    UI.Label(ref container, UIPanel.Scores, TrimToSize(scores[i].displayName), 10, new UI4(0.05f, yMin, 0.6f, yMax), TextAnchor.MiddleLeft);
                    UI.Label(ref container, UIPanel.Scores, scores[i].value1.ToString(), 10, new UI4(0.6f, yMin, 0.8f, yMax));
                    UI.Label(ref container, UIPanel.Scores, scores[i].value2.ToString(), 10, new UI4(0.8f, yMin, 1, yMax));
                    count++;
                }
            } 
            
            foreach(var eventPlayer in manager.eventPlayers)
            {
                DestroyUI(eventPlayer.player, UIPanel.Scores);
                AddUI(eventPlayer.player, UIPanel.Scores, container);
            }
            return container;  
        }
        #endregion

        #region Game Timer
        public CuiElementContainer UpdateTimer(int time, string message = "")
        {
            string clockTime = "";
            TimeSpan dateDifference = TimeSpan.FromSeconds(time);
            var hours = dateDifference.Hours;
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            if (hours > 0)
                clockTime = string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
            else clockTime = string.Format("{0:00}:{1:00}", mins, secs);

            CuiElementContainer container = UI.ElementContainer(UIPanel.Clock, "0.1 0.1 0.1 0.7", new UI4(0.45f, 0.915f, 0.55f, 0.955f), false);
            UI.Label(ref container, UIPanel.Clock, clockTime, 16, new UI4(0, 0, 1, 1));
            if (!string.IsNullOrEmpty(message))
                UI.Label(ref container, UIPanel.Clock, message, 16, new UI4(-5, 0, -0.1f, 1), TextAnchor.MiddleRight);
            return container;
        }
        #endregion

        #region UI Commands
        [ConsoleCommand("aui.change.element")]
        void ccmdChangeElement(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            var input = arg.GetString(0);
            if (input == "destroy")
                DestroyUI(player, UIPanel.Menu);
            else OpenMainMenu(player, input.Replace("<><>", " "), arg.GetInt(1));            
        }

        [ConsoleCommand("aui.change.team")]
        void ccmdChangeTeam(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            Arena.Team team = ParseType<Arena.Team>(arg.GetString(0));
            Arena.EventManager manager = Arena.GetEvent(arg.GetString(1).Replace("<><>", " "));

            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            if (eventPlayer == null)            
                ShowEventDetails(player, manager, team);            
            else
            {
                team = eventPlayer.team;
                Arena.Team newTeam = team == Arena.Team.A ? Arena.Team.B : Arena.Team.A;

                int maxDiff = manager.config.maximumPlayers == 2 ? 1 : 2;
                if (newTeam == Arena.Team.A)
                {
                    if (manager.GetTeamCount(Arena.Team.A) > manager.GetTeamCount(Arena.Team.B) + maxDiff)
                    {                        
                        manager.BroadcastToPlayer(eventPlayer, string.Format(msg("toomanyplayers", player.userID), string.IsNullOrEmpty(manager.config.teamA.name) ? msg("Team A", eventPlayer.player.userID) : manager.config.teamA.name));
                        return;
                    }
                    else
                    {
                        manager.OnPlayerDeath(eventPlayer, null);
                        eventPlayer.team = Arena.Team.A;                        
                    }
                }
                else if (newTeam == Arena.Team.B)
                {
                    if (manager.GetTeamCount(Arena.Team.B) > manager.GetTeamCount(Arena.Team.A) + maxDiff)
                    {
                        manager.BroadcastToPlayer(eventPlayer, string.Format(msg("toomanyplayers", player.userID), string.IsNullOrEmpty(manager.config.teamB.name) ? msg("Team B", eventPlayer.player.userID) : manager.config.teamB.name));
                        return;
                    }
                    else
                    {
                        manager.OnPlayerDeath(eventPlayer, null);
                        eventPlayer.team = Arena.Team.B;                        
                    }
                }
                DestroyUI(player, UIPanel.Menu);
            }
        }

        [ConsoleCommand("aui.event.enter")]
        void ccmdEnterEvent(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            Arena.Team team = ParseType<Arena.Team>(arg.GetString(0));
            Arena.EventManager manager = Arena.GetEvent(arg.GetString(1).Replace("<><>", " "));

            if (manager == null)
            {
                OpenMainMenu(player, Arena.events.First().Value.config.eventType);
                PopupError(player, msg("eventClosed", player.UserIDString));
                return;
            }

            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            if (eventPlayer != null)
                UnityEngine.Object.DestroyImmediate(eventPlayer);

            if (!Arena.CanJoinEvent(player))
            {
                DestroyUI(player, UIPanel.Menu);
                return;
            }

            if (!manager.CanJoinEvent())
            {
                ShowEventDetails(player, manager, team);
                return;
            }

            eventPlayer = player.gameObject.AddComponent<Arena.EventPlayer>();
            eventPlayer.currentEvent = manager.eventName;

            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                eventPlayer.SetPlayer();

            DestroyUI(player, UIPanel.Menu);

            if (Arena.Events.IsTeamEvent(manager.config.eventType))
            {
                int teamACount = manager.GetTeamCount(Arena.Team.A);
                int teamBCount = manager.GetTeamCount(Arena.Team.B);
                if (team == Arena.Team.A)
                {
                    if (teamACount > teamBCount + 1)
                    {
                        manager.JoinEvent(eventPlayer, Arena.Team.B);
                        manager.BroadcastToPlayer(eventPlayer, string.Format(msg("toomanyplayers", player.userID), string.IsNullOrEmpty(manager.config.teamA.name) ? msg("Team A", eventPlayer.player.userID) : manager.config.teamA.name) + string.Format(msg("autoassigning", eventPlayer.player.userID), string.IsNullOrEmpty(manager.config.teamB.name) ? msg("Team B", eventPlayer.player.userID) : manager.config.teamB.name));
                    }
                    else manager.JoinEvent(eventPlayer, Arena.Team.A);
                }
                else if (team == Arena.Team.B)
                {
                    if (teamBCount > teamACount + 1)
                    {
                        manager.JoinEvent(eventPlayer, Arena.Team.A);
                        manager.BroadcastToPlayer(eventPlayer, string.Format(msg("toomanyplayers", player.userID), string.IsNullOrEmpty(manager.config.teamB.name) ? msg("Team B", eventPlayer.player.userID) : manager.config.teamB.name) + string.Format(msg("autoassigning", eventPlayer.player.userID), string.IsNullOrEmpty(manager.config.teamA.name) ? msg("Team A", eventPlayer.player.userID) : manager.config.teamA.name));
                    }
                    else manager.JoinEvent(eventPlayer, Arena.Team.B);
                }
            }
            else manager.JoinEvent(eventPlayer, team);         
        }

        [ConsoleCommand("aui.player.respawn")]
        void ccmdRespawnPlayer(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            if (eventPlayer != null)                           
                Arena.RespawnPlayer(player);            
        }

        [ConsoleCommand("aui.player.classchosen")]
        void ccmdClassChosen(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            Arena.EventManager manager = Arena.GetEvent(eventPlayer.currentEvent);

            if (manager.status == Arena.EventStatus.Started)
                Arena.RespawnPlayer(player);
            else
            {
                DestroyUI(player, UIPanel.Death);
                DestroyUI(player, UIPanel.Class);
            }
        }

        [ConsoleCommand("aui.player.class")]
        void ccmdClassSelection(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            Arena.EventManager manager = Arena.GetEvent(eventPlayer.currentEvent);
            if (eventPlayer != null)
            {
                if (arg.Args != null && arg.Args.Length > 0)
                    eventPlayer.currentKit = arg.GetInt(0);
                AddClassSelector(eventPlayer, manager);                
            }
        }
       
        [ConsoleCommand("aui.player.controls")]
        void ccmdPlayerControl(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            var input = arg.GetString(0);
            var eventName = arg.GetString(1).Replace("<><>", " ");
            Arena.Team team = Arena.Team.A;

            Arena.EventPlayer eventPlayer = Arena.GetUser(player);
            Arena.EventManager manager = Arena.GetEvent(eventPlayer != null ? eventPlayer.currentEvent : eventName);

            if (manager == null)
            {
                OpenMainMenu(player, Arena.events.First().Value.config.eventType);
                PopupError(player, msg("eventClosed", player.UserIDString));
                return;
            }
            else
            {
                switch (input)
                {
                    case "view":
                        if (arg.Args.Length == 3)
                            team = ParseType<Arena.Team>(arg.GetString(2));
                        ShowEventDetails(player, manager, team);
                        return;                    
                    case "leave":
                        manager.LeaveEvent(eventPlayer);
                        break;
                    case "continue":
                        manager.BroadcastToPlayer(eventPlayer, msg("nextevent", eventPlayer.player.userID), true);
                        break;                  
                    case "cancel":                        
                        break;                    
                }
                DestroyUI(player, UIPanel.Menu);
            }
        }

        [ConsoleCommand("aui.donator.popup")]
        void ccmdDonatorPopup(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            PopupMessage(player, msg("noDonator", player.userID));
        }
        #endregion

        #region Event Creator
        [ChatCommand("create")]
        void cmdEventCreate(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "arena.admin")) return;
            if (Arena.GetUser(player) != null)
            {
                SendReply(player, "You can not create a new event whilst you are in one");
                return;
            }
            eventCreator.Add(player.userID, new Arena.ArenaData.EventConfig());
            OpenCreatorMenu(player);
        }
        [ChatCommand("edit")]
        void cmdEventEdit(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "arena.admin")) return;
            if (args.Length == 0)
            {
                SendReply(player, "/edit <eventname>");
                return;
            }
            if (Arena.GetUser(player) != null)
            {
                SendReply(player, "You can not edit an event whilst you are in one");
                return;
            }
            Arena.EventManager manager = Arena.GetEvent(args[0]);
            if (manager == null)
            {
                SendReply(player, $"Unable to find an event with the name: {args[0]}");
                return;
            }
            if (manager.status == Arena.EventStatus.Pending || manager.status == Arena.EventStatus.Started)
            {
                SendReply(player, $"You can not edit an event whilst it is in progress");
                return;
            }
            Arena.events.Remove(args[0]);
            eventCreator.Add(player.userID, manager.config);
            eventEditorBackup.Add(player.userID, manager.config);
            UnityEngine.Object.Destroy(manager);
            
            OpenCreatorMenu(player);
        }
        [ChatCommand("delete")]
        void cmdEventDelete(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "arena.admin")) return;
            if (args.Length == 0)
            {
                SendReply(player, "/delete <eventname>");
                return;
            }
            if (Arena.GetUser(player) != null)
            {
                SendReply(player, "You can not create a new event whilst you are in one");
                return;
            }
            Arena.EventManager manager = Arena.GetEvent(args[0]);
            if (manager == null)
            {
                SendReply(player, $"Unable to find an event with the name: {args[0]}");
                return;
            }
            if (manager.status == Arena.EventStatus.Pending || manager.status == Arena.EventStatus.Started)
            {
                SendReply(player, "You can not delete an event whilst it is in progress");
                return;
            }
            Arena.events.Remove(args[0]);
            UnityEngine.Object.Destroy(manager);
            Arena.RemoveEvent(args[0]);
            SendReply(player, $"You have successfully deleted the event {args[0]}");
        }

        private void OpenCreatorMenu(BasePlayer player)
        {
            Arena.ArenaData.EventConfig config = eventCreator[player.userID];
            CuiElementContainer container = UI.ElementContainer(UIPanel.Menu, uiColors[ColorType.BG_Main_Solid], new UI4(0, 0, 1, 1), true);
            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Main_Solid], new UI4(0.005f, 0.01f, 0.995f, 0.95f));
            UI.Label(ref container, UIPanel.Menu, "Event Creator", 16, new UI4(0, 0.95f, 1, 1));
            UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Exit", 15, new UI4(0.005f, 0.955f, 0.125f, 0.995f), "aui.creator exit");

            if (string.IsNullOrEmpty(config.eventType))
            {
                SelectionMenu(player, Arena.eventTypes.ToArray(), "Select an event type to begin", "aui.creator eventtype", false);
                return;
            }
            else
            {
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Save", 15, new UI4(0.135f, 0.955f, 0.255f, 0.995f), "aui.creator save");

                int i = 0;
                float yMin = GetVerticalPos(i);
                // Event Type
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Event Type", 15, new UI4(0.03f, yMin, 0.2f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, config.eventType, 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                i++;
                yMin = GetVerticalPos(i);

                // Event Name
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Event Name", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                if (string.IsNullOrEmpty(config.eventName))
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator eventname", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, config.eventName, 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), "aui.clear eventname");
                }
                i++;
                yMin = GetVerticalPos(i);

                // Event Name
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Icon filename or URL", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                if (string.IsNullOrEmpty(config.eventIcon))
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator icon", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, config.eventIcon, 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), "aui.clear icon");
                }
                i++;
                yMin = GetVerticalPos(i);

                // Event Description
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Event Description", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.8f, yMin + 0.04f));
                if (string.IsNullOrEmpty(config.description))
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator description", new UI4(0.215f, yMin, 0.8f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, config.description, 15, new UI4(0.215f, yMin, 0.8f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.803f, yMin, 0.83f, yMin + 0.04f), "aui.clear description");
                }
                i++;
                yMin = GetVerticalPos(i);

                if (Arena.Events.IsTeamEvent(config.eventType))
                {
                    // Team A Name
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Team A name", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.46f, yMin + 0.04f));
                    if (string.IsNullOrEmpty(config.teamA.name))
                        UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator nameA", new UI4(0.215f, yMin, 0.46f, yMin + 0.04f));
                    else
                    {
                        UI.Label(ref container, UIPanel.Menu, config.teamA.name, 15, new UI4(0.215f, yMin, 0.46f, yMin + 0.04f), TextAnchor.MiddleLeft);
                        UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.463f, yMin, 0.49f, yMin + 0.04f), "aui.clear nameA");
                    }

                    // Team B Name
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.51f, yMin, 0.692f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Team B name", 15, new UI4(0.52f, yMin, 0.692f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.695f, yMin, 0.95f, yMin + 0.04f));
                    if (string.IsNullOrEmpty(config.teamB.name))
                        UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator nameB", new UI4(0.705f, yMin, 0.95f, yMin + 0.04f));
                    else
                    {
                        UI.Label(ref container, UIPanel.Menu, config.teamB.name, 15, new UI4(0.705f, yMin, 0.95f, yMin + 0.04f), TextAnchor.MiddleLeft);
                        UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.953f, yMin, 0.98f, yMin + 0.04f), "aui.clear nameB");
                    }
                    i++;
                    yMin = GetVerticalPos(i);

                    // Team A Color
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Team A color (hex)", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.46f, yMin + 0.04f));
                    if (string.IsNullOrEmpty(config.teamA.color))
                        UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator colorA", new UI4(0.215f, yMin, 0.46f, yMin + 0.04f));
                    else
                    {
                        UI.Label(ref container, UIPanel.Menu, config.teamA.color, 15, new UI4(0.215f, yMin, 0.46f, yMin + 0.04f), TextAnchor.MiddleLeft);
                        UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.463f, yMin, 0.49f, yMin + 0.04f), "aui.clear colorA");
                    }

                    // Team B Color
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.51f, yMin, 0.692f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Team B color (hex)", 15, new UI4(0.52f, yMin, 0.692f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.695f, yMin, 0.95f, yMin + 0.04f));
                    if (string.IsNullOrEmpty(config.teamB.color))
                        UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator colorB", new UI4(0.705f, yMin, 0.95f, yMin + 0.04f));
                    else
                    {
                        UI.Label(ref container, UIPanel.Menu, config.teamB.color, 15, new UI4(0.705f, yMin, 0.95f, yMin + 0.04f), TextAnchor.MiddleLeft);
                        UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.953f, yMin, 0.98f, yMin + 0.04f), "aui.clear colorB");
                    }
                    i++;
                    yMin = GetVerticalPos(i);

                    // Team A Gear
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Team A kit (clothing only)", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.387f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, config.teamA.kit, 15, new UI4(0.207f, yMin, 0.387f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Select", 15, new UI4(0.39f, yMin, 0.49f, yMin + 0.04f), "aui.select kit A");

                    // Team B Gear
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.51f, yMin, 0.692f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Team B kit (clothing only)", 15, new UI4(0.52f, yMin, 0.692f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.695f, yMin, 0.877f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, config.teamB.kit, 15, new UI4(0.697f, yMin, 0.877f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Select", 15, new UI4(0.88f, yMin, 0.98f, yMin + 0.04f), "aui.select kit B");
                    i++;
                    yMin = GetVerticalPos(i);

                    // Team A Spawns
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Team A spawnfile", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.387f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, config.teamA.spawnfile, 15, new UI4(0.215f, yMin, 0.387f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Select", 15, new UI4(0.39f, yMin, 0.49f, yMin + 0.04f), "aui.select spawns A");

                    // Team B Spawns
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.51f, yMin, 0.692f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Team B spawnfile", 15, new UI4(0.52f, yMin, 0.692f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.695f, yMin, 0.877f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, config.teamB.spawnfile, 15, new UI4(0.71f, yMin, 0.877f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Select", 15, new UI4(0.88f, yMin, 0.98f, yMin + 0.04f), "aui.select spawns B");
                    i++;
                    yMin = GetVerticalPos(i);

                    // Friendly Fire
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Friendly Fire", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.497f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, config.ffEnabled ? "Enabled" : "Disabled", 15, new UI4(0.215f, yMin, 0.497f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], config.ffEnabled ? "Disable" : "Enable", 15, new UI4(0.5f, yMin, 0.6f, yMin + 0.04f), $"aui.creator friendlyFire {!config.ffEnabled}");
                    i++;
                    yMin = GetVerticalPos(i);

                    if (Arena.configData.EventSettings.Continual)
                    {
                        // Switch Teams On Repeat
                        UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                        UI.Label(ref container, UIPanel.Menu, "Switch teams on new event", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                        UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.497f, yMin + 0.04f));
                        UI.Label(ref container, UIPanel.Menu, config.switchTeamOnRepeat ? "Enabled" : "Disabled", 15, new UI4(0.215f, yMin, 0.497f, yMin + 0.04f), TextAnchor.MiddleLeft);
                        UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], config.ffEnabled ? "Disable" : "Enable", 15, new UI4(0.5f, yMin, 0.6f, yMin + 0.04f), $"aui.creator switchteam {!config.switchTeamOnRepeat}");
                        i++;
                        yMin = GetVerticalPos(i);
                    }
                }
                else
                {
                    // Spawnfile
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Spawnfile", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.497f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, config.teamA.spawnfile, 15, new UI4(0.215f, yMin, 0.497f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Select", 15, new UI4(0.5f, yMin, 0.6f, yMin + 0.04f), "aui.select spawns A");
                    i++;
                    yMin = GetVerticalPos(i);
                }

                // Kits
                if (!Arena.Events.IgnoreKitSelection(config.eventType))
                {
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Kit List", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.497f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, $"{config.eventKits.Count} kit(s) selected", 15, new UI4(0.215f, yMin, 0.497f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Select", 15, new UI4(0.5f, yMin, 0.6f, yMin + 0.04f), "aui.select kits");
                    i++;
                    yMin = GetVerticalPos(i);
                }

                // Class Selector
                if (!Arena.Events.IgnoreClassSelector(config.eventType))
                {
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Class Selector", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.497f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, config.useClassSelector ? "Enabled" : "Disabled", 15, new UI4(0.215f, yMin, 0.497f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], config.useClassSelector ? "Disable" : "Enable", 15, new UI4(0.5f, yMin, 0.6f, yMin + 0.04f), $"aui.creator classselector {!config.useClassSelector}");
                    i++;
                    yMin = GetVerticalPos(i);
                }

                // Zone ID
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Zone ID", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.497f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, config.zoneId, 15, new UI4(0.215f, yMin, 0.497f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Select", 15, new UI4(0.5f, yMin, 0.6f, yMin + 0.04f), "aui.select zone");
                i++;
                yMin = GetVerticalPos(i);

                // Time Limit
                if (!Arena.Events.IgnoreTimeLimit(config.eventType))
                {
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Time Limit", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                    if (config.timeLimit == 0)
                        UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator timeLimit", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                    else
                    {
                        UI.Label(ref container, UIPanel.Menu, $"{config.timeLimit} minutes", 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                        UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), "aui.clear timeLimit");
                    }
                    i++;
                    yMin = GetVerticalPos(i);
                }

                // Kill Limit
                if (!Arena.Events.IgnoreKillLimit(config.eventType))
                {
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Menu, "Score Limit", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                    if (config.killLimit == 0)
                        UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator killLimit", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                    else
                    {
                        UI.Label(ref container, UIPanel.Menu, $"{config.killLimit}", 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                        UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), "aui.clear killLimit");
                    }
                    i++;
                    yMin = GetVerticalPos(i);
                }

                // Minimum Players to start
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Required players to start event", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                if (config.playersToStart == 0)
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator playersToStart", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, $"{config.playersToStart}", 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), "aui.clear playersToStart");
                }
                i++;
                yMin = GetVerticalPos(i);

                // Minimum Players
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Minimum Players", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                if (config.minimumPlayers == 0)
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator minPlayers", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, $"{config.minimumPlayers}", 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), "aui.clear minPlayers");
                }
                i++;
                yMin = GetVerticalPos(i);

                // Maximum Players
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Maximum Players", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                if (config.maximumPlayers == 0)
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator maxPlayers", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, $"{config.maximumPlayers}", 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), "aui.clear maxPlayers");
                }
                i++;
                yMin = GetVerticalPos(i);

                // Permission
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Permission", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));
                if (string.IsNullOrEmpty(config.permission))
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator permission", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, config.permission, 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), "aui.clear permission");
                }
                i++;
                yMin = GetVerticalPos(i);

                if (Arena.additionalParameters.ContainsKey(config.eventType))
                {
                    List<Arena.AdditionalParamData> additionalParams = Arena.additionalParameters[config.eventType];
                    if (additionalParams != null)
                    {
                        foreach (var param in additionalParams)
                        {
                            if (!config.additionalParameters.ContainsKey(param.field))
                                config.additionalParameters.Add(param.field, param.type == "int" || param.type == "float" ? "0" : param.type == "string" ? "" : "False");

                            if (param.useInput)
                            {
                                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                                UI.Label(ref container, UIPanel.Menu, param.field, 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.6f, yMin + 0.04f));

                                if (config.additionalParameters[param.field] == null || ((param.type == "int" || param.type == "float") && config.additionalParameters[param.field].ToString() == "0")) 
                                    UI.Input(ref container, UIPanel.Menu, "", 15, $"aui.creator {param.field.Replace(" ", "<><>")}", new UI4(0.215f, yMin, 0.6f, yMin + 0.04f));
                                else
                                {
                                    UI.Label(ref container, UIPanel.Menu, config.additionalParameters[param.field].ToString(), 15, new UI4(0.215f, yMin, 0.6f, yMin + 0.04f), TextAnchor.MiddleLeft);
                                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.603f, yMin, 0.63f, yMin + 0.04f), $"aui.clear {param.field.Replace(" ", "<><>")}");
                                }
                            }
                            else if (param.useSelector)
                            {
                                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                                UI.Label(ref container, UIPanel.Menu, param.field, 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.497f, yMin + 0.04f));

                                UI.Label(ref container, UIPanel.Menu, config.additionalParameters[param.field].ToString(), 15, new UI4(0.215f, yMin, 0.497f, yMin + 0.04f), TextAnchor.MiddleLeft);
                                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Select", 15, new UI4(0.5f, yMin, 0.6f, yMin + 0.04f), $"aui.select {param.field.Replace(" ", "<><>")}");
                            }
                            else if (param.useToggle)
                            {
                                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                                UI.Label(ref container, UIPanel.Menu, param.field, 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);
                                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.205f, yMin, 0.497f, yMin + 0.04f));

                                UI.Label(ref container, UIPanel.Menu, (Convert.ToBoolean(config.additionalParameters[param.field])).ToString(), 15, new UI4(0.215f, yMin, 0.497f, yMin + 0.04f), TextAnchor.MiddleLeft);
                                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], Convert.ToBoolean(config.additionalParameters[param.field]) ? "Disable" : "Enable", 15, new UI4(0.5f, yMin, 0.6f, yMin + 0.04f), $"aui.creator {param.field.Replace(" ", "<><>")}");
                            }
                            i++;
                            yMin = GetVerticalPos(i);
                        }
                    }
                }

                // Reward Overrides
                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.202f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Reward Overrides", 15, new UI4(0.03f, yMin, 0.202f, yMin + 0.04f), TextAnchor.MiddleLeft);

                string killAmount = (config.killRewardOverride == -1 ? Arena.configData.RewardSettings.KillAmount : config.killRewardOverride).ToString();
                string headshotAmount = (config.headshotRewardOverride == -1 ? Arena.configData.RewardSettings.HeadshotAmount : config.headshotRewardOverride).ToString();
                string winAmount = (config.winRewardOverride == -1 ? Arena.configData.RewardSettings.WinAmount : config.winRewardOverride).ToString();

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.205f, yMin, 0.305f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Kills", 15, new UI4(0.21f, yMin, 0.305f, yMin + 0.04f), TextAnchor.MiddleLeft);

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.308f, yMin, 0.408f, yMin + 0.04f));

                if (config.killRewardOverride == -1)
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator killReward", new UI4(0.318f, yMin, 0.408f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, $"{config.killRewardOverride}", 15, new UI4(0.318f, yMin, 0.408f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.411f, yMin, 0.438f, yMin + 0.04f), "aui.clear killReward");
                }


                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.441f, yMin, 0.541f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Headshots", 15, new UI4(0.451f, yMin, 0.541f, yMin + 0.04f), TextAnchor.MiddleLeft);

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.544f, yMin, 0.644f, yMin + 0.04f));

                if (config.headshotRewardOverride == -1)
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator headShotReward", new UI4(0.554f, yMin, 0.644f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, $"{config.headshotRewardOverride}", 15, new UI4(0.554f, yMin, 0.644f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.647f, yMin, 0.674f, yMin + 0.04f), "aui.clear headShotReward");
                }

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.677f, yMin, 0.777f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Menu, "Wins", 15, new UI4(0.687f, yMin, 0.777f, yMin + 0.04f), TextAnchor.MiddleLeft);

                UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], new UI4(0.78f, yMin, 0.88f, yMin + 0.04f));

                if (config.winRewardOverride == -1)
                    UI.Input(ref container, UIPanel.Menu, "", 15, "aui.creator winReward", new UI4(0.79f, yMin, 0.88f, yMin + 0.04f));
                else
                {
                    UI.Label(ref container, UIPanel.Menu, $"{config.winRewardOverride}", 15, new UI4(0.79f, yMin, 0.88f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Button(ref container, UIPanel.Menu, uiColors[ColorType.Button_Selected], "X", 15, new UI4(0.91f, yMin, 0.937f, yMin + 0.04f), "aui.clear winReward");
                }

                i++;
                yMin = GetVerticalPos(i);
            }

            DestroyUI(player, UIPanel.Menu);
            AddUI(player, UIPanel.Menu, container);
        }

        private void KitSelectionMenu(BasePlayer player)
        {
            Arena.ArenaData.EventConfig config = eventCreator[player.userID];
            CuiElementContainer container = UI.ElementContainer(UIPanel.Menu, uiColors[ColorType.BG_Main_Transparent], new UI4(0, 0, 1, 1), true);
            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Main_Solid], new UI4(0.005f, 0.01f, 0.995f, 0.95f));
            UI.Label(ref container, UIPanel.Menu, "Select kits available in this event", 16, new UI4(0, 0.95f, 1, 1));
            UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Done", 15, new UI4(0.005f, 0.955f, 0.125f, 0.995f), "aui.creator return");
            
            int i = 0;
            foreach(string kit in (Kits?.Call("GetAllKits") as string[]).OrderBy(x => x))
            {
                float[] position = CalculateButtonPos(i);
                UI.StyledButton(ref container, UIPanel.Menu, config.eventKits.Contains(kit) ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], kit, 15, new UI4(position[0], position[1], position[2], position[3]), $"aui.select kits {kit.Replace(" ", "<><>")}");
                i++;
            }

            DestroyUI(player, UIPanel.Menu);
            AddUI(player, UIPanel.Menu, container);
        }

        private void SelectionMenu(BasePlayer player, string[] strings, string message, string callback, bool returnButton = true)
        {
            CuiElementContainer container = UI.ElementContainer(UIPanel.Menu, uiColors[ColorType.BG_Main_Transparent], new UI4(0, 0, 1, 1), true);
            UI.Panel(ref container, UIPanel.Menu, uiColors[ColorType.Panel_Main_Solid], new UI4(0.005f, 0.01f, 0.995f, 0.95f));
            UI.Label(ref container, UIPanel.Menu, message, 16, new UI4(0, 0.95f, 1, 1f));

            if (returnButton)
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], "Return", 15, new UI4(0.005f, 0.955f, 0.125f, 0.995f), "aui.creator return");

            int i = 0;
            foreach (string str in strings)
            {
                float[] position = CalculateButtonPos(i);
                UI.StyledButton(ref container, UIPanel.Menu, uiColors[ColorType.Button_Deselected], str, 15, new UI4(position[0], position[1], position[2], position[3]), $"{callback} {str}");
                i++;
            }

            DestroyUI(player, UIPanel.Menu);
            AddUI(player, UIPanel.Menu, container);
        }

        private void PopupError(BasePlayer player, string message)
        {
            CuiElementContainer container = UI.ElementContainer(UIPanel.Popup, uiColors[ColorType.BG_Main_Transparent], new UI4(0.35f, 0.01f, 0.65f, 0.05f));
            UI.Label(ref container, UIPanel.Popup, message, 16, new UI4(0, 0, 1, 1));
            AddUI(player, UIPanel.Popup, container);
            timer.In(5, () => DestroyUI(player, UIPanel.Popup));
        }

        private void PopupMessage(BasePlayer player, string message)
        {
            CuiElementContainer container = UI.ElementContainer(UIPanel.Popup, uiColors[ColorType.BG_Main_Transparent], new UI4(0.35f, 0.01f, 0.65f, 0.08f));
            UI.Label(ref container, UIPanel.Popup, message, 16, new UI4(0, 0, 1, 1));
            AddUI(player, UIPanel.Popup, container);
            timer.In(5, () => DestroyUI(player, UIPanel.Popup));
        }

        private float GetVerticalPos(int i, float start = 0.9f) => start - (i * 0.045f);
        private float[] CalculateButtonPos(int number)
        {
            Vector2 position = new Vector2(0.014f, 0.9f);
            Vector2 dimensions = new Vector2(0.12f, 0.04f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 8)
            {
                offsetX = (0.0015f + dimensions.x) * number;
            }
            if (number > 7 && number < 16)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 8);
                offsetY = (-0.005f - dimensions.y) * 1;
            }
            if (number > 15 && number < 24)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 16);
                offsetY = (-0.005f - dimensions.y) * 2;
            }
            if (number > 23 && number < 32)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 24);
                offsetY = (-0.005f - dimensions.y) * 3;
            }
            if (number > 31 && number < 40)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 32);
                offsetY = (-0.005f - dimensions.y) * 4;
            }
            if (number > 39 && number < 48)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 40);
                offsetY = (-0.005f - dimensions.y) * 5;
            }
            if (number > 47 && number < 56)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 48);
                offsetY = (-0.005f - dimensions.y) * 6;
            }
            if (number > 55 && number < 64)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 56);
                offsetY = (-0.005f - dimensions.y) * 7;
            }
            if (number > 63 && number < 72)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 64);
                offsetY = (-0.005f - dimensions.y) * 8;
            }
            if (number > 71 && number < 80)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 72);
                offsetY = (-0.005f - dimensions.y) * 9;
            }
            if (number > 79 && number < 88)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 80);
                offsetY = (-0.005f - dimensions.y) * 10;
            }
            if (number > 87 && number < 96)
            {
                offsetX = (0.0015f + dimensions.x) * (number - 88);
                offsetY = (-0.005f - dimensions.y) * 11;
            }

            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        #region Creation Commands
        [ConsoleCommand("aui.select")]
        void ccmdSelect(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "arena.admin")) return;

            Arena.ArenaData.EventConfig config = eventCreator[player.userID];
            string input = arg.GetString(0);
            switch (input)
            {
                case "kits":
                    if (arg.Args.Length > 1)
                    {
                        string kit = arg.GetString(1).Replace("<><>", " ");
                        if (config.eventKits.Contains(kit))
                            config.eventKits.Remove(kit);
                        else config.eventKits.Add(kit);
                    }
                    KitSelectionMenu(player);
                    return;
                case "kit":
                    SelectionMenu(player, Kits?.Call("GetAllKits") as string[], "Select a kit", $"aui.creator kit{arg.GetString(1)}");
                    return;
                case "zone":
                    SelectionMenu(player, ZoneManager?.Call("GetZoneIDs") as string[], "Select a zone", "aui.creator zoneId");
                    return;
                case "spawns":
                    SelectionMenu(player, Spawns?.Call("GetSpawnfileNames") as string[], "Select a spawnfile", $"aui.creator spawns{arg.GetString(1)}");
                    return;
                default:
                    if (config.additionalParameters.ContainsKey(input.Replace("<><>", " ")))
                    {
                        Arena.AdditionalParamData paramData = Arena.additionalParameters[config.eventType].Find(x => x.field == input.Replace("<><>", " "));
                        string[] stringList = (string[])Interface.Call(paramData.selectorHook);
                        if (stringList != null)                        
                            SelectionMenu(player, stringList, paramData.selectorDesc, $"aui.creator {paramData.field.Replace(" ", "<><>")}");                        
                    }
                    return;
            }
        }

        [ConsoleCommand("aui.creator")]
        void ccmdCreator(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "arena.admin")) return;

            Arena.ArenaData.EventConfig config = eventCreator[player.userID];
            string input = arg.GetString(0).Replace("<><>", " ");
            string value = arg.Args != null && arg.Args.Length > 1 ? string.Join(" ", arg.Args.Skip(1).ToArray()) : string.Empty;

            switch (input)
            {
                case "exit":
                    eventCreator.Remove(player.userID);
                    if (eventEditorBackup.ContainsKey(player.userID))
                    {
                        Arena.ArenaData.EventConfig eventConfig = eventEditorBackup[player.userID];
                        Arena.InitializeEvent(eventConfig.eventName, eventConfig);
                        eventEditorBackup.Remove(player.userID);
                    }
                    DestroyUI(player, UIPanel.Menu);
                    DestroyUI(player, UIPanel.Popup);
                    return;
                case "eventname":
                    if (string.IsNullOrEmpty(value))
                        PopupError(player, "You must enter an event name");
                    else config.eventName = value;
                    break;                
                case "eventtype":
                    if (string.IsNullOrEmpty(value))
                        return;
                    config.eventType = value;                                       
                    break;
                case "description":
                    config.description = value;
                    break;
                case "permission":
                    if (!value.StartsWith("arena."))
                        value = $"arena.{value}";
                    config.permission = value;
                    break;
                case "classselector":
                    config.useClassSelector = bool.Parse(value);
                    break;
                case "switchteam":
                    config.switchTeamOnRepeat = bool.Parse(value);
                    break;
                case "spawnsA":
                    config.teamA.spawnfile = value;
                    break;
                case "nameA":
                    config.teamA.name = value;
                    break;
                case "colorA":
                    config.teamA.color = value;
                    break;
                case "kitA":
                    config.teamA.kit = value;
                    break;
                case "spawnsB":
                    config.teamB.spawnfile = value;
                    break;
                case "nameB":
                    config.teamB.name = value;
                    break;
                case "colorB":
                    config.teamB.color = value;
                    break;
                case "kitB":
                    config.teamB.kit = value;
                    break;
                case "zoneId":
                    config.zoneId = value;
                    break;
                case "addKit":
                    config.eventKits.Add(value);
                    break;
                case "removeKit":
                    config.eventKits.Remove(value);
                    break;
                case "icon":
                    config.eventIcon = value;
                    break;
                case "killLimit":
                    int killLimit = 0;
                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out killLimit))
                        PopupError(player, "You must enter an kill limit (number)");
                    else config.killLimit = killLimit;
                    break;
                case "timeLimit":
                    int timeLimit = 0;
                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out timeLimit))
                        PopupError(player, "You must enter an time limit in minutes (number)");
                    else config.timeLimit = timeLimit;
                    break;
                case "playersToStart":
                    int playersToStart = 0;
                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out playersToStart))
                        PopupError(player, "You must enter an minimum number of players required to start the event");
                    else config.playersToStart = playersToStart;
                    break;
                case "minPlayers":
                    int minPlayers = 0;
                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out minPlayers))
                        PopupError(player, "You must enter an minimum number of players (number)");
                    else config.minimumPlayers = minPlayers;
                    break;
                case "maxPlayers":
                    int maxPlayers = 0;
                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out maxPlayers))
                        PopupError(player, "You must enter an maximum number of players (number)");
                    else config.maximumPlayers = maxPlayers;
                    break;
                case "friendlyFire":
                    config.ffEnabled = bool.Parse(value);
                    break;
                case "killReward":
                    int killReward = 0;
                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out killReward))
                        PopupError(player, "You must enter an valid number");
                    else config.killRewardOverride = Mathf.Abs(killReward);
                    break;
                case "headShotReward":
                    int headshotReward = 0;
                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out headshotReward))
                        PopupError(player, "You must enter an valid number");
                    else config.headshotRewardOverride = Mathf.Abs(headshotReward);
                    break;
                case "winReward":
                    int winReward = 0;
                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out winReward))
                        PopupError(player, "You must enter an valid number");
                    else config.winRewardOverride = Mathf.Abs(winReward);
                    break;
                case "save":
                    {
                        if (!eventEditorBackup.ContainsKey(player.userID))
                        {
                            if (Arena.events.ContainsKey(config.eventName))
                            {
                                PopupError(player, "An event with that name already exists");
                                return;
                            }
                        }
                        object success = Arena.ValidateEventConfig(config);
                        if (success is string)
                        {
                            PopupError(player, (string)success);
                            return;
                        }

                        if (Arena.additionalParameters.ContainsKey(config.eventType))
                        {
                            foreach(var param in Arena.additionalParameters[config.eventType])
                            {
                                if (config.additionalParameters.ContainsKey(param.field))
                                {
                                    if (!param.isRequired)
                                        continue;

                                    if (config.additionalParameters[param.field] != null)
                                    {
                                        int intField = 0;
                                        if (param.type == "int" && int.TryParse(config.additionalParameters[param.field].ToString(), out intField))
                                        {
                                            if (param.requireValue && intField > 0 || !param.requireValue)
                                                continue;
                                        }

                                        float floatField = 0;
                                        if (param.type == "float" && float.TryParse(config.additionalParameters[param.field].ToString(), out floatField))
                                        {
                                            if (param.requireValue && floatField > 0 || !param.requireValue)
                                                continue;
                                        }

                                        bool boolField = false;
                                        if (param.type == "bool" && bool.TryParse(config.additionalParameters[param.field].ToString(), out boolField))
                                            continue;

                                        if (param.type == "string" && !string.IsNullOrEmpty(config.additionalParameters[param.field].ToString()))
                                            continue;
                                    }                                   
                                }
                                PopupError(player, $"You must enter a value for the option: {param.field}");
                                return;
                            }
                        }
                        if (!eventEditorBackup.ContainsKey(player.userID))                        
                            Arena.AddNewEvent(config);
                        else
                        {
                            if (config.eventName != eventEditorBackup[player.userID].eventName)
                            {
                                Arena.RemoveEvent(eventEditorBackup[player.userID].eventName);
                                Arena.AddNewEvent(config);
                            }
                            else Arena.UpdateEvent(config.eventName, config);
                            eventEditorBackup.Remove(player.userID);                   
                        }

                        eventCreator.Remove(player.userID);
                        DestroyUI(player, UIPanel.Menu);
                        DestroyUI(player, UIPanel.Popup);
                    }
                    return;
                case "return":
                    break;
                default:
                    if (config.additionalParameters.ContainsKey(input))
                    {
                        string type = Arena.additionalParameters[config.eventType].Find(x => x.field == input)?.type;
                        switch (type)
                        {
                            case "string":
                                config.additionalParameters[input] = value;
                                break;
                            case "int":
                                {
                                    int parsedValue;
                                    if (string.IsNullOrEmpty(value) || !int.TryParse(value, out parsedValue))
                                        PopupError(player, "You must enter an number");
                                    else config.additionalParameters[input] = parsedValue;
                                }
                                break;
                            case "float":
                                {
                                    float parsedValue;
                                    if (string.IsNullOrEmpty(value) || !float.TryParse(value, out parsedValue))
                                        PopupError(player, "You must enter an number");
                                    else config.additionalParameters[input] = parsedValue;
                                }
                                break;
                            case "bool":                                
                                config.additionalParameters[input] = !Convert.ToBoolean(config.additionalParameters[input]);                                
                                break;
                            default:
                                break;
                        }
                    }
                    break;
            }
            OpenCreatorMenu(player);
        }

        [ConsoleCommand("aui.clear")]
        void ccmdClearEntry(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "arena.admin")) return;

            Arena.ArenaData.EventConfig config = eventCreator[player.userID];
            string input = arg.GetString(0).Replace("<><>", " ");

            switch (input)
            {                
                case "eventname":                   
                    config.eventName = string.Empty;
                    break;
                case "description":
                    config.description = string.Empty;
                    break;
                case "permission":
                    config.permission = string.Empty;
                    break;           
                case "nameA":                   
                    config.teamA.name = string.Empty;
                    break;
                case "colorA":                    
                    config.teamA.color = string.Empty;
                    break;  
                case "nameB":                   
                    config.teamB.name = string.Empty;
                    break;
                case "colorB":                   
                    config.teamB.color = string.Empty;
                    break;                
                case "icon":                   
                    config.eventIcon = string.Empty;
                    break;
                case "killLimit":                    
                    config.killLimit = 0;
                    break;
                case "timeLimit":                   
                    config.timeLimit = 0;
                    break;
                case "playersToStart":
                    config.playersToStart = 0;
                    break;
                case "minPlayers":                    
                    config.minimumPlayers = 0;
                    break;
                case "maxPlayers":                    
                    config.maximumPlayers = 0;
                    break;
                case "killReward":
                    config.killRewardOverride = -1;
                    break;
                case "headShotReward":
                    config.headshotRewardOverride = -1;
                    break;
                case "winReward":
                    config.winRewardOverride = -1;
                    break;
                default:
                    if (config.additionalParameters.ContainsKey(input))
                        config.additionalParameters[input] = null;
                    break;
            }
            OpenCreatorMenu(player);
        }
        #endregion
        #endregion
        #endregion

        #region Statistics Menu
        public enum StatisticTab { Personal, Global, Leaders, Events }
        public void OpenStatisticsMenu(BasePlayer player, StatisticTab openTab, int page = 0)
        {
            CuiElementContainer container = UI.ElementContainer(UIPanel.Statistics, uiColors[ColorType.BG_Main_Solid], new UI4(0, 0, 1, 1), true);
            AddStatisticHeader(ref container, player.userID, openTab);

            UI.StyledButton(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], msg("exit", player.userID), 16, new UI4(0.01f, 0.94f, 0.13f, 0.98f), "aui.statistics destroy");
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Main_Solid], new UI4(0.01f, 0.01f, 0.99f, 0.79f));

            switch (openTab)
            {
                case StatisticTab.Personal:
                    AddPersonalStats(ref container, player.userID, page);
                    break;
                case StatisticTab.Global:
                    AddGlobalStats(ref container, player.userID, page);
                    break;
                case StatisticTab.Leaders:
                    AddLeaderStats(ref container, player.userID, page);
                    break;
                case StatisticTab.Events:
                    AddEventStats(ref container, player.userID, page);
                    break;                
            }

            DestroyUI(player, UIPanel.Statistics);
            AddUI(player, UIPanel.Statistics, container);
        }

        private void AddStatisticHeader(ref CuiElementContainer container, ulong playerId, StatisticTab openTab)
        {
            if (hasLogo)
                UI.Image(ref container, UIPanel.Statistics, GetImage("logo"), new UI4(0.005f, 0.85f, 0.995f, 0.99f));

            int i = 0;
            float xMin = GetHorizontalPos(i);

            UI.StyledButton(ref container, UIPanel.Statistics, openTab == StatisticTab.Personal ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], msg("personalstats", playerId), 15, new UI4(xMin, 0.8f, xMin + 0.12f, 0.84f), openTab == StatisticTab.Personal ? "" : $"aui.statistics personal");
            i++;
            xMin = GetHorizontalPos(i);

            UI.StyledButton(ref container, UIPanel.Statistics, openTab == StatisticTab.Global ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], msg("globalstats", playerId), 15, new UI4(xMin, 0.8f, xMin + 0.12f, 0.84f), openTab == StatisticTab.Global ? "" : $"aui.statistics global");
            i++;
            xMin = GetHorizontalPos(i);

            UI.StyledButton(ref container, UIPanel.Statistics, openTab == StatisticTab.Events ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], msg("eventstats", playerId), 15, new UI4(xMin, 0.8f, xMin + 0.12f, 0.84f), openTab == StatisticTab.Events ? "" : $"aui.statistics events");
            i++;
            xMin = GetHorizontalPos(i);

            UI.StyledButton(ref container, UIPanel.Statistics, openTab == StatisticTab.Leaders ? uiColors[ColorType.Button_Selected] : uiColors[ColorType.Button_Deselected], msg("leaderstats", playerId), 15, new UI4(xMin, 0.8f, xMin + 0.12f, 0.84f), openTab == StatisticTab.Leaders ? "" : $"aui.statistics leaders");
            i++;
            xMin = GetHorizontalPos(i); 
        }      

        private void AddPersonalStats(ref CuiElementContainer container, ulong playerId, int page = 0)
        {
            ArenaStatistics.ScoreData data = ArenaStatistics.GetPlayerData(playerId);
            if (data != null)
            {
                if (data.GetGamesPlayed().Count > (16 * page) + 16)
                    UI.StyledButton(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], msg("next", playerId), 15, new UI4(0.86f, 0.745f, 0.98f, 0.785f), $"aui.statistics personal {page + 1}");
                if (page > 0)
                    UI.StyledButton(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], msg("back", playerId), 15, new UI4(0.7395f, 0.745f, 0.8595f, 0.785f), $"aui.statistics personal {page - 1}");

                int i = 0;
                UI.Label(ref container, UIPanel.Statistics, msg("Personal Statistics", playerId), 16, new UI4(0.025f, 0.745f, 0.38f, 0.785f), TextAnchor.MiddleLeft);

                float yMin = GetVerticalPos(i, 0.7f);
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.38f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], new UI4(0.3805f, yMin, 0.48f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, msg("rank", playerId), 15, new UI4(0.025f, yMin, 0.38f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Label(ref container, UIPanel.Statistics, ArenaStatistics.GetPlayerRank(playerId), 15, new UI4(0.3805f, yMin, 0.475f, yMin + 0.04f), TextAnchor.MiddleRight);
                i++;

                foreach (var score in data.GetScores())
                {                    
                    yMin = GetVerticalPos(i, 0.7f);
                    UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.38f, yMin + 0.04f));
                    UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], new UI4(0.3805f, yMin, 0.48f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Statistics, msg(score.Key.ToString(), playerId), 15, new UI4(0.025f, yMin, 0.38f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Label(ref container, UIPanel.Statistics, score.Value.ToString(), 15, new UI4(0.3805f, yMin, 0.475f, yMin + 0.04f), TextAnchor.MiddleRight);
                    i++;
                }

                UI.Label(ref container, UIPanel.Statistics, msg("Games Played", playerId), 16, new UI4(0.525f, 0.745f, 0.7f, 0.785f), TextAnchor.MiddleLeft);

                Hash<string, int> gameData = data.GetGamesPlayed();
                int j = 0;
                for (int k = page * 16; k < (page * 16) + 16; k++)
                {
                    if (k >= gameData.Count)
                        break;

                    KeyValuePair<string, int> eventGame = gameData.ElementAt(k);

                    yMin = GetVerticalPos(j, 0.7f);
                    UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.52f, yMin, 0.88f, yMin + 0.04f));
                    UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], new UI4(0.8805f, yMin, 0.98f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Statistics, eventGame.Key, 15, new UI4(0.525f, yMin, 0.88f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Label(ref container, UIPanel.Statistics, eventGame.Value.ToString(), 15, new UI4(0.8805f, yMin, 0.975f, yMin + 0.04f), TextAnchor.MiddleRight);
                    j++;
                }
            }
        }
        private void AddGlobalStats(ref CuiElementContainer container, ulong playerId, int page = 0)
        {
            ArenaStatistics.ScoreData data = ArenaStatistics.GetGlobalData();
            if (data != null)
            {
                if (data.GetGamesPlayed().Count > (16 * page) + 16)
                    UI.StyledButton(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], msg("next", playerId), 10, new UI4(0.86f, 0.745f, 0.98f, 0.785f), $"aui.statistics global {page + 1}");
                if (page > 0)
                    UI.StyledButton(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], msg("back", playerId), 10, new UI4(0.7395f, 0.745f, 0.8595f, 0.785f), $"aui.statistics global {page - 1}");

                int i = 0;                
                UI.Label(ref container, UIPanel.Statistics, msg("Global Statistics", playerId), 16, new UI4(0.025f, 0.745f, 0.38f, 0.785f), TextAnchor.MiddleLeft);
                               
                foreach (var score in data.GetScores())
                {
                    float yMin = GetVerticalPos(i, 0.7f);
                    UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.02f, yMin, 0.38f, yMin + 0.04f));
                    UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], new UI4(0.3805f, yMin, 0.48f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Statistics, msg(score.Key.ToString(), playerId), 15, new UI4(0.025f, yMin, 0.38f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Label(ref container, UIPanel.Statistics, score.Value.ToString(), 15, new UI4(0.3805f, yMin, 0.475f, yMin + 0.04f), TextAnchor.MiddleRight);
                    i++;
                }  
               
                UI.Label(ref container, UIPanel.Statistics, msg("Games Played", playerId), 16, new UI4(0.525f, 0.745f, 0.7f, 0.785f), TextAnchor.MiddleLeft);

                Hash<string, int> gameData = data.GetGamesPlayed();
                int j = 0;
                for (int k = page * 16; k < (page * 16) + 16; k++)
                {
                    if (k >= gameData.Count)
                        break;

                    KeyValuePair<string, int> eventGame = gameData.ElementAt(k);
                   
                    float yMin = GetVerticalPos(j, 0.7f);
                    UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.52f, yMin, 0.88f, yMin + 0.04f));
                    UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], new UI4(0.8805f, yMin, 0.98f, yMin + 0.04f));
                    UI.Label(ref container, UIPanel.Statistics, eventGame.Key, 15, new UI4(0.525f, yMin, 0.88f, yMin + 0.04f), TextAnchor.MiddleLeft);
                    UI.Label(ref container, UIPanel.Statistics, eventGame.Value.ToString(), 15, new UI4(0.8805f, yMin, 0.975f, yMin + 0.04f), TextAnchor.MiddleRight);
                    j++;                    
                }
            }
        }
        private void AddEventStats(ref CuiElementContainer container, ulong playerId, int page = 0)
        {
            Hash<string, ArenaStatistics.EventData> data = ArenaStatistics.GetEventData();

            if (data.Count > (16 * page) + 16)
                UI.Button(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], ">\n>\n>", 10, new UI4(0.975f, 0.36f, 0.99f, 0.44f), $"aui.statistics events {page + 1}");
            if (page > 0)
                UI.Button(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], "<\n<\n<", 10, new UI4(0.01f, 0.36f, 0.025f, 0.44f), $"aui.statistics events {page - 1}");

            float yMin = 0.745f;

            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.12f, yMin, 0.4f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.402f, yMin, 0.46f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.462f, yMin, 0.52f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.522f, yMin, 0.58f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.582f, yMin, 0.64f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.642f, yMin, 0.7f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.702f, yMin, 0.76f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.762f, yMin, 0.82f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.822f, yMin, 0.88f, yMin + 0.04f));

            UI.Label(ref container, UIPanel.Statistics, msg("eventname", playerId), 12, new UI4(0.125f, yMin, 0.4f, yMin + 0.04f), TextAnchor.MiddleLeft);
            UI.Label(ref container, UIPanel.Statistics, msg("Kill", playerId), 12, new UI4(0.402f, yMin, 0.46f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Death", playerId), 12, new UI4(0.462f, yMin, 0.52f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Assist", playerId), 12, new UI4(0.522f, yMin, 0.58f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Headshot", playerId), 12, new UI4(0.582f, yMin, 0.64f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Melee", playerId), 12, new UI4(0.642f, yMin, 0.7f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("winners", playerId), 12, new UI4(0.702f, yMin, 0.76f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("losers", playerId), 12, new UI4(0.762f, yMin, 0.82f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("played", playerId), 12, new UI4(0.822f, yMin, 0.88f, yMin + 0.04f));

            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], new UI4(0.12f, 0.741f, 0.88f, 0.744f));

            int j = 0;
            for (int i = page * 16; i < (page * 16) + 16; i++)
            {
                if (i >= data.Count)
                    break;
                KeyValuePair<string, ArenaStatistics.EventData> eventGame = data.ElementAt(i);

                yMin = GetVerticalPos(j, 0.7f);
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.12f, yMin, 0.4f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.402f, yMin, 0.46f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.462f, yMin, 0.52f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.522f, yMin, 0.58f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.582f, yMin, 0.64f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.642f, yMin, 0.7f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.702f, yMin, 0.76f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.762f, yMin, 0.82f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.822f, yMin, 0.88f, yMin + 0.04f));

                UI.Label(ref container, UIPanel.Statistics, eventGame.Key, 15, new UI4(0.125f, yMin, 0.4f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Label(ref container, UIPanel.Statistics, eventGame.Value.scores[ArenaStatistics.ScoreType.Kill].ToString(), 15, new UI4(0.402f, yMin, 0.46f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, eventGame.Value.scores[ArenaStatistics.ScoreType.Death].ToString(), 15, new UI4(0.462f, yMin, 0.52f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, eventGame.Value.scores[ArenaStatistics.ScoreType.Assist].ToString(), 15, new UI4(0.522f, yMin, 0.58f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, eventGame.Value.scores[ArenaStatistics.ScoreType.Headshot].ToString(), 15, new UI4(0.582f, yMin, 0.64f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, eventGame.Value.scores[ArenaStatistics.ScoreType.Melee].ToString(), 15, new UI4(0.642f, yMin, 0.7f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, eventGame.Value.scores[ArenaStatistics.ScoreType.Win].ToString(), 15, new UI4(0.702f, yMin, 0.76f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, eventGame.Value.scores[ArenaStatistics.ScoreType.Loss].ToString(), 15, new UI4(0.762f, yMin, 0.82f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, eventGame.Value.matchesPlayed.ToString(), 15, new UI4(0.822f, yMin, 0.88f, yMin + 0.04f));
                j++;               
            }
        }
        private void AddLeaderStats(ref CuiElementContainer container, ulong playerId, int page = 0)
        {
            List<ulong> data = ArenaStatistics.GetRankList();

            if (data.Count > (16 * page) + 16)
                UI.Button(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], ">\n>\n>", 10, new UI4(0.975f, 0.36f, 0.99f, 0.44f), $"aui.statistics leaders {page + 1}");
            if (page > 0)
                UI.Button(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], "<\n<\n<", 10, new UI4(0.01f, 0.36f, 0.025f, 0.44f), $"aui.statistics leaders {page - 1}");

            float yMin = 0.745f;

            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.12f, yMin, 0.153f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.155f, yMin, 0.4f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.402f, yMin, 0.46f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.462f, yMin, 0.52f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.522f, yMin, 0.58f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.582f, yMin, 0.64f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.642f, yMin, 0.7f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.702f, yMin, 0.76f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.762f, yMin, 0.82f, yMin + 0.04f));
            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Deselected], new UI4(0.822f, yMin, 0.88f, yMin + 0.04f));

            UI.Label(ref container, UIPanel.Statistics, msg("player", playerId), 12, new UI4(0.157f, yMin, 0.4f, yMin + 0.04f), TextAnchor.MiddleLeft);
            UI.Label(ref container, UIPanel.Statistics, msg("Kill", playerId), 12, new UI4(0.402f, yMin, 0.46f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Death", playerId), 12, new UI4(0.462f, yMin, 0.52f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Assist", playerId), 12, new UI4(0.522f, yMin, 0.58f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Headshot", playerId), 12, new UI4(0.582f, yMin, 0.64f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Melee", playerId), 12, new UI4(0.642f, yMin, 0.7f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Won", playerId), 12, new UI4(0.702f, yMin, 0.76f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("Lost", playerId), 12, new UI4(0.762f, yMin, 0.82f, yMin + 0.04f));
            UI.Label(ref container, UIPanel.Statistics, msg("played", playerId), 12, new UI4(0.822f, yMin, 0.88f, yMin + 0.04f));

            UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Button_Selected], new UI4(0.12f, 0.741f, 0.88f, 0.744f));

            int j = 0;
            for (int i = page * 16; i < (page * 16) + 16; i++)
            {
                if (i >= data.Count)
                    break;
                ArenaStatistics.ScoreData userData = ArenaStatistics.GetPlayerData(data[i]);
                
                yMin = GetVerticalPos(j, 0.7f);
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.12f, yMin, 0.153f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.155f, yMin, 0.4f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.402f, yMin, 0.46f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.462f, yMin, 0.52f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.522f, yMin, 0.58f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.582f, yMin, 0.64f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.642f, yMin, 0.7f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.702f, yMin, 0.76f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.762f, yMin, 0.82f, yMin + 0.04f));
                UI.Panel(ref container, UIPanel.Statistics, uiColors[ColorType.Panel_Alt_Solid], new UI4(0.822f, yMin, 0.88f, yMin + 0.04f));

                UI.Label(ref container, UIPanel.Statistics, (i + 1).ToString(), 15, new UI4(0.122f, yMin, 0.153f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, userData.playerName ?? "Unknown", 15, new UI4(0.157f, yMin, 0.4f, yMin + 0.04f), TextAnchor.MiddleLeft);
                UI.Label(ref container, UIPanel.Statistics, userData.GetScore(ArenaStatistics.ScoreType.Kill).ToString(), 15, new UI4(0.402f, yMin, 0.46f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, userData.GetScore(ArenaStatistics.ScoreType.Death).ToString(), 15, new UI4(0.462f, yMin, 0.52f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, userData.GetScore(ArenaStatistics.ScoreType.Assist).ToString(), 15, new UI4(0.522f, yMin, 0.58f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, userData.GetScore(ArenaStatistics.ScoreType.Headshot).ToString(), 15, new UI4(0.582f, yMin, 0.64f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, userData.GetScore(ArenaStatistics.ScoreType.Melee).ToString(), 15, new UI4(0.642f, yMin, 0.7f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, userData.GetScore(ArenaStatistics.ScoreType.Win).ToString(), 15, new UI4(0.702f, yMin, 0.76f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, userData.GetScore(ArenaStatistics.ScoreType.Loss).ToString(), 15, new UI4(0.762f, yMin, 0.82f, yMin + 0.04f));
                UI.Label(ref container, UIPanel.Statistics, userData.GetScore(ArenaStatistics.ScoreType.Played).ToString(), 15, new UI4(0.822f, yMin, 0.88f, yMin + 0.04f));
                j++;
            }
        }

        private float GetHorizontalPos(int i) => 0.01f + (0.1205f * i);

        #region Statistic Commands
        [ChatCommand("stats")]
        void cmdStatistics(BasePlayer player, string command, string[] args)
        {
            if (Arena.GetUser(player) != null)
            {
                SendReply(player, msg("inevent", player.UserIDString));
                return;
            }
            OpenStatisticsMenu(player, StatisticTab.Personal, 0);
        }

        [ConsoleCommand("aui.statistics")]
        void ccmdStatistics(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            int page = 0;
            if (arg.Args.Length > 1)
                page = arg.GetInt(1);

            switch (arg.GetString(0))
            {
                case "destroy":
                    DestroyUI(player, UIPanel.Statistics);
                    return;
                case "personal":
                    OpenStatisticsMenu(player, StatisticTab.Personal, page);
                    return;
                case "global":
                    OpenStatisticsMenu(player, StatisticTab.Global, page);
                    return;
                case "events":                    
                    OpenStatisticsMenu(player, StatisticTab.Events, page);
                    return;
                case "leaders":
                    OpenStatisticsMenu(player, StatisticTab.Leaders, page);
                    return;
                default:
                    break;
            }
        }
        #endregion
        #endregion
              
        #region Image Management
        private void LoadDefaultImages()
        {         
            if (!ImageLibrary)
            {
                timer.In(5, LoadDefaultImages);
                return;
            }
            if (!string.IsNullOrEmpty(configData.UISettings.ImageFilenames.Logo))
            {
                AddImage("logo", configData.UISettings.ImageFilenames.Logo);                
                hasLogo = true;
            }
           
            AddImage("placeholder", configData.UISettings.ImageFilenames.PlaceholderImage);
            AddImage("skullicon", configData.UISettings.ImageFilenames.SkullIcon);
            AddImage("logosmall", configData.UISettings.ImageFilenames.SmallLogo);
        }

        public void AddImage(string imageName, string fileName) => ImageLibrary.Call("AddImage", fileName.StartsWith("www") || fileName.StartsWith("http") ? fileName : dataDirectory + fileName, imageName);
        public string GetImage(string name) => (string)ImageLibrary.Call("GetImage", name);
        #endregion
                
        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "UI Options")]
            public UIOptions UISettings { get; set; }

            public class UIOptions
            {
                [JsonProperty(PropertyName = "UI Coloring")]
                public Dictionary<string, UIColor> Colors { get; set; }
                [JsonProperty(PropertyName = "Images")]
                public Imagery ImageFilenames { get; set; }

                public class Imagery
                {
                    [JsonProperty(PropertyName = "Server banner (URL or image filename)")]
                    public string Logo { get; set; }
                    [JsonProperty(PropertyName = "Place holder (URL or image filename)")]
                    public string PlaceholderImage { get; set; }
                    [JsonProperty(PropertyName = "Death screen icon (URL or image filename)")]
                    public string SkullIcon { get; set; }
                    [JsonProperty(PropertyName = "Small logo (URL or image filename)")]
                    public string SmallLogo { get; set; }
                }
                public class UIColor
                {
                    public string Hex { get; set; }
                    public float Alpha { get; set; }
                }
            }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                UISettings = new ConfigData.UIOptions
                {
                    Colors = new Dictionary<string, ConfigData.UIOptions.UIColor>
                    {
                        ["Button_Selected"] = new ConfigData.UIOptions.UIColor { Hex = "#d85540", Alpha = 1f },
                        ["Button_Deselected"] = new ConfigData.UIOptions.UIColor { Hex = "#393939", Alpha = 1f },
                        ["BG_Main_PartialTransparent"] = new ConfigData.UIOptions.UIColor { Hex = "#2b2b2b", Alpha = 0.98f },
                        ["BG_Main_Transparent"] = new ConfigData.UIOptions.UIColor { Hex = "#2b2b2b", Alpha = 0.8f },
                        ["BG_Main_Solid"] = new ConfigData.UIOptions.UIColor { Hex = "#2b2b2b", Alpha = 1f },
                        ["Panel_Main_Solid"] = new ConfigData.UIOptions.UIColor { Hex = "#404141", Alpha = 1f },
                        ["Panel_Alt_Solid"] = new ConfigData.UIOptions.UIColor { Hex = "#545554", Alpha = 1f },
                        ["Panel_Event_Full"] = new ConfigData.UIOptions.UIColor { Hex = "#404141", Alpha = 1f },
                        ["Panel_Event_Open"] = new ConfigData.UIOptions.UIColor { Hex = "#545554", Alpha = 1f },
                        ["Panel_Main_Transparent"] = new ConfigData.UIOptions.UIColor { Hex = "#404141", Alpha = 0.8f },
                        ["Panel_Alt_Transparent"] = new ConfigData.UIOptions.UIColor { Hex = "#545554", Alpha = 0.8f }
                    },
                    ImageFilenames = new ConfigData.UIOptions.Imagery
                    {
                        PlaceholderImage = "https://www.rustedit.io/images/placeholder.png",
                        Logo = "https://www.rustedit.io/images/logo.png",
                        SkullIcon = "https://www.rustedit.io/images/skullicon.png",
                        SmallLogo = "http://www.rustedit.io/images/logosmall.png"
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();
            if (configData.Version < new VersionNumber(0, 1, 60))
            {
                configData.UISettings.Colors.Add("Panel_Event_Full", new ConfigData.UIOptions.UIColor { Hex = "#e90000", Alpha = 1f });
                configData.UISettings.Colors.Add("Panel_Event_Open", new ConfigData.UIOptions.UIColor { Hex = "#8ee700", Alpha = 1f });
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }       
        #endregion

        #region Localization
        static string msg(string key, object playerId = null) => ins.lang.GetMessage(key, ins, playerId != null ? playerId.ToString() : null);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["returntolobby"] = "Return to Lobby",
            ["continueplaying"] = "Play Next Game",
            ["returntogame"] = "Return to Game",
            ["viewevent"] = "View Event",
            ["Team Deathmatch"] = "Team Deathmatch",
            ["Free For All"] = "Free For All",
            ["Survival"] = "Survival",
            ["GunGame"] = "GunGame",
            ["Team Survival"] = "Team Survival",
            ["One in the Chamber"] = "One in the Chamber",
            ["returntoevents"] = "Return to Events",
            ["gamestatus"] = "Game Status",
            ["players"] = "Players",
            ["scorelimit"] = "Score Limit",
            ["timelimit"] = "Time Limit",
            ["gear"] = "Gear",
            ["waitforplayers"] = "Waiting for players",
            ["inprogress"] = "In progress",
            ["classselect"] = "Class Selector",
            ["eventspec"] = "Event Specific",
            ["minutes"] = "minutes",
            ["player"] = "Player",
            ["kills"] = "Kills",
            ["deaths"] = "Deaths",
            ["maxcapacity"] = "This event is at maximum capacity",
            ["cantjoinstarted"] = "You can not join this event once it has started",
            ["selectteam"] = "Select a team to enter this event",
            ["Team A"] = "Team A",
            ["Team B"] = "Team B",
            ["enterevent"] = "Enter Event",
            ["canjoinevent"] = "Join this event",
            ["enter"] = "Enter",
            ["exit"] = "Exit",
            ["changeclass"] = "Change Class",
            ["respawn"] = "Respawn",
            ["leaveevent"] = "Leave Event",
            ["switchteam"] = "Switch Team",
            ["cancel"] = "Cancel",
            ["toomanyplayers"] = "There are too many players on ",
            ["autoassigning"] = " auto-assigning to ",
            ["description"] = "Description",
            ["donatoronly"] = "This is a donator event",
            ["menuhelp"] = "Type <color=#ce422b>{0}</color> to open the event menu",
            ["personalstats"] = "Personal",
            ["globalstats"] = "Global",
            ["eventstats"] = "Event",
            ["leaderstats"] = "Leaderboard",
            ["inevent"] = "You can't access the statistics menu whilst in an event",
            ["Assist"] = "Assists",
            ["Kill"] = "Kills",
            ["Death"] = "Deaths",
            ["Headshot"] = "Headshots",
            ["Melee"] = "Melee Kills",
            ["Win"] = "Games Won",
            ["Won"] = "Won",
            ["Loss"] = "Games Lost",
            ["Lost"] = "Lost",
            ["Personal Statistics"] = "Personal Statistics",
            ["Games Played"] = "Games Played",
            ["Global Statistics"] = "Global Statistics",
            ["Event Statistics"] = "Event Statistics",
            ["Leaderboard"] = "Leaderboard",
            ["rank"] = "Rank",
            ["eventname"] = "Event Name",
            ["winners"] = "Winners",
            ["losers"] = "Losers",
            ["played"] = "Played",
            ["next"] = "Next",
            ["back"] = "Back",
            ["selectclass"] = "Select a class to continue",
            ["spawn"] = "Spawn",
            ["unlimited"] = "Unlimited",
            ["nextevent"] = "The next event will begin shortly",
            ["eventClosed"] = "This event has been closed temporarily",
            ["noDonator"] = "You do not have VIP on this server."            
        };
        #endregion
    }
}
