using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour 
{
	public static float SPEED = 0.1f;

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
	}

	/// <summary> Update azimuths and altitudes </summary> 
	private void UpdateAngles()
	{
		//Clear all data (some light source may have disappeared)
		m_datas.Clear();

		//Get all sound sources
		foreach(Transform trans in GameObject.Find("SoundSource").transform)
		{
			SoundSource ss = trans.gameObject.GetComponent<SoundSource>();

			//Compute and push azimuth + altitude for every non-lighten sound source
			if(!ss.Lighted)
			{
				PDData data = ComputeAngle(ss.gameObject);
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
		return data;
	}

	/// <summary> Send data to PureData </summary> 
	private void SendOSC()
	{
	}
}