
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using IPlugin;
using Microsoft.Win32;
namespace Respawned
{
    [Serializable()]
    public class Profile : GameData
    {
        public static List<Profile> MyProfiles = new List<Profile>();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);
        [XmlIgnore][NonSerialized]
        public Thread D3Execute;
        [XmlIgnore][NonSerialized]
        public Thread StartGame;
        [XmlIgnore][NonSerialized]
        public Thread StuckTimerThread;
        [XmlIgnore][NonSerialized]
        public Thread HangTimerThread;
        [XmlIgnore]
        public Int32 D3ExecuteQueueIndex = 0;
        [XmlIgnore]
        public Boolean Paused = false;
        public int SkipCount = 0;
        public String Name = String.Empty;
        public String AccountEmail = String.Empty;
        public String AccountPassword = String.Empty;
        public String SecretQuestion = String.Empty;
        public String QuestName = String.Empty;
        public String StartScript = String.Empty;
        public Boolean UseSpellRules = false;
        public String SpellRulesPath = String.Empty;
        public Boolean AutoLogin = false;
        public Boolean StartProfileWhenLaucherStarts = false;
        public Int32 BattleNetRealm = 1;
        public Int32 StartOption = 0;
        [XmlIgnore]
        public Int32 Try_Login_Count = 0;

        [XmlIgnore][NonSerialized]
        private Logger _logger = Logger.GetInstance();

        [XmlIgnore]
        public Int32 StuckValue = 100;
        [XmlIgnore]
        public Int32 HangValue = 180;
        [XmlIgnore]
        public Int32 GoldStart;
        [XmlIgnore]
        public Int32 XPStart;
        [XmlIgnore]
        private bool _isStuck = false;
        [XmlIgnore]
        private Dictionary<D3CMD, short> _unstuckAttempts = new Dictionary<D3CMD, short>();
        [XmlIgnore]
        public Int16 _totalUnstuckAttempts;
        [XmlIgnore]
        Random _random = new Random();
        [XmlIgnore]
        int _processID = -1;

        public List<SellRule> SellRules = new List<SellRule>();

        [OptionalField(VersionAdded = 2)]
        public List<ScheduleEntry> ScheduleEntries = new List<ScheduleEntry>();
        [OptionalField(VersionAdded = 2)]
        public Boolean UseSchedule = false;
        [OptionalField(VersionAdded = 2)]
        public Boolean TakeBreaks = false;
        [OptionalField(VersionAdded = 2)]
        public int BreakIntervalInMinutes = 0;
        [OptionalField(VersionAdded = 2)]
        public int BreakLengthInMinutes = 0;

        public class MessageEventArgs : EventArgs
        {
            public MessageEventArgs(String Name)
            {
                this.Name = Name;
            }
            public String Name;
        }
        [XmlIgnore]
        public List<object> OutPutList = new List<object>();
        public void Message(String Message)
        {
            OutPutList.Add(Message);
            if (this.AddNewMessageEventHandler != null)
                this.AddNewMessageEventHandler(Message, new MessageEventArgs(this.Name));
        }
        [XmlIgnore]
        public Logger Logger
        {
            get { return _logger; }
        }

        [XmlIgnore]
        public bool IsStuck
        {
            get { return _isStuck; }
            set { _isStuck = value; }
        }

        [XmlIgnore]
        private Dictionary<D3CMD, short> UnstuckAttempts
        {
            get { return _unstuckAttempts; }
            set { _unstuckAttempts = value; }
        }


        [XmlIgnore]
        private Int16 TotalUnstuckAttempts
        {
            get { return _totalUnstuckAttempts; }
            set { _totalUnstuckAttempts = value; }
        }

        public event EventHandler AddNewMessageEventHandler;
        
        /// <summary>
        /// Tries to find the queue index of the 3rd to last D3Point command.
        /// </summary>
        /// <returns></returns>
        public Int32 DetermineUnstuckQueueIndex()
        {
            if (D3ExecuteQueueIndex < 4)
                return 0;
            int i = D3ExecuteQueueIndex - 3;
            if (this.D3Quest[i] is D3Point)
            {
                return i;
            }

            while (!(this.D3Quest[i] is D3Point))
            {
                ++i;
            }
            return i;
        }

        [XmlIgnore] public List<D3CMD> D3Quest = new List<D3CMD>();
        public void StuckTimerFunction()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (!this.Paused && !this.D3Mail.D3Info.Dead && D3Mail.D3Info.InGame == 1) this.StuckValue--;
                if (this.StuckValue <= 80 && (StuckValue % (_random.Next(4, 11))) == 0)
                {
                    this.IsStuck = true;
                    Thread.Sleep(100);
                }
                if (this.StuckValue.Equals(50))
                {
                    /*//Removing this because it seems to make things bug.
                    if (!UnstuckAttempts.ContainsKey(D3Quest[D3ExecuteQueueIndex]))
                    {
                        UnstuckAttempts.Add(D3Quest[D3ExecuteQueueIndex], 0);
                    }
                    if (UnstuckAttempts[D3Quest[D3ExecuteQueueIndex]] < 2)
                    {
                        IsStuck = true;
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Stuck: Trying to go back 3 steps...");
                        UnstuckAttempts[D3Quest[D3ExecuteQueueIndex]]++;
                        D3ExecuteQueueIndex = DetermineUnstuckQueueIndex();
                        TotalUnstuckAttempts++;
                    }*/
                }
                if (this.StuckValue <= 0 || TotalUnstuckAttempts > 9)
                {
                    this.IsStuck = true;
                    ++this.Count_Stucks;
                    this.StuckValue = this.DefaultStuckValue;
                    this.GoldStart = this.D3Mail.D3Info.Gold = 0;
                    this.StartTime = DateTime.Now;
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Bot is stuck, Leaving Game and Restarting quest.");
                    _logger.Log("[" + this.Name + "]  Bot is stuck, Leaving Game and Restarting quest.");
                    //this.D3Cmd(IPlugin.COMMANDS.D3_TakeScreenShot);
                    this.Paused = true;
                    this.D3ExecuteQueueIndex = 0;
                    this.D3Cmd(COMMANDS.D3_Update);
                    Thread.Sleep(_random.Next(1100, 2000));
                    UseTownPortal();
                    Thread.Sleep(_random.Next(2000, 3000));
                    this.Paused = false;
                    this.State = GameState.Running;
                    Thread.Sleep(_random.Next(1500, 2000));
                    this.D3Cmd(COMMANDS.D3_LeaveWorld);
                }
                if (this.D3Mail.D3Info.InGame != 1 || this.D3Mail.D3Info.Dead || this.D3Mail.D3Info.ReadyToStartQuest) this.D3Cmd(COMMANDS.D3_Update);
                if (this.D3Mail.D3Info.IsDisconnected)
                {
                    ++this.Count_Disconnects;
                    this.TerminateD3();
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Disconnected... :(");
                    _logger.Log("[" + this.Name + "] Bot disconnected, called TerminateD3.");
                    //this.D3Cmd(IPlugin.COMMANDS.D3_TakeScreenShot);
                    return;
                }
            }
        }
        public void HangTimerFunction()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (!this.Paused || this.IsStuck) this.HangValue--;
                if (this.HangValue <= 0)
                {
                    this.HangValue = this.DefaultHangValue;
                    this.GoldStart = this.D3Mail.D3Info.Gold = 0;
                    this.StartTime = DateTime.Now;
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Possible D3 hang, Restarting.");
                    _logger.Log("[" + this.Name + "]  Possible D3 hang, Restarting.");
                    this.IsStuck = false;
                    this.Paused = false;
                    this.StuckValue = this.DefaultStuckValue;
                    TerminateProcess(this.D3Process.Handle, 0);
                    Thread.Sleep(_random.Next(1500, 2000));
                }
            }
        }
        private void ImDeadRevivalMe()
        {
            do
            {
                Thread.Sleep(_random.Next(500, 1200));
                D3Cmd(COMMANDS.D3_Revival);
                Thread.Sleep(_random.Next(1000,2000));
                D3Cmd(COMMANDS.D3_Update);
            } while (this.D3Mail.D3Info.Dead && this.D3Mail.D3Info.Ghosted == 0);

            this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Calculate New Path");

            Single LowestDistance = 9999999.9f;
            Single BufferDistance = 0.0f;
            int StartIndex = 0;
            D3Cmd(COMMANDS.D3_Update);
            for (int i = 0; i < this.D3Quest.Count; ++i)
            {
                if (this.D3Quest[i].GetType().Equals(typeof(D3Point)))
                {
                    if ((BufferDistance = (Single)Math.Sqrt(Math.Pow(((D3Point)this.D3Quest[i]).X - this.D3Mail.D3Info.X, 2) + Math.Pow(((D3Point)this.D3Quest[i]).Y - this.D3Mail.D3Info.Y, 2))) < LowestDistance)
                    {
                        LowestDistance = BufferDistance;
                        StartIndex = i;
                    }
                }
            }
            this.D3ExecuteQueueIndex = StartIndex;
        }
        public int UseTownPortal()
        {
            if (this.D3Cmd(COMMANDS.D3_Update).D3Info.inTown == 1) return 2;
            int tries = 0;
            int OldAttackRange = this.D3Cmd(COMMANDS.D3_Update).D3Info.Settings.AttackRange;
            this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Setting Attack Range to 40 yards");
            while (this.D3Cmd(COMMANDS.D3_Update).D3Info.Settings.AttackRange != 70 && tries < 5)
            {
                this.D3Mail.D3Info.Settings.AttackRange = 40;
                Thread.Sleep(200);
                ++tries;
            }
            tries = 0;
            this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Checking for threats.");
            while ((this.D3Cmd(COMMANDS.D3_AttackMonster).r_1.i != 0 || tries < 20) && !this.D3Mail.D3Info.Dead)
            {
                if ((this.D3Cmd(COMMANDS.D3_AttackMonster).r_1.i == 1))
                {
                    if(tries > 0) --tries;
                    if (this.StuckValue < 100) ++this.StuckValue;
                }
                Thread.Sleep(100);
                ++tries;
            }
            //this.Message("done with threats");
            tries = 0;
            if (this.D3Cmd(COMMANDS.D3_Update).D3Info.Dead) return 0;
            this.StuckValue = this.DefaultStuckValue;
            Thread.Sleep(_random.Next(500,900));
            this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Attempting to use Townportal.");
            while (D3Cmd(COMMANDS.D3_UseTownPortal).r_1.i != 1 && tries < 10 && !this.D3Mail.D3Info.Dead)
            {
                if (this.D3Cmd(COMMANDS.D3_Update).D3Info.Dead) return 0;
                ++tries;
                D3Cmd(COMMANDS.D3_Update);
                if (this.D3Cmd(COMMANDS.D3_AttackMonster).r_1.i != 0 && tries >= 0)
                {
                    ++tries;
                    for (int i = 0; i < 10; ++i)
                    {
                        this.D3Cmd(COMMANDS.D3_AttackMonster);
                        Thread.Sleep(100);
                    }
                }
                Thread.Sleep(_random.Next(1500,2000));
            }
            this.D3Mail.D3Info.Settings.AttackRange = OldAttackRange;
            this.D3Cmd(COMMANDS.D3_Update);
            if (tries == 10) return 0;
            return 1;
        }
        public Boolean SkipScene(bool npc)
        {
            if (npc)
            {
                for (int i = 0; i < _random.Next(10,20); ++i)
                {
                    this.D3Mail.r_11.i = 0;
                    this.D3Cmd(COMMANDS.D3_SkipScene);
                    Thread.Sleep(_random.Next(5, 20));
                }
            }
            else
            {
                this.D3Mail.r_11.i = 1;
                this.D3Cmd(COMMANDS.D3_Update);
                Thread.Sleep(_random.Next(5, 20));
            }
            return true;
        }

        public bool StashItems()
        {
            this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Stashing Items...");
            switch (((D3SelectThisQuest) this.D3Quest[0]).Act)
            {
                case 0:
                    MoveTo(2969, 2793, 3.5f, true);
                    break;
                case 100:
                    MoveTo(325, 226, 3.5f, true);
                    break;
                default:
                    MoveTo(387, 385, 3.5f, true);
                    break;
            }

            Thread.Sleep(_random.Next(700, 900));
            InteractBySNO(130400, false, true);

            this.StuckValue = this.DefaultStuckValue;
            Thread.Sleep(_random.Next(1500, 2000));
            if (this.D3Cmd(COMMANDS.D3_Stash).r_1.i == 1)
            {
                this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Items Stashed...");
            }
            else
            {
                this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Could not stash...");
            }
            this.StuckValue = this.DefaultStuckValue;
            return true;
        }
        public void IdentifyItems()
        {
            this.StuckValue = this.DefaultStuckValue;
            this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "identifying Items...");
            switch (((D3SelectThisQuest)this.D3Quest[0]).Act)
            {
                case 0:
                    MoveTo(2955, 2808, 3.5f, true);
                    MoveTo(2952, 2782, 3.5f, true);
                    this.D3Mail.D3Info.ActorByName.Name = "Id_All_Book_Of_Cain";
                    break;
                case 100:
                    MoveTo(335, 235, 3.5f, true);
                    this.D3Mail.D3Info.ActorByName.Name = "Id_All_Book_Of_Cain";
                    break;
                default:
                    MoveTo(379, 388, 3.5f, true);
                    this.D3Mail.D3Info.ActorByName.Name = "Id_All_Book_Of_Cain";
                    break;
            }
            Thread.Sleep(_random.Next(500, 900));
            if (InteractBySNO(this.D3Cmd(COMMANDS.D3_Update).D3Info.ActorByName.ModelID, false, true))
            {
                this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Waiting...");
                Thread.Sleep(_random.Next(6900, 7900));
            }
            else
            {
                this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Could not identify.");
            }
            this.D3Mail.D3Info.ActorByName.Name = "";
            return;
        }
        public Boolean RepairSellSalvage()
        {
            this.StuckValue = this.DefaultStuckValue;
            Thread.Sleep(1200);
            int townportal_return = this.UseTownPortal();
            if (townportal_return > 0)
            {
                Thread.Sleep(1200);
                this.D3Cmd(COMMANDS.D3_Update);
                Single SaveXPos = this.D3Mail.D3Info.X;
                Single SaveYPos = this.D3Mail.D3Info.Y;
                if (this.D3Cmd(COMMANDS.D3_Update).D3Info.Dead) return false;
                this.D3Mail.p_10.i = 1;
                this.IdentifyItems();
                this.StuckValue = this.DefaultStuckValue;
                Thread.Sleep(_random.Next(1200, 1600));
                MoveToVendorRepairerInTown();
                Thread.Sleep(_random.Next(1200, 1600));
                this.D3Cmd(COMMANDS.D3_Update);
                if (!this.D3Mail.D3Info.Settings.SellItemQuality.Equals(11))
                {
                    InteractBySNO(this.D3Mail.D3Info.ActorByName.ModelID, false, true);
                    Thread.Sleep(_random.Next(1200, 1600));
                    if (this.D3Cmd(COMMANDS.D3_SellItemsLowerQuality).r_1.i == 1)
                    {
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Items Sold...");
                    }
                    else
                    {
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Could not sell...");
                    }
                }
                if (this.D3Cmd(COMMANDS.D3_Update).D3Info.Dead) return false;
                if (!InteractBySNO(this.D3Mail.D3Info.ActorByName.ModelID, false, true))
                {
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Couldn't Repair");
                }
                else
                {
                    this.D3Mail.p_10.i = 1;
                    Thread.Sleep(500);
                    this.D3Cmd(COMMANDS.D3_Repair, 1);
                    Thread.Sleep(500);
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + "Repaired");
                    this.D3Mail.p_10.i = 0;
                }
                this.StuckValue = this.DefaultStuckValue;

                if (this.D3Cmd(COMMANDS.D3_Update).D3Info.Dead) 
                    return false;

                MoveToStash();
                this.StuckValue = this.DefaultStuckValue;
                this.StashItems();

                //MoveTo(SaveXPos, SaveYPos, 3.5f, true);
                MoveToPortalInTown();
                Thread.Sleep(1000);

                if(townportal_return == 1) 
                    this.InteractBySNO(0x2EC04,true,true);

                Thread.Sleep(1000);
                return true;
            }
            return false;
        }
        private void MoveToVendorRepairerInTown()
        {
            switch (((D3SelectThisQuest)this.D3Quest[0]).Act)
            {
                case 0:
                    MoveTo(2909, 2793, 3.5f, true);
                    this.D3Mail.D3Info.ActorByName.Name = "A1_UniqueVendor_Miner_InTown";
                    break;
                case 100:
                    MoveTo(293, 228, 3.5f, true);
                    MoveTo(330, 144, 3.5f, true);
                    this.D3Mail.D3Info.ActorByName.Name = "A2_UniqueVendor_Fence_InTown";
                    break;
                default:
                    MoveTo(351, 401, 3.5f, true);
                    MoveTo(348, 458, 3.5f, true);
                    MoveTo(312, 492, 3.5f, true);
                    if (((D3SelectThisQuest)this.D3Quest[0]).Act == 200)
                        this.D3Mail.D3Info.ActorByName.Name = "A3_UniqueVendor_InnKeeper";
                    else if (((D3SelectThisQuest)this.D3Quest[0]).Act == 300)
                        this.D3Mail.D3Info.ActorByName.Name = "A4_UniqueVendor_InnKeeper";
                    break;
            }
        }
        private void MoveToStash()
        {
            switch (((D3SelectThisQuest)this.D3Quest[0]).Act)
            {
                case 0:
                    MoveTo(2963, 2818, 3.5f, true);
                    break;
                case 100:
                    MoveTo(294, 192, 3.5f, true);
                    MoveTo(311, 264, 3.5f, true);
                    break;
                default:
                    MoveTo(340, 484, 3.5f, true);
                    MoveTo(400, 458, 3.5f, true);
                    MoveTo(400, 436, 3.5f, true);
                    break;
            }
        }
        private void MoveToPortalInTown()
        {
            switch (((D3SelectThisQuest)this.D3Quest[0]).Act)
            {
                case 0:
                    MoveTo(2988, 2796, 1.0f, true);
                    break;
                case 100:
                    MoveTo(309, 273, 1.0f, true);
                    break;
                default:
                    MoveTo(373, 415, 1.0f, true);
                    break;
            }
        }
        public void GameEventWrapper()
        {
            String OldMessage = this.D3Mail.D3Info.retUnit.Name;
            switch(this.D3Mail.D3Info.Settings.State)
            {
                case 1:
                    if (D3Cmd(COMMANDS.D3_AttackMonster).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name) && !this.D3Mail.D3Info.retUnit.Name.Split('-')[0].Equals(String.Empty))
                    {
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Attack: " + this.D3Mail.D3Info.retUnit.Name.Split('-')[0]);
                        this.StuckValue = this.DefaultStuckValue;
                    }
                    break;
                case 2:
                    if (D3Cmd(COMMANDS.D3_PickUpMoney).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name) && !this.D3Mail.D3Info.retUnit.Name.Split('-')[0].Equals(String.Empty))
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Pickup Money: " + this.D3Mail.r_2.i + " gold");
                    break;
                case 3:
                    if (D3Cmd(COMMANDS.D3_GetNearestShrine).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name) && !this.D3Mail.D3Info.retUnit.Name.Split('-')[0].Equals(String.Empty))
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Go Shrine: " + this.D3Mail.D3Info.retUnit.Name.Split('-')[0]);
                    break;
                case 4:
                    if (D3Cmd(COMMANDS.D3_GetNearestChest).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name) && !this.D3Mail.D3Info.retUnit.Name.Split('-')[0].Equals(String.Empty))
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Open Chest: " + this.D3Mail.D3Info.retUnit.Name.Split('-')[0]);
                    break;
                case 5:
                    if (D3Cmd(COMMANDS.D3_GetNearestItemByQuality).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name) && !this.D3Mail.D3Info.retUnit.Name.Split('-')[0].Equals(String.Empty))
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Pickup Item: " + this.D3Mail.D3Info.retUnit.Name.Split('-')[0]);
                    break;
                case 7: //Sell
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Go selling");
                    RepairSellSalvage();
                    break;
                case 8: //Tod
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Died... :( ");
                    ++this.Count_Deads;
                    ImDeadRevivalMe();
                    break;
                case 9: //Repair
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Going for Repair");
                    this.StuckValue = this.DefaultStuckValue;
                    RepairSellSalvage();
                    break;
                case 10:
                    if (D3Cmd(COMMANDS.D3_GetNearestGEMByQuality).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name))
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Pickup GEM: " + this.D3Mail.D3Info.retUnit.Name.Split('-')[0]);
                    break;
                case 11:
                    if (D3Cmd(COMMANDS.D3_UsePotion).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name))
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] UsePotion: " + this.D3Mail.D3Info.retUnit.Name.Split('-')[0]);
                    break;
                case 12:
                    if (D3Cmd(COMMANDS.D3_GetNearestPotionByQuality).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name))
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Pickup Potion: " + this.D3Mail.D3Info.retUnit.Name.Split('-')[0]);
                    break;
                case 13:
                    //loot item in loot list
                    if (D3Cmd(COMMANDS.D3_GetItemOnLootList).r_1.i.Equals(1) && !OldMessage.Equals(this.D3Mail.D3Info.retUnit.Name))
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Pickup Loot: " + this.D3Mail.D3Info.retUnit.Name.Split('-')[0]);
                    break;
            }
            this.D3ExecuteQueueIndex--;
        }
        public override void PlayQuest(Object StartIndex)
        {
            try
            {
                Int32 CurrIndex = -1;
                this.D3Mail.r_11.i = 0;
                this.D3Mail.D3Info.IsDisconnected = false;
                this.D3Mail.D3Info.Settings.AttackRange = 35;
                if (!Convert.ToInt32(StartIndex).Equals(-1))
                    D3ExecuteQueueIndex = Convert.ToInt32(StartIndex);

                this.StuckTimerThread = new Thread(StuckTimerFunction);
                this.StuckTimerThread.Start();
                this.HangTimerThread = new Thread(HangTimerFunction);
                this.HangTimerThread.Start();
                while (this.D3Quest.Count > this.D3ExecuteQueueIndex)
                {
                    if (this.Paused)
                    {
                        this.State = GameState.Paused;
                        continue;
                    }
                    this.State = GameState.Running;

                    if (!CurrIndex.Equals(D3ExecuteQueueIndex))
                        Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] " + this.D3Quest[this.D3ExecuteQueueIndex].ToString());
                    CurrIndex = D3ExecuteQueueIndex;
                    if (!D3Quest[D3ExecuteQueueIndex].Execute(this))
                    {
                        if (this.Paused) continue;
                        GameEventWrapper();
                    }
                    else
                    {
                        this.StuckValue = this.DefaultStuckValue;
                        this.HangValue = this.DefaultHangValue;
                    }
                    ++this.D3ExecuteQueueIndex;
                }
                this.StopRunning(true);
            }
            finally
            {
                this.State = GameState.Active;
                try { this.StuckTimerThread.Abort(); this.StuckTimerThread = null; } catch { }
                try { this.HangTimerThread.Abort(); this.HangTimerThread = null; }  catch { }
                this.StuckValue = this.DefaultStuckValue;
            }
        }

        public Boolean MoveTo(Single X, Single Y, Single Distance = 3.5f, Boolean ForceFlag = false)
        {
            String Message = String.Empty;
            DateTime StruckTimeBeginning = DateTime.Now;
            Single CalcDistance = 0.0f;
            do
            {
                if ((!D3Mail.D3Info.Settings.State.Equals(0) && !ForceFlag) || this.Paused)
                    return false;

                CalcDistance = (Single)Math.Sqrt(Math.Pow(this.D3Mail.D3Info.X - X, 2) + Math.Pow(this.D3Mail.D3Info.Y - Y, 2));
                if(CalcDistance >= Distance)
                    D3Cmd(COMMANDS.D3_MoveTo, X, Y);
                if (IsStuck)
                {
                    IsStuck = !IsStuck;
                    if (this.D3Mail.D3Info.Settings.UnstuckSpell < 6)
                    {
                        this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] Stuck: Trying to cast a spell...");
                        this.D3Cmd(COMMANDS.D3_CastSpellToUnstuck, this.D3Mail.D3Info.Settings.UnstuckSpell, -1, -1, -1, -1);
                    }
                    return false;
                }
            } while (CalcDistance > Distance);
            return true;
        }
        public Boolean InteractBySNO(int SNO_ID, Boolean ForceFlag = false, Boolean DontSkip = false)
        {
            this.D3Mail.p_10.i = 1;
            Int32 InteractCounter = 0;
            this.D3Mail.D3Info.ActorByModelID.ModelID = SNO_ID;
            Int32 GUID = D3Cmd(COMMANDS.D3_Update).D3Info.ActorByModelID.GUID;
            if (GUID == 0)
            {
                this.D3Mail.p_10.i = 0;
                return false;
            }
            InteractCounter = D3Cmd(COMMANDS.D3_UsePowerToActor, GUID, InteractCounter).r_1.i;
            do
            {
                Thread.Sleep(50);
                if (InteractCounter.Equals(-1)) break;
                if (this.Paused) return false;
            }
            while ((InteractCounter = D3Cmd(COMMANDS.D3_UsePowerToActor, GUID, InteractCounter).r_1.i) < 10);
            this.D3Mail.p_10.i = 0;
            if(!DontSkip) SkipScene(true);
            return true;
        }
        //--------------------------------------------------------------------
        private Boolean InternetConnection()
        {
            try
            {
                IPHostEntry objIPHE = Dns.GetHostEntry("www.google.com");
                return true;
            }
            catch
            {
                return false; // host not reachable.
            }
        }
        public Profile(){}
        public Profile(String Name): this()
        {
            this.Name = Name;
        }
        private void WaitForInternetConnection()
        {
            Int32 ConnectionCounter = 0;
            while (!InternetConnection())
            {
                Thread.Sleep(2000);
                if (ConnectionCounter > 10)
                {
                    this.Message("[" + DateTime.Now.ToString("dd.MM HH:mm:ss") + "] No internet connection... retry!");
                    ConnectionCounter = 0;
                }
                ++ConnectionCounter;
            }
        }
        private bool DoVerifications()
        {
            WaitForInternetConnection();
            this.State = GameState.Active;
            string PathToLibrary = Directory.GetCurrentDirectory() + "\\DIIIBData\\D3Api.dll";
            if (!File.Exists(PathToLibrary))
            {
                MessageBox.Show("Couldn't find 'D3Api.dll' :(", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            _processID = StartD3(this.D3Pfad, PathToLibrary);
            if (_processID.Equals(-1))
            {
                MessageBox.Show("MC-Patch failed :(", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
        private bool StartD3()
        {
            D3Process = Process.GetProcessById(_processID);
            D3Process.EnableRaisingEvents = true;
            D3Process.Exited += new EventHandler(D3_Exited);

            while (D3Process != null && D3Process.MainWindowHandle.ToInt32() == 0) Thread.Sleep(500);

            return D3Process != null;
        }
        private void Login()
        {
            switch (this.BattleNetRealm)
            {
                case 1: //eu.actual.battle.net
                    Registry.CurrentUser.CreateSubKey("Software\\Blizzard Entertainment\\Battle.net\\D3").SetValue("RegionURL", "eu.actual.battle.net");
                    break;
                case 2: //us.actual.battle.net
                    Registry.CurrentUser.CreateSubKey("Software\\Blizzard Entertainment\\Battle.net\\D3").SetValue("RegionURL", "us.actual.battle.net");
                    break;
                case 3: //kr.actual.battle.net
                    Registry.CurrentUser.CreateSubKey("Software\\Blizzard Entertainment\\Battle.net\\D3").SetValue("RegionURL", "kr.actual.battle.net");
                    break;
            }
            Thread.Sleep(_random.Next(8000, 12000));
            int loginStatus = 0;
            this.Message("Logging in...");
            do
            {
                if (loginStatus == 3)
                {
                    this.TerminateD3();
                    Thread.Sleep(_random.Next(1000, 1500));
                    this.StartProfile();
                    return;
                }
                if (loginStatus == 2) Thread.Sleep(_random.Next(5000, 7000));
                if (loginStatus == 0)
                {
                    Thread.Sleep(_random.Next(2000, 3000));
                    if (this.Try_Login_Count >= 10)
                    {
                        _logger.Log("[" + this.Name + "] Tried to log in 10 times, called Terminate D3.");
                        this.TerminateD3();
                        this.Try_Login_Count = 0;
                        return;
                    }
                }
                if (loginStatus != 2) ++this.Try_Login_Count;
                if (loginStatus == 1) break;
                loginStatus = this.D3Login(this.AccountEmail, this.AccountPassword);
            } while (loginStatus != 1);
            this.Try_Login_Count = 0;
        }
        //[XmlIgnore]
        //private DateTime _sessionStartTime;
        [XmlIgnore]
        private DateTime _breakTime = DateTime.MinValue;
        private Thread _profileManager;
        private void ManageProfile()
        {
            //_sessionStartTime = DateTime.Now;
            while (true)
            {
                if (D3Process == null)
                {
                    if (CanRun())
                    {
                        if(!DoStart())
                            StopProfile();
                    }
                    else
                        State = GameState.Scheduler;
                }
                else
                {
                    if (!CanRun())
                        StopProfile(true);
                }
                Thread.Sleep(100);
            }
        }

        private int _intervalDeltaInSeconds = -1;
        private void  SetNewIntervalDelta()
        {
            int delta = (int)(((double)BreakIntervalInMinutes) / 10 * 60);
            _intervalDeltaInSeconds = _random.Next(-delta, delta);
        }
        private bool CanRun()
        {
            bool canRun = true;
            if (UseSchedule)
            {
                canRun = false;
                foreach (ScheduleEntry s in ScheduleEntries)
                {
                    if (DateTime.Now.TimeOfDay >= s.Start.TimeOfDay && DateTime.Now.TimeOfDay <= s.End.TimeOfDay)
                    {
                        canRun = true;
                        break;
                    }
                }
            }
            if (canRun && TakeBreaks && BreakIntervalInMinutes > 0 && BreakLengthInMinutes > 0)
            {
                if (_intervalDeltaInSeconds == -1)
                {
                    SetNewIntervalDelta();
                }
                if (_breakTime == DateTime.MinValue && ElapsedTime.Ticks > new TimeSpan(0, BreakIntervalInMinutes, _intervalDeltaInSeconds).Ticks )
                {
                    int lengthDelta = (int)(((double)BreakLengthInMinutes) / 10 * 60);
                    _breakTime = DateTime.Now.AddSeconds(_random.Next(-lengthDelta, lengthDelta));
                    SetNewIntervalDelta();
                    canRun = false;
                }
                else if (_breakTime != DateTime.MinValue)
                {
                    if(DateTime.Now - _breakTime > new TimeSpan(0, BreakLengthInMinutes, 0))
                    {
                        _breakTime = DateTime.MinValue;
                    }
                    else
                    {
                        canRun = false;
                    }
                }
            }
            return canRun;
        }

        private bool DoStart()
        {
            if (this.D3Process == null)
            {
                if (!DoVerifications())
                    return false;
                if (!StartD3())
                {
                    StopProfile();
                    return false;
                }

                if (!this.AutoLogin) return false;

                Login();

                StartQuest();
                return true;
            }
            return false;
        }
        public override void StartProfile()
        {
            _profileManager = new Thread(ManageProfile);
            _profileManager.Start();
        }

        private void StartQuest()
        {
            if (this.StartOption.Equals(1) && !this.StartScript.Equals(String.Empty))
            {
                if (Convert.ToBoolean(this.D3Mail.D3Info.GraphicFlag))
                {
                    this.DisableEnableGraphic(1);
                }
                using (Stream stream = File.Open("DIIIBData\\Quest\\" + this.StartScript + ".D3S", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    this.D3Quest = (List<D3CMD>)bin.Deserialize(stream);
                }

                this.D3Execute = new Thread(this.PlayQuest);
                this.D3Execute.Start(0);
            }
        }
        private void D3_Exited(object sender, EventArgs e)
        {
            this.StartTime = DateTime.Now;
            this.GoldStart = this.D3Mail.D3Info.Gold = 0;
            this.D3Process = null;
            if (this.State == GameState.Running && this.IsStuck == false && !this.D3Mail.D3Info.IsDisconnected)
            {
                ++this.Count_D3_Crash;
                _logger.Log("[" + this.Name + "] D3 Closed while GameState = Running (D3_Exited)");
                //this.D3Cmd(IPlugin.COMMANDS.D3_TakeScreenShot);
            }
            if (this.State.Equals(GameState.Running))
            {
                try { this.StopProfile(); } catch { }
                this.StartProfile();
            }
            this.State = GameState.None;
        }
        private void TerminateD3()
        {
            //while (!this.D3Mail.D3Info.IsDisconnected && this.D3Cmd(IPlugin.COMMANDS.D3_TakeScreenShot).r_1.i <= 0) Thread.Sleep(_random.Next(200, 400));
            Thread.Sleep(_random.Next(1200, 1500));
            try { this.D3Process.Kill(); this.D3Process = null; } catch { }
            _logger.Log("[" + this.Name + "] TerminateD3 called: " + Environment.StackTrace);
        }
        public void LogoutAndStopProfile(bool scheduler = false)
        {
            StopRunning();
            StopProfile(scheduler);
        }
        public override void StopProfile(bool scheduler = false)
        {
            Paused = false;
            StartTime = DateTime.Now;
            GoldStart = this.D3Mail.D3Info.Gold = 0;
            Thread.Sleep(200);
            KillAllThreads(scheduler);
            if (scheduler)
                State = GameState.Scheduler;
            else
            {
                State = GameState.None;
                _breakTime = DateTime.MinValue;
            }
        }
        public override void StopRunning(Boolean WithoutLeaveingGame = false)
        {
            this.Paused = false;
            this.StartTime = DateTime.Now;
            this.GoldStart = this.D3Mail.D3Info.Gold = 0;
            this.State = GameState.Active;
            this.D3Cmd(COMMANDS.D3_Update);
            Thread.Sleep(_random.Next(3000, 6000));
            if (!WithoutLeaveingGame)
            {
                Logout();
            }
            try { this.D3Execute.Abort(); } catch { }
        }
        private void KillAllThreads(bool dontKillManager = false)
        {
            if (D3Execute != null)
            {
                try
                {
                    D3Execute.Abort();
                    D3Execute = null;
                } catch{}
            }
            if (StartGame != null)
            {
                try
                {
                    StartGame.Abort();
                    StartGame = null;
                } catch{}
            }
            if (D3Process != null)
            {
                try
                {
                    D3Process.Kill();
                    D3Process = null;
                } catch{}
            }
            if (StuckTimerThread != null)
            {
                try
                {
                    StuckTimerThread.Abort();
                    StuckTimerThread = null;
                } catch{}
            }
            if (!dontKillManager)
            {
                if (_profileManager != null)
                {
                    try
                    {
                        _profileManager.Abort();
                        _profileManager = null;
                    }
                    catch { }
                }
            }
        }
        private void Logout()
        {
            D3Cmd(COMMANDS.D3_LeaveWorld);
            int attempts = 0;
            while (D3Mail.D3Info.InGame == 1 && attempts < 5)
            {
                Thread.Sleep(_random.Next(11000,15000));
                D3Cmd(COMMANDS.D3_LeaveWorld);
                attempts++;
            }
        }
        public void DisableEnableGraphic(object Invisible)
        {
            try
            {
                lock (this)
                {
                    if (Convert.ToInt32(Invisible).Equals(-1))
                        this.D3Mail.D3Info.GraphicFlag = Convert.ToInt32(!Convert.ToBoolean(this.D3Mail.D3Info.GraphicFlag));
                    else
                        this.D3Mail.D3Info.GraphicFlag = Convert.ToInt32(Invisible);
                    this.D3Cmd(COMMANDS.D3_Update);
                }
            }
            catch { MessageBox.Show("Failed to attach the graphic :(. (try again)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        public Int32 GetGPH
        {
            get
            {
                try
                {
                    if (this.GoldStart <= 0)
                    {
                        this.GoldStart = this.D3Mail.D3Info.Gold;
                        this.StartTime = DateTime.Now;
                        return 0;
                    }
                    double Round = Math.Round(((this.D3Mail.D3Info.Gold - this.GoldStart) / (DateTime.Now - this.StartTime).TotalHours), 0);
                    return Convert.ToInt32((Round - (Round % 100)));
                }
                catch { return 0; }
            }
        }
        /*public Int32 GetXPPH
        {
            get
            {
                try
                {
                    if (this.XPStart <= 0)
                    {
                        this.XPStart = this.D3Mail.D3Info.XP;
                        this.StartTime = DateTime.Now;
                        return 0;
                    }
                    double Round = Math.Round(((this.D3Mail.D3Info.XP - this.XPStart) / (DateTime.Now - this.StartTime).TotalHours), 0);
                    return Convert.ToInt32((Round - (Round % 100)));
                }
                catch { return 0; }
            }
        }*/
        public DateTime ElapsedTime
        {
            get { return new DateTime(DateTime.Now.Subtract(StartTime).Ticks); }
        }
        public virtual String[] Stats
        {
            get
            {
                if (this.D3Process != null)
                {
                    return new String[]{
                    "Start", ((this.D3Execute != null && this.D3Execute.IsAlive)?(this.StartTime.ToLongDateString() + " Elapsed time:  " + ElapsedTime.Hour.ToString("D2") +":" + ElapsedTime.Minute.ToString("D2") + ":" + ElapsedTime.Second.ToString("D2")) : "not running..."),
                    "Deaths", this.Count_Deads.ToString(),
                    "Disconnects", this.Count_Disconnects.ToString(),
                    "D3-Crashes", this.Count_D3_Crash.ToString(),
                    "Stucks", this.Count_Stucks.ToString() + " | stuck if zero =>" + this.StuckValue,
                    "Level", this.D3Mail.D3Info.Level.ToString(),
                    "Paragon Level", this.D3Mail.D3Info.ParagonLevel.ToString(),
                    "Durability", this.D3Mail.D3Info.Durability + "%",
                    "Backpack", (60-this.D3Mail.D3Info.BackpackFreeSlots)+"/60",
                    "Pickup Radius", String.Format("{0:0,0}",this.D3Mail.D3Info.GGRadius) +"m",
                    "Armor GF", String.Format("{0:0,0}",this.D3Mail.D3Info.GF) +"%   "+((this.D3Mail.D3Info.GF>300)?this.D3Mail.D3Info.GF-300 + "% too much":""),
                    "Gold", String.Format("{0:0,0}",this.D3Mail.D3Info.Gold),
                    "Gold collected", String.Format("{0:0,0}", this.D3Mail.D3Info.Gold-this.GoldStart),
                    "Gold/Hour", String.Format("{0:0,0}", this.GetGPH)
                    /*"XP Earned", string.Format("{0:0,0}", this.D3Mail.D3Info.XP-this.XPStart),
                    "XP/Hour", string.Format("{0:0,0}", 0)//this.GetXPPH)*/
                    };
                }
                return new String[] { "You have to be ingame" };

            }
        }
        public String D3Pfad
        {
            get {
                String D3Pfad = String.Empty;
                try{
                    D3Pfad = Convert.ToString(Registry.CurrentUser.OpenSubKey("Software\\DIIIB").GetValue("Path"));
                }
                catch {  }
                return D3Pfad;
            }
        }
    }
    public class ScheduleEntry
    {
        /// <summary>
        /// For Serialization
        /// </summary>
        public ScheduleEntry()
        {
        }

        public ScheduleEntry(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public override string ToString()
        {
            return "From " + Start.ToString("HH:mm") + " to " + End.ToString("HH:mm");
        }
    }
}
