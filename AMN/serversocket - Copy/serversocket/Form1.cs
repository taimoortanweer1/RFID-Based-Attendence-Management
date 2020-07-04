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
using System.Management;
using System.Diagnostics;
using System.Data.Common;
using System.Web;
//using System.Security.AccessControl.DirectorySecurity;

namespace serversocket
{
    public partial class Form1 : Form
    {
        int[] CARD_ID = new int[3];
        int[] DEVICE_ID = new int[4];
        int count_client = 0; //(no of clients which are connected - 1)
        int count_client_check = 0;
        //******************************************************************************
        byte[] data1 = new byte[60];  //data receiving from client 1
        string output1 = "0";
        int rcv1;

        byte[] data2 = new byte[60];  //data receiving from client 2
      // string output2 = "0";
       // int rcv2;
        //*****************************************************************************    
        string MSG_STR = null; //message string to be sent
       // string com; // com tellling which comport is accessed
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
        int listening_sockets = 1;
        //******************** variables for message sending *******************************

        public Thread SMSThread;     
       // string signal=null;
        //int signal_strength = 0;
        //int signal_threshold = 9;
        Thread SMS_SENDING_THREAD; //separate thread for retrieving PENDING SMS and sending SMS
        int msg_send_check = 0;

        //********************** variables for security ********************************************* 
      //  string mac = null;
        private static byte[] salt = Encoding.ASCII.GetBytes("salt");
        //**************** character count **************************************
        int char_count = 0;
        int sms_count = 1;
        //**********************************************************8
        bool ping = false;
        public Form1()
        {

            
            //initializing block of objects used in project
            InitializeComponent();
            SMS_SENDING_THREAD = new Thread(new ThreadStart(SMS_SENDING));
            timer1.Start();
            //if (!File.Exists(Environment.ExpandEnvironmentVariables("%windir%") + "\\lic.txt"))
            if (!File.Exists(Application.StartupPath + "\\Userfiles\\data.config"))
            {
                LicenceKey lk = new LicenceKey(this);
                this.Hide();
                this.Enabled = false;
                this.Opacity = 0.5;
                lk.Show();
            }
            else
            {

                licence_validity_check();
                load_message_from_file();
                load_IP_from_file();
                load_ID_from_file();
           
                this.Opacity = 1;
                this.Enabled = true;
            }
        }
        void licence_validity_check()
        {
            //string newPath = Environment.ExpandEnvironmentVariables("%windir%");
            string newPath = Application.StartupPath + "\\Userfiles";
            string newFileName = "data.config";
            newPath = System.IO.Path.Combine(newPath, newFileName);
            int time = 0;
            //FileInfo oFileInfo = new FileInfo(newPath);
            //DateTime dtCreationTime = oFileInfo.CreationTime;

            using (StreamReader sr = new StreamReader(newPath))
            {
                time = Convert.ToInt32(sr.ReadToEnd().ToString());
                time = time - 5269;
            }
           
            //if (dtCreationTime.AddYears(1) < DateTime.Now)
            if (time != DateTime.Now.Year+1)
            {
                MessageBox.Show("Licence Expired/Please Check the date of the system");
                SMS_SENDING_THREAD.Abort();
                Process[] myProcess = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                foreach (Process process in myProcess)
                {
                    process.Kill();
                    Application.DoEvents();

                }
            }

        }


        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Visible = true;
            timer2.Start();
            //*********** starting and creating waiting thread for client ******************
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
                if (toolStripStatusLabel2.Text == "0")
                {
                    MessageBox.Show("Waiting for response.......!! \n Check if device is properly connected !! \r\n" + ex.Message);
                }
                else
                {
                    MessageBox.Show("Devices already connected !! \r\n" + ex.Message);
                }
            }
            //*********** END OF starting and creating waiting thread for client ******************
        }
        void waitingforclient()
        {
            //*********** starting and creating receiver thread for client ******************
            byte[] data = new byte[40];
            while (true)
            {
                
                count_client = count_client + 1;
               // count_client_check = 0;
                this.Invoke(new EventHandler(UI_update));

                if (count_client == 1 && count_client_check == 0)
                clients[count_client] = sockfd.Accept();

                if (count_client == 1)
                {
                    Thread receiver1 = new Thread(new ThreadStart(myreceive1));
                    receiver1.Start();
                    count_client_check = 1;
                    this.Invoke(new EventHandler(UI_update));
                }
                /*
                if (count_client == 2)
                {
                    Thread receiver2 = new Thread(new ThreadStart(myreceive2));
                    receiver2.Start();
                }
                 */

            }
            //*********** END OF starting and creating waiting thread for client ******************

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
                    maintain_log(INFO_FROM_EX, ID, DevID);
                }
                else
                {
                    MessageBox.Show("Unauthorized device connection...\nCheck IP Settings of your Device or Server");
                }
            }
        }
        /*
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
        */
        void maintain_log(string[] INFO, string Cid, string DevID)
        {
            //*********************** creating table dynamically if required ************************
            DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.OleDb");
            DataTable userTables = null;
            string dateTable = null;
            using (DbConnection connection = factory.CreateConnection())
            {

                string path = Application.StartupPath + "\\Userfiles\\RFID_TRACKER.mdb";
                string con = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path;
                string[] restrictions = new string[4];
                bool found = false;
                connection.ConnectionString = con;
                restrictions[3] = "Table";
                connection.Open();
                userTables = connection.GetSchema("Tables", restrictions);

                dateTable = DateTime.Today.Date.ToShortDateString();
                dateTable = dateTable.Replace("/", "");
                string TableName = null;

                using (OleDbConnection conn = new OleDbConnection())
                {
                    conn.ConnectionString = con;
                    OleDbCommand cmmd = new OleDbCommand("", conn);
                    conn.Open();

                    for (int i = 0; i < userTables.Rows.Count; i++)
                    {
                        TableName = (userTables.Rows[i][2].ToString());
                        if (TableName == dateTable)
                        {
                            found = true;
                            break;
                        }
                        else
                        {
                            found = false;
                        }
                    }

                    if (found == false)
                    {
                        try
                        {
                            cmmd.CommandText = "CREATE TABLE " + dateTable + "( [CARD_ID] int, [DEVICE_ID] int, [NAME] Text, [ENTRY_TIME] DateTime, [DATE] DateTime, [STATUS] Text, [MOBILE] Text, [ENTRY_EXIT] Text)";
                            cmmd.ExecuteNonQuery();
                        }
                        catch
                        { }
                    }
                }
             
            }
            
            //****************** end of creating table ************************************************
            /*
            if (!SMS_SENDING_THREAD.IsAlive)
            {
                SMS_SENDING_THREAD = new Thread(SMS_SENDING);
                SMS_SENDING_THREAD.IsBackground = true;
                SMS_SENDING_THREAD.Start();
            }  
             */
              //*************** START SEARCHING FOR LAST ENTRY STATUS MAINTAINING LOG PART ***************************************
            DateTime prev_time = DateTime.Now.AddYears(-1000);
            DateTime cur_time;
            string ENTRY_EXIT = "ENTRY";
            try
            {
                if (check_LOG != 0)
                {
                    dbConn_LOG.Close(); ;
                    dbConn_LOG.Dispose();
                }
                check_LOG = 1;
                string path = Application.StartupPath + "\\Userfiles\\RFID_TRACKER.mdb";
                string con = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path;
                using (OleDbConnection excon = new OleDbConnection(con))
                {
                    excon.Open();

                    using (OleDbCommand command = new OleDbCommand("SELECT * FROM " + dateTable + "  WHERE DATE=#" + DateTime.Now.ToShortDateString() + "# AND CARD_ID=" + Cid, excon))
                    {
                        using (OleDbDataReader dr = command.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                cur_time = DateTime.Parse(dr[3].ToString());
                                if (cur_time > prev_time)
                                {
                                    prev_time = cur_time;
                                    if (dr[7].ToString() == "ENTRY")
                                    {
                                        ENTRY_EXIT = "EXIT";
                                    }
                                    else
                                    {
                                        ENTRY_EXIT = "ENTRY";
                                    }
                                }
                            }

                        }
                    }
                }
                //*************** END OF SEARCHING FOR LAST ENTRY STATUS MAINTAINING LOG PART ***************************************

                //*************** START MAINTAINING LOG PART ***************************************
                using (dbConn_LOG = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Application.StartupPath + "\\Userfiles\\RFID_TRACKER.mdb"))
                {
                    using (OleDbCommand adcommand = new OleDbCommand())
                    {
                        adcommand.Connection = dbConn_LOG;
                        adcommand.CommandText = "INSERT INTO " + dateTable + " VALUES(" + Cid + ","
                                                                            + DevID + ","
                                                                            + "\'" + INFO[1] + "\',"
                                                                            + "\'" + DateTime.Now.ToShortTimeString() + "\',"
                                                                            + "\'" + DateTime.Now.ToShortDateString() + "\',"
                                                                            + "\'" + "PENDING" + "\',"
                                                                            + "\'" + INFO[3] + "\',"
                                                                            + "\'" + ENTRY_EXIT + "\')";
                        ENT_NAME = INFO[1];
                        ENT_ID = Cid;
                        this.Invoke(new EventHandler(UI_update));
                        dbConn_LOG.Open();
                        adcommand.ExecuteNonQuery();
                        
                    }
                    dbConn_LOG.Close();
                }
                update_date_on_server(ENT_NAME ,ENT_ID, DateTime.Now.ToLongTimeString() +" "+ DateTime.Now.ToShortDateString() , ENTRY_EXIT.ToString());  
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            //*************** END OF MAINTAINING LOG PART ***************************************
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
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return null;
            }
        }
        private void UI_update(object sender, EventArgs e)
        {
            label5.Text = "Name : " + ENT_NAME;
            label6.Text = "Student ID : " + ENT_ID;
            if (count_client_check == 1)
                toolStripStatusLabel2.Text = "1";

            //if (count_client > 0)
            if (!toolStripStatusLabel2.Text.Contains("XXXX") && count_client_check == 1)
                toolStripStatusLabel4.Text = "Connected";

            if (ping == false)
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
           
            SMS_SENDING_THREAD.Abort();
            Process[] myProcess = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            foreach (Process process in myProcess)
            {
                process.Kill();
                //hence call to Application DoEvents forces the MSG
                Application.DoEvents();
            }


        }
        private void button4_Click(object sender, EventArgs e)
        {
            string lines = "";

            for (int i = 0; i < listBox1.Items.Count; i++)
                lines = lines + listBox1.Items[i] + "\r\n";

            try
            {
                FileStream fs = new FileStream(Application.StartupPath + "\\Userfiles\\IP.txt", FileMode.Create, FileAccess.Write);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fs))
                {
                    file.Write(lines);
                }
                MessageBox.Show("File Saved");
            }
            catch
            {
                MessageBox.Show("File Save Error");
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
            /*
            string today_date = DateTime.Today.Date.ToShortDateString(); ;
            today_date = today_date.Replace("/", "");
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
                        using (OleDbCommand adcommand = new OleDbCommand())
                        {
                            using (OleDbCommand adcommandupdate = new OleDbCommand())
                            {
                                adcommand.CommandText = "SELECT * FROM " + today_date + " WHERE DATE=#" + DateTime.Now.Date.ToShortDateString() + "# and " + " STATUS='PENDING' ";
                                OleDbDataReader read = null;
                                adcommand.Connection = dbConn_SMS;
                                adcommandupdate.Connection = dbConn_SMS;
                                dbConn_SMS.Open();
                                read = adcommand.ExecuteReader();
                                string time = null;
                                if (read.HasRows &&  ping == true)
                                {
                                    msg_send_check = 1;

                                    this.Invoke(new EventHandler(UI_update));
                                    while (read.Read())
                                    {

                                        if (read[3].ToString().Contains("12/30/1899"))
                                        {
                                            time = read[3].ToString();
                                            time = time.Replace("12/30/1899", " ");
                                        }

                                        if (textBox4.Text == "")
                                        {
                                            textBox4.Text = "Alert";
                                        }

                                        textBox2.Text = textBox2.Text.Replace("\r\n", "");
                                        MSG_STR = textBox4.Text + ": \n"
                                                    + "Name: "    + read[2].ToString() +"\n" //name
                                                    + "Card No: " + read[0].ToString() +"\n" //cardid
                                                    + "Status: "+read[7].ToString() +"\n"
                                                    + "Time:"+  time +"\n"
                                                    + textBox2.Text + "\n"
                                                    + "\nPowered By Robotek"; //time

                                    string no = "92" + read[6].ToString();
            */
                                /*
                                   string htmlmessage= readHtmlPage("http://www.sms4connect.com/api/sendsms.php/sendsms/url", MSG_STR, no);
                                        //SendSMS("0" + read[6].ToString(), MSG_STR);
                                        if(htmlmessage.Contains("Invalid API id/password for the customer"))
                                        {
                                            MessageBox.Show("Error : Invalid API id/password for the customer\n Go to Login Settings and enter correct UserName and Password");
                                            msg_send_check = 0;
                                            this.Invoke(new EventHandler(UI_update));
                                            return;
                                        }
                                        adcommandupdate.CommandText = " UPDATE " + today_date + " SET STATUS='SENT' WHERE CARD_ID=" + read[0].ToString() + "AND DATE= #" + DateTime.Now.ToShortDateString() + "# AND " + "STATUS = 'PENDING'";
                                        adcommandupdate.ExecuteNonQuery();
                                */
            /*
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
                if (ex.Message.Contains("cannot find the input table"))
                { }
                else
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
            */
        }
     
        private void button7_Click(object sender, EventArgs e)
        {
            MessageBox.Show("For any query\r\nPlease contact us via email : crg@case.edu.pk ");
        }
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {

            char_count = (textBox2.TextLength + textBox3.TextLength + textBox4.TextLength);
            sms_count = ((textBox2.TextLength + textBox3.TextLength + textBox4.TextLength) / 160) + 1;

            label2.Text = sms_count.ToString();
            label8.Text = char_count.ToString();
        }
        private void button8_Click(object sender, EventArgs e)
        {
            string line = "";

            line = textBox2.Text + "\r\n";

            try
            {
                FileStream fs = new FileStream(Application.StartupPath + "\\Userfiles\\messagetemplate.txt", FileMode.Create, FileAccess.Write);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fs))
                {
                    file.WriteLine(line);
                }
                MessageBox.Show("File Saved");
            }
            catch
            {
                MessageBox.Show("File Save Error");
            }
        }
        void load_message_from_file()
        {
            //loading of text sms to be loaded in the textbox for sending 
            //loaded at form1 constructor
            string line = "";
            if (File.Exists(Application.StartupPath + "\\Userfiles\\messagetemplate.txt"))
            {
                try
                {

                    using (System.IO.StreamReader file = new System.IO.StreamReader(Application.StartupPath + "\\Userfiles\\messagetemplate.txt", true))
                    {
                        line = file.ReadToEnd();
                        textBox2.Text = line;
                        label8.Text = (textBox3.TextLength + textBox2.TextLength).ToString();
                    }

                }
                catch
                {
                    MessageBox.Show("File Read Error");
                }
            }
            else
            {
                MessageBox.Show("Message file not available \r\n Kindly Go to Message Settings Tab, write Message template and save message");
            }

        }
        void load_ID_from_file()
        {
            //loading of IDs to be loaded in the listbox
            //loaded at form1 constructor
           // string line = "";
            if (File.Exists(Application.StartupPath + "\\Userfiles\\ID.txt"))
            {
                try
                {

                    using (System.IO.StreamReader file = new System.IO.StreamReader(Application.StartupPath + "\\Userfiles\\ID.txt", true))
                    {
                        textBox5.Text = file.ReadLine();
                        textBox6.Text = file.ReadLine();                            
                    }

                }
                catch
                {
                    MessageBox.Show("ID File Read Error");
                }
            }
            else
            {
                MessageBox.Show("ID file not available \r\n Kindly go to Login Tab, add Username with Password and click Save");
            }

        }
        void load_IP_from_file()
        {
            //loading of IDs to be loaded in the listbox
            //loaded at form1 constructor
            string line = "";
            if (File.Exists(Application.StartupPath + "\\Userfiles\\IP.txt"))
            {
                try
                {

                    using (System.IO.StreamReader file = new System.IO.StreamReader(Application.StartupPath + "\\Userfiles\\IP.txt", true))
                    {
                        while ((line = file.ReadLine()) != null)

                            listBox1.Items.Add(line);
                    }

                }
                catch
                {
                    MessageBox.Show("IP File Read Error");
                }
            }
            else
            {
                MessageBox.Show("IP file not available \r\n Kindly Go to IP Configuration Tab, Add Client IPs and Click Save");
            }

        }
        private void button9_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            }
        }
        private void button11_Click(object sender, EventArgs e)
        {
            if (tabControl1.Visible == true)
            {
                tabControl1.Hide();
            }
            else
            {
                tabControl1.Show();
            }

        } 
        private void button10_Click(object sender, EventArgs e)
        {
            //this.Hide();
            this.WindowState = FormWindowState.Minimized;
        }
       
        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.google.com.pk"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        private string readHtmlPage(string url , string MSG , string number)
        {
            
            String result = "";
            String message = HttpUtility.UrlEncode(MSG);
            //String message = HttpUtility.UrlEncode("Taimoor Tanweer");
            String strPost = "id="+textBox5.Text+"&pass="+textBox6.Text+"&msg=" + message +
                "&to="+number + "&mask=SMS4CONNECT&type=xml&lang=English";

            //String strPost = "id=92300xxxxxxx&pass=xxxxxxxx&msg=" + message +
            //    "&to=92300xxxxxxx" + "&mask=SMS4CONNECT&type=xml&lang=English";

            StreamWriter myWriter = null;
            HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(url);
            objRequest.Method = "POST";
            objRequest.ContentLength = Encoding.UTF8.GetByteCount(strPost);
            objRequest.ContentType = "application/x-www-form-urlencoded";
            
            try
            {
                myWriter = new StreamWriter(objRequest.GetRequestStream());
                myWriter.Write(strPost);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                myWriter.Close();
            }


            HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();
            using (StreamReader sr = new StreamReader(objResponse.GetResponseStream()))
            {
                result = sr.ReadToEnd();
                // Close and clean up the StreamReader
                sr.Close();
            }
            return result;
             
        }
        public bool PingTest()
        {
            Ping ping = new Ping();

            PingReply pingStatus = ping.Send(IPAddress.Parse("208.69.34.231"));

            if (pingStatus.Status == IPStatus.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                ping = PingTest();
                if (ping)
                {

                    toolStripStatusLabel12.ForeColor = Color.Green;
                    toolStripStatusLabel12.Text = "Available";
                }
                else
                {

                    toolStripStatusLabel12.ForeColor = Color.Red;
                    toolStripStatusLabel12.Text = "Not Available";
                }
            }
            catch
            { }
        
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           SMS_SENDING_THREAD.Start();            
        }
        private void button3_Click(object sender, EventArgs e)
        {

            string line = "";
            line = textBox2.Text + "\r\n";

            try
            {
                FileStream fs = new FileStream(Application.StartupPath + "\\Userfiles\\ID.txt", FileMode.Create, FileAccess.Write);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fs))
                {
                    string htmlmessage = readHtmlPage("http://www.sms4connect.com/api/sendsms.php/sendsms/url", " ", "3455298838");
                    
                    if (htmlmessage.Contains("<code>203"))
                    {
                        MessageBox.Show("Customer account has expired...!! Operation Failed");
                        return;
                    }
                    else if (htmlmessage.Contains("<code>200"))
                    {
                        MessageBox.Show("Invalid Username or Password...!! Operation Failed");
                        return;
                    }
                    else
                    {
                        file.WriteLine(textBox5.Text);
                        file.WriteLine(textBox6.Text);
                        MessageBox.Show("File Saved");
                        if (!SMS_SENDING_THREAD.IsAlive)
                        {
                            SMS_SENDING_THREAD = new Thread(SMS_SENDING);
                            SMS_SENDING_THREAD.IsBackground = true;
                            SMS_SENDING_THREAD.Start();
                        }
                    }
                }
                
            }
            catch
            {
                MessageBox.Show("File Save Error");
            }
        }
        private string update_date_on_server(string name ,string id, string time, string status)
        {

            String result = "";
            
            
            String str_new_url = "http://amnschool.com/biometric/insert?n="+ name +"&i="+id+"&t="+time+"&s="+status;

            HttpWebRequest request = WebRequest.Create(str_new_url) as HttpWebRequest;
            //optional
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            
            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                result = sr.ReadToEnd();
                // Close and clean up the StreamReader
                sr.Close();
            }
            return result;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Value > 99)
                progressBar1.Value = 0;

                progressBar1.Value = progressBar1.Value + 2;

                if (toolStripStatusLabel4.Text == "Connected")
                {
                    timer2.Stop();
                    timer2.Enabled = false;
                    progressBar1.Visible = false;

                }

        }
        
        


    }
}