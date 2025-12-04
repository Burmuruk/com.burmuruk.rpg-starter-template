using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Movement.PathFindig
{
    public struct OctreeObject
    {
        public GameObject gameObject;
        public Bounds bounds;

        public OctreeObject(GameObject obj)
        {
            gameObject = obj;
            bounds = obj.GetComponent<Collider>().bounds;
        }
    }

    public class OctreeNode
    {
        public int id;
        public Bounds nodeBounds;
        public OctreeNode[] children;
        public List<OctreeObject> containedObjects = new();
        public OctreeNode parent;
        public float minSize;
        public int depth;
        public static int max_depth = 16;
        public static int layer = 9;

        private static int idCounter = 0;

        public OctreeNode(Bounds bounds, float minSize, OctreeNode parent, int depth)
        {
            this.nodeBounds = bounds;
            this.minSize = minSize;
            this.parent = parent;
            this.depth = depth;
            this.id = idCounter++;
        }

        public void AddObject(GameObject go)
        {
            DivideAndAdd(new OctreeObject(go));
        }

        public void DivideAndAdd(OctreeObject octObj)
        {
            if (depth >= max_depth || nodeBounds.size.y <= minSize)
            {
                containedObjects.Add(octObj);
                return;
            }

            if (children == null)
            {
                children = new OctreeNode[8];
            }

            bool addedToChild = false;
            float quarter = nodeBounds.extents.y / 2;
            float childLength = nodeBounds.extents.y;
            Vector3 childSize = Vector3.one * childLength;

            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = new Vector3(
                    ((i & 1) == 0 ? -1 : 1) * quarter,
                    ((i & 2) == 0 ? 1 : -1) * quarter,
                    ((i & 4) == 0 ? -1 : 1) * quarter
                );
                Vector3 childCenter = nodeBounds.center + offset;
                Bounds childBounds = new(childCenter, childSize);

                if (Physics.CheckBox(childCenter + Vector3.up * .1f, childBounds.extents, Quaternion.identity, layer))
                //if (childBounds.Intersects(octObj.bounds))
                {
                    if (children[i] == null)
                    {
                        children[i] = new OctreeNode(childBounds, minSize, this, depth + 1);
                    }

                    children[i].DivideAndAdd(octObj);
                    addedToChild = true;
                }
                //Debug.DrawRay(nodeBounds.center, offset, Color.yellow, 5f);
                //Debug.DrawRay(childCenter, Vector3.down * childBounds.extents.y, Color.cyan, 5f);
            }

            if (!addedToChild)
            {
                containedObjects.Add(octObj);
            }
        }

        public void Draw()
        {
            Gizmos.color = Color.red;
            foreach (var obj in containedObjects)
            {
                Gizmos.DrawWireCube(obj.bounds.center, obj.bounds.size);
            }

            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child != null)
                        child.Draw();
                }
            }
            else if (containedObjects.Count != 0)
            {
                Gizmos.color = new Color(0, 0, 1, 0.25f);
                Gizmos.DrawCube(nodeBounds.center, nodeBounds.size);
            }
        }
    } 
}
