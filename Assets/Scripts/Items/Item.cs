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
    //What modifiers this item applies to various stats
    [SerializeField] protected Dictionary<StatType, int> _modifier;
    public Dictionary<StatType, int> Modifier {
        get {
            return _modifier;
        }
    }
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
        _equippedCharacter.Inventory.Add(this);
        //It then refreshes the UI;
        UIManager.Instance.RefreshCharacterUI();
        UIManager.Instance.RefreshItemUI();
        myManager.RefreshUnequippedItemsUI();
    }

    //Unequipping is much easier, because it's already attached to a character
    public virtual void Unequip() {
        foreach(var item in Modifier) {
            EquippedCharacter.StatModifier[item.Key] -= item.Value;
        }
        _equippedCharacter.Inventory.Remove(this);//Remove it from the player's equipment
        _equippedCharacter.RefreshUI();
        _equippedCharacter = null;
        myManager.UnequippedItems.Add(this);
        //It then refreshes the UI;
        UIManager.Instance.RefreshCharacterUI();
        UIManager.Instance.RefreshItemUI();
        myManager.RefreshUnequippedItemsUI();
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

    //A method that allows for other scripts to declare the traits of this object
    public virtual void DeclareTraits(Dictionary<StatType, int> n_modifier, string n_itemName, ItemType n_type, int n_level, int n_price, string n_abilityText, Sprite n_sprite) {
        //Gets the 1 existing ItemManager as this object's manager
        myManager = (ItemManager)FindObjectOfType(typeof(ItemManager));
        //Transfers the Modifier dictionary over, making sure to replace all that need to be replaced
        if(Modifier == null) {
            _modifier = new Dictionary<StatType, int>();
        }
        foreach(var item in n_modifier) {
            if(Modifier.ContainsKey(item.Key)) {
                _modifier[item.Key] = n_modifier[item.Key];
            }
            else {
                _modifier.Add(item.Key, item.Value);
            }
        }
        //Transfers over other easy traits
        _itemName = n_itemName;
        _type = n_type;
        _level = n_level;
        _price = n_price;
        _abilityText = n_abilityText;
        _sprite = n_sprite;
    }

}
