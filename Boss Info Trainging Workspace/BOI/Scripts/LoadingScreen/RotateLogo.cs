using UnityEngine;

public class RotateLogo : MonoBehaviour
{
    public float rotationSpeed = 50f;
    public float maxRotationAngle = 30f; // Maximum rotation angle in degrees

    private float currentAngle = 0f;
    private int rotationDirection = 1;

    // Update is called once per frame
    void Update()
    {
        float targetAngle = currentAngle + (rotationSpeed * Time.deltaTime * rotationDirection);

        // Clamp the targetAngle to the maximum rotation angle
        targetAngle = Mathf.Clamp(targetAngle, -maxRotationAngle, maxRotationAngle);

        // Calculate the rotation delta
        float rotationDelta = targetAngle - currentAngle;

        // Rotate the cube by the calculated delta
        transform.Rotate(Vector3.up * rotationDelta);

        // Update the currentAngle
        currentAngle = targetAngle;

        // Reverse the rotation direction when reaching the threshold
        if (currentAngle >= maxRotationAngle || currentAngle <= -maxRotationAngle)
        {
            rotationDirection *= -1;
        }
    }
}
