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

namespace Socket_Client
{
    public partial class Form1 : Form
    {
        int i;
        TcpClient client; // Creates a TCP Client
        NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data)
        byte[] datalength = new byte[4]; // creates a new byte with length 4 ( used for receivng data's lenght)
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                client = new TcpClient(IPAddressBox.Text, Convert.ToInt32(portBox.Text)); //Trys to Connect
                ClientReceive(); //Starts Receiving When Connected
                btnConnect.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // Error handler :D
            }
        }

        public void ClientReceive()
        {

            stream = client.GetStream(); //Gets The Stream of The Connection
            Thread t = new Thread(() => // Thread (like Timer)
            {
                while ((i = stream.Read(datalength, 0, 3)) != 0)//Keeps Trying to Receive the Size of the Message or Data
                {
                    byte[] data = new byte[Convert.ToInt32(datalength[2])];
                    for (int j = 0; j < 3; j++)
                        data[j] = datalength[j];

                    stream.Read(data, 3, data.Length - 3); //Receives The Real Data not the Size

                    string message = "";
                    foreach (byte data_byte in data)
                    {
                        message = message + Convert.ToInt32(data_byte).ToString("X2") + " ";
                    }

                    this.Invoke((MethodInvoker)delegate // To Write the Received data
                    {
                        DateTime localDate = DateTime.Now;
                        String timestamp = localDate.ToString(new CultureInfo("en-US"));
                        txtLog.AppendText(System.Environment.NewLine + timestamp + " | Server : " + message);
                        // Encoding.Default.GetString(data); Converts Bytes Received to String
                    });
                }
            });
            t.IsBackground = true;
            t.Start(); // Start the Thread
        }
        
        public void ClientSend(string msg)
        {
            stream = client.GetStream(); //Gets The Stream of The Connection
            byte[] data; // creates a new byte without mentioning the size of it cuz its a byte used for sending
            //data = Encoding.Default.GetBytes(msg); // put the msg in the byte ( it automaticly uses the size of the msg )
            
            string[] hexValues = msg.Split(' ');
            data = new byte[hexValues.Length];            

            for(int i=0;i<hexValues.Length;i++)
            {                
                data[i] = Convert.ToByte(Convert.ToInt32(hexValues[i], 16));
            }

            int length = data.Length; // Gets the length of the byte data
            byte[] datalength = new byte[4]; // Creates a new byte with length of 4
            datalength = BitConverter.GetBytes(length); //put the length in a byte to send it
            //stream.Write(datalength, 0, 4); // sends the data's length
            stream.Write(data, 0, data.Length); //Sends the real data
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (client.Connected) // if the client is connected
            {
                ClientSend(txtSend.Text); // uses the Function ClientSend and the msg as txtSend.Text
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
    }
}
