using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UIPostProcessController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Volume _volume;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustements;

    [Header("Parameters")]
    [SerializeField] private float _vignetteMaxValue = 0.7f;
    [SerializeField] private float _colorMinValue = 0f;

    private void Awake()
    {
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
        RunnerEventSystem.OnHordeLevelChange -= UpdatePostProcess;
    }

    private void UpdatePostProcess(float hordeLevel)
    {
        _colorAdjustements.saturation.value = _colorAdjustements.saturation.min + hordeLevel + _colorMinValue;
        _vignette.intensity.value = (_vignette.intensity.max * _vignetteMaxValue) - ((hordeLevel * _vignetteMaxValue) / 100); 
    }
}
