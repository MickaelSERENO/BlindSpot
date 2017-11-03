// Most of the code is based on a library for the Make Controller Kit1
// And the code provided by David Thery

using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;


/// <summary>
/// The Osc class provides the methods required to send, receive, and manipulate OSC messages.
/// Several of the helper methods are static since a running Osc instance is not required for 
/// their use.
/// 
/// When instanciated, the Osc class opens the PacketIO instance that's handed to it and 
/// begins to run a reader thread.  The instance is then ready to service Send OscMessage requests 
/// and to start supplying OscMessages as received back.
/// 
/// The Osc class can be called to Send either individual messages or collections of messages
/// in an Osc Bundle.  Receiving is done by delegate.  There are two ways: either submit a method
/// to receive all incoming messages or submit a method to handle only one particular address.
/// 
/// Messages can be encoded and decoded from Strings via the static methods on this class, or
/// can be hand assembled / disassembled since they're just a string (the address) and a list 
/// of other parameters in Object form. 
/// 
/// </summary>
public class OSCProtocol : MonoBehaviour 
{
    public static int BUFFER_LENGTH = 1024;

	public int m_inPort   = 6969;
	public string m_outIP = "127.0.0.1";
	public int m_outPort  = 6161;

    private UDPPacket         m_oscPacket;
    private Thread            m_readThread;
    private bool              m_readerRunning;
    private OscMessageHandler m_allMessageHandler;
    private Hashtable         m_addressTable      = new Hashtable();

    private ArrayList m_messageReceived = new ArrayList();
    private object    m_readThreadLock  = new object();

    private bool   m_paused = false;

    /// <summary> Handle the changes on the play mode </summary>
    void HandleOnPlayModeChanged()
    {
        m_paused = UnityEditor.EditorApplication.isPaused;
    }

    void Awake()
    {
        m_oscPacket               = new UDPPacket(m_outIP, m_outPort, m_inPort);

        //Init and launch the thread which will read the UDP Packet
        m_readThread              = new Thread(Read);
        m_readThread.IsBackground = true;
        m_readThread.Start();

		UnityEditor.EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
    }

    void OnDestroy()
    {
        Close();
    }

    /// <summary>
    /// Set the method to call back on when a message with the specified address is received.
    /// The method needs to have the OscMessageHandler signature - i.e. void amh(OscMessage oscM)
    /// </summary>
	/// <param name="key"> Address string to be matched </param>   
	/// <param name="handler"> The method to call back on </param>   
    public void SetAddressHandler(string key, OscMessageHandler handler)
    {
        ArrayList al = (ArrayList)Hashtable.Synchronized(m_addressTable)[key];

        //Create the address if not found
        if(al == null)
        {
            al = new ArrayList();
            al.Add(handler);
            Hashtable.Synchronized(m_addressTable).Add(key, al);
        }
        else
            al.Add(handler);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        m_paused = pauseStatus;
    }

	void Update() 
	{
        //If we received something
        if(m_messageReceived.Count > 0)
        {
            lock(m_readThreadLock)
            {
                //For all message received, called the MessageHandlers for this specific address
                foreach(OscMessage om in m_messageReceived)
                {
                    if(m_allMessageHandler != null)
                        m_allMessageHandler(om);
					ArrayList al = (ArrayList)Hashtable.Synchronized(m_addressTable)[om.Address];

                    if(al != null)
                        foreach(OscMessageHandler h in al)
                            h(om);
                }
                m_messageReceived.Clear();
            }
        }
	}

    /// <summary> Close the OSC object handler. Stop the running thread which reads the OSC packet over UDP and the UDP sockets </summary>
    public void Close()
    {
        //Stop the running Thread
        if(m_readerRunning)
        {
            m_readerRunning = false;
            m_readThread.Abort();
        }

        //Close the UDPPacket
        if(m_oscPacket != null && m_oscPacket.IsOpen())
            m_oscPacket.Close();
        m_oscPacket = null;
    }

	void Start() 
	{
		string s = "/test 10";
		Send(StringToOscMessage(s));
	}

    /// <summary>
    /// Read Thread. Loops waiting for packets. 
    /// When a packet is received, it is dispatched to any waiting All Message Handler in the main Thread via the method "Update". Also, the address is looked up and any matching handler is called.
    /// </summary>
    private void Read()
    {
        try
        {
            while(m_readerRunning)
            {
                byte[] buffer = m_oscPacket.ReceivePacket();
                if(buffer != null && buffer.Length > 0)
                {
                    lock(m_readThreadLock)
                    {
                        if(m_paused == false)
                        {
							ArrayList newMessages = OSCProtocol.PacketToOscMessages(buffer, buffer.Length);
							m_messageReceived.AddRange(newMessages);
                        }
                    }
                }
                else
                    Thread.Sleep(5);
            }
		}
        catch(Exception e)
        {
            Debug.LogWarning("ThreadAbortException " + e);
        }
    }

    /// <summary> Send an individual OSC message. Internally takes the OscMessage object and serializes it into a byte[] suitable for sending to the PacketIO </summary>
    /// <param name="message"> The OSC Message to send </param>
    public void Send(OscMessage message)
    {
        byte[] packet = new byte[BUFFER_LENGTH];
        int length    = OSCProtocol.OscMessageToPacket(message, packet, BUFFER_LENGTH);
        m_oscPacket.SendPacket(packet, length);
    }

    /// <summary> Sends a list of OSC Messages. Internally takes the OscMessage objects and serializes them into a byte[] suitable for sending to the PacketExchange </summary>
    /// <param name="oms"> The OSC Message to send </param>
    public void Send(ArrayList oms)
    {
		byte[] packet = new byte[BUFFER_LENGTH];
		int length = OscMessagesToPacket(oms, packet, BUFFER_LENGTH);
		m_oscPacket.SendPacket(packet, length);
	}
		
	/// <summary>
	/// Set the method to call back on when any message is received.
	/// The method needs to have the OscMessageHandler signature - i.e. void amh( OscMessage oscM )
	/// </summary>
	/// <param name="handler">The method to call back on.</param> 
	public void SetAllMessageHandler(OscMessageHandler handler)
    {
        m_allMessageHandler = handler;
    }

    /// <summary> Creates an OscMessage from a string - extracts the address and determines each of the values </summary>
    /// <param name="message"> The string to be turned into an OscMessage </param>
    /// <returns> The OscMessage </returns>
    public static OscMessage StringToOscMessage(string message)
    {
        OscMessage om  = new OscMessage();
        string[] ss    = message.Split(new char[]{' '});
        IEnumerator sE = ss.GetEnumerator();

        //Get the address
        if(sE.MoveNext())
            om.Address = (string)sE.Current; 

        //Get each values
        while(sE.MoveNext())
        {
            string s = (string)sE.Current;

            //If s starts with ", get all the string value between " and " - "string"
            if(s.StartsWith("\""))
            {
                //The string builder to save the string value
                StringBuilder quoted = new StringBuilder();
                bool looped = false;
                if(s.Length > 1)
                    quoted.Append(s.Substring(1));
                else
                    looped = true;

                while(sE.MoveNext())
                {
                    string a = (string)sE.Current;
                    //Append a backspace if something was added before
                    if(looped)
                        quoted.Append(" ");

                    //Fetch back the end of the string without the \" char
                    if(a.EndsWith("\""))
                    {
                        quoted.Append(a.Substring(0, a.Length-1));
                        break;
                    }
                    else if(a.Length == 0)
                        quoted.Append(" ");
                    else
                        quoted.Append(a);

                    looped = true;
                }
                om.Values.Add(quoted.ToString());
            }

            //We are dealing with another type of value
            else
            {
                if(s.Length > 0)
                {
                    //Integer
                    try
                    {
                        int i = int.Parse(s);
                        om.Values.Add(i);
                    }

                    catch
                    {
                        //Float
                        try
                        {
                            float f = float.Parse(s);
                            om.Values.Add(f);
                        }

                        //Or StringOk, je me demandais Ok, je me demandais Ok, je me demandais Ok, je me demandais Ok, je me demandais 
                        catch
                        {
                            om.Values.Add(s);
                        }
                    }
                }
            }
        }

        return om;
    }

    /// <summary> Puts an array of OscMessages into a packet (byte[]) </summary>
    /// <param name="messages"> An ArrayList of OscMessages </param>
    /// <param name="packet">   An array of bytes to be populated with the OscMessages </param>
    /// <param name="length">   The length available in the array "packet" </param>
    /// <returns>               The length of the packet </returns>
    public static int OscMessagesToPacket(ArrayList messages, byte[] packet, int length)
    {
        int index = 0;
        if(messages.Count == 1)
            index = OscMessageToPacket((OscMessage)messages[0], packet, 0, length);
        else
        {
            //Write the first bundle bit
            index = InsertString("#bundle", packet, index, length);

            //Write a null timestamp (another 8bytes)
            int c = 8;
            while((c--) > 0)
                packet[index++]++;

            //Now put each message preceded by its length
            foreach(OscMessage msg in messages)
            {
                //Save the index
                int lengthIndex = index;
                index += 4;
                int packetStart = index;

                //Save the content of this OscMessage
                index = OscMessageToPacket(msg, packet, index, length);

                //put the size (4 bytes)
                int packetSize = index - packetStart;
                packet[lengthIndex++] = (byte)((packetSize >> 24) & 0xFF);
                packet[lengthIndex++] = (byte)((packetSize >> 16) & 0xFF);
                packet[lengthIndex++] = (byte)((packetSize >> 8) & 0xFF);
                packet[lengthIndex++] = (byte)((packetSize) & 0xFF);
            }
        }
		return index;
    }

    /// <summary> Creates a packet (an array of bytes) from a single OscMessage </summary>
    /// <param name="message"> The OscMessage to be returned as a packet </param>
    /// <param name="packet">The packet to be populated with the OscMessage.</param>
    /// <param name="length">The usable size of the array of bytes.</param>
    /// <returns>The length of the packet</returns>
    public static int OscMessageToPacket(OscMessage message, byte[] packet, int length)
    {
        return OscMessageToPacket(message, packet, 0, length);
    }

    /// <summary> Creates a packet (an array of bytes) from a single OscMessage </summary>
    /// <remarks>Can specify where in the array of bytes the OscMessage should be put.</remarks>
    /// <param name="message"> The OscMessage to be returned as a packet </param>
    /// <param name="packet">The packet to be populated with the OscMessage.</param>
    /// <param name="start">The start index in the packet where the OscMessage should be put.</param>
    /// <param name="length">The usable size of the array of bytes.</param>
    /// <returns>The length of the packet</returns>
    public static int OscMessageToPacket(OscMessage message, byte[] packet, int start, int length)
    {
        int index = start;

        //Insert at first the address of this message
        index = InsertString(message.Address, packet, index, length);

        //Init the tag
        StringBuilder tag = new StringBuilder();
        tag.Append(",");
        int tagIndex = index;
        index += PadSize(2+message.Values.Count);

        //Get and insert all the objects
        foreach(object o in message.Values)
        {
            //if o is an integer
            if(o is int)
            {
                int i = (int)o;
                tag.Append("i");
                packet[index++] = (byte)((i >> 24) & 0xFF);
                packet[index++] = (byte)((i >> 16) & 0xFF);
                packet[index++] = (byte)((i >> 8) & 0xFF);
                packet[index++] = (byte)((i) & 0xFF);
            }

            //if o is a float
            else if(o is float)
            {
                float f = (float)o;
                tag.Append("f");
                byte[] buffer = new byte[4];
                MemoryStream ms = new MemoryStream(buffer);
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(f);
                packet[index++] = buffer[3];
                packet[index++] = buffer[2];
                packet[index++] = buffer[1];
                packet[index++] = buffer[0];
            }

            //if o is a string
            else if(o is string)
            {
                tag.Append("s");
                index = InsertString(o.ToString(), packet, index, length);
            }

            //Else, the object is unknown
            else
                tag.Append("?");

            //Insert the tag before adding the object
            InsertString(tag.ToString(), packet, tagIndex, length);
        }

        return index;
    }

    /// <summary> Takes a packet (byte[]) and turns it into a list of OscMessages </summary>
    /// <param name="packet"> The packet to be parsed </param>
    /// <param name="length"> The length of the packet </param>
    /// <returns> An ArrayList of OscMessages </returns>
    public static ArrayList PacketToOscMessages(byte[] packet, int length)
    {
        ArrayList messages = new ArrayList();
        ExtractMessages(messages, packet, 0, length);
        return messages;
    }

    /// <summary>  Extracts a messages from a packet </summary>
    /// <param name="messages">An ArrayList to be populated with the OscMessage.</param>
    /// <param name="packet">The packet of bytes to be parsed.</param>
    /// <param name="start">The index of where to start looking in the packet.</param>
    /// <param name="length">The length of the packet.</param>
    /// <returns>The index after the OscMessage is read.</returns>
    public static int ExtractMessages(ArrayList messages, byte[] packet, int start, int length)
    {
        OscMessage oscM = new OscMessage();

        //Get the address
        oscM.Address = ExtractString(packet, start, length);
        int index = start + PadSize(oscM.Address.Length + 1);

        //Get the tag (type) of the objects
        string typeTag = ExtractString(packet, index, length);

        //Fetch back each object
        foreach(char c in typeTag)
        {
            switch(c)
            {
                //Nothing
                case ',':
                    break;

                //A string
                case 's':
                {
                    string s = ExtractString(packet, index, length);
                    index += PadSize(s.Length+1);
                    oscM.Values.Add(s);
					break;
                }

                //An integer
                case 'i':
                {
                    int i = ( packet[index++] << 24 ) + ( packet[index++] << 16 ) + ( packet[index++] << 8 ) + packet[index++];
                    oscM.Values.Add(i);
                    break;
                }

                //A float
                case 'f':
                {
                    byte[] buffer = new byte[4];
                    buffer[3] = packet[index++];
                    buffer[2] = packet[index++];
                    buffer[1] = packet[index++];
                    buffer[0] = packet[index++];
                    MemoryStream ms = new MemoryStream(buffer);
                    BinaryReader br = new BinaryReader(ms);
                    float f = br.ReadSingle();
                    oscM.Values.Add(f);
                    break;
                }
            }
        }

		return index;
    }

    /// <summary> Removes a string from a packet. </summary>
    /// <param name="packet"> The packet of bytes to be parsed </param>
    /// <param name="start">  The index of where to start looking in the packet </param>
    /// <param name="length"> The length of the packet </param>
    /// <returns> The string parsed </returns>
    public static string ExtractString(byte[] packet, int start, int length)
    {
        StringBuilder sb = new StringBuilder();
        int index = start;
        while(packet[index] != 0 && index < length)
            sb.Append((char)packet[index++]);
        return sb.ToString();
    }

    /// <summary> Removes the content of the array of bytes starting by "start". </summary>
    /// <param name="packet"> The packet of bytes to be parsed </param>
    /// <param name="start">  The index of where to start looking in the packet </param>
    /// <param name="length"> The length of the packet </param>
    /// <returns> The Dum string : value|value|value </returns>
    public static string Dump(byte[] packet, int start, int length)
    {
        StringBuilder sb = new StringBuilder();
        int index = start;
        while(index < length)
            sb.Append(packet[index++]+"|");
		return sb.ToString();
    }

    /// <summary> Inserts a string, correctly padded into a packet </summary>
    /// <param name="string">The string to be inserted</param>
    /// <param name="packet">The packet of bytes to be parsed.</param>
    /// <param name="start">The index of where to start looking in the packet.</param>
    /// <param name="length">The length of the packet.</param>
    /// <returns>An index to the next byte in the packet after the padded string.</returns>
    private static int InsertString(string s, byte[] packet, int start, int length)
    {
        //Insert the string into the packet
        int index = start;
        foreach(char c in s)
        {
            packet[index++] = (byte)c;
            if(index == length)
                return index;
        }

        //Do not forget the "\0" character
        packet[index++] = 0;

        //Insert the padding
        int pad = (s.Length+1)%4;
        if(pad != 0)
        {
            pad = 4-pad;
            while(pad -- > 0)
                packet[index++] = 0;
        }
        return index;
    }

    /// <summary> Takes a length and returns what it would be if padded to the nearest 4 bytes </summary>
    /// <param name="rawSize">Original size</param>
    /// <returns>padded size</returns>
    public static int PadSize(int rawSize)
    {
        int pad = rawSize % 4;
        if (pad == 0)
            return rawSize;
        else
            return rawSize + (4 - pad);
    }
}
