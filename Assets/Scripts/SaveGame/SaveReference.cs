using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//Attached to a SaveGameObject in the Existing Saves List, to say the attributes of said save
public class SaveReference : MonoBehaviour
{
    //Attributes of the save file
    public string SaveName;
    //Details only appear when selected
    public int SaveCharacterNumber;
    public int SaveHighestLevel;
    //Text objects which contain the details
    [SerializeField] private TMP_Text NameText;
    //Refreshes the UI of this object's details when it spawns
    public void RefreshUI() {
        NameText.text = "\"" + SaveName + "\"";
    }
}
