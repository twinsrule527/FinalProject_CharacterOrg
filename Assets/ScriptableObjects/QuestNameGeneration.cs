using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//This scriptable object has a simple purpose: to generate random names for quests
public enum QuestCategory {//This contains the broad categories for which quests will be called, allows for mix-and-matching of quest names
    Defeat,
    Explore
}
[CreateAssetMenu(menuName = "ScriptableObjects/QuestNameGeneration")]
public class QuestNameGeneration : ScriptableObject
{
    [SerializeField] private List<string> DefeatVerbs;
    [SerializeField] private List<string> DefeatEnemies;
    [SerializeField] private List<string> ExploreVerbs;
    [SerializeField] private List<string> ExplorePlaces;

    public string NameQuest(QuestCategory questType) {
        string questName = "";
        //Uses a switch statement determining what it says depending on the quest type
        switch(questType) {
            case QuestCategory.Defeat:
                string verb = DefeatVerbs[Random.Range(0, DefeatVerbs.Count-1)];
                string noun = DefeatEnemies[Random.Range(0, DefeatEnemies.Count - 1)];
                questName = verb + " " + noun;
                break;
            case QuestCategory.Explore:
                verb = ExploreVerbs[Random.Range(0, DefeatVerbs.Count-1)];
                noun = ExplorePlaces[Random.Range(0, DefeatEnemies.Count - 1)];
                questName = verb + " " + noun;
                break;
        }
        return questName;
    }
}
