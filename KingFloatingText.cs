
using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("King Floating Text", "Pho3niX90", "0.0.1")]
    [Description("")]
    public class KingFloatingText : RustPlugin
    {
        private void OnServerInitialized() {
            timer.Every(1f, () => {
                RefreshFloatingTexts();
            });
        }

        private void RefreshFloatingTexts() {
            var warps = Interface.CallHook("GetAllZonesForFloatingText") as Dictionary<Vector3, string> ?? new Dictionary<Vector3, string>();

            foreach (var value in warps) {
                var position = value.Key;
                var gameName = value.Value;

                position = value.Key + config.warpsTextOffset;
                var text = value.Value;
                foreach (var player in BasePlayer.activePlayerList) {
                    if (Vector3.Distance(player.transform.position, position) > config.maxFloatingTextDistance) continue;

                    if (player.Connection.authLevel == 0) {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendNetworkUpdateImmediate();
                    }

                    player.SendConsoleCommand("ddraw.text", config.floatingTextRefreshRate, Color.white, position, text);

                    if (player.Connection.authLevel == 0) {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                        player.SendNetworkUpdateImmediate();
                    }
                }
            }
        }

        #region Config
        private static ConfigData config = new ConfigData();
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Floating text refresh rate")]
            public int floatingTextRefreshRate = 3;

            [JsonProperty(PropertyName = "Floating text maximal distance")]
            public int maxFloatingTextDistance = 50;

            [JsonProperty(PropertyName = "Warps text offset")]
            public Vector3 warpsTextOffset = new Vector3(0f, 0.5f, 0f);
        }
        protected override void LoadConfig() {
            base.LoadConfig();

            try {
                config = Config.ReadObject<ConfigData>();
                if (config == null) LoadDefaultConfig();
            } catch {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                timer.Every(10f,
                    () => {
                        PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                    });
                LoadDefaultConfig();
                return;
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new ConfigData();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion
    }
}