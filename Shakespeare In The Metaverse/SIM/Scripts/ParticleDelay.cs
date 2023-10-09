using System.Collections;
using UnityEngine;

public class ParticleDelay : MonoBehaviour
{
    public float minDelay = 0f;
    public float maxDelay = 9f;

    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        // Wait for a random delay
        yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

        // Start the particle system
        ps.Play();
    }
}
