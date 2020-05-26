//Requires: ArenaStatistics
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Configuration;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("Arena Events", "k1lly0u", "0.1.65")]
    [Description("Logic controllers from Arena events")]
    class ArenaEvents : RustPlugin
    {
        #region Fields
        StoredData storedData;
        private DynamicConfigFile data;

        public static Arena Arena;        
        public static ArenaStatistics Statistics;
        public static ArenaEvents ins;

        private static NavMeshHit navHit;

        private static FieldInfo _brain = typeof(global::HumanNPC).GetField("_brain", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        private static FieldInfo _memory = typeof(HumanNPC).GetField("myMemory", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        private static FrameBudgeter frameBudgeter;

        private const string FLAG_PREFAB = "assets/prefabs/deployable/signs/sign.post.double.prefab";
        private const string SCIENTIST_PREFAB = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc.prefab";

        static bool isUnloading;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("Arena/event_specific_data");
            ToggleHookSubscription(false);
            isUnloading = false;
        }

        private void OnServerInitialized()
        {
            ins = this;
            LoadData();

            frameBudgeter = new GameObject("ArenaEvents.NPCFrameBudgeter").AddComponent<FrameBudgeter>();
        }       

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {            
            if (entity != null && info != null)
            {
                CTF.CTFFlag ctfFlag = entity.GetComponent<CTF.CTFFlag>() ?? entity.GetComponentInParent<CTF.CTFFlag>();
                if (ctfFlag != null)                
                    Arena.NullifyDamage(info);                
            }
        }

        private void OnEntityDeath(ScientistNPC entity, HitInfo info)
        {
            if (entity == null)
                return;

            NPCSurvival.NPCController npcController = entity.GetComponent<NPCSurvival.NPCController>();
            if (npcController != null)            
                npcController.eventGame.OnNpcDeath(entity, info);                       
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {           
            if (Arena == null || entity == null)
                return;

            if (entity.GetComponent<LootableCorpse>())
            {
                foreach (KeyValuePair<string, Arena.EventManager> eventGame in Arena.events)
                {
                    if (eventGame.Value is NPCSurvival && eventGame.Value.IsEventNPC((entity as LootableCorpse).playerSteamID))
                    {
                        eventGame.Value.OnNpcCorpseSpawned(entity as LootableCorpse);
                        break;
                    }
                }
            }
        }

        private object OnPlayerDropActiveItem(BasePlayer player, Item item)
        {
            NPCPlayer npcPlayer = player?.GetComponent<NPCPlayer>();
            if (npcPlayer != null)
            {
                foreach (KeyValuePair<string, Arena.EventManager> eventGame in Arena.events)
                {
                    if (eventGame.Value is NPCSurvival)
                    {
                        if (eventGame.Value.IsEventNPC(player.userID))
                            return false;
                    }
                }
            }
            return null;
        }

        private object OnNpcTarget(global::HumanNPC npcPlayer, BaseEntity target)
        {
            NPCSurvival.NPCController monitor = npcPlayer?.GetComponent<NPCSurvival.NPCController>();
            if (monitor != null)
            {
                return monitor.CanTargetEntity(target);               
            }
            return null;
        }
        
        // Used for Slasher game mode to make it night time, disabled until spectate has been fixed
        //private object CanNetworkTo(BaseNetworkable entity, BasePlayer player)
        //{
        //    if (!(entity is EnvSync))            
        //        return null;
            
        //    Arena.EventPlayer eventPlayer = Arena.GetUser(player);
        //    if (eventPlayer == null)
        //        return null;

        //    Slasher manager = Arena.GetEvent(eventPlayer.currentEvent) as Slasher;
        //    if (manager == null)
        //        return null;

        //    if (manager.matchStatus == Slasher.MatchStatus.Pending)
        //        return null;

        //    EnvSync env = entity as EnvSync;
        //    if (Net.sv.write.Start())
        //    {
        //        Connection connection = player.net.connection;
        //        connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
        //        BaseNetworkable.SaveInfo saveInfo = new BaseNetworkable.SaveInfo
        //        {
        //            forConnection = player.net.connection,
        //            forDisk = false
        //        };
        //        Net.sv.write.PacketID(Message.Type.Entities);
        //        Net.sv.write.UInt32(player.net.connection.validate.entityUpdates);
        //        using (saveInfo.msg = Pool.Get<Entity>())
        //        {
        //            env.Save(saveInfo);                   
        //            saveInfo.msg.environment.dateTime = manager.timeSync;                   
        //            saveInfo.msg.ToProto(Net.sv.write);
        //            env.PostSave(saveInfo);
        //            Net.sv.write.Send(new SendInfo(player.net.connection));
        //        }
        //    }
        //    return false;
        //}

        private void Unload()
        {
            if (Arena != null)
                Arena.isUnloading = true;

            UnityEngine.Object.Destroy(frameBudgeter.gameObject);

            isUnloading = true;
            ins = null;
            Statistics = null;
            Arena = null;
        }
        #endregion

        #region NPC Logic Limiter
        private class FrameBudgeter : MonoBehaviour
        {
            private double maxMilliseconds = 1f;

            private Stopwatch sw = Stopwatch.StartNew();

            private int lastIndex = 0;

            private void Update()
            {
                sw.Reset();
                sw.Start();

                int count = NPCSurvival.NPCController.allNpcs?.Count ?? 0;
                if (lastIndex >= count)
                    lastIndex = 0;

                for (int i = lastIndex; i < count; i++)
                {
                    if (sw.Elapsed.TotalMilliseconds > maxMilliseconds)
                    {
                        lastIndex = i;
                        return;
                    }

                    NPCSurvival.NPCController.allNpcs[i]?.DoUpdate();
                }

                lastIndex = 0;
            }
        }
        #endregion

        #region Functions
        private void ToggleHookSubscription(bool enabled)
        {
            if (enabled)
            {
                Subscribe(nameof(OnEntityTakeDamage));
                Subscribe(nameof(OnEntityDeath));
                Subscribe(nameof(OnEntitySpawned));
                Subscribe(nameof(OnPlayerDropActiveItem));
            }
            else
            {
                Unsubscribe(nameof(OnEntityTakeDamage));
                Unsubscribe(nameof(OnEntityDeath));
                Unsubscribe(nameof(OnEntitySpawned));
                Unsubscribe(nameof(OnPlayerDropActiveItem));
            }
        }

        public void RegisterEvents(Arena arena)
        {
            Arena = arena;
            Statistics = Arena.Statistics;

            ToggleHookSubscription(true);

            Arena.eventTypes.Add("Free For All");
            Arena.eventTypes.Add("Team Deathmatch");
            Arena.eventTypes.Add("Survival");
            Arena.eventTypes.Add("Team Survival");
            Arena.eventTypes.Add("GunGame");
            Arena.eventTypes.Add("One in the Chamber");
            Arena.eventTypes.Add("Capture the Flag");
            Arena.eventTypes.Add("NPC Survival");
            //Arena.eventTypes.Add("Slasher"); Disabled until spectate is fixed...
            //Arena.eventTypes.Add("Infected");

            Arena.additionalParameters.Add("GunGame", new List<Arena.AdditionalParamData>
                {
                    new Arena.AdditionalParamData
                    {
                        field = "Weapon Set",
                        isRequired = true,
                        type = "string",
                        useSelector = true,
                        selectorHook = "GetWeaponSets",
                        selectorDesc = "Select a weapon set",
                        useInput = false
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Use Downgrade Weapon",
                        isRequired = true,
                        type = "bool",
                        useToggle = true
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Player Gear",
                        isRequired = true,
                        type = "string",
                        useSelector = true,
                        selectorDesc = "Select a kit for the players gear (No Weapons)",
                        selectorHook = "GetAllKits"
                    }
                });
            Arena.additionalParameters.Add("One in the Chamber", new List<Arena.AdditionalParamData>
                {
                    new Arena.AdditionalParamData
                    {
                        field = "Player Lives",
                        isRequired = true,
                        requireValue = true,
                        type = "int",
                        useInput = true
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Player Gear",
                        isRequired = true,
                        type = "string",
                        useSelector = true,
                        selectorDesc = "Select a kit for the players gear (No Weapons)",
                        selectorHook = "GetAllKits"
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Primary Weapon",
                        isRequired = true,
                        type = "string",
                        useSelector = true,
                        selectorHook = "GetAllWeapons",
                        selectorDesc = "Select a primary weapon. This will be the weapon that receives bullets for kills",
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Secondary Weapon",
                        isRequired = true,
                        type = "string",
                        useSelector = true,
                        selectorHook = "GetAllWeapons",
                        selectorDesc = "Select a secondary weapon. This will be the weapon that you use when you run out of bullets",
                    },
                });
            Arena.additionalParameters.Add("NPC Survival", new List<Arena.AdditionalParamData>
                {
                    new Arena.AdditionalParamData
                    {
                        field = "Player Lives",
                        isRequired = true,
                        requireValue = true,
                        type = "int",
                        useInput = true
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Rounds to Play",
                        isRequired = true,
                        requireValue = true,
                        type = "int",
                        useInput = true
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "NPCs to Spawn",
                        isRequired = true,
                        requireValue = true,
                        type = "int",
                        useInput = true
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Additional NPCs (per round)",
                        isRequired = true,
                        type = "int",
                        useInput = true
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "NPC Spawnfile",
                        isRequired = true,
                        type = "string",
                        useSelector = true,
                        selectorHook = "GetSpawnfileNames",
                        selectorDesc = "Select a spawnfile for NPCs",
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Use Scientists",
                        isRequired = true,
                        type = "bool",
                        useToggle = true
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Use Murderers",
                        isRequired = true,
                        type = "bool",
                        useToggle = true
                    }
                });

            Arena.additionalParameters.Add("Infected", new List<Arena.AdditionalParamData>()
            {
                new Arena.AdditionalParamData
                    {
                        field = "Infected Kit",
                        isRequired = true,
                        type = "string",
                        useSelector = true,
                        selectorDesc = "Select a kit for the infected",
                        selectorHook = "GetAllKits"
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Infected Spawnfile",
                        isRequired = true,
                        type = "string",
                        useSelector = true,
                        selectorHook = "GetSpawnfileNames",
                        selectorDesc = "Select a spawnfile for the infected",
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Survivors start with random weapon",
                        isRequired = true,
                        type = "bool",
                        useToggle = true
                    },
                    new Arena.AdditionalParamData
                    {
                        field = "Round Timer",
                        isRequired = true,
                        requireValue = true,
                        type = "int",
                        useInput = true
                    },
            });
            //Arena.additionalParameters.Add("Slasher", new List<Arena.AdditionalParamData>
            //    {                   
            //        new Arena.AdditionalParamData
            //        {
            //            field = "Rounds to Play",
            //            isRequired = true,
            //            requireValue = true,
            //            type = "int",
            //            useInput = true
            //        },
            //        new Arena.AdditionalParamData
            //        {
            //            field = "Slasher Kill Timer (seconds)",
            //            isRequired = true,
            //            requireValue = true,
            //            type = "int",
            //            useInput = true
            //        },
            //        new Arena.AdditionalParamData
            //        {
            //            field = "Player Kill Timer (seconds)",
            //            isRequired = true,
            //            requireValue = true,
            //            type = "int",
            //            useInput = true
            //        },
            //        new Arena.AdditionalParamData
            //        {
            //            field = "Slasher Weapon",
            //            isRequired = true,
            //            type = "string",
            //            useSelector = true,
            //            selectorHook = "GetAllWeapons",
            //            selectorDesc = "Select the weapon the slasher will use. This weapon will be automatically equipped with a flashlight",
            //        },
            //        new Arena.AdditionalParamData
            //        {
            //            field = "Slasher Kit",
            //            isRequired = true,
            //            type = "string",
            //            useSelector = true,
            //            selectorDesc = "Select a kit for the slasher (no weapons)",
            //            selectorHook = "GetAllKits"
            //        },
            //        new Arena.AdditionalParamData
            //        {
            //            field = "Slasher Spawnfile",
            //            isRequired = true,
            //            type = "string",
            //            useSelector = true,
            //            selectorHook = "GetSpawnfileNames",
            //            selectorDesc = "Select a spawnfile for the slasher",
            //        },
            //        new Arena.AdditionalParamData
            //        {
            //            field = "Player Kit",
            //            isRequired = true,
            //            type = "string",
            //            useSelector = true,
            //            selectorDesc = "Select a kit for the hunted (no weapons)",
            //            selectorHook = "GetAllKits"
            //        },
            //        new Arena.AdditionalParamData
            //        {
            //            field = "Player Damage Modifier",
            //            isRequired = true,
            //            requireValue = true,
            //            type = "float",
            //            useInput = true
            //        },
            //    });
        }
        public object SetEventMode(string eventName, GameObject eventManager)
        {
            switch (eventName)
            {
                case "Free For All":
                    eventManager.gameObject.AddComponent<FreeForAll>();
                    return true;
                case "Team Deathmatch":
                    eventManager.gameObject.AddComponent<TDM>();
                    return true;
                case "Survival":
                    eventManager.gameObject.AddComponent<Survival>();
                    return true;
                case "Team Survival":
                    eventManager.gameObject.AddComponent<TeamSurvival>();
                    return true;
                case "One in the Chamber":
                    eventManager.gameObject.AddComponent<OITC>();
                    return true;
                case "GunGame":
                    eventManager.gameObject.AddComponent<GunGame>();
                    return true;
                case "Capture the Flag":
                    eventManager.gameObject.AddComponent<CTF>();
                    return true;
                case "NPC Survival":
                    eventManager.gameObject.AddComponent<NPCSurvival>();
                    return true;
                //case "Slasher":
                //    eventManager.gameObject.AddComponent<Slasher>();
                //    return true;
                case "Infected":
                    eventManager.gameObject.AddComponent<Infected>();
                    return true;
                default:
                    break;
            }
            return null;
        }

        private string[] GetWeaponSets() => storedData.gungameSets.Keys.ToArray();        

        private string[] GetAllWeapons() => ItemManager.itemList.Where(x => x.category == ItemCategory.Weapon).Select(x => x.shortname).ToArray();        
        #endregion

        #region Event Require Checks
        public bool IsTeamEvent(string eventType)
        {
            if (eventType == "Team Deathmatch" || eventType == "Team Survival" || eventType == "Capture the Flag")
                return true;
            return false;
        }

        public bool IsSurvivalEvent(string eventType)
        {
            if (eventType == "Survival" || eventType == "Team Survival" || eventType == "One in the Chamber" || eventType == "NPC Survival")
                return true;
            return false;
        }

        public bool IgnoreKillLimit(string eventType)
        {
            if (eventType == "Survival" || eventType == "Team Survival" || eventType == "One in the Chamber" || eventType == "GunGame" || eventType == "NPC Survival" || eventType == "Slasher" || eventType == "Infected")
                return true;
            return false;
        }

        public bool IgnoreTimeLimit(string eventType)
        {
            if (eventType == "Slasher" || eventType == "Infected")
                return true;
            return false;
        }  
        
        public bool IgnoreKitSelection(string eventType)
        {
            if (eventType == "One in the Chamber" || eventType == "GunGame" || eventType == "Slasher")
                return true;
            return false;
        }

        public bool IgnoreClassSelector(string eventType)
        {
            if (eventType == "One in the Chamber" || eventType == "GunGame" || eventType == "NPC Survival" || eventType == "Slasher" || eventType == "Infected")
                return true;
            return false;
        }

        public bool RequiresTimeOrKillLimit(string eventType)
        {
            if (eventType == "Team Deathmatch" || eventType == "Free For All" || eventType == "Capture the Flag")
                return true;
            return false;
        }
        #endregion

        #region Game Modes
        #region Free For All
        private class FreeForAll : Arena.EventManager
        {
            private Arena.EventPlayer winner = null;

            public override void Prestart()
            {
                winner = null;
                base.Prestart();
            }

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team = Arena.Team.None)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        return;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        return;
                    case Arena.EventStatus.Started:
                        SpawnPlayer(eventPlayer, true, true);
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("leftevent", new string[] { eventPlayer.player.displayName });
                base.LeaveEvent(eventPlayer);
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.AddDeath();

                if (attacker != null)
                {
                    Arena.EventPlayer attackerPlayer = Arena.GetUser(attacker);

                    if (attackerPlayer != null && victim != attackerPlayer)
                    {
                        attackerPlayer.AddKill();
                        if (config.killLimit > 0 && attackerPlayer.kills >= config.killLimit)
                        {
                            winner = attackerPlayer;
                            EndMatch();
                            return;
                        }
                    }                  
                }               
               
                UpdateScoreboard();
                base.OnPlayerDeath(victim, attacker);
            }

            public override void EndMatch()
            {
                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                if (winner == null)
                {
                    if (eventPlayers.Count > 0)
                    {
                        int kills = 0;
                        int deaths = 0;

                        foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                        {
                            if (eventPlayer.kills > kills)
                            {
                                winner = eventPlayer;
                                kills = eventPlayer.kills;
                                deaths = eventPlayer.deaths;
                            }
                            else if (eventPlayer.kills == kills)
                            {
                                if (eventPlayer.deaths < deaths)
                                {
                                    winner = eventPlayer;
                                    kills = eventPlayer.kills;
                                    deaths = eventPlayer.deaths;
                                }
                            }
                        }                        
                    }
                }               

                List<ulong> losers = eventPlayers.Select(x => x.player.userID).ToList();
                if (winner != null)
                {
                    Statistics.OnEventWin(new List<ulong> { winner.player.userID }, config);
                    BroadcastWinners("globalwin", new string[] { winner.player.displayName, config.eventName });
                    losers.Remove(winner.player.userID);
                }                

                Statistics.OnEventLose(losers, config);

                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                    Statistics.OnGamePlayed(eventPlayer.player.userID, config);

                base.EndMatch();
            }
        }
        #endregion

        #region Team Deathmatch
        private class TDM : Arena.EventManager
        {
            public int teamAKills;
            public int teamBKills;

            public override void Prestart()
            {
                teamAKills = 0;
                teamBKills = 0;
                base.Prestart();
            }

            public override void StartMatch()
            {
                BalanceTeams();
                base.StartMatch();
            }

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team = Arena.Team.None)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();
                Arena.LockInventory(eventPlayer.player);

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        return;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        return;
                    case Arena.EventStatus.Started:
                        SpawnPlayer(eventPlayer, true, true);
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                Arena.UnlockInventory(eventPlayer.player);
                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("leftevent", new string[] { eventPlayer.player.displayName });
                base.LeaveEvent(eventPlayer);

                if (ins.configData.BalanceOnLeave)
                    BalanceTeams();
            }

            public override void OnPlayerTakeDamage(Arena.EventPlayer eventPlayer, HitInfo info)
            {
                BasePlayer attacker = info.InitiatorPlayer;
                if (attacker != null)
                {
                    Arena.EventPlayer eventAttacker = Arena.GetUser(attacker);
                    if (eventAttacker != null)
                    {
                        if (eventAttacker.team == eventPlayer.team)
                        {
                            BroadcastToPlayer(eventAttacker, Arena.msg("friendlyFire", attacker.userID));
                            if (!config.ffEnabled)
                            {
                                Arena.NullifyDamage(info);
                                return;
                            }
                        }
                    }
                }
                base.OnPlayerTakeDamage(eventPlayer, info);
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.AddDeath();

                if (attacker != null)
                {
                    Arena.EventPlayer attackerPlayer = Arena.GetUser(attacker);

                    if (attackerPlayer != null && victim != attackerPlayer)
                    {
                        attackerPlayer.AddKill();

                        if (attackerPlayer.team == Arena.Team.A)
                            teamAKills++;
                        if (attackerPlayer.team == Arena.Team.B)
                            teamBKills++;

                        if (config.killLimit > 0 && (teamAKills >= config.killLimit || teamBKills >= config.killLimit))
                        {
                            EndMatch();
                            return;
                        }
                    }  
                }
               
                UpdateScoreboard();
                base.OnPlayerDeath(victim, attacker);
            }

            public override void GiveTeamGear(Arena.EventPlayer eventPlayer)
            {
                string kitName = eventPlayer.team == Arena.Team.A ? config.teamA.kit : config.teamB.kit;
                Arena.GiveKit(eventPlayer.player, kitName);
            }

            public override void EndMatch()
            {
                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                Arena.Team team = teamAKills > teamBKills ? Arena.Team.A : teamBKills > teamAKills ? Arena.Team.B : Arena.Team.None;

                List<ulong> losers = eventPlayers.Where(x => x.team != team).Select(y => y.player.userID).ToList();
                Statistics.OnEventLose(losers, config);

                if (team != Arena.Team.None)
                {
                    List<ulong> winners = eventPlayers.Where(x => x.team == team).Select(y => y.player.userID).ToList();
                    Statistics.OnEventWin(winners, config);

                    BroadcastWinners("globalteamwin", new string[] { team == Arena.Team.A ? (string.IsNullOrEmpty(config.teamA.name) ? Arena.msg("Team A") : config.teamA.name) : (string.IsNullOrEmpty(config.teamB.name) ? Arena.msg("Team B") : config.teamB.name), eventPlayers.Where(x => x.team == team).Select(y => y.player.displayName).ToSentence(), config.eventName });
                }

                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                    Statistics.OnGamePlayed(eventPlayer.player.userID, config);
                
                base.EndMatch();
            }   
        }
        #endregion

        #region Survival
        private class Survival : Arena.EventManager
        {
            private List<Arena.DeadPlayer> deadPlayers = new List<Arena.DeadPlayer>();
            private Arena.EventPlayer winner;

            public override void Prestart()
            {
                winner = null;
                deadPlayers.Clear();
                base.Prestart();
            }

            public override bool CanJoinEvent()
            {
                if (status == Arena.EventStatus.Started)
                    return false;
                return base.CanJoinEvent();
            }

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team = Arena.Team.None)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        break;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        break;
                    case Arena.EventStatus.Started:
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                deadPlayers.Add(new Arena.DeadPlayer(eventPlayer, eventPlayer.kills, eventPlayer.deaths));

                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("survleftevent", new string[] { eventPlayer.player.displayName, (eventPlayers.Count - 1).ToString() });
                base.LeaveEvent(eventPlayer);
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.isDead = true;
                victim.isEliminated = true;
                victim.AddDeath();

                if (attacker != null)
                {
                    Arena.EventPlayer attackerPlayer = Arena.GetUser(attacker);

                    if (attackerPlayer != null && victim != attackerPlayer)                    
                        attackerPlayer.AddKill();
                }  

                base.OnPlayerDeath(victim, attacker, info);

                if (!deathKick)
                {
                    deadPlayers.Add(new Arena.DeadPlayer(victim, victim.kills, victim.deaths));

                    foreach (Arena.EventPlayer spectator in eventPlayers.Where(x => x.player.IsSpectating()))
                        spectator.UpdateSpectateTarget();

                    BroadcastToPlayer(victim, Arena.msg("youareout", victim.player.userID));
                    victim.BeginSpectating();
                }
                else LeaveEvent(victim);

                UpdateScoreboard();

                bool allDead = true;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                {
                    if (!eventPlayer.isEliminated)
                    {
                        allDead = false;
                        break;
                    }
                }

                if (allDead || eventPlayers.Count <= 1)                
                    EndMatch();   
                
                if (CountAlive() == 1)
                {
                    Arena.EventPlayer[] winners = eventPlayers.Where(x => !x.isDead && !x.player.IsSpectating()).ToArray();
                    if (winners.Length == 1)
                        winner = winners[0];
                    EndMatch();
                }
            }

            public override void EndMatch()
            {
                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                if (winner == null)
                {
                    if (eventPlayers.Count > 0)
                    {
                        int kills = 0;
                        int deaths = 0;

                        foreach (Arena.EventPlayer eventPlayer in eventPlayers.Where(x => !x.isDead))
                        {
                            if (eventPlayer.kills > kills)
                            {
                                winner = eventPlayer;
                                kills = eventPlayer.kills;
                                deaths = eventPlayer.deaths;
                            }
                            else if (eventPlayer.kills == kills)
                            {
                                if (eventPlayer.deaths < deaths)
                                {
                                    winner = eventPlayer;
                                    kills = eventPlayer.kills;
                                    deaths = eventPlayer.deaths;
                                }
                            }
                        }
                    }
                }

                if (winner != null)
                {
                    Statistics.OnGamePlayed(winner.player.userID, config);
                    Statistics.OnEventWin(new List<ulong> { winner.player.userID }, config);                    
                    BroadcastToPlayer(winner, Arena.msg("youarewinner", winner.player.userID));

                    BroadcastWinners("globalwin", new string[] { winner.player.displayName, config.eventName });
                }

                List<ulong> losers = deadPlayers.Select(x => x.playerId).ToList();
                losers.AddRange(eventPlayers.Where(x => x.isDead).Select(x => x.player.userID));

                foreach (ulong loser in losers)
                    Statistics.OnGamePlayed(loser, config);
                                
                Statistics.OnEventLose(losers, config);

                base.EndMatch();
            }

            public override List<Arena.ScoreEntry> GetGameScores(Arena.Team team = Arena.Team.A)
            {
                List<Arena.ScoreEntry> gameScores = new List<Arena.ScoreEntry>();
                ulong[] deadPlayerIds = deadPlayers.Select(x => x.playerId).ToArray();

                int i = 1;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.OrderByDescending(x => x.kills))
                {
                    if (deadPlayerIds.Contains(eventPlayer.player.userID))
                        continue;

                    gameScores.Add(new Arena.ScoreEntry(eventPlayer, i, eventPlayer.kills, eventPlayer.deaths));
                    i++;
                }
                List<Arena.DeadPlayer> reversedDead = new List<Arena.DeadPlayer>(deadPlayers);
                reversedDead.Reverse();
                foreach (Arena.DeadPlayer deadPlayer in reversedDead)
                {
                    if (gameScores.Find(x => x.displayName == deadPlayer.playerName) != null)
                        continue;
                    gameScores.Add(new Arena.ScoreEntry() { position = i, value2 = deadPlayer.value2, displayName = $"{Arena.msg("deadplayer")} {deadPlayer.playerName}", value1 = deadPlayer.value1 });
                    i++;
                }
                return gameScores;
            }

            public override string GetScoreString() => Arena.msg("Survivors");

        }
        #endregion

        #region Team Survival
        private class TeamSurvival : Arena.EventManager
        {
            private List<Arena.DeadPlayer> deadPlayers = new List<Arena.DeadPlayer>();
            private Arena.Team winners;

            public override void Prestart()
            {
                winners = Arena.Team.None;
                deadPlayers.Clear();
                base.Prestart();
            }
            public override void StartMatch()
            {
                BalanceTeams();
                base.StartMatch();
            }
            public override bool CanJoinEvent()
            {
                if (status == Arena.EventStatus.Started)
                    return false;
                return base.CanJoinEvent();
            }
            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team = Arena.Team.None)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();
                Arena.LockInventory(eventPlayer.player);

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        return;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        return;
                    case Arena.EventStatus.Started:
                        SpawnPlayer(eventPlayer, true, true);
                        return;
                }
            }
            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                deadPlayers.Add(new Arena.DeadPlayer(eventPlayer, eventPlayer.kills, eventPlayer.deaths));

                Arena.UnlockInventory(eventPlayer.player);
                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("leftevent", new string[] { eventPlayer.player.displayName });
                base.LeaveEvent(eventPlayer);

                if (ins.configData.BalanceOnLeave)
                    BalanceTeams();
            }
            public override void OnPlayerTakeDamage(Arena.EventPlayer eventPlayer, HitInfo info)
            {
                BasePlayer attacker = info.InitiatorPlayer;
                if (attacker != null)
                {
                    Arena.EventPlayer eventAttacker = Arena.GetUser(attacker);
                    if (eventAttacker != null)
                    {
                        if (eventAttacker.team == eventPlayer.team)
                        {
                            BroadcastToPlayer(eventAttacker, Arena.msg("friendlyFire", attacker.userID));
                            if (!config.ffEnabled)
                            {
                                Arena.NullifyDamage(info);
                                return;
                            }
                        }
                    }
                }
                base.OnPlayerTakeDamage(eventPlayer, info);
            }
            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.isDead = true;
                victim.isEliminated = true;
                victim.AddDeath();

                if (attacker != null)
                {
                    Arena.EventPlayer attackerPlayer = Arena.GetUser(attacker);

                    if (attackerPlayer != null && victim != attackerPlayer)                    
                        attackerPlayer.AddKill();
                }                
                
                base.OnPlayerDeath(victim, attacker);

                if (!deathKick)
                {
                    deadPlayers.Add(new Arena.DeadPlayer(victim, victim.kills, victim.deaths));

                    foreach (Arena.EventPlayer spectator in eventPlayers.Where(x => x.player.IsSpectating()))
                        spectator.UpdateSpectateTarget();

                    BroadcastToPlayer(victim, Arena.msg("youareout", victim.player.userID));
                    victim.BeginSpectating();
                }
                else LeaveEvent(victim);

                UpdateScoreboard();

                int teamACount = GetTeamAlive(Arena.Team.A);
                int teamBCount = GetTeamAlive(Arena.Team.B);

                if (teamACount == 0)
                    winners = Arena.Team.A;

                if (teamBCount == 0)
                    winners = Arena.Team.B;

                if (winners != Arena.Team.None)
                {
                    BroadcastWinners("globalteamwin", new string[] { winners == Arena.Team.A ? (string.IsNullOrEmpty(config.teamA.name) ? Arena.msg("Team A") : config.teamA.name) : (string.IsNullOrEmpty(config.teamB.name) ? Arena.msg("Team B") : config.teamB.name), eventPlayers.Where(x => x.team == winners).Select(y => y.player.displayName).ToSentence(), config.eventName });

                    BroadcastToPlayers("teamwin", new string[] { winners == Arena.Team.A ? (string.IsNullOrEmpty(config.teamA.name) ? Arena.msg("Team A") : config.teamA.name) : (string.IsNullOrEmpty(config.teamB.name) ? Arena.msg("Team B") : config.teamB.name) });

                    InvokeHandler.Invoke(this, EndMatch, 2f);
                    return;
                }

                bool allDead = true;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                {
                    if (!eventPlayer.isEliminated)
                    {
                        allDead = false;
                        break;
                    }
                }

                if (allDead || eventPlayers.Count <= 1)
                    EndMatch();
            }
            public override void GiveTeamGear(Arena.EventPlayer eventPlayer)
            {
                string kitName = eventPlayer.team == Arena.Team.A ? config.teamA.kit : config.teamB.kit;
                Arena.GiveKit(eventPlayer.player, kitName);
            }

            public override void EndMatch()
            {
                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                int teamACount = GetTeamCount(Arena.Team.A);
                int teamBCount = GetTeamCount(Arena.Team.B);
                winners = teamACount > teamBCount ? Arena.Team.A : teamBCount > teamACount ? Arena.Team.B : Arena.Team.None;

                List<ulong> winnerPlayers = eventPlayers.Where(x => x.team == winners).Select(y => y.player.userID).ToList();
                Statistics.OnEventWin(winnerPlayers, config);

                List<ulong> loserPlayers = eventPlayers.Where(x => x.team != winners).Select(y => y.player.userID).ToList();
                loserPlayers.AddRange(deadPlayers.Where(x => x.team != winners).Select(x => x.playerId));                
                Statistics.OnEventLose(loserPlayers, config);

                foreach (ulong winner in winnerPlayers)
                    Statistics.OnGamePlayed(winner, config);

                foreach (ulong loser in loserPlayers)
                    Statistics.OnGamePlayed(loser, config);

                base.EndMatch();
            }
            
            public override List<Arena.ScoreEntry> GetGameScores(Arena.Team team = Arena.Team.A)
            {
                List<Arena.ScoreEntry> gameScores = new List<Arena.ScoreEntry>();
                ulong[] deadPlayerIds = deadPlayers.Select(x => x.playerId).ToArray();

                int i = 1;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.Where(x => x.team == team).OrderByDescending(x => x.kills))
                {
                    if (deadPlayerIds.Contains(eventPlayer.player.userID))
                        continue;

                    gameScores.Add(new Arena.ScoreEntry(eventPlayer, i, eventPlayer.kills, eventPlayer.deaths));
                    i++;
                }
                List<Arena.DeadPlayer> reversedDead = new List<Arena.DeadPlayer>(deadPlayers.Where(x => x.team == team));
                reversedDead.Reverse();
                foreach (Arena.DeadPlayer deadPlayer in reversedDead)
                {
                    if (gameScores.Find(x => x.displayName == deadPlayer.playerName) != null)
                        continue;

                    gameScores.Add(new Arena.ScoreEntry() { position = i, value2 = deadPlayer.value2, displayName = $"{Arena.msg("deadplayer")} {deadPlayer.playerName}", value1 = deadPlayer.value1 });
                    i++;
                }
                return gameScores;
            }

            public override string GetScoreString() => Arena.msg("Survivors");

            public override int GetTeamScore(Arena.Team team)
            {
                return team == Arena.Team.A ? GetTeamCount(Arena.Team.A) : GetTeamCount(Arena.Team.B);
            }            
        }
        #endregion

        #region One in the Chamber
        private class OITC : Arena.EventManager
        {
            private int playerLives;
            private string eventKit;
            private string primaryWeapon;
            private string secondaryWeapon;

            private Arena.EventPlayer winner;
            private List<Arena.DeadPlayer> deadPlayers = new List<Arena.DeadPlayer>();

            public override void InitializeEvent(string eventName, Arena.ArenaData.EventConfig config)
            {
                playerLives = Convert.ToInt32(config.additionalParameters["Player Lives"]);
                eventKit = config.additionalParameters["Player Gear"] as string;
                primaryWeapon = config.additionalParameters["Primary Weapon"] as string;
                secondaryWeapon = config.additionalParameters["Secondary Weapon"] as string;
                base.InitializeEvent(eventName, config);
            }

            public override void Prestart()
            {
                winner = null;
                deadPlayers.Clear();
                base.Prestart();
            }

            public override bool CanDropAmmo()
            {
                return false;
            }

            public override bool CanDropWeapons()
            {
                return false;
            }

            public override bool CanDropBackpack()
            {
                return false;
            }

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);

                Arena.LockInventory(eventPlayer.player);

                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        return;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count == config.minimumPlayers)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        return;
                    case Arena.EventStatus.Started:
                        SpawnPlayer(eventPlayer, true, true);
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                deadPlayers.Add(new Arena.DeadPlayer(eventPlayer, eventPlayer.kills, eventPlayer.deaths));

                Arena.UnlockInventory(eventPlayer.player);
                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("leftevent", new string[] { eventPlayer.player.displayName });
                base.LeaveEvent(eventPlayer);
            }

            public override object OverridePlayerKit(Arena.EventPlayer eventPlayer)
            {
                Arena.GiveKit(eventPlayer.player, eventKit);
                CreateWeapon(eventPlayer, primaryWeapon);
                CreateWeapon(eventPlayer, secondaryWeapon);
                return true;
            }

            public override bool CanJoinEvent()
            {
                if (status == Arena.EventStatus.Started)
                    return false;
                return base.CanJoinEvent();
            }

            public override float GetDamageModifier(ulong attackerId)
            {
                if (attackerId != 0U)
                    return 100f;
                return 1f;
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.isDead = true;
                victim.AddDeath();

                if (attacker != null)
                {
                    Arena.EventPlayer attackerPlayer = Arena.GetUser(attacker);

                    if (attackerPlayer != null && victim != attackerPlayer)
                    {
                        attackerPlayer.AddKill();

                        Item item = null;
                        for (int i = 0; i < attacker.inventory.containerBelt.itemList.Count; i++)
                        {
                            Item it = attacker.inventory.containerBelt.itemList[i];
                            if (it.info.shortname == primaryWeapon)
                            {
                                item = it;
                                break;
                            }
                        }

                        if (item != null)
                        {
                            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                            if (weapon != null)
                                weapon.primaryMagazine.contents = weapon.primaryMagazine.contents + 1;
                            weapon.SendNetworkUpdate();
                        }
                    }
                }

                base.OnPlayerDeath(victim, attacker, info);
                UpdateScoreboard();

                if (victim.deaths >= playerLives)
                {
                    victim.isEliminated = true;
                    if (!deathKick)
                    {
                        deadPlayers.Add(new Arena.DeadPlayer(victim, victim.kills, victim.deaths));

                        foreach (Arena.EventPlayer spectator in eventPlayers)
                        {
                            if (spectator.player.IsSpectating())
                                spectator.UpdateSpectateTarget();
                        }

                        BroadcastToPlayer(victim, Arena.msg("youareout", victim.player.userID));
                        victim.BeginSpectating();
                    }
                    else LeaveEvent(victim);
                }

                bool allDead = true;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                {
                    if (!eventPlayer.isEliminated)
                    {
                        allDead = false;
                        break;
                    }
                }

                if (allDead)
                    EndMatch();

                if (CountStillPlaying() == 1)
                {
                    Arena.EventPlayer[] winners = eventPlayers.Where(x => !x.isEliminated).ToArray();
                    if (winners.Length == 1)
                        winner = winners[0];
                    EndMatch();
                }

                if (eventPlayers.Count <= 1)
                {
                    winner = eventPlayers[0] ?? null;
                    InvokeHandler.Invoke(this, EndMatch, 4f);
                }
            }

            public override void EndMatch()
            {
                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                if (winner == null)
                {
                    if (eventPlayers.Count > 0)
                    {
                        int kills = 0;
                        int deaths = 0;
                        
                        foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                        {
                            if (eventPlayer.isDead)
                                continue;

                            if (eventPlayer.kills > kills)
                            {
                                winner = eventPlayer;
                                kills = eventPlayer.kills;
                                deaths = eventPlayer.deaths;
                            }
                            else if (eventPlayer.kills == kills)
                            {
                                if (eventPlayer.deaths < deaths)
                                {
                                    winner = eventPlayer;
                                    kills = eventPlayer.kills;
                                    deaths = eventPlayer.deaths;
                                }
                            }
                        }
                    }
                }

                if (winner != null)
                {
                    Statistics.OnGamePlayed(winner.player.userID, config);
                    Statistics.OnEventWin(new List<ulong> { winner.player.userID }, config);
                    BroadcastToPlayer(winner, Arena.msg("youarewinner", winner.player.userID));

                    BroadcastWinners("globalwin", new string[] { winner.player.displayName, config.eventName });
                }

                List<ulong> losers = deadPlayers.Select(x => x.playerId).ToList();
                losers.AddRange(eventPlayers.Where(x => x.isDead).Select(x => x.player.userID));

                foreach (ulong loser in losers)
                    Statistics.OnGamePlayed(loser, config);

                Statistics.OnEventLose(losers, config);

                base.EndMatch();
            }
            public override List<Arena.ScoreEntry> GetGameScores(Arena.Team team = Arena.Team.A)
            {
                List<Arena.ScoreEntry> gameScores = new List<Arena.ScoreEntry>();
                ulong[] deadPlayerIds = deadPlayers.Select(x => x.playerId).ToArray();

                int i = 1;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.OrderByDescending(x => x.kills))
                {
                    if (deadPlayerIds.Contains(eventPlayer.player.userID))
                        continue;

                    gameScores.Add(new Arena.ScoreEntry(eventPlayer, i, eventPlayer.kills, eventPlayer.deaths));
                    i++;
                }
                List<Arena.DeadPlayer> reversedDead = new List<Arena.DeadPlayer>(deadPlayers);
                reversedDead.Reverse();
                foreach (Arena.DeadPlayer deadPlayer in reversedDead)
                {
                    if (gameScores.Find(x => x.displayName == deadPlayer.playerName) != null)
                        continue;

                    gameScores.Add(new Arena.ScoreEntry() { position = i, value2 = deadPlayer.value2, displayName = $"{Arena.msg("deadplayer")} {deadPlayer.playerName}", value1 = deadPlayer.value1 });
                    i++;
                }
                return gameScores;
            }

            public override string GetScoreString() => Arena.msg("Survivors");

            private void CreateWeapon(Arena.EventPlayer eventPlayer, string shortname)
            {
                Item item = ItemManager.Create(ItemManager.itemDictionaryByName[shortname]);

                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                    weapon.primaryMagazine.contents = 1;

                item.MoveToContainer(eventPlayer.player.inventory.containerBelt);
            }
        }
        #endregion

        #region Gungame
        private class GunGame : Arena.EventManager
        {
            public int rankLimit;
            private bool useDowngrade;
            private string eventKit;
            private Arena.EventWeapon downgradeWeapon;
            public List<Arena.EventWeapon> eventWeapons = new List<Arena.EventWeapon>();

            public override void InitializeEvent(string eventName, Arena.ArenaData.EventConfig config)
            {
                if (!ins.storedData.gungameSets.ContainsKey(config.additionalParameters["Weapon Set"].ToString()))
                {
                    print($"[ERROR] The weapon set titled \"{config.additionalParameters["Weapon Set"].ToString()}\" does not exist. Unable to start event (GunGame) {config.eventName}"); 
                    Destroy(this);
                    return;
                }
                useDowngrade = Convert.ToBoolean(config.additionalParameters["Use Downgrade Weapon"]); 
                eventWeapons = ins.storedData.gungameSets[config.additionalParameters["Weapon Set"].ToString()]; 
                eventKit = config.additionalParameters["Player Gear"].ToString(); 
                rankLimit = eventWeapons.Count; 
                downgradeWeapon = eventWeapons[0]; 

                base.InitializeEvent(eventName, config);
            }

            public override void Prestart()
            {
                base.Prestart();
            }

            public override void StartMatch()
            {
                DowngradeInfo();
                base.StartMatch();

                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                    eventPlayer.specialInt1 = 1;
                UpdateScoreboard();
            }
            
            public override bool CanDropWeapons()
            {
                return false;
            }

            public override bool CanDropBackpack()
            {
                return false;
            }

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();
                eventPlayer.specialInt1 = 1;

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);                

                Arena.LockInventory(eventPlayer.player);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        return;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        return;
                    case Arena.EventStatus.Started:
                        SpawnPlayer(eventPlayer, true, true);
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                Arena.UnlockInventory(eventPlayer.player);
                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("leftevent", new string[] { eventPlayer.player.displayName });
                base.LeaveEvent(eventPlayer);
            }

            public override object OverridePlayerKit(Arena.EventPlayer eventPlayer)
            {               
                Arena.GiveKit(eventPlayer.player, eventKit);
                if (useDowngrade)
                    CreateWeapon(eventPlayer, downgradeWeapon);

                CreateWeapon(eventPlayer, eventWeapons[Mathf.Clamp(eventPlayer.specialInt1, 1, eventWeapons.Count - 1)]);
                return true;
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.AddDeath();

                if (attacker != null)
                {
                    Arena.EventPlayer attackerPlayer = Arena.GetUser(attacker);

                    if (attackerPlayer != null && victim != attackerPlayer)
                    {
                        attackerPlayer.AddKill();
                        if (info != null)
                        {
                            string weapon = GetWeapon(info);
                            if (eventWeapons[attackerPlayer.specialInt1].shortname == weapon)
                            {
                                List<Item> list = Facepunch.Pool.GetList<Item>();
                                attackerPlayer.player.inventory.AllItemsNoAlloc(ref list);
                                for (int i = 0; i < list.Count; i++)
                                {
                                    Item item = list[i];
                                    if (item.info.shortname == eventWeapons[attackerPlayer.specialInt1].shortname)
                                    {
                                        item.RemoveFromContainer();
                                        item.Remove();
                                    }
                                }
                                Facepunch.Pool.FreeList(ref list);

                                attackerPlayer.specialInt1 += 1;
                                if (attackerPlayer.specialInt1 >= rankLimit)
                                {
                                    BroadcastWinners("globalwin", new string[] { attackerPlayer.player.displayName, config.eventName });
                                    EndMatch();
                                    return;
                                }
                                else ins.NextTick(()=> CreateWeapon(attackerPlayer, eventWeapons[attackerPlayer.specialInt1]));                                
                            }
                            else if (useDowngrade && weapon == downgradeWeapon.shortname)
                                victim.specialInt1 = Mathf.Clamp(victim.specialInt1 - 1, 1, rankLimit);

                            if (showKillFeed)
                                BroadcastToPlayers("killedplayergg", new string[] { attackerPlayer.player.displayName, attackerPlayer.specialInt1.ToString(), victim.player.displayName, victim.specialInt1.ToString() });
                        }
                    }                    
                }
                
                UpdateScoreboard();
                base.OnPlayerDeath(victim, attacker, info);
            }

            public override void EndMatch()
            {
                CancelInvoke();

                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                List<ulong> playerList = eventPlayers.OrderByDescending(x => x.specialInt1).Select(x => x.player.userID).ToList();

                if (playerList.Count > 0)
                {
                    Statistics.OnEventWin(new List<ulong>() { playerList[0] }, config);
                    Statistics.OnEventLose(playerList.GetRange(1, playerList.Count - 1), config);
                }

                foreach (ulong playerId in playerList)
                    Statistics.OnGamePlayed(playerId, config);

                base.EndMatch();
            }
        
            public override List<Arena.ScoreEntry> GetGameScores(Arena.Team team = Arena.Team.A)
            {
                List<Arena.ScoreEntry> gameScores = new List<Arena.ScoreEntry>();
                int i = 1;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.OrderByDescending(x => x.specialInt1))
                {
                    gameScores.Add(new Arena.ScoreEntry(eventPlayer, i, $"(R. {eventPlayer.specialInt1})", eventPlayer.kills, eventPlayer.deaths));
                    i++;
                }
                return gameScores;
            }

            public override string GetScoreString() => $"({Arena.msg("Rank Limit")}: {rankLimit - 1})";

            internal string GetWeapon(HitInfo hitInfo, string def = "")
            {
                Item item = hitInfo.Weapon?.GetItem();
                if (item == null && hitInfo.WeaponPrefab == null) return def;
                string shortname = item?.info.shortname ?? hitInfo.WeaponPrefab.name;
                if (shortname == "survey.charge" || shortname == "survey_charge.deployed") return "surveycharge";
                shortname = shortname.Replace(".prefab", string.Empty);
                shortname = shortname.Replace(".deployed", string.Empty);
                shortname = shortname.Replace(".entity", "");
                shortname = shortname.Replace("_", ".");
                switch (shortname)
                {
                    case "rocket.basic":
                    case "rocket.fire":
                    case "rocket.hv":
                    case "rocket.smoke":
                        shortname = "rocket.launcher";
                        break;                   
                    case "40mm.grenade.he":                   
                        shortname = "multiplegrenadelauncher";
                        break;
                }
                return shortname;
            }

            public void CreateWeapon(Arena.EventPlayer eventPlayer, Arena.EventWeapon eventWeapon)
            {
                ItemDefinition def = ItemManager.itemDictionaryByName[eventWeapon.shortname];
                if (def.stackable == 1 && eventWeapon.amount > 1)
                {
                    for (int i = 0; i < eventWeapon.amount; i++)
                    {
                        Item item = ItemManager.Create(def, 1, eventWeapon.skinId);
                        eventPlayer.player.GiveItem(item, BaseEntity.GiveItemReason.Generic);
                    }
                }
                else
                {
                    Item item = ItemManager.Create(def, eventWeapon.amount < 1 ? 1 : eventWeapon.amount, eventWeapon.skinId);
                    if (eventWeapon.shortname == "flamethrower" && eventWeapon.ammoAmount > 0)
                    {
                        Item ammo = ItemManager.CreateByName("lowgradefuel", eventWeapon.ammoAmount);
                        ammo.MoveToContainer(eventPlayer.player.inventory.containerMain);
                    }

                    BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        if (!string.IsNullOrEmpty(eventWeapon.ammoType))
                        {
                            ItemDefinition ammoType = ItemManager.itemDictionaryByName[eventWeapon.ammoType];
                            if (ammoType != null)
                                weapon.primaryMagazine.ammoType = ammoType;
                        }
                        if (eventWeapon.ammoAmount >= weapon.primaryMagazine.capacity)
                        {
                            weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
                            int ammoAmount = eventWeapon.ammoAmount - weapon.primaryMagazine.capacity;
                            Item ammo = ItemManager.Create(ItemManager.itemDictionaryByName[eventWeapon.ammoType], ammoAmount < 1 ? 1 : ammoAmount);
                            ammo.MoveToContainer(eventPlayer.player.inventory.containerMain);
                        }
                        else weapon.primaryMagazine.contents = eventWeapon.ammoAmount;
                    }
                    if (eventWeapon.attachments != null)
                    {
                        foreach (string attachment in eventWeapon.attachments)
                            item.contents.AddItem(ItemManager.itemDictionaryByName[attachment], 1);
                    }
                    eventPlayer.player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                }
            }

            private void DowngradeInfo()
            {
                BroadcastToPlayers("downgradeEnabled", new string[] { ItemManager.itemDictionaryByName[downgradeWeapon.shortname].displayName.english });
                Invoke("DowngradeInfo", 90);
            }            
        }
        #endregion

        #region Capture the Flag
        private class CTF : Arena.EventManager
        {
            public int teamACaps;
            public int teamBCaps;

            private CTFFlag ctfFlagA;
            private CTFFlag ctfFlagB;
                        
            public override void InitializeEvent(string eventName, Arena.ArenaData.EventConfig config)
            {               
                string teamColor = config.teamA.color;
                if (string.IsNullOrEmpty(teamColor))
                    teamColor = "FF0800";

                if (teamColor.StartsWith("#"))
                    teamColor = teamColor.Substring(1);
                
                Arena.UI.AddImage("flagA", $"https://dummyimage.com/256x256/{teamColor}/{teamColor}.png/&text=TeamA");// $"http://placeholdit.imgix.net/~text?bg={teamColor}&txtclr={teamColor}&txtsize=0&txt=CTF&w=256&h=256");

                teamColor = config.teamB.color;
                if (string.IsNullOrEmpty(teamColor))
                    teamColor = "0037B1";

                if (teamColor.StartsWith("#"))
                    teamColor = teamColor.Substring(1);

                Arena.UI.AddImage("flagB", $"https://dummyimage.com/256x256/{teamColor}/{teamColor}.png/&text=TeamA");//$"http://placeholdit.imgix.net/~text?bg={teamColor}&txtclr={teamColor}&txtsize=0&txt=CTF&w=256&h=256");

                base.InitializeEvent(eventName, config);
                    
                spawnManagerA.SetReserved();
                spawnManagerB.SetReserved();
            }

            public override void OnDestroy()
            {
                DestroyFlags();
                base.OnDestroy();
            }

            public override void Prestart()
            {
                teamACaps = 0;
                teamBCaps = 0;

                if (ctfFlagA == null)
                    CreateFlag(Arena.Team.A);
                if (ctfFlagB == null)
                    CreateFlag(Arena.Team.B);

                base.Prestart();
            }

            public override void StartMatch()
            {
                BalanceTeams();
                base.StartMatch();
            }

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team = Arena.Team.None)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();
                Arena.LockInventory(eventPlayer.player);

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        return;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        return;
                    case Arena.EventStatus.Started:
                        SpawnPlayer(eventPlayer, true, true);
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                if (eventPlayer.specialBool1)
                {
                    if (eventPlayer.team == Arena.Team.A)
                        ctfFlagB.DropFlag();
                    else ctfFlagA.DropFlag();

                    eventPlayer.specialBool1 = false;
                    eventPlayer.isDead = true;
                }

                Arena.UnlockInventory(eventPlayer.player);
                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("leftevent", new string[] { eventPlayer.player.displayName });
                base.LeaveEvent(eventPlayer);

                if (ins.configData.BalanceOnLeave)
                    BalanceTeams();
            }

            public override void OnPlayerTakeDamage(Arena.EventPlayer eventPlayer, HitInfo info)
            {
                BasePlayer attacker = info.InitiatorPlayer;
                if (attacker != null)
                {
                    Arena.EventPlayer eventAttacker = Arena.GetUser(attacker);
                    if (eventAttacker != null)
                    {
                        if (eventAttacker.team == eventPlayer.team)
                        {
                            BroadcastToPlayer(eventAttacker, Arena.msg("friendlyFire", attacker.userID));
                            if (!config.ffEnabled)
                            {
                                Arena.NullifyDamage(info);
                                return;
                            }
                        }
                    }
                }
                base.OnPlayerTakeDamage(eventPlayer, info);
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.AddDeath();

                if (victim.specialBool1)
                {
                    if (victim.team == Arena.Team.A)
                        ctfFlagB.DropFlag();
                    else ctfFlagA.DropFlag();

                    victim.specialBool1 = false;
                }

                if (attacker != null)
                {
                    Arena.EventPlayer attackerPlayer = Arena.GetUser(attacker);

                    if (attackerPlayer != null && victim != attackerPlayer)
                        attackerPlayer.AddKill();
                }
                
                UpdateScoreboard();
                base.OnPlayerDeath(victim, attacker);
            }

            public override void GiveTeamGear(Arena.EventPlayer eventPlayer)
            {
                string kitName = eventPlayer.team == Arena.Team.A ? config.teamA.kit : config.teamB.kit;
                Arena.GiveKit(eventPlayer.player, kitName);
            }

            public override void EndMatch()
            {
                ctfFlagA?.ResetFlag();
                ctfFlagB?.ResetFlag();
                DestroyFlags();

                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                Arena.Team team = teamACaps > teamBCaps ? Arena.Team.A : teamBCaps > teamACaps ? Arena.Team.B : Arena.Team.None;

                List<ulong> winners = eventPlayers.Where(x => x.team == team).Select(y => y.player.userID).ToList();
                List<ulong> losers = eventPlayers.Where(x => x.team != team).Select(y => y.player.userID).ToList();

                Statistics.OnEventWin(winners, config);
                Statistics.OnEventLose(losers, config);

                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                    Statistics.OnGamePlayed(eventPlayer.player.userID, config);

                BroadcastWinners("globalteamwin", new string[] { team == Arena.Team.A ? (string.IsNullOrEmpty(config.teamA.name) ? Arena.msg("Team A") : config.teamA.name) : (string.IsNullOrEmpty(config.teamB.name) ? Arena.msg("Team B") : config.teamB.name), eventPlayers.Where(x => x.team == team).Select(y => y.player.displayName).ToSentence(), config.eventName });

                base.EndMatch();
            }
                       
            public override int GetTeamScore(Arena.Team team)
            {
                return team == Arena.Team.A ? teamACaps : teamBCaps;
            }

            public override string GetScoreString() => $"{Arena.msg("Capture Limit")}: {config.killLimit}";
            
            public override string[] GetScoreType() => new string[] { "K", "C" };

            public override string GetScoreName(bool first) => first ? Arena.msg("kills") : Arena.msg("captures");

            public override List<Arena.ScoreEntry> GetGameScores(Arena.Team team = Arena.Team.A)
            {
                List<Arena.ScoreEntry> gameScores = new List<Arena.ScoreEntry>();
                int i = 1;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.Where(x => x.team == team).OrderByDescending(x => x.specialInt1))
                {
                    gameScores.Add(new Arena.ScoreEntry(eventPlayer, i, eventPlayer.kills, eventPlayer.specialInt1));
                    i++;
                }               
                return gameScores;
            }

            private void CreateFlag(Arena.Team team)
            {
                Signage signage = (Signage)GameManager.server.CreateEntity(FLAG_PREFAB, team == Arena.Team.A ? spawnManagerA.GetSpawnPoint(0) : spawnManagerB.GetSpawnPoint(0));
                signage.enableSaving = false;
                signage.Spawn();

                Destroy(signage.GetComponent<DestroyOnGroundMissing>());
                Destroy(signage.GetComponent<GroundWatch>());

                string image = Arena.UI.GetImage(team == Arena.Team.A ? "flagA" : "flagB");
                if (!string.IsNullOrEmpty(image))
                    signage.textureID = uint.Parse(image);

                signage.SetFlag(BaseEntity.Flags.Locked, true);
                signage.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                CTFFlag ctfFlag = signage.gameObject.AddComponent<CTFFlag>();
                ctfFlag.SetEvent(this, team);

                if (team == Arena.Team.A)
                    ctfFlagA = ctfFlag;
                else ctfFlagB = ctfFlag;
            }

            private void DestroyFlags()
            {
                if (ctfFlagA != null)
                {
                    if (ctfFlagA.flagHolder != null)
                        ctfFlagA.DropFlag();

                    Destroy(ctfFlagA);
                }

                if (ctfFlagB != null)
                {
                    if (ctfFlagB.flagHolder != null)
                        ctfFlagB.DropFlag();

                    Destroy(ctfFlagB);
                }                
            }

            public class CTFFlag : MonoBehaviour
            {
                public BaseEntity entity;
                public Signage signage;
                public Arena.Team team;

                public CTF eventManager;
                public Arena.EventPlayer flagHolder;

                public Vector3 basePosition;
                public bool isAtBase;

                public string teamName;
                public int respawnTimer;

                private ConfigData.CTFConfig configData;
                private Color color;

                private void Awake()
                {
                    entity = GetComponent<BaseEntity>();

                    entity.gameObject.layer = (int)Rust.Layer.Reserved1;

                    SphereCollider collider = entity.gameObject.AddComponent<SphereCollider>();
                    collider.transform.position = entity.transform.position + (Vector3.up / 2);
                    collider.isTrigger = true;
                    collider.radius = 1f;
                                        
                    basePosition = entity.transform.position;
                    isAtBase = true;

                    configData = ins.configData.CTF;                    
                }
                
                private void OnDestroy()
                {
                    if (InvokeHandler.IsInvoking(this, DrawFlagPosition))
                        InvokeHandler.CancelInvoke(this, DrawFlagPosition);

                    if (signage != null && !signage.IsDestroyed)
                    {
                        signage.SetParent(null);
                        signage.Kill();
                    }

                    if (entity != null && !entity.IsDestroyed)
                    {
                        entity.SetParent(null);
                        entity.Kill();
                    }
                }

                private void OnTriggerEnter(Collider col)
                {
                    Arena.EventPlayer eventPlayer = col.gameObject.GetComponent<Arena.EventPlayer>();
                    if (eventPlayer != null)
                    {
                        if (eventPlayer.isDead || eventPlayer == flagHolder || !eventManager.eventPlayers.Contains(eventPlayer) || eventManager.status != Arena.EventStatus.Started)
                            return;
                       
                        if (isAtBase)
                        {
                            if (eventPlayer.team != team)
                            {
                                PickupFlag(eventPlayer);
                                eventManager.BroadcastToPlayers("flagpickup", new string[] { eventPlayer.player.displayName, teamName });
                            }
                            else
                            {
                                if (eventPlayer.specialBool1)
                                {
                                    CTFFlag enemyFlag = team == Arena.Team.A ? eventManager.ctfFlagB : eventManager.ctfFlagA;
                                    enemyFlag.CaptureFlag(eventPlayer);                                    
                                }
                            }
                        }
                        else
                        {
                            if (flagHolder == null)
                            {
                                if (eventPlayer.team != team)
                                {
                                    PickupFlag(eventPlayer);
                                    eventManager.BroadcastToPlayers("flagpickup", new string[] { eventPlayer.player.displayName, teamName });
                                }
                                else
                                {
                                    ResetFlag();
                                    eventManager.BroadcastToPlayers("flagreturned", new string[] { eventPlayer.player.displayName, teamName });
                                }
                            }                           
                        }                        
                    }
                }

                public void SetEvent(CTF eventManager, Arena.Team team)
                {
                    this.eventManager = eventManager;
                    this.team = team;

                    respawnTimer = ins.configData.CTF.FlagRespawn;

                    if (team == Arena.Team.A)
                    {
                        if (string.IsNullOrEmpty(eventManager.config.teamA.name))
                            teamName = Arena.msg("Team A");
                        else teamName = eventManager.config.teamA.name;
                    }
                    else 
                    {
                        if (string.IsNullOrEmpty(eventManager.config.teamB.name))
                            teamName = Arena.msg("Team B");
                        else teamName = eventManager.config.teamB.name;
                    }

                    signage = (Signage)GameManager.server.CreateEntity(FLAG_PREFAB, entity.transform.position);
                    signage.enableSaving = false;
                    signage.Spawn();

                    Destroy(signage.GetComponent<DestroyOnGroundMissing>());
                    Destroy(signage.GetComponent<GroundWatch>());

                    signage.textureID = uint.Parse(Arena.UI.GetImage(team == Arena.Team.A ? "flagA" : "flagB"));
                    signage.SetFlag(BaseEntity.Flags.Locked, true);

                    signage.SetParent(entity, 0);
                    signage.transform.localPosition = new Vector3();
                    signage.transform.localEulerAngles = new Vector3(0, 180, 0);

                    signage.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                    DrawColor();

                    if (configData.UseDDraw)
                        InvokeHandler.InvokeRepeating(this, DrawFlagPosition, configData.DDrawRate, configData.DDrawRate);
                }

                private void DrawColor()
                {
                    string teamColor = team == Arena.Team.A ? eventManager.config.teamA.color : eventManager.config.teamB.color;

                    if (string.IsNullOrEmpty(teamColor))
                        color = team == Arena.Team.A ? Color.red : Color.blue;
                    else
                    {
                        if (teamColor.StartsWith("#"))
                            teamColor = teamColor.Substring(1);
                        int red = int.Parse(teamColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                        int green = int.Parse(teamColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                        int blue = int.Parse(teamColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);

                        color = new Color((float)red / 255, (float)green / 255, (float)blue / 255);
                    }
                }

                private void DrawFlagPosition()
                {
                    if (entity == null || eventManager.status != Arena.EventStatus.Started)
                        return;

                    foreach (Arena.EventPlayer eventPlayer in eventManager.eventPlayers)
                    {
                        if (eventPlayer == null || eventPlayer.player == null)
                            continue;

                        bool tempAdmin = false;
                        if (!eventPlayer.player.IsAdmin)
                        {
                            tempAdmin = true;
                            eventPlayer.player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                            eventPlayer.player.SendNetworkUpdateImmediate();
                        }

                        eventPlayer.player.SendConsoleCommand("ddraw.text", configData.DDrawRate, color, entity.transform.position + new Vector3(0, 1.5f, 0), $"<size=40>{teamName} {Arena.msg("flag")}</size>");
                        eventPlayer.player.SendConsoleCommand("ddraw.box", configData.DDrawRate, color, entity.transform.position, 1f);

                        if (tempAdmin)
                        {
                            eventPlayer.player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                            eventPlayer.player.SendNetworkUpdateImmediate();
                        }
                    }
                }

                public void PickupFlag(Arena.EventPlayer eventPlayer)
                {
                    flagHolder = eventPlayer;
                    eventPlayer.specialBool1 = true;

                    isAtBase = false;
                    InvokeHandler.CancelInvoke(this, ResetFlag);

                    entity.SetParent(eventPlayer.player, 0);
                    entity.transform.localPosition = Vector3.up * 2;
                }

                public void DropFlag()
                {
                    InvokeHandler.Invoke(this, ResetFlag, respawnTimer);

                    entity?.SetParent(null, 0);

                    if (flagHolder != null)
                    {
                        entity.transform.position = flagHolder.transform.position;

                        flagHolder.specialBool1 = false;
                        flagHolder = null;
                    }

                    entity?.UpdateNetworkGroup();
                    entity?.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);                    
                }

                private void CaptureFlag(Arena.EventPlayer eventPlayer)
                {
                    if (eventPlayer.team == Arena.Team.A)
                        eventManager.teamACaps++;
                    else eventManager.teamBCaps++;

                    eventPlayer.specialInt1++;

                    eventManager.BroadcastToPlayers("flagcaptured", new string[] { eventPlayer.player.displayName, teamName });
                    ResetFlag();

                    eventManager.UpdateScoreboard();
                                        
                    if (eventManager.config.killLimit > 0 && (eventManager.teamACaps >= eventManager.config.killLimit || eventManager.teamBCaps >= eventManager.config.killLimit))                    
                        eventManager.EndMatch();                    
                }

                public void ResetFlag()
                {      
                    if (flagHolder != null)
                    {
                        flagHolder.specialBool1 = false;
                        flagHolder = null;
                    }

                    if (entity != null)
                    {
                        entity.SetParent(null, 0);
                        InvokeHandler.CancelInvoke(this, ResetFlag);
                        entity.transform.position = basePosition;
                        entity.UpdateNetworkGroup();
                        entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        signage.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    }
                    isAtBase = true;
                }
            }
        }
        #endregion

        #region NPC Survival
        public class NPCSurvival : Arena.EventManager
        {            
            private List<Arena.DeadPlayer> deadPlayers = new List<Arena.DeadPlayer>();
            private Arena.EventPlayer winner;

            private int roundNumber;
            private List<NPCController> npcPlayers = new List<NPCController>();
            private List<ulong> npcPlayerIds = new List<ulong>();

            private int maxRounds;
            private int playerLives;

            private int npcCount;
            private int npcAddition;

            private bool spawnScientists;
            private bool spawnMurderers;

            private string npcSpawnfile;
            private Arena.SpawnManager spawnManager;

            private ConfigData.NPCSConfig configData;

            public override void InitializeEvent(string eventName, Arena.ArenaData.EventConfig config)
            {
                playerLives = Convert.ToInt32(config.additionalParameters["Player Lives"]);
                maxRounds = Convert.ToInt32(config.additionalParameters["Rounds to Play"]);
                npcCount = Convert.ToInt32(config.additionalParameters["NPCs to Spawn"]);
                npcAddition = Convert.ToInt32(config.additionalParameters["Additional NPCs (per round)"]);
                npcSpawnfile = config.additionalParameters["NPC Spawnfile"] as string;
                spawnScientists = Convert.ToBoolean(config.additionalParameters["Use Scientists"]);
                spawnMurderers = Convert.ToBoolean(config.additionalParameters["Use Murderers"]);

                spawnManager = new Arena.SpawnManager();
                spawnManager.SetSpawnFile(config.eventName, npcSpawnfile);

                configData = ins.configData.NPCS;

                base.InitializeEvent(eventName, config);
            }
            public override void Prestart()
            {
                winner = null;
                deadPlayers.Clear();
                npcPlayers.Clear();
                npcPlayerIds.Clear();
                roundNumber = 0;
                base.Prestart();
            }

            public override void OnDestroy()
            {
                for (int i = 0; i < npcPlayers.Count; i++)                
                    Destroy(npcPlayers[i]);
                
                base.OnDestroy();
            }

            public override void StartMatch()
            {                
                base.StartMatch();
                NextRound();
            }

            public void NextRound()
            {
                roundNumber++;
                
                if (roundNumber > maxRounds)
                {
                    EndMatch();
                    return;
                }

                BroadcastToPlayers("nextround", new string[] { roundNumber.ToString() }, false);

                if (roundNumber > 1)
                {
                    foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                    {
                        if (configData.ResetHealth)
                            eventPlayer.player.health = eventPlayer.player.MaxHealth();

                        if (configData.ResetInv)
                        {
                            Arena.StripInventory(eventPlayer.player);
                            ins.NextTick(() =>
                            {
                                Arena.GiveKit(eventPlayer.player, currentKit);
                            });
                        }
                    }
                }
                InvokeHandler.Invoke(this, SpawnNPCs, 10f);
            }

            private void SpawnNPCs() => ServerMgr.Instance.StartCoroutine(SpawnLoop());

            private IEnumerator SpawnLoop()
            {
                int maxNPCs = npcCount + ((roundNumber - 1) * npcAddition);
                for (int i = 0; i < maxNPCs; i++)
                {
                    global::HumanNPC entity = entity = InstantiateEntity(SCIENTIST_PREFAB, spawnManager.GetSpawnPoint());
                    entity.enableSaving = false;
                    entity.Spawn();

                    entity.displayName = spawnMurderers ? "Zombie" : "Scientist";

                    entity.LootSpawnSlots = new LootContainer.LootSpawnSlot[0];
                    (entity as ScientistNPC).DeathEffects = new GameObjectRef[0];

                    entity.NavAgent.enabled = false;
                    entity.CancelInvoke(entity.EquipTest);
                    entity.CancelInvoke(entity.EnableNavAgent);
                    entity.CancelInvoke(entity.EquipTest);

                    entity.InitializeHealth(spawnMurderers ? configData.ZHealth : configData.SHealth, spawnMurderers ? configData.ZHealth : configData.SHealth);

                    entity.damageScale = spawnMurderers ? configData.ZMod : configData.SMod;

                    if (!string.IsNullOrEmpty(spawnMurderers ? configData.ZKit : configData.SKit))
                    {
                        Arena.StripInventory(entity);

                        ins.NextTick(() =>
                        {
                            Arena.GiveKit(entity, spawnMurderers ? configData.ZKit : configData.SKit);
                            CreateControllerComponent(entity);
                        });
                    }
                    else if (spawnMurderers)
                    {
                        Arena.StripInventory(entity);

                        ins.NextTick(() =>
                        {
                            ItemManager.CreateByName("halloween.mummysuit").MoveToContainer(entity.inventory.containerWear);
                            ItemManager.CreateByName("pitchfork").MoveToContainer(entity.inventory.containerBelt, 0);

                            CreateControllerComponent(entity);                            
                        });
                    }       
                    
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                }

                UpdateScoreboard();
                yield return null;
            }

            private void CreateControllerComponent(global::HumanNPC entity)
            {
                NPCController npcController = entity.gameObject.AddComponent<NPCController>();
                npcController.Initialize(this, config.zoneId);
                npcPlayers.Add(npcController);
                npcPlayerIds.Add(entity.userID);
            }

            private global::HumanNPC InstantiateEntity(string type, Vector3 position)
            {
                GameObject gameObject = Facepunch.Instantiate.GameObject(GameManager.server.FindPrefab(type), position, new Quaternion());
                gameObject.name = type;

                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

                Destroy(gameObject.GetComponent<Spawnable>());

                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);

                global::HumanNPC component = gameObject.GetComponent<global::HumanNPC>();
                return component;
            }

            public override bool CanJoinEvent()
            {
                if (status == Arena.EventStatus.Started)
                    return false;
                return base.CanJoinEvent();
            }

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team = Arena.Team.None)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        break;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        break;
                    case Arena.EventStatus.Started:
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                deadPlayers.Add(new Arena.DeadPlayer(eventPlayer, eventPlayer.kills, eventPlayer.deaths));

                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("survleftevent", new string[] { eventPlayer.player.displayName, (eventPlayers.Count - 1).ToString() });
                base.LeaveEvent(eventPlayer);
            }

            public override bool CanPlayerDealDamage(Arena.EventPlayer attacker, BaseEntity entity, HitInfo info)
            {
                if (entity != null && entity is NPCPlayer)
                {
                    NPCController npcController = (entity as NPCPlayer).GetComponent<NPCController>();
                    if (npcController != null && npcPlayers.Contains(npcController))
                    {
                        return true;
                    }
                }
                return false;
            }

            public override void OnPlayerTakeDamage(Arena.EventPlayer eventPlayer, HitInfo info)
            {
                if (info != null)
                {
                    Arena.EventPlayer attacker = Arena.GetUser(info.InitiatorPlayer);
                    if (attacker != null)
                    {
                        Arena.NullifyDamage(info);
                        return;
                    }
                }
                base.OnPlayerTakeDamage(eventPlayer, info);
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.isDead = true;
                victim.AddDeath();
                                
                base.OnPlayerDeath(victim, attacker, info);

                if (victim.deaths >= playerLives)
                {
                    victim.isEliminated = true;

                    if (!deathKick)
                    {
                        deadPlayers.Add(new Arena.DeadPlayer(victim, victim.kills, victim.deaths));

                        foreach (Arena.EventPlayer spectator in eventPlayers)
                        {
                            if (spectator.player.IsSpectating())
                                spectator.UpdateSpectateTarget();
                        }

                        BroadcastToPlayer(victim, Arena.msg("youareout", victim.player.userID));
                        victim.BeginSpectating();

                        InvokeHandler.Invoke(this, () =>
                        {
                            if (victim != null && victim.player != null)
                                Arena.UI.DestroyUI(victim.player, ArenaUI.UIPanel.Death);
                        }, 3f);
                    }
                    else LeaveEvent(victim);
                }

                UpdateScoreboard();

                bool allDead = true;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                {
                    if (!eventPlayer.isEliminated)
                    {
                        allDead = false;
                        break;
                    }
                }

                if (allDead || eventPlayers.Count == 0)                
                    EndMatch();                
            }

            public override void OnNpcDeath(BaseCombatEntity entity, HitInfo info)
            {
                if (entity == null)
                    return;

                NPCController npcController = entity.GetComponent<NPCController>();

                if (npcController == null || !npcPlayers.Contains(npcController))
                    return;

                npcPlayers.Remove(npcController);

                if (status != Arena.EventStatus.Started)
                    return;

                if (info != null)
                {
                    Arena.EventPlayer eventPlayer = Arena.GetUser(info.InitiatorPlayer);
                    if (eventPlayer != null)
                    {
                        eventPlayer.kills++;
                        
                        Statistics.OnNPCPlayerDeath(eventPlayer, info, config);
                    }

                    UpdateScoreboard();
                }

                if (npcPlayers.Count == 0)
                    NextRound();
            }

            public override void OnNpcCorpseSpawned(LootableCorpse corpse)
            {
                if (npcPlayerIds.Contains(corpse.playerSteamID))
                {
                    if (corpse.containers != null)
                    {
                        ItemContainer[] itemContainerArray = corpse.containers;
                        for (int i = 0; i < (int)itemContainerArray.Length; i++)
                        {
                            if (i == 1)
                                continue;
                            itemContainerArray[i].Clear();
                        }
                    }
                    InvokeHandler.Invoke(corpse, corpse.DieInstantly, 5f);
                }
            }

            public override bool IsEventNPC(ulong npcId)
            {
                return npcPlayerIds.Contains(npcId);
            }

            public override void EndMatch()
            {
                InvokeHandler.CancelInvoke(this, SpawnNPCs);

                if (npcPlayers.Count > 0)
                {
                    for (int i = npcPlayers.Count - 1; i >= 0; i--)
                    {
                        NPCController monitor = npcPlayers[i];
                        if (monitor == null || monitor.Player == null)
                            continue;

                        if (!monitor.Player.IsDestroyed)
                            monitor.Player.Kill();
                    }                    
                                            
                }
                npcPlayers.Clear();

                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                if (winner == null)
                {
                    if (eventPlayers.Count > 0)
                    {
                        int kills = 0;
                        int deaths = 0;

                        foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                        {
                            if (eventPlayer.isDead)
                                continue;

                            if (eventPlayer.kills > kills)
                            {
                                winner = eventPlayer;
                                kills = eventPlayer.kills;
                                deaths = eventPlayer.deaths;
                            }
                            else if (eventPlayer.kills == kills)
                            {
                                if (eventPlayer.deaths < deaths)
                                {
                                    winner = eventPlayer;
                                    kills = eventPlayer.kills;
                                    deaths = eventPlayer.deaths;
                                }
                            }
                        }
                    }
                }

                if (winner != null)
                {
                    Statistics.OnGamePlayed(winner.player.userID, config);
                    Statistics.OnEventWin(new List<ulong> { winner.player.userID }, config);
                    BroadcastToPlayer(winner, Arena.msg("youarewinner", winner.player.userID));

                    BroadcastWinners("globalwin", new string[] { winner.player.displayName, config.eventName });
                }

                List<ulong> losers = deadPlayers.Select(x => x.playerId).ToList();
                losers.AddRange(eventPlayers.Where(x => x.isDead).Select(x => x.player.userID));

                foreach (ulong loser in losers)
                    Statistics.OnGamePlayed(loser, config);

                Statistics.OnEventLose(losers, config);

                base.EndMatch();
            }

            public override List<Arena.ScoreEntry> GetGameScores(Arena.Team team = Arena.Team.A)
            {
                List<Arena.ScoreEntry> gameScores = new List<Arena.ScoreEntry>();
                ulong[] deadPlayerIds = deadPlayers.Select(x => x.playerId).ToArray();

                int i = 1;                
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.OrderByDescending(x => x.kills))
                {
                    if (deadPlayerIds.Contains(eventPlayer.player.userID))
                        continue;

                    gameScores.Add(new Arena.ScoreEntry(eventPlayer, i, eventPlayer.kills, eventPlayer.deaths));
                    i++;
                }
                List<Arena.DeadPlayer> reversedDead = new List<Arena.DeadPlayer>(deadPlayers);
                reversedDead.Reverse();
                foreach (Arena.DeadPlayer deadPlayer in reversedDead)
                {
                    if (gameScores.Find(x => x.displayName == deadPlayer.playerName) != null)
                        continue;

                    gameScores.Add(new Arena.ScoreEntry() { position = i, value2 = deadPlayer.value2, displayName = $"{Arena.msg("deadplayer")} {deadPlayer.playerName}", value1 = deadPlayer.value1 });
                    i++;
                }
                return gameScores;
            }

            public override string GetScoreString() => string.Format(Arena.msg("roundsremain"), roundNumber, maxRounds);

            public override string GetAdditionalScoreString() => string.Format(Arena.msg("npcsremain"), npcPlayers.Count);

            public class NPCController : MonoBehaviour
            {
                internal static List<NPCController> allNpcs = new List<NPCController>();

                internal global::HumanNPC Player { get; private set; }

                internal Transform Transform { get; private set; }

                private HumanBrain brain;

                private Rust.AI.SimpleAIMemory memory;

                private Vector3 initialPosition;

                internal NPCSurvival eventGame;

                private string zoneId;

                private bool outOfBounds;

                private float attackRange;

                private void Awake()
                {
                    Player = GetComponent<global::HumanNPC>();
                    Transform = Player.transform;

                    brain = _brain.GetValue(Player) as HumanBrain;
                    memory = _memory.GetValue(Player) as Rust.AI.SimpleAIMemory;

                    initialPosition = Transform.position;

                    Player.NavAgent.areaMask = 1;
                    Player.NavAgent.agentTypeID = -1372625422;

                    initialPosition = Transform.position;

                    enabled = false;

                    ins.NextTick(() => AIThinkManager.Remove(Player));

                    Player.Invoke(() =>
                    {                        
                        if (!NavMesh.SamplePosition(Transform.position, out navHit, 5f, 1))
                            Player.SetNavMeshEnabled(false);
                        else
                        {
                            Player.ServerPosition = Transform.position = navHit.position;
                            Player.SetNavMeshEnabled(true);
                            Player.NavAgent.Warp(navHit.position);
                        }

                        allNpcs.Add(this);

                        EquipWeapon();

                        ClearMemory();
                        SetClosestPlayerTarget();

                    }, 0.25f);
                }

                private void OnDestroy()
                {
                    allNpcs.Remove(this);

                    if (Player != null && !Player.IsDestroyed)
                        Player.Kill();
                }

                public void Initialize(NPCSurvival eventGame, string zoneId)
                {
                    this.eventGame = eventGame;
                    this.zoneId = zoneId;
                }

                private void SetClosestPlayerTarget()
                {
                    BasePlayer target = null;

                    float distance = float.PositiveInfinity;
                    for (int i = 0; i < eventGame.eventPlayers.Count; i++)
                    {
                        BasePlayer p = eventGame.eventPlayers[i]?.player;
                        if (p != null)
                        {
                            float d = Vector3.Distance(Transform.position, p.transform.position);
                            if (d < distance)
                            {
                                target = p;
                                distance = d;
                            }
                        }
                    }

                    if (target != null)
                    {
                        memory.Update(target);
                        Player.currentTarget = target;
                    }
                }

                public void OnNPCLeaveZone(string zoneId)
                {
                    if (this.zoneId == zoneId)
                    {
                        outOfBounds = true;
                    }
                }

                public void OnNPCEnterZone(string zoneId)
                {
                    if (this.zoneId == zoneId)
                    {
                        outOfBounds = false;
                    }
                }

                public object CanTargetEntity(BaseEntity entity)
                {
                    BasePlayer target = entity?.ToPlayer();
                    if (target == null)
                        return false;

                    Arena.EventPlayer eventPlayer = target.GetComponent<Arena.EventPlayer>();
                    if (eventPlayer == null)
                        return false;

                    return null;
                }

                internal void EquipWeapon()
                {
                    Item slot = Player.inventory.containerBelt.GetSlot(0);
                    if (slot != null)
                    {
                        Player.UpdateActiveItem(slot.uid);
                        BaseEntity heldEntity = slot.GetHeldEntity();
                        if (heldEntity != null)
                        {
                            AttackEntity component = heldEntity.GetComponent<AttackEntity>();
                            if (component != null)
                            {
                                if (component is BaseProjectile)
                                {
                                    Item weapon = component.GetItem();
                                    if (weapon != null && weapon.contents != null)
                                    {
                                        if (UnityEngine.Random.Range(0, 3) == 0)
                                        {
                                            Item item1 = ItemManager.CreateByName("weapon.mod.flashlight", 1, (ulong)0);
                                            if (!item1.MoveToContainer(weapon.contents, -1, true))
                                            {
                                                item1.Remove(0f);
                                                return;
                                            }

                                            Player.InvokeRandomized(Player.LightCheck, 0f, 30f, 5f);
                                            Player.LightToggle(true);
                                            return;
                                        }
                                        Item item2 = ItemManager.CreateByName("weapon.mod.lasersight", 1, (ulong)0);
                                        if (!item2.MoveToContainer(weapon.contents, -1, true))
                                        {
                                            item2.Remove(0f);
                                        }
                                        Player.LightToggle(true);
                                    }

                                    attackRange = (component as BaseProjectile).effectiveRange * 1.5f;
                                }
                                else attackRange = component.effectiveRange;
                                component.TopUpAmmo();

                                melee = component as BaseMelee;
                            }
                        }
                    }                    
                }

                #region Hackfest9000
                //The hackiest shit. Follow NPC think logic all the way down to the use of the weapon just so conditions can be changed to deal damage to players because ScientistNPCs don't deal damage to players when using a melee item all because no where in the ScientistNPC inheritance chain is 1 hard coded string overriden (BaseEntity.Categorize()) so it thinks they are players... what the actual fuck
                private BaseMelee melee;

                private float timeSinceItemTick = 0.1f;

                private float timeSinceTargetUpdate = 0.5f;

                private float lastThinkTime = Time.time;

                private float targetAimedDuration;

                private void TryThink()
                {
                    float delta = Time.time - lastThinkTime;
                    Player.MovementUpdate(delta);
                    if (brain.ShouldThink())
                    {
                        brain.DoThink();
                    }
                    timeSinceItemTick += delta;
                    timeSinceTargetUpdate += delta;
                    if (timeSinceItemTick > 0.1f)
                    {
                        TickItems(timeSinceItemTick);
                        timeSinceItemTick = 0f;
                    }
                    if (timeSinceTargetUpdate > 0.5f)
                    {
                        Player.UpdateTargets(timeSinceTargetUpdate);
                        timeSinceTargetUpdate = 0f;
                    }
                }

                private void TickItems(float delta)
                {
                    if (Player.desiredSpeed == global::HumanNPC.SpeedType.Sprint || Player.currentTarget == null)
                    {
                        targetAimedDuration = 0f;
                        Player.triggerEndTime = Time.time;
                        return;
                    }
                    if (!Player.currentTargetLOS)
                    {
                        targetAimedDuration = 0f;
                    }
                    else if (Vector3.Dot(Player.eyes.BodyForward(), Player.currentTarget.CenterPoint() - Player.eyes.position) > 0.8f)
                    {
                        targetAimedDuration += delta;
                    }
                    if (targetAimedDuration <= 0.2f)
                    {
                        Player.triggerEndTime = Time.time + 0.2f;
                    }
                    else
                    {
                        AttackEntity attackEntity = Player.GetAttackEntity();
                        if (attackEntity)
                        {
                            if (Player.DistanceToTarget() < attackEntity.effectiveRange * (attackEntity.aiOnlyInRange ? 1f : 2f))
                            {
                                Attack();
                                return;
                            }
                        }
                    }
                }

                private bool Attack()
                {
                    AttackEntity heldEntity = Player.GetHeldEntity() as AttackEntity;
                    if (heldEntity == null)                    
                        return false;
                                        
                    if (Mathf.Approximately(heldEntity.attackLengthMin, -1f))
                    {
                        ServerUse();
                        Player.lastGunShotTime = Time.time;
                        return true;
                    }

                    if (Player.IsInvoking(TriggerDown))                    
                        return true;
                    
                    if (Time.time < Player.nextTriggerTime)                    
                        return true;

                    Player.InvokeRepeating(TriggerDown, 0f, 0.01f);
                    Player.triggerEndTime = Time.time + UnityEngine.Random.Range(heldEntity.attackLengthMin, heldEntity.attackLengthMax);
                    TriggerDown();
                    return true;
                }

                private void TriggerDown()
                {
                    AttackEntity heldEntity = Player.GetHeldEntity() as AttackEntity;
                    if (heldEntity != null)
                    {
                        ServerUse();
                    }
                    Player.lastGunShotTime = Time.time;
                    if (Time.time > Player.triggerEndTime)
                    {
                        Player.CancelInvoke(TriggerDown);
                        Player.nextTriggerTime = Time.time + (heldEntity != null ? heldEntity.attackSpacing : 1f);
                    }
                }

                private float nextAttackTime;
                private void ServerUse()
                {                    
                    if (Time.time < nextAttackTime)                    
                        return;                    

                    nextAttackTime = CalculateCooldownTime(nextAttackTime, melee.repeatDelay * 2f, true);
                    Player.SignalBroadcast(BaseEntity.Signal.Attack, string.Empty, null);
                    if (melee.swingEffect.isValid)
                    {
                        Effect.server.Run(melee.swingEffect.resourcePath, melee.transform.position, Vector3.forward, Player.net.connection, false);
                    }
                    if (melee.IsInvoking(ServerUse_Strike))
                    {
                        melee.CancelInvoke(ServerUse_Strike);
                    }
                    melee.Invoke(ServerUse_Strike, melee.aiStrikeDelay);
                }

                private void ServerUse_Strike()
                {
                    Vector3 position = Player.eyes.position;
                    Vector3 vector = Player.eyes.BodyForward();
                    for (int i = 0; i < 2; i++)
                    {
                        List<RaycastHit> list = Facepunch.Pool.GetList<RaycastHit>();
                        GamePhysics.TraceAll(new Ray(position - vector * ((i == 0) ? 0f : 0.2f), vector), (i == 0) ? 0f : melee.attackRadius, list, melee.effectiveRange + 0.2f, 1219701521, QueryTriggerInteraction.UseGlobal);
                        bool flag = false;

                        for (int j = 0; j < list.Count; j++)
                        {
                            RaycastHit hit = list[j];
                            BaseEntity entity = hit.GetEntity();

                            if (entity != null && entity != Player && !Player.ShortPrefabName.Equals(entity.ShortPrefabName))
                            {
                                float num = 0f;
                                foreach (Rust.DamageTypeEntry damageTypeEntry in melee.damageTypes)
                                {
                                    num += damageTypeEntry.amount;
                                }
                                entity.OnAttacked(new HitInfo(Player, entity, Rust.DamageType.Slash, num * Player.damageScale));

                                HitInfo hitInfo = Facepunch.Pool.Get<HitInfo>();
                                hitInfo.HitEntity = entity;
                                hitInfo.HitPositionWorld = hit.point;
                                hitInfo.HitNormalWorld = -vector;
                                if (entity is BaseNpc || entity is BasePlayer)
                                {
                                    hitInfo.HitMaterial = StringPool.Get("Flesh");
                                }
                                else
                                {
                                    hitInfo.HitMaterial = StringPool.Get((hit.GetCollider().sharedMaterial != null) ? hit.GetCollider().sharedMaterial.GetName() : "generic");
                                }
                                melee.ServerUse_OnHit(hitInfo);
                                Effect.server.ImpactEffect(hitInfo);
                                Facepunch.Pool.Free<HitInfo>(ref hitInfo);
                                flag = true;
                                if (!(entity != null) || entity.ShouldBlockProjectiles())
                                {
                                    break;
                                }
                            }
                        }
                        Facepunch.Pool.FreeList<RaycastHit>(ref list);
                        if (flag)
                        {
                            break;
                        }
                    }
                }

                protected float CalculateCooldownTime(float nextTime, float cooldown, bool catchup)
                {
                    float single = Time.time;
                    float single1 = 0f;
                    single1 += 0.1f;
                    single1 = single1 + cooldown * 0.1f;
                    single1 = single1 + (Player ? Player.desyncTime : 0.1f);
                    single1 += Mathf.Max(Time.deltaTime, Time.smoothDeltaTime);

                    if (nextTime < 0f)
                    {
                        nextTime = Mathf.Max(0f, single + cooldown - single1);
                    }
                    else if (single - nextTime > single1)
                    {
                        nextTime = Mathf.Max(nextTime + cooldown, single + cooldown - single1);
                    }
                    else
                    {
                        nextTime = Mathf.Min(nextTime + cooldown, single + cooldown);
                    }
                    return nextTime;
                }
                #endregion

                internal void DoUpdate()
                {
                    if (Player == null || Player.IsDestroyed || !Player.NavAgent.enabled)
                        return;
                    
                    Player.IsDormant = false;

                    float distanceFromInit = Vector3.Distance(Transform.position, initialPosition);

                    if (outOfBounds)
                    {
                        Player.SetDesiredSpeed(global::HumanNPC.SpeedType.Sprint);
                        ClearMemory();
                        UpdateTargetPosition(initialPosition);
                        return;
                    }

                    if (melee != null)
                        TryThink();
                    else Player.TryThink();

                    if (!Player.HasTarget())
                    {
                        SetClosestPlayerTarget();
                        return;                        
                    }
                    else
                    {
                        if (!(Player.currentTarget is BasePlayer) || !Arena.GetUser(Player.currentTarget as BasePlayer))
                        {
                            ClearMemory();
                            SetClosestPlayerTarget();
                            return;
                        }
                        else
                        {
                            if (Vector3.Distance(Transform.position, Player.currentTarget.transform.position) > Player.sightRangeLarge)
                            {
                                RemoveFromMemory(Player.currentTarget);
                                SetClosestPlayerTarget();
                                return;
                            }
                            else
                            {
                                if (Player.IsVisibleToUs(Player.currentTarget as BasePlayer) && Player.DistanceToTarget() < attackRange)
                                {
                                    Player.SetDesiredSpeed(global::HumanNPC.SpeedType.Walk);
                                    return;
                                }

                                Vector3 attackDesination = Player.currentTarget.transform.position;
                                
                                if (Vector3.Distance(attackDesination, Transform.position) < 2f)
                                {
                                    Player.SetDesiredSpeed(global::HumanNPC.SpeedType.Walk);
                                    return;
                                }

                                Player.SetDestination(attackDesination);
                                Player.SetDesiredSpeed(global::HumanNPC.SpeedType.Sprint);
                                return;
                            }
                        }
                    }
                }

                private void RemoveFromMemory(BaseEntity target)
                {
                    for (int i = 0; i < memory.All.Count; i++)
                    {
                        if (memory.All[i].Entity != null)
                        {
                            memory.Visible.Remove(memory.All[i].Entity);
                        }
                        memory.All.RemoveAt(i);
                        i--;
                    }
                    Player.currentTarget = null;
                }

                private void ClearMemory()
                {
                    memory.All.Clear();
                    memory.Visible.Clear();
                    Player.currentTarget = null;
                }

                public void UpdateTargetPosition(Vector3 targetPosition)
                {
                    if (Player == null || Player.IsDestroyed || Player.NavAgent == null)
                        return;

                    if (Player.NavAgent.isOnNavMesh)
                        Player.SetDestination(targetPosition);
                }

                public void OnReceivedDamage(BasePlayer attacker)
                {
                    if (Player == null || Player.IsDestroyed || Player.NavAgent == null || Player.isMounted)
                        return;

                    if (Vector3.Distance(Transform.position, attacker.transform.position) <= Player.sightRangeLarge)
                    {
                        memory.Update(attacker);

                        if (Player.currentTarget == null || Player.currentTarget == attacker)
                        {
                            Player.SetDestination(attacker.transform.position);
                            memory.Update(attacker);
                            Player.currentTarget = attacker;

                            if (Player.IsVisibleToUs(Player.currentTarget as BasePlayer) && Player.DistanceToTarget() < Player.GetAttackEntity()?.effectiveRange * 2f)
                                Player.SetDesiredSpeed(global::HumanNPC.SpeedType.Walk);
                            else Player.SetDesiredSpeed(global::HumanNPC.SpeedType.Sprint);
                        }
                    }
                }
            }
        }
        #endregion

        /* #region Slasher
        private class Slasher : Arena.EventManager
        {
            private List<Arena.DeadPlayer> deadPlayers = new List<Arena.DeadPlayer>();

            private Arena.EventPlayer matchWinner;

            private Arena.EventPlayer slasher;

            private Arena.EventPlayer winner;

            private Arena.GameTimer matchTimer;

            public MatchStatus matchStatus;

            private int roundNumber;

            private int matchRounds;
            private int slasherTime;
            private int playerTime;

            private float damageModifier;

            private string slasherSpawnfile;
            private string slasherWeapon;
            private string slasherAmmo;
            private string slasherKit;
            private string playerKit;

            public enum MatchStatus { Pending, Slasher, Hunted }

            public long timeSync = new DateTime(TOD_Sky.Instance.Cycle.Year, TOD_Sky.Instance.Cycle.Month, TOD_Sky.Instance.Cycle.Day, 23, 0, 0).ToBinary();

            public override void InitializeEvent(string eventName, Arena.ArenaData.EventConfig config)
            {
                base.InitializeEvent(eventName, config);
                
                matchRounds = Convert.ToInt32(config.additionalParameters["Rounds to Play"]);
                slasherTime = Convert.ToInt32(config.additionalParameters["Slasher Kill Timer (seconds)"]);
                playerTime = Convert.ToInt32(config.additionalParameters["Player Kill Timer (seconds)"]);
                slasherWeapon = config.additionalParameters["Slasher Weapon"] as string;
                slasherKit = config.additionalParameters["Slasher Kit"] as string;
                slasherSpawnfile = config.additionalParameters["Slasher Spawnfile"] as string;
                playerKit = config.additionalParameters["Player Kit"] as string;
                damageModifier = Convert.ToSingle(config.additionalParameters["Player Damage Modifier"]);

                matchTimer = gameObject.AddComponent<Arena.GameTimer>();
                matchTimer.Register(this, OnMatchTimerExpired, true);
            }

            public override void OnDestroy()
            {
                Destroy(matchTimer);
                base.OnDestroy();
            }

            public override void Prestart()
            {
                base.Prestart();
                deadPlayers.Clear();
                matchWinner = null;
                slasher = null;
                winner = null;
                matchTimer.StopTimer();
                matchStatus = MatchStatus.Pending;
                roundNumber = 0;
            }
            
            public override bool CanJoinEvent()
            {
                if (status == Arena.EventStatus.Started)
                    return false;
                return base.CanJoinEvent();
            }

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team = Arena.Team.None)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();

                eventPlayer.team = team;
                eventPlayers.Add(eventPlayer);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        break;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        break;
                    case Arena.EventStatus.Started:
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                deadPlayers.Add(new Arena.DeadPlayer(eventPlayer, eventPlayer.kills, eventPlayer.deaths));

                if (eventPlayer == slasher)
                    NextRound();

                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("survleftevent", new string[] { eventPlayer.player.displayName, (eventPlayers.Count - 1).ToString() });
                base.LeaveEvent(eventPlayer);                
            }

            public override float GetDamageModifier(Arena.EventPlayer attacker)
            {
                if (attacker == null || attacker == slasher)
                    return 0f;

                if (attacker.player?.GetActiveItem()?.info.shortname == "flashlight.held")
                    return damageModifier;

                return 0f;
            }

            public override bool CanShowDeathScreen()
            {
                return false;
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.isDead = true;
                victim.AddDeath();

                base.OnPlayerDeath(victim, attacker, info);

                victim.isDead = true;

                deadPlayers.Add(new Arena.DeadPlayer(victim, victim.kills, victim.specialInt1));

                foreach (Arena.EventPlayer spectator in eventPlayers.Where(x => x.player.IsSpectating()))
                {
                    //if (GetSpectateTargets(Arena.Team.A).Length == 0)
                       // spectator.FinishSpectating();
                    spectator.UpdateSpectateTarget();
                }

                BroadcastToPlayer(victim, Arena.msg("youareout", victim.player.userID));               

                InvokeHandler.Invoke(this, () =>
                {
                    if (victim != null && victim.player != null)
                        Arena.UI.DestroyUI(victim.player, ArenaUI.UIPanel.Death);
                }, 3f);

                UpdateScoreboard();

                if (eventPlayers.Count == 0)
                {
                    EndMatch();
                    return;
                }

                if (victim == slasher)
                {
                    foreach (Arena.EventPlayer eventPlayer in eventPlayers.Where(x => x != slasher && !x.isDead))
                        eventPlayer.specialInt1++;

                    if (attacker != null)
                    {
                        Arena.EventPlayer eventPlayer = attacker.GetComponent<Arena.EventPlayer>();
                        if (eventPlayer != null)
                            matchWinner = eventPlayer;
                    }

                    BroadcastToPlayers("hunterWinners", null, true);
                    EndRound();
                    return;
                }

                bool allDead = true;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.Where(x => x != slasher))
                {
                    if (!eventPlayer.isDead)
                    {
                        allDead = false;
                        break;
                    }
                }
                
                if (allDead)
                {
                    if (slasher != null)
                        slasher.specialInt1++;
                    BroadcastToPlayers("slasherWinners", null, true);
                    EndRound();
                    return;
                }

                victim?.BeginSpectating();
            }

            public override void StartMatch()
            {
                base.StartMatch();
                EndRound();
            }

            public override void EndMatch()
            {
                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                if (winner == null)
                {
                    if (eventPlayers.Count > 0)
                    {
                        int kills = 0;
                        int wins = 0;

                        foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                        {
                            if (eventPlayer.specialInt1 > wins)
                            {
                                winner = eventPlayer;
                                kills = eventPlayer.kills;
                                wins = eventPlayer.specialInt1;
                            }
                            else if (eventPlayer.specialInt1 == wins)
                            {
                                if (eventPlayer.kills > kills)
                                {
                                    winner = eventPlayer;
                                    kills = eventPlayer.kills;
                                    wins = eventPlayer.specialInt1;
                                }
                            }
                        }
                    }
                }

                if (winner != null)
                {
                    Statistics.OnGamePlayed(winner.player.userID, config);
                    Statistics.OnEventWin(new List<ulong> { winner.player.userID }, config);
                    BroadcastToPlayer(winner, Arena.msg("youarewinner", winner.player.userID));

                    BroadcastWinners("globalwin", new string[] { winner.player.displayName, config.eventName });
                }

                List<ulong> losers = deadPlayers.Select(x => x.playerId).ToList();
                losers.AddRange(eventPlayers.Where(x => x.isDead).Select(x => x.player.userID));

                foreach (var loser in losers)
                    Statistics.OnGamePlayed(loser, config);

                Statistics.OnEventLose(losers, config);

                base.EndMatch();
            }

            private void EndRound()
            {
                matchTimer.StopTimer();
                matchStatus = MatchStatus.Pending;

                roundNumber++;

                deadPlayers.Clear();
                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                {
                    eventPlayer.isDead = false;
                    if (eventPlayer.player.IsSpectating())
                        eventPlayer.FinishSpectating();

                    Arena.ResetMetabolism(eventPlayer.player);
                    Arena.StripInventory(eventPlayer.player);
                }

                if (roundNumber < matchRounds)
                {                    
                    BroadcastToPlayers("nextround", new string[] { (roundNumber).ToString() }, false);                    
                    InvokeHandler.Invoke(this, NextRound, 10f);
                }
                else EndMatch();                
            }

            private void NextRound()
            {
                matchStatus = MatchStatus.Slasher;

                if (matchWinner == null)
                    slasher = eventPlayers.GetRandom();
                else slasher = matchWinner;

                matchWinner = null;

                SpawnPlayers();
                matchTimer.StartTimer(slasherTime, Arena.msg("hideSlasher"));
            }

            private void OnMatchTimerExpired()
            {
                if (matchStatus == MatchStatus.Slasher)
                {
                    matchTimer.StartTimer(playerTime, Arena.msg("killSlasher"));
                    matchStatus = MatchStatus.Hunted;

                    BroadcastToPlayers("slasherRoundChange", null, true);
                    foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                    {
                        if (eventPlayer != slasher)
                            GiveSlasherWeapon(eventPlayer);
                    }
                }
                else
                {
                    BroadcastToPlayers("matchDraw", null, true);
                    EndRound();
                }
            }


            private void SpawnPlayers()
            {
                foreach(Arena.EventPlayer eventPlayer in eventPlayers)
                {
                    if (eventPlayer == slasher)
                        Arena.SpawnPlayer(eventPlayer.player, slasherSpawnfile);
                    else Arena.SpawnPlayer(eventPlayer.player, config.teamA.spawnfile);

                    GiveGear(eventPlayer);
                }
            }

            private void GiveGear(Arena.EventPlayer eventPlayer)
            {
                if (eventPlayer == slasher)
                {
                    Arena.GiveKit(eventPlayer.player, slasherKit);
                    GiveSlasherWeapon(eventPlayer);
                    BroadcastToPlayer(eventPlayer, Arena.msg("slasherHelp"));
                }
                else
                {
                    Arena.GiveKit(eventPlayer.player, playerKit);
                    GivePlayerTorch(eventPlayer);
                    BroadcastToPlayer(eventPlayer, Arena.msg("huntedHelp"));
                }
            }

            private void GivePlayerTorch(Arena.EventPlayer eventPlayer)
            {
                Item flashlight = ItemManager.Create(ItemManager.itemDictionaryByName["flashlight.held"]);
                flashlight.MoveToContainer(eventPlayer.player.inventory.containerBelt);                
            }

            private void GiveSlasherWeapon(Arena.EventPlayer eventPlayer)
            {
                Item item = ItemManager.Create(ItemManager.itemDictionaryByName[slasherWeapon]);

                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;

                    Item flashlight = ItemManager.Create(ItemManager.itemDictionaryByName["weapon.mod.flashlight"]);
                    flashlight.MoveToContainer(item.contents);

                    Item ammo = ItemManager.Create(weapon.primaryMagazine.ammoType, 1000);
                    ammo.MoveToContainer(eventPlayer.player.inventory.containerMain);
                }
                item.MoveToContainer(eventPlayer.player.inventory.containerBelt);
            }
           
            public override bool CanDropAmmo()
            {
                return false;
            }

            public override bool CanDropBackpack()
            {
                return false;
            }

            public override bool CanDropWeapons()
            {
                return false;
            }

            public override string GetScoreString() => string.Format(Arena.msg("roundsremain"), roundNumber, matchRounds);

            public override string[] GetScoreType() => new string[] { "K", "W" };

            public override string GetScoreName(bool first) => first ? Arena.msg("kills") : Arena.msg("wins");

            public override List<Arena.ScoreEntry> GetGameScores(Arena.Team team = Arena.Team.A)
            {
                List<Arena.ScoreEntry> gameScores = new List<Arena.ScoreEntry>();
                ulong[] deadPlayerIds = deadPlayers.Select(x => x.playerId).ToArray();

                int i = 1;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.OrderByDescending(x => x.specialInt1))
                {
                    if (deadPlayerIds.Contains(eventPlayer.player.userID))
                        continue;

                    gameScores.Add(new Arena.ScoreEntry(eventPlayer, i, eventPlayer.kills, eventPlayer.specialInt1));
                    i++;
                }
                List<Arena.DeadPlayer> reversedDead = new List<Arena.DeadPlayer>(deadPlayers);
                reversedDead.Reverse();
                foreach (Arena.DeadPlayer deadPlayer in reversedDead)
                {
                    if (gameScores.Find(x => x.displayName == deadPlayer.playerName) != null)
                        continue;
                    gameScores.Add(new Arena.ScoreEntry() { position = i, value2 = deadPlayer.value2, displayName = $"{Arena.msg("deadplayer")} {deadPlayer.playerName}", value1 = deadPlayer.value1 });
                    i++;
                }
                return gameScores;
            }

            public override Arena.EventPlayer[] GetSpectateTargets(Arena.Team team)
            {
                return eventPlayers.Where(x => x != slasher && !x.player.IsSpectating()).ToArray();
            }
        }
        #endregion*/

        #region Infected
        private class Infected : Arena.EventManager
        {
            private Arena.GameTimer matchTimer;

            private string infectedKit;
            private string infectedSpawns;
            private bool randomWeapon = false;
            private int roundTimer = 300;

            private bool hasStarted = false;

            private static List<ItemDefinition> weapons;

            public override void Prestart()
            {                
                base.Prestart();

                infectedKit = config.additionalParameters["Infected Kit"] as string;
                infectedSpawns = config.additionalParameters["Infected Spawnfile"] as string;
                randomWeapon = Convert.ToBoolean(config.additionalParameters["Survivors start with random weapon"]);
                roundTimer = Convert.ToInt32(config.additionalParameters["Round Timer"]);

                if (weapons == null)
                    weapons = ItemManager.GetItemDefinitions().Where(x => x.category == ItemCategory.Weapon).ToList();
            }

            public override void StartMatch()
            {
                base.StartMatch();
                matchTimer.StartTimer(9, Arena.msg("chooseInfected"));
            }

            public override void InitializeEvent(string eventName, Arena.ArenaData.EventConfig config)
            {
                base.InitializeEvent(eventName, config);

                matchTimer = gameObject.AddComponent<Arena.GameTimer>();
                matchTimer.Register(this, OnMatchTimerExpired, true);
            }

            public override void OnDestroy()
            {
                Destroy(matchTimer);
                base.OnDestroy();
            }

            private void OnMatchTimerExpired()
            {
                if (!hasStarted)
                {
                    Arena.EventPlayer eventPlayer = eventPlayers.GetRandom();
                    eventPlayer.team = Arena.Team.B;

                    Arena.SpawnPlayer(eventPlayer.player, infectedSpawns);

                    BroadcastToPlayer(eventPlayer, Arena.msg("chosenInfected"), true);

                    matchTimer.StartTimer(roundTimer);
                    BroadcastToTeam(Arena.Team.A, "infectedStart", null, false);
                    hasStarted = true;
                }
                else
                {
                    BroadcastToPlayers("survivorsWin", null, true);
                    EndMatch();
                }
            }

            public override bool CanDropAmmo() => true;

            public override bool CanDropWeapons() => false;

            public override bool CanDropBackpack() => false;

            public override void JoinEvent(Arena.EventPlayer eventPlayer, Arena.Team team = Arena.Team.None)
            {
                if (eventPlayers.Contains(eventPlayer))
                    return;

                eventPlayer.ResetStats();
                Arena.LockInventory(eventPlayer.player);

                eventPlayer.team = Arena.Team.A;
                eventPlayers.Add(eventPlayer);
                BroadcastToPlayers("joinevent", new string[] { eventPlayer.player.displayName });

                base.JoinEvent(eventPlayer, team);
                switch (status)
                {
                    case Arena.EventStatus.Pending:
                        if (Arena.configData.EventSettings.SendToArenaOnJoin)
                            SpawnPlayer(eventPlayer, false, true);
                        else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        return;
                    case Arena.EventStatus.Finished:
                        if (eventPlayers.Count >= config.minimumPlayers && status == Arena.EventStatus.Finished)
                        {
                            BroadcastToPlayers("playersreached", new string[] { Arena.configData.GameTimers.MatchPrestart[config.eventType].ToString() }, true);
                            Prestart();
                        }
                        else
                        {
                            if (Arena.configData.EventSettings.SendToArenaOnJoin)
                                SpawnPlayer(eventPlayer, false, true);
                            else BroadcastToPlayer(eventPlayer, Arena.msg("waitingForPlayers", eventPlayer.player.userID));
                        }
                        return;
                    case Arena.EventStatus.Started:
                        SpawnPlayer(eventPlayer, true, true);
                        return;
                }
            }

            public override void LeaveEvent(Arena.EventPlayer eventPlayer)
            {
                Arena.UnlockInventory(eventPlayer.player);
                if (status != Arena.EventStatus.Finished)
                    BroadcastToPlayers("leftevent", new string[] { eventPlayer.player.displayName });
                base.LeaveEvent(eventPlayer);

                if (hasStarted)
                {
                    if (GetTeamCount(Arena.Team.B) == 0)
                        BroadcastToPlayers("survivorsWin", null, true);
                    else if (GetTeamCount(Arena.Team.A) == 0)
                        BroadcastToPlayers("infectedWin", null, true);

                    EndMatch();
                }
            }

            public override void OnPlayerTakeDamage(Arena.EventPlayer eventPlayer, HitInfo info)
            {
                BasePlayer attacker = info.InitiatorPlayer;
                if (attacker != null)
                {
                    Arena.EventPlayer eventAttacker = Arena.GetUser(attacker);
                    if (eventAttacker != null)
                    {
                        if (eventAttacker.team == eventPlayer.team)
                        {
                            BroadcastToPlayer(eventAttacker, Arena.msg("friendlyFire", attacker.userID));
                            if (!config.ffEnabled)
                            {
                                Arena.NullifyDamage(info);
                                return;
                            }
                        }
                    }
                }
                base.OnPlayerTakeDamage(eventPlayer, info);
            }

            public override void OnPlayerDeath(Arena.EventPlayer victim, BasePlayer attacker = null, HitInfo info = null)
            {
                if (victim == null) return;

                victim.AddDeath();

                if (attacker != null)
                {
                    Arena.EventPlayer attackerPlayer = Arena.GetUser(attacker);

                    if (attackerPlayer != null && victim != attackerPlayer)
                    {
                        attackerPlayer.AddKill();

                        if (attackerPlayer.team == Arena.Team.B)
                        {
                            victim.team = Arena.Team.B;
                            BroadcastToPlayers("infectedKill", new string[] { attackerPlayer.player.displayName });

                            for (int i = 0; i < eventPlayers.Count; i++)
                            {
                                Arena.EventPlayer eventPlayer = eventPlayers[i];
                                if (eventPlayer.team == Arena.Team.A)
                                    eventPlayer.specialInt1 += 50;
                            }
                        }

                        attackerPlayer.specialInt1 += 100;

                        if (GetTeamCount(Arena.Team.A) == 0)
                        {
                            BroadcastToPlayers("infectedWin");
                            EndMatch();
                            return;
                        }
                        else BroadcastToPlayer(victim, Arena.msg("chosenInfected"));
                    }
                }

                UpdateScoreboard();
                base.OnPlayerDeath(victim, attacker);
            }

            public override void GiveTeamGear(Arena.EventPlayer eventPlayer)
            {
                string kitName = eventPlayer.team == Arena.Team.A ? config.teamA.kit : infectedKit;
                Arena.GiveKit(eventPlayer.player, kitName);

                if (eventPlayer.team == Arena.Team.A && randomWeapon)
                {
                    List<Item> items = Facepunch.Pool.GetList<Item>();
                    eventPlayer.player.inventory.AllItemsNoAlloc(ref items);

                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        Item item = items[i];
                        if (item.info.category == ItemCategory.Weapon || item.info.category == ItemCategory.Ammunition)
                        {
                            item.RemoveFromContainer();
                            item.Remove();
                        }
                    }

                    Facepunch.Pool.FreeList(ref items);

                    ItemDefinition randomDefinition = weapons.GetRandom();
                    Item weapon = ItemManager.CreateByItemID(randomDefinition.itemid, 1, 0);
                    eventPlayer.player.GiveItem(weapon, BaseEntity.GiveItemReason.Generic);

                    if (weapon.GetHeldEntity() is BaseProjectile)
                    {
                        ItemDefinition ammoDefinition = (weapon.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType;
                        Item ammo = ItemManager.CreateByItemID(ammoDefinition.itemid, 200, 0);
                        eventPlayer.player.GiveItem(ammo, BaseEntity.GiveItemReason.Generic);
                    }

                    if (weapon.GetHeldEntity() is FlameThrower)
                    {
                        Item ammo = ItemManager.CreateByName("lowgradefuel", 200, 0);
                        eventPlayer.player.GiveItem(ammo, BaseEntity.GiveItemReason.Generic);
                    }
                } 
            }

            public override string[] GetScoreType() => new string[] { "K", "S" };

            public override string GetScoreName(bool first) => first ? Arena.msg("kills") : Arena.msg("score");

            public override List<Arena.ScoreEntry> GetGameScores(Arena.Team team = Arena.Team.A)
            {
                List<Arena.ScoreEntry> gameScores = new List<Arena.ScoreEntry>();
                int i = 1;
                foreach (Arena.EventPlayer eventPlayer in eventPlayers.Where(x => x.team == team).OrderByDescending(x => x.specialInt1))
                {
                    gameScores.Add(new Arena.ScoreEntry(eventPlayer, i, eventPlayer.kills, eventPlayer.specialInt1));
                    i++;
                }
                return gameScores;
            }

            public override void EndMatch()
            {
                if (isUnloading || (Arena != null && Arena.isUnloading))
                    return;

                matchTimer.StopTimer();

                Arena.Team team = GetTeamCount(Arena.Team.A) == 0 ? Arena.Team.B : Arena.Team.A;

                List<ulong> losers = eventPlayers.Where(x => x.team != team).Select(y => y.player.userID).ToList();
                Statistics.OnEventLose(losers, config);

                if (team != Arena.Team.None)
                {
                    List<ulong> winners = eventPlayers.Where(x => x.team == team).Select(y => y.player.userID).ToList();
                    Statistics.OnEventWin(winners, config);

                    BroadcastWinners("globalteamwin", new string[] { team == Arena.Team.A ? (string.IsNullOrEmpty(config.teamA.name) ? Arena.msg("Survivors") : config.teamA.name) : (string.IsNullOrEmpty(config.teamB.name) ? Arena.msg("Infected") : config.teamB.name), eventPlayers.Where(x => x.team == team).Select(y => y.player.displayName).ToSentence(), config.eventName });
                }

                foreach (Arena.EventPlayer eventPlayer in eventPlayers)
                    Statistics.OnGamePlayed(eventPlayer.player.userID, config);

                base.EndMatch();

                hasStarted = false;
            }
        }
        #endregion
        #endregion

        #region WeaponSet Creator
        private Dictionary<ulong, List<Arena.EventWeapon>> setCreator = new Dictionary<ulong, List<Arena.EventWeapon>>();

        [ChatCommand("set")]
        private void cmdGunGameSet(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "arena.admin")) return;
            if (args == null || args.Length == 0)
            {
                if (!setCreator.ContainsKey(player.userID))
                {
                    SendReply(player, "<color=#ce422b>GunGame Weapon Set Creator</color>");
                    SendReply(player, "To begin type <color=#ce422b>/set new</color> then follow the instructions in the promts shown");
                    SendReply(player, "All set items will copy the skin, and the ammo type (if applicable) of the item in your hands!");
                    SendReply(player, "To see all set names type <color=#ce422b>/set list</color>");
                    SendReply(player, "To remove a set type <color=#ce422b>/set remove <name></color>");
                }
                else
                {
                    int count = setCreator[player.userID].Count;
                    if (count == 0)
                    {
                        SendReply(player, $"Before adding any weapon you must set a downgrade weapon (whether you plan on using it or not). Add this weapon by placing the it in your hands and typing <color=#ce422b>\"/set add <amount> <ammo amount>\"</color> replacing <amount> with the amount of the item, and <ammo amount> with the amount of ammo you wish to supply with this weapon (if applicable)");
                    }
                    else
                    {
                        SendReply(player, $"Set a weapon for rank {setCreator[player.userID].Count} by placing the weapon in your hands and typing <color=#ce422b>/set add <amount> <ammo amount></color> replacing <amount> with the amount of the item, and <ammo amount> with the amount of ammo you wish to supply with this weapon (if applicable)");
                        SendReply(player, "If you have finished your set type <color=#ce422b>/set save <name></color> replacing <name> with the name of the set you have created");
                    }
                }
                return;
            }
            switch (args[0].ToLower())
            {
                case "new":
                    if (!setCreator.ContainsKey(player.userID))
                    {
                        setCreator.Add(player.userID, new List<Arena.EventWeapon>());
                        SendReply(player, "You are now creating a new weapon set. You can check the status at any time by typing <color=#ce422b>/set</color>, cancel creation by typing <color=#ce422b>/set cancel</color>, or save by typing <color=#ce422b>/set save <name></color>");
                        SendReply(player, $"Before adding any weapon you must set a downgrade weapon (whether you plan on using it or not).\n You can add any weapon by placing the it in your hands and typing <color=#ce422b>\"/set add <amount> <ammo amount>\"</color> replacing <amount> with the amount of the item, and <ammo amount> with the amount of ammo you wish to supply with this weapon (if applicable)");
                    }
                    else SendReply(player, "You are already creating a weapon set");  
                    return;
                case "add":
                    if (setCreator.ContainsKey(player.userID))
                    {
                        Item item = player.GetActiveItem();
                        if (item != null)
                        {
                            int amount = 1;
                            int ammoAmount = 0;
                            if (args.Length > 1 && !int.TryParse(args[1], out amount))
                            {
                                SendReply(player, "The amount must be a number");
                                return;
                            }
                            if (args.Length > 2 && !int.TryParse(args[2], out ammoAmount))
                            {
                                SendReply(player, "The ammo amount must be a number");
                                return;
                            }
                            Arena.EventWeapon eventWeapon = new Arena.EventWeapon
                            {
                                shortname = item.info.shortname,
                                ammoAmount = ammoAmount > 0 ? ammoAmount : (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine?.contents ?? 0,
                                amount = amount,
                                ammoType = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine?.ammoType.shortname ?? "",
                                attachments = item?.contents?.itemList.Select(x => x.info.shortname).ToArray(),
                                skinId = item.skin
                            };
                            setCreator[player.userID].Add(eventWeapon);
                            if (setCreator[player.userID].Count == 1)
                                SendReply(player, $"Downgrade weapon successfully added! Now add your rank weapons by continuing the same process");
                            else SendReply(player, $"Weapon successfully added! Set a weapon for rank <color=#ce422b>{setCreator[player.userID].Count}</color> or type <color=#ce422b>/set save <name></color> to finish set creation");
                        }
                        else SendReply(player, "<color=#ce422b>You must place an item in your hands</color>");
                    }
                    else SendReply(player, "<color=#ce422b>You are not currently creating a weapon set</color>");
                    return;
                case "cancel":
                    if (setCreator.ContainsKey(player.userID))
                    {
                        setCreator.Remove(player.userID);
                        SendReply(player, "<color=#ce422b>You have cancelled the current set creation</color>");
                    }
                    else SendReply(player, "<color=#ce422b>You are not currently creating a weapon set</color>");
                    return;
                case "save":
                    if (setCreator.ContainsKey(player.userID))
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, "You must specify a name for your set by typing <color=#ce422b>/set save <name></color>");
                            return;
                        }
                        if (setCreator[player.userID].Count < 1)
                        {
                            SendReply(player, "<color=#ce422b>You have not set any weapons yet</color>");
                            return;
                        }
                        if (storedData.gungameSets.ContainsKey(args[1]))
                        {
                            SendReply(player, $"You already have a set with the name <color=#ce422b>{args[1]}</color>");
                            return;
                        }

                        storedData.gungameSets.Add(args[1], setCreator[player.userID]);
                        SaveData();
                        SendReply(player, $"You have successfully saved a new weapon set called <color=#ce422b>{args[1]}</color>");
                        setCreator.Remove(player.userID);
                        return;
                    }
                    else SendReply(player, "<color=#ce422b>You are not currently creating a weapon set</color>");
                    return;
                case "remove":
                    if (args.Length < 2)
                    {
                        SendReply(player, "You must specify a name for the set you wish to delete by typing <color=#ce422b>/set remove <name></color>");
                        return;
                    }
                    if (storedData.gungameSets.ContainsKey(args[1]))
                    {
                        storedData.gungameSets.Remove(args[1]);
                        SaveData();
                        SendReply(player, $"You have successfully removed the weapon set called <color=#ce422b>{args[1]}</color>");
                    }
                    else SendReply(player, $"There is not a set named <color=#ce422b>{args[1]}</color>");
                    return;
                case "list":
                    SendReply(player, $"Available Weapon Sets:\n<color=#ce422b>{storedData.gungameSets.Keys.ToSentence()}</color>");
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Capture the Flag")]
            public CTFConfig CTF { get; set; }

            [JsonProperty(PropertyName = "NPC Survival")]
            public NPCSConfig NPCS { get; set; }

            [JsonProperty(PropertyName = "Balance teams in team events when a player leaves")]
            public bool BalanceOnLeave { get; set; }

            public class NPCSConfig
            {
                [JsonProperty(PropertyName = "Reset player health at the start of each round")]
                public bool ResetHealth { get; set; }

                [JsonProperty(PropertyName = "Zombie kit")]
                public string ZKit { get; set; }

                [JsonProperty(PropertyName = "Zombie health")]
                public int ZHealth { get; set; }

                [JsonProperty(PropertyName = "Scientist kit")]
                public string SKit { get; set; }

                [JsonProperty(PropertyName = "Scientist health")]
                public int SHealth { get; set; }

                [JsonProperty(PropertyName = "Reset player inventory at the start of each round")]
                public bool ResetInv { get; set; }

                [JsonProperty(PropertyName = "Zombie attack damage modifier")]
                public float ZMod { get; set; }

                [JsonProperty(PropertyName = "Scientist attack damage modifier")]
                public float SMod { get; set; }
            }
            public class CTFConfig
            {
                [JsonProperty(PropertyName = "Flag respawn timer (seconds)")]
                public int FlagRespawn { get; set; }

                [JsonProperty(PropertyName = "Use DDraw to show flag positions")]
                public bool UseDDraw { get; set; }

                [JsonProperty(PropertyName = "DDraw update rate")]
                public float DDrawRate { get; set; }
            }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                BalanceOnLeave = false,
                CTF = new ConfigData.CTFConfig
                {
                    FlagRespawn = 30,
                    DDrawRate = 3f,
                    UseDDraw = true
                },
                NPCS = new ConfigData.NPCSConfig
                {
                    ZHealth = 100,
                    ZKit = "",
                    SHealth = 150,
                    SKit = "",
                    ResetInv = true,
                    ZMod = 3f,
                    SMod = 1f
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 1, 40))
            {
                configData.CTF.UseDDraw = baseConfig.CTF.UseDDraw;
                configData.CTF.DDrawRate = baseConfig.CTF.DDrawRate;
            }

            if (configData.Version < new VersionNumber(0, 1, 41))
                configData.NPCS.ResetHealth = baseConfig.NPCS.ResetHealth;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }        
        #endregion

        #region Data Management
        void SaveData() => data.WriteObject(storedData);
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }
        class StoredData
        {
            public Dictionary<string, List<Arena.EventWeapon>> gungameSets = new Dictionary<string, List<Arena.EventWeapon>>();
        }
        #endregion
    }
}
