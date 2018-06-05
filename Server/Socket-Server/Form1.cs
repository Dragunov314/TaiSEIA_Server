using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

namespace Socket_Server
{
    public partial class Form1 : Form
    {
        TcpListener server; // Creates a TCP Listener To Listen to Any IPAddress trying to connect to the program with port 1980

        int clientNum = 0;
        List<TcpClient> clients;// Creates a TCP Client list

        public Form1()
        {
            InitializeComponent();
            clients = new List<TcpClient>();
        }
        
        public void ServerReceive(ref List<TcpClient> tmp)
        {
            int i;
            NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
            int no = tmp.Count - 1;
            stream = tmp[no].GetStream(); //Gets The Stream of The Connection
            Thread t = new Thread(() => // Thread (like Timer)
            {
                try
                {
                    byte[] datalength = new byte[3]; // creates a new byte with length 4 ( used for receivng data's length)
                    while ((i = stream.Read(datalength, 0, 3)) != 0)//Keeps Trying to Receive the Size of the Message or Data
                    {
                        // how to make a byte E.X byte[] examlpe = new byte[the size of the byte here] , i used BitConverter.ToInt32(datalength,0) cuz i received the length of the data in byte called datalength :D
                        //byte[] data = new byte[BitConverter.ToInt32(datalength, 0)]; // Creates a Byte for the data to be Received On
                        byte[] data = new byte[Convert.ToInt32(datalength[2])];
                        for (int j = 0; j < 3; j++)
                            data[j] = datalength[j];
                        stream.Read(data, 3, data.Length-3); //Receives The Real Data not the Size
                        string message = "";
                        foreach (byte data_byte in data)
                        {
                            message = message + Convert.ToInt32(data_byte).ToString("X2") + " ";
                        }
                        this.Invoke((MethodInvoker)delegate // To Write the Received data
                        {
                            DateTime localDate = DateTime.Now;
                            String timestamp = localDate.ToString(new CultureInfo("en-US")); 
                            txtLog.AppendText(System.Environment.NewLine + timestamp+" | Client : " + message);
                            // Encoding.Default.GetString(data); Converts Bytes Received to String
                        });
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            });
            t.IsBackground=true;
            t.Start(); // Start the Thread

        }
        public void checkStatus()
        {
            this.Invoke((MethodInvoker)delegate
            {
                onlineStatusBox.Items.Clear();
            });

            try
            {
                List<int> delete_index = new List<int>();
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].Connected == false)
                    {
                        delete_index.Add(i);
                    }
                }
                for (int i = 0; i < delete_index.Count; i++)
                {
                    clients.RemoveAt(delete_index[i]);
                }
                for (int i = 0; i < clients.Count; i++)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        onlineStatusBox.Items.Add("Client " + i, true);
                    });
                }
            }
            catch (Exception e)
            {
            }
            
        }

        public void ServerSend(string msg)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Connected)
                {
                    try
                    {
                        NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data) 
                        stream = clients[i].GetStream(); //Gets The Stream of The Connection
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

                    }
                }                
            }
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //onlineStatusBox.Items.Add("123", false);
            //onlineStatusBox.SetItemChecked(0, true);
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            server = new TcpListener(IPAddress.Any, Convert.ToInt32(portBox.Text));
            server.Start(); // Starts Listening to Any IPAddress trying to connect to the program with port 1980
            MessageBox.Show("Waiting For Connection");
            Thread t = new Thread(() => // Creates a New Thread (like a timer)
            {
                while (true)
                {
                    clients.Add(server.AcceptTcpClient());  //Waits for the Client To Connect                 

                    MessageBox.Show("Connected To Client");
                    if (clients[clients.Count - 1].Connected) // If you are connected
                    {
                        clientNum++;
                        ServerReceive(ref clients); //Start Receiving                                          
                    }
                }

            });
            t.IsBackground = true;
            t.Start();

            btnListen.Enabled = false;
            cmdBox.Enabled = true;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            ServerSend(txtSend.Text); // uses the Function ClientSend and the msg as txtSend.Text            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            checkStatus();
        }
    }
}
