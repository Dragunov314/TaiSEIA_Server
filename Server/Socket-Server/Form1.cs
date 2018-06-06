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
        TaiSEIA_Server server_test;// Creates a TCP Listener To Listen to Any IPAddress trying to connect to the program with port 8080        

        public Form1()
        {
            InitializeComponent();            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            server_test = new TaiSEIA_Server(portBox.Text,txtLog,this);

            server_test.StartConnect();

            btnListen.Enabled = false;            
            cmdBox.Enabled = true;              
        }

        private void btnSend_Click(object sender, EventArgs e)
        {            
            server_test.ServerSend(txtSend.Text);
        }

        
    }
}
