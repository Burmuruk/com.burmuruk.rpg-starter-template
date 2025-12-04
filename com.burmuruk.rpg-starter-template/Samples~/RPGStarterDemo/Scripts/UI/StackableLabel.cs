using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    [Serializable]
    public record StackableLabel
    {
        public GameObject container;
        public StackableNode node;
        public float showingTime;
        public int amount;
        public int maxAmount;
        public bool instanciateParent = false;

        private ObjectPool<StackableNode> pool;
        public List<StackableNode> activeNodes { get; private set; }

        public void Initialize()
        {
            pool = new ObjectPool<StackableNode>(CreateElement, GetElement, ReleaseElement, RemoveElement, defaultCapacity: amount, maxSize: maxAmount);
            activeNodes = new List<StackableNode>();
        }

        private void ReleaseElement(StackableNode node)
        {
            node.label.transform.parent.gameObject.SetActive(false);
        }

        public StackableNode Get()
        {
            return pool.Get();
        }

        public void Release(int idx = 0)
        {
            try
            {
                Release(activeNodes[idx]);
            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }

        public void Release(StackableNode node)
        {
            pool.Release(node);
            activeNodes.Remove(node);
        }

        private void GetElement(StackableNode node)
        {
            node.label.transform.parent.gameObject.SetActive(true);
            activeNodes.Add(node);
        }

        private StackableNode CreateElement()
        {
            if (pool.CountAll <= 0)
                return node;

            GameObject newLabel;

            if (instanciateParent)
            {
                newLabel = UnityEngine.MonoBehaviour.Instantiate(container, container.transform.parent);
            }
            else
            {
                newLabel = UnityEngine.MonoBehaviour.Instantiate(node.label.transform.parent.gameObject, node.label.transform.parent.parent);
            }

            var all = newLabel.GetComponentsInChildren<Image>(true);
            Image newImage = null;
            foreach (var image in all)
                if (image.transform.name == "MainItemImage")
                {
                    newImage = image;
                    break;
                }

            StackableNode newNode = node with
            {
                label = newLabel.GetComponentInChildren<TextMeshProUGUI>(),
                image = newImage
            };

            return newNode;
        }

        private void RemoveElement(StackableNode node)
        {
            UnityEngine.MonoBehaviour.Destroy(node.label);
        }
    }

    [Serializable]
    public record StackableNode
    {
        public TMPro.TextMeshProUGUI label;
        public Image image;
    }
}
