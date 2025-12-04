using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Combat
{
     public class AbilitiesManager : MonoBehaviour
     {
        Movement.Movement m_movement;
        Queue<Coroutine> coolDowns = new Queue<Coroutine>();
        CoolDownAction cd_Action;

        public static readonly Dictionary<AbilityType, Action<Character, object, Action>> habilitiesList = new()
        {
            { AbilityType.Dash, Dash },
            { AbilityType.Jump, Jump },
            { AbilityType.StealHealth, StealLife },
        };

        public static void Dash(Character character, object args, Action callback)
        {
            //Vector3 dir = (Vector3) direction;

            //statsList.Speed += 2;
            print("In Dash!!!");
        }

        public static void Jump(Character character, object args, Action callback)
        {
            print("Jump!!!");
        }

        public static void StealLife(Character character, object args, Action callback)
        {
            print("In Dash!!!");
        }
    }

    public enum AbilityType
    {
        None,
        Dash,
        Jump,
        StealHealth
    }
}
