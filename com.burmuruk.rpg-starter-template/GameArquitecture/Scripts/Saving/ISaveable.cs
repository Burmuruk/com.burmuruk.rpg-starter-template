using System.Collections;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public interface ISaveable
    {
        int ID { get; }
        object CaptureState();
        void RestoreState(object args);
    }
}