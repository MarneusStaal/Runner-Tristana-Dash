using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Keeps the fuel UI (numeric label and progress slider) in sync with the player's current fuel level.
// Reacts to OnFuelLevelChange events rather than polling each frame.
public class UIFuelView : MonoBehaviour
{
    [Header("UI Elements")]
    // Numeric label showing the exact fuel amount (e.g. "42")
    [SerializeField] private TMP_Text _fuelText;
    // Progress bar representing fuel as a proportion of the maximum (0 = empty, 1 = full)
    [SerializeField] private Slider _fuelSlider;

    [Header("Fuel data")]
    // The maximum possible fuel value; used to normalise the slider (set in Inspector)
    [SerializeField] private float _maxFuelValue;

    private void Awake()
    {
        RunnerEventSystem.OnFuelLevelChange += HandleFuelLevelChange;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnFuelLevelChange -= HandleFuelLevelChange;
    }

    // Called whenever the player's fuel level changes; updates both the label and the slider
    private void HandleFuelLevelChange(int fuel)
    {
        // Display the raw fuel value as a whole number
        _fuelText.text = fuel.ToString();

        // Normalise to [0, 1] so the slider fills proportionally to the fuel remaining
        _fuelSlider.value = (float)(fuel / _maxFuelValue);
    }
}