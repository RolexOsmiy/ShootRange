using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SelectionAppear : MonoBehaviour
{
    [SerializeField] private Color _okColor;
    [SerializeField] private Color _notOkColor;

    [HideInInspector] public bool okColor;
    
    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        TapticManager.Impact(TapticImpact.Light);
        _renderer.sharedMaterial.color = okColor ? _okColor : _notOkColor;
        _renderer.sharedMaterial.DOFade(0.8f, 0.2f).From(0);
    }

    private void OnDisable()
    {
        _renderer.sharedMaterial.DOKill();
    }
}
