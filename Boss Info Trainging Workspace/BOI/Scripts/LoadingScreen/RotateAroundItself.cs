using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAroundItself : MonoBehaviour
{
    public float speed;
    void Update()
    {
        this.transform.Rotate(Vector3.up * speed * Time.deltaTime, Space.World);
    }
}
