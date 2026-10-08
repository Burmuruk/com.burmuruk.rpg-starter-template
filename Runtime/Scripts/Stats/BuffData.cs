using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    [Serializable]
    public struct BuffData
    {
        [Tooltip("Description only; each application has its own ID.")]
        public string name;
        public ModifiableStat stat;
        [Tooltip("Instant: once; Periodic: ticks; Temporary: timed modifier; UntilRemoved: manual modifier.")]
        public EffectType effectType;
        public float value;
        [Tooltip("Seconds. Required for Periodic and Temporary.")]
        public float duration;
        [Tooltip("Seconds between ticks. First tick occurs after this interval.")]
        public float rate;
        [Tooltip("10 means 10%. Modifiers use a snapshot of the unmodified value.")]
        public bool percentage;
        [Range(0,1)] 
        public float probability; //values between 0 - 1
        [Tooltip("Visual effect to display when the buff is applied.")]
        public VisualBuff visual;

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

    public enum EffectType
    {
        Instant = 0,
        Periodic = 1,
        Temporary = 2,
        UntilRemoved = 3
    }
}
