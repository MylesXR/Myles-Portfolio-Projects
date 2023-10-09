using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TriggerDoorAnim : MonoBehaviour
{
    public int index;
    private void OnEnable()
    {
        DoorAnimationController target = FindObjectsOfType<DoorAnimationController>().Where(doorController => doorController.index == index).ToArray()[0];
        target.SetTriggerOfDoorAnim();
    }
}
