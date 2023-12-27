using Fusion;
using Kreis.Audio; // custom audio manager of the experience 
using MUXR.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CircuitManager : NetworkBehaviour
{
    #region ---- Fields ----
    [SerializeField] private List<InteractionPoint> interactionPoints; // List of InteractionPoints script components - can be populated in inspector
    public List<InteractionPoint> InteractionPoints { get => interactionPoints; set => interactionPoints = value; } // Property for InteractionPoints Networking.
    [SerializeField] private TargetObjectController targetObjectController; // Controller script that triggers the target object

    // Various settings and variables
    public int cirucitManagerIndex = 0;
    public bool shouldhaveDynamicInteractionPoints = true;  

    private bool initiated = false;
    private List<InteractionPoint> neededInteractionPoints = new List<InteractionPoint>();
    private int currentPlayerCount = 0; //some InteractionPoints have a higher then 1 required player to create a differnt style of group Interaction. 
                                        //this variable is used for the networking of those InteractionPoints.
    private bool circuitComplete = false; 

    public float intervalInteractionsAffordance = 10f; //once a sound affordance has started at its random start time it will repeate at this rate. 
    private float timer = 0f;
    #endregion ---- Fields ----

    #region ---- Methods ----
    
    #region Init 
    //init region populates variables in circuit manager on spawnables that are in other scripts. This works as the brain that makes the modular system work in multiplayer.

    // Called when the object is spawned
    public override void Spawned()
    {
        GetComponent<Spawnable>().OnSpawned += Init;
    }

    // Initialization method for spawnables used so spawnables can talk to other scripts on the network. It first populates interaction points because they are needed for the target object as well.
    public void Init(GameObject originalSpawnable)
    {
        try
        {
            // Obtain the original CircuitManager component
            CircuitManager originalCircuitManager = originalSpawnable.GetComponents<CircuitManager>()[cirucitManagerIndex];
            Debug.Log("Init circuit" + originalSpawnable.name);

            // Initialize interaction points and target
            InitInteractionPoints(originalCircuitManager); //interaction points in the circuit manager
            InitTarget(originalCircuitManager); //call the circuit manager

            // Set a random timer for interaction affordance sound. This affordance plays a sound of a small burst of electricity.
            timer = UnityEngine.Random.Range(0, intervalInteractionsAffordance - 1);
            initiated = true; // Set initiation flag
        }
        catch (Exception e) { Debug.Log("Circuit manager init failed:i " + gameObject.name); }
    }

    // Initialize interaction points
    private void InitInteractionPoints(CircuitManager originalCircuitManager)
    {
        try
        {
            // Find missing interaction points and replace them on spawned objects
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

    // Initialize target object
    private void InitTarget(CircuitManager originalCircuitManager)
    { 
        //Initalize target object variable of circuit manager when interaction points have been populated
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

    // Main flow methods
    #region Flow
    
    private void Update()
    {
        // Check if interaction sound affordance should be played
        CheckIfShouldPlayInteractionAffordance();

        // Check conditions for circuit completion
        if (Object == null || !Object.HasStateAuthority || !initiated || circuitComplete)
            return;

        // If the circuit is complete, activate the target object unity event
        if (IsCircuitComplete())
            targetObjectController.Activate();
    }

    // Check if interaction affordance sound should be played
    private void CheckIfShouldPlayInteractionAffordance()
    {
        timer += Time.deltaTime;

        // Play affordance sound at random intervals. Randomized so sounds are not overlapped on all interaction points
        if (timer >= intervalInteractionsAffordance)
        {
            timer = 0f;
            if (interactionPoints.Count == 0)
                return;

            // Play sound at a random interaction point. Used to help players find InteractionPoints
            InteractionPoint randomInteractionPoint = interactionPoints[UnityEngine.Random.Range(0, interactionPoints.Count)];
            if (randomInteractionPoint != null)
                KreisAudioManager.PlayAudio(AudioConsts.SFX_INTERACTION_AFFORDANCE, new AudioJobOptions(volumen: 0.5f,
                                                                                        position: randomInteractionPoint.transform,
                                                                                        isOneShot: true));
        }
    }

    // Check if the circuit is complete
    private bool IsCircuitComplete()
    {
        try
        {
            // If dynamic interaction points are required, calculate them using RPC
            if (shouldhaveDynamicInteractionPoints)
                RPC_CalculateNeededInteractionPoints();

            // Check if all needed interaction points are active
            foreach (var point in neededInteractionPoints)
                if (!point.IsActive)
                    return false;

            circuitComplete = true; // Set circuit completion flag to trigger TargetObject
            return true;
        }
        catch (Exception e) { Debug.Log("IsCircuit complete failed", gameObject); }

        return false;
    }

    // RPC method to calculate needed interaction points on network. Each interaction point is triggered by a seperate player and therfore
    // the circut manager needs to be triggered on the network. This does not apply if the experience player count is 1. 
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CalculateNeededInteractionPoints()
    {
        //check if player count has been met or if player count is 0
        if (currentPlayerCount != FusionManager.Runner.ActivePlayers.Count() && FusionManager.Runner.ActivePlayers.Count() != 0)
        {

            currentPlayerCount = FusionManager.Runner.ActivePlayers.Count();
            int neededPlayerInteractions = Mathf.CeilToInt(currentPlayerCount * 0.75f);
            int currentPlayerInteractions = 0;
            //neededInteractionPoints is set to the size of the interaction point list
            neededInteractionPoints = new List<InteractionPoint>();
            foreach (var interactionPoint in interactionPoints)
            {
                if (interactionPoint == null)
                    continue;

                if (currentPlayerInteractions >= neededPlayerInteractions)
                {
                    // Some interaction point objects also have TargetObject components. This section is used to filter those Interaction points
                    // because they are set by the UnityEvent in other TargetObjects using the method IncreasePlayerCountWithoutHittingButton
                    // in the InteractionPoint script. This is so that a group of TargetObjects can be used to trigger another TargetObject.
                    // By doing this we can create layeres of interaction that complete goals when the desired amount of TargetObjects are triggered.
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
#endregion Methods
}
