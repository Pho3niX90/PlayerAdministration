namespace Oxide.Plugins {
    [Info("Location", "Pho3niX90", "0.0.1")]
    [Description("Location")]
    public class MyLoc : RustPlugin {
        [ChatCommand("loc")]
        private void IPanelCommand(BasePlayer player, string command, string[] args) {
            string loc = $"\"x\": {player.transform.position.x}, \"y\": {player.transform.position.y}, \"z\": {player.transform.position.z}";
            Puts(loc);
            SendReply(player, loc);
        }
    }
}
