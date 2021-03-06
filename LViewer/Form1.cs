﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Collections;
using System.Threading;

namespace LViewer
{
    public partial class Form_Main : Form
    {
        const int PORT_NUMBER= 9998;   //Port kết nối TCP.
        Hashtable hshClients = new Hashtable(); //HashTable hiển thị danh sách user.
        TcpListener listener;
        Thread thread;

        public Form_Main()
        {
            InitializeComponent();
        }
         
        //Hàm DoListen tạo background listener.
        private void DoListen()
        {
            try
            {
                //listener = new TcpListener(System.Net.IPAddress.Any, PORT_NUMBER);
                listener = new TcpListener(System.Net.IPAddress.Loopback, PORT_NUMBER);
                listener.Start(); //Bắt đầu lắng nghe.
                do
                {
                    UserConnection user = new UserConnection(listener.AcceptTcpClient());
                    user.ReiceivedMess += new dlgReceiveMess(OnLineReiceive);
                    UpdateStatus("Mot nguoi dang ket noi. Xin doi trong vai giay.");
                }
                while (true);
            }
            catch (Exception ex)
            {

            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            thread = new Thread(new ThreadStart(DoListen));
            thread.Start();
            UpdateStatus("Sang sang ket noi");
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
        }

        //Xử lý khi Line nhận một chuỗi thông điệm nào đó.
        private void OnLineReiceive(UserConnection user, string strData)
        {
            string[] dataArray;
            dataArray = strData.Split((char)124);
            switch (dataArray[0])
            {
                case "CONNECT":
                    ConnectUser(dataArray[1], user);
                    break;
                case "CHAT":
                    SendChatMesage(dataArray[1], user);
                    break;
                case "DISCONNECT":
                    DisconnectUser(user);
                    break;
                case "REQUESTUSERS":
                    ListUsers(user);
                    break;
                default:
                    UpdateStatus("Unknown message:" + strData);
                    break;
            }
        }

        //Gửi chuỗi tin nhắn  
        private void Send(string strMessage)
        {
            UserConnection client;

            //Với mỗi user trong bảng Hash thì thực hiện các đoạn bên dưới.
            foreach(DictionaryEntry de in hshClients)
            {
                client = (UserConnection)de.Value;
                client.SendData(strMessage);
            }
        }

        //Gửi tin nhắn đi tất cả clients trừ máy chủ: 
        private void SendToAllClients(string strMessage, UserConnection user)
        {
            UserConnection client;
            foreach (DictionaryEntry de in hshClients)
            {
                client = (UserConnection)de.Value;
                if(client.Username != user.Username)
                {
                    client.SendData(strMessage);
                }
            }
        }

        //Gửi lại tin nhắn cho một người cố định có trước (username).
        private void ReplyToSender(string strMessage, UserConnection user)
        {
           
            user.SendData(strMessage);
        }

        //Ghép các user và gửi theo list
        private void ListUsers(UserConnection user)
        {
            UserConnection client;

            string userList;
            UpdateStatus("Dang gui toi " + user.Username + " danh sach nhung nguoi dang online.");
            userList = "LISTUSERS";
            foreach(DictionaryEntry de in hshClients)
            {
                client = (UserConnection)de.Value;
                userList += client.Username;
            }
            ReplyToSender(userList, user);

        }

        private void SendChatMesage(string message, UserConnection user)
        {
            UpdateStatus(user.Username + ": " + message);
            SendToAllClients("CHAT|" + user.Username + ": " + message, user);
        }
  
        private void button_Sender_Click(object sender, EventArgs e)
        {
            if(textBox_Input.Text!="")
            {
                UpdateStatus("Server: " + textBox_Input.Text);
               
                Send("BROAD|" + textBox_Input.Text);
                textBox_Input.Text = string.Empty;
                
            }
        }

        private void ConnectUser(string userName, UserConnection user)
        {
            if(hshClients.Contains(userName))
            {
                ReplyToSender("REFUSE", user);
            }
            else
            {
                user.Username = userName;
                
                hshClients.Add(userName,user);
                ReplyToSender("JOIN", user);
                SendToAllClients("CHAT|" + "Waiting..." + user.Username + " vua dang nhap.", user);

            }
        }
        
        //Ngắt kết nối
        private void DisconnectUser(UserConnection sender)
        {
            UpdateStatus("Waiting..." + sender.Username + " da thoat ra.");
            SendToAllClients("CHAT|" + "Waiting..." + sender.Username + " da thoat ra.", sender);
            hshClients.Remove(sender.Username);
        }

        private void UpdateStatus(string mes)
        {
            listBox_Status.Items.Add(mes);
        }
        private void textBox_Input_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
