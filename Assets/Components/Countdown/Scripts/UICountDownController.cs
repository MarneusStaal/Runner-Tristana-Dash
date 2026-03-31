using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Manages the countdown UI window displayed before a run starts.
// Listens for state changes and mirrors the CountdownState's timer value each frame.
public class UICountDownController : MonoBehaviour
{
    // The root GameObject of the countdown window; shown/hidden based on the current state
    [SerializeField] private GameObject _window;
    // The text element that displays the current countdown value (e.g. "3", "2", "1")
    [SerializeField] private TMP_Text _countdownText;

    // True while the game is in a CountdownState; gates the Update logic
    private bool _inCountdown;
    // Reference to the active Countdown State, used to read the live timer value each frame
    private CountdownState _countdownState;

    private void Awake()
    {
        // Hide the window by default; it will be shown only when a Countdown State is entered
        _window.SetActive(false);
        RunnerEventSystem.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnStateChanged -= HandleStateChanged;
    }

    // Called whenever the game transitions to a new state
    private void HandleStateChanged(State state)
    {
        // If the new state is not a CountdownState, hide the window and stop updating
        if (state is not CountdownState countdownState)
        {
            _inCountdown = false;
            _window.SetActive(false);
            return;
        }

        // A countdown has started: show the window and store the state to poll its timer
        _window.SetActive(true);
        _countdownState = countdownState;
        _inCountdown = true;
    }

    private void Update()
    {
        // Skip rendering if no countdown is active
        if (!_inCountdown)
        {
            return;
        }

        // Refresh the displayed value every frame, formatted as a whole number (e.g. "3", "2", "1")
        _countdownText.text = _countdownState.Timer.ToString("0");
    }
}