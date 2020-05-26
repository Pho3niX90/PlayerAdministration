//Requires: ArenaUI
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Linq;
using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("ArenaStatistics", "k1lly0u", "0.1.54")]
    [Description("Collects and manages all event related statistics")]
    class ArenaStatistics : RustPlugin
    {
        #region Fields
        [PluginReference] Arena Arena;        

        StoredData storedData;
        private DynamicConfigFile data;

        private Hash<ulong, ScoreData> dataCache = new Hash<ulong, ScoreData>();
        private Hash<string, EventData> eventCache = new Hash<string, EventData>();
        private Hash<ulong, double> headShotTimes = new Hash<ulong, double>();
        private List<ulong> rankCache = new List<ulong>();
        private ScoreData globalCache = new ScoreData();
        #endregion

        #region Oxide Hooks        
        private void OnServerInitialized()
        {
            Arena.Statistics = this;
            data = Interface.Oxide.DataFileSystem.GetFile("Arena/statistics_data");
            LoadData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            ScoreData data;
            if (dataCache.TryGetValue(player.userID, out data))
                data.SetPlayerName(StripTags(player.displayName));
        }

        private void OnServerSave() => SaveData();

        private void Unload()
        {
            if (Arena != null)
                Arena.isUnloading = true;

            SaveData();
        }
        #endregion

        #region Hooks        
        public void OnEventPlayerDeath(Arena.EventPlayer eventPlayer, HitInfo info, Arena.ArenaData.EventConfig config)
        {
            if (eventPlayer == null || eventPlayer.player == null)
                return;

            AddScore(eventPlayer.player.userID, ScoreType.Death, config.eventType, config.eventName);

            List<ulong> contributors = eventPlayer.GetDamageContributors();

            ulong killerId = 0U;
            if (contributors.Count > 1)
            {
                killerId = contributors.Last();
                for (int i = 0; i < contributors.Count - 1; i++)
                    AddScore(contributors[i], ScoreType.Assist, config.eventType, config.eventName);
            }
            else if (contributors.Count == 1)
                killerId = contributors[0];

            if (killerId != 0U)
            {
                AddScore(killerId, ScoreType.Kill, config.eventType, config.eventName);

                if (info != null && info.damageTypes.IsMeleeType())
                    AddScore(killerId, ScoreType.Melee, config.eventType, config.eventName);
                
                Arena.IssueReward(killerId, Arena.RewardType.Kill, config);
            }
        }

        public void OnNPCPlayerDeath(Arena.EventPlayer eventPlayer, HitInfo info, Arena.ArenaData.EventConfig config)
        {
            if (eventPlayer == null || eventPlayer.player == null)
                return;

            ulong killerId = eventPlayer.player.userID;

            AddScore(killerId, ScoreType.Kill, config.eventType, config.eventName);

            if (info != null && info.damageTypes.IsMeleeType())
                AddScore(killerId, ScoreType.Melee, config.eventType, config.eventName);

            if (info.isHeadshot)
                AddScore(killerId, ScoreType.Headshot, config.eventType, config.eventName);

            Arena.IssueReward(killerId, Arena.RewardType.Kill, config);
        }

        public void OnEventLose(List<ulong> losers, Arena.ArenaData.EventConfig config)
        {
            foreach (var playerId in losers)
                AddScore(playerId, ScoreType.Loss, config.eventType, config.eventName);
        }

        public void OnEventWin(List<ulong> winners, Arena.ArenaData.EventConfig config)
        {
            foreach (var playerId in winners)
            {
                AddScore(playerId, ScoreType.Win, config.eventType, config.eventName);
                Arena.IssueReward(playerId, Arena.RewardType.Win, config);
            }
        }

        public void OnGamePlayed(ulong playerId, Arena.ArenaData.EventConfig config)
        {
            AddScore(playerId, ScoreType.Played, config.eventType, config.eventName);
            AddPlayed(playerId, config.eventType, config.eventName);            
        }

        public void OnGameFinished(string eventType, string eventName)
        {
            CheckEventData($"({eventType}) {eventName}");
            eventCache[$"({eventType}) {eventName}"].AddMatchPlayed();
        }

        public void OnHeadshot(ulong playerId, Arena.ArenaData.EventConfig config)
        {
            double time = UnityEngine.Time.realtimeSinceStartup;
            if (!headShotTimes.ContainsKey(playerId) || headShotTimes[playerId] < time)
            {
                AddScore(playerId, ScoreType.Headshot, config.eventType, config.eventName);
                Arena.IssueReward(playerId, Arena.RewardType.Headshot, config);
                headShotTimes[playerId] = time + 0.5f;
            }
        }

        public void OnNPCKill(ulong playerId, Arena.ArenaData.EventConfig config) => AddScore(playerId, ScoreType.Kill, config.eventType, config.eventName);
        #endregion

        #region Functions
        private void AddScore(ulong playerId, ScoreType type, string eventType, string eventName, int amount = 1)
        {
            CheckPlayerData(playerId);
            dataCache[playerId].AddScore(type, amount);
            globalCache.AddScore(type, amount);
            AddEventScore(eventType, eventName, type, amount);
        }

        private void AddPlayed(ulong playerId, string eventType, string eventName)
        {
            CheckPlayerData(playerId);
            dataCache[playerId].AddPlayed(eventType, eventName);
            globalCache.AddPlayed(eventType, eventName);
        }

        private void AddEventScore(string eventType, string eventName, ScoreType type, int amount = 1)
        {
            CheckEventData($"({eventType}) {eventName}");
            eventCache[$"({eventType}) {eventName}"].AddScore(type, amount);
        }
       
        private void CheckPlayerData(ulong playerId)
        {
            ScoreData data;
            if (!dataCache.TryGetValue(playerId, out data))                           
                dataCache.Add(playerId, new ScoreData(StripTags(covalence.Players.All.FirstOrDefault(x => x.Id == playerId.ToString()).Name ?? "Unknown Player")));                        
        }

        private void CheckEventData(string eventName)
        {
            EventData data;
            if (!eventCache.TryGetValue(eventName, out data))            
                eventCache.Add(eventName, new EventData());             
        }

        private string StripTags(string str)
        {
            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                str = str.Substring(str.IndexOf("]") + 1).Trim();

            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                StripTags(str);

            return str;
        }
        #endregion

        #region API
        public ScoreData GetPlayerData(ulong playerId)
        {
            ScoreData data;
            if (dataCache.TryGetValue(playerId, out data))
                return data;
            return null;           
        }

        public ScoreData GetGlobalData() => globalCache;

        public string GetPlayerRank(ulong playerId) => rankCache.Contains(playerId) ? (rankCache.IndexOf(playerId) + 1).ToString() : "Unranked";

        public List<ulong> GetRankList() => rankCache;
        public Hash<string, EventData> GetEventData() => eventCache;
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Point values assigned to various scoreboard statistics to determine a players rank")]
            public Dictionary<ScoreType, float> Scores { get; set; }
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
                Scores = new Dictionary<ScoreType, float>
                {
                    [ScoreType.Assist] = 0.5f,
                    [ScoreType.Death] = -1f,
                    [ScoreType.Headshot] = 1.5f,
                    [ScoreType.Kill] = 1f,
                    [ScoreType.Loss] = -1f,
                    [ScoreType.Melee] = 1.2f,
                    [ScoreType.Played] = 1f,
                    [ScoreType.Win] = 2f,
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        public enum ScoreType { Kill, Death, Headshot, Melee, Assist, Win, Loss, Played }

        private void SaveData()
        {
            rankCache = storedData.players.OrderByDescending(x => x.Value.GetPlayerScore()).Select(y => y.Key).ToList();
            storedData.ranks = rankCache;
            storedData.players = dataCache;
            storedData.events = eventCache;
            storedData.global = globalCache;

            data.WriteObject(storedData);
        }

        private void LoadData()
        {
            try
            {                
                storedData = data.ReadObject<StoredData>();
                dataCache = storedData.players;
                eventCache = storedData.events;
                globalCache = storedData.global;
                rankCache = storedData.ranks;
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        class StoredData
        {
            public ScoreData global = new ScoreData();
            public Hash<ulong, ScoreData> players = new Hash<ulong, ScoreData>();
            public Hash<string, EventData> events = new Hash<string, EventData>();
            public List<ulong> ranks = new List<ulong>();
        }

        public class EventData
        {
            public Dictionary<ScoreType, int> scores;
            public int matchesPlayed;

            public EventData()
            {
                scores = new Dictionary<ScoreType, int>()
                {
                    [ScoreType.Kill] = 0,
                    [ScoreType.Death] = 0,
                    [ScoreType.Assist] = 0,
                    [ScoreType.Headshot] = 0,
                    [ScoreType.Melee] = 0,
                    [ScoreType.Win] = 0,
                    [ScoreType.Loss] = 0,
                    [ScoreType.Played] = 0
                };
            }
            public void AddScore(ScoreType type, int amount = 1) => scores[type] += amount;
            public void AddMatchPlayed() => matchesPlayed += 1;
        }

        public class ScoreData
        {
            public string playerName;
            public int playerScore;

            public Hash<string, int> gamesPlayed;
            public Dictionary<ScoreType, int> scores;

            public ScoreData()
            {
                gamesPlayed = new Hash<string, int>();
                scores = new Dictionary<ScoreType, int>()
                {
                    [ScoreType.Kill] = 0,
                    [ScoreType.Death] = 0,
                    [ScoreType.Assist] = 0,
                    [ScoreType.Headshot] = 0,
                    [ScoreType.Melee] = 0,
                    [ScoreType.Win] = 0,
                    [ScoreType.Loss] = 0,
                    [ScoreType.Played] = 0
                };
            }  
            public ScoreData(string playerName)
            {
                this.playerName = playerName;
                gamesPlayed = new Hash<string, int>();
                scores = new Dictionary<ScoreType, int>()
                {
                    [ScoreType.Kill] = 0,
                    [ScoreType.Death] = 0,
                    [ScoreType.Assist] = 0,
                    [ScoreType.Headshot] = 0,
                    [ScoreType.Melee] = 0,
                    [ScoreType.Win] = 0,
                    [ScoreType.Loss] = 0,
                    [ScoreType.Played] = 0
                };
            }          
            
            private void CalculateScore()
            {
                var values = ArenaEvents.Statistics.configData.Scores;

                float tempScore = 0;
                foreach(var score in scores)                
                    tempScore += scores[score.Key] * values[score.Key];                
               
                playerScore = Convert.ToInt32(tempScore);
            }

            public void SetPlayerName(string newName) => playerName = newName;
            public string GetPlayerName() => playerName;

            public void AddScore(ScoreType type, int amount = 1) => scores[type] += amount;
            public void AddPlayed(string eventType, string eventName)
            {
                if (!gamesPlayed.ContainsKey($"({eventType}) {eventName}"))
                    gamesPlayed.Add($"({eventType}) {eventName}", 1);               
                else gamesPlayed[$"({eventType}) {eventName}"] += 1;

                CalculateScore();
            }

            public int GetScore(ScoreType type) => scores[type];
            public int GetPlayerScore() => playerScore;

            public Dictionary<ScoreType, int> GetScores() => scores;
            public Hash<string, int> GetGamesPlayed() => gamesPlayed;           
        }
        #endregion
    }
}
