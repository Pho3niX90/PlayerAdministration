using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("Stack Size Controller", "Pho3niX90", "0.0.1")]
    [Description("Allows you to multiply stack size of every item.")]
    public class StackSize : RustPlugin {
        #region Data

        private bool pluginLoaded = false;
        public Dictionary<string, int> defaultItemlist = new Dictionary<string, int>();
        public List<ItemDefinition> gameitemList;

        private bool LoadData() {
            gameitemList = ItemManager.itemList;
            var itemsdatafile = Interface.Oxide.DataFileSystem.GetFile("StackSize");
            try {
                defaultItemlist = itemsdatafile.ReadObject<Dictionary<string, int>>();

                foreach (ItemDefinition item in gameitemList) {
                    if (!defaultItemlist.ContainsKey(item.shortname)) defaultItemlist.Add(item.shortname, item.stackable);
                }

                SaveData();
                return true;
            } catch (Exception ex) {
                PrintWarning("Error: Data file is corrupt. Debug info: " + ex.Message);
                return false;
            }
        }

        private void UpdateStackSizes() {
            foreach (ItemDefinition item in gameitemList) {
                if (!config.MultiplyHealthItems && item.condition.enabled) continue;

                if (!defaultItemlist.ContainsKey(item.shortname)) {
                    defaultItemlist.Add(item.shortname, item.stackable);
                    item.stackable *= config.Multiply;
                } else if (item.stackable == defaultItemlist[item.shortname]) {
                    item.stackable *= config.Multiply;
                }
            }

            SaveData();
        }

        private void ResetStackSizes() {
            foreach (ItemDefinition item in gameitemList) item.stackable = defaultItemlist[item.shortname];
        }

        private void SaveData() {
            Interface.Oxide.DataFileSystem.WriteObject("StackSize_DefaultStacks", defaultItemlist);
        }

        #endregion

        #region Config
        private static ConfigData config = new ConfigData();
        private class ConfigData {
            [JsonProperty(PropertyName = "Multiply")]
            public int Multiply = 20;
            [JsonProperty(PropertyName = "Include Health Items")]
            public bool MultiplyHealthItems = false;
            [JsonProperty(PropertyName = "Multiply Health Items")]
            public int MultiplyHealth = 2;
        }

        private static void ValidateConfig() {
            if (config.Multiply == 0) config.Multiply = 20;
        }

        protected override void LoadDefaultConfig() {
            config = new ConfigData();
        }

        protected override void SaveConfig() {
            Config.WriteObject(config);
        }
        #endregion

        #region Hooks
        void Loaded() {
            pluginLoaded = LoadData();

            if (pluginLoaded) { UpdateStackSizes(); } else { Puts("Stack Sizes could not be changed due to a corrupt data file."); }
        }
        void Unload() {
            ResetStackSizes();
        }
        #endregion
    }
}