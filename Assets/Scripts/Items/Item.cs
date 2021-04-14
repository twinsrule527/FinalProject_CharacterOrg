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

}
