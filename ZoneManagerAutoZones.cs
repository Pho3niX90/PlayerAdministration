using System;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core.Plugins;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Zone Manager Auto Zones", "FastBurst", "1.2.0")]
    [Description("Adds zones and domes to monuments automatically.")]

    public class ZoneManagerAutoZones : RustPlugin
    {
        [PluginReference] Plugin ZoneManager, ZoneDomes, TruePVE;

        BasePlayer player;

        void OnServerInitialized()
        {
			
			
            PopulateZoneLocations();
			
			//if (configData.ZoneOption.UseZoneDomes)
			//	ZoneDomes?.Call("OnServerInitialized",player);
        }

        private void PopulateZoneLocations()
        {
            ConfigData.LocationOptions.Monument config = configData.Location.Monuments;
			
			//if (configData.ZoneOption.UseZoneDomes)
			//	ZoneDomes?.Call("DestroyAllSpheres",player);

            MonumentInfo[] monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
            for (int i = 0; i < monuments.Length; i++)
            {
                MonumentInfo monument = monuments[i];

                if (monument.name.Contains("harbor_1", CompareOptions.IgnoreCase))
                {
                    string friendlyname = "Large " + monument.displayPhrase.english;
                    string ID = "harbor_1";

                    if (config.HarborLarge.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.HarborLarge.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.HarborLarge.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.HarborLarge.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.HarborLarge.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.HarborLarge.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.HarborLarge.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("harbor_2", CompareOptions.IgnoreCase))
                {
                    string friendlyname = "Small " + monument.displayPhrase.english;
                    string ID = "harbor_2";

                    if (config.HarborSmall.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.HarborSmall.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.HarborSmall.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.HarborSmall.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.HarborSmall.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.HarborSmall.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.HarborSmall.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("airfield_1", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "airfield_1";

                    if (config.Airfield.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.Airfield.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.Airfield.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.Airfield.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.Airfield.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.Airfield.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.Airfield.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("launch_site_1", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "launch_site";

                    if (config.LaunchSite.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.LaunchSite.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.LaunchSite.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.LaunchSite.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.LaunchSite.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.LaunchSite.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("oilrigai2", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "oilrigai2";

                    if (config.LargeOilRig.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.LargeOilRig.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.LargeOilRig.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.LargeOilRig.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.LargeOilRig.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.LargeOilRig.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.LargeOilRig.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("oilrigai", CompareOptions.IgnoreCase))
                {
                    string friendlyname = "Small " + monument.displayPhrase.english;
                    string ID = "oilrigai";

                    if (config.SmallOilRig.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.SmallOilRig.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.SmallOilRig.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.SmallOilRig.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.SmallOilRig.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.SmallOilRig.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.SmallOilRig.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("powerplant_1", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "powerplant_1";

                    if (config.Powerplant.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.Powerplant.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.Powerplant.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.Powerplant.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.Powerplant.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.Powerplant.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.Powerplant.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("military_tunnel_1", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "military_tunnel_1";

                    if (config.MilitaryTunnels.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.MilitaryTunnels.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.MilitaryTunnels.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.MilitaryTunnels.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.MilitaryTunnels.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.MilitaryTunnels.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.MilitaryTunnels.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("junkyard_1", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "junkyard_1";

                    if (config.Junkyard.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.Junkyard.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.Junkyard.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.Junkyard.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.Junkyard.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.Junkyard.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.Junkyard.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("water_treatment_plant_1", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "water_treatment_plant_1";

                    if (config.WaterTreatment.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.WaterTreatment.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.WaterTreatment.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.WaterTreatment.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.WaterTreatment.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.WaterTreatment.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.WaterTreatment.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("trainyard_1", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "trainyard_1";

                    if (config.TrainYard.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.TrainYard.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.TrainYard.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.TrainYard.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.TrainYard.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.TrainYard.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.TrainYard.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("excavator", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "excavator";

                    if (config.Excavator.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.Excavator.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.Excavator.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.Excavator.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.Excavator.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.Excavator.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.Excavator.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);														
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
							
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("satellite_dish", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "satellite_dish";

                    if (config.SatelliteDish.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.SatelliteDish.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.SatelliteDish.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.SatelliteDish.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.SatelliteDish.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.SatelliteDish.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.SatelliteDish.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("radtown_small_3", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "radtown_small_3";

                    if (config.SewerBranch.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.SewerBranch.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.SewerBranch.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.SewerBranch.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.SewerBranch.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.SewerBranch.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.SewerBranch.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }

                if (monument.name.Contains("sphere_tank", CompareOptions.IgnoreCase))
                {
                    string friendlyname = monument.displayPhrase.english;
                    string ID = "sphere_tank";

                    if (config.Dome.Enabled)
                    {
                        string[] messages = new string[8];
                        messages[0] = "name";
                        messages[1] = friendlyname;
                        messages[2] = "enter_message";
                        messages[3] = configData.Location.Monuments.Dome.EnterMessage;
                        messages[4] = "leave_message";
                        messages[5] = configData.Location.Monuments.Dome.LeaveMessage;
                        messages[6] = "radius";
                        messages[7] = Convert.ToInt32(configData.Location.Monuments.Dome.Radius).ToString();

                        ZoneManager?.CallHook("CreateOrUpdateZone", ID, messages, monument.transform.position);

                        if (configData.Location.Monuments.Dome.ZoneFlags != null)
                            foreach (var flag in configData.Location.Monuments.Dome.ZoneFlags)
                                ZoneManager?.CallHook("AddFlag", ID, flag);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("AddOrUpdateMapping", ID, configData.Location.Monuments.Dome.TruePVERules);

                        if (configData.ZoneOption.UseZoneDomes)
						{
							ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                            ZoneDomes?.CallHook("AddNewDome", player, ID);
						}
                    }
                    else
                    {
                        ZoneManager?.CallHook("EraseZone", ID);

                        if (configData.ZoneOption.UseTruePVE)
                            TruePVE?.CallHook("RemoveMapping", ID);

                        if (configData.ZoneOption.UseZoneDomes)
                            ZoneDomes?.CallHook("RemoveExistingDome", player, ID);
                    }
                    continue;
                }
            }
			
			if (configData.ZoneOption.UseZoneDomes)
			{
				ZoneDomes?.Call("DestroyAllSpheres",player);
				ZoneDomes?.Call("OnServerInitialized",player);
			}
        }

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "ZonesDome Option")]
            public Options ZoneOption { get; set; }
            public class Options
            {
                [JsonProperty(PropertyName = "Use Zone Domes Spheres over Zones")]
                public bool UseZoneDomes { get; set; }
                [JsonProperty(PropertyName = "Enable TruePVE to allow Rule Sets")]
                public bool UseTruePVE { get; set; }
            }

            [JsonProperty(PropertyName = "Zone Location Options")]
            public LocationOptions Location { get; set; }

            public class LocationOptions
            {
                [JsonProperty(PropertyName = "Monument Options")]
                public Monument Monuments { get; set; }

                public class Monument
                {
                    [JsonProperty(PropertyName = "Airfield")]
                    public Options Airfield { get; set; }
                    [JsonProperty(PropertyName = "Small Harbour")]
                    public Options HarborSmall { get; set; }
                    [JsonProperty(PropertyName = "Large Harbour")]
                    public Options HarborLarge { get; set; }
                    [JsonProperty(PropertyName = "Launch Site")]
                    public Options LaunchSite { get; set; }
                    [JsonProperty(PropertyName = "Large Oil Rig")]
                    public Options LargeOilRig { get; set; }
                    [JsonProperty(PropertyName = "Small Oil Rig")]
                    public Options SmallOilRig { get; set; }
                    [JsonProperty(PropertyName = "Power Plant")]
                    public Options Powerplant { get; set; }
                    [JsonProperty(PropertyName = "Junk Yard")]
                    public Options Junkyard { get; set; }
                    [JsonProperty(PropertyName = "Military Tunnels")]
                    public Options MilitaryTunnels { get; set; }
                    [JsonProperty(PropertyName = "Train Yard")]
                    public Options TrainYard { get; set; }
                    [JsonProperty(PropertyName = "Water Treatment Plant")]
                    public Options WaterTreatment { get; set; }
                    [JsonProperty(PropertyName = "Giant Excavator Pit")]
                    public Options Excavator { get; set; }
                    [JsonProperty(PropertyName = "Satellite Dish")]
                    public Options SatelliteDish { get; set; }
                    [JsonProperty(PropertyName = "Sewer Branch")]
                    public Options SewerBranch { get; set; }
                    [JsonProperty(PropertyName = "The Dome")]
                    public Options Dome { get; set; }

                    public class Options
                    {
                        public bool Enabled { get; set; }
                        [JsonProperty(PropertyName = "Radius")]
                        public int Radius { get; set; }

                        [JsonProperty(PropertyName = "Enter Zone Message")]
                        public string EnterMessage { get; set; }

                        [JsonProperty(PropertyName = "Leave Zone Message")]
                        public string LeaveMessage { get; set; }

                        [JsonProperty(PropertyName = "Zone Flags")]
                        public string[] ZoneFlags { get; set; }
                        [JsonProperty(PropertyName = "TruePVE RuleSet to use if TruePVE is enabled")]
                        public string TruePVERules { get; set; }
                        [JsonIgnore]
                        public List<MonumentInfo> Monument { get; set; } = new List<MonumentInfo>();
                    }
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
                ZoneOption = new ConfigData.Options
                {
                    UseZoneDomes = true,
                    UseTruePVE = false
                },
                Location = new ConfigData.LocationOptions
                {
                    Monuments = new ConfigData.LocationOptions.Monument
                    {
                        Airfield = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 200,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        HarborLarge = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 150,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        HarborSmall = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 145,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        LaunchSite = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 295,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        SmallOilRig = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 100,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        LargeOilRig = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 165,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        Powerplant = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 150,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        Junkyard = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 165,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        MilitaryTunnels = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 115,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        TrainYard = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 165,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        WaterTreatment = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 175,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        Excavator = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 205,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        SatelliteDish = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 100,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        SewerBranch = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 80,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        },
                        Dome = new ConfigData.LocationOptions.Monument.Options
                        {
                            Enabled = true,
                            Radius = 80,
                            EnterMessage = "WARNING: You are now entering a PVP Zone",
                            LeaveMessage = "Returning to PVE Area",
                            ZoneFlags = new string[] { "autolights" },
                            TruePVERules = "exclude"
                        }
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
            if (configData.Version < new Core.VersionNumber(1, 0, 0))
                configData.ZoneOption.UseZoneDomes = baseConfig.ZoneOption.UseZoneDomes;

            if (configData.Version < new Core.VersionNumber(1, 2, 0))
            {
                configData.Location.Monuments = baseConfig.Location.Monuments;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion
    }
}