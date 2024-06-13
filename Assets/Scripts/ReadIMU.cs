// Developed by: Lucas Cardoso
// First version: 03/May/2024
// Latest release: 03/May/2024
// Description: This script need to be attached to the GameObject with the hand model (right or left). The same GameObject also needs to have a Animator component attached (with a proper Animation created). 
//              This script updates the "Blend" variable that controls the animation, so the pinch opens or closes. 
//              The method through which this variable is going to be updated (control method) is defined by the experimenter, choosing between "Shoulder" or "Finger" in a list shown in the Experiment Manager GameObject.
//              If "Finger" is selected (it means that finger tracking is going to be used), the script will connect to the "OnUpdatedHands" function to track the thumb and index finger tips, estimate the distance between them and update the "Blend" according to this distance.
//              If "Shoulder" is selected (it means that the elevation and depression of the shoulder will be used), the script will follow the steps below:
//                (i)   Connect to the Delsys base;
//                (ii)  Read IMU from two sensors; and
//                (iii) Calculate the position of the shoulder (elevation) based on the Kalman filter (using the matrices of the system identification built by the Matlab script "calibration.m").
//              This part of the algorithm is based on the previous version "AnimateHandOnImput".
//              All matrix manipulations use the code from the following link: https://visualstudiomagazine.com/Articles/2020/04/06/invert-matrix.aspx?Page=1
//              The most updated program with the matrices manipulation is in the folder "Mult_matrixes_rev1".

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;

public class ReadIMU : MonoBehaviour
{
    string response = "";
    private List<int> sensors = new List<int>();
    //The following are used for TCP/IP connections
    private TcpClient commandSocket = default!;
    private TcpClient accSocket = default!;
    private const int commandPort = 50040;  //server command port
    private const int accPort = 50044;  //port for ACC data

    //The following are streams and readers/writers for communication
    private NetworkStream commandStream = default!;
    private NetworkStream accStream = default!;
    private StreamReader commandReader = default!;
    private StreamWriter commandWriter = default!;

    //The following are streams and readers/writers for communication
    private List<float>[] ax = new List<float>[16];
    private List<float>[] ay = new List<float>[16];
    private List<float>[] az = new List<float>[16];
    private List<float>[] gx = new List<float>[16];
    private List<float>[] gy = new List<float>[16];
    private List<float>[] gz = new List<float>[16];


    private bool connected = false; //true if connected to server
    private bool running = false;   //true when acquiring data

    //Server commands
    private const string COMMAND_START = "START";
    private const string COMMAND_STOP = "STOP";

    //Threads for acquiring emg and acc data
    private Thread accThread = default!;

    float alpha = 0.65f; // Complementary filter blending factor -> experimentally, it should be between 0.65 and 0.75
    float pitch;
    float roll;

    void Start()
    {
        //call function to do the set up with the Delsys base
        setupDelsys();
    }

    void FixedUpdate()
    {
        int sensorIndex = sensors[0] - 1; // Assuming we're using the first detected sensor
        // Get the latest imu data
        float accx = ax[sensorIndex][ax[sensorIndex].Count - 1];
        float accy = ay[sensorIndex][ay[sensorIndex].Count - 1];
        float accz = az[sensorIndex][az[sensorIndex].Count - 1];
        float gyrx = gx[sensorIndex][gx[sensorIndex].Count - 1];
        float gyry = gy[sensorIndex][gy[sensorIndex].Count - 1];
        float gyrz = gz[sensorIndex][gz[sensorIndex].Count - 1];

        // Sample time
        float dt = Time.deltaTime;

        // Calculate pitch and roll from accelerometer
        float acc_pitch = Mathf.Atan2(-accx, Mathf.Sqrt(accy * accy + accz * accz)) * Mathf.Rad2Deg;
        float acc_roll = Mathf.Atan2(accy, accz) * Mathf.Rad2Deg;
        
        // Apply complementary filter for pitch and roll
        pitch = alpha * (pitch + gyry * dt) + (1 - alpha) * acc_pitch;
        roll = alpha * (roll + gyrx * dt) + (1 - alpha) * acc_roll;

        // Update cube rotation (optional)
        transform.rotation = Quaternion.Euler(pitch, 0.0f, roll);
        
    }
    


    private void setupDelsys() 
    {
        Debug.Log("Delsys setup is running...");
        try
        {
            //Establish TCP/IP connection to server using URL entered
            commandSocket = new TcpClient("localhost", commandPort);
                
            //Set up communication streams
            commandStream = commandSocket.GetStream();
            commandReader = new StreamReader(commandStream, Encoding.ASCII);
            commandWriter = new StreamWriter(commandStream, Encoding.ASCII);
            Debug.Log(commandReader.ReadLine());
            commandReader.ReadLine();   //get extra line terminator
            connected = true;
        }
        catch (Exception)
        {
            //connection failed, display error message
            Debug.LogError("Could not connect.");
            return;
        }

        string command = "UPSAMPLE OFF";
        response = SendCommand(command);
        Debug.Log("COMMAND: " + command);
        Debug.Log("RESPONSE: " + response);

        for (int i = 0; i < 16; i++)
        {
            command = "SENSOR " + (i+1) + " ACTIVE?";
            response = SendCommand(command);
            if(response == "YES")
            {
                sensors.Add(i+1);
                Debug.Log("SENSOR " + (i+1) + " DETECTED");
                command = "SENSOR " + (i+1) + " SETMODE 173";
                response = SendCommand(command);
                Debug.Log(response);
            }
        }

        for (int i = 0; i < 16; i++)
        {
            ax[i] = new List<float>();
            ay[i] = new List<float>();
            az[i] = new List<float>();
            gx[i] = new List<float>();
            gy[i] = new List<float>();
            gz[i] = new List<float>();
        }

        //Establish data connections and creat streams
        accSocket = new TcpClient("localhost", accPort);
        accStream = accSocket.GetStream();

        //Create data acquisition threads
        accThread = new Thread(accWorker);
        accThread.IsBackground = true;

        //Indicate we are running and start up the acquisition threads
        running = true;
        accThread.Start();

        //Send start command to server to stream data
        response = SendCommand(COMMAND_START);
        Debug.Log("COMMAND: " + COMMAND_START);
        Debug.Log("RESPONSE: " + response);   

        Thread.Sleep(1000); //wait 1s to ensure the buffer is full enough

        Debug.Log("Delsys is ready to be use!");
    }

    void OnDestroy()
    {
        response = SendCommand(COMMAND_STOP);
        Debug.Log("COMMAND: " + COMMAND_STOP);
        Debug.Log("RESPONSE: " + response);
        commandSocket.Close();
    } 

    //Send a command to the server and gets the response
    string SendCommand(string command)
    {
        string response = "";
        //Check if connected
        if (connected)
        {
            //Send the command
            commandWriter.WriteLine(command);
            commandWriter.WriteLine();  //terminate command
            commandWriter.Flush();  //make sure command is sent immediately

            //Read the response line and display    
            response = commandReader.ReadLine();
            commandReader.ReadLine();   //get extra line terminator
        }
        else
            Debug.Log("Not connected.");
        return response;    //return the response we got
    }

    // Thread for emg data acquisition
    void accWorker()
    {
        accStream.ReadTimeout = 1000;    //set timeout

        //Create a binary reader to read the data
        BinaryReader reader = new BinaryReader(accStream);

        while (running)
        {
            try
            {
                //Demultiplex the data for all sensors that were detected. Usually, it will be two.
                for (int sn = 0; sn < 16; ++sn)
                {
                    if((sn==(sensors[0]-1)))// || sn==(sensors[1]-1))
                    {
                        ax[sn].Add(reader.ReadSingle()*9.81f);
                        ay[sn].Add(reader.ReadSingle()*9.81f);
                        az[sn].Add(reader.ReadSingle()*9.81f);
                        gx[sn].Add(reader.ReadSingle());
                        gy[sn].Add(reader.ReadSingle());
                        gz[sn].Add(reader.ReadSingle());
                        // the following three lines read the buffer, just to move the pointer. They are responsible to read the magnetometer, that is not available in the Avanty type sensors
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadSingle();
                    } else
                    {
                        for(int j = 0; j < 9; ++j)
                        {
                            reader.ReadSingle();
                        }
                    }
                }
            }
            catch (IOException)
            {
                //Trace.WriteLine("Error in emg found");
            }
        }

        reader.Close(); //close the reader. This also disconnects
    }

    /* // used to tune the alpha
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            alpha = alpha + 0.05f; // Increment the counter
            Debug.Log(alpha); // Print the current value of a to the console
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            alpha = alpha - 0.05f; // Increment the counter
            Debug.Log(alpha); // Print the current value of a to the console
        }
    }
    */

}
