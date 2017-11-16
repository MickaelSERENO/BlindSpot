using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static float SPEED = 0.1f;
    public static float ROT_SPEED_KEYBOARD = 1.0f;
    public static float ROT_SPEED_MOUSE = 5 * ROT_SPEED_KEYBOARD;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private CursorLockMode wantedMode;

    /// <summary> An array of PDData. Will contain one value per sound source </summary>
    private List<PDData> m_datas = new List<PDData>();
    /// <summary> Use this for initialization </summary> 
    void Start()
    {
        //Send the number of spots
        OSCMessage msg = new OSCMessage();
        msg.Address = "/NbSpots";
        int nb = 0;
        foreach (var o in GameObject.FindGameObjectsWithTag("BlindSpot"))
        {
            nb++;
        }
        msg.Values.Add(nb);

        GameObject scene = GameObject.FindGameObjectWithTag("Scene");
        OSCProtocol osc = scene.GetComponent<OSCProtocol>();
        osc.Send(msg);
    }

    void OnCollisionEnter(Collision coll)
    {
        //Maybe look if there is a wall, and make a sound
    }

    /// <summary> Update is called once per frame </summary> 
    void Update()
    {
        HandleKeyboard();
        HandleMouse();
        UpdateAngles();
        SendOSC();
    }

    //Modifies camera view according to mouse moves
    private void HandleMouse()
    {
        yaw += ROT_SPEED_MOUSE * Input.GetAxis("Mouse X");
        pitch -= ROT_SPEED_MOUSE * Input.GetAxis("Mouse Y");

        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        // Release cursor on escape keypress
        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = wantedMode = CursorLockMode.None;

        switch (Cursor.lockState)
        {
            case CursorLockMode.None:
                GUILayout.Label("Cursor is normal");
                if (GUILayout.Button("Lock cursor"))
                    wantedMode = CursorLockMode.Locked;
                break;
            case CursorLockMode.Locked:
                GUILayout.Label("Cursor is locked -" + Environment.NewLine + "Press Esc to release it");
                if (GUILayout.Button("Unlock cursor"))
                    wantedMode = CursorLockMode.None;
                break;
        }

        GUILayout.EndVertical();

        SetCursorState();
    }


    // Apply requested cursor state
    void SetCursorState()
    {
        Cursor.lockState = wantedMode;
        // Hide cursor when locking
        Cursor.visible = (CursorLockMode.Locked != wantedMode);
    }

    /// <summary> Handles the keyboard </summary>
    private void HandleKeyboard()
    {
        //Get the direction entered
        int horizontal = 0;
        int vertical = 0;

        if (Input.GetKey(KeyCode.UpArrow))
            vertical += 1;
        if (Input.GetKey(KeyCode.DownArrow))
            vertical -= 1;
        if (Input.GetKey(KeyCode.LeftArrow))
            horizontal -= 1;
        if (Input.GetKey(KeyCode.RightArrow))
            horizontal += 1;

        //Then move the sphere
        Vector3 moveBy = transform.rotation * new Vector3(horizontal, 0, vertical);
        moveBy.y = 0;
        moveBy.Normalize();
        moveBy *= SPEED;
        gameObject.transform.position += moveBy;

        //Rotate the object
        if (Input.GetKey(KeyCode.T)) //Top
            transform.localRotation = transform.localRotation * Quaternion.Euler(-ROT_SPEED_KEYBOARD, 0, 0);
        if (Input.GetKey(KeyCode.B)) //Bot
            transform.localRotation = transform.localRotation * Quaternion.Euler(+ROT_SPEED_KEYBOARD, 0, 0);

        if (Input.GetKey(KeyCode.L)) //Left
            transform.localEulerAngles = transform.localEulerAngles + new Vector3(0, -ROT_SPEED_KEYBOARD, 0);
        if (Input.GetKey(KeyCode.R)) //Right
            transform.localEulerAngles = transform.localEulerAngles + new Vector3(0, +ROT_SPEED_KEYBOARD, 0);
    }

    /// <summary> Update azimuths and altitudes </summary> 
    private void UpdateAngles()
    {
        //Clear all data (some light source may have disappeared)
        m_datas.Clear();

        //Get all sound sources
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("BlindSpot"))
        {
            SoundSource ss = obj.gameObject.GetComponent<SoundSource>();
            Debug.Log("Sound Ss");

            //Compute and push azimuth + altitude for every sound source
            PDData data = ComputeAngle(obj);
            if (!ss.Lighted)
                data.Volume = 100;
            else
                data.Volume = 0;
            m_datas.Add(data);

        }
    }

    /// <summary> Computes the angle between this game object and the GameObject passed in parameters </summary>
    /// <returns>The angles.</returns>
    /// <param name="obj">The GameObject for which the angle is computed.</param>
    private PDData ComputeAngle(GameObject obj)
    {
        PDData data = new PDData();

        data.Amplitude = Vector3.Distance(transform.position, obj.transform.position); //Compute the distance between the source and the player
        Vector3 p = Quaternion.Inverse(transform.rotation) * (obj.transform.position - transform.position); //Compute q' * v * q to get the relative orientation
        data.Azimuth = 180 / Mathf.PI * Mathf.Atan2(p.x, -p.z) + 180;
        data.Altitude = 180 / Mathf.PI * Mathf.Asin(p.y / Mathf.Sqrt(p.x * p.x + p.y * p.y + p.z * p.z));

        Debug.Log("Azimuth : " + data.Azimuth + " Altitude : " + data.Altitude);
        return data;
    }

    /// <summary> Send data to PureData </summary> 
    private void SendOSC()
    {
        GameObject scene = GameObject.FindGameObjectWithTag("Scene");
        OSCProtocol osc = scene.GetComponent<OSCProtocol>();

        //For each spot, send the datas
        int i = 1;
        foreach (PDData data in m_datas)
        {
            OSCMessage pdMsg = new OSCMessage();
            pdMsg.Address = "/Position";
            pdMsg.Values.Add(i++);
            pdMsg.Values.Add(data.Volume);
            pdMsg.Values.Add(data.Amplitude);
            pdMsg.Values.Add(data.Azimuth);
            pdMsg.Values.Add(data.Altitude);
            osc.Send(pdMsg);
        }
    }
}