using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("PrivateMessages", "MisterPixie", "1.1.11")]
    [Description("Allows users to send private messages to each other")]
    class PrivateMessages : CovalencePlugin
    {
        private readonly Dictionary<string, string> pmHistory = new Dictionary<string, string>();
        private Dictionary<string, double> cooldown = new Dictionary<string, double>();
        private List<LastFivePms> lastFivePms = new List<LastFivePms>();
        private const string allowPerm = "privatemessages.allow";

        class LastFivePms
        {
            public string target { get; set; }
            public string sender { get; set; }
            public List<string> messages { get; set; }
        }

        [PluginReference] private Plugin Ignore, UFilter, BetterChatMute;

        #region lang

        private string Lang(string key, string id = null, params object[] args) =>
            string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"PMTo", "[#00FFFF]PM to {0}[/#]: {1}"},
                {"PMFrom", "[#00FFFF]PM from {0}[/#]: {1}"},
                {"PlayerNotOnline", "{0} is not online."},
                {"NotOnlineAnymore", "The last person you was talking to is not online anymore."},
                {"NotMessaged", "You haven't messaged anyone or they haven't messaged you."},
                {"IgnoreYou", "[#FF0000]{0} is ignoring you and cant receive your PMs[/#]"},
                {"SelfPM", "You can not send messages to yourself."},
                {"SyntaxR", "Incorrect Syntax use: /r <msg>"},
                {"HistorySyntax", "Incorrect Syntax use: /pmhistory <name>"},
                {"SyntaxPM", "Incorrect Syntax use: /{0} <name> <msg>"},
                {"NotAllowedToChat", "You are not allowed to chat here"},
                {"History", "Your History:\n{0}"},
                {"CooldownMessage", "You will be able to send a private message in {0} seconds"},
                {"NoHistory", "There is not any saved pm history with this player."},
                {"CannotFindUser", "Cannot find this user"},
                {"CommandDisabled", "This command has been disabled"},
                {"IsMuted", "You are currently muted & cannot send private messages"},
                {"TargetMuted", "This person is muted & cannot receive your private message"},
                {"NoPermission", "You don't have the correct permissions to run this command"},
                {"HistoryPM", "[#00FFFF]{0}[/#]: {1}"},
                {"Logging", "[PM]{0}->{1}:{2}"}
            }, this);
        }

        #endregion

        private void Init()
        {
            LoadVariables();

            if(string.IsNullOrEmpty(configData.PmCommand))
                AddCovalenceCommand("pm", "cmdPm");
            else
                AddCovalenceCommand(configData.PmCommand, "cmdPm");

            AddCovalenceCommand("r", "cmdPmReply");
            AddCovalenceCommand("pmhistory", "cmdPmHistory");
            permission.RegisterPermission(allowPerm, this);
        }

        private void OnUserDisconnected(IPlayer player)
        {
            if (pmHistory.ContainsKey(player.Id))
            {
                pmHistory.Remove(player.Id);
            }
        }

        private void cmdPm(IPlayer player, string command, string[] args)
        {
            if (configData.UsePermission)
            {
                if (!player.HasPermission(allowPerm))
                {
                    player.Reply(Lang("NoPermission", player.Id));
                    return;
                }
            }

            if (args.Length > 1)
            {
                var name = args[0];
                var p = FindPlayer(name);
                if (p != null)
                {
                    if (p.Id == player.Id)
                    {
                        player.Reply(Lang("SelfPM", player.Id));
                        return;
                    }

                    if (!(bool)(Interface.Oxide.CallHook("CanChat", player) ?? true))
                    {
                        player.Reply(Lang("NotAllowedToChat", player.Id));
                        return;
                    }

                    if (configData.UseBetterChatMute && BetterChatMute != null && CheckMuteStatus(player, p))
                    {
                        return;
                    }

                    if (IsCooldowned(player))
                    {
                        return;
                    }

                    if (IsIgnored(player, p))
                    {
                        return;
                    }

                    var msg = RemoveRichText(IsUFilter(args));

                    if (Interface.Oxide.CallHook("OnPMProcessed", player, p, msg) != null)
                    {
                        return;
                    }

                    AddPmHistory(player.Id, p.Id);

                    player.Reply(Lang("PMTo", player.Id, p.Name, msg));
                    p.Reply(Lang("PMFrom", p.Id, player.Name, msg));

                    AddHistoryAndLogging(player, p, msg);
                }
                else
                {
                    player.Reply(Lang("PlayerNotOnline", player.Id, name));
                }
            }
            else
            {
                player.Reply(Lang("SyntaxPM", player.Id, configData.PmCommand));
            }
        }

        private void cmdPmReply(IPlayer player, string command, string[] args)
        {
            if (configData.UsePermission)
            {
                if (!player.HasPermission(allowPerm))
                {
                    player.Reply(Lang("NoPermission", player.Id));
                    return;
                }
            }

            if (args.Length > 0)
            {
                string steamid;
                if (pmHistory.TryGetValue(player.Id, out steamid))
                {
                    var p = FindPlayer(steamid);
                    if (p != null)
                    {
                        if (!(bool)(Interface.Oxide.CallHook("CanChat", player) ?? true))
                        {
                            player.Reply(Lang("NotAllowedToChat", player.Id));
                            return;
                        }

                        if (configData.UseBetterChatMute && BetterChatMute != null && CheckMuteStatus(player, p))
                        {
                            return;
                        }

                        if (IsCooldowned(player))
                        {
                            return;
                        }

                        if (IsIgnored(player, p))
                        {
                            return;
                        }

                        var msg = RemoveRichText(IsUFilter(args, true));

                        if (Interface.Oxide.CallHook("OnPMProcessed", player, p, msg) != null)
                        {
                            return;
                        }

                        AddPmHistory(player.Id, p.Id);

                        player.Reply(Lang("PMTo", player.Id, p.Name, msg));
                        p.Reply(Lang("PMFrom", p.Id, player.Name, msg));

                        AddHistoryAndLogging(player, p, msg);
                    }
                    else
                    {
                        player.Reply(Lang("NotOnlineAnymore", player.Id));
                    }
                }
                else
                {
                    player.Reply(Lang("NotMessaged", player.Id));
                }
            }
            else
            {
                player.Reply(Lang("SyntaxR", player.Id));
            }
        }

        private void AddHistoryAndLogging(IPlayer initiator, IPlayer target, string message)
        {
            if (configData.EnableHistory)
                AddToHistory(initiator.Id, target.Id, Lang("HistoryPM", null, initiator.Name, message));

            if (configData.EnableLogging)
                Puts(Lang("Logging", null, initiator.Name, target.Name, message));
        }

        private void AddPmHistory(string initiatorId, string targetId)
        {
            pmHistory[initiatorId] = targetId;
            pmHistory[targetId] = initiatorId;
        }

        private bool CheckMuteStatus(IPlayer player, IPlayer target)
        {
            if ((bool)BetterChatMute.CallHook("API_IsMuted", player))
            {
                player.Reply(Lang("IsMuted", player.Id));
                return true;
            }

            if ((bool)BetterChatMute.CallHook("API_IsMuted", target))
            {
                player.Reply(Lang("TargetMuted", player.Id));
                return true;
            }

            return false;
        }

        private void cmdPmHistory(IPlayer player, string command, string[] args)
        {
            if (configData.EnableHistory)
            {
                if (args.Length == 0)
                {
                    player.Reply(Lang("HistorySyntax", player.Id));
                    return;
                }

                if (args.Length == 1)
                {
                    var p = covalence.Players.FindPlayer(args[0]);

                    if (p != null)
                    {
                        var History = GetLastFivePms(player.Id, p.Id);

                        if (History == null)
                        {
                            player.Reply(Lang("NoHistory", player.Id));
                            return;
                        }

                        string msg = string.Join(Environment.NewLine, History.messages.ToArray());

                        player.Reply(Lang("History", player.Id, msg));
                    }
                    else
                    {
                        player.Reply(Lang("CannotFindUser", player.Id));
                    }
                }
            }
            else
            {
                player.Reply(Lang("CommandDisabled", player.Id));
            }
        }

        private string RemoveRichText(string message)
        {
            char[] fullArray = new char[message.Length];
            int arrayIndex = 0;
            bool tags = false;
            char c;
            for (int i = 0; i < message.Length; i++)
            {
                c = message[i];
                if (c == '<')
                {
                    tags = true;
                    continue;
                }

                if (c == '>')
                {
                    tags = false;
                    continue;
                }

                if (!tags)
                {
                    fullArray[arrayIndex] = c;
                    arrayIndex++;
                }
            }

            return new string(fullArray, 0, arrayIndex);
        }

        private IPlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in covalence.Players.Connected)
            {
                if (activePlayer.Id == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.Name.Contains(nameOrIdOrIp))
                    return activePlayer;
                if (activePlayer.Name.ToLower().Contains(nameOrIdOrIp.ToLower()))
                    return activePlayer;
                if (activePlayer.Address == nameOrIdOrIp)
                    return activePlayer;
            }

            return null;
        }

        private void AddToHistory(string sender, string target, string msg)
        {
            var value = GetLastFivePms(sender, target);

            if (value == null)
            {
                lastFivePms.Add(new LastFivePms
                {
                    sender = sender,
                    target = target,
                    messages = new List<string>
                    {
                        msg
                    }
                });
            }
            else
            {
                value.messages.Add(msg);

                if (value.messages.Count == 6)
                {
                    value.messages.Remove(value.messages.First());
                }
            }
        }

        private LastFivePms GetLastFivePms(string sender, string target) => lastFivePms.Find(x =>
            x.sender == sender && x.target == target || x.sender == target && x.target == sender);

        private bool IsIgnored(IPlayer sender, IPlayer target)
        {
            if (configData.UseIgnore)
            {
                var hasIgnore = Ignore?.CallHook("HasIgnored", target.Id, sender.Id);

                if (hasIgnore != null && (bool)hasIgnore)
                {
                    sender.Reply(Lang("IgnoreYou", sender.Id, target.Name));
                    return true;
                }
            }

            return false;
        }

        private string IsUFilter(string[] args, bool isR = false)
        {
            string message = string.Join(" ", args.Skip(1).ToArray());

            if (isR)
                message = string.Join(" ", args.Skip(0).ToArray());

            if (configData.UseUFilter)
            {
                var hasUFilter = (object)UFilter?.Call("ProcessText", message);

                if (hasUFilter != null)
                {
                    message = hasUFilter.ToString();
                }
            }

            return message;
        }

        private bool IsCooldowned(IPlayer player)
        {
            if (configData.UseCooldown)
            {
                double time;
                if (cooldown.TryGetValue(player.Id, out time))
                {
                    if (time > GetTimeStamp())
                    {
                        player.Reply(Lang("CooldownMessage", player.Id, Math.Round(time - GetTimeStamp(), 2)));
                        return true;
                    }

                    cooldown.Remove(player.Id);
                }

                cooldown.Add(player.Id, GetTimeStamp() + configData.CooldownTime);
            }

            return false;
        }

        private double GetTimeStamp()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        #region Config

        private ConfigData configData;

        private class ConfigData
        {
            public bool UseUFilter;
            public bool UseIgnore;
            public bool UseCooldown;
            public bool UseBetterChatMute;
            public bool EnableLogging;
            public bool EnableHistory;
            public bool UsePermission;
            public int CooldownTime;
            public string PmCommand;
        }

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                UseIgnore = false,
                UseCooldown = false,
                UseUFilter = false,
                UsePermission = false,
                UseBetterChatMute = false,
                EnableLogging = true,
                EnableHistory = false,
                CooldownTime = 3,
                PmCommand = "pm"
            };
            SaveConfig(config);
        }

        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        #endregion
    }
}