using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//Reference script used for Characters that show up on Quest page
public class CharacterReference : MonoBehaviour
{
    public Character Reference;
    public TMP_Text myTitleText;
    void Awake()
    {
        myTitleText = GetComponentInChildren<TMP_Text>();
    }
}
