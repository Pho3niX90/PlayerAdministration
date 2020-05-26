using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Stash Marker", "supreme", "1.1.0")]
    [Description("Marks the hidden stashes on the ingame map")]
    public class StashMarker : RustPlugin
    {
        //credits to nivex for improvements
        const string permUse = "stashmarker.use";
        const string permAdmin = "stashmarker.admin";

        #region Class Variables
        private readonly Hash<ulong, List<MapMarkerGenericRadius>> _mapMarker = new Hash<ulong, List<MapMarkerGenericRadius>>();
        private WaitForEndOfFrame _cachedWaitForEndOfFrame = new WaitForEndOfFrame();
        private HashSet<StashContainer> _stashes = new HashSet<StashContainer>();
        private Coroutine _stashRoutine;
        #endregion

        #region Configuration

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Marker Radius")]
            public float markerRadius = 0.1f;

            [JsonProperty(PropertyName = "Marker Alpha")]
            public float markerAlpha = 0.8f;

            [JsonProperty(PropertyName = "Marker Color")]
            public string markerColor = "ACFA58";

            [JsonProperty(PropertyName = "Marker Color Outline")]
            public string markerColorOutline = "000000";

        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region Setup & Loading

        private void OnServerInitialized()
        {
            Subscribe(nameof(OnEntitySpawned));
            Subscribe(nameof(OnEntityKill));
            Subscribe(nameof(OnPlayerSleepEnded));
            Subscribe(nameof(CanNetworkTo));
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                OnPlayerSleepEnded(player);
            }
            _stashRoutine = ServerMgr.Instance.StartCoroutine(StashRoutine());
        }

        IEnumerator StashRoutine()
        {
            int total = 0;

            foreach (var e in BaseNetworkable.serverEntities)
            {
                if (e is StashContainer)
                {
                    _stashes.Add(e as StashContainer);
                }

                if (++total % 50 == 0)
                {
                    yield return _cachedWaitForEndOfFrame;
                }
            }

            _stashRoutine = null;
        }

        void Init()
        {
            Unsubscribe(nameof(OnEntitySpawned));
            Unsubscribe(nameof(OnEntityKill));
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(CanNetworkTo));
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permAdmin, this);
        }

        private void Unload()
        {
            if (_stashRoutine != null)
            {
                ServerMgr.Instance.StopCoroutine(_stashRoutine);
            }
            foreach (MapMarkerGenericRadius marker in _mapMarker.SelectMany(mm => mm.Value))
            {
                marker.Kill();
            }
        }

        #endregion

        #region uMod Hooks

        private void OnEntitySpawned(StashContainer stash)
        {
            if (stash != null)
            {
                _stashes.Add(stash);
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse)) return;
            List<MapMarkerGenericRadius> playerMarkers;
            if (!_mapMarker.TryGetValue(player.userID, out playerMarkers))
                _mapMarker.Add(player.userID, playerMarkers = new List<MapMarkerGenericRadius>());

            foreach (StashContainer stash in _stashes.Where(s => s.OwnerID == player.userID))
                if (stash.IsHidden())
                {
                    MapMarkerGenericRadius marker = GetOrAddMarker(player, stash.transform.position);
                    marker.SendUpdate();
                }
        }

        private void OnEntityKill(StashContainer stash)
        {
            _stashes.Remove(stash);
            List<MapMarkerGenericRadius> playerMarkers = _mapMarker[stash.OwnerID];
            MapMarkerGenericRadius marker = playerMarkers?.FirstOrDefault(m => m.transform.position == stash.transform.position);

            if (marker == null || marker.IsDestroyed) return;
            marker.Kill();
            playerMarkers.Remove(marker);
        }

        void CanSeeStash(BasePlayer player, StashContainer stash)
        {
            List<MapMarkerGenericRadius> playerMarkers;
            if (!_mapMarker.TryGetValue(player.userID, out playerMarkers))
                _mapMarker.Add(player.userID, playerMarkers = new List<MapMarkerGenericRadius>());

            MapMarkerGenericRadius marker = playerMarkers?.FirstOrDefault(m => m.transform.position == stash.transform.position);

            if (marker == null || marker.IsDestroyed) return;
            marker.Kill();
            playerMarkers.Remove(marker);
        }

        void CanHideStash(BasePlayer player, StashContainer stash)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse)) return;
            MapMarkerGenericRadius marker = GetOrAddMarker(player, stash.transform.position);
            marker.SendUpdate();
        }

        private object CanNetworkTo(MapMarkerGenericRadius marker, BasePlayer target)
        {
            if (marker == null) return null;

            List<MapMarkerGenericRadius> playerMarkers;
            if (!_mapMarker.TryGetValue(marker.OwnerID, out playerMarkers) || !playerMarkers.Contains(marker)) return null;

            return marker.OwnerID == target.userID || permission.UserHasPermission(target.UserIDString, permAdmin) ? (object)true : false;
        }

        #endregion

        #region Marker Methods

        private MapMarkerGenericRadius GetOrAddMarker(BasePlayer player, Vector3 pos)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse)) return null;
            List<MapMarkerGenericRadius> playerMarkers;
            if (!_mapMarker.TryGetValue(player.userID, out playerMarkers))
                _mapMarker.Add(player.userID, playerMarkers = new List<MapMarkerGenericRadius>());
            MapMarkerGenericRadius marker = playerMarkers.FirstOrDefault(m => m.transform.position == pos);
            marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", pos) as MapMarkerGenericRadius;
            if (marker == null) return null;
            marker.alpha = _config.markerAlpha;
            string colorMarker = _config.markerColor;
            string colorOutline = _config.markerColorOutline;
            ColorUtility.TryParseHtmlString($"#{colorMarker}", out marker.color1);
            ColorUtility.TryParseHtmlString($"#{colorOutline}", out marker.color2);
            marker.radius = _config.markerRadius;
            marker.OwnerID = player.userID;
            playerMarkers.Add(marker);
            marker.Spawn();
            return marker;
        }

        #endregion
    }
}