using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control
{
    public class PlayerData
    {
        public readonly int PlayerId;

        public Color PlayerColor { get; set; }
        public Gender PlayerGender { get; set; }

        public PlayerData()
        {
            PlayerId = GetHashCode();
        }

        public PlayerData(int id)
        {
            PlayerId = id;
        }

        public enum Gender
        {
            None,
            Male,
            Female
        }
    }
}
