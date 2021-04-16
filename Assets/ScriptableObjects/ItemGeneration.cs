using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//This Script contains the static variables needed to generate Items, so it can be more safely organized
[CreateAssetMenu(menuName = "ScriptableObjects/ItemGeneration")]
public class ItemGeneration : ScriptableObject
{
    [SerializeField] private Sprite test;
    [SerializeField] private Item ItemPrefab;
    public string Name;
    public Item BasicGeneration(int itemLevel) {
        Item newItem = Instantiate(ItemPrefab, Vector3.zero, Quaternion.identity);
        Dictionary<StatType, int> modifiers = new Dictionary<StatType, int>();
        modifiers.Add(StatType.Strength, 2);
        modifiers.Add(StatType.Defense, 3);

        newItem.DeclareTraits(modifiers, "NAME", ItemType.Weapon, 2, 15, "This is my ability text", test);
        return newItem;
    }
}
