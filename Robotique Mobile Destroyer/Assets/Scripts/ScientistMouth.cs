using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScientistMouth : MonoBehaviour
{

    public AudioSource source;
    public Transform mouthBone;

    public float mouthAmplitude;

    Vector3 initialMouthRotation;

    public float updateStep = 0.05f;
    public int sampleDataLength = 256;

    private float currentUpdateTime = 0f;

    private float clipLoudness;
    private float[] clipSampleData;

    // Start is called before the first frame update
    void Start()
    {
        currentUpdateTime = 0f;
        clipSampleData = new float[sampleDataLength];
        initialMouthRotation = mouthBone.localRotation.eulerAngles;
        source.pitch = Random.Range(0.9f, 1.1f);
    }

    // Update is called once per frame
    void Update()
    {
        if(source.isPlaying)
        {
            currentUpdateTime += Time.deltaTime;
            if (currentUpdateTime >= updateStep)
            {
                currentUpdateTime = 0f;
                source.clip.GetData(clipSampleData, source.timeSamples); //I read 1024 samples, which is about 80 ms on a 44khz stereo clip, beginning at the current sample position of the clip.
                clipLoudness = 0f;
                foreach (var sample in clipSampleData)
                {
                    clipLoudness += Mathf.Abs(sample);
                }
                clipLoudness /= sampleDataLength; //clipLoudness is what you are looking for
            }
            mouthBone.SetLocalPositionAndRotation(mouthBone.localPosition, Quaternion.Euler(initialMouthRotation + new Vector3(0f, 0f, -clipLoudness * mouthAmplitude)));
        }
    }

    public void Say(AudioClip clip)
    {
        source.Stop();
        currentUpdateTime = 0f;
        source.clip = clip;
        source.Play();
    }
}
