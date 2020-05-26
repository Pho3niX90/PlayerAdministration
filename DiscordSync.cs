using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.DiscordObjects;

namespace Oxide.Plugins
{
    [Info("Discord Sync", "Tricky", "1.0.0")]
    [Description("Integrates players with the discord server")]

    public class DiscordSync : CovalencePlugin
    {
        #region Declared
        [DiscordClient]
        private DiscordClient Client;
        #endregion

        #region Plugin Reference
        [PluginReference]
        private Plugin DiscordAuth;
        #endregion

        #region Config
        Configuration config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string BotToken = string.Empty;

            [JsonProperty(PropertyName = "Update Interval (Minutes)")]
            public int UpdateInterval = 10;

            [JsonProperty(PropertyName = "Enable Nick Syncing")]
            public bool NickSync = false;

            [JsonProperty(PropertyName = "Enable Ban Syncing")]
            public bool BanSync = false;

            [JsonProperty(PropertyName = "Enable Role Syncing")]
            public bool RoleSync = true;

            [JsonProperty(PropertyName = "Role Setup", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<RoleInfo> RoleSetup = new List<RoleInfo>
            {
                new RoleInfo
                {
                    OxideGroup = "default",
                    DiscordRole = "Member"
                },

                new RoleInfo
                {
                    OxideGroup = "vip",
                    DiscordRole = "Donator"
                }
            };

            public class RoleInfo
            {
                [JsonProperty(PropertyName = "Oxide Group")]
                public string OxideGroup;

                [JsonProperty(PropertyName = "Discord Role")]
                public string DiscordRole;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Oxide / Discord Hooks
        private void Init()
        {
            if (config.BotToken == string.Empty)
                return;

            Discord.CreateClient(this, config.BotToken);

            timer.Every(config.UpdateInterval * 60, () =>
            {
                Puts($"Checking for user changes, Guild: {Client.DiscordServer.name}");

                players.All.ToList().ForEach(player =>
                {
                    if (config.NickSync)
                        HandleNick(player);

                    if (config.BanSync)
                        HandleBan(player);

                    if (config.RoleSync)
                        HandleRole(player);
                });
            });
        }

        private void OnServerInitialized()
        {
            if (DiscordAuth == null)
                PrintError("This Plugin requires Discord Auth, get it at https://umod.org/plugins/discord-auth");

            if (!config.RoleSync)
                Unsubscribe(nameof(OnUserGroupAdded));

            if (!config.RoleSync)
                Unsubscribe(nameof(OnUserGroupRemoved));

            if (!config.BanSync)
                Unsubscribe(nameof(OnUserBanned));

            if (!config.BanSync)
                Unsubscribe(nameof(OnUserUnbanned));
        }

        private void Unload() 
            => Discord.CloseClient(Client);

        private void OnAuthenticate(string steamId, string discordId)
        {
            var player = players.FindPlayerById(steamId);
            if (player == null)
                return;

            if (config.NickSync)
                HandleNick(player, discordId);

            if (config.BanSync)
                HandleBan(player, discordId);

            if (config.RoleSync)
                HandleRole(player, discordId);
        }

        private void OnUserNameUpdated(string id)
        {
            var player = players.FindPlayerById(id);
            if (player == null)
                return;

            HandleNick(player);
        }

        private void OnUserBanned(string name, string id) => OnUserUnbanned(name, id);

        private void OnUserUnbanned(string name, string id)
        {
            var player = players.FindPlayerById(id);
            if (player == null)
                return;

            HandleBan(player);
        }

        private void OnUserGroupAdded(string id, string groupName) => OnUserGroupRemoved(id, groupName);

        private void OnUserGroupRemoved(string id, string groupName)
        {
            config.RoleSetup.ForEach(roleSetup =>
            {
                if (roleSetup.OxideGroup == groupName)
                    HandleRole(id, roleSetup.DiscordRole, groupName);
            });
        }
        #endregion

        #region Handle
        private void HandleNick(IPlayer player, string discordId = null)
        {
            discordId = discordId ?? GetDiscord(player.Id);
            if (discordId == null)
                return;

            var guildmember = GetGuildMember(discordId);
            if (guildmember == null)
                return;

            if (guildmember.nick == player.Name)
                return;

            Client.DiscordServer.ModifyUsersNick(Client, discordId, player.Name);
        }

        private void HandleBan(IPlayer player, string discordId = null)
        {
            discordId = discordId ?? GetDiscord(player.Id);
            if (discordId == null)
                return;

            if (GetGuildMember(discordId) == null)
                return;

            Client.DiscordServer.GetGuildBans(Client, bans =>
            {
                if ((bans.Any(ban => ban.user.id != discordId) || bans.Count() == 0) && player.IsBanned)
                {
                    Client.DiscordServer.CreateGuildBan(Client, discordId, 0);
                }
                else if (bans.Any(ban => ban.user.id == discordId) && !player.IsBanned)
                {
                    Client.DiscordServer.RemoveGuildBan(Client, discordId);
                }
            });
        }

        private void HandleRole(string id, string roleName, string oxideGroup, string discordId = null)
        {
            discordId = discordId ?? GetDiscord(id);
            if (discordId == null)
                return;

            var guildmember = GetGuildMember(discordId);
            if (guildmember == null)
                return;

            var role = GetRoleByName(roleName);
            if (role == null)
            {
                Puts($"Unable to find '{roleName}' discord role!");
                return;
            }

            if (HasGroup(id, oxideGroup) && !UserHasRole(discordId, role.id))
            {
                Client.DiscordServer.AddGuildMemberRole(Client, guildmember.user, role);
            }
            else if (!HasGroup(id, oxideGroup) && UserHasRole(discordId, role.id))
            {
                Client.DiscordServer.RemoveGuildMemberRole(Client, guildmember.user, role);
            }
        }

        private void HandleRole(IPlayer player, string discordId = null)
        {
            config.RoleSetup.ForEach(roleSetup =>
            {
                GetGroups(player.Id).ToList().ForEach(playerGroup =>
                {
                    if (roleSetup.OxideGroup == playerGroup)
                        HandleRole(player.Id, roleSetup.DiscordRole, playerGroup, discordId);
                });
            });
        }
        #endregion

        #region Helpers
        private string GetDiscord(string id) 
            => (string) DiscordAuth?.Call("API_GetDiscord", id);

        private bool HasGroup(string id, string groupName) 
            => permission.UserHasGroup(id, groupName);

        private string[] GetGroups(string id) 
            => permission.GetUserGroups(id);

        private Role GetRoleByName(string roleName) 
            => Client.DiscordServer.roles.Find(role => role.name == roleName);

        private GuildMember GetGuildMember(string discordId)
            => Client.DiscordServer.members.Find(member => member.user.id == discordId);

        private bool UserHasRole(string discordId, string roleId)
        {
            if (Client.DiscordServer.members.Any(member => member.user.id == discordId && member.roles.Contains(roleId)))
                return true;

            return false;
        }
        #endregion
    }
}
