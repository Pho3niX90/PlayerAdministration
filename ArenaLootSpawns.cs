// Requires: ArenaEvents
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Oxide.Plugins
{
    [Info("ArenaLootSpawns", "k1lly0u", "0.1.04")]
    [Description("Manages custom event loot spawns")]
    class ArenaLootSpawns : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin ZoneManager;

        private StoredData storedData;
        private DynamicConfigFile data;
        private static ArenaLootSpawns ins;
        private static int layerPlcmnt;

        private List<ZoneEditor> zoneEditors = new List<ZoneEditor>();
        private Hash<string, List<BaseEntity>> spawnedContainers = new Hash<string, List<BaseEntity>>();
        private Hash<string, ulong> spawnableTypes = new Hash<string, ulong>();
        #endregion
           
        #region Oxide Hooks
        private void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("Arena/lootspawns");
            layerPlcmnt = LayerMask.GetMask("Construction", "World", "Terrain");
        }

        private void OnServerInitialized()
        {
            ins = this;
            LoadData();
            FindLootTypes();
        }

        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (player == null || entity == null)
                return;

            ZoneEditor editor = player.GetComponent<ZoneEditor>();
            if (editor != null)
            {
                editor.OnLootEntityEnd(entity);                
            }
        }

        private void OnPlayerDisconnected(BasePlayer player) => DestroyComponent(player);

        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (player.GetComponent<ZoneEditor>())
            {
                if (player.GetComponent<ZoneEditor>().currentEntity != null)
                    return false;
                return null;
            }

            foreach(ZoneEditor editor in zoneEditors)
            {
                if (editor.IsLootContainer(container))
                    return false;
            }

            return null;
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            foreach(ZoneEditor editor in zoneEditors)
            {
                if (editor.IsLootContainer(entity))
                {
                    info.damageTypes = new Rust.DamageTypeList();
                    info.HitEntity = null;
                    info.HitMaterial = 0;
                    info.PointStart = Vector3.zero;
                    return;
                }
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            BasePlayer player = entity?.ToPlayer();
            if (player != null)
                DestroyComponent(player);
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null)
                return;

            ZoneEditor editor = player.GetComponent<ZoneEditor>();
            if (editor == null)
                return;

            if (input.WasJustPressed(BUTTON.USE))
            {
                BaseEntity entity = FindEntityFromRay(player);
                if (entity != null)
                    editor.AccessInventory(entity);               
            }
        }

        private void Unload()
        {
            ZoneEditor[] editors = UnityEngine.Object.FindObjectsOfType<ZoneEditor>();
            if (editors != null)
            {
                foreach (ZoneEditor editor in editors)
                {
                    editor.OnPlayerDeath();
                    zoneEditors.Remove(editor);
                    UnityEngine.Object.Destroy(editor);
                }
            }
            ins = null;
        }
        #endregion

        #region Functions
        private void DestroyComponent(BasePlayer player)
        {
            ZoneEditor editor = player.GetComponent<ZoneEditor>();
            if (editor != null)
            {
                editor.OnPlayerDeath();
                zoneEditors.Remove(editor);
                UnityEngine.Object.Destroy(editor);
            }
        }
        
        private void FindLootTypes()
        {
            Dictionary<string, Object> files = FileSystemBackend.cache;
            foreach (string str in files.Keys)
            {
                if ((str.StartsWith("assets/content/") || str.StartsWith("assets/bundled/") || str.StartsWith("assets/prefabs/")) && str.EndsWith(".prefab"))
                {
                    if (str.Contains("resource/loot") || str.Contains("radtown/crate") || str.Contains("radtown/loot") || str.Contains("loot") || str.Contains("radtown/oil") || str.Contains("misc/junkpile/junkpile"))
                    {
                        if (!str.Contains("ot/dm tier1 lootb"))
                        {
                            var gmobj = GameManager.server.FindPrefab(str);

                            if (gmobj?.GetComponent<BaseEntity>() != null)                            
                                spawnableTypes[str] = 0;                            
                        }
                    }
                }
            }
            spawnableTypes["assets/prefabs/deployable/large wood storage/box.wooden.large.prefab"] = 0;
            spawnableTypes["assets/prefabs/deployable/large wood storage/box.wooden.large.prefab"] = 10124;
            spawnableTypes["assets/prefabs/deployable/large wood storage/box.wooden.large.prefab"] = 10123;
            spawnableTypes["assets/prefabs/deployable/large wood storage/box.wooden.large.prefab"] = 10141;            
        }

        public void OnEventStarted(string zoneId)
        {
            List<StoredData.LootSpawn> lootSpawns;
            if (!storedData.lootSpawns.TryGetValue(zoneId, out lootSpawns))
                return;

            for (int i = 0; i < lootSpawns.Count; i++)
            {
                StoredData.LootSpawn lootSpawn = lootSpawns[i];
                BaseEntity entity = InstantiateEntity(lootSpawn.prefabName, lootSpawn.Position(), Quaternion.Euler(lootSpawn.Rotation()));
                entity.enableSaving = false;
                entity.skinID = lootSpawn.skinId;
                entity.Spawn();

                if (lootSpawn.lootItems.Length > 0 && entity.GetComponent<StorageContainer>())
                {
                    ClearContainer(entity);
                    NextTick(()=> FillContainer(lootSpawn.lootItems, entity.GetComponent<StorageContainer>()));
                }

                if (!spawnedContainers.ContainsKey(zoneId))
                    spawnedContainers.Add(zoneId, new List<BaseEntity>());

                spawnedContainers[zoneId].Add(entity);
            }
        }

        public void OnEventFinished(string zoneId)
        {
            List<BaseEntity> entities;
            if (!spawnedContainers.TryGetValue(zoneId, out entities))
                return;            

            for (int i = entities.Count - 1; i >= 0; i--)
            {
                BaseEntity entity = entities[i];

                if (entity != null && !entity.IsDestroyed)
                {
                    StorageContainer container = entity.GetComponent<StorageContainer>();
                    if (container != null)
                    {
                        ClearContainer(entity);
                        container.Die(new HitInfo(container, container, Rust.DamageType.Explosion, 1000f));
                    }

                    JunkPile junkPile = entity.GetComponent<JunkPile>();
                    if (junkPile != null)
                    {
                        for (int y = 0; y < junkPile.spawngroups.Length; y++)
                            junkPile.spawngroups[y].Clear();
                        junkPile.SinkAndDestroy();
                    }
                }
            }

            spawnedContainers[zoneId].Clear();
        }

        private BaseEntity InstantiateEntity(string type, Vector3 position, Quaternion rotation)
        {
            var gameObject = Facepunch.Instantiate.GameObject(GameManager.server.FindPrefab(type), position, rotation);
            gameObject.name = type;

            SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            BaseEntity component = gameObject.GetComponent<BaseEntity>();
            return component;
        }

        private void ClearContainer(BaseEntity container)
        {
            if (container is LootContainer)
            {
                LootContainer lootContainer = container as LootContainer;
                lootContainer.minSecondsBetweenRefresh = -1;
                lootContainer.maxSecondsBetweenRefresh = 0;
                lootContainer.CancelInvoke(lootContainer.SpawnLoot);

                while (lootContainer.inventory.itemList.Count > 0)
                {
                    Item item = lootContainer.inventory.itemList[0];
                    item.RemoveFromContainer();
                    item.Remove(0f);
                }
            }
            else
            {
                StorageContainer storageContainer = container as StorageContainer;
                while (storageContainer.inventory.itemList.Count > 0)
                {
                    Item item = storageContainer.inventory.itemList[0];
                    item.RemoveFromContainer();
                    item.Remove(0f);
                }
            }
        }

        private void FillContainer(StoredData.LootSpawn.ItemData[] items, StorageContainer container)
        {
            for (int i = 0; i < items.Length; i++)
            {
                StoredData.LootSpawn.ItemData itemData = items[i];
                Item item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                item.condition = itemData.condition;

                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }
                if (itemData.contents != null)
                {
                    foreach (var contentData in itemData.contents)
                    {
                        var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                        if (newContent != null)
                        {
                            newContent.condition = contentData.condition;
                            newContent.MoveToContainer(item.contents);
                        }
                    }
                }
                item.MoveToContainer(container.inventory);
            }
        }

        private BaseEntity FindEntityFromRay(BasePlayer player)
        {
            Ray ray = new Ray(player.eyes.position, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 5f))
                return null;

            var hitEnt = hit.collider.GetComponentInParent<BaseEntity>();
            if (hitEnt != null)
                return hitEnt;
            return null;
        }
        #endregion

        #region Placement Component
        public class ZoneEditor : MonoBehaviour
        {
            private BasePlayer player;
            public List<BaseEntity> zoneEntities = new List<BaseEntity>();
            public string zoneId;

            private bool isJunkPile;
            public BaseEntity currentEntity { get; private set; }

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
            }

            private void FixedUpdate()
            {                
                InputState input = player.serverInput;
                Vector3 eyePosition = player.transform.position + (player.modelState.ducked ? Vector3.up * 0.7f : Vector3.up * 1.5f);

                currentEntity.transform.position = new Ray(eyePosition, Quaternion.Euler(input.current.aimAngles) * Vector3.forward).GetPoint(isJunkPile ? 10 : 3);
                currentEntity.transform.rotation = Quaternion.Euler(new Vector3(currentEntity.transform.eulerAngles.x, input.IsDown(BUTTON.RELOAD) ? currentEntity.transform.eulerAngles.y + 5 : currentEntity.transform.eulerAngles.y, currentEntity.transform.eulerAngles.z));

                currentEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))                
                    PlaceItem();                
                else if (input.WasJustPressed(BUTTON.FIRE_SECONDARY))
                    CancelPlacement();                
            }

            private void OnDestroy()
            {
                for (int i = zoneEntities.Count - 1; i >= 0 ; i--)
                {
                    DestroyLoot(zoneEntities[i]);
                }                    
            }

            public void RemoveEntity(BaseEntity entity)
            {
                if (currentEntity == entity)
                    CancelPlacement();
                else
                {
                    if (zoneEntities.Contains(entity))
                    {
                        DestroyLoot(entity);
                        zoneEntities.Remove(entity);
                        player.ChatMessage("Entity removed from loot list");
                    }
                    else player.ChatMessage("The entity you are looking at is not in the loot list");
                }
            }

            public void AccessInventory(BaseEntity entity)
            {
                if (currentEntity != null)
                {
                    player.ChatMessage("You can't open a loot container while trying to place another");
                    return;
                }

                if (zoneEntities.Contains(entity))
                {
                    if (entity.GetComponent<StorageContainer>())
                    {
                        currentEntity = entity;
                        OpenInventory();
                    }
                    else player.ChatMessage("You can not edit the loot of that object");
                }
            }

            public void OnLootEntityEnd(BaseCombatEntity entity)
            {
                if (entity == currentEntity)
                {
                    currentEntity = null;
                    player.ChatMessage("Custom loot set. To edit this loot again look at the entity and press <color=#ce422b>USE</color>");
                }
            }

            public void PlaceNewEntity(string prefabName, ulong skinId = 0)
            {
                currentEntity = ins.InstantiateEntity(prefabName, player.transform.position + (Vector3.forward * 2), new Quaternion());
                currentEntity.enableSaving = false;
                currentEntity.skinID = skinId;               

                isJunkPile = currentEntity is JunkPile;

                if (currentEntity is LootContainer)
                {                    
                    (currentEntity as LootContainer).BlockPlayerItemInput = false;
                    (currentEntity as LootContainer).onlyAcceptCategory = ItemCategory.All;
                }
                currentEntity.Spawn();

                enabled = true;
                player.ChatMessage("Press <color=#ce422b>FIRE</color> to place the object, or <color=#ce422b>AIM</color> to cancel placement\nTo rotate a object hold <color=#ce422b>RELOAD</color>");
            } 

            private void PlaceItem()
            {
                enabled = false;
                zoneEntities.Add(currentEntity);
                if (currentEntity.GetComponent<StorageContainer>())                
                    OpenInventory();                
                else
                {                    
                    currentEntity = null;
                    player.ChatMessage("You can not edit the loot of this object");                   
                }                
            }

            private void CancelPlacement()
            {
                enabled = false;
                DestroyLoot(currentEntity);
                currentEntity = null;
                player.ChatMessage("You have cancelled placement");
            }

            public void OnPlayerDeath() => CancelPlacement();            

            private void OpenInventory()
            {
                player.inventory.loot.Clear();
                player.inventory.loot.PositionChecks = false;
                player.inventory.loot.entitySource = currentEntity;
                player.inventory.loot.itemSource = null;
                player.inventory.loot.AddContainer(currentEntity.GetComponent<StorageContainer>().inventory);
                player.inventory.loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic");
                player.SendNetworkUpdate();
            }

            private void DestroyLoot(BaseEntity entity)
            {
                if (entity == null)
                    return;

                StorageContainer container = entity.GetComponent<StorageContainer>();
                if (container != null)
                {
                    ins.ClearContainer(entity);
                    container.DieInstantly();
                }

                JunkPile junkPile = entity.GetComponent<JunkPile>();
                if (junkPile != null)
                {
                    for (int y = 0; y < junkPile.spawngroups.Length; y++)
                        junkPile.spawngroups[y].Clear();
                    junkPile.SinkAndDestroy();
                }                
            }

            public bool IsLootContainer(BaseEntity entity) => zoneEntities.Contains(entity) || currentEntity == entity;
        }
        #endregion

        #region Commands
        [ChatCommand("loot")]
        private void cmdLoot(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "arena.admin"))
                return;

            if (args.Length == 0)
            {
                SendReply(player, "<color=#939393>Start by navigating to a zone then type <color=#ce422b>/loot edit <zone ID></color>\nNow you are editing a zone. You can add new loot containers or remove existing ones using the commands provided below. If it is a existing loot zone all your previous loot containers will spawn in temporarily\nYou can edit the loot of any container by looking at the container and pressing <color=#ce422b>USE</color>\nWhen you are done editing type <color=#ce422b>/loot save</color> to save what you have done or <color=#ce422b>/loot cancel</color> to forget any changes you have made. All the loot will despawn until a event is played in this zone</color>");
                SendReply(player, "<color=#ce422b>/loot types</color> - Displays all spawnable types and their ID numbers in the ingame console");
                SendReply(player, "<color=#ce422b>/loot edit <zone ID></color> - Start creating, or edit existing, loot spawns for this zone");
                SendReply(player, "<color=#ce422b>/loot add <ID></color> - Create a new loot spawn");
                SendReply(player, "<color=#ce422b>/loot remove</color> - Removes the loot spawn you are looking at");
                SendReply(player, "<color=#ce422b>/loot save</color> - Save the edits you have made");
                SendReply(player, "<color=#ce422b>/loot cancel</color> - Cancel editing a zone");
                return;
            }
            
            switch (args[0].ToLower())
            {
                case "types":
                    {
                        int i = 0;
                        foreach (var lootType in spawnableTypes)
                        {
                            SendEchoConsole(player.net.connection, string.Format("ID: {0} - {1} - Skin: {2}", i, lootType.Key, lootType.Value));
                            i++;
                        }
                        SendReply(player, "Check your ingame console for a list of loot types");
                    }
                    break;                
                case "save":
                    {
                        ZoneEditor editor = player.GetComponent<ZoneEditor>();
                        if (editor == null)
                        {
                            SendReply(player, "You are not currently editing a zone");
                            return;
                        }

                        storedData.lootSpawns[editor.zoneId] = new List<StoredData.LootSpawn>();

                        foreach (BaseEntity entity in editor.zoneEntities)
                            storedData.lootSpawns[editor.zoneId].Add(new StoredData.LootSpawn(entity));

                        zoneEditors.Remove(editor);
                        UnityEngine.Object.Destroy(editor);
                        SaveData();
                        SendReply(player, "Zone loot has been saved!");
                    }
                    break;
                case "cancel":
                    {
                        ZoneEditor editor = player.GetComponent<ZoneEditor>();
                        if (editor == null)
                        {
                            SendReply(player, "You are not currently editing a zone");
                            return;
                        }

                        zoneEditors.Remove(editor);
                        UnityEngine.Object.Destroy(editor);                        
                        SendReply(player, "Zone editing has been cancelled!");
                    }
                    break;
                case "edit":
                    {
                        if (player.GetComponent<ZoneEditor>())
                        {
                            SendReply(player, "You are already editing a zone");
                            return;
                        }

                        if (args.Length != 2)
                        {
                            SendReply(player, "Invalid syntax! <color=#ce422b>/loot edit <zone ID></color>");
                            return;
                        }

                        if (!ZoneManager)
                        {
                            SendReply(player, "ZoneManager is not installed!");
                            return;
                        }

                        string zoneId = args[1];
                        object success = ZoneManager?.Call("CheckZoneID", zoneId);
                        if (success == null)
                        {
                            SendReply(player, "You have entered a invalid zone ID");
                            return;
                        }

                        ZoneEditor editor = player.gameObject.AddComponent<ZoneEditor>();
                        editor.zoneId = zoneId;
                        zoneEditors.Add(editor);
                        if (storedData.lootSpawns.ContainsKey(zoneId))
                        {
                            foreach(StoredData.LootSpawn lootSpawn in storedData.lootSpawns[zoneId])
                            {
                                BaseEntity entity = InstantiateEntity(lootSpawn.prefabName, lootSpawn.Position(), Quaternion.Euler(lootSpawn.Rotation()));
                                entity.enableSaving = false;
                                entity.skinID = lootSpawn.skinId;
                                if (entity is LootContainer)
                                {
                                    (entity as LootContainer).BlockPlayerItemInput = false;
                                    (entity as LootContainer).onlyAcceptCategory = ItemCategory.All;
                                }
                                entity.Spawn();

                                if (lootSpawn.lootItems.Length > 0 && entity.GetComponent<StorageContainer>())
                                {
                                    ClearContainer(entity);
                                    NextTick(() => FillContainer(lootSpawn.lootItems, entity.GetComponent<StorageContainer>()));
                                }
                                editor.zoneEntities.Add(entity);
                            }
                        }
                        SendReply(player, $"You are now editing the loot for zone: <color=#ce422b>{zoneId}</color>\nBe sure to disable culling whilst your are in edit mode by typing <color=#ce422b>culling.toggle false</color> in the ingame console!");
                    }
                    break;
                case "add":
                    {
                        ZoneEditor editor = player.GetComponent<ZoneEditor>();
                        if (editor == null)
                        {
                            SendReply(player, "You need to start editing a zone before you can add loot");
                            return;
                        }

                        if (args.Length != 2)
                        {
                            SendReply(player, "Invalid syntax! <color=#ce422b>/loot add <ID></color>");
                            return;
                        }

                        int lootId;
                        if (!int.TryParse(args[1], out lootId))
                        {
                            SendReply(player, "You must enter a loot ID number as shown in the ingame console");
                            return;
                        }

                        if (lootId < 0 || lootId > spawnableTypes.Count - 1)
                        {
                            SendReply(player, "You have entered a loot ID number that is out of range!");
                            return;
                        }

                        var spawnableType = spawnableTypes.ElementAt(lootId);
                        editor.PlaceNewEntity(spawnableType.Key, spawnableType.Value);
                    }
                    break;               
                case "remove":
                    {
                        ZoneEditor editor = player.GetComponent<ZoneEditor>();
                        if (editor == null)
                        {
                            SendReply(player, "You need to start editing a zone before you can remove loot");
                            return;
                        }

                        BaseEntity entity = FindEntityFromRay(player);
                        if (entity == null)
                        {
                            SendReply(player, "You need to look at the entity you want to remove from the loot spawns");
                            return;
                        }

                        editor.RemoveEntity(entity);                        
                    }
                    break;
                default:
                    break;
            }
        }

        private void SendEchoConsole(Network.Connection cn, string msg)
        {
            if (Network.Net.sv.IsConnected())
            {
                Network.Net.sv.write.Start();
                Network.Net.sv.write.PacketID(Network.Message.Type.ConsoleMessage);
                Network.Net.sv.write.String(msg);
                Network.Net.sv.write.Send(new Network.SendInfo(cn));
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData() => data.WriteObject(storedData);

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        private class StoredData
        {
            public Hash<string, List<LootSpawn>> lootSpawns = new Hash<string, List<LootSpawn>>();

            public class LootSpawn
            {
                public string prefabName;
                public ulong skinId;
                public float[] position;
                public float[] rotation;
                public ItemData[] lootItems = new ItemData[0];

                public LootSpawn() { }
                public LootSpawn(BaseEntity entity)
                {
                    prefabName = entity.PrefabName;
                    skinId = entity.skinID;
                    position = new float[] { entity.transform.position.x, entity.transform.position.y, entity.transform.position.z };
                    rotation = new float[] { entity.transform.eulerAngles.x, entity.transform.eulerAngles.y, entity.transform.eulerAngles.z };

                    if (entity.GetComponent<StorageContainer>())
                        lootItems = GetItems(entity.GetComponent<StorageContainer>().inventory).ToArray();
                }

                public Vector3 Position()
                {
                    return new Vector3(position[0], position[1], position[2]);
                }

                public Vector3 Rotation()
                {
                    return new Vector3(rotation[0], rotation[1], rotation[2]);
                }

                private IEnumerable<ItemData> GetItems(ItemContainer container)
                {
                    return container.itemList.Select(item => new ItemData
                    {
                        itemid = item.info.itemid,
                        amount = item.amount,
                        ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                        ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                        skin = item.skin,
                        condition = item.condition,
                        contents = item.contents?.itemList.Select(item1 => new ItemData
                        {
                            itemid = item1.info.itemid,
                            amount = item1.amount,
                            condition = item1.condition
                        }).ToArray()
                    });
                }

                public class ItemData
                {
                    public int itemid;
                    public ulong skin;
                    public int amount;
                    public float condition;
                    public int ammo;
                    public string ammotype;
                    public ItemData[] contents;                    
                }
            }            
        }
        #endregion
    }
}
