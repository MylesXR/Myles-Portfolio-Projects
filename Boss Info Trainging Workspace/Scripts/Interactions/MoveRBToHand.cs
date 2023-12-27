using System.Collections;
using UnityEngine;
using MUXR;

public class MoveRBToHand : MonoBehaviour
{
    [SerializeField] private Rigidbody objectToMoveR;
    [SerializeField] private Rigidbody objectToMoveL;

    private Transform _controllerR;
    private Transform _controllerL;

    [SerializeField] private float returnDelay = 3f;  // The delay before the object is moved back to the hand

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        // Wait until KernRig instance is ready
        while (KernRig.Instance == null || KernRig.Instance.RightHandController == null || KernRig.Instance.LeftHandController == null)
        {
            yield return null;
        }

        _controllerR = KernRig.Instance.RightHandController.transform;
        _controllerL = KernRig.Instance.LeftHandController.transform;

        StartCoroutine(MoveObjectCoroutine());
    }

    private IEnumerator MoveObjectCoroutine()
    {
        while (true)
        {
            // Wait for the delay duration
            yield return new WaitForSeconds(returnDelay);

            // Move each object to the corresponding controller's position
            objectToMoveR.MovePosition(_controllerR.position);
            objectToMoveL.MovePosition(_controllerL.position);
        }
    }
}
