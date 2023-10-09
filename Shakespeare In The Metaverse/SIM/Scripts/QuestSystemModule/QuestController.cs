using Fusion;
using Kreis.Audio;
using MUXR.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Server only class
[RequireComponent(typeof(Spawnable))]
public class QuestController : NetworkBehaviour
{
    #region ----Fields----
    public InteractionsProgresser lightsQuestProgresser;
    public VolcapSequenceController mainVolcapSequence;
    public List<AudioTrack> mutedTracks;

    public int secondsBeforeStoppingAudio = 10;
    public int secondsBeforeVolcapStarts = 14;
    public string questLabel = "Make sound with some of the furniture";

    private int playersJoined = 0;
    #endregion ----Fields----

    #region ----Methods----
    #region Initialization
    public override void Spawned()
    {
        GetComponent<Spawnable>().OnSpawned += Init;
    }

    public void Init(GameObject originalSpawnable)
    {
        // ,Get references
        if (Object.HasStateAuthority)
        {
            CreateQuest();
            ActivateQuest();
            InitVolcapEventController(originalSpawnable.GetComponent<QuestController>());
        }

        QuestChannel.Instance.OnQuestCompleted += CompletedQuest;
    }
    public void ResetQuest()
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log("ResetQuest");
        CreateQuest();
        ActivateQuest();
    }

    private void InitVolcapEventController(QuestController originalQuestController)
    {
        try
        {
            Spawnable spawnableTarget;
            if (originalQuestController.mainVolcapSequence.TryGetComponent<Spawnable>(out spawnableTarget))
            {
                var spawnedTarget = SpawnableManager.GetSpawnedObject(spawnableTarget.LocalIndex);
                if (spawnedTarget != null)
                    mainVolcapSequence = spawnedTarget.GetComponent<VolcapSequenceController>();
            }
        }
        catch (Exception e) { Debug.Log("Error init volcap quest controller"); }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        playersJoined++;
        if (playersJoined == 1)
        {
            CreateQuest();
            ActivateQuest();
        }
    }
    #endregion Initialization

    #region Quest handling
    public void CreateQuest()
    {
        if (!Object.HasStateAuthority)
            return;

        //TODO: Change to quest by parameter
        InteractionProgressQuest lightXTimesQuest = new InteractionProgressQuest();
        lightXTimesQuest.Init(questLabel, objective: lightsQuestProgresser.targetObjects.Count, uid: "L2L", lightsQuestProgresser);
    }

    public void ActivateQuest()
    {
        if (!Object.HasStateAuthority) return;
        QuestChannel.Instance.ActivateQuest("L2L");
    }

    public void CompletedQuest(QuestData questData)
    {
        if (Object.HasStateAuthority)
            StartCoroutine(WaitForSecondsBeforeVolcapStarts());


        Debug.Log("Quest completed: " + questData.uid + " Quest name: " + questData.questName + " Quest state: " + questData.state);
    }
    IEnumerator WaitForSecondsBeforeVolcapStarts()
    {
        yield return new WaitForSeconds(secondsBeforeStoppingAudio);
        RPC_StopInteractionTrack();
        if (secondsBeforeVolcapStarts > 0)
            yield return new WaitForSeconds(secondsBeforeVolcapStarts);

        RPC_StopInteractionTrack();
        mainVolcapSequence.RPC_PlaySequence();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StopInteractionTrack()
    {
        foreach (var track in mutedTracks)
            KreisAudioManager.StopAllAudioInATrack(track);

        KreisAudioManager.StopAudio(AudioConsts.D_KNOCKING);
        KreisAudioManager.StopAudio(AudioConsts.D_PORTER_SNORES);
    }
    #endregion Quest handling
    #endregion ----Methods----
}

