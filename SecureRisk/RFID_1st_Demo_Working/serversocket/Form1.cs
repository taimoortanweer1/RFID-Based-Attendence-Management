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
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO.Ports;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
//using System.Security.AccessControl.DirectorySecurity;

namespace serversocket
{   
    public partial class Form1 : Form
    {
        int[] CARD_ID = new int[3];
        int[] DEVICE_ID = new int[4];
        int count_client = 0; //(no of clients which are connected - 1)
    //******************************************************************************
        byte[] data1 = new byte[60];  //data receiving from client 1
        string output1 = "0";
        int rcv1;

        byte[] data2 = new byte[60];  //data receiving from client 2
        string output2= "0";
        int rcv2;
    //*****************************************************************************    
        string MSG_STR = null; //message string to be sent
        string com; // com tellling which comport is accessed
        string ENT_NAME; // GUI showing name using invoke function
        string ENT_ID;// GUI showing CARD using invoke function

        OleDbConnection dbConn_SMS; //db connection for SMS sending
        OleDbConnection dbConn_LOG; //db connection for log maintainence

      //***************************************************************************************8
        Socket sockfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 5010);
        Socket[] clients = new Socket[10];
        int check_SMS = 0;
        int check_LOG = 0;
        int listening_sockets = 2;        
        //******************** variables for message sending *******************************
       
        public Thread SMSThread;
        public Thread ReadThread;
        public static bool _Continue = false;
        public static bool _ContSMS = false;
        public bool _Wait = false;
        public static bool _ReadPort = false;
        public delegate void SendingEventHandler(bool Done);
        public event SendingEventHandler Sending;
        public delegate void DataReceivedEventHandler(string Message);
        public event DataReceivedEventHandler DataReceived;
        string signal;
        int signal_strength=0;
        int signal_threshold = 10;
        Thread SMS_SENDING_THREAD; //separate thread for retrieving PENDING SMS and sending SMS
        int msg_send_check = 0;
        
        //********************** variables for security ********************************************* 
        string mac = null;
        private static byte[] salt = Encoding.ASCII.GetBytes("salt"); 
        //******************************************************

     public Form1()
        {
            InitializeComponent();
            SMS_SENDING_THREAD = new Thread(new ThreadStart(SMS_SENDING));
            timer1.Start();
        }
     
     private void button1_Click(object sender, EventArgs e)
        {   
            byte[] data = new byte[40];
            try
            {
                sockfd.Bind(ipep);
                sockfd.Listen(listening_sockets);
                Thread waiting = new Thread(new ThreadStart(waitingforclient));
                waiting.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Devices already connected");
            }
        }
     void waitingforclient()
        {
            
            byte[] data = new byte[40];
            while (true)
            {

                this.Invoke(new EventHandler(UI_update));
                count_client = count_client + 1;
                clients[count_client] = sockfd.Accept();

                if (count_client == 1)
                {
                    Thread receiver1 = new Thread(new ThreadStart(myreceive1));
                    receiver1.Start();
                }
                if (count_client == 2)
                {
                    Thread receiver2 = new Thread(new ThreadStart(myreceive2));
                    receiver2.Start();
                }
               
            }

            
        }
     void myreceive1()
        {
         //Application.ExecutablePath + "log1.txt"
            string[] INFO_FROM_EX = new string[4];
            bool con = false;
            while (true)
            {
                data1 = new byte[30];
                try
                {
                    rcv1 = clients[1].Receive(data1, data1.Length, SocketFlags.None);
                }
                catch
                {
                    MessageBox.Show("Connection Lost");
                }
                output1 = Encoding.ASCII.GetString(data1);

                string s = clients[1].RemoteEndPoint.ToString();
                s = s.Remove(s.Length - 5);

                for (int i = 0; i < listBox1.Items.Count; i++)
                { 
                    if (s.Equals(listBox1.Items[i]))
                    {                   
                    con = true;
                    break;
                    }
                    
                }
                
                //con = listBox1.
                if (rcv1 == 25 && con) 
                {
                    // Storing new DATA in Big endian format
                    CARD_ID[0] = (data1[11] << 16);
                    CARD_ID[1] = (data1[12] << 8);
                    CARD_ID[2] = (data1[13]);

                    DEVICE_ID[0] = (data1[1] << 24);
                    DEVICE_ID[1] = (data1[2] << 16);
                    DEVICE_ID[2] = (data1[3] << 8);
                    DEVICE_ID[3] = (data1[4]);
                    string DevID = (DEVICE_ID[0] + DEVICE_ID[1] + DEVICE_ID[2] + DEVICE_ID[3]).ToString();
                    string ID = (CARD_ID[0] + CARD_ID[1] + CARD_ID[2]).ToString();

                    INFO_FROM_EX = ACQUIRING_DATA(ID);
                    maintain_log(INFO_FROM_EX,ID,DevID);
                }
            }
        }
     void myreceive2()
        {
            string[] INFO_FROM_EX = new string[4];
            bool con = false;
            while (true)
            {
                data2 = new byte[30];
                try
                {
                    rcv2 = clients[2].Receive(data2, data2.Length, SocketFlags.None);
                }
                catch
                {
                    MessageBox.Show("Connection Lost");
                }
                output2 = Encoding.ASCII.GetString(data2);

                string s = clients[1].RemoteEndPoint.ToString();
                s = s.Remove(s.Length - 5);

                for (int i = 0; i < listBox1.Items.Count; i++)
                {
                    if (s.Equals(listBox1.Items[i]))
                    {
                        con = true;
                        break;
                    }

                }
                if (rcv2 == 25 && con)
                {
                    // Storing new DATA in Big endian format
                    CARD_ID[0] = (data2[11] << 16);
                    CARD_ID[1] = (data2[12] << 8);
                    CARD_ID[2] = (data2[13]);

                    DEVICE_ID[0] = (data2[1] << 24);
                    DEVICE_ID[1] = (data2[2] << 16);
                    DEVICE_ID[2] = (data2[3] << 8);
                    DEVICE_ID[3] = (data2[4]);
                    string DevID = (DEVICE_ID[0] + DEVICE_ID[1] + DEVICE_ID[2] + DEVICE_ID[3]).ToString();
                    string ID = (CARD_ID[0] + CARD_ID[1] + CARD_ID[2]).ToString();

                    INFO_FROM_EX = ACQUIRING_DATA(ID);
                    maintain_log(INFO_FROM_EX, ID, DevID);
                }
            }
            
        }

     void maintain_log(string[] INFO, string Cid, string DevID)
        {
            try
            {
                if (check_LOG != 0)
                {
                    dbConn_LOG.Close(); ;
                    dbConn_LOG.Dispose();
                }
                check_LOG = 1;
                using (dbConn_LOG = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Application.StartupPath + "\\Userfiles\\RFID_TRACKER.mdb"))
                {
                    //dbConn_LOG = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\Taimoor\Desktop\Workshop\Client servercomunication\Client servercomunication\serversocket\serversocket\RFID_TRACKER.mdb");
                    using (OleDbCommand adcommand = new OleDbCommand())
                    {
                        adcommand.Connection = dbConn_LOG;
                        adcommand.CommandText = "INSERT INTO LOG" + " VALUES(" + Cid + ","
                                                                             + DevID + ","
                                                                            + "\'" + INFO[1] + "\',"
                                                                            + "\'" + DateTime.Now.ToShortTimeString() + "\',"
                                                                            + "\'" + DateTime.Now.ToShortDateString() + "\',"
                                                                            + "\'" + "PENDING" + "\',"
                                                                            + "\'" + INFO[3] + "\')";
                        ENT_NAME = INFO[1];
                        ENT_ID = Cid;
                        this.Invoke(new EventHandler(UI_update));
                        dbConn_LOG.Open();
                        adcommand.ExecuteNonQuery();
                    }
                    dbConn_LOG.Close();
                }
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }      
     public string[] ACQUIRING_DATA(string ID)
        {
            string path = Application.StartupPath + "\\Userfiles\\info.xls";
           //MessageBox.Show(path);
            //string path = @"C:\Users\Taimoor\Desktop\Workshop\Client servercomunication\Client servercomunication\serversocket\serversocket\info.xls";
            string con = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties=Excel 12.0;";
            string[] DATA_INFO = new string[4];

            try
            {
                using (OleDbConnection excon = new OleDbConnection(con))
                {
                    excon.Open();

                    using (OleDbCommand command = new OleDbCommand("SELECT * FROM [rfid$]", excon))
                    {
                        using (OleDbDataReader dr = command.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                if (dr[0].ToString().Equals(ID))
                                {
                                    DATA_INFO[0] = dr[0].ToString(); // card id
                                    DATA_INFO[1] = dr[1].ToString(); //name
                                    DATA_INFO[2] = dr[10].ToString();//rfid no
                                    DATA_INFO[3] = dr[11].ToString();//mobile no

                                    break;
                                }
                            }

                        }
                    }
                }
                return DATA_INFO;
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
                return null;
            }
        }

     private void UI_update(object sender, EventArgs e)
     {
         label5.Text = ENT_NAME;
         label6.Text = ENT_ID;
         //label9.Text = count_client.ToString();
         toolStripStatusLabel2.Text = count_client.ToString();
         toolStripStatusLabel4.Text = "Connected";

         if (signal_strength < signal_threshold)
             label11.Show();
         else
             label11.Hide();

         if (msg_send_check == 1)
             toolStripStatusLabel7.Text = "SENDING";
         else
             toolStripStatusLabel7.Text = "IDLE";
     }
     private void button2_Click(object sender, EventArgs e)
     {
         Application.Exit();
     }
     public  void button3_Click(object sender, EventArgs e)
     {
         bool check = false;
         int port;

         if (radioButton2.Checked)
         {
             com = comboBox1.Text;
             try
             {
                 PORT_PROP(com);
                 check = Open1();
                 if (check == false)
                 {
                     label3.ForeColor = Color.Red;
                     label3.Text = "No GSM Modem Found";
                     return;
                 }
                 else
                 {
                     label3.ForeColor = Color.Green;
                     label3.Text = "Connected to PORT : " + com;
                     // Close();
                     if (SMS_SENDING_THREAD.ThreadState.ToString() == "Unstarted")
                         SMS_SENDING_THREAD.Start();
                     return;
                 }
             }
             catch (Exception ex)
             {
                 //  MessageBox.Show(ex.ToString());
                 label3.ForeColor = Color.Red;
                 label3.Text = " GSM Connection Error";
             }
         }
         else
         {

             port = 1;
             com = "COM" + port.ToString();

             while (!check && port < 39)
             {
                 try
                 {
                     PORT_PROP(com);
                     check = Open1();

                     if (check == false)
                     {
                         port = port + 1;
                         com = "COM" + port.ToString();
                     }
                     else
                     {
                         check = true;
                         label3.ForeColor = Color.Green;
                         label3.Text = "Connected to PORT : " + com;
                         //   Close();
                         if (SMS_SENDING_THREAD.ThreadState.ToString() == "Unstarted")
                             SMS_SENDING_THREAD.Start();
                         return;
                     }

                 }
                 catch (Exception ex)
                 {
                     label3.ForeColor = Color.Red;
                     label3.Text = "No GSM Modem Found";
                     return;
                     //   MessageBox.Show(ex.ToString());
                 }
             }

         }

     } 
     private void button4_Click(object sender, EventArgs e)
     {
         string lines="";

         for (int i = 0; i < listBox1.Items.Count; i++)
             lines = lines + listBox1.Items[i] +"\r\n";

         try
         {
             using (System.IO.StreamWriter file = new System.IO.StreamWriter(Application.StartupPath + "\\Userfiles\\IP.txt", true))
             {
                 file.WriteLine(lines);
             }
             MessageBox.Show("File Saved");
         }
         catch
         {
             MessageBox.Show("File Save Error");
         }
         
     }
     private void button5_Click(object sender, EventArgs e)
     {
         
         if (SMSPort.IsOpen)
         {
             SMSPort.Close();
             if (SMS_SENDING_THREAD.ThreadState.ToString() == "Running")
                 SMS_SENDING_THREAD.Start();
             
             label3.ForeColor = Color.Red;
             label3.Text = "GSM Module Disconnected";
         }
          
     }
     private void button6_Click(object sender, EventArgs e)
     {
         if (IsValidIP(textBox1.Text))
         {
             listBox1.Items.Add(textBox1.Text);
         }
         else
         {
             MessageBox.Show("Not a valid IP Address");
         }
     }
     public static bool IsValidIP(string ipAddress)
     {
         IPAddress unused;
         return IPAddress.TryParse(ipAddress, out unused)
           &&
           (
               unused.AddressFamily != AddressFamily.InterNetwork
               ||
               ipAddress.Count(c => c == '.') == 3
           );
     }

     void SMS_SENDING()
     {
         try
         {
             while (true)
             {
                 if (check_SMS != 0)
                 {
                     dbConn_SMS.Close();
                     dbConn_SMS.Dispose();
                     dbConn_SMS.Close();
                 }
                 check_SMS = 1;
                 msg_send_check = 0;
                 using (dbConn_SMS = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Application.StartupPath + "\\Userfiles\\RFID_TRACKER.mdb"))
                 {
                     // dbConn_SMS = new OleDbConnection(Application.StartupPath + "\\RFID_TRACKER.mdb");
                     //dbConn_SMS = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\Taimoor\Desktop\Workshop\Client servercomunication\Client servercomunication\serversocket\serversocket\RFID_TRACKER.mdb");
                     using (OleDbCommand adcommand = new OleDbCommand())
                     {
                         using (OleDbCommand adcommandupdate = new OleDbCommand())
                         {
                             adcommand.CommandText = "SELECT * FROM LOG WHERE DATE=#" + DateTime.Now.Date.ToShortDateString() + "# and " + " STATUS='PENDING' ";
                             OleDbDataReader read = null;
                             adcommand.Connection = dbConn_SMS;
                             adcommandupdate.Connection = dbConn_SMS;
                             dbConn_SMS.Open();
                             read = adcommand.ExecuteReader();
                             string s = null;
                             if (read.HasRows && (signal_strength >= signal_threshold))
                             {
                                 msg_send_check = 1;
                                 
                                 this.Invoke(new EventHandler(UI_update));
                                 while (read.Read())
                                 {

                                     if (read[3].ToString().Contains("12/30/1899"))
                                     {
                                         s = read[3].ToString();
                                         s = s.Replace("12/30/1899", " ");
                                     }


                                     MSG_STR =  "HeadStart Alert\r\n"
                                               + read[2].ToString() //name
                                               + " with RFID CARD No:" + read[0].ToString() //cardid
                                               //+ " and Device ID:" + read[1].ToString() //device
                                               + " has entered at "
                                               + s
                                               + "\r\nPowered By Robotech Engineering"; //time

                                     SendSMS("0" + read[6].ToString(), MSG_STR);
                              
                                     adcommandupdate.CommandText = " UPDATE LOG SET STATUS='SENT' WHERE CARD_ID=" + read[0].ToString() + "AND DATE= #" + DateTime.Now.ToShortDateString() + "# AND " + "STATUS = 'PENDING'";
                                     adcommandupdate.ExecuteNonQuery();

                                 }
                                 msg_send_check = 0;
                             }
                             else
                             {
                                 this.Invoke(new EventHandler(UI_update));   
                             }
                         }
                     }
                     dbConn_SMS.Close();
                 }

             }
         }
         catch (Exception ex)
         {
             //MessageBox.Show(ex.ToString());
         }
     }      
     public void PORT_PROP(string COMMPORT)
        {
       
            SMSPort.PortName = COMMPORT;
            SMSPort.BaudRate = 9600;
            SMSPort.Parity = Parity.None;
            SMSPort.DataBits = 8;
            SMSPort.StopBits = StopBits.One;
            SMSPort.Handshake = Handshake.RequestToSend;
            SMSPort.DtrEnable = true;
            SMSPort.RtsEnable = true;
            SMSPort.NewLine = System.Environment.NewLine;         
            ReadThread = new Thread(new System.Threading.ThreadStart(ReadPort));
            

        }
     public bool SendSMS(string CellNumber, string SMSMessage)
        {
            string MyMessage = null;
            string s = null; ; ;
            //Check if Message Length <= 160
            if (SMSMessage.Length <= 160)
                MyMessage = SMSMessage;
            else
                MyMessage = SMSMessage.Substring(0, 160);

            if (SMSPort.IsOpen == true)
            {
                SMSPort.Write("AT+CMGF=1\r\n");            
                Thread.Sleep(1000);
                SMSPort.Write("AT+CMGS=\"" + CellNumber+ "\"\r\n");
                
                Thread.Sleep(1000);
                SMSPort.Write(SMSMessage + (char)(26) + Environment.NewLine);
                Thread.Sleep(1000);
                s = SMSPort.ReadExisting();
                _Continue = false;
                if (Sending != null)
                    Sending(false);
            }
            return false;
        }
     private void ReadPort()
        {
            string SerialIn = null;
            byte[] RXBuffer = new byte[SMSPort.ReadBufferSize + 1];
            while (SMSPort.IsOpen == true)
            {
                 
                
                try
                {
                if ((SMSPort.BytesToRead != 0) & (SMSPort.IsOpen == true))
                    {
                   
                        while (SMSPort.BytesToRead != 0)
                        {
                            SMSPort.Read(RXBuffer, 0, SMSPort.ReadBufferSize);
                            SerialIn =
                                SerialIn + System.Text.Encoding.ASCII.GetString(
                                RXBuffer);
                            if (SerialIn.Contains("+CSQ:") == true)
                            {
                                signal = SerialIn;
                            }
                            if (SerialIn.Contains(">") == true)
                            {
                                _ContSMS = true;
                            }
                            if (SerialIn.Contains("+CMGS:") == true)
                            {
                                _Continue = true;
                                if (Sending != null)
                                    Sending(true);
                                _Wait = false;
                                SerialIn = string.Empty;
                                RXBuffer = new byte[SMSPort.ReadBufferSize + 1];
                            }
                        }
                    }
                 }
                 catch 
                 { 
                  MessageBox.Show("Unexpected GSM Module Error");
                 }
                    if (DataReceived != null)
                        DataReceived(SerialIn);
                    SerialIn = string.Empty;
                    RXBuffer = new byte[SMSPort.ReadBufferSize + 1];
                }
            }       
     public void Open()
        {

            if (SMSPort.IsOpen == false)
            {
                try
                {
                    SMSPort.Open();
                    SMSPort.Write("AT+CFUN=1\r\n");
                    ReadThread.Start();
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString());
                }
            }
    
        }
     public bool Open1()
        {
            string s = null;
            if (SMSPort.IsOpen == false)
            {
                try
                {
                    SMSPort.Open();
                    SMSPort.Write("AT\r\n");
                    Thread.Sleep(100);
                    s = SMSPort.ReadExisting();
                    if (s.Contains("OK"))
                    {
                        SMSPort.Write("AT+CFUN=1\r\n");
                        ReadThread.Start();
                        return true;
                    }
                    else
                    {
                        if(SMSPort.IsOpen)
                        SMSPort.Close();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString());
                    return false;
                }
            }
            return true;
        }
     public void Close()
        {
            if (SMSPort.IsOpen == true)
            {
                SMSPort.Close();
            }
        }

     private void timer1_Tick(object sender, EventArgs e)
     {
        string sig=null;
        float value=0;
         if (SMSPort.IsOpen)
         {
             try
             {
                 SMSPort.Write("AT+CSQ\r\n");
                 Thread.Sleep(1000);
                 // signal = SMSPort.ReadExisting();
                 int in1 = signal.IndexOf("CSQ:");
                 string ret = signal.Substring(in1 + 4, 3);
                 ret = ret.Replace(" ", "");
               
                 signal_strength = int.Parse(ret);
                 toolStripStatusLabel12.Text = signal_strength.ToString();
                 value = (signal_strength / 32.0f) * 100.0f;
                 toolStripProgressBar1.Value = (int)(value);
             }
             catch
             { }

            
         }
     }

     private void button7_Click(object sender, EventArgs e)
     {
         MessageBox.Show("For any query\r\nPlease contact us via email : crg@case.edu.pk ");
     }

     private void pictureBox2_Click(object sender, EventArgs e)
     {

     }

     private void pictureBox1_Click(object sender, EventArgs e)
     {

     }

     
    
    
   

   
     } 
    
}

