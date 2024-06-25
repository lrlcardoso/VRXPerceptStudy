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
using UnityEngine.XR.Hands;
using System.Diagnostics;

public class PinchControl : MonoBehaviour
{
    XRHandSubsystem m_HandSubsystem;
    public Animator handAnimator; 
    private ExperimentManager experimentManager; 
    public float x = 0.0f;
    private double accelX;
    private double accelY;
    private double accelZ;
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
    private List<double>[] accXDataList = new List<double>[16];
    private List<double>[] accYDataList = new List<double>[16];
    private List<double>[] accZDataList = new List<double>[16];
    private List<double>[] gyrXDataList = new List<double>[16];
    private List<double>[] gyrYDataList = new List<double>[16];
    private List<double>[] gyrZDataList = new List<double>[16];

    private bool connected = false; //true if connected to server
    private bool running = false;   //true when acquiring data

    //Server commands
    private const string COMMAND_START = "START";
    private const string COMMAND_STOP = "STOP";

    //Threads for acquiring emg and acc data
    private Thread accThread = default!;

    private string id;
    private string fileName;
    private string filePath_save;
    private string filePath_load;
    public bool enableUpdate = false;
    public bool delsysReady = false;


    //Initialize the matrices that identify the system
    private double[][] A;
    private double[][] H;
    private double[][] Q;
    private double[][] W;
    private double[][] A_t; // A transpost
    private double[][] H_t; // H transpost
    private double[][] ECov_priori;
    private double[][] x_priori;
    private double[][] K;
    private double[][] I;
    private double[][] ECov_posteriori = new double[2][]
    {
        new double[] { 0.0f, 0.0f},
        new double[] { 0.0f, 0.0f}
    };
    private double[][] x_hat_online = new double[2][]
    {
        new double[] { 0.0f},
        new double[] { 0.0f}
    };

    private double[][] z = new double[6][]
    {
        new double[] { 0.0f},
        new double[] { 0.0f},
        new double[] { 0.0f},
        new double[] { 0.0f},
        new double[] { 0.0f},
        new double[] { 0.0f}
    };

    private double[][] X = new double[2][]
    {
        new double[] { 0.0f},
        new double[] { 0.0f}
    };

    // ShoulderData class
    public class ShoulderData
    {
        public float XX { get; set; }
        public string Timestamp { get; set; }
        public double Microseconds { get; set; } 
        public double Sensor1_Acc_x { get; set; }
        public double Sensor1_Acc_y { get; set; }
        public double Sensor1_Acc_z { get; set; }
        public double Sensor1_Gyr_x { get; set; }
        public double Sensor1_Gyr_y { get; set; }
        public double Sensor1_Gyr_z { get; set; }
        public double Sensor2_Acc_x { get; set; }
        public double Sensor2_Acc_y { get; set; }
        public double Sensor2_Acc_z { get; set; }
        public double Sensor2_Gyr_x { get; set; }
        public double Sensor2_Gyr_y { get; set; }
        public double Sensor2_Gyr_z { get; set; }
        public double Sensor3_Acc_x { get; set; }
        public double Sensor3_Acc_y { get; set; }
        public double Sensor3_Acc_z { get; set; }
        public double Sensor3_Gyr_x { get; set; }
        public double Sensor3_Gyr_y { get; set; }
        public double Sensor3_Gyr_z { get; set; }

        public override string ToString()
        {
            return $"{Timestamp},{Microseconds},{XX},{Sensor1_Acc_x},{Sensor1_Acc_y},{Sensor1_Acc_z},{Sensor1_Gyr_x},{Sensor1_Gyr_y},{Sensor1_Gyr_z},{Sensor2_Acc_x},{Sensor2_Acc_y},{Sensor2_Acc_z},{Sensor2_Gyr_x},{Sensor2_Gyr_y},{Sensor2_Gyr_z},{Sensor3_Acc_x},{Sensor3_Acc_y},{Sensor3_Acc_z},{Sensor3_Gyr_x},{Sensor3_Gyr_y},{Sensor3_Gyr_z}";
        }
    }

    double accx_1;
    double accy_1; 
    double accz_1;
    double accx_2;
    double accy_2; 
    double accz_2;  
    double accx_3;
    double accy_3; 
    double accz_3; 
    double gyrx_1;
    double gyry_1; 
    double gyrz_1;
    double gyrx_2;
    double gyry_2; 
    double gyrz_2;
    double gyrx_3;
    double gyry_3; 
    double gyrz_3;

    double dt = 0.02;

    double alpha = 0.75;

    double acc_pitch_1;
    double acc_roll_1;
    double acc_pitch_2;
    double acc_roll_2;
    double acc_pitch_3;
    double acc_roll_3;

    double pitch_1 = 0;
    double roll_1 = 0;
    double pitch_2 = 0;
    double roll_2 = 0;
    double pitch_3 = 0;
    double roll_3 = 0;
    private static Stopwatch stopwatch = new Stopwatch();
    private static bool isFirstEntry = true;

    void Start()
    {      
        experimentManager = GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>();

        // Define the name of the file that will be saved
        id = experimentManager.ID;
        fileName = id + "_ShoulderMov.csv";


        // Prepare the file to save data
        filePath_save = experimentManager.filePath + @"\" + id + @"\1_rawDATA";


        // Ensure the directory exists
        if (!Directory.Exists(filePath_save))
        {
            Directory.CreateDirectory(filePath_save);
        }
        // Initialize file path
        filePath_save = Path.Combine(filePath_save, fileName);

        // Ensure the file has headers if it's new
        if (!File.Exists(filePath_save))
        {
            File.WriteAllText(filePath_save, "Timestamp,Acquisition_Time(ms),Animation blend (x),Sensor1_Acc_x,Sensor1_Acc_y,Sensor1_Acc_z,Sensor1_Gyr_x,Sensor1_Gyr_y,Sensor1_Gyr_z,Sensor2_Acc_x,Sensor2_Acc_y,Sensor2_Acc_z,Sensor2_Gyr_x,Sensor2_Gyr_y,Sensor2_Gyr_z,Sensor3_Acc_x,Sensor3_Acc_y,Sensor3_Acc_z,Sensor3_Gyr_x,Sensor3_Gyr_y,Sensor3_Gyr_z\n");
        }
  

        //call function to do the set up with the Delsys base
        setupDelsys();
        
        if(experimentManager.ControlMode.ToString()=="shoulder")
        {
            filePath_load = experimentManager.filePath + @"\" + id + @"\0_calibrationMatrices\";

            try{
                // READ ALL MATRIX FOR THE KALMAN FILTER
                // read matrix A
                A = MatLoad(filePath_load+"A.txt",',');
                // read matrix H
                H = MatLoad(filePath_load+"H.txt",',');
                // read matrix Q
                Q = MatLoad(filePath_load+"Q.txt",',');
                // read matrix W
                W = MatLoad(filePath_load+"W.txt",',');
                // read matrix A_t (A transpost)
                A_t = MatLoad(filePath_load+"A_t.txt",',');
                // read matrix H_t (H transpost)
                H_t = MatLoad(filePath_load+"H_t.txt",',');
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogError("The folder was not well specified.");
                return;
            }

            //Create a identity matrix to be used in the last step of the system identification
            I = MatEye(ECov_posteriori.Length,ECov_posteriori[0].Length);

        }
        else if(experimentManager.ControlMode.ToString()=="None")
        {
            UnityEngine.Debug.Log("No control mode was selected.");
            Application.Quit();
        }

        
    }

    void Update()
    {   
        if(enableUpdate){

            //Define the limits between 0 and 1
            if(x<0)
                x=0;
            if(x>1)
                x=1;


            // Update hand pose according to the current x value
            handAnimator.SetFloat("Blend", x);
        
            GetSensorsData();

            SaveShoulderData();

            if(experimentManager.ControlMode.ToString()=="fingers"){

                if (m_HandSubsystem != null && m_HandSubsystem.running)
                    return;
                
                var handSubsystems = new List<XRHandSubsystem>();
                SubsystemManager.GetSubsystems(handSubsystems);

                for (var i = 0; i < handSubsystems.Count; ++i)
                {
                    var handSubsystem = handSubsystems[i];

                    if (handSubsystem.running)
                    {
                        m_HandSubsystem = handSubsystem;
                        break;
                    }
                }

                if (m_HandSubsystem != null)
                    m_HandSubsystem.updatedHands += OnUpdatedHands;

            }
            else if(experimentManager.ControlMode.ToString()=="shoulder")
            {
                // update x value according to the shoulder position
                X = shoulderElevation();
                //UnityEngine.Debug.Log(X[0][0] + ", " + X[1][0]);
                //x = (Convert.ToSingle(X[0][0])-0.1f)*1.2f;
                x = Convert.ToSingle(X[0][0])-0.15f;

                //UnityEngine.Debug.Log(x);
            }
            
        }
    }

    private void GetSensorsData()
    {
        // build vector z (feature vector) - same structure used in Matlab
        accx_1 = accXDataList[sensors[0]-1][accXDataList[sensors[0]-1].Count - 1]*9.81;
        accy_1 = accYDataList[sensors[0]-1][accYDataList[sensors[0]-1].Count - 1]*9.81; 
        accz_1 = accZDataList[sensors[0]-1][accZDataList[sensors[0]-1].Count - 1]*9.81;
        accx_2 =  accXDataList[sensors[1]][accXDataList[sensors[1]].Count - 1]*9.81;
        accy_2 =  accYDataList[sensors[1]][accYDataList[sensors[1]].Count - 1]*9.81; 
        accz_2 =  accZDataList[sensors[1]][accZDataList[sensors[1]].Count - 1]*9.81; 
        accx_3 =  accXDataList[sensors[2]+1][accXDataList[sensors[2]+1].Count - 1]*9.81;
        accy_3 =  accYDataList[sensors[2]+1][accYDataList[sensors[2]+1].Count - 1]*9.81; 
        accz_3 =  accZDataList[sensors[2]+1][accZDataList[sensors[2]+1].Count - 1]*9.81;    
        gyrx_1 =  gyrXDataList[sensors[0]-1][gyrXDataList[sensors[0]-1].Count - 1];
        gyry_1 =  gyrYDataList[sensors[0]-1][gyrYDataList[sensors[0]-1].Count - 1]; 
        gyrz_1 =  gyrZDataList[sensors[0]-1][gyrZDataList[sensors[0]-1].Count - 1];
        gyrx_2 =  gyrXDataList[sensors[1]][gyrXDataList[sensors[1]].Count - 1];
        gyry_2 =  gyrYDataList[sensors[1]][gyrYDataList[sensors[1]].Count - 1]; 
        gyrz_2 =  gyrZDataList[sensors[1]][gyrZDataList[sensors[1]].Count - 1];
        gyrx_3 =  gyrXDataList[sensors[2]+1][gyrXDataList[sensors[2]+1].Count - 1];
        gyry_3 =  gyrYDataList[sensors[2]+1][gyrYDataList[sensors[2]+1].Count - 1]; 
        gyrz_3 =  gyrZDataList[sensors[2]+1][gyrZDataList[sensors[2]+1].Count - 1];

        if (isFirstEntry)
        {
            // Initialize the stopwatch on the first entry
            stopwatch.Start();
            dt = 0.02; // Initial sample time
            isFirstEntry = false;
        }
        else
        {
            // Get the elapsed time and reset the stopwatch
            dt = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
        }

        // Sample time
        //dt = 0.02;

        // Calculate pitch and roll from accelerometer
        acc_pitch_1 = Math.Atan2(-accx_1, Math.Sqrt(accy_1 * accy_1 + accz_1 * accz_1)) * (180/Math.PI);
        acc_roll_1 = Math.Atan2(accy_1, accz_1) * (180/Math.PI);
        acc_pitch_2 = Math.Atan2(-accx_2, Math.Sqrt(accy_2 * accy_2 + accz_2 * accz_2)) * (180/Math.PI);
        acc_roll_2 = Math.Atan2(accy_2, accz_2) * (180/Math.PI);
        acc_pitch_3 = Math.Atan2(-accx_3, Math.Sqrt(accy_3 * accy_3 + accz_3 * accz_3)) * (180/Math.PI);
        acc_roll_3 = Math.Atan2(accy_3, accz_3) * (180/Math.PI);
        
        // Apply complementary filter for pitch and roll
        pitch_1 = alpha * (pitch_1 + gyry_1 * dt) + (1 - alpha) * acc_pitch_1;
        roll_1 = alpha * (roll_1 + gyrx_1 * dt) + (1 - alpha) * acc_roll_1;
        pitch_2 = alpha * (pitch_2 + gyry_2 * dt) + (1 - alpha) * acc_pitch_2;
        roll_2 = alpha * (roll_2 + gyrx_2 * dt) + (1 - alpha) * acc_roll_2;
        pitch_3 = alpha * (pitch_3 + gyry_3 * dt) + (1 - alpha) * acc_pitch_3;
        roll_3 = alpha * (roll_3 + gyrx_3 * dt) + (1 - alpha) * acc_roll_3;

        z[0][0] = pitch_1;
        z[1][0] = roll_1;
        z[2][0] = pitch_2;
        z[3][0] = roll_2;
        z[4][0] = pitch_3;
        z[5][0] = roll_3;

        //UnityEngine.Debug.Log(pitch_1 + ", " + roll_1 + ", " + pitch_2 + ", " + roll_2 + ", " + pitch_3 + ", " + roll_3);

    }

    private void SaveShoulderData()
    {
        ShoulderData shoulderData = new ShoulderData()
        {
            Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Microseconds = (Time.time),
            XX = x,
            Sensor1_Acc_x = accx_1,
            Sensor1_Acc_y = accy_1,
            Sensor1_Acc_z = accz_1,
            Sensor1_Gyr_x = gyrx_1,
            Sensor1_Gyr_y = gyry_1,
            Sensor1_Gyr_z = gyrz_1,
            Sensor2_Acc_x = accx_2,
            Sensor2_Acc_y = accy_2,
            Sensor2_Acc_z = accz_2,
            Sensor2_Gyr_x = gyrx_2,
            Sensor2_Gyr_y = gyry_2,
            Sensor2_Gyr_z = gyrz_2,
            Sensor3_Acc_x = accx_3,
            Sensor3_Acc_y = accy_3,
            Sensor3_Acc_z = accz_3,
            Sensor3_Gyr_x = gyrx_3,
            Sensor3_Gyr_y = gyry_3,
            Sensor3_Gyr_z = gyrz_3
        };

        using (StreamWriter sw = new StreamWriter(filePath_save, true))
        {
            sw.WriteLine(shoulderData.ToString());
        }
    }

    void OnUpdatedHands(XRHandSubsystem subsystem,
        XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
        XRHandSubsystem.UpdateType updateType)
    {
        
        var indexTip = subsystem.rightHand.GetJoint(XRHandJointIDUtility.FromIndex(XRHandJointID.IndexTip.ToIndex()));
        var thumbTip = subsystem.rightHand.GetJoint(XRHandJointIDUtility.FromIndex(XRHandJointID.ThumbTip.ToIndex()));

        indexTip.TryGetPose(out Pose poseIndex);
        thumbTip.TryGetPose(out Pose poseThumb);

        x = Vector3.Distance(poseIndex.position, poseThumb.position)/0.14f;
    }

    private double[][] shoulderElevation() 
    {
        x_priori = MatProduct(A,x_hat_online);
        ECov_priori = MatSum(MatProduct(MatProduct(A,ECov_posteriori),A_t),W);
        K = MatProduct(MatProduct(ECov_priori,H_t),MatInverse(MatSum(MatProduct(MatProduct(H,ECov_priori),H_t),Q)));
        x_hat_online = MatSum(x_priori,MatProduct(K,MatSub(z,MatProduct(H,x_priori))));
        ECov_posteriori = MatProduct(MatSub(I,MatProduct(K,H)),ECov_priori);

        return x_hat_online;
    }

    private void setupDelsys() 
    {
        UnityEngine.Debug.Log("Delsys setup is running...");
        try
        {
            //Establish TCP/IP connection to server using URL entered
            commandSocket = new TcpClient("localhost", commandPort);

            //Set up communication streams
            commandStream = commandSocket.GetStream();
            commandReader = new StreamReader(commandStream, Encoding.ASCII);
            commandWriter = new StreamWriter(commandStream, Encoding.ASCII);
            response = commandReader.ReadLine();
            //UnityEngine.Debug.Log(response);
            commandReader.ReadLine();   //get extra line terminator
            connected = true;
        }
        catch (Exception)
        {
            //connection failed, display error message
            UnityEngine.Debug.LogError("Could not connect.");
            return;
        }

        //string command = "UPSAMPLE OFF";
        //response = SendCommand(command);
        //UnityEngine.Debug.Log("COMMAND: " + command);
        //UnityEngine.Debug.Log("RESPONSE: " + response);

        for (int i = 0; i < 16; i++)
        {
            string command = "SENSOR " + (i+1) + " ACTIVE?";
            response = SendCommand(command);
            if(response == "YES")
            {
                sensors.Add(i+1);
                //UnityEngine.Debug.Log("SENSOR " + (i+1) + " DETECTED");
                command = "SENSOR " + (i+1) + " SETMODE 173";
                response = SendCommand(command);
                //UnityEngine.Debug.Log(response);
            }
        }

        for (int i = 0; i < 16; i++)
        {
            accXDataList[i] = new List<double>();
            accYDataList[i] = new List<double>();
            accZDataList[i] = new List<double>();
            gyrXDataList[i] = new List<double>();
            gyrYDataList[i] = new List<double>();
            gyrZDataList[i] = new List<double>();
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
        //UnityEngine.Debug.Log("COMMAND: " + COMMAND_START);
        //UnityEngine.Debug.Log("RESPONSE: " + response);   

        Thread.Sleep(1000); //wait 1s to ensure the buffer is full enough

        UnityEngine.Debug.Log("Delsys setup: OK");

        delsysReady = true;
    }

    void OnDestroy()
    {
        //if(experimentManager.ControlMode.ToString()=="shoulder")
        //{
            response = SendCommand(COMMAND_STOP);
            //UnityEngine.Debug.Log("COMMAND: " + COMMAND_STOP);
            //UnityEngine.Debug.Log("RESPONSE: " + response);
            commandSocket.Close();
        //}
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
            UnityEngine.Debug.Log("Not connected.");
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
                    if((sn==(sensors[0]-1)) || sn==(sensors[1]) || sn==(sensors[2]+1))
                    {
                        accXDataList[sn].Add(reader.ReadSingle());
                        accYDataList[sn].Add(reader.ReadSingle());
                        accZDataList[sn].Add(reader.ReadSingle());
                        gyrXDataList[sn].Add(reader.ReadSingle());
                        gyrYDataList[sn].Add(reader.ReadSingle());
                        gyrZDataList[sn].Add(reader.ReadSingle());
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

    //----- METHODS FOR MATRIX MANIPULATION ----- 
        
    // MatLoad: Read a .txt file, save in a matrix and return.
    //          This is used to read the matrices of the system identification, 
    //          built by the Matlab script, "calibration.m"
    static double[][] MatLoad(string fn, char delimiter)
    {  
        var lines = File.ReadAllLines(fn);
        double[][] result = new double[lines.Length][];
        for (int i = 0; i < lines.Length; i++)
        {
            result[i] = Array.ConvertAll(lines[i].Split(delimiter), Double.Parse);
        }
        return result;
    } 

    // MatSum: Sum up two matrices.
    static double[][] MatSum(double[][] matA,double[][] matB)
    {  
        int aRows = matA.Length;
        int aCols = matA[0].Length;
        int bRows = matB.Length;
        int bCols = matB[0].Length;
        if ((aCols != bCols) || (aRows != bRows))
            throw new Exception("Non-conformable matrices");

        double[][] result = MatCreate(aRows, bCols);
        for (int i = 0; i < aRows; i++)
        {
            for (int j = 0; j < bCols; j++)
            {
                result[i][j] = matA[i][j]+matB[i][j];
            }
        }
        return result;
    }

    // MatSub: Subtract two matrices.
    static double[][] MatSub(double[][] matA,double[][] matB)
    {  
        int aRows = matA.Length;
        int aCols = matA[0].Length;
        int bRows = matB.Length;
        int bCols = matB[0].Length;
        if ((aCols != bCols) || (aRows != bRows))
            throw new Exception("Non-conformable matrices");

        double[][] result = MatCreate(aRows, bCols);
        for (int i = 0; i < aRows; i++)
        {
            for (int j = 0; j < bCols; j++)
            {
                result[i][j] = matA[i][j]-matB[i][j];
            }
        }
        return result;
    }

    // MatShow: Print the matrix.
    static void MatShow(double[][] m, int dec, int wid)
    {
        for (int i = 0; i < m.Length; ++i)
        {
            for (int j = 0; j < m[0].Length; ++j)
            {
                double v = m[i][j];
                if (Math.Abs(v) < 1.0e-15) v = 0.0;  // avoid "-0.00"
                Console.Write(v.ToString("F" + dec).PadLeft(wid));
            }
            Console.WriteLine("");
        }
    }

    // MatProduct: Multiply two matrices.
    static double[][] MatProduct(double[][] matA,double[][] matB)
    {
        int aRows = matA.Length;
        int aCols = matA[0].Length;
        int bRows = matB.Length;
        int bCols = matB[0].Length;
        if (aCols != bRows)
        throw new Exception("Non-conformable matrices");

        double[][] result = MatCreate(aRows, bCols);

        for (int i = 0; i < aRows; ++i) // each row of A
            for (int j = 0; j < bCols; ++j) // each col of B
                for (int k = 0; k < aCols; ++k) // could use bRows
                    result[i][j] += matA[i][k] * matB[k][j];

        return result;
    }

    // MatInverse: Invert the matrix.
    static double[][] MatInverse(double[][] m)
    {
        // assumes determinant is not 0
        // that is, the matrix does have an inverse
        int n = m.Length;
        double[][] result = MatCreate(n, n); // make a copy
        for (int i = 0; i < n; ++i)
            for (int j = 0; j < n; ++j)
                result[i][j] = m[i][j];

        double[][] lum; // combined lower & upper
        int[] perm;  // out parameter
        MatDecompose(m, out lum, out perm);  // ignore return

        double[] b = new double[n];
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < n; ++j)
                if (i == perm[j])
                    b[j] = 1.0;
                else
                    b[j] = 0.0;

            double[] x = Reduce(lum, b); // 
            for (int j = 0; j < n; ++j)
                result[j][i] = x[j];
        }
        return result;
    }

    // MatDecompose: Decompose the matrix. This is called by MatInverse() function
    static int MatDecompose(double[][] m, out double[][] lum, out int[] perm)
    {
        // Crout's LU decomposition for matrix determinant and inverse
        // stores combined lower & upper in lum[][]
        // stores row permuations into perm[]
        // returns +1 or -1 according to even or odd number of row permutations
        // lower gets dummy 1.0s on diagonal (0.0s above)
        // upper gets lum values on diagonal (0.0s below)

        int toggle = +1; // even (+1) or odd (-1) row permutatuions
        int n = m.Length;

        // make a copy of m[][] into result lu[][]
        lum = MatCreate(n, n);
        for (int i = 0; i < n; ++i)
            for (int j = 0; j < n; ++j)
                lum[i][j] = m[i][j];

        // make perm[]
        perm = new int[n];
        for (int i = 0; i < n; ++i)
            perm[i] = i;

        for (int j = 0; j < n - 1; ++j) // process by column. note n-1 
        {
            double max = Math.Abs(lum[j][j]);
            int piv = j;

            for (int i = j + 1; i < n; ++i) // find pivot index
            {
                double xij = Math.Abs(lum[i][j]);
                if (xij > max)
                {
                    max = xij;
                    piv = i;
                }
            } // i

            if (piv != j)
            {
                double[] tmp = lum[piv]; // swap rows j, piv
                lum[piv] = lum[j];
                lum[j] = tmp;

                int t = perm[piv]; // swap perm elements
                perm[piv] = perm[j];
                perm[j] = t;

                toggle = -toggle;
            }

            double xjj = lum[j][j];
            if (xjj != 0.0)
            {
                for (int i = j + 1; i < n; ++i)
                {
                    double xij = lum[i][j] / xjj;
                    lum[i][j] = xij;
                    for (int k = j + 1; k < n; ++k)
                        lum[i][k] -= xij * lum[j][k];
                }
            }

        } // j

        return toggle;  // for determinant
    } // MatDecompose

    // Reduce: Helper function called by MatInverse() function.
    static double[] Reduce(double[][] luMatrix, double[] b) // helper
    {
        int n = luMatrix.Length;
        double[] x = new double[n];
        //b.CopyTo(x, 0);
        for (int i = 0; i < n; ++i)
            x[i] = b[i];

        for (int i = 1; i < n; ++i)
        {
            double sum = x[i];
            for (int j = 0; j < i; ++j)
                sum -= luMatrix[i][j] * x[j];
            x[i] = sum;
        }

        x[n - 1] /= luMatrix[n - 1][n - 1];
        for (int i = n - 2; i >= 0; --i)
        {
            double sum = x[i];
            for (int j = i + 1; j < n; ++j)
                sum -= luMatrix[i][j] * x[j];
            x[i] = sum / luMatrix[i][i];
        }

        return x;
    } // Reduce

    // MatCreate: set up any matrix.
    static double[][] MatCreate(int rows, int cols)
    {
        double[][] result = new double[rows][];
        for (int i = 0; i < rows; ++i)
            result[i] = new double[cols];
        return result;
    }

    // MatEye: create a identity matrix with dimensions rows x cols
    static double[][] MatEye(int rows, int cols)
    {
        double[][] result = MatCreate(rows, cols);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (i==j)
                    result[i][j] = 1;
            }
        }
        return result;
    }

}