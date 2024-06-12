using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using System.IO;
using System.IO.Ports;

public class HapticControl : MonoBehaviour
{
    Thread thread;
    bool isStopped = false;
    public SerialPort comPort;
    private bool portOpen=false;
    private byte[] data = new byte[] { 0x41, 0x41, 0x73, 0x73, 0x56, 0x01, 0x80, 0x78, 0x78, 0x45, 0x45 };
    public string msg;
    public bool msgReceived = false;
    private ExperimentManager experimentManager;

    // Start is called before the first frame update
    void Start()
    {   
        experimentManager = GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>();

        // Setup serial communication and call program 
        portOpen = SetupSerial(experimentManager.ArduinoPort);
        if(portOpen){
            comPort.Write(data, 0, data.Length);
        }

        // Receive messages on a separate thread so Unity doesn't freeze waiting for data
        ThreadStart ts = new ThreadStart(Arduino);
        thread = new Thread(ts);
        thread.Start();
    }

    public void Arduino()
    {

        while(!isStopped){
            try
            {
                msg = comPort.ReadLine();
                msgReceived = true;
            }
            catch (TimeoutException) { }
        }
    }

    private bool SetupSerial(string port)
    {
        bool ready = true;
        if (port.Length > 0)
        {
            try
            {
                comPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
            }
            catch (IOException e)
            {
                Debug.LogError(e.ToString() + " Could not open port " + port);
                ready = false;
            }
            comPort.Handshake = Handshake.None;
            comPort.RtsEnable = true;//false;

            comPort.ReadTimeout = 100;
            comPort.WriteTimeout = 50;
            //comPort.ReadBufferSize = 65536;

            try
            {
                comPort.DtrEnable = true;
                comPort.RtsEnable = true;
                comPort.Open();
                Debug.Log("Setup completed!");
            }
            catch (IOException e)
            {
                Debug.LogError(e.ToString() + " Could not open port " + port);
                ready = false;
            }
        }
        else
        {
            Debug.LogError("COM port not correctly defined.");
            ready = false;
        }
        return ready;
    }

    private void OnDestroy()  
    {  
        thread.Abort(); 
        isStopped = true;  
    }
    private void OnApplicationQuit()  
    {  
        thread.Abort(); 
        isStopped = true;  
    }
}

