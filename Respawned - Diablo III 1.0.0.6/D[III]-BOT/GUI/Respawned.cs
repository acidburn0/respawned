using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using IPlugin;
using Microsoft.Win32;

namespace Respawned
{
    public partial class DIIIBOT : Form
    {
        int[] allSkillIDs = { 0, 70455, 69979, 30668, 134456, 134872, 79242, 79076, 117402, 136954, 30680, 77552, 69130, 80049, 129216, 129213, 80263, 129214, 133695, 69866, 96311, 223473, 96203, 96019, 75599, 91549, 98878, 1765, 131325, 86991, 77113, 75873, 134209, 97328, 87525, 99120, 77546, 72785, 105963, 67567, 95940, 78548, 30718, 97435, 30624, 69182, 86610, 79446, 80028, 83602, 30631, 67668, 129215, 30725, 73223, 79528, 131366, 97222, 111676, 93409, 69867, 30744, 76108, 95572, 192405, 69490, 69484, 130738, 67600, 69190, 98027, 77649, 123208, 159169, 106465, 103181, 129212, 130831, 131192, 93395, 70472, 109342, 102572, 86989, 129217, 96215, 96694, 130830, 30783, 1769, 130695, 67616, 71548, 75301, 108506, 106237, 78551, 74499, 134030, 102573, 96090, 168344, 121442, 79077, 111215, 134837, 81612, 30796, 96033, 97110, 93885, 96296, 79607, 74003 };
        private IPlugin.Logger _logger = IPlugin.Logger.GetInstance();
        public DIIIBOT()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }
        
        public void GUI_Wrapper(object sender, EventArgs e)
        {
            String ReturnString = String.Empty;
            if (sender.GetType().Equals(typeof(DataGridView)))
            {
                switch (((DataGridView)sender).Name)
                {
                    case "dg_Plugins":
                        if (dg_Plugins.CurrentRow == null) return;
                        rtxt_Plugin_Info.Text = "Autor: " + Program.PluginManager.allPlugins[dg_Plugins.CurrentRow.Index].Autor +
                                                "\nVersion: " + Program.PluginManager.allPlugins[dg_Plugins.CurrentRow.Index].Version;
                        rtxt_Plugin_Description.Text = Program.PluginManager.allPlugins[dg_Plugins.CurrentRow.Index].Description;
                        break;
                }
            }
            if (sender.GetType().Equals(typeof(TabControl)))
            {
                switch (((TabControl)sender).Name)
                {
                    case "tc_bcu":
                        if (tc_bcu.SelectedIndex.Equals(3))
                        {
                            dg_Plugins.Rows.Clear();
                            for (int i = 0; i < Program.PluginManager.allPlugins.Count; ++i)
                            {
                                dg_Plugins.Rows.Add();
                                dg_Plugins[0, i].Value = Program.PluginManager.allPlugins[i].PluginName;
                                dg_Plugins.Rows[i].Height = 15;
                            }
                            GUI_Wrapper(dg_Plugins, null);
                        }
                        break;
                }
            }
            else if (sender.GetType().Equals(typeof(Button)))
            {
                switch (((Button)sender).Name)
                {
                    case "cmd_Add_Profil":
                        if (Program.InputBox("New Profile", "Enter Profile name:", ref ReturnString).Equals(DialogResult.OK))
                        {
                            if (!ReturnString.Equals(String.Empty))
                            {
                                Profile.MyProfiles.Add(new Profile(ReturnString));
                                Profile.MyProfiles[Profile.MyProfiles.Count - 1].AddNewMessageEventHandler += new EventHandler(AddNewMessageToList);
                                dg_Profile.Rows.Add("", Profile.MyProfiles[Profile.MyProfiles.Count - 1].Name);
                                for (int i = 0; i < Program.PluginManager.allPlugins.Count; ++i)
                                {
                                    Program.PluginManager.allPlugins[i].GameDataList.Add(Profile.MyProfiles[Profile.MyProfiles.Count - 1]);
                                }
                                dg_Profile.CurrentCell = dg_Profile[0, dg_Profile.RowCount - 1];
                                Set_GUI_States(null, null);
                            }
                            else
                            {
                                MessageBox.Show("Invalid Profile name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        break;
                    case "cmd_Delete_Profil":
                        if (dg_Profile.CurrentRow == null) return;

                        int save = dg_Profile.CurrentRow.Index;
                        for (int i = 0; i < Program.PluginManager.allPlugins.Count; ++i)
                        {
                            Program.PluginManager.allPlugins[i].GameDataList.RemoveAt(dg_Profile.CurrentRow.Index);
                        }
                        Profile.MyProfiles.RemoveAt(dg_Profile.CurrentRow.Index);

                        dg_Profile.Rows.RemoveAt(dg_Profile.CurrentRow.Index);

                        save = (save >= dg_Profile.RowCount) ? dg_Profile.RowCount - 1 : save;
                        if (save >= 0)
                            dg_Profile.Rows[save].Selected = true;
                        break;
                    case "cmd_Rename_Profil":
                        if (dg_Profile.CurrentRow == null) return;
                        if (Program.InputBox("Rename Profile", "New Profile name:", ref ReturnString).Equals(DialogResult.OK))
                        {
                            if (ReturnString.Equals(String.Empty))
                            {
                                MessageBox.Show("Invalid Profile name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            SelectedProfile.Name = ReturnString;
                            dg_Profile[1, dg_Profile.CurrentRow.Index].Value = SelectedProfile.Name;
                        }
                        break;
                    case "cmd_Start_Profil":
                        if (dg_Profile.CurrentRow == null) return;
                        if (SelectedProfile.D3Process == null && SelectedProfile.State != GameState.Scheduler)
                        {
                            if (!File.Exists(SelectedProfile.D3Pfad))
                            {
                                OpenFileDialog OpenD3 = new OpenFileDialog();
                                OpenD3.Filter = "Diablo III (Diablo III.exe)|Diablo III.exe";
                                if (OpenD3.ShowDialog().Equals(DialogResult.OK) && File.Exists(OpenD3.FileName))
                                {
                                    Registry.CurrentUser.CreateSubKey("Software\\DIIIB").SetValue("Path", OpenD3.FileName);
                                }
                            }
                            SelectedProfile.StartGame = new Thread(SelectedProfile.StartProfile);
                            SelectedProfile.StartGame.Start();
                        }
                        else
                        {
                            SelectedProfile.StopProfile();
                        }
                        break;
                    case "cmd_Shrink":
                        if (this.tc_bcu.Visible)
                        {
                            this.tc_bcu.Visible = false;
                            this.cmd_Shrink.Text = ">";
                            this.Size = new Size(408, this.Size.Height);
                        }
                        else
                        {
                            this.tc_bcu.Visible = true;
                            this.cmd_Shrink.Text = "<";
                            this.Size = new Size(865, this.Size.Height);
                        }
                        break;
                    case "cmd_Start_Quest":
                        if (dg_Profile.CurrentRow.Index >= 0 && SelectedProfile.D3Process != null)
                        {
                            if (SelectedProfile.D3Execute != null && SelectedProfile.D3Execute.IsAlive)
                            {
                                SelectedProfile.StopRunning();
                            }
                            else
                            {
                                SelectedProfile.D3Execute = new Thread(SelectedProfile.PlayQuest);
                                SelectedProfile.D3Execute.Start();
                            }
                        }
                        break;
                    case "cmd_Pause_Quest":
                        SelectedProfile.Paused = !SelectedProfile.Paused;
                        break;
                    case "cmd_Save_Quest":
                        if (cmb_QuestEditorList.SelectedIndex.Equals(-1)) break;
                        if (MessageBox.Show("Do you really want to save '" + cmb_QuestEditorList.Items[cmb_QuestEditorList.SelectedIndex].ToString() + "'?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question).Equals(DialogResult.Yes))
                        {
                            if (SelectedProfile.D3Quest[0].GetType().Equals(typeof(D3SelectThisQuest)))
                            {
                                Int32 Difficulty = cmb_Difficulty.SelectedIndex;
                                Int32 StartResume = Program.StartResume_Dialog();
                                int SubQuest = chk_overridestep.Checked ? -1 : ((D3SelectThisQuest)SelectedProfile.D3Quest[0]).SubQuestID;
                                chk_overridestep.Checked = false;
                                D3SelectThisQuest NewQuestSelect = new D3SelectThisQuest(((D3SelectThisQuest)SelectedProfile.D3Quest[0]).Act, ((D3SelectThisQuest)SelectedProfile.D3Quest[0]).QuestID, SubQuest, Difficulty, StartResume, SelectedProfile.D3Mail.D3Info.MonsterLevel);
                                SelectedProfile.D3Quest[0] = NewQuestSelect;
                                try
                                {

                                    using (Stream stream = File.Open("DIIIBData\\Quest\\" + cmb_QuestEditorList.Items[cmb_QuestEditorList.SelectedIndex].ToString() + ".D3S", FileMode.Create))
                                    {
                                        BinaryFormatter bin = new BinaryFormatter();
                                        bin.Serialize(stream, SelectedProfile.D3Quest);
                                    }
                                    GUI_Wrapper(cmb_QuestList, null);
                                    GUI_Wrapper(cmb_QuestEditorList, null);
                                }
                                catch { MessageBox.Show("Quest couldn't be saved.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                            }
                            else
                            {
                                MessageBox.Show("Quest couldn't be saved. Please make sure that the first is an SelectThisQuest(...) command. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        break;
                    case "cmd_New_Quest":
                        String CurruestName = String.Empty;
                        if (Program.InputBox("Create new Quest", "Quest name:", ref CurruestName).Equals(DialogResult.OK) && !CurruestName.Equals(String.Empty))
                        {
                            if (File.Exists("DIIIBData\\Quest\\" + CurruestName + ".D3S"))
                            {
                                MessageBox.Show("Quest with this name already exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            SelectedProfile.D3Cmd(IPlugin.COMMANDS.D3_Update);
                            if (SelectedProfile.D3Mail.D3Info.InGame == 1)
                            {
                                Int32 Difficulty = cmb_Difficulty.SelectedIndex;

                                Int32 StartResume = Program.StartResume_Dialog();

                                dg_Controls.Rows.Clear();
                                SelectedProfile.D3Quest.Clear();
                                int SubQuest = chk_overridestep.Checked ? -1 : SelectedProfile.D3Mail.D3Info.SubQuestID;
                                chk_overridestep.Checked = false;
                                SelectedProfile.D3Quest.Add(new D3SelectThisQuest(SelectedProfile.D3Mail.D3Info.Act, SelectedProfile.D3Mail.D3Info.QuestID, SubQuest, Difficulty, StartResume, SelectedProfile.D3Mail.D3Info.MonsterLevel));

                                dg_Controls.Rows.Add("SelectQuest(" + SelectedProfile.D3Mail.D3Info.Act + ", " + SelectedProfile.D3Mail.D3Info.QuestID + ", " + SelectedProfile.D3Mail.D3Info.SubQuestID + ", " + Difficulty + ")");
                                dg_Controls.Rows[dg_Controls.Rows.Count - 1].Height = 15;
                                dg_Controls.Rows[dg_Controls.Rows.Count - 1].DefaultCellStyle.BackColor = ((D3CMD)SelectedProfile.D3Quest[0]).CMDColor;

                                try
                                {
                                    using (Stream stream = File.Open("DIIIBData\\Quest\\" + CurruestName + ".D3S", FileMode.Create))
                                    {
                                        BinaryFormatter bin = new BinaryFormatter();
                                        bin.Serialize(stream, SelectedProfile.D3Quest);
                                    }
                                    GUI_Wrapper(cmb_QuestEditorList, null);
                                    GUI_Wrapper(cmb_QuestList, null);
                                    GUI_Wrapper(cmb_StartQuestOption, null);
                                    QuestListFill(null, null);
                                    for (int i = 0; i < cmb_QuestEditorList.Items.Count; ++i)
                                    {
                                        if (cmb_QuestEditorList.Items[i].ToString().Equals(CurruestName))
                                        {
                                            cmb_QuestList.SelectedIndex = i;
                                            cmb_QuestEditorList.SelectedIndex = i;
                                            break;
                                        }
                                    }
                                }
                                catch { MessageBox.Show("Quest couldn't be saved.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                            }
                            else
                            {
                                MessageBox.Show("You have to be InGame to Create a new Quest-Profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        break;
                    case "cmd_Delete_Quest":
                        if (cmb_QuestEditorList.SelectedIndex.Equals(-1)) break;
                        String Dat = cmb_QuestEditorList.Items[cmb_QuestEditorList.SelectedIndex].ToString();
                        if (MessageBox.Show("Do you really want delete '" + Dat + "'?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Information).Equals(DialogResult.Yes))
                        {
                            if (!File.Exists("DIIIBData\\Quest\\" + Dat + ".D3S")) break;
                            File.Delete("DIIIBData\\Quest\\" + Dat + ".D3S");
                            cmb_QuestEditorList.Text = String.Empty;
                            dg_Controls.Rows.Clear();
                            SelectedProfile.QuestName = String.Empty;
                        }
                        break;
                    case "cmd_DemoPlay":
                        if (dg_Profile.CurrentRow.Index >= 0 && SelectedProfile.D3Process != null)
                        {
                            if (SelectedProfile.D3Execute != null && SelectedProfile.D3Execute.IsAlive)
                            {
                                SelectedProfile.StopRunning(true);
                            }
                            else
                            {
                                Int32 StartIndex = dg_Controls.CurrentRow.Index;
                                SelectedProfile.D3Execute = new Thread(SelectedProfile.PlayQuest);
                                SelectedProfile.D3Execute.Start(StartIndex);
                            }
                        }
                        break;
                    case "cmd_DemoDelete":
                        if (dg_Controls.CurrentRow.Index != 0)
                        {
                            SelectedProfile.D3Quest.RemoveAt(dg_Controls.CurrentRow.Index);
                            dg_Controls.Rows.RemoveAt(dg_Controls.CurrentRow.Index);
                        }
                        break;
                    case "cmd_DemoUP":
                        if (dg_Controls.CurrentRow.Index > 1)
                        {
                            Object SwapBuffer = dg_Controls[0, dg_Controls.CurrentRow.Index].Value;
                            dg_Controls[0, dg_Controls.CurrentRow.Index].Value = dg_Controls[0, dg_Controls.CurrentRow.Index - 1].Value;
                            dg_Controls[0, dg_Controls.CurrentRow.Index - 1].Value = SwapBuffer;

                            SwapBuffer = SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index];
                            SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index] = SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index - 1];
                            SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index - 1] = (D3CMD)SwapBuffer;

                            dg_Controls.CurrentCell = dg_Controls[0, dg_Controls.CurrentRow.Index - 1];

                            dg_Controls.Rows[dg_Controls.CurrentRow.Index].DefaultCellStyle.BackColor = ((D3CMD)SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index]).CMDColor;
                            dg_Controls.Rows[dg_Controls.CurrentRow.Index + 1].DefaultCellStyle.BackColor = ((D3CMD)SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index + 1]).CMDColor;
                        }
                        break;
                    case "cmd_DemoDOWN":
                        if (dg_Controls.CurrentRow.Index != 0 && dg_Controls.CurrentRow.Index < dg_Controls.Rows.Count - 1)
                        {
                            Object SwapBuffer = dg_Controls[0, dg_Controls.CurrentRow.Index].Value;
                            dg_Controls[0, dg_Controls.CurrentRow.Index].Value = dg_Controls[0, dg_Controls.CurrentRow.Index + 1].Value;
                            dg_Controls[0, dg_Controls.CurrentRow.Index + 1].Value = SwapBuffer;

                            SwapBuffer = SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index];
                            SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index] = SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index + 1];
                            SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index + 1] = (D3CMD)SwapBuffer;

                            dg_Controls.CurrentCell = dg_Controls[0, dg_Controls.CurrentRow.Index + 1];

                            dg_Controls.Rows[dg_Controls.CurrentRow.Index].DefaultCellStyle.BackColor = ((D3CMD)SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index]).CMDColor;
                            dg_Controls.Rows[dg_Controls.CurrentRow.Index - 1].DefaultCellStyle.BackColor = ((D3CMD)SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index - 1]).CMDColor;
                        }
                        break;
                    case "cmd_QE_Position":
                        try
                        {
                            SelectedProfile.D3Cmd(IPlugin.COMMANDS.D3_Update);
                            SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3Point(SelectedProfile.D3Mail.D3Info.X, SelectedProfile.D3Mail.D3Info.Y));
                            InsertNewCommand();
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            MessageBox.Show("Could not add the move command.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);//Need to look into this BUG. Sometimes crashes the program. Test: Start empty profile, login, add a moveTo command in the quest editor
                        }
                        break;
                    case "cmd_QE_Waypoint":
                        String Index = String.Empty;
                        if (Program.InputBox("Waypoint", "Please enter the index(begins with zero [0]):", ref Index).Equals(DialogResult.OK) && !Index.Equals(String.Empty))
                        {
                            SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3Waypoint(Convert.ToInt32(Index)));
                            InsertNewCommand();
                        }
                        break;
                    case "cmd_QE_ByName":
                        cmd_QE_ByName.Enabled = false;
                        cmd_QE_ByName.Text = "loading...";
                        if (Program.InputBoxInteractByName(dg_Profile.CurrentRow.Index, "Search", "Actor name:", ref ReturnString).Equals(DialogResult.OK) && !ReturnString.Equals(String.Empty))
                        {
                            SelectedProfile.D3Mail.D3Info.ActorByName.Name = ReturnString;
                            SelectedProfile.D3Cmd(IPlugin.COMMANDS.D3_Update);
                            if (SelectedProfile.D3Mail.D3Info.ActorByName.GUID != 0)
                            {
                                SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3Interact(ReturnString, SelectedProfile.D3Mail.D3Info.ActorByName.ModelID));
                            }
                            else
                            {
                                MessageBox.Show("Actor: " + ReturnString + " doesn't exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                cmd_QE_ByName.Text = "Interact By Name";
                                cmd_QE_ByName.Enabled = true;
                                break;
                            }
                        }
                        else
                        {
                            cmd_QE_ByName.Text = "Interact By Name";
                            cmd_QE_ByName.Enabled = true;
                            break;
                        }
                        cmd_QE_ByName.Text = "Interact By Name";
                        cmd_QE_ByName.Enabled = true;
                        InsertNewCommand();
                        break;
                    case "cmd_QE_Search_Model":

                        break;

                    case "cmd_QE_AggroRange":
                        SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3AggroRange(Convert.ToInt32(num_QE_AggroRange.Value)));
                        InsertNewCommand();
                        break;
                    case "cmd_QE_SkipScene":
                        SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3SkipScene());
                        InsertNewCommand();
                        break;
                    case "cmd_QE_Sleep":
                        SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3Sleep(Convert.ToInt32(num_QE_Sleep_Time.Value)));
                        InsertNewCommand();
                        break;
                    case "cmd_QE_QuestStep_Reach":
                        String CurruestStep = String.Empty;
                        SelectedProfile.D3Cmd(IPlugin.COMMANDS.D3_Update);
                        String CurrQuestStep = SelectedProfile.D3Mail.D3Info.QuestStep.ToString();
                        if (Program.InputBox("QuestStep", "Step ( current: " + CurrQuestStep + " ) value:", ref CurruestStep).Equals(DialogResult.OK) && !CurruestStep.Equals(String.Empty))
                        {
                            SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3WaitQuestStepReach(Convert.ToInt32(CurruestStep)));
                            InsertNewCommand();
                        }
                        break;
                    case "cmd_QE_Sart_Other_Quest":
                        if (Program.InputBoxQuestScripts("Select QuestScript", "Choose", ref ReturnString).Equals(DialogResult.OK) && !ReturnString.Equals(String.Empty))
                        {
                            SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3LoadQuestScript(ReturnString));
                            InsertNewCommand();
                        }
                        break;
                    case "cmd_QE_Loop_Quest":
                        SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3LoopQuest());
                        InsertNewCommand();
                        break;
                    case "cmd_QE_Townportal":
                        SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3Townportal());
                        InsertNewCommand();
                        break;
                    case "cmd_QE_Comment":
                        String Comment = String.Empty;
                        if (Program.InputBox("Comment", "Please insert your comment:", ref Comment).Equals(DialogResult.OK) && !Comment.Equals(String.Empty))
                        {
                            SelectedProfile.D3Quest.Insert(dg_Controls.CurrentRow.Index + 1, new D3Comment(Comment));
                            InsertNewCommand();
                        }
                        break;
                }
            }
            else if (sender.GetType().Equals(typeof(CheckBox)))
            {
                switch (((CheckBox)sender).Name)
                {
                    case "chk_UsePotion":
                        SelectedProfile.D3Mail.D3Info.Settings.UsePotion = Convert.ToInt32(chk_UsePotion.Checked);
                        break;
                    case "chk_Topaz":
                        SelectedProfile.D3Mail.D3Info.Settings.Topaz = Convert.ToInt32(chk_Topaz.Checked);
                        break;
                    case "chk_Amethyst":
                        SelectedProfile.D3Mail.D3Info.Settings.Amethyst = Convert.ToInt32(chk_Amethyst.Checked);
                        break;
                    case "chk_Emerald":
                        SelectedProfile.D3Mail.D3Info.Settings.Emerald = Convert.ToInt32(chk_Emerald.Checked);
                        break;
                    case "chk_Ruby":
                        SelectedProfile.D3Mail.D3Info.Settings.Ruby = Convert.ToInt32(chk_Ruby.Checked);
                        break;
                    case "Chk_Pages":
                        SelectedProfile.D3Mail.D3Info.Settings.Pages = Convert.ToInt32(Chk_Pages.Checked);
                        break;
                    case "Chk_Tomes":
                        SelectedProfile.D3Mail.D3Info.Settings.Tomes = Convert.ToInt32(Chk_Tomes.Checked);
                        break;
                    case "Chk_LootTable":
                        SelectedProfile.D3Mail.D3Info.Settings.LootTable = Convert.ToInt32(Chk_LootTable.Checked);
                        break;
                    case "chk_EnlightenedShrine":
                        SelectedProfile.D3Mail.D3Info.Settings.EnlightenedShrine = Convert.ToInt32(chk_EnlightenedShrine.Checked);
                        break;
                    case "chk_FrenziedShrine":
                        SelectedProfile.D3Mail.D3Info.Settings.FrenziedShrine = Convert.ToInt32(chk_FrenziedShrine.Checked);
                        break;
                    case "chk_FortuneShrine":
                        SelectedProfile.D3Mail.D3Info.Settings.FortuneShrine = Convert.ToInt32(chk_FortuneShrine.Checked);
                        break;
                    case "chk_BlessedShrine":
                        SelectedProfile.D3Mail.D3Info.Settings.BlessedShrine = Convert.ToInt32(chk_BlessedShrine.Checked);
                        break;
                    case "chk_EmpoweredShrine":
                        SelectedProfile.D3Mail.D3Info.Settings.EmpoweredShrine = Convert.ToInt32(chk_EmpoweredShrine.Checked);
                        break;
                    case "chk_FleetingShrine":
                        SelectedProfile.D3Mail.D3Info.Settings.FleetingShrine = Convert.ToInt32(chk_FleetingShrine.Checked);
                        break;
                    case "chk_HealingWell":
                        SelectedProfile.D3Mail.D3Info.Settings.HealingWell = Convert.ToInt32(chk_HealingWell.Checked);
                        num_UseHealingWellsAt.Visible = chk_HealingWell.Checked ? true : false;
                        HwHPpL.Visible = chk_HealingWell.Checked ? true : false;
                        break;
                    case "chk_OpenChests":
                        SelectedProfile.D3Mail.D3Info.Settings.OpenChests = Convert.ToInt32(chk_OpenChests.Checked);
                        break;
                    case "chk_Autologin":
                        SelectedProfile.AutoLogin = ((CheckBox)sender).Checked;
                        break;
                    case "chk_DisableGraphic":
                        SelectedProfile.D3Mail.D3Info.GraphicFlag = Convert.ToInt32(((CheckBox)sender).Checked);
                        break;
                    case "chk_StartThisProfileWhenLauncherStarts":
                        SelectedProfile.StartProfileWhenLaucherStarts = ((CheckBox)sender).Checked;
                        break;
                }
            }
            else if (sender.GetType().Equals(typeof(ComboBox)))
            {
                switch (((ComboBox)sender).Name)
                {
                    case "cmb_PotionLevel":
                        SelectedProfile.D3Mail.D3Info.Settings.PotionLevel = cmb_PotionLevel.SelectedIndex;
                        break;
                    case "cmb_GEM_Quality":
                        SelectedProfile.D3Mail.D3Info.Settings.GEM_Quality = cmb_GEM_Quality.SelectedIndex;
                        break;
                    case "cmb_QuestList":
                        //LoadQuest
                        if (cmb_QuestList.SelectedIndex.Equals(-1)) break;
                        try
                        {
                            using (Stream stream = File.Open("DIIIBData\\Quest\\" + cmb_QuestList.Items[cmb_QuestList.SelectedIndex].ToString() + ".D3S", FileMode.Open))
                            {
                                BinaryFormatter bin = new BinaryFormatter();
                                SelectedProfile.D3Quest = (List<D3CMD>)bin.Deserialize(stream);
                            }
                            cmb_QuestEditorList.SelectedIndex = cmb_QuestList.SelectedIndex;
                        }
                        catch
                        {
                            MessageBox.Show("Questscript doesn't exist or Invalid", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;
                    case "cmb_QuestEditorList":
                        //LoadQuest
                        if (cmb_QuestEditorList.SelectedIndex.Equals(-1)) break;
                        try
                        {
                            using (Stream stream = File.Open("DIIIBData\\Quest\\" + cmb_QuestEditorList.Items[cmb_QuestEditorList.SelectedIndex].ToString() + ".D3S", FileMode.Open))
                            {
                                BinaryFormatter bin = new BinaryFormatter();
                                SelectedProfile.D3Quest = (List<D3CMD>)bin.Deserialize(stream);
                            }
                            cmb_QuestList.SelectedIndex = cmb_QuestEditorList.SelectedIndex;
                            dg_Controls.Rows.Clear();
                            for (int i = 0; i < SelectedProfile.D3Quest.Count; ++i)
                            {
                                dg_Controls.Rows.Add(SelectedProfile.D3Quest[i].ToString());
                            }

                            cmb_Difficulty.SelectedIndex = ((D3SelectThisQuest)SelectedProfile.D3Quest[0]).Difficulty;
                            tb_Monster_Level.Value = ((D3SelectThisQuest)SelectedProfile.D3Quest[0]).MonsterLevel;

                            //Highlight
                            for (int i = 0; i < dg_Controls.Rows.Count; ++i)
                            {
                                dg_Controls.Rows[i].Height = 15;
                                dg_Controls.Rows[i].DefaultCellStyle.BackColor = ((D3CMD)SelectedProfile.D3Quest[i]).CMDColor;
                            }
                        }
                        catch
                        {
                            MessageBox.Show("Questscript doesn't exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;
                    case "cmb_LootItemQuality":
                        SelectedProfile.D3Mail.D3Info.Settings.LootItemQuality = ((ComboBox)sender).SelectedIndex - 1;
                        break;
                    case "cmb_SellItemQuality":
                        SelectedProfile.D3Mail.D3Info.Settings.SellItemQuality = ((ComboBox)sender).SelectedIndex - 1;
                        break;
                    case "cmb_StartQuestOption":
                        if (!cmb_StartQuestOption.SelectedIndex.Equals(-1))
                            SelectedProfile.StartScript = cmb_StartQuestOption.Items[cmb_StartQuestOption.SelectedIndex].ToString();
                        break;
                    case "cmb_UnstuckSpell":
                        SelectedProfile.D3Mail.D3Info.Settings.UnstuckSpell = cmb_UnstuckSpell.SelectedIndex;
                        break;
                }
            }
            else if (sender.GetType().Equals(typeof(TextBox)))
            {
                switch (((TextBox)sender).Name)
                {
                    case "txt_Profile_AccountEmail":
                        SelectedProfile.AccountEmail = ((TextBox)sender).Text;
                        break;
                    case "txt_Profile_AccountPassword":
                        SelectedProfile.AccountPassword = ((TextBox)sender).Text;
                        break;
                    case "txt_Profile_SecretQuestion":
                        SelectedProfile.SecretQuestion = ((TextBox)sender).Text;
                        break;
                }
            }
            else if (sender.GetType().Equals(typeof(RadioButton)))
            {
                switch (((RadioButton)sender).Name)
                {
                    case "radio_Europe":
                        SelectedProfile.BattleNetRealm = 1;
                        break;
                    case "radio_America":
                        SelectedProfile.BattleNetRealm = 2;
                        break;
                    case "radio_Asia":
                        SelectedProfile.BattleNetRealm = 3;
                        break;
                    case "radio_Sell":
                        SelectedProfile.D3Mail.D3Info.Settings.SellSalvage = 1;
                        break;
                    case "radio_Salvage":
                        SelectedProfile.D3Mail.D3Info.Settings.SellSalvage = 2;
                        break;
                    case "radio_SellPotions":
                        SelectedProfile.D3Mail.D3Info.SellPotions = 1;
                        break;
                    case "radio_StashPotions":
                        SelectedProfile.D3Mail.D3Info.SellPotions = 0;
                        break;
                    case "radio_DoNothing":
                        SelectedProfile.StartOption = 0;
                        break;
                    case "radio_Questscript":
                        SelectedProfile.StartOption = 1;
                        break;
                }
            }
            else if (sender.GetType().Equals(typeof(ToolStripStatusLabel)))
            {
                switch (((ToolStripStatusLabel)sender).Name)
                {
                    case "ts_ChangePath":
                        OpenFileDialog OpenD3 = new OpenFileDialog();
                        OpenD3.Filter = "Diablo III (Diablo III.exe)|Diablo III.exe";
                        if (OpenD3.ShowDialog().Equals(DialogResult.OK) && File.Exists(OpenD3.FileName))
                        {
                            Registry.CurrentUser.CreateSubKey("Software\\DIIIB").SetValue("Path", OpenD3.FileName);
                        }
                        break;
                }
            }
            else if (sender.GetType().Equals(typeof(NumericUpDown)))
            {
                switch (((NumericUpDown)sender).Name)
                {
                    case "num_UsePotionPercent":
                        SelectedProfile.D3Mail.D3Info.Settings.UsePotionByPercent = Convert.ToInt32(((NumericUpDown)sender).Value);
                        break;
                    case "num_UseHealingWellsAt":
                        SelectedProfile.D3Mail.D3Info.Settings.UseHealingWellsAt = Convert.ToInt32(((NumericUpDown)sender).Value);
                        break;
                    case "num_PotionStacksAllowed":
                        SelectedProfile.D3Mail.D3Info.PotionStacksAllowed = Convert.ToInt32(((NumericUpDown)sender).Value);
                        break;
                    case "num_MinMoney":
                        SelectedProfile.D3Mail.D3Info.Settings.MoneyMinAmount = Convert.ToInt32(((NumericUpDown)sender).Value);
                        break;
                    case "num_MinItemValue":
                        SelectedProfile.D3Mail.D3Info.Settings.MinItemValue = Convert.ToInt32(((NumericUpDown)sender).Value);
                        break;
                    case "num_ItemMinLevel":
                        SelectedProfile.D3Mail.D3Info.Settings.ItemMinLevel = Convert.ToInt32(((NumericUpDown)sender).Value);
                        break;
                    case "num_RepairMe":
                        SelectedProfile.D3Mail.D3Info.Settings.Repair = Convert.ToInt32(((NumericUpDown)sender).Value);
                        break;
                }
            }
            else if (sender.GetType().Equals(typeof(TrackBar)))
            {
                switch (((TrackBar)sender).Name)
                {
                    case "tb_Monster_Level":
                        SelectedProfile.D3Mail.D3Info.MonsterLevel = Convert.ToInt32(((TrackBar)sender).Value);
                        //MessageBox.Show("Please save to make it work");
                        break;
                }
            }
                
        }
        private void InsertNewCommand()
        {
            dg_Controls.Rows.Insert(dg_Controls.CurrentRow.Index + 1, SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index + 1].ToString());
            dg_Controls.Rows[dg_Controls.CurrentRow.Index + 1].Height = 15;
            dg_Controls.Rows[dg_Controls.CurrentRow.Index + 1].DefaultCellStyle.BackColor = ((D3CMD)SelectedProfile.D3Quest[dg_Controls.CurrentRow.Index + 1]).CMDColor;
            dg_Controls.CurrentCell = dg_Controls[0, dg_Controls.CurrentRow.Index + 1];
        }
        delegate void AddNewMessageToListCallback(object sender, EventArgs e);
        private void AddNewMessageToList(object sender, EventArgs e)
        {
            try
            {
                if (dg_Profile.CurrentRow == null) return;
                if (SelectedProfile.Name.Equals(((Profile.MessageEventArgs)e).Name))
                {
                    if (dg_Commander.InvokeRequired)
                    {
                        dg_Commander.Invoke(new AddNewMessageToListCallback(AddNewMessageToList), new object[] { sender, e });
                    }
                    else
                    {
                        try
                        {
                            dg_Commander.Rows.Add(sender.ToString());
                            dg_Commander.Rows[dg_Commander.Rows.Count - 1].Height = 15;
                            if (dg_Commander.Rows.Count > 0)
                            {
                                dg_Commander.CurrentCell = dg_Commander[0, dg_Commander.Rows.Count - 1];
                                dg_Commander[0, dg_Commander.CurrentRow.Index].Selected = false;
                            }
                            if (dg_Commander.Rows.Count > 100) dg_Commander.Rows.Clear();
                        }
                        catch(Exception ex) { _logger.Log("[GUI Error] AddMessage Error: " + ex); }
                    }
                }
            }
            catch (Exception ex) { _logger.Log("[GUI Error] AddMessage Error: " + ex); }
        }
        public void CloseSettings(object O, FormClosingEventArgs e)
        {
            ((Form)O).Hide();
            e.Cancel = true;
        }
        private void DIIIBOT_Load(object sender, EventArgs e)
        {
            Program.PluginManager = new IPluginManager();
            this.Text = this.GetRandomHandleText();
            GUI_Wrapper(cmd_Shrink, null);
            this.txt_Version.Text = Application.ProductName + " " + Application.ProductVersion;
            GUI_Wrapper(cmb_QuestList, null);
            GUI_Wrapper(cmb_QuestEditorList, null);
            GUI_Wrapper(cmb_StartQuestOption, null);
            if (Profile.MyProfiles.Count.Equals(0))
                Profile.MyProfiles.Add(new Profile("Demo"));
            for (int i = 0; i < Profile.MyProfiles.Count; ++i)
            {
                if (i >= dg_Profile.Rows.Count)
                    dg_Profile.Rows.Add();
                Profile.MyProfiles[i].AddNewMessageEventHandler += new EventHandler(AddNewMessageToList);


                if (Profile.MyProfiles[i].StartProfileWhenLaucherStarts.Equals(true))
                    Profile.MyProfiles[i].StartProfile();
            }
            dg_Profile.Rows[0].Selected = true;
            Set_GUI_States(null, null);
            if (dg_Profile.CurrentRow != null)
            {
                for (int i = 0; i < cmb_StartQuestOption.Items.Count; ++i)
                {
                    if (cmb_StartQuestOption.Items[i].Equals(SelectedProfile.StartScript))
                    {
                        cmb_QuestEditorList.SelectedIndex = i;
                    }
                }
            }
            Program.PluginManager.LoadModules(tc_bcu);
        }
        private void DIIIBOT_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                this.Visible = false;
                NotifyIcon.Visible = true;
            }
        }
        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            NotifyIcon.Visible = false;
        }

        private void Timer_GUI_Update_Tick(object sender, EventArgs e)
        {
            Timer_GUI_Update.Enabled = false;
            for (int i = 0; i < Profile.MyProfiles.Count; ++i)
            {
                if (i >= dg_Profile.Rows.Count)
                    dg_Profile.Rows.Add();
                dg_Profile[0, i].Value = (Profile.MyProfiles[i].D3Mail.D3Info.GraphicFlag.Equals(1))?"@":"";
                dg_Profile[1, i].Value = Profile.MyProfiles[i].Name;
                dg_Profile[2, i].Value = string.Format("{0:n}", Profile.MyProfiles[i].GetGPH) + " GPH";
                dg_Profile[3, i].Value = Profile.MyProfiles[i].State.ToString();
            }
            Int32 MoneyComplete = 0;
            Int32 MoneyCompleteAVG = 0;
            for (int i = 0; i < Profile.MyProfiles.Count; ++i)
            {
                if (Profile.MyProfiles[i].D3Process != null)
                {
                    MoneyComplete += Profile.MyProfiles[i].D3Mail.D3Info.Gold;
                    MoneyCompleteAVG += Profile.MyProfiles[i].GetGPH;
                }
            }
            TS_Gold_Complete.Text = "Total Gold Amount: " + string.Format("{0:n}", MoneyComplete);
            TS_Total_GPH.Text = "Total GPH Amount: " + string.Format("{0:n}", MoneyCompleteAVG);

            for (int i = 0; i< Program.PluginManager.allPlugins.Count; ++i)
            {
                if (dg_Profile.CurrentRow == null) break;
                Program.PluginManager.allPlugins[i].SelectedProfileIndex = dg_Profile.CurrentRow.Index;
            }
            

            if (dg_Profile.CurrentRow != null)
            {
                if (SelectedProfile.State.Equals(IPlugin.GameState.Running) ||
                    SelectedProfile.State.Equals(IPlugin.GameState.Paused))
                {
                    panel_Commands.Enabled = false;
                    panel4.Enabled = false;
                    panel5.Enabled = false;
                    panel2.Enabled = false;
                    panel1.Enabled = false;
                    panel3.Enabled = false;
                    panel_DoByStart.Enabled = false;
                    panel_Repair.Enabled = false;
                    panel_Interact.Enabled = false;
                    tabPage6.Enabled = false;
                    panel_Sell_Quality.Enabled = false;
                    panel_Loot_Quality.Enabled = false;
                    panel_Loot_Money.Enabled = false;
                    panel_loot_GEM.Enabled = false;
                    panel_Unstuck.Enabled = false;

                    cmb_Difficulty.Enabled = false;

                    tb_Monster_Level.Enabled = false;
                }
                else
                {
                    panel_Commands.Enabled = true;
                    panel4.Enabled = true;
                    panel5.Enabled = true;
                    panel2.Enabled = true;
                    panel1.Enabled = true;
                    panel3.Enabled = true;
                    panel_DoByStart.Enabled = true;
                    panel_Repair.Enabled = true;
                    panel_Interact.Enabled = true;
                    tabPage6.Enabled = true;
                    panel_Sell_Quality.Enabled = true;
                    panel_Loot_Quality.Enabled = true;
                    panel_Loot_Money.Enabled = true;
                    panel_loot_GEM.Enabled = true;
                    panel_Unstuck.Enabled = true;

                    cmb_Difficulty.Enabled = true;

                    tb_Monster_Level.Enabled = true;
                }

                lbl_MonsterLevel.Text = "Monster-Level(" + SelectedProfile.D3Mail.D3Info.MonsterLevel + ")";

                if (SelectedProfile.D3Process != null)
                {
                    chk_Autologin.Enabled = false;
                    txt_Profile_AccountEmail.Enabled = false;
                    txt_Profile_AccountPassword.Enabled = false;
                    chk_StartThisProfileWhenLauncherStarts.Enabled = false;
                    txt_Profile_SecretQuestion.Enabled = false;
                    chk_DisableGraphic.Enabled = false;
                    radio_America.Enabled = false;
                    radio_Asia.Enabled = false;
                    radio_Europe.Enabled = false;
                }
                else
                {
                    chk_Autologin.Enabled = true;
                    txt_Profile_AccountEmail.Enabled = true;
                    txt_Profile_AccountPassword.Enabled = true;
                    chk_StartThisProfileWhenLauncherStarts.Enabled = true;
                    txt_Profile_SecretQuestion.Enabled = true;
                    chk_DisableGraphic.Enabled = true;
                    radio_America.Enabled = true;
                    radio_Asia.Enabled = true;
                    radio_Europe.Enabled = true;
                }

                cmd_DemoPlay.Text = (SelectedProfile.D3Execute != null && SelectedProfile.D3Execute.IsAlive) ? "Stop" : "Start";
                cmd_Start_Quest.Text = (SelectedProfile.D3Execute != null && SelectedProfile.D3Execute.IsAlive) ? "Stop" : "Start";

                cmd_Start_Profil.Text = (SelectedProfile.D3Process == null && SelectedProfile.State != GameState.Scheduler) ? "Start Profile" : "Stop Profile";
                cmd_Pause_Quest.Text = (SelectedProfile.Paused) ? "Resume" : "Pause";
                // Editor select Index
                if (SelectedProfile.D3Execute != null && SelectedProfile.D3Execute.IsAlive)
                {
                    if (dg_Controls.Rows.Count > SelectedProfile.D3ExecuteQueueIndex)
                        dg_Controls.CurrentCell = dg_Controls[0, SelectedProfile.D3ExecuteQueueIndex];
                }

                String[] Stats = SelectedProfile.Stats;
                Int32 CurrRow = 0;
                for (int i = 0; i < Stats.Length-1; i=i+2)
                {
                    if (CurrRow >= dg_Info.Rows.Count)
                        dg_Info.Rows.Add("", "");
                    dg_Info[0, CurrRow].Value = Stats[i].ToString();
                    dg_Info[1, CurrRow].Value = Stats[i + 1].ToString();
                    dg_Info.Rows[CurrRow].Height = 15;
                    ++CurrRow;
                }

            }
            Timer_GUI_Update.Enabled = true;
        }

        public string GetRandomHandleText()
        {
            var SHA1 = new SHA1CryptoServiceProvider();

            byte[] arrayData;
            byte[] arrayResult;
            string result = null;
            string temp = null;
            String HandleText = String.Empty;
            for (int i = 0; i < 32; ++i)
                HandleText += Convert.ToChar(new Random().Next(65, 90)).ToString();

            arrayData = Encoding.ASCII.GetBytes(HandleText);
            arrayResult = SHA1.ComputeHash(arrayData);
            for (int i = 0; i < arrayResult.Length; ++i)
            {
                temp = Convert.ToString(arrayResult[i], 16);
                if (temp.Length == 1)
                    temp = "0" + temp;
                result += temp;
            }
            return result;
        }

        private void QuestListFill(object sender, EventArgs e)
        {
            String CutDummy = String.Empty;
            String[] QList = Directory.GetFiles("DIIIBData\\Quest", "*.D3S");
            cmb_QuestEditorList.Items.Clear();
            cmb_QuestList.Items.Clear();
            cmb_StartQuestOption.Items.Clear();
            foreach (String Dummy in QList)
            {
                CutDummy = Dummy.Substring(16);
                CutDummy = CutDummy.Substring(0, CutDummy.Length - 4);
                cmb_QuestEditorList.Items.Add(CutDummy);
                cmb_QuestList.Items.Add(CutDummy);
                cmb_StartQuestOption.Items.Add(CutDummy);
            }
        }

        private void Set_GUI_States(object sender, EventArgs e)
        {
            if (dg_Profile.CurrentRow != null)
            {
                chk_Autologin.Checked = SelectedProfile.AutoLogin;

                chk_DisableGraphic.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.GraphicFlag);
                chk_StartThisProfileWhenLauncherStarts.Checked = SelectedProfile.StartProfileWhenLaucherStarts;

                num_MinMoney.Value = SelectedProfile.D3Mail.D3Info.Settings.MoneyMinAmount;
                num_MinItemValue.Value = SelectedProfile.D3Mail.D3Info.Settings.MinItemValue;
                num_RepairMe.Value = SelectedProfile.D3Mail.D3Info.Settings.Repair;
                num_ItemMinLevel.Value = SelectedProfile.D3Mail.D3Info.Settings.ItemMinLevel;


                cmb_LootItemQuality.SelectedIndex = SelectedProfile.D3Mail.D3Info.Settings.LootItemQuality + 1;
                cmb_SellItemQuality.SelectedIndex = SelectedProfile.D3Mail.D3Info.Settings.SellItemQuality + 1;
                cmb_UnstuckSpell.SelectedIndex = SelectedProfile.D3Mail.D3Info.Settings.UnstuckSpell;

                cmb_PotionLevel.SelectedIndex = SelectedProfile.D3Mail.D3Info.Settings.PotionLevel;

                chk_BlessedShrine.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.BlessedShrine);
                chk_EnlightenedShrine.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.EnlightenedShrine);
                chk_FortuneShrine.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.FortuneShrine);
                chk_FrenziedShrine.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.FrenziedShrine);
                chk_EmpoweredShrine.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.EmpoweredShrine);
                chk_FleetingShrine.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.FleetingShrine);
                chk_HealingWell.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.HealingWell);

                chk_OpenChests.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.OpenChests);

                chk_Topaz.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.Topaz);
                chk_Amethyst.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.Amethyst);
                chk_Emerald.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.Emerald);
                chk_Ruby.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.Ruby);
                Chk_Pages.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.Pages);
                Chk_Tomes.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.Tomes);
                Chk_LootTable.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.LootTable);

                cmb_GEM_Quality.SelectedIndex = SelectedProfile.D3Mail.D3Info.Settings.GEM_Quality;

                txt_Profile_AccountEmail.Text = SelectedProfile.AccountEmail;
                txt_Profile_AccountPassword.Text = SelectedProfile.AccountPassword;
                txt_Profile_SecretQuestion.Text = SelectedProfile.SecretQuestion;

                chk_UsePotion.Checked = Convert.ToBoolean(SelectedProfile.D3Mail.D3Info.Settings.UsePotion);
                num_UsePotionPercent.Value = SelectedProfile.D3Mail.D3Info.Settings.UsePotionByPercent;
                num_UseHealingWellsAt.Value = SelectedProfile.D3Mail.D3Info.Settings.UseHealingWellsAt;
                num_PotionStacksAllowed.Value = SelectedProfile.D3Mail.D3Info.PotionStacksAllowed;


                chkEnableSpellRules.Checked = SelectedProfile.UseSpellRules;

                if (SelectedProfile.UseSpellRules)
                {
                    string rulesPath = SelectedProfile.SpellRulesPath;
                    if (!String.IsNullOrWhiteSpace(rulesPath))
                    {
                        try
                        {
                            LoadSpellRules(rulesPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Spell rules could not be loaded properly. They will be disabled", "Spell Rules Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            _logger.Log("[Spell Rules] Error loading spell rules: " + ex.ToString());
                            SelectedProfile.UseSpellRules = false;
                            ResetSpellRulesGUI();
                        }
                    }
                    else
                    {
                        SelectedProfile.UseSpellRules = false;
                        ResetSpellRulesGUI();
                    }
                }
                else
                {
                    ResetSpellRulesGUI();
                }

                chkScheduleUseSchedule.Checked = SelectedProfile.UseSchedule;
                lstScheduleActiveTimes.Items.Clear();
                foreach(ScheduleEntry scheduleEntry in SelectedProfile.ScheduleEntries)
                {
                    lstScheduleActiveTimes.Items.Add(scheduleEntry);
                }
                btnScheduleRemove.Enabled = SelectedProfile.UseSchedule && lstScheduleActiveTimes.Items.Count > 0;
                tb_Monster_Level.Value = SelectedProfile.D3Mail.D3Info.MonsterLevel;
                chkScheduleBreaks.Checked = SelectedProfile.TakeBreaks;
                nudScheduleBreakInterval.Value = SelectedProfile.BreakIntervalInMinutes;
                nudScheduleBreakLength.Value = SelectedProfile.BreakLengthInMinutes;

                switch (SelectedProfile.BattleNetRealm)
                {
                    case 1: radio_Europe.Checked = true; break;
                    case 2: radio_America.Checked = true; break;
                    case 3: radio_Asia.Checked = true; break; 
                }

                switch (SelectedProfile.D3Mail.D3Info.Settings.SellSalvage)
                {
                    case 1: radio_Sell.Checked = true; break;
                    case 2: radio_Salvage.Checked = true; break;
                }

                switch (SelectedProfile.D3Mail.D3Info.SellPotions)
                {
                    case 0: radio_StashPotions.Checked = true; break;
                    case 1: radio_SellPotions.Checked = true; break;
                }

                switch (SelectedProfile.StartOption)
                {
                    case 0: radio_DoNothing.Checked = true; break;
                    case 1: radio_Questscript.Checked = true; break;
                }
                LoadSellRules();
                if (!SelectedProfile.State.Equals(IPlugin.GameState.Running))
                {
                    QuestListFill(null, null);
                    cmb_StartQuestOption.SelectedIndex = -1;
                    cmb_StartQuestOption.Text = String.Empty;
                    for(int i=0;i< cmb_StartQuestOption.Items.Count;++i)
                    {
                        if (cmb_StartQuestOption.Items[i].Equals(SelectedProfile.StartScript))
                        {
                            cmb_StartQuestOption.SelectedIndex = i;
                            cmb_QuestEditorList.SelectedIndex = i;
                            cmb_QuestList.SelectedIndex = i;
                        }
                    }
                }

                try
                {
                    dg_Commander.Rows.Clear();
                    Int32 Messages = SelectedProfile.OutPutList.Count;
                    
                    for (int i = (((Messages - 10) <= 0) ? 0 : Messages - 10); i < SelectedProfile.OutPutList.Count; ++i)
                    {
                        dg_Commander.Rows.Add(SelectedProfile.OutPutList[i].ToString());
                        dg_Commander.Rows[dg_Commander.Rows.Count - 1].Height = 15;
                        if (dg_Commander.Rows.Count > 0)
                        {
                            dg_Commander.CurrentCell = dg_Commander[0, dg_Commander.Rows.Count - 1];
                            dg_Commander[0, dg_Commander.CurrentRow.Index].Selected = false;
                        }
                    }
                }
                catch (Exception ex) { _logger.Log("[GUI Error] GUIState Error: " + ex); }
            }
        }

        public Profile SelectedProfile
        {
            get { return Profile.MyProfiles[dg_Profile.CurrentRow.Index]; }
        }
        private void dg_Profile_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData.Equals(Keys.F1))
            {
                new Thread(SelectedProfile.DisableEnableGraphic).Start(-1);
                chk_DisableGraphic.Checked = !chk_DisableGraphic.Checked;
            }
        }
        
        private void btnSaveProfiles_Click(object sender, EventArgs e)
        {
            if (Program.SaveProfiles()) 
                MessageBox.Show("Profiles Saved.", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnStartAllProfiles_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(StartAllProfiles);
            t.Start();
        }

        private void StartAllProfiles()
        {
            for (int i = 0; i < dg_Profile.RowCount; ++i)
            {
                if (Profile.MyProfiles[i].D3Process == null)
                {
                    if (!File.Exists(Profile.MyProfiles[i].D3Pfad))
                    {
                        OpenFileDialog OpenD3 = new OpenFileDialog();
                        OpenD3.Filter = "Diablo III (Diablo III.exe)|Diablo III.exe";
                        if (OpenD3.ShowDialog().Equals(DialogResult.OK) && File.Exists(OpenD3.FileName))
                        {
                            Registry.CurrentUser.CreateSubKey("Software\\DIIIB").SetValue("Path", OpenD3.FileName);
                        }
                    }
                    Profile.MyProfiles[i].StartGame = new Thread(Profile.MyProfiles[i].StartProfile);
                    Profile.MyProfiles[i].StartGame.Start();
                    Thread.Sleep(new Random().Next(1000,3000));
                }
            }
            return;
        }
        private void btnStopAllProfiles_Click(object sender, EventArgs e)
        {
            Program.StopAllProfiles();
            Program.StopAllProfiles();//first time doesn't always work, patchwork ftw...
        }

        private void btn_resetStats_Click(object sender, EventArgs e)
        {
            SelectedProfile.Count_Deads = 0;
            SelectedProfile.Count_Disconnects = 0;
            SelectedProfile.Count_Stucks = 0;
            SelectedProfile.Count_D3_Crash = 0;
            SelectedProfile.GoldStart = 0;
            SelectedProfile.D3Mail.D3Info.Gold = 0;
        }

        private void chkEnableSpellRules_CheckedChanged(object sender, EventArgs e)
        {
            tbcSpellRules.Enabled = chkEnableSpellRules.Checked;
            SelectedProfile.UseSpellRules = chkEnableSpellRules.Checked;
            SelectedProfile.D3Mail.D3Info.Settings.SpellRulesEnabled = (chkEnableSpellRules.Checked ? 1 : 0);
            btnSpellRulesSave.Enabled = chkEnableSpellRules.Checked;
            btnSpellRulesLoad.Enabled = chkEnableSpellRules.Checked;
        }

        private int GetSpellId(int index)
        {
            if (index < 0 || index >= allSkillIDs.Length) return 0;
            return allSkillIDs[index];
        }

        private string BuildSpellRuleString()
        {
            string s = String.Empty;
            //Slot 1
            if (nudSkill1.SelectedIndex > 0)
            {
                s += GetSpellId(nudSkill1.SelectedIndex).ToString() + ',';
                s += nudPriority1.Value.ToString() + ',';
                s += (chkRecastSpell1.Checked ? "1" : "0") + ',';
                s += nudMinRes1.Value.ToString() + ',';
                s += nudMaxRes1.Value.ToString() + ',';
                s += nudMinHP1.Value.ToString() + ',';
                s += nudMaxHP1.Value.ToString() + ',';
                s += (chkSpeedIncrease1.Checked ? "1" : "0") + ',';
                s += nudMinMonsters1.Value.ToString() + ',';
                s += nudMonsterRange1.Value.ToString() + ',';
                s += nudCastRange1.Value.ToString() + ',';
                s += nudMinResSec1.Value.ToString() + ',';
                s += nudMaxResSec1.Value.ToString() + '\n';
            }
            //Slot 2
            if (nudSkill2.SelectedIndex > 0)
            {
                s += GetSpellId(nudSkill2.SelectedIndex).ToString() + ',';
                s += nudPriority2.Value.ToString() + ',';
                s += (chkRecastSpell2.Checked ? "1" : "0") + ',';
                s += nudMinRes2.Value.ToString() + ',';
                s += nudMaxRes2.Value.ToString() + ',';
                s += nudMinHP2.Value.ToString() + ',';
                s += nudMaxHP2.Value.ToString() + ',';
                s += (chkSpeedIncrease2.Checked ? "1" : "0") + ',';
                s += nudMinMonsters2.Value.ToString() + ',';
                s += nudMonsterRange2.Value.ToString() + ',';
                s += nudCastRange2.Value.ToString() + ',';
                s += nudMinResSec2.Value.ToString() + ',';
                s += nudMaxResSec2.Value.ToString() + '\n';
            }
            //Slot 3
            if (nudSkill3.SelectedIndex > 0)
            {
                s += GetSpellId(nudSkill3.SelectedIndex).ToString() + ',';
                s += nudPriority3.Value.ToString() + ',';
                s += (chkRecastSpell3.Checked ? "1" : "0") + ',';
                s += nudMinRes3.Value.ToString() + ',';
                s += nudMaxRes3.Value.ToString() + ',';
                s += nudMinHP3.Value.ToString() + ',';
                s += nudMaxHP3.Value.ToString() + ',';
                s += (chkSpeedIncrease3.Checked ? "1" : "0") + ',';
                s += nudMinMonsters3.Value.ToString() + ',';
                s += nudMonsterRange3.Value.ToString() + ',';
                s += nudCastRange3.Value.ToString() + ',';
                s += nudMinResSec3.Value.ToString() + ',';
                s += nudMaxResSec3.Value.ToString() + '\n';
            }
            //Slot 4
            if (nudSkill4.SelectedIndex > 0)
            {
                s += GetSpellId(nudSkill4.SelectedIndex).ToString() + ',';
                s += nudPriority4.Value.ToString() + ',';
                s += (chkRecastSpell4.Checked ? "1" : "0") + ',';
                s += nudMinRes4.Value.ToString() + ',';
                s += nudMaxRes4.Value.ToString() + ',';
                s += nudMinHP4.Value.ToString() + ',';
                s += nudMaxHP4.Value.ToString() + ',';
                s += (chkSpeedIncrease4.Checked ? "1" : "0") + ',';
                s += nudMinMonsters4.Value.ToString() + ',';
                s += nudMonsterRange4.Value.ToString() + ',';
                s += nudCastRange4.Value.ToString() + ',';
                s += nudMinResSec4.Value.ToString() + ',';
                s += nudMaxResSec4.Value.ToString() + '\n';
            }
            //Slot 5
            if (nudSkill5.SelectedIndex > 0)
            {
                s += GetSpellId(nudSkill5.SelectedIndex).ToString() + ',';
                s += nudPriority5.Value.ToString() + ',';
                s += (chkRecastSpell5.Checked ? "1" : "0") + ',';
                s += nudMinRes5.Value.ToString() + ',';
                s += nudMaxRes5.Value.ToString() + ',';
                s += nudMinHP5.Value.ToString() + ',';
                s += nudMaxHP5.Value.ToString() + ',';
                s += (chkSpeedIncrease5.Checked ? "1" : "0") + ',';
                s += nudMinMonsters5.Value.ToString() + ',';
                s += nudMonsterRange5.Value.ToString() + ',';
                s += nudCastRange5.Value.ToString() + ',';
                s += nudMinResSec5.Value.ToString() + ',';
                s += nudMaxResSec5.Value.ToString() + '\n';
            }
            //Slot 6
            if (nudSkill6.SelectedIndex > 0)
            {
                s += GetSpellId(nudSkill6.SelectedIndex).ToString() + ',';
                s += nudPriority6.Value.ToString() + ',';
                s += (chkRecastSpell6.Checked ? "1" : "0") + ',';
                s += nudMinRes6.Value.ToString() + ',';
                s += nudMaxRes6.Value.ToString() + ',';
                s += nudMinHP6.Value.ToString() + ',';
                s += nudMaxHP6.Value.ToString() + ',';
                s += (chkSpeedIncrease6.Checked ? "1" : "0") + ',';
                s += nudMinMonsters6.Value.ToString() + ',';
                s += nudMonsterRange6.Value.ToString() + ',';
                s += nudCastRange6.Value.ToString() + ',';
                s += nudMinResSec6.Value.ToString() + ',';
                s += nudMaxResSec6.Value.ToString() + '\n';
            }
            return s;
        }

        private void ResetSpellRulesGUI()
        {
            nudSkill1.SelectedIndex = 0;
            nudPriority1.Value = 0;
            nudMinRes1.Value = 0;
            nudMaxRes1.Value = 0;
            nudMinHP1.Value = 0;
            nudMaxHP1.Value = 0;
            chkSpeedIncrease1.Checked = false;
            nudMinMonsters1.Value = 0;
            nudMonsterRange1.Value = 0;
            nudCastRange1.Value = 0;
            nudMinResSec1.Value = 0;
            nudMaxResSec1.Value = 0;
            chkRecastSpell1.Checked = false;

            nudSkill2.SelectedIndex = 0;
            nudPriority2.Value = 0;
            nudMinRes2.Value = 0;
            nudMaxRes2.Value = 0;
            nudMinHP2.Value = 0;
            nudMaxHP2.Value = 0;
            chkSpeedIncrease2.Checked = false;
            nudMinMonsters2.Value = 0;
            nudMonsterRange2.Value = 0;
            nudCastRange2.Value = 0;
            nudMinResSec2.Value = 0;
            nudMaxResSec2.Value = 0;
            chkRecastSpell2.Checked = false;

            nudSkill3.SelectedIndex = 0;
            nudPriority3.Value = 0;
            nudMinRes3.Value = 0;
            nudMaxRes3.Value = 0;
            nudMinHP3.Value = 0;
            nudMaxHP3.Value = 0;
            chkSpeedIncrease3.Checked = false;
            nudMinMonsters3.Value = 0;
            nudMonsterRange3.Value = 0;
            nudCastRange3.Value = 0;
            nudMinResSec3.Value = 0;
            nudMaxResSec3.Value = 0;
            chkRecastSpell3.Checked = false;

            nudSkill4.SelectedIndex = 0;
            nudPriority4.Value = 0;
            nudMinRes4.Value = 0;
            nudMaxRes4.Value = 0;
            nudMinHP4.Value = 0;
            nudMaxHP4.Value = 0;
            chkSpeedIncrease4.Checked = false;
            nudMinMonsters4.Value = 0;
            nudMonsterRange4.Value = 0;
            nudCastRange4.Value = 0;
            nudMinResSec4.Value = 0;
            nudMaxResSec4.Value = 0;
            chkRecastSpell4.Checked = false;

            nudSkill5.SelectedIndex = 0;
            nudPriority5.Value = 0;
            nudMinRes5.Value = 0;
            nudMaxRes5.Value = 0;
            nudMinHP5.Value = 0;
            nudMaxHP5.Value = 0;
            chkSpeedIncrease5.Checked = false;
            nudMinMonsters5.Value = 0;
            nudMonsterRange5.Value = 0;
            nudCastRange5.Value = 0;
            nudMinResSec5.Value = 0;
            nudMaxResSec5.Value = 0;
            chkRecastSpell5.Checked = false;

            nudSkill6.SelectedIndex = 0;
            nudPriority6.Value = 0;
            nudMinRes6.Value = 0;
            nudMaxRes6.Value = 0;
            nudMinHP6.Value = 0;
            nudMaxHP6.Value = 0;
            chkSpeedIncrease6.Checked = false;
            nudMinMonsters6.Value = 0;
            nudMonsterRange6.Value = 0;
            nudCastRange6.Value = 0;
            nudMinResSec6.Value = 0;
            nudMaxResSec6.Value = 0;
            chkRecastSpell6.Checked = false;

            chkEnableSpellRules.Checked = false;
            txtLoadedSpellRules.Text = "";

            btnSpellRulesLoad.Enabled = SelectedProfile.UseSpellRules;
            btnSpellRulesSave.Enabled = SelectedProfile.UseSpellRules;
        }
        private void LoadSpellRules(string rulesPath)
        {
            ResetSpellRulesGUI();
            chkEnableSpellRules.Checked = true;
            string rules = String.Empty;
            using (StreamReader sr = new StreamReader(rulesPath, false))
            {
                rules = sr.ReadToEnd();
            }
            txtLoadedSpellRules.Text = Path.GetFileName(rulesPath);
            SelectedProfile.SpellRulesPath = rulesPath;
            SelectedProfile.D3Mail.D3Info.PassedSpellRulePath = rulesPath;
            string[] lines = rules.Split(Char.Parse("\n"));
            string[] line;

            if (lines.Length > 1)
            {
                line = lines[0].Split(',');
                nudSkill1.SelectedIndex = Array.IndexOf(allSkillIDs,int.Parse(line[0]));
                nudPriority1.Value = int.Parse(line[1]);
                chkRecastSpell1.Checked = int.Parse(line[2]) == 1;
                nudMinRes1.Value = int.Parse(line[3]);
                nudMaxRes1.Value = int.Parse(line[4]);
                nudMinHP1.Value = int.Parse(line[5]);
                nudMaxHP1.Value = int.Parse(line[6]);
                chkSpeedIncrease1.Checked = int.Parse(line[7]) == 1;
                nudMinMonsters1.Value = int.Parse(line[8]);
                nudMonsterRange1.Value = int.Parse(line[9]);
                nudCastRange1.Value = int.Parse(line[10]);
                nudMinResSec1.Value = int.Parse(line[11]);
                nudMaxResSec1.Value = int.Parse(line[12]);
            }
            if (lines.Length > 2)
            {
                line = lines[1].Split(',');
                nudSkill2.SelectedIndex = Array.IndexOf(allSkillIDs, int.Parse(line[0]));
                nudPriority2.Value = int.Parse(line[1]);
                chkRecastSpell2.Checked = int.Parse(line[2]) == 1;
                nudMinRes2.Value = int.Parse(line[3]);
                nudMaxRes2.Value = int.Parse(line[4]);
                nudMinHP2.Value = int.Parse(line[5]);
                nudMaxHP2.Value = int.Parse(line[6]);
                chkSpeedIncrease2.Checked = int.Parse(line[7]) == 1;
                nudMinMonsters2.Value = int.Parse(line[8]);
                nudMonsterRange2.Value = int.Parse(line[9]);
                nudCastRange2.Value = int.Parse(line[10]);
                nudMinResSec2.Value = int.Parse(line[11]);
                nudMaxResSec2.Value = int.Parse(line[12]);
            }
            if (lines.Length > 3)
            {
                line = lines[2].Split(',');
                nudSkill3.SelectedIndex = Array.IndexOf(allSkillIDs, int.Parse(line[0]));
                nudPriority3.Value = int.Parse(line[1]);
                chkRecastSpell3.Checked = int.Parse(line[2]) == 1;
                nudMinRes3.Value = int.Parse(line[3]);
                nudMaxRes3.Value = int.Parse(line[4]);
                nudMinHP3.Value = int.Parse(line[5]);
                nudMaxHP3.Value = int.Parse(line[6]);
                chkSpeedIncrease3.Checked = int.Parse(line[7]) == 1;
                nudMinMonsters3.Value = int.Parse(line[8]);
                nudMonsterRange3.Value = int.Parse(line[9]);
                nudCastRange3.Value = int.Parse(line[10]);
                nudMinResSec3.Value = int.Parse(line[11]);
                nudMaxResSec3.Value = int.Parse(line[12]);
            }
            if (lines.Length > 4)
            {
                line = lines[3].Split(',');
                nudSkill4.SelectedIndex = Array.IndexOf(allSkillIDs, int.Parse(line[0]));
                nudPriority4.Value = int.Parse(line[1]);
                chkRecastSpell4.Checked = int.Parse(line[2]) == 1;
                nudMinRes4.Value = int.Parse(line[3]);
                nudMaxRes4.Value = int.Parse(line[4]);
                nudMinHP4.Value = int.Parse(line[5]);
                nudMaxHP4.Value = int.Parse(line[6]);
                chkSpeedIncrease4.Checked = int.Parse(line[7]) == 1;
                nudMinMonsters4.Value = int.Parse(line[8]);
                nudMonsterRange4.Value = int.Parse(line[9]);
                nudCastRange4.Value = int.Parse(line[10]);
                nudMinResSec4.Value = int.Parse(line[11]);
                nudMaxResSec4.Value = int.Parse(line[12]);
            }
            if (lines.Length > 5)
            {
                line = lines[4].Split(',');
                nudSkill5.SelectedIndex = Array.IndexOf(allSkillIDs, int.Parse(line[0]));
                nudPriority5.Value = int.Parse(line[1]);
                chkRecastSpell5.Checked = int.Parse(line[2]) == 1;
                nudMinRes5.Value = int.Parse(line[3]);
                nudMaxRes5.Value = int.Parse(line[4]);
                nudMinHP5.Value = int.Parse(line[5]);
                nudMaxHP5.Value = int.Parse(line[6]);
                chkSpeedIncrease5.Checked = int.Parse(line[7]) == 1;
                nudMinMonsters5.Value = int.Parse(line[8]);
                nudMonsterRange5.Value = int.Parse(line[9]);
                nudCastRange5.Value = int.Parse(line[10]);
                nudMinResSec5.Value = int.Parse(line[11]);
                nudMaxResSec5.Value = int.Parse(line[12]);
            }
            if (lines.Length > 6)
            {
                line = lines[5].Split(',');
                nudSkill6.SelectedIndex = Array.IndexOf(allSkillIDs, int.Parse(line[0]));
                nudPriority6.Value = int.Parse(line[1]);
                chkRecastSpell6.Checked = int.Parse(line[2]) == 1;
                nudMinRes6.Value = int.Parse(line[3]);
                nudMaxRes6.Value = int.Parse(line[4]);
                nudMinHP6.Value = int.Parse(line[5]);
                nudMaxHP6.Value = int.Parse(line[6]);
                chkSpeedIncrease6.Checked = int.Parse(line[7]) == 1;
                nudMinMonsters6.Value = int.Parse(line[8]);
                nudMonsterRange6.Value = int.Parse(line[9]);
                nudCastRange6.Value = int.Parse(line[10]);
                nudMinResSec6.Value = int.Parse(line[11]);
                nudMaxResSec6.Value = int.Parse(line[12]);
            }
        }

        private void btnSpellRulesSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog s = new SaveFileDialog();
            s.AddExtension = true;
            s.DefaultExt = "rsps";
            s.InitialDirectory = SelectedProfile.SpellRulesPath.Length > 0 ? SelectedProfile.SpellRulesPath : Path.GetDirectoryName(Application.ExecutablePath);
            s.FileName = txtLoadedSpellRules.Text.Length > 0 ? txtLoadedSpellRules.Text : SelectedProfile.Name + "_spellrules.rsps";
            s.Filter = "Spell Rules Files (*.rsps)|*.rsps";
            s.Title = "Save Spell Rules";
            DialogResult res = s.ShowDialog();

            if (res == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(s.FileName, false))
                {
                    sw.Write(BuildSpellRuleString());
                }
                SelectedProfile.SpellRulesPath = s.FileName;
            }
        }

        private void btnSpellRulesLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog s = new OpenFileDialog();
            s.DefaultExt = "rsps";
            s.FileName = SelectedProfile.Name + "_spellrules.rsps";
            s.Filter = "Spell Rules Files (*.rsps)|*.rsps";
            s.Title = "Load Spell Rules";
            DialogResult res = s.ShowDialog();

            if (res == DialogResult.OK)
            {
                LoadSpellRules(s.FileName);
            }
        }

        private void ModifySellRule()
        {
            if (lstSellRules.SelectedIndex < 0)
                return;
            SellRule selectedRule = (SellRule)lstSellRules.SelectedItem;
            int selectedIndex = lstSellRules.SelectedIndex;
            SellRuleWindow s = new SellRuleWindow(selectedRule);
            if (s.ShowDialog() == DialogResult.OK)
            {
                lstSellRules.Items.Remove(selectedRule);
                lstSellRules.Items.Insert(selectedIndex, s.SellRule);
                lstSellRules.SelectedItem = s.SellRule;
                SaveSellRules();
            }
        }

        private void SaveSellRules()
        {
            List <SellRule> sellRules = new List<SellRule>();
            for (int i = 0; i < lstSellRules.Items.Count; i++)
            {
                SellRule selectedRule = (SellRule)lstSellRules.Items[i];
                sellRules.Add(selectedRule);
            }
            SelectedProfile.SellRules = sellRules;
        }

        private void LoadSellRules()
        {
            lstSellRules.Items.Clear();
            for (int i = 0; i < SelectedProfile.SellRules.Count; i++)
            {
                lstSellRules.Items.Add(SelectedProfile.SellRules[i]);
            }
        }

        private void btnAddSellRule_Click(object sender, EventArgs e)
        {
            SellRuleWindow s = new SellRuleWindow();
            if (s.ShowDialog() == DialogResult.OK)
            {
                SellRule rule = s.SellRule;
                lstSellRules.Items.Add(rule);
                rule.Priority = lstSellRules.Items.Count;
                SaveSellRules();
            }
        }

        private void btnSellRuleUp_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstSellRules.SelectedIndex;
            if (selectedIndex <= 0)
                return;
            SellRule selectedRule = (SellRule)lstSellRules.SelectedItem;
            lstSellRules.Items.RemoveAt(selectedIndex);
            lstSellRules.Items.Insert(selectedIndex - 1, selectedRule);
            lstSellRules.SelectedIndex = selectedIndex - 1;
            AdjustSellRulesPriorities();
            SaveSellRules();
        }

        private void btnSellRuleDown_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstSellRules.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex == lstSellRules.Items.Count - 1)
                return;
            SellRule selectedRule = (SellRule)lstSellRules.SelectedItem;
            lstSellRules.Items.RemoveAt(selectedIndex);
            lstSellRules.Items.Insert(selectedIndex + 1, selectedRule);
            lstSellRules.SelectedIndex = selectedIndex + 1;
            AdjustSellRulesPriorities();
            SaveSellRules();
        }
        private void AdjustSellRulesPriorities()
        {
            for (int i = 0; i < lstSellRules.Items.Count; i++)
            {
                SellRule selectedRule = (SellRule)lstSellRules.Items[i];
                selectedRule.Priority = i + 1;
            }
        }
        private void btnDeleteSellRule_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstSellRules.SelectedIndex;
            if (selectedIndex < 0)
                return;
            SellRule selectedRule = (SellRule)lstSellRules.SelectedItem;
            lstSellRules.Items.Remove(selectedRule);
            AdjustSellRulesPriorities();
            SaveSellRules();

            lstSellRules.SelectedIndex = selectedIndex < lstSellRules.Items.Count ? selectedIndex : selectedIndex - 1;
        }

        private void btnModifySellRule_Click(object sender, EventArgs e)
        {
            ModifySellRule();
        }

        private void lstSellRules_DoubleClick(object sender, EventArgs e)
        {
            ModifySellRule();
        }

        private void chkScheduleBreaks_CheckedChanged(object sender, EventArgs e)
        {
            nudScheduleBreakInterval.Enabled = chkScheduleBreaks.Checked;
            nudScheduleBreakLength.Enabled = chkScheduleBreaks.Checked;
            SelectedProfile.TakeBreaks = chkScheduleBreaks.Checked;
        }

        private void chkScheduleUseSchedule_CheckedChanged(object sender, EventArgs e)
        {
            lstScheduleActiveTimes.Enabled = chkScheduleUseSchedule.Checked;
            btnScheduleAdd.Enabled = chkScheduleUseSchedule.Checked;
            btnScheduleRemove.Enabled = chkScheduleUseSchedule.Checked && lstScheduleActiveTimes.Items.Count > 0;
            dtpScheduleFrom.Enabled = chkScheduleUseSchedule.Checked;
            dtpScheduleTo.Enabled = chkScheduleUseSchedule.Checked;
            if (chkScheduleRandomize.Checked && chkScheduleUseSchedule.Checked)
                chkScheduleRandomize.Checked = false;
            SelectedProfile.UseSchedule = chkScheduleUseSchedule.Checked;
        }

        private void chkScheduleRandomize_CheckedChanged(object sender, EventArgs e)
        {
            if (chkScheduleUseSchedule.Checked && chkScheduleRandomize.Checked)
                chkScheduleUseSchedule.Checked = false;
            nudScheduleRandomize.Enabled = chkScheduleRandomize.Checked;
        }

        private void btnScheduleAdd_Click(object sender, EventArgs e)
        {
            DateTime start = dtpScheduleFrom.Value;
            DateTime end = dtpScheduleTo.Value;

            if (start.Equals(end))
            {
                MessageBox.Show("Start and end times cannot be the same.", "Schedule Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (start > end)
            {
                MessageBox.Show("Start time must be before end time.", "Schedule Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (ScheduleEntry i in lstScheduleActiveTimes.Items)
            {
                if (start > i.Start && start < i.End || end > i.Start && end < i.End || i.Start > start && i.Start < end || i.End > start && i.End < end || end.Equals(i.End) && start.Equals(i.Start))
                {
                    MessageBox.Show("The time you are trying to add is conflicting with another scheduled time.", "Schedule Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            var item = new ScheduleEntry(dtpScheduleFrom.Value, dtpScheduleTo.Value);
            lstScheduleActiveTimes.Items.Add(item);
            lstScheduleActiveTimes.SelectedItem = item;

            SelectedProfile.ScheduleEntries.Add(item);

            btnScheduleRemove.Enabled = lstScheduleActiveTimes.Items.Count > 0;
        }

        private void btnScheduleRemove_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstScheduleActiveTimes.SelectedIndex;
            if (selectedIndex < 0)
                return;
            var item = (ScheduleEntry)lstScheduleActiveTimes.SelectedItem;
            SelectedProfile.ScheduleEntries.Remove(item);
            lstScheduleActiveTimes.Items.Remove(item);

            if (lstScheduleActiveTimes.Items.Count > 0)
                lstScheduleActiveTimes.SelectedIndex = lstScheduleActiveTimes.Items.Count > selectedIndex ? selectedIndex : (selectedIndex - 1);

            btnScheduleRemove.Enabled = lstScheduleActiveTimes.Items.Count > 0;
        }

        private void nudScheduleBreakLength_ValueChanged(object sender, EventArgs e)
        {
            SelectedProfile.BreakLengthInMinutes = (int)nudScheduleBreakLength.Value;
        }

        private void nudScheduleBreakInterval_ValueChanged(object sender, EventArgs e)
        {
            SelectedProfile.BreakIntervalInMinutes = (int)nudScheduleBreakInterval.Value;
        }


        private void button1_Click(object sender, EventArgs e)
        {
        }
    }
}
