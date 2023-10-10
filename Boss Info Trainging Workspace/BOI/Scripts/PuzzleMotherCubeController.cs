using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PiecePrefabData
{
    public GameObject gameObject;
    public float cubeSize = 0.227f;
}

public class PuzzleMotherCubeController : MonoBehaviour
{

    public List<PiecePrefabData> PiecePrefabs;
    public PiecePrefabData currentPiece;


    public List<Transform> finalList = new List<Transform>();

    public void Start()
    {
        if (currentPiece == null)
            currentPiece = PiecePrefabs[0];
    }

    [ContextMenu("Fill all pieces")]
    public void FillAllPieces()
    {
        StartCoroutine(_FillAllPieces());
    }
    IEnumerator _FillAllPieces()
    {
        foreach (var piece in PiecePrefabs)
        {
            currentPiece = piece;
            AttachmentFiller();
            yield return new WaitForSeconds(4);
        }
    }

    [ContextMenu("Fill current piece")]
    public void AttachmentFiller()
    {
        finalList.Clear();
        GameObject parent = new GameObject("INSTANCE_" + currentPiece.gameObject.name);
        // Iterate through every possible position
        for (float x = -1; x <= 1; x++)
        {
            for (float y = -1; y <= 1; y++)
            {
                for (float z = -1; z <= 1; z++)
                {
                    // Iterate through every possible rotation
                    for (int rotX = 0; rotX < 360; rotX += 90)
                    {
                        for (int rotY = 0; rotY < 360; rotY += 90)
                        {
                            for (int rotZ = 0; rotZ < 360; rotZ += 90)
                            {
                                currentPiece.gameObject.SetActive(true);
                                GameObject attachmentInstance = Instantiate(currentPiece.gameObject, parent.transform);
                                currentPiece.gameObject.SetActive(false);
                                finalList.Add(attachmentInstance.transform);
                                Vector3 newPos = new Vector3(x * currentPiece.cubeSize, y * currentPiece.cubeSize, z * currentPiece.cubeSize);
                                Quaternion newRot = Quaternion.Euler(rotX, rotY, rotZ);
                                StartCoroutine(MovePieces(attachmentInstance, newPos, newRot));
                            }
                        }
                    }
                }
            }
        }
    }

    public void CleanseList()
    {
        var newList = new List<Transform>();
        foreach (var item in finalList)
            if (item != null)
                newList.Add(item);

        finalList = newList;
    }

    IEnumerator MovePieces(GameObject attachment, Vector3 pos, Quaternion rot)
    {
        yield return new WaitForSeconds(1);
        attachment.GetComponent<Rigidbody>().MovePosition(pos);
        attachment.GetComponent<Rigidbody>().MoveRotation(rot);
        yield return new WaitForSeconds(1);

        if (attachment != null)
        {
            Destroy(attachment.GetComponent<Rigidbody>());

            var collider = attachment.transform.GetChild(0);
            Destroy(collider.GetComponent<MeshCollider>());
            collider.parent = attachment.transform.parent;
            Destroy(attachment);

            collider.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
        }
        CleanseList();
    }
}

[Serializable]
public class AttachmentData
{
    public Vector3 position;
    public Quaternion rotation;
}
