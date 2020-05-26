using System;

namespace Oxide.Plugins {

	[Info("Recycler Speed", "yetzt", "1.0.4")]
	[Description("Change the Speed of Recyclers")]

	public class RecyclerSpeed : RustPlugin {
		
		public float speed = 5.0f;

		// config file overhead
		private PluginConfig config;
		
		protected override void LoadDefaultConfig()
		{
			Config.WriteObject(GetDefaultConfig(), true);
		}

		private PluginConfig GetDefaultConfig()
		{
			return new PluginConfig {
				Speed = 5
			};
		}

		private class PluginConfig
		{
			public float Speed;
		}
		
		private void Init() {
			// register permission
			permission.RegisterPermission("recyclerspeed.use", this);

			// get config
			config = Config.ReadObject<PluginConfig>();

			// filter insane values without throwing on exceptions, 
			// default to recycler default
			try {
				speed = Convert.ToSingle(config.Speed);
			} catch (FormatException) {
				speed = 5.0f;
			} catch (OverflowException) {
				speed = 5.0f;
			}

			// enforce minimum speed 
			if (speed < 0.1f) speed = 0.1f;

		}

		// actual code
		private object OnRecyclerToggle(Recycler r, BasePlayer p) {
		
			// check permission
			if (!permission.UserHasPermission(p.UserIDString, "recyclerspeed.use")) return null;
		
			if (r.IsOn() == false) {
				timer.Once(0.1f, () => {
					r.CancelInvoke(new Action(r.RecycleThink));
					r.InvokeRepeating(new Action(r.RecycleThink), (speed-0.1f), speed);
				});
			}
			return null;
		}
	}
}
