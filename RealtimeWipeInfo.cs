// Reference: Facepunch.Sqlite

using System;
using System.Collections.Generic;
using ConVar;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Realtime Wipe Info", "Ryan", "2.1.63")]
    public class RealtimeWipeInfo : RustPlugin
    {
        #region Declaration

        // Plugin references
        [PluginReference] private Plugin BetterChat, ColouredChat;

        // Datetimes
        private DateTime _cachedWipeTime;

        // Permissions
        private const string BypassPerm = "realtimewipeinfo.chatbypass";

        // Timers
        private Timer _descriptionTimer;
        private Timer _titleTimer;

        // Configuration and data
        private static ConfigFile _config;
        private static DataFile _data;

        // Instance
        //private static RealtimeWipeInfo _instance;

        // Other variables
        private bool _newConfig;

        #endregion

        #region Configuration

        private class ConfigFile
        {
            [JsonProperty("Description Settings")]
            public DescriptionSettings Description;

            [JsonProperty("Title Settings")]
            public TitleSettings Title;

            [JsonProperty("Phrase Settings")]
            public PhraseSettings Phrase;

            [JsonProperty("Connect Message Settings")]
            public ConnectSettings Connect;

            [JsonProperty("Command Settings")]
            public CommandSettings Command;

            [JsonProperty("Blueprint settings")]
            public BlueprintSettings Blueprint;

            public VersionNumber Version;

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    Description = new DescriptionSettings
                    {
                        Enabled = false,
                        Description = "Your description here. Put {0} where you want plugin info to go",
                        UseTime = true,
                        Date = new Date
                        {
                            Enabled = true,
                            Format = "dddd d/M"
                        },
                        Refresh = 120
                    },
                    Title = new TitleSettings
                    {
                        Enabled = false,
                        Title = "Your title here. Put {0} where you want plugin info to go",
                        UseTime = true,
                        Date = new Date
                        {
                            Enabled = true,
                            Format = "d/M"
                        },
                        Refresh = 60
                    },
                    Phrase = new PhraseSettings
                    {
                        Enabled = false,
                        Phrases = new Dictionary<string, PhraseItem>
                        {
                            ["wipe"] = new PhraseItem(true, true),
                            ["wipe?"] = new PhraseItem(true, false),
                            ["wiped"] = new PhraseItem(false, true),
                            ["wiped?"] = new PhraseItem(false, false)
                        },
                        UseTime = true,
                        Date = new Date
                        {
                            Enabled = true,
                            Format = "d/M"
                        },
                        Schedule = new ScheduleSettings
                        {
                            Enabled = true,
                            Schedule = 7,
                            Format = "dddd d/M"
                        }
                    },
                    Connect = new ConnectSettings
                    {
                        Enabled = false
                    },
                    Command = new CommandSettings()
                    {
                        Enabled = true,
                        Command = "wipe"
                    },
                    Blueprint = new BlueprintSettings()
                };
            }
        }

        #region Config Classes

        private class DescriptionSettings
        {
            [JsonProperty("Enable Description")]
            public bool Enabled;

            [JsonProperty("Full Server Description")]
            public string Description;

            [JsonProperty("Include Seed & Map Size")]
            public bool SeedSize;

            [JsonProperty("Enable Use Of Time")]
            public bool UseTime;

            public Date Date;

            [JsonProperty("Refresh Interval")]
            public float Refresh;
        }

        private class TitleSettings
        {
            [JsonProperty("Enable Title")]
            public bool Enabled;

            [JsonProperty("Full Server Hostname")]
            public string Title;

            [JsonProperty("Enable Use Of Time")]
            public bool UseTime;

            public Date Date;

            [JsonProperty("Refresh Interval")]
            public float Refresh;
        }

        private class PhraseSettings
        {
            [JsonProperty("Enable Phrases")]
            public bool Enabled;

            public Dictionary<string, PhraseItem> Phrases;

            [JsonProperty("Enable Use Of Time")]
            public bool UseTime;

            public Date Date;

            [JsonProperty("Schedule Settings")]
            public ScheduleSettings Schedule;
        }

        private class ConnectSettings
        {
            [JsonProperty("Enable Connect Messages")]
            public bool Enabled;
        }

        private class CommandSettings
        {
            public bool Enabled;
            public string Command;
        }

        private class Date
        {
            [JsonProperty("Enable Use Of Date")]
            public bool Enabled;

            [JsonProperty("Date format")]
            public string Format;
        }

        private class ScheduleSettings
        {
            [JsonProperty("Enable Wipe Schedule Messages")]
            public bool Enabled;

            [JsonProperty("Wipe Schedule In Days")]
            public int Schedule;

            [JsonProperty("Date Format")]
            public string Format;
        }

        private class PhraseItem
        {
            [JsonProperty("Send Reply")]
            public bool Message;

            [JsonProperty("Block Message")]
            public bool Block;

            public PhraseItem(bool message, bool block)
            {
                Message = message;
                Block = block;
            }
        }

        private class BlueprintSettings
        {
            [JsonProperty("Enable blueprint wipe tracking")]
            public bool Enabled;

            [JsonProperty("Add BP wipe to description")]
            public bool UseDescription;

            [JsonProperty("Use BP chat reply")]
            public bool UseChat;

            public BlueprintSettings()
            {
                Enabled = true;
                UseDescription = true;
                UseChat = true;
            }
        }

        #endregion

        protected override void LoadDefaultConfig()
        {
            PrintWarning($"All values are disabled by default, set them up at oxide/config/{Name}.json!");
            _newConfig = true;
            _config = ConfigFile.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigFile>();
                if (_config == null)
                {
                    Regenerate();
                    return;
                }
                if (_config.Blueprint == null)
                {
                    _config.Blueprint = new BlueprintSettings();
                    SaveConfig();
                }
            }
            catch { Regenerate(); }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void Regenerate()
        {
            PrintWarning($"Configuration file at 'oxide/config/{Name}.json' seems to be corrupt! Regenerating...");
            _config = ConfigFile.DefaultConfig();
            SaveConfig();
        }

        #endregion

        #region Lang

        private struct Msg
        {
            public const string TitleDay = "TitleDay";
            public const string TitleDays = "TitleDays";
            public const string TitleHour = "TitleHour";
            public const string TitleHours = "TitleHours";
            public const string TitleMinutes = "TitleMinutes";
            public const string DescLastWipe = "DescLastWipe";
            public const string DescNextWipe = "DescNextWipe";
            public const string DescSeedSize = "DescSeedSize";
            public const string MsgTime = "MsgTime";
            public const string MsgDate = "MsgDate";
            public const string MsgDateTime = "MsgDateTime";
            public const string MsgNextWipe = "MsgNextWipe";
            public const string DescBpWipe = "DescBpWipe";
            public const string DescMsgBpWipe = "DescMsgBpWipe";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Msg.TitleDay] = "{0} day ago",
                [Msg.TitleDays] = "{0} days ago",
                [Msg.TitleHour] = "{0} hour ago",
                [Msg.TitleHours] = "{0} hrs ago",
                [Msg.TitleMinutes] = "{0} mins ago",
                [Msg.DescLastWipe] = "The last wipe was on {0}",
                [Msg.DescNextWipe] = "The next wipe will be on {0} ({1} day wipe schedule)",
                [Msg.DescSeedSize] = "The map size is {0} and the seed is {1}",
                [Msg.MsgTime] = "The last wipe was {0} ago",
                [Msg.MsgDate] = "The last wipe was on {0}",
                [Msg.MsgDateTime] = "The last wipe was on {0} ({1} ago)",
                [Msg.MsgNextWipe] = "The next wipe will be on <color=orange>{0}</color> (<color=orange>{1}</color> day wipe schedule)",
                ["DayFormat"] = "<color=orange>{0}</color> day and <color=orange>{1}</color> hours",
                ["DaysFormat"] = "<color=orange>{0}</color> days and <color=orange>{1}</color> hours",
                ["HourFormat"] = "<color=orange>{0}</color> hour and <color=orange>{1}</color> minutes",
                ["HoursFormat"] = "<color=orange>{0}</color> hours and <color=orange>{1}</color> minutes",
                ["MinFormat"] = "<color=orange>{0}</color> minute and <color=orange>{1}</color> seconds",
                ["MinsFormat"] = "<color=orange>{0}</color> minutes and <color=orange>{1}</color> seconds",
                ["SecsFormat"] = "<color=orange>{0}</color> seconds",
                [Msg.DescBpWipe] = "(BP wiped {0})",
                [Msg.DescMsgBpWipe] = "(Blueprints wiped <color=orange>{0}</color>)"
            }, this);
        }

        #endregion

        #region Data

        private class DataFile
        {
            public string Hostname;
            public string Description;
            public DateTime BlueprintWipe;

            public DataFile()
            {
                Hostname = "";
                Description = "";
                BlueprintWipe = DateTime.MinValue;
            }

            public DataFile(string hostname, string description)
            {
                Hostname = hostname;
                Description = description;
            }
        }

        #endregion

        #region Methods

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private string GetFormattedTime(double time)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(time);
            if (timeSpan.TotalSeconds < 1) 
			{
				return null;
			}

            if (Math.Floor(timeSpan.TotalDays) >= 1)
			{
				return string.Format(timeSpan.Days > 1 ? Lang("DaysFormat", null, timeSpan.Days, timeSpan.Hours) : Lang("DayFormat", null, timeSpan.Days, timeSpan.Hours));
			}
			
            if (Math.Floor(timeSpan.TotalMinutes) >= 60)
			{
				return string.Format(timeSpan.Hours > 1 ? Lang("HoursFormat", null, timeSpan.Hours, timeSpan.Minutes) : Lang("HourFormat", null, timeSpan.Hours, timeSpan.Minutes));
			}
			
            if (Math.Floor(timeSpan.TotalSeconds) >= 60)
			{
				return string.Format(timeSpan.Minutes > 1 ? Lang("MinsFormat", null, timeSpan.Minutes, timeSpan.Seconds) : Lang("MinFormat", null, timeSpan.Minutes, timeSpan.Seconds));
			}
			
            return Lang("SecsFormat", null, timeSpan.Seconds);
        }

        #region Title Methods

        private void ApplyTitle(string title) => ConVar.Server.hostname = string.Format(_config.Title.Title, title);

        private void StartTitleRefresh()
        {
            ApplyTitle(GetFormattedTitle());
            timer.Every(_config.Title.Refresh, () =>
            {
                ApplyTitle(GetFormattedTitle());
            });
        }

        private string GetFormattedTitleTime()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds((DateTime.UtcNow.ToLocalTime() - _cachedWipeTime).TotalSeconds);
            if (timeSpan.TotalSeconds < 1) 
			{
				return null;
			}

            if (Math.Floor(timeSpan.TotalDays) >= 1)
			{
				return string.Format(timeSpan.Days > 1 ? Lang(Msg.TitleDays, null, timeSpan.Days) : Lang(Msg.TitleDay, null, timeSpan.Days));
			}
			
            if (Math.Floor(timeSpan.TotalMinutes) >= 60)
			{
				return string.Format(timeSpan.Hours > 1 ? Lang(Msg.TitleHours, null, timeSpan.Hours) : Lang(Msg.TitleHour, null, timeSpan.Hours));
			}
			
            return Lang(Msg.TitleMinutes, null, timeSpan.Minutes);
        }

        private string GetFormattedTitle()
        {
            if (_config.Title.UseTime && !_config.Title.Date.Enabled)
            {
                return GetFormattedTitleTime();
            }
			
            if (_config.Title.Date.Enabled && !_config.Title.UseTime)
            {
                string nowdate = (DateTime.Now).ToString(_config.Title.Date.Format);
                string date = _cachedWipeTime.ToString(_config.Title.Date.Format);
                Puts(nowdate);
                Puts(date);
                return (nowdate.Equals(date) ? $"Just Wiped " : $"Wiped ") + date;
            }
			
            if (_config.Title.Date.Enabled && _config.Title.UseTime)
            {
                return _cachedWipeTime.ToString(_config.Title.Date.Format) + " " + GetFormattedTitleTime();
            }
			
            return string.Empty;
        }

        #endregion

        #region Description Methods

        private void ApplyDescription(string description) => ConVar.Server.description = description;

        private void StartDescriptionRefresh()
        {
            ApplyDescription(GetFormattedDescription());
            timer.Every(_config.Description.Refresh, () =>
            {
                ApplyDescription(GetFormattedDescription());
            });
        }

        private string GetFormattedDescription()
        {
            var output = "";
            if (_config.Phrase.Schedule.Enabled)
            {
                output = string.Format(Lang(Msg.DescLastWipe, null, _cachedWipeTime.ToString(_config.Description.Date.Format)) + "\n" +
                    Lang(Msg.DescNextWipe, null, _cachedWipeTime.AddDays(_config.Phrase.Schedule.Schedule)
                    .ToString(_config.Description.Date.Format), _config.Phrase.Schedule.Schedule));
            }
            else
            {
                output = Lang(Msg.DescLastWipe, null, _cachedWipeTime.ToString(_config.Description.Date.Format));
            }
			
            if (_config.Description.SeedSize)
            {
                output += "\n" + Lang(Msg.DescSeedSize, null, ConVar.Server.worldsize, ConVar.Server.seed);
            }
			
            if (_config.Blueprint.Enabled)
            {
                output += " " + Lang(Msg.DescBpWipe, null, _data.BlueprintWipe.ToLocalTime().ToString(_config.Description.Date.Format));
            }
			
            return string.Format(_config.Description.Description, output);
        }

        #endregion

        #region Phrase Methods

        private object ChatMessageResult(BasePlayer player, string input, bool reply)
        {
            if (!_config.Phrase.Enabled) return null;
            foreach (var phrase in _config.Phrase.Phrases)
            {
                if (input.ToLower().Contains(phrase.Key.ToLower()))
                {
                    if (phrase.Value.Message && reply)
                    {
                        PrintToChat(player, GetFormattedMessage(player));
                    }
                    if (phrase.Value.Block)
                    {
                        return false;
                    }
                    //return null;
                }
            }
            return null;
        }

        private string GetFormattedMessageTime() => GetFormattedTime((DateTime.UtcNow.ToLocalTime() - _cachedWipeTime).TotalSeconds);

        private string GetFormattedMessage(BasePlayer player)
        {
            var addition = string.Empty;
            if (_config.Blueprint.Enabled) 
			{
				addition = " " + Lang(Msg.DescMsgBpWipe, player.UserIDString, _data.BlueprintWipe.ToLocalTime().ToString(_config.Phrase.Date.Format));
			}
			
            if (_config.Phrase.UseTime && !_config.Phrase.Date.Enabled)
            {
                var output = Lang(Msg.MsgTime, player.UserIDString, GetFormattedMessageTime());
                if (_config.Phrase.Schedule.Enabled)
				{
					output += "\n" + Lang(Msg.MsgNextWipe, player.UserIDString, _cachedWipeTime.AddDays(_config.Phrase.Schedule.Schedule).ToString(_config.Phrase.Schedule.Format),
                                  _config.Phrase.Schedule.Schedule);
				}				
                return output + addition;
            }
			
            if (_config.Phrase.Date.Enabled && !_config.Phrase.UseTime)
            {
                var output = Lang(Msg.MsgDate, player.UserIDString, _cachedWipeTime.ToString(_config.Phrase.Date.Format));
                if (_config.Phrase.Schedule.Enabled)
                {
                    output += Lang(Msg.MsgNextWipe, player.UserIDString, _cachedWipeTime.AddDays(_config.Phrase.Schedule.Schedule).ToString(_config.Phrase.Schedule.Format),
                        _config.Phrase.Schedule.Schedule);
                }
                return output + addition;
            }
			
            if (_config.Phrase.Date.Enabled && _config.Phrase.UseTime)
            {
                var output = Lang(Msg.MsgDateTime, player.UserIDString, _cachedWipeTime.ToString(_config.Phrase.Date.Format), GetFormattedMessageTime());
                if (_config.Phrase.Schedule.Enabled)
                {
                    output += "\n" + Lang(Msg.MsgNextWipe, player.UserIDString, _cachedWipeTime.AddDays(_config.Phrase.Schedule.Schedule).ToString(_config.Phrase.Schedule.Format),
                                  _config.Phrase.Schedule.Schedule);
                }
                return output + addition;
            }
			
            return null;
        }

        #endregion

        private void OnBpsWiped()
        {
            PrintWarning("Blueprint wipe detected!");
            Interface.Oxide.CallHook("OnUsersCleared");
            Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
        }

        #endregion

        #region Hooks

        private void Init()
        {
			Unsubscribe("OnPluginLoaded");
			Unsubscribe("OnPluginUnloaded");
			
            _data = Interface.Oxide.DataFileSystem.ReadObject<DataFile>(Name);
            permission.RegisterPermission(BypassPerm, this);
            cmd.AddChatCommand(_config.Command.Command, this, WipeCommand);
            //_instance = this;

            if (_config.Phrase.Enabled && BetterChat && BetterChat.Version < new VersionNumber(5, 1, 0))
            {
                PrintWarning("This plugin is only compatible with BetterChat version 5.1.0 or greater!");
                Unsubscribe("OnBetterChat");
            }

			if (_config.Phrase.Enabled && ColouredChat)
            {
				if (ColouredChat.Version < new VersionNumber(1, 4, 3))
				{
					PrintWarning("This plugin is only compatible with ColouredChat version 1.4.3 or greater!");
					Unsubscribe("OnColouredChat");
				}
                else
				{
					// Resolve the hook conflict
					Unsubscribe("OnPlayerChat");
				}
            }
			
            if (_newConfig)
            {
                PrintWarning("Saved your current hostname and description to apply at a later date if needed");
                _data = new DataFile(ConVar.Server.hostname, ConVar.Server.description);
                Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
            }

            if (_config.Version == default(VersionNumber))
            {
                _config.Version = Version;
                SaveConfig();
            }
        }

        private void OnServerInitialized()
        {
            _cachedWipeTime = SaveRestore.SaveCreatedTime.ToLocalTime();
			
            if (_config.Blueprint.Enabled)
            {
                var blueprints = UserPersistance.blueprints;
                var playerCount = blueprints?.QueryInt("SELECT COUNT(*) FROM data");
				
                if (playerCount != null && playerCount == 0)
                {
                    _data.BlueprintWipe = DateTime.UtcNow;
                    OnBpsWiped();
                }
				
                if (_data.BlueprintWipe == null || _data.BlueprintWipe == DateTime.MinValue)
                {
                    _data.BlueprintWipe = SaveRestore.SaveCreatedTime;
                    OnBpsWiped();
                }
            }
			
            if (!_config.Phrase.Enabled)
            {
                Unsubscribe("OnPlayerChat");
                Unsubscribe("OnBetterChat");
				Unsubscribe("OnColouredChat");
            }
			
            if (!_config.Connect.Enabled)
            {
                Unsubscribe("OnPlayerInit");
            }
			
            if (_config.Description.Enabled)
            {
                if (_config.Description.UseTime)
				{
					StartDescriptionRefresh();
				}
                else
				{
					ApplyDescription(GetFormattedDescription());
				}
            }
			
            if (_config.Title.Enabled)
            {
                if (_config.Title.UseTime)
				{
					StartTitleRefresh();
				}
                else
				{
					ApplyTitle(GetFormattedTitle());
				}
            }
			
			// Subscribe to these hooks if the phrases are enabled (the hooks are required for 3rd party plugins)
			if (_config.Phrase.Enabled)
			{
				Subscribe("OnPluginLoaded");
				Subscribe("OnPluginUnloaded");
			}			
        }

        private void Unload()
        {
            _titleTimer?.Destroy();
            _descriptionTimer?.Destroy();

           //if (!ConVar.Admin.ServerInfo().Restarting)
           //{
           //    PrintWarning($"Setting servers hostname and description to the originally stored ones in oxide/data/{Name}.json");
           //
           //    if (_config.Title.Enabled)
           //    {
           //        ConVar.Server.hostname = _data.Hostname;
           //    }
           //
           //    if (_config.Description.Enabled)
           //    {
           //        ConVar.Server.description = _data.Description;
           //    }
           //}

            //_instance = null;
        }

		private void OnPluginUnloaded(Plugin plugin)
		{
			// Fix if ColouredChat is unloaded after this one is loaded
			if (plugin == ColouredChat)
			{
				Unsubscribe("OnColouredChat");
				Subscribe("OnPlayerChat");
				return;
			}
		}
		
		private void OnPluginLoaded(Plugin plugin)
		{
			if (plugin == ColouredChat)
			{
				// Resolve the hook call conflict if the ColouredChat plugin is loaded after this one
				Unsubscribe("OnPlayerChat");
				Subscribe("OnColouredChat");
				return;
			}
		}
		
        private void OnPlayerInit(BasePlayer player)
        {
            timer.Once(3, () =>
            {
                PrintToChat(player, GetFormattedMessage(player));
            });
        }

        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (!permission.UserHasPermission(player.UserIDString, BypassPerm) && !player.IsAdmin)
            {
                return ChatMessageResult(player, message, true);
            }
            return null;
        }
		
		private object OnColouredChat(Dictionary<string, object> data)
        {
            var player = (IPlayer)data["Player"];
            if (!player.HasPermission(BypassPerm) && !player.IsAdmin)
            {
                return ChatMessageResult((BasePlayer)player.Object, data["Message"].ToString(), false);
            }
            return null;
        }

        private object OnBetterChat(Dictionary<string, object> data)
        {
            var player = (IPlayer)data["Player"];
            if (!player.HasPermission(BypassPerm) && !player.IsAdmin)
            {
                return ChatMessageResult((BasePlayer)player.Object, data["Message"].ToString(), false);
            }
            return null;
        }

        #endregion

        #region Commands

        private void WipeCommand(BasePlayer player, string command, string[] args)
        {
            PrintToChat(player, GetFormattedMessage(player));
        }

        #endregion
    }
}
