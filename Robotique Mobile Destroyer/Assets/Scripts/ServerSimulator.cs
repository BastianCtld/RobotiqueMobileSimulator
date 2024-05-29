using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ServerSimulator : MonoBehaviour
{
	TcpListener tcpListener = null;

	TcpClient client = null;

	public ArduinoSimulator arduinoSimulator;

	public TMP_Text connectionStatusTMPText;
	string statusText;

	public TMP_Text dataTMPText;
	string dirtyLog = "";

	public TMP_InputField commandTMPInputField;

	public IPEndPoint endPoint;

	byte[] sendingQueue = null;

	private void Start()
	{
		SetupServer();
    }

	private void SetupServer()
	{
		Thread serverSetupThread = new Thread(() =>
		{
			endPoint = new IPEndPoint(IPAddress.Loopback, 6340);

			tcpListener = new TcpListener(endPoint);
            tcpListener?.Stop();
            tcpListener.Start();
			statusText = "En attente d'une connexion sur le port 6340";
			client = tcpListener.AcceptTcpClient();
			statusText = "Connecté !";
			Debug.Log("Connecte");

			StartTCPListenerThread(tcpListener);
			Thread.Sleep(300);
			StartTCPSendingThread();
		}
		);
		serverSetupThread.Start();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
        {
			client.Client.Send(FormatMessageForSending(commandTMPInputField.text));
		}

		if(Input.GetKeyDown(KeyCode.Escape))
		{
			client.Client.Send(sendingQueue);
		}
		connectionStatusTMPText.text = statusText;
		dirtyLog = dirtyLog.Substring(0, Math.Min(dirtyLog.Length, 500));
        dataTMPText.text = dirtyLog;
	}

	private void StartTCPListenerThread(TcpListener listener)
	{
        
        Thread tcpListenerThread = new Thread(() =>
		{
            var bytes = new byte[1024];
			NetworkStream stream;
            while (true)
			{
				try
				{
					stream = client.GetStream();
                    stream.Read(bytes, 0, bytes.Length);
                    Process(bytes);
				}
				catch (Exception e)
				{
					Debug.Log(e.Message);
					return;
				}

				Thread.Sleep(10);
			}
		});
		tcpListenerThread.Start();
	}

	private void StartTCPSendingThread()
	{
		var tcpSendingThread = new Thread(() =>
		{
			while (true)
			{
				try
				{
                    Thread.Sleep(10);
					if (sendingQueue != null)
					{
						client.Client.Send(sendingQueue);
						dirtyLog += "Donnees envoyees de la sending queue ! ";
						sendingQueue = null;
					};
                }
				catch (Exception e)
				{
					Debug.Log(e.Message);
                    return;
                }
			}
		});
		tcpSendingThread.Start();
	}

	private void OnApplicationQuit()
	{
		tcpListener?.Stop();
	}

	public void Process(byte[] data)
	{
		byte[] dataPrefix = new byte[3] { data[4], data[5], data[6] };
		byte[] rovPrefix = new byte[3] {0x52, 0x4f, 0x56}; // ROV in ASCII
        if ( dataPrefix.SequenceEqual(rovPrefix))
		{
			int argument = -1;
			if (data[9] != 0xa) // if it's not a backslash, it's an argument
			{
                argument = (int)BitConverter.ToInt16(new byte[] { data[9], data[10] });
				Debug.Log("WITH ARGUMENT" + argument);
			}

			switch(data[8]) // 5th letter from the message
			{
				case 0x57: // W
					arduinoSimulator.RovFW(argument);
					break;

				case 0x54: // T
					arduinoSimulator.RovST();
					break;

				case 0x4c: // L
					arduinoSimulator.RovTL(argument);
					break;

				case 0x52: // R
					arduinoSimulator.RovTR(argument);
					break;

				case 0x50:
					if(argument >= 128)
					{
                        arduinoSimulator.RovSP(argument - 128);
                    }
                    break;

				default:
					Debug.Log("Interpretation echouée " + data);
					statusText = "Interpretation echouée";
					break;
			}
		}
		else
		{
			Debug.Log("Data received is not a movement command");
            statusText =
				"Pas une commande : " +
				BitConverter.ToString(dataPrefix) + " / " +
				BitConverter.ToString(rovPrefix);
        }

		print("processed");
	}

	public void SendLidarToClient(float[] lidar)
	{

		byte[] bytes = new byte[lidar.Length * 5 + 12];

		byte[] lidarSize = BitConverter.GetBytes((UInt32)lidar.Length);
		bytes[3] = lidarSize[0];
		bytes[2] = lidarSize[1];
		bytes[1] = lidarSize[2];
		bytes[0] = lidarSize[3];

		for(UInt16 i = 0; i < lidar.Length; i++)
		{
			byte[] angleBytes = BitConverter.GetBytes(i);
			bytes[(i * 2) + 4] = angleBytes[1];
			bytes[(i * 2) + 5] = angleBytes[0];
		}

        bytes[lidar.Length*2 + 4 + 3] = lidarSize[0];
        bytes[lidar.Length*2 + 4 + 2] = lidarSize[1];
        bytes[lidar.Length*2 + 4 + 1] = lidarSize[2];
        bytes[lidar.Length*2 + 4 + 0] = lidarSize[3];

        for (int i = 0; i < lidar.Length; i++)
		{
			byte[] convertedDistance = BitConverter.GetBytes((UInt16)(lidar[i] * 10));
			bytes[(lidar.Length*2) + (i * 2) + 8] = convertedDistance[1];
			bytes[(lidar.Length*2) + ((i * 2) + 9)] = convertedDistance[0];
		}

        bytes[lidar.Length * 4 + 8 + 3] = lidarSize[0];
        bytes[lidar.Length * 4 + 8 + 2] = lidarSize[1];
        bytes[lidar.Length * 4 + 8 + 1] = lidarSize[2];
        bytes[lidar.Length * 4 + 8 + 0] = lidarSize[3];

		for (int i = 0; i<lidar.Length; i++)
		{
			bytes[(lidar.Length * 4) + 12 + i] = 0;
		}

		byte[] header = BitConverter.GetBytes((lidar.Length * 5 + 12));
		Array.Reverse(header);

        byte[] msg = new byte[header.Length + bytes.Length];
        Buffer.BlockCopy(header, 0, msg, 0, header.Length);
        Buffer.BlockCopy(bytes, 0, msg, header.Length, bytes.Length);

        Debug.Log(bytes.Length);

        if (client == null)
        {
            return;
        }

        sendingQueue = msg;
	}

	public byte[] FormatMessageForSending(string message)
	{
        message = "0000" + message;
		byte[] header = BitConverter.GetBytes(message.Length);
		Array.Reverse(header);
        byte[] body = Encoding.UTF8.GetBytes(message);
		byte[] msg = new byte[header.Length + body.Length];
		Buffer.BlockCopy(header, 0, msg, 0, header.Length);
        Buffer.BlockCopy(body, 0, msg, header.Length, body.Length);

		return msg;
	}

    public void OnDestroy()
    {
        tcpListener?.Stop();
    }
}
