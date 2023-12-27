using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkInteractionParticle : NetworkBehaviour
{
    public ParticleSystem particleEffect;
    public InteractionPoint interactionPoint;

    private void Start()
    {
        particleEffect = GetComponentInChildren<ParticleSystem>();
        interactionPoint = GetComponentInParent<InteractionPoint>();
    }

    private void Update()
    {
        if (interactionPoint.IsActive)
            ParticleOn();
        else
            ParticleOff();

    }
    public void ParticleOn()    //vicinity is hovered
    {
        if (particleEffect.isStopped)
            RPC_ActivateParticleEffect(true);
    }

    public void ParticleOff()     //vicinity is not hovered
    {
        if (particleEffect.isPlaying)
            RPC_ActivateParticleEffect(false);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ActivateParticleEffect(bool isActive)
    {
        if (particleEffect != null)
        {
            if (isActive)
            {
                particleEffect.Play();
            }
            else
            {
                particleEffect.Stop();
            }
        }
    }
}
