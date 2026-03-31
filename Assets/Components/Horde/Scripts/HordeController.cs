using System;
using UnityEngine;

// Tracks the horde threat level and adjusts it each frame based on the player's current speed.
// Slower movement lets the horde catch up (negative progression, the horde meters goes down); flying gets you away (positive, meter goes up).
// Reaching zero triggers a game over; reaching the maximum simply caps the bar.
public class HordeController : MonoBehaviour
{
    [Header("Horde parameters")]
    // The live horde level, updated every frame; starts at a mid-point to give the player some leeway
    [SerializeField] private float _currentHordeLevel = 50f;
    // The upper bound of the horde level; the bar cannot exceed this value
    [SerializeField] private float _maxHordeLevel = 100f;
    // The rate (units/second) at which _currentHordeLevel changes; set by the active speed state
    [SerializeField] private float _hordeCurrentProgression = 0f;

    [Header("Horde bar progression")]
    // Negative values push you towards the horde (player is getting catched); positive values let you gets away
    // Applied when the runner is stopped — horde meter recedes fastest
    [SerializeField] private float _stopStateProgression = -8f;
    // Applied when the runner is walking — horde still recedes, but slowly
    [SerializeField] private float _walkStateProgression = -4f;
    // Applied when the runner is running — horde meter freezes
    [SerializeField] private float _runStateProgression = 0f;
    // Applied when the runner is flying — horde meter advances
    [SerializeField] private float _flyStateProgression = 4f;
    
    // Applied every Timer seconds to stop, walk and run states
    [SerializeField] private float _hordeMeterAdditive = -1f;
    // Total malus for the horde progression
    [SerializeField] private float _hordeMeterTotalAdditive = 0f;

    // Add a malus to progression every timer seconds
    [SerializeField] private float _hordeMeterAdditiveTimer = 30f;
    // Used to count time up to _hordeMeterAdditiveTimer
    [SerializeField] private float _timer = 0f;

    // When true, horde progression is paused (e.g. during menus or cutscenes)
    private bool _locked = false;

    private void Awake()
    {
        // Update progression rate whenever the player's speed state changes
        RunnerEventSystem.OnSpeedStateChange += HandleSpeedStateChange;
        // Lock/unlock the controller whenever the game state changes
        RunnerEventSystem.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnSpeedStateChange -= HandleSpeedStateChange;
        RunnerEventSystem.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        // Skip horde updates while the controller is locked (non-gameplay state)
        if (_locked)
        {
            return;
        }

        // If the horde meter is already at maximum and still advancing, clamp and skip the update
        // (allows it to recede again if progression turns negative later)
        if (_currentHordeLevel >= _maxHordeLevel && _hordeCurrentProgression > 0)
        {
            _currentHordeLevel = _maxHordeLevel;
            return;
        }

        // The horde has fully caught up — trigger game over
        if (_currentHordeLevel <= 0)
        {
            RunnerEventSystem.OnGameOver?.Invoke();
            return;
        }

        // Every _hordeMeterAdditiveTimer second, the penalty for errors augments
        _timer += Time.deltaTime;
        if (_timer > _hordeMeterAdditiveTimer)
        {
            _hordeMeterTotalAdditive += _hordeMeterAdditive;
            _timer = 0f;
        }

        if (_hordeCurrentProgression == _flyStateProgression)
        {
            // Advance or recede the horde level based on the current progression rate
            _currentHordeLevel += Time.deltaTime * _hordeCurrentProgression;
        }
        else
        {
            // Advance or recede the horde level based on the current progression rate + malus
            _currentHordeLevel += Time.deltaTime * (_hordeCurrentProgression + _hordeMeterTotalAdditive);
        }

        // Notify the UI and any other listeners of the updated value
        RunnerEventSystem.OnHordeLevelChange?.Invoke(_currentHordeLevel);
    }

    // Updates the horde progression rate to match the player's new speed state
    private void HandleSpeedStateChange(SpeedState speed)
    {
        switch (speed)
        {
            case SpeedState.Stop: _hordeCurrentProgression = _stopStateProgression; break;
            case SpeedState.Walk: _hordeCurrentProgression = _walkStateProgression; break;
            case SpeedState.Run: _hordeCurrentProgression = _runStateProgression; break;
            case SpeedState.Fly: _hordeCurrentProgression = _flyStateProgression; break;
            // Any unrecognised state is treated as a full stop
            default: goto case SpeedState.Stop;
        }
    }

    // Pauses horde progression during non-gameplay states (menus, death screen, etc.)
    private void HandleStateChanged(State newState)
    {
        if (newState is not GameState)
        {
            _locked = true;
            return;
        }

        _locked = false;
    }
}