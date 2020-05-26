using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Globalization;
using Rust;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins {
    [Info("Combat Mini", "Pho3niX90", "0.0.1")]
    [Description("Turns minicopters into combat minis")]
    class CombatMini : RustPlugin {
        private int rocketId;
        private int mgId;
        const string prefabRocketLauncher = "assets/prefabs/weapons/rocketlauncher/rocket_launcher.entity.prefab";
        static CombatMini _instance;

        #region Hooks
        private void OnServerInitialized() {
            _instance = this;
            rocketId = ItemManager.itemList.Find(x => x.shortname == configData.Weapons.Cannon.Type)?.itemid ?? 0;
            mgId = ItemManager.itemList.Find(x => x.shortname == configData.Weapons.MG.Type)?.itemid ?? 0;
        }
        void OnEntitySpawned(MiniCopter mini) {
            if (mini.name.Contains("trans")) return;
            mini.gameObject.AddComponent<MiniWeapons>();
            
        }
        #endregion

        #region Weapon Method

        #endregion

        #region Classes
        class MiniWeapons: MonoBehaviour {
            public MiniCopter entity;
            public ItemContainer inventory;

            private float lastRocketTime;

            void Awake() {
                entity = GetComponent<MiniCopter>();
            }

            BasePlayer GetPilot() => entity.GetDriver();

            private void Update() {
                if (GetPilot() != null) {
                    DoWeaponControls();
                    entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }
            }

            private void DoWeaponControls() {
                //if (Commander.serverInput.WasJustPressed(controlButtons[CommandType.Lights]))
                //    ToggleLights();
                //
                //if (Commander.serverInput.WasJustPressed(controlButtons[CommandType.Cannon]))
                //    FireCannon();

                if (GetPilot().serverInput.IsDown(BUTTON.FIRE_PRIMARY) || GetPilot().serverInput.WasJustPressed(BUTTON.FIRE_PRIMARY))
                    FireRocket();

                //if (Commander.serverInput.IsDown(controlButtons[CommandType.Coax]) || Commander.serverInput.WasJustPressed(controlButtons[CommandType.Coax]))
                //    FireCoax();
            }
            public Vector3 strafeTargetPosition;
            public void FireRocket() {
                RaycastHit raycastHit;
                string str;
                this.lastRocketTime = Time.realtimeSinceStartup;
                float single = 4f;
                Transform transforms = this.entity.frontWheel.transform;
                Vector3 vector3 = transforms.position + (transforms.forward * 1f);

                if (single > 0f) {
                    strafeTargetPosition = AimConeUtil.GetModifiedAimConeDirection(single, entity.frontWheel.transform.rotation * Vector3.forward, true);
                }
                if (Physics.Raycast(vector3, strafeTargetPosition, out raycastHit, 1f, 1236478737)) {
                }
                //Effect.server.Run(this.helicopterBase.rocket_fire_effect.resourcePath, this.helicopterBase, StringPool.Get((flag ? "rocket_tube_left" : "rocket_tube_right")), Vector3.zero, Vector3.forward, null, true);
                GameManager gameManager = GameManager.server;
                str = "assets/prefabs/npc/patrol helicopter/rocket_heli_airburst.prefab";
                Quaternion quaternion = new Quaternion();
                BaseEntity baseEntity = gameManager.CreateEntity(str, vector3, quaternion, true);
                if (baseEntity == null) {
                    return;
                }
                ServerProjectile component = baseEntity.GetComponent<ServerProjectile>();
                if (component) {
                    component.InitializeVelocity(strafeTargetPosition * component.speed);
                }
                baseEntity.Spawn();
            }
        }
        #endregion

        #region Config
        private static ConfigData configData;

        private class ConfigData {
            [JsonProperty(PropertyName = "Movement Settings")]
            public MovementSettings Movement { get; set; }

            [JsonProperty(PropertyName = "Button Configuration")]
            public ButtonConfiguration Buttons { get; set; }

            [JsonProperty(PropertyName = "Crushable Types")]
            public CrushableTypes Crushables { get; set; }

            [JsonProperty(PropertyName = "Passenger Options")]
            public PassengerOptions Passengers { get; set; }

            [JsonProperty(PropertyName = "Inventory Options")]
            public InventoryOptions Inventory { get; set; }

            [JsonProperty(PropertyName = "Weapon Options")]
            public WeaponOptions Weapons { get; set; }

            public class CrushableTypes {
                [JsonProperty(PropertyName = "Can crush buildings")]
                public bool Buildings { get; set; }

                [JsonProperty(PropertyName = "Can crush resources")]
                public bool Resources { get; set; }

                [JsonProperty(PropertyName = "Can crush loot containers")]
                public bool Loot { get; set; }

                [JsonProperty(PropertyName = "Can crush animals")]
                public bool Animals { get; set; }

                [JsonProperty(PropertyName = "Can crush players")]
                public bool Players { get; set; }

                [JsonProperty(PropertyName = "Amount of force required to crush various building grades")]
                public Dictionary<string, float> GradeForce { get; set; }

                [JsonProperty(PropertyName = "Amount of force required to crush external walls")]
                public float WallForce { get; set; }

                [JsonProperty(PropertyName = "Amount of force required to crush resources")]
                public float ResourceForce { get; set; }
            }

            public class ButtonConfiguration {
                [JsonProperty(PropertyName = "Enter/Exit vehicle")]
                public string Enter { get; set; }

                [JsonProperty(PropertyName = "Toggle light")]
                public string Lights { get; set; }

                [JsonProperty(PropertyName = "Open inventory")]
                public string Inventory { get; set; }

                [JsonProperty(PropertyName = "Speed boost")]
                public string Boost { get; set; }

                [JsonProperty(PropertyName = "Fire Cannon")]
                public string Cannon { get; set; }

                [JsonProperty(PropertyName = "Fire Coaxial Gun")]
                public string Coax { get; set; }

                [JsonProperty(PropertyName = "Fire MG")]
                public string MG { get; set; }
            }

            public class MovementSettings {
                [JsonProperty(PropertyName = "Forward torque (nm)")]
                public float ForwardTorque { get; set; }

                [JsonProperty(PropertyName = "Rotation torque (nm)")]
                public float TurnTorque { get; set; }

                [JsonProperty(PropertyName = "Brake torque (nm)")]
                public float BrakeTorque { get; set; }

                [JsonProperty(PropertyName = "Time to reach maximum acceleration (seconds)")]
                public float Acceleration { get; set; }

                [JsonProperty(PropertyName = "Boost torque (nm)")]
                public float BoostTorque { get; set; }
            }
            public class PassengerOptions {
                [JsonProperty(PropertyName = "Allow passengers")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Number of allowed passengers (Max 4)")]
                public int Max { get; set; }

                [JsonProperty(PropertyName = "Require passenger to be a friend (FriendsAPI)")]
                public bool UseFriends { get; set; }

                [JsonProperty(PropertyName = "Require passenger to be a clan mate (Clans)")]
                public bool UseClans { get; set; }
            }

            public class InventoryOptions {
                [JsonProperty(PropertyName = "Enable inventory system")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Drop inventory on death")]
                public bool DropInv { get; set; }

                [JsonProperty(PropertyName = "Drop loot on death")]
                public bool DropLoot { get; set; }

                [JsonProperty(PropertyName = "Inventory size (max 36)")]
                public int Size { get; set; }
            }

            public class WeaponOptions {
                [JsonProperty(PropertyName = "Cannon")]
                public WeaponSystem Cannon { get; set; }

                [JsonProperty(PropertyName = "Coaxial")]
                public WeaponSystem Coax { get; set; }

                [JsonProperty(PropertyName = "Machine Gun")]
                public WeaponSystem MG { get; set; }

                [JsonProperty(PropertyName = "Enable Crosshair")]
                public bool EnableCrosshair { get; set; }

                [JsonProperty(PropertyName = "Crosshair Color")]
                public SerializedColor CrosshairColor { get; set; }

                [JsonProperty(PropertyName = "Crosshair Size")]
                public int CrosshairSize { get; set; }

                public class SerializedColor {
                    public float R { get; set; }
                    public float G { get; set; }
                    public float B { get; set; }
                    public float A { get; set; }

                    private Color _color;
                    private bool _isInit;

                    public SerializedColor(float r, float g, float b, float a) {
                        R = r;
                        G = g;
                        B = b;
                        A = a;
                    }

                    [JsonIgnore]
                    public Color Color {
                        get {
                            if (!_isInit) {
                                _color = new Color(R, G, B, A);
                                _isInit = true;
                            }
                            return _color;
                        }
                    }
                }

                public class WeaponSystem {
                    [JsonProperty(PropertyName = "Enable weapon system")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Require ammunition in inventory")]
                    public bool RequireAmmo { get; set; }

                    [JsonProperty(PropertyName = "Ammunition type (item shortname)")]
                    public string Type { get; set; }

                    [JsonProperty(PropertyName = "Fire rate (seconds)")]
                    public float Interval { get; set; }

                    [JsonProperty(PropertyName = "Aim cone (smaller number is more accurate)")]
                    public float Accuracy { get; set; }

                    [JsonProperty(PropertyName = "Damage")]
                    public float Damage { get; set; }
                }
            }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig() {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig() {
            return new ConfigData {
                Buttons = new ConfigData.ButtonConfiguration {
                    Enter = "USE",
                    Lights = "RELOAD",
                    Inventory = "RELOAD",
                    Boost = "SPRINT",
                    Cannon = "FIRE_PRIMARY",
                    Coax = "FIRE_SECONDARY",
                    MG = "FIRE_THIRD"
                },
                Crushables = new ConfigData.CrushableTypes {
                    Animals = true,
                    Buildings = true,
                    Loot = true,
                    Players = true,
                    Resources = true,
                    GradeForce = new Dictionary<string, float> {
                        [BuildingGrade.Enum.Twigs.ToString()] = 1000f,
                        [BuildingGrade.Enum.Wood.ToString()] = 2000f,
                        [BuildingGrade.Enum.Stone.ToString()] = 3000f,
                        [BuildingGrade.Enum.Metal.ToString()] = 5000f,
                        [BuildingGrade.Enum.TopTier.ToString()] = 7000f,
                    },
                    ResourceForce = 1500f,
                    WallForce = 3000f
                },
                Movement = new ConfigData.MovementSettings {
                    Acceleration = 3f,
                    BrakeTorque = 50f,
                    ForwardTorque = 1500f,
                    TurnTorque = 1800f,
                    BoostTorque = 300f
                },
                Passengers = new ConfigData.PassengerOptions {
                    Enabled = true,
                    Max = 4,
                    UseClans = true,
                    UseFriends = true
                },
                Inventory = new ConfigData.InventoryOptions {
                    Enabled = true,
                    Size = 36,
                    DropInv = true,
                    DropLoot = false
                },
                Weapons = new ConfigData.WeaponOptions {
                    EnableCrosshair = true,
                    CrosshairColor = new ConfigData.WeaponOptions.SerializedColor(0.75f, 0.75f, 0.75f, 0.75f),
                    CrosshairSize = 40,
                    Cannon = new ConfigData.WeaponOptions.WeaponSystem {
                        Accuracy = 0.025f,
                        Damage = 90f,
                        Enabled = true,
                        Interval = 1.75f,
                        RequireAmmo = false,
                        Type = "ammo.rocket.hv"
                    },
                    Coax = new ConfigData.WeaponOptions.WeaponSystem {
                        Accuracy = 0.75f,
                        Damage = 10f,
                        Enabled = true,
                        Interval = 0.06667f,
                        RequireAmmo = false,
                        Type = "ammo.rifle.hv"
                    },
                    MG = new ConfigData.WeaponOptions.WeaponSystem {
                        Accuracy = 1.25f,
                        Damage = 10f,
                        Enabled = true,
                        Interval = 0.1f,
                        RequireAmmo = false,
                        Type = "ammo.rifle.hv"
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues() {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 2, 0))
                configData = baseConfig;

            if (configData.Version < new VersionNumber(0, 2, 2)) {
                configData.Weapons.EnableCrosshair = true;
                configData.Weapons.CrosshairColor = new ConfigData.WeaponOptions.SerializedColor(0.75f, 0.75f, 0.75f, 0.75f);
                configData.Weapons.CrosshairSize = 40;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion
    }
}
