using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFuelView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _fuelText;
    [SerializeField] private Slider _fuelSlider;

    [Header("Fuel data")]
    [SerializeField] private float _maxFuelValue;

    private void Awake()
    {
        RunnerEventSystem.OnFuelLevelChange += HandleFuelLevelChange;
    }

    private void OnDestroy()
    {
        RunnerEventSystem.OnFuelLevelChange -= HandleFuelLevelChange;
    }

    private void HandleFuelLevelChange(int fuel)
    {
        _fuelText.text = fuel.ToString();
        _fuelSlider.value = (float)(fuel / _maxFuelValue);
    }
}
