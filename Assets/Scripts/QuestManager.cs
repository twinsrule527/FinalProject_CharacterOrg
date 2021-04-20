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
        for(int i = 0; i< STARTING_NUM_QUESTS; i++) {
            Quest newQuest = CreateNewQuest(2, 1);
            activeQuests++;
            QuestSlots[i].Reference = newQuest;
        }
    }

    //This script runs through a quest, outputting needed 
    public void RunQuest(ref Quest quest) {
        //The quest happens in 3 Steps:
            //Step 1: StartQuest
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
        //TODO: Make all this below more than a framework
        int dmgToAssign = 100;
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


            //Luck is used to determine additional rewards
        //First, group rewards:
            //Gold
        tempParty.LuckGoldReward = tempParty.PartyLuck + Random.Range(-2, 2);
            //And possibly, an item
        float rnd = Random.Range(0f, 1f);
        float percPerLevelItem = ((float)tempParty.PartyLuck / quest.Level) / 100f;
        for(int i = quest.Level; i > 0; i--) {
            if(rnd < percPerLevelItem * (quest.Level - i + 1)) {
                //Generate a new item of Level i
                tempParty.LuckItemReward.Add(UIManager.Instance.GenerateItem.BasicGeneration(i));
                break;
            }
        }
        //Individual Rewards:
            //Each character has a chance of generating an item, dependent on Luck
        for(int i = 0; i < quest.partySize; i++) {
            percPerLevelItem = CalculateIndividualLuckItemRewardPercent(tempParty.Members[i]) / quest.partySize;
            rnd = Random.Range(0f, 1f);
            for(int j = tempParty.Members[i].Level; j > 0; j--) {
                if(rnd < percPerLevelItem * (tempParty.Members[i].Level - j + 1)) {
                    tempParty.LuckItemReward.Add(UIManager.Instance.GenerateItem.BasicGeneration(j));
                    break;
                }
            }
        }
        //After all Rewards are found
            //Step3: EndQuest
        //Each character has their individual endQuest abilities
        for(int i = 0; i < quest.partySize; i++) {
            quest.myParty.Members[i].EndQuest(ref quest);
        }
        //Checks to see if any characters are dead
    }
    //These variables are constants used to Run the Quest
        //These 4 first consts declare how difficult the dungeon is
    private const int DIFF_FIRST_CHAR = 15;
    private const int DIFF_FURTHER_CHARS = 12;
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
                curItemReward[i].myImage.enabled = false;
                curItemReward[i].Reference = null;
            }
            else {
                curItemReward[i].myImage.enabled = true;
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
    //Creating a new quest depends only on the number of characters and the level of the quest
    public static Quest CreateNewQuest(int numChar, int lvl) {
        Quest tempQuest = new Quest();
        tempQuest.exists = true;
        tempQuest.Title = "";
        tempQuest.partySize = numChar;
        tempQuest.Level = lvl;
        tempQuest.goldReward = CalculateGoldReward(numChar, lvl);
        tempQuest.ItemReward = CreateItemReward(numChar, lvl);
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
    private static int CalculateGoldReward(int numChar, int lvl) {
        int tempReward = 0;
        //TODO: Determine the correct amount of reward per quest
        tempReward += numChar * lvl;
        tempReward += Random.Range(-5, 5);
        return tempReward;
    }
    private static List<Item> CreateItemReward(int numChar, int lvl) {
        return new List<Item>();
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

}
