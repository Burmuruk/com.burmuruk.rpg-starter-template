using Burmuruk.Collections;
using Burmuruk.WorldG.Patrol;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Burmuruk.AI
{
    public class DebugNode : MonoBehaviour
    {
        private List<Vector3> lineBuffer;
        LinkedGrid<IPathNode> nodes;

        public void SetNodes(LinkedGrid<IPathNode> nodes)
        {
            if (nodes == null) return;
            this.nodes = nodes;
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR

            if (nodes == null || nodes.Count <= 0) return;

            Handles.color = Color.blue;

            if (lineBuffer == null || lineBuffer.Count <= 0)
            {
                lineBuffer = new();

                var enumerator = (LinkedGridEnumerator<IPathNode>)nodes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    foreach (var connection in enumerator.Current.NodeConnections)
                    {
                        lineBuffer.Add(enumerator.Current.Position);
                        lineBuffer.Add(connection.node.Position);
                    }
                }
            }

            Handles.DrawLines(lineBuffer.ToArray());
#endif
        }
    }
}
