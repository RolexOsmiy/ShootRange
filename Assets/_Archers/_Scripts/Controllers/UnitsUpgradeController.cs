using UnityEngine;

public class UnitsUpgradeController : MonoBehaviour
{
    private PositionsPlacementController _currentPlacement;
    private PlayerArcherController       _selectedUnit;
    private int                          _currentPositionInPlacement;
    private Vector3                      _screenPoint;
    private Vector3                      _offset;
    private Camera                       _mainCamera;

    #region Init

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        PlayerArcherController.OnUnitSelected += OnUnitSelected;
        PlayerArcherController.OnUnitDrag += OnUnitDrag;
        PlayerArcherController.OnUnitDeselected += OnUnitDeselected;
    }

    private void OnDisable()
    {
        PlayerArcherController.OnUnitSelected -= OnUnitSelected;
        PlayerArcherController.OnUnitDrag -= OnUnitDrag;
        PlayerArcherController.OnUnitDeselected -= OnUnitDeselected;
    }

    #endregion
    
    #region Public methods

    public void SetCurrentPlacement(PositionsPlacementController placementController)
    {
        _currentPlacement = placementController;
    }

    #endregion

    #region Delegate

    private void OnUnitSelected(PlayerArcherController transferedUnit)
    {
        _currentPositionInPlacement = transferedUnit.positionInPlacement;
        _currentPlacement.SelectPosition(_currentPositionInPlacement, true);
        
        _screenPoint = _mainCamera.WorldToScreenPoint(transferedUnit.transform.position);
        _offset = transferedUnit.transform.position - _mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _screenPoint.z));
    }

    private void OnUnitDrag(PlayerArcherController transferedUnit)
    {
        var curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, _screenPoint.z);
        var newPos = _mainCamera.ScreenToWorldPoint(curScreenPoint) + _offset;

        var selectedPos = _currentPlacement.GetPositionIndexByCoords(newPos);

        if (selectedPos != _currentPositionInPlacement && selectedPos >= 0)
        {
            bool okToSet = true;
            var (occupied, unitToUpgrade) = MainGameManager.Instance.IsPositionInPlacementOccupied(selectedPos);
            if (occupied && 
                (!CanBeUpgraded(transferedUnit, unitToUpgrade) && transferedUnit.positionInPlacement != unitToUpgrade.positionInPlacement)) // if the cell is occupaied, but unit there cannot be upgraded
                okToSet = false;
            
            _currentPlacement.SelectPosition(selectedPos, okToSet);
            _currentPositionInPlacement = selectedPos;
        }

        if (selectedPos >= 0)
        {
            var pos = transferedUnit.transform.position;
            pos.x = newPos.x;
            pos.z = newPos.z;
            transferedUnit.transform.position = pos;
        }
    }
    
    private void OnUnitDeselected(PlayerArcherController transferedUnit)
    {
        // Check if place occupied
        var (occupaed, unitToUpgrade) = MainGameManager.Instance.IsPositionInPlacementOccupied(_currentPositionInPlacement);

        if (occupaed)
        {
            if (CanBeUpgraded(transferedUnit, unitToUpgrade))
            {
                StatePersister.Instance.SaveUnitPosition(transferedUnit.positionInPlacement, -1);
                // UPGRADE
                MainGameManager.Instance.UpgradeUnit(unitToUpgrade, transferedUnit);
                
                StatePersister.Instance.SaveUnitPosition(unitToUpgrade.positionInPlacement, unitToUpgrade.UnitLevel);
            }
            else
            {
                // Move back
                var newPos = _currentPlacement.GetPositionFor(transferedUnit.positionInPlacement);
                transferedUnit.transform.position = newPos;
            }
        }
        else // the cell is free, just put
        {
            var newPos = _currentPlacement.GetPositionFor(_currentPositionInPlacement);
            transferedUnit.transform.position = newPos;
            
            StatePersister.Instance.SaveUnitPosition(transferedUnit.positionInPlacement,-1);
            
            transferedUnit.positionInPlacement = _currentPositionInPlacement;
            
            StatePersister.Instance.SaveUnitPosition(_currentPositionInPlacement, transferedUnit.UnitLevel);
        }

        _currentPlacement.SelectPosition(-1, false); // clear selection
        
        // TODO: Continue here with upgrades (и еще надо добавть покупку юнита)
    }

    private static bool CanBeUpgraded(PlayerArcherController transferedUnit, PlayerArcherController unitToUpgrade)
    {
        return transferedUnit.UnitLevel == unitToUpgrade.UnitLevel && 
               transferedUnit.UnitLevel < UnitsData.Instance.maxUnitLevel && 
               transferedUnit.positionInPlacement != unitToUpgrade.positionInPlacement;
    }

    #endregion
}
