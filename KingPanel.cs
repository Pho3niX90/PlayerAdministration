//Requires: ImageLibrary

using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("KingPanel", "Pho3niX90", "0.0.1")]
    [Description("InfoPanel replacement")]
    public class KingPanel : RustPlugin
    {
        [PluginReference]
        ImageLibrary ImageLibrary;
        Dictionary<string, string> openPanels;
        ulong devSteamID = 76561198007433923L;
        string imageURL = "https://i.imgur.com/n9EYIWi.png";

        void Init() {
            AddImage(imageURL);
            openPanels = new Dictionary<string, string>();
        }

        void Loaded() {
            DestroyUIAll(true);
            CreateUIAll();
        }

        void Unload() {
            DestroyUIAll();
        }

        void OnPlayerConnected(IPlayer player) {
            int failCount = 1;
            try {
                timer.Once(0.2f, () => UpdateUIAll());
            } catch (Exception e) {
                timer.Once(failCount / 10,
                () => OnPlayerConnected(player));
                failCount++;
            }
        }

        void OnPlayerDisconnected(IPlayer player, string reason) {
            timer.Once(0.1f, () => UpdateUIAll());
            DestroyUI(player);
        }

        #region UI
        private string GetImage(string url) {
            return ImageLibrary.GetImage(url, 1L);
        }

        private bool AddImage(string url) {//(string url, string imageName, ulong imageId, Action callback = null)
            if (ImageLibrary == null || !ImageLibrary.IsLoaded) {
                NextTick(() => AddImage(url));
                Puts("Image queued");
                return false;
            } else {
                Puts("Image added");
                return ImageLibrary.AddImage(url, url, 1L);
            }
        }

        string infoPanelElement;
        string infoPanel = "mainInfoPanel";
        void UpdateUIAll() {
            List<IPlayer> players = covalence.Players.Connected.ToList();
            for (int i = 0; i < players.Count; i++) {
                UpdateUI(players[i]);
            }
        }

        void CreateUIAll() {
            List<IPlayer> players = covalence.Players.Connected.ToList();
            for (int i = 0; i < players.Count; i++) {
                CreateUI(players[i]);
            }
        }

        void DestroyUIAll(bool force = false) {
            List<IPlayer> players = covalence.Players.Connected.ToList();
            for (int i = 0; i < players.Count; i++) {
                DestroyUI(players[i], force);
            }
        }

        void UpdateUI(IPlayer player) {
            if (player != null && !player.IsConnected) return;
            DestroyUI(player);
            CreateUI(player);
        }

        void CreateUI(IPlayer player) {
            CuiElementContainer containerMain = new CuiElementContainer();
            infoPanelElement = containerMain.Add(new CuiPanel {
                Image =
                {
                    Color = "0 0 0 0.5"
                },
                RectTransform =
                {
                    AnchorMin = "0.005 0.965",
                    AnchorMax = "0.175 0.995" //left digit is viewheight from left, right digit is Y axis, viewheight from bottom
                },
                CursorEnabled = false,
            }, "Hud", infoPanel);

            containerMain.Add(new CuiElement {
                Name = "Players",
                Parent = infoPanel,
                Components = {
             new CuiRawImageComponent {
                Png = GetImage(imageURL)
            }
           ,
         new CuiRectTransformComponent
         {
            AnchorMin = "0.025 0.005",
            AnchorMax = "0.120 0.995"}
         }
            });

            containerMain.Add(new CuiLabel {
                Text =
                    {
                        Text = $"{covalence.Players.Connected.Count()}/{ConVar.Server.maxplayers}",
                        FontSize = 12,
                        Align = TextAnchor.MiddleLeft
                    },
                RectTransform =
                    {
                        AnchorMin = $"0.150 0.005",
                        AnchorMax = $"0.370 0.995"
                    }
            }, infoPanelElement);

            containerMain.Add(new CuiLabel {
                Text =
                    {
                        Text = $"KingdomRust.com",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter
                    },
                RectTransform =
                    {
                        AnchorMin = $"0.400 0.005",
                        AnchorMax = $"0.985 0.995"
                    }
            }, infoPanelElement);

            CuiHelper.AddUi(BasePlayer.FindByID(ulong.Parse(player.Id)), containerMain);
            openPanels.Add(player.Id, infoPanel);
        }

        void DestroyUI(IPlayer player, bool force = false) {
            if (openPanels.ContainsKey(player.Id) || force) {
                try {
                    CuiHelper.DestroyUi(BasePlayer.FindByID(ulong.Parse(player.Id)), infoPanel);
                } catch (Exception e) {
                }
                openPanels.Remove(player.Id);
            }
        }
        #endregion

        #region Commands
        [ChatCommand("players")]
        void PlayersCommand(IPlayer player, string command, string[] arguments) {
            StringBuilder builder = new StringBuilder();
            int playerCount = covalence.Players.Connected.Count();

            builder.Append(string.Format("Online players ({0}): ", playerCount));
            List<string> players = new List<string>();

            foreach (IPlayer pl in covalence.Players.Connected) {
                players.Add("<color=#ff0000ff>" + pl.Name + "</color>");
            }
            builder.Append(String.Join(", ", players));

            SendReply(BasePlayer.FindByID(ulong.Parse(player.Id)), builder.ToString());
        }
        #endregion
    }
}