using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Object Remover", "Iv Misticos", "3.0.5")]
    [Description("Removes furnaces, lanterns, campfires, buildings etc. on command")]
    class ObjectRemover : RustPlugin
    {
        #region Variables
        
        private const string ShortnameCupboard = "cupboard.tool";

        #endregion

        #region Configuration

        private Configuration _config = new Configuration();

        private class Configuration
        {
            [JsonProperty(PropertyName = "Object Command Permission")]
            public string PermissionObject = "objectremover.use";
            
            [JsonProperty(PropertyName = "Statistics Command Permission")]
            public string PermissionStatistics = "objectremover.statistics";
            
            [JsonProperty(PropertyName = "Object Command")]
            public string CommandObject = "object";
            
            [JsonProperty(PropertyName = "Statistics Command")]
            public string CommandStatistics = "objtop";
            
            public string Prefix = "[<color=#ffbf00> Object Remover </color>] ";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion
        
        #region Hooks

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No Rights", "You do not have enough rights" },
                { "Count", "We found {count} entities in {time}s." },
                { "Removed", "You have removed {count} entities in {time}s." },
                { "Help", "Object command usage:\n" +
                          "/object (entity) [parameters]\n" +
                          "Parameters:\n" +
                          "action count/remove - Count or remove entities\n" +
                          "radius NUM - Radius\n" +
                          "inside true/false - Entities inside the cupboard\n" +
                          "outside true/false - Entities outside the cupboard" },
                { "No Console", "Please log in as a player to use that command" },
                { "Statistics Header", "Objects Statistics:" },
                { "Statistics Entry", "\n#{position} - {shortname} x {count}" },
                { "Statistics Footer", "\nDone." }
            }, this);
        }

        private void OnServerInitialized()
        {
            LoadDefaultMessages();
            LoadConfig();
            
            permission.RegisterPermission(_config.PermissionObject, this);
            permission.RegisterPermission(_config.PermissionStatistics, this);

            AddCovalenceCommand(_config.CommandObject, nameof(CommandObject));
            AddCovalenceCommand(_config.CommandStatistics, nameof(CommandStatistics));
        }
        
        #endregion

        #region Commands

        private void CommandObject(IPlayer player, string command, string[] args)
        {
            var prefix = player.IsServer ? string.Empty : _config.Prefix;
            if (!player.HasPermission(_config.PermissionObject))
            {
                player.Reply(prefix + GetMsg("No Rights", player.Id));
                return;
            }

            if (args.Length < 1)
            {
                player.Reply(prefix + GetMsg("Help", player.Id));
                return;
            }

            var options = new RemoveOptions();
            options.Parse(args);
            if (player.IsServer)
                options.Radius = 0f;
            
            var entity = args[0];
            var position = (player.Object as BasePlayer)?.transform.position ?? Vector3.zero;
            var before = Time.realtimeSinceStartup;
            var objects = FindObjects(position, entity, options);
            var count = objects.Count;

            if (!options.Count)
            {
                for (var i = 0; i < count; i++)
                {
                    var ent = objects[i];
                    if (ent == null || ent.IsDestroyed)
                        continue;
                    ent.Kill();
                }
            }

            player.Reply(prefix + GetMsg(options.Count ? "Count" : "Removed", player.Id)
                             .Replace("{count}", count.ToString()).Replace("{time}",
                                 (Time.realtimeSinceStartup - before).ToString("0.###")));
        }

        private void CommandStatistics(IPlayer player, string command, string[] args)
        {
            var prefix = player.IsServer ? string.Empty : _config.Prefix;
            if (!player.HasPermission(_config.PermissionStatistics))
            {
                player.Reply(prefix + GetMsg("No Rights", player.Id));
                return;
            }

            int amount;
            if (args == null || args.Length == 0 || !int.TryParse(args[0], out amount))
                amount = 10;

            var entities = new Dictionary<string, int>();

            using (var enumerator = BaseNetworkable.serverEntities.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var entity = enumerator.Current;
                    if (entity == null)
                        continue;

                    var shortname = entity.ShortPrefabName;
                    if (entities.ContainsKey(shortname))
                        entities[shortname]++;
                    else
                        entities[shortname] = 1;
                }
            }

            player.Reply(prefix + GetMsg("Statistics Header", player.Id));
            
            var builder = new StringBuilder();
            using (var enumerator = entities.OrderByDescending(x => x.Value).Take(amount).GetEnumerator())
            {
                var currentPosition = 1;
                while (enumerator.MoveNext())
                {
                    builder.Length = 0;
                    builder.Append(GetMsg("Statistics Entry", player.Id));
                    builder.Replace("{position}", $"{currentPosition++}");
                    builder.Replace("{shortname}", $"{enumerator.Current.Key}");
                    builder.Replace("{count}", $"{enumerator.Current.Value}");
                    player.Reply(prefix + builder);
                }
            }
            
            player.Reply(prefix + GetMsg("Statistics Footer", player.Id));
        }

        #endregion
        
        #region Remove Options

        private class RemoveOptions
        {
            public float Radius = 0f;
            public bool Count = true;
            public bool OutsideCupboard = true;
            public bool InsideCupboard = true;

            public void Parse(string[] args)
            {
                for (var i = 1; i + 1 < args.Length; i += 2)
                {
                    switch (args[i])
                    {
                        case "a":
                        case "action":
                        {
                            Count = args[i + 1] != "remove";

                            break;
                        }
                        
                        case "r":
                        case "radius":
                        {
                            if (!float.TryParse(args[i + 1], out Radius))
                                Radius = 0f;

                            break;
                        }

                        case "oc":
                        case "outside":
                        {
                            if (!bool.TryParse(args[i + 1], out OutsideCupboard))
                                OutsideCupboard = true;
                            
                            break;
                        }

                        case "ic":
                        case "inside":
                        {
                            if (!bool.TryParse(args[i + 1], out InsideCupboard))
                                InsideCupboard = true;

                            break;
                        }
                    }
                }
            }
        }
        
        #endregion

        #region Helpers

        private List<BaseEntity> FindObjects(Vector3 startPos, string entity, RemoveOptions options)
        {
            var entities = new List<BaseEntity>();
            var isAll = entity.Equals("all");
            if (options.Radius > 0)
            {
                var entitiesList = new List<BaseEntity>();
                Vis.Entities(startPos, options.Radius, entitiesList);
                var entitiesCount = entitiesList.Count;
                for (var i = entitiesCount - 1; i >= 0; i--)
                {
                    var ent = entitiesList[i];

                    if (IsNeededObject(ent, entity, isAll, options))
                        entities.Add(ent);
                }
            }
            else
            {
                var ents = UnityEngine.Object.FindObjectsOfType<BaseEntity>();
                var entsCount = ents.Length;
                for (var i = 0; i < entsCount; i++)
                {
                    var ent = ents[i];
                    if (IsNeededObject(ent, entity, isAll, options))
                        entities.Add(ent);
                }
            }

            return entities;
        }

        private bool IsNeededObject(BaseEntity entity, string shortname, bool isAll, RemoveOptions options)
        {
            return (isAll || entity.ShortPrefabName.IndexOf(shortname, StringComparison.CurrentCultureIgnoreCase) != -1) &&
                   (options.InsideCupboard && entity.GetBuildingPrivilege() != null ||
                    options.OutsideCupboard && entity.GetBuildingPrivilege() == null);
        }

        private string GetMsg(string key, string userId = null) => lang.GetMessage(key, this, userId);

        #endregion
    }
}