using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArduinoSimulator : MonoBehaviour
{
	[System.Serializable]
	public class Wheel
	{
		public WheelCollider wheelCollider;
		public bool leftSide;
	}

	public enum MovementState
	{
		forward,
		left,
		right,
		stop,
	}

	public MovementState movementState = MovementState.stop;

	public float odometer;
	float revolutionLimit;

	//This is the speed specifed from LabVIEW
	[HideInInspector]
	public float speed;

    [Header("Roues")]
    public Wheel roueAvG;
	public Wheel roueAvD;
	public Wheel roueArG;
	public Wheel roueArD;

	Wheel[] allWheels;

    [Header("Ajustements de comportement")]
    public float wheelSpeedTarget;
	public float turningRpmTargetMultiplier;
	public float torqueMultiplier;
	public float turningTorqueMultiplier;
	public float brakingTorque;

	public float degreesPerRevolution;
	public float incrementsPerRevolution;

    [Header("Roue cocaïne")]
    //la roue cocaïne mitige la secousse inuduite par l'endormissement des autres roues par PhysX
    public WheelCollider roueCocaine;

	// Start is called before the first frame update
	void Start()
	{
		allWheels = new Wheel[4];
		allWheels[0] = roueAvG;
		allWheels[1] = roueAvD;
		allWheels[2] = roueArG;
		allWheels[3] = roueArD;

		roueCocaine.motorTorque = 10f;

		if(speed == 0f)
		{
			speed = 20f;
		}
	}

	void FixedUpdate()
	{

		switch (movementState)
		{

			case MovementState.forward:
				foreach (Wheel wheel in allWheels)
				{
					wheel.wheelCollider.brakeTorque = 0f;
					wheel.wheelCollider.motorTorque = ((wheelSpeedTarget * speed) - wheel.wheelCollider.rpm) / 100 * torqueMultiplier;
				}
                break;

			case MovementState.left:
				foreach (Wheel wheel in allWheels)
				{
					wheel.wheelCollider.brakeTorque = 0f;
					wheel.wheelCollider.motorTorque =
						((wheelSpeedTarget * turningRpmTargetMultiplier * speed * (wheel.leftSide ? -1 : 1)) - wheel.wheelCollider.rpm)
						/ 100 * torqueMultiplier * turningTorqueMultiplier;
                }
                break;

            case MovementState.right:
                foreach (Wheel wheel in allWheels)
                {
                    wheel.wheelCollider.brakeTorque = 0f;
                    wheel.wheelCollider.motorTorque =
						((wheelSpeedTarget * turningRpmTargetMultiplier * speed * (wheel.leftSide ? 1 : -1)) - wheel.wheelCollider.rpm)
						/ 100 * torqueMultiplier * turningTorqueMultiplier;
                }
                break;

            case MovementState.stop:
                foreach (Wheel wheel in allWheels)
                {
                    wheel.wheelCollider.brakeTorque = brakingTorque;
                    wheel.wheelCollider.motorTorque = 0f;
                }
                break;
        }

        //we update the odometer every physics step
        foreach (Wheel wheel in allWheels)
		{
            odometer += Mathf.Abs((wheel.wheelCollider.rpm / 60) * Time.fixedDeltaTime)*0.25f;
        }

		//if there is a revolution limit and the odometer has reached it : stop, reset the odometer and revolution limit
		if(revolutionLimit > 0f && odometer > revolutionLimit)
		{
			RovST();
		}

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
			RovFW();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
			RovST();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
			RovTL();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
			RovTR();
        }
    }

    public void RovFW(float increments = -1f)
	{
		if (increments > 0f)
		{
			revolutionLimit = increments / incrementsPerRevolution;
		}
		else
		{
			revolutionLimit = -1f;
		}
		movementState = MovementState.forward;
        odometer = 0f;
    }

	public void RovTL(float degrees = -1f)
	{
		if (degrees > 0f)
		{
			revolutionLimit = degrees / degreesPerRevolution;
		}
		else
		{
            revolutionLimit = -1f;
        }
		movementState = MovementState.left;
        odometer = 0f;
    }

	public void RovTR(float degrees = -1f)
	{
		if (degrees > 0f)
		{
            revolutionLimit = degrees / degreesPerRevolution;
        }
		else
		{
            revolutionLimit = -1f;
        }
		movementState = MovementState.right;
        odometer = 0f;
    }

    public void RovST()
    {
		movementState = MovementState.stop;
        odometer = 0f;
        revolutionLimit = -1f;
    }

	public void RovSP(int wantedSpeed)
	{
		speed = (float)wantedSpeed;
	}
}