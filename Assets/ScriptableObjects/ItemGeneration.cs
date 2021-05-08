using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//This ScriptableObject contains the variables needed to generate Items, so it can be more safely organized
public enum ItemAbilityReference {
    None,
    Bloodied,
    BorrowStat,
    Butter,
    Challenge,
    Consolation,
    Cursed,
    DiverseInventory,
    EmptyHanded,
    Leveled,
    MultiplyBase,
    Powerhouse,
    Rally,
    Random,
    RepeatBonus,
    Reroll,
    Swap,
    Tidal,
    Wealthy,
    Worn
}
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
    public List<StatType> AbilityStats;
    public ItemAbilityReference Ability;
}

[System.Serializable] public class MiscTraits : ItemTraits {//contains general information for miscellaneous Items
    public List<StatType> Modifier;
    public List<int> ModifierValue;
    public List<StatType> AbilityStats;
    public ItemAbilityReference Ability;
}

//This delegate is used for Special abilities that Items may have
public delegate void ItemSpecialAbility(Item self, List<StatType> statsAffected);

[CreateAssetMenu(menuName = "ScriptableObjects/ItemGeneration")]
public class ItemGeneration : ScriptableObject
{
    //Structs for different types of Items
    public ItemTraits[] Items;
    public PotionTraits[] Potions;
    public SpecificTraits[] Specifics;
    [SerializeField] private Sprite test;
    [SerializeField] public Item ItemPrefab;
    public string Name;
    public Item BasicGeneration(int itemLevel) {
        /*Item newItem = Instantiate(ItemPrefab, Vector3.zero, Quaternion.identity);
        Dictionary<StatType, int> modifiers = new Dictionary<StatType, int>();
        modifiers.Add(StatType.Strength, 2);
        modifiers.Add(StatType.Defense, 3);

        newItem.DeclareTraits(modifiers, "NAME", ItemType.Weapon, itemLevel, 15, "This is my ability text", test);
        newItem.OnQuestStartAbility = DoNothing;
        newItem.OnQuestEndAbility = DoNothing;
        return newItem;*/
        Item newItem;
        //Actual generation starts here
            //Picks what ItemType is being generated, between Potions, Specifics, and Misc
        float rnd = Random.Range(0f, 1f);
        if(rnd > 2f) {
            //Always a 10% chance to be a Misc Item
            newItem = PotionGeneration(itemLevel);

        }
        //need to make these other options more of a scale
        else if(rnd > 0.45f) {
            //Generate a Potion Item
            newItem = PotionGeneration(itemLevel);
        }
        else {
            //Generate a SpecificItem
            newItem = SpecificGeneration(itemLevel);
        }
        return newItem;

    }
    public Item SpecificGeneration(int itemLevel) {
        Item newItem = Instantiate(ItemPrefab, Vector3.zero, Quaternion.identity);
        float rnd = Random.Range(0f, 1f);
        //Has a chance to be a item that generates at this level, or has a chance to be a levelled up lower level item
        SpecificTraits specItem;
        if(rnd > 0.1f * (itemLevel - 1)) {
            List<SpecificTraits> lvlItems = new List<SpecificTraits>();
            foreach(SpecificTraits item in Specifics) {
                if(item.Level == itemLevel) {
                    lvlItems.Add(item);
                }
            }
            int random = Random.Range(0, lvlItems.Count);
            specItem = lvlItems[random];
            Dictionary<StatType, int> modifiers = new Dictionary<StatType, int>();
            for(int i = 0; i < specItem.PrimaryModifier.Count; i++) {
                modifiers.Add(specItem.PrimaryModifier[i], specItem.PrimaryModifierValue[i]);
            }
            for(int i = 0; i < specItem.SecondaryModifier.Count; i++) {
                modifiers.Add(specItem.SecondaryModifier[i], specItem.SecondaryModifierValue[i]);
            }
            string declaredText = DeclareText(specItem);
            newItem.DeclareTraits(modifiers, specItem.ItemName, specItem.Type, itemLevel, CalculateBasePrice(specItem), declaredText, specItem.ItemSprite);
        }
        else {
            List<SpecificTraits> lvlItems = new List<SpecificTraits>();
            foreach(SpecificTraits item in Specifics) {
                if(item.Level < itemLevel) {
                    lvlItems.Add(item);
                }
            }
            specItem = lvlItems[Random.Range(0, lvlItems.Count)];
            int baseLevel = specItem.Level;
            //increase traits as needed
            for(int i = 0; i < specItem.PrimaryModifierValue.Count; i++) {
                specItem.PrimaryModifierValue[i] += (itemLevel - baseLevel);
            }
            //Secondary traits either get +1 every level or every few levels, depending on how many there are
            if(specItem.SecondaryModifierValue.Count == 1) {
                specItem.SecondaryModifierValue[0] += Mathf.CeilToInt(itemLevel - baseLevel);
            }
            else if(specItem.SecondaryModifierValue.Count > 0) {
                for(int i = baseLevel;  i < itemLevel; i++) {
                    specItem.SecondaryModifierValue[i % specItem.SecondaryModifierValue.Count] ++;
                }
            }
            //Then, modifiers are added to needed dictionary
            Dictionary<StatType, int> modifiers = new Dictionary<StatType, int>();
            for(int i = 0; i < specItem.PrimaryModifier.Count; i++) {
                modifiers.Add(specItem.PrimaryModifier[i], specItem.PrimaryModifierValue[i]);
            }
            for(int i = 0; i < specItem.SecondaryModifier.Count; i++) {
                modifiers.Add(specItem.SecondaryModifier[i], specItem.SecondaryModifierValue[i]);
            }
            string declaredText = DeclareText(specItem);
            newItem.DeclareTraits(modifiers, specItem.ItemName, specItem.Type, itemLevel, CalculateBasePrice(specItem), declaredText, specItem.ItemSprite);

        }
        //Needs to generate abilities
        newItem.AbilityReference = specItem.Ability;
        newItem.AbilityAffectedStats = specItem.AbilityStats;
            //Goes through each ability enumerator
        DeclareItemAbility(newItem, specItem.Ability);
        return newItem;
    }
    public void DeclareItemAbility(Item newItem, ItemAbilityReference Ability) {
        if(Ability == ItemAbilityReference.Bloodied) {
            newItem.OnQuestStartAbility = BloodiedStartQuest;
            newItem.OnQuestEndAbility = BloodiedEndQuest;
            newItem.AbilityText += BloodiedDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.BorrowStat) {
            newItem.OnQuestStartAbility = BorrowStatStartQuest;
            newItem.OnQuestEndAbility = BorrowStatEndQuest;
            newItem.AbilityText += BorrowStatDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Challenge) {
            newItem.OnQuestStartAbility = ChallengeStartQuest;
            newItem.OnQuestEndAbility = ChallengeEndQuest;
            newItem.AbilityText += ChallengeDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Consolation) {
            newItem.OnQuestStartAbility = ConsolationStartQuest;
            newItem.OnQuestEndAbility = ConsolationEndQuest;
            newItem.AbilityText += ConsolationDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Cursed) {
            newItem.OnQuestStartAbility = CursedStartQuest;
            newItem.OnQuestEndAbility = CursedEndQuest;
            newItem.AbilityText += CursedDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.DiverseInventory) {
            newItem.OnQuestStartAbility = DiverseInventoryStartQuest;
            newItem.OnQuestEndAbility = DiverseInventoryEndQuest;
            newItem.AbilityText += DiverseInventoryDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.EmptyHanded) {
            newItem.OnQuestStartAbility = EmptyHandedStartQuest;
            newItem.OnQuestEndAbility = EmptyHandedEndQuest;
            newItem.AbilityText += EmptyHandedDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Leveled) {
            newItem.OnQuestStartAbility = LeveledStartQuest;
            newItem.OnQuestEndAbility = LeveledEndQuest;
            newItem.AbilityText += LeveledDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.MultiplyBase) {
            newItem.OnQuestStartAbility = MultiplyBaseStartQuest;
            newItem.OnQuestEndAbility = MultiplyBaseEndQuest;
            newItem.AbilityText += MultiplyBaseDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Powerhouse) {
            newItem.OnQuestStartAbility = PowerhouseStartQuest;
            newItem.OnQuestEndAbility = PowerhouseEndQuest;
            newItem.AbilityText += PowerhouseDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Rally) {
            newItem.OnQuestStartAbility = RallyStartQuest;
            newItem.OnQuestEndAbility = RallyEndQuest;
            newItem.AbilityText += RallyDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Random) {
            newItem.OnQuestStartAbility = RandomStartQuest;
            newItem.OnQuestEndAbility = RandomEndQuest;
            newItem.AbilityText += RandomDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.RepeatBonus) {
            newItem.OnQuestStartAbility = RepeatBonusStartQuest;
            newItem.OnQuestEndAbility = RepeatBonusEndQuest;
            newItem.AbilityText += RepeatBonusDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Reroll) {
            newItem.OnQuestStartAbility = DoNothing;
            newItem.OnQuestEndAbility = RerollEndQuest;
            newItem.AbilityText += RerollDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Swap) {
            newItem.OnQuestStartAbility = SwapStartQuest;
            newItem.OnQuestEndAbility = SwapEndQuest;
            newItem.AbilityText += SwapDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Tidal) {
            newItem.OnQuestStartAbility = TidalStartQuest;
            newItem.OnQuestEndAbility = TidalEndQuest;
            newItem.AbilityText += TidalDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Wealthy) {
            newItem.OnQuestStartAbility = DoNothing;
            newItem.OnQuestEndAbility = WealthyEndQuest;
            newItem.AbilityText += WealthyDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else if(Ability == ItemAbilityReference.Worn) {
            newItem.OnQuestStartAbility = DoNothing;
            newItem.OnQuestEndAbility = WornEndQuest;
            newItem.AbilityText += WornDeclareText(newItem, newItem.AbilityAffectedStats);
        }
        else {
            newItem.OnQuestEndAbility = DoNothing;
            newItem.OnQuestStartAbility = DoNothing;
        }
    }
    public Item PotionGeneration(int itemLevel) {
        Item newItem = Instantiate(ItemPrefab, Vector3.zero, Quaternion.identity);
        GameObject newObj = newItem.gameObject;
        Destroy(newItem);
        Item newPotion = newObj.AddComponent<ItemPotion>();
        List<PotionTraits> lvlPotions = new List<PotionTraits>();
        foreach(PotionTraits item in Potions) {
            if(item.Level == itemLevel) {
                lvlPotions.Add(item);
            }
        }
        PotionTraits rnd = lvlPotions[Random.Range(0, lvlPotions.Count)];
        Dictionary<StatType, int> modifiers = new Dictionary<StatType, int>();
        modifiers.Add(rnd.PotionType, 0);
        string PotionAbilityText = DeclareText(ItemType.Potion, rnd.Level, rnd.PotionType, rnd.IsHealthPotion);
        newPotion.DeclareTraits(modifiers, rnd.ItemName, rnd.Type, rnd.Level, CalculateBasePrice(rnd), PotionAbilityText, rnd.ItemSprite);
        //The potion's special abilities are inherent to the PotionType, so they are actually in the RunQuest function
        newPotion.OnQuestStartAbility = DoNothing;
        newPotion.OnQuestEndAbility = DoNothing;
        newPotion.AbilityAffectedStats = new List<StatType>();
        newPotion.AbilityAffectedStats.Add(rnd.PotionType);
        return newPotion;
    }


    //The declare text for potions specifically
    public string DeclareText(ItemType myType, int lvl, StatType statModifier, bool modifiesHealth) {
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
    //String used for specific items
    public string DeclareText(SpecificTraits myTrait) {
        string myDescription = "";
        for(int i = 0; i < myTrait.PrimaryModifierValue.Count; i++) {
            //Only shows the stat if its not equal to 0
            if(myTrait.PrimaryModifierValue[i] != 0) {
                myDescription += "+" + myTrait.PrimaryModifierValue[i].ToString() + " " + myTrait.PrimaryModifier[i].ToString() + ", ";
            }
        }
        for(int i = 0; i < myTrait.SecondaryModifierValue.Count; i++) {
            if(myTrait.SecondaryModifierValue[i] != 0) {
                myDescription += "+" + myTrait.SecondaryModifierValue[i].ToString() + " " + myTrait.SecondaryModifier[i].ToString() + ", ";
            }
        }
        char[] removeChar = {',', ' '};
        myDescription = myDescription.TrimEnd(removeChar);
        myDescription += ". ";
        return myDescription;
    }
    //This calculates the Price of an item, given its traits
        //Generates price differently for normal items and for Potions
    private const int BASE_PRICE_PER_LEVEL = 5;
    private const float PRICE_PER_MODIFIER = 3f;
    private const int PRICE_PER_LEVEL_ABILITY = 2;
    private int CalculateBasePrice(SpecificTraits itemTraits) {
        int price = itemTraits.Level * BASE_PRICE_PER_LEVEL;
        float modIncrease = 0;
        foreach(int val in itemTraits.PrimaryModifierValue) {
            modIncrease += val * PRICE_PER_MODIFIER;
        }
        foreach(int val in itemTraits.SecondaryModifierValue) {
            modIncrease += val * PRICE_PER_MODIFIER;
        }
        price += Mathf.CeilToInt(modIncrease);
        //Further increases cost if it has an ability
        if(itemTraits.Ability != ItemAbilityReference.None) {
            price += PRICE_PER_LEVEL_ABILITY * itemTraits.Level;
        }
        return price;
    }
    private const int POTION_PRICE_PER_LEVEL = 10;
    private int CalculateBasePrice(PotionTraits itemTraits) {
        int price = itemTraits.Level * POTION_PRICE_PER_LEVEL;
        return price;
    }
    
    //This is for when an item does not have a special ability at a certain trigger time
    public void DoNothing(Item self, List<StatType> statsAffected) {

    }
    //Each modular ability has 3 functions:
        //1) a startQuest ability, which triggers when its equipped character starts a quest
        //2) a endQuest ability, which triggers when its equipped character ends a quest
        //3) a description ability, which declares an item with this ability's description text
    //This is for "Bloodied" Stat Increases - stat increases that occur when the character is not at full health
        //Increases by 1/2 Item's level, rounding down
    public void BloodiedStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        if(myChar.curHealth < myChar.baseHealth) {
            foreach(StatType stat in statsAffected) {
                myChar.StatModifier[stat] += Mathf.FloorToInt(self.Level / 2f);
                string tempString = "Due to not being at full health, " + myChar.CharacterName + " got +" + Mathf.FloorToInt(self.Level / 2f).ToString() + " to " + stat.ToString() + " from their " + self.ItemName + ".";
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
            self.abilityActive = true;
            //Text added to Log
            
        }
        else {
            self.abilityActive = false;
        }
    }
    public void BloodiedEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        if(self.abilityActive) {
            foreach(StatType stat in statsAffected) {
                myChar.StatModifier[stat] -= Mathf.FloorToInt(self.Level / 2f);
            }
        }
    }
    public string BloodiedDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Gives +" + Mathf.FloorToInt(self.Level / 2f).ToString() + " to ";
        foreach(StatType stat in statsAffected) {
            newString += stat.ToString() + ", ";
        }
        newString = newString.TrimEnd(',', ' ');
        newString += " while the character carrying this is not at full health";
        return newString;
    }
    
    //"BorrowStat" Ability - Increases stat if it is lower than another party member's by an amount relative to that party member's
    public void BorrowStatStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        StatType stat = statsAffected[0];
        //Checks to see if the stat can be increased, then if it can, it increases it
        int myStat = myChar.Stat[stat];
        int curVal = myChar.Stat[stat];
        foreach(Character chara in myChar.refQuest.myParty.Members) {
            if(chara.Stat[stat] > curVal) {
                curVal = chara.Stat[stat];
            }
        }
        if(myStat < curVal) {
            myChar.StatModifier[stat] += Mathf.CeilToInt((curVal - myStat) / 2f);
            self.abilityValue = Mathf.CeilToInt((curVal - myStat) / 2f);
            self.abilityActive = true;
        }
        else {
            self.abilityActive = false;
        }
    }
    public void BorrowStatEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        if(self.abilityActive) {
            myChar.StatModifier[statsAffected[0]] -= self.abilityValue;
        }
    }
    public string BorrowStatDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Boosts " + statsAffected[0].ToString() + " if another character in the party has a higher value for that stat.";
        return newString;
    }

    //"Butter" ability - character will drop the item if it seems likely that they will die on a quest, making it an item that is hard to lose
    //Doesn't have a startQuest ability
    public void ButterEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        //If the character is at 0 or less hp, they drop the item
        if(myChar.curHealth <= 0) {
            self.Unequip();
            string tempString = myChar.CharacterName + " dropped their " + self.ItemName + ", because they thought it was likely they would die.";
            myChar.refQuest.QuestOccurences.Add(tempString);
        }
    }
    public string ButterDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "This item will be dropped into the general inventory during a quest if it seems likely its carrier will die, guaranteeing that you won't lose it.";
        return newString;
    }

    //"Challenge" ability - stat increases if the player is going on a quest that is a higher level then them
        //Increase depends on level difference, plus itemLevel
    public void ChallengeStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        if(myChar.Level < myChar.refQuest.Level) {
            int tempValue = Mathf.CeilToInt((myChar.refQuest.Level - myChar.Level + self.Level) / 2f);
            self.abilityValue = tempValue;
            self.abilityActive = true;
            foreach(StatType stat in statsAffected) {
                if(stat != StatType.Endurance) {
                    myChar.StatModifier[stat] +=tempValue;
                    string tempString = myChar.CharacterName + " got a bonus to " + stat.ToString() + " from their " + self.ItemName + " for participating in a difficult quest.";
                    myChar.refQuest.QuestOccurences.Add(tempString);
                }
            }
        }
        else {
            self.abilityActive = false;
        }
    }
    public void ChallengeEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        if(self.abilityActive) {
            foreach(StatType stat in statsAffected) {
                if(stat != StatType.Endurance) {
                    myChar.StatModifier[stat] -=self.abilityValue;
                }
                else {
                    //heals an amount equal to the level difference
                    myChar.curHealth += (myChar.refQuest.Level - myChar.Level);
                    string tempString = myChar.CharacterName + "'s " + self.ItemName + " healed them for participating in a difficult quest.";
                    myChar.refQuest.QuestOccurences.Add(tempString);
                }
            }
        }
    }
    public string ChallengeDeclareText(Item self, List<StatType> statsAffected) {
        string newString ="Gives a bonus to ";
        foreach(StatType stat in statsAffected) {
            newString += stat.ToString() + "' ";
        }
        newString = newString.TrimEnd(',', ' ');
        newString += " when the equipped character goes on a higher level quest.";
        return newString;
    }

    //"Consolation" ability - boosts your lowest stat depending on Item level
    public void ConsolationStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        StatType lowestStat = StatType.Strength;
        int statValue = 150;
        foreach(var item in myChar.Stat) {
            //Cannot increase endurance
            if(item.Key != StatType.Endurance) {
                if(myChar.Stat[item.Key] + myChar.StatModifier[item.Key] < statValue) {
                    statValue = myChar.Stat[item.Key] + myChar.StatModifier[item.Key];
                    lowestStat = item.Key;
                }
            }
        }
        myChar.StatModifier[lowestStat] += self.Level;
        string tempString = "As " + lowestStat.ToString() + " was " + myChar.CharacterName + "'s lowest stat, " + self.ItemName + " gave it +" + self.Level.ToString() + " for this quest.";
        myChar.refQuest.QuestOccurences.Add(tempString);
        self.AbilityAffectedStats.Add(lowestStat);
    }
    public void ConsolationEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        StatType lowestStat = statsAffected[statsAffected.Count - 1];
        self.AbilityAffectedStats.RemoveAt(statsAffected.Count - 1);
        myChar.StatModifier[lowestStat] -= self.Level;
    }
    public string ConsolationDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Gives a bonus of " + self.Level.ToString() + " to a character's lowest stat.";
        return newString;
    }

    //"Cursed" Ability - Used on extra powerful items, but has a chance of being destroyed, depending on how much dmg the character takes
    public void CursedStartQuest(Item self, List<StatType> statsAffected) {
        //Do nothing except get current health of character
        self.abilityValue = self.EquippedCharacter.curHealth;
    }
    public void CursedEndQuest(Item self, List<StatType> statsAffected) {
        //Percent chance to stay existent is dependent on how much of their current health is remaining
        Character myChar = self.EquippedCharacter;
        float hpRatio = (float)myChar.curHealth / (float)self.abilityValue;
        float rndNum = Random.Range(0f, 1f);
        if(rndNum > hpRatio) {
            //After this, always a 50% chance
            float rnd = Random.Range(0f, 1f);
            if(rnd < 0.5f) {
                //Tells the world that it was destroyed
                string tempString = myChar.CharacterName = " took too much damage, and their " + self.ItemName + " was destroyed.";
                myChar.refQuest.QuestOccurences.Add(tempString);
                self.Unequip();
                self.myManager.UnequippedItems.Remove(self);
                Destroy(self.gameObject);
            }
        }
    }
    public string CursedDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "This item has a chance of being destroyed at the end of a quest if the equipped character takes too much damage.";
        return newString;
    }

    //"DiverseInventory" ability - gives a bonus to a stat, relative to how many different level items you have
    public void DiverseInventoryStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        List<int> inventoryLevels = new List<int>();
        foreach(Item item in myChar.Inventory) {
            if(!inventoryLevels.Contains(item.Level)) {
                inventoryLevels.Add(item.Level);
            }
        }
        self.abilityValue = inventoryLevels.Count;
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                myChar.StatModifier[stat] += Mathf.FloorToInt(inventoryLevels.Count * Mathf.Sqrt(self.Level) * 0.75f);
                string tempString = "Due to a diverse inventory, " + myChar.CharacterName + " gets +" + Mathf.FloorToInt(inventoryLevels.Count * Mathf.Sqrt(self.Level) * 0.75f).ToString() + " " + stat.ToString() + " from their " + self.ItemName + ".";
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
        }
    }
    public void DiverseInventoryEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                myChar.StatModifier[stat] -= Mathf.FloorToInt(self.abilityValue * Mathf.Sqrt(self.Level) * 0.75f);
            }
            else {
                myChar.curHealth += Mathf.FloorToInt(self.abilityValue * Mathf.Sqrt(self.Level) * 0.5f);
                string tempString = myChar.CharacterName + "'s " + self.ItemName + " healed " + Mathf.FloorToInt(self.abilityValue * Mathf.Sqrt(self.Level) * 0.5f).ToString() + " health due to their diverse inventory.";
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
        }
    }
    public string DiverseInventoryDeclareText(Item self, List<StatType> statsAffected) {
        string newString = " Gives a bonus to ";
        foreach(StatType stat in statsAffected) {
            newString += stat.ToString() + "' ";
        }
        newString = newString.TrimEnd(',', ' ');
        newString += " relative to the different levels of items its character carries.";
        return newString;
    }

    //"EmptyHanded" ability - Depending on the level of the item and how many empty inventory slots the character has, gives a better bonus
    public void EmptyHandedStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        int emptySlots = myChar.InventorySize - myChar.Inventory.Count;
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                myChar.StatModifier[stat] += Mathf.CeilToInt(emptySlots * self.Level / 3f);
                string tempString = "For having " + emptySlots.ToString() + " empty Inventory spots, " + self.ItemName + " gave " + myChar.CharacterName + " +" + Mathf.CeilToInt(emptySlots * self.Level / 3f).ToString() + " to their " + stat.ToString();
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
        }
    }
    public void EmptyHandedEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        int emptySlots = myChar.InventorySize - myChar.Inventory.Count;
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                myChar.StatModifier[stat] -= Mathf.CeilToInt(emptySlots * self.Level / 3f);
            }
            else {
                myChar.curHealth += Mathf.CeilToInt(emptySlots * self.Level / 4f);
                string tempString = "For having " + emptySlots.ToString() + " empty Inventory spots, " + self.ItemName + " healed " + myChar.CharacterName + " " + Mathf.CeilToInt(emptySlots * self.Level / 4f).ToString() + " health";
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
        }
    }
    public string EmptyHandedDeclareText(Item self, List<StatType> statsAffected) {
        string newString = " Gives a bonus to ";
        foreach(StatType stat in statsAffected) {
            newString += stat.ToString() + "' ";
        }
        newString = newString.TrimEnd(',', ' ');
        newString += " for having empty Inventory slots.";
        return newString;
    }

    //"Leveled" Ability - gives a bonus at the beginning of a quest relative to the character's level, rather than the item's level
    public void LeveledStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        self.abilityValue = myChar.Level;
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                myChar.StatModifier[stat] += self.abilityValue;
            }
        }
    }
    public void LeveledEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                myChar.StatModifier[stat] -= self.abilityValue;
            }
            else {
                myChar.curHealth += Mathf.FloorToInt(self.abilityValue / 2f);
            }
        }
    }
    public string LeveledDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Gives a bonus to the equipped character's ";
        foreach(StatType stat in statsAffected) {
            newString += stat.ToString() + "' ";
        }
        newString = newString.TrimEnd(',', ' ');
        newString += " relative to the character's level.";
        return newString;
    }

    //"MultiplyBase" Ability - gives a bonus to a stat relative to the character's base value fo that stat
    public void MultiplyBaseStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        myChar.StatModifier[statsAffected[0]] += Mathf.FloorToInt(myChar.Stat[statsAffected[0]] * 0.25f);
        self.abilityValue = Mathf.FloorToInt(myChar.Stat[statsAffected[0]] * 0.25f);
    }
    public void MultiplyBaseEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        myChar.StatModifier[statsAffected[0]] -= self.abilityValue;
    }
    public string MultiplyBaseDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Applies a bonus multiplier to the equipped character's base " + statsAffected[0].ToString() + ".";
        return newString;
    }

    //"Powerhouse" Ability - gives a bonus if the corresponding stat is the highest base of any character in the party
    public void PowerhouseStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        Character bestChar = myChar;
        int highestValue = 0;
        foreach(Character chara in myChar.refQuest.myParty.Members) {
            if(chara.Stat[statsAffected[0]] > highestValue) {
                highestValue = chara.Stat[statsAffected[0]];
                bestChar = chara;
            }
        }
        if(bestChar == myChar) {
            self.abilityActive = true;
            myChar.StatModifier[statsAffected[0]] += self.Level;
            string tempString = self.ItemName + " boosted " + myChar.CharacterName + " 's " + statsAffected[0].ToString() + " for being the highest in their party.";
            myChar.refQuest.QuestOccurences.Add(tempString);
        }
        else {
            self.abilityActive = false;
        }
    }
    public void PowerhouseEndQuest(Item self, List<StatType> statsAffected) {
        if(self.abilityActive) {
            self.EquippedCharacter.StatModifier[statsAffected[0]] -= self.Level;
        }
    }
    public string PowerhouseDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Grants a bonus to " + statsAffected[0].ToString() + " if it is the highest of those in their party.";
        return newString;
    }
    //This is for the "rally" ability. - stat increases applied to other characters in the party
        //Stat bonuses are the same as that of a character ability (1/3 the item level, rounded up)
    public void RallyStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        foreach(Character chara in myChar.refQuest.myParty.Members) {
            if(chara != myChar) {
                foreach(StatType stat in statsAffected) {
                    if(stat != StatType.Endurance) {
                        chara.StatModifier[stat] += Mathf.CeilToInt(self.Level / 3f);
                    }
                }
            }
        }
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                string tempString = myChar.CharacterName + " rallied their party using their " + self.ItemName + ", giving +" + Mathf.CeilToInt(self.Level / 3f).ToString() + " to their " + stat.ToString() + ".";
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
        }
    }
    public void RallyEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        foreach(Character chara in myChar.refQuest.myParty.Members) {
            if(chara != myChar) {
                foreach(StatType stat in statsAffected) {
                    if(stat != StatType.Endurance) {
                        chara.StatModifier[stat] -= Mathf.CeilToInt(self.Level / 3f);
                    }
                    else {
                        chara.curHealth += Mathf.CeilToInt(self.Level / 2f);
                    }
                }
            }
        }
        foreach(StatType stat in statsAffected) {
            if(stat == StatType.Endurance) {
                string tempString = myChar.CharacterName + " healed their party members using their " + self.ItemName + ".";
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
        }
    }
    public string RallyDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Rallies other characters in the party, boosting their ";
        foreach(StatType stat in statsAffected) {
            newString += stat.ToString() + "' ";
        }
        newString = newString.TrimEnd(',', ' ');
        newString += ".";
        return newString;
    }

    //"Random" ability - gives a boost to a random stat
    public void RandomStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        StatType rndStat = (StatType)Random.Range(0, StatType.GetNames(typeof(StatType)).Length);
        self.AbilityAffectedStats.Add(rndStat);
        if(rndStat != StatType.Endurance) {//As usual, Endurance serves as healing
            myChar.StatModifier[rndStat] += self.Level;
            string tempString = self.ItemName + " boosted " + myChar.CharacterName + "'s " + rndStat.ToString() + " by " + self.Level.ToString() + ".";
            myChar.refQuest.QuestOccurences.Add(tempString);
        }
    }
    public void RandomEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        StatType rndStat = statsAffected[statsAffected.Count - 1];
        self.AbilityAffectedStats.RemoveAt(statsAffected.Count - 1);
        if(rndStat != StatType.Endurance) {
            myChar.StatModifier[rndStat] -= self.Level;
        }
        else {
            myChar.curHealth += self.Level / 2;
            string tempString = self.ItemName + " healed " + myChar.CharacterName + " for " + (self.Level/2).ToString() + " health.";
            myChar.refQuest.QuestOccurences.Add(tempString);
        }
    }
    public string RandomDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Gives a +" + self.Level.ToString() + " bonus to a random stat during quests.";
        return newString;
    }

    //"RepeatBonus" Ability - Doubles the stat bonus given by other modifiers
    public void RepeatBonusStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        if(statsAffected[0] != StatType.Endurance) {
            self.abilityValue = myChar.StatModifier[statsAffected[0]];
            myChar.StatModifier[statsAffected[0]] += self.abilityValue;
            string tempString = "Bonus modifiers on " + myChar.CharacterName + "'s " + statsAffected[0].ToString() + " were doubled, due to their " + self.ItemName + ".";
            myChar.refQuest.QuestOccurences.Add(tempString);
        }
    }
    public void RepeatBonusEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        if(statsAffected[0] != StatType.Endurance) {
            myChar.StatModifier[statsAffected[0]] -= self.abilityValue;
        }
    }
    public string RepeatBonusDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Doubles the bonuses applied to the equipped character's " + statsAffected[0].ToString() +".";
        return newString;
    }

    //"Reroll" Ability - Rerolls into another item of the same type after each quest
    public List<SpecificTraits> RerollWeapon;
    public List<SpecificTraits> RerollArmor;
    public List<SpecificTraits> RerollShield;
    public List<SpecificTraits> RerollRing;
    //Does not have a startQuest ability
    public void RerollEndQuest(Item self, List<StatType> statsAffected) {
        SpecificTraits newTraits;
        //Rerolls into another item
        if(self.Type == ItemType.Weapon) {
            newTraits = RerollWeapon[Random.Range(0, RerollWeapon.Count)];
        }
        else if(self.Type == ItemType.Armor) {
            newTraits = RerollArmor[Random.Range(0, RerollArmor.Count)];
        }
        else if(self.Type == ItemType.Shield) {
            newTraits = RerollShield[Random.Range(0, RerollShield.Count)];
        }
        else if(self.Type == ItemType.Ring) {
            newTraits = RerollRing[Random.Range(0, RerollRing.Count)];
        }
        else {
            newTraits = RerollWeapon[0];
        }
        //Increases level if it needs to
        if(newTraits.Level < self.Level) {
            int itemLevel = self.Level;
            int baseLevel = newTraits.Level;
            for(int i = 0; i < newTraits.PrimaryModifierValue.Count; i++) {
                newTraits.PrimaryModifierValue[i] += (itemLevel - baseLevel);
            }
            //Secondary traits either get +1 every level or every few levels, depending on how many there are
            if(newTraits.SecondaryModifierValue.Count == 1) {
                newTraits.SecondaryModifierValue[0] += Mathf.CeilToInt(itemLevel - baseLevel);
            }
            else if(newTraits.SecondaryModifierValue.Count > 0) {
                for(int i = baseLevel;  i < itemLevel; i++) {
                    newTraits.SecondaryModifierValue[i % newTraits.SecondaryModifierValue.Count]++;
                }
            }
        }
        //Declares all the new traits of the item
        Dictionary<StatType, int> modifiers = new Dictionary<StatType, int>();
        for(int i = 0; i < newTraits.PrimaryModifier.Count; i++) {
            modifiers.Add(newTraits.PrimaryModifier[i], newTraits.PrimaryModifierValue[i]);
        }
        for(int i = 0; i < newTraits.SecondaryModifier.Count; i++) {
            modifiers.Add(newTraits.SecondaryModifier[i], newTraits.SecondaryModifierValue[i]);
        }
        string declaredText = DeclareText(newTraits);
        self.DeclareTraits(modifiers, newTraits.ItemName, self.Type, self.Level, 0, declaredText, newTraits.ItemSprite);
        self.AbilityText += RerollDeclareText(self, self.AbilityAffectedStats);
        UIManager.Instance.RefreshItemUI();
        UIManager.Instance.RefreshCharacterUI();
        self.EquippedCharacter.RefreshUI();
    }
    public string RerollDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "This item changes into a different Level " + self.Level.ToString() + " " + self.Type.ToString() + " after each quest.";
        return newString;
    }
    //"Swap" ability - reduces one stat, increases another stat by twice that much
        //Takes 1/4 of the one stat for the other
    public void SwapStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        //Only does anything if there are exactly 2 stats affected
        if(statsAffected.Count == 2) {
            int tempValue = Mathf.CeilToInt(myChar.Stat[statsAffected[0]] / 4f);
            myChar.StatModifier[statsAffected[0]] -= tempValue;
            myChar.StatModifier[statsAffected[1]] += 2 * tempValue;
            self.abilityValue = tempValue;
            string tempString = myChar.CharacterName + "'s " + self.ItemName + " boosted their " + statsAffected[1].ToString() + " at the cost of reducing their " + statsAffected[0].ToString() + ".";
        }
    }
    public void SwapEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        //Only does anything if there are exactly 2 stats affected
        if(statsAffected.Count == 2) {
            myChar.StatModifier[statsAffected[0]] += self.abilityValue;
            myChar.StatModifier[statsAffected[1]] -= 2 * self.abilityValue;
        }
    }
    public string SwapDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Increases the equipped character's " + statsAffected[1].ToString() + " but reduces their " + statsAffected[0].ToString() + " by half as much.";
        return newString;
    }

    //"Tidal" ability - Bonus ability, increases over multiple quests, then drops
    private const int TIDAL_MODULO = 6;
    public void TidalStartQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                myChar.StatModifier[stat] += (self.Level - 3 + self.abilityValue);
                string tempString = myChar.CharacterName + " got a bonus of +" + (self.Level - 2 + self.abilityValue).ToString() + " " + stat.ToString() + " from their " + self.ItemName +".";
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
        }
    }
    public void TidalEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        foreach(StatType stat in statsAffected) {
            if(stat != StatType.Endurance) {
                myChar.StatModifier[stat] -= (self.Level - 3 + self.abilityValue);
            }
            else {
                myChar.curHealth += Mathf.FloorToInt((self.Level - 3 + self.abilityValue) / 2f);
                string tempString = myChar.CharacterName + " was healed " + Mathf.FloorToInt((self.Level - 2 + self.abilityValue) / 2f).ToString() + " health by their " + self.ItemName + ".";
                myChar.refQuest.QuestOccurences.Add(tempString);
            }
        }
        //Tidal level increases by one
        self.abilityValue = (self.abilityValue + 1) % TIDAL_MODULO;
    }
    public string TidalDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Gives an additional bonus to ";
        foreach(StatType stat in statsAffected) {
            newString += stat.ToString() + "' ";
        }
        newString = newString.TrimEnd(',', ' ');
        newString += "that increases/decreases from quest to quest.";
        return newString;
    }

    //"Wealthy" Ability - Grants an additional gold bonus relative to the affected stats
        //Does not have a start quest ability
    public void WealthyEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        foreach(StatType stat in statsAffected) {
            myChar.refQuest.myParty.LuckGoldReward += Mathf.CeilToInt((myChar.Stat[stat] + myChar.StatModifier[stat]) / 2f);
            string tempString = myChar.CharacterName + " found " + Mathf.CeilToInt((myChar.Stat[stat] + myChar.StatModifier[stat]) / 2f).ToString() + " gold due to their " + self.ItemName + ".";
            myChar.refQuest.QuestOccurences.Add(tempString);
        }
    }
    public string WealthyDeclareText(Item self, List<StatType> statsAffected) {
        string newString = "Grants a gold reward during quests relative to the equipped character's ";
        foreach(StatType stat in statsAffected) {
            newString += stat.ToString() + "' ";
        }
        newString = newString.TrimEnd(',', ' ');
        newString += ".";
        return newString;
    }

    //"Worn" Ability - Starts extra powerful, but loses a level every time it is used on a quest
    //Doesn't have a start quest ability
    public void WornEndQuest(Item self, List<StatType> statsAffected) {
        Character myChar = self.EquippedCharacter;
        //Only loses a level if it is at a level higher than level 1
        if(self.Level > 1) {
            foreach(var mod in self.Modifier) {
                if(mod.Value > 1) {
                    self.Modifier[mod.Key]--;
                    myChar.StatModifier[mod.Key]--;
                }
            }
            self.Level--;
            string tempString = myChar.CharacterName + "'s " + self.ItemName + " got worn down, and now is a level " + self.Level.ToString() + " item.";
            myChar.refQuest.QuestOccurences.Add(tempString);
            string myDescription ="";
            foreach(var item in self.Modifier) {
                myDescription += ("+" + item.Value.ToString() + " " + item.Key.ToString() + ", ");
            }
            char[] removeChar = {',', ' '};
            myDescription = myDescription.TrimEnd(removeChar);
            myDescription += ". ";
            myDescription += WornDeclareText(self, statsAffected);
            self.AbilityText = myDescription;
        }
    }
    public string WornDeclareText(Item self, List<StatType> statsAffected) {
        if(self.Level > 1) {
            string newString = "Loses a level everytime it is used in a quest.";
            return newString;
        }
        else {
            return "";
        }
    }
}
