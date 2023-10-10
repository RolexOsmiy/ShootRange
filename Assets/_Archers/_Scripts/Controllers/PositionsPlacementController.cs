using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

public class PositionsPlacementController : MonoBehaviour
{
    public enum PlacementSide
    {
        Enemy,
        Player
    }

    private bool IsEnemies => _side == PlacementSide.Enemy;
    
    [ReadOnly][SerializeField] private TextMeshPro _placementIndex;
    [ReadOnly][SerializeField] private PlacementSide _side;
    [SerializeField] private GameObject _caption;
    [SerializeField] private Transform[] _positions;
    [SerializeField] private GameObject[] _positionSelections;
    [SerializeField] private GameObject _positionsPlane; // Отключать в рантайме (только для создания уровня)
    [SerializeField] private GameObject _cube; // Отключать в рантайме (только для создания уровня)
    [SerializeField] private Transform  _arrowsAppearPoint;
    [SerializeField] private float      _placementSizeWidth = 4f;
    [SerializeField] private float      _placementSizeHeight = 4f;
    [SerializeField] private int        _placementRows = 3;
    [SerializeField] private int        _placementColms = 3;
    [Space] 
    [ShowIf("IsEnemies")]
    [SerializeField] private int[]      _enemies;

    #region Init

    private void Awake()
    {
        HidePositions();
    }

    private void Start()
    {
        //_selfPos = transform.position;
    }

    private void OnValidate()
    {
        if(_placementIndex == null)
            return;

        _placementIndex.text = _side == PlacementSide.Player ? transform.GetSiblingIndex().ToString() : (transform.GetSiblingIndex() + 1).ToString();
    }

    #endregion

    public int GetPositionsNumber()
    {
        return _positions.Length;
    }

    public Vector3 GetPositionFor(int index)
    {
        if (index >= 0 && index < _positions.Length)
            return _positions[index].position;
        
        Debug.Log("[PositionPlacementController] Wrong position");
        return transform.position;
    }

    public Transform GetArrowsAppearPoint()
    {
        return _arrowsAppearPoint;
    }

    public void ShowPositions()
    {
        _positionsPlane.SetActive(true);
        //_placementIndex.gameObject.SetActive(true);
        if(_cube != null)
            _cube.SetActive(true);
    }

    public void HidePositions()
    {
        _positionsPlane.SetActive(false);
        _caption.SetActive(false);
        _placementIndex.gameObject.SetActive(false);
        if(_cube != null)
            _cube.SetActive(false);
    }

    public int[] GetStartEnemies()
    {
        return _enemies;
    }
    
    public void SelectPosition(int index, bool canSet) // canSet = если можно поставить или апгрейдить
    {
        for (int i = 0; i < _positionSelections.Length; i++)
        {
            _positionSelections[i].SetActive(false);
        }

        if (index >= 0 && index < _positionSelections.Length)
        {
            _positionSelections[index].GetComponent<SelectionAppear>().okColor = canSet;
            _positionSelections[index].SetActive(true);
        }
    }
    
    public int GetPositionIndexByCoords(Vector3 pos)
    {
        (int, int) IndToPos(int ind)
        {
            var r = ind / _placementColms; // ind = 7; r = 2
            var c = ind - r * _placementColms; // r = 2; 7-2*3
            return (c, r);
        }
        
        pos = transform.InverseTransformPoint(pos); // transform to local coordinates
        pos += new Vector3(_placementSizeWidth / 2, 0, _placementSizeHeight / 2f);
        
        if (pos.x < 0 || pos.x > _placementSizeWidth || pos.z < 0 || pos.z > _placementSizeWidth)
            return -1; // Out

        var column = Mathf.FloorToInt(pos.x / (_placementSizeWidth / _placementColms));
        var row = (_placementRows - 1) - Mathf.FloorToInt(pos.z / (_placementSizeHeight / _placementRows));

        var index = -1;
        for (int i = 0; i < _positions.Length; i++)
        {
            var (c, r) = IndToPos(i);
            if (c == column && r == row)
            {
                index = i;
                break;
            }
        }

        return index;
    }
}
