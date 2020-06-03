//Requires: RustNotificationsCore

using Oxide.Core.Plugins;
using Rust;
using System.Collections.Generic;
using System.Linq;

/**
 * Special thanks to Pho3niX90 for contributing to the development of this plugin.
 */

namespace Oxide.Plugins
{
    [Info("Rust Notifications Raid", "SEGVeenstra", "0.1.0")]
    [Description("Send notifications to the Rust Notifications App when walls, doors or the TC is destroyed")]
    class RustNotificationsRaid : RustPlugin
    {
        [PluginReference]
        private Plugin RustNotificationsCore;

        // List of Objects we're interrested in
        private Dictionary<string, string> BuildingBlocks = new Dictionary<string, string>
        {
            {"cupboard.tool.deployed", "Tool Cupboard"},
            {"wall", "Wall"},
            {"wall.doorway", "Wall"},
            {"wall.window", "Wall"},
            {"wall.low", "Wall"},
            {"wall.half", "Wall"},
            {"wall.frame", "Wall"},
            {"foundation", "Foundation"},
            {"foundation.triangle", "Foundation"},
            {"foundation.steps", "Steps"},
            {"floor", "Floor"},
            {"floor.triangle", "Floor"},
            {"floor.frame", "Floor"},
            {"roof", "Roof"},
            {"door.hinged.wood", "Door"},
            {"door.hinged.metal", "Door"},
            {"door.hinged.toptier", "Door"},
            {"door.double.hinged.toptier", "Door"},
            {"door.double.hinged.wood", "Door"},
            {"door.double.hinged.metal", "Door"},
            {"wall.frame.garagedoor", "Door"},
            {"wall.frame.shopfront.metal", "Shop Front"},
            {"wall.frame.shopfront.wood", "Shop Front"},
            {"wall.windows.bars.wood", "Window Bars"},
            {"wall.windows.bars.metal", "Window Bars"},
            {"wall.windows.bars.topteir", "Window Bars"},
            {"wall.window.glass.reinforced", "Window"},
            {"shutter.metal.embrasure.a", "Window Shutters"},
            {"shutter.metal.embrasure.b", "Window Shutters"},
        };

        private void Init()
        {
            Puts("RustNotificationsRaid Plugin Initialized");
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            // Ignore if the entity is destroyed by decaying or not in list.
            if ((info?.damageTypes?.Has(DamageType.Decay) ?? true))
                return; 

            if (entity is BuildingBlock)
            {
                var block = entity as BuildingBlock;
                if (block.grade == BuildingGrade.Enum.Twigs) return;
            }

            var tc = entity.GetBuildingPrivilege();

            // If there are no owners, ignore
            if (tc == null || tc.authorizedPlayers.Count == 0) return;

            // ignore player models death 
            if (entity.ShortPrefabName.Equals("player") || entity.ShortPrefabName.Equals("corpse")) return;

            // If the initiator has auth, ignore
            if (info?.InitiatorPlayer != null
                && tc.authorizedPlayers.FirstOrDefault(
                    ap => ap.userid.ToString() == info.InitiatorPlayer.UserIDString) != null) return;


            SendAlert(entity.ShortPrefabName,
                tc.authorizedPlayers.Select(p => p.userid.ToString()).ToArray());

            tc.authorizedPlayers?.ForEach(player => Puts($"{player.username}({player.userid.ToString()})"));
        }

        private void SendAlert(string entityName,string[] userIds)
        {
            if(RustNotificationsCore == null)
            {
                Puts("RustNotificationsCore is not loaded!");
                return;
            }

            if (userIds == null || userIds.Length < 1)
                return;

            RustNotificationsCore.Call(
                "Send",
                "a",
                $"{entityName} destroyed!",
                $"Your {entityName} has been destroyed! You might be under attack!",
                userIds);
        }
    }
}
