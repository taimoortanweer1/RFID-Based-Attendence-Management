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

namespace License_Creation_Project
{
    public partial class Form1 : Form
    {
        string mac = null;
        private static byte[] salt = Encoding.ASCII.GetBytes("saltsalt");
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
             string key            = null;
             string encrypted_text = null;
             string recoveredmac   = null;
            //if (!File.Exists(Environment.ExpandEnvironmentVariables("%windir%") + "\\lic.txt"))
            {
                try
                {
                    //mac = GetMACAddress();
                    mac = textBox1.Text;
                    key = "thedarkworld";
                    encrypted_text = Encrypt(mac, key);
                    textBox1.Text = mac;
                    textBox2.Text = encrypted_text;

                  //  recoveredmac = Decrypt(textBox1.Text, key);
                }
                catch
                {
                    MessageBox.Show("Invalid Password");
                    return;
                }
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
    }
}
