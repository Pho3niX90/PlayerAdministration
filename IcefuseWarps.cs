/*
Warp System

Copyright (c) 2015-2016 <talk@juliocesar.me>, <http://steamcommunity.com/profiles/76561197983103320/>
Copyright (c) 2016-2018 <pain45dev@gmail.com>, <http://steamcommunity.com/id/pain45/>
Copyright (c) 2020 <shaun@jvn.sx>, <https://steamcommunity.com/id/pho3nix90/>

-------------------------------------------------------------------------------------------------------------------
This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/4.0/deed.en_US
-----------------------------------------------------------------------------------------------------------------

$Id$
Version 1.9.13 by PaiN 2017-10-09 21:36 (UTC +03:00)

Version 2.0.0
- Added ground position check to not warp under map
- Fixed max uses issues.
- Added naked check option.
*/

using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("IFN Warp", "Pho3niX90", "2.0.0")]
    [Description("Original Developer: Dathus BR || Create warp points for players.")]
    class IcefuseWarps : RustPlugin {
        [PluginReference]
        Plugin Jail;

        const string PERMISSION_NAME = "icefusewarps.admin";
        const float aboveGoundPosition = 1.5f;

        private bool Changed;
        private int cooldown;
        private int warpbacktimer;
        private bool enablecooldown;
        private int backcmdauthlevel;
        private bool WarpIfRunning;
        private bool WarpIfWounded;
        private bool WarpIfSwimming;
        private bool WarpIfBuildingBlocked;
        private bool WarpIfDucking;
        private string backtolastloc;
        private string warplist;
        private string therealreadyis;
        private string warpadded;
        private string youhavetowait;
        private string cantwarpwhilerunning;
        private string cantwarpwhilewounded;
        private string cantwarpwhilebuildingblocked;
        private string cantwarpwhileducking;
        private string cantwarpwhileswimming;
        private string youhaveteleportedto;
        private string teleportingto;
        private string youhaveremoved;

        object GetConfig(string menu, string datavalue, object defaultValue) {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null) {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value)) {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        double GetTimeStamp() {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        void LoadVariables() {
            warpbacktimer = Convert.ToInt32(GetConfig("Settings", "WarpBackTimer", 5));
            cooldown = Convert.ToInt32(GetConfig("Settings", "Cooldown", 120));
            enablecooldown = Convert.ToBoolean(GetConfig("Settings", "EnableCooldown", true));
            backcmdauthlevel = Convert.ToInt32(GetConfig("Settings", "Warp_Back_Atleast_Required_Authlevel", 1));
            WarpIfWounded = Convert.ToBoolean(GetConfig("Settings", "WarpIfWounded", true));
            WarpIfSwimming = Convert.ToBoolean(GetConfig("Settings", "WarpIfSwimming", true));
            WarpIfRunning = Convert.ToBoolean(GetConfig("Settings", "WarpIfRunning", true));
            WarpIfBuildingBlocked = Convert.ToBoolean(GetConfig("Settings", "WarpIfBuildingBlocked", true));
            WarpIfDucking = Convert.ToBoolean(GetConfig("Settings", "WarpIfDucking", true));
            backtolastloc = Convert.ToString(GetConfig("Messages", "TELEPORTED_TO_LAST_LOCATION", "You have teleported back to your last location!"));
            warplist = Convert.ToString(GetConfig("Messages", "WARP_LIST", "Warp ID: <color=#91FFB5>{2}</color>\nWarp Name: <color=cyan>{0}</color> \nPermission:<color=orange> {1} </color> \nMaxUses Remaining: <color=lime>{3}</color>"));
            therealreadyis = Convert.ToString(GetConfig("Messages", "WARP_EXISTS", "This warp already exists!"));
            warpadded = Convert.ToString(GetConfig("Messages", "WARP_ADDED", "Warp added with Warp Name: <color=#91FFB5>{0}</color>"));
            youhavetowait = Convert.ToString(GetConfig("Messages", "COOLDOWN_MESSAGE", "You have to wait <color=#91FFB5>{0}</color> second(s) before you can teleport again."));
            cantwarpwhilerunning = Convert.ToString(GetConfig("Messages", "CANT_WARP_WHILE_RUNNING", "You can not warp while running!"));
            cantwarpwhilewounded = Convert.ToString(GetConfig("Messages", "CANT_WARP_WHILE_WOUNDED", "You can not warp while you are wounded!"));
            cantwarpwhilebuildingblocked = Convert.ToString(GetConfig("Messages", "CANT_WARP_WHILE_BUILDING_BLOCKED", "You can not warp while you are in a building blocked area!"));
            cantwarpwhileducking = Convert.ToString(GetConfig("Messages", "CANT_WARP_WHILE_DUCKING", "You can not warp while you are ducking!"));
            cantwarpwhileswimming = Convert.ToString(GetConfig("Messages", "CANT_WARP_WHILE_SWIMMING", "You can not warp while you are swimming!"));
            youhaveteleportedto = Convert.ToString(GetConfig("Messages", "TELEPORTED_TO", "You have teleported to <color=#91FFB5>{0}</color>"));
            teleportingto = Convert.ToString(GetConfig("Messages", "TELEPORTING_IN_TO", "Teleporting in <color=orange>{0}</color> second(s) to <color=#91FFB5>{1}</color>"));
            youhaveremoved = Convert.ToString(GetConfig("Messages", "WARP_REMOVED", "You have removed the warp <color=#91FFB5>{0}</color>"));

            if (Changed) {
                SaveConfig();
                Changed = false;

            }
        }

        protected override void LoadDefaultConfig() {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }

        class StoredData {
            public List<WarpInfo> WarpInfo = new List<WarpInfo> { };
            public Dictionary<ulong, double> cantele = new Dictionary<ulong, double>();
            public Dictionary<ulong, OldPosInfo> lastposition = new Dictionary<ulong, OldPosInfo>();
            public Dictionary<ulong, Dictionary<string, int>> maxuses = new Dictionary<ulong, Dictionary<string, int>>();

        }

        class OldPosInfo {
            public float OldX;
            public float OldY;
            public float OldZ;

            public OldPosInfo(float x, float y, float z) {
                OldX = x;
                OldY = y;
                OldZ = z;
            }

            public OldPosInfo() {
            }
        }
        class WarpInfo {
            public string WarpName;
            public int WarpId;
            public float WarpX;
            public float WarpY;
            public float WarpZ;
            public string WarpPermissionGroup;
            public int WarpTimer;
            public int WarpMaxUses;
            public string WarpCreatorName;
            public int RandomRange;
            public bool WarpNaked;

            public WarpInfo(string name, BasePlayer player, int timerp, string permissionp, int warpnum, int randomr, int maxusess, bool warpNaked) {
                WarpName = name;
                WarpId = warpnum;
                WarpX = player.transform.position.x;
                WarpMaxUses = maxusess;
                WarpY = player.transform.position.y;
                WarpZ = player.transform.position.z;
                WarpCreatorName = player.displayName;
                WarpTimer = timerp;
                WarpPermissionGroup = permissionp;
                RandomRange = randomr;
                WarpNaked = warpNaked;
            }
            /*
            public WarpInfo(string name, BasePlayer player, int timerp, string permissionp, int warpnum, int randomr, int maxusess) {
                WarpName = name;
                WarpId = warpnum;
                WarpX = player.transform.position.x;
                WarpMaxUses = maxusess;
                WarpY = player.transform.position.y;
                WarpZ = player.transform.position.z;
                WarpCreatorName = player.displayName;
                WarpTimer = timerp;
                WarpPermissionGroup = permissionp;
                RandomRange = randomr;
                WarpNaked = false;
            }
            */
            public WarpInfo() {
            }
        }

        StoredData storedData;

        void Loaded() {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("WarpSystem");
            if (!permission.PermissionExists(PERMISSION_NAME, this)) permission.RegisterPermission(PERMISSION_NAME, this);
            LoadVariables();
            foreach (WarpInfo info in storedData.WarpInfo) {
                if (!permission.GroupExists(info.WarpPermissionGroup)) permission.CreateGroup(info.WarpPermissionGroup, "", 0);
                cmd.AddChatCommand(info.WarpId.ToString(), this, "");
                cmd.AddChatCommand(info.WarpName, this, "");
            }
        }
        [ConsoleCommand("warp.wipemaxuses")]
        void cmdWarpMaxUses(ConsoleSystem.Arg arg) {
            if (arg.Connection != null && arg.Connection.authLevel < 1) {
                arg.ReplyWith("You cant use that command!");
                return;
            }
            storedData.maxuses.Clear();
            Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
        }
        [ConsoleCommand("warp.playerto")]
        void cmdWarpPlayerr(ConsoleSystem.Arg arg) {
            BasePlayer target = BasePlayer.Find(arg.Args[0]);
            if ((arg.Args == null) || (arg.Args != null && arg.Args.Length == 0)) {
                arg.ReplyWith("warp.playerto <PlayerName> <WarpName>");
                return;
            }
            if (arg.Connection != null && arg.Connection.authLevel < 1) {
                arg.ReplyWith("You cant use that command!");
                return;
            }
            if (target == null) {
                arg.ReplyWith("Player not found!");
                return;
            }

            ulong steamID = target.userID;
            double nextteletime;


            if (enablecooldown == true) {
                if (storedData.cantele.TryGetValue(steamID, out nextteletime)) {
                    if (GetTimeStamp() >= nextteletime) {
                        storedData.cantele[steamID] = GetTimeStamp() + cooldown;
                        Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                        goto Finish;
                    } else {
                        int nexttele = Convert.ToInt32(nextteletime - GetTimeStamp());
                        SendReplyWithIcon(target, youhavetowait, nexttele.ToString().Replace("-", ""));
                        return;
                    }
                } else {
                    storedData.cantele.Add(steamID, GetTimeStamp() + cooldown);
                    Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                    goto Finish;
                }
            }

            Finish:
            foreach (WarpInfo info in storedData.WarpInfo) {
                if (info.WarpName.ToLower() == arg.Args[1].ToLower() || info.WarpId.ToString().ToLower() == arg.Args[1].ToLower()) {
                    SendReplyWithIcon(target, teleportingto, info.WarpTimer, info.WarpName);
                    arg.ReplyWith($"Teleporting {target.displayName} to {info.WarpName} in {info.WarpTimer}");
                    timer.Once(info.WarpTimer, () => {
                        if (WarpIfRunning == false && target.IsRunning()) {
                            SendReplyWithIcon(target, cantwarpwhilerunning);
                            return;
                        }
                        if (WarpIfWounded == false && target.IsWounded()) {
                            SendReplyWithIcon(target, cantwarpwhilewounded);
                            return;
                        }
                        if (WarpIfSwimming == false && target.IsSwimming()) {
                            SendReplyWithIcon(target, cantwarpwhileswimming);
                            return;
                        }
                        if (WarpIfBuildingBlocked == false & !target.CanBuild()) {
                            SendReplyWithIcon(target, cantwarpwhilebuildingblocked);
                            return;
                        }
                        if (WarpIfDucking == false && target.IsDucked()) {
                            SendReplyWithIcon(target, cantwarpwhileducking);
                            return;
                        }
                        ForcePlayerPos(target, new Vector3(info.WarpX, info.WarpY, info.WarpZ), info.RandomRange > 0);
                        SendReplyWithIcon(target, youhaveteleportedto, info.WarpName);

                    });
                }
            }
        }


        /*[ConsoleCommand("test.it")]
        void cmdTestIT(ConsoleSystem.Arg arg)
        {
            arg.ReplyWith(new Random.Next(1,5).ToString());
        }*/
        int GetNewId() {

            int id = 0;
            foreach (WarpInfo info in storedData.WarpInfo) {
                id = Math.Max(0, info.WarpId);
            }
            return id + 1;
        }
        int GetRandomId(BasePlayer player) {
            int randomid = 0;
            foreach (WarpInfo info in storedData.WarpInfo) {
                if (permission.UserHasGroup(player.userID.ToString(), info.WarpPermissionGroup) || info.WarpPermissionGroup == "all") {
                    randomid = UnityEngine.Random.Range(0, Math.Max(0, info.WarpId));
                }
            }
            return randomid + 1;
        }
        [ChatCommand("warp")]
        void cmdWarp(BasePlayer player, string cmdd, string[] args) {
            if (args.Length == 0) {
                player.SendConsoleCommand("chat.say \"/warp help\" ");
                return;
            }

            bool isprisoner = Convert.ToBoolean(Jail?.Call("IsPrisoner", player));


            ulong steamId = player.userID;
            double nextteletime;
            switch (args[0]) {
                case "limit":
                    SendReplyWithIcon(player, "<color=#91FFB5>Current Warp Limits</color>");

                    if (storedData.cantele.TryGetValue(steamId, out nextteletime)) {
                        int nexttele = Convert.ToInt32(nextteletime - GetTimeStamp());
                        if (nexttele <= 0) {
                            nexttele = 0;
                        }
                        SendReplyWithIcon(player, $"You will be able to warp again in {nexttele} seconds");
                    }
                    SendReplyWithIcon(player, $"Warp Cooldown: <color=orage>{cooldown}</color>");
                    SendReplyWithIcon(player, $"Warp Cooldown Enabled: <color=orage>{enablecooldown}</color>");
                    SendReplyWithIcon(player, "<color=#91FFB5>*************</color>");
                    break;
                case "back":
                    if (isprisoner) {
                        SendReplyWithIcon(player, "You cant teleport out of the jail!");
                        return;
                    }
                    if (player.net.connection.authLevel >= backcmdauthlevel) {
                        SendReplyWithIcon(player, "Teleporting to you last saved locations in {0} seconds.", warpbacktimer.ToString());
                        timer.Once(warpbacktimer, () => {
                            if (WarpIfRunning == false && player.IsRunning()) {
                                SendReplyWithIcon(player, cantwarpwhilerunning);
                                return;
                            }
                            if (WarpIfWounded == false && player.IsWounded()) {
                                SendReplyWithIcon(player, cantwarpwhilewounded);
                                return;
                            }
                            if (WarpIfSwimming == false && player.IsSwimming()) {
                                SendReplyWithIcon(player, cantwarpwhileswimming);
                                return;
                            }
                            if (WarpIfBuildingBlocked == false & !player.CanBuild()) {
                                SendReplyWithIcon(player, cantwarpwhilebuildingblocked);
                                return;
                            }
                            if (WarpIfDucking == false && player.IsDucked()) {
                                SendReplyWithIcon(player, cantwarpwhileducking);
                                return;
                            }
                            ForcePlayerPos(player, new Vector3(storedData.lastposition[steamId].OldX, storedData.lastposition[steamId].OldY, storedData.lastposition[steamId].OldZ), false);
                            SendReplyWithIcon(player, backtolastloc);
                            storedData.lastposition.Remove(steamId);
                            Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                        });
                    }
                    break;

                case "random":
                    if (isprisoner) {
                        SendReplyWithIcon(player, "You cant teleport out of the jail!");
                        return;
                    }
                    player.SendConsoleCommand($"chat.say \"/warp to {GetRandomId(player)}\" ");
                    break;

                case "all":
                    if (!permission.UserHasPermission(player.userID.ToString(), PERMISSION_NAME)) {
                        SendReplyWithIcon(player, "You do not have permission to use this command!");
                        return;
                    }
                    if (args.Length == 2) {
                        foreach (BasePlayer current in BasePlayer.activePlayerList) {
                            foreach (WarpInfo info in storedData.WarpInfo) {
                                if (info.WarpName.ToLower() == args[1].ToLower() || info.WarpId.ToString() == args[1]) {
                                    ForcePlayerPos(current, new Vector3(info.WarpX, info.WarpY, info.WarpZ), info.RandomRange > 0);
                                    SendReplyWithIcon(current, "You got teleported to <color=#91FFB5>" + info.WarpName + "</color> by <color=orange>" + player.displayName + "</color>");

                                }
                            }
                        }
                    } else if (args.Length == 3 && args[1] == "sleepers") {
                        foreach (BasePlayer sleepers in BasePlayer.sleepingPlayerList) {
                            foreach (WarpInfo info in storedData.WarpInfo) {
                                if (info.WarpName.ToLower() == args[2].ToLower() || info.WarpId.ToString() == args[2]) {
                                    ForcePlayerPos(sleepers, new Vector3(info.WarpX, info.WarpY, info.WarpZ), info.RandomRange > 0);
                                    //SendReplyWithIcon(player, "You got teleported to <color=#91FFB5>" + info.WarpName + "</color> by <color=orange>" + player.displayName + "</color>");

                                }
                            }
                        }
                    } else {
                        SendReplyWithIcon(player, "<color=#91FFB5>Teleport all online players</color>: \n /warp all <WarpName>");
                        SendReplyWithIcon(player, "<color=#91FFB5>Teleport all sleepers</color>: \n /warp all sleepers <WarpName>");
                        return;
                    }
                    break;
                case "wipe":
                    if (!permission.UserHasPermission(player.userID.ToString(), PERMISSION_NAME)) {
                        SendReplyWithIcon(player, "You do not have permission to use this command!");
                        return;
                    }
                    storedData.WarpInfo.Clear();
                    storedData.cantele.Clear();
                    Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                    SendReplyWithIcon(player, "You have wiped all the teleports!");
                    break;

                case "list":
                    SendReplyWithIcon(player, "<color=#91FFB5>Current Warps</color>");
                    string maxusesrem;
                    foreach (WarpInfo info in storedData.WarpInfo) {
                        if (permission.UserHasGroup(steamId.ToString(), info.WarpPermissionGroup) || info.WarpPermissionGroup == "all") {

                            if (info.WarpMaxUses == 0) {
                                maxusesrem = "<color=red>UNLIMITED</color>";
                            } else if (!storedData.maxuses.ContainsKey(steamId)) {
                                maxusesrem = info.WarpMaxUses.ToString();
                            } else
                                maxusesrem = storedData.maxuses[steamId][info.WarpName].ToString();

                            SendReplyWithIcon(player, warplist, info.WarpName, info.WarpPermissionGroup, info.WarpId, maxusesrem);
                            SendReplyWithIcon(player, "<color=#91FFB5>*************</color>");
                        }

                    }
                    SendReplyWithIcon(player, "<color=#91FFB5>*************</color>");
                    break;

                case "add":

                    if (!permission.UserHasPermission(player.userID.ToString(), PERMISSION_NAME)) {
                        SendReplyWithIcon(player, "You do not have permission to use this command!");
                        return;
                    }
                    if (args.Length != 7) {
                        SendReplyWithIcon(player, "/warp <add> <WarpName> <WarpTimer> <Naked> <WarpRange> <WarpMaxUses> <WarpPermissionGroup>");
                        return;
                    }

                    /*
                     * Les sort out all args now. Makes maitaining the plugin going forward easier. 
                     */

                    string warpName;
                    int warpTimer;
                    bool warpNaked;
                    int warpRange;
                    int warpMaxUses;
                    string warpPermissionGroup;

                    try {
                        warpName = args[1];
                        warpTimer = Convert.ToInt32(args[2]);
                        warpNaked = Convert.ToBoolean(args[3]);
                        warpRange = Convert.ToInt32(args[4]);
                        warpMaxUses = Convert.ToInt32(args[5]);
                        warpPermissionGroup = args[6];
                    } catch (Exception e) {
                        SendReplyWithIcon(player, "Error occured.");
                        SendReplyWithIcon(player, "/warp <add> <WarpName:string> <WarpTimer:int> <Naked:bool> <WarpRange:int> <WarpMaxUses:int> <WarpPermissionGroup:string>");
                        return;
                    }


                    foreach (WarpInfo info in storedData.WarpInfo) {
                        if (warpName.ToLower() == info.WarpName.ToLower()) {
                            SendReplyWithIcon(player, therealreadyis);
                            return;
                        }
                    }

                    int warpnum = (storedData.WarpInfo == null) ? 1 : GetNewId();

                    var data = new WarpInfo(warpName, player, warpTimer, warpPermissionGroup, warpnum, warpRange, warpMaxUses, warpNaked);
                    storedData.WarpInfo.Add(data);
                    SendReplyWithIcon(player, warpadded, warpName);
                    Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                    if (!permission.GroupExists(warpPermissionGroup)) permission.CreateGroup(warpPermissionGroup, "", 0);
                    cmd.AddChatCommand(warpName, this, "");
                    cmd.AddChatCommand(warpnum.ToString(), this, "");
                    break;

                case "to":
                    if (args.Length != 2) {
                        SendReplyWithIcon(player, "/warp to <WarpName> || /warplist");
                        return;
                    }

                    if (isprisoner) {
                        SendReplyWithIcon(player, "You cant teleport out of the jail!");
                        return;
                    }

                    foreach (WarpInfo info in storedData.WarpInfo) {
                        if (info.WarpName.ToLower() == args[1].ToLower() || info.WarpId.ToString() == args[1]) {
                            if (info.WarpPermissionGroup == "all" || permission.UserHasGroup(steamId.ToString(), info.WarpPermissionGroup)) {
                                if (info.WarpMaxUses > 0) {
                                    if (!storedData.maxuses.ContainsKey(steamId)) {
                                        storedData.maxuses.Add(
                                        steamId,
                                        new Dictionary<string, int>{
                                            {info.WarpName, 1}
                                        }
                                    );
                                    } else if (!storedData.maxuses[steamId].ContainsKey(info.WarpName)) {
                                        storedData.maxuses[steamId].Add(info.WarpName, 1);
                                    }

                                    if (storedData.maxuses[steamId][info.WarpName] >= info.WarpMaxUses) {
                                        SendReplyWithIcon(player, "You have reached the max uses for this Warp!");
                                        return;
                                    }

                                    if (storedData.maxuses.ContainsKey(steamId)) {
                                        storedData.maxuses[steamId][info.WarpName] = storedData.maxuses[steamId][info.WarpName] + 1;
                                    }
                                }

                                if (enablecooldown == true) {
                                    if (storedData.cantele.TryGetValue(steamId, out nextteletime)) {
                                        if (GetTimeStamp() >= nextteletime) {
                                            storedData.cantele[steamId] = GetTimeStamp() + cooldown;
                                            Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                                            goto Finish;
                                        } else {
                                            int nexttele = Convert.ToInt32(GetTimeStamp() - nextteletime);
                                            SendReplyWithIcon(player, youhavetowait, nexttele.ToString().Replace("-", ""));
                                            return;
                                        }
                                    } else {
                                        storedData.cantele.Add(steamId, GetTimeStamp() + cooldown);
                                        Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                                        goto Finish;
                                    }
                                }


                                if (info.WarpNaked && !IsInventoryEmpty(player)) {
                                    SendReplyWithIcon(player, "You must be naked to warp here.");
                                    return;
                                }

                                Finish:
                                if (storedData.lastposition.ContainsKey(steamId) | !storedData.lastposition.ContainsKey(steamId)) {
                                    storedData.lastposition.Remove(steamId);
                                    Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                                    float x = player.transform.position.x;
                                    float y = player.transform.position.y;
                                    float z = player.transform.position.z;
                                    var oldinfo = new OldPosInfo(x, y, z);
                                    storedData.lastposition.Add(steamId, oldinfo);
                                    Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                                }

                                SendReplyWithIcon(player, teleportingto, info.WarpTimer, info.WarpName);
                                timer.Once(info.WarpTimer, () => {
                                    if (info.WarpNaked && !IsInventoryEmpty(player)) {
                                        SendReplyWithIcon(player, "You must be naked to warp here.");
                                        return;
                                    }
                                    if (WarpIfRunning == false && player.IsRunning()) {
                                        SendReplyWithIcon(player, cantwarpwhilerunning);
                                        return;
                                    }
                                    if (WarpIfWounded == false && player.IsWounded()) {
                                        SendReplyWithIcon(player, cantwarpwhilewounded);
                                        return;
                                    }
                                    if (WarpIfSwimming == false && player.IsSwimming()) {
                                        SendReplyWithIcon(player, cantwarpwhileswimming);
                                        return;
                                    }
                                    if (WarpIfBuildingBlocked == false & !player.CanBuild()) {
                                        SendReplyWithIcon(player, cantwarpwhilebuildingblocked);
                                        return;
                                    }
                                    if (WarpIfDucking == false && player.IsDucked()) {
                                        SendReplyWithIcon(player, cantwarpwhileducking);
                                        return;
                                    }
                                    int posx = UnityEngine.Random.Range(Convert.ToInt32(info.WarpX), info.RandomRange);
                                    int posz = UnityEngine.Random.Range(Convert.ToInt32(info.WarpZ), info.RandomRange);
                                    if (info.RandomRange == 0) {
                                        ForcePlayerPos(player, new Vector3(info.WarpX, info.WarpY, info.WarpZ), info.RandomRange > 0);
                                    } else
                                        ForcePlayerPos(player, new Vector3(posx, info.WarpY, posz), info.RandomRange > 0);
                                    SendReplyWithIcon(player, youhaveteleportedto, info.WarpName);
                                });
                            } else {
                                SendReplyWithIcon(player, "You are not allowed to use this warp!");
                                return;
                            }
                        }
                    }
                    break;
                case "help":
                    if (permission.UserHasPermission(player.userID.ToString(), PERMISSION_NAME)) {
                        SendReplyWithIcon(player, "<color=#91FFB5>Available Commands</color>");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp <add> <WarpName> <WarpTimer> <WarpRange> <WarpMaxUses> <WarpPermissionGroup>");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp limit");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp random");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp remove <WarpName>");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp wipe");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp list");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp to <WarpName> || /warp list");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /<WarpName> => A shorter version of /warp to <WarpName> || /warp list");
                        SendReplyWithIcon(player, "<color=#91FFB5>Teleport all online players</color>: \n<color=#91FFB5>-</color> /warp all <WarpName>");
                        SendReplyWithIcon(player, "<color=#91FFB5>Teleport all sleepers</color>: \n<color=#91FFB5>-</color> /warp all sleepers <WarpName>");
                    } else {
                        SendReplyWithIcon(player, "<color=#91FFB5>Available Commands</color>");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp list");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp limit");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp random");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /warp to <WarpName> || /warp list");
                        SendReplyWithIcon(player, "<color=#91FFB5>-</color> /<WarpName> => A shorter version of /warp to <WarpName> || /warp list");
                    }
                    break;
                case "remove":
                    if (!permission.UserHasPermission(player.userID.ToString(), PERMISSION_NAME)) {
                        SendReplyWithIcon(player, "You do not have permission to use this command!");
                        return;
                    }
                    if (args.Length != 2) {
                        SendReplyWithIcon(player, "/warp remove <WarpName>");
                        return;
                    }
                    foreach (WarpInfo info in storedData.WarpInfo) {
                        if (info.WarpName == args[1]) {
                            storedData.WarpInfo.Remove(info);
                            SendReplyWithIcon(player, youhaveremoved, info.WarpName);
                            Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                            break;
                        }
                    }
                    break;

            }
        }

        /*void Unloaded()
        {
            storedData.cantele.Clear();
            Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
        }*/
        void OnServerCommand(ConsoleSystem.Arg arg) {
            if (arg == null) return;
            if (arg.Connection == null) return;
            if (arg.Connection.player == null) return;
            if (arg.cmd == null) return;
            if (arg.cmd.Name == null) return;
            BasePlayer player = (BasePlayer)arg.Connection.player;
            ulong steamId = player.userID;
            double nextteletime;
            string cmd = arg.cmd.FullName;
            string text = arg.GetString(0, "text");


            if (cmd == "chat.say" && text.StartsWith("/")) {
                foreach (WarpInfo info in storedData.WarpInfo) {
                    if (text == "/" + info.WarpName || text == "/" + info.WarpId) {
                        if (info.WarpPermissionGroup == "all" || permission.UserHasGroup(steamId.ToString(), info.WarpPermissionGroup)) {

                            if (enablecooldown == true) {
                                if (storedData.cantele.TryGetValue(steamId, out nextteletime)) {
                                    if (GetTimeStamp() > nextteletime) {

                                        storedData.cantele[steamId] = GetTimeStamp() + cooldown;
                                        Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                                        goto Finish;
                                    } else {
                                        int nexttele = Convert.ToInt32(GetTimeStamp() - nextteletime);
                                        SendReplyWithIcon(player, youhavetowait, nexttele.ToString().Replace("-", ""));
                                        return;
                                    }
                                } else {
                                    storedData.cantele.Add(steamId, GetTimeStamp() + cooldown);
                                    Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                                    goto Finish;
                                }
                            }
                            Finish:
                            if (storedData.lastposition.ContainsKey(steamId) | !storedData.lastposition.ContainsKey(steamId)) {
                                storedData.lastposition.Remove(steamId);
                                Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
                                float x = player.transform.position.x;
                                float y = player.transform.position.y;
                                float z = player.transform.position.z;
                                var oldinfo = new OldPosInfo(x, y, z);
                                storedData.lastposition.Add(steamId, oldinfo);
                                Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);

                            }

                            SendReplyWithIcon(player, teleportingto, info.WarpTimer, info.WarpName);
                            timer.Once(info.WarpTimer, () => {
                                if (WarpIfRunning == false && player.IsRunning()) {
                                    SendReplyWithIcon(player, cantwarpwhilerunning);
                                    return;
                                }
                                if (WarpIfWounded == false && player.IsWounded()) {
                                    SendReplyWithIcon(player, cantwarpwhilewounded);
                                    return;
                                }
                                if (WarpIfSwimming == false && player.IsSwimming()) {
                                    SendReplyWithIcon(player, cantwarpwhileswimming);
                                    return;
                                }
                                if (WarpIfBuildingBlocked == false & !player.CanBuild()) {
                                    SendReplyWithIcon(player, cantwarpwhilebuildingblocked);
                                    return;
                                }
                                if (WarpIfDucking == false && player.IsDucked()) {
                                    SendReplyWithIcon(player, cantwarpwhileducking);
                                    return;
                                }
                                ForcePlayerPos(player, new Vector3(info.WarpX, info.WarpY, info.WarpZ), info.RandomRange > 0);
                                SendReplyWithIcon(player, youhaveteleportedto, info.WarpName);
                            });
                        } else {
                            SendReplyWithIcon(player, "You are not allowed to use this warp!");
                            return;
                        }
                    }
                }
            }

        }

        #region Helpers
        void ForcePlayerPos(BasePlayer player, Vector3 xyz, bool random) {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player)) BasePlayer.sleepingPlayerList.Add(player);

            player.CancelInvoke("InventoryUpdate");
            player.inventory.crafting.CancelAll(true);

            player.MovePosition(random ? GetGroundPosition(new Vector3(xyz.x, xyz.y + 50, xyz.z)) : xyz);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", xyz);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();

            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading");
            player.SendFullSnapshot();
        }

        bool IsInventoryEmpty(BasePlayer player) => player.inventory.AllItems().Length == 0;

        Vector3 GetGroundPosition(Vector3 sourcePos) {

            RaycastHit hitInfo;
            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo))
                sourcePos.y = hitInfo.point.y;

            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos)) + aboveGoundPosition;

            return sourcePos;
        }

        void SendReplyWithIcon(BasePlayer player, string format, params object[] args) {
            int cnt = 0;
            string msg = format;
            foreach (var arg in args) {
                msg = msg.Replace("{" + cnt + "}", arg.ToString());
                cnt++;
            }
            Player.Reply(player, msg, ulong.Parse("76561198795921619"));
        }
        #endregion
    }
}