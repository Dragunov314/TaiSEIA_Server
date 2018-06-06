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
using TaiSEIA;

namespace Socket_Server
{
    public partial class Form1 : Form
    {
        TcpListener server; // Creates a TCP Listener To Listen to Any IPAddress trying to connect to the program with port 1980        

        int clientNum = 0;
        List<TcpClient> clients;// Creates a TCP Client list

        TaiSEIA_Server server_test;

        public Form1()
        {
            InitializeComponent();
            clients = new List<TcpClient>();

            
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
            
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            server_test = new TaiSEIA_Server(portBox.Text);

            server_test.StartConnect();
            btnListen.Enabled = false;
            timer1.Enabled = true;
            cmdBox.Enabled = true;
            
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            //ServerSend(txtSend.Text); // uses the Function ClientSend and the msg as txtSend.Text            
            server_test.ServerSend(txtSend.Text);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < server_test.clients.Count; i++)
            {
                if (server_test.clients[i].isContainMessage())
                {
                    DateTime localDate = DateTime.Now;
                    String timestamp = localDate.ToString(new CultureInfo("en-US"));
                    txtLog.AppendText(timestamp + " | Client " + i + " : " + server_test.clients[i].getMessage()+Environment.NewLine);
                }
            }
        }
    }
}
