using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//Used by Quest UI to show what Quest this object is showing
public class QuestReference : MonoBehaviour
{
    public Quest Reference;
    //Has several UI Attributes
    [SerializeField] private GameObject BasePanel;
    [SerializeField] private TMP_Text _titleText;
    public TMP_Text TitleText {
        get {
            return _titleText;
        }
    }
    [SerializeField] private TMP_Text _partySizeText;
    public TMP_Text PartySizeText {
        get {
            return _partySizeText;
        }
    }
    [SerializeField] private TMP_Text _questLevelText;
    public TMP_Text QuestLevelText {
        get {
            return _questLevelText;
        }
    }
    [SerializeField] private TMP_Text _goldRewardText;
    public TMP_Text GoldRewardText {
        get {
            return _goldRewardText;
        }
    }
    [SerializeField] private List<ItemReference> _itemRewardImage;
    public List<ItemReference> ItemRewardImage {
        get {
            return _itemRewardImage;
        }
    }
    void Start() {
    }
    public void RefreshUI() {
        if(Reference.exists) {
            _titleText.text = Reference.Title;
            _partySizeText.text = "Party of " + Reference.partySize.ToString();
            _questLevelText.text = "Level " + Reference.Level.ToString();
            _goldRewardText.text = Reference.goldReward.ToString() + " gold";
            for(int i = 0; i < _itemRewardImage.Count; i++) {
                if(Reference.ItemReward.Count <= i) {
                    _itemRewardImage[i].myImage.enabled = false;
                    _itemRewardImage[i].Reference = null;
                }
                else {
                    _itemRewardImage[i].myImage.enabled = true;
                    _itemRewardImage[i].Reference = Reference.ItemReward[i];
                    _itemRewardImage[i].myImage.sprite = Reference.ItemReward[i].Sprite;
                }
            }
        }
        //If it doesn't exist, it is turned off, and must be turned on again
        else {
            gameObject.SetActive(false);
        }
    }
}
