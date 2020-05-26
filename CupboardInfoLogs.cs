using System;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("CupboardInfoLogs", "NubbbZ", "1.1.1")]
	[Description("Logs info about TC when placed!")]
	class CupboardInfoLogs : CovalencePlugin
	{
		#region Hooks
		void OnEntityBuilt(Planner plan, GameObject go)
		{
			BasePlayer Player = plan.GetOwnerPlayer();
			BaseEntity Entity = go.ToBaseEntity();

			if (Entity.ShortPrefabName.Contains("cupboard.tool"))
			{
				Make_Log(Player, Entity);
			}
		}
		#endregion

		#region Functions
		private void Make_Log(BasePlayer Player, BaseEntity TC)
		{
			string Filename = TC.net.ID.ToString();
			string Content = "Owner: " + Player.displayName;

			Content += "Owner: " + Player.displayName + "\n";
			Content += "\n" + "Location: " + TC.ServerPosition.ToString() + "\n";
			Content += "\n" + "Creation Date: " + DateTime.Now.ToString();

			LogToFile(Filename, Content, this, true);
		}
		#endregion
	}
}
