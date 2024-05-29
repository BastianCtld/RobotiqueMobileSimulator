using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WalkingSound : MonoBehaviour
{
    public AudioSource source;

    public List<AudioClip> stepSounds;

    public NavMeshAgent agent;

    // Start is called before the first frame update
    void Start()
    {
        source.pitch = Random.Range(0.95f, 1.05f);
        StartCoroutine(WalkingSoundCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Step()
    {
        source.clip = stepSounds[(int)(Random.value * stepSounds.Count)];
        source.Play();
    }

    IEnumerator WalkingSoundCoroutine()
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 1f));
        while (true)
        {
            yield return new WaitForSeconds(0.33f);
            if (agent.velocity.magnitude > 0.1f)
            {
                Step();
            }
        }
    }
}
