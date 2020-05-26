using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("Auto Lock", "birthdates", "2.3.0")]
    [Description("Automatically adds a codelock to a lockable entity with a set pin")]
    public class AutoLock : RustPlugin
    {
        #region Variables

        private const string permission_use = "autolock.use";
        private readonly Dictionary<BasePlayer, CodeLock> AwaitingResponse = new Dictionary<BasePlayer, CodeLock>();
        [PluginReference] private Plugin NoEscape;

        #endregion

        #region Hooks

        private void Init()
        {
            LoadConfig();
            permission.RegisterPermission(permission_use, this);
            _data = Interface.Oxide.DataFileSystem.ReadObject<Data>(Name);

            cmd.AddChatCommand("autolock", this, ChatCommand);
            cmd.AddChatCommand("al", this, ChatCommand);
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var player = plan.GetOwnerPlayer();
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, permission_use)) return;
            var Entity = go.ToBaseEntity() as DecayEntity;
            if (Entity == null || _config.Disabled.Contains(Entity.PrefabName)) return;

            if (NoEscape != null)
            {
                if (_config.NoEscapeSettings.BlockRaid && NoEscape.Call<bool>("IsRaidBlocked", player.UserIDString))
                {
                    player.ChatMessage(lang.GetMessage("RaidBlocked", this, player.UserIDString));
                    return;
                }

                if (_config.NoEscapeSettings.BlockCombat && NoEscape.Call<bool>("IsCombatBlocked", player.UserIDString))
                {
                    player.ChatMessage(lang.GetMessage("CombatBlocked", this, player.UserIDString));
                    return;
                }
            }

            if (!_data.Codes.ContainsKey(player.UserIDString))
                _data.Codes.Add(player.UserIDString, new PlayerData
                {
                    Code = GetRandomCode(),
                    Enabled = true
                });
            var pCode = _data.Codes[player.UserIDString];
            if (!pCode.Enabled || !HasCodeLock(player)) return;
            var S = Entity as StorageContainer;
            if (S?.inventorySlots < 12) return;
            if (!S && !(Entity is AnimatedBuildingBlock)) return;
            if (Entity.IsLocked()) return;
            var Code = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab") as CodeLock;
            if (Code != null)
            {
                Code.Spawn();
                Code.code = pCode.Code;
                Code.SetParent(Entity, Entity.GetSlotAnchorName(BaseEntity.Slot.Lock));
                Entity.SetSlot(BaseEntity.Slot.Lock, Code);
                Code.SetFlag(BaseEntity.Flags.Locked, true);
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock-code-deploy.prefab",
                    Code.transform.position);
                Code.whitelistPlayers.Add(player.userID);
            }

            TakeCodeLock(player);
            player.ChatMessage(string.Format(lang.GetMessage("CodeAdded", this, player.UserIDString),
                player.net.connection.info.GetBool("global.streamermode") ? "****" : pCode.Code));
        }

        private static string GetRandomCode()
        {
            return Random.Range(1000, 9999).ToString();
        }

        private void OnServerShutdown()
        {
            Unload();
        }

        private void Unload()
        {
            SaveData();
            foreach (var Lock in AwaitingResponse.Values.Where(Lock => !Lock.IsDestroyed)) Lock.Kill();
        }

        #endregion

        #region Command

        private void ChatCommand(BasePlayer player, string Label, string[] Args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permission_use))
            {
                player.ChatMessage(lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }

            if (Args.Length < 1)
            {
                player.ChatMessage(string.Format(lang.GetMessage("InvalidArgs", this, player.UserIDString), Label));
                return;
            }

            if (!_data.Codes.ContainsKey(player.UserIDString))
                _data.Codes.Add(player.UserIDString, new PlayerData
                {
                    Code = GetRandomCode(),
                    Enabled = true
                });
            switch (Args[0].ToLower())
            {
                case "code":
                    OpenCodeLockUI(player);
                    break;
                case "toggle":
                    player.ChatMessage(lang.GetMessage(Toggle(player) ? "Enabled" : "Disabled", this,
                        player.UserIDString));
                    break;
                default:
                    player.ChatMessage(string.Format(lang.GetMessage("InvalidArgs", this, player.UserIDString), Label));
                    break;
            }
        }

        private static bool HasCodeLock(BasePlayer Player)
        {
            return Player.inventory.FindItemID(1159991980) != null;
        }

        private static void TakeCodeLock(BasePlayer Player)
        {
            Player.inventory.Take(null, 1159991980, 1);
        }

        private void OpenCodeLockUI(BasePlayer player)
        {
            var Lock = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab",
                player.eyes.position + new Vector3(0, -3, 0)) as CodeLock;
            if (Lock == null) return;
            Lock.Spawn();
            Lock.SetFlag(BaseEntity.Flags.Locked, true);
            Lock.ClientRPCPlayer(null, player, "EnterUnlockCode");
            if (AwaitingResponse.ContainsKey(player)) AwaitingResponse.Remove(player);
            AwaitingResponse.Add(player, Lock);
            if (AwaitingResponse.Count == 1) Subscribe("OnCodeEntered");

            timer.In(20f, () =>
            {
                if (!Lock.IsDestroyed) Lock.Kill();
            });
        }

        private void OnCodeEntered(CodeLock codeLock, BasePlayer player, string code)
        {
            if (player == null || !AwaitingResponse.ContainsKey(player)) return;
            var A = AwaitingResponse[player];
            if (A != codeLock)
            {
                if (!A.IsDestroyed) A.Kill();
                AwaitingResponse.Remove(player);
                return;
            }

            var pData = _data.Codes[player.UserIDString];
            pData.Code = code;
            player.ChatMessage(string.Format(lang.GetMessage("CodeUpdated", this, player.UserIDString),
                player.net.connection.info.GetBool("global.streamermode") ? "****" : code));

            var Prefab = A.effectCodeChanged;
            if (!A.IsDestroyed) A.Kill();
            AwaitingResponse.Remove(player);

            Effect.server.Run(Prefab.resourcePath, player.transform.position);
            if (AwaitingResponse.Count < 1) Unsubscribe("OnCodeEntered");
        }

        private bool Toggle(BasePlayer player)
        {
            var Data = _data.Codes[player.UserIDString];
            var newToggle = !Data.Enabled;
            Data.Enabled = newToggle;
            return newToggle;
        }

        #endregion

        #region Configuration & Language

        private ConfigFile _config;
        private Data _data;

        private class PlayerData
        {
            public string Code;
            public bool Enabled;
        }

        private class Data
        {
            public readonly Dictionary<string, PlayerData> Codes = new Dictionary<string, PlayerData>();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"CodeAdded", "Codelock placed with code {0}."},
                {"Disabled", "You have disabled auto locks."},
                {"Enabled", "You have enabled auto locks."},
                {"CodeUpdated", "Your new code is {0}."},
                {"NoPermission", "You don't have permission."},
                {"InvalidArgs", "/{0} code|toggle|hide"},
                {"RaidBlocked", "The codelock wasn't automatically locked due to you being raid blocked!"},
                {"CombatBlocked", "The codelock wasn't automatically locked due to you being combat blocked!"}
            }, this);
        }

        public class ConfigFile
        {
            [JsonProperty("Disabled Items (Prefabs)")]
            public List<string> Disabled;

            [JsonProperty("No Escape")] public NoEscapeSettings NoEscapeSettings;

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    Disabled = new List<string>
                    {
                        "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab"
                    },
                    NoEscapeSettings = new NoEscapeSettings
                    {
                        BlockCombat = true,
                        BlockRaid = true
                    }
                };
            }
        }

        public class NoEscapeSettings
        {
            [JsonProperty("Block Auto Lock whilst in Combat?")]
            public bool BlockCombat;

            [JsonProperty("Block Auto Lock whilst Raid Blocked?")]
            public bool BlockRaid;
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigFile>();
            if (_config == null) LoadDefaultConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = ConfigFile.DefaultConfig();
            PrintWarning("Default configuration has been loaded.");
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion
    }
}
//Generated with birthdates' Plugin Maker