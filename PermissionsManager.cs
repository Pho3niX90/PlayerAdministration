using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;

//Removed unused vars
//Added static close button
//All "Inherited from" groups can be viewed in a pop up list.

namespace Oxide.Plugins
{
    [Info("PermissionsManager", "Steenamaroo", "2.0.3", ResourceId = 0)]
    class PermissionsManager : RustPlugin
    {
        #region Declarations
        List<string> PlugList = new List<string>();
        Dictionary<int, string> numberedPerms = new Dictionary<int, string>();
        List<ulong> MenuOpen = new List<ulong>();

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);
        const string permAllowed = "permissionsmanager.allowed";

        Dictionary<ulong, Info> ActiveAdmins = new Dictionary<ulong, Info>();

        public class Info
        {
            public string inheritedcheck = "";
            public int noOfPlugs;
            public int previousPage = 1;
            public string subjectGroup;
            public BasePlayer subject;
        }

        string ButtonColour1 = "0.7 0.32 0.17 1";
        string ButtonColour2 = "0.2 0.2 0.2 1";

        #endregion 

        #region Hooks
        void OnPluginLoaded(Plugin plugin)
        {
            if (plugin is PermissionsManager)
                return;
            Wipe();
            OnServerInitialized();
        }
        void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin is PermissionsManager)
                return;
            Wipe();
            OnServerInitialized();
        }

        void Loaded()
        {
            lang.RegisterMessages(messages, this);
            permission.RegisterPermission(permAllowed, this);
        }

        void OnServerInitialized()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList.Where(player => MenuOpen.Contains(player.userID)))
                DestroyMenu(player, true);
            LoadConfigVariables();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadConfigVariables();
            SaveConfig();
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList.Where(player => MenuOpen.Contains(player.userID)))
                DestroyMenu(player, true);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (MenuOpen.Contains(player.userID))
                DestroyMenu(player, true);
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (MenuOpen.Contains(player.userID))
                DestroyMenu(player, true);
        }
        #endregion

        #region Methods
        void DestroyMenu(BasePlayer player, bool all)
        {
            CuiHelper.DestroyUi(player, "MainUI");
            CuiHelper.DestroyUi(player, "PermsUI");
            CuiHelper.DestroyUi(player, "ConfirmUI");
            if (all)
            {
                CuiHelper.DestroyUi(player, "BgUI");
                MenuOpen.Remove(player.userID);
            }
        }

        void Wipe()
        {
            PlugList.Clear();
            numberedPerms.Clear();
        }

        void GetPlugs(BasePlayer player)
        {
            var path = ActiveAdmins[player.userID];
            PlugList.Clear();
            path.noOfPlugs = 0;
            foreach (var entry in plugins.GetAll())
            {
                if (entry.IsCorePlugin)
                    continue;

                var str = entry.ToString();
                var charsToRemove = new string[] { "Oxide.Plugins." };

                foreach (var c in charsToRemove)
                    str = str.Replace(c, string.Empty).ToLower();

                foreach (var perm in permission.GetPermissions().ToList().Where(perm => perm.Contains($"{str}") && !(BlockList.Split(',').ToList().Contains($"{str}"))))
                    if (!(PlugList.Contains(str)))
                        PlugList.Add(str);
            }
            PlugList.Sort();
        }

        bool IsAuth(BasePlayer player) => player?.net?.connection != null && player.net.connection.authLevel == 2;

        void SetButtons(bool on)
        {
            ButtonColour1 = (on) ? OffColour : OnColour;
            ButtonColour2 = (on) ? OnColour : OffColour;
        }

        object[] PermsCheck(BasePlayer player, string group, string info)
        {
            bool has = false;
            List<string> inherited = new List<string>();
            var path = ActiveAdmins[player.userID];
            if (group == "true")
            {
                if (permission.GroupHasPermission(path.subjectGroup, info))
                    has = true;
            }
            else
            {
                UserData userData = permission.GetUserData(path.subject.UserIDString);
                if (userData.Perms.Contains(info))
                    has = true;
                foreach (var group1 in permission.GetUserGroups(path.subject.UserIDString))
                    if (permission.GroupHasPermission(group1, info))
                        inherited.Add(group1);
            }
            return new object[] { has, inherited };
        }
        #endregion

        #region UI
        void BgUI(BasePlayer player)
        {
            MenuOpen.Add(player.userID);
            string guiString = String.Format("0.1 0.1 0.1 {0}", guitransparency);
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = guiString }, RectTransform = { AnchorMin = "0.3 0.1", AnchorMax = "0.7 0.9" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "BgUI");
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0.95", AnchorMax = $"0.998 1" }, Text = { Text = String.Empty } }, mainName);
            elements.Add(new CuiButton { Button = { Color = "0 0 0 1" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.998 0.05" }, Text = { Text = String.Empty } }, mainName);
            elements.Add(new CuiButton { Button = { Command = "ClosePM", Color = ButtonColour }, RectTransform = { AnchorMin = "0.955 0.96", AnchorMax = "0.99 0.995" }, Text = { Text = "X", FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        void MainUI(BasePlayer player, bool group, int page)
        {
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.32 0.1", AnchorMax = "0.68 0.9" }, CursorEnabled = true }, "Overlay", "MainUI");
            elements.Add(new CuiElement { Parent = "MainUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });
            string subject = (group) ? lang.GetMessage("GUIPlayers", this) : lang.GetMessage("GUIGroups", this);
            string current = (!group) ? lang.GetMessage("GUIPlayers", this) : lang.GetMessage("GUIGroups", this);
            elements.Add(new CuiLabel { Text = { Text = "Permissions Manager V2", FontSize = 16, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.95", AnchorMax = "1 1" } }, mainName);

            elements.Add(new CuiButton { Button = { Command = $"PMTogglePlayerGroup {group} 1", Color = ButtonColour }, RectTransform = { AnchorMin = "0.4 0.02", AnchorMax = "0.6 0.04" }, Text = { Text = lang.GetMessage("All", this) + " " + subject, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            int pos1 = 20 - (page * 20), quantity = 0;
            float top = 0.87f;
            float bottom = 0.85f;

            elements.Add(new CuiLabel { Text = { Text = lang.GetMessage("All", this) + " " + current, FontSize = 14, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 0.97" } }, mainName);

            if (group)
                foreach (var grp in permission.GetGroups())
                {
                    pos1++;
                    quantity++;
                    if (pos1 > 0 && pos1 < 21)
                    {
                        elements.Add(new CuiButton { Button = { Command = $"PMSelected group {grp}", Color = ButtonColour }, RectTransform = { AnchorMin = $"0.3 {bottom}", AnchorMax = $"0.7 {top}" }, Text = { Text = $"{grp}", FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                        top = top - 0.025f;
                        bottom = bottom - 0.025f;
                    }
                }
            else
                foreach (var plyr in GetAllPlayers())
                {
                    pos1++;
                    quantity++;
                    if (pos1 > 0 && pos1 < 21)
                    {
                        elements.Add(new CuiButton { Button = { Command = $"PMSelected player {plyr.userID}", Color = ButtonColour }, RectTransform = { AnchorMin = $"0.3 {bottom}", AnchorMax = $"0.7 {top}" }, Text = { Text = $"{plyr.displayName}", FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                        top = top - 0.025f;
                        bottom = bottom - 0.025f;
                    }

                }

            if (quantity > (page * 20))
                elements.Add(new CuiButton { Button = { Command = $"PMTogglePlayerGroup {!group} {page + 1}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.8 0.02", AnchorMax = "0.9 0.04" }, Text = { Text = lang.GetMessage("->", this), FontSize = 11, Align = TextAnchor.MiddleCenter }, }, mainName);

            if (page > 1)
                elements.Add(new CuiButton { Button = { Command = $"PMTogglePlayerGroup {!group} {page - 1}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.1 0.02", AnchorMax = "0.2 0.04" }, Text = { Text = lang.GetMessage("<-", this), FontSize = 11, Align = TextAnchor.MiddleCenter }, }, mainName);

            CuiHelper.AddUi(player, elements);
        }

        void PlugsUI(BasePlayer player, string msg, string isgroup, int page)
        {
            int pageNo = Convert.ToInt32(page);
            string group = isgroup;
            string toggle = (group == "true") ? "false" : "true";
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.32 0.1", AnchorMax = "0.68 0.9" }, CursorEnabled = true }, "Overlay", "PermsUI");
            elements.Add(new CuiElement { Parent = "PermsUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });

            int plugsTotal = 0, pos1 = 60 - (page * 60), next = page + 1, previous = page - 1;

            for (int i = 0; i < PlugList.Count; i++)
            {
                pos1++;
                plugsTotal++;

                if (pos1 > 0 && pos1 < 21)
                    elements.Add(new CuiButton { Button = { Command = $"PermsList {i} null null {group} null 1", Color = ButtonColour }, RectTransform = { AnchorMin = $"0.1 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.3 {0.91 - (pos1 * 3f) / 100f}" }, Text = { Text = $"{PlugList[i]}", FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
                if (pos1 > 20 && pos1 < 41)
                    elements.Add(new CuiButton { Button = { Command = $"PermsList {i} null null {group} null 1", Color = ButtonColour }, RectTransform = { AnchorMin = $"0.4 {(0.89 - ((pos1 - 20) * 3f) / 100f)}", AnchorMax = $"0.6 {0.91 - ((pos1 - 20) * 3f) / 100f}" }, Text = { Text = $"{PlugList[i]}", FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
                if (pos1 > 40 && pos1 < 61)
                    elements.Add(new CuiButton { Button = { Command = $"PermsList {i} null null {group} null 1", Color = ButtonColour }, RectTransform = { AnchorMin = $"0.7 {(0.89 - ((pos1 - 40) * 3f) / 100f)}", AnchorMax = $"0.9 {0.91 - ((pos1 - 40) * 3f) / 100f}" }, Text = { Text = $"{PlugList[i]}", FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
            }
            elements.Add(new CuiButton { Button = { Command = $"PMTogglePlayerGroup {toggle} 1", Color = ButtonColour }, RectTransform = { AnchorMin = "0.55 0.02", AnchorMax = "0.75 0.04" }, Text = { Text = lang.GetMessage("GUIBack", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = msg, FontSize = 16, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.95", AnchorMax = "1 1" } }, mainName);

            if (isgroup == "false")
                elements.Add(new CuiButton { Button = { Command = "Groups 1", Color = ButtonColour }, RectTransform = { AnchorMin = "0.25 0.02", AnchorMax = "0.45 0.04" }, Text = { Text = lang.GetMessage("GUIGroups", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            else
                elements.Add(new CuiButton { Button = { Command = "PlayersIn 1", Color = ButtonColour }, RectTransform = { AnchorMin = "0.25 0.02", AnchorMax = "0.45 0.04" }, Text = { Text = lang.GetMessage("GUIPlayers", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);

            if (plugsTotal > (page * 60))
                elements.Add(new CuiButton { Button = { Command = $"Navigate {group} {next}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.8 0.02", AnchorMax = "0.9 0.04" }, Text = { Text = lang.GetMessage("->", this), FontSize = 11, Align = TextAnchor.MiddleCenter }, }, mainName);

            if (page > 1)
                elements.Add(new CuiButton { Button = { Command = $"Navigate {group} {previous}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.1 0.02", AnchorMax = "0.2 0.04" }, Text = { Text = lang.GetMessage("<-", this), FontSize = 11, Align = TextAnchor.MiddleCenter }, }, mainName);

            CuiHelper.AddUi(player, elements);
        }

        void PermsUI(BasePlayer player, string msg, int PlugNumber, string group, int page)
        {
            var path = ActiveAdmins[player.userID];
            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.32 0.1", AnchorMax = "0.68 0.9" }, CursorEnabled = true }, "Overlay", "PermsUI");
            elements.Add(new CuiElement { Parent = "PermsUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });

            int permsTotal = 0, pos1 = 20 - (page * 20), next = (page + 1), previous = (page - 1);
            elements.Add(new CuiButton { Button = { Command = $"PermsList {PlugNumber} grant null {group} all {page}", Color = ButtonColour }, RectTransform = { AnchorMin = $"0.5 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.6 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = lang.GetMessage("GUIAll", this), FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
            elements.Add(new CuiButton { Button = { Command = $"PermsList {PlugNumber} revoke null {group} all {page}", Color = ButtonColour }, RectTransform = { AnchorMin = $"0.65 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.75 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = lang.GetMessage("GUINone", this), FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);

            var elements1 = new CuiElementContainer();
            bool list = false;
            foreach (var perm in numberedPerms)
            {
                SetButtons(true);
                pos1++;
                permsTotal++;
                var permNo = perm.Key;
                string showName = numberedPerms[permNo];
                string output = showName.Substring(showName.IndexOf('.') + 1);
                string granted = lang.GetMessage("GUIGranted", this);
                
                if (pos1 > 0 && pos1 < 21)
                {
                    granted = lang.GetMessage("GUIGranted", this);
                    if ((bool)PermsCheck(player, group, numberedPerms[permNo])[0])
                        SetButtons(false);
                    List<string> inheritcheck = (List<string>)(PermsCheck(player, group, numberedPerms[permNo])[1]);
                    if (inheritcheck.Count > 0)
                    {
                        if (path.inheritedcheck == numberedPerms[permNo]) 
                        {
                            var mainName1 = elements1.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.99" }, RectTransform = { AnchorMin = "0.3 0.1", AnchorMax = "0.7 0.86" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "ConfirmUI");
                            elements1.Add(new CuiButton { Button = { Command = $"ShowInherited {PlugNumber} null {numberedPerms[permNo]} {group} null {page} -", Color = ButtonColour }, RectTransform = { AnchorMin = "0.4 0.01", AnchorMax = "0.6 0.05" }, Text = { Text = lang.GetMessage("GUIBack", this), FontSize = 14, Align = TextAnchor.MiddleCenter }, }, mainName1);

                            float h1 = 0, h2 = 0;
                            elements1.Add(new CuiButton { Button = { Command = "", Color = ButtonColour }, RectTransform = { AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.825" }, Text = { Text = $"{numberedPerms[permNo]}", FontSize = 12, Align = TextAnchor.MiddleCenter }, }, mainName1);
                            elements1.Add(new CuiLabel { Text = { Text = $"{lang.GetMessage("GUIInheritedFrom", this)}", FontSize = 11, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.77", AnchorMax = "1 0.8" } }, mainName1);

                            for (int i = 0; i < inheritcheck.Count; i++)
                            {
                                h1 = i * 0.022f;
                                h2 = i * 0.022f;
                                elements1.Add(new CuiLabel { Text = { Text = $"{inheritcheck[i]}", FontSize = 11, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 {0.7 - h1}", AnchorMax = $"1 {0.75 - h2}" } }, mainName1);
                            }

                            list = true;
                        }
                        elements.Add(new CuiButton { Button = { Command = $"ShowInherited {PlugNumber} null {numberedPerms[permNo]} {group} null {page} {numberedPerms[permNo]}", Color = InheritedColour }, RectTransform = { AnchorMin = $"0.8 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.9 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = lang.GetMessage("GUIInherited", this), FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
                    } 
                    elements.Add(new CuiButton { Button = { Color = ButtonColour }, RectTransform = { AnchorMin = $"0.1 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.45 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = $"{output}", FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);

                    elements.Add(new CuiButton { Button = { Command = $"PermsList {PlugNumber} grant {numberedPerms[permNo]} {group} null {page}", Color = ButtonColour1 }, RectTransform = { AnchorMin = $"0.5 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.6 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = lang.GetMessage("GUIGranted", this), FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"PermsList {PlugNumber} revoke {numberedPerms[permNo]} {group} null {page}", Color = ButtonColour2 }, RectTransform = { AnchorMin = $"0.65 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.75 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = lang.GetMessage("GUIRevoked", this), FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
                }
            }

            elements.Add(new CuiButton { Button = { Command = $"Navigate {group} {path.previousPage}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.4 0.02", AnchorMax = "0.6 0.04" }, Text = { Text = lang.GetMessage("GUIBack", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = msg, FontSize = 16, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.95", AnchorMax = "1 1" } }, mainName);

            if (permsTotal > (page * 20))
                elements.Add(new CuiButton { Button = { Command = $"PermsList {PlugNumber} null null {group} null {next}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.8 0.02", AnchorMax = "0.9 0.04" }, Text = { Text = "->", FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            if (page > 1)
                elements.Add(new CuiButton { Button = { Command = $"PermsList {PlugNumber} null null {group} null {previous}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.1 0.02", AnchorMax = "0.2 0.04" }, Text = { Text = "<-", FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            CuiHelper.AddUi(player, elements);

            if (list)
                CuiHelper.AddUi(player, elements1);
        }

        void ViewPlayersUI(BasePlayer player, string msg, int page)
        {
            var path = ActiveAdmins[player.userID];
            var outmsg = string.Format(lang.GetMessage("GUIPlayersIn", this), msg);
            string guiString = String.Format("0.1 0.1 0.1 {0}", guitransparency);
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.32 0.1", AnchorMax = "0.68 0.9" }, CursorEnabled = true }, "Overlay", "PermsUI");
            elements.Add(new CuiElement { Parent = "PermsUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });

            int playerCounter = 0, pos1 = 20 - (page * 20), next = page + 1, previous = page - 1;

            foreach (var useringroup in permission.GetUsersInGroup(path.subjectGroup))
            {
                pos1++;
                playerCounter++;
                if (pos1 > 0 && pos1 < 21)
                    elements.Add(new CuiButton { Button = { Color = ButtonColour }, RectTransform = { AnchorMin = $"0.2 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.8 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = $"{useringroup}", FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
            }
            elements.Add(new CuiLabel { Text = { Text = outmsg, FontSize = 16, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.95", AnchorMax = "1 1" } }, mainName);

            if (playerCounter > (page * 20))
                elements.Add(new CuiButton { Button = { Command = $"PlayersIn {next}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.8 0.02", AnchorMax = "0.9 0.04" }, Text = { Text = lang.GetMessage("->", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            if (page > 1)
                elements.Add(new CuiButton { Button = { Command = $"PlayersIn {previous}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.1 0.02", AnchorMax = "0.2 0.04" }, Text = { Text = lang.GetMessage("<-", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);

            elements.Add(new CuiButton { Button = { Command = $"PMEmptyGroup", Color = ButtonColour }, RectTransform = { AnchorMin = "0.25 0.02", AnchorMax = "0.45 0.04" }, Text = { Text = lang.GetMessage("removePlayers", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            elements.Add(new CuiButton { Button = { Command = $"Navigate true {1}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.55 0.02", AnchorMax = "0.75 0.04" }, Text = { Text = lang.GetMessage("GUIBack", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);

            CuiHelper.AddUi(player, elements);
        }

        void ViewGroupsUI(BasePlayer player, string msg, int page)
        {
            var path = ActiveAdmins[player.userID];
            var outmsg = string.Format(lang.GetMessage("GUIGroupsFor", this), msg);
            string guiString = String.Format("0.1 0.1 0.1 {0}", guitransparency);
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.32 0.1", AnchorMax = "0.68 0.9" }, CursorEnabled = true }, "Overlay", "PermsUI");
            elements.Add(new CuiElement { Parent = "PermsUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });

            int groupTotal = 0, pos1 = 20 - (page * 20), next = page + 1, previous = page - 1;

            foreach (var group in permission.GetGroups())
            {
                SetButtons(true);
                pos1++;
                groupTotal++;
                if (pos1 > 0 && pos1 < 21)
                {
                    foreach (var user in permission.GetUsersInGroup(group))
                    {
                        if (user.Contains(path.subject.UserIDString))
                        {
                            SetButtons(false);
                            break;
                        }
                    }

                    //MAKE THIS OPEN UI FOR THAT GROUP 
                    elements.Add(new CuiButton { Button = { Color = ButtonColour }, RectTransform = { AnchorMin = $"0.2 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.5 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = $"{group}", FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"GroupAddRemove add {group} {page}", Color = ButtonColour1 }, RectTransform = { AnchorMin = $"0.55 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.65 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = lang.GetMessage("GUIGranted", this), FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"GroupAddRemove remove {group} {page}", Color = ButtonColour2 }, RectTransform = { AnchorMin = $"0.7 {(0.89 - (pos1 * 3f) / 100f)}", AnchorMax = $"0.8 {(0.91 - (pos1 * 3f) / 100f)}" }, Text = { Text = lang.GetMessage("GUIRevoked", this), FontSize = 10, Align = TextAnchor.MiddleCenter } }, mainName);
                }
            }
            elements.Add(new CuiButton { Button = { Command = $"Navigate false {1}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.4 0.02", AnchorMax = "0.6 0.04" }, Text = { Text = lang.GetMessage("GUIBack", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = outmsg, FontSize = 16, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.95", AnchorMax = "1 1" } }, mainName);

            if (groupTotal > (page * 20))
                elements.Add(new CuiButton { Button = { Command = $"Groups {next}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.7 0.02", AnchorMax = "0.8 0.04" }, Text = { Text = lang.GetMessage("->", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
            if (page > 1)
                elements.Add(new CuiButton { Button = { Command = $"Groups {previous}", Color = ButtonColour }, RectTransform = { AnchorMin = "0.2 0.02", AnchorMax = "0.3 0.04" }, Text = { Text = lang.GetMessage("<-", this), FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);

            CuiHelper.AddUi(player, elements); 
        }
        #endregion

        #region console commands
        [ConsoleCommand("PMToMain")]
        private void PMToMain(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            MainUI(player, false, 1);
        }

        [ConsoleCommand("PMTogglePlayerGroup")]
        private void PMTogglePlayerGroup(ConsoleSystem.Arg arg, bool group, int page)
        {
            var player = arg?.Connection?.player as BasePlayer;
            if (player == null || arg.Args == null || arg.Args.Length != 2) return;
            group = !(Convert.ToBoolean(arg.Args[0]));
            page = Convert.ToInt16(arg.Args[1]);
            DestroyMenu(player, false);
            MainUI(player, group, page);
        }

        [ConsoleCommand("ShowInherited")]
        private void ShowInherited(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            var path = ActiveAdmins[player.userID];
            if (player == null || arg.Args == null || arg.Args.Length < 6) return;
            int pageNo = Convert.ToInt32(arg.Args[5]);
            path.inheritedcheck = arg.Args[6];
            var plugNumber = Convert.ToInt32(arg.Args[0]);
            string plugName = PlugList[Convert.ToInt32(arg.Args[0])];
            DestroyMenu(player, false);
            PermsUI(player, $"{path.subject.displayName} - {plugName}", plugNumber, "false", pageNo);
        }

        [ConsoleCommand("PMEmptyGroup")]
        private void PMEmptyGroup(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;

            var elements1 = new CuiElementContainer();
            var mainName1 = elements1.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.8" }, RectTransform = { AnchorMin = "0.4 0.42", AnchorMax = "0.6 0.48" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "ConfirmUI");
            elements1.Add(new CuiElement { Parent = "ConfirmUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });
            elements1.Add(new CuiButton { Button = { Command = $"Empty true", Color = ButtonColour }, RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.4 0.8" }, Text = { Text = lang.GetMessage("confirm", this), FontSize = 14, Align = TextAnchor.MiddleCenter }, }, mainName1);
            elements1.Add(new CuiButton { Button = { Command = $"Empty false", Color = ButtonColour }, RectTransform = { AnchorMin = "0.6 0.2", AnchorMax = "0.9 0.8" }, Text = { Text = lang.GetMessage("cancel", this), FontSize = 14, Align = TextAnchor.MiddleCenter }, }, mainName1);

            CuiHelper.AddUi(player, elements1);
        }

        [ConsoleCommand("EmptyGroup")]//user console command
        private void EmptyGroup(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;

            if (player != null && !HasPermission(player.UserIDString, permAllowed) & !IsAuth(player))
            {
                SendReply(player, TitleColour + lang.GetMessage("title", this) + "</color>" + MessageColour + lang.GetMessage("NotAdmin", this) + "</color>");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 1)
                return;
            string groupname = arg.Args[0];
            var list = permission.GetUsersInGroup(groupname);
            if (list == null || list.Length == 0)
            {
                Puts($"Group {groupname} was not found.");
                return;
            }
            foreach (var user in permission.GetUsersInGroup(groupname))
            {
                string str = user.Substring(0, 17);
                permission.RemoveUserGroup(str, groupname);
            }
            Puts($"All users were removed from {groupname}");
        }

        [ConsoleCommand("Empty")]
        private void Empty(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            var path = ActiveAdmins[player.userID];
            string confirmation = arg.Args[0];
            if (confirmation == "true")
            {
                int count = 0;
                foreach (var user in permission.GetUsersInGroup(path.subjectGroup))
                {
                    count++;
                    string str = user.Substring(0, 17);
                    permission.RemoveUserGroup(str, path.subjectGroup);
                    DestroyMenu(player, false);
                    var argsOut = new string[] { "group", path.subjectGroup };
                    CmdPerms(player, null, argsOut);
                }
                if (count == 0)
                    CuiHelper.DestroyUi(player, "ConfirmUI");
            }
            else
                CuiHelper.DestroyUi(player, "ConfirmUI");
        }

        [ConsoleCommand("GroupAddRemove")]
        private void GroupAddRemove(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            var path = ActiveAdmins[player.userID];
            if (player == null || arg.Args == null || arg.Args.Length < 3) return;
            string Pname = path.subject.userID.ToString();
            string userGroup = arg.Args[1];
            int page = Convert.ToInt32(arg.Args[2]);
            if (arg.Args[0] == "add")
                permission.AddUserGroup(Pname, userGroup);
            if (arg.Args[0] == "remove")
                permission.RemoveUserGroup(Pname, userGroup);
            DestroyMenu(player, false);
            ViewGroupsUI(player, $"{path.subject.displayName}", page);
        }

        [ConsoleCommand("Groups")]
        private void GroupsPM(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            var path = ActiveAdmins[player.userID];
            if (player == null || arg.Args == null || arg.Args.Length < 1) return;
            int page = Convert.ToInt32(arg.Args[0]);
            DestroyMenu(player, false);
            ViewGroupsUI(player, $"{path.subject.displayName}", page);
        }

        [ConsoleCommand("PlayersIn")]
        private void PlayersPM(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            var path = ActiveAdmins[player.userID];
            if (player == null || arg.Args == null || arg.Args.Length < 1) return;
            int page = Convert.ToInt32(arg.Args[0]);
            DestroyMenu(player, false);
            ViewPlayersUI(player, $"{path.subjectGroup}", page);
        }

        [ConsoleCommand("ClosePM")]
        private void ClosePM(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            if (player == null) return;
            DestroyMenu(player, true);
        }

        [ConsoleCommand("Navigate")]
        private void Navigate(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            var path = ActiveAdmins[player.userID];
            if (player == null || arg.Args == null || arg.Args.Length < 2) return;
            ActiveAdmins[player.userID].previousPage = Convert.ToInt32(arg.Args[1]);
            DestroyMenu(player, false);
            string[] argsOut;
            if (arg.Args[0] == "true")
            {
                argsOut = new string[] { "group", path.subjectGroup, path.previousPage.ToString() };
                CmdPerms(player, null, argsOut);
            }
            else
            {
                argsOut = new string[] { "player", path.subject.userID.ToString(), path.previousPage.ToString() };
                CmdPerms(player, null, argsOut);
            }
            return;
        }

        [ConsoleCommand("PMSelected")]
        private void PMSelected(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            DestroyMenu(player, false);
            string[] argsOut;
            argsOut = new string[] { arg.Args[0], arg.Args[1] };
            if (arg.Args[0] == "player")
                ActiveAdmins[player.userID].subject = FindPlayer(Convert.ToUInt64(arg.Args[1]));
            else
                ActiveAdmins[player.userID].subjectGroup = arg.Args[1];
            CmdPerms(player, null, argsOut);
            return;
        }

        [ConsoleCommand("PermsList")]
        private void PermsList(ConsoleSystem.Arg arg, int plugNumber)
        {
            var player = arg?.Connection?.player as BasePlayer;
            var path = ActiveAdmins[player.userID];
            if (player == null || arg.Args == null || arg.Args.Length < 6) return;
            int pageNo = Convert.ToInt32(arg.Args[5]);
            string Pname;
            string group = arg.Args[3];

            if (arg.Args[4] == "all")
            {
                if (arg.Args[2] != null)
                {
                    Pname = path.subject?.userID.ToString();
                    string action = arg.Args[1];
                    foreach (var perm in numberedPerms)
                    {
                        if (AllPerPage == true && perm.Key > (pageNo * 20) - 20 && perm.Key < ((pageNo * 20) + 1))
                        {
                            if (action == "grant" && group == "false")
                                permission.GrantUserPermission(Pname, perm.Value, null);
                            if (action == "revoke" && group == "false")
                                permission.RevokeUserPermission(Pname, perm.Value);
                            if (action == "grant" && group == "true")
                                permission.GrantGroupPermission(path.subjectGroup, perm.Value, null);
                            if (action == "revoke" && group == "true")
                                permission.RevokeGroupPermission(path.subjectGroup, perm.Value);
                        }
                        if (AllPerPage == false)
                        {
                            if (action == "grant" && group == "false")
                                permission.GrantUserPermission(Pname, perm.Value, null);
                            if (action == "revoke" && group == "false")
                                permission.RevokeUserPermission(Pname, perm.Value);
                            if (action == "grant" && group == "true")
                                permission.GrantGroupPermission(path.subjectGroup, perm.Value, null);
                            if (action == "revoke" && group == "true")
                                permission.RevokeGroupPermission(path.subjectGroup, perm.Value);
                        }
                    }
                }
            }
            else
            {
                Pname = path.subject?.userID.ToString();
                string action = arg.Args[1];
                string PermInHand = arg.Args[2];
                if (arg.Args[2] != null)
                {
                    if (action == "grant" && group == "false")
                        permission.GrantUserPermission(Pname, PermInHand, null);
                    if (action == "revoke" && group == "false")
                        permission.RevokeUserPermission(Pname, PermInHand);
                    if (action == "grant" && group == "true")
                        permission.GrantGroupPermission(path.subjectGroup, PermInHand, null);
                    if (action == "revoke" && group == "true")
                        permission.RevokeGroupPermission(path.subjectGroup, PermInHand);
                }
            }
            plugNumber = Convert.ToInt32(arg.Args[0]);
            string plugName = PlugList[plugNumber];

            numberedPerms.Clear();
            int numOfPerms = 0;
            foreach (var perm in permission.GetPermissions())
            {
                if (perm.Contains($"{plugName}."))
                {
                    numOfPerms++;
                    numberedPerms.Add(numOfPerms, perm);
                }
            }
            DestroyMenu(player, false);
            if (group == "false")
                PermsUI(player, $"{path.subject.displayName} - {plugName}", plugNumber, group, pageNo);
            else
                PermsUI(player, $"{path.subjectGroup} - {plugName}", plugNumber, group, pageNo);
            return;
        }
        #endregion

        #region chat commands
        [ChatCommand("perms")]
        void CmdPerms(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player.UserIDString, permAllowed) & !IsAuth(player))
            {
                SendReply(player, TitleColour + lang.GetMessage("title", this) + "</color>" + MessageColour + lang.GetMessage("NotAdmin", this) + "</color>");
                return;
            }
            if (ActiveAdmins.ContainsKey(player.userID))
                ActiveAdmins.Remove(player.userID);
            ActiveAdmins.Add(player.userID, new Info());
            var path = ActiveAdmins[player.userID];
            GetPlugs(player);

            int page = 1;
            if (args.Length == 3)
                page = Convert.ToInt32(args[2]);

            if (args == null || args.Length < 2)
            {
                bool group = (args != null && args.Length == 1 && args[0] == "group") ? true : false;
                if (MenuOpen.Contains(player.userID))
                    DestroyMenu(player, group);
                BgUI(player);
                MainUI(player, group, 1);
                return;
            }
            if (args[0] == "player")
            {
                UInt64 n = 0;
                bool isNumeric = UInt64.TryParse(args[1], out n);
                path.subject = isNumeric ? FindPlayer(n) : FindPlayerByName(args[1]);
                if (path.subject == null)
                {
                    SendReply(player, TitleColour + lang.GetMessage("title", this) + "</color>" + MessageColour + lang.GetMessage("NoPlayer", this) + "</color>", args[1]);
                    return;
                }
                string msg = string.Format(lang.GetMessage("GUIName", this), path.subject.displayName);

                if (MenuOpen.Contains(player.userID))
                    DestroyMenu(player, true);
                BgUI(player);
                PlugsUI(player, msg, "false", page);
            }
            else if (args[0] == "group")
            {
                List<string> Groups = new List<string>();
                foreach (var group in permission.GetGroups())
                    Groups.Add(group);
                if (Groups.Contains($"{args[1]}"))
                {
                    string msg = string.Format(lang.GetMessage("GUIName", this), args[1]);

                    ActiveAdmins[player.userID].subjectGroup = args[1];
                    if (MenuOpen.Contains(player.userID))
                        DestroyMenu(player, true);
                    BgUI(player);
                    PlugsUI(player, msg, "true", page);
                    return;
                }
                SendReply(player, TitleColour + lang.GetMessage("title", this) + "</color>" + MessageColour + lang.GetMessage("NoGroup", this) + "</color>", args[1]);
            }
            else
                SendReply(player, TitleColour + lang.GetMessage("title", this) + "</color>" + MessageColour + lang.GetMessage("Syntax", this) + "</color>");
        }

        List<BasePlayer> GetAllPlayers()
        {
            List<BasePlayer> available = new List<BasePlayer>();
            foreach (BasePlayer online in BasePlayer.activePlayerList)
                available.Add(online);

            foreach (BasePlayer sleeper in BasePlayer.sleepingPlayerList)
                available.Add(sleeper);
            return available;
        }

        BasePlayer FindPlayer(ulong ID)
        {
            BasePlayer result = null;
            foreach (BasePlayer current in GetAllPlayers())
            {
                if (current.userID == ID)
                    result = current;
            }
            return result;
        }

        BasePlayer FindPlayerByName(string name)
        {
            BasePlayer result = null;
            foreach (var player in GetAllPlayers())
            {
                if (player.displayName.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || player.UserIDString.Contains(name, CompareOptions.OrdinalIgnoreCase)
                    || player.displayName.Contains(name, CompareOptions.OrdinalIgnoreCase))
                    result = player;
            }
            return result;
        }

        #endregion

        #region config
        string TitleColour = "<color=orange>";
        string MessageColour = "<color=white>";
        double guitransparency = 0.5;
        string BlockList = "playerranks,botspawn";
        string ButtonColour = "0.7 0.32 0.17 1";
        string OnColour = "0.7 0.32 0.17 1";
        string OffColour = "0.2 0.2 0.2 1";
        string InheritedColour = "0.9 0.6 0.17 1";

        bool AllPerPage;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        private void LoadConfigVariables()
        {
            CheckCfg("Options - GUI Transparency 0-1", ref guitransparency);
            CheckCfg("Options - Plugin BlockList", ref BlockList);
            CheckCfg("Chat - Title colour", ref TitleColour);
            CheckCfg("Chat - Message colour", ref MessageColour);
            CheckCfg("GUI - Label colour", ref ButtonColour);
            CheckCfg("GUI - All = per page", ref AllPerPage);
            CheckCfg("GUI - On colour", ref OnColour);
            CheckCfg("GUI - Off colour", ref OffColour);
            CheckCfg("GUI - Inherited colour", ref InheritedColour);
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        #endregion

        #region messages
        readonly Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "Permissions Manager: " }, 
            {"NoGroup", "Group {0} was not found." },
            {"NoPlayer", "Player {0} was not found." },
            {"GUIAll", "Grant All" },
            {"GUINone", "Revoke All" },
            {"GUIBack", "Back" },
            {"GUIClose", "Close" },
            {"GUIGroups", "Groups" },
            {"GUIPlayers", "Players" },
            {"GUIInherited", "Inherited" },
            {"GUIInheritedFrom", "Inherited from" },
            {"GUIGranted", "Granted" },
            {"GUIRevoked", "Revoked" },
            {"GUIName", "Permissions for {0}" },
            {"GUIGroupsFor", "Groups for {0}"},
            {"GUIPlayersIn", "Players in {0}"},
            {"removePlayers", "Remove All Players"},
            {"confirm", "Confirm"},
            {"cancel", "Cancel"},
            {"NotAdmin", "You need Auth Level 2, or permission, to use this command."},
            {"Back", "Back"},
            {"All", "All"},
            {"Syntax", "Use /perms, /perms player *name*, or /perms group *name*"}
        };
        #endregion
    }
}
