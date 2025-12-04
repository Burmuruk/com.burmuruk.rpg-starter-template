using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    [Serializable]
    public struct BuffData
    {
        [Tooltip("Use it to remember it's functionality. It doesn't serves as ID.")]
        public string name;
        public ModifiableStat stat;
        public float value;
        [Tooltip("Time in seconds of the effect")]
        public float duration;
        public float rate;
        public bool percentage;
        [Range(0,1)] public float probability; //values between 0 - 1

        public static bool operator == (BuffData lhs, BuffData rhs)
        {
            return (lhs.value == rhs.value &&
                lhs.stat == rhs.stat &&
                lhs.probability == rhs.probability &&
                lhs.duration == rhs.duration &&
                lhs.rate == rhs.rate);
        }

        public static bool operator != (BuffData lhs, BuffData rhs)
        {
            return (lhs.value != rhs.value ||
                lhs.stat != rhs.stat ||
                lhs.probability != rhs.probability ||
                lhs.duration != rhs.duration ||
                lhs.rate != rhs.rate);
        }

        public override bool Equals(object obj)
        {
            var rhs = (BuffData)obj;

            return (this.value == rhs.value &&
                this.stat == rhs.stat &&
                this.probability == rhs.probability &&
                this.duration == rhs.duration &&
                this.rate == rhs.rate);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
