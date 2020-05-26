using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Loot Defender", "Author Egor Blagov, Maintainer nivex", "1.0.5")]
    [Description("Defends loot from other players who dealt less damage than you.")]
    class LootDefender : RustPlugin
    {
        [PluginReference]
        Plugin PersonalHeli, BlackVenom;

        private const float KillEntitySpawnRadius = 10.0f;
        private const string permUse = "lootdefender.use";
        private const string permAdm = "lootdefender.adm";
        private static LootDefender Instance;

        #region Config

        class PluginConfig
        {
            public int AttackTimeoutSeconds = 300;
            public float RelativeAdvantageMin = 0.05f;
            public bool UseTeams = true;
            public string HexColorSinglePlayer = "#6d88ff";
            public string HexColorTeam = "#ff804f";
            public string HexColorOk = "#88ff6d";
            public string HexColorNotOk = "#ff5716";

            public int LockBradleySeconds = 900;
            public int LockHeliSeconds = 900;
            public int LockNPCSeconds = 300;
            public bool RemoveFireFromCrates = true;
        }

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            Config.WriteObject(config, true);
        }

        #endregion

        #region Stored data

        class StoredData
        {
            public Dictionary<uint, DamageInfo> damageInfos = new Dictionary<uint, DamageInfo>();
            public Dictionary<uint, LockInfo> lockInfos = new Dictionary<uint, LockInfo>();

            public void Sanitize()
            {
                HashSet<uint> allNetIds = new HashSet<uint>();
                foreach (var ent in BaseNetworkable.serverEntities)
                {
                    if (ent?.net != null)
                    {
                        allNetIds.Add(ent.net.ID);
                    }
                }

                var damageList = new List<uint>(damageInfos.Keys);

                foreach (var id in damageList)
                {
                    if (!allNetIds.Contains(id))
                    {
                        damageInfos.Remove(id);
                    }
                }

                damageList.Clear();

                var lockList = new List<uint>(lockInfos.Keys);

                foreach (var id in lockList)
                {
                    if (!allNetIds.Contains(id))
                    {
                        lockInfos.Remove(id);
                    }
                }

                lockList.Clear();
                allNetIds.Clear();
            }
        }

        private StoredData storedData = new StoredData();

        private Dictionary<uint, LockInfo> lockInfos => storedData.lockInfos;

        private Dictionary<uint, DamageInfo> damageInfos => storedData.damageInfos;

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData, true);
        }

        private void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch { }

            if (storedData == null)
            {
                storedData = new StoredData();
                SaveData();
            }
        }

        #endregion

        #region L10N

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You have no permission to use this command",
                ["DamageReport"] = "Damage report for {0}",
                ["CannotLoot"] = "You cannot loot it, major damage was not from you",
                ["CannotMine"] = "You cannot mine it, major damage was not from you",
                ["Heli"] = "Patrol helicopter",
                ["Bradley"] = "Bradley APC"
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "У Вас нет привилегии на использование этой команды",
                ["DamageReport"] = "Нанесенный урон по {0}",
                ["CannotLoot"] = "Это не Ваш лут, основная часть урона нанесена другими игроками",
                ["CannotMine"] = "Вы не можете добывать это, основная часть урона насена другими игроками",
                ["Heli"] = "Патрульному вертолету",
                ["Bradley"] = "Танку"
            }, this, "ru");
        }

        private static string _(string key, string userId, params object[] args)
        {
            string message = Instance.lang.GetMessage(key, Instance, userId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        #endregion

        #region Damage and Locks calculation

        class DamageEntry
        {
            public float DamageDealt;
            public DateTime Timestamp;

            [JsonIgnore]
            public bool IsOutdated => DateTime.Now.Subtract(Timestamp).TotalSeconds > Instance.config.AttackTimeoutSeconds;
            public void AddDamage(float amount)
            {
                DamageDealt += amount;
                Timestamp = DateTime.Now;
            }
        }

        class DamageInfo
        {
            private readonly StringBuilder sb = new StringBuilder();
            public Dictionary<ulong, DamageEntry> damageEntries = new Dictionary<ulong, DamageEntry>();
            public string NameKey;
            [JsonIgnore]
            public float FullDamage => damageEntries.Values.Select(x => x.DamageDealt).Sum();
            public DamageInfo() : this("unknown")
            {

            }
            public DamageInfo(string nameKey)
            {
                NameKey = nameKey;
            }

            public void AddDamage(HitInfo info)
            {
                DamageEntry entry;
                if (!damageEntries.TryGetValue(info.InitiatorPlayer.userID, out entry))
                {
                    damageEntries[info.InitiatorPlayer.userID] = entry = new DamageEntry();
                }

                entry.AddDamage(info.damageTypes.Total());
            }

            public void OnKilled()
            {
                var damageList = new List<ulong>(damageEntries.Keys);

                foreach (var key in damageList)
                {
                    if (damageEntries[key].IsOutdated)
                    {
                        damageEntries.Remove(key);
                    }
                }

                DisplayDamageReport();
                damageList.Clear();
            }

            public void DisplayDamageReport()
            {
                foreach (var damager in damageEntries.Keys)
                {
                    var player = RelationshipManager.FindByID(damager);

                    if (!player.IsValid())
                    {
                        continue;
                    }

                    Instance.SendReply(player, GetDamageReport(player.UserIDString));
                }
            }

            public string GetDamageReport(string playerId)
            {
                var damageGroups = GetDamageGroups();
                var topDamageGroups = GetTopDamageGroups(damageGroups);

                sb.Length = 0;
                sb.AppendLine($"{_("DamageReport", playerId, $"<color={Instance.config.HexColorOk}>{_(NameKey, playerId)}</color>")}:");

                foreach (var dg in damageGroups)
                {
                    if (topDamageGroups.Contains(dg))
                    {
                        sb.Append($"<color={Instance.config.HexColorOk}>√</color> ");
                    }
                    else
                    {
                        sb.Append($"<color={Instance.config.HexColorNotOk}>✖</color> ");
                    }

                    sb.Append($"{dg.ToReport(this)}\n");
                }

                string result = sb.ToString();
                sb.Length = 0;

                return result;
            }

            public bool CanInteract(ulong playerId)
            {
                var topDamageGroups = GetTopDamageGroups(GetDamageGroups());
                var ableToInteract = topDamageGroups.SelectMany(x => x.Players).ToList();

                if (ableToInteract == null || ableToInteract.Count == 0)
                {
                    return true;
                }

                return ableToInteract.Contains(playerId);
            }

            private List<DamageGroup> GetTopDamageGroups(List<DamageGroup> damageGroups)
            {
                var topDamageGroups = new List<DamageGroup>();

                if (damageGroups.Count == 0)
                {
                    return topDamageGroups;
                }

                var topDamageGroup = damageGroups.OrderByDescending(x => x.TotalDamage).First();

                foreach (var dg in damageGroups)
                {
                    if ((topDamageGroup.TotalDamage - dg.TotalDamage) <= Instance.config.RelativeAdvantageMin * FullDamage)
                    {
                        topDamageGroups.Add(dg);
                    }
                }

                return topDamageGroups;
            }

            private List<DamageGroup> GetDamageGroups()
            {
                var result = new List<DamageGroup>();

                foreach (var damage in damageEntries)
                {
                    bool merged = false;

                    foreach (var dT in result)
                    {
                        if (dT.TryMergeDamage(damage.Key, damage.Value.DamageDealt))
                        {
                            merged = true;
                            break;
                        }
                    }

                    if (!merged)
                    {
                        if (RelationshipManager.FindByID(damage.Key) == null)
                        {
                            Instance.PrintError($"Invalid id, unable to find: {damage.Key}");
                            continue;
                        }

                        result.Add(new DamageGroup(damage.Key, damage.Value.DamageDealt));
                    }
                }

                return result;
            }
        }

        class LockInfo
        {
            public DamageInfo damageInfo;
            public DateTime LockTimestamp;
            public int LockTimeout;

            [JsonIgnore]
            public bool IsLockOutdated => DateTime.Now.Subtract(LockTimestamp).TotalSeconds >= LockTimeout;

            public LockInfo(DamageInfo damageInfo, int lockTimeout)
            {
                LockTimestamp = DateTime.Now;
                LockTimeout = lockTimeout;
                this.damageInfo = damageInfo;
            }

            public bool CanInteract(ulong playerId) => damageInfo.CanInteract(playerId);
            public string GetDamageReport(string userIdString) => damageInfo.GetDamageReport(userIdString);
        }

        class DamageGroup
        {
            public float TotalDamage { get; private set; }
            public List<ulong> Players => new List<ulong> { FirstDamagerDealer }.Concat(additionalPlayers).ToList();
            public bool IsSingle => additionalPlayers.Count == 0;
            private ulong FirstDamagerDealer { get; set; }
            private List<ulong> additionalPlayers { get; } = new List<ulong>();

            public DamageGroup(ulong playerId, float damage)
            {
                TotalDamage = damage;
                FirstDamagerDealer = playerId;

                if (!Instance.config.UseTeams)
                {
                    return;
                }

                var target = RelationshipManager.FindByID(playerId);

                if (!target.IsValid())
                {
                    return;
                }

                RelationshipManager.PlayerTeam team;
                if (!RelationshipManager.Instance.playerToTeam.TryGetValue(playerId, out team))
                {
                    return;
                }

                for (int i = 0; i < team.members.Count; i++)
                {
                    ulong member = team.members[i];

                    if (member == playerId)
                    {
                        continue;
                    }

                    additionalPlayers.Add(member);
                }
            }

            public bool TryMergeDamage(ulong playerId, float damageAmount)
            {
                if (IsPlayerInvolved(playerId))
                {
                    TotalDamage += damageAmount;
                    return true;
                }

                return false;
            }

            public bool IsPlayerInvolved(ulong playerId) => playerId == FirstDamagerDealer || additionalPlayers.Contains(playerId);

            public string ToReport(DamageInfo damageInfo)
            {
                if (IsSingle)
                {
                    return getLineForPlayer(FirstDamagerDealer, Instance.config.HexColorSinglePlayer, damageInfo);
                }

                return string.Format("({1}) {0:0}%",
                    TotalDamage / damageInfo.FullDamage * 100,
                    string.Join(" ", Players.Select(x => getLineForPlayer(x, Instance.config.HexColorTeam, damageInfo)))
                );
            }

            private string getLineForPlayer(ulong playerId, string color, DamageInfo damageInfo)
            {
                var displayName = RelationshipManager.FindByID(playerId)?.displayName ?? Instance.covalence.Players.FindPlayerById(playerId.ToString())?.Name ?? playerId.ToString();
                float damage = 0.0f;
                if (damageInfo.damageEntries.ContainsKey(playerId))
                {
                    damage = damageInfo.damageEntries[playerId].DamageDealt;
                }
                string damageLine = string.Format("{0:0}%", damage / damageInfo.FullDamage * 100);
                return $"<color={color}>{displayName}</color> {damageLine}";
            }
        }

        #endregion

        #region uMod hooks

        private void OnServerSave()
        {
            SaveData();
        }

        private void Init()
        {
            Instance = this;
            AddCovalenceCommand("testlootdef", nameof(CommandTest), permAdm);
            permission.RegisterPermission(permUse, this);

            try
            {
                config = Config.ReadObject<PluginConfig>();
            }
            catch { }

            if (config == null)
            {
                config = new PluginConfig();
            }

            Config.WriteObject(config, true);
            LoadData();

            storedData.Sanitize();
        }

        private void Unload()
        {
            SaveData();
            Instance = null;
        }

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo hitInfo)
        {
            if (!entity.IsValid())
            {
                return;
            }

            var attacker = GetPlayerFromHitInfo(hitInfo);

            if (!attacker.IsValid() || attacker.IsNpc || !permission.UserHasPermission(attacker.UserIDString, permUse))
            {
                return;
            }

            string nameKey = null;

            if (entity is BaseHelicopter)
            {
                var heli = entity as BaseHelicopter;

                if (heli.IsValid())
                {
                    if (PersonalHeli != null && PersonalHeli.IsLoaded)
                    {
                        var success = PersonalHeli?.Call("IsPersonal", heli);

                        if (success != null && success is bool && (bool)success)
                        {
                            return;
                        }
                    }

                    if (BlackVenom != null && BlackVenom.IsLoaded && entity.OwnerID.IsSteamId())
                    {
                        var success = BlackVenom?.Call("IsBlackVenom", heli, attacker);

                        if (success != null && success is bool && !(bool)success)
                        {
                            return;
                        }
                    }

                    nameKey = "Heli";
                }
            }

            if (entity is BradleyAPC)
            {
                nameKey = "Bradley";
            }

            if (entity is BasePlayer) // why npcs? :p
            {
                var player = entity as BasePlayer;

                if (player.IsValid() && player.IsNpc)
                {
                    nameKey = player.displayName;
                }
            }

            if (string.IsNullOrEmpty(nameKey))
            {
                return;
            }

            DamageInfo damageInfo;
            if (!damageInfos.TryGetValue(entity.net.ID, out damageInfo))
            {
                damageInfos[entity.net.ID] = damageInfo = new DamageInfo(nameKey);
            }

            damageInfo.AddDamage(hitInfo);
        }

        private object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (!permission.UserHasPermission(attacker.UserIDString, permUse))
            {
                return null;
            }

            if (info.HitEntity is ServerGib && info.WeaponPrefab is BaseMelee)
            {
                LockInfo lockInfo;
                if (lockInfos.TryGetValue(info.HitEntity.net.ID, out lockInfo))
                {
                    if (lockInfo.IsLockOutdated)
                    {
                        lockInfos.Remove(info.HitEntity.net.ID);
                        return null;
                    }

                    if (!lockInfo.CanInteract(attacker.userID))
                    {
                        SendReply(attacker, _("CannotMine", attacker.UserIDString));
                        SendReply(attacker, lockInfo.GetDamageReport(attacker.UserIDString));
                        return false;
                    }
                }
            }

            return null;
        }

        private void OnEntityKill(BaseEntity entity)
        {
            if (!entity.IsValid() || (!damageInfos.ContainsKey(entity.net.ID) && !lockInfos.ContainsKey(entity.net.ID)))
            {
                return;
            }

            if (entity is BaseHelicopter || entity is BradleyAPC)
            {
                damageInfos[entity.net.ID].OnKilled();
                var lockInfo = new LockInfo(damageInfos[entity.net.ID], entity is BaseHelicopter ? Instance.config.LockHeliSeconds : Instance.config.LockBradleySeconds);
                LockInRadius<LootContainer>(entity.transform.position, lockInfo, KillEntitySpawnRadius);
                LockInRadius<HelicopterDebris>(entity.transform.position, lockInfo, KillEntitySpawnRadius);
            }

            if (entity is BasePlayer)
            {
                var npc = entity as BasePlayer;
                var corpses = Pool.GetList<NPCPlayerCorpse>();
                Vis.Entities(npc.transform.position, 3.0f, corpses);
                var corpse = corpses.FirstOrDefault(x => x.parentEnt == npc);
                if (corpse.IsValid())
                {
                    damageInfos[entity.net.ID].OnKilled();
                    lockInfos[corpse.net.ID] = new LockInfo(damageInfos[entity.net.ID], Instance.config.LockNPCSeconds);
                }
                Pool.FreeList(ref corpses);
            }

            if (entity is NPCPlayerCorpse)
            {
                var corpse = entity as NPCPlayerCorpse;
                var corpsePos = corpse.transform.position;
                var corpseId = corpse.playerSteamID;
                var lockInfo = lockInfos[corpse.net.ID];
                NextTick(() =>
                {
                    var containers = Pool.GetList<DroppedItemContainer>();
                    Vis.Entities(corpsePos, 1.0f, containers);
                    var container = containers.FirstOrDefault(x => x.playerSteamID == corpseId);
                    if (container.IsValid())
                    {
                        lockInfos[container.net.ID] = lockInfo;
                    }
                    Pool.FreeList(ref containers);
                });
            }

            if (lockInfos.ContainsKey(entity.net.ID))
            {
                lockInfos.Remove(entity.net.ID);
            }
            else damageInfos.Remove(entity.net.ID);
        }

        private object CanLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse) || !entity.IsValid())
            {
                return null;
            }

            LockInfo lockInfo;
            if (!lockInfos.TryGetValue(entity.net.ID, out lockInfo))
            {
                return null;
            }

            if (lockInfo.IsLockOutdated)
            {
                lockInfos.Remove(entity.net.ID);
                return null;
            }

            if (!lockInfo.CanInteract(player.userID))
            {
                SendReply(player, _("CannotLoot", player.UserIDString));
                SendReply(player, lockInfo.GetDamageReport(player.UserIDString));
                return false;
            }

            return null;
        }

        #endregion

        private void LockInRadius<T>(Vector3 position, LockInfo lockInfo, float radius) where T : BaseEntity
        {
            var entities = Facepunch.Pool.GetList<T>();
            Vis.Entities(position, radius, entities);
            foreach (var ent in entities)
            {
                lockInfos[ent.net.ID] = lockInfo;

                if (config.RemoveFireFromCrates)
                {
                    var e = (ent as LockedByEntCrate)?.lockingEnt?.ToBaseEntity();

                    if (e.IsValid() && !e.IsDestroyed)
                    {
                        e.Kill();
                    }
                }
            }
            Pool.FreeList(ref entities);
        }

        private BasePlayer GetPlayerFromHitInfo(HitInfo hitInfo)
        {
            var player = hitInfo?.Initiator as BasePlayer;

            if (!player.IsValid() && hitInfo?.Initiator is BaseMountable)
            {
                player = GetMountedPlayer(hitInfo.Initiator as BaseMountable);
            }

            return player;
        }

        private static BasePlayer GetMountedPlayer(BaseMountable m)
        {
            if (m.GetMounted())
            {
                return m.GetMounted();
            }

            if (m is BaseVehicle)
            {
                var vehicle = m as BaseVehicle;

                foreach (var point in vehicle.mountPoints)
                {
                    if (point.mountable.IsValid() && point.mountable.GetMounted())
                    {
                        return point.mountable.GetMounted();
                    }
                }
            }

            return null;
        }

        private void CommandTest(IPlayer p, string command, string[] args)
        {
            p.Reply($"total damage infos: {damageInfos.Count}");
            p.Reply($"total lock infos: {lockInfos.Count}");
        }
    }
}
