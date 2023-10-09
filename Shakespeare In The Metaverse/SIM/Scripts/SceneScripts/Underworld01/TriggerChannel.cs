using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerChannel : MonoBehaviour
{
    public Action OnTrigger;
    public void CallAction()
    {
        OnTrigger?.Invoke();
    }
}
