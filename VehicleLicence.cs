using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Licence", "Sorrow/TheDoc/Arainrr", "1.4.10")]
    [Description("Allows players to buy vehicles and then spawn or store it")]
    public class VehicleLicence : RustPlugin
    {
        #region Fields

        [PluginReference] private readonly Plugin Economics, ServerRewards, Friends, Clans, NoEscape;
        private const string PERMISSION_ALL = "vehiclelicence.all";
        private const string PERMISSION_ROWBOAT = "vehiclelicence.rowboat";
        private const string PERMISSION_RHIB = "vehiclelicence.rhib";
        private const string PERMISSION_SEDAN = "vehiclelicence.sedan";
        private const string PERMISSION_HOTAIRBALLOON = "vehiclelicence.hotairballoon";
        private const string PERMISSION_MINICOPTER = "vehiclelicence.minicopter";
        private const string PERMISSION_TRANSPORTCOPTER = "vehiclelicence.transportcopter";
        private const string PERMISSION_CHINOOK = "vehiclelicence.chinook";
        private const string PERMISSION_RIDABLEHORSE = "vehiclelicence.ridablehorse";
        private const string PERMISSION_BYPASS_COST = "vehiclelicence.bypasscost";

        private const string PREFAB_ROWBOAT = "assets/content/vehicles/boats/rowboat/rowboat.prefab";
        private const string PREFAB_RHIB = "assets/content/vehicles/boats/rhib/rhib.prefab";
        private const string PREFAB_SEDAN = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";
        private const string PREFAB_HOTAIRBALLOON = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab";
        private const string PREFAB_MINICOPTER = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        private const string PREFAB_TRANSPORTCOPTER = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";
        private const string PREFAB_CHINOOK = "assets/prefabs/npc/ch47/ch47.entity.prefab";
        private const string PREFAB_RIDABLEHORSE = "assets/rust.ai/nextai/testridablehorse.prefab";
        private const string PREFAB_ITEM_DROP = "assets/prefabs/misc/item drop/item_drop.prefab";

        private readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private readonly Dictionary<BaseEntity, Vehicle> vehiclesCache = new Dictionary<BaseEntity, Vehicle>();
        private readonly static int LAYER_GROUND = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed");
        private double CurrentTime => DateTime.Now.Subtract(epoch).TotalSeconds;

        private enum VehicleType
        {
            Rowboat,
            RHIB,
            Sedan,
            HotAirBalloon,
            MiniCopter,
            TransportHelicopter,
            Chinook,
            RidableHorse
        }

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            LoadData();
            UpdataOldData();
            permission.RegisterPermission(PERMISSION_ALL, this);
            permission.RegisterPermission(PERMISSION_ROWBOAT, this);
            permission.RegisterPermission(PERMISSION_RHIB, this);
            permission.RegisterPermission(PERMISSION_SEDAN, this);
            permission.RegisterPermission(PERMISSION_HOTAIRBALLOON, this);
            permission.RegisterPermission(PERMISSION_MINICOPTER, this);
            permission.RegisterPermission(PERMISSION_TRANSPORTCOPTER, this);
            permission.RegisterPermission(PERMISSION_CHINOOK, this);
            permission.RegisterPermission(PERMISSION_RIDABLEHORSE, this);
            permission.RegisterPermission(PERMISSION_BYPASS_COST, this);

            foreach (var perm in configData.permCooldown.Keys)
                if (!permission.PermissionExists(perm, this))
                    permission.RegisterPermission(perm, this);

            cmd.AddChatCommand(configData.chatS.helpCommand, this, nameof(CmdLicenceHelp));
            cmd.AddChatCommand(configData.chatS.buyCommand, this, nameof(CmdBuyVehicle));
            cmd.AddChatCommand(configData.chatS.spawnCommand, this, nameof(CmdSpawnVehicle));
            cmd.AddChatCommand(configData.chatS.recallCommand, this, nameof(CmdRecallVehicle));
        }

        private void OnServerInitialized()
        {
            if (!configData.settings.preventMounting) Unsubscribe(nameof(CanMountEntity));
            if (configData.settings.checkVehiclesTime > 0) CheckVehicles();
            else Unsubscribe(nameof(OnEntityDismounted));
            if (!configData.settings.noDecay) Unsubscribe(nameof(OnEntityTakeDamage));
        }

        private void Unload()
        {
            foreach (var entry in vehiclesCache.ToList())
            {
                if (entry.Key != null && !entry.Key.IsDestroyed)
                {
                    RefundFuel(entry.Key, entry.Value);
                    entry.Key.Kill(BaseNetworkable.DestroyMode.Gib);
                }
            }
            SaveData();
        }

        private void OnServerSave() => timer.Once(UnityEngine.Random.Range(0f, 60f), SaveData);

        private void OnPlayerConnected(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, PERMISSION_BYPASS_COST))
                PurchaseAllVehicles(player.userID);
        }

        private void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            var vehicleParent = entity?.VehicleParent();
            if (vehicleParent == null || vehicleParent.IsDestroyed) return;
            if (!vehiclesCache.ContainsKey(vehicleParent)) return;
            vehiclesCache[vehicleParent].lastDismount = CurrentTime;
        }

        private object CanMountEntity(BasePlayer friend, BaseMountable entity)
        {
            var vehicleParent = entity?.VehicleParent();
            if (vehicleParent == null || vehicleParent.IsDestroyed) return null;
            if (!vehiclesCache.ContainsKey(vehicleParent)) return null;
            var ownerID = vehiclesCache[vehicleParent].playerID;
            if (ownerID == friend.userID || AreFriends(ownerID.ToString(), friend.UserIDString)) return null;
            if (configData.settings.blockDriverSeat && vehicleParent.HasMountPoints() && entity != vehicleParent.mountPoints[0].mountable) return null;
            return false;
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null) return;
            if (!vehiclesCache.ContainsKey(entity)) return;
            if (hitInfo?.damageTypes?.Get(Rust.DamageType.Decay) > 0)
                hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info) => CheckEntity(entity, true);

        private void OnEntityKill(BaseEntity entity) => CheckEntity(entity);

        #endregion Oxide Hooks

        #region Update Old Data

        private void UpdataOldData()
        {
            Dictionary<ulong, LicencedPlayer> licencedPlayer;
            try { licencedPlayer = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, LicencedPlayer>>(Name); }
            catch { return; }
            foreach (var entry in licencedPlayer)
            {
                if (entry.Value.Vehicles.Count > 0)
                {
                    var vehicleTypes = new Dictionary<VehicleType, Vehicle>();
                    foreach (var vehicle in entry.Value.Vehicles)
                        vehicleTypes.Add(GetVehiclePrefab(vehicle.Value.Prefab), new Vehicle());
                    storedData.playerData.Add(entry.Key, vehicleTypes);
                }
            }
            SaveData();
        }

        private class LicencedPlayer
        {
            public readonly ulong Userid;
            public Dictionary<string, Vehicle1> Vehicles;
        }

        private class Vehicle1
        {
            public ulong Userid;
            public string Prefab;
            public uint Id;
            public TimeSpan Spawned;
            public DateTime LastDismount;
        }

        private VehicleType GetVehiclePrefab(string prefab)
        {
            switch (prefab)
            {
                case PREFAB_ROWBOAT: return VehicleType.Rowboat;
                case PREFAB_RHIB: return VehicleType.RHIB;
                case PREFAB_SEDAN: return VehicleType.Sedan;
                case PREFAB_HOTAIRBALLOON: return VehicleType.HotAirBalloon;
                case PREFAB_MINICOPTER: return VehicleType.MiniCopter;
                case PREFAB_TRANSPORTCOPTER: return VehicleType.TransportHelicopter;
                case PREFAB_CHINOOK: return VehicleType.Chinook;
                case PREFAB_RIDABLEHORSE: return VehicleType.RidableHorse;
            }
            return default(VehicleType);
        }

        #endregion Update Old Data

        #region Helpers

        private void CheckEntity(BaseEntity entity, bool isDeath = false)
        {
            if (entity == null) return;
            Vehicle vehicle;
            if (!vehiclesCache.TryGetValue(entity, out vehicle)) return;
            vehiclesCache.Remove(entity);
            if (!configData.settings.notRefundFuelOnCrash)
                RefundFuel(entity, vehicle);
            if (storedData.playerData.ContainsKey(vehicle.playerID) && storedData.playerData[vehicle.playerID].ContainsKey(vehicle.vehicleType))
            {
                if (isDeath && configData.settings.removeVehicleOnCrash)
                {
                    storedData.playerData[vehicle.playerID].Remove(vehicle.vehicleType);
                    return;
                }
                storedData.playerData[vehicle.playerID][vehicle.vehicleType].entityID = 0;
                storedData.playerData[vehicle.playerID][vehicle.vehicleType].lastDeath = CurrentTime;
            }
        }

        private void PurchaseAllVehicles(ulong playerID)
        {
            if (!storedData.playerData.ContainsKey(playerID)) storedData.playerData.Add(playerID, new Dictionary<VehicleType, Vehicle>());
            foreach (int value in Enum.GetValues(typeof(VehicleType)))
            {
                var vehicleType = (VehicleType)value;
                if (!storedData.playerData[playerID].ContainsKey(vehicleType))
                    storedData.playerData[playerID].Add(vehicleType, new Vehicle());
            }
            SaveData();
        }

        private void CheckVehicles()
        {
            foreach (var entry in vehiclesCache.ToList())
            {
                if (entry.Key == null || entry.Key.IsDestroyed) continue;
                if (VehicleAnyMounted(entry.Key)) continue;
                if (VehicleIsActive(entry.Value)) continue;
                RefundFuel(entry.Key, entry.Value);
                entry.Key.Kill(BaseNetworkable.DestroyMode.Gib);
            }
            timer.Once(configData.settings.checkVehiclesTime, CheckVehicles);
        }

        private bool VehicleIsActive(Vehicle vehicle)
        {
            var vehicleSetting = configData.vehicleS[vehicle.vehicleType];
            if (vehicleSetting.wipeTime <= 0) return true;
            return CurrentTime - vehicle.lastDismount < vehicleSetting.wipeTime;
        }

        private bool HasPermission(BasePlayer player, string key)
        {
            if (permission.UserHasPermission(player.UserIDString, PERMISSION_ALL)) return true;
            foreach (var entry in configData.vehicleS)
            {
                if (entry.Value.commands.Any(x => x.ToLower().Equals(key)))
                {
                    switch (entry.Key)
                    {
                        case VehicleType.Rowboat: return permission.UserHasPermission(player.UserIDString, PERMISSION_ROWBOAT);
                        case VehicleType.RHIB: return permission.UserHasPermission(player.UserIDString, PERMISSION_RHIB);
                        case VehicleType.Sedan: return permission.UserHasPermission(player.UserIDString, PERMISSION_SEDAN);
                        case VehicleType.HotAirBalloon: return permission.UserHasPermission(player.UserIDString, PERMISSION_HOTAIRBALLOON);
                        case VehicleType.MiniCopter: return permission.UserHasPermission(player.UserIDString, PERMISSION_MINICOPTER);
                        case VehicleType.TransportHelicopter: return permission.UserHasPermission(player.UserIDString, PERMISSION_TRANSPORTCOPTER);
                        case VehicleType.Chinook: return permission.UserHasPermission(player.UserIDString, PERMISSION_CHINOOK);
                        case VehicleType.RidableHorse: return permission.UserHasPermission(player.UserIDString, PERMISSION_RIDABLEHORSE);
                    }
                    return false;
                }
            }
            return false;
        }

        private string GetVehiclePrefab(VehicleType vehicleType)
        {
            switch (vehicleType)
            {
                case VehicleType.Rowboat: return PREFAB_ROWBOAT;
                case VehicleType.RHIB: return PREFAB_RHIB;
                case VehicleType.Sedan: return PREFAB_SEDAN;
                case VehicleType.HotAirBalloon: return PREFAB_HOTAIRBALLOON;
                case VehicleType.MiniCopter: return PREFAB_MINICOPTER;
                case VehicleType.TransportHelicopter: return PREFAB_TRANSPORTCOPTER;
                case VehicleType.Chinook: return PREFAB_CHINOOK;
                case VehicleType.RidableHorse: return PREFAB_RIDABLEHORSE;
            }
            return string.Empty;
        }

        private bool AreFriends(string playerID, string friendID)
        {
            if (configData.settings.useFriends && Friends != null)
            {
                var r = Friends.CallHook("HasFriend", playerID, friendID);
                if (r != null && (bool)r) return true;
            }
            if (configData.settings.useClans && Clans != null)
            {
                if (Clans.ResourceId == 842)//Rust:IO Clans
                {
                    var playerClan = Clans.Call("GetClanOf", playerID);
                    var friendClan = Clans.Call("GetClanOf", friendID);
                    if (playerClan != null && friendClan != null)
                        return (string)playerClan == (string)friendClan;
                }
                else//Clans
                {
                    var isMember = Clans.Call("IsClanMember", playerID, friendID);
                    if (isMember != null && (bool)isMember) return true;
                }
            }
            return false;
        }

        private bool VehicleAnyMounted(BaseEntity entity)
        {
            if (entity is BaseVehicle) return (entity as BaseVehicle).AnyMounted();
            List<BasePlayer> players = Facepunch.Pool.GetList<BasePlayer>();
            Vis.Entities(entity.transform.position, 2.5f, players, Rust.Layers.Server.Players);
            bool flag = players.Count > 0;
            Facepunch.Pool.FreeList(ref players);
            return flag;
        }

        private void RefundFuel(BaseEntity entity, Vehicle vehicle)
        {
            ItemContainer itemContainer = null;
            switch (vehicle.vehicleType)
            {
                case VehicleType.Chinook:
                case VehicleType.Sedan:
                    return;

                case VehicleType.MiniCopter:
                case VehicleType.TransportHelicopter:
                    itemContainer = (entity as MiniCopter)?.fuelStorageInstance.Get(true)?.GetComponent<StorageContainer>()?.inventory;
                    break;

                case VehicleType.HotAirBalloon:
                    itemContainer = (entity as HotAirBalloon)?.fuelStorageInstance.Get(true)?.GetComponent<StorageContainer>()?.inventory;
                    break;

                case VehicleType.RHIB:
                case VehicleType.Rowboat:
                    itemContainer = (entity as MotorRowboat)?.fuelStorageInstance.Get(true)?.GetComponent<StorageContainer>()?.inventory;
                    break;

                case VehicleType.RidableHorse:
                    itemContainer = (entity as RidableHorse)?.inventory;
                    break;
            }
            if (itemContainer == null) return;
            var player = BasePlayer.FindAwakeOrSleeping(vehicle.playerID.ToString());
            if (player == null) itemContainer.Drop(PREFAB_ITEM_DROP, entity.GetDropPosition(), entity.transform.rotation);
            else if (itemContainer?.itemList?.Count > 0)
            {
                foreach (var item in itemContainer.itemList.ToList())
                    player.GiveItem(item);
                Print(player, Lang("RefundedVehicleFuel", player.UserIDString, configData.vehicleS[vehicle.vehicleType].displayName));
            }
        }

        private bool IsInWater(BasePlayer player)
        {
            var modelState = player.modelState;
            return modelState != null && modelState.waterLevel > 0;
        }

        private bool IsBlocked(BasePlayer player)
        {
            if (configData.settings.useRaidBlocker && IsRaidBlocked(player.UserIDString))
            {
                Print(player, Lang("RaidBlocked", player.UserIDString));
                return true;
            }
            if (configData.settings.useCombatBlocker && IsCombatBlocked(player.UserIDString))
            {
                Print(player, Lang("CombatBlocked", player.UserIDString));
                return true;
            }
            return false;
        }

        private bool IsRaidBlocked(string playerID) => (bool)(NoEscape?.Call("IsRaidBlocked", playerID) ?? false);

        private bool IsCombatBlocked(string playerID) => (bool)(NoEscape?.Call("IsCombatBlocked", playerID) ?? false);

        #region API

        private bool HasVehicle(ulong playerID, string type)
        {
            VehicleType vehicleType;
            if (!Enum.TryParse(type, true, out vehicleType)) return false;
            return storedData.playerData.ContainsKey(playerID) && storedData.playerData[playerID].ContainsKey(vehicleType);
        }

        private List<string> GetPlayerVehicles(ulong playerID) => storedData.playerData.ContainsKey(playerID) ? storedData.playerData[playerID].Keys.Select(x => x.ToString()).ToList() : new List<string>();

        #endregion API

        #endregion Helpers

        #region Commands

        #region Help

        private void CmdLicenceHelp(BasePlayer player, string command, string[] args)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Lang("Help", player.UserIDString));
            stringBuilder.AppendLine(Lang("HelpLicence1", player.UserIDString, configData.chatS.buyCommand));
            stringBuilder.AppendLine(Lang("HelpLicence2", player.UserIDString, configData.chatS.spawnCommand));
            stringBuilder.AppendLine(Lang("HelpLicence3", player.UserIDString, configData.chatS.recallCommand));
            Print(player, stringBuilder.ToString());
        }

        #endregion Help

        #region Buy

        [ConsoleCommand("vl.buy")]
        private void CCmdBuyVehicle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null && arg.IsAdmin && arg.Args != null && arg.Args.Length == 2)
            {
                player = RustCore.FindPlayer(arg.Args[1]);
                if (player == null)
                {
                    Print(arg, $"Player '{arg.Args[1]}' not found");
                    return;
                }
                IsBuyOption(player, arg.Args[0].ToLower(), false);
                return;
            }
            if (player != null) CmdBuyVehicle(player, string.Empty, arg.Args);
            else Print(arg, $"The server console cannot use '{arg.cmd.FullName}'");
        }

        private void CmdBuyVehicle(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length < 1)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(Lang("Help", player.UserIDString));
                foreach (var entry in configData.vehicleS)
                {
                    if (entry.Value.purchasable && entry.Value.commands.Count > 0)
                    {
                        var price = string.Join(", ", from p in entry.Value.price select $"{p.Value.displayName} x{p.Value.amount}");
                        stringBuilder.AppendLine(Lang("HelpBuy", player.UserIDString, configData.chatS.buyCommand, entry.Value.commands[0], entry.Value.displayName, price));
                    }
                }
                Print(player, stringBuilder.ToString());
                return;
            }
            var arg = args[0].ToLower();
            if (configData.settings.usePermission && !HasPermission(player, arg))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            if (IsBlocked(player)) return;
            if (!storedData.playerData.ContainsKey(player.userID))
                storedData.playerData.Add(player.userID, new Dictionary<VehicleType, Vehicle>());
            IsBuyOption(player, arg);
        }

        private bool IsBuyOption(BasePlayer player, string key, bool pay = true)
        {
            foreach (var entry in configData.vehicleS)
            {
                if (entry.Value.commands.Any(x => x.ToLower().Equals(key)))
                {
                    BuyVehicle(player, entry.Key, pay);
                    return true;
                }
            }
            Print(player, Lang("OptionNotFound", player.UserIDString, key));
            return false;
        }

        private void BuyVehicle(BasePlayer player, VehicleType vehicleType, bool pay = true)
        {
            var vehicleSetting = configData.vehicleS[vehicleType];
            if (!vehicleSetting.purchasable)
            {
                Print(player, Lang("VehicleCannotBeBuyed", player.UserIDString, vehicleSetting.displayName));
                return;
            }
            if (storedData.playerData[player.userID].ContainsKey(vehicleType))
            {
                Print(player, Lang("VehicleAlreadyPurchased", player.UserIDString, vehicleSetting.displayName));
                return;
            }
            if (pay && !BuyVehicle(player, vehicleSetting)) return;
            storedData.playerData[player.userID].Add(vehicleType, new Vehicle());
            Print(player, Lang("VehiclePurchased", player.UserIDString, vehicleSetting.displayName, configData.chatS.spawnCommand));
            SaveData();
        }

        private bool BuyVehicle(BasePlayer player, ConfigData.VehicleSetting vehicleSetting)
        {
            if (permission.UserHasPermission(player.UserIDString, PERMISSION_BYPASS_COST)) return true;
            if (!CanPay(player, vehicleSetting))
            {
                Print(player, Lang("NoMoney", player.UserIDString, vehicleSetting.displayName));
                return false;
            }
            List<Item> collect = new List<Item>();
            foreach (var entry in vehicleSetting.price)
            {
                if (entry.Value.amount <= 0) continue;
                var item = ItemManager.FindItemDefinition(entry.Key);
                if (item != null)
                {
                    player.inventory.Take(collect, item.itemid, entry.Value.amount);
                    player.Command("note.inv", item.itemid, -entry.Value.amount);
                }
                else if (!CheckOrPay(entry.Key, entry.Value.amount, player.userID)) return false;
            }
            foreach (Item item in collect) item.Remove();
            return true;
        }

        private bool CanPay(BasePlayer player, ConfigData.VehicleSetting vehicleSetting)
        {
            foreach (var entry in vehicleSetting.price)
            {
                if (entry.Value.amount <= 0) continue;
                var item = ItemManager.FindItemDefinition(entry.Key);
                if (item != null)
                {
                    int amount = player.inventory.GetAmount(item.itemid);
                    if (amount < entry.Value.amount) return false;
                }
                else if (!CheckOrPay(entry.Key, entry.Value.amount, player.userID, true)) return false;
            }
            return true;
        }

        private bool CheckOrPay(string key, int price, ulong playerID, bool check = false)
        {
            switch (key.ToLower())
            {
                case "economics":
                    if (Economics == null) return false;
                    if (check)
                    {
                        var b = Economics.CallHook("Balance", playerID);
                        if (b == null || (double)b < price) return false;
                    }
                    else
                    {
                        var w = Economics.CallHook("Withdraw", playerID, (double)price);
                        if (w == null || !(bool)w) return false;
                    }
                    return true;

                case "serverrewards":
                    if (ServerRewards == null) return false;
                    if (check)
                    {
                        var c = ServerRewards.CallHook("CheckPoints", playerID);
                        if (c == null || (int)c < price) return false;
                    }
                    else
                    {
                        var t = ServerRewards.CallHook("TakePoints", playerID, price);
                        if (t == null || !(bool)t) return false;
                    }
                    return true;

                default:
                    return true;
            }
        }

        #endregion Buy

        #region Spawn

        [ConsoleCommand("vl.spawn")]
        private void CCmdSpawnVehicle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) Print(arg, $"The server console cannot use '{arg.cmd.FullName}'");
            else CmdSpawnVehicle(player, string.Empty, arg.Args);
        }

        private void CmdSpawnVehicle(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length < 1)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(Lang("Help", player.UserIDString));
                foreach (var entry in configData.vehicleS)
                    if (entry.Value.purchasable && entry.Value.commands.Count > 0)
                        stringBuilder.AppendLine(Lang("HelpSpawn", player.UserIDString, configData.chatS.spawnCommand, entry.Value.commands[0], entry.Value.displayName));
                Print(player, stringBuilder.ToString());
                return;
            }
            var arg = args[0].ToLower();
            if (configData.settings.usePermission && !HasPermission(player, arg))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            if (IsBlocked(player)) return;
            if (!storedData.playerData.ContainsKey(player.userID))
                storedData.playerData.Add(player.userID, new Dictionary<VehicleType, Vehicle>());
            IsSpawnOption(player, arg);
        }

        private bool IsSpawnOption(BasePlayer player, string key)
        {
            foreach (var entry in configData.vehicleS)
            {
                if (entry.Value.commands.Any(x => x.ToLower().Equals(key)))
                {
                    if (entry.Key == VehicleType.Rowboat || entry.Key == VehicleType.RHIB ? CanSpawn(player, entry.Key, true) : CanSpawn(player, entry.Key))
                        SpawnVehicle(player, entry.Key);
                    return true;
                }
            }
            Print(player, Lang("OptionNotFound", player.UserIDString, key));
            return false;
        }

        private bool CanSpawn(BasePlayer player, VehicleType vehicleType, bool checkWater = false)
        {
            var vehicleSetting = configData.vehicleS[vehicleType];
            if (player.IsBuildingBlocked())
            {
                Print(player, Lang("BuildindBlocked", player.UserIDString, vehicleSetting.displayName));
                return false;
            }
            if (!storedData.playerData[player.userID].ContainsKey(vehicleType))
            {
                Print(player, Lang("VehicleNotYetPurchased", player.UserIDString, vehicleSetting.displayName));
                return false;
            }
            var vehicle = storedData.playerData[player.userID][vehicleType];
            if (vehicle.entityID != 0)
            {
                Print(player, Lang("AlreadyVehicleOut", player.UserIDString, vehicleSetting.displayName, configData.chatS.recallCommand));
                return false;
            }
            if (checkWater && !IsInWater(player))
            {
                Print(player, Lang("NotInWater", player.UserIDString, vehicleSetting.displayName));
                return false;
            }
            var cooldown = GetCooldown(player, vehicleType, vehicleSetting.cooldown);
            if (cooldown > 0)
            {
                var timeleft = Math.Ceiling(cooldown - (CurrentTime - vehicle.lastDeath));
                if (timeleft > 0)
                {
                    Print(player, Lang("VehicleOnCooldown", player.UserIDString, timeleft.ToString(), vehicleSetting.displayName));
                    return false;
                }
            }
            return true;
        }

        private double GetCooldown(BasePlayer player, VehicleType vehicleType, double cooldown)
        {
            foreach (var entry in configData.permCooldown)
            {
                if (permission.UserHasPermission(player.UserIDString, entry.Key) && entry.Value.ContainsKey(vehicleType) && cooldown > entry.Value[vehicleType])
                    cooldown = entry.Value[vehicleType];
            }
            return cooldown;
        }

        private void SpawnVehicle(BasePlayer player, VehicleType vehicleType)
        {
            var prefab = GetVehiclePrefab(vehicleType);
            if (string.IsNullOrEmpty(prefab)) return;
            var vehicleSetting = configData.vehicleS[vehicleType];

            Vector3 position = Vector3.zero;
            if (configData.settings.spawnLookingAt) position = player.transform.position + player.eyes.HeadForward() * vehicleSetting.distance;
            else
            {
                var circle = UnityEngine.Random.insideUnitCircle * vehicleSetting.distance;
                position = player.transform.position + new Vector3(circle.x, 0, circle.y);
            }
            position = GetGroundPosition(position);
            var entity = GameManager.server.CreateEntity(prefab, position + new Vector3(0f, 1.8f, 0f), player.transform.rotation);
            if (entity == null) return;
            entity.enableSaving = false;
            entity.OwnerID = player.userID;
            entity.Spawn();

            if (configData.settings.noServerGibs && entity is BaseVehicle)
                (entity as BaseVehicle).serverGibs.guid = string.Empty;
            if (configData.settings.noFireBall && entity is BaseHelicopterVehicle)
                (entity as BaseHelicopterVehicle).fireBall.guid = string.Empty;
            var vehicle = new Vehicle { playerID = player.userID, vehicleType = vehicleType, entityID = entity.net.ID, lastDismount = CurrentTime };
            vehiclesCache.Add(entity, vehicle);
            storedData.playerData[player.userID][vehicleType] = vehicle;
            Print(player, Lang("VehicleSpawned", player.UserIDString, vehicleSetting.displayName));
        }

        private Vector3 GetGroundPosition(Vector3 position)
        {
            position.y += 100f;
            RaycastHit hitInfo;
            if (Physics.Raycast(position, Vector3.down, out hitInfo, 200f, LAYER_GROUND)) position.y = hitInfo.point.y;
            else position.y = TerrainMeta.HeightMap.GetHeight(position);
            return position;
        }

        #endregion Spawn

        #region Recall

        [ConsoleCommand("vl.recall")]
        private void CCmdRecallVehicle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) Print(arg, $"The server console cannot use '{arg.cmd.FullName}'");
            else CmdRecallVehicle(player, string.Empty, arg.Args);
        }

        private void CmdRecallVehicle(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length < 1)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(Lang("Help", player.UserIDString));
                foreach (var entry in configData.vehicleS)
                    if (entry.Value.purchasable && entry.Value.commands.Count > 0)
                        stringBuilder.AppendLine(Lang("HelpRecall", player.UserIDString, configData.chatS.recallCommand, entry.Value.commands[0], entry.Value.displayName));
                Print(player, stringBuilder.ToString());
                return;
            }
            var arg = args[0].ToLower();
            if (configData.settings.usePermission && !HasPermission(player, arg))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            if (IsBlocked(player)) return;
            if (!storedData.playerData.ContainsKey(player.userID))
                storedData.playerData.Add(player.userID, new Dictionary<VehicleType, Vehicle>());
            IsReallOption(player, arg);
        }

        private bool IsReallOption(BasePlayer player, string key)
        {
            foreach (var entry in configData.vehicleS)
            {
                if (entry.Value.commands.Any(x => x.ToLower().Equals(key)))
                {
                    RemoveVehicle(player, entry.Key);
                    return true;
                }
            }
            Print(player, Lang("OptionNotFound", player.UserIDString, key));
            return false;
        }

        private void RemoveVehicle(BasePlayer player, VehicleType vehicleType)
        {
            string vehicleName = configData.vehicleS[vehicleType].displayName;
            if (!storedData.playerData[player.userID].ContainsKey(vehicleType))
            {
                Print(player, Lang("VehicleNotYetPurchased", player.UserIDString, vehicleName));
                return;
            }
            if (storedData.playerData[player.userID][vehicleType].entityID != 0)
            {
                foreach (var entry in vehiclesCache.ToList())
                {
                    if (entry.Value.playerID == player.userID && entry.Value.vehicleType == vehicleType)
                    {
                        if (entry.Key != null && !entry.Key.IsDestroyed)
                        {
                            if (configData.settings.checkAnyMounted && VehicleAnyMounted(entry.Key))
                            {
                                Print(player, Lang("PlayerMountedOnVehicle", player.UserIDString, vehicleName));
                                return;
                            }
                            RefundFuel(entry.Key, entry.Value);
                            entry.Key.Kill(BaseNetworkable.DestroyMode.Gib);
                        }
                        Print(player, Lang("VehicleRecalled", player.UserIDString, vehicleName));
                        return;
                    }
                }
            }
            Print(player, Lang("VehicleNotOut", player.UserIDString, vehicleName));
        }

        #endregion Recall

        #endregion Commands

        #region ConfigurationFile

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Settings")]
            public Settings settings = new Settings();

            public class Settings
            {
                [JsonProperty(PropertyName = "Interval to check vehicle for wipe (Seconds)")]
                public float checkVehiclesTime = 300;

                [JsonProperty(PropertyName = "Prevent other players from mounting vehicle")]
                public bool preventMounting = true;

                [JsonProperty(PropertyName = "Prevent mounting on driver's seat only")]
                public bool blockDriverSeat = true;

                [JsonProperty(PropertyName = "Check if any player mounted when recalling a vehicle")]
                public bool checkAnyMounted = true;

                [JsonProperty(PropertyName = "Spawn vehicle in the direction you are looking at")]
                public bool spawnLookingAt = true;

                [JsonProperty(PropertyName = "Use Clans")]
                public bool useClans = true;

                [JsonProperty(PropertyName = "Use Friends")]
                public bool useFriends = true;

                [JsonProperty(PropertyName = "Use Permission")]
                public bool usePermission = true;

                [JsonProperty(PropertyName = "Vehicle No Decay")]
                public bool noDecay = false;

                [JsonProperty(PropertyName = "Vehicle No Fire Ball")]
                public bool noFireBall = true;

                [JsonProperty(PropertyName = "Vehicle No Server Gibs")]
                public bool noServerGibs = true;

                [JsonProperty(PropertyName = "Remove Vehicles On Crash")]
                public bool removeVehicleOnCrash = false;

                [JsonProperty(PropertyName = "Not Refund Fuel On Crash")]
                public bool notRefundFuelOnCrash = false;

                [JsonProperty(PropertyName = "Clear Vehicle Data On Map Wipe")]
                public bool clearVehicleOnWipe = false;

                [JsonProperty(PropertyName = "Use Raid Blocker (Need NoEscape Plugin)")]
                public bool useRaidBlocker = false;

                [JsonProperty(PropertyName = "Use Combat Blocker (Need NoEscape Plugin)")]
                public bool useCombatBlocker = false;
            }

            [JsonProperty(PropertyName = "Chat Settings")]
            public ChatSettings chatS = new ChatSettings();

            public class ChatSettings
            {
                [JsonProperty(PropertyName = "Help Chat Command")]
                public string helpCommand = "license";

                [JsonProperty(PropertyName = "Buy Chat Command")]
                public string buyCommand = "buy";

                [JsonProperty(PropertyName = "Spawn Chat Command")]
                public string spawnCommand = "spawn";

                [JsonProperty(PropertyName = "Recall Chat Command")]
                public string recallCommand = "recall";

                [JsonProperty(PropertyName = "Chat Prefix")]
                public string prefix = "[VehicleLicence]: ";

                [JsonProperty(PropertyName = "Chat Prefix Color")]
                public string prefixColor = "#B366FF";

                [JsonProperty(PropertyName = "Chat SteamID Icon")]
                public ulong steamIDIcon = 76561198924840872;
            }

            [JsonProperty(PropertyName = "Cooldown Permission Settings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, Dictionary<VehicleType, float>> permCooldown = new Dictionary<string, Dictionary<VehicleType, float>>()
            {
                ["vehiclelicence.vip"] = new Dictionary<VehicleType, float>
                {
                    [VehicleType.Rowboat] = 90f,
                    [VehicleType.RHIB] = 150f,
                    [VehicleType.Sedan] = 450f,
                    [VehicleType.HotAirBalloon] = 450f,
                    [VehicleType.MiniCopter] = 900f,
                    [VehicleType.TransportHelicopter] = 1200f,
                    [VehicleType.Chinook] = 1500f,
                    [VehicleType.RidableHorse] = 1500f,
                }
            };

            [JsonProperty(PropertyName = "Vehicle Settings")]
            public Dictionary<VehicleType, VehicleSetting> vehicleS = new Dictionary<VehicleType, VehicleSetting>()
            {
                [VehicleType.Rowboat] = new VehicleSetting { displayName = "Row Boat", purchasable = true, cooldown = 180, distance = 3, commands = new List<string> { "row", "rowboat" }, price = new Dictionary<string, VehicleSetting.ItemInfo> { ["scrap"] = new VehicleSetting.ItemInfo { amount = 500, displayName = "Scrap" } } },
                [VehicleType.RHIB] = new VehicleSetting { displayName = "RHIB", purchasable = true, cooldown = 300, distance = 10, commands = new List<string> { "rhib" }, price = new Dictionary<string, VehicleSetting.ItemInfo> { ["scrap"] = new VehicleSetting.ItemInfo { amount = 1000, displayName = "Scrap" } } },
                [VehicleType.Sedan] = new VehicleSetting { displayName = "Sedan", purchasable = true, cooldown = 180, distance = 5, commands = new List<string> { "car", "sedan" }, price = new Dictionary<string, VehicleSetting.ItemInfo> { ["scrap"] = new VehicleSetting.ItemInfo { amount = 300, displayName = "Scrap" } } },
                [VehicleType.HotAirBalloon] = new VehicleSetting { displayName = "Hot Air Balloon", purchasable = true, cooldown = 900, distance = 20, commands = new List<string> { "hab", "hotairballoon" }, price = new Dictionary<string, VehicleSetting.ItemInfo> { ["scrap"] = new VehicleSetting.ItemInfo { amount = 5000, displayName = "Scrap" } } },
                [VehicleType.MiniCopter] = new VehicleSetting { displayName = "Mini Copter", purchasable = true, cooldown = 1800, distance = 8, commands = new List<string> { "mini", "minicopter" }, price = new Dictionary<string, VehicleSetting.ItemInfo> { ["scrap"] = new VehicleSetting.ItemInfo { amount = 10000, displayName = "Scrap" } } },
                [VehicleType.TransportHelicopter] = new VehicleSetting { displayName = "Transport Copter", purchasable = true, cooldown = 2400, distance = 10, commands = new List<string> { "scrapcopter", "transportcopter" }, price = new Dictionary<string, VehicleSetting.ItemInfo> { ["scrap"] = new VehicleSetting.ItemInfo { amount = 20000, displayName = "Scrap" } } },
                [VehicleType.Chinook] = new VehicleSetting { displayName = "Chinook", purchasable = true, cooldown = 3000, distance = 20, commands = new List<string> { "ch47", "chinook" }, price = new Dictionary<string, VehicleSetting.ItemInfo> { ["scrap"] = new VehicleSetting.ItemInfo { amount = 30000, displayName = "Scrap" } } },
                [VehicleType.RidableHorse] = new VehicleSetting { displayName = "Ridable Horse", purchasable = true, cooldown = 3000, distance = 5, commands = new List<string> { "horse", "ridablehorse" }, price = new Dictionary<string, VehicleSetting.ItemInfo> { ["scrap"] = new VehicleSetting.ItemInfo { amount = 700, displayName = "Scrap" } } }
            };

            public class VehicleSetting
            {
                [JsonProperty(PropertyName = "Purchasable")]
                public bool purchasable = true;

                [JsonProperty(PropertyName = "Vehicle Display Name")]
                public string displayName = string.Empty;

                [JsonProperty(PropertyName = "Distance To Spawn")]
                public float distance = 5f;

                [JsonProperty(PropertyName = "Cooldown (Seconds)")]
                public double cooldown = 180;

                [JsonProperty(PropertyName = "Time Before Vehicle Wipe (Seconds)")]
                public double wipeTime = 1800;

                [JsonProperty(PropertyName = "Commands")]
                public List<string> commands = new List<string>();

                [JsonProperty(PropertyName = "Price")]
                public Dictionary<string, ItemInfo> price = new Dictionary<string, ItemInfo>();

                public class ItemInfo
                {
                    public int amount;
                    public string displayName;
                }
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintError("The configuration file is corrupted");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            configData = new ConfigData();
        }

        protected override void SaveConfig() => Config.WriteObject(configData);

        #endregion ConfigurationFile

        #region DataFile

        private StoredData storedData;

        private class StoredData
        {
            public Dictionary<ulong, Dictionary<VehicleType, Vehicle>> playerData = new Dictionary<ulong, Dictionary<VehicleType, Vehicle>>();
        }

        private class Vehicle
        {
            public double lastDeath;
            [JsonIgnore] public uint entityID;
            [JsonIgnore] public ulong playerID;
            [JsonIgnore] public double lastDismount;
            [JsonIgnore] public VehicleType vehicleType;
        }

        private void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch
            {
                ClearData();
            }
        }

        private void ClearData()
        {
            storedData = new StoredData();
            SaveData();
        }

        private void OnNewSave(string filename)
        {
            if (configData.settings.clearVehicleOnWipe) ClearData();
            else
            {
                foreach (var entry in storedData.playerData.ToList())
                    if (entry.Value.Count <= 0)
                        storedData.playerData.Remove(entry.Key);
                SaveData();
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);

        #endregion DataFile

        #region LanguageFile

        private void Print(BasePlayer player, string message) => Player.Message(player, message, $"<color={configData.chatS.prefixColor}>{configData.chatS.prefix}</color>", configData.chatS.steamIDIcon);

        private void Print(ConsoleSystem.Arg arg, string message)
        {
            var player = arg.Player();
            if (player == null) Puts(message);
            else PrintToConsole(player, message);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Help"] = "These are the available commands:",
                ["HelpLicence1"] = "<color=#4DFF4D>/{0}</color> -- To buy a vehicle",
                ["HelpLicence2"] = "<color=#4DFF4D>/{0}</color> -- To spawn a vehicle",
                ["HelpLicence3"] = "<color=#4DFF4D>/{0}</color> -- To recall a vehicle",
                ["HelpBuy"] = "<color=#4DFF4D>/{0} {1}</color> -- To buy a {2}, price: <color=#FF1919>{3}</color>",
                ["HelpSpawn"] = "<color=#4DFF4D>/{0} {1}</color> -- To spawn a {2}",
                ["HelpRecall"] = "<color=#4DFF4D>/{0} {1}</color> -- To recall a {2}",

                ["NotAllowed"] = "You do not have permission to use this command.",
                ["NoMoney"] = "You don't have enough money to buy a {0}.",
                ["RaidBlocked"] = "<color=#FF1919>You may not do that while raid blocked</color>.",
                ["CombatBlocked"] = "<color=#FF1919>You may not do that while combat blocked</color>.",
                ["OptionNotFound"] = "This '{0}' option doesn't exist.",
                ["VehiclePurchased"] = "You have purchased a {0}, type <color=#4DFF4D>/{1}</color> for more information.",
                ["VehicleAlreadyPurchased"] = "You have already purchased {0}.",
                ["VehicleCannotBeBuyed"] = "{0} is unpurchasable",
                ["VehicleNotOut"] = "{0} is not out.",
                ["AlreadyVehicleOut"] = "You already have a {0} outside, type <color=#4DFF4D>/{1}</color> for more information.",
                ["VehicleNotYetPurchased"] = "You have not yet purchased a {0}.",
                ["VehicleSpawned"] = "You spawned your {0}.",
                ["VehicleRecalled"] = "You recalled your {0}.",
                ["VehicleOnCooldown"] = "You must wait {0} seconds before you can spawn your {1}.",
                ["NotInWater"] = "You must be in the water to spawn a {0}.",
                ["BuildindBlocked"] = "You can't spawn a {0} appear if you don't have the building privileges.",

                ["RefundedVehicleFuel"] = "Your {0} fuel was refunded to your inventory.",
                ["PlayerMountedOnVehicle"] = "It cannot be recalled when players mounted on your {0}.",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Help"] = "可用命令列表:",
                ["HelpLicence1"] = "<color=#4DFF4D>/{0}</color> -- 购买一辆载具",
                ["HelpLicence2"] = "<color=#4DFF4D>/{0}</color> -- 生成一辆载具",
                ["HelpLicence3"] = "<color=#4DFF4D>/{0}</color> -- 召回一辆载具",
                ["HelpBuy"] = "<color=#4DFF4D>/{0} {1}</color> -- 购买一辆 {2}, 价格: <color=#FF1919>{3}</color>",
                ["HelpSpawn"] = "<color=#4DFF4D>/{0} {1}</color> -- 生成一辆 {2}",
                ["HelpRecall"] = "<color=#4DFF4D>/{0} {1}</color> -- 召回一辆 {2}",

                ["NotAllowed"] = "您没有权限使用该命令",
                ["NoMoney"] = "您没有足够的资源购买 {0}",
                ["RaidBlocked"] = "<color=#FF1919>您被突袭阻止了，不能使用该命令</color>.",
                ["CombatBlocked"] = "<color=#FF1919>您被战斗阻止了，不能使用该命令</color>.",
                ["OptionNotFound"] = "该 '{0}' 选项不存在",
                ["VehiclePurchased"] = "您购买了 {0}, 输入 <color=#4DFF4D>/{1}</color> 了解更多信息",
                ["VehicleAlreadyPurchased"] = "您已经购买了 {0}",
                ["VehicleCannotBeBuyed"] = "{0} 是不可购买的",
                ["VehicleNotOut"] = "您还没有生成您的 {0}",
                ["AlreadyVehicleOut"] = "您已经生成了您的 {0}, 输入 <color=#4DFF4D>/{1}</color> 了解更多信息",
                ["VehicleNotYetPurchased"] = "您还没有购买 {0}.",
                ["VehicleSpawned"] = "您生成了您的 {0}.",
                ["VehicleRecalled"] = "您召回了您的 {0}.",
                ["VehicleOnCooldown"] = "您必须等待 {0} 秒才能生成您的 {1}",
                ["NotInWater"] = "您必须在水中才能生成您的 {0}",
                ["BuildindBlocked"] = "您没有领地柜权限，无法生成您的 {0}",
                ["RefundedVehicleFuel"] = "您的 {0} 燃料已经归还回您的库存",
                ["PlayerMountedOnVehicle"] = "您的 {0} 上坐着玩家，无法被召回",
            }, this, "zh-CN");
        }

        #endregion LanguageFile
    }
}