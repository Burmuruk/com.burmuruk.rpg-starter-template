using System.Collections;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Movement.PathFindig
{
    public class CreateOctree : MonoBehaviour
    {
        public GameObject[] worldObjects;
        public int nodeMinSize = 5;
        public Octree ot;
        public Graph waypoints;
        public int maxDepth = 16;

        void Start()
        {
            waypoints = new Graph();
            ot = new Octree(worldObjects, nodeMinSize, waypoints, maxDepth);
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                ot.rootNode.Draw();
            }
        }
    } 
}