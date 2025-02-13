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
using System.Messaging;
using System.Threading;
using System.Xml.Linq;

namespace MSMQ
{
    public partial class frmMain : Form
    {
        private MessageQueue outgoingQ = null;      // очередь сообщений, в которую будет производиться запись сообщений
        private MessageQueue incomingQ = null;      // очередь сообщений, в которую будет производиться запись сообщений
        // конструктор формы
        string MyName;
        private Thread t = null;
        private bool _continue;
        public frmMain()
        {
            InitializeComponent(); 
            
            tbPath.Visible = false;
            btnConnect.Visible = false;
            lbPath.Visible = false;

            btnSend.Visible = false;
            rtbChat.Visible = false;
            tbMessage.Visible = false;
            lbMessage.Visible = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            
            if (MessageQueue.Exists(tbPath.Text))
            {
                // если очередь, путь к которой указан в поле tbPath существует, то открываем ее
                outgoingQ = new MessageQueue(tbPath.Text);

                InitializeComponent();
                string path = Dns.GetHostName() + "\\private$\\" + MyName;    // путь к очереди сообщений, Dns.GetHostName() - метод, возвращающий имя текущей машины

                // если очередь сообщений с указанным путем существует, то открываем ее, иначе создаем новую
                //if (MessageQueue.Exists(path))
                    incomingQ = new MessageQueue(path);
                //else
                    //incomingQ = MessageQueue.Create(path);

                // задаем форматтер сообщений в очереди
                incomingQ.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

                //SendMessage(" присоединился к беседе");
                outgoingQ.Send(" присоединился к беседе", "`"+MyName);
                //SendMessage(".\\private$\\" + MyName);

                t = new Thread(ReceiveMessage);
                t.Start();
                btnConnect.Enabled = false;
                btnSend.Enabled = true;
                btnSend.Enabled = true;
                rtbChat.Visible = true;
                btnSend.Visible = true;
                btnConnect.Visible = true;
                lbPath.Visible = true;
                lbMessage.Visible = true;
                tbPath.Visible = true;
                tbMessage.Visible = true;
            }
            else
                MessageBox.Show("Указан неверный путь к очереди, либо очередь не существует");
        }
        private void ReceiveMessage()
        {
            if (incomingQ == null)
                return;

            System.Messaging.Message msg = null;

            try
            {
                while (!_continue)
                {
                    if (incomingQ.Peek() != null)
                        msg = incomingQ.Receive(TimeSpan.FromSeconds(10.0));

                    if (_continue)
                        break;
                    string result = (string)msg.Body;

                    rtbChat.Invoke((MethodInvoker)delegate
                    {

                        if (msg != null)
                            rtbChat.Text += "\n >> " + msg.Label + " : " + msg.Body;
                    });
                    Thread.Sleep(500);
                }
            }
            catch (ThreadInterruptedException ex)
            {

            }
        }
        private void SendMessage(string msg)
        {
            outgoingQ.Send(msg, MyName);
        }       
        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage(tbMessage.Text);
            tbMessage.Text = "";
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnName_click(object sender, EventArgs e)
        {
            if (tbName.Text == null || tbName.Text == "")
            {
                MessageBox.Show("Введите ник");
            }
            else
            {
                MyName = tbName.Text;
                btnConnect.Visible = true;
                lbPath.Visible = true;
                tbPath.Visible = true;
                btnName.Enabled = false;
                tbName.ReadOnly = true;

            }
        }


    }
}