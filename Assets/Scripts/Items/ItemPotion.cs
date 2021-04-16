using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Potion items all use this class, which inherits from the base item class
public class ItemPotion : Item
{
    


    
    
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
