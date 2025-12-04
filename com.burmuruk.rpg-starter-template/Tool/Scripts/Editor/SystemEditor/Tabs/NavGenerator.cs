using Burmuruk.AI;
using Burmuruk.RPGStarterTemplate.Movement.PathFindig;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class NavGenerator : SubWindow
    {
        #region Variables
        [Header("General Settings")]
        [Space]
        [SerializeField] bool detectSize = false;
        [SerializeField] int layer = 9;
        [SerializeField] bool is3d;

        private Octree octree;
        private Graph waypoints;
        NodesList nodesList;
        IVisualElementScheduledItem drawingTimeOut = null;
        #endregion

        public VisualElement StatusContainer { get; private set; }
        public Label LblSceneName { get; private set; }
        public Label LblOctreeState { get; private set; }
        public Label LblMeshState { get; private set; }
        public Label LblSaved { get; private set; }
        public UnsignedIntegerField UILayer { get; private set; }
        public Toggle TglDetectSize { get; private set; }
        public VisualElement PointsContainer { get; private set; }
        public Toggle TglShowArea { get; private set; }
        public ObjectField P1 { get; private set; }
        public ObjectField P2 { get; private set; }
        public Toggle TglOctree { get; private set; }
        public FloatField NodeMinSize { get; private set; }
        public IntegerField MaxDepth { get; private set; }
        public Toggle TglMesh { get; private set; }
        public VisualElement OctreeControls { get; private set; }
        public VisualElement MeshControls { get; private set; }
        public Button BtnGenerate { get; private set; }
        public Button BtnDelete { get; private set; }
        public Button BtnSave { get; private set; }

        public void Initialize(VisualElement container, VisualElement buttonsContainer)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/SystemEditor/Tabs/NavGeneration.uxml");
            container.Add(visualTree.Instantiate());
            Initialize(container);

            StatusContainer = container.Q<VisualElement>("StatusContainer");
            LblSceneName = container.Q<Label>("LblSceneName");
            LblMeshState = container.Q<Label>("LblMeshState");
            LblOctreeState = container.Q<Label>("LblOctreeState");
            LblSaved = container.Q<Label>("LblSaved");
            UILayer = container.Q<UnsignedIntegerField>("UILayer");
            TglDetectSize = container.Q<Toggle>("TglDetectSize");
            TglShowArea = container.Q<Toggle>("TglShowArea");
            PointsContainer = container.Q<VisualElement>("PointsContainer");
            TglOctree = container.Q<Toggle>("TglIs3D");
            MaxDepth = container.Q<IntegerField>("IFMaxDepth");
            NodeMinSize = container.Q<FloatField>("FFMinSize");
            TglMesh = container.Q<Toggle>("TglMesh");
            OctreeControls = container.Q<VisualElement>("OctreeControls");
            MeshControls = container.Q<VisualElement>("MeshControls");
            BtnGenerate = buttonsContainer.parent.Q<Button>("BtnGenerate");
            BtnDelete = buttonsContainer.parent.Q<Button>("BtnDelete");
            BtnSave = buttonsContainer.parent.Q<Button>("BtnSaveNavigation");

            P1 = new ObjectField("Corner 1");
            P1.objectType = typeof(GameObject);
            PointsContainer.Add(P1);
            P2 = new ObjectField("Corner 2");
            P2.objectType = typeof(GameObject);
            PointsContainer.Add(P2);

            UILayer.RegisterValueChangedCallback(CheckLayer);
            TglDetectSize.RegisterValueChangedCallback(EnableSizeDetection);
            TglOctree.RegisterValueChangedCallback(EnableOctreeControls);
            TglMesh.RegisterValueChangedCallback(EnableMeshControls);
            TglShowArea.RegisterValueChangedCallback(EnableShowArea);
            BtnDelete.clicked += RemoveData;
            BtnGenerate.clicked += GenerateNavigation;
            BtnSave.clicked += SaveInfo;

            Clear();
            LoadInfo();
        }

        private void CheckLayer(ChangeEvent<uint> evt)
        {
            if (evt.newValue > 31)
            {
                UILayer.SetValueWithoutNotify(evt.previousValue);
            }

            layer = unchecked((int)UILayer.value);
        }

        private void EnableShowArea(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                Highlight(P1, false);
                Highlight(P2, false);
                bool isValid = true;

                if (!TglDetectSize.value)
                {
                    if (P1.value == null)
                    {
                        isValid = false;
                        Highlight(P1, true, BorderColour.Error);
                    }
                    if (P2.value == null)
                    {
                        isValid = false;
                        Highlight(P2, true, BorderColour.Error);
                    }
                }

                if (!isValid)
                {
                    TglShowArea.SetValueWithoutNotify(false);
                    return;
                }

                Draw();
            }

            if (nodesList != null)
                nodesList.showMeshZone = evt.newValue;
        }

        public bool LoadInfo()
        {
            LblSceneName.text = SceneManager.GetActiveScene().name;
            foreach (var element in _highlighted)
                Set_Tooltip(element.Key, element.Value, false);

            bool found = CheckNavFileStatus();

            LblMeshState.text = found ? "Generated" : "None";
            LblSaved.text = found ? "True" : "False";
            var colour = found ? BorderColour.Success : BorderColour.LightBorder;
            Highlight(StatusContainer, true, colour);

            BtnDelete.SetEnabled(found);
            return found;
        }

        private void SaveInfo()
        {
            try
            {
                nodesList.SaveList();
            }
            catch (Exception e)
            {
                Notify("The information couldn't be saved", BorderColour.Error, NotificationType.System);
                throw e;
            }

            Notify("Navigation saved", BorderColour.Success, NotificationType.System);
        }

        private void RemoveData()
        {
            NavSaver.RemoveFile(SceneManager.GetActiveScene().name);

            LoadInfo();
            Notify("Data removed", BorderColour.Success, NotificationType.System);
        }

        bool CheckNavFileStatus()
        {
            return NavSaver.Exists(SceneManager.GetActiveScene().name);
        }

        private void GenerateNavigation()
        {
            if (!VerifyData(out var errors))
            {
                Notify(errors.Count > 1 ? "Invalid data" : errors[0], BorderColour.Error, NotificationType.System);
                return;
            }

            if (TglOctree.value)
            {
                Notify("Generating", BorderColour.Success, NotificationType.System);
                CreateOctree();

                bool finished = octree.emptyLeaves.Count > 0;
                LblOctreeState.text = finished.ToString();
                Highlight(LblOctreeState);
                //BtnSave.SetEnabled(finished);
                //Highlight(BtnSave);
            }
            if (TglMesh.value)
            {
                Notify("Generating", BorderColour.Success, NotificationType.System);

                SetMeshAreaPoints();
                nodesList.layer = layer;
                CreateNavMesh();

                bool finished = nodesList.NodeCount > 0;
                LblMeshState.text = finished.ToString();
                Highlight(LblMeshState);
                BtnSave.SetEnabled(finished);
                Highlight(BtnSave);
            }

            Notify("Generated", BorderColour.Success, NotificationType.System);
        }

        private void SetMeshAreaPoints()
        {
            if (TglDetectSize.value)
            {
                DectecInitialSize(out Vector3 center, out Vector3 size);
                Vector3 x1 = new Vector3
                {
                    x = center.x - size.x * .5f - nodesList.NodeDistance,
                    y = center.y - size.y,
                    z = center.z - size.z * .5f - nodesList.NodeDistance
                };
                Vector3 x2 = new Vector3
                {
                    x = center.x + size.x * .5f + nodesList.NodeDistance,
                    y = center.y + size.y,
                    z = center.z + size.z * .5f + nodesList.NodeDistance
                };

                nodesList.x1 = x1;
                nodesList.x2 = x2;
                Debug.DrawRay(x1, Vector3.up * 20, Color.red, 10);
                Debug.DrawRay(x2, Vector3.up * 20, Color.red, 10);
            }
            else
            {
                nodesList.x1 = (P1.value as GameObject).transform.position;
                nodesList.x2 = (P2.value as GameObject).transform.position;
            }
        }

        private void EnableMeshControls(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                if (nodesList == null) 
                    nodesList = ScriptableObject.CreateInstance<NodesList>();

                MeshControls.Add(new InspectorElement(nodesList));
                EnableContainer(MeshControls, true);
                var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/com.burmuruk.rpg-starter-template/Tool/Prefabs/Editor/Navigation/Node.prefab");

                if (assets == null)
                    Debug.Log("Object not found");
                else if (assets.Length > 0)
                {
                    foreach (var node in assets)
                    {
                        if ((node as GameObject) != null)
                        {
                            nodesList.debugNode = node as GameObject;
                        }
                    }
                }

                if (nodesList.debugNode == null)
                {
                    Notify("Node prefab is missing!", BorderColour.Error, NotificationType.System);
                    return;
                }
            }
            else
                MeshControls.Clear();

            BtnGenerate.SetEnabled(evt.newValue || TglOctree.value);
            if (BtnGenerate.enabledSelf) Highlight(BtnGenerate);
        }

        private void EnableSizeDetection(ChangeEvent<bool> evt)
        {
            detectSize = evt.newValue;

            EnableContainer(PointsContainer, !evt.newValue);
        }

        private void EnableOctreeControls(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                bool isValid = true;

                if (NodeMinSize.value <= 0)
                {
                    isValid = false;
                    Highlight(NodeMinSize, true, BorderColour.Error);
                }
                if (MaxDepth.value <= 0)
                {
                    isValid = false;
                    Highlight(MaxDepth, true, BorderColour.Error);
                }

                if (!isValid)
                {
                    TglOctree.SetValueWithoutNotify(false);
                    return;
                }
            }

            EnableContainer(MaxDepth.parent, evt.newValue);
            EnableContainer(NodeMinSize.parent, evt.newValue);
            BtnGenerate.SetEnabled(evt.newValue || BtnDelete.enabledSelf);
            if (evt.newValue) Highlight(BtnGenerate);
        }

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            bool result = true;
            bool isValid = false;

            if (!TglDetectSize.value)
            {
                result &= isValid = P1.value != null;
                _highlighted[P1] = P1.tooltip;
                Set_ErrorTooltip(P1, "Value can't be empty", ref errors, isValid);
                result &= isValid = P2.value != null;
                _highlighted[P2] = P2.tooltip;
                Set_ErrorTooltip(P2, "Value can't be empty", ref errors, isValid);
            }
            if (TglOctree.value)
            {
                result &= isValid = NodeMinSize.value > 0;
                _highlighted[NodeMinSize] = NodeMinSize.tooltip;
                Set_ErrorTooltip(NodeMinSize, "Value must be greater than zero", ref errors, isValid);
                result &= isValid = MaxDepth.value > 0;
                _highlighted[MaxDepth] = MaxDepth.tooltip;
                Set_ErrorTooltip(MaxDepth, "Value must be greater than zero", ref errors, isValid);
            }
            if (TglMesh.value)
            {
                if (nodesList == null)
                {
                    nodesList = ScriptableObject.CreateInstance<NodesList>();
                }

                result &= isValid = nodesList.debugNode != null;
                //Highlight(nodesList, !isValue, BorderColour.Error);
            }

            return result;
        }

        public override ModificationTypes Check_Changes()
        {
            return ModificationTypes.None;
        }

        public override void Clear()
        {
            EnableContainer(OctreeControls, false);
            EnableContainer(MeshControls, false);
            EnableContainer(PointsContainer, false);
            TglOctree.value = false;
            TglMesh.value = false;
            TglDetectSize.value = true;
            EnableContainer(MaxDepth.parent, false);
            EnableContainer(NodeMinSize.parent, false);
            TglDetectSize.SetValueWithoutNotify(true);
            BtnDelete.SetEnabled(false);
            BtnGenerate.SetEnabled(false);
            BtnSave.SetEnabled(false);
            LblSaved.text = "False";

            NodeMinSize.value = 5;
            MaxDepth.value = 16;
            layer = unchecked((int)UILayer.value);
        }

        public override void Load_Changes()
        {
            throw new NotImplementedException();
        }

        #region Unity methods

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                octree.rootNode.Draw();
            }
        }
        #endregion

        #region Creation
        private void CreateOctree()
        {
            waypoints = new Graph();
            GetInitialSize(out Vector3 center, out Vector3 size, out float maxSize);

            OctreeNode.layer = layer;
            octree = new Octree(GetObjectsInArea(center, size), NodeMinSize.value, waypoints, MaxDepth.value);
        }

        private Octree CreateOctree(float minSize, int depth)
        {
            var waypoints = new Graph();
            GetInitialSize(out Vector3 center, out Vector3 size, out float maxSize);

            OctreeNode.layer = layer;
            return new Octree(GetObjectsInArea(center, size), minSize, waypoints, depth);
        }

        private void CreateNavMesh()
        {
            //var oct = CreateOctree(nodesList.NodeDistance, 20);
            //nodesList.SetAvailableAreaDetector(oct.IsPointAvailable);
            nodesList.ResetState();
            nodesList.Calculate_PathMesh();
            nodesList.CalculateNodesConnections();

            nodesList.Draw_Mesh();
            //nodesList.SaveList();
        }
        #endregion

        #region Initial size detection
        private void GetInitialSize(out Vector3 center, out Vector3 size, out float MaxSize)
        {
            if (detectSize)
            {
                DectecInitialSize(out center, out size);
            }
            else
                size = GetAreaSize(out center);

            float max1 = MathF.Max(size.x, size.y);
            MaxSize = MathF.Max(max1, size.z);
        }

        private void DectecInitialSize(out Vector3 center, out Vector3 size)
        {
            center = Vector3.zero;
            size = Vector3.one * 5;
            Bounds bounds = new Bounds(center, Vector3.one);
            var hits = Physics.BoxCastAll(center, size, Vector3.up, Quaternion.identity, .1f, 1 << layer);
            RaycastHit[] newHits = hits;

            do
            {
                hits = newHits;

                if (hits.Length > 0)
                {
                    foreach (var item in hits)
                    {
                        bounds.Encapsulate(item.collider.bounds);
                    }
                }
                else
                {
                    Debug.LogError("No objects found in the specified area. Please ensure there are objects to generate the octree.");
                    size = Vector3.zero;
                    return;
                }

                size = bounds.size;
                newHits = Physics.BoxCastAll(center, size, Vector3.up, Quaternion.identity, .1f, 1 << layer);
            } while (newHits.Length > hits.Length);

            Debug.DrawRay(bounds.center, Vector3.up * bounds.extents.y, Color.yellow, 5f);
            Debug.DrawRay(bounds.center, Vector3.down * bounds.extents.y, Color.yellow, 5f);
            Debug.DrawRay(bounds.center, Vector3.right * bounds.extents.x, Color.yellow, 5f);
            Debug.DrawRay(bounds.center, Vector3.left * bounds.extents.x, Color.yellow, 5f);
            //Debug.Log("Initial size detected: " + size + " at center: " + bounds.center);
            center = bounds.center;
        }

        private GameObject[] GetObjectsInArea(in Vector3 center, in Vector3 size)
        {
            var newSize = size * .5f;
            var hits = Physics.BoxCastAll(center, newSize, Vector3.up, Quaternion.identity, .1f, 1 << layer);

            return (from objects in hits select objects.collider.gameObject).ToArray();
        }

        private Vector3 GetAreaSize(out Vector3 center)
        {
            float x, y, z;
            float px, py, pz;

            (x, px) = GetSizeAndCenter(P1.transform.position.x, P2.transform.position.x);
            (y, py) = GetSizeAndCenter(P1.transform.position.y, P2.transform.position.y);
            (z, pz) = GetSizeAndCenter(P1.transform.position.z, P2.transform.position.z);

            center = new Vector3(px, py, pz);
            return new Vector3(x, y, z);

            (float distance, float position) GetSizeAndCenter(float x1, float x2)
            {
                return (x1 < 0, x2 < 0) switch
                {
                    (true, true) => GetCenterFromSize(x1, MathF.Abs(x1 - x2)),
                    (false, true) => GetCenterFromSize(x2, x1 + MathF.Abs(x2)),
                    (true, false) => GetCenterFromSize(x1, MathF.Abs(x1) + x2),
                    (false, false) => GetCenterFromSize(x1, MathF.Abs(x1 - x2)),
                };
            }

            (float distance, float center) GetCenterFromSize(in float start, in float distance) =>
                (distance, start + distance * .5f);
        }
        #endregion

        #region Drawing

        private void Draw()
        {
            drawingTimeOut?.Pause();

            drawingTimeOut = Container.schedule.Execute(() =>
            {
                DrawArea();

                if (TglShowArea.value)
                    Draw();
            });
            drawingTimeOut.ExecuteLater(1900);
        }

        void DrawArea()
        {
            if (!TglDetectSize.value &&
                nodesList != null && P1 != null && P2 != null)
            {
                nodesList.x1 = (P1.value as GameObject).transform.position;
                nodesList.x2 = (P2.value as GameObject).transform.position;

                Draw_Cube(P1.value as GameObject, P2.value as GameObject);
            }
            else
            {
                DectecInitialSize(out Vector3 center, out Vector3 size);
                Vector3 x1 = new Vector3
                {
                    x = center.x - size.x * .5f,
                    y = center.y - size.y * .5f,
                    z = center.z - size.z * .5f
                };
                Vector3 x2 = new Vector3
                {
                    x = center.x + size.x * .5f,
                    y = center.y + size.y * .5f,
                    z = center.z + size.z * .5f
                };
                Draw_Cube(x1, x2);
            }
        }

        public void Draw_Cube(GameObject x1, GameObject x2)
        {
            Vector3 dis = x2.transform.position - x1.transform.position;
            Debug.DrawLine(x1.transform.position, x1.transform.position + Vector3.right * dis.x, Color.red, 2);
            Debug.DrawLine(x1.transform.position, x1.transform.position + Vector3.forward * dis.z, Color.red, 2);
            Debug.DrawLine(x1.transform.position, x1.transform.position + Vector3.up * dis.y, Color.red, 2);

            Debug.DrawLine(x2.transform.position, x2.transform.position + Vector3.left * dis.x, Color.red, 2);
            Debug.DrawLine(x2.transform.position, x2.transform.position + Vector3.back * dis.z, Color.red, 2);
            Debug.DrawLine(x2.transform.position, x2.transform.position + Vector3.down * dis.y, Color.red, 2);

            var rd = x2.transform.position + Vector3.down * dis.y + Vector3.left * dis.x;
            Debug.DrawLine(rd, rd + Vector3.up * dis.y, Color.red, 2);
            Debug.DrawLine(rd, rd + Vector3.right * dis.x, Color.red, 2);
            Debug.DrawLine(rd + Vector3.up * dis.y, rd + Vector3.up * dis.y + Vector3.back * dis.z, Color.red, 2);

            var ld = x1.transform.position + Vector3.up * dis.y + Vector3.right * dis.x;
            Debug.DrawLine(ld, ld + Vector3.down * dis.y, Color.red, 2);
            Debug.DrawLine(ld, ld + Vector3.left * dis.x, Color.red, 2);
            Debug.DrawLine(ld + Vector3.down * dis.y, ld + Vector3.down * dis.y + Vector3.forward * dis.z, Color.red, 2);
        }

        public void Draw_Cube(Vector3 x1, Vector3 x2)
        {
            Vector3 dis = x2 - x1;
            Debug.DrawLine(x1, x1 + Vector3.right * dis.x, Color.red, 2);
            Debug.DrawLine(x1, x1 + Vector3.forward * dis.z, Color.red, 2);
            Debug.DrawLine(x1, x1 + Vector3.up * dis.y, Color.red, 2);

            Debug.DrawLine(x2, x2 + Vector3.left * dis.x, Color.red, 2);
            Debug.DrawLine(x2, x2 + Vector3.back * dis.z, Color.red, 2);
            Debug.DrawLine(x2, x2 + Vector3.down * dis.y, Color.red, 2);

            var rd = x2 + Vector3.down * dis.y + Vector3.left * dis.x;
            Debug.DrawLine(rd, rd + Vector3.up * dis.y, Color.red, 2);
            Debug.DrawLine(rd, rd + Vector3.right * dis.x, Color.red, 2);
            Debug.DrawLine(rd + Vector3.up * dis.y, rd + Vector3.up * dis.y + Vector3.back * dis.z, Color.red, 2);

            var ld = x1 + Vector3.up * dis.y + Vector3.right * dis.x;
            Debug.DrawLine(ld, ld + Vector3.down * dis.y, Color.red, 2);
            Debug.DrawLine(ld, ld + Vector3.left * dis.x, Color.red, 2);
            Debug.DrawLine(ld + Vector3.down * dis.y, ld + Vector3.down * dis.y + Vector3.forward * dis.z, Color.red, 2);
        }
        #endregion
    }
}