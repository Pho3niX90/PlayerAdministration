using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.CSharp;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("AutomatedEvents", "k1lly0u", "0.3.07", ResourceId = 0)]
    class AutomatedEvents : RustPlugin
    {
        #region Fields
        enum EventType { Bradley, CargoPlane, CargoShip, Chinook, Helicopter, HelicopterRefuel, PilotEject, PlaneCrash, XMasEvent, EasterEvent, HalloweenEvent, SantaEvent, None }

		DateTime unsetDate = new DateTime(1900, 1, 1, 1, 1, 0);
        private Dictionary<EventType, Timer> eventTimers = new Dictionary<EventType, Timer>();
        const string permAuto = "automatedevents.allowed";
        const string permNext = "automatedevents.next";
        #endregion

        #region Oxide Hooks
		[PluginReference] Plugin AlphaChristmas;
		[PluginReference] Plugin BradleyControl;
		[PluginReference] Plugin FancyDrop;
		[PluginReference] Plugin GUIAnnouncements;
		[PluginReference] Plugin HeliControl;
		[PluginReference] Plugin HeliRefuel;
		[PluginReference] Plugin PilotEject;
		[PluginReference] Plugin PlaneCrash;
        #endregion
		
		private bool AnnounceOnLoad = false;
		private bool doGUIAnnouncements = false;
		private bool RemoveOnStart = false;
		private bool unlimitedBradleys = false;
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["What"] = "no clue what {0} event is",
                ["blankevent"] = "you need to specify an event",
				["NotSet"] = "{0} is not set to run via Automated Events",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
				["Running"] = "Attempting to run automated event: {0}",
				["bradley"] = "Next Bradley APC expected in approximately {0} minutes",
				["cargoplane"] = "Next Cargo plane expected in approximately {0} minutes",
				["cargoship"] = "Next Cargo ship expected in approximately {0} minutes",
				["chinook"] = "Next Chinook CH47 expected in approximately {0} minutes",
				["easter"] = "Next Easter Egg Hunt expected in approximately {0} minutes",
				["halloween"] = "Next Halloween Candy Hunt expected in approximately {0} minutes",
				["helicopter"] = "Next Helicopter expected in approximately {0} minutes",
				["helicopterrefuel"] = "Next Helicopter Refuel expected in approximately {0} minutes",
				["piloteject"] = "Next Helicopter crash expected in approximately {0} minutes",
				["planecrash"] = "Next Plane crash expected in approximately {0} minutes",
				["santa"] = "Next Santa's visit expected in approximately {0} minutes",
				["xmas"] = "Next Christmas Presents expected in approximately {0} minutes"
            }, this);
        }

        #region Config
        private ConfigData configData;
		private ConfigData DefaultconfigData;
        class ConfigData
        {
            public Dictionary<string, string> Settings { get; set; }
            public Dictionary<EventType, EventEntry> Events { get; set; }
        }

        class EventEntry
        {
            public bool     Enabled { get; set; }
            public bool     AnnounceNext { get; set; }
			public bool     DisableDefault { get; set; }
			public bool     JustSpawned { get; set; }
            public int      MinimumTimeBetween { get; set; }
            public int      MaximumTimeBetween { get; set; }
            public string   Real_or_Game_Always { get; set; }
            public float    BeginRun { get; set; }
            public float    EndRun { get; set; }
            public DateTime NextRun { get; set; }
        }

        private void LoadVariables()
        {
			SetDefaultConfig();
            LoadConfigVariables();
            SaveConfig();
        }

         private void SetDefaultConfig()
        {
            var config = new ConfigData
            {
				Settings = new Dictionary<String, String>
				{
					{"AnnounceOnLoad","false"},
					{"UseGUIAnnouncementsPlugin","false"},
					{"RemoveOnStart","false"},
					{"UnlimitedBradleys","false"},
				},
                Events = new Dictionary<EventType, EventEntry>
                {
                    { EventType.Bradley, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 45,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.CargoPlane, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 45,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.CargoShip, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 45,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.Chinook, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
 						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 45,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.Helicopter, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 45,
                        MaximumTimeBetween = 60,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.HelicopterRefuel, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 45,
                        MaximumTimeBetween = 60,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.PlaneCrash, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 45,
                        MaximumTimeBetween = 60,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.PilotEject, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 45,
                        MaximumTimeBetween = 60,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.XMasEvent, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
 						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 60,
                        MaximumTimeBetween = 120,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.EasterEvent, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 60,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
					},
                    { EventType.HalloweenEvent, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 60,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    },
                    { EventType.SantaEvent, new EventEntry
                    {
                        Enabled = false,
						AnnounceNext = false,
						DisableDefault = false,
						JustSpawned = false,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 60,
						Real_or_Game_Always = "Always",
						BeginRun = 0,
						EndRun = 0,
						NextRun = unsetDate
                    }
                    }
                }
            };
			DefaultconfigData = config;
			configData = config;
        }

        protected override void LoadDefaultConfig()
        {
			SetDefaultConfig();
			SaveConfig(DefaultconfigData);
		}

        #endregion
        #region maincode
        private void OnServerInitialized()
        {
            permission.RegisterPermission(permAuto, this);
            permission.RegisterPermission(permNext, this);
        }

		private void CleanEmUp()
		{		
			// if defaults off kill any existing ones
			if (configData.Events[EventType.Bradley].DisableDefault)
			{
				foreach (var x in UnityEngine.Object.FindObjectsOfType<BradleyAPC>().ToList())
					x.Kill();
			}	
			if (configData.Events[EventType.CargoPlane].DisableDefault)
			{
				foreach (var x in UnityEngine.Object.FindObjectsOfType<CargoPlane>().ToList())
					x.Kill();
			}	
			if (configData.Events[EventType.CargoShip].DisableDefault)
			{
				foreach (var x in UnityEngine.Object.FindObjectsOfType<CargoShip>().ToList())
					x.Kill();
			}	
			if (configData.Events[EventType.Chinook].DisableDefault)
			{
				foreach (var x in UnityEngine.Object.FindObjectsOfType<CH47Helicopter>().ToList())
					x.Kill();
			}	
			if (configData.Events[EventType.Helicopter].DisableDefault)
			{
				foreach (var x in UnityEngine.Object.FindObjectsOfType<BaseHelicopter>().ToList())
					x.Kill();
			}	
		}

		private void Loaded()
		{
			LoadVariables();
			// we can't disable the CH47 as it will break the Oil Rig Event
			configData.Events[EventType.Chinook].DisableDefault = false;

			// clean up old entites (not recommended)
			if (RemoveOnStart)
				timer.Once(60, () => {CleanEmUp(); return;});

            foreach (var eventType in configData.Events)
			{
				// clear last run dates and spawned flag
				configData.Events[eventType.Key].NextRun = unsetDate;
				configData.Events[eventType.Key].JustSpawned = false;
				eventTimers[eventType.Key] = null;
				timer.Once(70, () => {StartEventTimer(eventType.Key,AnnounceOnLoad); return;});
			}
		}
        private void Unload()
        {
            foreach(var timer in eventTimers)
            {
                if (timer.Value != null)
				{
					try
					{
						timer.Value.Destroy();
					}
					catch
					{}
                    
				}
            }
        }
	
		// reset the timer if the Bradley, Helicopter of Chinook are destroyed
		private void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
		{
			EventType type = EventType.None;

			if (victim is BradleyAPC)
				type = EventType.Bradley;
			if (victim is BaseHelicopter)
				type = EventType.Helicopter;
			if (victim is CH47HelicopterAIController)
				type = EventType.Chinook;
			if (victim is CargoPlane)
				type = EventType.CargoPlane;
			if (victim is CargoShip)
				type = EventType.CargoShip;

			if (type != EventType.None) // Valid Death to process
			{
				// make sure the just spawned flag is cleared
				configData.Events[type].JustSpawned = false;
				// safe to start the event again if a player killed it
				// otherwise it could be due to cleanup of the default spawn
				if (info != null && type != EventType.CargoPlane && type != EventType.CargoShip)
				{
					StartEventTimer(type, configData.Events[type].AnnounceNext);
				}
			}
		}

		private void OnEntitySpawned(BaseNetworkable entity)
        {		
			if (entity == null || !(entity is BaseEntity) || !(entity is BaseCombatEntity) || entity.IsDestroyed) return;
			//Puts(entity.GetComponent<BaseEntity>()?.ShortPrefabName);
						
			EventType type = EventType.None;			
			if (entity is CargoShip && (configData.Events[EventType.CargoShip].DisableDefault) && !(configData.Events[EventType.CargoShip].JustSpawned))
				type = EventType.CargoShip;
			else if (entity is BradleyAPC && (configData.Events[EventType.Bradley].DisableDefault) && !(configData.Events[EventType.Bradley].JustSpawned))
				type = EventType.Bradley;
			// we will need to code around the oil rig event at some stage
			//else if (entity is CH47Helicopter && (configData.Events[EventType.Chinook].DisableDefault) && !(configData.Events[EventType.Chinook].JustSpawned))
			//	type = EventType.Chinook;
			else if (entity is CargoPlane)
			{
				if ((configData.Events[EventType.CargoPlane].DisableDefault) && !(configData.Events[EventType.CargoPlane].JustSpawned))
					type = EventType.CargoPlane;
				else if ((configData.Events[EventType.PlaneCrash].DisableDefault) && !(configData.Events[EventType.PlaneCrash].JustSpawned))
					type = EventType.PlaneCrash;
			}
			else if (entity is BaseHelicopter)
			{
				if ((configData.Events[EventType.Helicopter].DisableDefault) && !(configData.Events[EventType.Helicopter].JustSpawned))
					type = EventType.Helicopter;
				else if ((configData.Events[EventType.PilotEject].DisableDefault) && !(configData.Events[EventType.PilotEject].JustSpawned))
					type = EventType.PilotEject;
				else if ((configData.Events[EventType.HelicopterRefuel].DisableDefault) && !(configData.Events[EventType.HelicopterRefuel].JustSpawned))
					type = EventType.HelicopterRefuel;
			}
			else return;
							
			if (type == EventType.None) return;  // not something we should touch
			// if it is not one we spawned, it is an event type entity and the owner does not want default spawns, kill it
			else if (!configData.Events[type].JustSpawned)
			{
				switch (type)
					{
						case EventType.CargoShip: Puts("Killing CargoShip"); break;
						case EventType.Bradley: Puts("Killing Bradley"); break;
						case EventType.Chinook: Puts("Killing Chinook"); break;
						case EventType.CargoPlane: Puts("Killing CargoPlane"); break;
						case EventType.PlaneCrash: Puts("Killing Plane Crash"); break;
						case EventType.Helicopter: Puts("Killing Helicopter"); break;
						case EventType.PilotEject: Puts("Killing Pilot Eject"); break;
						case EventType.HelicopterRefuel: Puts("Killing Heli Refuel"); break;
						default: return; break;
					}
				 if (!entity.IsDestroyed)
					entity.Kill();
			}
        }

        #endregion
        //#region Command
		[ChatCommand("NextEvent")]
		private void AENextCommand(BasePlayer player, string command, string[] args)
        {
			IPlayer iplayer = player.IPlayer;
            if (!IsAllowedNext(iplayer))
            {
                iplayer.Message(Lang("NotAllowed", iplayer.Id, command));
                return;
            }

			if (args == null || args.Length < 1 || String.IsNullOrWhiteSpace(args[0]))
			{
				iplayer.Message(Lang("blankevent", iplayer.Id));
				return;
			}
			else
			{
				if (args[0] == "*")
				{
					// do them all
					foreach (var eventType in configData.Events)
					{
						AENextShow(iplayer, eventType.Key, " ");
					}
				}
				else
				{
					EventType Event = PickEvent(args[0],iplayer);
					AENextShow(iplayer, Event, args[0]);
				}
			}
		}

		private void AENextShow(IPlayer iplayer, EventType Event, string eventText)
        {
			DateTime localDate = System.DateTime.Now;
			TimeSpan deltaDate = new TimeSpan(0, 0, 0, 0, 0);
			int intMinutes     = 0;

			switch (Event)
			{
				case EventType.Bradley:
					if (configData.Events[EventType.Bradley].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.Bradley].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("bradley", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));

					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.CargoPlane:
					if (configData.Events[EventType.CargoPlane].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.CargoPlane].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("cargoplane", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.CargoShip:
					if (configData.Events[EventType.CargoShip].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.CargoShip].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("cargoship", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.Chinook:
					if (configData.Events[EventType.Chinook].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.Chinook].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("chinook", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.Helicopter:
					if (configData.Events[EventType.Helicopter].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.Helicopter].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("helicopter", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.XMasEvent:
					if (configData.Events[EventType.XMasEvent].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.XMasEvent].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("xmas", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.SantaEvent:
					if (configData.Events[EventType.SantaEvent].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.SantaEvent].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("santa", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.EasterEvent:
					if (configData.Events[EventType.EasterEvent].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.EasterEvent].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("easter", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.HalloweenEvent:
					if (configData.Events[EventType.HalloweenEvent].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.HalloweenEvent].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("halloween", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.HelicopterRefuel:
					if (configData.Events[EventType.HelicopterRefuel].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.HelicopterRefuel].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("helicopterrefuel", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.PilotEject:
					if (configData.Events[EventType.PilotEject].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.PilotEject].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("piloteject", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				case EventType.PlaneCrash:
					if (configData.Events[EventType.PlaneCrash].NextRun != unsetDate)
					{
						deltaDate = configData.Events[EventType.PlaneCrash].NextRun - localDate;
						intMinutes = (int)deltaDate.TotalMinutes;
						if (intMinutes >= 0)
							iplayer.Message(String.Concat(Lang("planecrash", iplayer.Id, intMinutes.ToString())));
						else if (!String.IsNullOrWhiteSpace(eventText))
							iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					}
					else if (!String.IsNullOrWhiteSpace(eventText))
						iplayer.Message(String.Concat(Lang("NotSet", iplayer.Id, eventText)));
					break;
				default:
					break;
			}
		}

		[ChatCommand("runevent")]
		private void AEchatCommand(BasePlayer player, string command, string[] args)
        {
			string[] pass_args  = args.Skip(1).ToArray();
			IPlayer iplayer = player.IPlayer;
            if (!IsAllowedCMD(iplayer))
            {
                iplayer.Message(Lang("NotAllowed", iplayer.Id, command));
                return;
            }

			if (args == null || args.Length < 1 || String.IsNullOrWhiteSpace(args[0]))
			{
				 iplayer.Message(Lang("blankevent", iplayer.Id));
				return;
			}
			else
			{
				iplayer.Message(Lang("Running", iplayer.Id, args[0].ToLower()));
				Puts("Player " + iplayer.Name + " (" + iplayer.Id.ToString() + ") is attempting to run automated event: " + args[0].ToLower());
				RunEvent(PickEvent(args[0].ToLower(),iplayer), pass_args, true);
			}
		}

        [ConsoleCommand("runevent")]
        private void consoleRunEvent(ConsoleSystem.Arg arg)
        {
			if (arg?.Args == null || arg?.Args?.Length < 1 || String.IsNullOrWhiteSpace(arg.Args[0]))
			{
				Puts("No event specified!");
				return;
			}
			else
			{
				string[] pass_args  = arg?.Args.Skip(1).ToArray();
				//Puts ("Running Automated Event: " + arg.Args[0].ToLower());
				RunEvent(PickEvent(arg.Args[0].ToLower(),null), pass_args, true);
			}

		}
        //#endregion

        private EventType PickEvent(string Event, IPlayer iplayer)
		{
			switch (Event.ToLower())
			{
				case "brad":
				case "bradley":
					return EventType.Bradley;
				case "plane":
				case "cargoplane":
					return EventType.CargoPlane;
				case "ship":
				case "cargo":
				case "cargoship":
					return EventType.CargoShip;
				case "ch47":
				case "chinook":
					return EventType.Chinook;
				case "heli":
				case "helicopter":
				case "copter":
					return EventType.Helicopter;
				case "xmas":
				case "chris":
				case "christmas":
				case "yule":
					return EventType.XMasEvent;
				case "santa":
				case "nick":
				case "wodan":
					return EventType.SantaEvent;
				case "easter":
				case "egghunt":
				case "bunny":
					return EventType.EasterEvent;
				case "samhain":
				case "spooky":
				case "halloween":
				case "halloweenhunt":
				case "candy":
					return EventType.HalloweenEvent;
				case "helicopterrefuel":
				case "helirefuel":
				case "refuel":
					return EventType.HelicopterRefuel;
				case "helicrash":
				case "piloteject":
				case "eject":
					return EventType.PilotEject;
				case "planecrash":
				case "crash":
					return EventType.PlaneCrash;
				default:
					if (iplayer != null && iplayer.Id != null)
						iplayer.Message(Lang("What", iplayer.Id, Event));
					else
						Puts("No clue what event this is: " + Event);
					return EventType.None;
			}
		}

        #region Functions
        private void StartEventTimer(EventType type, bool announce=true)
        {
            var config = configData.Events[type];

			// allow for idiots		
			float randomTime = 0;
			if (config.MinimumTimeBetween <= config.MaximumTimeBetween)
				randomTime = Oxide.Core.Random.Range(config.MinimumTimeBetween, config.MaximumTimeBetween);
			else
				randomTime = Oxide.Core.Random.Range(config.MaximumTimeBetween, config.MinimumTimeBetween);

            if (!config.Enabled)
			{
				switch (type)
				{
					case EventType.Bradley: Puts("Not Running Bradley");  break;
					case EventType.CargoPlane: Puts("Not Running Cargo Plane"); break;
					case EventType.CargoShip: Puts("Not Running Cargo Ship"); break;
					case EventType.Chinook: Puts("Not Running Chinook"); break;
					case EventType.Helicopter: Puts("Not Running Helicopter"); break;
					case EventType.SantaEvent: Puts("Not Running Santa"); break;
					case EventType.XMasEvent: Puts("Not Running Christmas"); break;
					case EventType.EasterEvent: Puts("Not Running Easter"); break;
					case EventType.HalloweenEvent: Puts("Not Running Halloween"); break;
					break;
				}
				return;
			}
			else
			{
				DateTime NextDate    = System.DateTime.Now.AddMinutes(randomTime);
				float GameHM         = TOD_Sky.Instance.Cycle.Hour;
				int   CurrentHours   = System.DateTime.Now.Hour;
				int   CurrentMinutes = System.DateTime.Now.Minute;
				float CurrentHM      = CurrentHours + (CurrentMinutes/60);
				int   NextHours      = NextDate.Hour;
				int   NextMinutes    = NextDate.Minute;
				float NextHM         = NextHours + (NextMinutes/60);

				// delete the old timer if it exists
				if (eventTimers[type] != null)
				{
					try
					{
						eventTimers[type].Destroy();
					}
					catch {}
				}

				if (config.Real_or_Game_Always.ToLower() == "game")
				{
					if (config.BeginRun > config.EndRun)
					{
						// if current time is < begin || > end or
						if (GameHM < config.BeginRun && GameHM > config.EndRun)
						{
							// run timer to test in a bit
							eventTimers[type] = timer.Once(60, () => {StartEventTimer(type, announce); return;});
							return;
						}
					}
					else if (config.BeginRun < config.EndRun)
					{
						// if current time is < begin || > end or
						// if old time was after end and before start and time now is not run it now, otherwise sleep
						if (GameHM < config.BeginRun || GameHM > config.EndRun)
						{
							// run timer to test in a bit
							eventTimers[type] = timer.Once(60, () => {StartEventTimer(type, announce); return;});
							return;
						}
					}
				}
				else if (config.Real_or_Game_Always.ToLower() == "real")
				{
					if (config.BeginRun > config.EndRun && (NextHM < config.BeginRun && NextHM > config.EndRun))
						// generate a date >= BeginRun
						NextDate =  NextDate.AddMinutes(((config.BeginRun*60) - ((CurrentHours*60) + CurrentMinutes)));
					if (config.BeginRun < config.EndRun && (NextHM < config.BeginRun || NextHM > config.EndRun))
						// generate a date >= BeginRun
						NextDate =  NextDate.AddMinutes(((config.BeginRun*60) - ((CurrentHours*60) + CurrentMinutes)));
				}	
				
				config.NextRun = NextDate;
				string[] pass_args  = null;
				eventTimers[type] = timer.In(randomTime * 60, () => RunEvent(type, pass_args, false));

				switch (type)
				{
					case EventType.Bradley:
					{
						Puts("Next Bradley Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("bradley",randomTime.ToString());
						break;
					}
					case EventType.CargoPlane:
					{
						Puts("Next Cargo Plane Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("cargoplane",randomTime.ToString());
						break;
					}
					case EventType.CargoShip:
					{
						Puts("Next Cargo Ship Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("cargoship",randomTime.ToString());
						break;
					}
					case EventType.Chinook:
					{
						Puts("Next Chinook Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("chinook",randomTime.ToString());
						break;
					}
					case EventType.Helicopter:
					{
						Puts("Next Helicopter Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("helicopter",randomTime.ToString());
						break;
					}
					case EventType.HelicopterRefuel:
					{
						Puts("Next Helicopter Refuel Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("helicopterrefuel",randomTime.ToString());
						break;
					}
					case EventType.PlaneCrash:
					{
						Puts("Next Plane Crash Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("planecrash",randomTime.ToString());
						break;
					}
					case EventType.PilotEject:
					{
						Puts("Next Pilot Eject Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("piloteject",randomTime.ToString());
						break;
					}
					case EventType.SantaEvent:
					{
						Puts("Next Santa Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("santa",randomTime.ToString());
						break;
					}
					case EventType.XMasEvent:
					{
						Puts("Next Christmas Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("xmas",randomTime.ToString());
						break;
					}
					case EventType.EasterEvent:
					{
						Puts("Next Easter Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("easter",randomTime.ToString());
						break;
					}
					case EventType.HalloweenEvent:
					{
						Puts("Next Halloween Event in " + randomTime.ToString()+" minutes");
						if (announce && config.AnnounceNext)
							MessagePlayers("halloween",randomTime.ToString());
						break;
					}
					default:
						break;
				}
			}
        }

        private void RunEvent(EventType type, string[] pass_args, bool runonce=false)
        {
			if (type == EventType.None)
				return;
			bool used_other = false;
            string prefabName = string.Empty;
			float  y_extra_offset = 0.0f;

			//Puts(ConVar.Server.worldsize.ToString());
			float ran_min =  0.75f;
			float ran_max =  0.85f;

			// this might push this too close to the edge on some maps
			if (type == EventType.CargoShip)
			{
				ran_min = 0.87f;
				ran_max = 0.93f;
			}

			float x_plus_minus = ((UnityEngine.Random.value)>=0.50f)?-1.0f:1.0f;
			float z_plus_minus = ((UnityEngine.Random.value)>=0.50f)?-1.0f:1.0f;

			//Puts("x_plus_minus: " + x_plus_minus.ToString());
			//Puts("z_plus_minus: " + z_plus_minus.ToString());

			// safety check in case the sign trick fails
			if (x_plus_minus == 0) x_plus_minus = 1.0f;
			if (z_plus_minus == 0) z_plus_minus = -1.0f;

			Vector3 vector3_1 = new Vector3();
			vector3_1.x = Oxide.Core.Random.Range(ran_min, ran_max) * x_plus_minus * (ConVar.Server.worldsize/2);
			vector3_1.z = Oxide.Core.Random.Range(ran_min, ran_max) * z_plus_minus * (ConVar.Server.worldsize/2);
			vector3_1.y = 0.0f;
			//Puts("water level: " + TerrainMeta.WaterMap.GetHeight(vector3_1).ToString());
			vector3_1.y = TerrainMeta.WaterMap.GetHeight(vector3_1);
			if (vector3_1.y < 0)  // make sure its not messed up
				vector3_1.y = 300;

			 //Puts("X1: " + vector3_1.x.ToString());
			 //Puts("Z1: " + vector3_1.z.ToString());
			 //Puts("Y1: " + vector3_1.y.ToString());


            switch (type)
            {
                case EventType.Bradley:
					if(UnityEngine.Object.FindObjectsOfType<BradleyAPC>().ToList().Count == 0 || unlimitedBradleys == true)
	                {
						configData.Events[EventType.Bradley].JustSpawned = true;
						try
						{
							if (BradleyControl.IsLoaded == true)
							{
								Puts("Spawning Bradley Control Bradley");
								rust.RunServerCommand("bradleycontrol.reset " + string.Join(" ", pass_args));
								used_other = true;
							}
						}
						catch
						{
							used_other = false;
						}
						if (used_other == false)
						{
							Puts("Spawning Bradley");
							prefabName = "assets/prefabs/npc/m2bradley/bradleyapc.prefab";
							BradleyAPC bradley = (BradleyAPC)GameManager.server.CreateEntity(prefabName, new Vector3(), new Quaternion(), true);
							bradley.Spawn();
							// Ensures there is a AI path to follow.
							Vector3 position = BradleySpawner.singleton.path.interestZones[Oxide.Core.Random.Range(0, BradleySpawner.singleton.path.interestZones.Count)].transform.position;
							bradley.transform.position = position;
							bradley.DoAI = true;
							bradley.DoSimpleAI();
							bradley.InstallPatrolPath(BradleySpawner.singleton.path);
						}
					}
					else
					{
						Puts(" Bradley already out");
					}
                    break;
                case EventType.CargoPlane:
					configData.Events[EventType.CargoPlane].JustSpawned = true;
					try
					{
						Puts("Spawning Fancy Drop Cargo Plane");
						if (FancyDrop.IsLoaded == true)
						{
							if (pass_args.Length == 0)
								rust.RunServerCommand("ad.random");
							else
								rust.RunServerCommand("ad.toplayer " + string.Join(" ", pass_args));
							used_other = true;
						}
					}
					catch
					{
						used_other = false;
					}
					if (used_other == false)
					{
						Puts("Spawning Cargo Plane");
						prefabName = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
						y_extra_offset = 300.0f;
						vector3_1.y  = vector3_1.y + y_extra_offset;
						var Plane = (CargoPlane)GameManager.server.CreateEntity(prefabName, vector3_1, new Quaternion(), true);
						Plane.Spawn();
					}
                    break;
                case EventType.CargoShip:
					configData.Events[EventType.CargoShip].JustSpawned = true;
					Puts("Spawning CargoShip");
                    prefabName = "assets/content/vehicles/boats/cargoship/cargoshiptest.prefab";
					var Ship = (CargoShip)GameManager.server.CreateEntity(prefabName, vector3_1, new Quaternion(), true);
					Ship.Spawn();
					// comment out previous three, and uncomment the next line if the cargoship is buggy
					//rust.RunServerCommand("spawn cargoshiptest");
                    break;
                case EventType.Chinook:
					configData.Events[EventType.Chinook].JustSpawned = true;
					try
					{
						if (HeliControl.IsLoaded == true)
						{
						Puts("Spawning HeliControl Chinook");
						rust.RunServerCommand("callch47 "  + string.Join(" ", pass_args));
							used_other = true;
						}
					}
					catch
					{
						used_other = false;
					}
					if (used_other == false)
					{
						Puts("Spawning Chinook");
						prefabName = "assets/prefabs/npc/ch47/ch47scientists.entity.prefab"; // "assets/prefabs/npc/ch47/ch47.entity.prefab";
						y_extra_offset = 300.0f;
						vector3_1.y  = vector3_1.y + y_extra_offset;
						var Chin = (CH47HelicopterAIController)GameManager.server.CreateEntity(prefabName, vector3_1, new Quaternion(), true);
						Chin.Spawn();
					}
                    break;
                case EventType.Helicopter:
					configData.Events[EventType.Helicopter].JustSpawned = true;
					try
					{
						if (HeliControl.IsLoaded == true)
						{
							Puts("Spawning HeliControl Helicopter");
							rust.RunServerCommand("callheli " + string.Join(" ", pass_args));
							used_other = true;
						}
					}
					catch
					{
						used_other = false;
					}
					if (used_other == false)
					{
						Puts("Spawning Helicopter");
						prefabName = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
						y_extra_offset = 300.0f;
						vector3_1.y  = vector3_1.y + y_extra_offset;
						var Heli = (BaseHelicopter)GameManager.server.CreateEntity(prefabName, vector3_1, new Quaternion(), true);
						Heli.Spawn();
					}
                    break;
                case EventType.PlaneCrash:
					configData.Events[EventType.PlaneCrash].JustSpawned = true;
					if (PlaneCrash.IsLoaded == true)
					{
						Puts("Spawning Plane Crash");
						rust.RunServerCommand("callcrash " + string.Join(" ", pass_args));
						used_other = true;
					}
                    break;
                case EventType.HelicopterRefuel:
					configData.Events[EventType.HelicopterRefuel].JustSpawned = true;
					if (HeliRefuel.IsLoaded == true)
					{
						Puts("Spawning Heli Refuel");
						rust.RunServerCommand("hr call " + string.Join(" ", pass_args));
						used_other = true;
					}
                    break;
                case EventType.PilotEject:
					configData.Events[EventType.PilotEject].JustSpawned = true;
					if (PilotEject.IsLoaded == true)
					{
						Puts("Spawning Pilot Eject");
						rust.RunServerCommand("pe call " + string.Join(" ", pass_args));
						used_other = true;
					}
                    break;
                case EventType.SantaEvent:
					{
						Puts("Santa is coming, have you been good?");
						rust.RunServerCommand("spawn santasleigh");
						break;
					}
                case EventType.XMasEvent:
					try
					{
						if (AlphaChristmas.IsLoaded == true)
						{
							Puts("Running AlphaChristmas refill");
							rust.RunServerCommand("alphachristmas.refill");
							used_other = true;
						}
					}
					catch
					{
						used_other = false;
					}
					if (used_other == false)
					{
						Puts("Christmas Refill is occuring");
						rust.RunServerCommand("xmas.refill");
					}
                    break;
                case EventType.EasterEvent:
					{
						// Thank you to Death for this command!
						Puts("Happy Easter Egg Hunt is occuring");
						rust.RunServerCommand("spawn egghunt");
						break;
					}
                case EventType.HalloweenEvent:
					{
						Puts("Spooky Halloween Hunt is occuring");
						rust.RunServerCommand("spawn halloweenhunt");
						break;
					}
				default:
					break;
            }
			if (!runonce)
			{
				var config = configData.Events[type];
				StartEventTimer(type,config.AnnounceNext);
			}
        }
        #endregion

        #region Helpers
        private void LoadConfigVariables()
		{
			configData = Config.ReadObject<ConfigData>();
			try
			{
				AnnounceOnLoad = Convert.ToBoolean(configData.Settings["AnnounceOnLoad"]);
				doGUIAnnouncements = Convert.ToBoolean(configData.Settings["UseGUIAnnouncementsPlugin"]);
				RemoveOnStart = Convert.ToBoolean(configData.Settings["RemoveOnStart"]);
				unlimitedBradleys = Convert.ToBoolean(configData.Settings["unlimitedBradleys"]);
				if (doGUIAnnouncements)
				{
					if (GUIAnnouncements.IsLoaded == true)
						doGUIAnnouncements = true;
					else
					{
						doGUIAnnouncements = false;
						configData.Settings["UseGUIAnnouncementsPlugin"] = "false";
						PrintWarning("Warning: GUIAnnouncements plugin was not found! Messages will be sent directly to players.");
					}
				}
			}
			catch
			{
				doGUIAnnouncements = false;
			}
		}

		private void MessagePlayers(string message, string minutes)
		{
			string mess = " ";

			if (doGUIAnnouncements && GUIAnnouncements.IsLoaded)
				foreach (var player in BasePlayer.activePlayerList)
				{
					mess = String.Concat(Lang(message,player.UserIDString, minutes));
					if (!String.IsNullOrWhiteSpace(mess))
						rust.RunServerCommand("announce.announceto "+player.UserIDString.Quote()+ " "+mess.Quote());
				}
			else
				foreach (var player in BasePlayer.activePlayerList)
				{
					mess = String.Concat(Lang(message, player.UserIDString, minutes));
					if (!String.IsNullOrWhiteSpace(mess))
						SendReply(player, mess);
				}
		}

        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        private T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T) Convert.ChangeType(Config[name], typeof (T));

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

		private bool IsAllowedCMD(IPlayer iplayer) {return iplayer != null && (iplayer.IsAdmin || iplayer.HasPermission(permAuto));}
		private bool IsAllowedNext(IPlayer iplayer) {return iplayer != null && (iplayer.IsAdmin || iplayer.HasPermission(permNext));}
        #endregion
    }
}