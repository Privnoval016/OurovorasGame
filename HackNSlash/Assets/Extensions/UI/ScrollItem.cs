using System;
using UnityEngine;

namespace Extensions.UI
{
    public interface ScrollItem
    {
        public abstract void OnScrollActive(); // called when the item is selected or focused

        public abstract void OnScrollInactive(); // called when the item is deselected or unfocused

        public abstract ItemUIInfo GetItemUIInfo(); // creates the UI for the item at the specified parent transform
    }

    public class ItemUIInfo
    {
        public string itemName;
        public string itemDescription;
        public int amount;
        //public Rarity itemRarity;
    }
}