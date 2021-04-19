using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//This ScriptableObject contains the variables needed to generate Items, so it can be more safely organized
[System.Serializable]
public class ItemTraits {//This class contains all the general information for a given object - I use a class instead of a struct bc of inheritance
    public string ItemName;
    public Sprite ItemSprite;
    public ItemType Type;
    public int Level;//The level/minimum level of the item
}

[System.Serializable] public class PotionTraits : ItemTraits {//Contains general information that is specific for potions
    public StatType PotionType;
    public bool IsHealthPotion;
}
[System.Serializable] public class SpecificTraits : ItemTraits {//Contains general information that is specific for specific item types
    //Needs 2 lists here, bc I'd like to use a Dictionary, but they can't be declared in the Inspector
    public List<StatType> PrimaryModifier;
    public List<int> PrimaryModifierValue;
    public List<StatType> SecondaryModifier;
    public List<int> SecondaryModifierValue;
}

[System.Serializable] public class MiscTraits : ItemTraits {//contains general information for miscellaneous Items
    public List<StatType> Modifier;
    public List<int> ModifierValue;
}

[CreateAssetMenu(menuName = "ScriptableObjects/ItemGeneration")]
public class ItemGeneration : ScriptableObject
{
    //Structs for different types of Items
    public ItemTraits[] Items;
    public PotionTraits[] Potions;
    public SpecificTraits[] Specifics;
    [SerializeField] private Sprite test;
    [SerializeField] private Item ItemPrefab;
    public string Name;
    public Item BasicGeneration(int itemLevel) {
        Item newItem = Instantiate(ItemPrefab, Vector3.zero, Quaternion.identity);
        Dictionary<StatType, int> modifiers = new Dictionary<StatType, int>();
        modifiers.Add(StatType.Strength, 2);
        modifiers.Add(StatType.Defense, 3);

        newItem.DeclareTraits(modifiers, "NAME", ItemType.Weapon, 1, 15, "This is my ability text", test);
        return newItem;
    }

    public Item PotionGeneration(int itemLevel) {
        Item newPotion = Instantiate(ItemPrefab, Vector3.zero, Quaternion.identity);
        List<PotionTraits> lvlPotions = new List<PotionTraits>();
        foreach(PotionTraits item in Potions) {
            if(item.Level == itemLevel) {
                lvlPotions.Add(item);
            }
        }
        PotionTraits rnd = lvlPotions[Random.Range(0, lvlPotions.Count)];
        Dictionary<StatType, int> modifiers = new Dictionary<StatType, int>();
        modifiers.Add(StatType.Strength, 0);
        string PotionAbilityText = DeclareText(ItemType.Potion, rnd.Level, rnd.PotionType, rnd.IsHealthPotion);
        newPotion.DeclareTraits(modifiers, rnd.ItemName, rnd.Type, rnd.Level, 0, PotionAbilityText, rnd.ItemSprite);
        return newPotion;
    }


    //The declare text for potions specifically
    private string DeclareText(ItemType myType, int lvl, StatType statModifier, bool modifiesHealth) {
        //Potions have a specific universal text that just gets plugged into
        string tempString = "";
        if(myType == ItemType.Potion) {
            if(modifiesHealth) {
                tempString = "Ingested during a quest if enough damage is taken. Heals " + ItemPotion.CalculatePotency(lvl, modifiesHealth) + " health.";
            }
            else {
                tempString = "Ingested at the beginning of a quest. Gives +" + ItemPotion.CalculatePotency(lvl, modifiesHealth) + " " + statModifier.ToString() + " during that quest.";
            }
        }
        return tempString;
    }

    private string DeclareText() {
        return "";
    }

}
