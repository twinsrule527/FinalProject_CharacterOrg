using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//A manager object that specifically manages items that are not currently equipped/are in the shop
public class ItemManager : MonoBehaviour
{
    //List of existing items and where they appear on the screen
    public List<Item> UnequippedItems;
    public List<ItemReference> UnequippedItemSlots;
    public List<Item> ShopItems;
    public List<ItemReference> ShopItemSlots;
    public InventoryGridControl myGridControl;
    
    void Start()
    {
        ShopItems = new List<Item>();
        UnequippedItems = new List<Item>();
        RefreshShopItemsUI();
        RefreshUnequippedItemsUI();
        
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) {
            Item newPotion = UIManager.Instance.GenerateItem.PotionGeneration(1);
            UnequippedItems.Add(newPotion);
            RefreshUnequippedItemsUI();
        }
    }

    //this function and the next function refresh the UI for Items that are not equipped/in the shop, displaying whatever newly needs to be displayed
    public void RefreshUnequippedItemsUI() {
        //Deletes all unequipped items which no longer exist
        UnequippedItems.RemoveAll(item => item == null);
        //If there are more items that slots, slots are created until there are the right number
        while(UnequippedItems.Count > UnequippedItemSlots.Count) {
            for(int i = 0; i <InventoryGridControl.ITEMS_PER_UNEQUIPPED_ROW; i++) {
                ItemReference newReference = myGridControl.CreateNewItemReference();
                UnequippedItemSlots.Add(newReference);
            }
        }
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
        
        if(UnequippedItems.Count <= UnequippedItemSlots.Count - InventoryGridControl.ITEMS_PER_UNEQUIPPED_ROW ) {
            //Only does something if the number of slots is also greater than the minimum number of rows
            if(UnequippedItemSlots.Count > InventoryGridControl.ITEMS_PER_UNEQUIPPED_ROW * InventoryGridControl.MIN_ITEM_ROWS) {
                //If there are too many unequipped slots, excess slots are destroyed
                for(int i = 0; i < InventoryGridControl.ITEMS_PER_UNEQUIPPED_ROW; i++) {
                    Destroy(UnequippedItemSlots[UnequippedItemSlots.Count - 1].myImageBack.gameObject);
                    UnequippedItemSlots.RemoveAt(UnequippedItemSlots.Count - 1);
                }
                //As a double-check, gets rid of all non-existant objects in the list
                UnequippedItemSlots.RemoveAll(item => item == null);
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
