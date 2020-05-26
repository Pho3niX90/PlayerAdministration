using UnityEngine;

namespace Oxide.Plugins {
    [Info("Delete Vending Machines", "Pho3niX90", "0.0.1")]
    [Description("Delete Vending Machines")]
    public class DelVM : RustPlugin {
        VendingMachine[] vmachines;
        [ChatCommand("delvms")]
        void DeleteVMS(BasePlayer player, string command, string[] args) {
            VendingMachine[] vmachines = GameObject.FindObjectsOfType<VendingMachine>();
            foreach (VendingMachine vm in vmachines) {
                if (vm.OwnerID == 0) {
                    vm.Kill();
                }
            }
        }
    }
}
