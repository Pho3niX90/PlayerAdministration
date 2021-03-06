﻿using Network;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("NetGroupSwitch", "Pho3niX90", "0.0.4")]
    [Description("")]
    public class NetGroupSwitch : RustPlugin
    {
        Dictionary<uint, string> netIds = new Dictionary<uint, string>();
        void OnPlayerDisconnected(BasePlayer player, string reason) {
            if (HasDim(player)) netIds.Remove(player.net.ID);
        }
        void OnPlayerConnected(BasePlayer player) {
            SwitchDim(player, string.Empty);
        }

        [ChatCommand("d")]
        void Disappear(BasePlayer player, string command, string[] args) {
            string dimension = args.Length > 0 ? args[0].ToString() : string.Empty;
            SwitchDim(player, dimension);
        }

        void UpdatePlayers() {
            foreach (BasePlayer target in BasePlayer.activePlayerList) {
                UpdateConnections(target);
            }
        }

        void UpdateConnections(BasePlayer player) {
            if (Net.sv.write.Start()) {
                Net.sv.write.PacketID(Message.Type.EntityDestroy);
                Net.sv.write.EntityID(player.net.ID);
                Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                Net.sv.write.Send(new SendInfo(player.net.group.subscribers.Where(x => x.userid != player.userID && !GetDim(x.player as BasePlayer).Equals(GetDim(player))).ToList()));
            }
            player.SendNetworkUpdateImmediate(true);
        }

        private object CanNetworkTo(BasePlayer player, BasePlayer target) {
            if (player == null || target == null) return null;
            if (GetDim(target).Equals(GetDim(player))) return null;
            return false;
        }

        #region Dimension Helpers
        private void SwitchDim(BasePlayer player, string dimension) {
            if (player == null) return;
            Puts($"Player `{player.displayName}` has moved to dimension `{dimension}`");
            if (netIds.ContainsKey(player.net.ID)) {
                netIds[player.net.ID] = dimension;
            } else if (!dimension.IsNullOrEmpty()) {
                netIds.Add(player.net.ID, dimension);
            }
            UpdatePlayers();
        }

        object CanNetworkTo(HeldEntity entity, BasePlayer target) {
            return entity == null ? null : CanNetworkTo(entity.GetOwnerPlayer(), target);
        }
        object CanNetworkTo(BaseProjectile entity, BasePlayer target) {
            return entity == null ? null : CanNetworkTo(entity.GetOwnerPlayer(), target);
        }
        object CanNetworkTo(Projectile entity, BasePlayer target) {
            return entity == null ? null : CanNetworkTo(entity.owner, target);
        }

        string GetDim(BaseEntity ent) => netIds.ContainsKey(ent.net.ID) ? netIds[ent.net.ID] : string.Empty;
        bool HasDim(BaseEntity ent) => netIds.ContainsKey(ent.net.ID) && !netIds[ent.net.ID].IsNullOrEmpty();
        #endregion
    }
}
