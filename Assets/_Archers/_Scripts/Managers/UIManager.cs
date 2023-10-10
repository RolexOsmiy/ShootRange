using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Image           _hider;
    [SerializeField] private TextMeshProUGUI _levelNumber;
    [SerializeField] private TextMeshProUGUI _goldBar;
    [SerializeField] private GameObject      _buttonsPanel;
    [SerializeField] private Button          _buyButton;
    [SerializeField] private TextMeshProUGUI _buyButtonText;
    [SerializeField] private GameObject      _powerTextBlock;
    [SerializeField] private TextMeshProUGUI _angleText;
    [SerializeField] private TextMeshProUGUI _powerText;

    private void Start()
    {
        _hider.gameObject.SetActive(true);
        HideNewRoundButtonsBlock();
        HidePowerTextBlock();
        //UnHideScreen();

        AddCoins(0);
    }

    #region Public methods

    public void HideScreen(Action callback = null)
    {
        _hider.gameObject.SetActive(true);
        _hider.DOFade(1f, 0.5f).OnComplete(() => callback?.Invoke());
    }

    public void UnHideScreen(Action callback = null)
    {
        _hider.DOFade(0f, 0.5f)
            .OnComplete(() =>
            {
                _hider.gameObject.SetActive(false);
                callback?.Invoke();
            });
    }

    public void SetLevelNumber(int levelNumber)
    {
        _levelNumber.text = "Level " + levelNumber;
    }

    public void AddCoins(int count)
    {
        StatePersister.Instance.PlayerCoins += count;
        _goldBar.text = "Gold: " + StatePersister.Instance.PlayerCoins;
    }

    public void ShowNewRoundButtonsBlock()
    {
        _buttonsPanel.SetActive(true);
        UpdateBuyButtonState();
    }
    
    public void HideNewRoundButtonsBlock()
    {
        _buttonsPanel.SetActive(false);
    }

    public void UpdateBuyButtonState()
    {
        _buyButtonText.text = StatePersister.Instance.UnitCurrentCost.ToString();
        
        _buyButton.interactable = MainGameManager.Instance.CanBuyUnits();
    }

    public void ShowPowerTextBlock()
    {
        _powerTextBlock.SetActive(true);
    }
    
    public void HidePowerTextBlock()
    {
        _powerTextBlock.SetActive(false);
    }

    public void SetAngleAndPower(int angle, float power)
    {
        _angleText.text = angle + "\u00B0";
        _powerText.text = Mathf.RoundToInt(power * 100) + "%";
    }
    
    #endregion

    #region Button callbacks

    public void StartButtonPressed()
    {
        TapticManager.Impact();
        HideNewRoundButtonsBlock();
        MainGameManager.Instance.PlayButtonPressed();
    }

    public void BuyUnitButtonPressed()
    {
        
        TapticManager.Impact();
        MainGameManager.Instance.BuyUnit();
        UpdateBuyButtonState();
        
        MainGameManager.Instance.gameData.BuyUnit();
    }
    
    #endregion
}
