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
        public string USER_ID;
        public string HG_ID;
        public List<HNAClients> clients; // Creates a TCP Client list

        private TcpListener server;


        public TaiSEIA_Server(string port, string usr = "8787", string hg = "1")
        {
            // Listen to any IP address with specific port
            server = new TcpListener(IPAddress.Any, Convert.ToInt32(port));

            // Initial a list of clients instance
            clients = new List<HNAClients>();

            USER_ID = usr;
            HG_ID = hg;
        }

        public void StartConnect()
        {
            server.Start(); // Starts Listening to Any IPAddress trying to connect to the program with port 8080
            Console.WriteLine("Waiting For Connection....");
            Thread t = new Thread(() => // Creates a New Thread 
            {
                while (true)
                {
                    HNAClients tmp = new HNAClients(server.AcceptTcpClient());

                    clients.Add(tmp);  //Waits for the Client To Connect
                    
                    if (clients[clients.Count - 1].cln_socket.Connected) // If you are connected
                    {
                        clients[clients.Count - 1].StartReceive();
                    }
                }

            });
            t.IsBackground = true;
            t.Start();
        }







        //Send message to specific client or all clients
        public void ServerSend(string msg, int target = -1)
        {
            if (target == -1) //Broadcast message for all clients
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].cln_socket.Connected)
                    {
                        try
                        {
                            NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
                            stream = clients[i].cln_socket.GetStream(); //Gets The Stream of The Connection
                            byte[] data; // creates a new byte without mentioning the size of it cuz its a byte used for sending

                            string[] hexValues = msg.Split(' ');
                            data = new byte[hexValues.Length];

                            for (int j = 0; j < hexValues.Length; j++)
                            {
                                data[j] = Convert.ToByte(Convert.ToInt32(hexValues[j], 16));
                            }
                            
                            clients[i].send2Client(data);
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

                if (clients[target].cln_socket.Connected)
                {
                    try
                    {
                        NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
                        stream = clients[target].cln_socket.GetStream(); //Gets The Stream of The Connection
                        byte[] data; // creates a new byte without mentioning the size of it cuz its a byte used for sending

                        string[] hexValues = msg.Split(' ');
                        data = new byte[hexValues.Length];

                        for (int j = 0; j < hexValues.Length; j++)
                        {
                            data[j] = Convert.ToByte(Convert.ToInt32(hexValues[j], 16));
                        }

                        clients[target].send2Client(data);//Send raw data
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }


        }
        

        //-----------------------------
    }
    public class TaiSEIA_G2N_Packet
    {
        /// <summary>
        /// Message Structure (26 + N bytes), communicaton packet between HG & HNA
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
        /// 
        /// ============================IMPORTANT!!==================================
        /// If you modified any members, you must call function refreshAllMembers()!!
        /// Otherwise crc, packet_length and some members will not be updated!!
        /// =========================================================================
        /// </summary>


        public byte[] header_ID = new byte[1];
        public byte[] packet_length = new byte[2];
        public byte[] sender_ID = new byte[6];
        public byte[] receiver_ID = new byte[6];
        public byte[] group_ID = new byte[1];
        public byte[] event_ID = new byte[2];
        public byte[] function_ID = new byte[2];
        public byte[] reserved_bytes = new byte[] { 0xFF, 0xFF };
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
        /// Construct an instance of TaiSEIA structure. Input parameters snd_usr_id,rcv_usr_id should be Int64 data type.        /// 
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
        public TaiSEIA_G2N_Packet(Int64 snd_usr_id, int snd_hg_id, Int64 rcv_usr_id, int rcv_hg_id, int grp_id, int evt_id, int fcn_id, string data_str = "", bool hasData = false)
        {
            //Check input
            if (snd_usr_id > 0xFFFFFFFF || snd_usr_id < 0x0000000000)
                throw new System.ArgumentException("Parameter is out of range", "snd_usr_id");
            if (snd_hg_id > 0xFFFF || snd_hg_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "snd_hg_id");
            if (rcv_usr_id > 0xFFFFFFFF || rcv_usr_id < 0x00000000)
                throw new System.ArgumentException("Parameter is out of range", "rcv_usr_id");
            if (rcv_hg_id > 0xFFFF || rcv_hg_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "rcv_hg_id");
            if (grp_id > 0xFF || grp_id < 0x00)
                throw new System.ArgumentException("Parameter is out of range", "grp_id");
            if (evt_id > 0xFFFF || evt_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "evt_id");
            if (fcn_id > 0xFFFF || fcn_id < 0x0000)
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
            Array.Copy(tmp, 0, receiver_ID, 0, 4);
            tmp = int2byteArray(rcv_hg_id, 16);
            Array.Copy(tmp, 0, receiver_ID, 4, 2);

            if (BitConverter.IsLittleEndian)
                group_ID[0] = BitConverter.GetBytes(Convert.ToUInt16(grp_id))[0];
            else
                group_ID[0] = BitConverter.GetBytes(Convert.ToUInt16(grp_id))[1];

            event_ID = int2byteArray(evt_id, 16);

            function_ID = int2byteArray(fcn_id, 16);

            //reserved bytes

            segment_ID = int2byteArray(0x0000, 16); //Default to 0

            // Assume no data

            //NEED TO WRITE CODE HERE!! ELSE DATA WILL NOT BE HANDLE!!!

            //

            //Generate CRC
            refreshAllMembers();

        }
        public TaiSEIA_G2N_Packet(TaiSEIA_G2N_Packet source)
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
        public TaiSEIA_G2N_Packet()
        {
            byte[] tmp = new byte[] { 0x13, 0x00, 0x1A, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                      0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x01, 0x01, 0x00, 0xFF, 0xFF,
                                      0x00, 0x00, 0xCA, 0x21 };
            //default broadcast code

            decode(tmp);
            refreshAllMembers();
        }

        public TaiSEIA_G2N_Packet(byte[] raw_code)
        {
            decode(raw_code);
            refreshAllMembers();
        }

        public void setFunctionID(int fcn_id, string[] cmd_data = null)
        {
            if (fcn_id > 0xFFFF || fcn_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "fcn_id");

            function_ID = int2byteArray(fcn_id, 16);

            if (this.isEqualFunctionID(0x0102))
            {
                if (cmd_data == null)
                    throw new System.ArgumentException("Data is null!! Requires DATA!!!", "cmd_data");
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
            else if (this.isEqualFunctionID(0x0201) || this.isEqualFunctionID(0x0203))
            {
                if (cmd_data == null)
                    throw new System.ArgumentException("Data is null!! Requires DATA!!!", "cmd_data");
                else if (cmd_data.Length != 1)
                    throw new System.ArgumentException("Data Length is not match!!", "cmd_data");
                else
                {
                    data = new byte[1];
                    int tmp = Convert.ToInt32(cmd_data[0]);
                    if (tmp > 4 || tmp < 0)
                        throw new System.ArgumentException("Parameter is out of range", "cmd_data");
                    data[0] = Convert.ToByte(tmp);
                }
            }
            else if (this.isEqualFunctionID(0x0400)|| this.isEqualFunctionID(0x0005))
            {
                if (cmd_data == null)
                    throw new System.ArgumentException("Data is null!! Requires DATA!!!", "cmd_data");

                byte[] tmp_dt = hexString2byteArray(cmd_data[0]);
                data = new byte[tmp_dt.Length];
                Array.Copy(tmp_dt, 0, data, 0, data.Length);
            }
            else
            {
                data = null;//Clear data
            }

            refreshAllMembers();
        }

        public void setSenderID(string usr, string hg)
        {
            Int64 snd_usr_id = Convert.ToInt64(usr);
            int snd_hg_id = Convert.ToInt16(hg);
            if (snd_usr_id > 0xFFFFFFFF || snd_usr_id < 0x0000000000)
                throw new System.ArgumentException("Parameter is out of range", "snd_usr_id");
            if (snd_hg_id > 0xFFFF || snd_hg_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "snd_hg_id");
            Array.Copy(int2byteArray(snd_usr_id, 32), 0, sender_ID, 0, 4);
            Array.Copy(int2byteArray(snd_hg_id, 16), 0, sender_ID, 4, 2);
            refreshAllMembers();
        }

        public void setReceiverID(string rcv, string hna)
        {
            Int64 rcv_usr_id = Convert.ToInt64(rcv);
            int rcv_hna_id = Convert.ToInt16(hna);
            if (rcv_usr_id > 0xFFFFFFFF || rcv_usr_id < 0x0000000000)
                throw new System.ArgumentException("Parameter is out of range", "rcv_usr_id");
            if (rcv_hna_id > 0xFFFF || rcv_hna_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "rcv_hna_id");
            Array.Copy(int2byteArray(rcv_usr_id, 32), 0, receiver_ID, 0, 4);
            Array.Copy(int2byteArray(rcv_hna_id, 16), 0, receiver_ID, 4, 2);
            refreshAllMembers();
        }
        public void setEventID(int evt_id)
        {
            if (evt_id > 0xFFFF || evt_id < 0x0000)
                throw new System.ArgumentException("Parameter is out of range", "evt_id");
            event_ID = int2byteArray(evt_id, 16);
        }
        public TaiSEIA_G2N_Packet(string hexstr)
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

            packet_length = int2byteArray(26 + (data == null ? 0 : data.Length), 16);

            all_members.Add(packet_length);
            all_members.Add(sender_ID);
            all_members.Add(receiver_ID);
            all_members.Add(group_ID);
            all_members.Add(event_ID);
            all_members.Add(function_ID);
            all_members.Add(reserved_bytes);
            all_members.Add(segment_ID);
            if (data != null)
                all_members.Add(data);

            byte[] tmp = new byte[] { 0x13 };
            for (int i = 1; i < all_members.Count; i++)
            {
                tmp = appendToByteArray(tmp, all_members[i]);
            }
            crc = int2byteArray(Compute_CRC16_Simple(tmp), 16);

            all_members.Add(crc);
        }
        private void decode(byte[] raw_code)
        {
            //check input
            if (raw_code.Length < 26)
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
            data = new byte[packet_len - 26];
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
                //Console.WriteLine("TaiSEIA_G2N_Packet construction succeed : CRC is correct!!");
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

        public static ushort byte2int(byte[] b)//b must be big-endian format and 2 bytes
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
            newArray[newArray.Length - 1] = newByte;
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

    public class TaiSEIA_N2S_Packet
    {
        /// <summary>
        /// HNA 2 SA Packet Structure
        /// 
        ///   請求封包結構:
        ///   Byte          Name                                  Description
        ///    0    |    Packet Length   | The length of whole packet, including packet length byte itself. (0-5, 255 is reserved)
        ///    1    |       SA ID        | Smart appliance's identification number, representing which type of appliance.
        ///    2    |  R/W & service ID  | bit 7 : Read(0)/Write(1) packet, bit 0~6 : Service ID that SA is capable of.
        ///    3    |        MSB         | Most significant byte of data
        ///    4    |        LSB         | Least significant byte of data
        ///    5    |     Check Byte     | Bit operation XOR from 0~4 byte and the result is Check Byte.
        ///    
        /// 
        /// ============================IMPORTANT!!==================================
        /// If you modified any members, you must call function refreshAllMembers()!!
        /// Otherwise packet_length and Check Byte will not be updated!!
        /// =========================================================================
        /// </summary>
        public byte packet_length;
        public byte SA_ID;
        public byte service_ID;
        public byte[] data;
        public byte check_code;

        public TaiSEIA_N2S_Packet()
        {
            //Creates a dummy instance
            packet_length = 0x06;
            SA_ID = 0x01;
            service_ID = 0x00;
            data = new byte[2] { 0x00, 0x01 };
            check_code = 0x06;
        }

        public TaiSEIA_N2S_Packet(byte[] input)
        {
            packet_length = input[0];
            SA_ID = input[1];
            service_ID = input[2];
            data = new byte[input.Length-4];
            Array.Copy(input, 3, data, 0, data.Length);
            check_code = input[input.Length - 1];
            checkCheckCode();
        }
        public void setSA_ID(int a)
        {
            //0x00-0xFF
            if (a > 0xFF || a<0x00)
                throw new System.ArgumentException("Parameter is out of range", "a");
            SA_ID = Convert.ToByte(a);
            refreshAllMembers();
        }
        public void setService_ID(int a,byte[] dt=null)
        {
            //0x00-0xFF
            if (a > 0xFF || a < 0x00)
                throw new System.ArgumentException("Parameter is out of range", "a");
            service_ID = Convert.ToByte(a);

            if (dt != null)
            {
                data = new byte[dt.Length];
                Array.Copy(dt, 0, data, 0, dt.Length);
            }
            refreshAllMembers();
        }
        public byte[] ToByteArray()
        {
            byte[] btArray = new byte[4+data.Length];
            btArray[0] = packet_length;
            btArray[1] = SA_ID;
            btArray[2] = service_ID;
            Array.Copy(data, 0, btArray, 3, data.Length);
            btArray[btArray.Length - 1] = check_code;
            return btArray;
        }
        public override string ToString()
        {
            string tmp = packet_length.ToString("X2");
            tmp += " " + SA_ID.ToString("X2");
            tmp += " " + service_ID.ToString("X2");

            for (int i = 0; i < data.Length; i++)
            {
                tmp += " " + data[i].ToString("X2");
            }

            tmp += " " + check_code.ToString("X2");
            return tmp;
        }

        private void refreshAllMembers()
        {
            packet_length = Convert.ToByte(4+data.Length);
            byte[] tmp = new byte[packet_length - 1];
            Array.Copy(ToByteArray(), 0, tmp, 0, tmp.Length);            
            check_code = Compute_check_code_XOR(tmp);
        }

        private bool checkCheckCode()
        {
            byte[] b = new byte[packet_length - 1];
            Array.Copy(ToByteArray(), 0, b, 0, b.Length);
            byte a = Compute_check_code_XOR(b);
            if (a == b[b.Length - 1])
            {
                //Console.WriteLine("SA Check Code is correct!!");
                return true;
            }
            else
            {
                Console.WriteLine("ERROR : SA Check Code is INCORRECT!!");
                return false;
            }
            
        }
        public static byte Compute_check_code_XOR(byte[] x)
        {
            byte result = Convert.ToByte(x[0]^x[1]);
            for (int i = 2; i < x.Length; i++)
            {
                result = Convert.ToByte(result ^ x[i]);
            }
            return result;
        }
    }

    public class HNAClients
    {
        private static Mutex mut = new Mutex();

        public int current_event = 1;

        public TcpClient cln_socket;
        public List<string> rcv_cmd = new List<string>(); //acts like a queue
        public List<int> support_security_type = new List<int>();
        public Dictionary<int, Func<int,int>> func_table = new Dictionary<int, Func<int,int>>();
        public Smart_Appliance paired_SA;
        

        public byte[] TaiSEIA_protocol_version = new byte[2];
        public byte[] HNA_firmware_version = new byte[2];
        public List<byte[]> support_function = new List<byte[]>();
        public string HNA_brand;
        public string HNA_model;

        public TaiSEIA_G2N_Packet send_code;
        public TaiSEIA_G2N_Packet rcv_code;
                
        private string USER_ID= Convert.ToInt64("0xA01",16).ToString(); //0x0A01 = (Decimal)2561
        private string HG_ID = "1";
        private string HNA_ID="2";
        private string Security_Type = "0";   
        private Thread eventThread=null;
        private bool isClientDataSetted = true;//IMPORTANT:TURN TO FALSE IF YOU WANT TO INPUT YOURSELF!!!
        private bool isInAction = false;

        public HNAClients()
        {
        }
        public HNAClients(TcpClient a)
        {
            cln_socket = a;
            send_code = new TaiSEIA_G2N_Packet();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nConnected To Client!!");
            Console.WriteLine("===============Connection Information======================\n");            
            Console.WriteLine("IP Address     : " + IPAddress.Parse(((IPEndPoint)a.Client.LocalEndPoint).Address.ToString()));
            Console.WriteLine("Port Number    : " + ((IPEndPoint)a.Client.LocalEndPoint).Port.ToString());
            Console.WriteLine("Start Time     : " + DateTime.Now.ToString(new CultureInfo("en-US")));
            Console.WriteLine("Address Family : " + a.Client.AddressFamily.ToString());
            Console.WriteLine("SocketType     : " + a.Client.SocketType.ToString());
            Console.WriteLine("ProtocolType   : " + a.Client.ProtocolType.ToString());
            Console.WriteLine("\n===========================================================");
            Console.ResetColor();

            //Add function table             
            func_table.Add(0x0005, func_0x0005);
            func_table.Add(0x0100, func_0x0100);
            func_table.Add(0x0201, func_0x0201);
            func_table.Add(0x0301, func_0x0301);
            func_table.Add(0x0303, func_0x0303);
            func_table.Add(0x0400, func_0x0400);
            func_table.Add(0xF0FF, func_0xF0FF);
            func_table.Add(0xF100, func_0xF100);
            func_table.Add(0xF101, func_0xF101);
            func_table.Add(0xF102, func_0xF102);
            func_table.Add(0xF103, func_0xF103);
            func_table.Add(0xF110, func_0xF110);
            func_table.Add(0xF131, func_0xF131);
        }
        
        public void setUserID(string a) { USER_ID = a; }
        public string getUSERID() { return USER_ID; }
        public void setHNAID(string a) { HNA_ID = a; }
        public string getHNAID() { return HNA_ID; }
        public void setSendCode(TaiSEIA_G2N_Packet a) { send_code = new TaiSEIA_G2N_Packet(a); }
        public void setSecurityType(string a) { Security_Type = a; }
        public string getSecurityType() { return Security_Type; }

        public void setClientData(string a,string b,string c, string d)
        {
            USER_ID = a;
            HG_ID = b;
            HNA_ID = c;
            Security_Type = d;
            isClientDataSetted = true;
        }

        public void send2Client(string msg)
        {
            try
            {
                NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
                stream = this.cln_socket.GetStream(); //Gets The Stream of The Connection
                send_code = new TaiSEIA_G2N_Packet(msg);
                byte[] tmp = send_code.ToByteArray();
                stream.Write(tmp, 0, tmp.Length); //Sends the real data

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                DateTime localDate = DateTime.Now;
                String timestamp = localDate.ToString(new CultureInfo("en-US"));
                Console.WriteLine("\n" + timestamp + " | Outgoing to Client " +
                                   Convert.ToInt32(USER_ID).ToString() + " : " + send_code.ToString());

                Console.WriteLine(timestamp + " | Event " + TaiSEIA_G2N_Packet.byte2int(send_code.event_ID) +
                                              ", Function ID : 0x" + Convert.ToInt32(send_code.function_ID[0]).ToString("X2") +
                                              ", Sub-function ID : 0x" + Convert.ToInt32(send_code.function_ID[1]).ToString("X2"));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("HG is sending Function ID {0}{1} \n", tmp[18].ToString("X2"), tmp[19].ToString("X2"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void send2Client(byte[] msg)
        {
            try
            {
                NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
                stream = this.cln_socket.GetStream(); //Gets The Stream of The Connection                
                stream.Write(msg, 0, msg.Length); //Sends the real data

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                DateTime localDate = DateTime.Now;
                String timestamp = localDate.ToString(new CultureInfo("en-US"));
                Console.WriteLine("\n" + timestamp + " | Outgoing to Client " +
                                   Convert.ToInt32(USER_ID).ToString() + " : " + send_code.ToString());

                Console.WriteLine(timestamp + " | Event " + TaiSEIA_G2N_Packet.byte2int(send_code.event_ID) +
                                              ", Function ID : 0x" + Convert.ToInt32(send_code.function_ID[0]).ToString("X2") +
                                              ", Sub-function ID : 0x" + Convert.ToInt32(send_code.function_ID[1]).ToString("X2"));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("HG is sending Function ID {0}{1} \n",msg[18].ToString("X2"), msg[19].ToString("X2"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void StartReceive()
        {
            Thread t = new Thread(new ThreadStart(receiveClientData));
            t.IsBackground = true;
            t.Start();
            eventThread = new Thread(new ThreadStart(eventThread_SetupProcess));
            eventThread.IsBackground = true;
            eventThread.Start();
        }

        public void sendCMD2SA(int fcn)
        {
            isInAction = true;
            try {
                func_table[fcn](1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error during sending cmd: " + e.ToString());
            }
            
            isInAction = false;
        }

        private void receiveClientData()
        {
            try
            {                
                while (true)
                {
                    while (cln_socket.GetStream().DataAvailable && isClientDataSetted)
                    {
                        lock (new object())
                        {
                            byte[] datalength = new byte[3];
                            cln_socket.GetStream().Read(datalength, 0, 3);
                            byte[] data = new byte[Convert.ToInt32(datalength[2])];
                            for (int j = 0; j < 3; j++)
                                data[j] = datalength[j];

                            cln_socket.GetStream().Read(data, 3, data.Length - 3); //Receives The Real Data not the Size

                            string message = "";
                            for (int j = 0; j < data.Length; j++)
                            {
                                if (j != data.Length - 1)
                                    message = message + Convert.ToInt32(data[j]).ToString("X2") + " ";
                                else
                                    message = message + Convert.ToInt32(data[j]).ToString("X2");
                            }

                            //Message string output   
                            Console.ForegroundColor = ConsoleColor.DarkCyan;

                            DateTime localDate = DateTime.Now;
                            String timestamp = localDate.ToString(new CultureInfo("en-US"));
                            Console.WriteLine("\n" + timestamp + " | Incoming from Client " + Convert.ToInt32(USER_ID).ToString() + " : " + message);

                            rcv_code = new TaiSEIA_G2N_Packet(data);
                            rcv_cmd.Add(message);                            

                            Console.WriteLine(timestamp + " | Event " + TaiSEIA_G2N_Packet.byte2int(rcv_code.event_ID) +
                                              ", Function ID : 0x" + Convert.ToInt32(rcv_code.function_ID[0]).ToString("X2") +
                                              ", Sub-function ID : 0x" + Convert.ToInt32(rcv_code.function_ID[1]).ToString("X2"));                            
                        }
                        

                        respond2rcv_cmd();// This method will modify rcv_code values, use it carefully!!
                    }
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR in receiveClientData() : "+e.Message + "\n");
            }
        }

        /// <summary>
        /// This method will handle with receive data. Once the socket receive data, 
        /// this method will add string message to rcv_cmd queue and create a TaiSEI packet structure temporary instance.
        /// After processing the receive data, DELETE rcv_cmd's first index.
        /// </summary>
        private void respond2rcv_cmd()
        {
            if (eventThread.IsAlive == false && isInAction == false)
            {
                Console.ResetColor();
                Console.Write("TaiSEIA Server>");
                rcv_cmd.RemoveAt(0);//remove first index after processed the msg
            }
        }


        private void sendFCNCode(int evt_id, int fcn_id, bool isIDrequired = false,string[] dt = null)
        {
            send_code = new TaiSEIA_G2N_Packet();

            send_code.setEventID(evt_id);

            if (isIDrequired)
            {
                //Set ID
                send_code.setSenderID(USER_ID, HG_ID);
                send_code.setReceiverID(USER_ID, HNA_ID);
            }

            if (dt != null)
                send_code.setFunctionID(fcn_id, dt);
            else
                send_code.setFunctionID(fcn_id);
                        
            send2Client(send_code.ToByteArray());

            //HG should wait for ACK after all success sending
            //Wait for ACK from HNA
            if (fcn_id != 0xF0FF)
            {
                if (waitForCode(0xF0FF) == -1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Timeout : HNA has no response, resending message in 3 seconds...");
                    Task.Delay(3000);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Resending message to HNA...");
                    sendFCNCode(evt_id, fcn_id, isIDrequired, dt);
                }
                else
                {
                    
                }
            }                

        }

        /// <summary>
        /// USE ONLY FOR EVENT THREAD!!!
        /// wait_time_max unit : millisecond
        /// </summary>
        private int waitForCode(int fcn_code, double wait_time_max = 5000)
        {
            int status = 0;            
            DateTime time_before = DateTime.Now;
            double wait_time = 0;
            while (rcv_cmd.Count == 0 && wait_time <= wait_time_max) {
                wait_time = ((TimeSpan)(DateTime.Now - time_before)).TotalMilliseconds;                
            }//Wait for HNA's ACK msg
            
            Task.Delay(20);//MUST ADD!! Wait for rcv_cmd to store data...
            if (rcv_cmd == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR : rcv_cmd[0] is a null pointer!!");                
                return -1;
            }
            if (wait_time > wait_time_max)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR : Waiting time is larger than wait_time_max {0}", wait_time_max);                
                return -1;
            }

            TaiSEIA_G2N_Packet rcv_code = new TaiSEIA_G2N_Packet(rcv_cmd[0]);
            if (rcv_code.isEqualFunctionID(fcn_code)|| rcv_code.function_ID[0] == 0xF1)
            {
                //Respond to message here
                int cmd = TaiSEIA_G2N_Packet.byte2int(rcv_code.function_ID);
                try
                {                    
                    func_table[cmd](0); 
                    if(cmd!=0xF0FF)
                        sendFCNCode(current_event, 0xF0FF);//Send ACK
                    rcv_cmd.RemoveAt(0);//remove first index after processed the msg                    
                }
                catch (Exception e)
                {
                    sendFCNCode(current_event, 0xF0FF);//Send ACK
                    rcv_cmd.RemoveAt(0);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nERROR : " + e.ToString() + "\n");
                    Console.ResetColor();
                }
            }
            else
            {
                rcv_cmd.RemoveAt(0);//remove first index after processed the msg
                status = -1;//Failed to wait for desired function ID       
                string tmp = String.Format("\nERROR : Function CODE is no expected!! Desired {0} , Received {1}{2}",
                                  fcn_code.ToString("X4"), rcv_code.function_ID[0].ToString("X2"), rcv_code.function_ID[1].ToString("X2"));
                throw new System.ArgumentException(tmp);
            }
            
            return status;// 0: success, -1: some errors occurred
        }

        private void eventThread_SetupProcess() //Event 1
        {
            try
            {
                waitForCode(0x0100);                
                current_event = 1;                

                //Set HNA's ID & wait for ACK
                string[] tmp_str = new string[3] { USER_ID, HG_ID, HNA_ID };
                sendFCNCode(current_event, 0x0102, false, tmp_str);

                //Wait for HNA's Receive Set Success & Send ACK!!
                waitForCode(0xF100);

                Console.ForegroundColor = ConsoleColor.Green;                
                Console.WriteLine("\nEVENT 1 succeeded. Fowarding to event 2.");                
                Console.WriteLine("\nEVENT 2 : Security confirmation process is starting....");

                waitForCode(0x0201);
                
                current_event = 2;

                //Set Security Type & wait for ACK
                sendFCNCode(current_event, 0x0203, true, new string[] { Security_Type });

                //Wait for HNA's Receive Set Success & send ACK!!
                waitForCode(0xF100);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nEVENT 2 succeeded. Fowarding to event 3.");
                Console.WriteLine("\nEVENT 3 : Registration process starting....");
                current_event = 3;
                //Inform HNA to start registration & wait for ACK
                sendFCNCode(current_event, 0x0300, true);

                //Wait for HNA inform HNA's support function & send ACK
                waitForCode(0x0301);

                //Wait for HNA informing Smart Appliance's ability
                waitForCode(0x0303);

                //Wait for HNA's registration complete signal
                waitForCode(0xF100);

            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.ToString() + "\n");
                Console.WriteLine("\nEVENT "+current_event+" : FAILED and terminating...\n");
                Console.ResetColor();
                Console.Write("TaiSEIA Server>");

                rcv_cmd.Clear();
                eventThread.Abort();
            }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nEVENT 3 Succeeded! Event thread is terminating...\n");
            Console.ResetColor();
            Console.Write("TaiSEIA Server>");

            rcv_cmd.Clear();
            eventThread.Abort();

        }       

        /// <summary>
        /// USE ONLY IN EVENT THREAD!!!!
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>

        private int func_0x0005(int a)  //Reset function
        {
            if (a == 1)//Send
            {                
                current_event = 5;
                sendFCNCode(current_event, 0x0005, true, new string[] {"FF FF FF FF"});
                waitForCode(0xF131);
            }
            return 0;
        }
        private int func_0x0100(int a)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nEVENT 1 : Setup ID process is starting....");
            return 0;
        }
        private int func_0x0201(int a)
        {
            //add support sercurity type
            for (int i = 0; i < rcv_code.data.Length; i++)
            {
                support_security_type.Add(rcv_code.data[0]);
            }            
            return 0;
        }
        private int func_0x0301(int a)
        {
            //process data                    
            byte[] tmp = rcv_code.data;
            Array.Copy(tmp, 0, TaiSEIA_protocol_version, 0, 2);
            Array.Copy(tmp, 2, HNA_firmware_version, 0, 2);

            int[] null_index = new int[2];
            int j = 0;
            for (int i = 6; i < 46; i++)
            {
                if (tmp[i] == 0x00)
                {
                    null_index[j] = i;

                    if (j == 1)
                        break;

                    j++;
                }
            }

            byte[] tmp2 = new byte[null_index[0] - 6];
            Array.Copy(tmp, 6, tmp2, 0, null_index[0] - 6);
            HNA_brand = System.Text.Encoding.ASCII.GetString(tmp2);

            tmp2 = new byte[null_index[1] - null_index[0] - 1];
            Array.Copy(tmp, null_index[0] + 1, tmp2, 0, null_index[1] - null_index[0] - 1);
            HNA_model = System.Text.Encoding.ASCII.GetString(tmp2);

            for (int i = null_index[1] + 1; i < tmp.Length; i += 2)
            {
                byte[] tmp3 = new byte[2];
                Array.Copy(tmp, i, tmp3, 0, 2);
                support_function.Add(tmp3);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("HNA informed its supporting functions!");
            return 0;
        }

        private int func_0x0303(int a)
        {
            //Store SA DATA CODE HERE...

            if (rcv_code.data[7] == 1)
            {                
                paired_SA = new Electric_Fan(rcv_code.data);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("SA Electric Fan paired!!");
            }
            else {
                paired_SA = new Smart_Appliance(rcv_code.data);
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("HNA informed SA's supporting functions!");
            return 0;
        }

        private int func_0x0400(int a)//1: Send, 0: Receive
        {

            // Test CODE
            if (a == 1)
            {                
                paired_SA.ActionCode(new byte[] { 0 });
                current_event = 5;
                sendFCNCode(current_event, 0x400, true, new string[] { paired_SA.msg_code.ToString() });                
                waitForCode(0x0400);
            }
            if (a == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("HNA sent monitored-device responding packet");
            }
            

            return 0;
        }
        private int func_0xF0FF(int a)
        {
            if (a == 0)
            {
                //Success received ACK
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ACK received from HNA.");
            }            
            return 0;
        }

        private int func_0xF100(int a) 
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("The function just sent by HG succeeded executing or was agreed by HNA!");            
            return 0;
        }
        private int func_0xF101(int a)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HNA sent header ID error");
            return 0;
        }
        private int func_0xF102(int a)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HNA sent packet length error");
            return 0;
        }
        private int func_0xF103(int a)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HNA sent CRC-is-not-correct error");
            return 0;
        }
        private int func_0xF110(int a)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HNA doesn't support this function ID!!");
            return 0;
        }
        private int func_0xF131(int a)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("HNA is going to execute command..");
            return 0;
        }

        //-----------
    }

    /// <summary>
    /// This is a base class of all smart appliances. 
    /// DO NOT CREATE AN INSTANCE OF THIS CLASS!!
    /// </summary>
    public class Smart_Appliance
    {
        public TaiSEIA_N2S_Packet msg_code;
        public byte SA_ID;
        public List<byte[]> support_function = new List<byte[]>();
        public string Brand;
        public string Model;
        public int data_type; //0:一般資料型態, 1:多位元組資料型態
        public int[] TaiSEIA_Device_Monitor_Version = new int[2];
        public int Baud_Rate;
        public int Device_Type;//0:Home appliance, 1:Power generator, 2:Energy storing, 3:Sensor

        private int[] BR_Table = new int[] {1200,2400,4800,9600,19200,38400,57600};


        public Smart_Appliance(byte[] regist_msg)
        {
            //Deal with the Registration Responding Packet(註冊回應封包) here.

            // Check check code
            byte[] tmp2 = new byte[Convert.ToInt32(regist_msg[0]) - 1];
            Array.Copy(regist_msg, 0, tmp2, 0, tmp2.Length);
            byte cc = TaiSEIA_N2S_Packet.Compute_check_code_XOR(tmp2);
            if (cc == regist_msg[regist_msg.Length - 1])
            {
                //Console.WriteLine("Registration Responding Packet check code is correct!!");

                byte tmp = regist_msg[2];
                data_type = (tmp & Convert.ToInt32("1000000", 2)) >> 7;// bit 7 is data type, bit 4-6 is reserved
                Device_Type = tmp & Convert.ToInt32("00001111", 2);   // bit 0-3 is device type

                TaiSEIA_Device_Monitor_Version[0] = regist_msg[3]; //Main verseion
                TaiSEIA_Device_Monitor_Version[1] = regist_msg[4]; //Sub verseion

                tmp = regist_msg[5];
                Baud_Rate = BR_Table[(tmp & Convert.ToInt32("00001111", 2))];

                //bit 4-7 segment code order ASSUME NO SEGMENT

                //Set SA ID
                SA_ID = regist_msg[7];

                int[] null_index = new int[2];
                int j = 0;
                for (int i = 8; i < 28; i++)
                {
                    if (regist_msg[i] == 0x00)
                    {
                        null_index[j] = i;

                        if (j == 1)
                            break;

                        j++;
                    }
                }

                //Set Brand
                tmp2 = new byte[null_index[0] - 8];
                Array.Copy(regist_msg, 8, tmp2, 0, tmp2.Length);
                Brand = System.Text.Encoding.ASCII.GetString(tmp2);

                //Set Model
                tmp2 = new byte[null_index[1] - null_index[0] - 1];
                Array.Copy(regist_msg, null_index[0] + 1, tmp2, 0, tmp2.Length);
                Model = System.Text.Encoding.ASCII.GetString(tmp2);

                //Add support function
                for (int i = null_index[1] + 1; i < regist_msg.Length - 1; i += 3)
                {
                    byte[] tmp3 = new byte[3];
                    Array.Copy(regist_msg, i, tmp3, 0, tmp3.Length);
                    support_function.Add(tmp3);
                }

                msg_code = new TaiSEIA_N2S_Packet();
                msg_code.setSA_ID(SA_ID);                          
            }
            else
            {
                Console.WriteLine("Registration Responding Packet check code is INCORRECT!!");
                Console.WriteLine("Smart Appliance Class construction failed!!");
            }                       
        }
        virtual public void ActionCode(byte[] cmd)
        {
            //Leave to each SA class to complete the definition
        }            
    }

    public class Electric_Fan : Smart_Appliance
    {
        public Electric_Fan(byte[] reg):base(reg)
        {

        }
        override public void ActionCode(byte[] cmd)
        {
            //Define SA service actions here

            if (cmd[0] == 0)//Turn off
            {
                byte[] dt = new byte[] { 00, 00 };
                msg_code.setService_ID(0x80, dt);
            }
            if (cmd[0] == 1)//Turn on
            {
                byte[] dt = new byte[] { 00, 01 };
                msg_code.setService_ID(0x80, dt);
            }

        }
    }
}
