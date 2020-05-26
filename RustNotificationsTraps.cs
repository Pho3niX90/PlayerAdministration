using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Rust Notifications Traps", "Stephan E.G. Veenstra", "0.0.2")]
    [Description("Send notifications to the Rust Notifications App when traps are being triggered")]
    class RustNotificationsTraps : RustPlugin
    {
        // Requires 
        [PluginReference]
        private Plugin RustNotificationsCore;

        private static Dictionary<int, DateTime> timeoutCache = new Dictionary<int,DateTime>();

        private static readonly Dictionary<string, string> Traps = new Dictionary<string, string>() {
            { "guntrap.deployed", "Shotgun Trap" },
            { "flameturret.deployed", "Flame Turret" },
            { "beartrap", "Bear Trap" },
            { "landmine", "Landmine" },
        };

        private void Init()
        {
            Puts("RustNotificationsTraps Plugin Initialized");
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var initiator = info?.Initiator;

            if (initiator == null)
                return null;

            if (!Traps.ContainsKey(initiator.ShortPrefabName))
                return null;

            var tc = initiator.GetBuildingPrivilege();

            if (tc == null)
                return null;

            var trapName = Traps[initiator.ShortPrefabName];

            var filteredIds = FilterRecentlyNotified(tc.authorizedPlayers, initiator);

            if(filteredIds.Length > 0)
                SendAlert(trapName,filteredIds);

            return null;
        }

        private string[] FilterRecentlyNotified(List<ProtoBuf.PlayerNameID> authorizedPlayers, BaseEntity initiator)
        {
            var filtered = new List<string>();
            var currentTimeStamp = DateTime.UtcNow;

            authorizedPlayers.ForEach(player => {
                var cacheKey = initiator.GetInstanceID();

                if (!timeoutCache.ContainsKey(cacheKey))
                {
                    timeoutCache.Add(cacheKey, currentTimeStamp);
                    filtered.Add(player.userid.ToString());
                } else if(currentTimeStamp > timeoutCache[cacheKey].AddSeconds(60))
                {
                    timeoutCache[cacheKey] = currentTimeStamp;
                    filtered.Add(player.userid.ToString());
                }
            });
            return filtered.ToArray();
        }

        private void SendAlert(string trap,string[] userIds)
        {
            if(RustNotificationsCore == null)
            {
                Puts("RustNotificationsCore is not loaded!");
                return;
            }

            RustNotificationsCore.Call(
                "Send",
                "w",
                $"{trap} triggered!",
                $"Someone has triggered a {trap} within your building privilege.",
                userIds);
        }
    }
}
