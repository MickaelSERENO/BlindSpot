﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour 
{
	public static float SPEED     = 0.1f;
	public static float ROT_SPEED = 1.0f;

	/// <summary> An array of PDData. Will contain one value per sound source </summary>
	private List<PDData> m_datas;

	/// <summary> Use this for initialization </summary> 
	void Start() 
	{
		m_datas = new List<PDData>();
	}

	void OnCollisionEnter(Collision coll)
	{
		//Maybe look if there is a wall, and make a sound
	}
	
	/// <summary> Update is called once per frame </summary> 
	void Update() 
	{
		HandleKeyboard();
		UpdateAngles();
		SendOSC();
	}

	/// <summary> Handles the keyboard </summary>
	private void HandleKeyboard()
	{
		//Get the direction entered
		int horizontal = 0;
		int vertical   = 0;

		if(Input.GetKey(KeyCode.UpArrow))
			vertical   += 1;
		if(Input.GetKey(KeyCode.DownArrow))
			vertical   -= 1;
		if(Input.GetKey(KeyCode.LeftArrow))
			horizontal -= 1;
		if(Input.GetKey(KeyCode.RightArrow))
			horizontal += 1;

		//Then move the sphere
		Vector3 moveBy = new Vector3(horizontal, 0, vertical);
		moveBy.Normalize();
		moveBy *= SPEED;
		gameObject.transform.position += moveBy;

		//Rotate the object
		if (Input.GetKey(KeyCode.T)) //Top
			transform.Rotate(-ROT_SPEED, 0, 0, 0);	
		if (Input.GetKey(KeyCode.B)) //Bot
			transform.Rotate(+ROT_SPEED, 0, 0, 0);	

		if (Input.GetKey(KeyCode.L)) //Left
			transform.Rotate(0, -ROT_SPEED, 0, 0);	
		if (Input.GetKey(KeyCode.R)) //Right
			transform.Rotate(0, +ROT_SPEED, 0, 0);	
	}

	/// <summary> Update azimuths and altitudes </summary> 
	private void UpdateAngles()
	{
		//Clear all data (some light source may have disappeared)
		m_datas.Clear();

		//Get all sound sources
		foreach(GameObject obj in GameObject.FindGameObjectsWithTag("BlindSpot"))
		{
			SoundSource ss = obj.gameObject.GetComponent<SoundSource>();
			Debug.Log("Sound Ss");
			//Compute and push azimuth + altitude for every non-lighten sound source
			if(!ss.Lighted)
			{
				PDData data = ComputeAngle(obj);
				m_datas.Add(data);
			}
		}
	}

	/// <summary> Computes the angle between this game object and the GameObject passed in parameters </summary>
	/// <returns>The angles.</returns>
	/// <param name="obj">The GameObject for which the angle is computed.</param>
	private PDData ComputeAngle(GameObject obj)
	{
		PDData data = new PDData();

		data.Amplitude = Vector3.Distance(transform.position, obj.transform.position); //Compute the distance between the source and the player
		Vector3 p      = Quaternion.Inverse(transform.rotation) * (obj.transform.position - transform.position); //Compute q' * v * q to get the relative orientation
		data.Azimuth   = 180 / Mathf.PI * Mathf.Atan2(p.x, -p.z) + 180;
		data.Altitude  = 180 / Mathf.PI * Mathf.Asin(p.y/Mathf.Sqrt(p.x*p.x + p.y * p.y + p.z * p.z));
	
		Debug.Log("Azimuth : " + data.Azimuth + " Altitude : " + data.Altitude); 
		return data;
	}

	/// <summary> Send data to PureData </summary> 
	private void SendOSC()
	{
		//Send the number of spots
		OSCMessage msg = new OSCMessage();
		msg.Address    = "/NbSpots";
		msg.Values.Add(m_datas.Count);

		GameObject scene = GameObject.FindGameObjectWithTag("Scene");
		OSCProtocol osc  = scene.GetComponent<OSCProtocol>();
		osc.Send(msg);
	
		//For each spot, send the datas
		int i=1;
		foreach(PDData data in m_datas)
		{
			OSCMessage pdMsg = new OSCMessage();
			pdMsg.Address = "/Position";
			pdMsg.Values.Add(i++);
			pdMsg.Values.Add(data.Amplitude);
			pdMsg.Values.Add(data.Azimuth);			
			pdMsg.Values.Add(data.Altitude);
			osc.Send(pdMsg);
		}
	}
}