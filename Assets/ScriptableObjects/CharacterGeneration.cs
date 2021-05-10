using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Similar to the ItemGeneration scriptable object, this is used for character generation - mainly used as an organizational thing

//This delegate is used by characters to create their abilities
public delegate void CharacterAbility(Character self, List<StatType> statsAffected);
public enum AbilityReference {LvlIncreaseSpecific, LvlIncreaseChoice, QuestIncrease}

[System.Serializable] public struct Ability {
    public AbilityReference reference;
    public List<StatType> statsAffected;
    public string displayText;

}
[CreateAssetMenu(menuName = "ScriptableObjects/CharacterGeneration")]
public class CharacterGeneration : ScriptableObject
{
    //This integer array declares how experience a character needs to level up
    public static readonly int[] LevelUpXP = {0, 10, 25, 45, 70, 100, 135, 175, 220, 265};
    
    [SerializeField] private List<Ability> possibleAbilities;
    //Determines a possible ability this new character can start with
    public Ability DetermineSpecialAbility(int charLevel) {
        if(charLevel == 1) {
            //Determines a random ability to give them
            int rnd = Random.Range(0, possibleAbilities.Count);
            return possibleAbilities[rnd];
        }
        return new Ability();
    }

    private const int BASE_START_STAT = 5;
    private const int STAT_MOD_TO_BASE = 1;
    private const int STAT_BASE_MULTIPLIER = 2;
    public void AssignCharacterWithAbility(Character character, Ability ability, int lvl) {
        //Here, there is a special formula for calculating a character's starting stats
        List<int> statBase = new List<int>();
        for(int i = 0; i < StatType.GetNames(typeof(StatType)).Length; i++) {
            statBase.Add(i);
        }
        //Goes through each stat, one by one, and either sets it as a base value, 1 lower than the base value, or 1 higher than the base value, with the stats evening out in the end
        int statAve = 0;
        //Starts with strength
        int mod = Random.Range(-STAT_MOD_TO_BASE, STAT_MOD_TO_BASE + 1);
        int rndBase = Random.Range(0, statBase.Count);
        int newBase = statBase[rndBase];
        statBase.RemoveAt(rndBase);
        character.Stat[StatType.Strength] = newBase * STAT_BASE_MULTIPLIER + mod + BASE_START_STAT;
        statAve += mod;
        //Then does Magic
        mod = Random.Range(-STAT_MOD_TO_BASE, STAT_MOD_TO_BASE + 1);
        rndBase = Random.Range(0, statBase.Count);
        newBase = statBase[rndBase];
        statBase.RemoveAt(rndBase);
        character.Stat[StatType.Magic] = newBase * STAT_BASE_MULTIPLIER + mod + BASE_START_STAT;
        statAve += mod;
        //For the last 3, it has to do things a little differently, bc it may have to balance things out
        //Defense
        if(statAve >= STAT_MOD_TO_BASE * 2) {
            mod = Random.Range(-STAT_MOD_TO_BASE, 1);
        }
        else if(statAve <= -STAT_MOD_TO_BASE * 2) {
            mod = Random.Range(0, STAT_MOD_TO_BASE + 1);
        }
        else {
            mod = Random.Range(-STAT_MOD_TO_BASE, STAT_MOD_TO_BASE + 1);
        }
        rndBase = Random.Range(0, statBase.Count);
        newBase = statBase[rndBase];
        statBase.RemoveAt(rndBase);
        character.Stat[StatType.Defense] = newBase * STAT_BASE_MULTIPLIER + mod + BASE_START_STAT;
        statAve += mod;
        //And for Endurance and Luck, things become even more restrictive
        //Endurance
        if(statAve >= STAT_MOD_TO_BASE * 2) {
            mod = -STAT_MOD_TO_BASE;
        }
        else if(statAve <= -STAT_MOD_TO_BASE * 2) {
            mod = STAT_MOD_TO_BASE;
        }
        else if(statAve == STAT_MOD_TO_BASE) {
            mod = Random.Range(-STAT_MOD_TO_BASE, 1);
        }
        else if(statAve == -STAT_MOD_TO_BASE) {
            mod = Random.Range(0, STAT_MOD_TO_BASE + 1);
        }
        else {
            mod = Random.Range(-STAT_MOD_TO_BASE, STAT_MOD_TO_BASE + 1);
        }
        rndBase = Random.Range(0, statBase.Count);
        newBase = statBase[rndBase];
        statBase.RemoveAt(rndBase);
        character.Stat[StatType.Endurance] = newBase * STAT_BASE_MULTIPLIER + mod + BASE_START_STAT;
        statAve += mod;
        //For Luck, there's only one possible value left
        mod = -statAve;
        newBase = statBase[0];
        statBase.RemoveAt(0);
        character.Stat[StatType.Luck] = newBase * STAT_BASE_MULTIPLIER + mod + BASE_START_STAT;

        //After assigning stats, the character is assigned their special ability
        character.AbilityString = ability.displayText;
        character.myAbilityReference = ability.reference;
        DeclareCharAbility(character, ability.reference);
        character.AbilityAffectedStats = new List<StatType>(ability.statsAffected);
        character.alive = true;

        //Finally, the character levels up to the needed level
        for(int i = 0; i<lvl; i++) {
            LevelUpCharacter(character);
        }
    }
    public void DeclareCharAbility(Character character, AbilityReference reference) {
        if(reference == AbilityReference.LvlIncreaseChoice) {
            character.OnLevelUpAbility = LevelUpIncreaseChoiceStat;
            character.OnQuestEndAbility = DoNothing;
            character.OnQuestStartAbility = DoNothing;
        }
        else if(reference == AbilityReference.LvlIncreaseSpecific) {
            character.OnLevelUpAbility = LevelUpIncreaseSpecificStats;
            character.OnQuestEndAbility = DoNothing;
            character.OnQuestStartAbility = DoNothing;
        }
        else if(reference == AbilityReference.QuestIncrease) {
            character.OnLevelUpAbility = DoNothing;
            character.OnQuestStartAbility = StartQuestIncreasePartyStat;
            character.OnQuestEndAbility = EndQuestIncreasePartyStat;
        }
    }
    //This function levels up a character once
    public void LevelUpCharacter(Character character) {
        character.statPoints += Character.STANDARD_STAT_POINTS;
        //Does any special abilities that occur on level up
        character.OnLevelUpAbility(character, character.AbilityAffectedStats);
        //every stat increases by one
        for(int i = 0; i < StatType.GetNames(typeof(StatType)).Length; i++) {
            character.StatIncrease[(StatType)i]++;
        }
        character.Level++;
        character.RefreshHealth();
    }

    //This ability increases specific abilities when the character levels up
    public void LevelUpIncreaseSpecificStats(Character self, List<StatType> statsAffected) {
        foreach(StatType stat in statsAffected) {
            self.StatIncrease[stat] += 1;
        }
    }
    //This ability increases an ability of the player's choice when they level up
    public void LevelUpIncreaseChoiceStat(Character self, List<StatType> statsAffected) {
        self.statPoints++;
    }

    //These next two work together to increase other party members stats during a quest
    private const float BONUS_DIVIDER = 2f;
    public void StartQuestIncreasePartyStat(Character self, List<StatType> statsAffected) {
        //NEED TO IMPLEMENT QUESTS MORE BEFORE USING THIS
        if(statsAffected.Contains(StatType.Endurance)) {
            //Endurance means it is a healing ability
        }
        else {
            foreach(StatType stat in statsAffected) {
                string tempString = self.CharacterName + " boosted their party member's " + stat + " with their special ability.";
                self.refQuest.QuestOccurences.Add(tempString);
                foreach(Character chara in self.refQuest.myParty.Members) {
                    if(chara != self) {
                        chara.StatModifier[stat] += Mathf.CeilToInt(self.Level / BONUS_DIVIDER);
                    }
                }
            }
        }
    }
    public void EndQuestIncreasePartyStat(Character self, List<StatType> statsAffected) {
        //NEED TO IMPLEMENT QUESTS MORE BEFORE USING THIS
        if(statsAffected.Contains(StatType.Endurance)) {
            //Endurance means it is a healing ability
            string tempString = self.CharacterName + " healed their party members with their special ability.";
            foreach(Character chara in self.refQuest.myParty.Members) {
                if(chara != self) {
                    chara.curHealth += Mathf.CeilToInt(self.Level / BONUS_DIVIDER);
                }
            }
        }
        else {
            foreach(StatType stat in statsAffected) {
                foreach(Character chara in self.refQuest.myParty.Members) {
                    if(chara != self) {
                        chara.StatModifier[stat] -= Mathf.CeilToInt(self.Level / BONUS_DIVIDER);
                    }
                }
            }
        }
    }



    //This do-nothing Ability is used for when a CharacterAbility does not need to make use of certain elements of the character's options
    public void DoNothing(Character self, List<StatType> statsAffected) {
        //Do nothing
    }
}
