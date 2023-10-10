using QFSW.MOP2;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimulateBlood : MonoBehaviour, IAnimateHit
{
    [SerializeField] private Transform _bloodContainer;
    
    private Collider _selfCollider;

    private void Awake()
    {
        _selfCollider = GetComponent<Collider>();
    }

    public void AnimateHit(Transform otherTransform)
    {
        var otherPos = otherTransform.position;
        var pos = _selfCollider.ClosestPoint(otherPos);
        var blood = MasterObjectPooler.Instance.GetObject("Blood", pos);
        blood.transform.SetParent(_bloodContainer);
        blood.transform.localScale = Vector3.one;
        blood.transform.LookAt(otherPos);
    }
}
