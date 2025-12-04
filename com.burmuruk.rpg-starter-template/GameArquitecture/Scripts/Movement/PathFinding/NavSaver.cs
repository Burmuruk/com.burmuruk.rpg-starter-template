using Burmuruk.AI;
using Burmuruk.WorldG.Patrol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Burmuruk.RPGStarterTemplate.Movement.PathFindig
{
    public static class NavSaver
    {
        static bool saved;
        static float radious;
        static float distance;
        static float maxAngle;
        static int writeX = 0, writeY = 0, writeZ = 0;

        static Dictionary<uint, Queue<((int x, int y, int z) idx, int connectionIdx)>> nodesWaiting;
        static Dictionary<uint, IPathNode> addedNodes;

        const string FILE_NAME = "NavGrid";
        static private bool working = false;
        static bool loaded;
        public static NodeListSuplier NodeList { get; private set; }

        public static event Action OnPathLoaded;

        public static bool Saved { get => saved; }
        public static bool Loaded { get => loaded; private set => loaded = value; }

        public static void Restart()
        {
            if (working) return;

            saved = false;
            nodesWaiting = null;
            addedNodes = null;
            loaded = false;
        }

        public static void SaveList(IPathNode[][][] nodes, int count)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string path = Path.Combine(Application.streamingAssetsPath, FILE_NAME + "_" + sceneName + ".txt");

            if (!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);

            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                int sizeX = nodes.Length;
                writer.WriteLine($"NODES {sizeX}");

                // Guardar dimensiones reales
                for (int x = 0; x < sizeX; x++)
                {
                    int sizeY = nodes[x].Length;
                    int sizeZ = sizeY > 0 ? nodes[x][0].Length : 0;

                    writer.WriteLine($"DIM {sizeY} {sizeZ}");
                }

                // Guardar nodos
                for (int x = 0; x < sizeX; x++)
                {
                    for (int y = 0; y < nodes[x].Length; y++)
                    {
                        for (int z = 0; z < nodes[x][y].Length; z++)
                        {
                            var n = nodes[x][y][z];
                            writer.WriteLine($"NODE {n.ID} {n.Position.x} {n.Position.y} {n.Position.z}");
                        }
                    }
                }

                // Guardar conexiones
                for (int x = 0; x < sizeX; x++)
                {
                    for (int y = 0; y < nodes[x].Length; y++)
                    {
                        for (int z = 0; z < nodes[x][y].Length; z++)
                        {
                            var n = nodes[x][y][z];
                            string conn = string.Join(" ", n.NodeConnections.Select(c => c.node.ID));
                            writer.WriteLine($"CONN {n.ID} {conn}");
                        }
                    }
                }

                writer.WriteLine("END");
            }

            AssetDatabase.Refresh();
            saved = true;
        }

        public static bool Exists(string sceneName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, FILE_NAME + "_" + sceneName + ".txt");
            return File.Exists(path);
        }

        public static void RemoveFile(string sceneName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, FILE_NAME + "_" + sceneName + ".txt");

            File.Delete(path);
            AssetDatabase.Refresh();
        }

        private static void Write(LinkedList<string> text)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string path = Path.Combine(Application.streamingAssetsPath, FILE_NAME + "_" + sceneName + ".txt");

            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }

            using (Stream stream = new FileStream(path, FileMode.Create))
            {
                foreach (var item in text)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(item);
                    stream.Write(bytes, 0, item.Length);
                }
            }

            AssetDatabase.Refresh();
        }

        //private static string GetNodeConnections(IPathNode node)
        //{
        //    string connections = "";

        //    foreach (var cnc in node.NodeConnections)
        //    {
        //        connections += cnc.node.ID + "*";
        //    }

        //    return connections += ")";
        //}

        private static string GetNodeConnections(IPathNode node)
        {
            return string.Join(",", node.NodeConnections.Select(c => c.node.ID));
        }


        public static void SaveExtraData(float radious, float distance, float maxAngle)
        {
            NavSaver.distance = distance;
            NavSaver.maxAngle = maxAngle;
            NavSaver.radious = radious;
        }

        private static void SetNodeList(IPathNode[][][] nodes)
        {
            NodeList = new NodeListSuplier(nodes);

            NodeList.SetTarget(radious, distance, maxAngle);
        }

        public static void LoadNavMesh()
        {
            if (Loaded || working) return;

            working = true;
            SynchronizationContext context = SynchronizationContext.Current;
            SetNodeList(GenerateNodesArray());

            Loaded = true;
            working = false;
            //Task<IPathNode[][][]> loadDataTask = Task.Run(() => GenerateNodesArray());
            //loadDataTask.ContinueWith((antecedent) =>
            //{
            //    SetNodeList(antecedent.Result);

            //    Loaded = true;
            //    working = false;

            //    context.Post(_ => OnPathLoaded?.Invoke(), null);
            //}, TaskContinuationOptions.ExecuteSynchronously);
        }

        private static IPathNode[][][] GenerateNodesArray()
        {
            addedNodes = new();
            nodesWaiting = new();

            string sceneName = SceneManager.GetActiveScene().name;
            string path = Path.Combine(Application.streamingAssetsPath, FILE_NAME + "_" + sceneName + ".txt");

            string[] lines = File.ReadAllLines(path);
            int lineIndex = 0;

            // Leer cantidad X
            string[] header = lines[lineIndex++].Split(' ');
            int sizeX = int.Parse(header[1]);

            // Leer las dimensiones Y,Z de cada X
            int[] sizeY = new int[sizeX];
            int[] sizeZ = new int[sizeX];

            for (int x = 0; x < sizeX; x++)
            {
                string[] dim = lines[lineIndex++].Split(' ');
                sizeY[x] = int.Parse(dim[1]);
                sizeZ[x] = int.Parse(dim[2]);
            }

            // Crear la matriz real
            IPathNode[][][] nodes = new IPathNode[sizeX][][];

            for (int x = 0; x < sizeX; x++)
            {
                nodes[x] = new IPathNode[sizeY[x]][];

                for (int y = 0; y < sizeY[x]; y++)
                {
                    nodes[x][y] = new IPathNode[sizeZ[x]];
                }
            }

            int cx = 0, cy = 0, cz = 0;

            // Leer nodos
            for (; lineIndex < lines.Length; lineIndex++)
            {
                if (lines[lineIndex].StartsWith("NODE"))
                {
                    string[] p = lines[lineIndex].Split(' ');

                    uint id = uint.Parse(p[1]);
                    float px = float.Parse(p[2]);
                    float py = float.Parse(p[3]);
                    float pz = float.Parse(p[4]);

                    ScrNode node = new ScrNode(id, new Vector3(px, py, pz));

                    nodes[cx][cy][cz] = node;
                    addedNodes[id] = node;

                    cz++;
                    if (cz >= sizeZ[cx])
                    {
                        cz = 0; cy++;
                        if (cy >= sizeY[cx])
                        {
                            cy = 0; cx++;
                        }
                    }
                }
                else break;
            }

            // Leer conexiones
            for (; lineIndex < lines.Length; lineIndex++)
            {
                if (lines[lineIndex].StartsWith("CONN"))
                {
                    string[] p = lines[lineIndex].Split(' ');

                    uint id = uint.Parse(p[1]);
                    ScrNode node = (ScrNode)addedNodes[id];

                    List<NodeConnection> conns = new();

                    for (int i = 2; i < p.Length; i++)
                    {
                        if (uint.TryParse(p[i], out uint cid))
                        {
                            if (addedNodes.TryGetValue(cid, out IPathNode target))
                                conns.Add(new NodeConnection(target, ConnectionType.BIDIMENSIONAL, 0.5f));
                        }
                    }

                    node.SetConnections(conns);
                }
            }

            return nodes;
        }
    }
}
