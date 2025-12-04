using System;
using UnityEngine;

namespace Burmuruk.WorldG.Patrol
{
    public struct CopyData
    {
        public bool wasSelected;
        public PatrolNode point;

        public CopyData(bool wasSelected, PatrolNode node)
        {
            this.wasSelected = wasSelected;
            point = node;
        }
    }

    //[ExecuteInEditMode]
    public class PatrolPoint : MonoBehaviour
    {
        #region variables
        public NodeData nodeData = null;
        
        [NonSerialized] public static CopyData copyData;

        public event Action<ISplineNode, ISplineNode> OnNodeAdded;
        public event Action<ISplineNode> OnNodeRemoved;
        private float selectionTime = 1f;
        private float currentTime = 0;
        private bool isTiming = false;
        private bool isSelected = false;
        private uint id;
        #endregion

        #region properties
        public bool IsSelected { get => isSelected; }
        public Vector3 Position { get => transform.position; }

        public uint ID => id;

        public NodeData NodeData => nodeData;

        public Transform Transform => transform;

        public PatrolController PatrolController { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        #region overrides
        public static bool operator true(PatrolPoint p) => p != null;

        public static bool operator false(PatrolPoint p) => p == null;

        #endregion

        #region unity methods
        private void Awake()
        {
            if (copyData.wasSelected)
            {
                //copyData.point.OnNodeAdded?.Invoke(copyData.point, this);
            }
        }

        private void OnDisable()
        {
            //OnNodeRemoved?.Invoke(this);
        }

        private void Update()
        {
            //if (isTiming)
            //{
            //    if (currentTime < selectionTime)
            //        currentTime += Time.deltaTime;
            //    else
            //        Deselect(); 
            //}
        }

        private void LateUpdate()
        {
            //if (isSelected && !isTiming)
            //{
            //    copyData.point = this;
            //    copyData.wasSelected = true;

            //    currentTime = 0;
            //    print("selected " + transform.name);
            //    isTiming = true;
            //}

            //isSelected = false;
        }

        //[DrawGizmo(GizmoType.Consumable & GizmoType.Selected)]
        private void OnDrawGizmos()
        {
            Gizmos.color = nodeData.NodeColor;
            Gizmos.DrawSphere(transform.position + Vector3.up * (float)nodeData.VerticalOffset, (float)nodeData.Radius);
        }

        private void OnDrawGizmosSelected()
        {
            Select();
            isSelected = true;
        }
        #endregion

        #region private methods
        private void Select()
        {
            //copyData = new CopyData(true, this);
        }

        private void Deselect()
        {
            if (copyData.point != this) return;

            copyData.point = null;
            copyData.wasSelected = false;
            isTiming = false;
        }

        public void SetNodeData(NodeData nodeData)
        {
            throw new NotImplementedException();
        }
        #endregion
    } 
}


