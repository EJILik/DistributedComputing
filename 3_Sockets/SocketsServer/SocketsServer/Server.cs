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
using System.Threading;
using System.Collections;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;

namespace Sockets
{
    public partial class frmMain : Form
    {
        private TcpListener tcpListener;

        private Thread t;

        // флаг, указывающий продолжается ли работа с сокетами
        private bool _continue = true;
        private IPAddress MyIp;
        private List<TcpClient> tcpClients = new List<TcpClient>();

        private const int PORT = 61931;

        UdpClient updClient = new UdpClient(PORT);

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();

            // информация об IP-адресах и имени машины, на которой запущено приложение
            IPHostEntry hostEntity = Dns.GetHostEntry(Dns.GetHostName());

            // IP-адрес, который будет указан при создании сокета
            this.MyIp = hostEntity.AddressList[0];

            // определяем IP-адрес машины в формате IPv4
            foreach (IPAddress address in hostEntity.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    this.MyIp = address;
                    break;
                }

            this.Text += "`" + MyIp.ToString() + ":" + PORT.ToString();

            tbIP.Text = this.MyIp.ToString();
            

            tcpListener = new TcpListener(this.MyIp, PORT);
            tcpListener.Start();

            t = new Thread(ReceiveMessage);
            t.Start();

        }
        private void ReceiveMessage()
        {
            IPEndPoint IPEndPoint = null;
            bool Send = true;
            try
            {
                while (_continue)
                {
                    byte[] totalBytes = updClient.Receive(ref IPEndPoint);

                    string msg = Encoding.Unicode.GetString(totalBytes);

                    Send = true;
                    if (msg[0]=='`')
                    {
                        msg = msg.Replace("`", "");
                        TcpClient tcpClient = new TcpClient();
                        tcpClient.Connect(IPAddress.Parse(tbIP.Text), int.Parse(msg));
                        tcpClients.Add(tcpClient);
                        Send = false;
                    }

                    if (Send)
                        rtbMessages.Invoke((MethodInvoker)delegate
                        {
                            if (msg != "")
                            {
                                rtbMessages.Text += msg;
                            }
                            SendMessage(msg, tcpClients, totalBytes);
                        });
                }
            }
            catch (Exception exception) 
            {
            }
        }
        private void SendMessage(string msg, List<TcpClient> tcpClients, byte[] bytes)
        {
            foreach (TcpClient tcpClient in tcpClients)
            {
                byte[] buff = Encoding.Unicode.GetBytes(msg);  
                Stream stream = tcpClient.GetStream();                                                   
                stream.Write(buff, 0, bytes.Length);
            }

        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;
            updClient.Close();
            t.Abort();
            t.Join();
            tcpListener.Stop();
        }
    }
}