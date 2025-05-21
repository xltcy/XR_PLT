using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.UI;
using System;
using System.IO;

public class TestConnection : Client
{
    public InputField ToSend;

    // Start is called before the first frame update
    void Start()
    {
        //BuildClient();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TestSendMsg()
    {
        if(BuildClient())
        {
            string message = ToSend.text;
            //byte[] byteArrayData = Encoding.UTF8.GetBytes(message);
            //byte[] byteArrayLength = BitConverter.GetBytes(byteArrayData.Length);
            var byteArrayLength = new byte[4];
            int[] length = new int[1];
            length[0] = message.Length;
            Buffer.BlockCopy(length, 0, byteArrayLength, 0, 4);

            Array data = message.ToCharArray();
            var byteArrayData = new byte[length[0] * 2];
            Buffer.BlockCopy(data, 0, byteArrayData, 0, byteArrayData.Length);


            ClientSocket.Send(byteArrayLength);
            ClientSocket.Send(byteArrayData);
        }
    }

    public float[] receivedata()
    {
        // receive length
        byte[] bytes = new byte[4];
        // return bytes length used
        int IndUsedBytes = ClientSocket.Receive(bytes);
        float[] data_length = new float[1];
        Buffer.BlockCopy(bytes, 0, data_length, 0, IndUsedBytes);
        int length = (int)data_length[0];

        // receive data
        float[] data_received = new float[length];
        int UsedBytesAccumulated = 0;
        int next_bytes_length = length * 4 > 1024 ? 1024 : length * 4;
        while (true)
        {
            byte[] data_bytes = new byte[next_bytes_length];
            IndUsedBytes = ClientSocket.Receive(data_bytes);
            Buffer.BlockCopy(data_bytes, 0, data_received, UsedBytesAccumulated, IndUsedBytes);
            UsedBytesAccumulated += IndUsedBytes;
            if (UsedBytesAccumulated == length * 4)
            {
                break;
            }
            next_bytes_length = length * 4 - UsedBytesAccumulated > 1024 ? 1024 : length * 4 - UsedBytesAccumulated;
        }

        Debug.Log($"Successfully receive {length} data!");
        return data_received;
    }
}
