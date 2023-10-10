using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraEventMask : MonoBehaviour
{
    // Used to catch touches only on units when upgrading
    [SerializeField] private LayerMask _inputLayerMask;
    
    private void Start()
    {
        GetComponent<Camera>().eventMask = _inputLayerMask;
    }

}
