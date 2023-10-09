using Fusion;
using Kreis.Audio;
using MUXR.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Spawnable))]
public class InteractionsProgresser : NetworkBehaviour
{
    public QuestChannel questChannel;

    public List<TargetObjectController> targetObjects;
    public Action questProgressed;
    public bool isDiningHall = false;
    public Transform porterDoorTransform;

    [Networked(OnChanged = "OnInteractionProgressed")]
    public int InteractionProgress { get; set; }

    public override void Spawned()
    {
        GetComponent<Spawnable>().OnSpawned += (gameObject) =>
        {
            targetObjects.Clear();
            var originalLightProgresser = gameObject.GetComponent<InteractionsProgresser>();
            for (int i = 0; i < originalLightProgresser.targetObjects.Count; i++)
            {
                var lightTargets = originalLightProgresser.targetObjects[i];

                bool isTargetOnParent = false;
                Spawnable spawnableTarget;
                if (!lightTargets.gameObject.TryGetComponent<Spawnable>(out spawnableTarget))
                {
                    spawnableTarget = lightTargets.gameObject.GetComponentInParent<Spawnable>(true);
                    isTargetOnParent = true;
                    if (spawnableTarget == null)
                        return;
                }

                var spawnedTarget = SpawnableManager.GetSpawnedObject(spawnableTarget.LocalIndex);
                if (spawnedTarget != null)
                {
                    var spawnedTargetController = spawnedTarget.GetComponent<TargetObjectController>();
                    if (spawnedTargetController != null)
                    {
                        this.targetObjects.Add(isTargetOnParent ? spawnedTarget.GetComponentInChildren<TargetObjectController>(true) : spawnedTarget.GetComponent<TargetObjectController>());
                        this.targetObjects[i]._eventActivated += ProgressQuest;
                    }
                    else
                    {
                        Debug.Log("ERROR IN INTERACTIONS PROGRESSER");
                    }
                }
            }
        };
    }

    public void ProgressQuest()
    {
        RPC_ServerProgressQuest();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ServerProgressQuest()
    {
        InteractionProgress++;
        RPC_PlayPorterSound(InteractionProgress);
        questProgressed?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayPorterSound(int progress)
    {
        if (!isDiningHall)
        {
            KreisAudioManager.StopAudio(AudioConsts.D_PORTER_SNORES);
            if (progress == 1)
                KreisAudioManager.PlayAudio(AudioConsts.D_PORTER_WAKES_1, new AudioJobOptions() { volume = 0.1f, delay = 3, position = porterDoorTransform });
            if (progress == 2)
                KreisAudioManager.PlayAudio(AudioConsts.D_PORTER_WAKES_2, new AudioJobOptions() { volume = 0.1f, delay = 3, position = porterDoorTransform });
            if (progress == 3)
            {
                KreisAudioManager.PlayAudio(AudioConsts.D_PORTER_WAKES_3, new AudioJobOptions() { volume = 0.1f, delay = 2, position = porterDoorTransform });
                KreisAudioManager.PlayAudio(AudioConsts.D_PORTER_WAKES, new AudioJobOptions() { volume = 0.7f, delay = 5, position = porterDoorTransform });
                return;
            }

            KreisAudioManager.PlayAudio(AudioConsts.D_PORTER_SNORES, new AudioJobOptions() { volume = 0.7f, delay = 8 });
        }
    }

    public static void OnInteractionProgressed(Changed<InteractionsProgresser> changed)
    {
        Debug.Log("Interactions progressed: " + changed.Behaviour.InteractionProgress);
    }

}
