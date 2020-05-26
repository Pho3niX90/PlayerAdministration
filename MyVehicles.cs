//#define DEBUG
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Convert = System.Convert;

namespace Oxide.Plugins {
    [Info("My Vehicles", "RFC1920/bsdinis/Pho3niX90", "0.2.5")]
    // Thanks to BuzZ[PHOQUE], the original author of this plugin
    [Description("Spawn vehicles")]
    public class MyVehicles : RustPlugin {
        string Prefix = "[My Vehicles]: ";
        const string prefabmini = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        const string prefabscrap = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";
        const string prefabboat = "assets/content/vehicles/boats/rowboat/rowboat.prefab";
        const string prefabrhib = "assets/content/vehicles/boats/rhib/rhib.prefab";
        const string prefabhab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab";
        const string prefabhorse = "assets/rust.ai/nextai/testridablehorse.prefab";
        const string prefabchinook = "assets/prefabs/npc/ch47/ch47.entity.prefab";
        const string prefabsedan = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";

        const string prefabpiano = "assets/prefabs/instruments/piano/piano.deployed.prefab";
        const string prefabautoturret = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
        const string prefabswitch = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
        const string prefabcctv = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab";

        private bool ConfigChanged;
        private bool useCooldown = true;
        private bool vehicleDecay = true;
        private bool allowWhenBlocked = false;
        private bool killOnSleep = false;
        private bool allowFuelIfUnlimited = false;
        private bool allowDriverDismountWhileFlying = true;
        private bool allowPassengerDismountWhileFlying = true;
        private bool ShowChinookMapMarker = false;
        private bool allowSpawnIfAlreadyOwnsVehicle = false;
        private float miniHealth = 750f;
        private float scrapHealth = 1000f;
        private float boatHealth = 400f;
        private float rhibHealth = 500f;
        private float habHealth = 1500f;
        private float horseHealth = 400f;
        private float chinookHealth = 1000f;
        private float sedanHealth = 300f;
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
        private float maxDismountHeight = 7f;
        const string MyVehiclesAdmin = "myvehicles.admin";
        const string MinicopterSpawn = "myvehicles.minispawn";
        const string CombatMinicopterSpawn = "myvehicles.combatminispawn";
        const string CCTVMinicopterSpawn = "myvehicles.cctvminispawn";
        const string Minicopter1Spawn = "myvehicles.mini1spawn";
        const string Minicopter2Spawn = "myvehicles.mini2spawn";
        const string Minicopter3Spawn = "myvehicles.mini3spawn";
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
        //        const string Horse1Spawn = "myvehicles.horse1spawn";
        const string HorseFetch = "myvehicles.horsefetch";
        const string HorseWhere = "myvehicles.horsewhere";
        const string HorseCooldown = "myvehicles.horsecooldown";
        const string ChinookSpawn = "myvehicles.chinookspawn";
        const string PianoChinookSpawn = "myvehicles.pianochinookspawn";
        const string ChinookFetch = "myvehicles.chinookfetch";
        const string ChinookWhere = "myvehicles.chinookwhere";
        const string ChinookCooldown = "myvehicles.chinookcooldown";
        const string SedanSpawn = "myvehicles.sedanspawn";
        const string PianoSedanSpawn = "myvehicles.pianosedanspawn";
        const string CombatSedanSpawn = "myvehicles.combatsedanspawn";
        const string CombatSedan2Spawn = "myvehicles.combatsedan2spawn";
        const string SedanFetch = "myvehicles.sedanfetch";
        const string SedanWhere = "myvehicles.sedanwhere";
        const string SedanCooldown = "myvehicles.sedancooldown";

        static LayerMask layerMask = LayerMask.GetMask("Terrain", "World", "Construction");
        double cooldownmini = 60;
        double cooldownscrap = 60;
        double cooldownboat = 30;
        double cooldownrhib = 45;
        double cooldownhab = 30;
        double cooldownhorse = 15;
        double cooldownchinook = 90;
        double cooldownsedan = 90;

        private Dictionary<ulong, ulong> currentMounts = new Dictionary<ulong, ulong>();
        public Dictionary<ulong, BaseVehicle> baseplayerminicop = new Dictionary<ulong, BaseVehicle>();
        public Dictionary<ulong, BaseVehicle> baseplayerscrapcop = new Dictionary<ulong, BaseVehicle>();
        public Dictionary<ulong, BaseVehicle> baseplayerboatcop = new Dictionary<ulong, BaseVehicle>();
        public Dictionary<ulong, BaseVehicle> baseplayerrhibcop = new Dictionary<ulong, BaseVehicle>();
        public Dictionary<ulong, HotAirBalloon> baseplayerhabcop = new Dictionary<ulong, HotAirBalloon>();
        public Dictionary<ulong, BaseVehicle> baseplayerhorsecop = new Dictionary<ulong, BaseVehicle>();
        public Dictionary<ulong, BaseVehicle> baseplayerchinookcop = new Dictionary<ulong, BaseVehicle>();
        public Dictionary<ulong, BaseVehicle> baseplayersedancop = new Dictionary<ulong, BaseVehicle>();
        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        class StoredData {
            public Dictionary<ulong, FrankenVehicle> playerminiID = new Dictionary<ulong, FrankenVehicle>();
            public Dictionary<ulong, double> playerminicounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, FrankenVehicle> playerscrapID = new Dictionary<ulong, FrankenVehicle>();
            public Dictionary<ulong, double> playerscrapcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, FrankenVehicle> playerboatID = new Dictionary<ulong, FrankenVehicle>();
            public Dictionary<ulong, double> playerboatcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, FrankenVehicle> playerrhibID = new Dictionary<ulong, FrankenVehicle>();
            public Dictionary<ulong, double> playerrhibcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, FrankenVehicle> playerhabID = new Dictionary<ulong, FrankenVehicle>();
            public Dictionary<ulong, double> playerhabcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, FrankenVehicle> playerhorseID = new Dictionary<ulong, FrankenVehicle>();
            public Dictionary<ulong, double> playerhorsecounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, FrankenVehicle> playerchinookID = new Dictionary<ulong, FrankenVehicle>();
            public Dictionary<ulong, double> playerchinookcounter = new Dictionary<ulong, double>();
            public Dictionary<ulong, FrankenVehicle> playersedanID = new Dictionary<ulong, FrankenVehicle>();
            public Dictionary<ulong, double> playersedancounter = new Dictionary<ulong, double>();
            public StoredData() {
            }
        }
        class FrankenVehicle {
            public uint vehicleId;
            public bool turret;
            public bool turret2;
            public bool cctv;
            public bool piano;
            public bool sideSeats;
            public bool backSeats;
            public FrankenVehicle(uint vehicleId) {
                this.vehicleId = vehicleId;
            }
            public FrankenVehicle() {
            }
            public FrankenVehicle EnableTurret() {
                turret = true;
                return this;
            }
            public FrankenVehicle EnableTurret2() {
                turret = true;
                return this;
            }
            public FrankenVehicle EnableCCTV() {
                cctv = true;
                return this;
            }
            public FrankenVehicle EnablePiano() {
                piano = true;
                return this;
            }
            public FrankenVehicle EnableSideSeats() {
                sideSeats = true;
                return this;
            }
            public FrankenVehicle EnableRotorSeats() {
                backSeats = true;
                return this;
            }
            public FrankenVehicle EnableAllSeats() {
                backSeats = true;
                sideSeats = true;
                return this;
            }
        }
        private StoredData storedData;
        private bool HasPermission(ConsoleSystem.Arg arg, string permname) => (arg.Connection.player as BasePlayer) == null ? true : permission.UserHasPermission((arg.Connection.player as BasePlayer).UserIDString, permname);

        #region loadunload
        void OnNewSave() {
            storedData = new StoredData();
            SaveData();
        }

        void Loaded() {
            LoadVariables();
            permission.RegisterPermission(MyVehiclesAdmin, this);
            permission.RegisterPermission(MinicopterSpawn, this);
            permission.RegisterPermission(CombatMinicopterSpawn, this);
            permission.RegisterPermission(CCTVMinicopterSpawn, this);
            permission.RegisterPermission(Minicopter1Spawn, this);
            permission.RegisterPermission(Minicopter2Spawn, this);
            permission.RegisterPermission(Minicopter3Spawn, this);
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
            //            permission.RegisterPermission(Horse1Spawn, this);
            permission.RegisterPermission(HorseFetch, this);
            permission.RegisterPermission(HorseWhere, this);
            permission.RegisterPermission(HorseCooldown, this);
            permission.RegisterPermission(ChinookSpawn, this);
            permission.RegisterPermission(PianoChinookSpawn, this);
            permission.RegisterPermission(ChinookFetch, this);
            permission.RegisterPermission(ChinookWhere, this);
            permission.RegisterPermission(ChinookCooldown, this);
            permission.RegisterPermission(SedanSpawn, this);
            permission.RegisterPermission(PianoSedanSpawn, this);
            permission.RegisterPermission(CombatSedanSpawn, this);
            permission.RegisterPermission(CombatSedan2Spawn, this);
            permission.RegisterPermission(SedanFetch, this);
            permission.RegisterPermission(SedanWhere, this);
            permission.RegisterPermission(SedanCooldown, this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }

        void Unload() {
            SaveData();
        }
        #endregion

        #region MESSAGES
        protected override void LoadDefaultMessages() {
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

        private string _(string msgId, BasePlayer player, params object[] args) {
            var msg = lang.GetMessage(msgId, this, player?.UserIDString);
            return args.Length > 0 ? string.Format(msg, args) : msg;
        }

        private void PrintMsgL(BasePlayer player, string msgId, params object[] args) {
            if (player == null) return;
            PrintMsg(player, _(msgId, player, args));
        }

        private void PrintMsg(BasePlayer player, string msg) {
            if (player == null) return;
            SendReply(player, $"{Prefix}{msg}");
        }

        // Chat message to online player with ulong
        private void ChatPlayerOnline(ulong ailldi, string message) {
            BasePlayer player = BasePlayer.FindByID(ailldi);
            if (player != null) {
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
        protected override void LoadDefaultConfig() {
            LoadVariables();
        }

        private void LoadVariables() {
            allowWhenBlocked = Convert.ToBoolean(GetConfig("Global", "Allow spawn when building blocked", false));
            allowSpawnIfAlreadyOwnsVehicle = Convert.ToBoolean(GetConfig("Global", "Allow spawning new vehicle if player already owns one", false));
            miniHealth = (float)Convert.ToSingle(GetConfig("Health", "Minicopter", 750));
            scrapHealth = (float)Convert.ToSingle(GetConfig("Health", "Scrap Helicopter", 1000));
            boatHealth = (float)Convert.ToSingle(GetConfig("Health", "Row Boat", 400));
            rhibHealth = (float)Convert.ToSingle(GetConfig("Health", "RHIB", 500));
            habHealth = (float)Convert.ToSingle(GetConfig("Health", "Hot Air Balloon", 1500));
            horseHealth = (float)Convert.ToSingle(GetConfig("Health", "Ridable Horse", 400));
            chinookHealth = (float)Convert.ToSingle(GetConfig("Health", "Chinook", 1000));
            sedanHealth = (float)Convert.ToSingle(GetConfig("Health", "Sedan", 300));
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
            allowDriverDismountWhileFlying = Convert.ToBoolean(GetConfig("Global", "Allow driver dismount while flying", true));
            allowPassengerDismountWhileFlying = Convert.ToBoolean(GetConfig("Global", "Allow passenger dismount while flying", true));
            maxDismountHeight = Convert.ToSingle(GetConfig("Maximum height for dismount", "Value in meters", "7"));

            if (!ConfigChanged) return;
            SaveConfig();
            ConfigChanged = false;
        }

        private object GetConfig(string menu, string datavalue, object defaultValue) {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null) {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                ConfigChanged = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value)) {
                value = defaultValue;
                data[datavalue] = value;
                ConfigChanged = true;
            }
            return value;
        }

        void SaveData() {
            // Save the data file as we add/remove vehicles.
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }
        #endregion

        #region bools
        private bool IsInWater(BasePlayer player) {
            var modelState = player.modelState;
            return modelState != null && modelState.waterLevel > 0;
        }

        #endregion

        private object OnEntityTakeDamage(StaticInstrument piano, HitInfo info) {
            if (piano != null) {
                var scrapTransport = piano.GetComponentInParent<ScrapTransportHelicopter>();
                if (scrapTransport != null) {
                    scrapTransport.Hurt(info);
                    return true;
                }
                var rhib = piano.GetComponentInParent<RHIB>();
                if (rhib != null) {
                    rhib.Hurt(info);
                    return true;
                }
                var chinook = piano.GetComponentInParent<CH47Helicopter>();
                if (chinook != null) {
                    chinook.Hurt(info);
                    return true;
                }
                var sedan = piano.GetComponentInParent<BaseCar>();
                if (sedan != null) {
                    sedan.Hurt(info);
                    return true;
                }
            }

            return null;
        }
        private object OnEntityTakeDamage(CCTV_RC cctv, HitInfo info) {
            if (cctv != null) {
                var miniCopter = cctv.GetComponentInParent<MiniCopter>();
                if (miniCopter != null) {
                    miniCopter.Hurt(info);
                    return true;
                }
            }

            return null;
        }

        #region Hooks
        bool OnMiniCanCreateSideSeats(BaseVehicle entity) {
            if (storedData.playerminiID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                FrankenVehicle fc = storedData.playerminiID.First(x => x.Value.vehicleId == entity.net.ID).Value;
                return fc.sideSeats;
            }
            return false;
        }

        bool OnMiniCanCreateRotorSeat(BaseVehicle entity) {
            if (storedData.playerminiID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                FrankenVehicle fc = storedData.playerminiID.First(x => x.Value.vehicleId == entity.net.ID).Value;
                return fc.backSeats;
            }
            return false;
        }

        //        bool OnHorseCanCreateBackSeat(BaseVehicle entity) {
        //            bool makeit = createHorseBackSeat;
        //            createHorseBackSeat = false;
        //            return makeit;
        //        }
        /*
                bool CreatePianoOnVehicle(BaseVehicle entity) {
                    bool makeit = createPiano;
                    createPiano = false;
                    return makeit;
                }

                bool CreateCCTVOnVehicle(BaseVehicle entity) {
                    bool makeit = createCCTV;
                    createCCTV = false;
                    return makeit;
                }

                bool CreateAutoTurretOnVehicle(BaseVehicle entity) {
                    bool makeit = createAutoTurret;
                    createAutoTurret = false;
                    return makeit;
                }

                bool CreateAutoTurret2OnVehicle(BaseVehicle entity) {
                    bool makeit = createAutoTurret2;
                    createAutoTurret2 = false;
                    return makeit;
                }
                */
        #endregion

        #region chatcommands
        // Chat spawn Combat Minicopter
        [ChatCommand("mycombatmini")]
        private void SpawnMyCombatMinicopterChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, CombatMinicopterSpawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            SpawnMini(player, new FrankenVehicle().EnableTurret());
        }
        // Chat spawn CCTV Minicopter
        [ChatCommand("mycctvmini")]
        private void SpawnMyCCTVMinicopterChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, CCTVMinicopterSpawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            SpawnMini(player, new FrankenVehicle().EnableCCTV());
        }

        FrankenVehicle GetFrankenVehicle(Dictionary<ulong, FrankenVehicle> fvs, BaseEntity entity) {
            if (fvs.ContainsKey(entity.OwnerID)) {
                return storedData.playerminiID[entity.OwnerID];
            }
            return new FrankenVehicle();
        }

        private void OnEntitySpawned(MiniCopter miniCopter) {
            FrankenVehicle fv = GetFrankenVehicle(storedData.playerminiID, miniCopter);

            if (fv.turret) {
                //miniCopter.liftFraction = 0.25f;
                //miniCopter.torqueScale = new Vector3(400f, 400f, 200f);
                var autoturret = GameManager.server.CreateEntity(prefabautoturret, miniCopter.transform.position) as AutoTurret;
                if (autoturret == null) return;
                autoturret.Spawn();
                autoturret.pickup.enabled = false;
                autoturret.sightRange = 50f;
                UnityEngine.Object.Destroy(autoturret.GetComponent<DestroyOnGroundMissing>());
                autoturret.SetParent(miniCopter);
                autoturret.transform.localPosition = new Vector3(0, 0.001f, 2.47f);
                autoturret.transform.localRotation = Quaternion.Euler(0, 0, 0);
                autoturret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID { userid = miniCopter.OwnerID, username = BasePlayer.FindByID(miniCopter.OwnerID)?.displayName });
                timer.Once(1f, () => {
                    AddSwitch(autoturret);
                });
            }
            if (fv.cctv) {
                var cctv = GameManager.server.CreateEntity(prefabcctv, miniCopter.transform.position) as CCTV_RC;
                if (cctv == null) return;
                cctv.Spawn();
                cctv.pickup.enabled = false;
                UnityEngine.Object.Destroy(cctv.GetComponent<DestroyOnGroundMissing>());
                cctv.SetParent(miniCopter);
                cctv.transform.localPosition = new Vector3(0, 1.9f, 0);
                cctv.transform.localRotation = Quaternion.Euler(0, 0, 0);
                cctv.UpdateIdentifier(RandomString(3));
                cctv.UpdateHasPower(25, 1);
            }
        }

        private void OnEntitySpawned(ScrapTransportHelicopter scrapTransport) {
            FrankenVehicle fv = GetFrankenVehicle(storedData.playerscrapID, scrapTransport);

            if (!fv.piano) return;
            var piano = GameManager.server.CreateEntity(prefabpiano, scrapTransport.transform.position) as StaticInstrument;
            if (piano == null) return;
            piano.Spawn();
            piano.pickup.enabled = false;
            UnityEngine.Object.Destroy(piano.GetComponent<DestroyOnGroundMissing>());
            piano.SetParent(scrapTransport);
            piano.transform.localPosition = new Vector3(0, 0.85f, 0.5f);
            piano.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        private string RandomString(int length) {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            var stringChars = new char[length];
            var random = new System.Random();

            for (int i = 0; i < stringChars.Length; i++) {
                stringChars[i] = chars[random.Next(chars.Length)];
            }
            return new String(stringChars);
        }

        private void AddSwitch(AutoTurret autoturret) {
            var autoturretSwitch = autoturret.GetComponentInChildren<ElectricSwitch>();
            if (autoturretSwitch != null) {
                UnityEngine.Object.Destroy(autoturretSwitch.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(autoturretSwitch.GetComponent<GroundWatch>());
                autoturretSwitch.pickup.enabled = false;
                return;
            }
            autoturretSwitch = GameManager.server.CreateEntity(prefabswitch, autoturret.transform.position)?.GetComponent<ElectricSwitch>();
            if (autoturretSwitch == null) {
                return;
            }
            autoturretSwitch.pickup.enabled = false;
            autoturretSwitch.SetParent(autoturret);
            autoturretSwitch.transform.localPosition = new Vector3(0f, -0.65f, 0.325f);
            autoturretSwitch.transform.localRotation = Quaternion.Euler(0, 0, 0);
            autoturretSwitch.Spawn();
            UnityEngine.Object.Destroy(autoturretSwitch.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.Destroy(autoturretSwitch.GetComponent<GroundWatch>());
        }
        private object OnEntityTakeDamage(ElectricSwitch autoturretSwitch, HitInfo info) {
            if (autoturretSwitch != null) {
                var autoturret = autoturretSwitch.GetComponentInParent<AutoTurret>();
                if (autoturret != null) {
                    autoturret.Hurt(info);
                    return true;
                }
            }
            return null;
        }
        private void OnSwitchToggle(ElectricSwitch autoturretSwitch, BasePlayer player) {
            var autoturret = autoturretSwitch.GetComponentInParent<AutoTurret>();
            if (autoturret == null) {
                return;
            }
            if (autoturret.authorizedPlayers.Any(x => x.userid == player.userID) == false) {
                player.ChatMessage("No permission");
                autoturretSwitch.SetSwitch(!autoturretSwitch.IsOn());
                return;
            }
            if (autoturret.GetParentEntity() is MiniCopter) {
                ToggleTurret(autoturret);
            }
            if (autoturret.GetParentEntity() is BaseCar) {
                ToggleTurret(autoturret);
            }
        }
        private void ToggleTurret(AutoTurret autoturret) {
            if (autoturret.IsOnline()) {
                PowerTurretOff(autoturret);
            } else {
                PowerTurretOn(autoturret);
            }
        }
        private void PowerTurretOn(AutoTurret autoturret) {
            autoturret.SetFlag(BaseEntity.Flags.Reserved8, true);
            autoturret.SetIsOnline(true);
        }
        private void PowerTurretOff(AutoTurret autoturret) {
            autoturret.SetFlag(BaseEntity.Flags.Reserved8, false);
            autoturret.SetIsOnline(false);
        }

        // Chat spawn Minicopter1
        [ChatCommand("mymini1")]
        private void SpawnMyMinicopter1ChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, Minicopter1Spawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            SpawnMini(player, new FrankenVehicle().EnableRotorSeats());
        }

        // Chat spawn Minicopter2
        [ChatCommand("mymini2")]
        private void SpawnMyMinicopter2ChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, Minicopter2Spawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            SpawnMini(player, new FrankenVehicle().EnableSideSeats());
        }

        // Chat spawn Minicopter3
        [ChatCommand("mymini3")]
        private void SpawnMyMinicopter3ChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, Minicopter3Spawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            SpawnMini(player, new FrankenVehicle().EnableRotorSeats());
        }

        // Chat spawn Minicopter
        [ChatCommand("mymini")]
        private void SpawnMyMinicopterChatCommand(BasePlayer player, string command, string[] args) {

            bool isspawner = permission.UserHasPermission(player.UserIDString, MinicopterSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!allowSpawnIfAlreadyOwnsVehicle) {
                if (storedData.playerminiID.ContainsKey(player.userID) == true) {
                    PrintMsgL(player, "AlreadyMiniMsg");
                    return;
                }
            }
            SpawnMini(player, new FrankenVehicle());
        }
        void SpawnMini(BasePlayer player, FrankenVehicle fc) {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;
            bool hascooldown = permission.UserHasPermission(player.UserIDString, MinicopterCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true) {
                if (storedData.playerminicounter.ContainsKey(player.userID) == false) {
                    storedData.playerminicounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                } else {
                    double count;
                    storedData.playerminicounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownmini * 60)) {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerminicounter.Remove(player.userID);
                        SaveData();
                    } else {
                        secsleft = Math.Abs((int)((cooldownmini * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0) {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownMiniMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            } else {
                if (storedData.playerminicounter.ContainsKey(player.userID)) {
                    storedData.playerminicounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyMinicopter(player, fc);
        }

        // Fetch Minicopter
        [ChatCommand("gmini")]
        private void GetMyMinicopterChatCommand(BasePlayer player, string command, string[] args) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, MinicopterSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, MinicopterFetch);
            if (!(canspawn & canfetch)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerminiID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerminiID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    // Distance check
                    if (fetchdistancemini > 0f) {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancemini) {
                            PrintMsgL(player, "DistanceMiniMsg", fetchdistancemini);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as BaseVehicle;
                    BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++) {
                        BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null) {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted) {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 1.5f), player.transform.position.y + 1f, (float)(player.transform.position.z + 0f));
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundMiniMsg", newLoc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundMiniMsg");
                return;
            }
        }

        // Find Minicopter
        [ChatCommand("wmini")]
        private void WhereIsMyMinicopterChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, MinicopterWhere);
            if (canspawn == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerminiID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerminiID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundMiniMsg", loc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundMiniMsg");
                return;
            }
        }

        // Chat despawn Minicopter
        [ChatCommand("nomini")]
        private void KillMyMinicopterChatCommand(BasePlayer player, string command, string[] args) {
            bool isspawner = permission.UserHasPermission(player.UserIDString, MinicopterSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyMinicopterPlease(player);
        }

        // Chat spawn Piano Scrap Helicopter
        [ChatCommand("mypianoheli")]
        private void SpawnMyPianoScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, PianoScrapSpawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            SpawnMini(player, new FrankenVehicle().EnablePiano());
        }


        // Chat spawn Scrap Helicopter
        [ChatCommand("myheli")]
        private void SpawnMyScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args) {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, ScrapSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!allowSpawnIfAlreadyOwnsVehicle) {
                if (storedData.playerscrapID.ContainsKey(player.userID) == true) {
                    PrintMsgL(player, "AlreadyScrapMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, ScrapCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true) {
                if (storedData.playerscrapcounter.ContainsKey(player.userID) == false) {
                    storedData.playerscrapcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                } else {
                    double count;
                    storedData.playerscrapcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownscrap * 60)) {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerscrapcounter.Remove(player.userID);
                        SaveData();
                    } else {
                        secsleft = Math.Abs((int)((cooldownscrap * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0) {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownScrapMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            } else {
                if (storedData.playerscrapcounter.ContainsKey(player.userID)) {
                    storedData.playerscrapcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyScrapTransportHelicopter(player, new FrankenVehicle());
        }

        // Fetch Scrap Helicopter
        [ChatCommand("gheli")]
        private void GetMyScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, ScrapSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, ScrapFetch);
            if (!(canspawn & canfetch)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerscrapID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerscrapID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    // Distance check
                    if (fetchdistancescrap > 0f) {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancescrap) {
                            PrintMsgL(player, "DistanceScrapMsg", fetchdistancescrap);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as BaseVehicle;
                    BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++) {
                        BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null) {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted) {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 3f), player.transform.position.y + 1f, (float)(player.transform.position.z + 0f));
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundScrapMsg", newLoc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundScrapMsg");
                return;
            }
        }

        // Find Scrap Helicopter
        [ChatCommand("wheli")]
        private void WhereIsMyScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, ScrapWhere);
            if (canspawn == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerscrapID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerscrapID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundScrapMsg", loc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundScrapMsg");
                return;
            }
        }

        // Chat despawn Scrap Helicopter
        [ChatCommand("noheli")]
        private void KillMyScrapTransportHelicopterChatCommand(BasePlayer player, string command, string[] args) {
            bool isspawner = permission.UserHasPermission(player.UserIDString, ScrapSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyScrapTransportHelicopterPlease(player);
        }

        // Chat spawn Row Boat
        [ChatCommand("myboat")]
        private void SpawnMyRowBoatChatCommand(BasePlayer player, string command, string[] args) {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, BoatSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (!IsInWater(player)) {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!allowSpawnIfAlreadyOwnsVehicle) {
                if (storedData.playerboatID.ContainsKey(player.userID) == true) {
                    PrintMsgL(player, "AlreadyBoatMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, BoatCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true) {
                if (storedData.playerboatcounter.ContainsKey(player.userID) == false) {
                    storedData.playerboatcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                } else {
                    double count;
                    storedData.playerboatcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownboat * 60)) {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerboatcounter.Remove(player.userID);
                        SaveData();
                    } else {
                        secsleft = Math.Abs((int)((cooldownboat * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0) {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownBoatMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            } else {
                if (storedData.playerboatcounter.ContainsKey(player.userID)) {
                    storedData.playerboatcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyRowBoat(player, new FrankenVehicle());
        }

        // Fetch Row Boat
        [ChatCommand("gboat")]
        private void GetMyRowBoatChatCommand(BasePlayer player, string command, string[] args) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (!IsInWater(player)) {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, BoatSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, BoatFetch);
            if (!(canspawn & canfetch)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerboatID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerboatID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    // Distance check
                    if (fetchdistanceboat > 0f) {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistanceboat) {
                            PrintMsgL(player, "DistanceBoatMsg", fetchdistanceboat);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as BaseVehicle;
                    BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++) {
                        BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null) {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted) {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 1.5f), player.transform.position.y, (float)(player.transform.position.z + 0f));
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundBoatMsg", newLoc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundBoatMsg");
                return;
            }
        }

        // Find Row Boat
        [ChatCommand("wboat")]
        private void WhereIsMyRowBoatChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, BoatWhere);
            if (canspawn == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerboatID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerboatID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundBoatMsg", loc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundBoatMsg");
                return;
            }
        }

        // Chat despawn Row Boat
        [ChatCommand("noboat")]
        private void KillMyRowBoatChatCommand(BasePlayer player, string command, string[] args) {
            bool isspawner = permission.UserHasPermission(player.UserIDString, BoatSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyRowBoatPlease(player);
        }

        // Chat spawn Piano RHIB
        [ChatCommand("mypianorhib")]
        private void SpawnMyPianoRHIBChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, PianoRHIBSpawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            SpawnMyRHIB(player, new FrankenVehicle().EnablePiano());
        }
        private void OnEntitySpawned(RHIB rhib) {
            FrankenVehicle fv = GetFrankenVehicle(storedData.playerrhibID, rhib);

            if (!fv.piano) return;
            var piano = GameManager.server.CreateEntity(prefabpiano, rhib.transform.position) as StaticInstrument;
            if (piano == null) return;
            piano.Spawn();
            piano.pickup.enabled = false;
            UnityEngine.Object.Destroy(piano.GetComponent<DestroyOnGroundMissing>());
            piano.SetParent(rhib);
            piano.transform.localPosition = new Vector3(0, 1.25f, 4.25f);
            piano.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }

        // Chat spawn RHIB
        [ChatCommand("myrhib")]
        private void SpawnMyRHIBChatCommand(BasePlayer player, string command, string[] args) {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, RHIBSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (!IsInWater(player)) {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!allowSpawnIfAlreadyOwnsVehicle) {
                if (storedData.playerrhibID.ContainsKey(player.userID) == true) {
                    PrintMsgL(player, "AlreadyRHIBMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, RHIBCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true) {
                if (storedData.playerrhibcounter.ContainsKey(player.userID) == false) {
                    storedData.playerrhibcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                } else {
                    double count;
                    storedData.playerrhibcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownrhib * 60)) {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerrhibcounter.Remove(player.userID);
                        SaveData();
                    } else {
                        secsleft = Math.Abs((int)((cooldownrhib * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0) {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownRHIBMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            }
            SpawnMyRHIB(player, new FrankenVehicle());
        }

        // Fetch RHIB
        [ChatCommand("grhib")]
        private void GetMyRHIBChatCommand(BasePlayer player, string command, string[] args) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (!IsInWater(player)) {
                PrintMsgL(player, "NotInWaterMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, RHIBSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, RHIBFetch);
            if (!(canspawn & canfetch)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerrhibID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerrhibID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    // Distance check
                    if (fetchdistancerhib > 0f) {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancerhib) {
                            PrintMsgL(player, "DistanceRHIBMsg", fetchdistancerhib);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as BaseVehicle;
                    BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++) {
                        BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null) {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted) {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 2.5f), player.transform.position.y, (float)(player.transform.position.z + 0f));
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundRHIBMsg", newLoc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundRHIBMsg");
                return;
            }
        }

        // Find RHIB
        [ChatCommand("wrhib")]
        private void WhereIsMyRHIBChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, RHIBWhere);
            if (canspawn == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerrhibID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerrhibID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundRHIBMsg", loc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundRHIBMsg");
                return;
            }
        }

        // Chat despawn RHIB
        [ChatCommand("norhib")]
        private void KillMyRHIBChatCommand(BasePlayer player, string command, string[] args) {
            bool isspawner = permission.UserHasPermission(player.UserIDString, RHIBSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyRHIBPlease(player);
        }

        // Chat spawn Hot Air Balloon
        [ChatCommand("myhab")]
        private void SpawnMyHotAirBalloonChatCommand(BasePlayer player, string command, string[] args) {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, HABSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!allowSpawnIfAlreadyOwnsVehicle) {
                if (storedData.playerhabID.ContainsKey(player.userID) == true) {
                    PrintMsgL(player, "AlreadyHABMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, HABCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true) {
                if (storedData.playerhabcounter.ContainsKey(player.userID) == false) {
                    storedData.playerhabcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                } else {
                    double count;
                    storedData.playerhabcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownhab * 60)) {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerhabcounter.Remove(player.userID);
                        SaveData();
                    } else {
                        secsleft = Math.Abs((int)((cooldownhab * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0) {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownHABMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            } else {
                if (storedData.playerhabcounter.ContainsKey(player.userID)) {
                    storedData.playerhabcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyHotAirBalloon(player, new FrankenVehicle());
        }

        // Fetch Hot Air Balloon
        [ChatCommand("ghab")]
        private void GetMyHotAirBalloonChatCommand(BasePlayer player, string command, string[] args) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, HABSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, HABFetch);
            if (!(canspawn & canfetch)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerhabID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerhabID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    // Distance check
                    if (fetchdistancehab > 0f) {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancehab) {
                            PrintMsgL(player, "DistanceHABMsg", fetchdistancehab);
                            return;
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + -2.5f), player.transform.position.y + 1f, (float)(player.transform.position.z + -2.5f));
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundHABMsg", newLoc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundHABMsg");
                return;
            }
        }

        // Find Hot Air Balloon
        [ChatCommand("whab")]
        private void WhereIsMyHotAirBalloonChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, HABWhere);
            if (canspawn == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerhabID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerhabID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundHABMsg", loc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundHABMsg");
                return;
            }
        }

        // Chat despawn Hot Air Balloon
        [ChatCommand("nohab")]
        private void KillMyHotAirBalloonChatCommand(BasePlayer player, string command, string[] args) {
            bool isspawner = permission.UserHasPermission(player.UserIDString, HABSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyHotAirBalloonPlease(player);
        }

        //        // Chat spawn Ridable Horse1
        //        [ChatCommand("myhorse1")]
        //        private void SpawnMyRidableHorse1ChatCommand(BasePlayer player, string command, string[] args)
        //        {
        //            bool canspawn = permission.UserHasPermission(player.UserIDString, Horse1Spawn);
        //            if(!(canspawn))
        //            {
        //                PrintMsgL(player, "NoPermMsg");
        //                return;
        //            }
        //            createHorseBackSeat = true;
        //            SpawnMyRidableHorseChatCommand(player, command, args);
        //        }

        // Chat spawn Ridable Horse
        [ChatCommand("myhorse")]
        private void SpawnMyRidableHorseChatCommand(BasePlayer player, string command, string[] args) {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, HorseSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (IsInWater(player)) {
                PrintMsgL(player, "InWaterMsg");
                return;
            }

            if (!player.IsOnGround()) {
                PrintMsgL(player, "FlyingMsg");
                return;
            }

            if (!allowSpawnIfAlreadyOwnsVehicle) {
                if (storedData.playerhorseID.ContainsKey(player.userID) == true) {
                    PrintMsgL(player, "AlreadyHorseMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, HorseCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true) {
                if (storedData.playerhorsecounter.ContainsKey(player.userID) == false) {
                    storedData.playerhorsecounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                } else {
                    double count;
                    storedData.playerhorsecounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownhorse * 60)) {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerhorsecounter.Remove(player.userID);
                        SaveData();
                    } else {
                        secsleft = Math.Abs((int)((cooldownhorse * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0) {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownHorseMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            } else {
                if (storedData.playerhorsecounter.ContainsKey(player.userID)) {
                    storedData.playerhorsecounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyRidableHorse(player, new FrankenVehicle());
        }

        // Fetch Ridable Horse
        [ChatCommand("ghorse")]
        private void GetMyRidableHorseChatCommand(BasePlayer player, string command, string[] args) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (IsInWater(player)) {
                PrintMsgL(player, "InWaterMsg");
                return;
            }

            if (!player.IsOnGround()) {
                PrintMsgL(player, "FlyingMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, HorseSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, HorseFetch);
            if (!(canspawn & canfetch)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerhorseID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerhorseID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    // Distance check
                    if (fetchdistancehorse > 0f) {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancehorse) {
                            PrintMsgL(player, "DistanceHorseMsg", fetchdistancehorse);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as BaseVehicle;
                    BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++) {
                        BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null) {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted) {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 1f), player.transform.position.y, (float)(player.transform.position.z + 1f));
                    newLoc = GetGroundPosition(newLoc);
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundHorseMsg", newLoc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundHorseMsg");
                return;
            }
        }

        // Find Ridable Horse
        [ChatCommand("whorse")]
        private void WhereIsMyRidableHorseChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, HorseWhere);
            if (canspawn == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerhorseID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerhorseID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundHorseMsg", loc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundHorseMsg");
                return;
            }
        }

        // Chat despawn Ridable Horse
        [ChatCommand("nohorse")]
        private void KillMyRidableHorseChatCommand(BasePlayer player, string command, string[] args) {
            bool isspawner = permission.UserHasPermission(player.UserIDString, HorseSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyRidableHorsePlease(player);
        }

        // Chat spawn Piano Chinook
        [ChatCommand("mypianoch47")]
        private void SpawnMyPianoChinookChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, PianoChinookSpawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            SpawnMyChinook(player, new FrankenVehicle().EnablePiano());
        }
        private void OnEntitySpawned(CH47Helicopter chinook) {
            FrankenVehicle fv = GetFrankenVehicle(storedData.playerchinookID, chinook);
            if (!fv.piano) return;
            var piano = GameManager.server.CreateEntity(prefabpiano, chinook.transform.position) as StaticInstrument;
            if (piano == null) return;
            piano.Spawn();
            piano.pickup.enabled = false;
            UnityEngine.Object.Destroy(piano.GetComponent<DestroyOnGroundMissing>());
            piano.SetParent(chinook);
            piano.transform.localPosition = new Vector3(0, 1.3f, 4.5f);
            piano.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        // Chat spawn Chinook
        [ChatCommand("mych47")]
        private void SpawnMyChinookChatCommand(BasePlayer player, string command, string[] args) {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, ChinookSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!allowSpawnIfAlreadyOwnsVehicle) {
                if (storedData.playerchinookID.ContainsKey(player.userID) == true) {
                    PrintMsgL(player, "AlreadyChinookMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, ChinookCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true) {
                if (storedData.playerchinookcounter.ContainsKey(player.userID) == false) {
                    storedData.playerchinookcounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                } else {
                    double count;
                    storedData.playerchinookcounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownchinook * 60)) {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playerchinookcounter.Remove(player.userID);
                        SaveData();
                    } else {
                        secsleft = Math.Abs((int)((cooldownchinook * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0) {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownChinookMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            } else {
                if (storedData.playerchinookcounter.ContainsKey(player.userID)) {
                    storedData.playerchinookcounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMyChinook(player, new FrankenVehicle());
        }

        // Fetch Chinook
        [ChatCommand("gch47")]
        private void GetMyChinookChatCommand(BasePlayer player, string command, string[] args) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, ChinookSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, ChinookFetch);
            if (!(canspawn & canfetch)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerchinookID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerchinookID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    // Distance check
                    if (fetchdistancechinook > 0f) {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancechinook) {
                            PrintMsgL(player, "DistanceChinookMsg", fetchdistancechinook);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as BaseVehicle;
                    BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++) {
                        BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null) {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted) {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 3f), player.transform.position.y + 1f, (float)(player.transform.position.z + 0f));
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundChinookMsg", newLoc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundChinookMsg");
                return;
            }
        }

        // Find Chinook
        [ChatCommand("wch47")]
        private void WhereIsMyChinookChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, ChinookWhere);
            if (canspawn == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playerchinookID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playerchinookID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundChinookMsg", loc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundChinookMsg");
                return;
            }
        }

        // Chat despawn Chinook
        [ChatCommand("noch47")]
        private void KillMyChinookChatCommand(BasePlayer player, string command, string[] args) {
            bool isspawner = permission.UserHasPermission(player.UserIDString, ChinookSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMyChinookPlease(player);
        }

        // Chat spawn Piano Sedan
        [ChatCommand("mypianosedan")]
        private void SpawnMyPianoSedanChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, PianoSedanSpawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            SpawnMySedan(player, new FrankenVehicle().EnablePiano());
        }
        // Chat spawn Combat Sedan
        [ChatCommand("mycombatsedan")]
        private void SpawnMyCombatSedanChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, CombatSedanSpawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            SpawnMySedan(player, new FrankenVehicle().EnableTurret());
        }
        // Chat spawn Combat Sedan 2
        [ChatCommand("mycombatsedan2")]
        private void SpawnMyCombatSedan2ChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, CombatSedan2Spawn);
            if (!(canspawn)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            SpawnMySedan(player, new FrankenVehicle().EnableTurret().EnableTurret2());
        }
        private void OnEntitySpawned(BaseCar sedan) {
            FrankenVehicle fv = GetFrankenVehicle(storedData.playersedanID, sedan);
            if (fv.piano) {
                var piano = GameManager.server.CreateEntity(prefabpiano, sedan.transform.position) as StaticInstrument;
                if (piano == null) return;
                piano.Spawn();
                piano.pickup.enabled = false;
                UnityEngine.Object.Destroy(piano.GetComponent<DestroyOnGroundMissing>());
                piano.SetParent(sedan);
                piano.transform.localPosition = new Vector3(0, 1.7f, 0);
                piano.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            if (fv.turret) {
                var autoturret = GameManager.server.CreateEntity(prefabautoturret, sedan.transform.position) as AutoTurret;
                if (autoturret == null) return;
                autoturret.Spawn();
                autoturret.pickup.enabled = false;
                autoturret.sightRange = 50f;
                UnityEngine.Object.Destroy(autoturret.GetComponent<DestroyOnGroundMissing>());
                autoturret.SetParent(sedan);
                autoturret.transform.localPosition = new Vector3(0f, 1.64f, 0.37f);
                autoturret.transform.localRotation = Quaternion.Euler(0, 0, 0);
                autoturret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID { userid = sedan.OwnerID, username = BasePlayer.FindByID(sedan.OwnerID)?.displayName });
                timer.Once(1f, () => {
                    AddSwitch(autoturret);
                });
            }
            if (fv.turret2) {
                var autoturret = GameManager.server.CreateEntity(prefabautoturret, sedan.transform.position) as AutoTurret;
                if (autoturret == null) return;
                autoturret.Spawn();
                autoturret.pickup.enabled = false;
                autoturret.sightRange = 50f;
                UnityEngine.Object.Destroy(autoturret.GetComponent<DestroyOnGroundMissing>());
                autoturret.SetParent(sedan);
                autoturret.transform.localPosition = new Vector3(0f, 1.1f, -1.65f);
                autoturret.transform.localRotation = Quaternion.Euler(0, 180, 0);
                autoturret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID { userid = sedan.OwnerID, username = BasePlayer.FindByID(sedan.OwnerID)?.displayName });
                timer.Once(1f, () => {
                    AddSwitch(autoturret);
                });
            }
        }

        // Chat spawn Sedan
        [ChatCommand("mysedan")]
        private void SpawnMySedanChatCommand(BasePlayer player, string command, string[] args) {
            double secondsSinceEpoch = DateTime.UtcNow.Subtract(epoch).TotalSeconds;

            bool isspawner = permission.UserHasPermission(player.UserIDString, SedanSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            if (!allowSpawnIfAlreadyOwnsVehicle) {
                if (storedData.playersedanID.ContainsKey(player.userID) == true) {
                    PrintMsgL(player, "AlreadySedanMsg");
                    return;
                }
            }

            bool hascooldown = permission.UserHasPermission(player.UserIDString, SedanCooldown);
            if (!useCooldown) hascooldown = false;

            int secsleft = 0;
            if (hascooldown == true) {
                if (storedData.playersedancounter.ContainsKey(player.userID) == false) {
                    storedData.playersedancounter.Add(player.userID, secondsSinceEpoch);
                    SaveData();
                } else {
                    double count;
                    storedData.playersedancounter.TryGetValue(player.userID, out count);

                    if ((secondsSinceEpoch - count) > (cooldownsedan * 60)) {
#if DEBUG
                        Puts($"Player reached cooldown.  Clearing data.");
#endif
                        storedData.playersedancounter.Remove(player.userID);
                        SaveData();
                    } else {
                        secsleft = Math.Abs((int)((cooldownsedan * 60) - (secondsSinceEpoch - count)));

                        if (secsleft > 0) {
#if DEBUG
                            Puts($"Player DID NOT reach cooldown. Still {secsleft.ToString()} secs left.");
#endif
                            PrintMsgL(player, "CooldownSedanMsg", secsleft.ToString());
                            return;
                        }
                    }
                }
            } else {
                if (storedData.playersedancounter.ContainsKey(player.userID)) {
                    storedData.playersedancounter.Remove(player.userID);
                    SaveData();
                }
            }
            SpawnMySedan(player, new FrankenVehicle());
        }

        // Fetch Sedan
        [ChatCommand("gsedan")]
        private void GetMySedanChatCommand(BasePlayer player, string command, string[] args) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            if (player.GetMountedVehicle() || player.GetParentEntity()) {
                PrintMsgL(player, "MountedOrParentedMsg");
                return;
            }

            bool canspawn = permission.UserHasPermission(player.UserIDString, SedanSpawn);
            bool canfetch = permission.UserHasPermission(player.UserIDString, SedanFetch);
            if (!(canspawn & canfetch)) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playersedanID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playersedanID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    // Distance check
                    if (fetchdistancesedan > 0f) {
                        if (Vector3.Distance(player.transform.position, foundit.transform.position) > fetchdistancesedan) {
                            PrintMsgL(player, "DistanceSedanMsg", fetchdistancesedan);
                            return;
                        }
                    }

                    // Check for and dismount all players before moving the vehicle
                    var vehicle = foundit as BaseVehicle;
                    BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                    for (int i = 0; i < (int)mountpoints.Length; i++) {
                        BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                        if (mountPointInfo.mountable != null) {
                            BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                            if (mounted) {
                                Vector3 player_pos = mounted.transform.position + new Vector3(1, 0, 1);
                                mounted.DismountObject();
                                mounted.MovePosition(player_pos);
                                mounted.SendNetworkUpdateImmediate(false);
                                mounted.ClientRPCPlayer(null, player, "ForcePositionTo", player_pos);
                                mountPointInfo.mountable._mounted = null;
                            }
                        }
                    }
                    var newLoc = new Vector3((float)(player.transform.position.x + 1.7f), player.transform.position.y + 0.5f, (float)(player.transform.position.z + 0f));
                    foundit.transform.position = newLoc;
                    PrintMsgL(player, "FoundSedanMsg", newLoc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundSedanMsg");
                return;
            }
        }

        // Find Sedan
        [ChatCommand("wsedan")]
        private void WhereIsMySedanChatCommand(BasePlayer player, string command, string[] args) {
            bool canspawn = permission.UserHasPermission(player.UserIDString, SedanWhere);
            if (canspawn == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            if (storedData.playersedanID.ContainsKey(player.userID) == true) {
                FrankenVehicle findme;
                storedData.playersedanID.TryGetValue(player.userID, out findme);
                var foundit = BaseNetworkable.serverEntities.Find(findme.vehicleId);
                if (foundit != null) {
                    var loc = foundit.transform.position.ToString();
                    PrintMsgL(player, "FoundSedanMsg", loc);
                }
                return;
            } else {
                PrintMsgL(player, "NoFoundSedanMsg");
                return;
            }
        }

        // Chat despawn Sedan
        [ChatCommand("nosedan")]
        private void KillMySedanChatCommand(BasePlayer player, string command, string[] args) {
            bool isspawner = permission.UserHasPermission(player.UserIDString, SedanSpawn);
            if (isspawner == false) {
                PrintMsgL(player, "NoPermMsg");
                return;
            }
            KillMySedanPlease(player);
        }
        #endregion

        #region consolecommands
        // Console spawn Combat Minicopter
        [ConsoleCommand("spawncombatminicopter")]
        private void SpawnMyCombatMinicopterConsoleCommand(ConsoleSystem.Arg arg) {

            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);


            SpawnMini(player, new FrankenVehicle().EnableTurret());
        }

        // Console spawn CCTV Minicopter
        [ConsoleCommand("spawncctvminicopter")]
        private void SpawnMyCCTVMinicopterConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);


            SpawnMini(player, new FrankenVehicle().EnableCCTV());
        }

        // Console spawn Minicopter1
        [ConsoleCommand("spawnminicopter1")]
        private void SpawnMyMinicopter1ConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);


            SpawnMini(player, new FrankenVehicle().EnableRotorSeats());
        }

        // Console spawn Minicopter2
        [ConsoleCommand("spawnminicopter2")]
        private void SpawnMyMinicopter2ConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);


            SpawnMini(player, new FrankenVehicle().EnableSideSeats());
        }

        // Console spawn Minicopter3
        [ConsoleCommand("spawnminicopter3")]
        private void SpawnMyMinicopter3ConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);


            SpawnMini(player, new FrankenVehicle().EnableAllSeats());
        }

        // Console spawn Minicopter
        [ConsoleCommand("spawnminicopter")]
        private void SpawnMyMinicopterConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }
        }
        void SpawnMini(ulong steamId, FrankenVehicle fv) {
            BasePlayer player = BasePlayer.FindByID(steamId);
            if (player != null) {
                SpawnMyMinicopter(player, fv);
            }
        }

        // Console despawn Minicopter
        [ConsoleCommand("killminicopter")]
        private void KillMyMinicopterConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    KillMyMinicopterPlease(player);
                }
            }
        }

        // Console spawn Piano Scrap Helicopter
        [ConsoleCommand("spawnpianoscraphelicopter")]
        private void SpawnMyPianoScrapTransportHelicopterConsoleCommand(ConsoleSystem.Arg arg) {
            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);
            SpawnMyScrapTransportHelicopter(player, new FrankenVehicle().EnablePiano());
        }

        // Console spawn Scrap Helicopter
        [ConsoleCommand("spawnscraphelicopter")]
        private void SpawnMyScrapTransportHelicopterConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    SpawnMyScrapTransportHelicopter(player, new FrankenVehicle());
                }
            }
        }

        // Console despawn Scrap Helicopter
        [ConsoleCommand("killscraphelicopter")]
        private void KillMyScrapTransportHelicopterConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    KillMyScrapTransportHelicopterPlease(player);
                }
            }
        }

        // Console spawn Row Boat
        [ConsoleCommand("spawnrowboat")]
        private void SpawnMyRowBoatConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    SpawnMyRowBoat(player, new FrankenVehicle());
                }
            }
        }

        // Console despawn Row Boat
        [ConsoleCommand("killrowboat")]
        private void KillMyRowBoatConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    KillMyRowBoatPlease(player);
                }
            }
        }

        // Console spawn Piano RHIB
        [ConsoleCommand("spawnpianorhib")]
        private void SpawnMyPianoRHIBConsoleCommand(ConsoleSystem.Arg arg) {
            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);
            SpawnMyRHIB(player, new FrankenVehicle().EnablePiano());
        }

        // Console spawn RHIB
        [ConsoleCommand("spawnrhib")]
        private void SpawnMyRHIBConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    SpawnMyRHIB(player, new FrankenVehicle());
                }
            }
        }

        // Console despawn RHIB
        [ConsoleCommand("killrhib")]
        private void KillMyRHIBConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    KillMyRHIBPlease(player);
                }
            }
        }

        // Console spawn Hot Air Balloon
        [ConsoleCommand("spawnhotairballoon")]
        private void SpawnMyHotAirBalloonConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    SpawnMyHotAirBalloon(player, new FrankenVehicle());
                }
            }
        }

        // Console despawn Hot Air Balloon
        [ConsoleCommand("killhotairballoon")]
        private void KillMyHotAirBalloonConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    KillMyHotAirBalloonPlease(player);
                }
            }
        }

        //        // Console spawn Ridable Horse1
        //        [ConsoleCommand("spawnridablehorse1")]
        //        private void SpawnMyRidableHorse1ConsoleCommand(ConsoleSystem.Arg arg)
        //        {
        //			createHorseBackSeat = true;
        //			SpawnMyRidableHorseConsoleCommand(arg);
        //		  }

        // Console spawn Ridable Horse
        [ConsoleCommand("spawnridablehorse")]
        private void SpawnMyRidableHorseConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    SpawnMyRidableHorse(player, new FrankenVehicle());
                }
            }
        }

        // Console despawn Ridable Horse
        [ConsoleCommand("killridablehorse")]
        private void KillMyRidableHorseConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    KillMyRidableHorsePlease(player);
                }
            }
        }

        // Console spawn Piano Chinook
        [ConsoleCommand("spawnpianoch47")]
        private void SpawnMyPianoChinookConsoleCommand(ConsoleSystem.Arg arg) {
            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);
            SpawnMyChinook(player, new FrankenVehicle().EnablePiano());
        }

        // Console spawn Chinook
        [ConsoleCommand("spawnch47")]
        private void SpawnMyChinookConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    SpawnMyChinook(player, new FrankenVehicle());
                }
            }
        }

        // Console despawn Chinook
        [ConsoleCommand("killch47")]
        private void KillMyChinookConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    KillMyChinookPlease(player);
                }
            }
        }

        // Console spawn Piano Sedan
        [ConsoleCommand("spawnpianosedan")]
        private void SpawnMyPianoSedanConsoleCommand(ConsoleSystem.Arg arg) {
            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);
            SpawnMySedan(player, new FrankenVehicle().EnablePiano());
        }

        // Console spawn Combat Sedan
        [ConsoleCommand("spawncombatsedan")]
        private void SpawnMyCombatSedanConsoleCommand(ConsoleSystem.Arg arg) {
            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);
            SpawnMySedan(player, new FrankenVehicle().EnableTurret());
        }

        // Console spawn Combat Sedan 2
        [ConsoleCommand("spawncombatsedan2")]
        private void SpawnMyCombatSedan2ConsoleCommand(ConsoleSystem.Arg arg) {
            ulong steamid = Convert.ToUInt64(arg.Args[0]);
            if (steamid == 0) return;
            if (steamid.IsSteamId() == false) return;
            BasePlayer player = BasePlayer.FindByID(steamid);
            SpawnMySedan(player, new FrankenVehicle().EnableTurret().EnableTurret2());
        }

        // Console spawn Sedan
        [ConsoleCommand("spawnsedan")]
        private void SpawnMySedanConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    SpawnMySedan(player, new FrankenVehicle());
                }
            }
        }

        // Console despawn Sedan
        [ConsoleCommand("killsedan")]
        private void KillMySedanConsoleCommand(ConsoleSystem.Arg arg) {
            if (arg.IsRcon) {
                if (arg.Args == null) {
                    Puts("You need to supply a valid SteamID.");
                    return;
                }
            } else if (!HasPermission(arg, MyVehiclesAdmin)) {
                SendReply(arg, _("NoPermMsg", arg.Connection.player as BasePlayer));
                return;
            } else if (arg.Args == null) {
                SendReply(arg, _("SpawnUsage", arg.Connection.player as BasePlayer));
                return;
            }

            if (arg.Args.Length == 1) {
                ulong steamid = Convert.ToUInt64(arg.Args[0]);
                if (steamid == 0) return;
                if (steamid.IsSteamId() == false) return;
                BasePlayer player = BasePlayer.FindByID(steamid);
                if (player != null) {
                    KillMySedanPlease(player);
                }
            }
        }
        #endregion

        #region ourhooks
        // Spawn Minicopter hook
        private void SpawnMyMinicopter(BasePlayer player, FrankenVehicle fc) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Quaternion rotation = player.GetNetworkRotation();
            Vector3 forward = rotation * Vector3.forward;
            // Make straight perpendicular to up axis so we don't spawn into ground or above player's head.
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + straight * 3f;
            position.y = player.transform.position.y + 1f;

            if (position == null) return;
            BaseVehicle vehicleMini = (BaseVehicle)GameManager.server.CreateEntity(prefabmini, position, new Quaternion());
            if (vehicleMini == null) return;
            BaseEntity miniEntity = vehicleMini as BaseEntity;
            miniEntity.OwnerID = player.userID;

            MiniCopter miniCopter = vehicleMini as MiniCopter;
            vehicleMini.Spawn();
            vehicleMini._maxHealth = miniHealth;
            vehicleMini.health = miniHealth;

            if (permission.UserHasPermission(player.UserIDString, MinicopterUnlimited)) {
                // Set fuel requirements to 0
                miniCopter.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited) {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = miniCopter.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            } else {
                miniCopter.fuelPerSec = miniFuelConsumption;
            }

            PrintMsgL(player, "SpawnedMiniMsg");
            fc.vehicleId = vehicleMini.net.ID;
#if DEBUG
            Puts($"SPAWNED MINICOPTER {fc.vehicleId.ToString()} for player {player.displayName} OWNER {miniEntity.OwnerID}");
#endif
            storedData.playerminiID.Remove(player.userID);
            var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
            currentMounts.Remove(myKey);
            storedData.playerminiID.Add(player.userID, fc);
            SaveData();

            miniEntity = null;
            miniCopter = null;
        }

        // Kill Minicopter hook
        private void KillMyMinicopterPlease(BasePlayer player) {
            bool foundmini = false;
            if (maxdistancemini == 0f) {
                foundmini = true;
            } else {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancemini, vehiclelist);

                foreach (BaseEntity p in vehiclelist) {
                    var foundent = p.GetComponentInParent<MiniCopter>() ?? null;
                    if (foundent != null) {
                        foundmini = true;
                    }
                }
            }

            if (storedData.playerminiID.ContainsKey(player.userID) == true && foundmini) {
                FrankenVehicle deluint;
                storedData.playerminiID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill != null) {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerminiID.Remove(player.userID);
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerminicounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerminicounter.Remove(player.userID);
                }
                SaveData();
            } else if (foundmini == false) {
#if DEBUG
                Puts($"Player too far from Minicopter to destroy.");
#endif
                PrintMsgL(player, "DistanceMiniMsg", maxdistancemini);
            }
        }
        // Spawn Scrap Helicopter hook
        private void SpawnMyScrapTransportHelicopter(BasePlayer player, FrankenVehicle fv) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Quaternion rotation = player.GetNetworkRotation();
            Vector3 forward = rotation * Vector3.forward;
            // Make straight perpendicular to up axis so we don't spawn into ground or above player's head.
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + straight * 5f;
            position.y = player.transform.position.y + 1f;

            if (position == null) return;
            BaseVehicle vehicleScrap = (BaseVehicle)GameManager.server.CreateEntity(prefabscrap, position, new Quaternion());
            if (vehicleScrap == null) return;
            BaseEntity scrapEntity = vehicleScrap as BaseEntity;
            scrapEntity.OwnerID = player.userID;

            ScrapTransportHelicopter scrapCopter = vehicleScrap as ScrapTransportHelicopter;
            vehicleScrap.Spawn();
            vehicleScrap._maxHealth = scrapHealth;
            vehicleScrap.health = scrapHealth;

            if (permission.UserHasPermission(player.UserIDString, ScrapUnlimited)) {
                // Set fuel requirements to 0
                scrapCopter.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited) {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = scrapCopter.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            } else {
                scrapCopter.fuelPerSec = scrapFuelConsumption;
            }

            PrintMsgL(player, "SpawnedScrapMsg");
            fv.vehicleId = vehicleScrap.net.ID;
#if DEBUG
            Puts($"SPAWNED SCRAP HELICOPTER {fv.vehicleId.ToString()} for player {player.displayName} OWNER {scrapEntity.OwnerID}");
#endif
            storedData.playerscrapID.Remove(player.userID);
            var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
            currentMounts.Remove(myKey);
            storedData.playerscrapID.Add(player.userID, fv);
            SaveData();

            scrapEntity = null;
            scrapCopter = null;
        }

        // Kill Scrap Helicopter hook
        private void KillMyScrapTransportHelicopterPlease(BasePlayer player) {
            bool foundscrap = false;
            if (maxdistancescrap == 0f) {
                foundscrap = true;
            } else {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancescrap, vehiclelist);

                foreach (BaseEntity p in vehiclelist) {
                    var foundent = p.GetComponentInParent<ScrapTransportHelicopter>() ?? null;
                    if (foundent != null) {
                        foundscrap = true;
                    }
                }
            }

            if (storedData.playerscrapID.ContainsKey(player.userID) == true && foundscrap) {
                FrankenVehicle deluint;
                storedData.playerscrapID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill != null) {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerscrapID.Remove(player.userID);
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerscrapcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerscrapcounter.Remove(player.userID);
                }
                SaveData();
            } else if (foundscrap == false) {
#if DEBUG
                Puts($"Player too far from Scrap Helicopter to destroy.");
#endif
                PrintMsgL(player, "DistanceScrapMsg", maxdistancescrap);
            }
        }

        // Spawn Row Boat hook
        private void SpawnMyRowBoat(BasePlayer player, FrankenVehicle fv) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Quaternion rotation = player.GetNetworkRotation();
            Vector3 forward = rotation * Vector3.forward;
            // Make straight perpendicular to up axis so we don't spawn into ground or above player's head.
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + straight * 3f;
            position.y = player.transform.position.y;

            if (position == null) return;
            BaseVehicle vehicleBoat = (BaseVehicle)GameManager.server.CreateEntity(prefabboat, position, new Quaternion());
            if (vehicleBoat == null) return;
            BaseEntity boatEntity = vehicleBoat as BaseEntity;
            boatEntity.OwnerID = player.userID;

            MotorRowboat rowBoat = vehicleBoat as MotorRowboat;
            vehicleBoat.Spawn();
            vehicleBoat._maxHealth = boatHealth;
            vehicleBoat.health = boatHealth;

            if (permission.UserHasPermission(player.UserIDString, BoatUnlimited)) {
                // Set fuel requirements to 0
                rowBoat.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited) {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = rowBoat.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            } else {
                rowBoat.fuelPerSec = boatFuelConsumption;
            }

            PrintMsgL(player, "SpawnedBoatMsg");
            fv.vehicleId = vehicleBoat.net.ID;
#if DEBUG
            Puts($"SPAWNED ROW BOAT {fv.vehicleId.ToString()} for player {player.displayName} OWNER {boatEntity.OwnerID}");
#endif
            storedData.playerboatID.Remove(player.userID);
            var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
            currentMounts.Remove(myKey);
            storedData.playerboatID.Add(player.userID, fv);
            SaveData();

            boatEntity = null;
            rowBoat = null;
        }

        // Kill Row Boat hook
        private void KillMyRowBoatPlease(BasePlayer player) {
            bool foundboat = false;
            if (maxdistanceboat == 0f) {
                foundboat = true;
            } else {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistanceboat, vehiclelist);

                foreach (BaseEntity p in vehiclelist) {
                    var foundent = p.GetComponentInParent<MotorRowboat>() ?? null;
                    if (foundent != null) {
                        foundboat = true;
                    }
                }
            }

            if (storedData.playerboatID.ContainsKey(player.userID) == true && foundboat) {
                FrankenVehicle deluint;
                storedData.playerboatID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill != null) {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerboatID.Remove(player.userID);
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerboatcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerboatcounter.Remove(player.userID);
                }
                SaveData();
            } else if (foundboat == false) {
#if DEBUG
                Puts($"Player too far from Row Boat to destroy.");
#endif
                PrintMsgL(player, "DistanceBoatMsg", maxdistanceboat);
            }
        }

        // Spawn RHIB hook
        private void SpawnMyRHIB(BasePlayer player, FrankenVehicle fv) {
            if (storedData.playerrhibcounter.ContainsKey(player.userID)) {
                storedData.playerrhibcounter.Remove(player.userID);
                SaveData();
            }

            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Quaternion rotation = player.GetNetworkRotation();
            Vector3 forward = rotation * Vector3.forward;
            // Make straight perpendicular to up axis so we don't spawn into ground or above player's head.
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + straight * 5f;
            position.y = player.transform.position.y;

            if (position == null) return;
            RHIB rhib = (RHIB)GameManager.server.CreateEntity(prefabrhib, position, new Quaternion());
            if (rhib == null) return;
            rhib.OwnerID = player.userID;
            rhib.Spawn();
            rhib._maxHealth = rhibHealth;
            rhib.health = rhibHealth;

            if (permission.UserHasPermission(player.UserIDString, RHIBUnlimited)) {
                // Set fuel requirements to 0
                rhib.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited) {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = rhib.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            } else {
                rhib.fuelPerSec = rhibFuelConsumption;
            }

            PrintMsgL(player, "SpawnedRHIBMsg");
            fv.vehicleId = rhib.net.ID;
            storedData.playerrhibID.Remove(player.userID);
            var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
            currentMounts.Remove(myKey);
            storedData.playerrhibID.Add(player.userID, fv);
            SaveData();

            rhib = null;
        }

        // Kill RHIB hook
        private void KillMyRHIBPlease(BasePlayer player) {
            bool foundrhib = false;
            if (maxdistancerhib == 0f) {
                foundrhib = true;
            } else {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancerhib, vehiclelist);

                foreach (BaseEntity p in vehiclelist) {
                    var foundent = p.GetComponentInParent<RHIB>() ?? null;
                    if (foundent != null) {
                        foundrhib = true;
                    }
                }
            }

            if (storedData.playerrhibID.ContainsKey(player.userID) == true && foundrhib) {
                FrankenVehicle deluint;
                storedData.playerrhibID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill != null) {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerrhibID.Remove(player.userID);
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerrhibcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerrhibcounter.Remove(player.userID);
                }
                SaveData();
            } else if (foundrhib == false) {
#if DEBUG
                Puts($"Player too far from RHIB to destroy.");
#endif
                PrintMsgL(player, "DistanceRHIBMsg", maxdistancerhib);
            }
        }

        // Spawn Hot Air Balloon hook
        private void SpawnMyHotAirBalloon(BasePlayer player, FrankenVehicle fv) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Quaternion rotation = player.GetNetworkRotation();
            Vector3 forward = rotation * Vector3.forward;
            // Make straight perpendicular to up axis so we don't spawn into ground or above player's head.
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + straight * 3.1f;
            position.y = player.transform.position.y + 0.5f;

            if (position == null) return;
            HotAirBalloon hab = (HotAirBalloon)GameManager.server.CreateEntity(prefabhab, position, new Quaternion());
            if (hab == null) return;
            hab.OwnerID = player.userID;
            hab.Spawn();
            hab._maxHealth = habHealth;
            hab.health = habHealth;

            if (permission.UserHasPermission(player.UserIDString, HABUnlimited)) {
                // Set fuel requirements to 0
                hab.fuelPerSec = 0f;
                if (!allowFuelIfUnlimited) {
                    // If the player is not allowed to use the fuel container, add 1 fuel so the vehicle will start.
                    // Also lock fuel container since there is no point in adding/removing fuel
                    StorageContainer fuelCan = hab.fuelStorageInstance.Get(true).GetComponent<StorageContainer>();
                    ItemManager.CreateByItemID(-946369541, 1)?.MoveToContainer(fuelCan.inventory);
                    fuelCan.SetFlag(BaseEntity.Flags.Locked, true);
                }
            } else {
                hab.fuelPerSec = habFuelConsumption;
            }

            PrintMsgL(player, "SpawnedHABMsg");
            fv.vehicleId = hab.net.ID;
#if DEBUG
            Puts($"SPAWNED HOT AIR BALLOON {fv.vehicleId.ToString()} for player {player.displayName} OWNER {hab.OwnerID}");
#endif
            storedData.playerhabID.Remove(player.userID);
            storedData.playerhabID.Add(player.userID, fv);
            SaveData();

            hab = null;
        }

        // Kill Hot Air Balloon hook
        private void KillMyHotAirBalloonPlease(BasePlayer player) {
            bool foundhab = false;
            if (maxdistancehab == 0f) {
                foundhab = true;
            } else {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancehab, vehiclelist);

                foreach (BaseEntity p in vehiclelist) {
                    var foundent = p.GetComponentInParent<HotAirBalloon>() ?? null;
                    if (foundent != null) {
                        foundhab = true;
                    }
                }
            }

            if (storedData.playerhabID.ContainsKey(player.userID) == true && foundhab) {
                FrankenVehicle deluint;
                storedData.playerhabID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill != null) {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerhabID.Remove(player.userID);

                if (storedData.playerhabcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerhabcounter.Remove(player.userID);
                }
                SaveData();
            } else if (foundhab == false) {
#if DEBUG
                Puts($"Player too far from Hot Air Balloon to destroy.");
#endif
                PrintMsgL(player, "DistanceHABMsg", maxdistancehab);
            }
        }

        // Spawn Ridable Horse hook
        private void SpawnMyRidableHorse(BasePlayer player, FrankenVehicle fv) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Vector3 position = new Vector3((float)(player.transform.position.x + 1f), player.transform.position.y, (float)(player.transform.position.z + 0f));

            position = GetGroundPosition(position);

            if (position == null) return;
            BaseVehicle vehicleHorse = (BaseVehicle)GameManager.server.CreateEntity(prefabhorse, position, new Quaternion());
            if (vehicleHorse == null) return;
            BaseEntity horseEntity = vehicleHorse as BaseEntity;
            horseEntity.OwnerID = player.userID;

            RidableHorse horse = vehicleHorse as RidableHorse;
            vehicleHorse.Spawn();
            vehicleHorse._maxHealth = horseHealth;
            vehicleHorse.health = horseHealth;

            PrintMsgL(player, "SpawnedHorseMsg");
            fv.vehicleId = vehicleHorse.net.ID;
#if DEBUG
            Puts($"SPAWNED RIDABLE HORSE {fv.vehicleId.ToString()} for player {player.displayName} OWNER {horseEntity.OwnerID}");
#endif
            storedData.playerhorseID.Remove(player.userID);
            var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
            currentMounts.Remove(myKey);
            storedData.playerhorseID.Add(player.userID, fv);
            SaveData();

            horseEntity = null;
            horse = null;
        }

        private readonly static int LAYER_GROUND = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed");
        private Vector3 GetGroundPosition(Vector3 position) {
            position.y += 100f;
            RaycastHit hitInfo;
            if (Physics.Raycast(position, Vector3.down, out hitInfo, 200f, LAYER_GROUND)) position.y = hitInfo.point.y;
            else position.y = TerrainMeta.HeightMap.GetHeight(position);
            return position;
        }

        // Kill Ridable Horse hook
        private void KillMyRidableHorsePlease(BasePlayer player) {
            bool foundhorse = false;
            if (maxdistancehorse == 0f) {
                foundhorse = true;
            } else {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancehorse, vehiclelist);

                foreach (BaseEntity p in vehiclelist) {
                    var foundent = p.GetComponentInParent<RidableHorse>() ?? null;
                    if (foundent != null) {
                        foundhorse = true;
                    }
                }
            }

            if (storedData.playerhorseID.ContainsKey(player.userID) == true && foundhorse) {
                FrankenVehicle deluint;
                storedData.playerhorseID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill != null) {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerhorseID.Remove(player.userID);
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerhorsecounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerhorsecounter.Remove(player.userID);
                }
                SaveData();
            } else if (foundhorse == false) {
#if DEBUG
                Puts($"Player too far from Ridable Horse to destroy.");
#endif
                PrintMsgL(player, "DistanceHorseMsg", maxdistancehorse);
            }
        }

        // Spawn Chinook hook
        private void SpawnMyChinook(BasePlayer player, FrankenVehicle fv) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Quaternion rotation = player.GetNetworkRotation();
            Vector3 forward = rotation * Vector3.forward;
            // Make straight perpendicular to up axis so we don't spawn into ground or above player's head.
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + straight * 8f;
            position.y = player.transform.position.y + 1f;

            if (position == null) return;
            BaseVehicle vehicleChinook = (BaseVehicle)GameManager.server.CreateEntity(prefabchinook, position, new Quaternion());
            if (vehicleChinook == null) return;
            BaseEntity chinookEntity = vehicleChinook as BaseEntity;
            chinookEntity.OwnerID = player.userID;

            CH47Helicopter chinook = vehicleChinook as CH47Helicopter;
            vehicleChinook.Spawn();
            vehicleChinook._maxHealth = chinookHealth;
            vehicleChinook.health = chinookHealth;

            if (!ShowChinookMapMarker && chinookEntity is CH47Helicopter) {
                var mapMarker = chinookEntity as CH47Helicopter;
                mapMarker.mapMarkerInstance?.Kill();
                mapMarker.mapMarkerEntityPrefab.guid = string.Empty;
            }

            PrintMsgL(player, "SpawnedChinookMsg");
            fv.vehicleId = vehicleChinook.net.ID;
#if DEBUG
            Puts($"SPAWNED CHINOOK {fv.vehicleId.ToString()} for player {player.displayName} OWNER {chinookEntity.OwnerID}");
#endif
            storedData.playerchinookID.Remove(player.userID);
            var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
            currentMounts.Remove(myKey);
            storedData.playerchinookID.Add(player.userID, fv);
            SaveData();

            chinookEntity = null;
            chinook = null;
        }

        // Kill Chinook hook
        private void KillMyChinookPlease(BasePlayer player) {
            bool foundchinook = false;
            if (maxdistancechinook == 0f) {
                foundchinook = true;
            } else {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancechinook, vehiclelist);

                foreach (BaseEntity p in vehiclelist) {
                    var foundent = p.GetComponentInParent<CH47Helicopter>() ?? null;
                    if (foundent != null) {
                        foundchinook = true;
                    }
                }
            }

            if (storedData.playerchinookID.ContainsKey(player.userID) == true && foundchinook) {
                FrankenVehicle deluint;
                storedData.playerchinookID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill != null) {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playerchinookID.Remove(player.userID);
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerchinookcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerchinookcounter.Remove(player.userID);
                }
                SaveData();
            } else if (foundchinook == false) {
#if DEBUG
                Puts($"Player too far from Chinook to destroy.");
#endif
                PrintMsgL(player, "DistanceChinookMsg", maxdistancechinook);
            }
        }

        // Spawn Sedan hook
        private void SpawnMySedan(BasePlayer player, FrankenVehicle fv) {
            if (player.IsBuildingBlocked() & !allowWhenBlocked) {
                PrintMsgL(player, "BlockedMsg");
                return;
            }

            Quaternion rotation = player.GetNetworkRotation();
            Vector3 forward = rotation * Vector3.forward;
            // Make straight perpendicular to up axis so we don't spawn into ground or above player's head.
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + straight * 4f;
            position.y = player.transform.position.y + 0.5f;

            if (position == null) return;
            BaseVehicle vehicleSedan = (BaseVehicle)GameManager.server.CreateEntity(prefabsedan, position, new Quaternion());
            if (vehicleSedan == null) return;
            BaseEntity sedanEntity = vehicleSedan as BaseEntity;
            sedanEntity.OwnerID = player.userID;

            BaseCar sedan = vehicleSedan as BaseCar;
            vehicleSedan.Spawn();
            vehicleSedan._maxHealth = sedanHealth;
            vehicleSedan.health = sedanHealth;

            PrintMsgL(player, "SpawnedSedanMsg");
            fv.vehicleId = vehicleSedan.net.ID;
#if DEBUG
            Puts($"SPAWNED SEDAN {fv.vehicleId.ToString()} for player {player.displayName} OWNER {sedanEntity.OwnerID}");
#endif
            storedData.playersedanID.Remove(player.userID);
            var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
            currentMounts.Remove(myKey);
            storedData.playersedanID.Add(player.userID, fv);
            SaveData();

            sedanEntity = null;
            sedan = null;
        }

        // Kill Sedan hook
        private void KillMySedanPlease(BasePlayer player) {
            bool foundsedan = false;
            if (maxdistancesedan == 0f) {
                foundsedan = true;
            } else {
                List<BaseEntity> vehiclelist = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, maxdistancesedan, vehiclelist);

                foreach (BaseEntity p in vehiclelist) {
                    var foundent = p.GetComponentInParent<BaseCar>() ?? null;
                    if (foundent != null) {
                        foundsedan = true;
                    }
                }
            }

            if (storedData.playersedanID.ContainsKey(player.userID) == true && foundsedan) {
                FrankenVehicle deluint;
                storedData.playersedanID.TryGetValue(player.userID, out deluint);
                var tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill != null) {
                    tokill.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                storedData.playersedanID.Remove(player.userID);
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playersedancounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playersedancounter.Remove(player.userID);
                }
                SaveData();
            } else if (foundsedan == false) {
#if DEBUG
                Puts($"Player too far from Sedan to destroy.");
#endif
                PrintMsgL(player, "DistanceSedanMsg", maxdistancesedan);
            }
        }
        #endregion

        #region hooks
        //        object CanMountEntity(BasePlayer player, BaseMountable mountable)
        //        {
        //            if(player == null) return null;
        //            var mini = mountable.GetComponentInParent<MiniCopter>() ?? null;
        //            if (mini != null)
        //            {
        //#if DEBUG
        //                Puts($"Player {player.userID.ToString()} wants to mount seat id {mountable.net.ID.ToString()}");
        //#endif
        //                var id = mountable.net.ID - 2;
        //                for (int i = 0; i < 3; i++)
        //                {
        //                    // Find copter and seats in storedData
        //#if DEBUG
        //                    Puts($"  Is this our copter with ID {id.ToString()}?");
        //#endif
        //                    if (storedData.playerminiID.ContainsValue(id))
        //                    {
        //#if DEBUG
        //                        Puts("    yes, it is...");
        //#endif
        //                        if (currentMounts.ContainsValue(player.userID))
        //                        {
        //                            if (!player.GetMounted())
        //                            {
        //                                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
        //                                currentMounts.Remove(myKey);
        //                            }
        //                            return false;
        //                        }
        //                    }
        //                    id++;
        //                }
        //            }
        //            return null;
        //        }
        //
        //        void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        //        {
        //            var mini = mountable.GetComponentInParent<MiniCopter>() ?? null;
        //            if (mini != null)
        //            {
        //#if DEBUG
        //                Puts($"Player {player.userID.ToString()} mounted seat id {mountable.net.ID.ToString()}");
        //#endif
        //                // Check this seat's ID to see if the copter is one of ours
        //                uint id = mountable.net.ID - 2; // max seat == copter.net.ID + 2, e.g. passenger seat id - 2 == copter id
        //                for (int i = 0; i < 3; i++)
        //                {
        //                    // Find copter in storedData
        //#if DEBUG
        //                    Puts($"Is this our copter with ID {id.ToString()}?");
        //#endif
        //                    if (storedData.playerminiID.ContainsValue(id))
        //                    {
        //#if DEBUG
        //                        Puts($"Removing {player.displayName}'s ID {player.userID} from currentMounts for seat {mountable.net.ID.ToString()} on {id}");
        //#endif
        //                        currentMounts.Remove(mountable.net.ID);
        //#if DEBUG
        //                        Puts($"Adding {player.displayName}'s ID {player.userID} to currentMounts for seat {mountable.net.ID.ToString()} on {id}");
        //#endif
        //                        currentMounts.Add(mountable.net.ID, player.userID);
        //                        break;
        //                    }
        //                    id++;
        //                }
        //            }
        //        }
        //
        //        object CanDismountEntity(BasePlayer player, BaseMountable mountable)
        //        {
        //            if (player == null) return null;
        //            var mini = mountable.GetComponentInParent<MiniCopter>() ?? null;
        //            if (mini != null)
        //            {
        //                if (!Physics.Raycast(new Ray(mountable.transform.position, Vector3.down), maxDismountHeight, layerMask))
        //                {
        //                    // Is this one of ours?
        //                    if (storedData.playerminiID.ContainsValue(mountable.net.ID - 1))
        //                    {
        //                        if (!allowDriverDismountWhileFlying)
        //                        {
        //#if DEBUG
        //                            Puts("DENY PILOT DISMOUNT");
        //#endif
        //                            return false;
        //                        }
        //                        var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
        //                        currentMounts.Remove(myKey);
        //                    }
        //                    else if (storedData.playerminiID.ContainsValue(mountable.net.ID - 2))
        //                    {
        //                        if (!allowPassengerDismountWhileFlying)
        //                        {
        //#if DEBUG
        //                            Puts("DENY PASSENGER DISMOUNT");
        //#endif
        //                            return false;
        //                        }
        //                        var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
        //                        currentMounts.Remove(myKey);
        //                    }
        //                }
        //                return null;
        //            }
        //            return null;
        //        }
        //
        //        void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        //        {
        //            var mini = mountable.GetComponentInParent<MiniCopter>() ?? null;
        //            if (mini != null)
        //            {
        //#if DEBUG
        //                Puts($"Player {player.userID.ToString()} dismounted seat id {mountable.net.ID.ToString()}");
        //#endif
        //                var id = mountable.net.ID - 2;
        //                for (int i = 0; i < 3; i++)
        //                {
        //                    // Find copter and seats in storedData
        //#if DEBUG
        //                    Puts($"Is this our copter with ID {id.ToString()}?");
        //#endif
        //                    if (storedData.playerminiID.ContainsValue(id))
        //                    {
        //#if DEBUG
        //                        Puts($"Removing {player.displayName}'s ID {player.userID} from currentMounts for seat {mountable.net.ID.ToString()} on {id}");
        //#endif
        //                        currentMounts.Remove(mountable.net.ID);
        //                        break;
        //                    }
        //                    id++;
        //                }
        //            }
        //            var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
        //            currentMounts.Remove(myKey);
        //        }

        // On kill - tell owner
        void OnEntityKill(BaseNetworkable entity) {
            if (entity == null) return;
            if (entity.net.ID == 0) return;
            if (storedData.playerminiID == null) return;
            if (storedData.playerscrapID == null) return;
            if (storedData.playerboatID == null) return;
            if (storedData.playerrhibID == null) return;
            if (storedData.playerhabID == null) return;
            if (storedData.playerhorseID == null) return;
            if (storedData.playerminiID.Any(x => x.Value.vehicleId == entity.net.ID) == false)
                if (storedData.playerscrapID.Any(x => x.Value.vehicleId == entity.net.ID) == false)
                    if (storedData.playerboatID.Any(x => x.Value.vehicleId == entity.net.ID) == false)
                        if (storedData.playerrhibID.Any(x => x.Value.vehicleId == entity.net.ID) == false)
                            if (storedData.playerhabID.Any(x => x.Value.vehicleId == entity.net.ID) == false)
                                if (storedData.playerhorseID.Any(x => x.Value.vehicleId == entity.net.ID) == false)
                                    if (storedData.playerchinookID.Any(x => x.Value.vehicleId == entity.net.ID) == false)
                                        if (storedData.playersedanID.Any(x => x.Value.vehicleId == entity.net.ID) == false) {
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
            foreach (var item in storedData.playerminiID) {
                if (item.Value.vehicleId == entity.net.ID) {
                    ChatPlayerOnline(item.Key, "killedmini");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletemini = item.Key;
                }
            }
            foreach (var item in storedData.playerscrapID) {
                if (item.Value.vehicleId == entity.net.ID) {
                    ChatPlayerOnline(item.Key, "killedscrap");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletescrap = item.Key;
                }
            }
            foreach (var item in storedData.playerboatID) {
                if (item.Value.vehicleId == entity.net.ID) {
                    ChatPlayerOnline(item.Key, "killedboat");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeleteboat = item.Key;
                }
            }
            foreach (var item in storedData.playerrhibID) {
                if (item.Value.vehicleId == entity.net.ID) {
                    ChatPlayerOnline(item.Key, "killedrhib");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeleterhib = item.Key;
                }
            }
            foreach (var item in storedData.playerhabID) {
                if (item.Value.vehicleId == entity.net.ID) {
                    ChatPlayerOnline(item.Key, "killedhab");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletehab = item.Key;
                }
            }
            foreach (var item in storedData.playerhorseID) {
                if (item.Value.vehicleId == entity.net.ID) {
                    ChatPlayerOnline(item.Key, "killedhorse");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletehorse = item.Key;
                }
            }
            foreach (var item in storedData.playerchinookID) {
                if (item.Value.vehicleId == entity.net.ID) {
                    ChatPlayerOnline(item.Key, "killedchinook");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletechinook = item.Key;
                }
            }
            foreach (var item in storedData.playersedanID) {
                if (item.Value.vehicleId == entity.net.ID) {
                    ChatPlayerOnline(item.Key, "killedsedan");
                    BasePlayer player = BasePlayer.FindByID(item.Key);
                    todeletesedan = item.Key;
                }
            }
            if (todeletemini != 0) {
                storedData.playerminiID.Remove(todeletemini);
                currentMounts.Remove(entity.net.ID);
                currentMounts.Remove(entity.net.ID + 1);
                currentMounts.Remove(entity.net.ID + 2);
                SaveData();
            }
            if (todeletescrap != 0) {
                storedData.playerscrapID.Remove(todeletescrap);
                currentMounts.Remove(entity.net.ID);
                currentMounts.Remove(entity.net.ID + 1);
                currentMounts.Remove(entity.net.ID + 2);
                SaveData();
            }
            if (todeleteboat != 0) {
                storedData.playerboatID.Remove(todeleteboat);
                currentMounts.Remove(entity.net.ID);
                currentMounts.Remove(entity.net.ID + 1);
                currentMounts.Remove(entity.net.ID + 2);
                SaveData();
            }
            if (todeleterhib != 0) {
                storedData.playerrhibID.Remove(todeleterhib);
                currentMounts.Remove(entity.net.ID);
                currentMounts.Remove(entity.net.ID + 1);
                currentMounts.Remove(entity.net.ID + 2);
                SaveData();
            }
            if (todeletehab != 0) {
                storedData.playerhabID.Remove(todeletehab);
                SaveData();
            }
            if (todeletehorse != 0) {
                storedData.playerhorseID.Remove(todeletehorse);
                currentMounts.Remove(entity.net.ID);
                currentMounts.Remove(entity.net.ID + 1);
                currentMounts.Remove(entity.net.ID + 2);
                SaveData();
            }
            if (todeletechinook != 0) {
                storedData.playerchinookID.Remove(todeletechinook);
                currentMounts.Remove(entity.net.ID);
                currentMounts.Remove(entity.net.ID + 1);
                currentMounts.Remove(entity.net.ID + 2);
                SaveData();
            }
            if (todeletesedan != 0) {
                storedData.playersedanID.Remove(todeletesedan);
                currentMounts.Remove(entity.net.ID);
                currentMounts.Remove(entity.net.ID + 1);
                currentMounts.Remove(entity.net.ID + 2);
                SaveData();
            }
        }

        // Disable decay for our vehicles if so configured
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo) {
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

            if (storedData.playerminiID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                if (vehicleDecay) {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Minicopter {entity.net.ID.ToString()}.");
#endif
                } else {
#if DEBUG
                    Puts($"Disabling decay for spawned Minicopter {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }

            if (storedData.playerscrapID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                if (vehicleDecay) {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Scrap Helicopter {entity.net.ID.ToString()}.");
#endif
                } else {
#if DEBUG
                    Puts($"Disabling decay for spawned Scrap Helicopter {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerboatID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                if (vehicleDecay) {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Row Boat {entity.net.ID.ToString()}.");
#endif
                } else {
#if DEBUG
                    Puts($"Disabling decay for spawned Row Boat {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerrhibID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                if (vehicleDecay) {
#if DEBUG
                    Puts($"Enabling standard decay for spawned RHIB {entity.net.ID.ToString()}.");
#endif
                } else {
#if DEBUG
                    Puts($"Disabling decay for spawned RHIB {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerhabID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                if (vehicleDecay) {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Hot Air Balloon {entity.net.ID.ToString()}.");
#endif
                } else {
#if DEBUG
                    Puts($"Disabling decay for spawned Hot Air Balloon {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerhorseID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                if (vehicleDecay) {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Ridable Horse {entity.net.ID.ToString()}.");
#endif
                } else {
#if DEBUG
                    Puts($"Disabling decay for spawned Ridable Horse {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playerchinookID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                if (vehicleDecay) {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Chinook {entity.net.ID.ToString()}.");
#endif
                } else {
#if DEBUG
                    Puts($"Disabling decay for spawned Chinook {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            if (storedData.playersedanID.Any(x => x.Value.vehicleId == entity.net.ID)) {
                if (vehicleDecay) {
#if DEBUG
                    Puts($"Enabling standard decay for spawned Sedan {entity.net.ID.ToString()}.");
#endif
                } else {
#if DEBUG
                    Puts($"Disabling decay for spawned Sedan {entity.net.ID.ToString()}.");
#endif
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                }
                return;
            }
            return;
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason) {
            if (!killOnSleep) return;
            if (player == null) return;

            if (storedData.playerminiID.ContainsKey(player.userID) == true) {
                FrankenVehicle deluint;
                storedData.playerminiID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill == null) return; // Didn't find it

                // Check for mounted players
                BaseVehicle vehicle = tokill as BaseVehicle;
                BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++) {
                    BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null) {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted) {
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
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerminicounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerminicounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerscrapID.ContainsKey(player.userID) == true) {
                FrankenVehicle deluint;
                storedData.playerscrapID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill == null) return; // Didn't find it

                // Check for mounted players
                BaseVehicle vehicle = tokill as BaseVehicle;
                BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++) {
                    BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null) {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted) {
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
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerscrapcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerscrapcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerboatID.ContainsKey(player.userID) == true) {
                FrankenVehicle deluint;
                storedData.playerboatID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill == null) return; // Didn't find it

                // Check for mounted players
                BaseVehicle vehicle = tokill as BaseVehicle;
                BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++) {
                    BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null) {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted) {
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
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerboatcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerboatcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerrhibID.ContainsKey(player.userID) == true) {
                FrankenVehicle deluint;
                storedData.playerrhibID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill == null) return; // Didn't find it

                // Check for mounted players
                BaseVehicle vehicle = tokill as BaseVehicle;
                BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++) {
                    BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null) {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted) {
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
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerrhibcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerrhibcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerhabID.ContainsKey(player.userID) == true) {
                FrankenVehicle deluint;
                storedData.playerhabID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill == null) return; // Didn't find it
#if DEBUG
                Puts("Hot Air Balloon owner sleeping - destroying vehicle");
#endif
                tokill.Kill();
                storedData.playerhabID.Remove(player.userID);

                if (storedData.playerhabcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerhabcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerhorseID.ContainsKey(player.userID) == true) {
                FrankenVehicle deluint;
                storedData.playerhorseID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill == null) return; // Didn't find it

                // Check for mounted players
                BaseVehicle vehicle = tokill as BaseVehicle;
                BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++) {
                    BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null) {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted) {
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
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerhorsecounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerhorsecounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playerchinookID.ContainsKey(player.userID) == true) {
                FrankenVehicle deluint;
                storedData.playerchinookID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill == null) return; // Didn't find it

                // Check for mounted players
                BaseVehicle vehicle = tokill as BaseVehicle;
                BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++) {
                    BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null) {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted) {
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
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playerchinookcounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playerchinookcounter.Remove(player.userID);
                }
                SaveData();
            }
            if (storedData.playersedanID.ContainsKey(player.userID) == true) {
                FrankenVehicle deluint;
                storedData.playersedanID.TryGetValue(player.userID, out deluint);
                BaseNetworkable tokill = BaseNetworkable.serverEntities.Find(deluint.vehicleId);
                if (tokill == null) return; // Didn't find it

                // Check for mounted players
                BaseVehicle vehicle = tokill as BaseVehicle;
                BaseVehicle.MountPointInfo[] mountpoints = vehicle.mountPoints;
                for (int i = 0; i < (int)mountpoints.Length; i++) {
                    BaseVehicle.MountPointInfo mountPointInfo = mountpoints[i];
                    if (mountPointInfo.mountable != null) {
                        BasePlayer mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted) {
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
                var myKey = currentMounts.FirstOrDefault(x => x.Value == player.userID).Key;
                currentMounts.Remove(myKey);

                if (storedData.playersedancounter.ContainsKey(player.userID) & !useCooldown) {
                    storedData.playersedancounter.Remove(player.userID);
                }
                SaveData();
            }
        }
        #endregion
    }
}
