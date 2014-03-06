using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using Microsoft.Win32;
using System.Xml.Serialization;
using System.Xml;

namespace Respawned
{
    static class Program
    {
        [DllImport("DIIIBData\\D3Api.dll")]
        public static extern int CheckKey(String D3Pfad, System.Text.StringBuilder version);
        public static String ActivateKey = "TRIAL";//String.Empty;
        public static String KeyLifeTime = String.Empty;
        public static Mutex OnlyOneInstance;
        public static Boolean isValid = false;
        public static WebClient CheckVersion = new WebClient();
        public static IPluginManager PluginManager;
        private static IPlugin.Logger _logger = IPlugin.Logger.GetInstance();

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        /// 
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                String CurrentVersion = String.Empty;
                OnlyOneInstance = new System.Threading.Mutex(false, Application.ProductName);
                if (OnlyOneInstance.WaitOne(0, false))
                {
                    //Login
                    new Login();
                    //
                    try
                    {
                        string d3version_file = "D3Version.html";
#if(STAFF_RELEASE)
                        d3version_file = "Staff_D3Version.html";
#endif
                        try
                        {
                            CurrentVersion = CheckVersion.DownloadString("http://auth3.respawned.us/auth/" + d3version_file);
                        }
                        catch (Exception ex)
                        {
                            CurrentVersion = Application.ProductVersion;
                            _logger.Log("[Program error] " + ex);
                        }
                        if (!CurrentVersion.Equals(Application.ProductVersion))
                        {
                            if (MessageBox.Show("There is a new version (" + CurrentVersion + ") available do you want to update?", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Information).Equals(DialogResult.Yes))
                            {
                                string binary_address = "http://auth3.respawned.us/binary/Respawned_v_" + CurrentVersion + ".zip";
#if(STAFF_RELEASE)
                                binary_address = "http://auth3.respawned.us/res_stf/binary/Respawned_staff_v_" + CurrentVersion + ".zip";
#endif
                                Process.Start(binary_address);
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory("DIIIBData\\Quest");
                            Directory.CreateDirectory("DIIIBData\\Plugins");
                            try { File.WriteAllBytes("DIIIBData\\D3Api.dll", Properties.Resources.D3Api); }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error:D3Api.dll is Read-only or already in use. \nPlease terminate all Diablo III processes, delete D3Api.dll or remove the read only attribute from it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                _logger.Log("[D3API] " + ex.ToString());
                                return;
                            }
                            //Console.WriteLine(Datei2MD5("DIIIBData\\D3Api.dll"));
                            try { File.WriteAllBytes("IPlugin.dll", Properties.Resources.IPlugin); }
                            catch { }
                            if (File.Exists("DIIIBData\\D3Api.dll"))// && Datei2MD5("DIIIBData\\D3Api.dll").Equals(CheckVersion.DownloadString("http://auth3.respawned.us/auth/auth.php?CRC")))
                            {
                                Login TestLogin = new Login();
                                bool autoLogin = false;

                                string key = "";
                                try
                                {
                                    autoLogin = Convert.ToInt32(Registry.CurrentUser.OpenSubKey("Software\\DIIIB").GetValue("Autologin")) == 1;
                                }
                                catch{}

                                try
                                {
                                    TestLogin.txt_Profile_AccountEmail.Text = Convert.ToString(Registry.CurrentUser.OpenSubKey("Software\\DIIIB").GetValue("User")).ToString();
                                }
                                catch
                                {
                                    TestLogin.txt_Profile_AccountEmail.Text = "UNKNOWN";
                                }

                                try
                                {
                                    key = Convert.ToString(Registry.CurrentUser.OpenSubKey("Software\\DIIIB").GetValue("Key"));
                                    TestLogin.txt_KeyCode.Text = key;
                                    
                                }
                                catch
                                {
                                    key = "TRIAL";
                                    TestLogin.txt_KeyCode.Text = "TRIAL";
                                }

                                try
                                {
                                    TestLogin.chkAutologin.Checked = autoLogin;
                                }
                                catch { }
                                do
                                {
                                    if (autoLogin || TestLogin.ShowDialog().Equals(DialogResult.OK))
                                    {
                                        ActivateKey = TestLogin.txt_KeyCode.Text;
                                        System.Text.StringBuilder version = new System.Text.StringBuilder(255);
                                        version.Append(Application.ProductVersion.ToCharArray());

                                        if (CheckKey(ActivateKey, version).Equals(0))
                                        {
                                            TestLogin.txt_KeyCode.Text = "Your key isn't valid!!! - " + key;
                                            autoLogin = false;
                                            continue;
                                        }

                                        try
                                        {
                                            if (!TestLogin.txt_KeyCode.Text.Length.Equals(0))
                                                Registry.CurrentUser.CreateSubKey("Software\\DIIIB").SetValue("Key", TestLogin.txt_KeyCode.Text);
                                            if (!TestLogin.txt_Profile_AccountEmail.Text.Length.Equals(0))
                                                Registry.CurrentUser.CreateSubKey("Software\\DIIIB").SetValue("User", TestLogin.txt_Profile_AccountEmail.Text);
                                            Registry.CurrentUser.CreateSubKey("Software\\DIIIB").SetValue("Autologin", TestLogin.chkAutologin.Checked ? "1" : "0");
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.Log("Registry exception: " + ex);
                                            throw;
                                        }
                                        LoadProfiles();

                                        Application.Run(new DIIIBOT());

                                        SaveProfiles();
                                        StopAllProfiles();

                                        PluginManager.UnloadModules();
                                    }
                                    break;
                                } while (true);
                            }
                            else
                                MessageBox.Show("Couldn't find D3Api.dll or it's broken.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An error occured preventing the program to work properly.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _logger.Log("[Program error] " + ex);
                    }
                }
                else
                {
                    MessageBox.Show("This program is already opened.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception e)
            {
                _logger.Log("[Exception] Caught in Main: " + e);
                throw;
            }
            finally
            {
                _logger.Dispose();
            }
        }

        public static void StopAllProfiles()
        {
            for (int i = 0; i < Profile.MyProfiles.Count; ++i)
            {
                if(Profile.MyProfiles[i].State != IPlugin.GameState.None)
                {
                    Profile.MyProfiles[i].StopProfile();
                }
            }
        }

        public static void LoadProfiles()
        {
            try
            {
                if (File.Exists("DIIIBData\\GameData.bin"))
                {
                    XmlSerializer SerializerObj = new XmlSerializer(typeof(List<Profile>));
                    FileStream ReadFileStream = new FileStream("DIIIBData\\GameData.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
                    Profile.MyProfiles = (List<Profile>)SerializerObj.Deserialize(ReadFileStream);
                    ReadFileStream.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("The GameData.bin file you are using is either corrupted or was created with an older version of Respawned.\r\nPlease delete it and restart the program.", "Game Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.Log("[GameData.bin] Error while loading game data. " + e);
                Environment.Exit(0);
            }
        }

        public static bool SaveProfiles()
        {
            try
            {
                XmlSerializer aXmlSer = new XmlSerializer(typeof(List<Profile>));
                XmlTextWriter aXmlWrt = new XmlTextWriter("DIIIBData\\GameData.bin", new System.Text.UTF8Encoding());
                aXmlSer.Serialize(aXmlWrt, Profile.MyProfiles);
                aXmlWrt.Close();
            }
            catch(Exception e)
            {
                MessageBox.Show("There was an error while saving your profiles. Please delete your GameData.bin file and restart the program.", "Game Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.Log("[GameData.bin] Error while saving game data. " + e);
                return false;
            }
            return true;
        }
        private static string Datei2MD5(string Dateipfad)
        {
            //Datei einlesen
            System.IO.FileStream FileCheck = System.IO.File.OpenRead(Dateipfad);
            // MD5-Hash aus dem Byte-Array berechnen
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] md5Hash = md5.ComputeHash(FileCheck);
            FileCheck.Close();
            //in string wandeln
            return BitConverter.ToString(md5Hash).Replace("-", "").ToLower();
        }
        public static DialogResult InputBoxQuestScripts(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            ComboBox comboBoxBox = new ComboBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            comboBoxBox.FlatStyle = FlatStyle.Flat;

            form.Text = title;
            label.Text = promptText;
            comboBoxBox.Text = value;
 
            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            comboBoxBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            comboBoxBox.Anchor = comboBoxBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, comboBoxBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            String[] QList = Directory.GetFiles("DIIIBData\\Quest", "*.D3S");
            String CutDummy = String.Empty;
            foreach (String Dummy in QList)
            {
                CutDummy = Dummy.Substring(16);
                CutDummy = CutDummy.Substring(0, CutDummy.Length - 4);
                comboBoxBox.Items.Add(CutDummy);
            }

            DialogResult dialogResult = form.ShowDialog();
            if(comboBoxBox.SelectedIndex != -1)
                value = comboBoxBox.Items[comboBoxBox.SelectedIndex].ToString();
            return dialogResult;
        }
        public static DialogResult InputBoxInteractByName(int ProfileID, string title, string promptText, ref string value)
        {
            Form form = new Form();

            DataGridView GridView = new DataGridView();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatStyle = FlatStyle.Flat;

            form.Text = title;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            GridView.SetBounds(4, 4, 499, 326);
            buttonOk.SetBounds(408, 336, 95, 23);
            buttonCancel.SetBounds(307, 336, 95, 23);

            form.ClientSize = new Size(507, 365);
            form.Controls.AddRange(new Control[] { GridView, buttonOk, buttonCancel });

            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;


            GridView.ColumnCount = 5;
            GridView.AllowUserToResizeColumns = false;
            GridView.AllowUserToResizeRows =  false;
            GridView.MultiSelect = false;

            GridView.Columns[0].Name = "Type"; 
            GridView.Columns[0].Width = 60;

            GridView.Columns[1].Name = "ModelName";
            GridView.Columns[1].FillWeight = 100;
            GridView.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            GridView.Columns[2].Name = "ACDPtr";
            GridView.Columns[2].Width = 80;

            GridView.Columns[3].Name = "Model ID";
            GridView.Columns[3].Width = 80;

            GridView.Columns[4].Name = "Distance"; 
            GridView.Columns[4].Width = 75;



            GridView.RowHeadersVisible = false;
            GridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            if (Profile.MyProfiles[ProfileID].D3Process != null)
            {
                Profile.MyProfiles[ProfileID].D3Cmd(IPlugin.COMMANDS.D3_Update);
                if (Profile.MyProfiles[ProfileID].D3Mail.D3Info.InGame == 1)
                {
                    for (int i = 0; i < Profile.MyProfiles[ProfileID].D3Mail.D3Info.Actor.Length; ++i)
                    {
                        if (Profile.MyProfiles[ProfileID].D3Mail.D3Info.Actor[i].ACDPTR == 0) continue;
                        GridView.Rows.Add(Enum.GetName(typeof(IPlugin.UnitType), Profile.MyProfiles[ProfileID].D3Mail.D3Info.Actor[i].Type), Profile.MyProfiles[ProfileID].D3Mail.D3Info.Actor[i].Name.Split('-')[0],
                            Profile.MyProfiles[ProfileID].D3Mail.D3Info.Actor[i].ACDPTR.ToString("X"),
                            "0x"+Profile.MyProfiles[ProfileID].D3Mail.D3Info.Actor[i].ModelID.ToString("X"),
                            Math.Round(Math.Sqrt(Math.Pow(Profile.MyProfiles[ProfileID].D3Mail.D3Info.X - Profile.MyProfiles[ProfileID].D3Mail.D3Info.Actor[i].X, 2) + Math.Pow(Profile.MyProfiles[ProfileID].D3Mail.D3Info.Y - Profile.MyProfiles[ProfileID].D3Mail.D3Info.Actor[i].Y, 2)), 0));
                    }
                }
                else
                {
                    GridView.Rows.Add("", "Please stay InGame to use this.");
                }
            }
            else
                GridView.Rows.Add("", "Please run the game before using this.");
            DialogResult dialogResult = form.ShowDialog();
            if (GridView.CurrentRow != null && GridView[1, GridView.CurrentRow.Index].Value != null)
                value =  GridView[1,GridView.CurrentRow.Index].Value.ToString();
            return dialogResult;
        }
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatStyle = FlatStyle.Flat;

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }


        public static Int32 StartResume_Dialog()
        {
            Form form = new Form();
            Label label = new Label();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatStyle = FlatStyle.Flat;

            form.Text = "Start/Resume Quest";
            label.Text = "Choose if you want to start a new quest or continue the old one.";

            buttonOk.Text = "Resume";
            buttonCancel.Text = "Start";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);

            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;

            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, buttonCancel, buttonOk });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            return Convert.ToInt32(form.ShowDialog().Equals(DialogResult.OK));
        }

    }
}
