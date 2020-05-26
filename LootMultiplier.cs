using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("Loot Multiplier", "Pho3niX90", "1.1.3")]
    [Description("Multiply items in all loot containers in the game")]
    public class LootMultiplier : RustPlugin {
        #region Oxide Hooks
        void OnServerInitialized() {
            RefreshLootContainers();
        }

        private void OnLootSpawn(StorageContainer container) {
            timer.Once(config.delay, () => {
                Multiply(container);
            });
        }

        #endregion

        #region Core

        private void Multiply(StorageContainer container) {
            if (container == null) {
                return;
            }

            var multiplier = 0;
            if (config.containers.TryGetValue(container.ShortPrefabName, out multiplier) == false) {
                return;
            }

            foreach (var item in container.inventory.itemList.ToArray()) {
                var shortname = item.info.shortname;
                var category = item.info.category.ToString();

                if (config.blacklist.Contains(shortname) || config.blacklist.Contains(category)) {
                    continue;
                }

                if (item.hasCondition && config.multiplyItemsWithCondition == false) {
                    continue;
                }

                item.amount *= multiplier;
            }
        }

        private void RefreshLootContainers() {
            int count = 0;
            LootContainer[] loot = BaseNetworkable.FindObjectsOfType<LootContainer>();
            foreach (var container in loot) {
                ClearContainer(container);
                container.PopulateLoot();
                Multiply(container);
                count++;
            }
            Puts("Repopulated " + count.ToString() + " loot containers.");
        }

        void ClearContainer(LootContainer container) {
            while (container.inventory.itemList.Count > 0) {
                var item = container.inventory.itemList[0];
                item.RemoveFromContainer();
                item.Remove(0f);
            }
        }
        #endregion

        #region Configuration

        private static ConfigData config;

        private class ConfigData {
            [JsonProperty(PropertyName = "Shortname -> Multiplier")]
            public Dictionary<string, int> containers = new Dictionary<string, int>();

            [JsonProperty(PropertyName = "Multiply items with condition")]
            public bool multiplyItemsWithCondition;

            [JsonProperty(PropertyName = "Delay after spawning crate to multiply it")]
            public float delay;

            [JsonProperty(PropertyName = "Item Blacklist")]
            public List<string> blacklist = new List<string>();
        }

        private ConfigData GetDefaultConfig() {
            return new ConfigData {
                multiplyItemsWithCondition = false,
                delay = 1f,
                containers = new Dictionary<string, int>
                {
                    {"loot-barrel-1", 10},
                    {"loot-barrel-2", 10},
                    {"loot_barrel_1", 10},
                    {"loot_barrel_2", 10},
                    {"crate_underwater_basic", 10},
                    {"crate_underwater_advanced", 10},
                    {"foodbox", 10},
                    {"trash-pile-1", 210},
                    {"minecart", 10},
                    {"oil_barrel", 10},
                    {"crate_basic", 10},
                    {"crate_mine", 2},
                    {"crate_tools", 2},
                    {"crate_normal", 2},
                    {"crate_normal_2", 2},
                    {"crate_normal_2_food", 2},
                    {"crate_normal_2_medical", 2},
                    {"crate_elite", 2},
                    {"codelockedhackablecrate", 10},
                    {"bradley_crate", 10},
                    {"heli_crate", 10},
                    {"supply_drop", 10}
                },
                blacklist = new List<string> {
                }
            };
        }

        protected override void LoadConfig() {
            base.LoadConfig();

            try {
                config = Config.ReadObject<ConfigData>();

                if (config == null) {
                    LoadDefaultConfig();
                }
            } catch {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() {
            PrintError("Configuration file is corrupt(or not exists), creating new one!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig() {
            Config.WriteObject(config);
        }

        #endregion
    }
}