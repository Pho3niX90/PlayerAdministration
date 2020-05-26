using Oxide.Core.Plugins;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Rust Notifications Vendor", "Stephan E.G. Veenstra", "0.0.2")]
    [Description("Send notifications to the Rust Notifications App when items are sold from a Vending Machine")]
    class RustNotificationsVendor : RustPlugin
    {
        // Requires 
        [PluginReference]
        private Plugin RustNotificationsCore;

        private void Init()
        {
            Puts("RustNotificationsVendor Plugin Initialized");
        }

        object OnBuyVendingItem(VendingMachine vm, BasePlayer player, int sellOrderId, int numberOfTransactions)
        {
            var sellOrder = vm.sellOrders.sellOrders[sellOrderId];

            var item = vm.inventory.FindItemByItemID(sellOrder.itemToSellID);
            var name = item.info.displayName.english;

            var tc = vm.GetBuildingPrivilege();

            if (tc == null) return null;

            var playersToNotify = tc.authorizedPlayers.Select(p => p.userid.ToString()).ToArray();

            SendAlert(name, playersToNotify);
            return null;
        }

        private void SendAlert(string item, string[] userIds)
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
                "s",
                $"Sold: {item}",
                $"{item} has been sold",
                userIds);
        }
    }
}
