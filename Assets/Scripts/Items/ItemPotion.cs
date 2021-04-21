using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Potion items all use this class, which inherits from the base item class
public class ItemPotion : Item
{

    //These two functions are the potion versions of the start/end quest abilities
    public override void StartQuest(ref Quest quest)
    {
        base.StartQuest(ref quest);
        //At start quest, the potion increases the corresponding stat
        //If Endurance, it is a health potion
        if(AbilityAffectedStats.Contains(StatType.Endurance)) {
            EquippedCharacter.curHealth += CalculatePotency(Level, true);
        }
        else {
            EquippedCharacter.StatModifier[AbilityAffectedStats[0]] += CalculatePotency(Level, false);
        }
    }
    public override void EndQuest(ref Quest quest)
    {
        base.EndQuest(ref quest);
        //On endQuest, either the potion is destroyed, or possibly not (if it is a health potion)
        if(AbilityAffectedStats.Contains(StatType.Endurance)) {
            //If the character needs to drink the potion they do, but otherwise they don't
            if(EquippedCharacter.curHealth - CalculatePotency(Level, true) <= 0 || EquippedCharacter.curHealth < EquippedCharacter.baseHealth) {
                Unequip();
                myManager.UnequippedItems.Remove(this);
                Destroy(gameObject);
            }
            else {
                EquippedCharacter.curHealth -= CalculatePotency(Level, true);
            }
        }
        else {
            EquippedCharacter.StatModifier[AbilityAffectedStats[0]] -= CalculatePotency(Level, false);
            Unequip();
            myManager.UnequippedItems.Remove(this);
            Destroy(gameObject);
        }
    }
    
    //These private variables serve to calculate how potent a potion is, given its level
    private const float BASE_POTENCY = 2f;
    private const float BASE_HEALTH_POTENCY = 3f;
    private const float POTENCY_PER_LEVEL = 2.5f;
    private const float HEALTH_POTENCY_PER_LEVEL = 3.5f;
    
    //This function calculates a potion's value with respect to its level
    public static int CalculatePotency(int lvl, bool isHealthPotion) {
        if(isHealthPotion) {
            return Mathf.FloorToInt(BASE_HEALTH_POTENCY + lvl * HEALTH_POTENCY_PER_LEVEL);
        }
        else {
            return Mathf.FloorToInt(BASE_POTENCY + lvl * POTENCY_PER_LEVEL);
        }
    }
}
