using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
//This script is used to manage saving the game
[System.Serializable]
public class SaveFile {//This serializable class is the class which is put in a JSON save file
    public string FileName;//Name of the File
    public List<CharacterSave> myCharacters;
    public List<ItemSave> myUnequippedItems;
    public List<ItemSave> myShopItems;
    public List<QuestSave> myQuests;
    public int Gold;
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
[System.Serializable] public struct SaveList {//Saves the existing save files as a list of strings, so it can be loaded when the game is opened to edit them
    public List<string> FileNames;//Name of the file 
    public List<int> NumChars;//How many characters are in the corresponding save file
    public List<int> HighestLevel;//The highest level of a character in the save file
}
public class SaveGameJSON : MonoBehaviour
{
    private const string BASE_SAVE_FILE_NAME = "~Saves";//Name of the save file which contains reference to other save files
    //Create a new Save File
    public void CreateSaveFile(string filename) {
        //Only saves it if the save file is not the same as the BASE_SAVE_FILE_NAME
        if(filename != BASE_SAVE_FILE_NAME) {
            SaveFile newFile = new SaveFile();
            newFile.FileName = filename;
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
            newFile.Gold = Manager.CurrentGold;
            //After adding everything to this new Save File, saves the file through JSON
            //File is saved to the list of saves
            SaveDeleteFile(newFile, false);
            string JSONConversion = JsonUtility.ToJson(newFile);
            WriteToFile(JSONConversion, filename);
        }

    }
    //Load an existing save File
    public void LoadSaveFile(string filename) {
        if(filename != BASE_SAVE_FILE_NAME) {
            string JSONString = ReadFromFile(filename);
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
                newItem.InShop = true;
                Manager.GeneralItemManager.ShopItems.Add(newItem);
            }
            foreach(QuestReference questRef in Manager.GeneralQuestManager.QuestSlots) {
                questRef.gameObject.SetActive(false);
            }
            for(int i = 0; i < loadedFile.myQuests.Count; i++) {
                LoadQuestSave(loadedFile.myQuests[i], Manager.GeneralQuestManager.QuestSlots[i]);
                Manager.GeneralQuestManager.activeQuests++;
            }
            Manager.CurrentGold = loadedFile.Gold;
            //Everything gets refreshed as part of the Save Management script
            
        }
    }
    //Additional Save File that saves existing files
        //Is opened, then writes/overwrites files
            //The second bool tells you whether the file is being deleted or added
    public void SaveDeleteFile(SaveFile newFile, bool delete) {
        string readFile = ReadFromFile(BASE_SAVE_FILE_NAME);
        SaveList baseFile = JsonUtility.FromJson<SaveList>(readFile);
        //If it is deleting, it does something different than otherwise
        if(delete) {
            if(baseFile.FileNames.Contains(newFile.FileName)) {
                //Deletes the file if it exists
                File.Delete(Application.persistentDataPath + "\\" + newFile.FileName);
                int pos = baseFile.FileNames.IndexOf(newFile.FileName);
                baseFile.FileNames.RemoveAt(pos);
                baseFile.NumChars.RemoveAt(pos);
                baseFile.HighestLevel.RemoveAt(pos);
            }
        }
        else {
            if(baseFile.FileNames.Contains(newFile.FileName)) {
                //If the save file exists, it overwrites it (just re-adds it to the end of the list at the moment)
                int pos = baseFile.FileNames.IndexOf(newFile.FileName);
                baseFile.FileNames.RemoveAt(pos);
                baseFile.NumChars.RemoveAt(pos);
                baseFile.HighestLevel.RemoveAt(pos);
                baseFile.FileNames.Add(newFile.FileName);
                baseFile.NumChars.Add(newFile.myCharacters.Count);
                int highestLvl = 0;
                foreach(CharacterSave chara in newFile.myCharacters) {
                    if(chara.Level > highestLvl) {
                        highestLvl = chara.Level;
                    }
                }
                baseFile.HighestLevel.Add(highestLvl);

            }
            else {
                //Otherwise, it creates a new savefile.
                baseFile.FileNames.Add(newFile.FileName);
                baseFile.NumChars.Add(newFile.myCharacters.Count);
                int highestLvl = 0;
                foreach(CharacterSave chara in newFile.myCharacters) {
                    if(chara.Level > highestLvl) {
                        highestLvl = chara.Level;
                    }
                }
                baseFile.HighestLevel.Add(highestLvl);
            }
        }
        //Then, writes the save to the file
        string writeFile = JsonUtility.ToJson(baseFile);
        WriteToFile(writeFile, BASE_SAVE_FILE_NAME);
    }
    //Function for loading the existing SaveManagerFile
    public SaveList LoadExistingSaves() {
        string loadString = ReadFromFile(BASE_SAVE_FILE_NAME);
        SaveList myList = JsonUtility.FromJson<SaveList>(loadString);
        return myList;
    }
    //Checks to see if the Existing saves file exists, and if it doesn't, it creates it
    public void CreateExistingSavesFile() {
        if(!File.Exists(Application.persistentDataPath + "\\" + BASE_SAVE_FILE_NAME)) {
            SaveList newList = new SaveList();
            newList.FileNames = new List<string>();
            newList.HighestLevel = new List<int>();
            newList.NumChars = new List<int>();
            string ListJSON = JsonUtility.ToJson(newList);
            WriteToFile(ListJSON, BASE_SAVE_FILE_NAME);
        }
    }
    //Function for writing the JSON string to the save file - Used the code from Class 10 - Save Files; Slide 28
    public void WriteToFile(string toWrite, string filename) {
        StreamWriter writer = new StreamWriter(Application.persistentDataPath + "\\" + filename, false);
        writer.WriteLine(toWrite);
        writer.Close();
    }
    public string ReadFromFile(string filename) {
        StreamReader reader = new StreamReader(Application.persistentDataPath + "\\" + filename, true);
        string readString = reader.ReadToEnd();
        reader.Close();
        return readString;
    }
    //Each of these following functions writes to/loads from a saveable format, CHaracters, Items, and Quests
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
        tempChar.alive = true;
        //Has to use the CharacterGeneration delegates to give ability
        UIManager.Instance.GenerateCharacter.DeclareCharAbility(tempChar, overWriteChar.Ability);
        tempChar.AbilityAffectedStats = overWriteChar.AbilityStats;
        tempChar.AbilityString = overWriteChar.AbilityDisplayText;
        //Fills inventory, by using Loading Item Save
        tempChar.Inventory = new List<Item>();
        foreach(ItemSave item in overWriteChar.Inventory) {
            Item newItem = LoadItemSave(item);
            tempChar.Inventory.Add(newItem);
            newItem.EquippedCharacter = tempChar;
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
        if(oItem.Type == ItemType.Potion) {
            GameObject newObj = newItem.gameObject;
            Destroy(newItem);
            Item newPotion = newObj.AddComponent<ItemPotion>();
            if(oItem.AbilityStats[0] == StatType.Endurance) {
                string PotionAbilityText = UIManager.Instance.GenerateItem.DeclareText(ItemType.Potion, oItem.Level, oItem.AbilityStats[0], true);
                newPotion.DeclareTraits(oItem.Modifiers, oItem.ItemName, oItem.Type, oItem.Level, oItem.Price, PotionAbilityText, oItem.sprite);
            }
            else {
                string PotionAbilityText = UIManager.Instance.GenerateItem.DeclareText(ItemType.Potion, oItem.Level, oItem.AbilityStats[0], false);
                newPotion.DeclareTraits(oItem.Modifiers, oItem.ItemName, oItem.Type, oItem.Level, oItem.Price, PotionAbilityText, oItem.sprite);
            }
            newPotion.OnQuestStartAbility = UIManager.Instance.GenerateItem.DoNothing;
            newPotion.OnQuestEndAbility = UIManager.Instance.GenerateItem.DoNothing;
            newPotion.AbilityAffectedStats = new List<StatType>();
            newPotion.AbilityAffectedStats.Add(oItem.AbilityStats[0]);
            return newPotion;
        }
        else {
            newItem.DeclareTraits(oItem.Modifiers, oItem.ItemName, oItem.Type, oItem.Level, oItem.Price, oItem.AbilityDescription, oItem.sprite);
            //Has to declare the item text independently because of the weird way in which normal items have their text generated
            string myDescription = "";
            Dictionary<StatType, int> tempDictionary = oItem.Modifiers;
            foreach(var modifier in tempDictionary) {
                if(modifier.Value != 0) {
                    myDescription += "+" + modifier.Value.ToString() + " " + modifier.Key.ToString() + ", ";
                }
            }
            char[] removeChar = {',', ' '};
            myDescription = myDescription.TrimEnd(removeChar);
            myDescription += ". ";
            newItem.AbilityText = myDescription;
            newItem.AbilityAffectedStats = new List<StatType>(oItem.AbilityStats);
            newItem.AbilityReference = oItem.AbilityReference;
            UIManager.Instance.GenerateItem.DeclareItemAbility(newItem, oItem.AbilityReference);
            return newItem;
        }
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

//Serializable Dictionary (couldn't remember if dictionaries were fully save-able, and this was a quick thing to do - and actually may have uses in other places)
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
