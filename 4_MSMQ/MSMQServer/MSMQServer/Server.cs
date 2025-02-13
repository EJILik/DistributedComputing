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
using System.Messaging;
using System.Xml.Linq;
using System.IO;

namespace MSMQ
{
    public partial class frmMain : Form
    {
        private MessageQueue incomingQ = null;
        //private MessageQueue outgoingQ = null;  // очередь сообщений
        private Thread t = null;                // поток, отвечающий за работу с очередью сообщений
        private bool _continue = true;          // флаг, указывающий продолжается ли работа с мэйлслотом
        private MessageQueue outgoingQ;
        private List<string> Clients = new List<string>();
        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            string path = Dns.GetHostName() + "\\private$\\ServerQueue";    // путь к очереди сообщений, Dns.GetHostName() - метод, возвращающий имя текущей машины

            // если очередь сообщений с указанным путем существует, то открываем ее, иначе создаем новую
            if (MessageQueue.Exists(path))
                incomingQ = new MessageQueue(path);
            else
                incomingQ = MessageQueue.Create(path);

            // задаем форматтер сообщений в очереди
            incomingQ.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

            // вывод пути к очереди сообщений в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Text += "     " + incomingQ.Path;

            // создание потока, отвечающего за работу с очередью сообщений
            t = new Thread(ReceiveMessage);
            t.Start();
        }

        // получение сообщения
        private void ReceiveMessage()
        {
            if (incomingQ == null)
                return;
            
            System.Messaging.Message msg = null;

            // входим в бесконечный цикл работы с очередью сообщений
            while (_continue)
            {
                if (incomingQ.Peek() != null)   // если в очереди есть сообщение, выполняем его чтение, интервал до следующей попытки чтения равен 10 секундам
                    msg = incomingQ.Receive(TimeSpan.FromSeconds(1.0));
                if (msg.Label.Contains("`"))
                {
                    string client = msg.Label;
                    client = client.Replace("`", "");
                    Clients.Add(client);
                }
                SendMessage(msg,msg.Label.Replace("`", ""), Clients);
                rtbMessages.Invoke((MethodInvoker)delegate
                {
                    if (msg != null)
                        rtbMessages.Text += "\n >> " + msg.Label + " : " + msg.Body;     // выводим полученное сообщение на форму
                });
                Thread.Sleep(500);          // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
            }
        }
        private void SendMessage(System.Messaging.Message msg, string sender, List<string> clients)
        {   
            foreach (string client in clients)
            {
                rtbMessages.Invoke((MethodInvoker)delegate
                {
                    if (msg != null)
                        rtbMessages.Text += "\n"+@".\private$\" + client;     // выводим полученное сообщение на форму
                });
                //MessageQueue outgoingQ = new MessageQueue(@".\private$\" + client);

                if (MessageQueue.Exists(@".\private$\" + client))
                {
                    outgoingQ = new MessageQueue(@".\private$\" + client);
                }
                else
                {
                    outgoingQ = MessageQueue.Create(@".\private$\" + client);
                }
                outgoingQ.Send(msg, sender);
            }

        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с очередью сообщений завершена

            if (t != null)
            {
                t.Abort();          // завершаем поток
            }

            if (incomingQ != null)
            {
                MessageQueue.Delete(incomingQ.Path);      // в случае необходимости удаляем очередь сообщений
            }
        }
    }
}