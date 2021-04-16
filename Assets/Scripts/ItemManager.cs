using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//A manager object that specifically manages items that are not currently equipped/are in the shop
public class ItemManager : MonoBehaviour
{
    public List<Item> UnequippedItems;
    public List<ItemReference> UnequippedItemSlots;
    public List<Item> ShopItems;
    public List<ItemReference> ShopItemSlots;
    void Start()
    {
        ShopItems = new List<Item>();
        UnequippedItems = new List<Item>();
        RefreshShopItemsUI();
        Item newItem = UIManager.Instance.GenerateItem.BasicGeneration(2);
        UnequippedItems.Add(newItem);
        RefreshUnequippedItemsUI();
        
    }

    void Update()
    {
        
    }

    //this function and the next function refresh the UI for Items that are not equipped/in the shop, displaying whatever newly needs to be displayed
    public void RefreshUnequippedItemsUI() {
        //Deletes all unequipped items which no longer exist
        UnequippedItems.RemoveAll(item => item == null);
        //runs through all unequipped slots, and makes the Item there match what item should be there
        for(int i = 0; i < UnequippedItemSlots.Count; i++) {
            if(UnequippedItems.Count <= i) {
                UnequippedItemSlots[i].myImage.enabled = false;
                UnequippedItemSlots[i].Reference = null;
            }
            else {
                UnequippedItemSlots[i].myImage.enabled = true;
                UnequippedItemSlots[i].Reference = UnequippedItems[i];
                UnequippedItemSlots[i].myImage.sprite = UnequippedItems[i].Sprite;
            }
        }
    }

    public void RefreshShopItemsUI() {
        //Deletes all shop items which don't exist
        ShopItems.RemoveAll(item => item == null);
        for(int i = 0; i < ShopItemSlots.Count; i++) {
            if(ShopItems.Count <= i) {
                ShopItemSlots[i].myImage.enabled = false;
                ShopItemSlots[i].Reference = null;
            }
            else {
                ShopItemSlots[i].myImage.enabled = true;
                ShopItemSlots[i].Reference = ShopItems[i];
                ShopItemSlots[i].myImage.sprite = ShopItems[i].Sprite;
            }
        }
    }

    //This function equips/unequips the currently selected Item, after going through a double-check to see if that's possible
    public void EquipUnequipCurrentItem() {
        //Does different things depending if the current Item is equipped or unequipped
        if(UIManager.Instance.currentItem.EquippedCharacter != null ) {
            UIManager.Instance.currentItem.Unequip();
        }
        //If it is not equipped, and not in the shop, it will attempt to equip it
        else if(!UIManager.Instance.currentItem.InShop) {
            if(UIManager.Instance.currentItem.EquippableTo(UIManager.Instance.currentCharacter)) {
                UIManager.Instance.currentItem.Equip(UIManager.Instance.currentCharacter);
            }
        }
        else {
        }
    }
}
