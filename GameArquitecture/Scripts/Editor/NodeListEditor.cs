using UnityEditor;
using UnityEngine;

namespace Burmuruk.AI
{
    //[CustomEditor(typeof(NodesList))]
    //public class NodeListEditor : Editor
    //{
    //    public NodesList nodeList;

    //    private void OnEnable()
    //    {
    //        nodeList = (NodesList)target;
    //    }

    //    public override void OnInspectorGUI()
    //    {
    //        DrawDefaultInspector();

    //        if (nodeList == null)
    //        {
    //            return;
    //        }

    //        string btnCreateName = (nodeList.MeshState, nodeList.ConnectionsState) switch
    //        {
    //            (pState.running, _) => "Creating Mesh",
    //            (_, pState.running) => "Calculating Conections",
    //            (pState.None, _) => "Create Mesh",
    //            (pState.finished, pState.None) => "Calculate Conections",
    //            (pState.finished, pState.finished) => "Recalculate Conections",
    //            _ => "",
    //        };

    //        GUILayout.Space(10);

    //        if (GUILayout.Button(btnCreateName))
    //        {
    //            if (nodeList.MeshState == pState.None)
    //                nodeList.Calculate_PathMesh();
    //            else if (nodeList.ConnectionsState == pState.None || nodeList.ConnectionsState == pState.finished)
    //                nodeList.CalculateNodesConnections();
    //        }

    //        string btnDeleteName = (nodeList.MeshState, nodeList.ConnectionsState) switch
    //        {
    //            (pState.running, _) => "",
    //            (_, pState.running) => "",
    //            (pState.None, _) => "",
    //            (pState.finished, pState.None) => "Delete Nodes",
    //            (pState.finished, pState.finished) => "Remove Conections",
    //            _ => "",
    //        };

    //        if (nodeList.meshState == pState.finished)
    //        {
    //            if (GUILayout.Button(btnDeleteName))
    //            {
    //                if (nodeList.ConnectionsState == pState.finished)
    //                {
    //                    nodeList.ClearNodeConnections();
    //                }
    //                else if (nodeList.MeshState == pState.finished)
    //                {
    //                    nodeList.Destroy_Nodes();
    //                }
    //            }
    //        }

    //        if (nodeList.connectionsState == pState.finished && nodeList.meshState == pState.finished)
    //        {
    //            GUILayout.Space(10);

    //            string btnToJson = nodeList.memoryFreed switch
    //            {
    //                pState.None => "Free space",
    //                pState.running => "Freeing space",
    //                pState.finished => "Space freed",
    //                _ => "",
    //            };

    //            if (GUILayout.Button(btnToJson))
    //            {
    //                nodeList.SaveList();
    //            }
    //        }

    //        //GUILayout.Space(10);

    //        //string btnDjksName = nodeList.DijkstraState switch
    //        //{
    //        //    pState.None => "Calculate Dijkstra",
    //        //    pState.running => "Running Dijkstra",
    //        //    pState.finished => "Recalcular Dijkstra",
    //        //    _ => "",
    //        //};

    //        //if (GUILayout.Button(btnDjksName))
    //        //{
    //        //    nodeList.Calculate_Dijkstra();
    //        //}

    //        //string btnDelDijkstra = "Delete Dijkstra";

    //        //if (nodeList.DijkstraState == pState.running)
    //        //    btnDelDijkstra = "Deleting Dijkstra";
    //        //else if (nodeList.DijkstraState == pState.None)
    //        //    btnDelDijkstra = "";

    //        //if (GUILayout.Button(btnDelDijkstra))
    //        //{
    //        //    nodeList.Clear_Dijkstra();
    //        //}

    //        EditorUtility.SetDirty(target);
    //    }
    //} 
}
