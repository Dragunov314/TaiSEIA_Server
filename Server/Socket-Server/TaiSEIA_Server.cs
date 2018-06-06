using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;
using System.Windows.Forms;

namespace TaiSEIA
{
    public class TaiSEIA_Server
    {
        //Declaring Global variables
        public string USER_ID;
        public string HG_ID;
        public List<TcpclientsWithMessage> clients; // Creates a TCP Client list

        private TcpListener server;
        private TextBox txtLog;
        private Form fm1;


        public TaiSEIA_Server(string port, TextBox a, Form b, string usr = "8787", string hg = "1")
        {
            // Listen to any IP address with specific port
            server = new TcpListener(IPAddress.Any, Convert.ToInt32(port));

            // Initial a list of clients instance
            clients = new List<TcpclientsWithMessage>();

            USER_ID = usr;
            HG_ID = hg;
            txtLog = a;
            fm1 = b;
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
                        setupIDProcess(clients.Count - 1);                                     
                    }
                }

            });
            t.IsBackground = true;
            t.Start();
        }
        private void setupIDProcess(int index)
        {
            while (clients[index].isContainMessage() == false) { };

            string msg = clients[index].getMessage();
            TaiSEIA_Packet_structure receive_code = new TaiSEIA_Packet_structure(msg);
            if (receive_code.function_ID[0] == 0x01 && receive_code.function_ID[1] == 0x00) //GET broadcast
            {
                string[] data = new string[6];
                data[0] = "4294967295";
                data[1] = "65535";
                data[2] = "4294967295";
                data[3] = "65535";
                data[4] = "255"; 
                data[5] = "1";

                byte[] db = generateTaiSEIACode(0xF0FF,data);//Generate ACK
                TaiSEIA_Packet_structure tmp = new TaiSEIA_Packet_structure(db);

                Console.WriteLine(tmp.ToString());
                ServerSend(tmp.ToString(), index);
            }

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
                        for (int j = 0; j < data.Length; j++)
                        {
                            if(j!=data.Length-1)
                                message = message + Convert.ToInt32(data[j]).ToString("X2") + " ";
                            else
                                message = message + Convert.ToInt32(data[j]).ToString("X2");
                        }

                        //Message output
                        current_client.setMessage(message);
                        DateTime localDate = DateTime.Now;
                        String timestamp = localDate.ToString(new CultureInfo("en-US"));

                        fm1.Invoke((MethodInvoker)delegate // To Write the Received data
                        {                            
                            txtLog.AppendText(timestamp + " | Client : " + message + Environment.NewLine);
                        });
                        
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
            //data[6~END]     : N bytes , data content N>=0, exists if hasData = true
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

            //--- Set Packet Length ----
            string2byte((code.Length+2).ToString(), 16).CopyTo(code, 1);
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
            if (type == 16)//2 bytes
            {
                result = new byte[2];
                UInt16 buf16;
                buf16 = Convert.ToUInt16(t);
                result = BitConverter.GetBytes(buf16);
                Array.Reverse(result);
            }
            else if (type == 32)// 4 bytes
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
    public class TaiSEIA_Packet_structure
    {
        //Message Structure (26 + N bytes)
        //Header ID     : 1 byte  , constant value = 0x13
        //Packet Length : 2 bytes ,
        //Sender ID     : 6 bytes , (4bytes USER ID  + 2bytes HG/HNA ID )
        //Receiver ID   : 6 bytes , (4bytes USER ID  + 2bytes HG/HNA ID )
        //Group ID      : 1 byte  , set to 0xFF currently
        //Event ID      : 2 bytes 
        //Function Code : 2 bytes , Function ID + Sub-function ID from hexCode
        //Reserved bytes: 2 bytes , constant 0xFFFF
        //Segment ID    : 2 bytes , data order of splitted data, assumed no splitting for now
        //DATA          : N bytes , data content N>=0, exists if hasData = true
        //CRC           : 2 bytes , CRC-16-CCITT with initial value 0xFFFF
        public byte header_ID;
        public byte[] packet_length = new byte[2];
        public byte[] sender_ID = new byte[6];
        public byte[] receiver_ID = new byte[6];
        public byte group_ID;
        public byte[] event_ID = new byte[2];
        public byte[] function_ID = new byte[2];
        public byte[] segment_ID = new byte[2];
        public byte[] data = null;
        public byte[] crc = new byte[2];

        public TaiSEIA_Packet_structure(byte[] raw_code)
        {
            decode(raw_code);
        }

        public TaiSEIA_Packet_structure(string hexstr)
        {
           decode(hexString2byteArray(hexstr));
        }

        public void decode(byte[] raw_code)
        {
            header_ID = raw_code[0];

            Array.Copy(raw_code, 1, packet_length, 0, 2);

            Array.Copy(raw_code, 3, sender_ID, 0, 6);

            Array.Copy(raw_code, 9, receiver_ID, 0, 6);

            group_ID = raw_code[15];

            Array.Copy(raw_code, 16, event_ID, 0, 2);

            Array.Copy(raw_code, 18, function_ID, 0, 2);

            Array.Copy(raw_code, 22, segment_ID, 0, 2);

            int packet_len = byte2int(packet_length);
            byte[] tmp = new byte[packet_len - 2];
            if (packet_len > 26)
            {
                Array.Copy(raw_code, 24, data, 0, packet_len - 26);
                Array.Copy(raw_code, packet_len - 2, crc, 0, 2);
            }
            else
            {
                Array.Copy(raw_code, 24, crc, 0, 2);
            }

            Array.Copy(raw_code, 0, tmp, 0, packet_len - 2);
            if (checkCRC(tmp, crc))
            {
                Console.WriteLine("CRC Correct!!");
            }
            else
            {
                Console.WriteLine("CRC Incorrect!!");
            }

        }
        public byte[] hexString2byteArray(string str)
        {
            string[] hexValues = str.Split(' ');
            byte[] hexValuesByte = new byte[hexValues.Length];

            for (int i = 0; i < hexValues.Length; i++)
            {
                hexValuesByte[i] = Convert.ToByte(Convert.ToInt32(hexValues[i], 16));
            }
            return hexValuesByte;
        }
            
        public ushort byte2int(byte[] b)
        {
            byte[] tmp = new byte[b.Length];
            b.CopyTo(tmp, 0);
            Array.Reverse(tmp);
            return BitConverter.ToUInt16(tmp, 0);
        }

        
        public override string ToString()
        {
            string str = "";
            str += header_ID.ToString("X2")+" ";
            str += packet_length[0].ToString("X2")+" ";
            str += packet_length[1].ToString("X2")+" ";
            for(int i=0;i<6;i++)
                str += sender_ID[i].ToString("X2") + " ";
            for (int i = 0; i < 6; i++)
                str += receiver_ID[i].ToString("X2") + " ";
            str += group_ID.ToString("X2")+" ";
            str += event_ID[0].ToString("X2")+ " ";
            str += event_ID[1].ToString("X2") + " ";
            str += function_ID[0].ToString("X2") + " ";
            str += function_ID[1].ToString("X2") + " ";
            str += "FF FF ";//Reserved ID
            str += segment_ID[0].ToString("X2") + " ";
            str += segment_ID[1].ToString("X2") + " ";
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                    str += data[i].ToString("X2") + " ";
            }
            str += crc[0].ToString("X2") + " ";
            str += crc[1].ToString("X2");

            return str;
        }

        public bool checkCRC(byte[] d, byte[] receive_crc)
        {
            ushort ans = Compute_CRC16_Simple(d);
            ushort rc = byte2int(receive_crc);

            if (ans == rc)
                return true;
            else
                return false;
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
