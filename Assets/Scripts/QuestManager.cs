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

}
//This struct manages a Party of characters who are going on an adventure
public struct Party {
    public List<Character> Members;
    public int PartyPower;//The total Strength + Magic Power of this Party
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
            Quest newQuest = CreateNewQuest(i+1, 1);
            activeQuests++;
            QuestSlots[i].Reference = newQuest;
        }
    }

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
        tempParty.PartyLuck = 0;
        tempParty.LuckItemReward = new List<Item>();
        tempParty.LuckGoldReward = 0;
        tempQuest.myParty = tempParty;
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
}
