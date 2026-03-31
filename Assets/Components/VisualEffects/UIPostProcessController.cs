using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Dynamically adjusts post-processing effects based on the current horde threat level.
// As the horde level rises, the vignette shrinks and colour saturation increases,
// giving the player a visual cue that danger is receding. When the horde is close,
// the screen edges darken and colour drains away to build tension.
public class UIPostProcessController : MonoBehaviour
{
    [Header("Components")]
    // The URP Volume whose profile contains the Vignette and ColorAdjustments overrides
    [SerializeField] private Volume _volume;
    // Cached references to the URP effect overrides extracted from the volume profile at startup
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustements;

    [Header("Parameters")]
    // The vignette intensity when the horde level is at its lowest (maximum danger)
    [SerializeField] private float _vignetteMaxValue = 0.7f;
    // Baseline offset added to the saturation calculation; shift this to tune the colour curve
    [SerializeField] private float _colorMinValue = 0f;

    private void Awake()
    {
        // TryGet extracts the effect override from the shared volume profile;
        // storing the reference avoids a profile lookup on every horde update
        Vignette tmpVignette;
        ColorAdjustments tmpColor;

        if (_volume.profile.TryGet<Vignette>(out tmpVignette))
        {
            _vignette = tmpVignette;
        }

        if (_volume.profile.TryGet<ColorAdjustments>(out tmpColor))
        {
            _colorAdjustements = tmpColor;
        }

        RunnerEventSystem.OnHordeLevelChange += UpdatePostProcess;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnHordeLevelChange -= UpdatePostProcess;
    }

    // Recalculates both post-process effects whenever the horde level changes.
    // hordeLevel is expected in the range [0, 100] where 0 = maximum threat, 100 = horde fully repelled.
    private void UpdatePostProcess(float hordeLevel)
    {
        // Saturation rises with the horde level: the screen becomes more vivid as the player pulls ahead.
        // Clamped to 0 to prevent oversaturation artefacts.
        float tempColor = _colorAdjustements.saturation.min + hordeLevel + _colorMinValue;
        _colorAdjustements.saturation.value = tempColor > 0 ? 0 : tempColor;

        // Vignette intensity falls as the horde level rises: the darkened border shrinks when the player is safe.
        // Formula: starts at (intensity.max * _vignetteMaxValue) and decreases proportionally with hordeLevel.
        _vignette.intensity.value = (_vignette.intensity.max * _vignetteMaxValue) - ((hordeLevel * _vignetteMaxValue) / 100);
    }
}