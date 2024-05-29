using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelAnimation : MonoBehaviour
{
    [System.Serializable]
    public class Wheel
    {
        public WheelCollider wheelCollider;
        public Transform meshTransform;
    }

    Vector3 rotationAxis = new Vector3(0f, 0f, 1f);


    public Wheel[] roues;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        foreach(Wheel roue in roues)
        {
            roue.meshTransform.Rotate(rotationAxis, roue.wheelCollider.rpm / 60 * Time.deltaTime * -360);
        }
    }
}
