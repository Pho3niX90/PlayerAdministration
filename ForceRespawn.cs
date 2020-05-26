namespace Oxide.Plugins {
    [Info("Force Respawn", "Pho3niX90", "0.1.2")]
    [Description("Forces a player respawn when they cannot from death screen")]
    class ForceRespawn : RustPlugin {

        [ChatCommand("delpl")]
        void KillPlayerChatAdmin(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin || args.Length == 0) return;
            ulong playerId;
            ulong.TryParse(args[0].ToString(), out playerId);
            BasePlayer delPlayer = BasePlayer.FindByID(playerId);
            ForceRespawnLifeStory(delPlayer);
        }

        [ConsoleCommand("delpl")]
        void KillPlayerConsoleAdmin(ConsoleSystem.Arg arg) {
            if (!(arg.IsAdmin || arg.IsServerside) || arg.Args.Length == 0) return;
            ulong playerId;
            ulong.TryParse(arg.Args[0].ToString(), out playerId);
            BasePlayer delPlayer = BasePlayer.FindByID(playerId);
            ForceRespawnLifeStory(delPlayer);
        }

        [ChatCommand("frespawn")]
        void KillPlayerChat(BasePlayer player, string command, string[] args) {
            ForceRespawnLifeStory(player);
        }

        [ConsoleCommand("frespawn")]
        private void KillPlayerConsole(ConsoleSystem.Arg arg) {
            ForceRespawnLifeStory(arg.Connection.player as BasePlayer);
        }

        void ForceRespawnLifeStory(BasePlayer playerF) {
            if (playerF != null && playerF.IsDead()) {
                playerF.LifeStoryEnd();
                playerF.Respawn();
            }
        }
    }
}