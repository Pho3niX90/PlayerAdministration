﻿//Requires: Clans
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("Tournament Whitelist", "Pho3niX90", "0.0.9")]
    [Description("Restricts server access to whitelisted players only")]

    class Whitelist : CovalencePlugin
    {
        #region Initialization

        [PluginReference] Plugin Clans;
        //const string permAdmin = "whitelist.admin";
        const string permAllow = "whitelist.allow";
        const string permKick = "whitelist.kicked";

        bool adminExcluded;
        bool resetOnRestart;

        protected override void LoadDefaultConfig() {
            // Options
            Config["Admin Excluded (true/false)"] = adminExcluded = GetConfig("Admin Excluded (true/false)", true);
            Config["Reset On Restart (true/false)"] = resetOnRestart = GetConfig("Reset On Restart (true/false)", false);

            // Cleanup
            Config.Remove("AdminExcluded");
            Config.Remove("ResetOnRestart");

            SaveConfig();
        }
        void Unload() {
            foreach (var player in players.All) {
                if (!player.HasPermission(permKick)) continue;
                permission.RevokeUserPermission(player.Id, permKick);
            }
        }
        void OnServerInitialized() {
            LoadDefaultConfig();
            LoadDefaultMessages();

            permission.RegisterPermission(permKick, this);
            permission.RegisterPermission(permAllow, this);

            foreach (var player in players.All) {
                if (!player.HasPermission("whitelist.allowed")) continue;
                permission.GrantUserPermission(player.Id, permAllow, null);
                permission.RevokeUserPermission(player.Id, "whitelist.allowed");
            }

            foreach (var group in permission.GetGroups()) {
                if (!permission.GroupHasPermission(group, "whitelist.allowed")) continue;
                permission.GrantGroupPermission(group, permAllow, null);
                permission.RevokeGroupPermission(group, "whitelist.allowed");
            }

            if (!resetOnRestart) return;
            foreach (var group in permission.GetGroups())
                if (permission.GroupHasPermission(group, permAllow)) permission.RevokeGroupPermission(group, permAllow);
            foreach (var user in permission.GetPermissionUsers(permAllow))
                permission.RevokeUserPermission(Regex.Replace(user, "[^0-9]", ""), permAllow);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages() {
            // English
            lang.RegisterMessages(new Dictionary<string, string> {
                //["CommandUsage"] = "Usage: {0} <name or id> <permission>",
                //["NoPlayersFound"] = "No players were found using '{0}'",
                //["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["NotWhitelisted"] = "You are not whitelisted",
                //["WhitelistAdd"] = "'{0}' has been added to the whitelist",
                //["WhitelistRemove"] = "'{0}' has been removed from the whitelist"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string> {
                //["CommandUsage"] = "Utilisation : {0} <nom ou id> <permission>",
                //["NoPlayersFound"] = "Pas de joueurs ont été trouvés à l’aide de « {0} »",
                //["NotAllowed"] = "Vous n’êtes pas autorisé à utiliser la commande « {0} »",
                ["NotWhitelisted"] = "Vous n’êtes pas dans la liste blanche",
                //["Whitelisted"] = ""
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string> {
                //["CommandUsage"] = "Verbrauch: {0} < Name oder Id> <erlaubnis>",
                //["NoPlayersFound"] = "Keine Spieler wurden durch '{0}' gefunden",
                //["NotAllowed"] = "Sie sind nicht berechtigt, verwenden Sie den Befehl '{0}'",
                ["NotWhitelisted"] = "Du bist nicht zugelassenen",
                //["Whitelisted"] = ""
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string> {
                //["CommandUsage"] = "Использование: {0} <имя или идентификатор> <разрешение>",
                //["NoPlayersFound"] = "Игроки не были найдены с помощью {0}",
                //["NotAllowed"] = "Нельзя использовать команду «{0}»",
                ["NotWhitelisted"] = "Вы не можете",
                //["Whitelisted"] = ""
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string> {
                //["CommandUsage"] = "Uso: {0} <nombre o id> <permiso>",
                //["NoPlayersFound"] = "No hay jugadores se encontraron con '{0}'",
                //["NotAllowed"] = "No se permite utilizar el comando '{0}'",
                ["NotWhitelisted"] = "No estás en lista blanca",
                //["Whitelisted"] = ""
            }, this, "es");
        }

        #endregion

        #region Whitelisting

        bool IsWhitelisted(string id) {
            var player = players.FindPlayerById(id);
            return player != null && adminExcluded && player.IsAdmin || permission.UserHasPermission(id, permAllow);
        }
        bool IsEliminated(string id) {
            var player = players.FindPlayerById(id);
            return player != null && adminExcluded && player.IsAdmin || permission.UserHasPermission(id, permKick);
        }

        object CanUserLogin(string name, string id) => (!IsEliminated(id) && GetClan(id) != null && !GetClan(id).Trim().IsNullOrEmpty()) || IsWhitelisted(id) ? null : Lang("NotWhitelisted", id);

        string GetClan(string userID) => Clans?.Call<string>("GetClanOf", userID);
        JObject GetClanData(string tag) {
            return Clans?.Call<JObject>("GetClan", tag);
        }
        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}