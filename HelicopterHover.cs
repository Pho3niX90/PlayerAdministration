using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Helicopter Hover", "0x89A", "1.1.8")]
    [Description("Allows minicopters to hover without driver on command")]

    public class HelicopterHover : RustPlugin
    {
        #region -Fields-
        const string canHover = "helicopterhover.enable";

        Dictionary<int, bool> helicopterHovering = new Dictionary<int, bool>();
        Dictionary<int, float> helicopterFuelUseTime = new Dictionary<int, float>();
        Dictionary<int, Timer> helicopterTimers = new Dictionary<int, Timer>();

        #endregion

        #region -Init-

        void Init()
        {
            if (!_config.enterBroadcast)
            {
                Unsubscribe("OnEntityMounted");
            }

            permission.RegisterPermission(canHover, this);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["OnlyAdmins"] = "Command exclusive to admins",
                ["NotFlying"] = "The helicopter is not flying",
                ["Mounted"] = "Use '/hover' to toggle hover",
                ["ScrapHeliCantHover"] = "You cannot hover in a scrap transport helicopter",
                ["NoPermission"] = "You do not have permission to hover",
                ["CantHoverTwoOccupants"] = "Cannot hover with two occupants",
                ["HelicopterEnabled"] = "Helicopter hover: enabled",
                ["HelicopterDisabled"] = "Helicopter hover: disabled",
                ["NotMounted"] = "You are not mounted",
                ["NotInHelicopter"] = "You are not in a helicopter",
                ["HelicopterNull"] = "Could not get helicopter, is null",
                ["RigidbodyNull"] = "Rigidbody of helicopter is null",
                ["ErrorFound"] = "Error found, please retry"
            }
            , this);
        }

        #endregion

        #region -Configuration-

        private Configuration _config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Broadcast message on mounted")]
            public bool enterBroadcast = true;

            [JsonProperty(PropertyName = "Permissions")]
            public PermissionClass Permission = new PermissionClass();

            [JsonProperty(PropertyName = "Hovering")]
            public HoveringClass Hovering = new HoveringClass();

            public class PermissionClass
            {
                [JsonProperty(PropertyName = "Scraptranporthelicopter can hover")]
                public bool scrapheliCanHover = true;

                [JsonProperty(PropertyName = "Chinook can hover")]
                public bool chinookCanHover = true;

                [JsonProperty(PropertyName = "Enable hover with two occupants")]
                public bool enableHoverWithTwoOccupants = true;

                [JsonProperty(PropertyName = "Passenger can toggle hover")]
                public bool passengerToggle = true;
            }

            public class HoveringClass
            {
                [JsonProperty(PropertyName = "Timed hover")]
                public bool timedHover = false;

                [JsonProperty(PropertyName = "Timed hover duration")]
                public float hoverDuration = 60;

                [JsonProperty(PropertyName = "Use fuel while hovering")]
                public bool useFuelOnHover = true;

                [JsonProperty(PropertyName = "Keep engine on when hovering")]
                public bool keepEngineOnHover = true;

                [JsonProperty(PropertyName = "Enable helicopter rotation on hover")]
                public bool enableRotationOnHover = true;

                [JsonProperty(PropertyName = "Disable hover on dismount")]
                public bool disableHoverOnDismount = true;

                [JsonProperty(PropertyName = "Disable hover on change seats")]
                public bool disableHoverOnSeat = false;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new System.Exception();
                SaveConfig();
            }
            catch
            {
                PrintWarning("Error with config, using default values");

                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject<Configuration>(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region -Commands-

        [ConsoleCommand("helicopterhover.hover")]
        void ConsoleCommand(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
            {
                Hover(args.Player());
            }
        }

        [ChatCommand("hover")]
        void Hover(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, canHover) && player.GetMountedVehicle() != null)
            {
                BaseVehicle playerVehicle = player.GetMountedVehicle();

                if (_config.Permission.passengerToggle || !_config.Permission.passengerToggle && playerVehicle.GetDriver() == player)
                    if (player.isMounted && playerVehicle.ShortPrefabName == "minicopter.entity"
                       || playerVehicle.ShortPrefabName == "scraptransporthelicopter" && _config.Permission.scrapheliCanHover
                       || playerVehicle.ShortPrefabName == "ch47.entity" && _config.Permission.chinookCanHover)
                    {
                        MiniCopter minicopter = playerVehicle as MiniCopter;

                        //Chinook
                        if (minicopter == null && playerVehicle.ShortPrefabName == "ch47.entity")
                        {
                            CH47Helicopter chinook = playerVehicle as CH47Helicopter;

                            if (chinook != null)
                            {
                                if (_config.Permission.enableHoverWithTwoOccupants)
                                {
                                    ChinookHover(player, chinook);
                                    return;
                                }
                                else
                                {
                                    if (chinook.NumMounted() >= 2)
                                    {
                                        PrintToChat(player, lang.GetMessage("CantHoverTwoOccupants", this, player.UserIDString));

                                        return;
                                    }
                                }
                            }
                        }

                        if (minicopter != null)
                        {
                            if (minicopter.IsEngineOn() && minicopter.isMobile || minicopter.isMobile && minicopter.GetDriver() != player)
                            {
                                if (_config.Permission.enableHoverWithTwoOccupants)
                                {
                                    ToggleHover(player, minicopter);
                                }
                                else
                                {
                                    if (minicopter.NumMounted() >= 2)
                                    {
                                        PrintToChat(player, lang.GetMessage("CantHoverTwoOccupants", this, player.UserIDString));

                                        return;
                                    }
                                }
                            }
                            else
                            {
                                PrintToChat(player, lang.GetMessage("NotFlying", this, player.UserIDString));
                            }
                        }
                        else
                        {
                            PrintWarning(lang.GetMessage("HelicopterNull", this, player.UserIDString));

                            PrintToChat(player, lang.GetMessage("ErrorFound", this, player.UserIDString));

                            return;
                        }
                    }
                    else
                    {
                        if (!player.isMounted)
                        {
                            PrintToChat(lang.GetMessage("NotMounted", this, player.UserIDString));
                        }

                        if (playerVehicle is MiniCopter == false)
                        {
                            PrintToChat(player, lang.GetMessage("NotInHelicopter", this, player.UserIDString));
                        }
                    }
            }
            else if (player.GetMountedVehicle() == null) PrintToChat(player, lang.GetMessage("NotInHelicopter", this, player.UserIDString));
            else PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
        }

        #endregion

        #region -Hover-

        void ToggleHover(BasePlayer player, MiniCopter minicopter)
        {
            if (!helicopterHovering.ContainsKey(minicopter.GetInstanceID())) helicopterHovering.Add(minicopter.GetInstanceID(), false);

            if (!helicopterFuelUseTime.ContainsKey(minicopter.GetInstanceID())) helicopterFuelUseTime.Add(minicopter.GetInstanceID(), 0f);

            if (!helicopterTimers.ContainsKey(minicopter.GetInstanceID())) helicopterTimers.Add(minicopter.GetInstanceID(), null);

            Rigidbody rb = minicopter.GetComponent<Rigidbody>();

            if (rb == null)
            {
                PrintWarning(lang.GetMessage("RigidbodyNull", this, null));

                PrintToChat(player, lang.GetMessage("ErrorFound", this, player.UserIDString));

                return;
            }

            if (!helicopterHovering[minicopter.GetInstanceID()])
            {
                helicopterHovering[minicopter.GetInstanceID()] = true;

                rb.constraints = RigidbodyConstraints.FreezePositionY;

                if (!_config.Hovering.enableRotationOnHover)
                {
                    rb.freezeRotation = true;
                }

                PrintToChat(player, lang.GetMessage("HelicopterEnabled", this, player.UserIDString));

                if (_config.Hovering.keepEngineOnHover)
                {
                    MonoBehaviour monoBehaviour = minicopter.GetComponent<MonoBehaviour>();

                    monoBehaviour.StartCoroutine(WhileHovering(minicopter, rb));
                }
            }
            else
            {
                helicopterHovering[minicopter.GetInstanceID()] = false;

                rb.constraints = RigidbodyConstraints.None;

                rb.freezeRotation = false;

                PrintToChat(player, lang.GetMessage("HelicopterDisabled", this, player.UserIDString));
            }
        }

        void ChinookHover(BasePlayer player, CH47Helicopter chinook)
        {
            if (!helicopterHovering.ContainsKey(chinook.GetInstanceID()))
            {
                helicopterHovering.Add(chinook.GetInstanceID(), false);
            }

            Rigidbody rb = chinook.GetComponent<Rigidbody>();

            if (rb == null)
            {
                PrintWarning(lang.GetMessage("RigidbodyNull", this, null));

                PrintToChat(player, lang.GetMessage("ErrorFound", this, player.UserIDString));

                return;
            }

            if (!helicopterHovering[chinook.GetInstanceID()])
            {
                helicopterHovering[chinook.GetInstanceID()] = true;

                rb.constraints = RigidbodyConstraints.FreezePositionY;

                if (!_config.Hovering.enableRotationOnHover)
                {
                    rb.freezeRotation = true;
                }

                PrintToChat(player, lang.GetMessage("HelicopterEnabled", this, player.UserIDString));
            }
            else
            {
                helicopterHovering[chinook.GetInstanceID()] = false;

                rb.constraints = RigidbodyConstraints.None;

                rb.freezeRotation = false;

                PrintToChat(player, lang.GetMessage("HelicopterDisabled", this, player.UserIDString));
            }
        }

        //Keeps engine running after changing seats
        IEnumerator WhileHovering(MiniCopter minicopter, Rigidbody rb)
        {
            if (_config.Hovering.timedHover)
            {
                string position = minicopter.transform.position.ToString();

                Timer hovertime = timer.Once(_config.Hovering.hoverDuration, () =>
                {
                    if (minicopter != null)
                    {
                        helicopterHovering[minicopter.GetInstanceID()] = false;

                        minicopter.EngineOff();
                        rb.constraints = RigidbodyConstraints.None;
                        minicopter.EngineOn();
                        minicopter.StopAllCoroutines();
                    }
                    else PrintError(lang.GetMessage("ErrorFound", this) + $"at {position}");
                });
            }

            while (helicopterHovering[minicopter.GetInstanceID()])
            {
                if (!minicopter.IsEngineOn() && minicopter.HasAnyPassengers() && minicopter.HasFuel() || !_config.Hovering.disableHoverOnDismount && !minicopter.HasAnyPassengers() && !minicopter.IsEngineOn() && minicopter.HasFuel())
                {
                    minicopter.EngineStartup();
                }

                if (minicopter.HasDriver() && minicopter.HasFuel())
                {
                    minicopter.EngineOn();
                }

                if (_config.Hovering.useFuelOnHover && minicopter.HasFuel())
                {
                    if (Time.time - helicopterFuelUseTime[minicopter.GetInstanceID()] > 1)
                    {
                        minicopter.UseFuel(minicopter.fuelPerSec);

                        helicopterFuelUseTime[minicopter.GetInstanceID()] = Time.time;
                    }
                }

                if ((!minicopter.HasFuel() && !_config.Hovering.disableHoverOnDismount) || (_config.Hovering.disableHoverOnSeat && minicopter.HasAnyPassengers() && minicopter.HasDriver() == false))
                {
                    minicopter.EngineOff();
                    rb.constraints = RigidbodyConstraints.None;

                    minicopter.StopAllCoroutines();
                }

                yield return 1;
            }

            if (!helicopterHovering[minicopter.GetInstanceID()])
            {
                yield break;
            }
        }

        #endregion

        #region -Hooks-

        void OnEntityMounted(BaseMountable mount, BasePlayer player)
        {
            if (mount.GetParentEntity() != null)
            {
                if (mount.GetParentEntity().ShortPrefabName == "minicopter.entity" || mount.GetParentEntity().ShortPrefabName == "scraptransporthelicopter" && _config.Permission.scrapheliCanHover)
                {
                    MiniCopter minicopter = mount.GetParentEntity() as MiniCopter;

                    if (minicopter != null && permission.UserHasPermission(player.UserIDString, canHover))
                    {
                        PrintToChat(player, lang.GetMessage("Mounted", this, player.UserIDString));
                    }
                }
            }

        }

        void OnEntityDismounted(BaseMountable mount, BasePlayer player)
        {
            if (mount.GetParentEntity() != null)
            {
                if (_config.Hovering.disableHoverOnDismount && mount.GetParentEntity().ShortPrefabName == "minicopter.entity" || mount.GetParentEntity().ShortPrefabName == "scraptransporthelicopter" && _config.Permission.scrapheliCanHover)
                {
                    MiniCopter minicopter = mount.GetParentEntity() as MiniCopter;

                    if (minicopter != null && helicopterHovering.ContainsKey(minicopter.GetInstanceID()))
                    {
                        if (helicopterHovering[minicopter.GetInstanceID()] && mount.GetParentEntity() is MiniCopter && !minicopter.HasAnyPassengers() && minicopter != null)
                        {
                            helicopterHovering[minicopter.GetInstanceID()] = false;

                            minicopter.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                            minicopter.GetComponent<Rigidbody>().freezeRotation = false;
                        }
                    }
                    else
                    {
                        if (minicopter == null)
                        {
                            PrintWarning(lang.GetMessage("HelicopterNull", this, null));
                        }
                        else if (!helicopterHovering.ContainsKey(minicopter.GetInstanceID()))
                        {
                            return;
                        }
                    }
                }
                else if (!_config.Hovering.disableHoverOnDismount && mount.GetParentEntity().ShortPrefabName == "minicopter.entity" || mount.GetParentEntity().ShortPrefabName == "scraptransporthelicopter" && _config.Permission.scrapheliCanHover)
                {
                    if (_config.Hovering.keepEngineOnHover)
                    {
                        MiniCopter minicopter = mount.GetParentEntity() as MiniCopter;

                        if (minicopter != null && helicopterHovering.ContainsKey(minicopter.GetInstanceID()))
                        {
                            Rigidbody rb = minicopter.GetComponent<Rigidbody>();

                            minicopter.StartCoroutine(WhileHovering(minicopter, rb));
                        }

                    }
                }
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (helicopterHovering.ContainsKey(entity.GetInstanceID()))
            {
                helicopterHovering.Remove(entity.GetInstanceID());
            }
        }

        #endregion
    }
}