using Fusion;
using MUXR.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlabController : NetworkBehaviour
{
    public List<GameObject> slabs = new List<GameObject>();
    public List<InteractionPoint> interactionPoints = new List<InteractionPoint>();

    public override void Spawned()
    {
        GetComponent<Spawnable>().OnSpawned += Init;
    }

    public void Init(GameObject originalSpawnable)
    {
        try
        {
            SlabController originalSlabController = originalSpawnable.GetComponent<SlabController>();
            this.slabs = originalSlabController.slabs;
        }
        catch (Exception e) { Debug.Log("Error init slab controller: " + gameObject.name); }
    }

    public void SpawnSlabs(int numberOfPlayers)
    {
        RPC_SpawnSlabs(numberOfPlayers);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SpawnSlabs(int numberOfPlayers)
    {
        if (numberOfPlayers > slabs.Count)
            numberOfPlayers = slabs.Count;
        if (numberOfPlayers < 0)
            numberOfPlayers = 0;

        //Slabs
        for (int i = 0; i < slabs.Count; i++)
            slabs[i].SetActive(i < numberOfPlayers);

        //Interaction points
        List<InteractionPoint> interactionPointsForCircuitManager = new List<InteractionPoint>();
        for (int i = 0; i < interactionPoints.Count; i++)
        {
            interactionPoints[i].gameObject.SetActive(i < numberOfPlayers);
            if (i < numberOfPlayers)
                interactionPointsForCircuitManager.Add(interactionPoints[i]);
        }

        this.GetComponent<CircuitManager>().InteractionPoints = interactionPointsForCircuitManager;
    }


}
