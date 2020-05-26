// Reference: System.Drawing
using Facepunch;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("TrophySigns", "k1lly0u", "0.2.1")]
    [Description("Collect human skulls and mount them to signs and spears in your base")]
    class TrophySigns : RustPlugin
    {
        #region Fields        
        private StoredData storedData;

        private DynamicConfigFile data;
                
        private bool wipeDetected;

        private Hash<Signage, DroppedItem> signRegisteredSkulls = new Hash<Signage, DroppedItem>();

        private Hash<DroppedItem, List<DroppedItem>> spearRegisteredSkulls = new Hash<DroppedItem, List<DroppedItem>>();

        private ImageStorage imageStorage;

        private static TrophySigns Instance { get; set; }

        private const int TARGET_LAYERS = (1 << 0 | 1 << 8 | 1 << 16 | 1 << 21 | 1 << 23 | 1 << 26);

        private const string BURLAPSACK_PREFAB = "assets/prefabs/misc/burlap sack/generic_world.prefab";

        private const int SKULL_ITEM_ID = 996293980;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission("trophysigns.use", this);
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("trophysigns_data");
        }

        private void OnServerInitialized()
        {
            Instance = this;

            imageStorage = new GameObject().AddComponent<ImageStorage>();

            LoadData();

            if (wipeDetected)
            {
                storedData = new StoredData();
                SaveData();
            }

            ServerMgr.Instance.StartCoroutine(FindRegisteredSignage(BaseNetworkable.serverEntities.Where(x => x is Signage).Cast<Signage>()));
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
            {
                ItemPlacement placement = entity.GetComponent<ItemPlacement>();
                if (placement != null)
                    placement.OnPlayerDeath();                
            }
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            Signage signage = networkable.GetComponent<Signage>();
            if (signage != null)
            {
                if (signRegisteredSkulls.ContainsKey(signage))
                    signRegisteredSkulls.Remove(signage);
            }
        }

        private void OnNewSave(string filename) => wipeDetected = true;

        private void OnServerSave() => SaveData();

        private void OnActiveItemChanged(BasePlayer player, Item olditem, Item item)
        {
            if (player == null || item == null || !permission.UserHasPermission(player.UserIDString, "trophysigns.use"))
                return;

            if (!CanPlace(player))
                return;

            if (item.info.itemid == SKULL_ITEM_ID && !player.GetComponent<ItemPlacement>())
                player.gameObject.AddComponent<ItemPlacement>();
        }

        private object OnItemPickup(Item item, BasePlayer player)
        {
            if (item != null && player != null)
            {
                ItemPlacement placement = player.GetComponent<ItemPlacement>();
                if (placement != null)
                    return false;
                else
                {
                    DroppedItem droppedItem = item.GetWorldEntity()?.GetComponent<DroppedItem>();

                    if (droppedItem == null)
                        return null;

                    List<DroppedItem> skulls;

                    if (spearRegisteredSkulls.TryGetValue(droppedItem, out skulls))
                    {
                        if (skulls?.Count == 0)
                        {
                            if (!CanRemove(player, "Error.NoBuildingAuthSpear"))
                                return false;

                            Pool.FreeList(ref skulls);

                            spearRegisteredSkulls.Remove(droppedItem);
                        }
                        else
                        {
                            SendReply(player, msg("Error.SkullsOnSpear", player.userID));
                            return false;
                        }
                    }

                    if (droppedItem.item.info.itemid == SKULL_ITEM_ID)
                    {
                        Signage signage = droppedItem.GetComponentInParent<Signage>();

                        if (signage != null && signRegisteredSkulls.ContainsKey(signage))
                        {
                            if (!CanRemove(player, "Error.NoBuildingAuthSkull"))
                                return false;

                            signRegisteredSkulls.Remove(signage);

                            if (signage.HasFlag(BaseEntity.Flags.Locked))
                                signage.SetFlag(BaseEntity.Flags.Locked, false);

                            UpdateSignImage(signage, "", true);
                        }
                        else
                        {
                            if (droppedItem.HasParent())
                            {
                                DroppedItem spear = droppedItem.GetParentEntity().GetComponent<DroppedItem>();
                                if (spear == null)
                                    return null;

                                if (spearRegisteredSkulls.TryGetValue(spear, out skulls) && skulls.Contains(droppedItem))
                                {
                                    if (!CanRemove(player, "Error.NoBuildingAuthSkull"))
                                        return false;

                                    spearRegisteredSkulls[spear].Remove(droppedItem);
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private void Unload()
        {
            if (!ServerMgr.Instance.Restarting)
                SaveData();

            ItemPlacement[] placementComps = UnityEngine.Object.FindObjectsOfType<ItemPlacement>();
            if (placementComps != null)
            {
                foreach (ItemPlacement placement in placementComps)
                    placement.CancelPlacement();
            }

            for (int i = signRegisteredSkulls.Count - 1; i >= 0; i--)
            {
                DroppedItem skullItem = signRegisteredSkulls.ElementAt(i).Value;
                skullItem?.DestroyItem();
                if (skullItem != null && !skullItem.IsDestroyed)
                    skullItem.Kill();
            }

            for (int i = 0; i < spearRegisteredSkulls.Count; i++)
            {
                KeyValuePair<DroppedItem, List<DroppedItem>> kvp = spearRegisteredSkulls.ElementAt(i);

                DroppedItem spear = kvp.Key;
                List<DroppedItem> skulls = kvp.Value;

                if (spear == null)
                    continue;

                for (int y = 0; y < skulls.Count; y++)
                {
                    DroppedItem skullItem = skulls[y];
                    if (skullItem != null)
                    {
                        skullItem.DestroyItem();
                        if (skullItem != null && !skullItem.IsDestroyed)
                            skullItem.Kill();
                    }
                }

                Pool.FreeList(ref skulls);

                spear.DestroyItem();

                if (!spear.IsDestroyed)
                    spear.Kill();
            }

            configData = null;
            Instance = null;
        }
        #endregion
      
        #region Initialization  
        private IEnumerator FindRegisteredSignage(IEnumerable<Signage> signage)
        {
            for (int i = 0; i < signage.Count(); i++)
            {
                Signage sign = signage.ElementAt(i);

                if (sign == null || sign.IsDestroyed || !storedData.data.ContainsKey(sign.net.ID))
                    continue;

                StoredData.TrophyData data = storedData.data[sign.net.ID];                
                                
                InitializeSign(sign, data.displayName, data.Position, data.Rotation);

                yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.2f));
            }

            yield return ServerMgr.Instance.StartCoroutine(CreateAndPopulateSpears());
        }

        private IEnumerator CreateAndPopulateSpears()
        {
            for (int i = 0; i < storedData.spearData.Count; i++)
            {
                StoredData.SpearData spearData = storedData.spearData[i];

                BaseEntity spear = CreateWorldObject(spearData.itemId, string.Empty, spearData.Position, true);  
                spear.GetComponent<Rigidbody>().isKinematic = true;
                spear.transform.rotation = Quaternion.Euler(spearData.Rotation);

                List<DroppedItem> skulls = Pool.GetList<DroppedItem>();

                for (int y = 0; y < spearData.trophyData.Count; y++)
                {
                    StoredData.TrophyData data = spearData.trophyData[y];

                    DroppedItem skullItem = CreateSkullItem(data.displayName, spear, data.Position, data.Rotation);

                    skulls.Add(skullItem);
                }

                spearRegisteredSkulls.Add(spear as DroppedItem, skulls);

                yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.2f));
            }
        }

        private void InitializeSign(Signage sign, string name, Vector3 localPosition, Vector3 localRotation)
        {              
            BaseEntity[] children = sign.children.Where(x => x.GetComponent<DroppedItem>())?.ToArray() ?? null;

            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    sign.RemoveChild(children[i]);
                    children[i].Kill();
                }
            }
          
            DroppedItem skullItem = CreateSkullItem(name, sign, localPosition, localRotation);

            UpdateSignImage(sign, name);

            signRegisteredSkulls.Add(sign, skullItem);
        }

        private void InitializeSpear(DroppedItem spear, string name, Vector3 localPosition, Vector3 localRotation)
        {      
            spear.CancelInvoke(spear.IdleDestroy);

            DroppedItem droppedItem = CreateSkullItem(name, spear, localPosition, localRotation);

            List<DroppedItem> skulls;
            if (!spearRegisteredSkulls.TryGetValue(spear, out skulls))
                skulls = spearRegisteredSkulls[spear] = Pool.GetList<DroppedItem>();

            skulls.Add(droppedItem);
        }
        #endregion

        #region Entity Creation
        private DroppedItem CreateSkullItem(string name, BaseEntity parent, Vector3 localPosition, Vector3 localRotation, bool canPickup = true)
        {
            DroppedItem skullItem = CreateSkullItem(name, parent.transform.position);

            skullItem.SetParent(parent);
            skullItem.transform.localPosition = localPosition;
            skullItem.transform.rotation = Quaternion.Euler(localRotation);

            return skullItem;
        }

        private DroppedItem CreateSkullItem(string name, Vector3 position, bool canPickup = true)
        {
            BaseEntity skullEntity = CreateWorldObject(SKULL_ITEM_ID, name, position, canPickup);

            UnityEngine.Object.Destroy(skullEntity.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(skullEntity.GetComponent<EntityCollisionMessage>());
            UnityEngine.Object.Destroy(skullEntity.GetComponent<PhysicsEffects>());

            return skullEntity as DroppedItem;
        }

        private BaseEntity CreateWorldObject(int itemId, string name, Vector3 pos, bool canPickup)
        {
            Item item = ItemManager.CreateByItemID(itemId);

            if (!string.IsNullOrEmpty(name))
                item.name = name;

            BaseEntity worldEntity = GameManager.server.CreateEntity(BURLAPSACK_PREFAB, pos);

            WorldItem worldItem = worldEntity as WorldItem;
            if (worldItem != null)
                worldItem.InitializeItem(item);

            worldEntity.Invoke(() =>
            {
                (worldEntity as DroppedItem).CancelInvoke((worldEntity as DroppedItem).IdleDestroy);                      
            }, 1f);
            
            worldItem.enableSaving = false;
            worldItem.allowPickup = canPickup;
            worldEntity.Spawn();

            item.SetWorldEntity(worldEntity);

            return worldEntity;
        }
        #endregion

        #region Functions

        private bool CanRemove(BasePlayer player, string key)
        {
            if (!configData.Placement.RemoveSkulls)
                return false;

            if (configData.Placement.RequirePrivilegeRemove && !player.IsBuildingAuthed())
            {
                SendReply(player, msg(key, player.userID));
                return false;
            }

            return true;
        }

        private static bool CanPlace(BasePlayer player)
        {
            if (configData.Placement.RequirePrivilegePlace && !player.IsBuildingAuthed())
                return false;

            return true;
        }
        #endregion

        #region Image Storage
        private void UpdateSignImage(Signage signage, string text, bool hideText = false)
        {
            if (!configData.Sign.GenerateImage)
                return;
            
            imageStorage.AddQueueItem(new ImageStorage.QueueItem(text, signage, hideText));            
        }        

        private class ImageStorage : MonoBehaviour
        {
            private Queue<QueueItem> queue;
            private WWW www;
            private QueueItem queueItem;
            private bool isBusy;

            private string backgroundColor;
            private string textColor;
            private int textSize;

            private void Awake()
            {
                queue = new Queue<QueueItem>();
                backgroundColor = configData.Sign.BackgroundColor;
                textColor = configData.Sign.TextColor;
                textSize = configData.Sign.TextSize;
            }

            private void OnDestroy()
            {
                www.Dispose();
                queue.Clear();
                Destroy(gameObject);
            }

            public void AddQueueItem(QueueItem queueItem)
            {
                queue.Enqueue(queueItem);
                if (!isBusy)
                    StartNextItem();
            }

            private void StartNextItem()
            {
                isBusy = true;

                queueItem = queue.Dequeue();

                if (queueItem.signage == null || queueItem.signage.IsDestroyed)
                {
                    isBusy = false;
                    StartNextItem();
                    return;
                }

                StartCoroutine(DownloadImage());
            }
            
            private IEnumerator DownloadImage()
            {
                ImageSize imageSize;
                if (!ImageSize.Sizes.TryGetValue(queueItem.signage.ShortPrefabName, out imageSize))
                {
                    print($"[ERROR] TrophySigns: No sign sizes set for {queueItem.signage.ShortPrefabName}, please report this in the plugin support thread");

                    isBusy = false;
                    if (queue.Count > 0)
                        StartNextItem();
                    yield break;
                }

                float scale = (float)imageSize.Width / (float)600;

                int targetFontSize = (int)(textSize * scale);

                string imageUrl = $"https://dummyimage.com/{imageSize.Width}x{imageSize.Height}/{backgroundColor}/{(queueItem.hideText ? backgroundColor : textColor)}.png/&text={queueItem.text.Replace(" ", "+")}";

                www = new WWW(imageUrl);

                yield return new WaitWhile(() => !www.isDone);

                byte[] imageBytes = www.texture.EncodeToPNG();

                if (imageSize.Width != imageSize.ImageWidth || imageSize.Height != imageSize.ImageHeight)
                    imageBytes = ResizeImage(imageBytes, imageSize);

                queueItem.signage.textureID = FileStorage.server.Store(imageBytes, FileStorage.Type.png, queueItem.signage.net.ID, 0U);
                queueItem.signage.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                if (!queueItem.signage.HasFlag(BaseEntity.Flags.Locked))
                    queueItem.signage.SetFlag(BaseEntity.Flags.Locked, true);

                isBusy = false;
                www.Dispose();

                if (queue.Count > 0)
                    StartNextItem();
            }

            private byte[] ResizeImage(byte[] bytes, ImageSize imageSize)
            {
                byte[] resizedImageBytes;
                using (MemoryStream originalBytesStream = new MemoryStream(), resizedBytesStream = new MemoryStream())
                {
                    originalBytesStream.Write(bytes, 0, bytes.Length);
                    Bitmap image = new Bitmap(originalBytesStream);

                    Bitmap resizedImage = new Bitmap(imageSize.Width, imageSize.Height);

                    using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(resizedImage))
                    {
                        graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                        graphics.DrawImage(image, new Rectangle(0, 0, imageSize.ImageWidth, imageSize.ImageHeight));
                    }

                    resizedImage.Save(resizedBytesStream, ImageFormat.Png);
                    resizedImageBytes = resizedBytesStream.ToArray();
                }
                return resizedImageBytes;
            }

            public class QueueItem
            {
                public string text;
                public bool hideText;
                public Signage signage;

                public QueueItem() { }
                public QueueItem(string text, Signage signage, bool hideText)
                {
                    this.text = text;
                    this.signage = signage;
                    this.hideText = hideText;
                }
            }                       

            private class ImageSize
            {
                public int Width { get; }
                public int Height { get; }
                public int ImageWidth { get; }
                public int ImageHeight { get; }

                public ImageSize(int width, int height) : this(width, height, width, height) { }
                public ImageSize(int width, int height, int imageWidth, int imageHeight)
                {
                    Width = width;
                    Height = height;
                    ImageWidth = imageWidth;
                    ImageHeight = imageHeight;
                }

                public static Dictionary<string, ImageSize> Sizes = new Dictionary<string, ImageSize>
                {
                    ["sign.pictureframe.landscape"] = new ImageSize(256, 128),
                    ["sign.pictureframe.tall"] = new ImageSize(128, 512),
                    ["sign.pictureframe.portrait"] = new ImageSize(128, 256),
                    ["sign.pictureframe.xxl"] = new ImageSize(1024, 512),
                    ["sign.pictureframe.xl"] = new ImageSize(512, 512),
                    ["sign.small.wood"] = new ImageSize(128, 64),
                    ["sign.huge.wood"] = new ImageSize(512, 128),
                    ["sign.medium.wood"] = new ImageSize(256, 128),
                    ["sign.large.wood"] = new ImageSize(256, 128),
                    ["sign.hanging.banner.large"] = new ImageSize(64, 256),
                    ["sign.pole.banner.large"] = new ImageSize(64, 256),
                    ["sign.post.single"] = new ImageSize(128, 64),
                    ["sign.post.double"] = new ImageSize(256, 256),
                    ["sign.post.town"] = new ImageSize(256, 128),
                    ["sign.post.town.roof"] = new ImageSize(256, 128),
                    ["sign.hanging"] = new ImageSize(128, 256),
                    ["sign.hanging.ornate"] = new ImageSize(256, 128),
                    ["spinner.wheel.deployed"] = new ImageSize(512, 512, 285, 285),
                };
            }
        }
        #endregion

        #region Component
        private class ItemPlacement : MonoBehaviour
        {
            private BasePlayer player;

            private DroppedItem skullItem;

            private BaseEntity hitEntity = null;

            private bool isValidPlacement;

            private float placementDistance = 3f;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;

                SpawnDroppedItem(player.GetActiveItem()?.name);
                player.ChatMessage(msg("Help.Placement.1", player.userID));
            }

            private void Update()
            {
                Item activeItem = player.GetActiveItem();
                if (activeItem == null || activeItem.info.itemid != SKULL_ITEM_ID)
                    CancelPlacement();

                isValidPlacement = false;

                InputState input = player.serverInput;

                RaycastHit hit;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, placementDistance, TARGET_LAYERS))
                {
                    skullItem.transform.position = hit.point + (-skullItem.transform.up * 0.1f);
                    skullItem.transform.rotation = Quaternion.LookRotation(player.eyes.position - skullItem.transform.position, Vector3.up) * Quaternion.Euler(270, 0, 0);

                    hitEntity = hit.GetEntity();

                    isValidPlacement = hitEntity is Signage || ((hitEntity is DroppedItem) && (hitEntity as DroppedItem).item.info.shortname.Contains("spear."));
                }
                else
                {
                    skullItem.transform.position = player.eyes.HeadRay().GetPoint(2);
                    skullItem.transform.rotation = Quaternion.LookRotation(player.eyes.position - skullItem.transform.position, Vector3.up) * Quaternion.Euler(270, 0, 0);
                }

                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    if (!isValidPlacement)
                    {
                        player.ChatMessage(msg("Help.Placement.2", player.userID));
                        return;
                    }
                    else
                    {
                        if (hitEntity is Signage)
                        {
                            if (Instance.signRegisteredSkulls.ContainsKey(hitEntity as Signage))
                                player.ChatMessage(msg("Help.Placement.5", player.userID));
                            else PlaceSkull(activeItem);
                        }
                        else if (hitEntity is DroppedItem)
                        {
                            if (hitEntity.GetComponent<Rigidbody>()?.isKinematic ?? false)
                                PlaceSkull(activeItem);
                        }
                    }
                }
                else if (input.WasJustPressed(BUTTON.FIRE_SECONDARY))
                    CancelPlacement();
            }

            private void SpawnDroppedItem(string name)
            {
                skullItem = Instance.CreateSkullItem(name, player.transform.position, false);
                skullItem.item.GetWorldEntity().gameObject.SetLayerRecursive(28);
                enabled = true;
            }

            public void CancelPlacement(bool notify = true)
            {
                enabled = false;
                skullItem.DestroyItem();
                skullItem.Kill();
                if (notify)
                    player.ChatMessage(msg("Help.Placement.3", player.userID));
                Destroy(this);
            }

            private void PlaceSkull(Item activeItem)
            {
                if (!CanPlace(player))
                {
                    player.ChatMessage(msg("Error.NoBuildingAuthPlacement", player.userID));
                    CancelPlacement(false);
                    return;
                }

                player.ChatMessage(msg("Help.Placement.4", player.userID));

                activeItem.MarkDirty();
                activeItem.RemoveFromContainer();

                string name = string.IsNullOrEmpty(skullItem.item.name) ? player.displayName : skullItem.item.name.Replace("Skull of ", "").Replace("\"", "");
                name = string.Format(configData.Sign.SignFormat, name);

                if (hitEntity is Signage)                
                    Instance.InitializeSign(hitEntity as Signage, name, hitEntity.transform.InverseTransformPoint(skullItem.transform.position), skullItem.transform.eulerAngles);                
                else Instance.InitializeSpear(hitEntity as DroppedItem, name, hitEntity.transform.InverseTransformPoint(skullItem.transform.position), skullItem.transform.eulerAngles);

                skullItem.DestroyItem();
                skullItem.Kill();

                Destroy(this);
            }

            public void OnPlayerDeath()
            {
                CancelPlacement();
                Destroy(this);
            }
        }
        #endregion

        #region Config        
        private static ConfigData configData;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "Placement Options")]
            public PlacementOptions Placement { get; set; }

            [JsonProperty(PropertyName = "Sign Options")]
            public SignOptions Sign { get; set; }

            public class SignOptions
            {
                [JsonProperty(PropertyName = "Auto-generate sign image using skull owner")]
                public bool GenerateImage { get; set; }

                [JsonProperty(PropertyName = "Image background color (hex without the #)")]
                public string BackgroundColor { get; set; }

                [JsonProperty(PropertyName = "Text color (hex without the #)")]
                public string TextColor { get; set; }

                [JsonProperty(PropertyName = "Sign text format")]
                public string SignFormat { get; set; }

                [JsonProperty(PropertyName = "Text size")]
                public int TextSize { get; set; }
            }

            public class PlacementOptions
            {
                [JsonProperty(PropertyName = "Allow skulls and spears to be removed")]
                public bool RemoveSkulls { get; set; }

                [JsonProperty(PropertyName = "Require building privilege to remove skulls and spears")]
                public bool RequirePrivilegeRemove { get; set; }

                [JsonProperty(PropertyName = "Require building privilege to place skulls and spears")]
                public bool RequirePrivilegePlace { get; set; }

            }
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
                Placement = new ConfigData.PlacementOptions
                {
                    RemoveSkulls = true,
                    RequirePrivilegePlace = true,
                    RequirePrivilegeRemove = true,
                },
                Sign = new ConfigData.SignOptions
                {
                    BackgroundColor = "282828",
                    GenerateImage = true,
                    TextColor = "cccccc",
                    TextSize = 60,
                    SignFormat = "Skull of \"{0}\""
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 1, 05))
                configData.Sign.TextSize = baseConfig.Sign.TextSize;

            if (configData.Version < new VersionNumber(0, 1, 10))
                configData.Sign.SignFormat = baseConfig.Sign.SignFormat;

            if (configData.Version < new VersionNumber(0, 2, 0))
                configData = baseConfig;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData()
        {
            storedData.data.Clear();
            storedData.spearData.Clear();

            foreach (KeyValuePair<Signage, DroppedItem> kvp in signRegisteredSkulls)
            {
                if (kvp.Key == null || kvp.Key.IsDestroyed || kvp.Value == null || kvp.Value.IsDestroyed)
                    continue;

                storedData.data.Add(kvp.Key.net.ID, new StoredData.TrophyData(kvp.Value));
            }

            foreach (KeyValuePair<DroppedItem, List<DroppedItem>> kvp in spearRegisteredSkulls)
            {
                if (kvp.Key == null || kvp.Key.IsDestroyed || kvp.Value == null || kvp.Value.Count == 0)
                    continue;

                storedData.spearData.Add(new StoredData.SpearData(kvp.Key, kvp.Value.Select(x => new StoredData.TrophyData(x)).ToList()));
            }

            data.WriteObject(storedData);
        }

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
            public Dictionary<uint, TrophyData> data = new Dictionary<uint, TrophyData>();

            public List<SpearData> spearData = new List<SpearData>();

            public class SpearData : PositionData
            {               
                public int itemId;

                public List<TrophyData> trophyData;

                public SpearData() { }

                public SpearData(DroppedItem item, List<TrophyData> trophyData) : base(item)
                {                   
                    itemId = item.item.info.itemid;
                    this.trophyData = trophyData;
                }
            }

            public class TrophyData : PositionData
            {                
                public string displayName;

                public TrophyData() { }

                public TrophyData(DroppedItem item) : base(item)
                {                   
                    displayName = item.item.name;
                }
            }

            public class PositionData
            {
                public float[] position;
                public float[] rotation;

                public PositionData() { }

                public PositionData(DroppedItem item)
                {
                    position = new float[] { item.transform.localPosition.x, item.transform.localPosition.y, item.transform.localPosition.z };
                    rotation = new float[] { item.transform.eulerAngles.x, item.transform.eulerAngles.y, item.transform.eulerAngles.z };
                }

                [JsonIgnore]
                public Vector3 Position
                {
                    get
                    {
                        return new Vector3(position[0], position[1], position[2]);
                    }
                }

                [JsonIgnore]
                public Vector3 Rotation
                {
                    get
                    {
                        return new Vector3(rotation[0], rotation[1], rotation[2]);
                    }
                }
            }
        }
        #endregion

        #region Localization
        private static string msg(string key, ulong playerId = 0U) => Instance.lang.GetMessage(key, Instance, playerId == 0U ? null : playerId.ToString());

        private Dictionary<string, string> Messages = new Dictionary<string, string>
        {            
            ["Help.Placement.1"] = "<color=#939393>Use the <color=#ce422b>fire</color> button to place this skull on any sign or spear</color>",
            ["Help.Placement.2"] = "<color=#939393>Skulls can only be placed on signs or spears</color>",
            ["Help.Placement.3"] = "<color=#ce422b>Skull placement cancelled!</color>",
            ["Help.Placement.4"] = "<color=#ce422b>Skull placed!</color>",
            ["Help.Placement.5"] = "<color=#939393>This sign already has a skull on it!</color>",
            ["Error.NoBuildingAuthSkull"] = "<color=#939393>You need building auth to remove skulls</color>",
            ["Error.NoBuildingAuthSpear"] = "<color=#939393>You need building auth to remove spears</color>",
            ["Error.SkullsOnSpear"] = "<color=#939393>You need to remove the skulls before you can remove the spear</color>",
            ["Error.NoBuildingAuthPlacement"] = "<color=#939393>You need building auth to place skulls</color>",
        };
        #endregion
    }
}
