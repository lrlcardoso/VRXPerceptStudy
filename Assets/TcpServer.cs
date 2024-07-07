using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServer : MonoBehaviour
{
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;

    public GameObject cube; // Assign the cube GameObject in the Inspector
    private MainThreadDispatcher mainThreadDispatcher;

    void Start()
    {
        mainThreadDispatcher = GameObject.FindObjectOfType<MainThreadDispatcher>();

        tcpListenerThread = new Thread(new ThreadStart(ListenForIncomingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    private void ListenForIncomingRequests()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, 8052);
            tcpListener.Start();
            Debug.Log("Server is listening");

            while (true)
            {
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    NetworkStream stream = connectedTcpClient.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        var data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        //Debug.Log("Received data: " + data);
                        
                        if (float.TryParse(data, out float distance))
                        {
                            Debug.Log(distance);
                            // Update cube height on the main thread
                            mainThreadDispatcher.Enqueue(() => UpdateCubeHeight(distance));
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    private void UpdateCubeHeight(float height)
    {
        if (cube != null)
        {
            Vector3 newPosition = cube.transform.position;
            newPosition.y = height;
            cube.transform.position = newPosition;
        }
    }

    void OnDestroy()
    {
        if (tcpListener != null)
        {
            tcpListener.Stop();
        }
    }
}
