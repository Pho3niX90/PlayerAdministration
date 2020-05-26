using Oxide.Core.Plugins;

namespace Oxide.Plugins {
    [Info("Rust Notifications Player Dead", "Stephan E.G. Veenstra", "0.0.2")]
    [Description("Send notifications to the Rust Notifications App when a sleeping player is killed")]
    class RustNotificationsPlayerDead : RustPlugin {
        // Requires 
        [PluginReference]
        private Plugin RustNotificationsCore;

        private void Init() {
            Puts("RustNotificationsRaid Plugin Initialized");
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info) {
            if (player.IPlayer?.Id == null || player.IsConnected)
                return null;

            SendAlert(new string[] { player.IPlayer.Id });
            return null;
        }

        private void SendAlert(string[] userIds) {
            if (RustNotificationsCore == null) {
                Puts("RustNotificationsCore is not loaded!");
                return;
            }

            if (userIds == null || userIds.Length < 1)
                return;

            RustNotificationsCore.Call(
                "Send",
                "a",
                $"You have died!",
                $"You died.",
                userIds);
        }
    }
}
