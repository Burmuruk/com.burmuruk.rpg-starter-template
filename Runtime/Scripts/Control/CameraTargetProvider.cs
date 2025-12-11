using Cinemachine;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control
{
    class CameraTargetProvider : MonoBehaviour
    {
        //[Header("Referenes")]
        PlayerManager playerManager;
        new CinemachineVirtualCamera camera;
        CinemachineVirtualCamera lastCamera;

        private void Awake()
        {
            playerManager = FindObjectOfType<PlayerManager>();
            playerManager.OnPlayerChanged += SetTarget;
            camera = GetComponent<CinemachineVirtualCamera>();
        }

        void SetTarget()
        {
            camera.Follow = playerManager.CurPlayer.transform;
        }

        public void DisableCurrentCamera()
        {
            lastCamera = camera;
            camera.gameObject.SetActive(false);
        }

        public void EnableLastCamera()
        {
            if (lastCamera == null) return;

            lastCamera.gameObject.SetActive(true);
        }
    }
}
