using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class PythonListener : MonoBehaviour
{
    private TcpListener tcpListener;
    private TcpClient connectedTcpClient;
    private float distance; // Assuming this is where you store the received distance value

    void Start()
    {
        StartListening();
    }

    private void StartListening()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse("192.168.1.2"), 8052);
            tcpListener.Start();
            Debug.Log("Python communication setup: OK");

            // Start accepting incoming connections asynchronously
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), tcpListener);
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    private void DoAcceptTcpClientCallback(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        connectedTcpClient = listener.EndAcceptTcpClient(ar);
        Debug.Log("Python client connected");

        NetworkStream stream = connectedTcpClient.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            var data = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (float.TryParse(data, out float receivedDistance))
            {
                distance = receivedDistance;
                Debug.Log($"Received distance: {distance}");
            }
        }

        // Continue listening for new clients
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), tcpListener);
    }

    private void OnApplicationQuit()
    {
        if (tcpListener != null)
        {
            tcpListener.Stop();
        }

        if (connectedTcpClient != null)
        {
            connectedTcpClient.Close();
        }
    }
}

