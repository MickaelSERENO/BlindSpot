using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;


/// <summary> UdpPacket provides packetIO over UDP </summary>
public class UDPPacket
{
	private UdpClient m_sender;
	private UdpClient m_receiver;
	private bool      m_socketOpen;
	private string    m_remoteHostName;
	private int       m_remotePort;
	private int       m_localPort;

	/// <summary> Constructor, inits the UDPPacket but does not open it </summary>
	/// <param name="hostIP"> The ip to send packets </param>
	/// <param name="remotePort"> The remote port to send packets </param>
	/// <param name="localPort"> The local port of the client </param>
	public UDPPacket(string hostIP, int remotePort, int localPort)
	{
		m_remoteHostName = hostIP;
		m_remotePort     = remotePort;
		m_localPort      = localPort;
		m_socketOpen     = false;
	}


	/// <summary> Destructor. Close the socket at destruction </summary>
	~UDPPacket()
	{
		if(IsOpen())
			Close();
	}

	/// <summary> Open the UDP socket and create an UDP sender </summary>
	/// <returns> True on success, false on failure </returns>
	public bool Open()
	{
		try
		{
			m_sender = new UdpClient();
			IPEndPoint listenerIp = new IPEndPoint(IPAddress.Any, m_localPort);
			m_receiver = new UdpClient(listenerIp);

			m_socketOpen = true;
			return true;
		}
		catch(Exception e)
		{
			Debug.LogWarning("Cannot open UdpClient interface at port"+m_localPort);
			Debug.LogWarning(e);
		}
		return false;
	}

	/// <summary> Close the socket currently listening, and destroy the UDP sender device </summary>
	public void Close()
	{
		//Close the UDPClient
		if(m_sender != null)
			m_sender.Close();
		if(m_receiver != null)
			m_receiver.Close();

		//Reinit variables
		m_receiver = m_sender = null;
		m_socketOpen          = false;
	}

	/// <summary> Close the socket </summary>
	public void OnDisable()
	{
		Close();
	}

	/// <summary> Tell wheter or not the UdpClient is opened </summary>
	/// <returns> True if the socket is opened, false otherwise </returns>
	public bool IsOpen()
	{
		return m_socketOpen;
	}

	/// <summary> Send a packet of bytes out via UDP </summary>
	/// <param name="packet"> The packet of bytes to send </param>
	/// <param name="length"> The length of "packet" </param>
	public void SendPacket(byte[] packet, int length)
	{
		//Try to open the UdpClient if not already opened.
		if(!IsOpen())
			Open();

		//If the UdpClient is still closed, return.
		if(!IsOpen())
			return;

		m_sender.Send(packet, length, m_remoteHostName, m_remotePort);
	}

	/// <summary> Receive a packet of bytes over UDP </summary>
	/// <param name="buffer"> The buffer to save the packet </param>
	/// <returns> The packet read. null on failure </returns>
	public byte[] ReceivePacket()
	{
		//Try to open the UdpClient if not already opened.
		if(!IsOpen())
			Open();

		//If the UdpClient is still closed, return.
		if(!IsOpen())
			return null;

		//Read the incoming data
		IPEndPoint iep = new IPEndPoint(IPAddress.Any, m_localPort);
		byte[] incoming = m_receiver.Receive(ref iep);

		return incoming;
	}

	/// <summary> The address of the board that you are sending to </summary>
	public string RemoteHostName
	{
		get
		{
			return m_remoteHostName;
		}
		set
		{
			m_remoteHostName = value;
		}
	}

	/// <summary> The remote port you are sending to </summary>
	public int RemotePort
	{
		get
		{
			return m_remotePort;
		}
		set
		{
			m_remotePort = value;
		}
	}

	public int LocalPort
	{
		get
		{
			return m_localPort;
		}
		set
		{
			m_localPort = value;
			Close();
			Open();
		}
	}
}
