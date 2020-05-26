using UnityEngine;
using Oxide.Core.Configuration;
using System.Linq;
using System;

namespace Oxide.Plugins {

	[Info("Scrap Heli Storage", "yetzt", "0.0.5")]
	[Description("Adds Storage Boxes to Scrap Transport Helicopters")]

	public class ScrapHeliStorage : RustPlugin {

		private string prefab = "assets/content/vehicles/boats/rhib/subents/rhib_storage.prefab";

		#region Config
		private PluginConfig config;
		private int num;
		private bool retrofit;
		
		protected override void LoadDefaultConfig() {
			Config.WriteObject(GetDefaultConfig(), true);
		}

		private PluginConfig GetDefaultConfig() {
			return new PluginConfig
			{
				Enabled = true,
				NumBoxes = 2,
				Retrofit = false
			};
		}

		private class PluginConfig {
			public bool Enabled;
			public int NumBoxes;
			public bool Retrofit;
		}
		#endregion

		#region Oxide
		private void Init() {
			config = Config.ReadObject<PluginConfig>();
			
			try {
				num = Convert.ToInt32(config.NumBoxes);
			} catch {
				num = 2;
			}

			if (num < 1) num = 1;
			if (num > 2) num = 2;
			
			try {
				retrofit = Convert.ToBoolean(config.Retrofit);
			} catch {
				retrofit = false;
			}
			
		}
		
		// add boxes to already existing copters without boxes when plugin is loaded
		void OnServerInitialized(){
			if (retrofit) RetrofitBoxes();
		}

		void RetrofitBoxes(){
			// find existing copters and add bxes
			var copters = GameObject.FindObjectsOfType<ScrapTransportHelicopter>();
			for (int i = 0; i < copters.Length; i++) {
				AddBoxes(copters[i]);
			}
		}

		private void OnEntitySpawned(ScrapTransportHelicopter entity) {
			if (entity == null || !config.Enabled) return;

			// defer checking to ensure storage box is loaded (loads after parent, race condition there)
			timer.Once(0.1f, () => {
				AddBoxes(entity);
			});

		}
		
		private void AddBoxes(ScrapTransportHelicopter entity) {
			if (entity == null || !config.Enabled || num == 0) return;

			// check if there is already a box
			foreach (var child in entity.GetComponentsInChildren<StorageContainer>(true)) {
				if (child.name == prefab) return;
			}
					
			// Putting the Transport in Transport Helicopter
			if (num == 1) {

				var box = GameManager.server?.CreateEntity(prefab, entity.transform.position) as StorageContainer;
				if (box == null) return;
				box.Spawn();
				box.SetParent(entity);
				box.transform.localPosition = new Vector3(0f, 0.85f, 1.75f);
				box.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
				box.SendNetworkUpdateImmediate(true);
				
			} else if (num == 2) {

				var box = GameManager.server?.CreateEntity(prefab, entity.transform.position) as StorageContainer;
				if (box == null) return;
				box.Spawn();
				box.SetParent(entity);
				box.transform.localPosition = new Vector3(-0.5f, 0.85f, 1.75f);
				box.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
				box.SendNetworkUpdateImmediate(true);

				var box2 = GameManager.server?.CreateEntity(prefab, entity.transform.position) as StorageContainer;
				if (box2 == null) return;
				box2.Spawn();
				box2.SetParent(entity);
				box2.transform.localPosition = new Vector3(0.5f, 0.85f, 1.75f);
				box2.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
				box2.SendNetworkUpdateImmediate(true);
				
			}

		}

		// drop items when entity is killed (don't want salty tears)
		private void OnEntityKill(ScrapTransportHelicopter entity) {
			if (entity == null) return;
			foreach (var child in entity.GetComponentsInChildren<StorageContainer>(true)) {
				if (child.name == prefab) {
					child.DropItems();
				}
			}
		}
		
		#endregion
	}
}

