using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Serialization;

namespace IPlugin
{
    public enum COMMANDS
    {
        D3_Update, D3_Activate, D3_Login, D3_SelectQuestStart, D3_SelectQuestResume,
        D3_GetACDActor, D3_UsePowerToActor, D3_AttackMonster,
        D3_UseTownPortal, D3_UseWaypoint, D3_Repair, D3_LeaveWorld, D3_Revival, D3_MoveTo,
        D3_PickUpMoney, D3_GetNearestItemByQuality, D3_GetNearestGEMByQuality, D3_GetNearestPotionByQuality, D3_SellItemsLowerQuality,
        D3_IdentifyBackPack, D3_GetNearestChest, D3_GetNearestShrine, D3_UsePotion, D3_CastSpellToUnstuck, D3_SkipScene, D3_GetItemOnLootList,
        D3_Stash, D3_TakeScreenShot
    }
    [Serializable()]
    public abstract class GameCommunicator
    {
        ///////// D3Api
        [XmlIgnore]
        [NonSerializedAttribute]
        public Process D3Process;
        [XmlIgnore] private GameState _state = GameState.None;
        [XmlIgnore]
        [NonSerializedAttribute]
        protected Logger _logger = Logger.GetInstance();
        [NonSerializedAttribute]
        public D3Header D3Mail = new D3Header();

        public GameState State
        {
            get { return _state; }
            set { _state = value; }
        }

        [DllImport("DIIIBData\\D3Api.dll")]
        protected static extern int StartD3(String D3Pfad, String InjectDLL);
        public struct GWHeaderType
        {
            public int i;
            public float f;
        }
        public struct Unit
        {
            public Int32 Type;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public String Name;
            public Single X, Y, Z;
            public Int32 GUID;
            public Int32 ACDGUID;
            public Int32 ModelID;
            public Int32 ACDPTR;
        };
        public struct sSettings
        {
            public Int32 State;
            public Int32 AttackRange;
            public Int32 MoneyMinAmount;
            public Int32 MinItemValue;
            public Int32 LootItemQuality;
            public Int32 SellItemQuality;
            public Int32 ItemMinLevel;
            public Int32 BlessedShrine;
            public Int32 FrenziedShrine;
            public Int32 FortuneShrine;
            public Int32 EnlightenedShrine;
            public Int32 EmpoweredShrine;
            public Int32 FleetingShrine;
            public Int32 HealingWell;
            public Int32 OpenChests;
            public Int32 Repair;
            public Int32 SellSalvage;
            public Int32 Topaz;
            public Int32 Amethyst;
            public Int32 Emerald;
            public Int32 Ruby;
            public Int32 Pages;
            public Int32 Tomes;
            public Int32 LootTable;
            public Int32 GEM_Quality;
            public Int32 UsePotion;
            public Int32 UsePotionByPercent;
            public Int32 UseHealingWellsAt;
            public Int32 PotionLevel;
            public Int32 UnstuckSpell;
            public Int32 SpellRulesEnabled;
        };
        public struct sD3Info
        {
            public Int32 GraphicFlag;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 261)]
            public String PassedLootTablePath;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 261)]
            public String PassedSpellRulePath;
            public sSettings Settings;
            public Int32 PTR;
            public Int32 ACDPTR;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public String Name;
            public Single X;
            public Single Y;
            public Single Z;
            public Int32 GUID;
            public Int32 ACDGUID;
            public Int32 ModelID;
            public Single HP;
            public Single HP_max;
            public Int32 playerClass;
            public Int32 resource;
            public Int32 resource2;
            public Int32 Ghosted;
            public bool Dead;
            public Int32 Level;
            public Int32 ParagonLevel;
            public Int32 BackpackFreeSlots;
            public Int32 BackbackFreeDoubleSlot;
            public Int32 GGRadius;
            public Int32 GF;
            public Int32 Gold;
            public Int32 XP;
            public Int32 isMoving;
            public bool ReadyToStartQuest;
            public Int32 Durability;
            public Int32 QuestStep;
            public bool IsDisconnected;
            public Int32 InGame;
            public Int32 inTown;
            public Int32 Act;
            public Int32 QuestID;
            public Int32 SubQuestID;
            public Int32 MonsterLevel;
            public Int32 SellPotions;
            public Int32 PotionStacksAllowed;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 500)]
            public Unit[] Actor;
            public Unit ActorByName, ActorByModelID, retUnit;
        };
        public struct D3Header
        {
            public ushort header;
            public GWHeaderType p_1, p_2, p_3, p_4, p_5, p_6, p_7, p_8, p_9, p_10,
                                r_1, r_2, r_3, r_4, r_5, r_6, r_7, r_8, r_9, r_10, r_11;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public String StringBuffer;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public String StringEmail;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public String StringPassword;
            public sD3Info D3Info;
        }

        Random random = new Random();
        private D3Header SendD3Cmd()
        {

            Thread.Sleep(10); // damit CPU nicht ausgelastet wird. so CPU is not busy.
            lock (this)
            {
                object bufferOBJ = (object)this.D3Mail;
                if (this.D3Process != null)
                {
                    byte[] ByteStruct = new byte[Marshal.SizeOf(this.D3Mail)];
                    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(this.D3Mail));
                    Marshal.StructureToPtr(this.D3Mail, ptr, true);
                    Marshal.Copy(ptr, ByteStruct, 0, Marshal.SizeOf(this.D3Mail));
                    Marshal.FreeHGlobal(ptr);
                    NamedPipeClientStream pipeClient;
                    pipeClient = new NamedPipeClientStream(".", "D3_" + this.D3Process.Id, PipeDirection.InOut);
                    try
                    {
                        pipeClient.Connect(200);
                        if (pipeClient.IsConnected)
                        {
                            pipeClient.Write(ByteStruct, 0, Marshal.SizeOf(this.D3Mail));
                            pipeClient.Read(ByteStruct, 0, Marshal.SizeOf(this.D3Mail));
                        }
                        IntPtr i = Marshal.AllocHGlobal(Marshal.SizeOf(bufferOBJ));
                        Marshal.Copy(ByteStruct, 0, i, Marshal.SizeOf(bufferOBJ));
                        bufferOBJ = Marshal.PtrToStructure(i, bufferOBJ.GetType());
                        Marshal.FreeHGlobal(i);
                    }
                    catch (Exception e) { _logger.Log("[IPlugin] D3 Command Exception." + e); }
                    finally
                    {
                        pipeClient.Close();
                    }
                }
                return this.D3Mail = (D3Header)bufferOBJ;
            }

        }
        public D3Header D3Cmd(COMMANDS s_header)
        {
            D3Mail.header = Convert.ToUInt16(s_header);
            return SendD3Cmd();
        }
        public D3Header D3Cmd(COMMANDS s_header, String sBuffer)
        {
            D3Mail.header = Convert.ToUInt16(s_header);
            D3Mail.StringBuffer = sBuffer;
            return SendD3Cmd();
        }
        public D3Header D3Cmd(COMMANDS s_header, Single p_1 = 0, Single p_2 = 0, Single p_3 = 0, Single p_4 = 0)
        {
            D3Mail.header = Convert.ToUInt16(s_header);
            D3Mail.p_1.f = p_1;
            D3Mail.p_2.f = p_2;
            D3Mail.p_3.f = p_3;
            D3Mail.p_4.f = p_4;
            return SendD3Cmd();
        }
        public D3Header D3Cmd(COMMANDS s_header, Int32 p_1 = 0, Int32 p_2 = 0, Int32 p_3 = 0, Int32 p_4 = 0, Int32 p_5 = 0)
        {
            D3Mail.header = Convert.ToUInt16(s_header);
            D3Mail.p_1.i = p_1;
            D3Mail.p_2.i = p_2;
            D3Mail.p_3.i = p_3;
            D3Mail.p_4.i = p_4;
            D3Mail.p_5.i = p_5;
            return SendD3Cmd();
        }
        public int D3Login(String Email, String Password)
        {
            D3Mail.header = Convert.ToUInt16(COMMANDS.D3_Login);
            D3Mail.StringEmail = Email;
            D3Mail.StringPassword = Password;
            return SendD3Cmd().r_1.i;
        }
    }
}
