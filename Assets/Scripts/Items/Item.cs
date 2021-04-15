using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Parent class for all Items, includes universal abilities

//This enum declares the different types of items that can exist
public enum ItemType {
    Potion,
    Weapon,
    Armor,
    Ring,
    Misc
}
public class Item : MonoBehaviour
{
    public Dictionary<StatType, int> Modifier;//What modifiers this item applies to various stats
    [SerializeField] protected string _itemName;
    public string ItemName {
        get{
            return _itemName;
        }
    }
    [SerializeField] protected ItemType _type;
    public ItemType Type {
        get {
            return _type;
        }
    }
    [SerializeField] protected int _level;
    public int Level {
        get {
            return _level;
        }
    }
    [SerializeField] protected int _price;
    public int Price {
        get {
            return _price;
        }
    }
    [SerializeField] protected string _abilityText;
    public string AbilityText {
        get {
            return _abilityText;
        }
    }
    private Character _equippedCharacter;//Character it is equipped to; = null if not equipped
    public Character EquippedCharacter {
        get {
            return _equippedCharacter;
        }
    }
    public bool InShop;//Whether the item is in the shop or your general inventory
    [SerializeField] private Sprite _sprite;
    public Sprite Sprite {
        get {
            return _sprite;
        }
    }

    public ItemManager myManager;//The overall manager of this object - is one universal one

    //Equip function equips it to a character, requiring the character you're equipping it to
    public virtual void Equip(Character equipTo) {
        _equippedCharacter = equipTo;
        myManager.UnequippedItems.Remove(this);
        foreach(var item in Modifier) {
            //For every stat modifier this object has, it is added to the equipped Player's stat Modifiers
            equipTo.StatModifier[item.Key] += item.Value;
        }
    }

    //Unequipping is much easier, because it's already attached to a character
    public virtual void Unequip() {
        foreach(var item in Modifier) {
            EquippedCharacter.StatModifier[item.Key] -= item.Value;
        }
        _equippedCharacter = null;
        myManager.UnequippedItems.Add(this);
    }

    //This bool says whether the item is able to be equipped to the chosen character
    public virtual bool EquippableTo(Character choice) {
        //Base version will only check to see if the player's inventory slots are all full
            //Higher-level versions check to see if other req.s are fulfilled
        int temp = 0;
        //This loop runs in case the Inventory contains some null items
        foreach(Item item in choice.Inventory) {
            if(item != null) {
                temp++;
            }
        }
        if(temp < choice.InventorySize) {
            return true;
        }
        else {
            return false;
        }
    }

}
