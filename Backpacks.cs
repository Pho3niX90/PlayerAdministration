using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Backpacks", "Pho3niX90", "0.1.0")]
    [Description("")]
    public class Backpacks : RustPlugin
    {
        #region Vars

        private const ulong skinID = 1733476229;
        private const string itemShortname = "xmas.present.medium";
        private const string itemWorldModel = "assets/prefabs/misc/item drop/item_drop_backpack.prefab";
        private const string spherePrefab = "assets/prefabs/visualization/sphere.prefab";
        private const string command = "backpacks.give";
        private static bool IsBackpack(ulong id) => id == skinID;
        private static bool IsPlayerContainer(ItemContainer container) => container.playerOwner != null
                                                                   || container.entityOwner?.GetComponent<LootableCorpse>() != null
                                                                   || container.entityOwner?.GetComponent<DroppedItemContainer>() != null;

        #endregion

        #region Oxide Hooks

        private void Init() {
            cmd.AddConsoleCommand(command, this, nameof(cmdGiveBackpackConsole));

            foreach (var value in config.commandsGet) {
                cmd.AddChatCommand(value, this, nameof(cmdGetBackpackChat));
            }

            foreach (var value in config.commandsOpen) {
                cmd.AddConsoleCommand(value, this, nameof(cmdOpenBackpack));
                cmd.AddChatCommand(value, this, nameof(cmdOpenBackpackChat));
            }

            foreach (var value in config.permissions) {
                if (permission.PermissionExists(value.permission) == false) {
                    permission.RegisterPermission(value.permission, this);
                }
            }

            if (config.onlyOneInContainer == false && config.canPlaceInStorage == true) {
                Unsubscribe(nameof(CanAcceptItem));
            }

            if (config.permissions.Any(x => x.canSpawnWithBackpack == true) == false) {
                Unsubscribe(nameof(OnPlayerRespawned));
            }
        }

        private void OnServerInitialized() {
            LoadData();
        }

        private void Unload() {
            SaveData();
        }

        private object OnItemAction(Item item, string action, BasePlayer player) {
            return CheckAction(item, action, player);
        }

        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos) {
            return CheckAcceptingItem(container, item);
        }

        object CanLootEntity(BasePlayer player, DroppedItemContainer container) {

            if (container.playerName.Equals("_back")) {
                bool canPickup = false;
                foreach (var item in container.inventory.itemList.ToList()) {
                    canPickup = CheckForMultipleBackpacks(player.inventory.containerMain, item) == null;
                    if (!canPickup) break;
                    item.MoveToContainer(player.inventory.containerMain);
                }
                if (canPickup) {
                    container.Kill();
                    GiveBackpackWorldmodel(player);
                }
                return false;
            } else {
                return null;
            }
        }

        private void OnPlayerRespawned(BasePlayer player) {
            var perm = GetPermission(player.UserIDString, config.permissions);
            if (perm == null || perm.canSpawnWithBackpack == false) {
                return;
            }

            timer.Once(config.respawnDelay, () => { GiveBackpack(player); });
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info) {
            DeleteBackpackWorldmodel(player);
            return null;
        }

        #endregion

        #region Commands

        private void cmdGetBackpackChat(BasePlayer player, string command, string[] args) {
            TryGetBackpack(player);
        }

        private void cmdOpenBackpackChat(BasePlayer player, string command, string[] args) {
            OpenBackpack(player);
        }

        private void cmdOpenBackpack(ConsoleSystem.Arg arg) {
            var player = arg.Player();
            if (player != null) {
                OpenBackpack(player);
            }
        }

        private void cmdGiveBackpackConsole(ConsoleSystem.Arg arg) {
            var args = arg.Args;
            if (arg.IsAdmin == false || args == null || args.Length == 0) {
                return;
            }

            var targetString = FindPlayer(args[0]);
            if (targetString is string) {
                SendReply(arg, targetString?.ToString());
                return;
            }

            var targetPlayer = targetString as BasePlayer;
            if (targetPlayer != null) {
                GiveBackpack(targetPlayer);
                SendReply(arg, $"{targetPlayer.displayName} received a backpack!");
            }
        }

        #endregion

        #region Core

        private void OpenBackpack(BasePlayer player) {
            var backpacks = player.inventory.AllItems().Where(x => IsBackpack(x.skin)).ToArray();
            var backpack = backpacks.GetRandom();
            if (backpack == null) {
                return;
            }

            OpenContainer(player, backpack.contents);
        }

        private void TryGetBackpack(BasePlayer player) {
            var perm = GetPermission(player.UserIDString, config.permissions);
            if (perm == null || perm.canGetBackpackFromCommand == false || perm.commandCooldown < 0) {
                SendMessage(player, MessageType.Permission);
                return;
            }

            if (perm.commandCooldown > 0) {
                var data = Data.Get(player.UserIDString, true);
                var passed = (DateTime.Now - data.lastCommandUse).TotalSeconds;
                var left = perm.commandCooldown - passed;
                if (left > 0) {
                    SendMessage(player, MessageType.Cooldown, "{seconds}", left.ToString("0"));
                    return;
                }

                data.lastCommandUse = DateTime.Now;
            }

            GiveBackpack(player);
        }

        private Item CreateItem() {
            var item = ItemManager.CreateByName(itemShortname, 1, skinID);

            if (item != null) {
                //ItemModWearable imod = new ItemModWearable();
                //item.info.gameObject.AddComponent<ItemModWearable>();
                item.name = GetMessage(MessageType.BackPackName, null, "{random}", Core.Random.Range(1000, 10000));
                //item.info.category = ItemCategory.Attire;
                item.contents = new ItemContainer();
                item.contents.ServerInitialize(item, 6);
                item.contents.GiveUID();
            }

            return item;
        }

        private void GiveBackpackWorldmodel(BasePlayer player) {
            SphereEntity sph = GameManager.server.CreateEntity(spherePrefab, player.eyes.position, player.transform.rotation) as SphereEntity;
            sph.Spawn();
            sph.lerpSpeed = 0f;
            sph.currentRadius = 0.6f;
            sph.SetParent(player, "spine3");
            sph.transform.localPosition = new Vector3(-0.08f, 0, 0);
            sph.transform.rotation = Quaternion.Euler(0, 270, 180);
            DroppedItemContainer gObj = GameManager.server.CreateEntity(itemWorldModel, player.transform.position) as DroppedItemContainer;
            //PrintComponents(sph);
            gObj.ResetRemovalTime(9999999999999);
            //gObj.transform.localPosition = new Vector3(0, 0, 0);
            //gObj.transform.rotation = Quaternion.Euler(0, 270, 180);
            gObj.Spawn();
            UnityEngine.Object.DestroyImmediate(gObj.GetComponent<Rigidbody>());
            gObj.SetParent(sph);
            gObj.transform.localPosition = new Vector3(0, 0, 0);
            gObj.SendNetworkUpdateImmediate();
            timer.Once(1.0f, () => {
                gObj.SetParent(player, "spine3");
                gObj.transform.localPosition = new Vector3(-0.08f, 0.0f, 0);
                gObj.transform.rotation = Quaternion.Euler(0, 270, 180);
                timer.Once(0.2f, () => {
                    sph?.Kill();
                });
            });
        }

        private void DeleteBackpackWorldmodel(BasePlayer player) {
            if (player == null) return;
            BaseEntity backpack = FindBackpackWorld(player);
                backpack?.Kill();
        }

        BaseEntity FindBackpackWorld(BasePlayer player) {
            BaseEntity sph = player.GetComponentInChildren<SphereEntity>();
            return sph;
        }

        void PrintComponents(BaseEntity ent) {
            foreach (var sl in ent.GetComponents<Component>()) {
                Puts($"-P- {sl.GetType().Name} | {sl.name}");
                foreach (var s in sl.GetComponentsInChildren<Component>()) {
                    Puts($"-C- {s.GetType().Name} | {s.name}");
                }
            }
        }

        private void GiveBackpack(BasePlayer player) {
            if (player.IsValid() == false || player.IsAlive() == false) {
                return;
            }

            var item = CreateItem();
            if (item != null) {
                player.GiveItem(item);
                GiveBackpackWorldmodel(player);
            }
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item) {
            if (item?.skin == skinID) {
                DeleteBackpackWorldmodel(item?.GetOwnerPlayer());
            }
        }

        private object CheckAction(Item item, string action, BasePlayer player) {
            if (item.skin != skinID) return null;

            if (string.Equals(action, "drop", StringComparison.OrdinalIgnoreCase)) {
                BaseEntity entity = GameManager.server.CreateEntity(itemWorldModel, player.transform.position + Vector3.up, Quaternion.identity);
                DroppedItemContainer container = entity as DroppedItemContainer;

                container.lootPanelName = "genericlarge";

                if (player != null) {
                    container.playerName = $"_back";
                    container.playerSteamID = player.userID;
                }
                container.inventory = new ItemContainer();
                container.inventory.ServerInitialize(null, 42);
                container.inventory.GiveUID();
                container.inventory.entityOwner = container;
                container.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                if (item.MoveToContainer(container.inventory)) {
                    container.ResetRemovalTime();
                    container.Spawn();

                    ItemManager.DoRemoves();
                    DeleteBackpackWorldmodel(player);
                    return true;
                }
                return null;
            }

            NextTick(() => { OpenContainer(player, item.contents); });
            return true;
        }

        ItemContainer.CanAcceptResult? CheckAcceptingItem(ItemContainer container, Item item) {
            return CheckForBackpackBlacklist(container, item) ?? CheckForBackpackLocation(container, item) ?? CheckForMultipleBackpacks(container, item);
        }

        private ItemContainer.CanAcceptResult? CheckForBackpackBlacklist(ItemContainer container, Item item) {
            var parent = container.parent?.skin ?? 0;
            if (IsBackpack(parent) == true) {
                var shortname = item.info.shortname;
                var skin = item.skin.ToString();
                if (config.blacklist.Contains(shortname) || config.blacklist.Contains(skin) || item.skin == skinID) {
                    // Server.Broadcast("CheckForBackpackBlacklist");
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }

            return null;
        }

        private ItemContainer.CanAcceptResult? CheckForBackpackLocation(ItemContainer container, Item item) {
            if (IsBackpack(item.skin)) {
                if (!config.canPlaceInStorage && !IsPlayerContainer(container)) {
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }
            return null;
        }

        private ItemContainer.CanAcceptResult? CheckForMultipleBackpacks(ItemContainer container, Item item) {
            if (!config.onlyOneInContainer || !IsBackpack(item.skin) || item.parent == container) {
                return null;
            }

            var items = container.itemList.ToArray();
            if (container.playerOwner != null) {
                items = container.playerOwner.inventory.AllItems();
            }

            if (items.Any(x => x != item && x.skin == skinID)) {
                return ItemContainer.CanAcceptResult.CannotAccept;
            }

            return null;
        }

        private void OpenContainer(BasePlayer player, ItemContainer container) {
            var loot = player.inventory.loot;
            var position = player.transform.position - new Vector3(0, 500, 0);
            var entity = GameManager.server.CreateEntity(spherePrefab, position);
            if (entity == null) return;

            var capacity = container.itemList.Count;
            var perm = GetPermission(player.UserIDString, config.permissions);
            if (perm != null && perm.size > capacity) {
                capacity = perm.size;
            }

            entity.enableSaving = false;
            entity.Spawn();
            container.entityOwner = entity;
            container.playerOwner = player;
            container.capacity = capacity;

            foreach (var item in container.itemList.ToArray()) {
                if (item.position >= capacity) {
                    item.RemoveFromContainer();
                    item.MoveToContainer(container);
                }
            }

            timer.Once(0.5f, () => {
                if (player == null) {
                    return;
                }

                loot.Clear();
                loot.PositionChecks = false;
                loot.entitySource = entity;
                loot.itemSource = (Item)null;
                loot.MarkDirty();
                loot.AddContainer(container);
                loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "genericlarge");
                player.SendNetworkUpdateImmediate();
            });
        }

        #endregion

        #region Configuration | 2.0.1

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Commands to get backpack in game")]
            public string[] commandsGet =
            {
                "backpack",
                "rucksack",
            };

            [JsonProperty(PropertyName = "Commands to open backpack")]
            public string[] commandsOpen =
            {
                "backpack.open",
                "backpacks.open",
                "b"
            };

            [JsonProperty(PropertyName = "Can be placed in a chest")]
            public bool canPlaceInStorage = false;

            [JsonProperty(PropertyName = "Only one in container (or player)")]
            public bool onlyOneInContainer = true;

            [JsonProperty(PropertyName = "Delay for giving backpack after respawning (seconds)")]
            public float respawnDelay = 0.5f;

            [JsonProperty(PropertyName = "Blacklist")]
            public string[] blacklist =
            {
                "1733476229",
                "explosive.timed",
                "rifle.ak"
            };

            [JsonProperty(PropertyName = "Permissions")]
            public PermissionEntry[] permissions =
            {
                new PermissionEntry
                {
                    permission = "backpacks.size.6",
                    priority = 6,
                    size = 6,
                },
                new PermissionEntry
                {
                    permission = "backpacks.size.12",
                    priority = 12,
                    size = 12,
                },
                new PermissionEntry
                {
                    permission = "backpacks.size.18",
                    priority = 18,
                    size = 18,
                },
                new PermissionEntry
                {
                    permission = "backpacks.size.42",
                    priority = 42,
                    size = 42,
                    canSpawnWithBackpack = true,
                }
            };
        }

        protected override void LoadConfig() {
            base.LoadConfig();

            try {
                config = Config.ReadObject<ConfigData>();
                if (config == null) {
                    LoadDefaultConfig();
                }
            } catch {
                for (var i = 0; i < 3; i++) {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }

                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private static void ValidateConfig() {

        }

        protected override void LoadDefaultConfig() {
            config = new ConfigData();
        }

        protected override void SaveConfig() {
            Config.WriteObject(config);
        }

        #endregion

        #region Language | 2.0.1

        private Dictionary<object, string> langMessages = new Dictionary<object, string>
        {
            {MessageType.Permission, "You don't have permission to use that!"},
            {MessageType.Cooldown, "Cooldown for {seconds} seconds!"},
            {MessageType.BackPackName, "Backpack #{random}"},

        };

        private enum MessageType
        {
            BackPackName,
            Permission,
            Cooldown,
        }

        protected override void LoadDefaultMessages() {
            var dictionary = new Dictionary<string, string>();
            foreach (var pair in langMessages) {
                dictionary.TryAdd(pair.Key.ToString(), pair.Value);
            }
            lang.RegisterMessages(dictionary, this);
        }

        private string GetMessage(MessageType key, string playerID = null, params object[] args) {
            return ReplaceArgs(lang.GetMessage(key.ToString(), this, playerID), OrganizeArgs(args));
        }

        private static Dictionary<string, object> OrganizeArgs(object[] args) {
            var dic = new Dictionary<string, object>();
            for (var i = 0; i < args.Length; i += 2) {
                var value = args[i].ToString();
                var nextValue = i + 1 < args.Length ? args[i + 1] : null;
                dic.Add(value, nextValue);
            }

            return dic;
        }

        private static string ReplaceArgs(string message, Dictionary<string, object> args) {
            if (args == null || args.Count < 1) {
                return message;
            }

            foreach (var pair in args) {
                var s0 = "{" + pair.Key + "}";
                var s1 = pair.Key;
                var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
            }

            return message;
        }

        private void SendMessage(object receiver, MessageType key, params object[] args) {
            var userID = (receiver as BasePlayer)?.UserIDString;
            var message = GetMessage(key, userID, args);
            SendMessage(receiver, message);
        }

        private void SendMessage(object receiver, string message) {
            if (receiver == null) {
                Puts(message);
                return;
            }

            var console = receiver as ConsoleSystem.Arg;
            if (console != null) {
                SendReply(console, message);
                return;
            }

            var player = receiver as BasePlayer;
            if (player != null) {
                player.ChatMessage(message);
                return;
            }
        }

        #endregion

        #region Chat Commands
        [ChatCommand("p")]
        private void DelteObjects(BasePlayer player, string command, string[] args) {
            FindBackpackWorld(player)?.Kill();

            PrintToConsole("-------------------------------------------------");
            foreach (var p in player.GetComponentsInChildren<SphereEntity>()) {
                p.Kill();
            }
            foreach (var p in player.GetComponentsInChildren<DroppedItemContainer>()) {
                p.Kill();
            }
        }

        RaycastHit raycastHit;
        private void GetTargetEntity(BasePlayer player) {
            bool flag = Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 10f);
        }
        private string GetEnglishName(string shortName) { return ItemManager.FindItemDefinition(shortName)?.displayName?.english ?? shortName; }
        #endregion

        #region Data | 2.2.0

        private static PluginData Data = new PluginData();
        private string dataFilename => $"{Name}\\data";
        private bool dataValid = false;

        private class DataEntry
        {
            [JsonProperty] private DateTime lastUse = DateTime.UtcNow;
            [JsonProperty] private int loadsCount = 0;
            [JsonIgnore] public double daysSinceLastUse => (DateTime.UtcNow - lastUse).TotalDays;
            [JsonProperty] public DateTime lastCommandUse;

            public void MarkUsed() {
                lastUse = DateTime.UtcNow;
                loadsCount++;
            }
        }

        private class PluginData
        {
            /* ### Values ### */
            // ReSharper disable once MemberCanBePrivate.Local
            [JsonProperty] private Dictionary<string, DataEntry> values = new Dictionary<string, DataEntry>();
            [JsonProperty] public readonly DateTime creationTime = SaveRestore.SaveCreatedTime;
            [JsonIgnore] private Dictionary<string, DataEntry> cache = new Dictionary<string, DataEntry>();

            /* ### Variables ### */
            [JsonIgnore] public bool needWipe => SaveRestore.SaveCreatedTime > creationTime && differentCreationDates;
            [JsonIgnore] public bool needCleanup => values.Count > minimalLimitForCleanup;
            [JsonIgnore] private bool differentCreationDates => (SaveRestore.SaveCreatedTime - creationTime).TotalHours > 1;
            [JsonIgnore] private static int cacheLifeSpan => 300;
            [JsonIgnore] public static int unusedDataLifeSpanDays => 14;
            [JsonIgnore] private static int minimalLimitForCleanup => 500;
            [JsonIgnore] public int valuesCount => values.Count;

            public DataEntry Get(object param, bool createNewOnMissing) {
                var key = GetKeyFrom(param);
                if (string.IsNullOrEmpty(key) == true) {
                    return null;
                }

                var value = (DataEntry)null;
                if (cacheLifeSpan > 0 && cache.TryGetValue(key, out value) == true) {
                    return value;
                }

                if (values.TryGetValue(key, out value) == false && createNewOnMissing == true) {
                    value = new DataEntry();
                    values.Add(key, value);
                }

                if (value != null) {
                    value.MarkUsed();

                    if (cacheLifeSpan > 0) {
                        cache.TryAdd(key, value);
                    }
                }

                return value;
            }

            public void Set(object param, DataEntry value) {
                var key = GetKeyFrom(param);
                if (string.IsNullOrEmpty(key) == true) {
                    return;
                }

                if (value == null) {
                    if (values.ContainsKey(key) == true) {
                        values.Remove(key);
                    }

                    if (cache.ContainsKey(key) == true) {
                        cache.Remove(key);
                    }
                } else {
                    if (values.TryAdd(key, value) == false) {
                        values[key] = value;

                        if (cache.ContainsKey(key) == true) {
                            cache[key] = value;
                        }
                    }
                }
            }

            public void Cleanup() {
                var keys = new List<string>();
                foreach (var pair in values) {
                    var key = pair.Key;
                    var value = pair.Value;

                    if (value.daysSinceLastUse > unusedDataLifeSpanDays) {
                        keys.Add(key);
                    }
                }

                foreach (var key in keys) {
                    Set(key, null);
                }
            }

            public void ResetCache() {
                cache.Clear();
            }

            private static string GetKeyFrom(object obj) {
                if (obj == null) {
                    return null;
                }

                if (obj is string) {
                    return obj as string;
                }

                if (obj is BasePlayer) {
                    return (obj as BasePlayer).UserIDString;
                }

                if (obj is BaseNetworkable) {
                    return (obj as BaseNetworkable).net?.ID.ToString();
                }

                return obj.ToString();
            }
        }

        private void LoadData() {
            try {
                Data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>($"{dataFilename}");

                if (Data.needWipe == true) {
                    Interface.Oxide.DataFileSystem.WriteObject($"{dataFilename}_old", Data);
                    Data = new PluginData();
                    PrintWarning($"Data was wiped by auto-wiping function (Old: {Data.creationTime}, New: {SaveRestore.SaveCreatedTime})");
                }

                if (Data.needCleanup == true) {
                    var oldCount = Data.valuesCount;
                    Data.Cleanup();
                    var newCount = Data.valuesCount;
                    PrintWarning($"Removed {oldCount - newCount} values that are older than {PluginData.unusedDataLifeSpanDays} days (Was: {oldCount}, Now: {newCount})");
                }

                dataValid = true;
                timer.Every(Core.Random.Range(500, 700), SaveData);
                SaveData();
            } catch (Exception e) {
                Data = new PluginData();
                dataValid = false;

                for (var i = 0; i < 5; i++) {
                    PrintError("!!! CRITICAL DATA ERROR !!!\n * Data was not loaded!\n * Data auto-save was disabled!");
                }

                LogToFile("errors", $"\n\nError: {e.Message}\n\nTrace: {e.StackTrace}\n\n", this);
            }
        }

        private void SaveData() {
            if (Data != null && dataValid == true) {
                Data.ResetCache();
                cachePermission.Clear();
                Interface.Oxide.DataFileSystem.WriteObject(dataFilename, Data);
            }
        }

        #endregion

        #region Permissions Support

        private Dictionary<string, PermissionEntry> cachePermission = new Dictionary<string, PermissionEntry>();

        private class PermissionEntry
        {
            [JsonProperty(PropertyName = "Permission")]
            public string permission;

            [JsonProperty(PropertyName = "Priority")]
            public int priority;

            [JsonProperty(PropertyName = "Size")]
            public int size = 6;

            [JsonProperty(PropertyName = "Can spawn with backpack")]
            public bool canSpawnWithBackpack = false;

            [JsonProperty(PropertyName = "Can get backpack from command")]
            public bool canGetBackpackFromCommand = false;

            [JsonProperty(PropertyName = "Cooldown (seconds)")]
            public int commandCooldown = 3600;
        }

        private PermissionEntry GetPermission(string playerID, PermissionEntry[] permissions) {
            var value = (PermissionEntry)null;
            if (cachePermission.TryGetValue(playerID, out value) == true) {
                return value;
            }

            var idString = playerID.ToString();
            var num = -1;

            foreach (var entry in permissions) {
                if (permission.UserHasPermission(idString, entry.permission) && entry.priority > num) {
                    num = entry.priority;
                    value = entry;
                }
            }

            if (value != null) {
                cachePermission.Add(playerID, value);
            }

            return value;
        }

        #endregion

        #region Utils

        private object FindPlayer(string targetName) {
            var match = new Predicate<BasePlayer>(x => x.UserIDString == targetName || x.displayName.Contains(targetName, CompareOptions.IgnoreCase));
            var targets = BasePlayer.activePlayerList.Where(x => match(x)).ToArray();

            if (targets.Length == 0) {
                return $"There are no players with that 'Name' or 'Steam ID' ({targetName})";
            }

            if (targets.Length > 1) {
                return $"There are multiple players with that 'Name' :\n{targets.Select(x => x.displayName).ToSentence()}";
            }

            return targets[0];
        }

        #endregion
    }
}