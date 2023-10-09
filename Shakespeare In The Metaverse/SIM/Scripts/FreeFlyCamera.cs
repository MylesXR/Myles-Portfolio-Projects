using MyBox;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class FreeFlyCamera : MonoBehaviour
{
    #region ----Fields----
    #region Exposed
    [Space]

    [SerializeField, Tooltip("The script is currently active")]
    private bool active = true;

    [Space]
    [Header("Rotation")]

    [SerializeField, Tooltip("Camera rotation by mouse movement is active")]
    private bool enableRotation = true;

    [SerializeField, Tooltip("Sensitivity of mouse rotation")]
    private float mouseSense = 1.8f;

    [Space]
    [Header("FOV")]

    [SerializeField, Tooltip("Camera fov modification")]
    private bool enableFov = true;

    [SerializeField, Tooltip("Velocity of camera zooming in/out")]
    private float fovSpeed = 55f;

    [SerializeField, Tooltip("Min and max fov"), MinMaxRange(0, 200)]
    private RangedInt fovMaxMin = new RangedInt(40, 120);

    [Space]
    [Header("Movement")]

    [SerializeField, Tooltip("Camera movement by 'W','A','S','D','Q','E' keys is active")]
    private bool enableMovement = true;

    [SerializeField, Tooltip("Camera movement speed")]
    private float movementSpeed = 10f;

    [SerializeField, Tooltip("Speed of the quick camera movement when holding the 'Left Shift' key")]
    private float boostedSpeed = 50f;

    [Space]
    [Header("Acceleration")]

    [SerializeField, Tooltip("Acceleration at camera movement is active")]
    private bool enableSpeedAcceleration = true;

    [SerializeField, Tooltip("Rate which is applied during camera movement"), Range(0.1f, 0.9f)]
    private float speedAccelerationFactor = 0.5f;
    #endregion Exposed

    #region Private
    private Camera currentCamera;
    private CursorLockMode wantedMode;

    private float currentIncrease = 1;
    private float currentIncreaseMem = 0;
    private float currentSpeed = 0;

    private Vector3 initPosition;
    private Vector3 initRotation;
    #endregion Private
    #endregion ----Fields----

    #region ----Methods----
    #region Main logic
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (boostedSpeed < movementSpeed)
            boostedSpeed = movementSpeed;
    }
#endif


    private void Start()
    {
        currentCamera = GetComponent<Camera>();
        initPosition = transform.position;
        initRotation = transform.eulerAngles;
    }

    private void OnEnable()
    {
        if (active)
            wantedMode = CursorLockMode.Locked;
    }

    // Apply requested cursor state
    private void SetCursorState()
    {
        if (Keyboard.current.escapeKey.isPressed)
            Cursor.lockState = wantedMode = CursorLockMode.None;

        if (Mouse.current.leftButton.isPressed)
            wantedMode = CursorLockMode.Locked;

        // Apply cursor state
        Cursor.lockState = wantedMode;
        // Hide cursor when locking
        Cursor.visible = (CursorLockMode.Locked != wantedMode);
    }

    private void CalculateCurrentSpeed(bool moving)
    {
        currentIncrease = Time.deltaTime;

        if (!enableSpeedAcceleration || enableSpeedAcceleration && !moving)
        {
            currentIncreaseMem = 0;
            currentSpeed = (enableSpeedAcceleration && !moving) ? 0 : movementSpeed;
            return;
        }

        if (currentSpeed + currentIncrease < movementSpeed)
        {
            currentIncrease = (speedAccelerationFactor * movementSpeed) * Time.deltaTime;
            currentSpeed += currentIncrease;
        }
        else
            currentSpeed = movementSpeed;
    }

    private void Update()
    {
        if (Keyboard.current.backslashKey.wasPressedThisFrame)
        {
            active = !active;
            currentCamera.enabled = active;
            wantedMode = CursorLockMode.None;
        }
        if (!active)
            return;

        SetCursorState();

        if (Cursor.visible)
            return;

        // FOV
        var newFovValue = currentCamera.fieldOfView - (Mouse.current.scroll.ReadValue().normalized.y * Time.deltaTime * fovSpeed);
        if (enableFov)
        {
            if (newFovValue < fovMaxMin.Min)
                currentCamera.fieldOfView = fovMaxMin.Min;
            else if (newFovValue > fovMaxMin.Max)
                currentCamera.fieldOfView = fovMaxMin.Max;
            else
                currentCamera.fieldOfView = newFovValue;
        }

        // Movement
        if (enableMovement)
        {
            Vector3 deltaPosition = Vector3.zero;

            // Direction

            if (Keyboard.current.wKey.isPressed)
                deltaPosition += transform.forward;

            if (Keyboard.current.sKey.isPressed)
                deltaPosition -= transform.forward;

            if (Keyboard.current.aKey.isPressed)
                deltaPosition -= transform.right;

            if (Keyboard.current.dKey.isPressed)
                deltaPosition += transform.right;

            if (Keyboard.current.eKey.isPressed)
                deltaPosition += transform.up;

            if (Keyboard.current.qKey.isPressed)
                deltaPosition -= transform.up;

            // Speed
            if (Keyboard.current.leftShiftKey.isPressed)
                currentSpeed = boostedSpeed;
            else
                CalculateCurrentSpeed(deltaPosition != Vector3.zero);

            transform.position += deltaPosition * currentSpeed * Time.deltaTime;
        }

        // Rotation
        if (enableRotation)
        {
            // Pitch
            transform.rotation *= Quaternion.AngleAxis(
                -Mouse.current.delta.ReadValue().y * mouseSense,
                Vector3.right
            );

            // Paw
            transform.rotation = Quaternion.Euler(
                transform.eulerAngles.x,
                transform.eulerAngles.y + Mouse.current.delta.ReadValue().x * mouseSense,
                transform.eulerAngles.z
            );
        }

        // Return to init position
        if (Keyboard.current.rKey.isPressed)
        {
            transform.position = initPosition;
            transform.eulerAngles = initRotation;
        }
    }
    #endregion Main logic
    #endregion ----Methods----
}
