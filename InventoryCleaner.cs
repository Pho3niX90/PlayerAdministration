using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Inventory Cleaner", "Iv Misticos", "2.0.0")]
    [Description("This plugin allows players with permission to clean inventories.")]
    class InventoryCleaner : CovalencePlugin
    {
        #region Variables

        private const string PermissionSelf = "inventorycleaner.self";
        private const string PermissionTarget = "inventorycleaner.target";
        private const string PermissionAll = "inventorycleaner.all";

        private const string CommandName = "inventorycleaner.clean";

        #endregion
        
        #region Hooks

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No Permission", "You don't have enough permissions (inventorycleaner.*)." },
                { "Syntax", "Command Syntax:\n" +
                            "self - Clean your own inventory\n" +
                            "all - Clean all players' inventories\n" +
                            "(name or ID) - Clean specific player's inventory" },
                { "Players Only", "This command is available only for players." },
                { "Cleaned", "This inventory was successfully cleaned." },
                { "Not Found", "This player was not found." }
            }, this);
        }

        private void Init()
        {
            permission.RegisterPermission(PermissionAll, this);
            permission.RegisterPermission(PermissionSelf, this);
            permission.RegisterPermission(PermissionTarget, this);
            
            AddCovalenceCommand(CommandName, nameof(CommandUse));
        }

        #endregion

        #region Commands

        private void CommandUse(IPlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                goto syntax;
            }

            switch (args[0].ToLower())
            {
                case "self":
                {
                    if (!player.HasPermission(PermissionSelf))
                        goto noPermissions;

                    if (!(player.Object is BasePlayer))
                    {
                        player.Reply(GetMsg("Players Only", player.Id));
                        return;
                    }
                    
                    ((BasePlayer) player.Object).inventory.Strip();
                    goto cleaned;
                }

                case "all":
                {
                    if (!player.HasPermission(PermissionAll))
                        goto noPermissions;

                    foreach (var user in players.Connected)
                    {
                        (user.Object as BasePlayer)?.inventory.Strip();
                    }
                    
                    goto cleaned;
                }

                default:
                {
                    if (!player.HasPermission(PermissionTarget))
                        goto noPermissions;
                    
                    var users = players.FindPlayers(args[0]);
                    using (var enumerator = users.GetEnumerator())
                    {
                        var firstDone = false;
                        while (enumerator.MoveNext())
                        {
                            if (!(enumerator.Current?.Object is BasePlayer))
                                continue;
                            
                            firstDone = true;
                            ((BasePlayer) enumerator.Current.Object).inventory.Strip();
                        }

                        if (!firstDone)
                        {
                            player.Reply(GetMsg("Not Found", player.Id));
                            return;
                        }
                    }

                    goto cleaned;
                }
            }
            
            syntax:
            player.Reply(GetMsg("Syntax", player.Id));
            return;
            
            cleaned:
            player.Reply(GetMsg("Cleaned", player.Id));
            return;
            
            noPermissions:
            player.Reply(GetMsg("No Permission", player.Id));
        }

        #endregion
        
        #region Helpers

        private string GetMsg(string key, string userId = null) => lang.GetMessage(key, this, userId);

        #endregion
    }
}