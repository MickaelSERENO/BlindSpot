using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;

/// <summary> The OscMessage class is a data structure that represents an OSC address and an arbitrary number of values to be sent to that address </summary>
public class OscMessage
{
	private string    m_address;
	private ArrayList m_values;

	/// <summary> Default constructor. Init the object </summary>
	public OscMessage()
	{
		m_values = new ArrayList();
	}

	/// <summary> Convert the object to a string. The syntax is "address" followed by the values separated by backspace </summary>
	/// <returns> The Object as a String </returns>
	public override string ToString()
	{
		StringBuilder s = new StringBuilder();
		s.Append(m_address);
		foreach(object o in m_values)
		{
			s.Append(" ");
			s.Append(o.ToString());
		}

		return s.ToString();
	}

	/// <summary> Get the value at index "index" from the array of values as a int </summary>
	/// <param name="index"> The index to look at in the array of values </param>
	/// <returns> The value as a int. Returns 0 on error </returns>
	public int GetInt(int index)
	{
		if(m_values[index].GetType() == typeof(int) || m_values[index].GetType() == typeof(float))
		{
			int data = (int)m_values[index];
			if(Double.IsNaN(data))
				return 0;
			return data;
		}

		Debug.LogWarning("Wrong type at index " + index);
		return 0;
	}

	/// <summary> Get the value at index "index" from the array of values as a float </summary>
	/// <param name="index"> The index to look at in the array of values </param>
	/// <returns> The value as a float. Returns 0 on error </returns>
	public float GetFloat(int index)
	{
		if(m_values[index].GetType() == typeof(int) || m_values[index].GetType() == typeof(float))
		{
			float data = (float)m_values[index];
			if(Double.IsNaN(data))
				return 0f;
			return data;
		}

		Debug.LogWarning("Wrong type at index " + index);
		return 0f;
	}

	public String Address
	{
		get
		{
			return m_address;
		}

		set
		{
			m_address = value;
		}
	}

	public ArrayList Values
	{
		get
		{
			return m_values;
		}

		set
		{
			m_values = value;
		}
	}
}

public delegate void OscMessageHandler(OscMessage oscM);
