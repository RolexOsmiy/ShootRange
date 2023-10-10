using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform _camTr;
    private Transform _selfTr;
    private void Awake()
    {
        _camTr = Camera.main.transform;
        _selfTr = transform;
    }

    private void Update()
    {
        _selfTr.LookAt(_camTr);
    }
}
