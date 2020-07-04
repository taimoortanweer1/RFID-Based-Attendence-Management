using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Permissions;
using System.IO.IsolatedStorage;
using System.Diagnostics;

namespace serversocket
{
    public partial class LicenceKey : Form
    {
        //********************** variables for security ********************************************* 
        string mac = null;
        private static byte[] salt = Encoding.ASCII.GetBytes("saltsalt");
        Form1 frm1; 
        //******************************************************

        public LicenceKey()
        {
            InitializeComponent();
            label3.Text = GetMACAddress();
        }

        public LicenceKey( Form1 f1 )
        {
           // frm1 = new Form1();
            frm1 = f1;
            InitializeComponent();
            label3.Text = GetMACAddress();


        }

        private void LicenceKey_Load(object sender, EventArgs e)
        {

            /*
            label3.Text = GetMACAddress();
            if (File.Exists(Environment.ExpandEnvironmentVariables("%windir%") + "\\lic.txt"))
            {

                this.Hide();
                frm1.ShowDialog();
            }
           */
             
        }
        public static string Encrypt(string plainText, string keyString)
        {
            Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(keyString, salt);

            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(new CryptoStream(ms, new RijndaelManaged().CreateEncryptor(key.GetBytes(32), key.GetBytes(16)), CryptoStreamMode.Write));
            sw.Write(plainText);
            sw.Close();
            ms.Close();
            return Convert.ToBase64String(ms.ToArray());
        }
        public static string Decrypt(string base64Text, string keyString)
        {
            Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(keyString, salt);

            ICryptoTransform d = new RijndaelManaged().CreateDecryptor(key.GetBytes(32), key.GetBytes(16));
            byte[] bytes = Convert.FromBase64String(base64Text);
            return new StreamReader(new CryptoStream(new MemoryStream(bytes), d, CryptoStreamMode.Read)).ReadToEnd();
        }
        public string GetMACAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {

                //if (nic.OperationalStatus == OperationalStatus.Up && (!nic.Description.Contains("Virtual") && !nic.Description.Contains("Pseudo")))
                //{
                    if (nic.GetPhysicalAddress().ToString() != "")
                    {
                        sMacAddress = nic.GetPhysicalAddress().ToString();
                        return sMacAddress;
                    }
                //}
            }
            return null;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string key = null;
           // string encrypted_text = null;
            string recoveredmac=null;
           // if (!File.Exists(Environment.ExpandEnvironmentVariables("%windir%") + "\\lic.txt"))
            if (!File.Exists(Application.StartupPath + "\\Userfiles\\data.config"))
            {
                try
                {
                    mac = GetMACAddress();
                    key = "thedarkworld";
                 //   encrypted_text = Encrypt(mac, key);
                    recoveredmac = Decrypt(textBox1.Text,key);
                }
                catch
                {
                    MessageBox.Show("Invalid Password");
                    return;
                }
               // string activeDir = @"%windir%\system32";
                //string newPath = Environment.ExpandEnvironmentVariables("%windir%");
                string newPath = Application.StartupPath + "\\Userfiles";
                string newFileName = "data.config";
                int time = 2016+5269;
                string timestr = time.ToString();
                newPath = System.IO.Path.Combine(newPath, newFileName);
                if (recoveredmac == mac)
                {
                    if (!System.IO.File.Exists(newPath))
                    {

                        using (StreamWriter outfile = new StreamWriter(newPath))
                        {
                            outfile.Write(timestr);
                        }
                        /*
                        using (System.IO.FileStream fs = System.IO.File.Create(newPath))
                        {
                            for (byte i = 0; i < 3; i++)
                            {
                                fs.WriteByte(sevenItems[i]);
                            }
                        }
                         */
                    }

                        MessageBox.Show("Software Activated");
                        //Form1 f = new Form1();
                        this.frm1.Enabled = true;
                        this.frm1.Opacity = 1;
                        
                        this.Hide();
                        frm1.Show();
                }
                else
                { 
                    return; 
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            Process[] myProcess = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            foreach (Process process in myProcess)
            {
                process.Kill();
                //hence call to Application DoEvents forces the MSG
                Application.DoEvents();
            }
        }
    }
}
