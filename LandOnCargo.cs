using UnityEngine;

namespace Oxide.Plugins {
    [Info("LandOnCargo", "Pho3niX90", "0.0.1")]
    public class LandOnCargo : RustPlugin {
        static LandOnCargo instance;

        void OnServerInitialized() {
            instance = this;
            UpdateMiniTriggers();
        }

        void Unload() {
            DestroyMiniTriggers();
        }

        void OnEntitySpawned(MiniCopter mini) {
            if (mini == null)
                return;

            mini.gameObject.AddComponent<MiniTrigger>();
        }

        void DestroyMiniTriggers() {
            var miniTriggers = UnityEngine.Object.FindObjectsOfType<MiniTrigger>();
            foreach (var miniTrigger in miniTriggers)
                UnityEngine.Object.Destroy(miniTrigger);
        }

        void UpdateMiniTriggers() {
            DestroyMiniTriggers();

            var miniCopters = UnityEngine.Object.FindObjectsOfType<MiniCopter>();
            foreach (var miniCopter in miniCopters)
                miniCopter.gameObject.AddComponent<MiniTrigger>();
        }

        public class MiniTrigger : MonoBehaviour {
            public MiniCopter MiniCopter;
            private uint netId;
            private bool isDestroyed = false;

            void Awake() {
                this.MiniCopter = this.gameObject.GetComponent<MiniCopter>();
            }

            void OnTriggerEnter(Collider col) {
                if (netId > 0) {
                    CancelInvoke("DelayedExit");
                    return;
                }

                if (!string.Equals(col.gameObject.name, "trigger")) return;

                var cargoShip = col.ToBaseEntity() as CargoShip;
                if (cargoShip == null)
                    return;

                netId = cargoShip.net.ID;
                MiniCopter.SetParent(cargoShip, true);
            }

            void OnTriggerExit(Collider col) {
                
                if (isDestroyed || netId <= 0)
                    return;

                if (col.ToBaseEntity().net.ID != netId)
                    return;

                Invoke("DelayedExit", 1.5f);
            }

            void DelayedExit() {
                netId = 0;
                if (isDestroyed || MiniCopter == null || MiniCopter.IsDestroyed) {
                    return;
                }

                MiniCopter.SetParent(null, true);
            }

            void OnDestroy() {
                isDestroyed = true;
                CancelInvoke("DelayedExit");

                if (MiniCopter == null || MiniCopter.IsDestroyed)
                    return;

                MiniCopter.SetParent(null, true, true);
            }
        }
    }
}
