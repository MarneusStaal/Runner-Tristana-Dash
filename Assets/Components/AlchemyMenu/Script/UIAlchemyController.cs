using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIAlchemyController : MonoBehaviour
{
    [Header("Craft Buttons")]
    [SerializeField] private Button _redGreenButton;
    [SerializeField] private Button _redBlueButton;
    [SerializeField] private Button _redCandleButton;

    [Header("Resource Texts")]
    [SerializeField] private TMP_Text _redBottleCountText;
    [SerializeField] private TMP_Text _greenBottleCountText;
    [SerializeField] private TMP_Text _blueBottleCountText;
    [SerializeField] private TMP_Text _candleCountText;

    private SaveData _save;
    private void Start()
    {
        _save = SaveService.Load();

        _redGreenButton.interactable = false;
        _redBlueButton.interactable = false;
        _redCandleButton.interactable = false;

        CheckCraftAvailability();
        UpdateResourceWindow();
    }

    private void CheckCraftAvailability()
    {
        if (_save.RedBottleCount > 0 && _save.GreenBottleCount > 0 && !_save.RedGreenPotionActive) _redGreenButton.interactable = true;
        else _redGreenButton.interactable = false;

        if (_save.RedBottleCount > 0 && _save.BlueBottleCount > 0 && !_save.RedBluePotionActive) _redBlueButton.interactable = true;
        else _redBlueButton.interactable = false;

        if (_save.RedBottleCount > 0 && _save.CandleCount > 0 && !_save.RedCandlePotionActive) _redCandleButton.interactable = true;
        else _redCandleButton.interactable = false;
    }

    private void UpdateResourceWindow()
    {
        _redBottleCountText.text = $"X   {_save.RedBottleCount}";
        _greenBottleCountText.text = $"X   {_save.GreenBottleCount}";
        _blueBottleCountText.text = $"X   {_save.BlueBottleCount}";
        _candleCountText.text = $"X   {_save.CandleCount}";
    }

    public void CraftRedGreen()
    {
        _save.RedBottleCount--;
        _save.GreenBottleCount--;
        _save.RedGreenPotionActive = true;

        CheckCraftAvailability();
        UpdateResourceWindow();
    }

    public void CraftRedBlue()
    {
        _save.RedBottleCount--;
        _save.BlueBottleCount--;
        _save.RedBluePotionActive = true;

        CheckCraftAvailability();
        UpdateResourceWindow();
    }

    public void CraftRedCandle()
    {
        _save.RedBottleCount--;
        _save.CandleCount--;
        _save.RedCandlePotionActive = true;

        CheckCraftAvailability();
        UpdateResourceWindow();
    }

    public void GoToMainMenu()
    {
        SaveService.Save(_save);

        SceneLoaderService.UnloadAlchemyMenu();
    }
}
