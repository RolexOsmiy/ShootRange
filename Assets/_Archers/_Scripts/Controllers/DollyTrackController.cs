using Cinemachine;
using NaughtyAttributes;
using UnityEngine;

public class DollyTrackController : MonoBehaviour
{
    private const float kUpMiddlePoint = 3f; // how to up middle point of the dolly track
    private const float kFinishOffsetToStart = 5f; // насколько отодвинуть финиш к старту (надо дальше от врага)
    
    [SerializeField] private CinemachineSmoothPath _dollyTrack;
    [SerializeField] private Vector3   _offset;
    [SerializeField] private Transform _debugFirstPlacement;
    [SerializeField] private Transform _debugSecondPlacement;
    
    public void SetNewPlacements(Transform playersPlacement, Transform enemiesPlacement)
    {
        _debugFirstPlacement = playersPlacement;
        _debugSecondPlacement = enemiesPlacement;
        UpdatePositions();
    }

    private void UpdatePositions()
    {
        var middlePoint = Vector3.Lerp(_debugFirstPlacement.position, _debugSecondPlacement.position, 0.5f);
        _dollyTrack.transform.position = middlePoint + _offset;
        
        var trackStart = _dollyTrack.transform.InverseTransformPoint(_debugFirstPlacement.TransformPoint(_offset));
        var trackFinish = trackStart + (_debugSecondPlacement.position - _debugFirstPlacement.position);
        var directionFromFinishToStart = (trackStart - trackFinish).normalized;
        trackFinish += directionFromFinishToStart * kFinishOffsetToStart;

        var waypoints = _dollyTrack.m_Waypoints;
        waypoints[0].position = trackStart;
        waypoints[2].position = trackFinish;
        waypoints[1].position = Vector3.Lerp(trackStart, trackFinish, 0.5f) + Vector3.up * kUpMiddlePoint;
        _dollyTrack.InvalidateDistanceCache();
    }

    [Button()]
    private void UpdatePositionsDebug()
    {
        if (_debugFirstPlacement == null || _debugSecondPlacement == null)
        {
            MyDebug.LogRed("[DollyTrackController] No points!");
            return;
        }
        
        SetNewPlacements(_debugFirstPlacement, _debugSecondPlacement);
    }
}
