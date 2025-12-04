using UnityEngine;

namespace Burmuruk.WorldG
{
    public class BezierDraw : MonoBehaviour
    {
        public Transform[] points = new Transform[4];
        public Transform target;
        public float tValue = 2;
        public float rate = .1f;
        Vector3 curPoint = default;

        Matrix4x4 bezier = new Matrix4x4(
            new Vector4(1, -3, 3, -1),
            new Vector4(0, 3, -6, 3),
            new Vector4(0, 0, 3, -3),
            new Vector4(0, 0, 0, 1)
        );
        Vector4 hi;


        // StartAction is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            curPoint = points[0].position;
            for (float i = 0; i < tValue; i += rate)
            {
                var nextPoint = Get_Position(i);
                Debug.DrawLine(curPoint, nextPoint);
                curPoint = nextPoint;
            }


        }

        private Vector3 Get_Position(float t)
        {
            //var l = 3 * Mathf.Pow(1 - t, 2) * (points[1].position - points[0].position) + 6 * (1 - t) * t * (points[2].position - points[1].position) + 3 * Mathf.Pow(t, 3) * (points[3].position - points[2].position);
            var p = Mathf.Pow(1 - t, 3) * points[0].position + 3 * Mathf.Pow(1 - t, 2) * t * points[1].position + 3 * (1 - t) * Mathf.Pow(t, 2) * points[2].position + Mathf.Pow(t, 3) * points[3].position;
            return p;
        }
    } 
}
