using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Beds Cooldowns", "Orange", "1.1.1")]
    [Description("Allows to change cooldowns for respawns on bags and beds")]
    public class BedsCooldowns : RustPlugin
    {
        #region Oxide Hooks

        private void Init()
        {
            foreach (var value in config.list)
            {
                permission.RegisterPermission(value.perm, this);
            }
        }

        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList.ToList())
            {
                OnPlayerInit(player);
            }
        }

        private void OnEntitySpawned(SleepingBag entity)
        {
            var settings = GetSettings(entity.OwnerID.ToString());
            SetCooldown(entity, settings);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            CheckPlayer(player);
        }

        #endregion

        #region Core

        private void CheckPlayer(BasePlayer player)
        {
            var settings = GetSettings(player.UserIDString);
            if (settings == null) {return;}
            ServerMgr.Instance.StartCoroutine(CheckBags(player.userID, settings));
        }
        
        private void SetCooldown(SleepingBag entity, SettingsEntry info)
        {
            if (info == null) {return;}

            if (entity.ShortPrefabName.Contains("bed"))
            {
                entity.secondsBetweenReuses = info.bed;
                entity.unlockTime = info.unlockTimeBed + UnityEngine.Time.realtimeSinceStartup;
            }
            else
            {
                entity.secondsBetweenReuses = info.bag;
                entity.unlockTime = info.unlockTimeBag + UnityEngine.Time.realtimeSinceStartup;
            }
            
            entity.SendNetworkUpdate();
        }

        private SettingsEntry GetSettings(string playerID)
        {
            var num = -1;
            var info = (SettingsEntry) null;

            foreach (var value in config.list)
            {
                if (permission.UserHasPermission(playerID, value.perm))
                {
                    var priority = value.priority;
                    if (priority > num)
                    {
                        num = priority;
                        info = value;
                    }
                }
            }

            return info;
        }

        private IEnumerator CheckBags(ulong playerID, SettingsEntry settings)
        {
            foreach (var entity in SleepingBag.sleepingBags)
            {
                if (entity.OwnerID == playerID)
                {
                    SetCooldown(entity, settings);
                }
            }
            
            yield break;
        }

        #endregion
        
        #region Configuration 1.1.0

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "List")]
            public List<SettingsEntry> list = new List<SettingsEntry>();
        }
        
        private class SettingsEntry
        {
            [JsonProperty(PropertyName = "Permission")]
            public string perm;
            
            [JsonProperty(PropertyName = "Priority")]
            public int priority;
                
            [JsonProperty(PropertyName = "Sleeping bag cooldown")]
            public float bag;
                
            [JsonProperty(PropertyName = "Bed cooldown")]
            public float bed;

            [JsonProperty(PropertyName = "Sleeping bag unlock time")]
            public float unlockTimeBag;

            [JsonProperty(PropertyName = "Bed unlock time")]
            public float unlockTimeBed;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                list = new List<SettingsEntry>
                {
                    new SettingsEntry
                    {
                        perm = "bedscooldowns.vip1",
                        priority = 1,
                        bag = 100,
                        bed = 100,
                        unlockTimeBag = 50,
                        unlockTimeBed = 50,
                    },
                    new SettingsEntry
                    {
                        perm = "bedscooldowns.vip2",
                        priority = 2,
                        bag = 75,
                        bed = 75,
                        unlockTimeBag = 50,
                        unlockTimeBed = 50,
                    },
                    new SettingsEntry
                    {
                        perm = "bedscooldowns.vip3",
                        priority = 3,
                        bag = 0,
                        bed = 0,
                        unlockTimeBag = 50,
                        unlockTimeBed = 50,
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                Interface.Oxide.RootPluginManager.RemovePlugin(this);
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}