using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Respawned
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }
        public static WebClient CheckVersion = new WebClient();

        private void Login_Load(object sender, EventArgs e)
        {
            try
            {
                string messageFile = "Message.html";
#if(STAFF_RELEASE)
                messageFile = "Staff_Message.html";
#endif
                String GetImportantMessage = CheckVersion.DownloadString("http://auth3.respawned.us/auth/" + messageFile);
                this.ClientSize = new Size(388, 345);
                rtxt_News.Text = GetImportantMessage;
            }
            catch { this.ClientSize = new Size(388, 130); }
        }
    }
}
