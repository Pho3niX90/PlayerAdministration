using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Broken Items Cleaner", "Orange", "1.1.3")]
    [Description("Cleaning server from broken items and held entities")]
    public class BrokenItemsCleaner : RustPlugin
    {
        #region Oxide Hooks

        private void Init()
        {
            cmd.AddConsoleCommand("brokenitemscleaner.run", this, nameof(cmdControlConsole));
        }

        #endregion

        #region Commands

        private void cmdControlConsole(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == true)
            {
                Server.Broadcast(GetMessage(Message.ExpectLags));
                timer.Once(3f, Repair);
            }
        }

        #endregion

        #region Core

        private void Repair()
        {
            var countRepaired = 0;
            var countRemoved = 0;
            var items = GetAllItems();
            var heldList = UnityEngine.Object.FindObjectsOfType<HeldEntity>().ToList();

            foreach (var item in items.ToArray())
            {
                var held = item.GetHeldEntity()?.GetComponent<HeldEntity>();
                if (held != null)
                {
                    heldList.Remove(held);
                }
                else
                {
                    item.OnItemCreated();
                    item.MarkDirty();
                    held = item.GetHeldEntity()?.GetComponent<HeldEntity>();
                    if (held != null)
                    {
                        countRepaired++;
                    }
                }
            }

            foreach (var entity in heldList.ToArray())
            {
                if (entity.IsValid() == true)
                {
                    entity.Kill();
                    countRemoved++;
                }
            }

            var message = GetMessage(Message.ReportInfo, new Dictionary<string, object> {{"{countRemoved}", countRemoved}, {"{countRepaired}", countRepaired}});
            PrintWarning(message);
        }

        private static Item[] GetAllItems()
        {
            var allItems = new List<Item>();
            var boxes = UnityEngine.Object.FindObjectsOfType<StorageContainer>().SelectMany(x => x?.inventory?.itemList);
            var players = UnityEngine.Object.FindObjectsOfType<BasePlayer>().SelectMany(x => x?.inventory?.AllItems());
            var corpses = UnityEngine.Object.FindObjectsOfType<LootableCorpse>().SelectMany(x => x?.containers).SelectMany(x => x?.itemList);
            var dropped = UnityEngine.Object.FindObjectsOfType<DroppedItemContainer>().SelectMany(x => x?.inventory?.itemList);
            var onGround = UnityEngine.Object.FindObjectsOfType<DroppedItem>().Select(x => x?.item);
            var ioContainer = UnityEngine.Object.FindObjectsOfType<ContainerIOEntity>().SelectMany(x => x?.inventory?.itemList);
            allItems.AddRange(boxes);
            allItems.AddRange(players);
            allItems.AddRange(corpses);
            allItems.AddRange(dropped);
            allItems.AddRange(onGround);
            allItems.AddRange(ioContainer);

            foreach (var item in allItems.ToArray())
            {
                if (item.contents != null)
                {
                    allItems.AddRange(item.contents.itemList);
                }
            }
            
            return allItems.ToArray();
        }

        #endregion
        
        #region Language | 2.0.0
        
        private Dictionary<object, string> langMessages = new Dictionary<object, string>
        {
            {Message.ReportInfo, "Removed {countRemoved} broken entities and repaired {countRepaired}"},
            {Message.ExpectLags, "Server will be cleaned from some broken entities! Expect lag in few seconds"},
        };
        
        private enum Message
        {
            ReportInfo,
            ExpectLags,
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(langMessages.ToDictionary(x => x.Key.ToString(), y => y.Value), this);
        }

        private string GetMessage(Message key, Dictionary<string, object> args = null, string playerID = null)
        {
            var message = lang.GetMessage(key.ToString(), this, playerID);

            if (args != null)
            {
                foreach (var pair in args)
                {
                    var s0 = "{" + pair.Key + "}";
                    var s1 = pair.Key;
                    var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                    message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                    message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            return message;
        }

        #endregion
    }
}