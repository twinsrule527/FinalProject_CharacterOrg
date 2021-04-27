using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//A class for each character to have - manages their items, stats, etc.
public class Character : MonoBehaviour
{
    public Dictionary<StatType, int> Stat;//The character's base stats
    public Dictionary<StatType, int> StatModifier;//modifiers to character's stats, such as through items
    public Dictionary<StatType, int> StatIncrease;//How much the character's stats increase when they level up - only relevant when levelling up, after which they are merged with normal char stats
    public List<Item> Inventory = new List<Item>();//A list of inventory items - I use a list rather than an array bc the max # of objects in the inventory can change
    private int _inventorySize;
    public int InventorySize {
        get {
            return _inventorySize;
        }
    }
    //Manages the character's health
    public int baseHealth;
    [HideInInspector] public int healthModifier;
    [HideInInspector] public int curHealth;

    [SerializeField] private float xp;//Experience (required to level up)
    public float XP {
        get {
            return xp;
        }
        set {
            xp = value;
        }
    }
    public int Level;
    public string CharacterName;
    //Stuff related to leveling up
    public const int STANDARD_STAT_POINTS = 2;//How many stat points a char will normally get on levelling up
    public int statPoints;
    //Special ability is determined through a delegate (see CharacterGeneration ScriptableObject)
    public CharacterAbility OnLevelUpAbility;
    public CharacterAbility OnQuestStartAbility;
    public CharacterAbility OnQuestEndAbility;
    public AbilityReference myAbilityReference;
    [HideInInspector] public List<StatType> AbilityAffectedStats;//Stats that are affected by the character's ability
    public string AbilityString;
    public bool inParty;//Whether the Character is in a party for a quest or not
    public bool alive;//Whether the character is alive
    
    //Need to use Awake, because it is set Active, and then immediately called
    void Awake() {
        _inventorySize = 5;
        Stat = new Dictionary<StatType, int>();
        StatModifier = new Dictionary<StatType, int>();
        StatIncrease = new Dictionary<StatType, int>();
        for(int i = 0; i < StatType.GetNames(typeof(StatType)).Length; i++) {
            Stat.Add((StatType)i, 0);
            StatModifier.Add((StatType)i, 0);
            StatIncrease.Add((StatType)i, 0);
        }
        _statText = new Dictionary<StatType, TMP_Text>();
        _statText.Add(StatType.Strength, StrText);
        _statText.Add(StatType.Magic, MagText);
        _statText.Add(StatType.Defense, DefText);
        _statText.Add(StatType.Endurance, EndText);
        _statText.Add(StatType.Luck, LuckText);
    }
    
    //Has functions that trigger when you start or end a quest with this character
    public Quest refQuest;//The quest the player is currently on is used as a reference for some abilities
    public void StartQuest(ref Quest quest) {
        _leveledUp = false;
        refQuest = quest;
        //Character starts by triggering abilities
        OnQuestStartAbility(this, AbilityAffectedStats);
        //Also triggers abilities of all Items with StartQuest Abilties
        for(int i = 0; i < Inventory.Count; i++) {
            Inventory[i].StartQuest(ref quest);
        }
        quest = refQuest;

    }
    private bool _leveledUp;
    public bool LeveledUp {
        get {
            return _leveledUp;
        }
    }
    private const float HEAL_DIVIDE = 5f;
    private const int XP_PER_QUEST_LEVEL = 4;
    public void EndQuest(ref Quest quest) {
        refQuest = quest;
        //trigger Item EndQuest Abilities
        for(int i = Inventory.Count - 1; i >= 0; i--) {
            Inventory[i].EndQuest(ref quest);
        }
        //then removes all items that were destroyed //NOT WORKING
        //Inventory.RemoveAll(item => item == null);
        //Triggers personal ability
        OnQuestEndAbility(this, AbilityAffectedStats);
        //Character also gains experience (need to calculate this)
        xp += XP_PER_QUEST_LEVEL * quest.Level * quest.PercentQuestComplete;
        if(xp >= CharacterGeneration.LevelUpXP[Level]) {
            _leveledUp = true;
        }
        //Character heals, depending on their endurance
        curHealth += Mathf.CeilToInt((Stat[StatType.Endurance] + StatModifier[StatType.Endurance]) / HEAL_DIVIDE);
        curHealth = Mathf.Clamp(curHealth, 0, baseHealth);
        quest = refQuest;
    }

    //This function occurs when the character dies
    public int timeSinceDeath;//How long it's been since this character has died - after a certain point, you unlock a new character
    public void Die(ref Quest quest) {
        alive = false;
        _leveledUp = false;
        baseHealth = 0;
        curHealth = 0;
        xp = 0;
        timeSinceDeath = 0;
        UIManager.Instance.livingCharacters--;
        //NEED TO: Delete items, show up on the PopUp, and More
        foreach(Item item in Inventory) {
            Destroy(item.gameObject);
        }
        //gameObject.SetActive(false);
    }

    //These objects are UI objects that this affects
        //All have properties that are getters, so that the Character Manager can access them
    [SerializeField] private TMP_Text _nameText;
    public TMP_Text NameText {
        get {
            return _nameText;
        }
    }
    [SerializeField] private TMP_Text _levelText;
    public TMP_Text LevelText {
        get {
            return _levelText;
        }
    }
    [SerializeField] private TMP_Text _healthText;
    public TMP_Text HealthText {
        get {
            return _healthText;
        }
    }
    [SerializeField] private Image _healthBar;
    public Image HealthBar {
        get {
            return _healthBar;
        }
    }
    [SerializeField] private TMP_Text StrText;
    [SerializeField] private TMP_Text MagText;
    [SerializeField] private TMP_Text DefText;
    [SerializeField] private TMP_Text EndText;
    [SerializeField] private TMP_Text LuckText;
    public Dictionary<StatType, TMP_Text> _statText;
    public Dictionary<StatType, TMP_Text> StatText {
        get {
            return _statText;
        }
    }
    [SerializeField] private List<Image> InventoryImage;
    //This function refreshes the UI associated with this specific character
    public void RefreshUI() {
        if(alive) {
            _nameText.text = CharacterName;
            _levelText.text = "Level " + Level.ToString();
            //Health is slight more complicated
            _healthText.text = "HP: " + curHealth.ToString() + "/" + (baseHealth + healthModifier).ToString();
            HealthBar.fillAmount = (float)curHealth / (float)(baseHealth + healthModifier);
            /*
            StrText.text = Stat[StatType.Strength].ToString();
            if(StatModifier[StatType.Strength] > 0) {
                StrText.text += " (+" + StatModifier[StatType.Strength].ToString() + ")";
            }
            else if(StatModifier[StatType.Strength] < 0) {
                StrText.text += " (-" + Mathf.Abs(StatModifier[StatType.Strength]).ToString() + ")";
            }
            */
            //Trying out a method where the different Stat Text displays are all in a dictionary - will have to be declared separately from in the Inspector
            for(int i = 0; i < StatType.GetNames(typeof(StatType)).Length; i++) {
                _statText[(StatType)i].text = Stat[(StatType)i].ToString();
                if(StatModifier[(StatType)i] > 0) {
                _statText[(StatType)i].text += " + " + StatModifier[(StatType)i].ToString();
                }
                else if(StatModifier[(StatType)i] < 0) {
                    _statText[(StatType)i].text += " - " + Mathf.Abs(StatModifier[(StatType)i]).ToString();
                }
            }
            //Each inventory image is set according to the corresponding item in the inventory
            for(int i = 0; i < InventoryImage.Count; i++) {
                //If the object doesn't exist, it is invisible
                if(Inventory.Count <= i || Inventory[i] == null) {
                    InventoryImage[i].enabled = false;
                    InventoryImage[i].GetComponent<ItemReference>().Reference = null;
                }
                else {
                    //Otherwise, the image matches that of the Item
                    InventoryImage[i].enabled = true;
                    InventoryImage[i].sprite = Inventory[i].Sprite;
                    InventoryImage[i].GetComponent<ItemReference>().Reference = Inventory[i];
                }
            }
        }
        else {
            //If not alive, things happen differently
            _nameText.text = "DEAD";
        }
    }

    private const float BASE_HP_VALUE = 5;
    //Using their Endurance stat + endurance modifiers, determines what their health should be
    public void RefreshHealth() {
        float tempHealthBase = BASE_HP_VALUE;
        int maxHealthTemp = baseHealth;
        tempHealthBase += Level / 4f * Stat[StatType.Endurance];
        baseHealth = Mathf.CeilToInt(tempHealthBase);
        curHealth += Mathf.CeilToInt(baseHealth - maxHealthTemp);
    }
    //Resets XP for when you get a new character
    public void ResetXP() {
        xp = 0;
    }
}
