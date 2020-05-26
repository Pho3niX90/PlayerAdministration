using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    [Info("DMBuildingBlocks", "ColonBlow", "1.0.6")]
    class DMBuildingBlocks : RustPlugin
    {

        #region Loadup

        void Loaded()
        {
            lang.RegisterMessages(messages, this);
            permission.RegisterPermission("dmbuildingblocks.admin", this);
        }

        #endregion

        #region Configuration

        private static PluginConfig config;

        private class PluginConfig
        {
            public GlobalSettings globalSettings { get; set; }

            public class GlobalSettings
            {
                [JsonProperty(PropertyName = "Scale - Precent of damage allowed to any Building block that is currently protected (default is 0%) : ")] public float ProtectionScale { get; set; }
                [JsonProperty(PropertyName = "Protect - Foundation Square ? ")] public bool ProtectFoundation { get; set; }
                [JsonProperty(PropertyName = "Protect - Foundation Steps ? ")] public bool ProtectFoundationSteps { get; set; }
                [JsonProperty(PropertyName = "Protect - Foundation Triange ? ")] public bool ProtectFoundationTriangle { get; set; }
                [JsonProperty(PropertyName = "Protect - Wall Low ? ")] public bool ProtectLowWall { get; set; }
                [JsonProperty(PropertyName = "Protect - Wall Half ? ")] public bool ProtectHalfWall { get; set; }
                [JsonProperty(PropertyName = "Protect - Wall Full ? ")] public bool ProtectWall { get; set; }
                [JsonProperty(PropertyName = "Protect - Wall Frame ? ")] public bool ProtectWallFrame { get; set; }
                [JsonProperty(PropertyName = "Protect - Wall Window ? ")] public bool ProtectWindowWall { get; set; }
                [JsonProperty(PropertyName = "Protect - Wall Doorway ? ")] public bool ProtectDoorway { get; set; }
                [JsonProperty(PropertyName = "Protect - Floor Square ? ")] public bool ProtectFloor { get; set; }
                [JsonProperty(PropertyName = "Protect - Floor Triangle ? ")] public bool ProtectFloorTriangle { get; set; }
                [JsonProperty(PropertyName = "Protect - Floor Frame ? ")] public bool ProtectFloorFrame { get; set; }
                [JsonProperty(PropertyName = "Protect - Stairs L Shaped ? ")] public bool ProtectStairsLShaped { get; set; }
                [JsonProperty(PropertyName = "Protect - Stairs U Shaped ? ")] public bool ProtectStairsUShaped { get; set; }
                [JsonProperty(PropertyName = "Protect - Roof ? ")] public bool ProtectRoof { get; set; }
            }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                globalSettings = new PluginConfig.GlobalSettings
                {
                    ProtectionScale = 0f,
                    ProtectFoundation = false,
                    ProtectFoundationSteps = false,
                    ProtectFoundationTriangle = false,
                    ProtectLowWall = false,
                    ProtectHalfWall = false,
                    ProtectWall = false,
                    ProtectWallFrame = false,
                    ProtectWindowWall = false,
                    ProtectDoorway = false,
                    ProtectFloor = false,
                    ProtectFloorTriangle = false,
                    ProtectFloorFrame = false,
                    ProtectStairsLShaped = false,
                    ProtectStairsUShaped = false,
                    ProtectRoof = false
                }
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        Dictionary<string, string> messages = new Dictionary<string, string>()
            {
                {"nopermission", "You do not have permission to use that command" },
                {"protectscale", "Protection Scale is set to : " },
                {"wrongsyntax", "Incorrect Syntax used. Please check to make sure you typed the commmand correctly" },
                {"ProtectFoundation", "You have set ProtectFoundations to " },
                {"ProtectFoundationSteps", "You have set ProtectFoundationSteps to " },
                {"ProtectFoundationTriangle", "You have set ProtectFoundationTriangle to " },
                {"ProtectWindowWall", "You have set ProtectWindowWall to " },
                {"ProtectDoorway", "You have set ProtectDoorway to " },
                {"ProtectFloor", "You have set ProtectFloor to " },
                {"ProtectFloorTriangle", "You have set ProtectFloorTriangles to " },
                {"ProtectHalfWall", "You have set ProtectHalfWall to " },
                {"ProtectStairsLShaped", "You have set ProtectStairsLShaped to " },
                {"ProtectStairsUShaped", "You have set ProtectStairsUShaped to " },
                {"ProtectRoof", "You have set ProtectRoof to " },
                {"ProtectLowWall", "You have set ProtectLowWall to " },
                {"ProtectWall", "You have set ProtectWall to " },
                {"ProtectWallFrame", "You have set ProtectWallFrame to " },
                {"ProtectFloorFrame", "You have set ProtectFloorFrame to " }
            };

        #endregion

        #region Hooks

        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        void OnEntityTakeDamage(BuildingBlock buildingBlock, HitInfo hitInfo)
        {
            if (config.globalSettings.ProtectFoundation == true && buildingBlock.name.Contains("foundation") && !buildingBlock.name.Contains("triangle") && !buildingBlock.name.Contains("steps")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectFoundationTriangle == true && buildingBlock.name.Contains("foundation.triangle")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectFoundationSteps == true && buildingBlock.name.Contains("foundation.steps")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectWindowWall == true && buildingBlock.name.Contains("wall.window")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectDoorway == true && buildingBlock.name.Contains("wall.doorway")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectFloor == true && buildingBlock.name.Contains("floor") && !buildingBlock.name.Contains("triangle")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectFloorTriangle == true && buildingBlock.name.Contains("floor.triangle")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectHalfWall == true && buildingBlock.name.Contains("halfwall")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectStairsLShaped == true && buildingBlock.name.Contains("stairs.l")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectStairsUShaped == true && buildingBlock.name.Contains("stairs.u")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectRoof == true && buildingBlock.name.Contains("roof")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectLowWall == true && buildingBlock.name.Contains("wall.low")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectWall == true && buildingBlock.name.Contains("wall") && !buildingBlock.name.Contains("wall.low") && !buildingBlock.name.Contains("wall.doorway") && !buildingBlock.name.Contains("wall.window")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectWallFrame == true && buildingBlock.name.Contains("wall.frame")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
            if (config.globalSettings.ProtectFloorFrame == true && buildingBlock.name.Contains("floor.frame")) { hitInfo.damageTypes.ScaleAll(config.globalSettings.ProtectionScale * 0.01f); return; }
        }

        #endregion

        #region Commands

        [ChatCommand("ProtectionScale")]
        void chatCommand_ProtectionScale(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin"))
            {
                SendReply(player, lang.GetMessage("nopermission", this));
            }
            if (HasPermission(player, "dmbuildingblocks.admin"))
            {
                if (args != null && args.Length > 0)
                {
                    float argstring;

                    if (float.TryParse(args[0].ToLower(), out argstring))
                    {
                        config.globalSettings.ProtectionScale = argstring;
                        SaveConfig();
                        SendReply(player, lang.GetMessage("protectscale", this) + argstring.ToString() + "% Damage Allowed");
                    }
                    else
                    {
                        SendReply(player, lang.GetMessage("wrongsyntax", this));
                    }
                }
                return;
            }
        }

        [ChatCommand("ProtectFoundation")]
        void chatCommand_ProtectFoundation(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectFoundation = true;
                if (argstring == "false") config.globalSettings.ProtectFoundation = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectFoundation", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectFoundationSteps")]
        void chatCommand_ProtectFoundationSteps(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectFoundationSteps = true;
                if (argstring == "false") config.globalSettings.ProtectFoundationSteps = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectFoundationSteps", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectFoundationTriangle")]
        void chatCommand_ProtectFoundationTriangle(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectFoundationTriangle = true;
                if (argstring == "false") config.globalSettings.ProtectFoundationTriangle = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectFoundationTriangle", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectWindowWall")]
        void chatCommand_ProtectWindowWall(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectWindowWall = true;
                if (argstring == "false") config.globalSettings.ProtectWindowWall = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectWindowWall", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectDoorway")]
        void chatCommand_ProtectDoorway(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectDoorway = true;
                if (argstring == "false") config.globalSettings.ProtectDoorway = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectDoorway", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectFloor")]
        void chatCommand_ProtectFloor(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectFloor = true;
                if (argstring == "false") config.globalSettings.ProtectFloor = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectFloor", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectFloorTriangle")]
        void chatCommand_ProtectFloorTriangle(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectFloorTriangle = true;
                if (argstring == "false") config.globalSettings.ProtectFloorTriangle = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectFloorTriangle", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectHalfWall")]
        void chatCommand_ProtectHalfWall(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectHalfWall = true;
                if (argstring == "false") config.globalSettings.ProtectHalfWall = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectHalfWall", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectStairsLShaped")]
        void chatCommand_ProtectStairsLShaped(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectStairsLShaped = true;
                if (argstring == "false") config.globalSettings.ProtectStairsLShaped = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectStairsLShaped", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectStairsUShaped")]
        void chatCommand_ProtectStairsUShaped(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectStairsUShaped = true;
                if (argstring == "false") config.globalSettings.ProtectStairsUShaped = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectStairsUShaped", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectRoof")]
        void chatCommand_ProtectRoof(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectRoof = true;
                if (argstring == "false") config.globalSettings.ProtectRoof = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectRoof", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectLowWall")]
        void chatCommand_ProtectLowWall(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectLowWall = true;
                if (argstring == "false") config.globalSettings.ProtectLowWall = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectLowWall", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectWall")]
        void chatCommand_ProtectWall(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectWall = true;
                if (argstring == "false") config.globalSettings.ProtectWall = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectWall", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectWallFrame")]
        void chatCommand_ProtectWallFrame(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectWallFrame = true;
                if (argstring == "false") config.globalSettings.ProtectWallFrame = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectWallFrame", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        [ChatCommand("ProtectFloorFrame")]
        void chatCommand_ProtectFloorFrame(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "dmbuildingblocks.admin")) { SendReply(player, lang.GetMessage("nopermission", this)); return; }
            if (args != null && args.Length > 0)
            {
                string argstring = args[0].ToLower();
                if (argstring == "true") config.globalSettings.ProtectFloorFrame = true;
                if (argstring == "false") config.globalSettings.ProtectFloorFrame = false;
                SaveConfig();
                SendReply(player, lang.GetMessage("ProtectFloorFrame", this) + argstring);
                return;
            }
            SendReply(player, lang.GetMessage("wrongsyntax", this));
        }

        #endregion
    }
}