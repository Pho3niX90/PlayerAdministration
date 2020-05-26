using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Fast Ovens", "Orange", "1.0.1")]
    [Description("https://rustworkshop.space/resources/fast-ovens.159/")]
    public class FastOvens : RustPlugin
    {
        #region Oxide Hooks
        
        private void OnConsumeFuel(BaseOven oven, Item item, ItemModBurnable burnable) {
            SmeltItems(oven);
        }

        #endregion

        #region Core

        private void SmeltItems(BaseOven oven)
        {
            if (oven.inventory.capacity != 6 && oven.inventory.capacity != 18)
            {
                return;
            }
            
            foreach (var item in oven.inventory.itemList.ToArray())
            {
                var rate = 0;
                if (config.rates.TryGetValue(item.info.shortname, out rate) == false)
                {
                    continue;
                }
                
                var result = item.info.GetComponent<ItemModCookable>()?.becomeOnCooked;
                if (result == null)
                {
                    result = item.info.GetComponent<ItemModBurnable>()?.byproductItem;
                }
                
                Smelt(oven, item, rate, result);
            }
        }

        private static void Smelt(BaseOven oven, Item item, int rate, ItemDefinition result)
        {
            if (result == null)
            {
                return;
            }

            var amount = 0;
            if (item.amount > rate)
            {
                item.amount -= rate;
                item.MarkDirty();
                amount = rate;
            }
            else
            {
                item.GetHeldEntity()?.Kill();
                item.DoRemove();
                amount = item.amount;
            }
                
            var obj = ItemManager.Create(result, amount);
            if (obj.MoveToContainer(oven.inventory) == false)
            {
                obj.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
            }
        }

        #endregion
        
        #region Configuration | 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Rates")]
            public Dictionary<string, int> rates = new Dictionary<string, int>();
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
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");

                timer.Every(10f,
                    () =>
                    {
                        PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                    });
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private static void ValidateConfig()
        {
            if (config.rates.Count == 0)
            {
                config.rates = new Dictionary<string, int>
                {
                    {"sulfur.ore", 10},
                    {"hq.metal.ore", 10},
                    {"metal.ore", 10},
                    {"wood", 10}
                };
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}