using Fusion;
using MUXR;
using MUXR.Networking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class ElectricBeamController : NetworkBehaviour
{
    public ParticleSystem particleEffect; // Particle system for the electric beam effect
    public InputActionReference inputActionReference; // input action for triggering the electric beam
    public AudioSource audioSource; // AudioSource for playing sound effects
    public Color defaultParticleColor; // Default color of the particle effect
    public Color interactionPointColor; // Color of the particle effect when pointing at an interaction point

    // Cooldown time in seconds, adjustable from the Unity inspector, used to prevent spamming
    public float cooldownTime = 1.0f;

    private bool isPlayer; // Flag to check if the controller is associated with a player
    private float lastReleaseTime; // Time of the last button release
    private bool isCooldown; // Flag to indicate if the electric beam is in cooldown
    private bool wasActive; // Flag to track if the electric beam was active
    private bool activationRequested; // Flag to indicate if activation was requested during cooldown
    private ParticleSystem.Particle[] particles; // Array to store particle information

    private void Start()
    {
        // Check if the module is Shakespeare In the Metavrse if not do not apply to controller
        if (ModuleManager.Settings.module != KernSettings.Module.SIM)
        {
            this.transform.parent.gameObject.SetActive(false);
            return;
        }

        // Attach methods to input action events
        inputActionReference.action.performed += OnButtonPress;
        inputActionReference.action.canceled += OnButtonRelease;

        // player is the Kern avatarcontroller
        isPlayer = GetComponentInParent<AvatarController>().Object.HasInputAuthority;

        // Initialize variables
        lastReleaseTime = 0.0f;
        isCooldown = false;
        wasActive = false;
        activationRequested = false;
        particles = new ParticleSystem.Particle[particleEffect.main.maxParticles];
    }

    private void OnDisable()
    {
        // Detach methods from input action events
        inputActionReference.action.performed -= OnButtonPress;
        inputActionReference.action.canceled -= OnButtonRelease;
    }

    private void OnButtonPress(InputAction.CallbackContext context)
    {
        // If the cooldown is active, request activation after cooldown
        if (isPlayer && isCooldown)
        {
            activationRequested = true;
        }
        // If no cooldown, activate right away
        else if (isPlayer && !isCooldown)
        {
            RPC_ActivateParticleEffect(true);
            wasActive = true;  // mark that the effect was active
        }
    }

    private void OnButtonRelease(InputAction.CallbackContext context)
    {
        if (isPlayer)
        {
            RPC_ActivateParticleEffect(false);

            // Start cooldown only if the effect was active before button release
            if (wasActive)
            {
                lastReleaseTime = Time.time;
                isCooldown = true;
            }

            wasActive = false;  // reset the flag
        }
    }

    private void Update()
    {
        // Check if cooldown is active and enough time has passed since the last release
        if (isCooldown && Time.time - lastReleaseTime >= cooldownTime)
        {
            isCooldown = false;

            // If activation was requested during cooldown, activate now
            if (activationRequested)
            {
                RPC_ActivateParticleEffect(true);
                wasActive = true;
                activationRequested = false;
            }
        }

        var main = particleEffect.main;  

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit))
        {
            if (hit.collider.CompareTag("Interaction Point"))
            {
                // Change color when pointing at Interaction Point
                main.startColor = interactionPointColor;
            }
            else
            {
                // Reset color to default when not pointing at Interaction Point
                main.startColor = defaultParticleColor;
            }

            int numParticlesAlive = particleEffect.GetParticles(particles);

            // Change the color of all live particles, used because particles were sometimes two colors when hover stopped
            for (int i = 0; i < numParticlesAlive; i++)
            {
                particles[i].startColor = main.startColor.color;
            }

            // Apply the particle changes to the particle system
            particleEffect.SetParticles(particles, numParticlesAlive);
        }
    }

    //others need to see your lightning color to know if all are activating Interaction points
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ActivateParticleEffect(bool isActive)
    {
        if (particleEffect != null)
        {
            if (isActive)
            {
                // Start particle system and play audio
                particleEffect.Play();
                audioSource.Play();
            }
            else
            {
                // Stop particle system and audio
                particleEffect.Stop();
                audioSource.Stop();
            }
        }
    }
}
