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

//Quests are rather simple, so while characters and items have whole classes, Quests just have structs + a reference class
public struct Quest {//TODO: Add all the traits of a quest
    public bool exists;//Whether the quest even exists, as a check for when clicking on quests
}
public class UIManager : Singleton<UIManager>//Probably the only singleton in the game, because everything needs to access it
{
    public ItemGeneration GenerateItem;//The Scriptable Object that Generates Items
    public Character currentCharacter;
    public Item currentItem;
    private Quest currentQuest;
    //Raycasting Stuff for managing 
    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;
    void Start() {
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
        if(Input.GetMouseButtonDown(0)) {
            m_PointerEventData = new PointerEventData(m_EventSystem);
            m_PointerEventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            m_Raycaster.Raycast(m_PointerEventData, results);
            //Gets the first object in the list that can be interacted with, and interacts with it
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
                        currentQuest = checkQuest.Reference;
                        break;
                    }
                }
            }
        }
    }

    //UI Elements for the CurrentCharacter
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
    }

    //UI elements for the currentItem
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
// TO BE IMPLEMENTED--------------------------------------------------------------------------------------------------------------------------------------------------------
    }

    //Calculates how much an item would be sold for, relative to its full price
    private const float SELL_PERCENT = 0.75f;
    private int CalculateSellPrice(int amt) {
        return Mathf.CeilToInt(amt * SELL_PERCENT);
    }
}

