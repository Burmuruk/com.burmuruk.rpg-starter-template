using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.Samples
{
    public class LookAtCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            var inversedCameraPosition = transform.position + (transform.position - Camera.main.transform.position);

            Vector3 h = transform.position - Camera.main.transform.position;
            float angle = Vector3.Angle(Camera.main.transform.forward, h);
            float co = Mathf.Sin(angle) * h.magnitude;

            var finalPoint = inversedCameraPosition + Vector3.up * co;
            transform.LookAt(finalPoint);
            //transform.rotation = Quaternion.Euler(new Vector3(finalPoint.x, Camera.main.transform.rotation.eulerAngles.y, 0));
        }
    }
}
