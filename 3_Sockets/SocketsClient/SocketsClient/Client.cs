using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Sockets
{
    public partial class frmMain : Form
    {
        // клиентский сокет
        private TcpClient tcpClient = new TcpClient();
        // IP-адрес клиента
        private IPAddress IP;

        private int MyPort;
        private Socket MySocket;

        private TcpListener tcpListner;
        private const int AllownPort = 61931;
        private bool _continue = true;

        private Thread t = null;

        private string MyName = string.Empty;

        public frmMain()
        {
            InitializeComponent();

            btnSend.Enabled = true;
            rtbChat.Visible = false;
            btnSend.Visible = false;
            btnConnect.Visible = false;
            lblIP.Visible = false;
            lblMessage.Visible = false;
            tbIP.Visible = false;
            tbMessage.Visible = false;

            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            IP = hostEntry.AddressList[0];

            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }

            tbIP.Text = IP.ToString();
            tcpListner = new TcpListener(IP, 0);
            tcpListner.Start();

            MyPort = ((IPEndPoint)tcpListner.LocalEndpoint).Port;
            this.Text = "MyPort" + MyPort.ToString();

            t = new Thread(ReceiveMessage);
            t.Start();
        }
        
        private void SendMessage(string msg)
        {
            IPAddress IP = IPAddress.Parse(tbIP.Text);

            UdpClient udpClient = new UdpClient();

            IPEndPoint endPoint = new IPEndPoint(IP, AllownPort);
            byte[] buff = Encoding.Unicode.GetBytes(msg);
            try
            {
                udpClient.Send(buff, buff.Length, endPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            udpClient.Close();
        }

        private void ReceiveMessage()
        {
            string messageText = "";
            MySocket = tcpListner.AcceptSocket();
            try
            {
                while (_continue)
                {

                    byte[] buff = new byte[1024];
                    MySocket.Receive(buff);
                    messageText = System.Text.Encoding.Unicode.GetString(buff);

                    rtbChat.Invoke((MethodInvoker)delegate
                    {
                        if (messageText.Replace("\0", "") != "")
                            rtbChat.Text += messageText;
                    });
                    Thread.Sleep(500);
                }
            }
            catch (Exception exception) { }
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            MySocket.Close();
            tcpClient.Close();
            t.Abort();
            t.Join();
        }

        private void btnName_Click(object sender, EventArgs e)
        {
            if (tbName.Text == null || tbName.Text == "")
            {
                MessageBox.Show("Введите ник");
            }
            else
            {
                MyName = tbName.Text;
                btnConnect.Visible = true;
                lblIP.Visible = true;
                tbIP.Visible = true;
                btnName.Enabled = false;
                tbName.ReadOnly = true;
            }
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                SendMessage("`" + MyPort.ToString());
                SendMessage("\n>> " + MyName + " >> Вошёл в чат");
                btnConnect.Enabled = false;
                btnSend.Enabled = true;
                btnConnect.Enabled = false;
                btnSend.Enabled = true;
                rtbChat.Visible = true;
                btnSend.Visible = true;
                btnConnect.Visible = true;
                lblIP.Visible = true;
                lblMessage.Visible = true;
                tbIP.Visible = true;
                tbMessage.Visible = true;
            }
            catch
            {
                MessageBox.Show("Некорректный IP");
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage("\n>> "+MyName + " >> " + tbMessage.Text);
            tbMessage.Text = "";
        }
    }
}