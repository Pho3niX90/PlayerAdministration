using System.Collections.Generic;

namespace Oxide.Plugins
{
  [Info("Vehicle Locks", "MrPoundsign", "1.0.3")]
  [Description("Vehicle ownership and locking")]

  class VehicleLocks : RustPlugin
  {
    // Jockey (Driver) Seats
    const string chinookPilotSeat = "assets/prefabs/vehicle/seats/pilotseat.prefab";
    const string sedanDriverSeat = "assets/prefabs/vehicle/seats/driverseat.prefab";
    const string smallBoatDriverSeat = "assets/prefabs/vehicle/seats/smallboatdriver.prefab";
    const string miniHeliSeat = "assets/prefabs/vehicle/seats/miniheliseat.prefab";
    const string rhibDriverSeat = "assets/prefabs/vehicle/seats/standingdriver.prefab";
    const string saddlePrefab = "assets/prefabs/vehicle/seats/saddletest.prefab";
    const string scrapTransportSeat = "assets/prefabs/vehicle/seats/transporthelipilot.prefab";

    #region Oxide Hooks
    protected override void LoadDefaultMessages()
    {
      lang.RegisterMessages(new Dictionary<string, string>
      {
        ["vehicle.locked"] = "You don't have the keys for this vehicle!",
        ["vehicle.owned"] = "You now own this vehicle.",
      }, this);
    }
    #endregion

    object CanMountEntity(BasePlayer player, BaseMountable entity)
    {
      if (
        player == null ||
        entity == null ||
        player is Scientist ||
        player.IPlayer == null
      ) return null;

      // Only restrict to driver's seats
      if (
          entity.name != chinookPilotSeat &&
          entity.name != sedanDriverSeat &&
          entity.name != smallBoatDriverSeat &&
          entity.name != miniHeliSeat &&
          entity.name != rhibDriverSeat &&
          entity.name != saddlePrefab &&
          entity.name != scrapTransportSeat
      ) return null;

      var vehicle = entity.VehicleParent();

      // If the player already owns the vehicle
      if (vehicle == null || vehicle.OwnerID == player.userID) return null;

      // If the player is in a safe zone, they can always take any vehicle
      // Additionaly, if it's unowned, it can be taken. This sets ownership
      // to the player taking the vehicle.
      if (vehicle.OwnerID == 0 || (!player.InSafeZone() && player.CanBuild()))
      {
        player.IPlayer.Message(lang.GetMessage("vehicle.owned", this, player.IPlayer.Id));
        vehicle.OwnerID = player.userID;
        return null;
      }

      // Players who are standing in a place they are authorized to build,
      // with or without TC coverage, they can take a vehicle.
      var bp = player.GetBuildingPrivilege();
      if (bp != null)
      {
        // At this point we need to determine if the owner is authorized to the
        // tool cupboard providing the build privileges. This is so if someone
        // parks their vehicle at an enemy base, anyone can take it.
        foreach (ProtoBuf.PlayerNameID playerNameID in bp.authorizedPlayers)
        {
          if (playerNameID.userid == vehicle.OwnerID)
          {
            player.IPlayer.Message(lang.GetMessage("vehicle.locked", this, player.IPlayer.Id));
            return false;
          }
        }
      }

      // All other checks have failed, the vehicle is theirs to take.
      player.IPlayer.Message(lang.GetMessage("vehicle.owned", this, player.IPlayer.Id));
      vehicle.OwnerID = player.userID;
      return null;
    }
  }
}