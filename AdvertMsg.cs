using Oxide.Core;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("Advert Msgs", "Pho3niX90", "0.0.1")]
    class AdvertMsg : RustPlugin {
        List<Advert> adverts = new List<Advert>();

        private void Init() {
            LoadData();
            if (adverts.Count == 0) {
                adverts.Add(new Advert(60, "Get <color=#d8b300>PREMIUM</color> access to mods, kits, skins and queue skipping and more. Visit <color=#d8b300>http://kingdomrust.com</color>"));
                //adverts.Add(new Advert(30, "Join the upcoming <color=#d8b300>KING OF THE KINGDOM</color> tournament 2 May, with a cash prize of <color=#d8b300>R2000</color>, see discord for more information"));
                adverts.Add(new Advert(30, "See our other servers with <color=#d8b300>/servers</color>"));
                adverts.Add(new Advert(35, "Want to get a notification on your phone when you get raided? Download <color=#d8b300>Rust Notifications</color> on your phone."));
                adverts.Add(new Advert(55, "Have you tried out our new Scrim and Aim Train server: <color=#d8b300>Kingdom AIMTRAIN</color>"));
            }

            foreach (Advert ad in adverts)
                timer.Every(60 * ad.intervalMinutes, () => SendReplyWithIcon(ad.msg));
        }

        void SendReplyWithIcon(string msg) {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                Player.Reply(player, msg, 76561199044451528L);
        }

        public class Advert {
            public int intervalMinutes = 60;
            public string msg = "";
            public Advert(int interval, string msg) {
                this.intervalMinutes = interval;
                this.msg = msg;
            }
        }
        void SaveData() {

            try {
                Interface.Oxide.DataFileSystem.WriteObject<List<Advert>>($"Adverts", adverts, true);
            } catch (Exception e) {
            }
        }
        void LoadData(bool reset = false) {
            try {
                adverts = Interface.Oxide.DataFileSystem.ReadObject<List<Advert>>($"Adverts");
            } catch (Exception e) {

            }
        }
    }
}
