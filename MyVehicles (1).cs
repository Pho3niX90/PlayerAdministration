//#define DEBUG
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core;
using Convert = System.Convert;
using System;
using System.Linq;
using Rust;

namespace Oxide.Plugins
{
    [Info("My Vehicles", "RFC1920/bsdinis", "0.2.4")]
    [Description("Spawn vehicles")]
    public class MyVehicles : RustPlugin
    {
        string Prefix = "[My Vehicles]: ";
        const string prefabmini = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        const string prefabscrap = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";
        const string prefabboat = "assets/content/vehicles/boats/rowboat/rowboat.prefab";
        const string prefabrhib = "assets/content/vehicles/boats/rhib/rhib.prefab";
        const string prefabhab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab";
        const string prefabhorse = "assets/rust.ai/nextai/testridablehorse.prefab";
        const string prefabchinook = "assets/prefabs/npc/ch47/ch47.entity.prefab";
        const string prefabsedan = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";

        private bool ConfigChanged;
        private bool useCooldown = true;
        private bool vehicleDecay = true;
        private bool allowWhenBlocked = false;
        private bool killOnSleep = false;
        private bool allowFuelIfUnlimited = false;
        private bool ShowChinookMapMarker = false;
        private bool allowSpawnIfAlreadyOwnsVehicle = false;
        private float miniFuelConsumption = 0.5f;
        private float scrapFuelConsumption = 0.5f;
        private float boatFuelConsumption = 0.1f;
        private float rhibFuelConsumption = 0.25f;
        private float habFuelConsumption = 0.25f;
        private float maxdistancemini = 50f;
        private float maxdistancescrap = 50f;
        private float maxdistanceboat = 50f;
        private float maxdistancerhib = 50f;
        private float maxdistancehab = 50f;
        private float maxdistancehorse = 100f;
        private float maxdistancechinook = 50f;
        private float maxdistancesedan = 100f;
        private float fetchdistancemini = 50f;
        private float fetchdistancescrap = 50f;
        private float fetchdistanceboat = 50f;
        private float fetchdistancerhib = 50f;
        private float fetchdistancehab = 50f;
        private float fetchdistancehorse = 100f;
        private float fetchdistancechinook = 50f;
        private float fetchdistancesedan = 100f;
        const string MyVehiclesAdmin = "myvehicles.admin";
        const string MinicopterSpawn = "myvehicles.minispawn";
        const string Minicopter1Spawn = "myvehicles.mini1spawn";
        const string Minicopter2Spawn = "myvehicles.mini2spawn";
        const string Minicopter3Spawn = "myvehicles.mini3spawn";
        const string CombatMinicopterSpawn = "myvehicles.combatminispawn";
        const string CCTVMinicopterSpawn = "myvehicles.cctvminispawn";
        const string MinicopterFetch = "myvehicles.minifetch";
        const string MinicopterWhere = "myvehicles.miniwhere";
        const string MinicopterCooldown = "myvehicles.minicooldown";
        const string MinicopterUnlimited = "myvehicles.miniunlimited";
        const string ScrapSpawn = "myvehicles.scrapspawn";
        const string PianoScrapSpawn = "myvehicles.pianoscrapspawn";
        const string ScrapFetch = "myvehicles.scrapfetch";
        const string ScrapWhere = "myvehicles.scrapwhere";
        const string ScrapCooldown = "myvehicles.scrapcooldown";
        const string ScrapUnlimited = "myvehicles.scrapunlimited";
        const string BoatSpawn = "myvehicles.boatspawn";
        const string BoatFetch = "myvehicles.boatfetch";
        const string BoatWhere = "myvehicles.boatwhere";
        const string BoatCooldown = "myvehicles.boatcooldown";
        const string BoatUnlimited = "myvehicles.boatunlimited";
        const string RHIBSpawn = "myvehicles.rhibspawn";
        const string PianoRHIBSpawn = "myvehicles.pianorhibspawn";
        const string RHIBFetch = "myvehicles.rhibfetch";
        const string RHIBWhere = "myvehicles.rhibwhere";
        const string RHIBCooldown = "myvehicles.rhibcooldown";
        const string RHIBUnlimited = "myvehicles.rhibunlimited";
        const string HABSpawn = "myvehicles.habspawn";
        const string HABFetch = "myvehicles.habfetch";
        const string HABWhere = "myvehicles.habwhere";
        const string HABCooldown = "myvehicles.habcooldown";
        const string HABUnlimited = "myvehicles.habunlimited";
        const string HorseSpawn = "myvehicles.horsespawn";
        const string HorseFetch = "myvehicles.horsefetch";
        const string HorseWhere = "myvehicles.horsewhere";
        const string HorseCooldown = "myvehicles.horsecooldown";
        const string ChinookSpawn = "myvehicles.chinookspawn";
        const string ChinookFetch = "myvehicles.chinookfetch";
        const string ChinookWhere = "myvehicles.chinookwhere";
        const string ChinookCooldown = "myvehicles.chinookcooldown";
        const string SedanSpawn = "myvehicles.sedanspawn";
        const string SedanFetch = "myvehicles.sedanfetch";
        const string SedanWhere = "myvehicles.sedanwhere";
        const string SedanCooldown = "myvehicles.sedancooldown";

        double cooldownmini = 60;
        double cooldownscrap = 60;
        double cooldownboat = 30;
        double cooldownrhib = 45;
        double cooldownhab = 30;
        double cooldownhorse = 15;
        double cooldownchinook = 90;
        double cooldownsedan = 90;

        public Dictionary<ulong, MiniCopter> baseplayerminicop = new Dictionary<ulong, MiniCopter>();
        public Dictionary<ulong, ScrapTransportHelicopter> baseplayerscrapcop = new Dictionary<ulong, ScrapTransportHelicopter>();
        public Dictionary<ulong, MotorRowboat> baseplayerboatcop = new Dictionary<ulong, MotorRowboat>();
        public Dictionary<ulong, RHIB> baseplayerrhibcop = new Dictionary<ulong, RHIB>();
        public Dictionary<ulong, HotAirBalloon> baseplayerhabcop = new Dictionary<ulong, HotAirBalloon>();
        public Dictionary<ulong, RidableHorse> baseplayerhorsecop = new Dictionary<ulong, RidableHorse>();
        public Dictionary<ulong, CH47Helicopter> baseplayerchinookcop = new Dictionary<ulong, CH47Helicopter>();
        public Dictionary<ulong, BaseCar> baseplayersedancop = new Dictionary<ulong, BaseCar>();
        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        class StoredData
        {
            public Dictionary<ulong, uint> playerminiID = new Dictionary<ulong, uint>();
            public Dictionary<ulong, double> playerminicounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, uint> playerscrapID = new Dictionary<ulong, uint>();
            public Dictionary<ulong, double> playerscrapcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, uint> playerboatID = new Dictionary<ulong, uint>();
            public Dictionary<ulong, double> playerboatcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, uint> playerrhibID = new Dictionary<ulong, uint>();
            public Dictionary<ulong, double> playerrhibcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, uint> playerhabID = new Dictionary<ulong, uint>();
            public Dictionary<ulong, double> playerhabcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, uint> playerhorseID = new Dictionary<ulong, uint>();
            public Dictionary<ulong, double> playerhorsecounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, uint> playerchinookID = new Dictionary<ulong, uint>();
            public Dictionary<ulong, double> playerchinookcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, uint> playersedanID = new Dictionary<ulong, uint>();
            public Dictionary<ulong, double> playersedancounter = new Dictionary<ulong, double>();
            public StoredData()
            {
            }
        }
        private StoredData storedData;
        private bool HasPermission(ConsoleSystem.Arg arg, string permname) => (arg.Connection.player as BasePlayer) == null ? true : permission.UserHasPermission((arg.Connection.player as BasePlayer).UserIDString, permname);

        #region loadunload
        void OnNewSave()
        {
            storedData = new StoredData();
            SaveData();
        }

        void Loaded()
        {
            LoadVariables();
            permission.RegisterPermission(MyVehiclesAdmin, this);
            permission.RegisterPermission(MinicopterSpawn, this);
            permission.RegisterPermission(Minicopter1Spawn, this);
            permission.RegisterPermission(Minicopter2Spawn, this);
            permission.RegisterPermission(Minicopter3Spawn, this);
            permission.RegisterPermission(CombatMinicopterSpawn, this);
            permission.RegisterPermission(CCTVMinicopterSpawn, this);
            permission.RegisterPermission(MinicopterFetch, this);
            permission.RegisterPermission(MinicopterWhere, this);
            permission.RegisterPermission(MinicopterCooldown, this);
            permission.RegisterPermission(MinicopterUnlimited, this);
            permission.RegisterPermission(ScrapSpawn, this);
            permission.RegisterPermission(PianoScrapSpawn, this);
            permission.RegisterPermission(ScrapFetch, this);
            permission.RegisterPermission(ScrapWhere, this);
            permission.RegisterPermission(ScrapCooldown, this);
            permission.RegisterPermission(ScrapUnlimited, this);
            permission.RegisterPermission(BoatSpawn, this);
            permission.RegisterPermission(BoatFetch, this);
            permission.RegisterPermission(BoatWhere, this);
            permission.RegisterPermission(BoatCooldown, this);
            permission.RegisterPermission(BoatUnlimited, this);
            permission.RegisterPermission(RHIBSpawn, this);
            permission.RegisterPermission(PianoRHIBSpawn, this);
            permission.RegisterPermission(RHIBFetch, this);
            permission.RegisterPermission(RHIBWhere, this);
            permission.RegisterPermission(RHIBCooldown, this);
            permission.RegisterPermission(RHIBUnlimited, this);
            permission.RegisterPermission(HABSpawn, this);
            permission.RegisterPermission(HABFetch, this);
            permission.RegisterPermission(HABWhere, this);
            permission.RegisterPermission(HABCooldown, this);
            permission.RegisterPermission(HABUnlimited, this);
            permission.RegisterPermission(HorseSpawn, this);
            permission.RegisterPermission(HorseFetch, this);
            permission.RegisterPermission(HorseWhere, this);
            permission.RegisterPermission(HorseCooldown, this);
            permission.RegisterPermission(ChinookSpawn, this);
            permission.RegisterPermission(ChinookFetch, this);
            permission.RegisterPermission(ChinookWhere, this);
            permission.RegisterPermission(ChinookCooldown, this);
            permission.RegisterPermission(SedanSpawn, this);
            permission.RegisterPermission(SedanFetch, this);
            permission.RegisterPermission(SedanWhere, this);
            permission.RegisterPermission(SedanCooldown, this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }

        void Unload()
        {
            SaveData();
        }
        #endregion

        #region MESSAGES
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"AlreadyMiniMsg", "You already have a Minicopter.\nUse command '/nomini' to remove it."},
                {"SpawnedMiniMsg", "Your Minicopter has spawned !\nUse command '/nomini' to remove it."},
                {"KilledMiniMsg", "Your Minicopter has been removed/killed."},
                {"NoFoundMiniMsg", "You do not have an active Minicopter."},
                {"FoundMiniMsg", "Your Minicopter is located at {0}."},
                {"CooldownMiniMsg", "You must wait {0} seconds before spawning a new Minicopter."},
                {"DistanceMiniMsg", "You must be within {0} meters of your Minicopter."},
                {"AlreadyScrapMsg", "You already have a Scrap Helicopter.\nUse command '/noheli' to remove it."},
                {"SpawnedScrapMsg", "Your Scrap Helicopter has spawned !\nUse command '/noheli' to remove it."},
                {"KilledScrapMsg", "Your Scrap Helicopter has been removed/killed."},
                {"NoFoundScrapMsg", "You do not have an active Scrap Helicopter."},
                {"FoundScrapMsg", "Your Scrap Helicopter is located at {0}."},
                {"CooldownScrapMsg", "You must wait {0} seconds before spawning a new Scrap Helicopter."},
                {"DistanceScrapMsg", "You must be within {0} meters of your Scrap Helicopter."},
                {"AlreadyBoatMsg", "You already have a Row Boat.\nUse command '/noboat' to remove it."},
                {"SpawnedBoatMsg", "Your Row Boat has spawned !\nUse command '/noboat' to remove it."},
                {"KilledBoatMsg", "Your Row Boat has been removed/killed."},
                {"NoFoundBoatMsg", "You do not have an active Row Boat."},
                {"FoundBoatMsg", "Your Row Boat is located at {0}."},
                {"CooldownBoatMsg", "You must wait {0} seconds before spawning a new Row Boat."},
                {"DistanceBoatMsg", "You must be within {0} meters of your Row Boat."},
                {"AlreadyRHIBMsg", "You already have a RHIB.\nUse command '/norhib' to remove it."},
                {"SpawnedRHIBMsg", "Your RHIB has spawned !\nUse command '/norhib' to remove it."},
                {"KilledRHIBMsg", "Your RHIB has been removed/killed."},
                {"NoFoundRHIBMsg", "You do not have an active RHIB."},
                {"FoundRHIBMsg", "Your RHIB is located at {0}."},
                {"CooldownRHIBMsg", "You must wait {0} seconds before spawning a new RHIB."},
                {"DistanceRHIBMsg", "You must be within {0} meters of your RHIB."},
                {"AlreadyHABMsg", "You already have a Hot Air Balloon.\nUse command '/nohab' to remove it."},
                {"SpawnedHABMsg", "Your Hot Air Balloon has spawned !\nUse command '/nohab' to remove it."},
                {"KilledHABMsg", "Your Hot Air Balloon has been removed/killed."},
                {"NoFoundHABMsg", "You do not have an active Hot Air Balloon."},
                {"FoundHABMsg", "Your Hot Air Balloon is located at {0}."},
                {"CooldownHABMsg", "You must wait {0} seconds before spawning a new Hot Air Balloon."},
                {"DistanceHABMsg", "You must be within {0} meters of your Hot Air Balloon."},
                {"AlreadyHorseMsg", "You already have a Ridable Horse.\nUse command '/nohorse' to remove it."},
                {"SpawnedHorseMsg", "Your Ridable Horse has spawned !\nUse command '/nohorse' to remove it."},
                {"KilledHorseMsg", "Your Ridable Horse has been removed/killed."},
                {"NoFoundHorseMsg", "You do not have an active Ridable Horse."},
                {"FoundHorseMsg", "Your Ridable Horse is located at {0}."},
                {"CooldownHorseMsg", "You must wait {0} seconds before spawning a new Ridable Horse."},
                {"DistanceHorseMsg", "You must be within {0} meters of your Ridable Horse."},
                {"AlreadyChinookMsg", "You already have a Chinook.\nUse command '/noch47' to remove it."},
                {"SpawnedChinookMsg", "Your Chinook has spawned !\nUse command '/noch47' to remove it."},
                {"KilledChinookMsg", "Your Chinook has been removed/killed."},
                {"NoFoundChinookMsg", "You do not have an active Chinook."},
                {"FoundChinookMsg", "Your Chinook is located at {0}."},
                {"CooldownChinookMsg", "You must wait {0} seconds before spawning a new Chinook."},
                {"DistanceChinookMsg", "You must be within {0} meters of your Chinook."},
                {"AlreadySedanMsg", "You already have a Sedan.\nUse command '/nosedan' to remove it."},
                {"SpawnedSedanMsg", "Your Sedan has spawned !\nUse command '/nosedan' to remove it."},
                {"KilledSedanMsg", "Your Sedan has been removed/killed."},
                {"NoFoundSedanMsg", "You do not have an active Sedan."},
                {"FoundSedanMsg", "Your Sedan is located at {0}."},
                {"CooldownSedanMsg", "You must wait {0} seconds before spawning a new Sedan."},
                {"DistanceSedanMsg", "You must be within {0} meters of your Sedan."},
                {"FlyingMsg", "You cannot spawn or fetch Horses while flying."},
                {"MountedOrParentedMsg", "You cannot spawn or fetch vehicles while mounted or parented to another vehicle."},
                {"BlockedMsg", "You cannot spawn or fetch vehicles while building blocked."},
                {"NotInWaterMsg", "You cannot spawn or fetch boats out of the water."},
                {"InWaterMsg", "You cannot spawn or fetch Horses in the water."},
                {"NoPermMsg", "You are not allowed to do this."},
                {"SpawnUsage", "You need to supply a valid SteamID."}
            }, this, "en");
        }

        private string _(string msgId, BasePlayer player, params object[] args)
        {
            var msg = lang.GetMessage(msgId, this, player?.UserIDString);
            return args.Length > 0 ? string.Format(msg, args) : msg;
        }

        private void PrintMsgL(BasePlayer player, string msgId, params object[] args)
        {
            if (player == null) return;
            PrintMsg(player, _(msgId, player, args));
        }

        private void PrintMsg(BasePlayer player, string msg)
        {
            if (player == null) return;
            SendReply(player, $"{Prefix}{msg}");
        }

        // Chat message to online player with ulong
        private void ChatPlayerOnline(ulong ailldi, string message)
        {
            BasePlayer player = BasePlayer.FindByID(ailldi);
            if (player != null)
            {
                if (message == "killedmini") PrintMsgL(player, "KilledMiniMsg");
                if (message == "killedscrap") PrintMsgL(player, "KilledScrapMsg");
                if (message == "killedboat") PrintMsgL(player, "KilledBoatMsg");
                if (message == "killedrhib") PrintMsgL(player, "KilledRHIBMsg");
                if (message == "killedhab") PrintMsgL(player, "KilledHABMsg");
                if (message == "killedhorse") PrintMsgL(player, "KilledHorseMsg");
                if (message == "killedchinook") PrintMsgL(player, "KilledChinookMsg");
                if (message == "killedsedan") PrintMsgL(player, "KilledSedanMsg");
            }
        }
        #endregion

        #region CONFIG
        protected override void LoadDefaultConfig()
        {
            LoadVariables();
        }

        private void LoadVariables()
        {
            allowWhenBlocked = Convert.ToBoolean(GetConfig("Global", "Allow spawn when building blocked", false));
            allowSpawnIfAlreadyOwnsVehicle = Convert.ToBoolean(GetConfig("Global", "Allow spawning new vehicle if player already owns one", false));
            miniFuelConsumption = (float)Convert.ToSingle(GetConfig("Fuel consumption (per second)", "Minicopter", 0.5));
            scrapFuelConsumption = (float)Convert.ToSingle(GetConfig("Fuel consumption (per second)", "Scrap Helicopter", 0.5));
            boatFuelConsumption = (float)Convert.ToSingle(GetConfig("Fuel consumption (per second)", "Row Boat", 0.1));
            rhibFuelConsumption = (float)Convert.ToSingle(GetConfig("Fuel consumption (per second)", "RHIB", 0.25));
            habFuelConsumption = (float)Convert.ToSingle(GetConfig("Fuel consumption (per second)", "Hot Air Balloon", 0.25));
            Prefix = Convert.ToString(GetConfig("Global", "Chat prefix", "[My Vehicles]: "));
            cooldownmini = Convert.ToSingle(GetConfig("Cooldown (minutes)", "Minicopter", "60"));
            cooldownscrap = Convert.ToSingle(GetConfig("Cooldown (minutes)", "Scrap Helicopter", "60"));
            cooldownboat = Convert.ToSingle(GetConfig("Cooldown (minutes)", "Row Boat", "30"));
            cooldownrhib = Convert.ToSingle(GetConfig("Cooldown (minutes)", "RHIB", "45"));
            cooldownhab = Convert.ToSingle(GetConfig("Cooldown (minutes)", "Hot Air Balloon", "30"));
            cooldownhorse = Convert.ToSingle(GetConfig("Cooldown (minutes)", "Ridable Horse", "15"));
            cooldownchinook = Convert.ToSingle(GetConfig("Cooldown (minutes)", "Chinook", "90"));
            cooldownsedan = Convert.ToSingle(GetConfig("Cooldown (minutes)", "Sedan", "90"));
            useCooldown = Convert.ToBoolean(GetConfig("Cooldown (minutes)", "Use cooldown (on permission)", true));
            vehicleDecay = Convert.ToBoolean(GetConfig("Global", "Vehicle decay", true));
            maxdistancemini = Convert.ToSingle(GetConfig("Maximum distance for killing vehicle (meters)", "Minicopter", "50"));
            maxdistancescrap = Convert.ToSingle(GetConfig("Maximum distance for killing vehicle (meters)", "Scrap Helicopter", "50"));
            maxdistanceboat = Convert.ToSingle(GetConfig("Maximum distance for killing vehicle (meters)", "Row Boat", "50"));
            maxdistancerhib = Convert.ToSingle(GetConfig("Maximum distance for killing vehicle (meters)", "RHIB", "50"));
            maxdistancehab = Convert.ToSingle(GetConfig("Maximum distance for killing vehicle (meters)", "Hot Air Balloon", "50"));
            maxdistancehorse = Convert.ToSingle(GetConfig("Maximum distance for killing vehicle (meters)", "Ridable Horse", "100"));
            maxdistancechinook = Convert.ToSingle(GetConfig("Maximum distance for killing vehicle (meters)", "Chinook", "50"));
            maxdistancesedan = Convert.ToSingle(GetConfig("Maximum distance for killing vehicle (meters)", "Sedan", "100"));
            fetchdistancemini = Convert.ToSingle(GetConfig("Maximum distance for fetching vehicle (meters)", "Minicopter", "50"));
            fetchdistancescrap = Convert.ToSingle(GetConfig("Maximum distance for fetching vehicle (meters)", "Scrap Helicopter", "50"));
            fetchdistanceboat = Convert.ToSingle(GetConfig("Maximum distance for fetching vehicle (meters)", "Row Boat", "50"));
            fetchdistancerhib = Convert.ToSingle(GetConfig("Maximum distance for fetching vehicle (meters)", "RHIB", "50"));
            fetchdistancehab = Convert.ToSingle(GetConfig("Maximum distance for fetching vehicle (meters)", "Hot Air Balloon", "50"));
            fetchdistancehorse = Convert.ToSingle(GetConfig("Maximum distance for fetching vehicle (meters)", "Ridable Horse", "100"));
            fetchdistancechinook = Convert.ToSingle(GetConfig("Maximum distance for fetching vehicle (meters)", "Chinook", "50"));
            fetchdistancesedan = Convert.ToSingle(GetConfig("Maximum distance for fetching vehicle (meters)", "Sedan", "100"));
            killOnSleep = Convert.ToBoolean(GetConfig("Global", "Kill vehicle on player disconnect", false));
            allowFuelIfUnlimited = Convert.ToBoolean(GetConfig("Global", "Allow unlimited to use fuel tank", false));
            ShowChinookMapMarker = Convert.ToBoolean(GetConfig("Global", "Show Chinook map marker", false));

            if(!ConfigChanged) return;
            SaveConfig();
            ConfigChanged = false;
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                ConfigChanged = true;
            }
            object value;
            if(!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                ConfigChanged = true;
            }
            return value;
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }
        #endregion

        #region Hooks
        private readonly static int LAYER_GROUND = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed", "Water");
        private Vector3 GetGroundPosition(Vector3 position)
        {
            position.y += 100f;
            RaycastHit hitInfo;
            if (Physics.Raycast(position, Vector3.down, out hitInfo, 200f, LAYER_GROUND)) position.y = hitInfo.point.y;
            else position.y = TerrainMeta.HeightMap.GetHeight(position);
            return position;
        }

        private bool IsInWater(BasePlayer player)
        {
            var modelState = player.modelState;
            return modelState != null && modelState.waterLevel > 0;
        }

        bool createMiniSideSeats = false;
        bool OnMiniCanCreateSideSeats(MiniCopter entity)
        {
            bool makeit = createMiniSideSeats;
            createMiniSideSeats = false;
            return makeit;
        }

        bool createMiniBackSeat = false;
        bool OnMiniCanCreateRotorSeat(MiniCopter entity)
        {
            bool makeit = createMiniBackSeat;
            createMiniBackSeat = false;
            return makeit;
        }

        private void OnSwitchToggle(ElectricSwitch autoturretSwitch, BasePlayer player)
        {
            var autoturret = autoturretSwitch.GetComponentInParent<AutoTurret>();
            if (autoturret == null)
            {
                return;
            }
            if (autoturret.authorizedPlayers.Any(x => x.userid == player.userID) == false)
            {
                player.ChatMessage("No permission");
                autoturretSwitch.SetSwitch(!autoturretSwitch.IsOn());
                return;
            }
            if (autoturret.GetParentEntity() is MiniCopter)
            {
                if (!autoturretSwitch.IsOn())
                    PowerTurretOn(autoturret);
                else
                    PowerTurretOff(autoturret);
            }
        }
        private void ToggleTurret(AutoTurret autoturret)
        {
            if (autoturret.IsOnline())
            {
                PowerTurretOff(autoturret);
            }
            else
            {
                PowerTurretOn(autoturret);
            }
        }
        private void PowerTurretOn(AutoTurret autoturret)
        {
            autoturret.SetFlag(BaseEntity.Flags.Reserved8,true);
            autoturret.SetIsOnline(true);
        }
        private void PowerTurretOff(AutoTurret autoturret)
        {
            autoturret.SetFlag(BaseEntity.Flags.Reserved8,false);
            autoturret.SetIsOnline(false);
        }

        private string RandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            var stringChars = new char[length];
            var random = new System.Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }
            return new String(stringChars);
        }
        #endregion

        #region chatcommands
        // Chat spawn Minicopter1
        [ChatCommand("mymini1")]
        private void SpawnMyMinicopter1ChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, Minicopter1Spawn);
            if (!(canspawn))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            createMiniBackSeat = true;
            SpawnMyMinicopterChatCommand(player, command, args);
        }

        // Chat spawn Minicopter2
        [ChatCommand("mymini2")]
        private void SpawnMyMinicopter2ChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, Minicopter2Spawn);
            if (!(canspawn))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            createMiniSideSeats = true;
            SpawnMyMinicopterChatCommand(player, command, args);
        }

        // Chat spawn Minicopter3
        [ChatCommand("mymini3")]
        private void SpawnMyMinicopter3ChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, Minicopter3Spawn);
            if (!(canspawn))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            createMiniBackSeat = true;
            createMiniSideSeats = true;
            SpawnMyMinicopterChatCommand(player, command, args);
        }

        // Chat spawn Combat Minicopter
        [ChatCommand("mycombatmini")]
        private void SpawnMyCombatMinicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, CombatMinicopterSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerminiID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyMiniMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, MinicopterCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerminicounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerminicounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerminicounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownmini * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerminicounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownmini * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownMiniMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerminicounter.ContainsKey(player.userID))
                {
                    storedData.playerminicounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyCombatMinicopter(player);
        }

        // Chat spawn CCTV Minicopter
        [ChatCommand("mycctvmini")]
        private void SpawnMyCCTVMinicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, CCTVMinicopterSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerminiID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyMiniMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, MinicopterCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerminicounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerminicounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerminicounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownmini * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerminicounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownmini * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownMiniMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerminicounter.ContainsKey(player.userID))
                {
                    storedData.playerminicounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyCCTVMinicopter(player);
        }

        // Chat spawn Minicopter
        [ChatCommand("mymini")]
        private void SpawnMyMinicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, MinicopterSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerminiID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyMiniMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, MinicopterCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerminicounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerminicounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerminicounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownmini * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerminicounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownmini * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownMiniMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerminicounter.ContainsKey(player.userID))
                {
                    storedData.playerminicounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyMinicopter(player);
        }

        // Fetch Minicopter
        [ChatCommand("gmini")]
        private void GetMyMinicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, MinicopterSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, MinicopterFetch);
            if (!(canspawn & canfetch))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerminiID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerminiID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if (foundit != null)
                {
                    // Distance check
                    if (fetchdistancemini > 0f)
                    {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancemini)
                        {
                            PrintMsgL(player, "DistanceMiniMsg", fetchdistancemini);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as MiniCopter;
                    MiniCopter.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++)
                    {
                        MiniCopter.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null)
                        {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted)
                            {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 3.2f), player.transform.position.y, player.transform.position.z);
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundMiniMsg", newLoc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundMiniMsg");
                return;
            }
        }

        // Find Minicopter
        [ChatCommand("wmini")]
        private void WhereIsMyMinicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, MinicopterWhere);
            if (canspawn == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerminiID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerminiID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if(foundit != null)
                {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundMiniMsg", loc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundMiniMsg");
                return;
            }
        }

        // Chat despawn Minicopter
        [ChatCommand("nomini")]
        private void KillMyMinicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            bool isspawner = permission.UserHasPermission(player.UserIDString, MinicopterSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyMinicopterPlease(player);
        }

        // Chat spawn Piano Scrap Helicopter
        [ChatCommand("mypianoheli")]
        private void SpawnMyPianoScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, PianoScrapSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerscrapID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyScrapMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, ScrapCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerscrapcounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerscrapcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerscrapcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownscrap * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerscrapcounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownscrap * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownScrapMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerscrapcounter.ContainsKey(player.userID))
                {
                    storedData.playerscrapcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyPianoScrapTransportHelicopter(player);
        }

        // Chat spawn Scrap Helicopter
        [ChatCommand("myheli")]
        private void SpawnMyScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, ScrapSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerscrapID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyScrapMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, ScrapCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerscrapcounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerscrapcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerscrapcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownscrap * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerscrapcounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownscrap * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownScrapMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerscrapcounter.ContainsKey(player.userID))
                {
                    storedData.playerscrapcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyScrapTransportHelicopter(player);
        }

        // Fetch Scrap Helicopter
        [ChatCommand("gheli")]
        private void GetMyScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, ScrapSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, ScrapFetch);
            if (!(canspawn & canfetch))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerscrapID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerscrapID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if (foundit != null)
                {
                    // Distance check
                    if (fetchdistancescrap > 0f)
                    {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancescrap)
                        {
                            PrintMsgL(player, "DistanceScrapMsg", fetchdistancescrap);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as ScrapTransportHelicopter;
                    ScrapTransportHelicopter.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++)
                    {
                        ScrapTransportHelicopter.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null)
                        {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted)
                            {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 5f), player.transform.position.y, player.transform.position.z);
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundScrapMsg", newLoc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundScrapMsg");
                return;
            }
        }

        // Find Scrap Helicopter
        [ChatCommand("wheli")]
        private void WhereIsMyScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, ScrapWhere);
            if (canspawn == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerscrapID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerscrapID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if(foundit != null)
                {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundScrapMsg", loc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundScrapMsg");
                return;
            }
        }

        // Chat despawn Scrap Helicopter
        [ChatCommand("noheli")]
        private void KillMyScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args)
        {
            bool isspawner = permission.UserHasPermission(player.UserIDString, ScrapSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyScrapTransportHelicopterPlease(player);
        }

        // Chat spawn Row Boat
        [ChatCommand("myboat")]
        private void SpawnMyRowBoatChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, BoatSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!IsInWater(player))
            {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerboatID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyBoatMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, BoatCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerboatcounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerboatcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerboatcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownboat * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerboatcounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownboat * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownBoatMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerboatcounter.ContainsKey(player.userID))
                {
                    storedData.playerboatcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyRowBoat(player);
        }

        // Fetch Row Boat
        [ChatCommand("gboat")]
        private void GetMyRowBoatChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!IsInWater(player))
            {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, BoatSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, BoatFetch);
            if (!(canspawn & canfetch))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerboatID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerboatID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if (foundit != null)
                {
                    // Distance check
                    if (fetchdistanceboat > 0f)
                    {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistanceboat)
                        {
                            PrintMsgL(player, "DistanceBoatMsg", fetchdistanceboat);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as MotorRowboat;
                    MotorRowboat.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++)
                    {
                        MotorRowboat.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null)
                        {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted)
                            {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 2.9f), player.transform.position.y, player.transform.position.z);
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundBoatMsg", newLoc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundBoatMsg");
                return;
            }
        }

        // Find Row Boat
        [ChatCommand("wboat")]
        private void WhereIsMyRowBoatChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, BoatWhere);
            if (canspawn == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerboatID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerboatID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if(foundit != null)
                {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundBoatMsg", loc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundBoatMsg");
                return;
            }
        }

        // Chat despawn Row Boat
        [ChatCommand("noboat")]
        private void KillMyRowBoatChatCommand(BasePlayer player, string command, string[] args)
        {
            bool isspawner = permission.UserHasPermission(player.UserIDString, BoatSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyRowBoatPlease(player);
        }

        // Chat spawn Piano RHIB
        [ChatCommand("mypianorhib")]
        private void SpawnMyPianoRHIBChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, PianoRHIBSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!IsInWater(player))
            {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerrhibID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyRHIBMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, RHIBCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerrhibcounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerrhibcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerrhibcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownrhib * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerrhibcounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownrhib * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownRHIBMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerrhibcounter.ContainsKey(player.userID))
                {
                    storedData.playerrhibcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyPianoRHIB(player);
        }

        // Chat spawn RHIB
        [ChatCommand("myrhib")]
        private void SpawnMyRHIBChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, RHIBSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!IsInWater(player))
            {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerrhibID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyRHIBMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, RHIBCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerrhibcounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerrhibcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerrhibcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownrhib * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerrhibcounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownrhib * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownRHIBMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerrhibcounter.ContainsKey(player.userID))
                {
                    storedData.playerrhibcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyRHIB(player);
        }

        // Fetch RHIB
        [ChatCommand("grhib")]
        private void GetMyRHIBChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!IsInWater(player))
            {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, RHIBSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, RHIBFetch);
            if (!(canspawn & canfetch))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerrhibID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerrhibID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if (foundit != null)
                {
                    // Distance check
                    if (fetchdistancerhib > 0f)
                    {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancerhib)
                        {
                            PrintMsgL(player, "DistanceRHIBMsg", fetchdistancerhib);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as RHIB;
                    RHIB.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++)
                    {
                        RHIB.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null)
                        {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted)
                            {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 5.15f), player.transform.position.y, player.transform.position.z);
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundRHIBMsg", newLoc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundRHIBMsg");
                return;
            }
        }

        // Find RHIB
        [ChatCommand("wrhib")]
        private void WhereIsMyRHIBChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, RHIBWhere);
            if (canspawn == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerrhibID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerrhibID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if(foundit != null)
                {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundRHIBMsg", loc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundRHIBMsg");
                return;
            }
        }

        // Chat despawn RHIB
        [ChatCommand("norhib")]
        private void KillMyRHIBChatCommand(BasePlayer player, string command, string[] args)
        {
            bool isspawner = permission.UserHasPermission(player.UserIDString, RHIBSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyRHIBPlease(player);
        }

        // Chat spawn Hot Air Balloon
        [ChatCommand("myhab")]
        private void SpawnMyHotAirBalloonChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, HABSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerhabID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyHABMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, HABCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerhabcounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerhabcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerhabcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownhab * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerhabcounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownhab * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownHABMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerhabcounter.ContainsKey(player.userID))
                {
                    storedData.playerhabcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyHotAirBalloon(player);
        }

        // Fetch Hot Air Balloon
        [ChatCommand("ghab")]
        private void GetMyHotAirBalloonChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, HABSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, HABFetch);
            if (!(canspawn & canfetch))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerhabID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerhabID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if (foundit != null)
                {
                    // Distance check
                    if (fetchdistancehab > 0f)
                    {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancehab)
                        {
                            PrintMsgL(player, "DistanceHABMsg", fetchdistancehab);
                            return;
                        }
                    }
                    var newLoc = new Vector3(player.transform.position.x, player.transform.position.y, (float)(player.transform.position.z - 3));
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundHABMsg", newLoc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundHABMsg");
                return;
            }
        }

        // Find Hot Air Balloon
        [ChatCommand("whab")]
        private void WhereIsMyHotAirBalloonChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, HABWhere);
            if (canspawn == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerhabID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerhabID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if(foundit != null)
                {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundHABMsg", loc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundHABMsg");
                return;
            }
        }

        // Chat despawn Hot Air Balloon
        [ChatCommand("nohab")]
        private void KillMyHotAirBalloonChatCommand(BasePlayer player, string command, string[] args)
        {
            bool isspawner = permission.UserHasPermission(player.UserIDString, HABSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyHotAirBalloonPlease(player);
        }

        // Chat spawn Ridable Horse
        [ChatCommand("myhorse")]
        private void SpawnMyRidableHorseChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, HorseSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (IsInWater(player))
            {
                PrintMsgL(player, "InWaterMsg");
                return;
            }

            if (!player.IsOnGround())
            {
                PrintMsgL(player, "FlyingMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerhorseID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyHorseMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, HorseCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerhorsecounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerhorsecounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerhorsecounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownhorse * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerhorsecounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownhorse * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownHorseMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerhorsecounter.ContainsKey(player.userID))
                {
                    storedData.playerhorsecounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyRidableHorse(player);
        }

        // Fetch Ridable Horse
        [ChatCommand("ghorse")]
        private void GetMyRidableHorseChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (IsInWater(player))
            {
                PrintMsgL(player, "InWaterMsg");
                return;
            }

            if (!player.IsOnGround())
            {
                PrintMsgL(player, "FlyingMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, HorseSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, HorseFetch);
            if (!(canspawn & canfetch))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerhorseID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerhorseID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if (foundit != null)
                {
                    // Distance check
                    if (fetchdistancehorse > 0f)
                    {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancehorse)
                        {
                            PrintMsgL(player, "DistanceHorseMsg", fetchdistancehorse);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as RidableHorse;
                    RidableHorse.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++)
                    {
                        RidableHorse.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null)
                        {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted)
                            {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 1.5f), player.transform.position.y, player.transform.position.z);
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundHorseMsg", newLoc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundHorseMsg");
                return;
            }
        }

        // Find Ridable Horse
        [ChatCommand("whorse")]
        private void WhereIsMyRidableHorseChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, HorseWhere);
            if (canspawn == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerhorseID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerhorseID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if(foundit != null)
                {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundHorseMsg", loc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundHorseMsg");
                return;
            }
        }

        // Chat despawn Ridable Horse
        [ChatCommand("nohorse")]
        private void KillMyRidableHorseChatCommand(BasePlayer player, string command, string[] args)
        {
            bool isspawner = permission.UserHasPermission(player.UserIDString, HorseSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyRidableHorsePlease(player);
        }

        // Chat spawn Chinook
        [ChatCommand("mych47")]
        private void SpawnMyChinookChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, ChinookSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playerchinookID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadyChinookMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, ChinookCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playerchinookcounter.ContainsKey(player.userID) == false)
                {
                    storedData.playerchinookcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playerchinookcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownchinook * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerchinookcounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownchinook * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownChinookMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playerchinookcounter.ContainsKey(player.userID))
                {
                    storedData.playerchinookcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyChinook(player);
        }

        // Fetch Chinook
        [ChatCommand("gch47")]
        private void GetMyChinookChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, ChinookSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, ChinookFetch);
            if (!(canspawn & canfetch))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerchinookID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerchinookID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if (foundit != null)
                {
                    // Distance check
                    if (fetchdistancechinook > 0f)
                    {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancechinook)
                        {
                            PrintMsgL(player, "DistanceChinookMsg", fetchdistancechinook);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as CH47Helicopter;
                    CH47Helicopter.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++)
                    {
                        CH47Helicopter.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null)
                        {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted)
                            {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 8.5f), player.transform.position.y, player.transform.position.z);
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundChinookMsg", newLoc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundChinookMsg");
                return;
            }
        }

        // Find Chinook
        [ChatCommand("wch47")]
        private void WhereIsMyChinookChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, ChinookWhere);
            if (canspawn == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerchinookID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playerchinookID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if(foundit != null)
                {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundChinookMsg", loc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundChinookMsg");
                return;
            }
        }

        // Chat despawn Chinook
        [ChatCommand("noch47")]
        private void KillMyChinookChatCommand(BasePlayer player, string command, string[] args)
        {
            bool isspawner = permission.UserHasPermission(player.UserIDString, ChinookSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyChinookPlease(player);
        }

        // Chat spawn Sedan
        [ChatCommand("mysedan")]
        private void SpawnMySedanChatCommand(BasePlayer player, string command, string[] args)
        {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, SedanSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if(!allowSpawnIfAlreadyOwnsVehicle)
            {
                if(storedData.playersedanID.ContainsKey(player.userID) == true)
                {
                    PrintMsgL(player, "AlreadySedanMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, SedanCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true)
            {
                if (storedData.playersedancounter.ContainsKey(player.userID) == false)
                {
                    storedData.playersedancounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                }
                else
                {
                    double count;
                    storedData.playersedancounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownsedan * 60))
                    {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playersedancounter.Remove(player.userID);
                        SaveData();
                    }
                    else
                    {
                        secsleft = Math.Abs((int)((cooldownsedan * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0)
                        {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownSedanMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            else
            {
                if (storedData.playersedancounter.ContainsKey(player.userID))
                {
                    storedData.playersedancounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMySedan(player);
        }

        // Fetch Sedan
        [ChatCommand("gsedan")]
        private void GetMySedanChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity())
            {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, SedanSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, SedanFetch);
            if (!(canspawn & canfetch))
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playersedanID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playersedanID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if (foundit != null)
                {
                    // Distance check
                    if (fetchdistancesedan > 0f)
                    {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancesedan)
                        {
                            PrintMsgL(player, "DistanceSedanMsg", fetchdistancesedan);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as BaseCar;
                    BaseCar.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++)
                    {
                        BaseCar.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null)
                        {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted)
                            {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 3.7f), player.transform.position.y, player.transform.position.z);
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundSedanMsg", newLoc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundSedanMsg");
                return;
            }
        }

        // Find Sedan
        [ChatCommand("wsedan")]
        private void WhereIsMySedanChatCommand(BasePlayer player, string command, string[] args)
        {
            bool canspawn = permission.UserHasPermission(player.UserIDString, SedanWhere);
            if (canspawn == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playersedanID.ContainsKey(player.userID) == true)
            {
                uint findme;
                storedData.playersedanID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme);
                if(foundit != null)
                {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundSedanMsg", loc);
                }
                return;
            }
            else
            {
                PrintMsgL(player, "NoFoundSedanMsg");
                return;
            }
        }

        // Chat despawn Sedan
        [ChatCommand("nosedan")]
        private void KillMySedanChatCommand(BasePlayer player, string command, string[] args)
        {
            bool isspawner = permission.UserHasPermission(player.UserIDString, SedanSpawn);
            if (isspawner == false)
            {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMySedanPlease(player);
        }
        #endregion

        #region consolecommands
        // Console spawn Minicopter1
        [ConsoleCommand("spawnminicopter1")]
        private void SpawnMyMinicopter1ConsoleCommand(ConsoleSystem.Arg arg)
        {
            createMiniBackSeat = true;
            SpawnMyMinicopterConsoleCommand(arg);
        }

        // Console spawn Minicopter2
        [ConsoleCommand("spawnminicopter2")]
        private void SpawnMyMinicopter2ConsoleCommand(ConsoleSystem.Arg arg)
        {
            createMiniSideSeats = true;
            SpawnMyMinicopterConsoleCommand(arg);
        }

        // Console spawn Minicopter3
        [ConsoleCommand("spawnminicopter3")]
        private void SpawnMyMinicopter3ConsoleCommand(ConsoleSystem.Arg arg)
        {
            createMiniBackSeat = true;
            createMiniSideSeats = true;
            SpawnMyMinicopterConsoleCommand(arg);
        }

        // Console spawn Combat Minicopter
        [ConsoleCommand("spawncombatminicopter")]
        private void SpawnMyCombatMinicopterConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyCombatMinicopter(player);
                }
            }
        }

        // Console spawn CCTV Minicopter
        [ConsoleCommand("spawncctvminicopter")]
        private void SpawnMyCCTVMinicopterConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyCCTVMinicopter(player);
                }
            }
        }

        // Console spawn Minicopter
        [ConsoleCommand("spawnminicopter")]
        private void SpawnMyMinicopterConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyMinicopter(player);
                }
            }
        }

        // Console despawn Minicopter
        [ConsoleCommand("killminicopter")]
        private void KillMyMinicopterConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null)
                {
                    KillMyMinicopterPlease(player);
                }
            }
        }

        // Console spawn Piano Scrap Helicopter
        [ConsoleCommand("spawnpianoscraphelicopter")]
        private void SpawnMyPianoScrapTransportHelicopterConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyPianoScrapTransportHelicopter(player);
                }
            }
        }

        // Console spawn Scrap Helicopter
        [ConsoleCommand("spawnscraphelicopter")]
        private void SpawnMyScrapTransportHelicopterConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyScrapTransportHelicopter(player);
                }
            }
        }

        // Console despawn Scrap Helicopter
        [ConsoleCommand("killscraphelicopter")]
        private void KillMyScrapTransportHelicopterConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null)
                {
                    KillMyScrapTransportHelicopterPlease(player);
                }
            }
        }

        // Console spawn Row Boat
        [ConsoleCommand("spawnrowboat")]
        private void SpawnMyRowBoatConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyRowBoat(player);
                }
            }
        }

        // Console despawn Row Boat
        [ConsoleCommand("killrowboat")]
        private void KillMyRowBoatConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null)
                {
                    KillMyRowBoatPlease(player);
                }
            }
        }

        // Console spawn Piano RHIB
        [ConsoleCommand("spawnpianorhib")]
        private void SpawnMyPianoRHIBConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyPianoRHIB(player);
                }
            }
        }

        // Console spawn RHIB
        [ConsoleCommand("spawnrhib")]
        private void SpawnMyRHIBConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyRHIB(player);
                }
            }
        }

        // Console despawn RHIB
        [ConsoleCommand("killrhib")]
        private void KillMyRHIBConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null)
                {
                    KillMyRHIBPlease(player);
                }
            }
        }

        // Console spawn Hot Air Balloon
        [ConsoleCommand("spawnhotairballoon")]
        private void SpawnMyHotAirBalloonConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyHotAirBalloon(player);
                }
            }
        }

        // Console despawn Hot Air Balloon
        [ConsoleCommand("killhotairballoon")]
        private void KillMyHotAirBalloonConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null)
                {
                    KillMyHotAirBalloonPlease(player);
                }
            }
        }

        // Console spawn Ridable Horse
        [ConsoleCommand("spawnridablehorse")]
        private void SpawnMyRidableHorseConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyRidableHorse(player);
                }
            }
        }

        // Console despawn Ridable Horse
        [ConsoleCommand("killridablehorse")]
        private void KillMyRidableHorseConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null)
                {
                    KillMyRidableHorsePlease(player);
                }
            }
        }

        // Console spawn Chinook
        [ConsoleCommand("spawnch47")]
        private void SpawnMyChinookConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMyChinook(player);
                }
            }
        }

        // Console despawn Chinook
        [ConsoleCommand("killch47")]
        private void KillMyChinookConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null)
                {
                    KillMyChinookPlease(player);
                }
            }
        }

        // Console spawn Sedan
        [ConsoleCommand("spawnsedan")]
        private void SpawnMySedanConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if(steamid == 0) return;
                if(steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null)
                {
                    SpawnMySedan(player);
                }
            }
        }

        // Console despawn Sedan
        [ConsoleCommand("killsedan")]
        private void KillMySedanConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsRcon)
            {
                if (arg.Args == null)
                {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            }
            else if (!HasPermission(arg, MyVehiclesAdmin))
            {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            }
            else if (arg.Args == null)
            {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1)
            {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null)
                {
                    KillMySedanPlease(player);
                }
            }
        }
        #endregion

        #region ourhooks
        // Spawn Combat Minicopter hook
        private void SpawnMyCombatMinicopter(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 1.1f), player.transform.position.y, (float)(player.transform.position.z - 0.15f));
            position = GetGroundPosition(position);

            if (position == null) return;
            MiniCopter vehicleMini = (MiniCopter)GameManager.server.CreateEntity(prefabmini, position, new Quaternion());
            if (vehicleMini == null) return;
            BaseEntity miniEntity = vehicleMini as BaseEntity;
            miniEntity.OwnerID = player.userID;

            MiniCopter miniCopter = vehicleMini as MiniCopter;
            vehicleMini.Spawn();

            if (permission.UserHasPermission(player.UserIDString, MinicopterUnlimited))
            {
                // Set fuel requirements to 0
                miniCopter.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = miniCopter.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                miniCopter.fuelPerSec = miniFuelConsumption;
            }

            AutoTurret autoturret = GameManager.server.CreateEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab", miniCopter.transform.position) as AutoTurret;
            autoturret.Spawn();
            autoturret.pickup.enabled = false;
            autoturret.sightRange = 50f;
            UnityEngine.Object.Destroy(autoturret.GetComponent<DestroyOnGroundMissing>());
            autoturret.SetParent(miniCopter);
            autoturret.transform.localPosition = new Vector3(0, 0, 2.47f);
            autoturret.transform.localRotation = Quaternion.Euler(0, 0, 0);
            autoturret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID { userid = miniCopter.OwnerID, username = BasePlayer.FindByID(miniCopter.OwnerID)?.displayName });

            timer.Once(0.1f, () =>
            {
                var autoturretSwitch = autoturret.GetComponentInChildren<ElectricSwitch>();
                if (autoturretSwitch != null)
                {
                    UnityEngine.Object.Destroy(autoturretSwitch.GetComponent<DestroyOnGroundMissing>());
                    UnityEngine.Object.Destroy(autoturretSwitch.GetComponent<GroundWatch>());
                    return;
                }
                autoturretSwitch = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab", autoturret.transform.position)?.GetComponent<ElectricSwitch>();
                if (autoturretSwitch == null)
                {
                    return;
                }
                autoturretSwitch.pickup.enabled = false;
                autoturretSwitch.SetParent(autoturret);
                autoturretSwitch.transform.localPosition = new Vector3(0f, -0.65f, 0.325f);
                autoturretSwitch.transform.localRotation = Quaternion.Euler(0, 0, 0);
                autoturretSwitch.Spawn();
                UnityEngine.Object.Destroy(autoturretSwitch.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(autoturretSwitch.GetComponent<GroundWatch>());
                autoturretSwitch._maxHealth = 2147483000f;
                autoturretSwitch.health = 2147483000f;
                autoturretSwitch.UpdateHasPower(12, 0);
            });

            PrintMsgL(player, "SpawnedMiniMsg");
            uint minicopteruint = vehicleMini.net.ID;
#if DEBUG
            Puts($"SPAWNED COMBAT MINICOPTER {minicopteruint.ToString()} for player {player.displayName} OWNER {miniEntity.OwnerID}");
#endif
            storedData.playerminiID.Remove(player.userID);
            storedData.playerminiID.Add(player.userID, minicopteruint);
            SaveData();

            miniEntity = null; 
            miniCopter = null;
        }

        // Spawn CCTV Minicopter hook
        private void SpawnMyCCTVMinicopter(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 1.1f), player.transform.position.y, (float)(player.transform.position.z - 0.15f));
            position = GetGroundPosition(position);

            if (position == null) return;
            MiniCopter vehicleMini = (MiniCopter)GameManager.server.CreateEntity(prefabmini, position, new Quaternion());
            if (vehicleMini == null) return;
            BaseEntity miniEntity = vehicleMini as BaseEntity;
            miniEntity.OwnerID = player.userID;

            MiniCopter miniCopter = vehicleMini as MiniCopter;
            vehicleMini.Spawn();

            if (permission.UserHasPermission(player.UserIDString, MinicopterUnlimited))
            {
                // Set fuel requirements to 0
                miniCopter.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = miniCopter.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                miniCopter.fuelPerSec = miniFuelConsumption;
            }

            var cctv = GameManager.server.CreateEntity("assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab", miniCopter.transform.position) as CCTV_RC;
            if (cctv == null) return;
            cctv.Spawn();
            cctv.pickup.enabled = false;
            UnityEngine.Object.Destroy(cctv.GetComponent<DestroyOnGroundMissing>());
            cctv.SetParent(miniCopter);
            cctv.transform.localPosition = new Vector3(0, 1.9f, 0);
            cctv.transform.localRotation = Quaternion.Euler(0, 0, 0);
            cctv._maxHealth = 2147483000f;
            cctv.health = 2147483000f;
            cctv.UpdateIdentifier(RandomString(3));
            cctv.UpdateHasPower(25, 1);

            PrintMsgL(player, "SpawnedMiniMsg");
            uint minicopteruint = vehicleMini.net.ID;
#if DEBUG
            Puts($"SPAWNED CCTV MINICOPTER {minicopteruint.ToString()} for player {player.displayName} OWNER {miniEntity.OwnerID}");
#endif
            storedData.playerminiID.Remove(player.userID);
            storedData.playerminiID.Add(player.userID, minicopteruint);
            SaveData();

            miniEntity = null; 
            miniCopter = null;
        }

        // Spawn Minicopter hook
        private void SpawnMyMinicopter(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 1.1f), player.transform.position.y, (float)(player.transform.position.z - 0.15f));
            position = GetGroundPosition(position);

            if (position == null) return;
            MiniCopter vehicleMini = (MiniCopter)GameManager.server.CreateEntity(prefabmini, position, new Quaternion());
            if (vehicleMini == null) return;
            BaseEntity miniEntity = vehicleMini as BaseEntity;
            miniEntity.OwnerID = player.userID;

            MiniCopter miniCopter = vehicleMini as MiniCopter;
            vehicleMini.Spawn();

            if (permission.UserHasPermission(player.UserIDString, MinicopterUnlimited))
            {
                // Set fuel requirements to 0
                miniCopter.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = miniCopter.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                miniCopter.fuelPerSec = miniFuelConsumption;
            }

            PrintMsgL(player, "SpawnedMiniMsg");
            uint minicopteruint = vehicleMini.net.ID;
#if DEBUG
            Puts($"SPAWNED MINICOPTER {minicopteruint.ToString()} for player {player.displayName} OWNER {miniEntity.OwnerID}");
#endif
            storedData.playerminiID.Remove(player.userID);
            storedData.playerminiID.Add(player.userID, minicopteruint);
            SaveData();

            miniEntity = null; 
            miniCopter = null;
        }

        // Kill Minicopter hook
        private void KillMyMinicopterPlease(BasePlayer player)
        {
            bool foundmini = false;
            if (maxdistancemini == 0f)
            {
                foundmini = true;
            }
            else
            {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancemini, vehiclelist);

                foreach (BaseEntity p in vehiclelist)
                {
                    var foundent = p.GetComponentInParent<MiniCopter>() ?? null;
                    if (foundent != null)
                    {
                        foundmini = true;
                    }
                }
            }

            if (storedData.playerminiID.ContainsKey(player.userID) == true && foundmini)
            {
                uint deluint;
                storedData.playerminiID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill != null)
                {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerminiID.Remove(player.userID);

                if (storedData.playerminicounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerminicounter.Remove(player.userID);
                }
                SaveData();
            }
            else if (foundmini == false)
            {
#if DEBUG
                Puts($"Player too far from Minicopter to destroy.");
#endif
                PrintMsgL(player, "DistanceMiniMsg", maxdistancemini);
            }
        }

        // Spawn Piano Scrap Helicopter hook
        private void SpawnMyPianoScrapTransportHelicopter(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 2.15f), player.transform.position.y, player.transform.position.z);
            position = GetGroundPosition(position);

            if (position == null) return;
            ScrapTransportHelicopter vehicleScrap = (ScrapTransportHelicopter)GameManager.server.CreateEntity(prefabscrap, position, new Quaternion());
            if (vehicleScrap == null) return;
            BaseEntity scrapEntity = vehicleScrap as BaseEntity;
            scrapEntity.OwnerID = player.userID;

            ScrapTransportHelicopter scrapCopter = vehicleScrap as ScrapTransportHelicopter;
            vehicleScrap.Spawn();

            if (permission.UserHasPermission(player.UserIDString, ScrapUnlimited))
            {
                // Set fuel requirements to 0
                scrapCopter.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = scrapCopter.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                scrapCopter.fuelPerSec = scrapFuelConsumption;
            }

            var piano = GameManager.server.CreateEntity("assets/prefabs/instruments/piano/piano.deployed.prefab", scrapCopter.transform.position) as StaticInstrument;
            if (piano == null) return;
            piano.Spawn();
            piano.pickup.enabled = false;
            UnityEngine.Object.Destroy(piano.GetComponent<DestroyOnGroundMissing>());
            piano.SetParent(scrapCopter);
            piano.transform.localPosition = new Vector3(0, 0.85f, 0.5f);
            piano.transform.localRotation = Quaternion.Euler(0, 0, 0);

            PrintMsgL(player, "SpawnedScrapMsg");
            uint scrapuint = vehicleScrap.net.ID;
#if DEBUG
            Puts($"SPAWNED PIANO SCRAP HELICOPTER {scrapuint.ToString()} for player {player.displayName} OWNER {scrapEntity.OwnerID}");
#endif
            storedData.playerscrapID.Remove(player.userID);
            storedData.playerscrapID.Add(player.userID, scrapuint);
            SaveData();

            scrapEntity = null;
            scrapCopter = null;
        }

        // Spawn Scrap Helicopter hook
        private void SpawnMyScrapTransportHelicopter(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 2.15f), player.transform.position.y, player.transform.position.z);
            position = GetGroundPosition(position);

            if (position == null) return;
            ScrapTransportHelicopter vehicleScrap = (ScrapTransportHelicopter)GameManager.server.CreateEntity(prefabscrap, position, new Quaternion());
            if (vehicleScrap == null) return;
            BaseEntity scrapEntity = vehicleScrap as BaseEntity;
            scrapEntity.OwnerID = player.userID;

            ScrapTransportHelicopter scrapCopter = vehicleScrap as ScrapTransportHelicopter;
            vehicleScrap.Spawn();

            if (permission.UserHasPermission(player.UserIDString, ScrapUnlimited))
            {
                // Set fuel requirements to 0
                scrapCopter.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = scrapCopter.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                scrapCopter.fuelPerSec = scrapFuelConsumption;
            }

            PrintMsgL(player, "SpawnedScrapMsg");
            uint scrapuint = vehicleScrap.net.ID;
#if DEBUG
            Puts($"SPAWNED SCRAP HELICOPTER {scrapuint.ToString()} for player {player.displayName} OWNER {scrapEntity.OwnerID}");
#endif
            storedData.playerscrapID.Remove(player.userID);
            storedData.playerscrapID.Add(player.userID, scrapuint);
            SaveData();

            scrapEntity = null;
            scrapCopter = null;
        }

        // Kill Scrap Helicopter hook
        private void KillMyScrapTransportHelicopterPlease(BasePlayer player)
        {
            bool foundscrap = false;
            if (maxdistancescrap == 0f)
            {
                foundscrap = true;
            }
            else
            {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancescrap, vehiclelist);

                foreach (BaseEntity p in vehiclelist)
                {
                    var foundent = p.GetComponentInParent<ScrapTransportHelicopter>() ?? null;
                    if (foundent != null)
                    {
                        foundscrap = true;
                    }
                }
            }

            if (storedData.playerscrapID.ContainsKey(player.userID) == true && foundscrap)
            {
                uint deluint;
                storedData.playerscrapID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill != null)
                {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerscrapID.Remove(player.userID);

                if (storedData.playerscrapcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerscrapcounter.Remove(player.userID);
                }
                SaveData();
            }
            else if (foundscrap == false)
            {
#if DEBUG
                Puts($"Player too far from Scrap Helicopter to destroy.");
#endif
                PrintMsgL(player, "DistanceScrapMsg", maxdistancescrap);
            }
        }

        // Spawn Row Boat hook
        private void SpawnMyRowBoat(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 1.35f), player.transform.position.y, player.transform.position.z);
            position = GetGroundPosition(position);

            if (position == null) return;
            MotorRowboat vehicleBoat = (MotorRowboat)GameManager.server.CreateEntity(prefabboat, position, new Quaternion());
            if (vehicleBoat == null) return;
            BaseEntity boatEntity = vehicleBoat as BaseEntity;
            boatEntity.OwnerID = player.userID;

            MotorRowboat rowBoat = vehicleBoat as MotorRowboat;
            vehicleBoat.Spawn();

            if (permission.UserHasPermission(player.UserIDString, BoatUnlimited))
            {
                // Set fuel requirements to 0
                rowBoat.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = rowBoat.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                rowBoat.fuelPerSec = boatFuelConsumption;
            }

            PrintMsgL(player, "SpawnedBoatMsg");
            uint boatuint = vehicleBoat.net.ID;
#if DEBUG
            Puts($"SPAWNED ROW BOAT {boatuint.ToString()} for player {player.displayName} OWNER {boatEntity.OwnerID}");
#endif
            storedData.playerboatID.Remove(player.userID);
            storedData.playerboatID.Add(player.userID, boatuint);
            SaveData();

            boatEntity = null;
            rowBoat = null;
        }

        // Kill Row Boat hook
        private void KillMyRowBoatPlease(BasePlayer player)
        {
            bool foundboat = false;
            if (maxdistanceboat == 0f)
            {
                foundboat = true;
            }
            else
            {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistanceboat, vehiclelist);

                foreach (BaseEntity p in vehiclelist)
                {
                    var foundent = p.GetComponentInParent<MotorRowboat>() ?? null;
                    if (foundent != null)
                    {
                        foundboat = true;
                    }
                }
            }

            if (storedData.playerboatID.ContainsKey(player.userID) == true && foundboat)
            {
                uint deluint;
                storedData.playerboatID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill != null)
                {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerboatID.Remove(player.userID);

                if (storedData.playerboatcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerboatcounter.Remove(player.userID);
                }
                SaveData();
            }
            else if (foundboat == false)
            {
#if DEBUG
                Puts($"Player too far from Row Boat to destroy.");
#endif
                PrintMsgL(player, "DistanceBoatMsg", maxdistanceboat);
            }
        }

        // Spawn Piano RHIB hook
        private void SpawnMyPianoRHIB(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 2), player.transform.position.y, player.transform.position.z);
            position = GetGroundPosition(position);

            if (position == null) return;
            RHIB rhib = (RHIB)GameManager.server.CreateEntity(prefabrhib, position, new Quaternion());
            if (rhib == null) return;
            rhib.OwnerID = player.userID;
            rhib.Spawn();

            if (permission.UserHasPermission(player.UserIDString, RHIBUnlimited))
            {
                // Set fuel requirements to 0
                rhib.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = rhib.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                rhib.fuelPerSec = rhibFuelConsumption;
            }

            var piano = GameManager.server.CreateEntity("assets/prefabs/instruments/piano/piano.deployed.prefab", rhib.transform.position) as StaticInstrument;
            if (piano == null) return;
            piano.Spawn();
            piano.pickup.enabled = false;
            UnityEngine.Object.Destroy(piano.GetComponent<DestroyOnGroundMissing>());
            piano.SetParent(rhib);
            piano.transform.localPosition = new Vector3(0, 1.25f, 4.25f);
            piano.transform.localRotation = Quaternion.Euler(0, 180, 0);

            PrintMsgL(player, "SpawnedRHIBMsg");
            uint rhibuint = rhib.net.ID;
#if DEBUG
            Puts($"SPAWNED PIANO RHIB {rhibuint.ToString()} for player {player.displayName} OWNER {rhibEntity.OwnerID}");
#endif
            storedData.playerrhibID.Remove(player.userID);
            storedData.playerrhibID.Add(player.userID, rhibuint);
            SaveData();

            rhib = null;
        }

        // Spawn RHIB hook
        private void SpawnMyRHIB(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 2), player.transform.position.y, player.transform.position.z);
            position = GetGroundPosition(position);

            if (position == null) return;
            RHIB rhib = (RHIB)GameManager.server.CreateEntity(prefabrhib, position, new Quaternion());
            if (rhib == null) return;
            rhib.OwnerID = player.userID;
            rhib.Spawn();

            if (permission.UserHasPermission(player.UserIDString, RHIBUnlimited))
            {
                // Set fuel requirements to 0
                rhib.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = rhib.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                rhib.fuelPerSec = rhibFuelConsumption;
            }

            PrintMsgL(player, "SpawnedRHIBMsg");
            uint rhibuint = rhib.net.ID;
#if DEBUG
            Puts($"SPAWNED RHIB {rhibuint.ToString()} for player {player.displayName} OWNER {rhibEntity.OwnerID}");
#endif
            storedData.playerrhibID.Remove(player.userID);
            storedData.playerrhibID.Add(player.userID, rhibuint);
            SaveData();

            rhib = null;
        }

        // Kill RHIB hook
        private void KillMyRHIBPlease(BasePlayer player)
        {
            bool foundrhib = false;
            if (maxdistancerhib == 0f)
            {
                foundrhib = true;
            }
            else
            {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancerhib, vehiclelist);

                foreach (BaseEntity p in vehiclelist)
                {
                    var foundent = p.GetComponentInParent<RHIB>() ?? null;
                    if (foundent != null)
                    {
                        foundrhib = true;
                    }
                }
            }

            if (storedData.playerrhibID.ContainsKey(player.userID) == true && foundrhib)
            {
                uint deluint;
                storedData.playerrhibID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill != null)
                {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerrhibID.Remove(player.userID);

                if (storedData.playerrhibcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerrhibcounter.Remove(player.userID);
                }
                SaveData();
            }
            else if (foundrhib == false)
            {
#if DEBUG
                Puts($"Player too far from RHIB to destroy.");
#endif
                PrintMsgL(player, "DistanceRHIBMsg", maxdistancerhib);
            }
        }

        // Spawn Hot Air Balloon hook
        private void SpawnMyHotAirBalloon(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3(player.transform.position.x, player.transform.position.y, (float)(player.transform.position.z - 2));
            position = GetGroundPosition(position);

            if (position == null) return;
            HotAirBalloon hab = (HotAirBalloon)GameManager.server.CreateEntity(prefabhab, position, new Quaternion());
            if (hab == null) return;
            hab.OwnerID = player.userID;
            hab.Spawn();

            if (permission.UserHasPermission(player.UserIDString, HABUnlimited))
            {
                // Set fuel requirements to 0
                hab.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited)
                {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = hab.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else
            {
                hab.fuelPerSec = habFuelConsumption;
            }

            PrintMsgL(player, "SpawnedHABMsg");
            uint habuint = hab.net.ID;
#if DEBUG
            Puts($"SPAWNED HOT AIR BALLOON {habuint.ToString()} for player {player.displayName} OWNER {hab.OwnerID}");
#endif
            storedData.playerhabID.Remove(player.userID);
            storedData.playerhabID.Add(player.userID, habuint);
            SaveData();

            hab = null;
        }

        // Kill Hot Air Balloon hook
        private void KillMyHotAirBalloonPlease(BasePlayer player)
        {
            bool foundhab = false;
            if (maxdistancehab == 0f)
            {
                foundhab = true;
            }
            else
            {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancehab, vehiclelist);

                foreach (BaseEntity p in vehiclelist)
                {
                    var foundent = p.GetComponentInParent<HotAirBalloon>() ?? null;
                    if (foundent != null)
                    {
                        foundhab = true;
                    }
                }
            }

            if (storedData.playerhabID.ContainsKey(player.userID) == true && foundhab)
            {
                uint deluint;
                storedData.playerhabID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill != null)
                {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerhabID.Remove(player.userID);

                if (storedData.playerhabcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerhabcounter.Remove(player.userID);
                }
                SaveData();
            }
            else if (foundhab == false)
            {
#if DEBUG
                Puts($"Player too far from Hot Air Balloon to destroy.");
#endif
                PrintMsgL(player, "DistanceHABMsg", maxdistancehab);
            }
        }

        // Spawn Ridable Horse hook
        private void SpawnMyRidableHorse(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 1), player.transform.position.y, player.transform.position.z);
            position = GetGroundPosition(position);

            if (position == null) return;
            RidableHorse vehicleHorse = (RidableHorse)GameManager.server.CreateEntity(prefabhorse, position, new Quaternion());
            if (vehicleHorse == null) return;
            BaseEntity horseEntity = vehicleHorse as BaseEntity;
            horseEntity.OwnerID = player.userID;

            RidableHorse horse = vehicleHorse as RidableHorse;
            vehicleHorse.Spawn();

            PrintMsgL(player, "SpawnedHorseMsg");
            uint horseuint = vehicleHorse.net.ID;
#if DEBUG
            Puts($"SPAWNED RIDABLE HORSE {horseuint.ToString()} for player {player.displayName} OWNER {horseEntity.OwnerID}");
#endif
            storedData.playerhorseID.Remove(player.userID);
            storedData.playerhorseID.Add(player.userID, horseuint);
            SaveData();

            horseEntity = null;
            horse = null;
        }

        // Kill Ridable Horse hook
        private void KillMyRidableHorsePlease(BasePlayer player)
        {
            bool foundhorse = false;
            if (maxdistancehorse == 0f)
            {
                foundhorse = true;
            }
            else
            {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancehorse, vehiclelist);

                foreach (BaseEntity p in vehiclelist)
                {
                    var foundent = p.GetComponentInParent<RidableHorse>() ?? null;
                    if (foundent != null)
                    {
                        foundhorse = true;
                    }
                }
            }

            if (storedData.playerhorseID.ContainsKey(player.userID) == true && foundhorse)
            {
                uint deluint;
                storedData.playerhorseID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill != null)
                {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerhorseID.Remove(player.userID);

                if (storedData.playerhorsecounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerhorsecounter.Remove(player.userID);
                }
                SaveData();
            }
            else if (foundhorse == false)
            {
#if DEBUG
                Puts($"Player too far from Ridable Horse to destroy.");
#endif
                PrintMsgL(player, "DistanceHorseMsg", maxdistancehorse);
            }
        }

        // Spawn Chinook hook
        private void SpawnMyChinook(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 2.5f), player.transform.position.y, player.transform.position.z);
            position = GetGroundPosition(position);

            if (position == null) return;
            CH47Helicopter vehicleChinook = (CH47Helicopter)GameManager.server.CreateEntity(prefabchinook, position, new Quaternion());
            if (vehicleChinook == null) return;
            BaseEntity chinookEntity = vehicleChinook as BaseEntity;
            chinookEntity.OwnerID = player.userID;

            CH47Helicopter chinook = vehicleChinook as CH47Helicopter;
            vehicleChinook.Spawn();

            if (!ShowChinookMapMarker && chinookEntity is CH47Helicopter)
            {
                var mapMarker = chinookEntity as CH47Helicopter;
                mapMarker.mapMarkerInstance?.Kill();
                mapMarker.mapMarkerEntityPrefab.guid = string.Empty;
            }

            PrintMsgL(player, "SpawnedChinookMsg");
            uint chinookuint = vehicleChinook.net.ID;
#if DEBUG
            Puts($"SPAWNED CHINOOK {chinookuint.ToString()} for player {player.displayName} OWNER {chinookEntity.OwnerID}");
#endif
            storedData.playerchinookID.Remove(player.userID);
            storedData.playerchinookID.Add(player.userID, chinookuint);
            SaveData();

            chinookEntity = null;
            chinook = null;
        }

        // Kill Chinook hook
        private void KillMyChinookPlease(BasePlayer player)
        {
            bool foundchinook = false;
            if (maxdistancechinook == 0f)
            {
                foundchinook = true;
            }
            else
            {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancechinook, vehiclelist);

                foreach (BaseEntity p in vehiclelist)
                {
                    var foundent = p.GetComponentInParent<CH47Helicopter>() ?? null;
                    if (foundent != null)
                    {
                        foundchinook = true;
                    }
                }
            }

            if (storedData.playerchinookID.ContainsKey(player.userID) == true && foundchinook)
            {
                uint deluint;
                storedData.playerchinookID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill != null)
                {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerchinookID.Remove(player.userID);

                if (storedData.playerchinookcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerchinookcounter.Remove(player.userID);
                }
                SaveData();
            }
            else if (foundchinook == false)
            {
#if DEBUG
                Puts($"Player too far from Chinook to destroy.");
#endif
                PrintMsgL(player, "DistanceChinookMsg", maxdistancechinook);
            }
        }

        // Spawn Sedan hook
        private void SpawnMySedan(BasePlayer player)
        {
            if (player.IsBuildingBlocked() & !allowWhenBlocked)
            {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 1.65f), player.transform.position.y, player.transform.position.z);
            position = GetGroundPosition(position);

            if (position == null) return;
            BaseCar vehicleSedan = (BaseCar)GameManager.server.CreateEntity(prefabsedan, position, new Quaternion());
            if (vehicleSedan == null) return;
            BaseEntity sedanEntity = vehicleSedan as BaseEntity;
            sedanEntity.OwnerID = player.userID;

            BaseCar sedan = vehicleSedan as BaseCar;
            vehicleSedan.Spawn();

            PrintMsgL(player, "SpawnedSedanMsg");
            uint sedanuint = vehicleSedan.net.ID;
#if DEBUG
            Puts($"SPAWNED SEDAN {sedanuint.ToString()} for player {player.displayName} OWNER {sedanEntity.OwnerID}");
#endif
            storedData.playersedanID.Remove(player.userID);
            storedData.playersedanID.Add(player.userID, sedanuint);
            SaveData();

            sedanEntity = null;
            sedan = null;
        }

        // Kill Sedan hook
        private void KillMySedanPlease(BasePlayer player)
        {
            bool foundsedan = false;
            if (maxdistancesedan == 0f)
            {
                foundsedan = true;
            }
            else
            {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancesedan, vehiclelist);

                foreach (BaseEntity p in vehiclelist)
                {
                    var foundent = p.GetComponentInParent<BaseCar>() ?? null;
                    if (foundent != null)
                    {
                        foundsedan = true;
                    }
                }
            }

            if (storedData.playersedanID.ContainsKey(player.userID) == true && foundsedan)
            {
                uint deluint;
                storedData.playersedanID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill != null)
                {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playersedanID.Remove(player.userID);

                if (storedData.playersedancounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playersedancounter.Remove(player.userID);
                }
                SaveData();
            }
            else if (foundsedan == false)
            {
#if DEBUG
                Puts($"Player too far from Sedan to destroy.");
#endif
                PrintMsgL(player, "DistanceSedanMsg", maxdistancesedan);
            }
        }
        #endregion

        #region hooks
        // On kill - tell owner
        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (entity.net.ID == 0) return;
            if (storedData.playerminiID == null) return;
            if (storedData.playerscrapID == null) return;
            if (storedData.playerboatID == null) return;
            if (storedData.playerrhibID == null) return;
            if (storedData.playerhabID == null) return;
            if (storedData.playerhorseID == null) return;
            if (storedData.playerchinookID == null) return;
            if (storedData.playersedanID == null) return;
            if (storedData.playerminiID.ContainsValue(entity.net.ID) == false)
            if (storedData.playerscrapID.ContainsValue(entity.net.ID) == false)
            if (storedData.playerboatID.ContainsValue(entity.net.ID) == false)
            if (storedData.playerrhibID.ContainsValue(entity.net.ID) == false)
            if (storedData.playerhabID.ContainsValue(entity.net.ID) == false)
            if (storedData.playerhorseID.ContainsValue(entity.net.ID) == false)
            if (storedData.playerchinookID.ContainsValue(entity.net.ID) == false)
            if (storedData.playersedanID.ContainsValue(entity.net.ID) == false)
            {
#if DEBUG
                Puts($"KILLED non-plugin vehicle");
#endif
                return;
            }
            ulong todeletemini = new ulong();
            ulong todeletescrap = new ulong();
            ulong todeleteboat = new ulong();
            ulong todeleterhib = new ulong();
            ulong todeletehab = new ulong();
            ulong todeletehorse = new ulong();
            ulong todeletechinook = new ulong();
            ulong todeletesedan = new ulong();
            foreach (var item in storedData.playerminiID)
            {
                if (item.Value == entity.net.ID)
                {
                    ChatPlayerOnline(item.Key, "killedmini");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletemini = item.Key;
                }
            }
            foreach (var item in storedData.playerscrapID)
            {
                if (item.Value == entity.net.ID)
                {
                    ChatPlayerOnline(item.Key, "killedscrap");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletescrap = item.Key;
                }
            }
            foreach (var item in storedData.playerboatID)
            {
                if (item.Value == entity.net.ID)
                {
                    ChatPlayerOnline(item.Key, "killedboat");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeleteboat = item.Key;
                }
            }
            foreach (var item in storedData.playerrhibID)
            {
                if (item.Value == entity.net.ID)
                {
                    ChatPlayerOnline(item.Key, "killedrhib");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeleterhib = item.Key;
                }
            }
            foreach (var item in storedData.playerhabID)
            {
                if (item.Value == entity.net.ID)
                {
                    ChatPlayerOnline(item.Key, "killedhab");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletehab = item.Key;
                }
            }
            foreach (var item in storedData.playerhorseID)
            {
                if (item.Value == entity.net.ID)
                {
                    ChatPlayerOnline(item.Key, "killedhorse");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletehorse = item.Key;
                }
            }
            foreach (var item in storedData.playerchinookID)
            {
                if (item.Value == entity.net.ID)
                {
                    ChatPlayerOnline(item.Key, "killedchinook");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletechinook = item.Key;
                }
            }
            foreach (var item in storedData.playersedanID)
            {
                if (item.Value == entity.net.ID)
                {
                    ChatPlayerOnline(item.Key, "killedsedan");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletesedan = item.Key;
                }
            }
            if (todeletemini != 0)
            {
                storedData.playerminiID.Remove(todeletemini);
                SaveData();
            }
            if (todeletescrap != 0)
            {
                storedData.playerscrapID.Remove(todeletescrap);
                SaveData();
            }
            if (todeleteboat != 0)
            {
                storedData.playerboatID.Remove(todeleteboat);
                SaveData();
            }
            if (todeleterhib != 0)
            {
                storedData.playerrhibID.Remove(todeleterhib);
                SaveData();
            }
            if (todeletehab != 0)
            {
                storedData.playerhabID.Remove(todeletehab);
                SaveData();
            }
            if (todeletehorse != 0)
            {
                storedData.playerhorseID.Remove(todeletehorse);
                SaveData();
            }
            if (todeletechinook != 0)
            {
                storedData.playerchinookID.Remove(todeletechinook);
                SaveData();
            }
            if (todeletesedan != 0)
            {
                storedData.playersedanID.Remove(todeletesedan);
                SaveData();
            }
        }

        // Disable decay for our vehicles if so configured
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null) return;
            if (!hitInfo.damageTypes.Has(Rust.DamageType.Decay)) return;
            if (storedData.playerminiID == null) return;
            if (storedData.playerscrapID == null) return;
            if (storedData.playerboatID == null) return;
            if (storedData.playerrhibID == null) return;
            if (storedData.playerhabID == null) return;
            if (storedData.playerhorseID == null) return;
            if (storedData.playerchinookID == null) return;
            if (storedData.playersedanID == null) return;

            if (storedData.playerminiID.ContainsValue(entity.net.ID))
            {
                if (vehicleDecay)
                {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Minicopter {entity.net.ID.ToString()}.");
#endif
                }
                else
                {
#if DEBUG
                    Puts($"Disabling decay for spawned Minicopter {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }

            if (storedData.playerscrapID.ContainsValue(entity.net.ID))
            {
                if (vehicleDecay)
                {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Scrap Helicopter {entity.net.ID.ToString()}.");
#endif
                }
                else
                {
#if DEBUG
                    Puts($"Disabling decay for spawned Scrap Helicopter {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerboatID.ContainsValue(entity.net.ID))
            {
                if (vehicleDecay)
                {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Row Boat {entity.net.ID.ToString()}.");
#endif
                }
                else
                {
#if DEBUG
                    Puts($"Disabling decay for spawned Row Boat {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerrhibID.ContainsValue(entity.net.ID))
            {
                if (vehicleDecay)
                {
#if DEBUG
                    Puts($"Enabling standard decay for spawned RHIB {entity.net.ID.ToString()}.");
#endif
                }
                else
                {
#if DEBUG
                    Puts($"Disabling decay for spawned RHIB {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerhabID.ContainsValue(entity.net.ID))
            {
                if (vehicleDecay)
                {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Hot Air Balloon {entity.net.ID.ToString()}.");
#endif
                }
                else
                {
#if DEBUG
                    Puts($"Disabling decay for spawned Hot Air Balloon {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerhorseID.ContainsValue(entity.net.ID))
            {
                if (vehicleDecay)
                {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Ridable Horse {entity.net.ID.ToString()}.");
#endif
                }
                else
                {
#if DEBUG
                    Puts($"Disabling decay for spawned Ridable Horse {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerchinookID.ContainsValue(entity.net.ID))
            {
                if (vehicleDecay)
                {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Chinook {entity.net.ID.ToString()}.");
#endif
                }
                else
                {
#if DEBUG
                    Puts($"Disabling decay for spawned Chinook {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playersedanID.ContainsValue(entity.net.ID))
            {
                if (vehicleDecay)
                {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Sedan {entity.net.ID.ToString()}.");
#endif
                }
                else
                {
#if DEBUG
                    Puts($"Disabling decay for spawned Sedan {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            return;
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!killOnSleep) return;
            if (player == null) return;

            if (storedData.playerminiID.ContainsKey(player.userID) == true)
            {
                uint deluint;
                storedData.playerminiID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill == null) return; // Didn't find it

                // Check for mounted players
                MiniCopter vehicle = tokill as MiniCopter;
                MiniCopter.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++)
                {
                    MiniCopter.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null)
                    {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted)
                        {
#if DEBUG
                            Puts("Minicopter owner sleeping but another player is mounted - cannot destroy vehicle");
#endif
                            return;
                        }
                    }
                }
#if DEBUG
                Puts("Minicopter owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playerminiID.Remove(player.userID);

                if (storedData.playerminicounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerminicounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerscrapID.ContainsKey(player.userID) == true)
            {
                uint deluint;
                storedData.playerscrapID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill == null) return; // Didn't find it

                // Check for mounted players
                ScrapTransportHelicopter vehicle = tokill as ScrapTransportHelicopter;
                ScrapTransportHelicopter.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++)
                {
                    ScrapTransportHelicopter.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null)
                    {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted)
                        {
#if DEBUG
                            Puts("Scrap Helicopter owner sleeping but another player is mounted - cannot destroy vehicle");
#endif
                            return;
                        }
                    }
                }
#if DEBUG
                Puts("Scrap Helicopter owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playerscrapID.Remove(player.userID);

                if (storedData.playerscrapcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerscrapcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerboatID.ContainsKey(player.userID) == true)
            {
                uint deluint;
                storedData.playerboatID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill == null) return; // Didn't find it

                // Check for mounted players
                MotorRowboat vehicle = tokill as MotorRowboat;
                MotorRowboat.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++)
                {
                    MotorRowboat.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null)
                    {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted)
                        {
#if DEBUG
                            Puts("Row Boat owner sleeping but another player is mounted - cannot destroy vehicle");
#endif
                            return;
                        }
                    }
                }
#if DEBUG
                Puts("Row Boat owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playerboatID.Remove(player.userID);

                if (storedData.playerboatcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerboatcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerrhibID.ContainsKey(player.userID) == true)
            {
                uint deluint;
                storedData.playerrhibID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill == null) return; // Didn't find it

                // Check for mounted players
                RHIB vehicle = tokill as RHIB;
                RHIB.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++)
                {
                    RHIB.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null)
                    {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted)
                        {
#if DEBUG
                            Puts("RHIB owner sleeping but another player is mounted - cannot destroy vehicle");
#endif
                            return;
                        }
                    }
                }
#if DEBUG
                Puts("RHIB owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playerrhibID.Remove(player.userID);

                if (storedData.playerrhibcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerrhibcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerhabID.ContainsKey(player.userID) == true)
            {
                uint deluint;
                storedData.playerhabID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill == null) return; // Didn't find it
#if DEBUG
                Puts("Hot Air Balloon owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playerhabID.Remove(player.userID);

                if (storedData.playerhabcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerhabcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerhorseID.ContainsKey(player.userID) == true)
            {
                uint deluint;
                storedData.playerhorseID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill == null) return; // Didn't find it

                // Check for mounted players
                RidableHorse vehicle = tokill as RidableHorse;
                RidableHorse.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++)
                {
                    RidableHorse.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null)
                    {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted)
                        {
#if DEBUG
                            Puts("Ridable Horse owner sleeping but another player is mounted - cannot destroy vehicle");
#endif
                            return;
                        }
                    }
                }
#if DEBUG
                Puts("Ridable Horse owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playerhorseID.Remove(player.userID);

                if (storedData.playerhorsecounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerhorsecounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerchinookID.ContainsKey(player.userID) == true)
            {
                uint deluint;
                storedData.playerchinookID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill == null) return; // Didn't find it

                // Check for mounted players
                CH47Helicopter vehicle = tokill as CH47Helicopter;
                CH47Helicopter.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++)
                {
                    CH47Helicopter.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null)
                    {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted)
                        {
#if DEBUG
                            Puts("Chinook owner sleeping but another player is mounted - cannot destroy vehicle");
#endif
                            return;
                        }
                    }
                }
#if DEBUG
                Puts("Chinook owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playerchinookID.Remove(player.userID);

                if (storedData.playerchinookcounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playerchinookcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playersedanID.ContainsKey(player.userID) == true)
            {
                uint deluint;
                storedData.playersedanID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint);
                if(tokill == null) return; // Didn't find it

                // Check for mounted players
                BaseCar vehicle = tokill as BaseCar;
                BaseCar.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++)
                {
                    BaseCar.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null)
                    {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted)
                        {
#if DEBUG
                            Puts("Sedan owner sleeping but another player is mounted - cannot destroy vehicle");
#endif
                            return;
                        }
                    }
                }
#if DEBUG
                Puts("Sedan owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playersedanID.Remove(player.userID);

                if (storedData.playersedancounter.ContainsKey(player.userID) & !useCooldown)
                {
                    storedData.playersedancounter.Remove(player.userID);
                }
                SaveData();
            }
        }
        #endregion
    }
}