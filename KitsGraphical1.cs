using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kits GUI #1", "Orange", "1.2.2")]
    [Description("https://rustworkshop.space/resources/kits.29/")]
    public class KitsGraphical1 : RustPlugin
    {
        #region Vars
       
        private const string elemMain = "KitsGraphical1.Main";
        private const string elemInfo = "KitsGraphical1.Info";

        #endregion
        
        #region Oxide Hooks

        private object CanSeeKits(BasePlayer player)
        {
            CreateKitsUI(player, 1);
            return true;
        }

        private object CanSeeKitsInfo(BasePlayer player, string kitName)
        {
            BuildContentsUI(player, kitName);
            return true;
        }

        private void OnKitUIRefreshRequested(BasePlayer player)
        {
            var obj = CanSeeKits(player);
        }

        private void OpenKitUIPage(BasePlayer player, int page)
        {
            CreateKitsUI(player, page);
        }

        #endregion

        #region Core

        private void CreateKitsUI(BasePlayer player, int currentPage)
        {
            var kits = API_GetPlayerUIKits(player);
            var maxPages = (int) Math.Ceiling((double) kits.Length / config.kitsOnPage);
            if (currentPage <= 0)
            {
                currentPage = 1;
            }

            if (currentPage > maxPages)
            {
                currentPage = maxPages;
            }

            var forUI = new List<KitUI>();
            var start = (currentPage - 1) * config.kitsOnPage;
            var end = start + config.kitsOnPage;

            for (var i = 0; i < kits.Length; i++)
            {
                if (i >= start && i < end)
                {
                    forUI.Add(kits[i]);
                }
            }
            
            var container = new CuiElementContainer();
            BuildMainUI(player.UserIDString, container, currentPage, maxPages);
            BuildKitsUI(container, forUI.ToArray(), player.UserIDString);
            CuiHelper.DestroyUi(player, elemMain);
            CuiHelper.AddUi(player, container);
        }

        private void BuildMainUI(string userID, CuiElementContainer container, int currentPage, int maxPage)
        {
            // Main panel
            container.Add(new CuiElement
            {
                Name = elemMain,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = config.backgroundColor,
                        Sprite = config.backgroundSprite,
                        Material = config.backgroundMaterial
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
                    new CuiNeedsCursorComponent()
                }
            });
            // Header text
            container.Add(new CuiElement
            {
                Parent = elemMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage(Message.TextHead, userID), Align = TextAnchor.UpperCenter
                    },
                    new CuiOutlineComponent
                    {
                        Color  = config.outlineColor,
                        Distance = config.outlineDistance
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0.5", AnchorMax = "1 1"},
                }
            });
            // Text bottom
            container.Add(new CuiElement
            {
                Parent = elemMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage(Message.TextBottom, userID), Align = TextAnchor.LowerCenter
                    },
                    new CuiOutlineComponent
                    {
                        Color  = config.outlineColor,
                        Distance = config.outlineDistance
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 0.5"},
                }
            });
            
            if (currentPage > 1)
            {
                container.Add(
                    new CuiButton
                    {
                        Text = {Text = GetMessage(Message.PreviousPage, userID), Align = TextAnchor.MiddleCenter},
                        Button = {Command = $"kits.core page {currentPage-1}", Color = "0 0 0 0",},
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "0.1 1"}
                        
                    }, elemMain);
            }

            if (currentPage < maxPage)
            {
                container.Add(
                    new CuiButton
                    {
                        Text = {Text = GetMessage(Message.NextPage, userID), Align = TextAnchor.MiddleCenter},
                        Button = {Command = $"kits.core page {currentPage+1}", Color = "0 0 0 0",},
                        RectTransform = {AnchorMin = "0.9 0", AnchorMax = "1 1"}
                    }, elemMain);
            }

            container.Add(new CuiButton{
                    Text = {Text = null},
                    Button = {Close = elemMain, Color = "1 0 0 0",},
                    RectTransform = {AnchorMin = "0.1 0", AnchorMax = "0.9 1"}
                }, elemMain);
        }

        private void BuildKitsUI(CuiElementContainer container, KitUI[] kits, string userID)
        {
            var i = 0;
            var lines = (int) Math.Ceiling((double) kits.Length / config.kitsPerLine);
            var kitsOnLine = config.kitsPerLine > kits.Length ? kits.Length : config.kitsPerLine;
            var sizeX = config.KitsizeX;
            var sizeY = config.KitsizeY;

            var sizePanelX = kitsOnLine * (config.KitsizeX + config.KitoffsetX) - config.KitoffsetX;
            var sizePanelY = lines * (config.KitsizeY + config.KitoffsetY) - config.KitoffsetY;
            var originalX = - sizePanelX / 2;
            var originalY = sizePanelY / 2;
            var startX = originalX;
            var startY = originalY;
            
            foreach (var kit in kits)
            {
                var panelName = elemMain + "." + kit.name + "." + Core.Random.Range(1000, 9999);
                // Panel
                container.Add(new CuiElement
                {
                    Name = panelName,
                    Parent = elemMain,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = config.kitBackGroundColor
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = $"{startX} {startY - sizeY}",
                            OffsetMax = $"{startX + sizeX} {startY}"
                        }
                    }
                });
                // Icon
                if (string.IsNullOrEmpty(kit.url) == false)
                {
                    container.Add(new CuiElement
                    {
                        Parent = panelName,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage(kit.url),
                                Color = "1 1 1 1"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            }
                        }
                    });
                }
                // Kit Name
                container.Add(new CuiElement
                {
                    Name = panelName + ".name",
                    Parent = panelName,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = kit.displayName,
                            Align = TextAnchor.UpperCenter
                        },
                        new CuiOutlineComponent
                        {
                            Color  = config.outlineColor,
                            Distance = config.outlineDistance
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.05 0.1",
                            AnchorMax = "0.95 0.9"
                        }
                    }
                });
                // Kit Info
                container.Add(new CuiButton
                {
                    Button =
                    {
                        Color  = "0 0 0 0",
                        Command = $"kits.core info {kit.name}"
                    },
                    Text =
                    {
                        Text = GetMessage(Message.InfoText, userID),
                        Align = TextAnchor.UpperRight,
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.05 0.5",
                        AnchorMax = "0.95 0.9"
                    }
                }, panelName);
                
                // Kit claim
                container.Add(new CuiButton
                {
                    Button =
                    {
                        Color  = "0 0 0 0",
                        Command = $"kits.core {kit.name} ui.refresh"
                    },
                    Text =
                    {
                        Text = kit.messageOnButton,
                        Align = TextAnchor.LowerCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.05 0.1",
                        AnchorMax = "0.95 0.5"
                    }
                }, panelName);
                

                startX += sizeX + config.KitoffsetX;
                i++;

                if (i > 0 && i % kitsOnLine == 0)
                {
                    startX = originalX;
                    startY -= sizeY + config.KitoffsetY;
                }
            }
        }

        private void BuildContentsUI(BasePlayer player, string kitName)
        {
            var kit = API_GetKitForUI(player, kitName);
            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = elemInfo,
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = config.backgroundColor,
                            Sprite = config.backgroundSprite,
                            Material = config.backgroundMaterial
                        },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
                        new CuiNeedsCursorComponent()
                    }
                }
            };

            var numBlock = TimeSpan.FromSeconds(kit.block);
            var strBlock = string.Empty;
            if (numBlock.Days > 0)
            {
                strBlock += $"{numBlock.Days} d ";
            }
            
            if (numBlock.Hours > 0)
            {
                strBlock += $"{numBlock.Hours} h ";
            }
            
            if (numBlock.Minutes > 0)
            {
                strBlock += $"{numBlock.Minutes} m ";
            }
            
            if (numBlock.Seconds > 0)
            {
                strBlock += $"{numBlock.Seconds} s ";
            }
            
            var numCooldown = TimeSpan.FromSeconds(kit.cooldown);
            var strCooldown = string.Empty;
            if (numCooldown.Days > 0)
            {
                strCooldown += $"{numCooldown.Days} d ";
            }
            
            if (numCooldown.Hours > 0)
            {
                strCooldown += $"{numCooldown.Hours} h ";
            }
            
            if (numCooldown.Minutes > 0)
            {
                strCooldown += $"{numCooldown.Minutes} m ";
            }
            
            if (numCooldown.Seconds > 0)
            {
                strCooldown += $"{numCooldown.Seconds} s ";
            }
            
            strBlock = string.IsNullOrEmpty(strBlock) ? GetMessage(Message.None, player.UserIDString) : strBlock;
            strCooldown = string.IsNullOrEmpty(strCooldown) ? GetMessage(Message.None, player.UserIDString) : strCooldown;
            var strUses = kit.uses > 1 ? kit.uses.ToString() : GetMessage(Message.Infinite, player.UserIDString);
            
            var text = GetMessage(Message.KitContents, player.displayName, "{name}", kit.displayName, "{description}", kit.description, "{wipeblock}", strBlock, "{cooldown}", strCooldown, "{uses}", strUses);
            container.Add(new CuiElement
            {
                Parent = elemInfo,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = text
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05 0.7",
                        AnchorMax = "0.95 0.995"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Parent = elemInfo,
                Components =
                {
                    new CuiButtonComponent
                    {
                        Color = "0 0 0 0",
                        Close = elemInfo
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            
            var i = 0;
            var itemsOnLine = (14 * 75) / config.ContentsizeX;
            var sizeX = config.ContentsizeX;
            var sizeY = config.ContentsizeY;
            var originalX = 0;
            var originalY = 0;
            var startX = originalX;
            var startY = originalY;

            foreach (var pair in kit.items)
            {
                var url = $"https://rustlabs.com/img/items180/{pair.Key}.png";
                var amount = pair.Value;
                
                // Icon
                container.Add(new CuiElement
                {
                    Name = elemInfo + pair.Key,
                    Parent = elemInfo,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(url),
                            Color = "1 1 1 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1 0.5",
                            AnchorMax = "0.1 0.5",
                            OffsetMin = $"{startX} {startY - sizeY}",
                            OffsetMax = $"{startX + sizeX} {startY}"
                        }
                    }
                });
                
                // Amount
                container.Add(new CuiElement
                {
                    Parent = elemInfo + pair.Key,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = amount.ToString(),
                            Align = TextAnchor.LowerRight
                        },
                        new CuiOutlineComponent
                        {
                            Color  = config.outlineColor,
                            Distance = config.outlineDistance
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });


                startX += sizeX + config.ContentoffsetX;
                i++;

                if (i > 0 && i % itemsOnLine == 0)
                {
                    startX = originalX;
                    startY -= sizeY + config.ContentoffsetY;
                }
                
            }

            CuiHelper.DestroyUi(player, elemInfo);
            CuiHelper.AddUi(player, container);
        }

        #endregion
        
        #region Configuration | 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Background color")]
            public string backgroundColor = "0.25 0.25 0.25 0.75";
            
            [JsonProperty(PropertyName = "Background material")]
            public string backgroundMaterial = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
            
            [JsonProperty(PropertyName = "Background sprite")]
            public string backgroundSprite = "";
            
            [JsonProperty(PropertyName = "Maximal kits on one page")]
            public int kitsOnPage = 25;
            
            [JsonProperty(PropertyName = "Maximal kits on one line")]
            public int kitsPerLine = 5;
            
            [JsonProperty(PropertyName = "Kit image size X")]
            public int KitsizeX = 200;
            
            [JsonProperty(PropertyName = "Kit image size Y")]
            public int KitsizeY = 100;
            
            [JsonProperty(PropertyName = "Kit image offset X")]
            public int KitoffsetX = 5;
            
            [JsonProperty(PropertyName = "Kit image offset Y")]
            public int KitoffsetY = 5;
            
            [JsonProperty(PropertyName = "Content image size X")]
            public int ContentsizeX = 75;
            
            [JsonProperty(PropertyName = "Content image size Y")]
            public int ContentsizeY = 75;
            
            [JsonProperty(PropertyName = "Content image offset X")]
            public int ContentoffsetX = 2;
            
            [JsonProperty(PropertyName = "Content image offset Y")]
            public int ContentoffsetY = 2;
            
            [JsonProperty(PropertyName = "Kit background color")]
            public string kitBackGroundColor = "0 1 0 0.3";
            
            [JsonProperty(PropertyName = "Outline color")]
            public string outlineColor = "0 0 0 1";
            
            [JsonProperty(PropertyName = "Outline distance")]
            public string outlineDistance = "1.0 -0.5";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private static void ValidateConfig()
        {
            
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
        
        #region Language | 2.0.0
        
        private Dictionary<object, string> langMessages = new Dictionary<object, string>
        {
            {Message.InfoText, "Info"},
            {Message.CloseMark, "x"},
            
            {Message.NextPage, "<size=30> >> </size>"},
            {Message.PreviousPage, "<size=30> << </size>"},
            
            {Message.TextHead, "<size=25>\nFollowing kits available:</size>"},
            {Message.TextBottom, "<size=15><color=#ff0000>Click on free place to exit!\n</color></size>"},
            {Message.None, "None"},
            {Message.Infinite, "Unlimited"},
            {Message.KitContents, "<size=40><color=#00ffff>{name}</color></size>\n<size=20>Description: {description}\nBlock after wipe: {wipeblock}\nCooldown: {cooldown}\nUses left: {uses}</size>"},
        };
        
        private enum Message
        {
            InfoText,
            CloseMark,
            TextHead,
            TextBottom,
            
            NextPage,
            PreviousPage,
            KitContents,
            None,
            Infinite,
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(langMessages.ToDictionary(x => x.Key.ToString(), y => y.Value), this);
        }

        private string GetMessage(Message key, string playerID = null, params object[] args)
        {
            var message = lang.GetMessage(key.ToString(), this, playerID);
            var dic = OrganizeArgs(args);
            if (dic != null)
            {
                foreach (var pair in dic)
                {
                    var s0 = "{" + pair.Key + "}";
                    var s1 = pair.Key;
                    var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                    message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                    message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            return message;
        }

        private void SendMessage(object receiver, string message)
        {
            if (receiver == null)
            {
                Puts(message);
                return;
            }
            
            var console = receiver as ConsoleSystem.Arg;
            if (console != null)
            {
                SendReply(console, message);
                return;
            }
            
            var player = receiver as BasePlayer;
            if (player != null)
            {
                player.ChatMessage(message);
                return;
            }
        }

        private void SendMessage(object receiver, Message key, params object[] args)
        {
            var userID = (receiver as BasePlayer)?.UserIDString;
            var message = GetMessage(key, userID, args);
            SendMessage(receiver, message);
        }

        private static Dictionary<string, object> OrganizeArgs(object[] args)
        {
            var dic = new Dictionary<string, object>();
            for (var i = 0; i < args.Length; i += 2)
            {
                var value = args[i].ToString();
                var nextValue = i + 1 < args.Length ? args[i + 1] : null;
                dic.Add(value, nextValue);
            }

            return dic;
        }

        #endregion

        #region Image Library Support

        [PluginReference] private Plugin ImageLibrary;

        private string GetImage(string url)
        {
            return ImageLibrary?.Call<string>("GetImage", url);
        }

        #endregion
        
        #region Kit UI Class 1.2.0

        private class KitUI
        {
            public string name = string.Empty;
            public string displayName = string.Empty;
            public int cooldown = 3600;
            public int block = 0;
            public string url = string.Empty;
            public string description = string.Empty;
            public int uses = 0;
            public string messageOnButton = string.Empty;
            public Dictionary<string, int> items = new Dictionary<string, int>();
        }

        #endregion

        #region API

        private static KitUI[] API_GetPlayerUIKits(BasePlayer player)
        {
            var obj = Interface.CallHook(nameof(API_GetPlayerUIKits), player);
            return obj == null ? new KitUI[] { } : JsonConvert.DeserializeObject<KitUI[]>(obj.ToString());
        }

        private static KitUI API_GetKitForUI(BasePlayer player, string kitName)
        {
            var obj = Interface.CallHook(nameof(API_GetKitForUI), player, kitName);
            return obj == null ? new KitUI(): JsonConvert.DeserializeObject<KitUI>(obj.ToString());
        }

        #endregion
    }
}