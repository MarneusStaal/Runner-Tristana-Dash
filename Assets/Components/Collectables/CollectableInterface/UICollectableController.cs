using System;
using TMPro;
using UnityEngine;

public class UICollectableController : MonoBehaviour
{
    [SerializeField] private TMP_Text _redBottleCountText;
    [SerializeField] private TMP_Text _greenBottleCountText;
    [SerializeField] private TMP_Text _blueBottleCountText;
    [SerializeField] private TMP_Text _candleCountText;

    private void Awake()
    {
        RunnerEventSystem.OnCollectableValueChanged += HandleCollectableValueChanged;
    }

    private void OnDestroy()
    {
        RunnerEventSystem.OnCollectableValueChanged -= HandleCollectableValueChanged;
    }

    private void HandleCollectableValueChanged()
    {
        _redBottleCountText.text = $"X   {PlayerInventoryController.Instance.RedBottleCount}";
        _greenBottleCountText.text = $"X   {PlayerInventoryController.Instance.GreenBottleCount}";
        _blueBottleCountText.text = $"X   {PlayerInventoryController.Instance.BlueBottleCount}";
        _candleCountText.text = $"X   {PlayerInventoryController.Instance.CandleCount}";
    }
}
