using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//A simple class, attached to Images of Items, telling them what Item they are referencing
    //For the sake of when they are clicked on
public class ItemReference : MonoBehaviour
{
    [HideInInspector] public Item Reference;
    public Image myImage;
    public Image myImageBack;//The Image back behind this object - will always be its parent
    void Awake() {
        myImage = GetComponent<Image>();
        myImageBack = transform.parent.GetComponent<Image>();
    }
}
