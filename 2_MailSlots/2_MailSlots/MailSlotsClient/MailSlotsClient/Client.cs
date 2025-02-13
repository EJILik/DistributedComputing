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
using System.Xml.Linq;

namespace MailSlots
{
    public partial class frmMain : Form
    {
        private int ServerMailSlot;   // дескриптор мэйлслота
        private int MyMailSlot;       // дескриптор мэйлслота
        private string MyMailSlotName = "\\\\" + Dns.GetHostName() + "\\mailslot\\Nick";    // имя мэйлслота, Dns.GetHostName() - метод, возвращающий имя машины, на которой запущено приложение
        private Thread t;                       // поток для обслуживания мэйлслота
        private bool _continue = true;          // флаг, указывающий продолжается ли работа с мэйлслотом
        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            this.Text += "     " + Dns.GetHostName();   // выводим имя текущей машины в заголовок формы
            rtbChat.Visible = false;
            btnSend.Visible = false;
            btnConnect.Visible = false;
            lblMailSlot.Visible = false;
            lblMessage.Visible = false;
            tbMailSlot.Visible = false;
            tbMessage.Visible = false;
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                ServerMailSlot = DIS.Import.CreateFile(tbMailSlot.Text,DIS.Types.EFileAccess.GenericWrite,DIS.Types.EFileShare.Read,0,DIS.Types.ECreationDisposition.OpenExisting,0,0);

                if (ServerMailSlot != -1)
                {
                    btnConnect.Enabled = false;
                    btnSend.Enabled = true;
                    btnConnect.Enabled = false;
                    btnSend.Enabled = true;
                    rtbChat.Visible = true;
                    btnSend.Visible = true;
                    btnConnect.Visible = true;
                    lblMailSlot.Visible = true;
                    lblMessage.Visible = true;
                    tbMailSlot.Visible = true;
                    tbMessage.Visible = true;

                    MyMailSlot = DIS.Import.CreateMailslot("\\\\.\\mailslot\\" + tbName.Text,0,DIS.Types.MAILSLOT_WAIT_FOREVER,0);


                    t = new Thread(ReceiveMessage);
                    t.Start();

                    SendMessage("`" + tbName.Text);
                    SendMessage("\n>> " + tbName.Text + " >> Вошёл в чат");
                }
                else
                    MessageBox.Show("Не удалось подключиться к мейлслоту");
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к мейлслоту");
            }

        }

        // отправка сообщения   
        private void btnSend_Click(object sender, EventArgs e)
        {
            
            SendMessage("\n >> " + tbName.Text + " >> " + tbMessage.Text);    // выполняем запись последовательности байт в мэйлслот
            tbMessage.Text = "";
        }
        private void SendMessage(string msg)
        {
            uint BytesWritten = 0;  // количество реально записанных в мэйлслот байт
            byte[] buff = Encoding.Unicode.GetBytes(msg);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт
            DIS.Import.WriteFile(ServerMailSlot,buff,Convert.ToUInt32(buff.Length),ref BytesWritten,0);     // выполняем запись последовательности байт в мэйлслот
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            DIS.Import.CloseHandle(ServerMailSlot);     // закрываем дескриптор мэйлслота
            _continue = false;      // сообщаем, что работа с мэйлслотом завершена

            if (t != null)
                t.Abort();          // завершаем поток

            if (MyMailSlot != -1)
                DIS.Import.CloseHandle(MyMailSlot);            // закрываем дескриптор мэйлслота
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void ReceiveMessage()
        {
            string msg = "";            // прочитанное сообщение
            int maxMessageText = 0;       // максимальный размер сообщения
            int MessagesSize = 0;         // размер следующего сообщения
            int CountMessages = 0;       // количество сообщений в мэйлслоте
            uint countBytesRead = 0;   // количество реально прочитанных из мэйлслота байтов

            try
            {
                while (_continue)
                {
                    DIS.Import.GetMailslotInfo(MyMailSlot,maxMessageText,ref MessagesSize,ref CountMessages,0);

                    for (int i = 0; i < CountMessages; i++)
                    {
                        byte[] buff = new byte[1024];                           // буфер прочитанных из мэйлслота байтов
                        DIS.Import.FlushFileBuffers(MyMailSlot);      // "принудительная" запись данных, расположенные в буфере операционной системы, в файл мэйлслота
                        DIS.Import.ReadFile(MyMailSlot, buff, 1024, ref countBytesRead, 0);      // считываем последовательность байтов из мэйлслота в буфер buff
                        msg = Encoding.Unicode.GetString(buff);                 // выполняем преобразование байтов в последовательность символов

                        if (!string.IsNullOrEmpty(msg))
                        {
                            rtbChat.Invoke((MethodInvoker)delegate
                            {
                                if (msg != "")
                                    rtbChat.Text += msg;     // выводим полученное сообщение на форму
                            });
                        }// приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                    }
                    Thread.Sleep(500);
                }
            }
            catch (ThreadInterruptedException ex)
            {
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (tbName.Text == null || tbName.Text.Trim() == "")
            {
                MessageBox.Show("Введите ник");
            }
            else
            {
                btnConnect.Visible = true;
                lblMailSlot.Visible = true;
                tbMailSlot.Visible = true;
                btnName.Enabled = false;
                tbName.ReadOnly = true;
            }
            
            


        }
    }
}