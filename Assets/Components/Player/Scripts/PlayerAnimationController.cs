using System.Collections;
using UnityEngine;

// Drives the player's Animator and visual effects in response to game events.
// Handles animator bool states for locomotion (run, walk, jump, fly) and
// plays blink + bounce feedback animations when the player takes damage.
public class PlayerAnimationController : MonoBehaviour
{
    // Duration of the damage feedback animation (blink + optional bounce)
    [SerializeField] private float _outOfFuelAnimationDuration = 1f;

    // Height of the bounce arc played during the out-of-fuel damage animation
    [SerializeField] private float _outOfFuelJumpHeight = 1f;
    // Curve shaping the bounce arc; evaluated 0→1 over _damageAnimationDuration
    [SerializeField] private AnimationCurve _damageJumpCurve;
    // Time step (seconds) between each blink toggle and coroutine yield during damage animations
    [SerializeField] private float _blinkAnimationSpeed = 0.05f;
    // Reference to the Animator component that drives the character rig
    [SerializeField] private Animator _playerAnimator;

    // All skinned mesh renderers on the player model; toggled rapidly to produce a blink effect
    [SerializeField] private SkinnedMeshRenderer[] _meshs;

    private void Awake()
    {
        // Subscribe to all gameplay events that require an animation response
        RunnerEventSystem.OnPlayerJump += Jump; // The player is jumping
        RunnerEventSystem.OnPlayerOutOfFuel += HandleOutOfFuel; // The player has run out of fuel while flying
        RunnerEventSystem.OnFlyingDamage += HandleFlyingDamage; // The player hit an obstacle while flying
        RunnerEventSystem.OnFlyDown += FlyDown; // The player is flying down to the ground
        RunnerEventSystem.OnFlyUp += FlyUp; // The player is flying up to the sky
        RunnerEventSystem.OnStartRunning += Run; // The player start running
        RunnerEventSystem.OnStartWalking += Walk; // The player start walking
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnPlayerJump -= Jump;
        RunnerEventSystem.OnPlayerOutOfFuel -= HandleOutOfFuel;
        RunnerEventSystem.OnFlyingDamage -= HandleFlyingDamage;
        RunnerEventSystem.OnFlyDown -= FlyDown;
        RunnerEventSystem.OnFlyUp -= FlyUp;
        RunnerEventSystem.OnStartRunning -= Run;
        RunnerEventSystem.OnStartWalking -= Walk;
    }

    // Entry point for the out-of-fuel damage sequence (triggered by the flying coroutine)
    private void HandleOutOfFuel()
    {
        StartCoroutine(TakeDamageOutOfFuelCoroutine());
    }

    // Plays a bounce arc + blink effect when the player runs out of fuel mid-flight.
    // Unlike the flying damage variant, this one also physically moves the player on the Y axis.
    private IEnumerator TakeDamageOutOfFuelCoroutine()
    {
        float timer = 0f;
        bool isBlinking = false;

        // Store the player's Y position at the moment damage is taken as the arc's baseline
        float currentHeight = transform.position.y;

        // Switch the animator from flying to falling immediately
        _playerAnimator.SetBool("IsFlying", false);
        _playerAnimator.SetBool("IsFalling", true);

        // Flip the character to face the opposite direction (visual damage feedback)
        DamageFlip();

        while (timer <= _outOfFuelAnimationDuration)
        {
            timer += _blinkAnimationSpeed;

            // Toggle mesh visibility each step to produce a rapid blink effect
            if (!isBlinking)
            {
                HandleDamageMesh(false); // hide
                isBlinking = true;
            }
            else
            {
                HandleDamageMesh(true); // show
                isBlinking = false;
            }

            // Sample the damage curve to compute the Y offset for this frame
            float normalizedTime = timer / _outOfFuelAnimationDuration;
            float targetHeight = (_damageJumpCurve.Evaluate(normalizedTime) * _outOfFuelJumpHeight) + currentHeight;
            Vector3 targetPosition = new Vector3(transform.position.x, targetHeight, transform.position.z);

            transform.position = targetPosition;

            yield return new WaitForSeconds(_blinkAnimationSpeed);
        }

        // Flip back to the original facing direction once the animation is done
        DamageFlip();

        // Ensure the mesh is visible when the animation ends
        HandleDamageMesh(true);
    }

    // Entry point for the mid-flight damage sequence (e.g. hit by an obstacle while flying)
    private void HandleFlyingDamage()
    {
        StartCoroutine(TakeFlyingDamageCoroutine());
    }

    // Plays a blink effect when the player takes damage while flying.
    // Unlike the out-of-fuel variant, this does NOT move the player — only the animator and mesh blink.
    private IEnumerator TakeFlyingDamageCoroutine()
    {
        float timer = 0f;
        bool isBlinking = false;

        // Switch the animator from flying to falling immediately
        _playerAnimator.SetBool("IsFlying", false);
        _playerAnimator.SetBool("IsFalling", true);

        while (timer <= _outOfFuelAnimationDuration)
        {
            timer += _blinkAnimationSpeed;

            // Toggle mesh visibility each step to produce a rapid blink effect
            if (!isBlinking)
            {
                HandleDamageMesh(false); // hide
                isBlinking = true;
            }
            else
            {
                HandleDamageMesh(true); // show
                isBlinking = false;
            }

            yield return new WaitForSeconds(_blinkAnimationSpeed);
        }

        // Ensure the mesh is visible when the animation ends
        HandleDamageMesh(true);
    }

    // Enables or disables all skinned mesh renderers simultaneously to create a blink effect
    private void HandleDamageMesh(bool enabled)
    {
        for (int i = 0; i < _meshs.Length; i++)
        {
            _meshs[i].enabled = enabled;
        }
    }

    // Flips the player 180° on the Y axis; used as a visual "recoil" at the start and end of
    // the out-of-fuel damage animation
    private void DamageFlip()
    {
        if (transform.rotation.eulerAngles.y >= 180f) transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        else transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
    }

    // Starts the jump animation coroutine; jumpDuration is passed in from the movement system
    // so the animation timing stays perfectly in sync with the physics arc
    private void Jump(float jumpDuration)
    {
        StartCoroutine(JumpCoroutine(jumpDuration));
    }

    // Splits the jump duration in half: first half plays the rising animation, second half the falling one
    private IEnumerator JumpCoroutine(float jumpDuration)
    {
        //_playerAnimator.SetTrigger("Jumping"); // legacy trigger approach, replaced by bools

        _playerAnimator.SetBool("IsJumping", true);
        _playerAnimator.SetBool("IsWalking", false);
        _playerAnimator.SetBool("IsRunning", false);

        // Wait until the apex of the jump before switching to the falling animation
        yield return new WaitForSeconds(jumpDuration / 2);

        //_playerAnimator.SetTrigger("Falling"); // legacy trigger approach, replaced by bools
        _playerAnimator.SetBool("IsJumping", false);
        _playerAnimator.SetBool("IsFalling", true);

        // Wait for the descent before restoring the running animation on landing
        yield return new WaitForSeconds(jumpDuration / 2);

        _playerAnimator.SetBool("IsRunning", true);
        _playerAnimator.SetBool("IsFalling", false);
    }

    // Switches the animator to the flying state and clears locomotion bools
    private void FlyUp()
    {
        _playerAnimator.SetBool("IsFlying", true);
        _playerAnimator.SetBool("IsRunning", false);
        _playerAnimator.SetBool("IsWalking", false);
    }

    // Switches the animator from flying to falling (descent will transition to run via HandleDescent)
    private void FlyDown()
    {
        _playerAnimator.SetBool("IsFlying", false);
        _playerAnimator.SetBool("IsFalling", true);
    }

    // Switches the animator to the running state and clears conflicting bools
    private void Run()
    {
        _playerAnimator.SetBool("IsRunning", true);
        _playerAnimator.SetBool("IsWalking", false);
        _playerAnimator.SetBool("IsFalling", false);
    }

    // Switches the animator to the walking state
    private void Walk()
    {
        _playerAnimator.SetBool("IsRunning", false);
        _playerAnimator.SetBool("IsWalking", true);
    }
}