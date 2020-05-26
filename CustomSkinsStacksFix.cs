using System;

namespace Oxide.Plugins
{
    [Info("Custom Skins Stacks Fix", "Orange", "1.0.3")]
    [Description("Fixing problem with stacking/splitting/moving custom items (skins)")]
    public class CustomSkinsStacksFix : RustPlugin
    {
        private object CanStackItem(Item original, Item target)
        {
            if (original.skin == 0 && target.skin == 0)
            {
                return null;
            }
            
            if (original.skin != target.skin)
            {
                return false;
            }

            if (original.contents != null || target.contents != null)
            {
                return false;
            }

            if (Math.Abs(original._condition - target._condition) > 0f)
            {
                return false;
            }

            if (Math.Abs(original._maxCondition - target._maxCondition) > 0f)
            {
                return false;
            }

            return null;
        }
        
        private Item OnItemSplit(Item item, int amount)
        {
            if (item.skin == 0)
            {
                return null;
            }

            item.amount -= amount;
            var newItem = ItemManager.Create(item.info, amount, item.skin);
            newItem.name = item.name;
            newItem._condition = item._condition;
            newItem._maxCondition = item._maxCondition;
            
            if (item.IsBlueprint())
            {
                newItem.blueprintTarget = item.blueprintTarget;
            }
                
            item.MarkDirty();
            return newItem;
        }
        
        private object CanAcceptItem(ItemContainer container, Item movingItem, int targetPos)
        {
            if (movingItem.skin == 0)
            {
                return null;
            }
            
            var containerItem = container.parent;
            if (containerItem == null || containerItem.skin == 0)
            {
                return null;
            }
            
            if (movingItem.contents != null && movingItem.contents.capacity > 4)
            {
                return ItemContainer.CanAcceptResult.CannotAccept;
            }
            
            return null;
        }
        
        private object CanCombineDroppedItem(WorldItem first, WorldItem second)
        {
            return CanStackItem(first.item, second.item);
        }
    }
}