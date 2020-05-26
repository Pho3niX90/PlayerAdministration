using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Oxide.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Inbound", "Substrata", "0.5.0")]
    [Description("Broadcasts notifications when patrol helicopters, supply drops, cargo ships, etc. are inbound")]

    class Inbound : RustPlugin
    {
        bool hasSRig;
        bool hasLRig;
        Vector3 posSRig;
        Vector3 posLRig;

        private void Init()
        {
            LoadVariables();
            LoadDefaultMessages();
        }

        private void OnServerInitialized()
        {
            if (configData.Grid.ShowRigCargo)
            {
                foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
                {
                    if (monument.name == "OilrigAI")
                    {
                        hasSRig = true;
                        posSRig = monument.transform.position;
                    }
                    if (monument.name == "OilrigAI2")
                    {
                        hasLRig = true;
                        posLRig = monument.transform.position;
                    }
                }
            }
        }

        #region Hooks
        void OnBradleyApcInitialize(BradleyAPC apc)
        {
            if (!configData.Alerts.BradleyAPC) return;
            if (apc == null) return;
            var pos = apc.transform.position;
            string msg = Lang("BradleyAPC", null, GetLocation(pos, null, null));
            Server.Broadcast(msg);
            if (configData.Misc.LogToConsole) Puts(msg);
            if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
        }

        void OnEntitySpawned(CargoPlane plane)
        {
            if (!configData.Alerts.CargoPlane) return;
            if (plane == null) return;
            var srcPos = plane.startPos;
            var destPos = plane.dropPosition;
            string msg = Lang("CargoPlane", null, GetLocation(srcPos, null, null), GetLocationDest(destPos));
            Server.Broadcast(msg);
            if (configData.Misc.LogToConsole) Puts(msg);
            if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
        }

        void OnEntitySpawned(CargoShip ship)
        {
            if (!configData.Alerts.CargoShip) return;
            if (ship == null) return;
            var pos = ship.transform.position;
            string msg = Lang("CargoShip", null, GetLocation(pos, null, null));
            Server.Broadcast(msg);
            if (configData.Misc.LogToConsole) Puts(msg);
            if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
        }

        void OnEntitySpawned(CH47HelicopterAIController ch47)
        {
            if (!configData.Alerts.CH47) return;
            if (ch47 == null) return;
            var srcPos = ch47.transform.position;
            timer.Once(1f, () =>
            {
                var destPos = ch47.GetMoveTarget();
                string msg = Lang("CH47", null, GetLocation(srcPos, null, null), GetLocationDest(destPos));
                Server.Broadcast(msg);
                if (configData.Misc.LogToConsole) Puts(msg);
                if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
            });
        }

        void OnEntitySpawned(HackableLockedCrate crate)
        {
            if (!configData.Alerts.HackableCrate) return;
            if (crate == null) return;
            var pos = crate.transform.position;
            NextTick(() =>
            {
                string msg = Lang("HackableCrate", null, GetLocation(pos, null, crate));
                Server.Broadcast(msg);
                if (configData.Misc.LogToConsole) Puts(msg);
                if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
            });
        }

        void OnEntitySpawned(BaseHelicopter heli)
        {
            if (!configData.Alerts.PatrolHeli) return;
            if (heli == null) return;
            var srcPos = heli.transform.position;
            var destPos = heli.GetComponentInParent<PatrolHelicopterAI>().destination;
            string msg = Lang("PatrolHeli", null, GetLocation(srcPos, null, null), GetLocationDest(destPos));
            Server.Broadcast(msg);
            if (configData.Misc.LogToConsole) Puts(msg);
            if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
        }

        void CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (!configData.Alerts.HackingCrate) return;
            if (player == null || crate == null) return;
            var pos = crate.transform.position;
            string msg = Lang("HackingCrate", player.UserIDString, player.displayName, GetLocation(pos, null, crate));
            Server.Broadcast(msg);
            if (configData.Misc.LogToConsole) Puts(msg);
            if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
        }

        void OnExplosiveThrown(BasePlayer player, SupplySignal signal)
        {
            if (!configData.Alerts.SupplySignal) return;
            if (player == null || signal == null) return;
            var pos = player.transform.position;
            string msg = Lang("SupplySignal", player.UserIDString, player.displayName, GetLocation(pos, player, null));
            Server.Broadcast(msg);
            if (configData.Misc.LogToConsole) Puts(msg);
            if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
        }

        void OnExplosiveDropped(BasePlayer player, SupplySignal signal)
        {
            if (!configData.Alerts.SupplySignal) return;
            if (player == null || signal == null) return;
            var pos = player.transform.position;
            string msg = Lang("SupplySignal", player.UserIDString, player.displayName, GetLocation(pos, player, null));
            Server.Broadcast(msg);
            if (configData.Misc.LogToConsole) Puts(msg);
            if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
        }

        void OnEntitySpawned(SupplyDrop drop)
        {
            if (!configData.Alerts.SupplyDrop) return;
            if (drop == null) return;
            var pos = drop.transform.position;
            string msg = Lang("SupplyDrop", null, GetLocation(pos, null, null));
            Server.Broadcast(msg);
            if (configData.Misc.LogToConsole) Puts(msg);
            if (configData.Misc.LogToFile) LogToFile("log", $"[{DateTime.Now.ToString("HH:mm:ss")}] {msg}", this);
        }
        #endregion

        #region Helpers
        string GetLocation(Vector3 pos, BasePlayer player, BaseEntity entity)
        {
            if (configData.Grid.ShowGrid && configData.Coordinates.ShowCoords)
            {
                string Grid = GetGrid(pos, player, entity)+" ";
                string Coords = pos.ToString();
                string posStr = Grid+Coords;
                if (Grid == null || !Regex.IsMatch(Grid, "^[A-Z]")) posStr = Coords.Replace("(", string.Empty).Replace(")", string.Empty);
                return Lang("Location", null, posStr);
            }

            if (configData.Grid.ShowGrid)
            {
                string Grid = GetGrid(pos, player, entity);
                if (Grid == null || !Regex.IsMatch(Grid, "^[A-Z]")) return string.Empty;
                return Lang("Location", null, Grid);
            }

            if (configData.Coordinates.ShowCoords)
            {
                string Coords = pos.ToString().Replace("(", string.Empty).Replace(")", string.Empty);
                return Lang("Location", null, Coords);
            }
            return string.Empty;
        }

        string GetLocationDest(Vector3 pos)
        {
            if (configData.Grid.ShowGrid && configData.Coordinates.ShowCoords)
            {
                if (configData.Grid.ShowDestination && configData.Coordinates.ShowDestination)
                {
                    string Grid = GetGrid(pos, null, null)+" ";
                    string Coords = pos.ToString();
                    string posStr = Grid+Coords;
                    if (Grid == null || !Regex.IsMatch(Grid, "^[A-Z]")) posStr = Coords.Replace("(", string.Empty).Replace(")", string.Empty);
                    return Lang("LocationDestination", null, posStr);
                }

                if (configData.Grid.ShowDestination)
                {
                    string Grid = GetGrid(pos, null, null);
                    if (Grid == null || !Regex.IsMatch(Grid, "^[A-Z]")) return string.Empty;
                    return Lang("LocationDestination", null, Grid);
                }

                if (configData.Coordinates.ShowDestination)
                {
                    string Coords = pos.ToString().Replace("(", string.Empty).Replace(")", string.Empty);
                    return Lang("LocationDestination", null, Coords);
                }
            }

            if (configData.Grid.ShowGrid && configData.Grid.ShowDestination)
            {
                string Grid = GetGrid(pos, null, null);
                if (Grid == null || !Regex.IsMatch(Grid, "^[A-Z]")) return string.Empty;
                return Lang("LocationDestination", null, Grid);
            }

            if (configData.Coordinates.ShowCoords && configData.Coordinates.ShowDestination)
            {
                string Coords = pos.ToString().Replace("(", string.Empty).Replace(")", string.Empty);
                return Lang("LocationDestination", null, Coords);
            }
            return string.Empty;
        }

        string GetGrid(Vector3 pos, BasePlayer player, BaseEntity entity)
        {
			var x = Mathf.Floor((pos.x+(ConVar.Server.worldsize/2)) / 146.3f); // Credit: yetzt
			var z = (Mathf.Floor(ConVar.Server.worldsize/146.3f)-1)-Mathf.Floor((pos.z+(ConVar.Server.worldsize/2)) / 146.3f); // Credit: yetzt

            string Grid = $"{GetGridLetter((int)(x))}{z}";

            if (!configData.Grid.ShowRigCargo) return Grid;

            // On Cargo Ship
            if ((player != null && player.GetComponentInParent<CargoShip> ()) || (entity != null && entity.GetComponentInParent<CargoShip> ())) return "Cargo Ship";

            // On Oil Rigs
            if (hasSRig)
            {
                float xDist = Mathf.Abs(posSRig.x - pos.x);
                float zDist = Mathf.Abs(posSRig.z - pos.z);
                if (xDist <= 60f && zDist <= 60f) return "Oil Rig";
            }
            if (hasLRig)
            {
                float xDist = Mathf.Abs(posLRig.x - pos.x);
                float zDist = Mathf.Abs(posLRig.z - pos.z);
                if (xDist <= 75f && zDist <= 75f) return "Large Oil Rig";
            }
			return Grid;
		}

		public static string GetGridLetter(int num) // Credit: Jake_Rich
		{
			int num2 = Mathf.FloorToInt((float)(num / 26));
			int num3 = num % 26;
			string text = string.Empty;
			if (num2 > 0)
			{
				for (int i = 0; i < num2; i++)
				{
					text += Convert.ToChar(65 + i);
				}
			}
			return text + Convert.ToChar(65 + num3).ToString();
		}
        #endregion

        #region Configuration
        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Alerts (true/false)")]
            public Alerts Alerts { get; set; }
            [JsonProperty(PropertyName = "Coordinates (true/false)")]
            public Coordinates Coordinates { get; set; }
            [JsonProperty(PropertyName = "Grid (true/false)")]
            public Grid Grid { get; set; }
            [JsonProperty(PropertyName = "Misc (true/false)")]
            public Misc Misc { get; set; }
        }

        class Alerts
        {
            [JsonProperty(PropertyName = "Bradley APC Alerts")]
            public bool BradleyAPC { get; set; }
            [JsonProperty(PropertyName = "Cargo Plane Alerts")]
            public bool CargoPlane { get; set; }
            [JsonProperty(PropertyName = "Cargo Ship Alerts")]
            public bool CargoShip { get; set; }
            [JsonProperty(PropertyName = "CH47 Chinook Alerts")]
            public bool CH47 { get; set; }
            [JsonProperty(PropertyName = "Hackable Crate Alerts")]
            public bool HackableCrate { get; set; }
            [JsonProperty(PropertyName = "Patrol Helicopter Alerts")]
            public bool PatrolHeli { get; set; }
            [JsonProperty(PropertyName = "Player Hacking Crate Alerts")]
            public bool HackingCrate { get; set; }
            [JsonProperty(PropertyName = "Player Supply Signal Alerts")]
            public bool SupplySignal { get; set; }
            [JsonProperty(PropertyName = "Supply Drop Alerts")]
            public bool SupplyDrop { get; set; }
        }

        class Coordinates
        {
            [JsonProperty(PropertyName = "Show Coordinates")]
            public bool ShowCoords { get; set; }
            [JsonProperty(PropertyName = "Show Coordinates - Destination")]
            public bool ShowDestination { get; set; }
        }

        class Grid
        {
            [JsonProperty(PropertyName = "Show Grid")]
            public bool ShowGrid { get; set; }
            [JsonProperty(PropertyName = "Show Grid - Destination")]
            public bool ShowDestination { get; set; }
            [JsonProperty(PropertyName = "Show Oil Rig / Cargo Ship Labels")]
            public bool ShowRigCargo { get; set; }
        }

        class Misc
        {
            [JsonProperty(PropertyName = "Log To Console")]
            public bool LogToConsole { get; set; }
            [JsonProperty(PropertyName = "Log To File")]
            public bool LogToFile { get; set; }
        }

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Alerts = new Alerts
                {
                    BradleyAPC = true,
                    CargoPlane = true,
                    CargoShip = true,
                    CH47 = true,
                    HackableCrate = true,
                    PatrolHeli = true,
                    HackingCrate = true,
                    SupplySignal = true,
                    SupplyDrop = true
                },
                Coordinates = new Coordinates
                {
                    ShowCoords = false,
                    ShowDestination = false
                },
                Grid = new Grid
                {
                    ShowGrid = true,
                    ShowDestination = true,
                    ShowRigCargo = true
                },
                Misc = new Misc
                {
                    LogToConsole = false,
                    LogToFile = false
                }
            };
            SaveConfig(config);
        }

        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();

        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Localization
		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				{"BradleyAPC", "Bradley APC inbound{0}"},
				{"CargoPlane", "Cargo Plane inbound{0}{1}"},
                {"CargoShip", "Cargo Ship inbound{0}"},
                {"CH47", "Chinook inbound{0}{1}"},
                {"HackableCrate", "Hackable Crate has spawned{0}"},
                {"PatrolHeli", "Patrol Helicopter inbound{0}{1}"},
                {"HackingCrate", "{0} is hacking a locked crate{1}"},
                {"SupplySignal", "{0} has deployed a supply signal{1}"},
                {"SupplyDrop", "Supply Drop has dropped{0}"},
                {"Location", " at {0}"},
                {"LocationDestination", " and headed to {0}"}
			}, this);
		}

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion
    }
}