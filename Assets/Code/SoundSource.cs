using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundSource : MonoBehaviour 
{
	/// <summary> Tell if the sound source has to be lighted or not </summary>
	private bool m_lighted = false;

	/// <summary> Use this for initialization </summary> 
	void Start()
	{
		SetLighted(false);
	}
	
	/// <summary> Update is called once per frame </summary> 
	void Update ()
	{
		
	}

	/// <summary> Raises the trigger enter event. </summary>
	/// <param name="coll">The collision value.</param>
	void OnTriggerEnter(Collider coll)
	{
		SetLighted(true);
	}

	/// <summary> Set the statut of the light associated to the sound source </summary>
	/// <param name="lighted">If set to <c>true</c>, the light is turned on. Otherwise, the light is turned off</param>
	public void SetLighted(bool lighted)
	{
		m_lighted = lighted;
		GetComponent<Light>().enabled = lighted;
	}

	/// <summary> Gets or sets a value indicating whether this <see cref="SoundSource"/> is lighted. </summary>
	/// <value><c>true</c> if lighted; otherwise, <c>false</c>.</value>
	public bool Lighted
	{
		get
		{
			return m_lighted;
		}
		set
		{
			SetLighted(value);
		}
	}
}