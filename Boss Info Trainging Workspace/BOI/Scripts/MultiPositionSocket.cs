using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MultiPositionSocket : XRSocketInteractor
{
    [Range(0, 2)] public int correctAttachment = 0;
    [SerializeField] private List<Transform> attachmentTransforms;
    private int currentAttachmentIndex = 0;
    private MeshFilter testPieceMesh;

    public Transform GetCurrentAttachment(Transform currentPiece)
    {
        if (this.transform.localScale != Vector3.one * 0.1180f)
            this.transform.localScale = Vector3.one * 0.1180f;

        // Set possible rotations
        IEnumerable<Transform> angleAttachments = FindClosesRotation(currentPiece.rotation);

        float closestDistance = float.MaxValue;
        Transform closestAttachment = null;
        foreach (var attachment in angleAttachments)
        {
            var distance = Vector3.Distance(currentPiece.position, attachment.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestAttachment = attachment;
            }
        }

        if (closestAttachment != null)
            return closestAttachment.transform;

        return null;
    }

    private IEnumerable<Transform> FindClosesRotation(Quaternion rotation)
    {
        float closestAngle = float.MaxValue;
        Quaternion closestQuaternionAngle = Quaternion.identity;

        for (int i = 0; i < attachmentTransforms.Count; i++)
        {
            float angle = Quaternion.Angle(rotation, attachmentTransforms[i].rotation);

            if (angle < closestAngle)
            {
                closestAngle = angle;
                closestQuaternionAngle = attachmentTransforms[i].rotation;
            }
        }
        return attachmentTransforms.Where(attachment => attachment.rotation == closestQuaternionAngle);
    }
}
