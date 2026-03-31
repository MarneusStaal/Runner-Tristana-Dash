using UnityEngine;
using UnityEngine.UI;

// Keeps the horde progress slider in sync with the current horde threat level.
// Reacts to OnHordeLevelChange events rather than polling each frame.
public class UIHordeView : MonoBehaviour
{
    [Header("UI Elements")]
    // Progress bar representing the horde level as a proportion of the maximum (0 = none, 1 = full)
    [SerializeField] private Slider _hordeSlider;

    [Header("Horde data")]
    // The maximum possible horde value; used to normalise the slider (set in Inspector)
    [SerializeField] private float _maxHordeValue;

    private void Awake()
    {
        RunnerEventSystem.OnHordeLevelChange += HandleHordeLevelChange;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnHordeLevelChange -= HandleHordeLevelChange;
    }

    // Called whenever the horde level changes; normalises the value and updates the slider
    private void HandleHordeLevelChange(float horde)
    {
        // Normalise to [0, 1] so the slider fills proportionally to the current horde threat
        _hordeSlider.value = horde / _maxHordeValue;
    }
}