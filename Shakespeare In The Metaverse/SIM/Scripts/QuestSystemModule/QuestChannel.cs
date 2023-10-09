using Fusion;
using MUXR.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestChannel : NetworkBehaviour
{
    #region ----Fields----
    public TMP_Text questLabel;
    public TMP_Text questProgress;

    public Action<QuestData> OnQuestActivated;
    public Action<QuestData> OnQuestProgress;
    public Action<QuestData> OnQuestCompleted;
    public Dictionary<string, Quest> quests = new Dictionary<string, Quest>();
    public string currentActiveQuest;

    public static QuestChannel Instance;
    #endregion ----Fields----

    #region ----Methods----
    #region Singleton
    public override void Spawned()
    {
        GetComponent<Spawnable>().OnSpawned += Init;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestState()
    {
        Debug.Log($" RPC request state quest view");
        if (!String.IsNullOrEmpty(currentActiveQuest))
            OnUpdateView(currentActiveQuest);
    }

    public void Init(GameObject originalSpawnable)
    {
        RPC_RequestState();
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);

        // Get references
        try
        {
            questLabel = originalSpawnable.GetComponent<QuestChannel>().questLabel;
            questProgress = originalSpawnable.GetComponent<QuestChannel>().questProgress;
        }
        catch (Exception e) { Debug.Log("Quest channel init fail: " + gameObject.name); }
    }
    #endregion Singleton

    #region Quest Initialization 
    public void RegisterQuest(Quest quest)
    {
        if (!Object.HasStateAuthority) return;

        if (quests.ContainsKey(quest.questData.uid.ToString()))
            quests[quest.questData.uid.ToString()] = quest;
        else
            quests.Add(quest.questData.uid.ToString(), quest);
    }
    #endregion Quest Initialization 

    #region Update view
    public void OnUpdateView(string questToActivate)
    {
        var data = quests[questToActivate].questData;
        Debug.Log($" RPC update quest view On: " + data.state);
        RPC_UpdateQuestView(data);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdateQuestView(QuestData data)
    {
        Debug.Log($" RPC update quest view to all: " + data.state);
        UpdateQuestView(data);
    }

    public void UpdateQuestView(QuestData data)
    {
        Debug.Log($"Update view: {data.questName} {data.progress}");
        Debug.Log($"Debug data => State:{data.state}");

        questLabel.text = $"{data.questName} ->";
        questProgress.text = $"{data.progress} / {data.total}";
        Color colorOfLabel = data.state == QuestState.Completed ? Color.green : Color.blue;
        questLabel.color = colorOfLabel;
        questProgress.color = colorOfLabel;
    }
    #endregion Update view

    #region Activate
    public void ActivateQuest(string questToActivate)
    {
        if (!Object.HasStateAuthority) return;
        //Set quest and activate it
        currentActiveQuest = questToActivate;

        if (quests[questToActivate].ActivateQuest())
            RPC_ActivateQuest(quests[questToActivate].questData);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ActivateQuest(QuestData data)
    {
        OnQuestActivated?.Invoke(data);
        UpdateQuestView(data);
    }
    #endregion Activate

    #region Progress
    public void QuestProgress(string questProgress)
    {
        if (!Object.HasStateAuthority) return;

        if (quests[questProgress].ProgressQuest())
            RPC_ProgressQuest(quests[questProgress].questData);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ProgressQuest(QuestData data)
    {
        OnQuestProgress?.Invoke(data);
        UpdateQuestView(data);
    }
    #endregion Progress

    #region Completion
    public void CompleteQuest(string completedQuest)
    {
        Debug.Log($" Complete 1");
        if (!Object.HasStateAuthority) return;
        if (quests[completedQuest].CompleteQuest())
            RPC_CompleteQuest(quests[completedQuest].questData);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CompleteQuest(QuestData data)
    {
        OnQuestCompleted?.Invoke(data);
        UpdateQuestView(data);
        if (currentActiveQuest == data.uid)
            currentActiveQuest = null;
    }
    #endregion Completion
    #endregion ----Methods----
}

public enum QuestState
{
    Pending,
    Active,
    Completed
}
