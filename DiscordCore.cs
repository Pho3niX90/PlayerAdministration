﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.DiscordEvents;
using Oxide.Ext.Discord.DiscordObjects;
using DiscordUser = Oxide.Ext.Discord.DiscordObjects.User;

namespace Oxide.Plugins
{
    [Info("Discord Core", "MJSU", "0.14.6")]
    [Description("Sets up a channel link between a user and a game server")]
    internal class DiscordCore : CovalencePlugin
    {
        #region Class Fields
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config

        [DiscordClient] private DiscordClient _client;

        private const string AccentColor = "de8732";
        private const string UsePermission = "discordcore.use";
        private const string PluginsPermission = "discordcore.plugins";

        private readonly Hash<string, DiscordCommand> _discordCommands = new Hash<string, DiscordCommand>();
        private readonly Hash<string, List<ChannelSubscription>> _channelSubscriptions = new Hash<string, List<ChannelSubscription>>();

        private readonly List<DiscordActivation> _pendingDiscordActivations = new List<DiscordActivation>();
        private readonly Hash<string, InGameActivation> _pendingInGameActivation = new Hash<string, InGameActivation>();

        private readonly List<Plugin> _hookPlugins = new List<Plugin>();

        private DiscordUser _bot;
        private Guild _discordServer;

        private enum ConnectionStateEnum : byte { Disconnected, Connecting, Connected }
        private ConnectionStateEnum _connectionState = ConnectionStateEnum.Disconnected;
        private DateTime _lastUpdate;
        private bool _initialized;
        private Timer _connectionTimeout;
        private int _guildCount;
        #endregion

        #region Setup & Loading
        private void Init()
        {
            ConfigLoad();

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);

            permission.RegisterPermission(UsePermission, this);
            permission.RegisterPermission(PluginsPermission, this);

            AddCovalenceCommand(_pluginConfig.GameChatCommand, nameof(DiscordChatCommand));
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        private void ConfigLoad()
        {
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = Config.ReadObject<PluginConfig>();
            Config.WriteObject(_pluginConfig);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.NoPermission] = "You do not have permission to use this command",
                [LangKeys.ChatFormat] = $"[#BEBEBE][[#{AccentColor}]{Title}[/#]] {{0}}[/#]",
                [LangKeys.UnknownCommand] = "Unknown Command",
                [LangKeys.DiscordLeave] = "You have removed your discord bot connection",
                [LangKeys.DiscordLeaveFailed] = "You are not subscribed to discord and were not removed",
                [LangKeys.JoinAlreadySignedUp] = $"You're already signed up for discord. If you want to remove yourself from discord type [#{AccentColor}]/{{0}} leave[/#]",
                [LangKeys.JoinInvalidSyntax] = $"Invalid Syntax. Ex. [#{AccentColor}]/{{0}} join name#1234[/#] to sign up for discord",
                [LangKeys.JoinUnableToFindUser] = $"Unable to find user '{{0}}' in the {{1}} discord. Have you joined the {{1}} discord server @ [#{AccentColor}]discord.gg/{{2}}[/#]?",
                [LangKeys.JoinReceivedPm] = $"You have received a PM from the {{0}}. Please respond to the bot with your code to complete the discord activation.\n[#{AccentColor}]{{1}}[/#]",
                [LangKeys.JoinPleaseEnterCode] = "Please enter your code here to complete activation.",
                [LangKeys.DiscordJoinInvalidSyntax] = $"Invalid syntax. Type [#{AccentColor}]/{{0}} code 123456[/#] where 123456 is the code you got from discord",
                [LangKeys.DiscordJoinNoPendingActivations] = "You either don't have a pending activation or your code is invalid",
                [LangKeys.DiscordJoinSuccessfullyRegistered] = "You have successfully registered your discord with the server",
                [LangKeys.DiscordCommandsPreText] = "Available Commands:",
                [LangKeys.DiscordCommandsBotChannelAllowed] = "(Bot Channel Allowed)",
                [LangKeys.DiscordPlugins] = "List of Discord enabled plugins:\n",
                [LangKeys.DiscordJoinWrongChannel] = "Please use /{0} in this private message and not to any discord server channels",
                [LangKeys.DiscordUseCommandsHere] = "Please use all commands here.",
                [LangKeys.DiscordPrivateChannel] = "Please use all commands in the bot private message or in the bot channel",
                [LangKeys.DiscordNotAllowedBotChannel] = "This command is not allowed in the bot channel and can only be used here.",
                [LangKeys.DiscordMustSignUpBeforeCommands] = "You need to sign up for discord before you can start using commands. To begin type /{0}",
                [LangKeys.DiscordClientNotConnected] = "The discord client is not connected. Please contact an admin.",
                [LangKeys.GameJoinCodeNotMatch] = "Your activation code does not match! Please reply with only the code.",
                [LangKeys.DiscordCompleteActivation] = "To complete your discord activation in the server chat please type /{0} code {1}",


                [LangKeys.DiscordUserJoin] = "If you would like to link your discord account with the {0} server respond to this message with /{1}. " +
                                      "This provides you with a bot which gives you access to commands while not on the server. Once you sign up type /{2} to learn more",
                [LangKeys.HelpNotLinked] = "Discord: Allows communication between a server and discord.\n" +
                               "Type /{0} - (In a private message to the bot) - to start linking your server player and discord together\n" +
                               "Type /{1} - to see this message again",
                [LangKeys.HelpLinked] = "Discord: Allows communication between a server and discord.\n" +
                                  "Type /{0} - (In a private message to the bot) - to see the list of available commands\n" +
                                  "Type /{1} - (In a private message to the bot) - to unlink your discord from the game server\n" +
                                  "Type /{2} - to see this message again",

                [LangKeys.HelpText] = $"Allows players to link their player and discord accounts together. Players must first join the {{0}} Discord @ [#{AccentColor}]discord.gg/{{1}}[/#]\n" +
                                      $"Type [#{AccentColor}]/{{2}} join discordusername#discorduserid[/#] to start the activation of discord\n" +
                                      $"Type [#{AccentColor}]/{{2}} leave[/#] to remove yourself from discord\n" +
                                      $"Type [#{AccentColor}]/{{2}}[/#] to see this message again"
            }, this);
        }
        
        private void OnServerInitialized()
        {
            ConnectClient();
            _lastUpdate = DateTime.Now;
            timer.Every(60, () =>
            {
                if (_connectionState != ConnectionStateEnum.Connecting && (DateTime.Now - _lastUpdate).TotalSeconds > 120)
                {
                    PrintWarning("Heartbeat timed out. Reconnecting");
                    CloseClient();
                    ConnectClient();
                }
            });
        }

        private void Unload()
        {
            CloseClient();
        }

        private void ConnectClient()
        {
            if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
            {
                PrintWarning("Please enter your discord bot API key and reload the plugin");
                return;
            }
            
            _connectionState = ConnectionStateEnum.Connecting;
            timer.In(5f, () =>
            {
                Discord.CreateClient(this, _pluginConfig.DiscordApiKey); // Create a new DiscordClient
                timer.In(60f, () =>
                {
                    if (_connectionState == ConnectionStateEnum.Connecting)
                    {
                        _connectionState = ConnectionStateEnum.Disconnected;
                    }
                });
            });
        }

        private void CloseClient()
        {
            _connectionState = ConnectionStateEnum.Disconnected;
            Discord.CloseClient(_client);
        }

        private void OnPluginUnloaded(Plugin plugin)
        {
            foreach (KeyValuePair<string, DiscordCommand> commands in _discordCommands.Where(dc => dc.Value.PluginName == plugin.Name).ToList())
            {
                UnregisterCommand(commands.Key, plugin);
            }

            foreach (KeyValuePair<string, List<ChannelSubscription>> channelSubscription in _channelSubscriptions)
            {
                if (channelSubscription.Value.Any(cs => cs.PluginName == plugin.Name))
                {
                    UnsubscribeChannel(channelSubscription.Key, plugin);
                }
            }

            if (_hookPlugins.Contains(plugin))
            {
                _hookPlugins.Remove(plugin);
                _client.Plugins.Remove(plugin);
            }
        }

        private void OnUserConnected(IPlayer player)
        {
            DiscordInfo info = _storedData.PlayerDiscordInfo[player.Id];
            if (info == null)
            {
                return;
            }

            info.DisplayName = player.Name;
            NextTick(SaveData);
        }
        #endregion

        #region Chat Commands
        private void DiscordChatCommand(IPlayer player, string cmd, string[] args)
        {
            if (!player.HasPermission(UsePermission) && !player.IsAdmin)
            {
                Chat(player, Lang(LangKeys.NoPermission, player));
                return;
            }

            if (args.Length == 0)
            {
                DisplayHelp(player);
                return;
            }

            switch (args[0].ToLower())
            {
                case "join":
                    HandleJoin(player, args);
                    break;

                case "code":
                    HandleDiscordJoinCode(player, args);
                    break;

                case "leave":
                    if (_storedData.PlayerDiscordInfo.ContainsKey(player.Id))
                    {
                        _storedData.PlayerDiscordInfo.Remove(player.Id);
                        SaveData();
                        Chat(player, Lang(LangKeys.DiscordLeave, player));
                        Interface.Call("OnDiscordCoreLeave", player);
                    }
                    else
                    {
                        Chat(player, Lang(LangKeys.DiscordLeaveFailed, player));
                    }
                    break;

                default:
                    DisplayHelp(player);
                    break;
            }
        }

        private void DisplayHelp(IPlayer player)
        {
            Chat(player, Lang(LangKeys.HelpText, player, GetDiscordName(), _pluginConfig.JoinCode, _pluginConfig.GameChatCommand));
        }

        private void HandleJoin(IPlayer player, string[] args)
        {
            if (_storedData.PlayerDiscordInfo.ContainsKey(player.Id))
            {
                Chat(player, Lang(LangKeys.JoinAlreadySignedUp, player, _pluginConfig.GameChatCommand));
                return;
            }

            if (args.Length < 2)
            {
                Chat(player, Lang(LangKeys.JoinInvalidSyntax, player, _pluginConfig.GameChatCommand));
                return;
            }

            if (_client == null)
            {
                Chat(player, Lang(LangKeys.DiscordClientNotConnected, player));
                return;
            }

            DiscordUser user = GetUserByUsername(args[1]);
            if (user == null)
            {
                Chat(player, Lang(LangKeys.JoinUnableToFindUser, player, args[1], GetDiscordName(), _pluginConfig.JoinCode));
                return;
            }

            if (_storedData.PlayerDiscordInfo.Values.Any(di => di.DiscordId == user.id))
            {
                Chat(player, Lang(LangKeys.JoinAlreadySignedUp, player, _pluginConfig.GameChatCommand));
                return;
            }

            string code = Core.Random.Range(0, 1000000).ToString("D6");
            _pendingInGameActivation[user.id] = new InGameActivation
            {
                Code = code,
                PlayerId = player.Id
            };

            Chat(player, Lang(LangKeys.JoinReceivedPm, player, _bot.username, code));
            CreateChannelActivation(player, user, code, Lang(LangKeys.JoinPleaseEnterCode, player));
        }

        private void HandleDiscordJoinCode(IPlayer player, string[] args)
        {
            if (_storedData.PlayerDiscordInfo.ContainsKey(player.Id))
            {
                Chat(player, Lang(LangKeys.JoinAlreadySignedUp, player, _pluginConfig.GameChatCommand));
                return;
            }

            if (args.Length < 2)
            {
                Chat(player, Lang(LangKeys.DiscordJoinInvalidSyntax, player, _pluginConfig.GameChatCommand));
                return;
            }

            DiscordActivation act = _pendingDiscordActivations.FirstOrDefault(a => a.Code == args[1]);
            if (act == null)
            {
                Chat(player, Lang(LangKeys.DiscordJoinNoPendingActivations, player));
                return;
            }

            _storedData.PlayerDiscordInfo[player.Id] = new DiscordInfo
            {
                DisplayName = player.Name,
                PlayerId = player.Id,
                DiscordId = act.DiscordId,
                ChannelId = act.ChannelId
            };

            SaveData();
            SendMessageToUser(player.Id, Lang(LangKeys.DiscordJoinSuccessfullyRegistered, player));
            Chat(player, Lang(LangKeys.DiscordJoinSuccessfullyRegistered, player));
            Interface.Call("OnDiscordCoreJoin", player);
        }
        #endregion

        #region Internal Discord Commands
        private object HandleHelp(IPlayer player, string channelId, string cmd, string[] args)
        {
            string message;
            if (player == null)
            {
                message = Lang(LangKeys.HelpNotLinked, null, _pluginConfig.DiscordJoinCommand, _pluginConfig.HelpCommand);
            }
            else
            {
                message = Lang(LangKeys.HelpLinked, player, _pluginConfig.CommandsCommand, _pluginConfig.LeaveCommand, _pluginConfig.HelpCommand);
            }
            
            SendMessageToChannel(channelId, message);
            return null;
        }

        private object HandleCommands(IPlayer player, string channelId, string cmd, string[] args)
        {
            string commands = $"Discord: {Lang(LangKeys.DiscordCommandsPreText, player)}\n";
            foreach (KeyValuePair<string, DiscordCommand> command in _discordCommands)
            {
                if (!string.IsNullOrEmpty(command.Value.Permission) && !player.HasPermission(command.Value.Permission))
                {
                    continue;
                }

                string commandText = $"/{command.Key}";
                string botChannel = $" {(_pluginConfig.EnableBotChannel && command.Value.AllowInBotChannel ? Lang(LangKeys.DiscordCommandsBotChannelAllowed, player) : "")}";
                string helpText = lang.GetMessage(command.Value.HelpText, command.Value.Plugin, player.Id);

                commands += $"{commandText}{botChannel} - {helpText}\n";
            }

            SendMessageToChannel(channelId, commands);
            return null;
        }

        private object HandleLeave(IPlayer player, string channelId, string cmd, string[] args)
        {
            SendMessageToChannel(channelId, "Discord: " + Lang(LangKeys.DiscordLeave, player));
            _storedData.PlayerDiscordInfo.Remove(player.Id);
            SaveData();
            Interface.Call("OnDiscordCoreLeave", player);
            return null;
        }

        private object HandlePlugins(IPlayer player, string channelId, string cmd, string[] args)
        {
            string pluginList = _discordCommands.Select(dc => dc.Value.PluginName).Distinct().Aggregate("Discord: " + Lang(LangKeys.DiscordPlugins, player), (current, plugin) => current + $"{plugin}\n");
            SendMessageToChannel(channelId, pluginList);
            return null;
        }

        #endregion

        #region Discord Hooks
        private void Discord_Ready(Ready ready)
        {
            try
            {
                _guildCount = ready.Guilds.Count;
                if (_guildCount > 1)
                {
                    if (string.IsNullOrEmpty(_pluginConfig.BotGuildId))
                    {
                        PrintError("Bot Guild Id is blank and the bot is in multiple guilds. " +
                                   "Please set the bot guild id to the discord server you wish it to work for.");
                        return;
                    }

                    if (ready.Guilds.All(g => g.id != _pluginConfig.BotGuildId))
                    {
                        PrintError("Failed to find a matching guild for the Bot Guild Id. " +
                                   "Please make sure your guild Id is correct and the bot is in that discord server.");
                        return;
                    }
                }

                _bot = ready.User;
                Puts($"Connected to bot: {_bot.username}");

                _connectionTimeout = timer.In(60f, () =>
                {
                    CloseClient();
                    ConnectClient();
                });
            }
            catch (Exception ex)
            {
                _connectionState = ConnectionStateEnum.Disconnected;
                PrintError($"Failed to load DiscordCore: {ex}");
            }
        }
        
        private void Discord_GuildCreate(Guild guild)
        {
            if (_connectionState != ConnectionStateEnum.Connecting)
            {
                return;
            }

            if (_guildCount != 1 && guild.id != _pluginConfig.BotGuildId)
            {
                return;
            }

            GuildConnected(guild);
        }

        private void GuildConnected(Guild guild)
        {
            try
            {
                _connectionTimeout?.Destroy();
                _discordServer = guild;
                Puts($"Discord connected to server: {_discordServer.name}");

                _connectionState = ConnectionStateEnum.Connected;

                if (!_initialized)
                {
                    _initialized = true;
                    Interface.Call("OnDiscordCoreReady");
                }

                if (_pluginConfig.EnableBotChannel)
                {
                    if (_discordServer.channels.All(c => c.name != _pluginConfig.BotChannel && c.id != _pluginConfig.BotChannel))
                    {
                        PrintWarning($"Bot channel is enabled but there is no channel found with name or id '{_pluginConfig.BotChannel}' on the discord server");
                    }
                }
            }
            catch (Exception ex)
            {
                _connectionState = ConnectionStateEnum.Disconnected;
                PrintError($"Failed to connect to guild: {ex}");
            }
        }

        private void DiscordSocket_HeartbeatSent()
        {
            _lastUpdate = DateTime.Now;
        }

        private void Discord_MessageCreate(Message message)
        {
            if (!_initialized)
            {
                return;
            }

            if (message.author.username == _bot.username)
            {
                Interface.Oxide.CallHook("OnDiscordBotMessage", message);
                return;
            }

            bool botChannel = false;
            string content = message.content;

            Channel channel = _discordServer.channels.FirstOrDefault(c => c.id == message.channel_id);
            if (channel != null) 
            {
                bool shouldReturn = true;
                if (content.ToLower().StartsWith(_pluginConfig.DiscordJoinCommand))
                {
                    message.author.CreateDM(_client, pmChannel =>
                    {
                        pmChannel.CreateMessage(_client, Lang(LangKeys.DiscordJoinWrongChannel, null, _pluginConfig.DiscordJoinCommand));
                    });
                    NextTick(() =>
                    {
                        message.DeleteMessage(_client);
                    });
                }
                else if (content.StartsWith("/"))
                {
                    if (!_pluginConfig.EnableBotChannel)
                    {
                        NextTick(() =>
                        {
                            message.DeleteMessage(_client);
                        });
                        message.author.CreateDM(_client, pmChannel =>
                        {
                            pmChannel.CreateMessage(_client, Lang(LangKeys.DiscordUseCommandsHere));
                        });
                    }
                    else
                    {
                        if (!channel.name.ToLower().Equals(_pluginConfig.BotChannel.ToLower()) && channel.id != _pluginConfig.BotChannel)
                        {
                            NextTick(() =>
                            {
                                message.DeleteMessage(_client);
                            });
                            message.author.CreateDM(_client, pmChannel =>
                            {
                                pmChannel.CreateMessage(_client, Lang(LangKeys.DiscordPrivateChannel));
                            });
                        }
                        else
                        {
                            string cmd = content.TrimStart('/').Split(' ')[0];
                            if (_discordCommands.ContainsKey(cmd) && !_discordCommands[cmd].AllowInBotChannel)
                            {
                                NextTick(() =>
                                {
                                    message.DeleteMessage(_client);
                                });
                                message.author.CreateDM(_client, pmChannel =>
                                {
                                    pmChannel.CreateMessage(_client, Lang(LangKeys.DiscordNotAllowedBotChannel));
                                });
                            }
                            else
                            {
                                botChannel = true;
                                shouldReturn = false;
                            }
                        }
                    }
                }
                else if (_channelSubscriptions.ContainsKey(channel.id))
                {
                    object output = null;
                    foreach (ChannelSubscription subscription in _channelSubscriptions[channel.id])
                    {
                        try
                        {
                            output = subscription.Method.Invoke(message);
                        }
                        catch (Exception ex)
                        {
                            PrintError($"Error invoking method on plugin {subscription.PluginName} on subscribed channel method {subscription.Method.Method.Name}:\n{ex}");
                        }
                    }

                    if (output != null)
                    {
                        shouldReturn = false;
                    }
                }

                if (shouldReturn)
                {
                    return;
                }
            }

            InGameActivation inGameActivation = _pendingInGameActivation[message.author.id];
            if (inGameActivation != null)
            {
                HandleGameServerJoin(message, inGameActivation);
                return;
            }

            if (content.ToLower().StartsWith("/join"))
            {
                HandleDiscordJoin(message);
                return;
            }

            //Puts($"MessageCreate: {content}");

            DiscordInfo info = _storedData.GetInfoByDiscordId(message.author.id);
            if (info == null && !botChannel)
            {
                SendMessageToUser(message.author.id, Lang(LangKeys.DiscordMustSignUpBeforeCommands, null, _pluginConfig.DiscordJoinCommand));
                return;
            }

            try
            {
                IPlayer player = info == null ? null : covalence.Players.FindPlayerById(info.PlayerId);
                if (content.StartsWith("/"))
                {
                    string[] args;
                    string command;

                    ParseCommand(message.content.TrimStart('/'), out command, out args);
                    DiscordCommand discord = _discordCommands[command];
                    if (discord != null)
                    {
                        if (!string.IsNullOrEmpty(discord.Permission) && !player.HasPermission(discord.Permission))
                        {
                            SendMessageToUser(player, Lang(LangKeys.NoPermission, player));
                            return;
                        }
                        
                        discord.Method(player, message.channel_id, command, args);
                    }
                    else
                    {
                        SendMessageToUser(message.author.id, Lang(LangKeys.UnknownCommand, player));
                    }
                }
                else
                {
                    Interface.Call("OnDiscordChat", player, message.content);
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error in message create:\n{ex}");
            }
        }

        private void Discord_MemberAdded(GuildMember member)
        {
            if (!_pluginConfig.EnabledJoinNotifications)
            {
                return;
            }
            
            member.user.CreateDM(_client, channel =>
            {
                channel.CreateMessage(_client, Lang(LangKeys.DiscordUserJoin, null, _discordServer.name, _pluginConfig.DiscordJoinCommand, _pluginConfig.HelpCommand));
            });
        }

        private void Discord_ChannelDelete(Channel channel)
        {
            // Delete all subscriptions to this channel for all plugins
            _channelSubscriptions.Remove(channel.id);

            //TODO: Remove once fix is implemented in Discord Extension
            _discordServer.channels.RemoveAll(c => c.id == channel.id);
        }

        private void Discord_UnhandledEvent(JObject messageObject)
        {
            if (_connectionState == ConnectionStateEnum.Connected)
            {
                PrintError($"Unhandled Event: {messageObject}");
                CloseClient();
                ConnectClient();
            }
        }

        private void DiscordSocket_WebSocketErrored(Exception exception, string message)
        {
            if (_connectionState == ConnectionStateEnum.Connected)
            {
                PrintError($"WebSocketError: {exception}\n{message}");
                CloseClient();
                ConnectClient();
            }
        }

        private void DiscordSocket_WebSocketClosed(string reason, int code, bool clean)
        {
            if (_connectionState == ConnectionStateEnum.Connected)
            {
                PrintWarning("WebSocketClose Detected. Restarting Connection.");
                CloseClient();
                ConnectClient();
            }
        }
        #endregion

        #region Joining
        private void HandleGameServerJoin(Message message, InGameActivation activation)
        {
            if (activation.Code != message.content)
            {
                message.Reply(_client, Lang(LangKeys.GameJoinCodeNotMatch), false);
                return;
            }

            _pendingInGameActivation.Remove(message.author.id);
            _storedData.PlayerDiscordInfo[activation.PlayerId] = new DiscordInfo
            {
                ChannelId = message.channel_id,
                DiscordId = message.author.id,
                PlayerId = activation.PlayerId,
                DisplayName = activation.DisplayName
            };

            IPlayer player = covalence.Players.FindPlayer(activation.PlayerId);
            SaveData();
            Chat(player, Lang(LangKeys.DiscordJoinSuccessfullyRegistered, player));
            message.Reply(_client, $"Discord: {Lang(LangKeys.DiscordJoinSuccessfullyRegistered, player)}", false);
            Interface.Call("OnDiscordCoreJoin", player);
        }

        private void HandleDiscordJoin(Message message)
        {
            if (_storedData.PlayerDiscordInfo.Any(di => di.Value.DiscordId == message.author.id))
            {
                message.Reply(_client, Formatter.ToPlaintext(Lang(LangKeys.JoinAlreadySignedUp, null, _pluginConfig.DiscordJoinCommand)), false);
                return;
            }

            string code = Core.Random.Range(0, 1000000).ToString("D6");
            _pendingDiscordActivations.Add(new DiscordActivation
            {
                Code = code,
                DiscordId = message.author.id,
                ChannelId = message.channel_id,
            });

            message.Reply(_client, Lang(LangKeys.DiscordCompleteActivation, null, _pluginConfig.GameChatCommand, code), false);
        }

        private DiscordUser GetUserByUsername(string userName)
        {
            return _discordServer.members.FirstOrDefault(m => $"{m?.user?.username?.ToLower()}#{m?.user?.discriminator}" == userName.ToLower())?.user;
        }

        private void CreateNewDmChannel(string discordId, string message)
        {
            DiscordInfo info = _storedData.GetInfoByDiscordId(discordId);
            if (info == null)
            {
                return;
            }

            DiscordUser.GetUser(_client, discordId, user =>
            {
                user?.CreateDM(_client, channel =>
                {
                    channel.CreateMessage(_client, message);
                    info.ChannelId = channel.id;
                    SaveData();
                });
            });
        }
        
        private void CreateNewDmChannel(string discordId, Message message)
        {
            DiscordInfo info = _storedData.GetInfoByDiscordId(discordId);
            if (info == null)
            {
                return;
            }

            DiscordUser.GetUser(_client, discordId, user =>
            {
                user?.CreateDM(_client, channel =>
                {
                    channel.CreateMessage(_client, message);
                    info.ChannelId = channel.id;
                    SaveData();
                });
            });
        }

        private void CreateChannelActivation(IPlayer player, DiscordUser user, string code, string message)
        {
            user.CreateDM(_client, channel =>
            {
                channel.CreateMessage(_client, message);
                _pendingInGameActivation[user.id] = new InGameActivation
                {
                    PlayerId = player.Id,
                    Code = code,
                    ChannelId = channel.id,
                    DisplayName = player.Name
                };
            });
        }
        #endregion

        #region API

        #region Discord Server
        [HookMethod("IsReady")]
        public bool IsReady()
        {
            return _client?.DiscordServer != null;
        }

        [HookMethod("GetClient")]
        public DiscordClient GetClient()
        {
            return _client;
        }

        [HookMethod("GetBot")]
        public DiscordUser GetBot()
        {
            return _bot;
        }

        [HookMethod("GetDiscordName")]
        public string GetDiscordName()
        {
            return _client?.DiscordServer.name ?? "Not Connected";
        }

        [HookMethod("GetDiscordJoinCode")]
        public string GetDiscordJoinCode()
        {
            return _pluginConfig.JoinCode;
        }

        [HookMethod("UpdatePresence")]
        public void UpdatePresence(Presence presence)
        {
            _client?.UpdateStatus(presence);
        }

        [HookMethod("RegisterPluginForExtensionHooks")]
        public void RegisterPluginForExtensionHooks(Plugin plugin)
        {
            if (!_client.Plugins.Contains(plugin))
            {
                _client.Plugins.Add(plugin);
            }

            if (!_hookPlugins.Contains(plugin))
            {
                _hookPlugins.Add(plugin);
            }
        }
        #endregion

        #region Send Message

        #region Channel
        [HookMethod("SendMessageToChannel")]
        public void SendMessageToChannel(string channelNameOrId, string message)
        {
            Channel channel = GetChannel(channelNameOrId);

            channel?.CreateMessage(_client,  StripRustTags(Formatter.ToPlaintext(message)));
        }
        
        [HookMethod("SendMessageToChannel")]
        public void SendMessageToChannel(string channelNameOrId, Message message)
        {
            Channel channel = GetChannel(channelNameOrId);

            channel?.CreateMessage(_client, message);
        }
        #endregion

        #region User
        [HookMethod("SendMessageToUser")]
        public void SendMessageToUser(string id, string message)
        {
            DiscordInfo info = _storedData.PlayerDiscordInfo[id];
            if (info == null)
            {
                info = _storedData.GetInfoByDiscordId(id);
                if (info == null)
                {
                    return;
                }
            }

            Channel.GetChannel(_client, info.ChannelId, channel =>
            {
                if (channel != null)
                {
                    channel.CreateMessage(_client, StripRustTags(Formatter.ToPlaintext(message)));
                }
                else
                {
                    CreateNewDmChannel(info.DiscordId, StripRustTags(Formatter.ToPlaintext(message)));
                }
            });
        }

        [HookMethod("SendMessageToUser")]
        public void SendMessageToUser(IPlayer player, string message)
        {
            SendMessageToUser(player.Id, message);
        }
        
        [HookMethod("SendMessageToUser")]
        public void SendMessageUser(string id, Message message)
        {
            DiscordInfo info = _storedData.PlayerDiscordInfo[id];
            if (info == null)
            {
                info = _storedData.GetInfoByDiscordId(id);
                if (info == null)
                {
                    return;
                }
            }

            Channel.GetChannel(_client, info.ChannelId, channel =>
            {
                if (channel != null)
                {
                    channel.CreateMessage(_client, message);
                }
                else
                {
                    CreateNewDmChannel(info.DiscordId, message);
                }
            });
        }
        
        [HookMethod("SendMessageToUser")]
        public void SendMessageUser(IPlayer player, Message message)
        {
            SendMessageUser(player.Id, message);
        }
        #endregion
        #endregion

        #region Message
        [HookMethod("DeleteMessage")]
        public void DeleteMessage(Message message)
        {
            message.DeleteMessage(_client);
        }
        #endregion

        #region User
        [HookMethod("GetAllUsers")]
        public List<string> GetAllUsers()
        {
            return _storedData.PlayerDiscordInfo.Keys.ToList();
        }

        [HookMethod("GetGuildMember")]
        public GuildMember GetGuildMember(string steamId)
        {
            return _discordServer.members.FirstOrDefault(m => _storedData.PlayerDiscordInfo[steamId]?.DiscordId == m.user.id);
        }

        [HookMethod("GetLinkedPlayers")]
        public List<DiscordUser> GetLinkedPlayers()
        {
            return _discordServer.members
                .Where(m => _storedData.PlayerDiscordInfo.Values.Any(pdi => pdi.DiscordId == m.user.id))
                .Select(m => m.user)
                .ToList();
        }

        [HookMethod("GetGuildAllUsers")]
        public List<DiscordUser> GetGuildAllUsers()
        {
            return _discordServer.members.Select(u => u.user).ToList();
        }

        [HookMethod("GetUserDiscordInfo")]
        public JObject GetUserDiscordInfo(object user)
        {
            DiscordInfo info = null;
            if (user is string)
            {
                info = _storedData.PlayerDiscordInfo[(string)user];
                if (info == null)
                {
                    info = _storedData.GetInfoByDiscordId((string)user);
                }
            }
            else if (user is IPlayer)
            {
                info = _storedData.PlayerDiscordInfo[((IPlayer)user).Id];
            }

            return info != null ? JObject.FromObject(info) : null;
        }

        [HookMethod("GetUsersDiscordInfo")]
        public List<JObject> GetUsersDiscordInfo(List<object> users)
        {
            return users.Select(GetUserDiscordInfo).Where(i => i != null).ToList();
        }

        [HookMethod("GetSteamIdFromDiscordId")]
        public string GetSteamIdFromDiscordId(string discordId)
        {
            return _storedData.GetInfoByDiscordId(discordId)?.PlayerId;
        }

        [HookMethod("GetDiscordIdFromSteamId")]
        public string GetDiscordIdFromSteamId(string steamId)
        {
            return _storedData.PlayerDiscordInfo[steamId]?.DiscordId;
        }

        [HookMethod("KickDiscordUser")]
        public void KickDiscordUser(string id)
        {
            DiscordInfo info = _storedData.PlayerDiscordInfo[id];
            if (info == null)
            {
                info = _storedData.GetInfoByDiscordId(id);
                if (info == null)
                {
                    return;
                }

            }

            _discordServer.RemoveGuildMember(_client, info.DiscordId);
        }

        [HookMethod("BanDiscordUser")]
        public void BanDiscordUser(string id, int? deleteMessageDays)
        {
            DiscordInfo info = _storedData.PlayerDiscordInfo[id];
            if (info == null)
            {
                info = _storedData.GetInfoByDiscordId(id);
                if (info == null)
                {
                    return;
                }

            }

            _discordServer.CreateGuildBan(_client, info.DiscordId, deleteMessageDays);
        }
        #endregion

        #region Discord Chat Commands
        [HookMethod("RegisterCommand")]
        public void RegisterCommand(string command, Plugin plugin, Func<IPlayer, string, string, string[], object> method, string helpText, string permission = null, bool allowInBotChannel = false)
        {
            command = command.TrimStart('/');
            if (_discordCommands.ContainsKey(command) && _discordCommands[command].PluginName != plugin.Name)
            {
                PrintWarning($"Discord Commands already contains command: {command}. Previously registered to {_discordCommands[command].PluginName}");
            }

            if (plugin == null)
            {
                PrintWarning($"Cannot register command: {command} with a null plugin!");
                return;
            }

            //Puts($"Registered Command: /{command} for plugin: {pluginName}");

            _discordCommands[command] = new DiscordCommand
            {
                PluginName = plugin.Name,
                Plugin = plugin,
                Method = method,
                HelpText = helpText,
                Permission = permission,
                AllowInBotChannel = allowInBotChannel
            };
        }

        [HookMethod("UnregisterCommand")]
        public void UnregisterCommand(string command, Plugin plugin)
        {
            command = command.TrimStart('/');
            if (!_discordCommands.ContainsKey(command))
            {
                PrintWarning($"Command: {command} could not be unregistered because it was not found");
                return;
            }

            if (_discordCommands[command]?.PluginName != plugin.Name)
            {
                PrintWarning("Cannot unregister commands which don't belong to your plugin\n" +
                             $"Command: {command} CommandPlugin:{_discordCommands[command]?.PluginName} Unregistering Plugin: {plugin.Title}");
                return;
            }

            _discordCommands.Remove(command);
        }
        #endregion

        #region Channel
        [HookMethod("GetAllChannels")]
        public List<Channel> GetAllChannels()
        {
            return _discordServer.channels.ToList();
        }
        
        [HookMethod("GetAllDms")]
        public List<Channel> GetAllDms()
        {
            return _client.DMs.ToList();
        }

        [HookMethod("GetChannel")]
        public Channel GetChannel(string nameOrId)
        {
            Channel channel = GetAllChannels().FirstOrDefault(c => string.Equals(c.name, nameOrId, StringComparison.InvariantCultureIgnoreCase)
                                                                      || c.id == nameOrId);
            if (channel != null)
            {
                return channel;
            }

            channel = GetAllDms().FirstOrDefault(d => d.id == nameOrId);
            return channel;
        }

        [HookMethod("GetChannelMessages")]
        public void GetChannelMessages(string nameOrId, string responseKey)
        {
            GetChannel(nameOrId)?.GetChannelMessages(_client, messages =>
            {
                Interface.Call("OnGetChannelMessages", messages, responseKey);
            });
        }

        [HookMethod("SubscribeChannel")]
        public void SubscribeChannel(string channelNameOrId, Plugin plugin, Func<Message, object> method)
        {
            if (_connectionState != ConnectionStateEnum.Connected)
            {
                PrintError("Trying to subscribe to channel while bot is not in connect state");
                return;
            }

            Channel channel = GetChannel(channelNameOrId);
            if (channel == null)
            {
                PrintError($"Channel not found in guild: {channelNameOrId}");
                return;
            }

            if (!_channelSubscriptions.ContainsKey(channel.id))
            {
                _channelSubscriptions[channel.id] = new List<ChannelSubscription>();
            }

            List<ChannelSubscription> subscriptions = _channelSubscriptions[channel.id];
            if (subscriptions.Any(s => s.PluginName == plugin.Name))
            {
                LogWarning($"The plugin {plugin.Title} already has a subscription to channel {channelNameOrId}");
                return;
            }

            subscriptions.Add(new ChannelSubscription
            {
                PluginName = plugin.Name,
                Method = method
            });

            Interface.Call("OnChannelSubscribed", channel, plugin);
        }

        [HookMethod("UnsubscribeChannel")]
        public void UnsubscribeChannel(string channelNameOrId, Plugin plugin)
        {
            if (_connectionState != ConnectionStateEnum.Connected)
            {
                PrintError("Trying to unsubscribe to channel while bot is not in connect state");
                return;
            }

            Channel channel = GetChannel(channelNameOrId);
            if (channel == null)
            {
                PrintError($"Channel not found in guild: {channelNameOrId}");
                return;
            }

            List<ChannelSubscription> subscriptions = _channelSubscriptions[channel.id];

            subscriptions?.RemoveAll(s => s.PluginName == plugin.Name);
            Interface.Call("OnChannelUnsubscribed", channel, plugin);
        }
        #endregion

        #region Roles
        [HookMethod("GetRoles")]
        public List<Role> GetRoles()
        {
            return _discordServer.roles;
        }
        
        [HookMethod("GetRole")]
        public Role GetRole(string nameOrId)
        {
            return _discordServer.roles.FirstOrDefault(r => r.id == nameOrId || string.Equals(r.name, nameOrId, StringComparison.OrdinalIgnoreCase));
        }

        [HookMethod("UserHasRole")]
        public bool UserHasRole(string userId, string nameOrId)
        {
            GuildMember member = GetGuildMember(userId);
            if (member == null)
            {
                return false;
            }

            Role role = GetRole(nameOrId);
            if (role == null)
            {
                return false;
            }
            
            return member.roles.Any(r => r == role.id);
        }

        [HookMethod("AddRoleToUser")]
        public void AddRoleToUser(string userId, string roleId)
        {
            Role role = GetRole(roleId);
            if (role == null)
            {
                PrintWarning($"Tried to add a role to player that doesn't exist: '{roleId}'");
                return;
            }

            _discordServer.AddGuildMemberRole(_client, userId, role.id);
        }

        [HookMethod("RemoveRoleFromUser")]
        public void RemoveRoleFromUser(string userId, string roleId)
        {
            Role role = GetRole(roleId);
            if (role == null)
            {
                PrintWarning($"Tried to remove a role from a player that doesn't exist: '{roleId}'");
                return;
            }

            _discordServer.RemoveGuildMemberRole(_client, userId, role.id);
        }

        [HookMethod("CreateGuildRole")]
        public void CreateGuildRole(Role role)
        {
            _discordServer.CreateGuildRole(_client, role);
        }

        [HookMethod("DeleteGuildRole")]
        public void DeleteGuildRole(string roleId)
        {
            _discordServer.DeleteGuildRole(_client, roleId);
        }

        [HookMethod("DeleteGuildRole")]
        public void DeleteGuildRole(Role role)
        {
            DeleteGuildRole(role.id);
        }
        #endregion
        #endregion

        #region Method Handling
        /// <summary>
        /// Parses the specified command into uMod command format
        /// Sourced from RustCore.cs of OxideMod (https://github.com/theumod/uMod.Rust/blob/oxide/src/RustCore.cs)
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void ParseCommand(string argstr, out string command, out string[] args)
        {
            List<string> argList = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inLongArg = false;

            foreach (char c in argstr)
            {
                if (c == '"')
                {
                    if (inLongArg)
                    {
                        string arg = sb.ToString().Trim();
                        if (!string.IsNullOrEmpty(arg))
                            argList.Add(arg);
                        sb = new StringBuilder();
                        inLongArg = false;
                    }
                    else
                        inLongArg = true;
                }
                else if (char.IsWhiteSpace(c) && !inLongArg)
                {
                    string arg = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg))
                        argList.Add(arg);
                    sb = new StringBuilder();
                }
                else
                    sb.Append(c);
            }

            if (sb.Length > 0)
            {
                string arg = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(arg))
                    argList.Add(arg);
            }

            if (argList.Count == 0)
            {
                command = null;
                args = null;
                return;
            }

            command = argList[0].ToLower();
            argList.RemoveAt(0);
            args = argList.ToArray();
        }

        #endregion Method Handling

        #region Rust Tag Handling
        private readonly List<Regex> _regexTags = new List<Regex>
        {
            new Regex("<color=.+?>", RegexOptions.Compiled),
            new Regex("<size=.+?>", RegexOptions.Compiled)
        };

        private readonly List<string> _tags = new List<string>
        {
            "</color>",
            "</size>",
            "<i>",
            "</i>",
            "<b>",
            "</b>"
        };

        private string StripRustTags(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return string.Empty;
            }

            foreach (string tag in _tags)
            {
                original = original.Replace(tag, "");
            }

            foreach (Regex regexTag in _regexTags)
            {
                original = regexTag.Replace(original, "");
            }

            return original;
        }
        #endregion

        #region Helper Methods
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _storedData);
        
        private void Chat(IPlayer player, string format) => player.Reply(Lang(LangKeys.ChatFormat, player, format));

        private string Lang(string key, IPlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player?.Id), args);
            }
            catch(Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }

        #endregion

        #region Classes
        private class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord API Key")]
            public string DiscordApiKey { get; set; }
            
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Bot Guild ID (Can be left blank if bot in only 1 guild)")]
            public string BotGuildId { get; set; }

            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Join Code")]
            public string JoinCode { get; set; }

            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Enable Commands In Bot Channel")]
            public bool EnableBotChannel { get; set; }

            [DefaultValue("bot")]
            [JsonProperty(PropertyName = "Bot Channel Name or Id")]
            public string BotChannel { get; set; }

            [DefaultValue("dc")]
            [JsonProperty(PropertyName = "In Game Chat Command")]
            public string GameChatCommand { get; set; }

            [DefaultValue("join")]
            [JsonProperty(PropertyName = "Discord Bot Join Command")]
            public string DiscordJoinCommand { get; set; }

            [DefaultValue("plugins")]
            [JsonProperty(PropertyName = "Discord Bot Plugins Command")]
            public string PluginsCommand { get; set; }

            [DefaultValue("help")]
            [JsonProperty(PropertyName = "Discord Bot Help Command")]
            public string HelpCommand { get; set; }

            [DefaultValue("leave")]
            [JsonProperty(PropertyName = "Discord Bot Leave Command")]
            public string LeaveCommand { get; set; }

            [DefaultValue("commands")]
            [JsonProperty(PropertyName = "Discord Bot Commands Command")]
            public string CommandsCommand { get; set; }
            
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Enable Discord Server Join Notification")]
            public bool EnabledJoinNotifications { get; set; }
        }

        private class StoredData
        {
            public Hash<string, DiscordInfo> PlayerDiscordInfo = new Hash<string, DiscordInfo>();

            public DiscordInfo GetInfoByDiscordId(string id)
            {
                return PlayerDiscordInfo.Select(d => d.Value).FirstOrDefault(d => d.DiscordId == id);
            }
        }

        private class DiscordInfo
        {
            public string ChannelId { get; set; }
            public string DiscordId { get; set; }
            public string PlayerId { get; set; }
            public string DisplayName { get; set; }
        }

        private class InGameActivation
        {
            public string Code { get; set; }
            public string PlayerId { get; set; }
            public string ChannelId { get; set; }
            public string DisplayName { get; set; }
        }

        private class DiscordActivation
        {
            public string Code { get; set; }
            public string DiscordId { get; set; }
            public string ChannelId { get; set; }
        }

        private class DiscordCommand
        {
            public string PluginName { get; set; }
            public Plugin Plugin { get; set; }
            public Func<IPlayer, string, string, string[], object> Method { get; set; }
            public string HelpText { get; set; }
            public string Permission { get; set; }
            public bool AllowInBotChannel { get; set; }
        }

        private class ChannelSubscription
        {
            public string PluginName { get; set; }
            public Func<Message, object> Method { get; set; }
        }

        private static class LangKeys
        {
            public const string NoPermission = "NoPermission";
            public const string ChatFormat = "ChatFormatV1";
            public const string UnknownCommand = "UnknownCommand";
            public const string DiscordLeave = "DiscordLeave";
            public const string DiscordLeaveFailed = "DiscordLeaveFailed";
            public const string JoinAlreadySignedUp = "JoinAlreadySignedUpV1";
            public const string JoinInvalidSyntax = "JoinInvalidSyntaxV1";
            public const string JoinUnableToFindUser = "JoinUnableToFindUserV2";
            public const string JoinReceivedPm = "JoinReceivedPmV2";
            public const string JoinPleaseEnterCode = "JoinPleaseEnterCode";
            public const string DiscordJoinInvalidSyntax = "DiscordJoinInvalidSyntaxV1";
            public const string DiscordJoinNoPendingActivations = "DiscordJoinNoPendingActivations";
            public const string DiscordJoinSuccessfullyRegistered = "DiscordJoinSuccessfullyRegistered";
            public const string DiscordCommandsPreText = "DiscordCommandsPreTextV1";
            public const string DiscordCommandsBotChannelAllowed = "DiscordCommandsBotChannelAllowed";
            public const string DiscordPlugins = "DiscordPluginsV1";
            public const string DiscordJoinWrongChannel = "DiscordJoinWrongChannelV1";
            public const string DiscordUseCommandsHere = "DiscordUseCommandsHere";
            public const string DiscordPrivateChannel = "DiscordPrivateChannel";
            public const string DiscordNotAllowedBotChannel = "DiscordNotAllowedBotChannel";
            public const string DiscordMustSignUpBeforeCommands = "DiscordMustSignUpBeforeCommandsV1";
            public const string DiscordClientNotConnected = "DiscordClientNotConnected";
            public const string GameJoinCodeNotMatch = "GameJoinCodeNotMatch";
            public const string DiscordCompleteActivation = "DiscordCompleteActivationV2";
            public const string DiscordUserJoin = "DiscordUserJoinV1";
            public const string HelpNotLinked = "HelpNotLinkedV1";
            public const string HelpLinked = "HelpLinkedV1";
            public const string HelpText = "HelpTextV2";
        }
        #endregion
    }
}
