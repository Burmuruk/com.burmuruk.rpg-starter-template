using UnityEngine;

[ExecuteAlways]
public class TestWatcher : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.Log("TestWatcher OnDisable", this);
    }

    private void OnEnable()
    {
        Debug.Log("TestWatcher OnEnable", this);
    }
}
