using Fusion;
using System.Collections;
using UnityEngine;

public class InteractionProgressQuest : Quest
{
    public InteractionsProgresser questProgresser;
    public int neededClicks;


    public override string Init(string questName, int objective = 0, string uid = null, params object[] args)
    {
        questProgresser = (InteractionsProgresser)args[0];
        return base.Init(questName, objective, uid, args);
    }

    protected override void QuestActivated()
    {
        if (questProgresser.InteractionProgress >= questData.total)
            this.NotifyCompletion();
        else
            questProgresser.questProgressed += NotifyProgress;
    }

    protected override void QuestProgressed()
    {
        Debug.Log("Progress");
        if (questProgresser.InteractionProgress >= questData.total)
        {
            questProgresser.InteractionProgress = 0;
            this.NotifyCompletion();
        }
    }

    protected override void QuestCompleted()
    {
        Debug.Log("Quest Completed");
        questProgresser.questProgressed -= NotifyProgress;
    }
}
