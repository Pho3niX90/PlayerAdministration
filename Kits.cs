using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kits Core", "Pho3niX90", "1.0.0")]
    [Description("")]
    public class Kits : RustPlugin
    {
        #region Vars

        private const string commandWipe = "kits.wipe";
        private const string commandConstant = "kits.core";
        private const string commandGiveConstant = "kits.give";

        #endregion

        #region Oxide Hooks

        private void Init() {
            foreach (var kit in config.kits) {
                if (!permission.PermissionExists(kit.permission)) {
                    permission.RegisterPermission(kit.permission, this);
                }
            }

            foreach (var command in config.commands) {
                cmd.AddChatCommand(command, this, nameof(cmdControlChat));
                cmd.AddConsoleCommand(command, this, nameof(cmdControlConsole));
            }

            cmd.AddConsoleCommand(commandConstant, this, nameof(cmdControlConsole));
            cmd.AddConsoleCommand(commandWipe, this, nameof(cmdControlConsole));
            cmd.AddConsoleCommand(commandGiveConstant, this, nameof(cmdControlConsole));
            LoadData();
        }

        private void OnServerInitialized() {
            timer.Once(3f, LoadImages);
        }

        private void Unload() {
            SaveData();
        }

        #endregion

        #region Commands

        private void cmdControlConsole(ConsoleSystem.Arg arg) {
            var args = arg.Args ?? new string[] { };
            var command = arg.cmd?.FullName;

            if (command == commandWipe && arg.IsAdmin) {
                Data = new PluginData();
                SaveData();
                SendReply(arg, "Cooldown data was cleared for all!");
                return;
            }

            if (command == commandGiveConstant && arg.IsAdmin) {
                var playerName = args.Length > 0 ? args[0] : "null";
                var kitName = args.Length > 1 ? args[1] : "null";
                var player = FindPlayer(playerName);
                if (player == null) {
                    SendReply(arg, $"Can't find player with name '{playerName}'");
                    return;
                }

                var kit = FindKitByName(kitName);
                if (kit == null) {
                    SendReply(arg, $"Can't find kit with name '{kitName}'");
                    return;
                }

                GiveKit(player, kitName);
                SendReply(arg, $"You gave kit {kitName} to {player.displayName}!");
                return;
            }
            cmdControlChat(arg.Player(), command, args);
        }

        private void cmdControlChat(BasePlayer player, string command, string[] args) {
            args = args ?? new string[] { };
            var action = args.Length > 0 ? args[0].ToLower() : "true";
            var name = args.Length > 1 ? args[1] : "null";
            var refreshUI = args.Contains("ui.refresh");
            var sendContent = args.Contains("items") || args.Contains("info") || args.Contains("content");
            if (sendContent) {
                SendInfo(player, name);
                return;
            }

            switch (action) {
                case "true":
                    SendKits(player);
                    break;

                case "add":
                case "create":
                case "new":
                    AddNewKit(player, name);
                    break;

                case "remove":
                case "delete":
                case "destroy":
                case "del":
                    RemoveExistingKit(player, name);
                    break;

                case "update":
                case "updateitems":
                    UpdateKitItems(player, name);
                    break;

                case "reload":
                case "load":
                    LoadKits(player);
                    break;

                case "page":
                    var page = 0;
                    int.TryParse(name, out page);
                    Interface.CallHook("OpenKitUIPage", player, page);
                    break;

                default:
                    TryToClaimKit(player, action, refreshUI);
                    break;
            }
        }

        #endregion

        #region Core

        private void TryToClaimKit(BasePlayer player, string name, bool refreshUI) {
            if (refreshUI) {
                NextTick(() => {
                    Interface.CallHook("OnKitUIRefreshRequested", player);
                });
            }

            var kit = FindKitByName(name);
            if (kit == null) {
                SendMessage(player, Message.NoKit, "{name}", name);
                return;
            }

            if (!CanUse(player)) {
                SendMessage(player, Message.CantUse);
                return;
            }

            if (!HasPermission(player, kit.permission) || kit.forAPI) {
                SendMessage(player, Message.Permission);
                return;
            }

            var kitInfo = GetDataKitInfo(player, kit.name);
            if (kit.uses > 0) {
                var left = kit.uses - kitInfo.uses;
                if (left <= 0) {
                    SendMessage(player, Message.UsesLimitChat, "{limit}", kit.uses);
                    return;
                }

                SendMessage(player, Message.UsesLeftChat, "{left}", left - 1);
            }

            if (kit.block > 0 && !kit.blockBypass) {
                var unblockDate = SaveRestore.SaveCreatedTime.AddSeconds(kit.block);
                var leftSpan = unblockDate - DateTime.UtcNow;

                if (leftSpan.TotalSeconds > 0) {
                    SendMessage(player, Message.WipeblockChat, "{h}", leftSpan.Hours, "{m}", leftSpan.Minutes, "{s}", leftSpan.Seconds);
                    return;
                } else {
                    kit.blockBypass = true;
                }
            }

            if (kit.cooldown > 0) {
                var unblockDate = kitInfo.lastUse.AddSeconds(kit.cooldown);
                var leftSpan = unblockDate - DateTime.UtcNow;
                if (leftSpan.TotalSeconds > 0) {
                    SendMessage(player, Message.CooldownChat, "{d}", GetTimeLeft(leftSpan.Days, "d"), "{h}", GetTimeLeft(leftSpan.Hours, "h"), "{m}", GetTimeLeft(leftSpan.Minutes, "m"), "{s}", GetTimeLeft(leftSpan.Seconds, "s"));
                    return;
                }
            }

            kitInfo.lastUse = DateTime.UtcNow;
            kitInfo.uses += 1;
            GiveKit(player, kit);
        }

        private void SendKits(BasePlayer player) {
            if (Interface.Oxide.CallHook("CanSeeKits", player) == null) {
                SendKitsInChat(player);
            }
        }

        private void SendInfo(BasePlayer player, string name) {
            if (Interface.Oxide.CallHook("CanSeeKitsInfo", player, name) == null) {
                SendKitContentText(player, name);
            }
        }

        private void SendKitsInChat(BasePlayer player) {
            var kitsString = string.Empty;

            foreach (var kit in GetAvailableKits(player)) {
                kitsString += GetMessage(Message.KitChatEntry, player.UserIDString,
                    "{name}", kit.displayName,
                    "{description}", kit.description,
                    "{cooldown}", kit.cooldown);
            }

            SendMessage(player, Message.KitsListChat, "{list}", kitsString);
        }

        private void SendKitContentText(BasePlayer player, string name) {
            var kit = FindKitByName(name);
            if (kit == null) {
                SendMessage(player, Message.NoKit, "{name}", name);
                return;
            }

            var contentString = string.Empty;

            foreach (var item in kit.items) {
                contentString += GetMessage(Message.ItemChatEntry, player.UserIDString,
                    "{name}", item.shortname,
                    "{amount}", item.amount);
            }

            SendMessage(player, Message.KitsContentChat, "{name}", kit.displayName, "{list}", contentString);
        }

        private Kit[] GetAvailableKits(BasePlayer player) {
            var list = new List<Kit>();

            foreach (var kit in config.kits) {
                if (kit.forAPI) {
                    continue;
                }

                if (!kit.showWithoutPermission && !HasPermission(player.UserIDString, kit.permission)) {
                    continue;
                }

                list.Add(kit);
            }

            return list.ToArray();
        }

        private static KitInfo GetDataKitInfo(BasePlayer player, string kitName, DataEntry data = null) {
            data = data ?? Data.Get(player.UserIDString);
            var kitInfo = (KitInfo)null;
            if (!data.kitsInfo.TryGetValue(kitName, out kitInfo)) {
                kitInfo = new KitInfo();
                data.kitsInfo.Add(kitName, kitInfo);
            }

            return kitInfo;
        }

        private void LoadImages() {
            var previews = config.kits.Where(x => !string.IsNullOrEmpty(x.url)).Select(xx => xx.url).ToArray();
            var contents = config.kits.SelectMany(x => x.items).Select(x => x.shortname).Distinct().ToArray();
            var total = previews.Concat(contents).ToArray();

            foreach (var url in total) {
                if (string.IsNullOrEmpty(url)) {
                    continue;
                }

                if (url.StartsWith("http") || url.StartsWith("www")) {
                    AddImage(url);
                } else {
                    AddImage($"https://rustlabs.com/img/items180/{url}.png");
                }
            }
        }

        #endregion

        #region Helpers

        private void LoadKits(BasePlayer player = null) {
            if (player != null && !player.IsAdmin) {
                SendMessage(player, Message.Permission);
                return;
            }

            LoadConfig();
            Interface.CallHook("OnKitsLoaded");
        }

        private void UpdateKitItems(BasePlayer player, string name) {
            if (!player.IsAdmin) {
                SendMessage(player, Message.Permission);
                return;
            }

            LoadConfig();

            var kit = FindKitByName(name);
            if (kit == null) {
                SendMessage(player, Message.NoKit, "{name}", name);
                return;
            }

            kit.items = GetPlayerItems(player);
            SendMessage(player, $"{kit.displayName} [{kit.name}] items was updated!");
            SaveConfig();
        }

        private void RemoveExistingKit(BasePlayer player, string name) {
            if (!player.IsAdmin) {
                SendMessage(player, Message.Permission);
                return;
            }

            LoadConfig();

            var kit = FindKitByName(name);
            if (kit == null) {
                SendMessage(player, Message.NoKit, "{name}", name);
                return;
            }

            RemoveKit(kit);
            SendMessage(player, Message.KitRemoved, "{name}", name);
        }

        private void AddNewKit(BasePlayer player, string name) {
            if (!player.IsAdmin) {
                SendMessage(player, Message.Permission);
                return;
            }

            LoadConfig();

            var kit = new Kit {
                name = name,
                displayName = name,
                items = GetPlayerItems(player),
                description = $"Created by {player.displayName} at {DateTime.UtcNow}"
            };

            AddKit(kit);
            SendMessage(player, Message.KitAdded, "{count}", GetPlayerItems(player).Length, "{name}", name);
        }

        private BaseItem[] GetPlayerItems(BasePlayer player) {
            var inventory = player.inventory;
            var items = new List<BaseItem>();

            foreach (var item in inventory.containerBelt.itemList) {
                items.Add(new BaseItem().Create(item, "Belt"));
            }

            foreach (var item in inventory.containerWear.itemList) {
                items.Add(new BaseItem().Create(item, "Wear"));
            }

            foreach (var item in inventory.containerMain.itemList) {
                if (item.position < 24) {
                    items.Add(new BaseItem().Create(item, "Main"));
                }
            }

            return items.ToArray();
        }

        private void GiveKit(BasePlayer player, Kit kit) {
            if (kit == null) {
                return;
            }

            foreach (var value in kit.items) {
                value.GiveTo(player);
            }
            if (!kit.forAPI)
                SendMessage(player, Message.KitReceived, "{name}", kit.name);

            Interface.Oxide.CallHook("OnKitRedeemed", player, kit.name);
        }

        private static Kit FindKitByName(string name) {
            return config.kits.FirstOrDefault(x => string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));
        }

        private void GiveKit(BasePlayer player, string name) {
            GiveKit(player, FindKitByName(name));
        }

        private void AddKit(Kit kit) {
            config.kits = config.kits.Concat(new Kit[] { kit }).ToArray();
            SaveConfig();
            Interface.CallHook("OnKitAdded", kit.name);
        }

        private void RemoveKit(Kit kit) {
            config.kits = config.kits.Where(x => x != kit).ToArray();
            SaveConfig();
            Interface.CallHook("OnKitRemoved", kit.name);
        }

        private bool HasPermission(string userID, string perm) {
            return string.IsNullOrEmpty(perm) || permission.UserHasPermission(userID, perm);
        }

        private bool HasPermission(BasePlayer player, string perm) {
            return HasPermission(player.UserIDString, perm);
        }

        private static BasePlayer FindPlayer(string nameOrID) {
            var players = BasePlayer.activePlayerList;
            var targets = players.Where(x => x.UserIDString == nameOrID || x.displayName.ToLower().Contains(nameOrID.ToLower())).ToList();

            if (targets.Count == 0 || targets.Count > 1) {
                return null;
            }

            return targets[0];
        }

        #endregion

        #region Configuration | 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Command")]
            public string[] commands =
            {
                "kit",
                "kits"
            };

            [JsonProperty(PropertyName = "Wipe data on new map")]
            public bool autoWipeData = false;

            [JsonProperty(PropertyName = "Kit list")]
            public Kit[] kits =
            {
                new Kit
                {
                    displayName = "Test Kit #1",
                    cooldown = 300,
                    block = 600,
                    description = "Test kit #1",
                    name = "test1",
                    permission = "",
                },
                new Kit
                {
                    displayName = "Test Kit #2",
                    cooldown = 300,
                    block = 86400 * 5,
                    description = "Test kit #2",
                    name = "test2",
                    permission = "",
                },
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

        protected override void LoadDefaultConfig() {
            config = new ConfigData();
        }

        protected override void SaveConfig() {
            Config.WriteObject(config);
        }

        #endregion

        #region Language | 2.0.0

        private Dictionary<object, string> langMessages = new Dictionary<object, string>
        {
            {Message.Usage, "Usage:\n"},
            {Message.Permission, "<color=#ff0000>You don't have permission to use that!</color>"},
            {Message.CantUse, "You can't use it right now"},
            {Message.NoKit, "Can't find kit with name '{name}'"},
            {Message.AvailableUI, "Click to redeem"},

            {Message.PermissionChat, "You don't have permission to use that!"},
            {Message.PermissionUI, "No Permission"},

            {Message.CooldownChat, "Cooldown for {d} {h} {m} {s} !"},
            {Message.CooldownUI, "Cooldown: {d} {h} {m} {s}"},

            {Message.WipeblockChat, "Kit available in {d} {h} {m} {s} left"},
            {Message.WipeblockUI, "Wipe block: {d} {h} {m} {s}"},

            {Message.UsesLimitChat, "You already used maximal amount of that kit! (Limit: {limit})"},
            {Message.UsesLeftChat, "Uses left: {left}"},
            {Message.UsesLeftUI, "Uses: {left}"},

            {Message.KitAdded, "You successfully added kit '{name}' with '{count}' items"},
            {Message.KitRemoved, "You successfully removed kit '{name}'"},
            {Message.KitReceived, "You successfully redeemed a kit!"},

            {Message.KitsListChat, "Currents kits:\n{list}"},
            {Message.KitsContentChat, "Kit {name} including:\n{list}"},

            {Message.KitChatEntry, "<color=#ff0000> {name}</color> - {description} (Cooldown: {cooldown})\n"},
            {Message.ItemChatEntry, " {name} x{amount}\n"}
        };

        private enum Message
        {
            Usage,
            Permission,
            CantUse,
            AvailableUI,
            NoKit,
            KitsListChat,
            KitsContentChat,

            PermissionChat,
            PermissionUI,
            CooldownChat,
            CooldownUI,
            WipeblockChat,
            WipeblockUI,
            UsesLimitChat,
            UsesLeftChat,
            UsesLeftUI,

            KitAdded,
            KitRemoved,
            KitReceived,

            KitChatEntry,
            ItemChatEntry
        }

        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(langMessages.ToDictionary(x => x.Key.ToString(), y => y.Value), this);
        }

        private string GetMessage(Message key, string playerID = null, params object[] args) {
            var message = lang.GetMessage(key.ToString(), this, playerID);
            var dic = OrganizeArgs(args);
            if (dic != null) {
                foreach (var pair in dic) {
                    var s0 = "{" + pair.Key + "}";
                    var s1 = pair.Key;
                    var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                    message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                    message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            return message;
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

        private void SendMessage(object receiver, Message key, params object[] args) {
            var userID = (receiver as BasePlayer)?.UserIDString;
            var message = GetMessage(key, userID, args);
            SendMessage(receiver, message);
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

        #endregion

        #region Data | 2.0.0

        private const string filename = "Temp/Kits/Players";
        private bool corruptedData;
        private Timer saveTimer;

        private class DataEntry
        {
            public Dictionary<string, KitInfo> kitsInfo = new Dictionary<string, KitInfo>();
        }

        private class KitInfo
        {
            public DateTime lastUse;
            public int uses;
        }

        private static PluginData Data = new PluginData();
        private class PluginData
        {
            // ReSharper disable once MemberCanBePrivate.Local
            public Dictionary<string, DataEntry> values = new Dictionary<string, DataEntry>();
            public DateTime creationTime = SaveRestore.SaveCreatedTime;
            [JsonIgnore] public bool needWipe => config.autoWipeData && SaveRestore.SaveCreatedTime != creationTime;
            [JsonIgnore] public Dictionary<string, DataEntry> cache = new Dictionary<string, DataEntry>();

            public DataEntry Get(object param) {
                var key = param?.ToString();
                if (key == null) {
                    return null;
                }

                var value = (DataEntry)null;
                if (cache.TryGetValue(key, out value)) {
                    return value;
                }

                if (!values.TryGetValue(key, out value)) {
                    value = new DataEntry();
                    values.Add(key, value);
                }

                cache.Add(key, value);
                return value;
            }
        }

        private void LoadData(string keyName = filename) {
            if (saveTimer == null) {
                saveTimer = timer.Every(Core.Random.Range(500, 700), () => SaveData());
                saveTimer.Reset();
            }

            try {
                Data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>($"{Name}/{keyName}");
                if (Data.needWipe) {
                    PrintWarning($"Data was wiped by auto-wiping function (Old: {Data.creationTime}, New: {SaveRestore.SaveCreatedTime})");
                    SaveData(filename + "_old");
                    Data = new PluginData();
                    SaveData();
                }
            } catch (Exception e) {
                corruptedData = true;
                Data = new PluginData();

                timer.Every(30f, () => {
                    PrintError($"!!! CRITICAL DATA ERROR !!!\n * Data was not loaded!\n * Data auto-save was disabled!\n * Error: {e.Message}");
                });

                LogToFile("errors", $"\n\nError: {e.Message}\n\nTrace: {e.StackTrace}\n\n", this);
            }
        }

        private void SaveData(string keyName = filename) {
            if (!corruptedData && Data != null) {
                Data.cache.Clear();
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/{keyName}", Data);
            }
        }

        #endregion

        #region BaseItem Support 1.1.0

        private class BaseItem
        {
            [JsonProperty(PropertyName = "Command")]
            public string command = string.Empty;

            [JsonProperty(PropertyName = "Shortname")]
            public string shortname = string.Empty;

            [JsonProperty(PropertyName = "Amount")]
            public int amount = 1;

            [JsonProperty(PropertyName = "Skin")]
            public ulong skinId;

            [JsonProperty(PropertyName = "Random skin")]
            public bool randomizeSkin = false;

            [JsonProperty(PropertyName = "Display name")]
            public string displayName;

            [JsonProperty(PropertyName = "Blueprint")]
            public bool isBlueprint;

            [JsonProperty(PropertyName = "Container")]
            public string container = string.Empty;

            [JsonProperty(PropertyName = "Slot")]
            public int slot;

            [JsonProperty(PropertyName = "Fuel")]
            public float fuel;

            [JsonProperty(PropertyName = "Contents")]
            public Dictionary<string, int> contents = new Dictionary<string, int>();

            [JsonProperty(PropertyName = "Condition (don't change me)")]
            public float condition;

            [JsonProperty(PropertyName = "Maximal Condition (don't change me)")]
            public float maxCondition;

            public void GiveTo(BasePlayer player) {
                if (player == null) {
                    return;
                }

                var item = Create();
                if (item != null) {
                    var container = (ItemContainer)null;

                    switch (this.container.ToLower()) {
                        case "belt":
                            container = player.inventory.containerBelt;
                            break;

                        case "main":
                            container = player.inventory.containerMain;
                            break;

                        case "wear":
                            container = player.inventory.containerWear;
                            break;
                    }

                    if (container == null || !item.MoveToContainer(container, slot)) {
                        player.GiveItem(item);
                    }
                }

                if (!string.IsNullOrEmpty(command)) {
                    RunCommand(player);
                }
            }

            public void GiveTo(string playerID) {
                var player = BasePlayer.Find(playerID) ?? BasePlayer.FindSleeping(playerID);

                if (player == null) {
                    RunCommand(playerID);
                } else {
                    GiveTo(player);
                }
            }

            private Item Create() {
                if (isBlueprint) {
                    var blueprint = ItemManager.CreateByName("blueprintbase", amount);
                    blueprint.blueprintTarget = ItemManager.FindItemDefinition(shortname).itemid;
                    return blueprint;
                }

                var item = ItemManager.CreateByName(shortname, amount, skinId);
                if (item == null) {
                    return null;
                }

                if (randomizeSkin) {
                    Interface.CallHook("SetRandomSkin", null, item);
                }

                item.name = displayName;

                if (maxCondition > 0) {
                    item.maxCondition = maxCondition;
                    item.condition = condition;
                }

                item.fuel = fuel;

                var weapon = item?.GetHeldEntity()?.GetComponent<BaseProjectile>();
                foreach (var defC in contents) {
                    var name = defC.Key;
                    var amount = defC.Value;

                    if ((name.StartsWith("arrow.") || name.StartsWith("ammo.")) && weapon != null) {
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(name);
                        weapon.primaryMagazine.contents = amount;
                        continue;
                    }

                    var content = ItemManager.CreateByName(name, amount);
                    content?.MoveToContainer(item.contents);
                }

                weapon?.SendNetworkUpdateImmediate();
                return item;
            }

            public BaseItem Create(Item item, string rootContainer = null) {
                var value = new BaseItem {
                    shortname = item.blueprintTarget != 0
                        ? ItemManager.FindItemDefinition(item.blueprintTarget).shortname
                        : item.info.shortname,
                    amount = item.amount,
                    skinId = item.skin,
                    displayName = item.name,
                    condition = item.condition,
                    maxCondition = item.maxCondition,
                    fuel = item.fuel,
                    isBlueprint = item.blueprintTarget != 0,
                    slot = item.position,
                    container = rootContainer,
                };

                foreach (var subItem in item.contents?.itemList ?? new List<Item>()) {
                    value.contents.Add(subItem.info.shortname, subItem.amount);
                }

                var weapon = item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine;
                if (weapon != null) {
                    value.contents.Add(weapon.ammoType.shortname, weapon.contents);
                }

                return value;
            }

            private void RunCommand(BasePlayer player) {
                RunCommand(player.UserIDString);
            }

            private void RunCommand(string playerID) {
                if (string.IsNullOrEmpty(command)) {
                    return;
                }

                var cmd = command
                    .Replace("{userid}", playerID, StringComparison.OrdinalIgnoreCase)
                    .Replace("{playerid}", playerID, StringComparison.OrdinalIgnoreCase);
                ConsoleSystem.Run(ConsoleSystem.Option.Server, cmd);
            }
        }

        #endregion

        #region Classes

        private class Kit
        {
            [JsonProperty(PropertyName = "Shortname", Order = 1)]
            public string name = string.Empty;

            [JsonProperty(PropertyName = "Display name", Order = 2)]
            public string displayName = string.Empty;

            [JsonProperty(PropertyName = "Permission", Order = 3)]
            public string permission = "kits.new";

            [JsonProperty(PropertyName = "Cooldown", Order = 4)]
            public int cooldown = 3600;

            [JsonProperty(PropertyName = "Wipe-block time", Order = 5)]
            public int block = 0;

            [JsonProperty(PropertyName = "Icon", Order = 6)]
            public string url = string.Empty;

            [JsonProperty(PropertyName = "Description", Order = 7)]
            public string description = string.Empty;

            [JsonProperty(PropertyName = "Max uses", Order = 8)]
            public int uses = 0;

            [JsonProperty(PropertyName = "API Kit", Order = 800)]
            public bool forAPI = false;

            [JsonProperty(PropertyName = "Show without uses", Order = 801)]
            public bool showWithoutUses = false;

            [JsonProperty(PropertyName = "Show without permission", Order = 802)]
            public bool showWithoutPermission = false;

            [JsonProperty(PropertyName = "Show if wipe-blocked", Order = 803)]
            public bool showIfWipeBlocked = true;

            [JsonProperty(PropertyName = "Show if on cooldown", Order = 804)]
            public bool showIfOnCooldown = true;

            [JsonProperty(PropertyName = "Custom message if doesn't have permission", Order = 900)]
            public string messageForNoPermission = string.Empty;

            [JsonProperty(PropertyName = "Items", Order = 999)]
            public BaseItem[] items =
            {
                new BaseItem
                {
                    shortname = "stonehatchet"
                },
                new BaseItem
                {
                    shortname = "pickaxe"
                }
            };

            [JsonIgnore]
            public bool blockBypass = false;
        }

        private class uModData
        {
            public Dictionary<string, uModKit> Kits = new Dictionary<string, uModKit>();
        }

        private class uModKit
        {
            public string name;
            public string description;
            public int max;
            public double cooldown;
            public int authlevel;
            public bool hide;
            public bool npconly;
            public string permission;
            public string image;
            public string building;
            public List<uModKitItem> items = new List<uModKitItem>();

            public Kit ToNormalKit() {
                return new Kit {
                    name = name,
                    description = description,
                    uses = max,
                    cooldown = Convert.ToInt32(cooldown),
                    forAPI = hide || npconly,
                    permission = permission,
                    url = image,
                    items = items.Select(x => x.ToBaseItem()).ToArray()
                };
            }
        }

        private class uModKitItem
        {
            public int itemid;
            public string container;
            public int amount;
            public ulong skinid;
            public bool weapon;
            public int blueprintTarget;
            public List<int> mods = new List<int>();

            public BaseItem ToBaseItem() {
                return new BaseItem {
                    shortname = ItemManager.FindItemDefinition(itemid)?.shortname,
                    container = container,
                    amount = amount,
                    skinId = skinid,
                    isBlueprint = blueprintTarget != 0,
                    contents = mods.ToDictionary(x => ItemManager.FindItemDefinition(x)?.shortname, y => 1)
                };
            }
        }

        #endregion

        #region Image Library Support

        [PluginReference] private Plugin ImageLibrary;

        private void AddImage(string url) {
            ImageLibrary?.Call("AddImage", url, url, (ulong)0);
        }

        private string GetImage(string url) {
            return ImageLibrary?.Call<string>("GetImage", url);
        }

        #endregion

        #region API

        private static bool CanUse(BasePlayer player) {
            return Interface.Oxide.CallHook("canRedeemKit", player) == null &&
                   Interface.Oxide.CallHook("CanUseKit", player) == null;
        }
        private bool isKit(string kitname) {
            return FindKitByName(kitname) != null;
        }

        private string[] GetKitContents(string kitname, bool random = false) {
            var kit = FindKitByName(kitname);
            if (kit == null) {
                return null;
            }

            var items = new List<string>();
            foreach (var item in kit.items) {
                var itemString = $"{ItemManager.FindItemDefinition(item.shortname)?.itemid}_{item.amount}";
                if (item.contents.Count > 0)
                    foreach (var mod in item.contents) {
                        itemString += $"_{mod}";
                    }

                items.Add(itemString);
            }

            return items.Count > 0 ? items.ToArray() : null;
        }

        private object GetKitInfo(string kitname) {
            var kit = FindKitByName(kitname);
            if (kit == null) {
                return null;
            }

            var obj = new JObject {
                ["name"] = kit.name,
                ["permission"] = kit.permission,
                ["npconly"] = kit.forAPI,
                ["max"] = kit.uses,
                ["image"] = kit.url,
                ["hide"] = kit.forAPI,
                ["description"] = kit.description,
                ["cooldown"] = kit.cooldown,
                ["building"] = false,
                ["authlevel"] = 0
            };

            var items = new JArray();
            foreach (var itemEntry in kit.items) {
                var item = new JObject();
                item["amount"] = itemEntry.amount;
                item["container"] = itemEntry.container;
                item["itemid"] = ItemManager.FindItemDefinition(itemEntry.shortname)?.itemid;
                item["skinid"] = itemEntry.skinId;
                item["weapon"] = ItemManager.FindItemDefinition(itemEntry.shortname)?.GetComponent<HeldEntity>() != null;
                item["blueprint"] = itemEntry.isBlueprint;
                //                var mods = new JArray();
                //                foreach (var mod in itemEntry.contents ?? new Dictionary<string, int>())
                //                    mods.Add(mod);
                item["mods"] = null;
                items.Add(item);
            }

            obj["items"] = items;
            return obj;
        }

        private string API_GetPlayerUIKits(BasePlayer player) {
            return GetPlayerKitsForUI(player);
        }

        private string API_GetKitForUI(BasePlayer player, string kit) {
            return GetKitForUI(player, kit);
        }

        #endregion

        #region Graphical API

        private string GetPlayerKitsForUI(BasePlayer player) {
            var kits = config.kits.Where(x => !x.forAPI);
            var list = new List<KitUI>();
            var data = Data.Get(player.UserIDString);

            foreach (var kit in kits) {
                var kitInfo = GetDataKitInfo(player, kit.name, data);
                var message = GetUIMessage(player, kit, kitInfo);
                if (message == null) {
                    continue;
                }

                var obj = new KitUI {
                    block = kit.block,
                    cooldown = kit.cooldown,
                    description = kit.description,
                    name = kit.name,
                    items = new Dictionary<string, int>(),
                    url = kit.url,
                    uses = kit.uses,
                    displayName = kit.displayName,
                    messageOnButton = message
                };

                foreach (var item in kit.items) {
                    if (!obj.items.TryAdd(item.shortname, item.amount)) {
                        obj.items[item.shortname] += item.amount;
                    }
                }

                list.Add(obj);
            }

            var array = list.ToArray();
            return JsonConvert.SerializeObject(array);
        }

        private string GetKitForUI(BasePlayer player, string name) {
            var kit = FindKitByName(name);
            if (kit == null) {
                return null;
            }

            var data = GetDataKitInfo(player, name);

            var obj = new KitUI {
                block = kit.block,
                cooldown = kit.cooldown,
                uses = kit.uses - data.uses,
                displayName = kit.displayName,
                description = kit.description,
                items = new Dictionary<string, int>(),
            };

            foreach (var item in kit.items) {
                if (obj.items.ContainsKey(item.shortname)) {
                    obj.items[item.shortname] += item.amount;
                } else {
                    obj.items.Add(item.shortname, item.amount);
                }
            }

            return JsonConvert.SerializeObject(obj);
        }

        string GetTimeLeft(int num, string suff) => num > 0 ? $"{num}{suff}" : "";

        private string GetUIMessage(BasePlayer player, Kit kit, KitInfo kitInfo) {
            if (!HasPermission(player, kit.permission)) {
                if (!kit.showWithoutPermission) {
                    return null;
                }

                return string.IsNullOrEmpty(kit.messageForNoPermission) ? GetMessage(Message.PermissionUI, player.UserIDString) : kit.messageForNoPermission;
            }

            if (kit.uses > 0) {
                var left = kit.uses - kitInfo.uses;
                if (left <= 0) {
                    return !kit.showWithoutUses ? null : GetMessage(Message.UsesLeftUI, "{limit}", 0);
                }
            }

            if (kit.block > 0 && !kit.blockBypass) {
                var unblockDate = SaveRestore.SaveCreatedTime.AddSeconds(kit.block);
                var leftSpan = unblockDate - DateTime.UtcNow;
                if (leftSpan.TotalSeconds > 0) {
                    return !kit.showIfWipeBlocked ? null : GetMessage(Message.WipeblockUI, player.UserIDString, "{d}", GetTimeLeft(leftSpan.Days, "d"), "{h}", GetTimeLeft(leftSpan.Hours, "h"), "{m}", GetTimeLeft(leftSpan.Minutes, "m"), "{s}", GetTimeLeft(leftSpan.Seconds, "s"));
                } else {
                    kit.blockBypass = true;
                }
            }

            if (kit.cooldown > 0) {
                var unblockDate = kitInfo.lastUse.AddSeconds(kit.cooldown);
                var leftSpan = unblockDate - DateTime.UtcNow;
                if (leftSpan.TotalSeconds > 0) {
                    return !kit.showIfOnCooldown ? null : GetMessage(Message.CooldownUI, player.UserIDString, "{d}", GetTimeLeft(leftSpan.Days, "d"), "{h}", GetTimeLeft(leftSpan.Hours, "h"), "{m}", GetTimeLeft(leftSpan.Minutes, "m"), "{s}", GetTimeLeft(leftSpan.Seconds, "s"));
                }
            }

            return GetMessage(Message.AvailableUI, player.UserIDString);
        }

        #endregion

        #region Kit UI Class 1.2.0

        private class KitUI
        {
            public string name = string.Empty;
            public string displayName = string.Empty;
            public int cooldown = 3600;
            public int block = 0;
            public string url = string.Empty;
            public string description = string.Empty;
            public int uses = 0;
            public string messageOnButton;
            public Dictionary<string, int> items = new Dictionary<string, int>();
        }

        #endregion
    }
}