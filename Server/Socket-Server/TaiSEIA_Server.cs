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
        public List<HNAClients> clients; // Creates a TCP Client list

        private TcpListener server;
        private TextBox txtLog;
        private Form fm1;


        public TaiSEIA_Server(string port, TextBox a, Form b, string usr = "8787", string hg = "1")
        {
            // Listen to any IP address with specific port
            server = new TcpListener(IPAddress.Any, Convert.ToInt32(port));

            // Initial a list of clients instance
            clients = new List<HNAClients>();

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
                    HNAClients tmp = new HNAClients(server.AcceptTcpClient());
                    
                    clients.Add(tmp);  //Waits for the Client To Connect                 

                    Console.WriteLine("Connected To Client");
                    if (clients[clients.Count - 1].cl.Connected) // If you are connected
                    {
                        ServerReceive(ref clients); //Start Receiving     
                        //setupIDProcess(clients.Count - 1);                                     
                    }
                }

            });
            t.IsBackground = true;
            t.Start();
        }
        private void setupIDProcess(int index)
        {
            while (clients[index].isContainMessage() == false) { }; //Wait for client to broadcast

            string msg = clients[index].getMessage();
            TaiSEIA_Packet_structure receive_code = new TaiSEIA_Packet_structure(msg);
            if (receive_code.function_ID[0] == 0x01 && receive_code.function_ID[1] == 0x00) //GET broadcast
            {
                TaiSEIA_Packet_structure tmp = new TaiSEIA_Packet_structure(0xFFFFFFFF, 0xFFFF,  //Sender ID
                                                                            0xFFFFFFFF, 0xFFFF,  //Receiver ID
                                                                            0xFF,    //Group ID
                                                                            0x01,    //Event ID
                                                                            0xF0FF); //Function ID
                ServerSend(tmp.ToString(), index);

                tmp.setFunctionID(0x0102, new string[3] { Convert.ToInt64("0xA01",16).ToString(), "1", "2" });
                ServerSend(tmp.ToString(), index);
            }



        }

        //Handling receiveing data;
        private void ServerReceive(ref List<HNAClients> tmp)
        {
            int i;
            NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
            int no = tmp.Count - 1;
            HNAClients current_client = tmp[no];
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
        

        

        //-----------------------------
    }
    public class TaiSEIA_Packet_structure
    {
        /// <summary>
        /// Message Structure (26 + N bytes)
        /// Header ID     : 1 byte  , constant value = 0x13
        /// Packet Length : 2 bytes ,
        /// Sender ID     : 6 bytes , (4bytes USER ID  + 2bytes HG/HNA ID )
        /// Receiver ID   : 6 bytes , (4bytes USER ID  + 2bytes HG/HNA ID )
        /// Group ID      : 1 byte  , set to 0xFF currently
        /// Event ID      : 2 bytes 
        /// Function Code : 2 bytes , Function ID + Sub-function ID from hexCode
        /// Reserved bytes: 2 bytes , constant 0xFFFF
        /// Segment ID    : 2 bytes , data order of splitted data, assumed no splitting for now
        /// DATA          : N bytes , data content N>=0, exists if hasData = true
        /// CRC           : 2 bytes , CRC-16-CCITT with initial value 0xFFFF
        /// </summary>

        
        public byte[] header_ID = new byte[1];
        public byte[] packet_length = new byte[2];
        public byte[] sender_ID = new byte[6];
        public byte[] receiver_ID = new byte[6];
        public byte[] group_ID =new byte[1];
        public byte[] event_ID = new byte[2];
        public byte[] function_ID = new byte[2];
        public byte[] reserved_bytes = new byte[]{ 0xFF, 0xFF }; 
        public byte[] segment_ID = new byte[2];
        public byte[] data = null;
        public byte[] crc = new byte[2];

        private List<byte[]> all_members;
        //Look up table for all_members list
        private Dictionary<string, int> mb_table = new Dictionary<string, int>() {
            {"header_ID",0}, {"packet_length", 1},{"sender_ID",2},{"receiver_ID",3},{"group_ID",4},
            { "event_ID",5},{"function_ID",6},{"reserved_bytes",7},{"segment_ID",8}
        };

        /// <summary>
        /// Construct an instance of TaiSEIA structure. Input parameters snd_usr_id,rcv_usr_id should be Int64 data type.
        /// </summary>
        /// <param name="snd_usr_id"></param>
        /// <param name="snd_hg_id"></param>
        /// <param name="rcv_usr_id"></param>
        /// <param name="rcv_hg_id"></param>
        /// <param name="grp_id"></param>
        /// <param name="evt_id"></param>
        /// <param name="fcn_id"></param>
        /// <param name="data"></param>
        /// <param name="hasData"></param>
        public TaiSEIA_Packet_structure(Int64 snd_usr_id, int snd_hg_id, Int64 rcv_usr_id, int rcv_hg_id, int grp_id, int evt_id, int fcn_id, string data_str = "", bool hasData = false)
        {   
            //Check input
            if (snd_usr_id > 0xFFFFFFFF || snd_usr_id < 0x0000000000)
                throw new System.ArgumentException("Parameter is out of range", "snd_usr_id");
            if (snd_hg_id > 0xFFFF || snd_hg_id<0x0000)
                throw new System.ArgumentException("Parameter is out of range", "snd_hg_id");
            if (rcv_usr_id > 0xFFFFFFFF || rcv_usr_id < 0x00000000)
                throw new System.ArgumentException("Parameter is out of range", "rcv_usr_id");
            if (rcv_hg_id > 0xFFFF || rcv_hg_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "rcv_hg_id");
            if (grp_id>0xFF || grp_id<0x00)
                throw new System.ArgumentException("Parameter is out of range", "grp_id");
            if (evt_id > 0xFFFF || evt_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "evt_id");
            if (fcn_id>0xFFFF || fcn_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "fcn_id");
            ///
            header_ID[0] = 0x13;

            packet_length = int2byteArray(0x1A, 16); //length = 26

            byte[] tmp;
            tmp = int2byteArray(snd_usr_id, 32);
            Array.Copy(tmp, 0, sender_ID, 0, 4);
            tmp = int2byteArray(snd_hg_id, 16);
            Array.Copy(tmp, 0, sender_ID, 4, 2);

            tmp = int2byteArray(rcv_usr_id, 32);
            Array.Copy( tmp, 0, receiver_ID, 0, 4);
            tmp = int2byteArray(rcv_hg_id, 16);
            Array.Copy( tmp, 0, receiver_ID, 4, 2);

            if (BitConverter.IsLittleEndian)
                group_ID[0] = BitConverter.GetBytes(Convert.ToUInt16(grp_id))[0];
            else
                group_ID[0] = BitConverter.GetBytes(Convert.ToUInt16(grp_id))[1];

            event_ID = int2byteArray(evt_id, 16);

            function_ID = int2byteArray(fcn_id, 16);

            //reserved bytes

            segment_ID = int2byteArray(0x0000, 16); //Default to 0

            // Assume no data
            //

            //Generate CRC
            refreshAllMembers();
            
        }
        public TaiSEIA_Packet_structure(TaiSEIA_Packet_structure source)
        {
            if (source.data != null)
                data = new byte[source.data.Length];

            refreshAllMembers();
            for (int i = 0; i < this.all_members.Count; i++)
            {
                for (int j = 0; j < this.all_members[i].Length; j++)
                {
                    this.all_members[i][j] = source.all_members[i][j];
                }
            }            
            
        }
        public TaiSEIA_Packet_structure()
        {
            byte[] tmp = new byte[] { 0x13, 0x00, 0x1A, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                      0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x01, 0x01, 0x00, 0xFF, 0xFF,
                                      0x00, 0x00, 0xCA, 0x21 };
            //default broadcast code

            decode(tmp);
            refreshAllMembers();
        }

        public TaiSEIA_Packet_structure(byte[] raw_code)
        {
            decode(raw_code);
            refreshAllMembers();
        }

        public void setFunctionID(int fcn_id, string[] cmd_data=null)
        {
            if (fcn_id > 0xFFFF || fcn_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "fcn_id");

            function_ID = int2byteArray(fcn_id, 16);

            if (this.isEqualFunctionID(0x0102))
            {
                if (cmd_data == null)
                    throw new System.ArgumentException("Data is null!!", "cmd_data");
                else if (cmd_data.Length != 3)
                    throw new System.ArgumentException("Data Length is not match!!", "cmd_data");
                else
                {
                    if (Convert.ToInt64(cmd_data[0]) > 0xFFFFFFFF || Convert.ToInt64(cmd_data[0]) < 0x00000000)
                        throw new System.ArgumentException("Parameter is out of range", "cmd_data[0]");
                    if (Convert.ToInt32(cmd_data[1]) > 0xFFFF || Convert.ToInt32(cmd_data[1]) < 0x0000)
                        throw new System.ArgumentException("Parameter is out of range", "cmd_data[1]");
                    if (Convert.ToInt32(cmd_data[2]) > 0xFFFF || Convert.ToInt32(cmd_data[2]) < 0x0000)
                        throw new System.ArgumentException("Parameter is out of range", "cmd_data[2]");

                    data = new byte[1] { 0 };
                    data = appendToByteArray(data, int2byteArray(Convert.ToInt64(cmd_data[0]), 32));
                    data = appendToByteArray(data, 0x01);
                    data = appendToByteArray(data, int2byteArray(Convert.ToInt32(cmd_data[1]), 16));
                    data = appendToByteArray(data, 0x02);
                    data = appendToByteArray(data, int2byteArray(Convert.ToInt32(cmd_data[2]), 16));
                }
            }
            else
            {
                data = null;//Clear data
            }

            refreshAllMembers();
        }

        public TaiSEIA_Packet_structure(string hexstr)
        {
           decode(hexString2byteArray(hexstr));
           refreshAllMembers();
        }

        public override string ToString()
        {
            refreshAllMembers();
            string str = "";

            for (int i = 0; i < all_members.Count; i++)
            {
                for (int j = 0; j < all_members[i].Length; j++)
                {
                    if (i == (all_members.Count - 1) && j == (all_members[i].Length - 1))
                        str += all_members[i][j].ToString("X2");
                    else
                        str += all_members[i][j].ToString("X2") + " ";
                }
            }
            return str;
        }

        public byte[] ToByteArray()
        {
            refreshAllMembers();
            byte[] result = new byte[1];
            result[0] = header_ID[0];

            for (int i = 1; i < all_members.Count; i++)
            {
                for (int j = 0; j < all_members[i].Length; j++)
                {
                    result = appendToByteArray(result, all_members[i][j]);
                }
            }

            return result;
        }

        public bool isEqualFunctionID(int a)
        {
            if (a > 0xFFFF || a < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "a");

            byte[] tmp = int2byteArray(a, 16);
            if (tmp[0] == function_ID[0] && tmp[1] == function_ID[1])
                return true;
            else
                return false;
        }

        private void refreshAllMembers()
        {

            //add all members to pointer list (it applies pointer not data copy)
            all_members = new List<byte[]>();
            all_members.Add(header_ID);
                        
            packet_length = int2byteArray(26 + (data==null?0:data.Length), 16);

            all_members.Add(packet_length);
            all_members.Add(sender_ID);
            all_members.Add(receiver_ID);
            all_members.Add(group_ID);
            all_members.Add(event_ID);
            all_members.Add(function_ID);
            all_members.Add(reserved_bytes);
            all_members.Add(segment_ID);
            if(data!=null)
                all_members.Add(data);

            byte[] tmp = new byte[] { 0x13 };
            for (int i = 1; i < all_members.Count; i++)
            {
                tmp = appendToByteArray(tmp, all_members[i]);
            }
            crc = int2byteArray(Compute_CRC16_Simple(tmp),16);

            all_members.Add(crc);
        }
        private void decode(byte[] raw_code)
        {
            //check input
            if(raw_code.Length<26)
                throw new System.ArgumentException("Array length is not enough!!", "raw_code");
            //

            header_ID[0] = raw_code[0];

            Array.Copy(raw_code, 1, packet_length, 0, 2);

            Array.Copy(raw_code, 3, sender_ID, 0, 6);

            Array.Copy(raw_code, 9, receiver_ID, 0, 6);

            group_ID[0] = raw_code[15];

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
                Console.WriteLine("CRC is correct!!");
            }
            else
            {
                throw new System.ArgumentException("Parameter CRC is not correct!!", "crc");
            }

        }
        

        private byte[] int2byteArray(int a, int type)
        {
            byte[] tmp;
            if (type == 16)
            {
                tmp = new byte[2];
                tmp = BitConverter.GetBytes(Convert.ToUInt16(a));
            }
            else if (type == 32)
            {
                tmp = new byte[4];
                tmp = BitConverter.GetBytes(Convert.ToUInt32(a));
            }
            else
            {
                tmp = new byte[2];//Dummy line
            }

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);

            return tmp;
        }

        private byte[] int2byteArray(Int64 a, int type)
        {
            byte[] tmp;
            if (type == 16)
            {
                tmp = new byte[2];
                tmp = BitConverter.GetBytes(Convert.ToUInt16(a));
            }
            else if (type == 32)
            {
                tmp = new byte[4];
                tmp = BitConverter.GetBytes(Convert.ToUInt32(a));
            }
            else
            {
                tmp = new byte[2];//Dummy line
            }

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);

            return tmp;
        }


        private byte[] hexString2byteArray(string str)
        {
            string[] hexValues = str.Split(' ');
            byte[] hexValuesByte = new byte[hexValues.Length];

            for (int i = 0; i < hexValues.Length; i++)
            {
                hexValuesByte[i] = Convert.ToByte(Convert.ToInt32(hexValues[i], 16));
            }
            return hexValuesByte;
        }
            
        private ushort byte2int(byte[] b)//b must be big-endian format and 2 bytes
        {
            byte[] tmp = new byte[b.Length];
            b.CopyTo(tmp, 0);
            Array.Reverse(tmp);
            return BitConverter.ToUInt16(tmp, 0);
        }

        
        

        private byte[] appendToByteArray(byte[] bArray, byte[] newByte)
        {
            byte[] newArray = new byte[bArray.Length + newByte.Length];
            bArray.CopyTo(newArray, 0);
            newByte.CopyTo(newArray, bArray.Length);
            return newArray;
        }

        private byte[] appendToByteArray(byte[] bArray, byte newByte)
        {
            byte[] newArray = new byte[bArray.Length + 1];
            bArray.CopyTo(newArray, 0);
            newArray[newArray.Length-1]=newByte;
            return newArray;
        }

        private bool checkCRC(byte[] d, byte[] receive_crc)
        {
            ushort ans = Compute_CRC16_Simple(d);
            ushort rc = byte2int(receive_crc);

            if (ans == rc)
                return true;
            else
                return false;
        }

        private ushort Compute_CRC16_Simple(byte[] bytes)
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

    public class HNAClients
    {
        
        public TcpClient cl;
        public List<string> msg = new List<string>(); //acts like a stack

        public TaiSEIA_Packet_structure send_code;
        private string USER_ID="0xA01";
        private string HG_ID = "1";
        private string HNA_ID="2";        
        private Thread eventThread=null;

        public HNAClients()
        {
        }
        public HNAClients(TcpClient a)
        {
            cl = a;
            send_code = new TaiSEIA_Packet_structure();
        }

        public void setUserID(string a) { USER_ID = a; }
        public string getUSERID() { return USER_ID; }
        public void setHNAID(string a) { HNA_ID = a; }
        public string getHNAID() { return HNA_ID; }
        public void setSendCode(TaiSEIA_Packet_structure a) { send_code = new TaiSEIA_Packet_structure(a); }
        
        public void clearMessage(){ msg.Clear(); }
        public string getMessage() { string tmp = msg[0]; msg.RemoveAt(0);  return tmp; }
        public int setMessage(string a){
            msg.Add(a);

            if (eventThread != null)
            {
                if (eventThread.IsAlive == true)
                    return -1;
            }            

            TaiSEIA_Packet_structure rcv_code = new TaiSEIA_Packet_structure(msg[0]);
            
            //Send Actions
            if (rcv_code.isEqualFunctionID(0x0100))
            { //Receive broadcast from HNA. Then start set ID process
                eventThread = new Thread(new ThreadStart(eventThread_SetIDProcess));
                eventThread.IsBackground = true;
                eventThread.Start();
            }


            msg.RemoveAt(0);//Client processed the msg, so clean it
            return 0;
        }

        public void send2Client(string msg)
        {
            try
            {
                NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
                stream = this.cl.GetStream(); //Gets The Stream of The Connection
                byte[] data; // creates a new byte without mentioning the size of it cuz its a byte used for sending

                string[] hexValues = msg.Split(' ');
                data = new byte[hexValues.Length];

                for (int j = 0; j < hexValues.Length; j++)
                {
                    data[j] = Convert.ToByte(Convert.ToInt32(hexValues[j], 16));
                }
                stream.Write(data, 0, data.Length); //Sends the real data
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public bool isContainMessage()
        {
            if (msg.Equals("") == true)
                return false;
            else
                return true;
        }

        private void eventThread_SetIDProcess()
        {
            send_code = new TaiSEIA_Packet_structure();
            send_code.setFunctionID(0xF0FF);
            send2Client(send_code.ToString()); //Send ACK


            string tmp_usrid = "0xA01";
            string tmp_hgid = "1";
            string tmp_hna_id = "2";

            send_code = new TaiSEIA_Packet_structure();
            send_code.setFunctionID(0x0102, new string[3] { Convert.ToInt64(tmp_usrid, 16).ToString(), tmp_hgid, tmp_hna_id });
            send2Client(send_code.ToString());

            while (msg.Count==0) { }//Wait for HNA's ACK msg

            TaiSEIA_Packet_structure rcv_code = new TaiSEIA_Packet_structure(msg[0]);
            if (rcv_code.isEqualFunctionID(0xF0FF))//Receive ACK
            {
                msg.RemoveAt(0);
            }
            else {
                //do something
            }

            while (msg.Count == 0) { }//Wait for HNA's msg

            rcv_code = new TaiSEIA_Packet_structure(msg[0]);
            if (rcv_code.isEqualFunctionID(0xF100))//Receive Set Success!!
            {
                msg.RemoveAt(0);
                send_code = new TaiSEIA_Packet_structure();
                send_code.setFunctionID(0xF0FF);
                send2Client(send_code.ToString()); //Send ACK
            }
            else
            {
                //do something
            }

            msg.Clear();
            eventThread.Abort();
        }
    }
}
