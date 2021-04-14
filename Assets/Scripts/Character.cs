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
    public List<Item> Inventory = new List<Item>();//A list of inventory items - I use a list rather than an array bc the max # of objects in the inventory can change
    //Manages the character's health
    private int baseHealth;
    [HideInInspector] public int healthModifier;
    private int curHealth;

    private int xp;//Experience (required to level up)
    private int level;
    private string CharacterName;

    public bool alive;//Whether the character is alive
    void Start() {
        alive = true;
        //Declares dictionaries, and starts assigning their attributes
        Stat = new Dictionary<StatType, int>();
        StatModifier = new Dictionary<StatType, int>();
        for(int i = 0; i < StatType.GetNames(typeof(StatType)).Length; i++) {
            Stat.Add((StatType)i, Random.Range(5, 16));
            StatModifier.Add((StatType)i, 0);
        }
        baseHealth = Stat[StatType.Endurance];
        curHealth = baseHealth;
        _statText = new Dictionary<StatType, TMP_Text>();
        _statText.Add(StatType.Strength, StrText);
        _statText.Add(StatType.Magic, MagText);
        _statText.Add(StatType.Defense, DefText);
        _statText.Add(StatType.Endurance, EndText);
        _statText.Add(StatType.Luck, LuckText);
        RefreshUI();
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
        _nameText.text = CharacterName;
        _levelText.text = "Level " + level.ToString();
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
            if(StatModifier[StatType.Strength] > 0) {
               _statText[(StatType)i].text += " (+" + StatModifier[(StatType)i].ToString() + ")";
            }
            else if(StatModifier[StatType.Strength] < 0) {
                _statText[(StatType)i].text += " (-" + Mathf.Abs(StatModifier[(StatType)i]).ToString() + ")";
            }
        }
        //Each inventory image is set according to the corresponding item in the inventory
        for(int i = 0; i < InventoryImage.Count; i++) {
            //If the object doesn't exist, it is invisible
            if(Inventory.Count < i || Inventory[i] == null) {
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
}
