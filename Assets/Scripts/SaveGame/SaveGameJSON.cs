using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
//This script is used to manage saving the game
[System.Serializable]
public class SaveFile {//This serializable class is the class which is put in a JSON save file
    public List<CharacterSave> myCharacters;
    public List<ItemSave> myUnequippedItems;
    public List<ItemSave> myShopItems;
    public List<QuestSave> myQuests;
}
[System.Serializable] public struct CharacterSave {//Just saves all the character information in a simpler serializable form
    public string CharacterName;
    public int Level;
    public int maxHP;
    public int curHP;
    public SerializableStatDictionary Stats;
    public SerializableStatDictionary StatModifiers;
    public float xp;
    public List<ItemSave> Inventory;
    //Special ability
    public AbilityReference Ability;
    public List<StatType> AbilityStats;
    public string AbilityDisplayText;
}
[System.Serializable] public struct ItemSave {//Saves Item info in an easily readible form
    public string ItemName;
    public ItemType Type;
    public int Level;
    public int Price;
    public SerializableStatDictionary Modifiers;
    public ItemAbilityReference AbilityReference;
    public List<StatType> AbilityStats;
    public string AbilityDescription;
    public Sprite sprite;

}
[System.Serializable] public struct QuestSave {//Saves Quest info in a more easily accessible manner
    public string Title;
    public int PartySize;
    public int Level;
    public int GoldReward;
    public List<ItemSave> ItemRewards;
    //Does not include any characters on the quest, bc save must occur before that
}
public class SaveGameJSON : MonoBehaviour
{
    public void CreateSaveFile() {
        SaveFile newFile = new SaveFile();
        newFile.myCharacters = new List<CharacterSave>();
        newFile.myShopItems = new List<ItemSave>();
        newFile.myUnequippedItems = new List<ItemSave>();
        newFile.myQuests = new List<QuestSave>();
        UIManager Manager = UIManager.Instance;
        foreach(Character chara in Manager.allCharacters) {
            if(chara.gameObject.activeInHierarchy && chara.alive) {
                newFile.myCharacters.Add(CreateCharacterSave(chara));
            }
        }
        foreach(Item item in Manager.GeneralItemManager.UnequippedItems) {
            newFile.myUnequippedItems.Add(CreateItemSave(item));
        }
        foreach(Item item in Manager.GeneralItemManager.ShopItems) {
            newFile.myShopItems.Add(CreateItemSave(item));
        }
        foreach(QuestReference slot in Manager.GeneralQuestManager.QuestSlots) {
            if(slot.Reference.exists) {
                newFile.myQuests.Add(CreateQuestSave(slot.Reference));
            }
        }
        //After adding everything to this new Save File, saves the file through JSON
        string JSONConversion = JsonUtility.ToJson(newFile);
        WriteToFile(JSONConversion, "mySave");

    }
    public void LoadSaveFile() {
        string JSONString = ReadFromFile("mySave");
        SaveFile loadedFile = JsonUtility.FromJson<SaveFile>(JSONString);
        //Goes through each element of the loaded file, overwriting existing info
        UIManager Manager = UIManager.Instance;
        foreach(Character chara in Manager.allCharacters) {
            chara.gameObject.SetActive(false);
        }
        for(int i = 0; i < loadedFile.myCharacters.Count; i++) {
            LoadCharacterSave(Manager.allCharacters[i], loadedFile.myCharacters[i]);
        }
        Manager.GeneralItemManager.UnequippedItems = new List<Item>();
        foreach(ItemSave item in loadedFile.myUnequippedItems) {
            Item newItem = LoadItemSave(item);
            Manager.GeneralItemManager.UnequippedItems.Add(newItem);
        }
        Manager.GeneralItemManager.ShopItems = new List<Item>();
        foreach(ItemSave item in loadedFile.myShopItems) {
            Item newItem = LoadItemSave(item);
            Manager.GeneralItemManager.ShopItems.Add(newItem);
        }
        foreach(QuestReference questRef in Manager.GeneralQuestManager.QuestSlots) {
            questRef.gameObject.SetActive(false);
        }
        for(int i = 0; i < loadedFile.myQuests.Count; i++) {
            LoadQuestSave(loadedFile.myQuests[i], Manager.GeneralQuestManager.QuestSlots[i]);
        }
        //Then, everything needs to get refreshed

    }
    //Function for writing the JSON string to the save file - Used the code from Class 10 - Save Files; Slide 28
    private void WriteToFile(string toWrite, string filename) {
        StreamWriter writer = new StreamWriter(Application.persistentDataPath + "\\" + filename, false);
        writer.WriteLine(toWrite);
        writer.Close();
    }
    private string ReadFromFile(string filename) {
        StreamReader reader = new StreamReader(Application.persistentDataPath + "\\" + filename, true);
        string readString = reader.ReadToEnd();
        reader.Close();
        return readString;
    }
    public CharacterSave CreateCharacterSave(Character chosenCharacter) {
        CharacterSave nSave = new CharacterSave();
        nSave.CharacterName = chosenCharacter.CharacterName;
        nSave.Level = chosenCharacter.Level;
        nSave.maxHP = chosenCharacter.baseHealth;
        nSave.curHP = chosenCharacter.curHealth;
        nSave.Stats = chosenCharacter.Stat;
        nSave.StatModifiers = chosenCharacter.StatModifier;
        nSave.xp = chosenCharacter.XP;
        nSave.Ability = chosenCharacter.myAbilityReference;
        nSave.AbilityStats = new List<StatType>(chosenCharacter.AbilityAffectedStats);
        nSave.AbilityDisplayText = chosenCharacter.AbilityString;
        nSave.Inventory = new List<ItemSave>();
        foreach(Item item in chosenCharacter.Inventory) {
            nSave.Inventory.Add(CreateItemSave(item));
        }
        return nSave;
    }
    public void LoadCharacterSave(Character tempChar, CharacterSave overWriteChar) {
        tempChar.gameObject.SetActive(true);
        tempChar.CharacterName = overWriteChar.CharacterName;
        tempChar.Level = overWriteChar.Level;
        tempChar.baseHealth = overWriteChar.maxHP;
        tempChar.curHealth = overWriteChar.curHP;
        tempChar.Stat = overWriteChar.Stats;
        tempChar.StatModifier = overWriteChar.StatModifiers;
        tempChar.XP = overWriteChar.xp;
        tempChar.myAbilityReference = overWriteChar.Ability;
        //Has to use the CharacterGeneration delegates to give ability
        UIManager.Instance.GenerateCharacter.DeclareCharAbility(tempChar, overWriteChar.Ability);
        tempChar.AbilityAffectedStats = overWriteChar.AbilityStats;
        tempChar.AbilityString = overWriteChar.AbilityDisplayText;
        //Fills inventory, by using Loading Item Save
        tempChar.Inventory = new List<Item>();
        foreach(ItemSave item in overWriteChar.Inventory) {
            Item newItem = LoadItemSave(item);
            tempChar.Inventory.Add(newItem);
        }
    }
    public ItemSave CreateItemSave(Item item) {
        ItemSave nSave = new ItemSave();
        nSave.ItemName = item.ItemName;
        nSave.Type = item.Type;
        nSave.Level = item.Level;
        nSave.Price = item.Price;
        nSave.Modifiers = item.Modifier;
        nSave.AbilityReference = item.AbilityReference;
        nSave.AbilityStats = item.AbilityAffectedStats;
        nSave.AbilityDescription = item.AbilityText;
        nSave.sprite = item.Sprite;
        return nSave;
    }
    public Item LoadItemSave(ItemSave oItem) {
        Item newItem = Instantiate(UIManager.Instance.GenerateItem.ItemPrefab, Vector3.zero, Quaternion.identity);
        newItem.DeclareTraits(oItem.Modifiers, oItem.ItemName, oItem.Type, oItem.Level, oItem.Price, oItem.AbilityDescription, oItem.sprite);
        UIManager.Instance.GenerateItem.DeclareItemAbility(newItem, oItem.AbilityReference);
        newItem.AbilityAffectedStats = oItem.AbilityStats;
        return newItem;
    }
    public QuestSave CreateQuestSave(Quest quest) {
        QuestSave nSave = new QuestSave();
        nSave.Title = quest.Title;
        nSave.PartySize = quest.partySize;
        nSave.Level = quest.Level;
        nSave.GoldReward = quest.goldReward;
        nSave.ItemRewards = new List<ItemSave>();
        foreach(Item item in quest.ItemReward) {
            nSave.ItemRewards.Add(CreateItemSave(item));
        }
        return nSave;
    }
    public void LoadQuestSave(QuestSave quest, QuestReference reference) {
        Quest nQuest = new Quest();
        nQuest.exists = true;
        nQuest.Title = quest.Title;
        nQuest.partySize = quest.PartySize;
        nQuest.Level = quest.Level;
        nQuest.goldReward = quest.GoldReward;
        nQuest.ItemReward = new List<Item>();
        foreach(ItemSave item in quest.ItemRewards) {
            nQuest.ItemReward.Add(LoadItemSave(item));
        }
        Party tempParty = new Party();
        tempParty.Members = new List<Character>();
        tempParty.PartyPower = 0;
        tempParty.PowerTarget = new List<float>();
        tempParty.PartyLuck = 0;
        tempParty.LuckItemReward = new List<Item>();
        tempParty.LuckGoldReward = 0;
        nQuest.myParty = tempParty;
        nQuest.QuestOccurences = new List<string>();
        nQuest.PercentQuestComplete = 0;
        reference.Reference = nQuest;
    }

}

//Based this on the serializable Quaternion/Vector3 provided in class from https://answers.unity.com/questions/956047/serialize-quaternion-or-vector3.html
    // By Unity forum user Cherno
[System.Serializable] public struct SerializableStatDictionary {//Turns a stat dictionary into a more serializable form
    public int Str;
    public int Mag;
    public int Def;
    public int End;
    public int Luck;

    public SerializableStatDictionary(int nStr, int nMag, int nDef, int nEnd, int nLuck) {
        Str = nStr;
        Mag = nMag;
        Def = nDef;
        End = nEnd;
        Luck = nLuck;
    }

    public static implicit operator Dictionary<StatType, int>(SerializableStatDictionary nValue) {
        Dictionary<StatType, int> nDictionary = new Dictionary<StatType, int>();
        nDictionary.Add(StatType.Strength, nValue.Str);
        nDictionary.Add(StatType.Magic, nValue.Mag);
        nDictionary.Add(StatType.Defense, nValue.Def);
        nDictionary.Add(StatType.Endurance, nValue.End);
        nDictionary.Add(StatType.Luck, nValue.Luck);
        return nDictionary;
    }

    public static implicit operator SerializableStatDictionary(Dictionary<StatType, int> nDict) {
        SerializableStatDictionary nSeries;
        //Has to check for each element to see if it exists
        if(nDict.ContainsKey(StatType.Strength)) {
            nSeries.Str = nDict[StatType.Strength];
        }
        else {
            nSeries.Str = 0;
        }
        if(nDict.ContainsKey(StatType.Magic)) {
            nSeries.Mag = nDict[StatType.Magic];
        }
        else {
            nSeries.Mag = 0;
        }
        if(nDict.ContainsKey(StatType.Defense)) {
            nSeries.Def = nDict[StatType.Defense];
        }
        else {
            nSeries.Def = 0;
        }
        if(nDict.ContainsKey(StatType.Endurance)) {
            nSeries.End = nDict[StatType.Endurance];
        }
        else {
            nSeries.End = 0;
        }
        
        if(nDict.ContainsKey(StatType.Luck)) {
            nSeries.Luck = nDict[StatType.Luck];
        }
        else {
            nSeries.Luck = 0;
        }
        return nSeries;
    }
}
