using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System.Globalization;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Fuel Gauge", "Oryx", "0.5.0")]
    [Description("HUD for amount of fuel when riding a vehicle.")]

    public class FuelGauge : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin ImageLibrary;

        private string perm = "fuelgauge.allow";
        private List<VehicleCache> vehicles = new List<VehicleCache>();

        ConfigData configData;

        private bool useIcon;
        private string imageURL;
        private string dock;
        private bool onlyDriver;
        private string backgroundColor;
        private float backgroundTransparency;
        private string gaugeType = "bar";

        public class VehicleCache
        {
            public BaseMountable entity;
            public BasePlayer player;

            public int GetFuelAmount()
            {
                if (entity.GetParentEntity() as MotorRowboat)
                {
                    return (entity.GetParentEntity() as MotorRowboat).GetFuelAmount();
                }
                else if (entity.GetParentEntity() as MiniCopter)
                {
                    return (entity.GetParentEntity() as MiniCopter).GetFuelAmount();
                }
                else
                {
                    return 0;
                }
            }
        }
        #endregion

        #region Config
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                settings = new Settings
                {
                    Dock = "Left",
                    DriverOnly = true
                },
                display = new Display
                {
                    ImageURL = "https://i.imgur.com/n9Vp4yz.png",
                    BackgroundColor = "#9b9696",
                    Transparency = 0.3f,
                    UseIcon = true

                }
            };

            Config.WriteObject(config, true);
            configData = Config.ReadObject<ConfigData>();
            LoadVariables();
        }

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Settings")]
            public Settings settings { get; set; }
            [JsonProperty(PropertyName = "Display")]
            public Display display { get; set; }
        }

        private class Settings
        {
            [JsonProperty(PropertyName = "Dock")]
            public string Dock { get; set; }
            [JsonProperty(PropertyName = "Only Display to Driver")]
            public bool DriverOnly { get; set; }
        }

        private class Display
        {
            [JsonProperty(PropertyName = "Use Icon")]
            public bool UseIcon { get; set; }
            [JsonProperty(PropertyName = "Image URL")]
            public string ImageURL { get; set; }
            [JsonProperty(PropertyName = "Background Color")]
            public string BackgroundColor { get; set; }
            [JsonProperty(PropertyName = "Background Transparency")]
            public float Transparency { get; set; }
        }

        private void LoadVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            imageURL = configData.display.ImageURL;
            backgroundColor = configData.display.BackgroundColor;
            backgroundTransparency = configData.display.Transparency;
            useIcon = configData.display.UseIcon;

            dock = configData.settings.Dock;
            onlyDriver = configData.settings.DriverOnly;
        }
        #endregion

        #region Hooks
        void Init()
        {
            permission.RegisterPermission(perm, this);
            LoadVariables();
        }

        void Loaded()
        {
            if (ImageLibrary != null)
            {
                ImageLibrary.Call("AddImage", imageURL, imageURL, 0UL);
            }
            else
            {
                PrintWarning("IMAGE LIBRARY IS NOT INSTALLED.");
            }
        }

        void OnServerInitialized()
        {
            UIManager();
        }

        private void Unload()
        {
            DestoryAllUI();
            vehicles.Clear();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (GetPlayer(player))
            {
                RemoveVehicleByPlayer(player);
            }
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (GetPlayer(player))
            {
                RemoveVehicleByPlayer(player);
            }
            return null;
        }

        void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            if (entity == null | player == null) { return; }

            if (!(entity.GetParentEntity() as MotorRowboat || entity.GetParentEntity() as MiniCopter)) { return; }

            if (!permission.UserHasPermission(player.UserIDString, perm)) { return; }

            if (GetPlayer(player))
            {
                RemoveVehicleByPlayer(player);
            }

            if (onlyDriver)
            {
                if (!(entity.ShortPrefabName == "miniheliseat" | entity.ShortPrefabName == "standingdriver" | entity.ShortPrefabName == "smallboatdriver"))
                {
                    return;
                }
            }

            VehicleCache vehicle = new VehicleCache();

            vehicle.player = player;
            vehicle.entity = entity;
            vehicles.Add(vehicle);

            CreateUI(vehicle);
        }

        void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            if (GetPlayer(player))
            {
                RemoveVehicleByPlayer(player);
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity.GetParentEntity() as MotorRowboat || entity.GetParentEntity() as MiniCopter)
            {
                foreach (VehicleCache vehicle in vehicles)
                {
                    if (vehicle.entity == entity)
                    {
                        RemoveVehicleByPlayer(vehicle.player);
                        return;
                    }
                }
            }
        }
        #endregion

        #region UIHelper
        static class UIHelper
        {
            public static CuiElementContainer NewCuiElement(string name, string color, string aMin, string aMax)
            {
                var element = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = false
                        },
                        new CuiElement().Parent = "Overlay",
                        name
                    }
                };
                return element;
            }

            public static void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel, CuiHelper.GetGuid());
            }

            public static void CreateLabel(ref CuiElementContainer container, string panel, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, string color = null)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());

            }

            public static void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            public static string HexToRGBA(string hex, float alpha)
            {
                if (hex.StartsWith("#"))
                {
                    hex = hex.TrimStart('#');
                }

                int red = int.Parse(hex.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hex.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hex.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion

        #region Methods
        public void CreateUI(VehicleCache vehicle)
        {
            var element = UIHelper.NewCuiElement("SHOWFUEL_UI", UIHelper.HexToRGBA(backgroundColor, backgroundTransparency), GetMinDock(), GetMaxDock());

            if (ImageLibrary == null || !useIcon)
            {
                UIHelper.CreatePanel(ref element, "SHOWFUEL_UI", UIHelper.HexToRGBA(backgroundColor, backgroundTransparency), "0.0 0.0", "1.0 1.0");
                UIHelper.CreateLabel(ref element, "SHOWFUEL_UI", "x" + vehicle.GetFuelAmount(), 14, "0.1 0.1", "0.9 0.9");
                CuiHelper.AddUi(vehicle.player, element);
            }
            else
            {
                string icon = GetImage(imageURL);
                UIHelper.CreatePanel(ref element, "SHOWFUEL_UI", UIHelper.HexToRGBA(backgroundColor, backgroundTransparency), "0.0 0.0", "1.0 1.0");
                UIHelper.LoadImage(ref element, "SHOWFUEL_UI", icon, "0.1 0.2", "0.7 0.8");
                UIHelper.CreateLabel(ref element, "SHOWFUEL_UI", "x" + vehicle.GetFuelAmount(), 11, "0.1 0.1", "0.9 0.4", TextAnchor.MiddleRight);
                CuiHelper.AddUi(vehicle.player, element);
            }
        }

        public void UpdateUI(VehicleCache vehicle)
        {
            CuiHelper.DestroyUi(vehicle.player, "SHOWFUEL_UI");
            CreateUI(vehicle);
        }

        public void UIManager()
        {
            timer.Every(3f, () =>
            {
                if (vehicles.Count != 0)
                {
                    foreach (VehicleCache vehicle in vehicles)
                    {
                        if (GetPlayer(vehicle.player))
                        {
                            UpdateUI(vehicle);
                        }
                    }
                }
            });
        }

        public void DestoryUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SHOWFUEL_UI");
        }

        public void DestoryAllUI()
        {
            foreach (VehicleCache vehicle in vehicles)
            {
                DestoryUI(vehicle.player);
            }
        }

        public void RemoveVehicleByPlayer(BasePlayer player)
        {
            if (player == null) { return; }

            foreach (VehicleCache v in vehicles)
            {
                if (v.player.userID == player.userID)
                {
                    DestoryUI(player);
                    vehicles.Remove(v);
                    return;
                }
            }
        }

        public bool GetPlayer(BasePlayer player)
        {
            if (player == null) { return false; }

            foreach (VehicleCache vehicle in vehicles)
            {
                if (vehicle.player.userID == player.userID)
                {
                    return true;
                }
            }
            return false;
        }

        private string GetImage(string fileName, ulong skin = 0)
        {
            string id = ImageLibrary.Call<string>("GetImage", fileName, skin);

            if (id == null)
            {
                return string.Empty;
            }
            return id;
        }

        public string GetMinDock()
        {
            if (dock == "Right")
            {
                return "0.65 0.025";
            }
            else if (dock == "Left")
            {
                return "0.30 0.025";
            }

            return "0.65 0.025";
        }

        public string GetMaxDock()
        {
            if (dock == "Right")
            {
                return "0.7 0.085";
            }
            else if (dock == "Left")
            {
                return "0.34 0.082";
            }

            return "0.7 0.085";
        }
        #endregion
    }
}
