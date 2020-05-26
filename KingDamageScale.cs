namespace Oxide.Plugins {
    [Info("King Damage Scaler", "Pho3niX90", "0.0.1")]
    [Description("Scale damage")]
    class KingDamageScale : RustPlugin {

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo) {
            if (entity == null || hitInfo == null) return;
            var attacker = hitInfo?.Initiator as BasePlayer;

            if(attacker is BasePlayer) {
                Puts("Weapno " + hitInfo.Weapon.ShortPrefabName);
                Puts("ammo " + hitInfo.Weapon?.GetItem()?.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.ammoType?.shortname);
            }
           //if (!string.IsNullOrEmpty(ammoName) ) {//&& ammoName.Equals("ammo.rocket.basic")) {
           //    float scale = 0.115f;
           //    hitInfo?.damageTypes?.ScaleAll(scale);
           //}
        }
    }
}
