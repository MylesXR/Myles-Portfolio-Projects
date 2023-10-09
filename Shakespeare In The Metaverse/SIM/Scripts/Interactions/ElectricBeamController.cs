using Fusion;
using MUXR;
using MUXR.Networking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class ElectricBeamController : NetworkBehaviour
{
    public ParticleSystem particleEffect;
    public InputActionReference inputActionReference;
    public AudioSource audioSource;
    public Color defaultParticleColor;
    public Color interactionPointColor;

    // Cooldown time in seconds, adjustable from the Unity inspector
    public float cooldownTime = 1.0f;

    private bool isPlayer;
    private float lastReleaseTime;
    private bool isCooldown;
    private bool wasActive;
    private bool activationRequested;
    private ParticleSystem.Particle[] particles;

    private void Start()
    {
        if (ModuleManager.Settings.module != KernSettings.Module.SIM)
        {
            this.transform.parent.gameObject.SetActive(false);
            return;
        }
        inputActionReference.action.performed += OnButtonPress;
        inputActionReference.action.canceled += OnButtonRelease;
        isPlayer = GetComponentInParent<AvatarController>().Object.HasInputAuthority;
        lastReleaseTime = 0.0f;
        isCooldown = false;
        wasActive = false;
        activationRequested = false;
        particles = new ParticleSystem.Particle[particleEffect.main.maxParticles];
    }

    private void OnDisable()
    {
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

        var main = particleEffect.main;  // Define main here so it can be accessed later

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

            // Change the color of all live particles.
            for (int i = 0; i < numParticlesAlive; i++)
            {
                particles[i].startColor = main.startColor.color;
            }

            // Apply the particle changes to the particle system
            particleEffect.SetParticles(particles, numParticlesAlive);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ActivateParticleEffect(bool isActive)
    {
        if (particleEffect != null)
        {
            if (isActive)
            {
                particleEffect.Play();
                audioSource.Play();
            }
            else
            {
                particleEffect.Stop();
                audioSource.Stop();
            }
        }
    }
}
