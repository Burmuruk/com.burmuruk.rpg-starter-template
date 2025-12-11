using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.AI
{
    public class DynamicNavObject : MonoBehaviour
    {
        [SerializeField] bool isEnable = true;

        public List<uint> Nodes { get; private set; } = new();
        public bool IsEnable { get => isEnable; }

        public event Action<List<uint>> OnModified;

        public void SetNodes(List<uint> nodes)
        {
            this.Nodes = nodes;
        }

        public void Enable(bool shouldEnable = true)
        {
            isEnable = shouldEnable;
            OnModified?.Invoke(Nodes);
        }

        public void ChangeLayer(int layer)
        {

        }
    }
}
