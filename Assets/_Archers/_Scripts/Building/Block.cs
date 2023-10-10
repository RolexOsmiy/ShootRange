using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class Block : MonoBehaviour
{
    private const float kGridSize = 2f;

    [Button(null, EButtonEnableMode.Editor)]
    private void UpBlock()
    {
        transform.Translate(Vector3.up * kGridSize);
    }
    
    [Button(null, EButtonEnableMode.Editor)]
    private void DownBlock()
    {
        transform.Translate(Vector3.down * kGridSize);
    }
    
    [Button(null, EButtonEnableMode.Editor)]
    private void RotateCW()
    {
        transform.Rotate(Vector3.up, 90);
    }
    
    [Button(null, EButtonEnableMode.Editor)]
    private void RotateCCW()
    {
        transform.Rotate(Vector3.up, -90);
    }
}
