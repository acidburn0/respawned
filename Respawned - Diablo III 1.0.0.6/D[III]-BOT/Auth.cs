using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Security.Cryptography;

namespace BlizzBuddy
{

    class Auth
    {
        private String CPUID = String.Empty;
        private String Email = String.Empty;
        private String Password = String.Empty;
        private String ComputerName = String.Empty;
        private String UserName = String.Empty;
        private CookieContainer CookieCon = new CookieContainer();
        private CookieCollection CookieCol = new CookieCollection();
        //-----------------------------------------------------
        public Auth()
        {
            string cpuInfo = string.Empty;
            ManagementObjectCollection moc = new ManagementClass("win32_processor").GetInstances();
            foreach (ManagementObject mo in moc)
            {
                this.CPUID = mo.Properties["processorID"].Value.ToString();
                break;
            }
            this.UserName = SystemInformation.UserName;
            this.ComputerName = SystemInformation.ComputerName;
        }
        public Boolean Login(String Email, String Password)
        {
            this.Email = Email;
            this.Password = Password;
            String Reqeust = String.Empty; // sendRequest("www.gw-storage.com", "/C#Login/Auth.php", "Email=" + Email + "&Password=" + Password);


            return true;
        }
        private string sendRequest(string host, string path, string post)
        {
            HttpWebRequest request1 = (HttpWebRequest)HttpWebRequest.Create("http://" + host + path);
            request1.Method = "POST";
            request1.Host = host;
            request1.ContentType = "application/x-www-form-urlencoded";

            request1.Referer = "referer";
            request1.CookieContainer = new CookieContainer();
            request1.CookieContainer = CookieCon;
            request1.CookieContainer.Add(CookieCol);
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] loginDataBytes = encoding.GetBytes(post);
            request1.ContentLength = loginDataBytes.Length;

            Stream stream = request1.GetRequestStream();
            stream.Write(loginDataBytes, 0, loginDataBytes.Length);
            stream.Close();

            HttpWebResponse response = (HttpWebResponse)request1.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            string html = sr.ReadToEnd();
            sr.Close();
            CookieCon.Add(response.Cookies);
            CookieCol.Add(response.Cookies);
            response.Close();
            return html;
        }
    }
}
