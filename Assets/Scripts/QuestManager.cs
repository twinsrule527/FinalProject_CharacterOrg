using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
//Script which manages Quests as they are being used by the UI (works similarly to the ItemManager)

//Quests are rather simple, so while characters and items have whole classes, Quests just have structs + a reference class
[System.Serializable]
public struct Quest {
    public bool exists;//Whether the quest even exists, as a check for when clicking on quests
    public string Title;
    public int partySize;//How many characters should be in this quest's party
    public int Level;//The difficulty level of the quest
    public int goldReward;//Reward for Quest in Gold
    public List<Item> ItemReward;//Item Rewards
    public Party myParty;
    //Below are things which are not really determined until while the quest is being completed
    public float PercentQuestComplete;//How much of the quest the party manages to complete (1 = 100% - but it can go beyond that)
    public List<string> QuestOccurences;//A list of strings which describe the going-ons during the quest

}
//This struct manages a Party of characters who are going on an adventure
public struct Party {
    public List<Character> Members;
    public int PartyPower;//The total Strength + Magic Power of this Party
    public List<float> PowerTarget;//How the damage will be assigned back at the characters
    public int PartyLuck;//The Total Luck of the party
    public List<Item> LuckItemReward;//Reward Items the characters receive bc of luck
    public int LuckGoldReward;//Additional Gold reward received bc of Luck
}
public class QuestManager : MonoBehaviour
{
    //Has reference to the existing possible quests
    public List<QuestReference> QuestSlots;
    public int activeQuests;
    public const int STARTING_NUM_QUESTS = 2;
    void Awake() {
        activeQuests = 0;
        QuestReference[] tempReference = FindObjectsOfType<QuestReference>();
        QuestSlots = new List<QuestReference>();
        foreach(QuestReference reference in tempReference) {
            QuestSlots.Add(reference);
            reference.gameObject.SetActive(false);
        }
        //THIS IS CREATED TOO EARLY IN THE GAME
        /*List<Quest> newQuests = CreateNewQuestsUsingCurrent();
        for(int i = 0; i< newQuests.Count; i++) {
            Quest newQuest = newQuests[i];
            activeQuests++;
            QuestSlots[i].Reference = newQuest;
        }
        */
    }
    //Triggered by the RunQuest Button, activates all active quests
    public void RunAllActiveQuests() {
        for(int i = 0; i < activeQuests; i++) {
            //Each quest that has enough characters fires
            if(QuestSlots[i].Reference.partySize == QuestSlots[i].Reference.myParty.Members.Count) {
                RunQuest(ref QuestSlots[i].Reference);
            }
        }
        //Reduces number of active quests to 0
        activeQuests = 0;
        UIManager.Instance.SwitchItemQuestPage();
        foreach(Character chara in UIManager.Instance.allCharacters) {
            chara.inParty = false;
        }

        //Check to see if this means you unlock a new character
            //Calculation: 1/2 + 1 of alive characters are level X or higher, where X is the number of alive characters
        List<Character> visibleCharacters = new List<Character>();
        List<Character> livingCharacters = new List<Character>();
        foreach(Character chara in UIManager.Instance.allCharacters) {
            if(chara.gameObject.activeInHierarchy) {
                visibleCharacters.Add(chara);
                if(chara.alive) {
                    livingCharacters.Add(chara);
                }
            }
        }
        int numChars = livingCharacters.Count;
        int numCalc = 0;
        foreach(Character chara in livingCharacters) {
            if(chara.Level >= numChars) {
                numCalc++;
            }
        }
        if(numChars > 1 && numCalc >= Mathf.FloorToInt(numChars / 2f) + 1) {
            //Create a new character
            PopUp tempPopUp;
            tempPopUp.ChosenCharacter = null;
            tempPopUp.ChosenItem = null;
            tempPopUp.ChosenQuest = QuestManager.CreateNewQuest(1, 1);
            tempPopUp.Type = PopUpType.NewCharacter;
            UIManager.Instance.WaitingPopUps.Add(tempPopUp);
        }
        //This second chance exists to replace a dead character
            //Only happens if no characters are levelling up
        else {
            if(visibleCharacters.Count > livingCharacters.Count) {
                bool charLvled = false;
                foreach(Character chara in livingCharacters) {
                    if(chara.LeveledUp) {
                        charLvled = true;
                    }
                }
                if(!charLvled) {
                    //See the longest time a character has been dead for
                    Character reviveChar = null;
                    for(int i = 0; i < visibleCharacters.Count; i++) {
                        if(!visibleCharacters[i].alive) {
                            visibleCharacters[i].timeSinceDeath++;
                            if(visibleCharacters[i].timeSinceDeath > visibleCharacters[i].Level) {
                                reviveChar = visibleCharacters[i];
                            }
                        }
                    }
                    if(reviveChar != null) {
                        reviveChar.Level = 0;
                        reviveChar.alive = true;
                        reviveChar.gameObject.SetActive(true);
                    }
                    //Create a new character
                    PopUp tempPopUp;
                    tempPopUp.ChosenCharacter = null;
                    tempPopUp.ChosenItem = null;
                    tempPopUp.ChosenQuest = QuestManager.CreateNewQuest(1, 1);
                    tempPopUp.Type = PopUpType.NewCharacter;
                    UIManager.Instance.WaitingPopUps.Add(tempPopUp);
                }
            }
        }
        
    }
    //This script runs through a quest, outputting needed 
    public void RunQuest(ref Quest quest) {
        //The quest happens in 3 Steps:
            //Step 1: StartQuest
        //Text log declares who's on the text;
        foreach(Character chara in quest.myParty.Members) {
            string newString = chara.CharacterName + " joined this quest.";
            quest.QuestOccurences.Add(newString);
        }
        //Goes through every character participating in this quest, and adds their StartQuest Functions
        for(int i = 0; i < quest.partySize; i++) {
            quest.myParty.Members[i].StartQuest(ref quest);
        }
        //Then, stats are added together in the quest's party struct
        Party tempParty = quest.myParty;
        foreach(Character chara in tempParty.Members) {
            tempParty.PartyPower += (chara.Stat[StatType.Strength] + chara.StatModifier[StatType.Strength]);
            tempParty.PartyPower += (chara.Stat[StatType.Magic] + chara.StatModifier[StatType.Magic]);
            tempParty.PowerTarget.Add(AssignTargetAmount(chara.Stat[StatType.Strength] + chara.StatModifier[StatType.Strength], chara.Stat[StatType.Magic] + chara.StatModifier[StatType.Magic]));
            tempParty.PartyLuck += (chara.Stat[StatType.Luck] + chara.StatModifier[StatType.Luck]);
        }
            //Step2: "DungeonRun"
        //After stats have been added, the characters "run the dungeon", where the game just runs a set of numbers
        quest.myParty = tempParty;
        float QuestDiff;//This float will be used to determine if the characters complete the quest
        QuestDiff = DIFF_FIRST_CHAR + DIFF_PER_LEVEL * quest.Level;
        //If this is a quest for more than 1 character, additional difficulties are added
        if(quest.partySize > 1) {
            for(int i = 1; i < quest.partySize; i++) {
                QuestDiff += (DIFF_FURTHER_CHARS + DIFF_PER_LEVEL * quest.Level);
            }
        }
        //Then a little bit of randomization is added
            //going to be slightly easier, bc the highest value is not included
                //High-number-of-character quests have a more consistent difficulty rating
        QuestDiff += Random.Range(-DIFF_RND_PER_LEVEL * quest.Level, DIFF_RND_PER_LEVEL * quest.Level);
        //Compares quest difficulty to characters strength
        quest.PercentQuestComplete = tempParty.PartyPower / QuestDiff;
        //Deals damage back to players
//------------------------------------------------------
        float dmgToAssign = QuestDiff;
        float totalEffPow = 0;
        foreach(float target in tempParty.PowerTarget) {
            totalEffPow += target;
        }
        List<float> AssignedDmg = new List<float>();
        for(int i = 0; i < quest.partySize; i++) {
            float tempDmg = dmgToAssign * tempParty.PowerTarget[i] / totalEffPow;
            AssignedDmg.Add(tempDmg);
        }
        //Damage is reduced with Defense
        for(int i = 0; i < quest.partySize; i++) {
            //Assign dmg to each character
            Character tempChar = quest.myParty.Members[i];
            int dmgDealt = Mathf.CeilToInt(AssignedDmg[i] / CalculateEffDef(tempChar));
            tempChar.curHealth -= dmgDealt;
            string newString = tempChar.CharacterName + " was dealt " + dmgDealt.ToString() + " damage.";
        }

            //Luck is used to determine additional rewards
        //First, group rewards:
            //Gold
        tempParty.LuckGoldReward = tempParty.PartyLuck + Random.Range(-2, 2);
            //And possibly, an item
        float rnd = Random.Range(0f, 1f);
        float percPerLevelItem = ((float)tempParty.PartyLuck / quest.Level) / 100f;
        for(int i = quest.Level; i > 0; i--) {
            if(rnd > 0.99f) {
                //Always has a small chance of getting no item
                break;
            }
            if(rnd < percPerLevelItem * (quest.Level - i + 1)) {
                //Generate a new item of Level i
                tempParty.LuckItemReward.Add(UIManager.Instance.GenerateItem.BasicGeneration(i));
                break;
            }
        }
        //Individual Rewards:
            //Each character has a chance of generating an item, dependent on Luck
        for(int i = 0; i < quest.partySize; i++) {
            percPerLevelItem = CalculateIndividualLuckItemRewardPercent(tempParty.Members[i]) / tempParty.Members[i].Level;
            rnd = Random.Range(0f, 1f);
            for(int j = tempParty.Members[i].Level; j > 0; j--) {
                if(rnd > 0.99f) {
                    //Always has a small chance of getting no item
                    break;
                }
                if(rnd < percPerLevelItem * (tempParty.Members[i].Level - j + 1)) {
                    tempParty.LuckItemReward.Add(UIManager.Instance.GenerateItem.BasicGeneration(j));
                    break;
                }
            }
        }
        quest.myParty = tempParty;
        //After all Rewards are found
            //Step3: EndQuest
        //Each character has their individual endQuest abilities
        for(int i = 0; i < quest.partySize; i++) {
            quest.myParty.Members[i].EndQuest(ref quest);
        }
        //Checks to see if any characters are dead
            //Needs to occur separetly from EndQuest, bc characters might heal
        foreach(Character chara in quest.myParty.Members) {
            if(chara.curHealth <= 0) {
                //Character dies
                chara.Die(ref quest);
            }
        }
        //Prepares for the EndQuest PopUp, configuring things
        PopUp tempPopUp;
        tempPopUp.ChosenCharacter = null;
        tempPopUp.ChosenItem = null;
        tempPopUp.ChosenQuest = quest;
        tempPopUp.Type = PopUpType.QuestComplete;
        UIManager.Instance.WaitingPopUps.Add(tempPopUp);
        //Each character that levels up also will get a popUp
        foreach(Character chara in quest.myParty.Members) {
            if(chara.LeveledUp) {
                PopUp charPopUp;
                UIManager.Instance.GenerateCharacter.LevelUpCharacter(chara);
                charPopUp.ChosenCharacter = chara;
                charPopUp.ChosenItem = null;
                charPopUp.ChosenQuest = quest;
                charPopUp.Type = PopUpType.CharacterLevelUp;
                UIManager.Instance.WaitingPopUps.Add(charPopUp);
            }
        }

    }
    //These variables are constants used to Run the Quest
        //These 4 first consts declare how difficult the dungeon is
    private const int DIFF_FIRST_CHAR = 18;
    private const int DIFF_FURTHER_CHARS = 16;
    private const int DIFF_PER_LEVEL = 5;
    private const int DIFF_RND_PER_LEVEL = 2;
//-------------------------------------------------------------------------

    //Refreshes all the Quest UI
    public void RefreshEntireQuestPage() {
        //Every quest has its UI refreshed
        for(int i = 0; i <QuestSlots.Count; i++) {
            QuestSlots[i].gameObject.SetActive(true);
            QuestSlots[i].RefreshUI();
        }
        RefreshSelectedQuest();
    }
    //UI script for refreshing the currently selected Quest
    [Header("Selected Quest UI")]
    [SerializeField] private TMP_Text curTitleText;
    [SerializeField] private TMP_Text curPartyNumText;
    [SerializeField] private TMP_Text curLevelText;
    [SerializeField] private TMP_Text curGoldRewardText;
    [SerializeField] private List<ItemReference> curItemReward;
    [SerializeField] private List<CharacterReference> curCharOnQuest;
    public void RefreshSelectedQuest() {
        Quest CurQuest = UIManager.Instance.currentQuestReference.Reference;
        curTitleText.text = CurQuest.Title;
        curPartyNumText.text = "Party of " + CurQuest.partySize.ToString();
        curLevelText.text = "Challenge Level " + CurQuest.Level.ToString();
        curGoldRewardText.text = CurQuest.goldReward.ToString() + " gold";
        //For each item, it checks to see if it exists
        for(int i = 0; i < curItemReward.Count; i++) {
            if(CurQuest.ItemReward.Count <= i) {
                curItemReward[i].myImageBack.gameObject.SetActive(false);
                curItemReward[i].Reference = null;
            }
            else {
                curItemReward[i].myImageBack.gameObject.SetActive(true);
                curItemReward[i].Reference = CurQuest.ItemReward[i];
                curItemReward[i].myImage.sprite = CurQuest.ItemReward[i].Sprite;
            }
        }
        //It determines what characters are currently in the active Party
        for(int i = 0; i < curCharOnQuest.Count; i++) {
            if(CurQuest.myParty.Members.Count <= i) {
                curCharOnQuest[i].Reference = null;
                curCharOnQuest[i].gameObject.SetActive(false);
            }
            else {
                curCharOnQuest[i].gameObject.SetActive(true);
                curCharOnQuest[i].Reference = CurQuest.myParty.Members[i];
                curCharOnQuest[i].myTitleText.text = curCharOnQuest[i].Reference.CharacterName + ", Level " + curCharOnQuest[i].Reference.Level.ToString();
            }
        }
        RefreshQuestButtons();
    }
    //Button UI also needs to be refreshed
    [SerializeField] private Button QuestStartButton;
    [SerializeField] private Button AssignCharacterButton;
    public void RefreshQuestButtons() {
        //Gets the current character - determines whether they can be assigned/unassigned from a quest
        Character tempChar = UIManager.Instance.currentCharacter;
        Quest curQuest = UIManager.Instance.currentQuestReference.Reference;
        if(tempChar.inParty) {
            AssignCharacterButton.GetComponentInChildren<TMP_Text>().text = "Remove Character from Current Quest";
            AssignCharacterButton.interactable = true;//Can always be interacted with
        }
        else {
            AssignCharacterButton.GetComponentInChildren<TMP_Text>().text = "Add Character to Selected Quest";
            //Button becomes inactive if current quest is full
            if(curQuest.myParty.Members.Count >= curQuest.partySize) {
                AssignCharacterButton.interactable = false;
            }
            else {
                AssignCharacterButton.interactable = true;
            }
        }

        //If all characters are assigned to quests, StartQuest becomes active
        int charactersOnQuests = 0;
        for(int i = 0; i< UIManager.Instance.livingCharacters; i++) {
            if(UIManager.Instance.allCharacters[i].inParty) {
                charactersOnQuests++;
            }
        }
        if(charactersOnQuests >= UIManager.Instance.livingCharacters) {
            QuestStartButton.interactable = true;
        }
        else {
            QuestStartButton.interactable = false;
        }
    }
    //Adding or Removing a Character to the CurrentQuest
    public void AddRemoveCharacterFromQuest() {
        //Gets the currently selected character so they can be referenced
        Character curChar = UIManager.Instance.currentCharacter;
        Quest curQuest = UIManager.Instance.currentQuestReference.Reference;
        if(curChar.inParty) {
            //Find which party the character is in, and remove them from it
            for(int i = 0; i < activeQuests; i++) {
                Quest tempQuest = QuestSlots[i].Reference;
                Party tempParty = tempQuest.myParty;
                if(tempParty.Members.Contains(curChar)) {
                    tempParty.Members.Remove(curChar);
                    tempQuest.myParty = tempParty;
                    QuestSlots[i].Reference = tempQuest;
                    break;
                }
            }
            curChar.inParty = false;
        }
        else {
            //Otherwise, character is added to the current quest - if the quest has available space
            Party tempParty = curQuest.myParty;
            if(tempParty.Members.Count < curQuest.partySize) {
                tempParty.Members.Add(curChar);
                curQuest.myParty = tempParty;
                UIManager.Instance.currentQuestReference.Reference= curQuest;
                curChar.inParty = true;
            }
        }
        //Then, UI is refreshed
        RefreshEntireQuestPage();
    }
    //This function creates several quests, depending on the characters who are currently alive
        //Will always generate twice the needed quests - one set rounding down in level, one rounding up
    private const int MIN_QUEST_SIZE = 2;
    private const int MAX_QUEST_SIZE = 6;
    public List<Quest> CreateNewQuestsUsingCurrent() {
        //First, needs to delete any unused Items from old quests
        foreach(QuestReference reference in QuestSlots) {
            if(reference.Reference.ItemReward != null ) {
                foreach(Item item in reference.Reference.ItemReward) {
                    //If the unequipped items do not contain this item, it is destroyed
                    if(!UIManager.Instance.GeneralItemManager.UnequippedItems.Contains(item)) {
                        Destroy(item.gameObject);
                    }
                }
                reference.Reference.ItemReward = null;
            }
        }
        List<Quest> newQuests = new List<Quest>();
        List<Character> liveChars = new List<Character>();
        float aveLevel = 0;//Average level of a living character
        int maxLevelOnQuest = 0;
        int minLevelOnQuest = 10;
        foreach(Character chara in UIManager.Instance.allCharacters) {
            if(chara.gameObject.activeInHierarchy && chara.alive) {
                liveChars.Add(chara);
                aveLevel += chara.Level;
                if(chara.Level > maxLevelOnQuest) {
                    maxLevelOnQuest = chara.Level;
                }
                if(chara.Level < minLevelOnQuest) {
                    minLevelOnQuest = chara.Level;
                }
            }
        }
        aveLevel = aveLevel / (float)liveChars.Count;
        //Will only create 2 quests total if the number of chars isn't high enough
        if(liveChars.Count < MIN_QUEST_SIZE * 2) {
            newQuests.Add(CreateNewQuest(liveChars.Count, Mathf.CeilToInt(aveLevel)));
            newQuests.Add(CreateNewQuest(liveChars.Count, Mathf.FloorToInt(aveLevel)));
            return newQuests;
        }
        else {
            //Runs through this twice, so you get enough quests
            for(int i = 0; i < 2; i++) {
                int numCharAccountedFor = liveChars.Count;
                List<int> newQuestNumbers = new List<int>();
                while(numCharAccountedFor > MIN_QUEST_SIZE * 2) {
                    int maxSize = Mathf.Clamp(numCharAccountedFor, MIN_QUEST_SIZE, MAX_QUEST_SIZE);
                    int newQuestSize = Random.Range(MIN_QUEST_SIZE, maxSize);
                    newQuestNumbers.Add(newQuestSize);
                    numCharAccountedFor -= newQuestSize;
                }
                //Deals with last few characters
                if(numCharAccountedFor == MIN_QUEST_SIZE * 2) {
                    int rnd = Random.Range(0, 2);
                    if(rnd == 0) {
                        newQuestNumbers.Add(numCharAccountedFor);
                    }
                    else {
                        newQuestNumbers.Add(MIN_QUEST_SIZE);
                        newQuestNumbers.Add(MIN_QUEST_SIZE);
                    }
                }
                else {
                    newQuestNumbers.Add(numCharAccountedFor);
                }
                //For each quest, it is given an appropriate quest level
                int lvlsToDistribute = Mathf.RoundToInt(aveLevel * liveChars.Count);
                List<int> newQuestLevels = new List<int>();
                for(int j = 0; j < newQuestNumbers.Count; j++) {
                    newQuestLevels.Add(minLevelOnQuest);
                    lvlsToDistribute -= minLevelOnQuest * newQuestNumbers[j];
                }
                if(i == 0) {
                    //Rounds up
                    while(lvlsToDistribute > 0) {
                        int rnd = Random.Range(0, newQuestLevels.Count);
                        //Adds a level, and removes that much from the levels to distribute
                        newQuestLevels[rnd] += 1;
                        lvlsToDistribute -= newQuestNumbers[rnd];
                        //If it reaches max level, it is removed, and a quest is created
                        if(newQuestLevels[rnd] >= maxLevelOnQuest) {
                            newQuests.Add(CreateNewQuest(newQuestNumbers[rnd], newQuestLevels[rnd]));
                            newQuestLevels.RemoveAt(rnd);
                            newQuestNumbers.RemoveAt(rnd);
                        }
                    }
                }
                else {
                    //Rounds down
                    while(lvlsToDistribute > MIN_QUEST_SIZE) {
                        int rnd = Random.Range(0, newQuestLevels.Count);
                        //Adds a level, and removes that much from the levels to distribute
                        newQuestLevels[rnd] += 1;
                        lvlsToDistribute -= newQuestNumbers[rnd];
                        //If it reaches max level, it is removed, and a quest is created
                        if(newQuestLevels[rnd] >= maxLevelOnQuest) {
                            newQuests.Add(CreateNewQuest(newQuestNumbers[rnd], newQuestLevels[rnd]));
                            newQuestLevels.RemoveAt(rnd);
                            newQuestNumbers.RemoveAt(rnd);
                        }
                    }
                }
                //Then, remaining questnumbers are turned into quests
                while(newQuestLevels.Count > 0) {
                    newQuests.Add(CreateNewQuest(newQuestNumbers[0], newQuestLevels[0]));
                    newQuestLevels.RemoveAt(0);
                    newQuestNumbers.RemoveAt(0);
                }
            }
            return newQuests;
        }
    }
    //Creating a new quest depends only on the number of characters and the level of the quest
    public static Quest CreateNewQuest(int numChar, int lvl) {
        Quest tempQuest = new Quest();
        tempQuest.exists = true;
        tempQuest.Title = "";
        tempQuest.partySize = numChar;
        tempQuest.Level = lvl;
        tempQuest.ItemReward = CreateItemReward(numChar, lvl);
        tempQuest.goldReward = CalculateGoldReward(numChar, lvl, tempQuest.ItemReward);
        Party tempParty = new Party();
        tempParty.Members = new List<Character>();
        tempParty.PartyPower = 0;
        tempParty.PowerTarget = new List<float>();
        tempParty.PartyLuck = 0;
        tempParty.LuckItemReward = new List<Item>();
        tempParty.LuckGoldReward = 0;
        tempQuest.myParty = tempParty;
        tempQuest.QuestOccurences = new List<string>();
        tempQuest.PercentQuestComplete = 0;
        return tempQuest;
    }

    //This function determines a gold reward for a quest
    private const int AVE_GOLD_PER_LEVEL_CHAR = 15;
    private static int CalculateGoldReward(int numChar, int lvl, List<Item> currItemReward) {
        int tempReward = 0;
        //TODO: Determine the correct amount of reward per quest
        tempReward += numChar * lvl * AVE_GOLD_PER_LEVEL_CHAR;
        tempReward += Random.Range(0, AVE_GOLD_PER_LEVEL_CHAR / 2);
        //Reduce Temp reward depending on ItemRewards
        foreach(Item item in currItemReward) {
            tempReward -= item.Price;
        }
        //Has a minimum reward amount
        if(tempReward < 0) {
            tempReward = Random.Range(AVE_GOLD_PER_LEVEL_CHAR / 3, AVE_GOLD_PER_LEVEL_CHAR);
        }
        return tempReward;
    }
    //This function determines what items are received for a quest
    private const int MAX_ITEM_REWARDS = 4;
    private static List<Item> CreateItemReward(int numChar, int lvl) {
        List<Item> rewardList = new List<Item>();
        //Always generates a potion
        Item newItem = UIManager.Instance.GenerateItem.PotionGeneration(lvl);
        rewardList.Add(newItem);
        //Has a chance to get a few more items, depending on the lvl and Number of Characters
        float rnd = Random.Range(0f, 1f);
        for(int i = 1; i < MAX_ITEM_REWARDS; i++) {
            if(rnd < lvl * numChar * (0.15f / (MAX_ITEM_REWARDS - 1) * i )) {
                newItem = UIManager.Instance.GenerateItem.BasicGeneration(Random.Range(1, lvl +1));
                rewardList.Add(newItem);
            }
        } 
        return rewardList;
    }

    //Calculates how much of a target a character is in the RunQuest-Party.PowerTarget section
    private const float  MAGIC_TARGET_PERCENT = 1f / 3f;
    private static float AssignTargetAmount(int str, int mag) {
        float temp = 0;
        temp += str;
        temp += mag * MAGIC_TARGET_PERCENT;
        return temp;
    }
    //Calculates chance a specific character will generate an item, dependent on luck
    private const float INDIV_LUCK_ITEM_DIVIDER = 4f;
    private static float CalculateIndividualLuckItemRewardPercent(Character chara) {
        float basePerc = chara.Stat[StatType.Luck] + chara.StatModifier[StatType.Luck];
        basePerc = basePerc / INDIV_LUCK_ITEM_DIVIDER;
        basePerc = Mathf.Pow(basePerc, 2);
        basePerc = basePerc / 100f;
        return basePerc;
    }

    //Calculates how much the dmg a character takes is divided by
    private const float DEF_MULTIPLIER = 1.75f;
    private static float CalculateEffDef(Character chara) {
        float temp = chara.Stat[StatType.Defense] + chara.StatModifier[StatType.Defense];
        temp = Mathf.Pow(temp, 0.5f);//Is square rooted
        temp *= DEF_MULTIPLIER;
        return temp;
    }

}
