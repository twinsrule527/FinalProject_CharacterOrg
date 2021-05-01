using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//Serves as a secondary script for the SaveGameJSON script, where this one includes all the actual in-game functionality (such as UI and such)
public class SaveManagement : MonoBehaviour
{
    //Note: The start new game function is in the UIManager
    [SerializeField] private SaveGameJSON SaveGameFunctions;
    [SerializeField] private GameObject ExistingSavePrefab;//Prefab for a save file that already exists, added to the existing saves list at the beginning of the game
    [SerializeField] private VerticalLayoutGroup ExistingSavesGroup;
    private List<GameObject> CurrentExistingSaves = new List<GameObject>();
    public void LoadExistingSaves() {
        foreach(GameObject obj in CurrentExistingSaves) {
            Destroy(obj);
        }
        CurrentExistingSaves = new List<GameObject>();
        SaveList mySaves = SaveGameFunctions.LoadExistingSaves();
        for(int i = 0; i < mySaves.FileNames.Count; i++) {
            //Creates a new existing save UI, puts it in the list
            GameObject newObj = Instantiate(ExistingSavePrefab);
            newObj.transform.SetParent(ExistingSavesGroup.transform);
            newObj.transform.localScale = new Vector3(1f, 1f, 1f);
            CurrentExistingSaves.Add(newObj);
            SaveReference newObjRef = newObj.GetComponent<SaveReference>();
            newObjRef.SaveName = mySaves.FileNames[i];
            newObjRef.SaveCharacterNumber = mySaves.NumChars[i];
            newObjRef.SaveHighestLevel = mySaves.HighestLevel[i];
            newObjRef.RefreshUI();
        }
    }
    void Start() {
        SaveGameFunctions.CreateExistingSavesFile();
        LoadExistingSaves();
    }
    //When a save is selected, it becomes the selected save on the UI, etc.
    [Header("Selected Save Attributes")]
    [SerializeField] private TMP_Text SelectedNameText;
    [SerializeField] private TMP_Text SelectedCharsText;
    [SerializeField] private TMP_Text SelectedLevelText;
    private SaveReference curSelectedRef;
    public void SelectSave(GameObject obj) {
        //Objects with the tag "Save" will always have a save reference
        SaveReference myRef = obj.GetComponent<SaveReference>();
        SelectedNameText.text = "\"" + myRef.SaveName + "\"";
        SelectedCharsText.text = myRef.SaveCharacterNumber.ToString() + " Characters";
        SelectedLevelText.text = "Highest Level: " + myRef.SaveHighestLevel.ToString();
        curSelectedRef = myRef;
    }

    //Deletes the selected save at a button press
    public void DeleteSelectedSave() {
        string loadString = SaveGameFunctions.ReadFromFile(Application.persistentDataPath + "\\" + curSelectedRef.SaveName);
        SaveFile loadFile = JsonUtility.FromJson<SaveFile>(loadString);
        SaveGameFunctions.SaveDeleteFile(loadFile, true);
        int refPos = CurrentExistingSaves.IndexOf(curSelectedRef.gameObject);
        Destroy(curSelectedRef.gameObject);
        CurrentExistingSaves.RemoveAt(refPos);
        if(CurrentExistingSaves.Count > 0) {
            SelectSave(CurrentExistingSaves[0].gameObject);
        }
    }

    //Saves the game in its current state (ignoring whoever might be in certain parties), and then returns to the pregame menu
    void SaveQuitGame() {
        
    }
}
