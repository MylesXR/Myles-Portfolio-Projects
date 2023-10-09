using Fusion;
using UnityEngine;

public struct QuestData : INetworkStruct
{
    public NetworkString<_16> uid;
    public NetworkString<_16> questName;
    public QuestState state;
    public int progress;
    public int total;
}

public abstract class Quest
{
    #region ----Fields----
    protected QuestChannel questsChannel;
    public QuestData questData;
    #endregion ----Fields----

    #region ----Methods----
    #region Initialization 
    public virtual string Init(string questName, int total = 0, string uid = null, params object[] args)
    {
        questData = new QuestData()
        {
            questName = questName,
            uid = uid != null ? uid : Random.Range(0, 100).ToString(),
            state = QuestState.Pending,
            progress = 0,
            total = total
        };

        questsChannel = QuestChannel.Instance;
        questsChannel.RegisterQuest(this);
        return uid;
    }
    #endregion Initialization 

    #region Quest Active
    public bool ActivateQuest()
    {
        if (questData.state == QuestState.Active)
        {
            Debug.Log($"Quest: {questData.questName}, already active");
            return false;
        }

        questData.state = QuestState.Active;
        QuestActivated();

        Debug.Log($"Quest: {questData.questName}, activated");
        return true;
    }

    protected abstract void QuestActivated();
    #endregion Quest Active

    #region Quest Progress
    /// <summary>
    /// Notify that the quest has been progressed
    /// </summary>
    protected void NotifyProgress()
    {
        questData.progress++;
        questsChannel.QuestProgress(this.questData.uid.ToString());
    }

    public bool ProgressQuest()
    {
        if (questData.state == QuestState.Completed)
        {
            Debug.Log($"Quest: {questData.questName}, already completed, can't progress it more");
            return false;
        }
        if (questData.state == QuestState.Pending)
        {
            Debug.Log($"Quest: {questData.questName}, hasn't started yet.");
            return false;
        }

        //Allow childs of this abstraction to perform logic on progression of quest
        QuestProgressed();

        Debug.Log($"Quest: {questData.questName}, progressed");
        return true;
    }

    protected abstract void QuestProgressed();
    #endregion Quest Progress

    #region Quest Completed
    /// <summary>
    /// Notify that the quest has been completed
    /// </summary>
    protected void NotifyCompletion()
    {
        questsChannel.CompleteQuest(questData.uid.ToString());
    }

    public bool CompleteQuest()
    {
        if (questData.state == QuestState.Completed)
        {
            Debug.Log($"Quest: {questData.questName}, already completed");
            return false;
        }

        questData.state = QuestState.Completed;

        //Allow childs of this abstraction to perform logic on completion of quest
        QuestCompleted();
        Debug.Log($"Quest: {questData.questName}, completed");
        return true;

    }

    protected abstract void QuestCompleted();
    #endregion Quest Completed
    #endregion ----Methods----
}
