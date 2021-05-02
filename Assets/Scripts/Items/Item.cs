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
    Shield,
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
        set {
            _level = value;
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
        set {
            _abilityText = value;
        }
    }
    protected Character _equippedCharacter;//Character it is equipped to; = null if not equipped
    public Character EquippedCharacter {
        get {
            return _equippedCharacter;
        }
        set {
            _equippedCharacter = value;
        }
    }
    public bool InShop;//Whether the item is in the shop or your general inventory
    [SerializeField] protected Sprite _sprite;
    public Sprite Sprite {
        get {
            return _sprite;
        }
    }
    //May have special abilities that are declared as delegates
    public ItemSpecialAbility OnQuestStartAbility;
    public ItemSpecialAbility OnQuestEndAbility;
    public List<StatType> AbilityAffectedStats;
    public ItemAbilityReference AbilityReference;
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
        _equippedCharacter.RefreshHealth();
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
        _equippedCharacter.RefreshHealth();
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
        //If the player has fewer items than max, then it goes to the next step
        if(temp < choice.InventorySize) {
            //If the player is a higher or equal level to the item, they go to the next step
            if(choice.Level >= Level) {
                 //If the player has not met the limit on that type of item:
                 //runs through each type individually
                 //if it's a potion, you can carry 2
                 if(Type == ItemType.Potion) {
                     int temp2 = 0;
                     foreach(Item item in choice.Inventory) {
                         if(item.Type == ItemType.Potion) {
                             temp2++;
                         }
                     }
                     if(temp2 >= 2) {
                         return false;
                     } 
                     else {
                         return true;
                     }
                 }
                 //If it's a miscellaneous item, it's automatically able to be added
                 else if(Type == ItemType.Misc) {
                     return true;
                 }
                 //Otherwise, you can only have 1 item of that type
                 else {
                     int temp2 = 0;
                     foreach(Item item in choice.Inventory) {
                         if(item.Type == Type) {
                             temp2++;
                         }
                     }
                     if(temp2 >= 1) {
                         return false;
                     }
                     else {
                         return true;
                     }
                 }
                 
            }
            else {
                return false;
            }
        }
        else {
            return false;
        }
    }

    //These functions are called whenever the equipped character starts/ends a quest
    public bool abilityActive;//Some abilities will only sometimes be active, so this toggles that
    public int abilityValue;//Some abilities will need to save an integer value, so we have this variable
    public virtual void StartQuest(ref Quest quest) {
        //At the very least, it always triggers the start quest ability
        OnQuestStartAbility(this, AbilityAffectedStats);
    }
    public virtual void EndQuest(ref Quest quest) {
        OnQuestEndAbility(this, AbilityAffectedStats);
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
