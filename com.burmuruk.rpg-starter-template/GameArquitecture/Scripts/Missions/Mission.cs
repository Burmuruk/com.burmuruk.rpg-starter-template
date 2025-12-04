using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Missions
{
    [Serializable]
    public abstract class Mission
    {
        [SerializeField] string name;
        [SerializeField] string description;

        public bool Completed { get; set; }
        public float Progress { get; set; }
        public bool Started { get; set; }
        public string Name { get => name; set => name = value; }
        public string Description { get => description; set => description = value; }

        public event Action<Mission> OnFinished;
        public event Action<Mission> OnStarted;

        public virtual void Start() => OnStarted?.Invoke(this);

        public virtual void Finish() => OnFinished?.Invoke(this);
    }
}
