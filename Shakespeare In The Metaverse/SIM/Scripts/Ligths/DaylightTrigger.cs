using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DaylightTrigger : MonoBehaviour
{
    private void OnEnable()
    {
        FindObjectOfType<LightTrigger>().ChangeLightToDaylight();
    }
}
