using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LidarAnimation : MonoBehaviour
{

    public Transform lidarMeshTransform;

    public float lidarRevPerSec;

    Vector3 rotationAxis = new Vector3(0f, 0f, 1f);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        lidarMeshTransform.Rotate(rotationAxis, 360 * lidarRevPerSec * Time.fixedDeltaTime);
    }
}
