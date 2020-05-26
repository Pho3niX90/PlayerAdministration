using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("Hide Kick", "Pho3niX90", "0.0.1")]
    [Description("Hides kick msgs")]
    class HideKick : RustPlugin {

        public List<string> msgs = new List<string>();
        private object OnServerMessage(string message, string name) {

            if (message.Contains("Kicking") && name == "SERVER") {
                if (msgs.Contains(message)) {
                    return true;
                } else {
                    msgs.Add(message);
                }
            }
            return null;
        }
    }
}
