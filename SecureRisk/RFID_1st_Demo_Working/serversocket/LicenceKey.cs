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

namespace serversocket
{
    public partial class LicenceKey : Form
    {
        //********************** variables for security ********************************************* 
        string mac = null;
        private static byte[] salt = Encoding.ASCII.GetBytes("saltsalt");
        //******************************************************

        public LicenceKey()
        {
            InitializeComponent();
            label3.Text = GetMACAddress();
        }

        private void LicenceKey_Load(object sender, EventArgs e)
        {

            Form1 f = null;
            label3.Text = GetMACAddress();
            /*
            if (File.Exists(Application.StartupPath + "\\lic.txt"))
            {
                if (f == null)
                    f = new Form1();
                    this.Hide();
                    f.ShowDialog();                                
            }
             */
            if (File.Exists(Environment.ExpandEnvironmentVariables("%windir%") + "\\lic.txt"))
            {
                if (f == null)
                    f = new Form1();
                    this.Hide();
                    f.ShowDialog();                                
            }
             
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
            //string lic_path = Path.GetPathRoot(Environment.SystemDirectory);
            //string lic_path = Environment.SystemDirectory;
            string key = null;
            string encrypted_text = null;
            string recoveredmac=null;
            if (!File.Exists(Environment.ExpandEnvironmentVariables("%windir%") + "\\lic.txt"))
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
                string newPath = Environment.ExpandEnvironmentVariables("%windir%");
                string newFileName = "lic.txt";
                newPath = System.IO.Path.Combine(newPath, newFileName);
                if (recoveredmac == mac)
                {
                    if (!System.IO.File.Exists(newPath))
                    {

                        using (System.IO.FileStream fs = System.IO.File.Create(newPath))
                        {
                            for (byte i = 0; i < 10; i++)
                            {
                                fs.WriteByte(i);
                            }
                        }
                    }
                    /*
                    using (StreamWriter sw = new StreamWriter(Environment.ExpandEnvironmentVariables("%windir%") + "\\lic.txt"))
                        {
                            sw.Write("Do not delete this file");
                        }
                     */ 
                        MessageBox.Show("Software Activated");
                        Form1 f = new Form1();
                        this.Hide();
                        f.Show();
                }
                else
                { 
                    return; 
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
