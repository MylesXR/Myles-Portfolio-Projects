using Fusion;
using Kreis.Audio;
using MUXR.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CircuitManager : NetworkBehaviour
{
    #region ----Fields----
    [SerializeField] private List<InteractionPoint> interactionPoints;
    public List<InteractionPoint> InteractionPoints { get => interactionPoints; set => interactionPoints = value; }
    [SerializeField] private TargetObjectController targetObjectController;

    public int cirucitManagerIndex = 0;
    public bool shouldhaveDynamicInteractionPoints = true;

    private bool initiated = false;
    private List<InteractionPoint> neededInteractionPoints = new List<InteractionPoint>();
    private int currentPlayerCount = 0;
    private bool circuitComplete = false;

    public float intervalInteractionsAffordance = 10f;
    private float timer = 0f;
    #endregion ----Fields----

    #region ----Methods----
    #region Init
    public override void Spawned()
    {
        GetComponent<Spawnable>().OnSpawned += Init;
    }

    public void Init(GameObject originalSpawnable)
    {
        try
        {
            CircuitManager originalCircuitManager = originalSpawnable.GetComponents<CircuitManager>()[cirucitManagerIndex];
            Debug.Log("Init circuit" + originalSpawnable.name);
            InitInteractionPoints(originalCircuitManager);
            InitTarget(originalCircuitManager);

            timer = UnityEngine.Random.Range(0, intervalInteractionsAffordance - 1);
            initiated = true;
        }
        catch (Exception e) { Debug.Log("Circuit manager init failed:i " + gameObject.name); }
    }

    private void InitInteractionPoints(CircuitManager originalCircuitManager)
    {
        try
        {
            var missingInteractionPointsIndex = interactionPoints.Where(interactionPoint => interactionPoint == null).Select((item, index) => index).ToList();
            if (missingInteractionPointsIndex.Count > 0)
            {
                foreach (var missingPoint in missingInteractionPointsIndex)
                {
                    Spawnable spawnableTarget;
                    if (originalCircuitManager.interactionPoints[missingPoint].TryGetComponent<Spawnable>(out spawnableTarget))
                    {
                        var spawnedTarget = SpawnableManager.GetSpawnedObject(spawnableTarget.LocalIndex);
                        if (spawnedTarget != null)
                            interactionPoints[missingPoint] = spawnedTarget.GetComponent<InteractionPoint>();
                    }
                    else
                        interactionPoints[missingPoint] = originalCircuitManager.interactionPoints[missingPoint];
                }
            }
            neededInteractionPoints = interactionPoints;
        }
        catch (Exception e) { Debug.Log("Fail init circuit: " + gameObject.name); }
    }

    private void InitTarget(CircuitManager originalCircuitManager)
    {
        try
        {
            Spawnable spawnableTarget;
            if (originalCircuitManager.targetObjectController.TryGetComponent<Spawnable>(out spawnableTarget))
            {
                var spawnedTarget = SpawnableManager.GetSpawnedObject(spawnableTarget.LocalIndex);
                if (spawnedTarget != null)
                    targetObjectController = spawnedTarget.GetComponent<TargetObjectController>();
            }
        }
        catch (Exception e) { Debug.Log("Fail init circuit target: " + gameObject.name); }
    }
    #endregion Init

    #region Flow

    private void Update()
    {
        CheckIfShouldPlayInteractionAffordance();

        if (Object == null || !Object.HasStateAuthority || !initiated || circuitComplete)
            return;

        if (IsCircuitComplete())
            targetObjectController.Activate();
    }

    private void CheckIfShouldPlayInteractionAffordance()
    {
        timer += Time.deltaTime;

        if (timer >= intervalInteractionsAffordance)
        {
            timer = 0f;
            if (interactionPoints.Count == 0)
                return;

            InteractionPoint randomInteractionPoint = interactionPoints[UnityEngine.Random.Range(0, interactionPoints.Count)];
            if (randomInteractionPoint != null)
                KreisAudioManager.PlayAudio(AudioConsts.SFX_INTERACTION_AFFORDANCE, new AudioJobOptions(volumen: 0.5f,
                                                                                        position: randomInteractionPoint.transform,
                                                                                        isOneShot: true));
        }
    }

    private bool IsCircuitComplete()
    {
        try
        {
            if (shouldhaveDynamicInteractionPoints)
                RPC_CalculateNeededInteractionPoints();
            foreach (var point in neededInteractionPoints)
                if (!point.IsActive)
                    return false;

            circuitComplete = true;
            return true;
        }
        catch (Exception e) { Debug.Log("IsCircuit complete failed", gameObject); }

        return false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CalculateNeededInteractionPoints()
    {
        if (currentPlayerCount != FusionManager.Runner.ActivePlayers.Count() && FusionManager.Runner.ActivePlayers.Count() != 0)
        {
            currentPlayerCount = FusionManager.Runner.ActivePlayers.Count();
            int neededPlayerInteractions = Mathf.CeilToInt(currentPlayerCount * 0.75f);
            int currentPlayerInteractions = 0;
            neededInteractionPoints = new List<InteractionPoint>();
            foreach (var interactionPoint in interactionPoints)
            {
                if (interactionPoint == null)
                    continue;

                if (currentPlayerInteractions >= neededPlayerInteractions)
                {
                    if (interactionPoint.GetComponent<TargetObjectController>() == null)
                        interactionPoint.gameObject.SetActive(false);
                    continue;
                }
                else if (currentPlayerInteractions + interactionPoint._activationThresholds[0] > neededPlayerInteractions)
                    interactionPoint._activationThresholds[0] = neededPlayerInteractions - currentPlayerInteractions;


                if (interactionPoint.GetComponent<TargetObjectController>() == null)
                    interactionPoint.gameObject.SetActive(true);
                neededInteractionPoints.Add(interactionPoint);
                currentPlayerInteractions += interactionPoint._activationThresholds[0];
            }
        }
    }


    #endregion Flow
    #endregion ----Methods----
}
