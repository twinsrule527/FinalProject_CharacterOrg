using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//This script is used by the Item Manager to re-size the Item List of Unequipped Items
    //All of my scrolling stuff was helped along by this youtube playlist, but none of it was directly copied:
        //https://www.youtube.com/playlist?list=PLL8DeMf3fIgFSJj6OTxXceaMtD5vWFER5
public class InventoryGridControl : MonoBehaviour
{
    public const int ITEMS_PER_UNEQUIPPED_ROW = 5;//How many items appear in any one row of unequipped items
    public const int MIN_ITEM_ROWS = 2;
    [SerializeField] private GridLayoutGroup InventoryGrid;
    [SerializeField] private Image ItemReferencePrefab;//A prefab that will be generated when more rows of items are needed
    
    void Start() {
        InventoryGrid.constraintCount = ITEMS_PER_UNEQUIPPED_ROW;
    }
    
    public ItemReference CreateNewItemReference() {
        Image newItemParent = Instantiate(ItemReferencePrefab);
        newItemParent.transform.SetParent(InventoryGrid.transform);
        newItemParent.transform.localScale = new Vector3(1f, 1f, 1f);
        return newItemParent.GetComponentInChildren<ItemReference>();
    }
}
