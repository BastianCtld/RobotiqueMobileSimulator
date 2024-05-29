using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;


public class LidarSimulator : MonoBehaviour
{
	public ServerSimulator serverSimulator;

	public float lidarErrorRange;

	public float[] distances;

	public Vector3 sensorOffset;

	int angleScannedLastFrame;

	float theoreticalLidarAngle;

	RaycastHit hit;

	// Start is called before the first frame update
	void Start()
	{
		distances = new float[360];

		angleScannedLastFrame = 0;
		theoreticalLidarAngle = 0f;
	}

	private void OnDrawGizmos()
	{
		for(int i = 0; i<distances.Length; i++)
		{
			Vector3 hitPoint = transform.TransformPoint(sensorOffset) + (Quaternion.AngleAxis((float)i, transform.up) * transform.forward * distances[i]);
			Gizmos.DrawLine(transform.TransformPoint(sensorOffset), hitPoint);
			Gizmos.DrawSphere(hitPoint, 0.05f);
		}
	}

    private void FixedUpdate()
    {
		theoreticalLidarAngle += Time.fixedDeltaTime * 360 * 10f;
		for(int i = angleScannedLastFrame; i< Mathf.FloorToInt(theoreticalLidarAngle); i++)
		{
			LidarScan(-i%(-360));
			angleScannedLastFrame = i%360;
		}

		if(theoreticalLidarAngle > 360)
		{
			theoreticalLidarAngle -= 360;

			Debug.Log(distances);
			Debug.Log(distances);
			serverSimulator.SendLidarToClient(distances);
		}

    }

    private void LidarScan(int angle)
	{
		//RaycastHit hit;
		//for (int i = 0; i < 360; i++)
		//{
		//	Ray ray = new Ray(transform.TransformPoint(sensorOffset), Quaternion.AngleAxis((float)i, transform.up) * transform.forward);

		//	if (Physics.Raycast(ray, out hit))
		//	{
		//		distances[i] = hit.distance + Random.Range(-lidarErrorRange*0.5f, lidarErrorRange*0.5f);
		//	}
		//	else
		//	{
		//		distances[i] = 0;
		//	}
		//}
		Ray ray = new Ray(transform.TransformPoint(sensorOffset), Quaternion.AngleAxis((float)angle, transform.up) * transform.forward);

		if (Physics.Raycast(ray, out hit))
		{
			distances[-angle] = hit.distance + UnityEngine.Random.Range(-lidarErrorRange * 0.5f, lidarErrorRange * 0.5f);
		}
		else
		{
			distances[angle] = 0;
		}
	}
}