using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
//Manages UI elements, checking where they are/which ones are selected, etc.
    //Serves as an inetermediary between other managers that otherwise are unable to interact

//This Enumerator exists to manage the 5 main stats that characters have
public enum StatType {
    Strength,//Affects characters chance to clear a quest
    Magic,//Affects characters chance to clear a quest - but less likely to be hit back
    Defense,//Affects how much dmg the character takes
    Endurance,//Determines the character's health/how fast they heal
    Luck//Determines additional rewards at the end of a quest

}

//Used as a reference for various types of popUpScreens
public enum PopUpType {
    None,
    NewCharacter,
    CharacterLevelUp,
    QuestComplete
}
//This struct makes use of the PopUpType by declaring any objects that need to be declared
public struct PopUp {
    public PopUpType Type;
    public Character ChosenCharacter;
    public Item ChosenItem;
    public Quest ChosenQuest;
}
public class UIManager : Singleton<UIManager>//Probably the only singleton in the game, because everything needs to access it
{
    public ItemGeneration GenerateItem;//The Scriptable Object that Generates Items
    public CharacterGeneration GenerateCharacter;//THe scriptable Object that generates characters' Abilities (mainly)
    public ItemManager GeneralItemManager;
    public QuestManager GeneralQuestManager;
    public Character currentCharacter;
    public Item currentItem;
    public QuestReference currentQuestReference;
    private int _currentGold;
    public int CurrentGold;
    //Raycasting Stuff for managing 
    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;
    //This list of all characters includes inactive characters
    public List<Character> allCharacters;
    public int livingCharacters;//Number of characters which are active
    //These are used for when a pop-up window appears
    [SerializeField] private GameObject PopUpCoverImage;
    private PopUpType curPopUp;//Different values correspond to different possible pop-ups
    private const int NUM_START_CHARACTERS = 2;//number of characters the player starts with access to
    public List<PopUp> WaitingPopUps;
    void Start() {
        _itemPageActive = true;
        QuestPage.SetActive(false);
        WaitingPopUps = new List<PopUp>();
        PopUp tempPopUp;
        tempPopUp.ChosenCharacter = null;
        tempPopUp.ChosenItem = null;
        tempPopUp.ChosenQuest = QuestManager.CreateNewQuest(1, 1);
        tempPopUp.Type = PopUpType.NewCharacter;
        for(int i =0; i < NUM_START_CHARACTERS; i++) {
            WaitingPopUps.Add(tempPopUp);
        }
        //Starts the manner for creating characters - will deactivate all characters not yet needed
        Character[] tempCharacters = FindObjectsOfType<Character>();
        foreach(Character chara in tempCharacters) {
            allCharacters.Add(chara);
        }
        for(int i = 0;  i< allCharacters.Count; i++) {
            allCharacters[i].gameObject.SetActive(false);
        }
        CurrentGold = 0;
        curCharStatText = new Dictionary<StatType, TMP_Text>();
        curCharStatText.Add(StatType.Strength, curStrText);
        curCharStatText.Add(StatType.Magic, curMagText);
        curCharStatText.Add(StatType.Defense, curDefText);
        curCharStatText.Add(StatType.Endurance, curEndText);
        curCharStatText.Add(StatType.Luck, curLuckText);
        m_Raycaster = GetComponent<GraphicRaycaster>();
        m_EventSystem = GetComponent<EventSystem>();
    }

    void Update() {
        //if therere are no PopUps, but you have the option for PopUps, then PopUps occur
        if(curPopUp == PopUpType.None && WaitingPopUps.Count > 0) {
            StartPopUp();
        }
        if(Input.GetMouseButtonDown(0)) {
            m_PointerEventData = new PointerEventData(m_EventSystem);
            m_PointerEventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            m_Raycaster.Raycast(m_PointerEventData, results);
            //Gets the first object in the list that can be interacted with, and interacts with it
            //If there is no Pop-up window, there's different actions than if there is a pop-up window
            if(curPopUp == PopUpType.None) {
            for(int i = 0; i< results.Count; i++) {
                
                    //These tag are used to determine something that can be clicked but isn't a button
                if(results[i].gameObject.CompareTag("Character")) {
                    //if it is a choosable character, it becomes the current character
                        //Objects with the "Character" tag will always have a parent with the Character Class
                    Character checkCharacter = results[i].gameObject.GetComponentInParent<Character>();
                    if(checkCharacter.alive) {
                        currentCharacter = checkCharacter;
                        RefreshCharacterUI();
                        break;
                    }
                }
                if(results[i].gameObject.CompareTag("Item")) {
                    //Objects with the "Item" tag will always have an ItemReference compoenent
                    ItemReference checkItem = results[i].gameObject.GetComponent<ItemReference>();
                    if(checkItem.Reference != null) {
                        currentItem = checkItem.Reference;
                        RefreshItemUI();
                        break;
                    }
                }
                if(results[i].gameObject.CompareTag("Quest")) {
                    //Objects with "Quest" tag always have a QuestReference class
                    QuestReference checkQuest = results[i].gameObject.GetComponent<QuestReference>();
                    if(checkQuest.Reference.exists) {
                        currentQuestReference = checkQuest;
                        GeneralQuestManager.RefreshSelectedQuest();
                        break;
                    }
                }
                if(results[i].gameObject.CompareTag("CharacterReference")) {
                    //Character References on the Quest page work similar to with general Character
                    CharacterReference checkRef = results[i].gameObject.GetComponent<CharacterReference>();
                    if(checkRef.Reference.alive) {
                        currentCharacter = checkRef.Reference;
                        RefreshCharacterUI();
                        break;
                    }
                }
                if(results[i].gameObject.CompareTag("Cover")) {
                    break;
                }
               
            }
            }
            else {
                //Different PopUp Windows have different functions
                if(curPopUp == PopUpType.NewCharacter) {
                    //1 = A New Character Window
                    NewCharacterRaycast(results);
                }
                else if(curPopUp == PopUpType.CharacterLevelUp) {
                    //2 = LevelUp character window (has no actions to take here)
                    LevelUpRaycast(results);
                }
            }
        }
    }

    //UI Elements for the CurrentCharacter
    [Header("Current Character UI")]
    [SerializeField] private TMP_Text curCharNameText;
    [SerializeField] private TMP_Text curCharLevelText;
    [SerializeField] private TMP_Text curHealthText;
    [SerializeField] private Image curHealthBar;
    [SerializeField] private TMP_Text curStrText;
    [SerializeField] private TMP_Text curMagText;
    [SerializeField] private TMP_Text curDefText;
    [SerializeField] private TMP_Text curEndText;
    [SerializeField] private TMP_Text curLuckText;
    public Dictionary<StatType, TMP_Text> curCharStatText;
    [SerializeField] private List<Image> curInventoryImage;
    [SerializeField] private TMP_Text curCharSpecialAbilityText;
    //Function that Refreshes the UI for the Current Character
    public void RefreshCharacterUI() {//STILL NEEDS TO SHOW SPECIAL ABILITIES
        currentCharacter.RefreshUI();
        //UI is set to be the same as that of the selected character
        curCharNameText.text = currentCharacter.NameText.text;
        curCharLevelText.text = currentCharacter.LevelText.text;
        curHealthText.text = currentCharacter.HealthText.text;
        curHealthBar.fillAmount = currentCharacter.HealthBar.fillAmount;
        curCharSpecialAbilityText.text = currentCharacter.AbilityString;
        for(int i = 0; i < StatType.GetNames(typeof(StatType)).Length; i++) {
            curCharStatText[(StatType)i].text = currentCharacter.StatText[(StatType)i].text;
        }
        for(int i = 0; i<curInventoryImage.Count; i++) {
            if(currentCharacter.Inventory.Count <= i || currentCharacter.Inventory[i] == null) {
                curInventoryImage[i].enabled = false;
                curInventoryImage[i].GetComponent<ItemReference>().Reference = null;
            }
            else {
                //Otherwise, the image matches that of the Item
                curInventoryImage[i].enabled = true;
                curInventoryImage[i].sprite = currentCharacter.Inventory[i].Sprite;
                curInventoryImage[i].GetComponent<ItemReference>().Reference = currentCharacter.Inventory[i];
            }
        }
        //Refreshes the Item's buttons - eventually will be disabled if on the Quest Page
            //But only if the Item exists
        //And does the same for buttons
        if(_itemPageActive) {
            if(currentItem != null) {
                RefreshItemButtonUI();
            }
        }
        else {
            GeneralQuestManager.RefreshQuestButtons();
        }
        
    }

    //UI elements for the currentItem
    [Header("Current Item UI")]
    [SerializeField] private Image curItemImage;
    [SerializeField] private TMP_Text curItemNameText;
    [SerializeField] private TMP_Text curItemTypeText;
    [SerializeField] private TMP_Text curItemLevelText;
    [SerializeField] private TMP_Text curItemPriceText;
    [SerializeField] private TMP_Text curItemAbilityText;
    [SerializeField] private Button SellPurchaseButton;
    [SerializeField] private Button EquipUnequipButton;
    //Refreshes the UI of the currently selected Item, similar to how is done with characters
    public void RefreshItemUI() {
        curItemImage.sprite = currentItem.Sprite;
        curItemNameText.text = currentItem.ItemName;
        curItemTypeText.text = currentItem.Type.ToString();
        curItemLevelText.text = "Level " + currentItem.Level.ToString();
        //An item in your inventory can only be sold for a fraction of its full price
        if(currentItem.InShop) {
            curItemPriceText.text = "Costs " + currentItem.Price.ToString() + " gold";
        }
        else {
            curItemPriceText.text = "Sells for " + CalculateSellPrice(currentItem.Price).ToString() + " gold";
        }
        curItemAbilityText.text = currentItem.AbilityText;
        //For different buttons, they change depending on what options the player has
            //But only if there is a currently selected Character
        if(currentCharacter != null) {
            RefreshItemButtonUI();
        }
    }

    //Calculates how much an item would be sold for, relative to its full price
    private const float SELL_PERCENT = 0.75f;
    private int CalculateSellPrice(int amt) {
        return Mathf.CeilToInt(amt * SELL_PERCENT);
    }

    //This function refreshes the Buttons for the selected Item, showing what your options are
    public void RefreshItemButtonUI() {
        TMP_Text shopText = SellPurchaseButton.GetComponentInChildren<TMP_Text>();
        if(currentItem.InShop) {
            shopText.text = "Buy Item";
            if(CurrentGold >= currentItem.Price) {
                SellPurchaseButton.interactable = true;
            }
            else {
                SellPurchaseButton.interactable = false;
            }
        }
        else {
            shopText.text = "Sell Item";
            SellPurchaseButton.interactable = true;
        }
        TMP_Text equipText = EquipUnequipButton.GetComponentInChildren<TMP_Text>();
        if(currentItem.EquippedCharacter != null) {
            equipText.text = "Unequip Item";
            EquipUnequipButton.interactable = true;
        }
        else {
            equipText.text = "Equip Item";
            if(currentItem.EquippableTo(currentCharacter)) {
                EquipUnequipButton.interactable = true;
            }
            else {
                EquipUnequipButton.interactable = false;
            }
        }
    }

    //This function is called by a button, and switches between Item and Quest pages
    [Header("Switch Quest And Item Pages")]
    [SerializeField] private GameObject ItemPage;
    [SerializeField] private GameObject QuestPage;
    [SerializeField] private Button SwitchPageButton;
    private bool _itemPageActive;
    public bool ItemPageActive {
        get {
            return _itemPageActive;
        }
    }
    public void SwitchItemQuestPage() {
        _itemPageActive = !_itemPageActive;
        if(_itemPageActive) {
            ItemPage.SetActive(true);
            QuestPage.SetActive(false);
            SwitchPageButton.GetComponentInChildren<TMP_Text>().text = "Go to Quest Page";
            RefreshItemUI();
            GeneralItemManager.RefreshShopItemsUI();
            GeneralItemManager.RefreshUnequippedItemsUI();
        }
        else {
            ItemPage.SetActive(false);
            QuestPage.SetActive(true);
            SwitchPageButton.GetComponentInChildren<TMP_Text>().text = "Go to Item Page";
            currentQuestReference = GeneralQuestManager.QuestSlots[0];
            GeneralQuestManager.RefreshEntireQuestPage();
            
        }

    }
    //This Sorting Algorithm overrides the one in the ItemManager, but it doesn't need inputs
    [Header("Inventory Sorting")]
    [SerializeField] private TMP_Dropdown InventorySortDrop;
    public void SortInventoryOverride() {
        GeneralItemManager.SortInventory(ref GeneralItemManager.UnequippedItems, (SortByType) InventorySortDrop.value);
    }
    //PopUpGeneral Windows:
    [Header("PopUp Window General")]
    [SerializeField] private GameObject NewCharacterPopUp;
    [SerializeField] private GameObject CharacterLevelUpPopUp;
    [SerializeField] private GameObject QuestCompletePopUp;
    //This function starts a new PopUp, using the 0th element of the waitingPopUp List
    public void StartPopUp() {
        PopUp tempPopUp = WaitingPopUps[0];
        WaitingPopUps.RemoveAt(0);
        PopUpCoverImage.SetActive(true);
        curPopUp = tempPopUp.Type;
        //Depending on which popUp type it is, different things happen
        if(curPopUp == PopUpType.NewCharacter) {
            NewCharacterPopUp.SetActive(true);
            livingCharacters++;
            CreateNewCharacterStep1(1);
            RefreshNewCharacterPopUp();
        }
        else if(curPopUp == PopUpType.CharacterLevelUp) {
            CharacterLevelUpPopUp.SetActive(true);
            characterLevelledUp = tempPopUp.ChosenCharacter;
            RefreshLevelUpPopUp();
        }
        else if(curPopUp == PopUpType.QuestComplete) {
            QuestCompletePopUp.SetActive(true);
            completedQuest = tempPopUp.ChosenQuest;
            RefreshQuestCompletePopUp();
        }
    }
    //This function ends the current popUp, returning to the base screen
    public void EndPopUp() {
        PopUpCoverImage.SetActive(false);
        curPopUp = PopUpType.None;
    }
    //PopUp Window Function 1: When the PopUp window is for a new Character
    [Header("PopUp Window 1: New Character")]
    [SerializeField] private List<GameObject> AbilityPanels;
    private List<Ability> AbilitiesToChooseFrom;
    [SerializeField] private List<TMP_Text> AbilityTexts;
    [SerializeField] private TMP_InputField NameField;
    [SerializeField] private Button NewCharacterDoneButton;
    private Ability SelectedAbility;
    //Colors for the ability panels
    [SerializeField] private Color AbilityPanelBaseColor;
    [SerializeField] private Color AbilityPanelSelectedColor;
    private bool abilityIsSelected;//Bool used to see if button can be pressed
    //Starts the first step for creating a new character
    private void CreateNewCharacterStep1(int charLevel) {
        AbilitiesToChooseFrom = new List<Ability>();
        AbilitiesToChooseFrom.Add(GenerateCharacter.DetermineSpecialAbility(charLevel));
        AbilitiesToChooseFrom.Add(GenerateCharacter.DetermineSpecialAbility(charLevel));
        NameField.text = "";
        for(int i = 0; i < 2; i++) {
            AbilityTexts[i].text = AbilitiesToChooseFrom[i].displayText;
        }
        abilityIsSelected = false;
        RefreshNewCharacterPopUp();
        
    }
    //Raycast for this window
    private void NewCharacterRaycast(List<RaycastResult> results) {
        for(int i = 0; i < results.Count; i++) {
            //if it is an ability Panel
            if(AbilityPanels.Contains(results[i].gameObject)) {
                int temp = AbilityPanels.IndexOf(results[i].gameObject);
                SelectedAbility = AbilitiesToChooseFrom[temp];
                abilityIsSelected = true;
                RefreshNewCharacterPopUp();
                break;
            }
        }
    }
    //Refreshes the UI for this pop up, mainly to check to see if the button can be pressed
    public void RefreshNewCharacterPopUp() {
        foreach(Ability ab in AbilitiesToChooseFrom) {
            if(ab.Equals(SelectedAbility)) {
                int temp = AbilitiesToChooseFrom.IndexOf(ab);
                for(int i = 0; i < AbilityPanels.Count; i++) {
                    //Selected panels change color
                    if(i == temp && abilityIsSelected) {
                        AbilityPanels[i].GetComponent<Image>().color = AbilityPanelSelectedColor;
                    }
                    else {
                        AbilityPanels[i].GetComponent<Image>().color = AbilityPanelBaseColor;
                    }
                }
            }
        }
        //Checks to see if button can be turned on
        //First, an ability needs to be selected
        if(abilityIsSelected) {
            //Second, a name needs to be given
            if(NameField.text != "") {
                NewCharacterDoneButton.interactable = true;
            }
            else {
                NewCharacterDoneButton.interactable = false;
            }
        }
        else {
            NewCharacterDoneButton.interactable = false;
        }

    }
    //Closes the PopUp window - in this case, sets the character's traits, and levels them up to level 1
    public void CloseNewCharacterPopUp() {
        Character newCharacter = null;
        //gets the first inactive character
        for(int i = 0; i< allCharacters.Count; i++) {
            if(!allCharacters[i].gameObject.activeInHierarchy) {
                newCharacter = allCharacters[i];
                newCharacter.gameObject.SetActive(true);
                break;
            }
        }
        //First, character gets their name
        newCharacter.CharacterName = NameField.text;
        //To assign the ability, we return to Character Generation Code
        GenerateCharacter.AssignCharacterWithAbility(newCharacter, SelectedAbility, 1);
        abilityIsSelected = false;
        foreach(GameObject obj in AbilityPanels) {
            obj.GetComponent<Image>().color = AbilityPanelBaseColor;
        }
        //Immediately opens the character's Level Up popUp
        characterLevelledUp = newCharacter;
        //Closes this window, and opens the new one
        NewCharacterPopUp.SetActive(false);
        CharacterLevelUpPopUp.SetActive(true);
        curPopUp = PopUpType.CharacterLevelUp;
        //Character is given a starting potion
        Item startPotion = GenerateItem.PotionGeneration(1);
        currentItem = startPotion;
        currentCharacter = newCharacter;
        startPotion.Equip(newCharacter);
        RefreshLevelUpPopUp();
    }

    //PopUp Window Function2: For when a character Levels Up
    [HideInInspector] public Character characterLevelledUp;
    [Header("PopUp Window 2: Level Up")]
    [SerializeField] private TMP_Text LevelUpTitle;
    [SerializeField] private TMP_Text AvailableStatPointsText;
    [SerializeField] private List<TMP_Text> LevelUpStatValueText;
    [SerializeField] private List<TMP_Text> LevelUpStatIncreaseText;
    [SerializeField] private List<TMP_Text> LevelUpPointsAssignedText;
    [SerializeField] private List<Button> LevelUpDecreaseButton;
    [SerializeField] private List<Button> LevelUpIncreaseButton;
    [SerializeField] private List<int> AssignedStatPoints;
    [SerializeField] private Button LevelUpFinishButton;
    //The Graphics raycast response when this is the available pop-up
    private void LevelUpRaycast(List<RaycastResult> results) {
        //Does nothing
    }
    //These 2 functions increase/decrease a players stat modifiers
    public void IncreaseStatLevelUpPoint(int stat) {
        AssignedStatPoints[stat]++;
        characterLevelledUp.statPoints--;
        RefreshLevelUpPopUp();
    }
    public void DecreaseStatLevelUpPoint(int stat) {
        AssignedStatPoints[stat]--;
        characterLevelledUp.statPoints++;
        RefreshLevelUpPopUp();
    }
    //Refreshes the UI for levelling up a character
    public void RefreshLevelUpPopUp() {
        LevelUpTitle.text = characterLevelledUp.CharacterName + " Leveled Up to Level " + characterLevelledUp.Level.ToString() + "!";
        AvailableStatPointsText.text = characterLevelledUp.statPoints.ToString();
        //Runs through every stat value, checking things out
        for(int i = 0; i < LevelUpPointsAssignedText.Count; i++) {
            LevelUpStatValueText[i].text = characterLevelledUp.Stat[(StatType)i].ToString();
            LevelUpStatIncreaseText[i].text = "+" + characterLevelledUp.StatIncrease[(StatType)i].ToString();
            LevelUpPointsAssignedText[i].text = AssignedStatPoints[i].ToString();
            //Each one that has already been increased cannot be increased further
            if(AssignedStatPoints[i] > 0) {
                LevelUpIncreaseButton[i].interactable = false;
                LevelUpDecreaseButton[i].interactable = true;
            }
            else {
                LevelUpIncreaseButton[i].interactable = true;
                LevelUpDecreaseButton[i].interactable = false;
            }
        }
        if(characterLevelledUp.statPoints == 0) {
            foreach(Button increase in LevelUpIncreaseButton) {
                increase.interactable = false;
            }
        }
        if(characterLevelledUp.statPoints == 0) {
            LevelUpFinishButton.interactable = true;
        }
        else {
            LevelUpFinishButton.interactable = false;
        }
    }
    //Functions which occurs when a character has finished leveling up
    public void CloseLevelUpPopUp() {
        //All increases are added together for the character
        for(int i = 0; i < AssignedStatPoints.Count; i++) {
            characterLevelledUp.StatIncrease[(StatType)i] += AssignedStatPoints[i];
            AssignedStatPoints[i] = 0;
        }
        //Then adds all the increases to the player
        foreach(var item in characterLevelledUp.StatIncrease) {
            characterLevelledUp.Stat[item.Key] += item.Value;
        }
        for(int i = 0; i < StatType.GetNames(typeof(StatType)).Length; i++) {
            characterLevelledUp.StatIncrease[(StatType)i] = 0;
        }
        //Character has a few final things that needs to be done
        //Health needs to be set to starting health
        characterLevelledUp.RefreshHealth();
        if(characterLevelledUp.Level == 1) {
            characterLevelledUp.curHealth = characterLevelledUp.baseHealth;
        }
        CharacterLevelUpPopUp.SetActive(false);
        currentCharacter = characterLevelledUp;
        characterLevelledUp.RefreshUI();
        RefreshCharacterUI();
        RefreshItemUI();
        EndPopUp();
    }

    //PopUp Function 3: QuestComplete
    [HideInInspector] public Quest completedQuest;
    [Header("PopUp Window 3: Quest Complete")]
    [SerializeField] private TMP_Text QuestCompleteTitle;
    [SerializeField] private TMP_Text QuestCompleteBasicDescription;
    [SerializeField] private TMP_Text QuestCompleteRewardText;
    [SerializeField] private List<ItemReference> QuestCompleteItemReward;
    [SerializeField] private TMP_Text QuestCompleteLuckText;
    [SerializeField] private List<ItemReference> QuestCompleteLuckItem;

    //Raycast UI
    private void QuestCompleteRaycast(List<RaycastResult> results) {
        //Does nothing
    }
    //Determines the percent of a reward that is given for a quest
    private void DetermineQuestPercReward(float percComplete, ref int money, ref List<Item> items) {
        //Only calculates something new if your reward is less than 100%
        if(percComplete < 1) {
            money = Mathf.FloorToInt(money * percComplete);
            int itemsKeepNum = 0;
            for(int i = 0; i < items.Count; i++) {
                if(i * 1f / items.Count < percComplete) {
                    itemsKeepNum++;
                }
            }
            //Destroys unneeded items
            while(items.Count > itemsKeepNum) {
                Destroy(items[items.Count - 1].gameObject);
                items.RemoveAt(items.Count - 1);
            }


        }
    }
    //Refreshes the UI
    public void RefreshQuestCompletePopUp() {
        QuestCompleteTitle.text = "\"" + completedQuest.Title + "\" Quest Finished!";
        int questCompletePercentInt = Mathf.FloorToInt(completedQuest.PercentQuestComplete * 100);
        if(questCompletePercentInt < 100) {
            QuestCompleteBasicDescription.text = "A " + completedQuest.partySize.ToString() + " person party completed " + questCompletePercentInt.ToString() + "% of the quest. As such, the party receives these rewards:";
            int rewardMoney = completedQuest.goldReward;
            //Determines gold and Item Rewards, and then posts them
            DetermineQuestPercReward(completedQuest.PercentQuestComplete, ref rewardMoney, ref completedQuest.ItemReward);
            QuestCompleteRewardText.text = rewardMoney.ToString() + " gold";
            for(int i = 0; i < QuestCompleteItemReward.Count; i++) {
                if(completedQuest.ItemReward.Count <= i) {
                    QuestCompleteItemReward[i].myImageBack.gameObject.SetActive(false);
                    QuestCompleteItemReward[i].Reference = null;
                }
                else {
                    QuestCompleteItemReward[i].myImageBack.gameObject.SetActive(true);
                    QuestCompleteItemReward[i].Reference = completedQuest.ItemReward[i];
                    QuestCompleteItemReward[i].myImage.sprite = completedQuest.ItemReward[i].Sprite;
                }
            }

        }
        else {
            QuestCompleteBasicDescription.text = "A " + completedQuest.partySize.ToString() + " person party completed 100% of the quest. As such, the party receives these rewards:";
            QuestCompleteRewardText.text = completedQuest.goldReward.ToString() + " gold";
            for(int i = 0; i < QuestCompleteItemReward.Count; i++) {
                if(completedQuest.ItemReward.Count <= i) {
                    QuestCompleteItemReward[i].myImageBack.gameObject.SetActive(false);
                    QuestCompleteItemReward[i].Reference = null;
                }
                else {
                    QuestCompleteItemReward[i].myImageBack.gameObject.SetActive(true);
                    QuestCompleteItemReward[i].Reference = completedQuest.ItemReward[i];
                    QuestCompleteItemReward[i].myImage.sprite = completedQuest.ItemReward[i].Sprite;
                }
            }
        }
        //Also shows Luck Rewards
        QuestCompleteLuckText.text = completedQuest.myParty.LuckGoldReward.ToString() + " gold";
        for(int i = 0; i < QuestCompleteLuckItem.Count; i++) {
            if(completedQuest.myParty.LuckItemReward.Count <= i) {
                    QuestCompleteLuckItem[i].myImageBack.gameObject.SetActive(false);
                    QuestCompleteLuckItem[i].Reference = null;
                }
                else {
                    QuestCompleteLuckItem[i].myImageBack.gameObject.SetActive(true);
                    QuestCompleteLuckItem[i].Reference = completedQuest.myParty.LuckItemReward[i];
                    QuestCompleteLuckItem[i].myImage.sprite = completedQuest.myParty.LuckItemReward[i].Sprite;
                }
        }
        //TODO: Also needs to have text of other effects, but that will be added later
    }
    //Finalizes quest rewards, than closes this pop-up
    public void CloseQuestCompletePopUp() {
        //Adds all items to your Unequipped Items
        foreach(Item item in completedQuest.ItemReward) {
            GeneralItemManager.UnequippedItems.Add(item);
        }
        foreach(Item item in completedQuest.myParty.LuckItemReward) {
            GeneralItemManager.UnequippedItems.Add(item);
        }
        _currentGold += (completedQuest.goldReward + completedQuest.myParty.LuckGoldReward);
        foreach(Character chara in completedQuest.myParty.Members) {
            chara.RefreshUI();
        }
        RefreshCharacterUI();
        QuestCompletePopUp.SetActive(false);
        EndPopUp();
    }
}
