using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// the object is enabled by the hands physics, the base object is kinetic so the push obj doesnt move it. both objects are in emptys so that the objects are at location 0. needed info for start position vector3.
public class EventButton : MonoBehaviour
{
    [SerializeField]
    private float threshold = 0.1f;
    [SerializeField]
    private float deadZone = 0.025f;
    private bool isPressed;
    private Vector3 startPos;
    private ConfigurableJoint joint;
    public UnityEvent onPressed, onRealeased;

    void Start()
    {
        startPos = transform.localPosition;
        joint = GetComponent<ConfigurableJoint>();
    }

    void OnEnable()
    {
        transform.localPosition = startPos;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < timeBetweenPresses)
            return;
        timer = 0;

        if (!isPressed && GetValue() + threshold >= 1)
            Pressed();
        if (isPressed && GetValue() - threshold <= 0)
            Released();
    }
    private float timer = 0;
    private float timeBetweenPresses = .2f;

    private void Pressed()
    {
        isPressed = true;
        onPressed.Invoke();
        transform.localPosition = startPos;
        Debug.Log("pressed");
    }

    private void Released()
    {
        isPressed = false;
        transform.localPosition = startPos;
        onRealeased.Invoke();
        Debug.Log("released");
    }

    private float GetValue()
    {
        var value = Vector3.Distance(startPos, transform.localPosition) / joint.linearLimit.limit;

        if (Mathf.Abs(value) < deadZone)
            value = 0;

        return Mathf.Clamp(value, -1f, 1f);
    }
}
