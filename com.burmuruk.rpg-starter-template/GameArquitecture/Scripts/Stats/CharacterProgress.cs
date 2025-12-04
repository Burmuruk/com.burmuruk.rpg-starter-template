using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    [CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/CharacterProgress", order = 1)]
    public class CharacterProgress : ScriptableObject
    {
        [SerializeField] bool initilized;
        [SerializeField] CharacterData[] statsList;

        Dictionary<CharacterType, (bool aplyForAll, Dictionary<int, BasicStats> levels)> levelStatsDic;

        [Serializable]
        public struct CharacterData
        {
            public CharacterType characterType;
            public bool applyForAllLevels; // if true, all levels will have the same stats as the first level
            public List<LevelData> levelData;
        }

        [Serializable]
        public struct LevelData
        {
            public int level;
            public BasicStats stats;
        }

        public BasicStats? GetDataByLevel(CharacterType type, int level)
        {
            if (levelStatsDic == null)
                Initlize();

            if (!levelStatsDic.ContainsKey(type)) return null;
            if (!levelStatsDic[type].levels.ContainsKey(level))
                return null;

            return levelStatsDic[type].levels[level];
        }

        public bool ApplyForAll(CharacterType type)
        {
            if (levelStatsDic == null)
                Initlize();

            if (!levelStatsDic.ContainsKey(type)) return false;

            return levelStatsDic[type].aplyForAll;
        }

        private void Initlize()
        {
            //if (initilized) return;
            levelStatsDic = new Dictionary<CharacterType, (bool forAll, Dictionary<int, BasicStats> levels)>();

            foreach ( var statData in statsList)
            {
                if (levelStatsDic.ContainsKey(statData.characterType))
                    continue;

                levelStatsDic[statData.characterType] = (statData.applyForAllLevels, new Dictionary<int, BasicStats>());

                if (statData.levelData == null)
                    continue;

                foreach (var levelData in statData.levelData)
                {
                    levelStatsDic[statData.characterType].levels[levelData.level] = levelData.stats;
                }
            }

            //initilized = true;
        }

        public void SetData(CharacterType characterType, bool applyForAllLevels, List<LevelData> list)
        {
            levelStatsDic = null;
            //levelStatsDic ??= new Dictionary<CharacterType, (bool forAll, Dictionary<int, BasicStats> levels)>();

            //if (levelStatsDic.ContainsKey(characterType)) return;

            //var stats = new Dictionary<int, BasicStats>();

            //foreach (LevelData progression in list)
            //{
            //    stats.TryAdd(progression.level, progression.stats);
            //}

            //levelStatsDic[characterType] = (applyForAllLevels, stats);

            if (statsList != null)
            {
                for (int i = 0; i < statsList.Length; i++)
                {
                    if (statsList[i].characterType == characterType)
                    {
                        statsList[i].applyForAllLevels = applyForAllLevels;
                        statsList[i].levelData = list;
                        return;
                    }
                } 
            }

            List<CharacterData> newStats = new();

            if (statsList != null)
                newStats.AddRange(statsList.ToList());

            newStats.Add(new CharacterData
            {
                characterType = characterType,
                applyForAllLevels = applyForAllLevels,
                levelData = list
            });

            statsList = newStats.ToArray();
        }
    }
}
