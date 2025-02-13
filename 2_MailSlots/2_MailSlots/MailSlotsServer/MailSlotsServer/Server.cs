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
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Xml.Linq;
using System.Data.Common;
using static System.Net.Mime.MediaTypeNames;

namespace MailSlots
{
    public partial class frmMain : Form
    {
        private int MyMailSlot;       // дескриптор мэйлслота
        private string ThisSlotName = "\\\\" + Dns.GetHostName() + "\\mailslot\\ServerMailslot";    // имя мэйлслота, Dns.GetHostName() - метод, возвращающий имя машины, на которой запущено приложение
        private Thread t;                       // поток для обслуживания мэйлслота
        private bool _continue = true;          // флаг, указывающий продолжается ли работа с мэйлслотом
        List<string> clients;
        string _lastMessage = string.Empty;

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            clients = new List<string>();
            clients = new List<string>();
            // создание мэйлслота
            MyMailSlot = DIS.Import.CreateMailslot("\\\\.\\mailslot\\ServerMailslot", 0, DIS.Types.MAILSLOT_WAIT_FOREVER, 0);

            // вывод имени мэйлслота в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Text += "     " + ThisSlotName;

            // создание потока, отвечающего за работу с мэйлслотом
            Thread t = new Thread(ReceiveMessage);
            t.Start();
        }
        private void ReceiveMessage()
        {
            string msg = "";
            int maxMessageSize = 0;
            int nextMessageSize = 0;
            int messagesCountInSlot = 0;
            uint realBytesRead = 0;

            try
            {
                while (_continue)
                {
                    DIS.Import.GetMailslotInfo(MyMailSlot,maxMessageSize,ref nextMessageSize,ref messagesCountInSlot,0);

                    for (int i = 0; i < messagesCountInSlot; i++)
                    {
                        byte[] buff = new byte[1024];DIS.Import.FlushFileBuffers(MyMailSlot);DIS.Import.ReadFile(MyMailSlot,buff,1024,ref realBytesRead,0);
                        msg = Encoding.Unicode.GetString(buff);
                        if (_lastMessage.Equals(msg))
                            continue;
                        bool needSending = true;
                        if (string.IsNullOrEmpty(msg))
                            break;

                        _lastMessage = msg;
                        if (msg[0]== '`')
                        {
                            msg = msg.Replace("`","");
                            if(!clients.Contains(msg))
                            {
                                clients.Add(msg);
                                rtbMessages.Invoke((MethodInvoker)delegate
                                {
                                    if (msg != "")
                                        rtbMessages.Text += "\n Подключился новый пользователь:" + msg;
                                });
                                needSending = false;
                            }
                            else
                            {
                                needSending = false;
                            }
                        }

                        rtbMessages.Invoke((MethodInvoker)delegate
                        {
                            if (msg != "")
                                rtbMessages.Text += msg;
                        });

                        if (needSending)
                        {
                            SendMessage(msg, clients);
                            

                        }// приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                        

                    }
                    Thread.Sleep(500);
                }
            }
            catch (ThreadInterruptedException ex)
            { }
        }
        private void SendMessage(string msg, List<string> clients)
        {
                
                foreach (string clientAdr in clients)
                {


                    rtbMessages.Invoke((MethodInvoker)delegate
                    {
                        //if (msg != "")
                            //rtbMessages.Text += @"\\.\mailslot\" + clientAdr;
                    });

                    //отправить сообщение обратно
                    int adrMailSlot = DIS.Import.CreateFile(@"\\.\mailslot\" + clientAdr, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);

                    if (adrMailSlot != -1)
                    {
                        uint bytesWritten = 0;
                        byte[] buff = Encoding.Unicode.GetBytes(msg);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт
                        DIS.Import.WriteFile(adrMailSlot, buff, Convert.ToUInt32(buff.Length), ref bytesWritten, 0);

                    }
                }
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с мэйлслотом завершена

            if (t != null)
                t.Abort();          // завершаем поток

            if (MyMailSlot != -1)
                DIS.Import.CloseHandle(MyMailSlot);            // закрываем дескриптор мэйлслота
        }
    }
}