using UnityEngine;

public class DoorAnimationController : MonoBehaviour
{
    public int index;
    public Animator animator;
    public string trigger;

    public void SetTriggerOfDoorAnim()
    {
        animator.SetTrigger(trigger);
    }

}
