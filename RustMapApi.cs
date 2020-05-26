using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Facepunch.Utility;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using UnityEngine.Networking;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("Rust Map Api", "MJSU", "1.0.0")]
    [Description("An API to generate the rust server map image")]
    internal class RustMapApi : RustPlugin
    {
        #region Class Fields
        private PluginConfig _pluginConfig; //Plugin Config
        private StoredData _storedData;
        
        private TerrainTexturing _terrainTexture;
        private Terrain _terrain;
        private TerrainHeightMap _heightMap;
        private TerrainSplatMap _splatMap;
        
        private readonly Hash<string, Array2D<Color>> _renders = new Hash<string, Array2D<Color>>();
        private readonly Hash<string, Hash<string, Hash<string, Hash<string, object>>>> _imageCache = new Hash<string, Hash<string, Hash<string, Hash<string, object>>>>();
        private List<Hash<string, object>> _iconOverlay;
        
        private enum EncodingMode {Jpg = 1, Png = 2}

        private bool _isReady;

        private Coroutine _storeImageRoutine;
        private readonly Queue<StorageInfo> _storageQueue = new Queue<StorageInfo>();
        
        private const string DefaultMapName = "Default";
        private const string IconMapName = "Icons";
        #endregion

        #region Setup & Loading

        private void Init()
        {
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.StartingSplits = config.StartingSplits ?? new List<string>
            {
                "2x2", "3x3", "4x4"
            };

            config.IconSettings = config.IconSettings ?? new Hash<string, IconConfig>
            {
                ["Harbor"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/ND4c70v.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Giant Excavator Pit"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/hmUKFwS.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Junkyard"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/V8D4ZGc.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Launch Site"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/gjdynsc.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Water Treatment Plant"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/5L2Gdag.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Military Tunnel"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/6RwXvC2.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Airfield"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/KhQXhIs.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Power Plant"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/ZxqiBc6.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Train Yard"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/wVifXqr.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Outpost"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/hb7JZ9i.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Bandit Camp"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/cIR4YOt.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Sewer Branch"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/PbKZQdZ.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["HQM Quarry"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/bGFogbM.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Satellite Dish"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/XwSpCJY.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["The Dome"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/mPRgBF2.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Sulfur Quarry"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/bGFogbM.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Stone Quarry"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/bGFogbM.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Power Sub Station"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/LQUknms.png",
                    Width = 60,
                    Height = 60,
                    Show = false
                },
                ["Water Well"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/TASWRD0.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Abandoned Cabins"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/xigwDcW.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Wild Swamp"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/2tcTYKA.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Abandoned Supermarket"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/ZyP2W9F.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Mining Outpost"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/C0acqvj.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Oxum's Gas Station"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/oW1bDdF.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Cave"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/ByKJj9C.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Lighthouse"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/r5vbzhm.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Large Oil Rig"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/AAhZO7k.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
                ["Oil Rig"] = new IconConfig
                {
                    ImageUrl = "https://i.imgur.com/AAhZO7k.png",
                    Width = 60,
                    Height = 60,
                    Show = true
                },
            };

            return config;
        }

        private void OnServerInitialized()
        {
            _terrainTexture = TerrainTexturing.Instance;
            if (_terrainTexture == null)
            {
                return;
            }
                
            _terrain = _terrainTexture.GetComponent<Terrain>();
            _heightMap = _terrainTexture.GetComponent<TerrainHeightMap>();
            if (_heightMap == null)
            {
                return;
            }
            
            _splatMap = _terrainTexture.GetComponent<TerrainSplatMap>();
            if (_splatMap == null)
            {
                return;
            }

            InvokeHandler.Instance.StartCoroutine(CreateStartupImages());
        }
        
        private IEnumerator CreateStartupImages()
        {
            foreach (IconConfig iconSetting in _pluginConfig.IconSettings.Values)
            {
                if (!iconSetting.Show || string.IsNullOrEmpty(iconSetting.ImageUrl))
                {
                    continue;
                }
                
                yield return LoadIcon(iconSetting);
                yield return null;
            }
            
            Puts("Loaded image icons");
            
            Stopwatch sw = Stopwatch.StartNew();
            
            Array2D<Color> render = Render(new ImageConfig());
            sw.Stop();
            if (render.IsEmpty())
            {
                PrintError("Failed to generate map render");
                yield break;
            }

            _renders[DefaultMapName] = render;
            Puts($"Map Render Took: {GetDuration(sw.ElapsedMilliseconds)}");
            
            yield return new WaitForSeconds(.1f);
            
            sw.Restart();
            _iconOverlay = BuildIconMonuments();
            render = RenderOverlay(render, _iconOverlay.Select(o => new OverlayConfig(o)).ToList());
            sw.Stop();
            
            if (render.IsEmpty())
            {
                PrintError("Failed to generate icon render");
                yield break;
            }

            _renders[IconMapName] = render;
            
            Puts($"Icon Render Took: {GetDuration(sw.ElapsedMilliseconds)}");
            
            yield return new WaitForSeconds(.1f);

            if (!HasSplit(DefaultMapName, 1, 1))
            { 
                SaveSingleImage(DefaultMapName);
                yield return new WaitForSeconds(.1f);
            }

            if (!HasSplit(IconMapName, 1, 1))
            { 
                SaveSingleImage(IconMapName);
                yield return new WaitForSeconds(.1f);
            }

            foreach (string splitText in _pluginConfig.StartingSplits)
            {
                if (!splitText.ToLower().Contains("x"))
                {
                    PrintError($"split {splitText} does not contain an x");
                    continue;
                }

                string[] splits = splitText.Split('x', 'X');
                if (splits.Length < 2)
                {
                    PrintError($"split {splitText} is not valid. Format should be 2x2 for 2 rows x 2 columns");
                    continue;
                }

                int row;
                if (!int.TryParse(splits[0], out row))
                {
                    PrintError($"Row of {splits[0]} is not a valid number");
                    continue;
                }
                
                int col;
                if (!int.TryParse(splits[1], out col))
                {
                    PrintError($"Column of {splits[1]} is not a valid number");
                    continue;
                }
                
                if (!HasSplit(DefaultMapName, row, col))
                {
                    SaveSplitImage(DefaultMapName, row, col);
                    yield return new WaitForSeconds(.1f);
                }

                if (!HasSplit(IconMapName, row, col))
                {
                    SaveSplitImage(IconMapName, row, col);
                    yield return new WaitForSeconds(.1f);
                }
            }
                
            yield return new WaitForSeconds(1f);
            _isReady = true;
            Interface.Call("OnRustMapApiReady");
        }

        private void OnNewSave(string filename)
        {
            _storedData = new StoredData();
        }

        private void Unload()
        {
            SaveData();
        }
        #endregion

        #region Console Command
        [ConsoleCommand("rma_regenerate")]
        private void RegenerateConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
            {
                return;
            }

            InvokeHandler.Instance.StartCoroutine(HandleRegenerate(arg));
        }

        private IEnumerator HandleRegenerate(ConsoleSystem.Arg arg)
        {
            Puts("Removing maps from file storage");   
            foreach (Hash<string, Hash<string, uint>> maps in _storedData.MapIds.Values)
            {
                foreach (Hash<string,uint> split in maps.Values)
                {
                    foreach (uint section in split.Values)
                    {
                        FileStorage.server.Remove(section, FileStorage.Type.jpg, CommunityEntity.ServerInstance.net.ID);
                        yield return null;
                    }
                }
            }

            Puts("Removing Icons from file storage");   
            foreach (uint icon in _storedData.IconIds.Values)
            {
                FileStorage.server.Remove(icon, FileStorage.Type.jpg, CommunityEntity.ServerInstance.net.ID);
                yield return null;
            }
            
            Puts("Wiping stored data");   
            _storedData = new StoredData();

            Puts("Regenerating images");
            yield return CreateStartupImages();
        }
        #endregion

        #region API

        private bool IsReady()
        {
            return _isReady;
        }
        
        private Hash<string, object> CreateRender(string mapName, Hash<string, object> config)
        {
            if (!_isReady)
            {
                return null;
            }
            
            ImageConfig imageConfig = new ImageConfig(config);
            Array2D<Color> render = Render(imageConfig);
            _renders[mapName] = render;
            return RenderToHash(render);
        }

        private Hash<string, object> CreateRenderOverlay(string renderSource, string newMapName, List<Hash<string, object>> overlay)
        {
            if (!_isReady)
            {
                return null;
            }

            Array2D<Color> render = _renders[renderSource];
            if (render.IsEmpty())
            {
                return null;
            }
            
            Array2D<Color> overlayRender = RenderOverlay(render, overlay.Select(o => new OverlayConfig(o)).ToList());

            _renders[newMapName] = overlayRender;
            return RenderToHash(overlayRender);
        }
        
        private Hash<string, object> GetRender(string mapName)
        {
            if (!_renders.ContainsKey(mapName))
            {
                return null;
            }

            return RenderToHash(_renders[mapName]);
        }
        
        private Hash<string, Hash<string, object>> CreateSingle(string mapName, int encodingMode)
        {
            if (!_renders.ContainsKey(mapName))
            {
                return null;
            }
            
            EncodingMode mode = (EncodingMode) encodingMode;
            Array2D<Color> render = _renders[mapName];
            
            return new Hash<string, Hash<string, object>>
            {
                [GetIndex(0,0)] = CreateMapData(render, mode)
            };
        }
        
        private Hash<string, Hash<string,object>> CreateSplice(string mapName, int numRows, int numCols, int encodingMode)
        {
            if (!_renders.ContainsKey(mapName))
            {
                return null;
            }
            
            Array2D<Color> render = _renders[mapName];
            EncodingMode mode = (EncodingMode) encodingMode;
            Hash<string, Hash<string,object>> splice = new Hash<string, Hash<string, object>>();

            int rowSize = render.Height / numRows;
            int colSize = render.Width / numCols;
            for (int x = 0; x < numRows; x++)
            {
                for (int y = 0; y < numCols; y++)
                {
                    Array2D<Color> splicedColors = render.Splice(y * colSize, x * rowSize, colSize, rowSize);
                    splice[GetIndex(x,y)] = CreateMapData(splicedColors, mode);
                }
            }

            return splice;
        }
        
        private Hash<string, Hash<string, object>> SaveSingleImage(string mapName)
        {
            if (!_renders.ContainsKey(mapName))
            {
                return null;
            }

            Stopwatch sw = Stopwatch.StartNew();
            Hash<string, Hash<string, object>> single = CreateSingle(mapName, (int) EncodingMode.Jpg);
            sw.Stop();
            
            Puts($"{mapName} Encoding Took: {GetDuration(sw.ElapsedMilliseconds)}");
            
            SaveCache(mapName, GetIndex(1, 1), single);
            SaveSplit(mapName, GetIndex(1, 1), single);
            return single;
        }

        private Hash<string, Hash<string, object>> SaveSplitImage(string mapName, int numRows, int numCols)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Hash<string, Hash<string, object>> split = CreateSplice(mapName, numRows, numCols, (int) EncodingMode.Jpg);
            sw.Stop();
            
            string index = GetIndex(numRows, numCols);
            Puts($"{mapName} {index} Split Took: {GetDuration(sw.ElapsedMilliseconds)}");

            SaveCache(mapName, index, split);
            SaveSplit(mapName, index, split);
            return split;
        }
        
        private List<string> GetRenderNames()
        {
            return _renders.Keys.ToList();
        }
        
        private List<string> GetSavedSplits(string mapName)
        {
            return _storedData.GetSavedSplits(mapName);
        }
        
        private Hash<string, object> GetFullMap(string mapName)
        {
            return GetSection(mapName, 1, 1, 0, 0);
        }

        private List<Hash<string, object>> GetIconOverlay()
        {
            return _iconOverlay;
        }

        private bool HasSplit(string mapName, int numRows, int numCols)
        {
            Hash<string, uint> split = _storedData.MapIds[mapName]?[GetIndex(numRows, numCols)];
            if (split != null)
            {
                return split.Count == numRows * numCols;
            }

            return false;
        }

        private Hash<string, object> GetSection(string mapName, int numRows, int numCols, int row, int col)
        {
            if (numRows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numRows), "numRows cannot be less <= 0!");
            }
            
            if (numCols <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numCols), "numCols cannot be less <= 0!");
            }
            
            if (row < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(row), "Row cannot be less < 0!");
            }
            
            if (col < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(col), "Col cannot be less < 0!");
            }

            if (row >= numRows)
            {
                throw new ArgumentOutOfRangeException(nameof(numRows), "row cannot be >= numRows");
            }
            
            if (col >= numCols)
            {
                throw new ArgumentOutOfRangeException(nameof(numRows), "col cannot be >= numCols");
            }
            
            string splitIndex = GetIndex(numRows, numCols);
            string sectionIndex = GetIndex(row, col);
            Hash<string, object> cacheSection = GetCacheSection(mapName, splitIndex, sectionIndex);
            if (cacheSection != null)
            {
                return cacheSection;
            }
            
            if (HasSplit(mapName, numRows, numCols))
            {
                Hash<string, object> section = LoadSection(mapName, splitIndex, sectionIndex);
                if (section != null)
                {
                    return section;
                }
            }

            if (numRows == 1 && numCols == 1)
            {
                return SaveSingleImage(mapName)[sectionIndex];
            }

            return SaveSplitImage(mapName, numRows, numCols)[sectionIndex];
        }

        private Hash<string, Hash<string, object>> GetSplit(string mapName, int numRows, int numCols)
        {
            if (numRows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numRows), "numRows cannot be less <= 0!");
            }
            
            if (numCols <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numCols), "numCols cannot be less <= 0!");
            }
            
            string splitIndex = GetIndex(numRows, numCols);
            Hash<string, Hash<string, object>> cacheSplit = GetCacheSplit(mapName, splitIndex);
            if (cacheSplit != null)
            {
                return cacheSplit;
            }
            
            if (HasSplit(mapName, numRows, numCols))
            {
                Hash<string, Hash<string, object>> split = new Hash<string, Hash<string, object>>();
                foreach (KeyValuePair<string,uint> pair in _storedData.MapIds[mapName][splitIndex])
                {
                    split[pair.Key] = LoadSection(mapName, splitIndex, pair.Key);
                }

                return split;
            }

            if (numRows == 1 && numCols == 1)
            {
                return SaveSingleImage(mapName);
            }

            return SaveSplitImage(mapName, numRows, numCols);
        }
        #endregion

        #region Icon Handling

        private List<Hash<string, object>> BuildIconMonuments()
        {
            float half = World.Size / 2.0f;
            float scale = (float)_terrain.terrainData.alphamapResolution / World.Size;
            List<Hash<string, object>> overlays = new  List<Hash<string, object>>();
            if (TerrainMeta.Path != null && TerrainMeta.Path.Monuments != null)
            {
                foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
                {
                    string name = GetMonumentName(monument);

                    IconConfig config = _pluginConfig.IconSettings[name];
                    if (config == null)
                    {
                        _pluginConfig.IconSettings[name] = new IconConfig
                        {
                            Height = 90,
                            Width = 90,
                            Show = false,
                            ImageUrl = string.Empty
                        };
                        Config.WriteObject(_pluginConfig);
                        continue;
                    }

                    if (!config.Show)
                    {
                        continue;
                    }
                    
                    float x = (monument.transform.position.z + half) * scale;
                    float z = (monument.transform.position.x + half) * scale;

                    overlays.Add(new Hash<string, object>
                    {
                        [nameof(OverlayConfig.Height)] = config.Height,
                        [nameof(OverlayConfig.Width)] = config.Width,
                        [nameof(OverlayConfig.Image)] = config.Image,
                        [nameof(OverlayConfig.XPos)] = (int) x,
                        [nameof(OverlayConfig.YPos)] = (int) z,
                        [nameof(OverlayConfig.DebugName)] = name
                    });
                }
            }
            
            return overlays;
        }

        private string GetMonumentName(MonumentInfo monument)
        {
            string name = monument.displayPhrase.english.Replace("\n", "");
            if (string.IsNullOrEmpty(name))
            {
                if (monument.Type == MonumentType.Cave)
                {
                    name = "Cave";
                }
                else if(monument.name.Contains("power_sub"))
                {
                    name = "Power Sub Station";
                }
                else
                {
                    name = monument.name;
                }
            }

            return name;
        }

        #endregion
        
        #region Storage Handling
        private Hash<string, object> LoadSection(string mapName, string split, string section)
        {
            Hash<string, object> cache = GetCacheSection(mapName, split, section);
            if (cache != null)
            {
                return cache;
            }

            uint? imageId = _storedData.MapIds[mapName]?[split]?[section];
            if (imageId == null)
            {
                return null;
            }

            byte[] data = LoadImage(imageId.Value, FileStorage.Type.jpg);

            Hash<string, object> imageData = ImageDataFromBytes(data);
            SaveCache(mapName, split, section, imageData);

            return imageData;
        }
        
        private void SaveSplit(string mapName, string split, Hash<string, Hash<string, object>> splitData)
        {
            _storageQueue.Enqueue(new StorageInfo
            {
                MapName = mapName,
                Split = split,
                SplitData = splitData
            });
            
            if (_storeImageRoutine == null)
            {
                _storeImageRoutine = InvokeHandler.Instance.StartCoroutine(HandleSave());
            }
        }

        private IEnumerator HandleSave()
        {
            while (_storageQueue.Count > 0)
            {
                StorageInfo next = _storageQueue.Dequeue();
                foreach (KeyValuePair<string,Hash<string,object>> data in next.SplitData)
                {
                    StoreSection(next.MapName, next.Split, data.Key, data.Value);
                    yield return null;
                }
            }

            if (_isReady)
            {
                SaveData();
            }

            _storeImageRoutine = null;
        }
        
        private void StoreSection(string mapName, string split, string section, Hash<string, object> imageData)
        {
            Hash<string, Hash<string, uint>> map = _storedData.MapIds[mapName];
            if (map == null)
            {
                map = new Hash<string, Hash<string, uint>>();
                _storedData.MapIds[mapName] = map;
            }
            
            byte[] storageBytes = BytesFromImageData(imageData);
            Hash<string, uint> splitData = map[split];
            if (splitData == null)
            {
                splitData = new Hash<string, uint>();
                map[split] = splitData;
            }

            splitData[section] = StoreImage(storageBytes, FileStorage.Type.jpg);
        }

        private Hash<string, Hash<string, object>> GetCacheSplit(string mapName, string split)
        {
            return _imageCache[mapName]?[split];
        }
        
        private Hash<string, object> GetCacheSection(string mapName, string split, string section)
        {
            return GetCacheSplit(mapName,split)?[section];
        }

        private void SaveCache(string mapName, string split, Hash<string, Hash<string, object>> data)
        {
            Hash<string, Hash<string, Hash<string, object>>> map = _imageCache[mapName];
            if (map == null)
            {
                map = new Hash<string, Hash<string, Hash<string, object>>>();
                _imageCache[mapName] = map;
            }

            map[split] = data;
        }
        
        private void SaveCache(string mapName, string split, string section,  Hash<string, object> data)
        {
            Hash<string, Hash<string, Hash<string, object>>> map = _imageCache[mapName];
            if (map == null)
            {
                map = new Hash<string, Hash<string, Hash<string, object>>>();
                _imageCache[mapName] = map;
            }

            Hash<string, Hash<string, object>> cache = map[split];
            if (cache == null)
            {
                cache = new Hash<string, Hash<string, object>>();
                map[split] = cache;
            }

            cache[section] = data;
        }

        private uint StoreImage(byte[] bytes, FileStorage.Type type)
        {
            return FileStorage.server.Store(Compression.Compress(bytes), type, CommunityEntity.ServerInstance.net.ID);
        }

        private byte[] LoadImage(uint id, FileStorage.Type type)
        {
            return Compression.Uncompress(FileStorage.server.Get(id, type, CommunityEntity.ServerInstance.net.ID));
        }

        private Hash<string, object> ImageDataFromBytes(byte[] bytes)
        {
            return new Hash<string, object>
            {
                ["width"] = BitConverter.ToInt32(bytes, 0),
                ["height"] = BitConverter.ToInt32(bytes, 4),
                ["image"] = bytes.Skip(8).ToArray()
            };
        }
        
        private byte[] BytesFromImageData(Hash<string, object> data)
        {
            byte[] width = BitConverter.GetBytes((int) data["width"]);
            byte[] height = BitConverter.GetBytes((int) data["height"]);
            byte[] image = (byte[]) data["image"];
            
            byte[] bytes = new byte[width.Length + height.Length + image.Length];
            Array.Copy(width, 0, bytes, 0, width.Length);
            Array.Copy(height, 0, bytes, width.Length, height.Length);
            Array.Copy(image, 0, bytes, width.Length + height.Length, image.Length);
            return bytes;
        }
        #endregion

        #region Helper Methods
        private Hash<string, object> RenderToHash(Array2D<Color> colors)
        {
            return new Hash<string, object>
            {
                ["colors"] = colors.Items,
                ["width"] = colors.Width,
                ["height"] = colors.Height
            };
        }

        private Hash<string, object> CreateMapData(Array2D<Color> colors, EncodingMode mode)
        {
            return  new Hash<string, object>
            {
                ["image"] = EncodeTo(colors.Items, colors.Width, colors.Height, mode),
                ["width"] = colors.Width,
                ["height"] = colors.Height
            };
        }
        
        private string GetIndex(int row, int col)
        {
            return $"{row}x{col}";
        }

        private string GetDuration(double milliseconds)
        {
            return $"{TimeSpan.FromMilliseconds(milliseconds).TotalSeconds:0.00} Seconds";
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _storedData);
        }
        #endregion

        #region Map Renderer

        private Array2D<Color> Render(ImageConfig config)
        {
            int waterOffset = config.WaterOffset;
            int halfWaterOffset = waterOffset / 2;

            if (_heightMap == null || _splatMap == null || _terrain == null)
            {
                return new Array2D<Color>();
            }

            int mapSize = _terrain.terrainData.alphamapResolution;
            int imageWidth = mapSize + waterOffset;
            int imageHeight = mapSize + waterOffset;
            int widthWithWater = mapSize + halfWaterOffset;
            
            Array2D<Color> output = new Array2D<Color>(imageWidth, imageHeight);

            Parallel.For(-halfWaterOffset, imageHeight - halfWaterOffset, row =>
            {
                for (int col = -halfWaterOffset; col < widthWithWater; col++)
                {
                    float terrainHeight = GetHeight(row, col);
                    float sun = Math.Max(Vector3.Dot(GetNormal(row, col), config.SunDirection), 0.0f);
                    Vector3 pixel = Vector3.Lerp(config.StartColor, config.GravelColor, GetSplat(row, col, 128) * config.GravelColor.w);
                    pixel = Vector3.Lerp(pixel, config.PebbleColor, GetSplat(row, col, 64) * config.PebbleColor.w);
                    pixel = Vector3.Lerp(pixel, config.RockColor, GetSplat(row, col, 8) * config.RockColor.w);
                    pixel = Vector3.Lerp(pixel, config.DirtColor, GetSplat(row, col, 1) * config.DirtColor.w);
                    pixel = Vector3.Lerp(pixel, config.GrassColor, GetSplat(row, col, 16) * config.GrassColor.w);
                    pixel = Vector3.Lerp(pixel, config.ForestColor, GetSplat(row, col, 32) * config.ForestColor.w);
                    pixel = Vector3.Lerp(pixel, config.SandColor, GetSplat(row, col, 4) * config.SandColor.w);
                    pixel = Vector3.Lerp(pixel, config.SnowColor, GetSplat(row, col, 2) * config.SnowColor.w);
                    float waterDepth = -terrainHeight;
                    if (waterDepth > config.OceanWaterLevel)
                    {
                        pixel = Vector3.Lerp(pixel, config.WaterColor, Mathf.Clamp(0.5f + waterDepth / 5.0f, 0.0f, 1f));
                        pixel = Vector3.Lerp(pixel, config.OffShoreColor, Mathf.Clamp(waterDepth / 50f, 0.0f, 1f));
                        sun = config.SunPower;
                    }

                    pixel += (sun - config.SunPower) * config.SunPower * pixel;
                    pixel = (pixel - config.Half) * config.Contrast + config.Half;
                    pixel *= config.Brightness;
                    
                    output[row + halfWaterOffset, col + halfWaterOffset] = new Color(pixel.x, pixel.y, pixel.z);
                }
            });
            
            return output;
        }

        private Array2D<Color> RenderOverlay(Array2D<Color> previous, List<OverlayConfig> overlays)
        {
            Array2D<Color> colors = previous.Clone();
            foreach (OverlayConfig overlay in overlays)          
            {
                if (overlay.Image == null || overlay.Image.Length == 0)
                {
                    Puts($"{overlay.DebugName} contains an invalid image");
                    continue;
                }
                
                using (Bitmap image = ResizeImage(overlay.Image, overlay.Width, overlay.Height))
                {
                    int startRow = overlay.YPos - overlay.Height / 2;
                    int startCol = overlay.XPos - overlay.Width / 2;
                    
                    for (int row = 1; row <= image.Height; row++)
                    {
                        for (int col = 1; col <= image.Width; col++)
                        {
                            System.Drawing.Color pixel = image.GetPixel(overlay.Height - row, overlay.Width - col);
                            int overlayRow = row + startRow;
                            int overlayCol = col + startCol;
                            if (pixel.A != 0)
                            {
                                if (overlayRow >= colors.Height || overlayCol >= colors.Width || overlayRow < 0 || overlayCol < 0)
                                {
                                    continue;
                                }
                                
                                colors[row + startRow, col + startCol] = new Color32(pixel.R, pixel.G, pixel.B, pixel.A);
                            }
                        }
                    }
                }
            }

            return colors;
        }

        float GetHeight(int x, int y)
        {
            return _heightMap.GetHeight(x, y);
        }

        Vector3 GetNormal(int x, int y)
        {
            return _heightMap.GetNormal(x, y);
        }

        float GetSplat(int x, int y, int mask)
        {
            return _splatMap.GetSplat(x, y, mask);
        }
        
        private byte[] EncodeTo(Color[] color, int width, int height, EncodingMode mode)
        {
            Texture2D tex = null;
            try
            {
                tex = new Texture2D(width, height);
                tex.SetPixels(color);
                tex.Apply();
                return mode == EncodingMode.Jpg ? tex.EncodeToJPG(85) : tex.EncodeToPNG();
            }
            finally
            {
                if (tex != null)
                    UnityEngine.Object.Destroy(tex);
            }
        }
        
        private Bitmap ResizeImage(byte[] bytes, int targetWidth, int targetHeight)
        {
            using (MemoryStream original = new MemoryStream())
            {
                original.Write(bytes, 0, bytes.Length);
                using (Bitmap img = new Bitmap(Image.FromStream(original)))
                {
                    return new Bitmap(img, new Size(targetWidth, targetHeight));
                }
            }
        }
        #endregion

        #region Icon Handling

        private IEnumerator LoadIcon(IconConfig config)
        {
            int code = config.ImageUrl.GetHashCode();
            uint iconId = _storedData.IconIds[code];
            if (iconId != 0)
            {
                config.Image = LoadImage(iconId, FileStorage.Type.png);
            }
            else
            {
                yield return DownloadIcon(config, code);
            }
        }

        private IEnumerator DownloadIcon(IconConfig config, int code)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(config.ImageUrl))
            {
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    PrintError($"Failed to download icon: {www.error}");
                    www.Dispose();
                    yield break;
                }
                
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(www.downloadHandler.data);
                if (texture != null)
                {
                    byte[] bytes = texture.EncodeToPNG();
                    GameObject.Destroy(texture);
                    config.Image = bytes;
                    _storedData.IconIds[code] = StoreImage(bytes, FileStorage.Type.png);
                }
            }
        }

        #endregion
        
        #region Classes
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Starting Splits (Rows x Columns)")]
            public List<string> StartingSplits { get; set; }
            public Hash<string, IconConfig> IconSettings { get; set; }
        }

        private class IconConfig
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public string ImageUrl { get; set; }
            public bool Show { get; set; }
            
            [JsonIgnore]
            public byte[] Image { get; set; }
        }

        private class StoredData
        {
            public Hash<string, Hash<string, Hash<string, uint>>> MapIds = new Hash<string, Hash<string, Hash<string, uint>>>();
            public Hash<int, uint> IconIds = new Hash<int, uint>();
            
            public List<string> GetSavedSplits(string mapName)
            {
                Hash<string, Hash<string, uint>> splits = MapIds[mapName];
                return splits == null ? new List<string>() : splits.Keys.ToList();
            }
        }

        private class StorageInfo
        {
            public string MapName { get; set; }
            public string Split { get; set; }
            public Hash<string, Hash<string, object>> SplitData { get; set; }
        }

        private class ImageConfig
        {
            private static readonly Vector3 DefaultStartColor = new Vector3(0.324313372f, 0.397058845f, 0.195609868f);
            private static readonly Vector4 DefaultWaterColor = new Vector4(0.269668937f, 0.4205476f, 0.5660378f, 1f);
            private static readonly Vector4 DefaultGravelColor = new Vector4(0.139705867f, 0.132621378f, 0.114024632f, 0.372f);
            private static readonly Vector4 DefaultDirtColor = new Vector4(0.322227329f, 0.375f, 0.228860289f, 1f);
            private static readonly Vector4 DefaultSandColor = new Vector4(1f, 0.8250507f, 0.448529422f, 1f);
            private static readonly Vector4 DefaultGrassColor = new Vector4(0.4509804f, 0.5529412f, 0.270588249f, 1f);
            private static readonly Vector4 DefaultForestColor = new Vector4(0.5529412f, 0.440000027f, 0.270588249f, 1f);
            private static readonly Vector4 DefaultRockColor = new Vector4(0.42344287f, 0.4852941f, 0.314013839f, 1f);
            private static readonly Vector4 DefaultSnowColor = new Vector4(0.8088235f, 0.8088235f, 0.8088235f, 1f);
            private static readonly Vector4 DefaultPebbleColor = new Vector4(0.121568628f, 0.419607848f, 0.627451f, 1f);
            private static readonly Vector4 DefaultOffShoreColor = new Vector4(0.166295841f, 0.259337664f, 0.3490566f, 1f);
            private static readonly Vector3 DefaultSunDirection = Vector3.Normalize(new Vector3(0.95f, 2.87f, 2.37f));
            private static readonly Vector3 DefaultHalf = new Vector3(0.5f, 0.5f, 0.5f);
            private const float DefaultSunPower = 0.5f;
            private const float DefaultBrightness = 1f;
            private const float DefaultContrast = 0.87f;
            private const float DefaultOceanWaterLevel = 0.0f;
            
            public Vector3 StartColor { get; }
            public Vector4 WaterColor { get; }
            public Vector4 GravelColor  { get; }
            public Vector4 DirtColor  { get; }
            public Vector4 SandColor  { get; }
            public Vector4 GrassColor  { get; }
            public Vector4 ForestColor  { get; }
            public Vector4 RockColor  { get; }
            public Vector4 SnowColor  { get; }
            public Vector4 PebbleColor  { get; }
            public Vector4 OffShoreColor { get; }
            public Vector3 SunDirection  { get; }
            public Vector3 Half  { get; }
            public int WaterOffset { get; }
            public float SunPower { get; }
            public float Brightness { get; }
            public float Contrast { get; }
            public float OceanWaterLevel { get; }

            public ImageConfig() : this(new Hash<string, object>())
            {
                
            }
            
            public ImageConfig(Hash<string, object> config)
            {
                StartColor = config.ContainsKey(nameof(StartColor)) ? (Vector3) config[nameof(StartColor)] : DefaultStartColor;
                WaterColor = config.ContainsKey(nameof(WaterColor)) ? (Vector4) config[nameof(WaterColor)] : DefaultWaterColor;
                GravelColor = config.ContainsKey(nameof(GravelColor)) ? (Vector4) config[nameof(GravelColor)] : DefaultGravelColor;
                DirtColor = config.ContainsKey(nameof(DirtColor)) ? (Vector4) config[nameof(DirtColor)] : DefaultDirtColor;
                SandColor = config.ContainsKey(nameof(SandColor)) ? (Vector4) config[nameof(SandColor)] : DefaultSandColor;
                GrassColor = config.ContainsKey(nameof(GrassColor)) ? (Vector4) config[nameof(GrassColor)] : DefaultGrassColor;
                ForestColor = config.ContainsKey(nameof(ForestColor)) ? (Vector4) config[nameof(ForestColor)] : DefaultForestColor;
                RockColor = config.ContainsKey(nameof(RockColor)) ? (Vector4) config[nameof(RockColor)] : DefaultRockColor;
                SnowColor = config.ContainsKey(nameof(SnowColor)) ? (Vector4) config[nameof(SnowColor)] : DefaultSnowColor;
                PebbleColor = config.ContainsKey(nameof(PebbleColor)) ? (Vector4) config[nameof(PebbleColor)] : DefaultPebbleColor;
                OffShoreColor = config.ContainsKey(nameof(OffShoreColor)) ? (Vector4) config[nameof(OffShoreColor)] : DefaultOffShoreColor;
                SunDirection = config.ContainsKey(nameof(SunDirection)) ? (Vector3) config[nameof(SunDirection)] : DefaultSunDirection;
                SunPower = config.ContainsKey(nameof(SunPower)) ? (float) config[nameof(SunPower)] : DefaultSunPower;
                Brightness = config.ContainsKey(nameof(Brightness)) ? (float) config[nameof(Brightness)] : DefaultBrightness;
                Contrast = config.ContainsKey(nameof(Contrast)) ? (float) config[nameof(Contrast)] : DefaultContrast;
                OceanWaterLevel = config.ContainsKey(nameof(OceanWaterLevel)) ? (float) config[nameof(OceanWaterLevel)] : DefaultOceanWaterLevel;
                Half = config.ContainsKey(nameof(Half)) ? (Vector3) config[nameof(Half)] : DefaultHalf;
                WaterOffset = config.ContainsKey(nameof(WaterOffset)) ? (int) config[nameof(WaterOffset)] : 0;
            }
        }

        private class OverlayConfig
        {
            public int XPos { get; set; }
            public int YPos { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public byte[] Image { get; set; }
            public string DebugName { get; set; }

            public OverlayConfig()
            {
                
            }

            public OverlayConfig(Hash<string, object> data)
            {
                XPos = data.ContainsKey(nameof(XPos)) ? (int)data[nameof(XPos)] : 0;
                YPos = data.ContainsKey(nameof(YPos)) ? (int)data[nameof(YPos)] : 0;
                Width = data.ContainsKey(nameof(Width)) ? (int)data[nameof(Width)] : 0;
                Height = data.ContainsKey(nameof(Height)) ? (int)data[nameof(Height)] : 0;
                Image = data.ContainsKey(nameof(Image)) ? (byte[])data[nameof(Image)] : null;
                DebugName = data.ContainsKey(nameof(DebugName)) ? (string)data[nameof(DebugName)] : null;
            }
        }
        
        private struct Array2D<T>
        {
            public readonly T[] Items;

            public readonly int Width;

            public readonly int Height;

            public Array2D(T[] items, int width, int height)
            {
                Items = items;
                Width = width;
                Height = height;
            }
            
            public Array2D(int width, int height)
            {
                Items = new T[width * height];
                Width = width;
                Height = height;
            }
            
            public T this[int row, int col]
            {
                get
                {
                    if (row < 0 || row > Width - 1)
                    {
                        throw new IndexOutOfRangeException( $"Get row out of range at {row} Min: 0 Max: {Width - 1}");
                    }

                    if (col < 0 || col > Height - 1)
                    {
                        throw new IndexOutOfRangeException($"Get col out of range at {col} Min: 0 Max: {Height - 1}");
                    }
                    
                    return Items[col * Width + row];
                }
                set
                {
                    if (row < 0 || row > Width - 1)
                    {
                        throw new IndexOutOfRangeException( $"Set row out of range at {row} Min: 0 Max: {Width - 1}");
                    }

                    if (col < 0 || col > Height - 1)
                    {
                        throw new IndexOutOfRangeException($"Set col out of range at {col} Min: 0 Max: {Height - 1}");
                    }
                    
                    Items[col * Width + row] = value;
                }
            }

            public bool IsEmpty()
            {
                return Items == null || Width == 0 && Height == 0;
            }

            public Array2D<T> Splice(int startX, int startY, int width, int height)
            {
                if (startX < 0 || startX >= Width)
                {
                    throw new IndexOutOfRangeException($"startX is < 0 or greater than {Width}: {startX}");
                }

                if (startY < 0 || startY >= Height)
                {
                    throw new IndexOutOfRangeException($"startY is < 0 or greater than {Height}: {startY}");
                }

                if (width == 0 || startX + width > Width)
                {
                    throw new IndexOutOfRangeException($"width is < 0 or greater than {Width}: {width}");
                }

                if (height == 0 || startY + height > Height)
                {
                    throw new IndexOutOfRangeException($"height is < 0 or greater than {Height}: {height}");
                }
                
                Array2D<T> splice = new Array2D<T>(width, height);
                Array2D<T> copyThis = this;
                Parallel.For(0, width, x =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        splice[x, y] = copyThis[startX + x, startY + y];
                    }
                });

                return splice;
            }

            public Array2D<T> Clone()
            {
                return new Array2D<T>((T[])Items.Clone(), Width, Height);
            }

        }
        #endregion
    }
}
