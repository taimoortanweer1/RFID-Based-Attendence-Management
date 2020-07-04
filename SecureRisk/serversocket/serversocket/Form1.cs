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


namespace serversocket
{   public partial class Form1 : Form
    {
        int[] CARD_ID = new int[3];
        int[] DEVICE_ID = new int[4];

        byte[] data1 = new byte[60];  //data receiving from client 1
        string output1 = "0";
        int rcv1;

        byte[] data2 = new byte[60];  //data receiving from client 2
        string output2= "0";
        int rcv2;

        string input = "0";
        
        int interrupt = 0;
        
        string MSG_STR = null;
        OleDbConnection dbConn_SMS;
        OleDbConnection dbConn_LOG;
        Socket sockfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 5010);
        Socket[] clients = new Socket[10];
        //SMSCOMMS SMSEngine = new SMSCOMMS("COM16"); 
        SMSCOMMS SMSEngine;
        int check_SMS = 0;
        int check_LOG = 0;


        public Form1()
        {
            InitializeComponent();           
            Thread SMS_SENDING_THREAD = new Thread(new ThreadStart(SMS_SENDING));
            SMS_SENDING_THREAD.Start();              
        }
        void SMS_SENDING()
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
          
                dbConn_SMS = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\Taimoor\Desktop\Workshop\Client servercomunication\Client servercomunication\serversocket\serversocket\RFID_TRACKER.mdb");
                OleDbCommand adcommand = new OleDbCommand();
                OleDbCommand adcommandupdate = new OleDbCommand();
                adcommand.CommandText = "SELECT * FROM LOG WHERE DATE=#" + DateTime.Now.Date.ToShortDateString() + "# and " + " STATUS='PENDING' ";
                OleDbDataReader read = null;
                adcommand.Connection = dbConn_SMS;
                adcommandupdate.Connection = dbConn_SMS;
                dbConn_SMS.Open();
                read = adcommand.ExecuteReader();
                string s = null;
                if (read.HasRows)
                {
                    while (read.Read())
                    {

                        if (read[3].ToString().Contains("12/30/1899"))
                        {
                            s = read[3].ToString();
                            s = s.Replace("12/30/1899", " ");
                        }

                        MSG_STR = read[2].ToString() //name
                                  + " with CARD-ID : " + read[0].ToString() //cardid
                                  + " and Device ID : " + read[1].ToString() //device
                                  + " has entered at "
                                  + s;                  //time
                                  //+ " " +read[4].ToString(); 
                               
                        SMSEngine = new SMSCOMMS("COM16");                        
                        SMSEngine.Open();

                        SMSEngine.SendSMS("0"+read[6].ToString(), MSG_STR);
                        SMSEngine.Close();

                        adcommandupdate.CommandText = " UPDATE LOG SET STATUS='SENT' WHERE CARD_ID=" + read[0].ToString() + "AND DATE= #" + DateTime.Now.ToShortDateString() + "# AND " + "STATUS = 'PENDING'";
                        adcommandupdate.ExecuteNonQuery();

                    }
                }
                dbConn_SMS.Close();
            }
           
        }
    
        private void button1_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[40];
            label1.Show();
            label2.Text = "Waiting for the client";
            try
            {
                sockfd.Bind(ipep);
                sockfd.Listen(2);
                Thread waiting = new Thread(new ThreadStart(waitingforclient));
                waiting.Start();
            }
            catch
            {
                MessageBox.Show("already connect");
            }
        }
        void waitingforclient()
        {
            int count = 0;
            byte[] data = new byte[40];
            while (true)
            {
                count = count + 1;
                clients[count] = sockfd.Accept();
                input = "*Welcome to my test server*";
                data = Encoding.ASCII.GetBytes("*Welcome to my test server*");
                clients[count].Send(data, data.Length, SocketFlags.None);

                if (count == 1)
                {
                    Thread receiver1 = new Thread(new ThreadStart(myreceive1));
                    receiver1.Start();
                }
                if (count == 2)
                {
                    Thread receiver2 = new Thread(new ThreadStart(myreceive2));
                    receiver2.Start();
                }
            }
        }
        void myreceive1()
        {
            string[] INFO_FROM_EX = new string[4];
            
            while (true)
            {
                data1 = new byte[30];
                rcv1 = clients[1].Receive(data1, data1.Length, SocketFlags.None);
                output1 = Encoding.ASCII.GetString(data1);

                if (rcv1 == 25)
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
            while (true)
            {
                data2 = new byte[30];
                rcv2 = clients[2].Receive(data2, data2.Length, SocketFlags.None);
                output2 = Encoding.ASCII.GetString(data2);

                if (rcv2 == 25)
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
            if (check_LOG != 0)
            {
                dbConn_LOG.Close(); ;
                dbConn_LOG.Dispose();
            }
            check_LOG = 1;
            dbConn_LOG = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\Taimoor\Desktop\Workshop\Client servercomunication\Client servercomunication\serversocket\serversocket\RFID_TRACKER.mdb");
            OleDbCommand adcommand = new OleDbCommand();
            adcommand.Connection = dbConn_LOG;
            adcommand.CommandText = "INSERT INTO LOG"+" VALUES(" + Cid +   ","
                                                                 + DevID + ","
                                                                + "\'" + INFO[1] + "\',"
                                                                + "\'" + DateTime.Now.ToShortTimeString()+ "\',"
                                                                + "\'" + DateTime.Now.ToShortDateString() + "\',"
                                                                + "\'" + "PENDING" + "\',"
                                                                + "\'" + INFO[3]+ "\')";

            dbConn_LOG.Open();
            adcommand.ExecuteNonQuery();
            adcommand.Dispose();
            dbConn_LOG.Close();
        }      
        private void button3_Click(object sender, EventArgs e)
        {
            bool check = false;
            string com = "COM";
            int port = 1;
            com = "COM" + port.ToString(); 
            while (!check)
            {
                try
                {
                    SMSCOMMS SMSEngine = new SMSCOMMS(com);
                    check = SMSEngine.Open1();

                    if (check == false)
                    {
                        port = port + 1;
                        com = "COM" + port.ToString();
                    }
                    else
                    {
                        check = true;
                        label3.Text = "Connected to PORT : " + com;
                    }
                    
                }
                catch
                {
                    
                }
            }
            /*
                dbConn.Open();
                dataGrid.DataSource = @"Provider=Microsoft.Jet.OleDb.4.0;Data Source=C:\Users\Taimoor\Desktop\Workshop\Client servercomunication\Client servercomunication\serversocket\serversocket\RFID_TRACKER.mdb";
                dataGrid.Refresh();
                this.dataGrid.Refresh();
                DataSet ds = new DataSet();
                DataTable dt = new DataTable();
                ds.Tables.Add(dt);
                OleDbDataAdapter dd = new OleDbDataAdapter();
                dd = new OleDbDataAdapter("Select * From LOG", dbConn);
                dd.Fill(dt);
                dataGrid.DataSource = dt.DefaultView;
                dbConn.Close();
            */
        }    
        public string[] ACQUIRING_DATA(string ID)
        {            
            string path = @"C:\Users\Taimoor\Desktop\Workshop\Client servercomunication\Client servercomunication\serversocket\serversocket\info.xls";
            string con = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties=Excel 12.0;";
            string[] DATA_INFO = new string[4];

            OleDbConnection excon = new OleDbConnection(con);
            excon.Open();

            OleDbCommand command = new OleDbCommand("SELECT * FROM [rfid$]", excon);
            using (OleDbDataReader dr = command.ExecuteReader())
            {
                while (dr.Read())
                {
                    if (dr[0].ToString().Equals(ID) )
                    {
                        DATA_INFO[0] = dr[0].ToString(); // card id
                        DATA_INFO[1] = dr[1].ToString(); //name
                        DATA_INFO[2] = dr[10].ToString();//rfid no
                        DATA_INFO[3] = dr[11].ToString();//mobile no
                        break;
                    }
                }
            }
            return DATA_INFO;
        }             

    }

    public class SMSCOMMS
    {
        //******************** variables for message sending *******************************
        public SerialPort SMSPort;        
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

        //*******************************************************************
        public SMSCOMMS(string COMMPORT)
        {
            SMSPort = new SerialPort();
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
            //Check if Message Length <= 160
            if (SMSMessage.Length <= 160)
                MyMessage = SMSMessage;
            else
                MyMessage = SMSMessage.Substring(0, 160);
            if (SMSPort.IsOpen == true)
            {
                SMSPort.Write("AT\n");
                Thread.Sleep(1000);
                SMSPort.Write("AT+CMGF=1\r\n");
                Thread.Sleep(1000);
                SMSPort.Write("AT+CMGS=\"" + CellNumber+ "\"\r\n");
                Thread.Sleep(1000);
                SMSPort.Write(SMSMessage  +(char)(26));
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
            string SMSMessage = null;
            int Strpos = 0;
            string TmpStr = null;
            while (SMSPort.IsOpen == true)
            {
                if ((SMSPort.BytesToRead != 0) & (SMSPort.IsOpen == true))
                {
                    try
                    {
                        while (SMSPort.BytesToRead != 0)
                        {
                            SMSPort.Read(RXBuffer, 0, SMSPort.ReadBufferSize);
                            SerialIn =
                                SerialIn + System.Text.Encoding.ASCII.GetString(
                                RXBuffer);
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
                    catch { }
                    if (DataReceived != null)
                        DataReceived(SerialIn);
                    SerialIn = string.Empty;
                    RXBuffer = new byte[SMSPort.ReadBufferSize + 1];
                }
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
                catch
                {

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
                    return false;
                }
                catch
                {
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


      
    }
}

