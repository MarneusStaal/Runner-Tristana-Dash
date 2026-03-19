using UnityEngine;
using UnityEngine.UI;

public class UIHordeView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider _hordeSlider;

    [Header("Horde data")]
    [SerializeField] private float _maxHordeValue;

    private void Awake()
    {
        RunnerEventSystem.OnHordeLevelChange += HandleHordeLevelChange;
    }

    private void OnDestroy()
    {
        RunnerEventSystem.OnHordeLevelChange -= HandleHordeLevelChange;
    }

    private void HandleHordeLevelChange(float horde)
    {
        _hordeSlider.value = horde / _maxHordeValue;
    }
}
