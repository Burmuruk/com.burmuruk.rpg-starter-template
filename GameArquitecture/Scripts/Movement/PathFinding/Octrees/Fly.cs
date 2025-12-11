using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Movement.PathFindig
{
    public class Fly : MonoBehaviour
    {
        float speed = 5;
        float accuracy = 1;
        float rotSpeed = 5;

        int currentWP = 1;
        Vector3 goal;
        OctreeNode currentNode;

        public GameObject octree;
        Graph graph;
        List<Node> pathList = new List<Node>();

        void Start()
        {
            Invoke("Navigate", 1);
        }

        void Navigate()
        {
            graph = octree.GetComponent<CreateOctree>().waypoints;
            currentNode = graph.nodes[currentWP].octreeNode;
            GetRandomDestination();
        }

        private void GetRandomDestination()
        {
            int randNode = Random.Range(0, graph.nodes.Count);
            graph.AStar(graph.nodes[currentWP].octreeNode, graph.nodes[randNode].octreeNode, pathList);
            currentWP = 0;
        }

        public int GetPathLength()
        {
            return pathList.Count;
        }

        public OctreeNode GetPathPoint(int index)
        {
            return pathList[index].octreeNode;
        }

        private void LateUpdate()
        {
            if (graph == null) return;

            if (GetPathLength() == 0 || currentWP == GetPathLength())
            {
                GetRandomDestination();
                return;
            }

            if (Vector3.Distance(GetPathPoint(currentWP).nodeBounds.center, transform.position) <= accuracy)
            {
                ++currentWP;
            }

            if (currentWP < GetPathLength())
            {
                goal = GetPathPoint(currentWP).nodeBounds.center;
                currentNode = GetPathPoint(currentWP);

                Vector3 lookAtGoal = new Vector3(goal.x, goal.y, goal.z);
                Vector3 direction = lookAtGoal - transform.position;

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotSpeed);

                transform.Translate(0, 0, speed * Time.deltaTime);
            }
            else
            {
                GetRandomDestination();

                if (GetPathLength() == 0)
                    Debug.Log("No path");
            }
        }
    }
}
