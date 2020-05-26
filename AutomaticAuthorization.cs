using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using ProtoBuf;

namespace Oxide.Plugins
{
    [Info("Automatic Authorization", "k1lly0u/Arainrr", "1.1.6", ResourceId = 2063)]
    public class AutomaticAuthorization : RustPlugin
    {
        #region Fields

        [PluginReference] private readonly Plugin Clans, Friends;
        private bool initialized = false;
        private const string PERMISSION_USE = "automaticauthorization.use";
        private Dictionary<ulong, EntityEntry> playerEntites = new Dictionary<ulong, EntityEntry>();

        public class EntityEntry
        {
            public HashSet<AutoTurret> autoTurrets = new HashSet<AutoTurret>();
            public HashSet<BuildingPrivlidge> buildingPrivlidges = new HashSet<BuildingPrivlidge>();
        }

        #endregion Fields

        #region OxideHooks

        private void Init()
        {
            LoadData();
            UpdateData();
            permission.RegisterPermission(PERMISSION_USE, this);
            cmd.AddChatCommand(configData.chatSettings.command, this, nameof(CmdAutoAuth));
            if (!configData.pluginEnabled)
            {
                Unsubscribe(nameof(OnPlayerInit));
                Unsubscribe(nameof(OnEntitySpawned));
                Unsubscribe(nameof(OnEntityKill));
                Unsubscribe(nameof(CanUseLockedEntity));
            }
        }

        private void OnServerInitialized()
        {
            if (!configData.pluginEnabled) return;
            initialized = true;
            foreach (var entity in BaseNetworkable.serverEntities)
                if (entity is BaseEntity)
                    CheckEntity(entity as BaseEntity);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;
            if (permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                CreateShareData(player.userID);
        }

        private void OnServerSave() => timer.Once(UnityEngine.Random.Range(0f, 60f), () => SaveData());

        private void Unload() => SaveData();

        private void OnEntitySpawned(BaseEntity entity) => CheckEntity(entity, true);

        private void CheckEntity(BaseEntity entity, bool justCreated = false)
        {
            if (!initialized || entity == null || entity.OwnerID == 0) return;
            if (entity is BuildingPrivlidge)
            {
                var buildingPrivlidge = entity as BuildingPrivlidge;
                if (playerEntites.ContainsKey(entity.OwnerID)) playerEntites[entity.OwnerID].buildingPrivlidges.Add(buildingPrivlidge);
                else playerEntites.Add(entity.OwnerID, new EntityEntry { buildingPrivlidges = new HashSet<BuildingPrivlidge> { buildingPrivlidge } });
                if (justCreated && permission.UserHasPermission(entity.OwnerID.ToString(), PERMISSION_USE))
                    AuthToCupboard(new HashSet<BuildingPrivlidge> { buildingPrivlidge }, entity.OwnerID, true);
            }
            else if (entity is AutoTurret)
            {
                var autoTurret = entity as AutoTurret;
                if (playerEntites.ContainsKey(entity.OwnerID)) playerEntites[entity.OwnerID].autoTurrets.Add(autoTurret);
                else playerEntites.Add(entity.OwnerID, new EntityEntry { autoTurrets = new HashSet<AutoTurret> { autoTurret } });
                if (justCreated && permission.UserHasPermission(entity.OwnerID.ToString(), PERMISSION_USE))
                    AuthToTurret(new HashSet<AutoTurret> { autoTurret }, entity.OwnerID, true);
            }
        }

        private void OnEntityKill(BaseEntity entity)
        {
            if (entity == null || entity.OwnerID == 0) return;
            if (entity is BuildingPrivlidge)
            {
                var buildingPrivlidge = entity as BuildingPrivlidge;
                foreach (var entry in playerEntites)
                {
                    if (entry.Value.buildingPrivlidges.Contains(buildingPrivlidge))
                    {
                        entry.Value.buildingPrivlidges.Remove(buildingPrivlidge);
                        return;
                    }
                }
            }
            else if (entity is AutoTurret)
            {
                var autoTurret = entity as AutoTurret;
                foreach (var entry in playerEntites)
                {
                    if (entry.Value.autoTurrets.Contains(autoTurret))
                    {
                        entry.Value.autoTurrets.Remove(autoTurret);
                        return;
                    }
                }
            }
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            var parentEntity = baseLock?.GetParentEntity();
            if (player == null || parentEntity == null || !baseLock.IsLocked() || parentEntity.OwnerID == 0) return null;
            if (!permission.UserHasPermission(parentEntity.OwnerID.ToString(), PERMISSION_USE)) return null;
            StoredData.ShareData shareData = GetShareData(parentEntity.OwnerID);
            if (shareData.friendsShareEntry.enabled && HasFriend(parentEntity.OwnerID, player.userID))
            {
                if (baseLock is KeyLock)
                {
                    if (shareData.friendsShareEntry.shareKeyLock && CanUnlockEntity(parentEntity, configData.friendsShareSettings.keyLockSettings))
                        return true;
                }
                else if (baseLock is CodeLock)
                {
                    if (shareData.friendsShareEntry.shareCodeLock && CanUnlockEntity(parentEntity, configData.friendsShareSettings.codeLockSettings))
                        return SendUnlockedEffect(baseLock as CodeLock);
                }
            }
            if (shareData.clanShareEntry.enabled && SameClan(parentEntity.OwnerID, player.userID))
            {
                if (baseLock is KeyLock)
                {
                    if (shareData.clanShareEntry.shareKeyLock && CanUnlockEntity(parentEntity, configData.clanShareSettings.keyLockSettings))
                        return true;
                }
                else if (baseLock is CodeLock)
                {
                    if (shareData.clanShareEntry.shareCodeLock && CanUnlockEntity(parentEntity, configData.clanShareSettings.codeLockSettings))
                        return SendUnlockedEffect(baseLock as CodeLock);
                }
            }
            return null;
        }

        private bool CanUnlockEntity(BaseEntity parentEntity, ConfigData.LockSettings lockConfigSettings)
        {
            if (parentEntity is Door)
            {
                if (lockConfigSettings.shareDoor) return true;
            }
            else if (parentEntity is BoxStorage)
            {
                if (lockConfigSettings.shareBox) return true;
            }
            else if (lockConfigSettings.shareOtherEntity) return true;
            return false;
        }

        private bool SendUnlockedEffect(CodeLock codeLock)
        {
            Effect.server.Run(codeLock.effectUnlocked.resourcePath, codeLock.transform.position);
            return true;
        }

        #endregion OxideHooks

        #region Functions

        private enum AutoAuthType
        {
            All,
            Turret,
            Cupboard,
        }

        private void UpdateAuthList(ulong playerID, AutoAuthType autoAuthType, bool justCreated = false)
        {
            if (!permission.UserHasPermission(playerID.ToString(), PERMISSION_USE) || !configData.pluginEnabled) return;
            if (!playerEntites.ContainsKey(playerID)) return;
            var autoTurrets = playerEntites[playerID].autoTurrets;
            var buildingPrivlidges = playerEntites[playerID].buildingPrivlidges;
            switch (autoAuthType)
            {
                case AutoAuthType.All:
                    AuthToCupboard(buildingPrivlidges, playerID, justCreated);
                    AuthToTurret(autoTurrets, playerID, justCreated);
                    return;

                case AutoAuthType.Turret:
                    AuthToTurret(autoTurrets, playerID, justCreated);
                    return;

                case AutoAuthType.Cupboard:
                    AuthToCupboard(buildingPrivlidges, playerID, justCreated);
                    return;
            }
        }

        private void AuthToCupboard(HashSet<BuildingPrivlidge> buildingPrivlidges, ulong playerID, bool justCreated = false)
        {
            if (buildingPrivlidges.Count <= 0) return;
            List<PlayerNameID> authList = GetPlayerNameIDs(playerID, AutoAuthType.Cupboard);
            foreach (var buildingPrivlidge in buildingPrivlidges)
            {
                if (buildingPrivlidge == null || buildingPrivlidge.IsDestroyed) continue;
                buildingPrivlidge.authorizedPlayers = authList;
                buildingPrivlidge.SendNetworkUpdateImmediate();
            }

            var player = RustCore.FindPlayerById(playerID);
            if (player == null || !justCreated || !configData.chatSettings.sendMessage) return;
            if (authList.Count > 1) Print(player, Lang("CupboardSuccess", player.UserIDString, authList.Count - 1, buildingPrivlidges.Count));
        }

        private void AuthToTurret(HashSet<AutoTurret> autoTurrets, ulong playerID, bool justCreated = false)
        {
            if (autoTurrets.Count <= 0) return;
            List<PlayerNameID> authList = GetPlayerNameIDs(playerID, AutoAuthType.Turret);
            foreach (var autoTurret in autoTurrets)
            {
                if (autoTurret == null || autoTurret.IsDestroyed) continue;
                bool isOnline = false;
                if (autoTurret.IsOnline())
                {
                    autoTurret.SetIsOnline(false);
                    isOnline = true;
                }
                autoTurret.authorizedPlayers = authList;
                autoTurret.SendNetworkUpdateImmediate();
                if (isOnline) autoTurret.SetIsOnline(true);
            }
            var player = RustCore.FindPlayerById(playerID);
            if (player == null || !justCreated || !configData.chatSettings.sendMessage) return;
            if (authList.Count > 1) Print(player, Lang("TurretSuccess", player.UserIDString, authList.Count - 1, autoTurrets.Count));
        }

        private List<PlayerNameID> GetPlayerNameIDs(ulong playerID, AutoAuthType autoAuthType)
        {
            List<PlayerNameID> playerNameIDs = new List<PlayerNameID>();
            HashSet<ulong> authList = GetAuthList(playerID, autoAuthType);
            foreach (var auth in authList)
                playerNameIDs.Add(new PlayerNameID { userid = auth, username = RustCore.FindPlayerById(auth)?.displayName ?? string.Empty, ShouldPool = true });
            return playerNameIDs;
        }

        private HashSet<ulong> GetAuthList(ulong playerID, AutoAuthType autoAuthType)
        {
            StoredData.ShareData shareData = GetShareData(playerID);
            HashSet<ulong> sharePlayers = new HashSet<ulong> { playerID };
            if (shareData.friendsShareEntry.enabled && (autoAuthType == AutoAuthType.Turret ? shareData.friendsShareEntry.shareTurret : shareData.friendsShareEntry.shareCupboard))
                foreach (var ID in GetFriends(playerID))
                    sharePlayers.Add(ID);
            if (shareData.clanShareEntry.enabled && (autoAuthType == AutoAuthType.Turret ? shareData.clanShareEntry.shareTurret : shareData.clanShareEntry.shareCupboard))
                foreach (var ID in GetClanMembers(playerID))
                    sharePlayers.Add(ID);
            return sharePlayers;
        }

        private StoredData.ShareData GetShareData(ulong playerID)
        {
            if (!storedData.playerShareData.ContainsKey(playerID)) CreateShareData(playerID);
            return storedData.playerShareData[playerID];
        }

        private void CreateShareData(ulong playerID)
        {
            if (storedData.playerShareData.ContainsKey(playerID)) return;
            storedData.playerShareData.Add(playerID, new StoredData.ShareData
            {
                friendsShareEntry = new StoredData.ShareDataEntry
                {
                    enabled = configData.friendsShareSettings.enabled,
                    shareTurret = configData.friendsShareSettings.shareTurret,
                    shareCupboard = configData.friendsShareSettings.shareCupboard,
                    shareKeyLock = configData.friendsShareSettings.keyLockSettings.enabled,
                    shareCodeLock = configData.friendsShareSettings.codeLockSettings.enabled,
                },
                clanShareEntry = new StoredData.ShareDataEntry
                {
                    enabled = configData.clanShareSettings.enabled,
                    shareTurret = configData.clanShareSettings.shareTurret,
                    shareCupboard = configData.clanShareSettings.shareCupboard,
                    shareKeyLock = configData.clanShareSettings.keyLockSettings.enabled,
                    shareCodeLock = configData.clanShareSettings.codeLockSettings.enabled,
                }
            });
        }

        private void UpdateData()
        {
            foreach (var entry in storedData.playerShareData)
            {
                if (!configData.friendsShareSettings.enabled) entry.Value.friendsShareEntry.enabled = false;
                if (!configData.friendsShareSettings.shareCupboard) entry.Value.friendsShareEntry.shareCupboard = false;
                if (!configData.friendsShareSettings.shareTurret) entry.Value.friendsShareEntry.shareTurret = false;
                if (!configData.friendsShareSettings.keyLockSettings.enabled) entry.Value.friendsShareEntry.shareKeyLock = false;
                if (!configData.friendsShareSettings.codeLockSettings.enabled) entry.Value.friendsShareEntry.shareCodeLock = false;

                if (!configData.clanShareSettings.enabled) entry.Value.clanShareEntry.enabled = false;
                if (!configData.clanShareSettings.shareCupboard) entry.Value.clanShareEntry.shareCupboard = false;
                if (!configData.clanShareSettings.shareTurret) entry.Value.clanShareEntry.shareTurret = false;
                if (!configData.clanShareSettings.keyLockSettings.enabled) entry.Value.clanShareEntry.shareKeyLock = false;
                if (!configData.clanShareSettings.codeLockSettings.enabled) entry.Value.clanShareEntry.shareCodeLock = false;
            }
            SaveData();
        }

        #region Clan

        private void OnClanDestroy(string clanName) => UpdateClanAuthList(clanName);

        private void OnClanUpdate(string clanName) => UpdateClanAuthList(clanName);

        private void UpdateClanAuthList(string clanName)
        {
            foreach (var member in GetClanMembers(clanName))
                UpdateAuthList(member, AutoAuthType.All);
        }

        private List<ulong> GetClanMembers(ulong ownerID)
        {
            var clanName = Clans?.Call("GetClanOf", ownerID);
            if (clanName != null && clanName is string)
                return GetClanMembers((string)clanName);
            return new List<ulong>();
        }

        private List<ulong> GetClanMembers(string clanName)
        {
            var clan = Clans?.Call("GetClan", clanName);
            if (clan != null && clan is JObject)
            {
                var members = (clan as JObject).GetValue("members");
                if (members != null && members is JArray)
                    return ((JArray)members).Select(x => ulong.Parse(x.ToString())).ToList();
            }
            return new List<ulong>();
        }

        private bool SameClan(ulong playerID, ulong otherPlayerID)
        {
            if (Clans == null) return false;
            if (Clans.ResourceId == 842)//Rust:IO Clans
            {
                var playerClan = Clans?.Call("GetClanOf", playerID);
                var otherPlayerClan = Clans?.Call("GetClanOf", otherPlayerID);
                if (playerClan != null && playerClan is string && otherPlayerClan != null && otherPlayerClan is string)
                    return (string)playerClan == (string)otherPlayerClan;
                return false;
            }
            else//Clans
            {
                var isMember = Clans?.Call("IsClanMember", playerID.ToString(), otherPlayerID.ToString());
                if (isMember != null && (bool)isMember) return true;
            }
            return false;
        }

        #endregion Clan

        #region Friends

        private void OnFriendAdded(string playerID, string friendID) => UpdateFriendAuthList(playerID);

        private void OnFriendRemoved(string playerID, string friendID) => UpdateFriendAuthList(playerID);

        private void UpdateFriendAuthList(string playerID) => UpdateAuthList(ulong.Parse(playerID), AutoAuthType.All);

        private List<ulong> GetFriends(ulong playerID)
        {
            var friends = Friends?.Call("GetFriends", playerID);
            if (friends != null && friends is ulong[])
                return (friends as ulong[]).ToList();
            return new List<ulong>();
        }

        private bool HasFriend(ulong playerID, ulong otherPlayerID)
        {
            if (Friends == null) return false;
            var friend = Friends?.Call("HasFriend", playerID, otherPlayerID);
            if (friend != null && (bool)friend) return true;
            return false;
        }

        #endregion Friends

        #endregion Functions

        #region ChatCommands

        private void CmdAutoAuth(BasePlayer player, string command, string[] args)
        {
            if (!configData.pluginEnabled)
            {
                Print(player, Lang("PluginDisabled", player.UserIDString));
                return;
            }
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            CreateShareData(player.userID);
            if (args == null || args.Length == 0)
            {
                if (Clans == null && Friends == null)
                {
                    Print(player, Lang("NoSharePlugin", player.UserIDString));
                    return;
                }
                StringBuilder stringBuilder = new StringBuilder();
                if (Friends != null)
                {
                    stringBuilder.AppendLine(Lang("AutoShareFriendsStatus", player.UserIDString));
                    stringBuilder.AppendLine(Lang("AutoShareFriends", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.enabled ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                    stringBuilder.AppendLine(Lang("AutoShareFriendsCupboard", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.shareCupboard ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                    stringBuilder.AppendLine(Lang("AutoShareFriendsTurret", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.shareTurret ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                    stringBuilder.AppendLine(Lang("AutoShareFriendsKeyLock", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.shareKeyLock ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                    stringBuilder.AppendLine(Lang("AutoShareFriendsCodeLock", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.shareCodeLock ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                }
                if (Clans != null)
                {
                    stringBuilder.AppendLine(Lang("AutoShareClansStatus", player.UserIDString));
                    stringBuilder.AppendLine(Lang("AutoShareClans", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.enabled ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                    stringBuilder.AppendLine(Lang("AutoShareClansCupboard", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.shareCupboard ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                    stringBuilder.AppendLine(Lang("AutoShareClansTurret", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.shareTurret ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                    stringBuilder.AppendLine(Lang("AutoShareClansKeyLock", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.shareKeyLock ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                    stringBuilder.AppendLine(Lang("AutoShareClansCodeLock", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.shareCodeLock ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                }
                Print(player, $"\n{stringBuilder.ToString()}");
                return;
            }

            switch (args[0].ToLower())
            {
                case "af":
                case "autofriends":
                    if (!configData.friendsShareSettings.enabled)
                    {
                        Print(player, Lang("FriendsDisabled", player.UserIDString));
                        return;
                    }
                    if (args.Length <= 1)
                    {
                        storedData.playerShareData[player.userID].friendsShareEntry.enabled = !storedData.playerShareData[player.userID].friendsShareEntry.enabled;
                        Print(player, Lang("Friends", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.enabled ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                        UpdateAuthList(player.userID, AutoAuthType.All);
                        return;
                    }
                    else
                    {
                        switch (args[1].ToLower())
                        {
                            case "c":
                            case "cupboard":
                                if (!configData.friendsShareSettings.shareCupboard)
                                {
                                    Print(player, Lang("FriendsCupboardDisabled", player.UserIDString));
                                    return;
                                }
                                storedData.playerShareData[player.userID].friendsShareEntry.shareCupboard = !storedData.playerShareData[player.userID].friendsShareEntry.shareCupboard;
                                Print(player, Lang("FriendsCupboard", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.shareCupboard ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                                UpdateAuthList(player.userID, AutoAuthType.Cupboard);
                                return;

                            case "t":
                            case "turret":
                                if (!configData.friendsShareSettings.shareTurret)
                                {
                                    Print(player, Lang("FriendsTurretDisable", player.UserIDString));
                                    return;
                                }
                                storedData.playerShareData[player.userID].friendsShareEntry.shareTurret = !storedData.playerShareData[player.userID].friendsShareEntry.shareTurret;
                                Print(player, Lang("FriendsTurret", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.shareTurret ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                                UpdateAuthList(player.userID, AutoAuthType.Turret);
                                return;

                            case "kl":
                            case "keylock":
                                if (!configData.friendsShareSettings.keyLockSettings.enabled)
                                {
                                    Print(player, Lang("FriendsKeyLockDisable", player.UserIDString));
                                    return;
                                }
                                storedData.playerShareData[player.userID].friendsShareEntry.shareKeyLock = !storedData.playerShareData[player.userID].friendsShareEntry.shareKeyLock;
                                Print(player, Lang("FriendsKeyLock", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.shareKeyLock ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                                return;

                            case "cl":
                            case "codelock":
                                if (!configData.friendsShareSettings.codeLockSettings.enabled)
                                {
                                    Print(player, Lang("FriendsCodeLockDisable", player.UserIDString));
                                    return;
                                }
                                storedData.playerShareData[player.userID].friendsShareEntry.shareCodeLock = !storedData.playerShareData[player.userID].friendsShareEntry.shareCodeLock;
                                Print(player, Lang("FriendsCodeLock", player.UserIDString, storedData.playerShareData[player.userID].friendsShareEntry.shareCodeLock ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                                return;

                            case "h":
                            case "help":
                                StringBuilder stringBuilder1 = new StringBuilder();
                                stringBuilder1.AppendLine(Lang("FriendsSyntax", player.UserIDString, configData.chatSettings.command));
                                stringBuilder1.AppendLine(Lang("FriendsSyntax1", player.UserIDString, configData.chatSettings.command));
                                stringBuilder1.AppendLine(Lang("FriendsSyntax2", player.UserIDString, configData.chatSettings.command));
                                stringBuilder1.AppendLine(Lang("FriendsSyntax3", player.UserIDString, configData.chatSettings.command));
                                stringBuilder1.AppendLine(Lang("FriendsSyntax4", player.UserIDString, configData.chatSettings.command));
                                Print(player, $"\n{stringBuilder1.ToString()}");
                                return;
                        }
                    }
                    Print(player, Lang("SyntaxError", player.UserIDString, configData.chatSettings.command));
                    return;

                case "ac":
                case "autoclan":
                    if (!configData.clanShareSettings.enabled)
                    {
                        Print(player, Lang("ClansDisabled", player.UserIDString));
                        return;
                    }
                    if (args.Length <= 1)
                    {
                        storedData.playerShareData[player.userID].clanShareEntry.enabled = !storedData.playerShareData[player.userID].clanShareEntry.enabled;
                        Print(player, Lang("Clans", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.enabled ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                        UpdateAuthList(player.userID, AutoAuthType.All);
                        return;
                    }
                    else
                    {
                        switch (args[1].ToLower())
                        {
                            case "c":
                            case "cupboard":
                                if (!configData.clanShareSettings.shareCupboard)
                                {
                                    Print(player, Lang("ClansCupboardDisable", player.UserIDString));
                                    return;
                                }
                                storedData.playerShareData[player.userID].clanShareEntry.shareCupboard = !storedData.playerShareData[player.userID].clanShareEntry.shareCupboard;
                                Print(player, Lang("ClansCupboard", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.shareCupboard ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                                UpdateAuthList(player.userID, AutoAuthType.Cupboard);
                                return;

                            case "t":
                            case "turret":
                                if (!configData.clanShareSettings.shareTurret)
                                {
                                    Print(player, Lang("ClansTurretDisable", player.UserIDString));
                                    return;
                                }
                                storedData.playerShareData[player.userID].clanShareEntry.shareTurret = !storedData.playerShareData[player.userID].clanShareEntry.shareTurret;
                                Print(player, Lang("ClansTurret", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.shareTurret ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                                UpdateAuthList(player.userID, AutoAuthType.Turret);
                                return;

                            case "kl":
                            case "keylock":
                                if (!configData.clanShareSettings.keyLockSettings.enabled)
                                {
                                    Print(player, Lang("ClansKeyLockDisable", player.UserIDString));
                                    return;
                                }
                                storedData.playerShareData[player.userID].clanShareEntry.shareKeyLock = !storedData.playerShareData[player.userID].clanShareEntry.shareKeyLock;
                                Print(player, Lang("ClansKeyLock", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.shareKeyLock ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                                return;

                            case "cl":
                            case "codelock":
                                if (!configData.clanShareSettings.codeLockSettings.enabled)
                                {
                                    Print(player, Lang("ClansCodeLockDisable", player.UserIDString));
                                    return;
                                }
                                storedData.playerShareData[player.userID].clanShareEntry.shareCodeLock = !storedData.playerShareData[player.userID].clanShareEntry.shareCodeLock;
                                Print(player, Lang("ClansCodeLock", player.UserIDString, storedData.playerShareData[player.userID].clanShareEntry.shareCodeLock ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));

                                return;

                            case "h":
                            case "help":
                                StringBuilder stringBuilder1 = new StringBuilder();
                                stringBuilder1.AppendLine(Lang("ClansSyntax", player.UserIDString, configData.chatSettings.command));
                                stringBuilder1.AppendLine(Lang("ClansSyntax1", player.UserIDString, configData.chatSettings.command));
                                stringBuilder1.AppendLine(Lang("ClansSyntax2", player.UserIDString, configData.chatSettings.command));
                                stringBuilder1.AppendLine(Lang("ClansSyntax3", player.UserIDString, configData.chatSettings.command));
                                stringBuilder1.AppendLine(Lang("ClansSyntax4", player.UserIDString, configData.chatSettings.command));
                                Print(player, $"\n{stringBuilder1.ToString()}");
                                return;
                        }
                    }
                    Print(player, Lang("SyntaxError", player.UserIDString, configData.chatSettings.command));
                    return;

                case "h":
                case "help":
                    if (Clans == null && Friends == null)
                    {
                        Print(player, Lang("NoSharePlugin", player.UserIDString));
                        return;
                    }
                    StringBuilder stringBuilder = new StringBuilder();
                    if (Friends != null)
                    {
                        stringBuilder.AppendLine(Lang("FriendsSyntax", player.UserIDString, configData.chatSettings.command));
                        stringBuilder.AppendLine(Lang("FriendsSyntax1", player.UserIDString, configData.chatSettings.command));
                        stringBuilder.AppendLine(Lang("FriendsSyntax2", player.UserIDString, configData.chatSettings.command));
                        stringBuilder.AppendLine(Lang("FriendsSyntax3", player.UserIDString, configData.chatSettings.command));
                        stringBuilder.AppendLine(Lang("FriendsSyntax4", player.UserIDString, configData.chatSettings.command));
                    }
                    if (Clans != null)
                    {
                        stringBuilder.AppendLine(Lang("ClansSyntax", player.UserIDString, configData.chatSettings.command));
                        stringBuilder.AppendLine(Lang("ClansSyntax1", player.UserIDString, configData.chatSettings.command));
                        stringBuilder.AppendLine(Lang("ClansSyntax2", player.UserIDString, configData.chatSettings.command));
                        stringBuilder.AppendLine(Lang("ClansSyntax3", player.UserIDString, configData.chatSettings.command));
                        stringBuilder.AppendLine(Lang("ClansSyntax4", player.UserIDString, configData.chatSettings.command));
                    }
                    Print(player, $"\n{stringBuilder.ToString()}");
                    return;

                default:
                    Print(player, Lang("SyntaxError", player.UserIDString, configData.chatSettings.command));
                    return;
            }
        }

        #endregion ChatCommands

        #region ConfigurationFile

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Enabled plugin")]
            public bool pluginEnabled = false;

            [JsonProperty(PropertyName = "Friends share settings")]
            public ShareSettings friendsShareSettings = new ShareSettings();

            [JsonProperty(PropertyName = "Clan share settings")]
            public ShareSettings clanShareSettings = new ShareSettings();

            [JsonProperty(PropertyName = "Chat settings")]
            public ChatSettings chatSettings = new ChatSettings();

            public class ChatSettings
            {
                [JsonProperty(PropertyName = "Send authorization success message")]
                public bool sendMessage = true;

                [JsonProperty(PropertyName = "Chat command")]
                public string command = "autoauth";

                [JsonProperty(PropertyName = "Chat prefix")]
                public string prefix = "[AutoAuth]: ";

                [JsonProperty(PropertyName = "Chat prefix color")]
                public string prefixColor = "#00FFFF";

                [JsonProperty(PropertyName = "Chat steamID icon")]
                public ulong steamIDIcon = 0;
            }

            public class ShareSettings
            {
                [JsonProperty(PropertyName = "Enabled")]
                public bool enabled = false;

                [JsonProperty(PropertyName = "Share cupboard")]
                public bool shareCupboard = false;

                [JsonProperty(PropertyName = "Share turret")]
                public bool shareTurret = false;

                [JsonProperty(PropertyName = "Key lock settings")]
                public LockSettings keyLockSettings = new LockSettings();

                [JsonProperty(PropertyName = "Code lock settings")]
                public LockSettings codeLockSettings = new LockSettings();
            }

            public class LockSettings
            {
                [JsonProperty(PropertyName = "Enabled")]
                public bool enabled = false;

                [JsonProperty(PropertyName = "Share door")]
                public bool shareDoor = false;

                [JsonProperty(PropertyName = "Share box")]
                public bool shareBox = false;

                [JsonProperty(PropertyName = "Share other locked entities")]
                public bool shareOtherEntity = false;
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
            public Dictionary<ulong, ShareData> playerShareData = new Dictionary<ulong, ShareData>();

            public class ShareData
            {
                public ShareDataEntry friendsShareEntry = new ShareDataEntry();
                public ShareDataEntry clanShareEntry = new ShareDataEntry();
            }

            public class ShareDataEntry
            {
                public bool enabled = false;
                public bool shareCupboard = false;
                public bool shareTurret = false;
                public bool shareKeyLock = false;
                public bool shareCodeLock = false;
            }
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

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);

        private void OnNewSave(string filename) => ClearData();

        #endregion DataFile

        #region LanguageFile

        private void Print(BasePlayer player, string message) => Player.Message(player, message, $"<color={configData.chatSettings.prefixColor}>{configData.chatSettings.prefix}</color>", configData.chatSettings.steamIDIcon);

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "You do not have permission to use this command",
                ["PluginDisabled"] = "The plugin is not enabled. Unable to use chat command",
                ["Enabled"] = "<color=#8ee700>Enabled</color>",
                ["Disabled"] = "<color=#ce422b>Disabled</color>",
                ["NoSharePlugin"] = "Clans and Friends is not installed on this server. Unable to automatically authorize other players",
                ["SyntaxError"] = "Syntax error, please enter '<color=#ce422b>/{0} <help | h></color>' to view help",
                ["TurretSuccess"] = "Successfully added <color=#ce422b>{0}</color> friends/clan members to <color=#ce422b>{1}</color> turrets auth list",
                ["CupboardSuccess"] = "Successfully added <color=#ce422b>{0}</color> friends/clan members to <color=#ce422b>{1}</color> cupboards auth list",

                ["FriendsSyntax"] = "<color=#ce422b>/{0} <autofriends | af></color> - Enable/Disable automatic authorization for your friends",
                ["FriendsSyntax1"] = "<color=#ce422b>/{0} <autofriends | af> <cupboard | c></color> - Sharing cupboard with your friends",
                ["FriendsSyntax2"] = "<color=#ce422b>/{0} <autofriends | af> <turret | t></color> - Sharing turret with your friends",
                ["FriendsSyntax3"] = "<color=#ce422b>/{0} <autofriends | af> <keylock | kl></color> - Sharing key lock with your friends",
                ["FriendsSyntax4"] = "<color=#ce422b>/{0} <autofriends | af> <codelock | cl></color> - Sharing code lock with your friends",

                ["ClansSyntax"] = "<color=#ce422b>/{0} <autoclan | ac></color> - Enable/Disable automatic authorization for your clan members",
                ["ClansSyntax1"] = "<color=#ce422b>/{0} <autoclan | ac> <cupboard | c></color> - Sharing cupboard with your clan members",
                ["ClansSyntax2"] = "<color=#ce422b>/{0} <autoclan | ac> <turret | t></color> - Sharing turret with your clan members",
                ["ClansSyntax3"] = "<color=#ce422b>/{0} <autoclan | ac> <keylock | kl></color> - Sharing key lock with your clan members",
                ["ClansSyntax4"] = "<color=#ce422b>/{0} <autoclan | ac> <codelock | cl></color> - Sharing code lock with your clan members",

                ["AutoShareFriendsStatus"] = "<color=#ffa500>Current friends sharing status: </color>",
                ["AutoShareFriends"] = "Automatically sharing with friends: {0}",
                ["AutoShareFriendsCupboard"] = "Automatically sharing cupboard with friends: {0}",
                ["AutoShareFriendsTurret"] = "Automatically sharing turret with friends: {0}",
                ["AutoShareFriendsKeyLock"] = "Automatically sharing key lock with friends: {0}",
                ["AutoShareFriendsCodeLock"] = "Automatically sharing code lock with friends: {0}",

                ["AutoShareClansStatus"] = "<color=#ffa500>Current clan sharing status: </color>",
                ["AutoShareClans"] = "Automatically sharing with clan: {0}",
                ["AutoShareClansCupboard"] = "Automatically sharing cupboard with clan: {0}",
                ["AutoShareClansTurret"] = "Automatically sharing turret with clan: {0}",
                ["AutoShareClansKeyLock"] = "Automatically sharing key lock with clan: {0}",
                ["AutoShareClansCodeLock"] = "Automatically sharing code lock with clan: {0}",

                ["Friends"] = "Friends automatic authorization {0}",
                ["FriendsCupboard"] = "Sharing cupboard with friends is {0}",
                ["FriendsTurret"] = "Sharing turret with friends is {0}",
                ["FriendsKeyLock"] = "Sharing key lock with friends is {0}",
                ["FriendsCodeLock"] = "Sharing code lock with friends is {0}",

                ["Clans"] = "Clan automatic authorization {0}",
                ["ClansCupboard"] = "Sharing cupboard with clan is {0}",
                ["ClansTurret"] = "Sharing turret with clan is {0}",
                ["ClansKeyLock"] = "Sharing key lock with clan is {0}",
                ["ClansCodeLock"] = "Sharing code lock with clan is {0}",

                ["FriendsDisabled"] = "Server has disabled friends sharing",
                ["FriendsCupboardDisabled"] = "Server has disabled sharing cupboard with friends",
                ["FriendsTurretDisable"] = "Server has disabled sharing turret with friends",
                ["FriendsKeyLockDisable"] = "Server has disabled sharing key lock with friends",
                ["FriendsCodeLockDisable"] = "Server has disabled sharing code lock with friends",

                ["ClansDisabled"] = "Server has disabled clan sharing",
                ["ClansCupboardDisable"] = "Server has disabled sharing cupboard with clan",
                ["ClansTurretDisable"] = "Server has disabled sharing turret with clan",
                ["ClansKeyLockDisable"] = "Server has disabled sharing key lock with clan",
                ["ClansCodeLockDisable"] = "Server has disabled sharing code lock with clan",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "您没有权限使用该命令",
                ["PluginDisabled"] = "插件未启用，无法使用聊天命令",
                ["Enabled"] = "<color=#8ee700>已启用</color>",
                ["Disabled"] = "<color=#ce422b>已禁用</color>",
                ["NoSharePlugin"] = "服务器未没有安装战队插件和朋友插件。无法使用自动授权",
                ["SyntaxError"] = "语法错误, 输入 '<color=#ce422b>/{0} <help | h></color>' 查看帮助",
                ["TurretSuccess"] = "自动添加了 <color=#ce422b>{0}</color> 个朋友/战队成员到您的 <color=#ce422b>{1}</color> 个炮台授权列表中",
                ["CupboardSuccess"] = "自动添加了 <color=#ce422b>{0}</color> 个朋友/战队成员到您的 <color=#ce422b>{1}</color> 个领地柜授权列表中",

                ["FriendsSyntax"] = "<color=#ce422b>/{0} <autofriends | af></color> - 启用/禁用朋友自动授权",
                ["FriendsSyntax1"] = "<color=#ce422b>/{0} <autofriends | af> <cupboard | c></color> - 自动与朋友共享领地柜",
                ["FriendsSyntax2"] = "<color=#ce422b>/{0} <autofriends | af> <turret | t></color> - 自动与朋友共享炮台",
                ["FriendsSyntax3"] = "<color=#ce422b>/{0} <autofriends | af> <keylock | kl></color> - 自动与朋友共享钥匙锁",
                ["FriendsSyntax4"] = "<color=#ce422b>/{0} <autofriends | af> <codelock | cl></color> - 自动与朋友共享密码锁",

                ["ClansSyntax"] = "<color=#ce422b>/{0} <autoclan | ac></color> - 启用/禁用战队自动授权",
                ["ClansSyntax1"] = "<color=#ce422b>/{0} <autoclan | ac> <cupboard | c></color> - 自动与战队共享领地柜",
                ["ClansSyntax2"] = "<color=#ce422b>/{0} <autoclan | ac> <turret | t></color> - 自动与战队共享炮台",
                ["ClansSyntax3"] = "<color=#ce422b>/{0} <autoclan | ac> <keylock | kl></color> - 自动与战队共享钥匙锁",
                ["ClansSyntax4"] = "<color=#ce422b>/{0} <autoclan | ac> <codelock | cl></color> - 自动与战队共享密码锁",

                ["AutoShareFriendsStatus"] = "<color=#ffa500>当前朋友自动授权状态: </color>",
                ["AutoShareFriends"] = "自动与朋友共享: {0}",
                ["AutoShareFriendsCupboard"] = "自动与朋友共享领地柜: {0}",
                ["AutoShareFriendsTurret"] = "自动与朋友共享炮台: {0}",
                ["AutoShareFriendsKeyLock"] = "自动与朋友共享钥匙锁: {0}",
                ["AutoShareFriendsCodeLock"] = "自动与朋友共享密码锁: {0}",

                ["AutoShareClansStatus"] = "<color=#ffa500>当前战队自动授权状态: </color>",
                ["AutoShareClans"] = "自动与战队共享: {0}",
                ["AutoShareClansCupboard"] = "自动与战队共享领地柜: {0}",
                ["AutoShareClansTurret"] = "自动与战队共享炮台: {0}",
                ["AutoShareClansKeyLock"] = "自动与战队共享钥匙锁: {0}",
                ["AutoShareClansCodeLock"] = "自动与战队共享密码锁: {0}",

                ["Friends"] = "朋友自动授权 {0}",
                ["FriendsCupboard"] = "自动与朋友共享领地柜 {0}",
                ["FriendsTurret"] = "自动与朋友共享炮台 {0}",
                ["FriendsKeyLock"] = "自动与朋友共享钥匙锁 {0}",
                ["FriendsCodeLock"] = "自动与朋友共享密码锁 {0}",

                ["Clans"] = "战队自动授权 {0}",
                ["ClansCupboard"] = "自动与战队共享领地柜 {0}",
                ["ClansTurret"] = "自动与战队共享炮台 {0}",
                ["ClansKeyLock"] = "自动与战队共享钥匙锁 {0}",
                ["ClansCodeLock"] = "自动与战队共享密码锁 {0}",

                ["FriendsDisabled"] = "服务器已禁用朋友自动授权",
                ["FriendsCupboardDisabled"] = "服务器已禁用自动与朋友共享领地柜",
                ["FriendsTurretDisable"] = "服务器已禁用自动与朋友共享炮台",
                ["FriendsKeyLockDisable"] = "服务器已禁用自动与朋友共享钥匙锁",
                ["FriendsCodeLockDisable"] = "服务器已禁用自动与朋友共享密码锁",

                ["ClansDisabled"] = "服务器已禁用战队自动授权",
                ["ClansCupboardDisable"] = "服务器已禁用自动与战队共享领地柜",
                ["ClansTurretDisable"] = "服务器已禁用自动与战队共享炮台",
                ["ClansKeyLockDisable"] = "服务器已禁用自动与战队共享钥匙锁",
                ["ClansCodeLockDisable"] = "服务器已禁用自动与战队共享密码锁",
            }, this, "zh-CN");
        }

        #endregion LanguageFile
    }
}