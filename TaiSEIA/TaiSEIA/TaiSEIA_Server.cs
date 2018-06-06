using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

namespace TaiSEIA
{
    public class TaiSEIA_Server
    {
        //Declaring Global variables
        private TcpListener server;                
        public List<TcpclientsWithMessage> clients; // Creates a TCP Client list


        public TaiSEIA_Server(string port)
        {
            // Listen to any IP address with specific port
            server = new TcpListener(IPAddress.Any, Convert.ToInt32(port));

            // Initial a list of clients instance
            clients = new List<TcpclientsWithMessage>();

            
        }

        public void StartConnect()
        {
            server.Start(); // Starts Listening to Any IPAddress trying to connect to the program with port 8080
            Console.WriteLine("Waiting For Connection....");
            Thread t = new Thread(() => // Creates a New Thread (like a timer)
            {
                while (true)
                {
                    TcpclientsWithMessage tmp = new TcpclientsWithMessage(server.AcceptTcpClient());
                    
                    clients.Add(tmp);  //Waits for the Client To Connect                 

                    Console.WriteLine("Connected To Client");
                    if (clients[clients.Count - 1].cl.Connected) // If you are connected
                    {
                        ServerReceive(ref clients); //Start Receiving     
                        //setupIDProcess();                                     
                    }
                }

            });
            t.IsBackground = true;
            t.Start();
        }
        private void setupIDProcess()
        {

        }

        //Handling receiveing data;
        private void ServerReceive(ref List<TcpclientsWithMessage> tmp)
        {
            int i;
            NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
            int no = tmp.Count - 1;
            TcpclientsWithMessage current_client = tmp[no];
            stream = tmp[no].cl.GetStream(); //Gets The Stream of The Connection
            Thread t = new Thread(() => // Thread (like Timer)
            {
                try
                {
                    byte[] datalength = new byte[3]; // creates a new byte with length 4 ( used for receivng data's length)
                    while ((i = current_client.cl.GetStream().Read(datalength, 0, 3)) != 0)//Keeps Trying to Receive the Size of the Message or Data
                    {
                        //Datalength data is at index 2, so go and fetch it!                        
                        byte[] data = new byte[Convert.ToInt32(datalength[2])];
                        for (int j = 0; j < 3; j++)
                            data[j] = datalength[j];
                        
                        current_client.cl.GetStream().Read(data, 3, data.Length - 3); //Receives The Real Data not the Size
                        
                        string message = "";
                        foreach (byte data_byte in data)
                        {
                            message = message + Convert.ToInt32(data_byte).ToString("X2") + " ";
                        }

                        //Message output
                        current_client.setMessage(message);
                        //-----
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
            t.IsBackground = true;
            t.Start(); // Start the Thread
        }
        
        

        //Send message to specific client or all clients
        public void ServerSend(string msg, int target = -1)
        {
            if (target == -1) //Broadcast message for all clients
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].cl.Connected)
                    {
                        try
                        {
                            NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
                            stream = clients[i].cl.GetStream(); //Gets The Stream of The Connection
                            byte[] data; // creates a new byte without mentioning the size of it cuz its a byte used for sending

                            string[] hexValues = msg.Split(' ');
                            data = new byte[hexValues.Length];

                            for (int j = 0; j < hexValues.Length; j++)
                            {
                                data[j] = Convert.ToByte(Convert.ToInt32(hexValues[j], 16));
                            }

                            //data = Encoding.Default.GetBytes(msg); // put the msg in the byte ( it automaticly uses the size of the msg )
                            //int length = data.Length; // Gets the length of the byte data
                            //byte[] datalength = new byte[4]; // Creates a new byte with length of 4
                            //datalength = BitConverter.GetBytes(length); //put the length in a byte to send it
                            //stream.Write(datalength, 0, 4); // sends the data's length
                            stream.Write(data, 0, data.Length); //Sends the real data
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
            else
            {
                if (!(target >= 0 && target < clients.Count))
                {
                    Console.WriteLine("Target number out of range!!");
                    return;
                }

                if (clients[target].cl.Connected)
                {
                    try
                    {
                        NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
                        stream = clients[target].cl.GetStream(); //Gets The Stream of The Connection
                        byte[] data; // creates a new byte without mentioning the size of it cuz its a byte used for sending
                        data = Encoding.Default.GetBytes(msg); // put the msg in the byte ( it automaticly uses the size of the msg )
                        int length = data.Length; // Gets the length of the byte data
                        byte[] datalength = new byte[4]; // Creates a new byte with length of 4
                        datalength = BitConverter.GetBytes(length); //put the length in a byte to send it
                        stream.Write(datalength, 0, 4); // sends the data's length
                        stream.Write(data, 0, data.Length); //Sends the real data
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            

        }

        //Generate byte array msg from function code, 
        //2 byte hexCode = function ID + Sub-function ID, ex: 0x00 + 0xF0 --> hexCode = 0x00F0
        private byte[] generateTaiSEIACode(ushort hexCode, string[] data=null, bool hasData = false)
        {
            byte[] code = new byte[24];

            byte[] tmp;

            //Message Structure (26 + N bytes)
            //Header ID     : 1 byte  , constant value = 0x13
            //Packet Length : 2 bytes ,
            //Sender ID     : 6 bytes , (4bytes USER ID : data[0] + 2bytes HG/HNA ID : data[1])
            //Receiver ID   : 6 bytes , (4bytes USER ID : data[2] + 2bytes HG/HNA ID : data[3])
            //Group ID      : 1 byte  , set to 0xFF currently, data[4]
            //Event ID      : 2 bytes , data[5]
            //Function Code : 2 bytes , Function ID + Sub-function ID from hexCode
            //Reserved bytes: 2 bytes , constant 0xFFFF
            //Segment ID    : 2 bytes , data order of splitted data, assumed no splitting for now
            //data[5~END]     : N bytes , data content N>=0, exists if hasData = true
            //CRC           : 2 bytes , CRC-16-CCITT with initial value 0xFFFF

            //---- Set Header ID constant = 0x13 -----
            byte current_byte = 0x13;
            code[0] = current_byte;
            //--------------------------------

            //----- Sender ID, code[3~8] ----
            //4bytes USER ID   
            string2byte(data[0],32).CopyTo(code, 3);
            //2bytes HG/HNA ID
            string2byte(data[1],16).CopyTo(code, 7);
            //--------------------------------

            //---- Receiver ID, code[9~14] -----
            //4bytes USER ID   
            string2byte(data[2], 32).CopyTo(code, 9);
            //2bytes HG/HNA ID
            string2byte(data[3], 16).CopyTo(code, 13);
            //--------------------------------

            //---- Set Group ID, code[15] ----
            code[15] = 0xFF;

            //----------------------------------


            //---- Set Event ID, code[16~17] ----           
            string2byte(data[5], 16).CopyTo(code, 16);
            //----------------------------------

            //---- Set Function ID from hexcode, code[18~19] ----
            tmp = BitConverter.GetBytes(Convert.ToUInt16(hexCode));
            code[18] = tmp[1]; //Function ID
            code[19] = tmp[0]; //Sub-Function ID
            //----------------------------------


            //---- Set Reserved Bytes, code[20~21] = 0xFFFF-----
            code[20] = 0xFF;
            code[21] = 0xFF;
            //----------------------------------

            //---- Set Segment ID, code[22~23] = 0x0000-----
            code[22] = 0x00;
            code[23] = 0x00;
            //----------------------------------


            //---- Set Data by appending bytes, code[N] ----

            //Write Code here

            //-----------------------------------


            //---- Set CRC by appending bytes -----
            ushort crc = Compute_CRC16_Simple(code);
            tmp = BitConverter.GetBytes(Convert.ToUInt16(crc));
            Array.Reverse(tmp);
            code = appendToByteArray(code,tmp);
            //-----------------------------------


            return code;
        }

        byte[] string2byte(string t, int type)
        {
            byte[] result;
            if (type == 16)
            {
                result = new byte[2];
                UInt16 buf16;
                buf16 = Convert.ToUInt16(t);
                result = BitConverter.GetBytes(buf16);
                Array.Reverse(result);
            }
            else if (type == 32)
            {
                result = new byte[4];
                UInt32 buf32;
                buf32 = Convert.ToUInt32(t);
                result = BitConverter.GetBytes(buf32);
                Array.Reverse(result);
            }
            else
            {
                result = new byte[2];//Dummy line, just preventing debug errors
            }

            return result;
        }
        public ushort Compute_CRC16_Simple(byte[] bytes)
        {
            /*
            Reference : http://www.sunshine2k.de/articles/coding/crc/understanding_crc.html
            Algorithm : CRC-16-CCIT-FALSE
            Polynomial : 0x1021
            Initial Value : 0xFFFF
            Final XOR Value : 0x0 (Actually 0 has no impact)            
            */
            const ushort generator = 0x1021;    /* divisor is 16bit */
            ushort crc = 0xFFFF; /* CRC value is 16bit (Initial Value)*/

            foreach (byte b in bytes)
            {
                crc ^= (ushort)(b << 8); /* move byte into MSB of 16bit CRC */

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0) /* test for MSB = bit 15 */
                    {
                        crc = (ushort)((crc << 1) ^ generator);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }

            return crc;
        }

        private byte[] appendToByteArray(byte[] bArray, byte[] newByte)
        {
            byte[] newArray = new byte[bArray.Length + newByte.Length];
            bArray.CopyTo(newArray, 0);
            newByte.CopyTo(newArray, bArray.Length);
            return newArray;
        }

        //-----------------------------
    }

    public class TcpclientsWithMessage
    {
        public string USER_ID;
        public string HNA_ID;
        public TcpClient cl;
        public string msg="";

        public TcpclientsWithMessage()
        {
        }
        public TcpclientsWithMessage(TcpClient a)
        {
            cl = a;
        }
        public void clearMessage()
        {
            msg = "";
        }
        public string getMessage()
        {
            string tmp = msg;
            clearMessage();
            return tmp;
        }
        public void setMessage(string a)
        {
            msg = a;
        }
        public bool isContainMessage()
        {
            if (msg.Equals("") == true)
                return false;
            else
                return true;
        }
    }
}
