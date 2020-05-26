using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Oxide.Plugins
{
    [Info("Craft Spam Blocker", "Iv Misticos", "1.0.1")]
    [Description("Prevents items from being crafted if the player's inventory is full")]
    class CraftSpamBlocker : RustPlugin
    {
        #region Variables
        
        private static List<PlayerController> _data = new List<PlayerController>();

        private static CraftSpamBlocker _ins;
        
        #endregion
        
        #region Configuration

        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Incorrect Crafts To Block")]
            public int IncorrectNeeded = 10;

            [JsonProperty(PropertyName = "Incorrect Craft Lifetime (In Seconds)")]
            public float IncorrectCraftLifetime = 5f;

            [JsonProperty(PropertyName = "Block Time (In Seconds)")]
            public float BlockTime = 300f;

            [JsonProperty(PropertyName = "Debug")]
            public bool Debug = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();
        
        #endregion
        
        #region Hooks

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Start Limited Crafting", "Your crafting was just limited for {time} seconds." },
                { "Limited Crafting", "Your crafting is limited. Please, stop spamming. Time left: {time}s" },
            }, this);
        }

        private object CanCraft(ItemCrafter crafter, ItemBlueprint blueprint, int amount) => Process(crafter, blueprint, amount);

        private void OnServerInitialized()
        {
            _ins = this;
            
            for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerInit(BasePlayer.activePlayerList[i]);
            }
        }

        private void Unload()
        {
            for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerDisconnected(BasePlayer.activePlayerList[i]);
            }
        }

        private void OnPlayerInit(BasePlayer player)
        {
            _data.Add(player.gameObject.AddComponent<PlayerController>());
            foreach (var task in player.inventory.crafting.queue)
            {
                Process(player.inventory.crafting, task.blueprint, task.amount);
            }
        }

        private void OnPlayerDisconnected(Object player)
        {
            var index = PlayerController.FindIndex(player);
            if (index == -1)
                return;
            
            UnityEngine.Object.Destroy(_data[index]);
            _data.RemoveAt(index);
        }

        #endregion
        
        #region Controller

        private class PlayerController : FacepunchBehaviour
        {
            public BasePlayer player;

            public List<float> craftHistory = new List<float>();
            
            public float blockStartTime;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            public object UpdateCraftTime()
            {
                PrintDebug($"Craft History Length: {craftHistory.Count}");
                
                var current = Time.realtimeSinceStartup;
                
                {
                    var diff = current - blockStartTime;
                    if (diff < _config.BlockTime)
                    {
                        player.ChatMessage(GetMsg("Limited Crafting", player.UserIDString)
                            .Replace("{time}", $"{Math.Round(_config.BlockTime - diff, 1)}"));
                        return false;
                    }
                }

                // Cleaning old entries here, cuz why would we need a timer for it? We need to access it only here :)
                for (var i = craftHistory.Count - 1; i >= 0; i--)
                {
                    var craft = craftHistory[i];
                    var diff = current - craft;
                    if (diff > _config.IncorrectCraftLifetime)
                        craftHistory.RemoveAt(i);
                }

                if (craftHistory.Count >= _config.IncorrectNeeded)
                {
                    player.ChatMessage(GetMsg("Start Limited Crafting", player.UserIDString)
                        .Replace("{time}", $"{Math.Round(_config.BlockTime, 1)}"));
                    blockStartTime = current;
                    return false;
                }
                
                craftHistory.Add(current);
                return null;
            }

            public static int FindIndex(Object player)
            {
                for (var i = 0; i < _data.Count; i++)
                {
                    if (_data[i].player == player)
                        return i;
                }

                return -1;
            }

            public static PlayerController Find(Object player)
            {
                var index = FindIndex(player);
                return index == -1 ? null : _data[index];
            }
        }
        
        #endregion
        
        #region Helpers

        private object Process(ItemCrafter crafter, ItemBlueprint blueprint, int amount)
        {
            PrintDebug("PROCESSING");
            var player = crafter.gameObject.GetComponent<BasePlayer>(); // Getting the player
            if (player == null)
            {
                PrintWarning("Crafter player is null");
                return null;
            }

            return Process(player, blueprint, amount);
        }

        private object Process(BasePlayer player, ItemBlueprint blueprint, int amount)
        {
            var inventory = player.inventory;
            if (inventory.containerMain.itemList.Count < inventory.containerMain.capacity ||
                inventory.containerBelt.itemList.Count < inventory.containerBelt.capacity)
                return null; // Return if inventory is NOT full

            var controller = PlayerController.Find(player);

            var toReturn = (object) null;
            for (var i = 0; i < amount; i += blueprint.targetItem.stackable)
            {
                var result = controller.UpdateCraftTime();
                if (result != null)
                    toReturn = result;
            }

            return toReturn;
        }

        private static string GetMsg(string key, string userId = null) => _ins.lang.GetMessage(key, _ins, userId);

        private static void PrintDebug(string message)
        {
            if (_config.Debug)
                Interface.Oxide.LogDebug(message);
        }

        #endregion
    }
}