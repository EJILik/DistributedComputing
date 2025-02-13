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
using Microsoft.SqlServer.Server;
using System.Runtime.Remoting.Messaging;

namespace Pipes
{
    public partial class frmMain : Form
    {
        private Int32 MyPipeHandle;   // дескриптор канала
        private Int32 PipeHandle;
        private string MyPipeName = "\\\\" + Dns.GetHostName() + "\\pipe\\ServerPipe";    // имя канала, Dns.GetHostName() - метод, возвращающий имя машины, на которой запущено приложение
        private Thread t;                                                               // поток для обслуживания канала
        private bool _continue = true;
        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            //MyPipeHandle = DIS.Import.CreateNamedPipe("\\\\.\\pipe\\ServerPipe", DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);
            this.Text += "     " + Dns.GetHostName();   // выводим имя текущей машины в заголовок формы
            // создание потока, отвечающего за работу с каналом
            tbMessage.Visible = false;
            lbName.Visible = true;
            tbName.Visible = true;
            tbPipe.Visible = false;
            lblPipe.Visible = false ;
            rtbChat.Visible = false ;
            tbMessage.Visible = false;
            btnSend.Visible = false;
            lblMessage.Visible = false ;
            btnEnterChat.Visible = false ;

            
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            
            uint BytesWritten = 0;  // количество реально записанных в канал байт
            byte[] buff = Encoding.Unicode.GetBytes("\n >> " + tbName.Text + " >> " + tbMessage.Text);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт
            // открываем именованный канал, имя которого указано в поле tbPipe
            PipeHandle = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandle);
            tbMessage.Text = "";
        }

        private void ReceiveMessage()
        {
            string msg = "";            // прочитанное сообщение
            uint realBytesReaded = 0;   // количество реально прочитанных из канала байтов

            // входим в бесконечный цикл работы с каналом
            while (_continue)
            {
                if (DIS.Import.ConnectNamedPipe(MyPipeHandle, 0))
                {
                    byte[] buff = new byte[1024];                                           // буфер прочитанных из канала байтов
                    DIS.Import.FlushFileBuffers(MyPipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                    DIS.Import.ReadFile(MyPipeHandle, buff, 1024, ref realBytesReaded, 0);    // считываем последовательность байтов из канала в буфер buff
                    msg = Encoding.Unicode.GetString(buff);

                    if (msg != "")
                        rtbChat.Text += msg;                             // выводим полученное сообщение на форму

                    

                    DIS.Import.DisconnectNamedPipe(MyPipeHandle);                             // отключаемся от канала клиента 
                    //Thread.Sleep(50);                                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                }
            }
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void frmMain_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с каналом завершена

            if (t != null)
                t.Abort();          // завершаем поток

            if (PipeHandle != -1)
                DIS.Import.CloseHandle(PipeHandle);     // закрываем дескриптор канала
            if (MyPipeHandle != -1)
                DIS.Import.CloseHandle(MyPipeHandle);     // закрываем дескриптор канала
        }

        private void bSubmitName_Click(object sender, EventArgs e)
        {
            if (tbName.Text != null && tbName.Text.Trim() != "")
            {
                MyPipeName = "\\\\.\\pipe\\" + tbName.Text;
                MyPipeHandle = DIS.Import.CreateNamedPipe(MyPipeName, DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);

                t = new Thread(ReceiveMessage);
                t.Start();// закрываем дескриптор канала

                bSubmitName.Enabled = false;
                tbName.ReadOnly = true;
                btnEnterChat.Visible = true;
                tbMessage.Visible = true;
                tbPipe.Visible = true;
                lblPipe.Visible = true;
                
                
               
            }
            
        }

        private void btnEnterChat_Click(object sender, EventArgs e)
        {

            rtbChat.Visible = true;
            tbMessage.Visible = true;
            btnSend.Visible = true;
            lblMessage.Visible = true;
            uint BytesWritten = 0;  // количество реально записанных в канал байт
            byte[] buff = Encoding.Unicode.GetBytes("`"+tbName.Text);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт
            // открываем именованный канал, имя которого указано в поле tbPipe
            PipeHandle = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandle);
        }
    }
}
