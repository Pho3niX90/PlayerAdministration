using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.DiscordEvents;
using Oxide.Ext.Discord.DiscordObjects;

namespace Oxide.Plugins
{
    [Info("Discord Core Roles", "MJSU", "1.1.0")]
    [Description("Syncs players oxide group with discord roles")]
    internal class DiscordCoreRoles : CovalencePlugin
    {
        #region Class Fields

        [PluginReference] private Plugin DiscordCore;

        private PluginConfig _pluginConfig; //Plugin Config

        private enum Source
        {
            Umod,
            Discord
        }

        #endregion

        #region Setup & Loading

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.SyncData = config.SyncData ?? new List<SyncData>
            {
                new SyncData
                {
                    Oxide = "Default",
                    Discord = "DiscordRoleNameOrId",
                    Source = Source.Umod,
                },
                new SyncData
                {
                    Oxide = "VIP",
                    Discord = "VIP",
                    Source = Source.Discord,
                }
            };
            return config;
        }

        private void OnServerInitialized()
        {
            if (DiscordCore == null)
            {
                PrintError("Missing plugin dependency DiscordCore: https://umod.org/plugins/discord-core");
            }

            OnDiscordCoreReady();
        }

        private void OnDiscordCoreReady()
        {
            if (!(DiscordCore?.Call<bool>("IsReady") ?? false))
            {
                return;
            }

            DiscordCore.Call("RegisterPluginForExtensionHooks", this);

            foreach (var data in _pluginConfig.SyncData.ToList())
            {
                bool remove = false;
                if (!permission.GroupExists(data.Oxide))
                {
                    PrintWarning($"Oxide group does not exist: '{data.Oxide}'. Please create the group or correct the name");
                    remove = true;
                }

                Role role = DiscordCore.Call<Role>("GetRole", data.Discord);
                if (role == null)
                {
                    PrintWarning($"Discord role name or id does not exist: '{data.Discord}'.\n" +
                                 "Please add the discord role or fix the role name/id.");
                    remove = true;
                }

                if (remove)
                {
                    _pluginConfig.SyncData.Remove(data);
                }
            }

            int index = 1;
            foreach (IPlayer player in covalence.Players.Connected)
            {
                timer.In(index, () => { OnUserConnected(player); });
                index++;
            }
        }

        #endregion

        #region uMod Hooks

        private void OnUserConnected(IPlayer player)
        {
            string discordId = GetDiscordId(player.Id);
            if (string.IsNullOrEmpty(discordId))
            {
                return;
            }

            foreach (string groupName in permission.GetUserGroups(player.Id))
            {
                HandleOxideGroup(player.Id, discordId, groupName);
            }

            HandleDiscordRoles(player.Id);
        }

        private void OnUserGroupAdded(string id, string groupName)
        {
            HandleOxideGroup(id, GetDiscordId(id), groupName);
        }

        private void OnUserGroupRemoved(string id, string groupName)
        {
            HandleOxideGroup(id, GetDiscordId(id), groupName);
        }

        private void HandleOxideGroup(string playerId, string discordId, string groupName)
        {
            if (DiscordCore == null)
            {
                PrintError("Missing plugin dependency DiscordCore: https://umod.org/plugins/discord-core");
                return;
            }
            
            if (string.IsNullOrEmpty(discordId))
            {
                return;
            }
            
            IPlayer player = covalence.Players.FindPlayerById(playerId);
            foreach (SyncData data in _pluginConfig.SyncData.Where(s => s.Oxide == groupName && s.Source == Source.Umod))
            {
                bool isInGroup = permission.UserHasGroup(playerId, groupName);
                bool isInDiscord = DiscordCore.Call<bool>("UserHasRole", playerId, data.Discord);
                if (isInDiscord == isInGroup)
                {
                    Puts($"{player?.Name} skipping Umod Sync: {data.Oxide} -> {data.Discord} {isInGroup}");
                    return;
                }
                
                string hook = isInGroup ? "AddRoleToUser" : "RemoveRoleFromUser";
                DiscordCore.Call(hook, discordId, data.Discord);
                
                if (isInGroup)
                {
                    Puts($"Adding player {player?.Name}({playerId}) to discord role {data.Discord}");
                }
                else
                {
                    Puts($"Removing player {player?.Name}({playerId}) from discord role {data.Discord}");
                }
            }
        }

        private void Discord_MemberAdded(GuildMember member)
        {
            HandleDiscordRoles(GetPlayerId(member.user.id));
        }

        private void Discord_MemberRemoved(GuildMember member)
        {
            HandleDiscordRoles(GetPlayerId(member.user.id));
        }

        private void Discord_GuildMemberUpdate(GuildMemberUpdate update, GuildMember oldMember)
        {
            List<string> roles = oldMember.roles.Where(role => !update.roles.Contains(role)).ToList();
            roles.AddRange(update.roles.Where(role => !oldMember.roles.Contains(role)));

            HandleDiscordRoles(GetPlayerId(oldMember.user.id), roles);
        }

        private void HandleDiscordRoles(string playerId, List<string> roles = null)
        {
            if (DiscordCore == null)
            {
                PrintError("Missing plugin dependency DiscordCore: https://umod.org/plugins/discord-core");
                return;
            }
            
            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }
            
            IPlayer player = covalence.Players.FindPlayerById(playerId);
            foreach (SyncData data in _pluginConfig.SyncData.Where(s => s.Source == Source.Discord))
            {
                if (roles != null)
                {
                    Role role = GetRole(data.Discord);
                    if (role == null)
                    {
                        continue;
                    }
                    
                    if (!roles.Contains(role.id))
                    {
                        continue;
                    }
                }
                
                bool isInGroup = permission.UserHasGroup(playerId, data.Oxide);
                bool isInDiscord = DiscordCore.Call<bool>("UserHasRole", playerId, data.Discord);
                if (isInDiscord == isInGroup)
                {
                    Puts($"{player?.Name} skipping Discord Sync: {data.Discord} {data.Oxide} {isInGroup}");
                    return;
                }
                
                if (isInDiscord)
                {
                    Puts($"Adding player {player?.Name}({playerId}) to oxide group {data.Oxide}");
                    permission.AddUserGroup(playerId, data.Oxide);
                }
                else
                {
                    Puts($"Removing player {player?.Name}({playerId}) from oxide group {data.Oxide}");
                    permission.RemoveUserGroup(playerId, data.Oxide);
                }
            }
        }

        private string GetDiscordId(string playerId)
        {
            return DiscordCore?.Call<string>("GetDiscordIdFromSteamId", playerId);
        }

        private string GetPlayerId(string discordId)
        {
            return DiscordCore?.Call<string>("GetSteamIdFromDiscordId", discordId);
        }

        private Role GetRole(string role)
        {
            return DiscordCore.Call<Role>("GetRole", role);
        }
        #endregion

        #region Classes

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Sync Data")]
            public List<SyncData> SyncData { get; set; }
        }

        private class SyncData
        {
            [JsonProperty(PropertyName = "Oxide Group")]
            public string Oxide { get; set; }

            [JsonProperty(PropertyName = "Discord Role (Name or Id)")]
            public string Discord { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Sync Source (Umod or Discord)")]
            public Source Source { get; set; }
        }

        #endregion
    }
}