using System;
using UnityEngine;

public class HordeController : MonoBehaviour
{
    [Header("Horde parameters")]
    [SerializeField] private float _currentHordeLevel = 50f;
    [SerializeField] private float _maxHordeLevel = 100f;
    [SerializeField] private float _hordeCurrentProgression = 0f;

    [Header("Horde bar progression")]
    [SerializeField] private float _stopStateProgression = -8f;
    [SerializeField] private float _walkStateProgression = -4f;
    [SerializeField] private float _runStateProgression = -2f;
    [SerializeField] private float _flyStateProgression = 4f;

    private bool _locked = false;

    private void Awake()
    {
        RunnerEventSystem.OnSpeedStateChange += HandleSpeedStateChange;
        RunnerEventSystem.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        RunnerEventSystem.OnSpeedStateChange -= HandleSpeedStateChange;
        RunnerEventSystem.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (_locked)
        {
            return;
        }

        if (_currentHordeLevel >= _maxHordeLevel && _hordeCurrentProgression > 0)
        {
            _currentHordeLevel = _maxHordeLevel;
            return;
        }

        if (_currentHordeLevel <= 0)
        {
            RunnerEventSystem.OnGameOver?.Invoke();
            return;
        }

        _currentHordeLevel += Time.deltaTime * _hordeCurrentProgression;
        RunnerEventSystem.OnHordeLevelChange?.Invoke(_currentHordeLevel);
    }

    private void HandleSpeedStateChange(SpeedState speed)
    {
        switch (speed)
        {
            case SpeedState.Stop: _hordeCurrentProgression = _stopStateProgression; break;
            case SpeedState.Walk: _hordeCurrentProgression = _walkStateProgression; break;
            case SpeedState.Run: _hordeCurrentProgression = _runStateProgression; break;
            case SpeedState.Fly: _hordeCurrentProgression = _flyStateProgression; break;
            default: goto case SpeedState.Stop;
        }
    }

    private void HandleStateChanged(State newState)
    {
        if (newState is not GameState)
        {
            _locked = true;
            StopAllCoroutines();
            return;
        }

        _locked = false;
    }
}
