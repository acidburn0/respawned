using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Respawned
{
    [Serializable()]
    public abstract class D3CMD
    {
        public Color CMDColor;
        public abstract Boolean Execute(Profile ExecuteFrom);
    }
    [Serializable()]
    public class D3SearchModel : D3CMD
    {
        String ModelName = String.Empty;
        String SceneName = String.Empty;
        public D3SearchModel(String ModelName, String SceneName)
        {
            this.ModelName = ModelName;
            this.SceneName = SceneName;
        }
        public override string ToString()
        {
            return "Search(" + this.ModelName + " in " + this.SceneName + ")";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            return true;
        }
    }
    [Serializable()]
    public class D3Comment : D3CMD
    {
        String Text = String.Empty;
        public D3Comment(String Text)
        {
            this.Text = Text;
        }
        public override string ToString()
        {
            this.CMDColor = Color.Orange;
            return ">>>" + this.Text + "<<<";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            return true;
        }
    }
    [Serializable()]
    public class D3Point : D3CMD
    {
        public Single X;
        public Single Y;
        public D3Point(Single X, Single Y)
        {
            this.X = X;
            this.Y = Y;
        }
        public override string ToString()
        {
            this.CMDColor = Color.White;
            return "MoveTo(" + this.X + ", " + this.Y + ")";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            return ExecuteFrom.MoveTo(((D3Point)ExecuteFrom.D3Quest[ExecuteFrom.D3ExecuteQueueIndex]).X, ((D3Point)ExecuteFrom.D3Quest[ExecuteFrom.D3ExecuteQueueIndex]).Y);
        }
    }
    [Serializable()]
    public class D3Sleep : D3CMD
    {
        public int Time;
        public D3Sleep(int Time)
        {
            this.Time = Time;
        }
        public override string ToString()
        {
            this.CMDColor = Color.White;
            return "Sleep(" + this.Time + ")";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            DateTime CurrTime = DateTime.Now;
            D3Sleep sleep = ExecuteFrom.D3Quest[ExecuteFrom.D3ExecuteQueueIndex] as D3Sleep;
            while (sleep != null && ((DateTime.Now.Ticks - CurrTime.Ticks) / 10000) < sleep.Time)
            {
                if (ExecuteFrom.Paused) return false;
                if (!ExecuteFrom.MoveTo(ExecuteFrom.D3Mail.D3Info.X, ExecuteFrom.D3Mail.D3Info.Y))
                {
                    break;
                }
            }
            return true;
        }
    }

    [Serializable()]
    public class D3AggroRange : D3CMD
    {
        public int Yards;
        public D3AggroRange(int yards)
        {
            this.Yards = yards;
        }
        public override string ToString()
        {
            this.CMDColor = Color.MediumVioletRed;
            return "Aggro Range: " + this.Yards + " yards.";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            ExecuteFrom.D3Mail.D3Info.Settings.AttackRange = this.Yards;
            return true;
        }
    }
    [Serializable()]
    public class D3SkipScene : D3CMD
    {
        public override string ToString()
        {
            this.CMDColor = Color.Chartreuse;
            return "Skip Scene";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            return ExecuteFrom.SkipScene(false);
        }
    }
    [Serializable()]
    public class D3Townportal : D3CMD
    {
        public override string ToString()
        {
            this.CMDColor = Color.LightSteelBlue;
            return "Townportal()";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            return ExecuteFrom.UseTownPortal() > 0;
        }
    }
    [Serializable()]
    public class D3Interact : D3CMD
    {
        public String Name;
        public Int32 SNO_ID;
        public override string ToString()
        {
            this.CMDColor = Color.YellowGreen;
            return "Interact(" + Name + ", " + SNO_ID + ")";
        }
        public D3Interact(String Name, Int32 SNO_ID)
        {
            this.Name = Name;
            this.SNO_ID = SNO_ID;
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            if (!ExecuteFrom.InteractBySNO(this.SNO_ID))
            {
                ExecuteFrom.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Actor not found...");
            }
            return true;
        }
    }
    [Serializable()]
    public class D3Waypoint : D3CMD
    {
        public int Index;
        public D3Waypoint(Int32 Index)
        {
            this.Index = Index;
        }
        public override string ToString()
        {
            this.CMDColor = Color.White;
            return "Waypoint(" + this.Index + ")";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            if (ExecuteFrom.InteractBySNO(0x192A) || ExecuteFrom.InteractBySNO(0x2EEA4) || ExecuteFrom.InteractBySNO(0x36A0D))
            {
                ExecuteFrom.D3Cmd(IPlugin.COMMANDS.D3_UseWaypoint, this.Index);
                Thread.Sleep(1500);
            }
            else
            {
                ExecuteFrom.Message("Waypoint not found. (" + ExecuteFrom.Name + ")");
            }
            return true;
        }
    }
    [Serializable()]
    public class D3WaitQuestStepReach : D3CMD
    {
        public int QuestStep;
        public D3WaitQuestStepReach(int QuestStep)
        {
            this.QuestStep = QuestStep;
        }
        public override string ToString()
        {
            this.CMDColor = Color.White;
            return "Wait while Queststep(" + this.QuestStep + ") not reach";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            do
            {
                if (!ExecuteFrom.MoveTo(ExecuteFrom.D3Mail.D3Info.X, ExecuteFrom.D3Mail.D3Info.Y))
                    return false;
                ExecuteFrom.D3Cmd(IPlugin.COMMANDS.D3_Update);
                if (ExecuteFrom.Paused) return false;
            } while (this.QuestStep != ExecuteFrom.D3Mail.D3Info.QuestStep);
            return true;
        }
    }
    [Serializable()]
    public class D3LoopQuest : D3CMD
    {
        public override string ToString()
        {
            this.CMDColor = Color.LightSalmon;
            return "LoopQuest()";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            ExecuteFrom.D3ExecuteQueueIndex = -1;
            return true;
        }
    }
    [Serializable()]
    public class D3SelectThisQuest : D3CMD
    {
        public int Act;
        public int QuestID;
        public int SubQuestID;
        public int Difficulty;
        public int StartResume;
        public int MonsterLevel;
        public D3SelectThisQuest(int Act, int QuestID, int SubQuestID, int Difficulty, int StartResume, int MonsterLevel)
        {
            this.CMDColor = Color.Black;
            this.Act = Act;
            this.QuestID = QuestID;
            this.SubQuestID = SubQuestID;
            this.Difficulty = Difficulty;
            this.StartResume = StartResume;
            this.MonsterLevel = MonsterLevel;
        }
        public override string ToString()
        {
            this.CMDColor = Color.ForestGreen;
            return ((this.StartResume == 0) ? "StartQuest" : "ResumeQuest") + "(" + this.Act + ", " + this.QuestID + ", " + this.SubQuestID + ", " + this.Difficulty + ")";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            int startResumeCount = 0;
            Random random = new Random();
            ExecuteFrom.D3Cmd(IPlugin.COMMANDS.D3_Update);

            if (ExecuteFrom.D3Mail.D3Info.InGame == 1)
                ExecuteFrom.D3Cmd(IPlugin.COMMANDS.D3_LeaveWorld);
            do{
                if (ExecuteFrom.Paused) return false;
                Thread.Sleep(1000);
                ExecuteFrom.D3Cmd(IPlugin.COMMANDS.D3_Update);
            }while (!ExecuteFrom.D3Mail.D3Info.ReadyToStartQuest);
            Thread.Sleep(random.Next(1500, 2500));

            do
            {
                if (startResumeCount % 3 == 0) ExecuteFrom.D3Cmd(((this.StartResume == 0) ? IPlugin.COMMANDS.D3_SelectQuestStart : IPlugin.COMMANDS.D3_SelectQuestResume), this.Act, this.QuestID, this.SubQuestID, this.Difficulty, this.MonsterLevel);
                Thread.Sleep(random.Next(2500,3500));
                ExecuteFrom.D3Cmd(IPlugin.COMMANDS.D3_Update);
                ++startResumeCount;
            } while (ExecuteFrom.D3Mail.D3Info.InGame != 1);
            return true;
        }
    }
    [Serializable()]
    public class D3LoadQuestScript : D3CMD
    {
        public String Questname;
        public D3LoadQuestScript(String Questname)
        {
            this.Questname = Questname;
        }
        public override string ToString()
        {
            this.CMDColor = Color.LightSalmon;
            return "LoadQuestScript(" + this.Questname + ")";
        }
        public override Boolean Execute(Profile ExecuteFrom)
        {
            try
            {
                using (Stream stream = File.Open("DIIIBData\\Quest\\" + this.Questname + ".D3S", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    ExecuteFrom.D3Quest = (List<D3CMD>)bin.Deserialize(stream);
                }
                ExecuteFrom.D3ExecuteQueueIndex = -1;
            }
            catch(Exception e)
            {
                MessageBox.Show("Questscript doesn't exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExecuteFrom.Logger.Log("[" + ExecuteFrom.Name + "] StopRunning called (D3LoadQuestScript): " + e);
                ExecuteFrom.StopRunning(true);
                return false;
            }
            return true;
        }
    }
}
