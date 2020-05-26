using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.DiscordObjects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("Discord Status", "Tricky", "2.0.4")]
    [Description("Shows server information as a discord bot status")]

    public class DiscordStatus : CovalencePlugin
    {
        #region Fields
        [DiscordClient]
        private DiscordClient Client;

        [PluginReference]
        private Plugin DiscordAuth;

        Configuration config;
        private int statusIndex = -1;
        private string[] StatusTypes = new string[]
        {
            "Game",
            "Stream",
            "Listen",
            "Watch"
        };
        #endregion

        #region Config
        class Configuration
        {
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string BotToken = string.Empty;

            [JsonProperty(PropertyName = "Update Interval (Seconds)")]
            public int UpdateInterval = 5;

            [JsonProperty(PropertyName = "Randomize Status")]
            public bool Randomize = false;

            [JsonProperty(PropertyName = "Status Type (Game/Stream/Listen/Watch)")]
            public string StatusType = "Game";

            [JsonProperty(PropertyName = "Status", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Status = new List<string>
            {
                "{players.online} / {server.maxplayers} Online!",
                "{server.entities} Entities",
                "{players.sleepers} Sleepers!",
                "{players.authenticated} Linked Account(s)"
            };
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

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            if (config.BotToken == string.Empty)
                return;

            Discord.CreateClient(this, config.BotToken);

            timer.Every(config.UpdateInterval, () => UpdateStatus());
        }

        private void Unload() => Discord.CloseClient(Client);
        #endregion

        #region Status Update
        private void UpdateStatus()
        {
            if (config.Status.Count == 0)
                return;

            var index = GetStatusIndex();

            Client.UpdateStatus(new Presence()
            {
                Game = new Ext.Discord.DiscordObjects.Game()
                {
                    Name = Format(config.Status[index]),
                    Type = GetStatusType()
                }
            });

            statusIndex = index;
        }
        #endregion

        #region Helper Methods
        private int GetStatusIndex()
        {
            if (!config.Randomize)
                return (statusIndex + 1) % config.Status.Count;

            var index = 0;
            do index = Random.Range(0, config.Status.Count - 1);
            while (index == statusIndex);

            return index;
        }

        private ActivityType GetStatusType()
        {
            if (!StatusTypes.Contains(config.StatusType))
                PrintError($"Unknown Status Type '{config.StatusType}'");

            switch (config.StatusType)
            {
                case "Game":
                    return ActivityType.Game;
                case "Stream":
                    return ActivityType.Streaming;
                case "Listen":
                    return ActivityType.Listening;
                case "Watch":
                    return ActivityType.Watching;
                default:
                    return default(ActivityType);
            }
        }

        private string Format(string message)
        {
            message = message
                .Replace("{guild.name}", Client?.DiscordServer?.name ?? "{unknown}")
                .Replace("{members.total}", Client?.DiscordServer?.member_count.ToString() ?? "{unknown}")
                .Replace("{channels.total}", Client?.DiscordServer?.channels?.Count.ToString() ?? "{unknown}")
                .Replace("{server.hostname}", server.Name)
                .Replace("{server.maxplayers}", server.MaxPlayers.ToString())
                .Replace("{players.online}", players.Connected.Count().ToString())
                .Replace("{players.authenticated}", DiscordAuth != null ? GetAuthCount().ToString() : "{unknown}");

#if RUST
            message = message
                .Replace("{server.ip}", ConVar.Server.ip)
                .Replace("{server.port}", ConVar.Server.port.ToString())
                .Replace("{server.entities}", BaseNetworkable.serverEntities.Count.ToString())
                .Replace("{server.worldsize}", ConVar.Server.worldsize.ToString())
                .Replace("{server.seed}", ConVar.Server.seed.ToString())
                .Replace("{players.queued}", ConVar.Admin.ServerInfo().Queued.ToString())
                .Replace("{players.joining}", ConVar.Admin.ServerInfo().Joining.ToString())
                .Replace("{players.sleepers}", BasePlayer.sleepingPlayerList.Count.ToString())
                .Replace("{players.total}", players.Connected.Count() + BasePlayer.sleepingPlayerList.Count.ToString());
#endif

            return message;
        }

        private int GetAuthCount() => (int) DiscordAuth.Call("API_GetAuthCount");
        #endregion
    }
}
