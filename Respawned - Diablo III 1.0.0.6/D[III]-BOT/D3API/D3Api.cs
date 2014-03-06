using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml.Serialization;

namespace D_III__BOT
{
    public enum COMMANDS
    {
        D3_TEST,D3_Activate, D3_SetGraphic, D3_Login, D3_SelectQuestStart, D3_SelectQuestResume,
        D3_GetACDActor, D3_GetMyACDActor, D3_UsePowerToActor, D3_GetNearestACDActorByModelID, D3_GetNearestACDActorByName, D3_AttackMonster,
        D3_UseTownPortal, D3_UseWaypoint, D3_Repair, D3_LeaveWorld, D3_Revival, D3_MoveTo,
        D3_PickUpMoney, D3_GetNearestItemByQuality, D3_SellItemsLowerQuality, D3_IdentifyBackPack, D3_GetNearestChest
    }

    public class D3Api
    {
        [DllImport("DIIIBData\\D3Api.dll")]
        public static extern int StartD3(String D3Pfad, String InjectDLL);

        [XmlIgnore]
        public Process D3Process;
        public D3Header D3Mail = new D3Header();
        public struct GWHeaderType
        {
            public int i;
            public float f;
        }
        public struct Unit
        {
            public sMovingState MovingState;
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
            public Int32 Ghosted;
            public Int32 Level;
            public Single GoldFind;
            public Int32 BackpackFreeSlots;
            public Int32 Gold;
            public Int32 isMoving;
            public Int32 ReadyToStartQuest;
            public Int32 Durability;
            public Int32 QuestStep;
            public Int32 IsDisconnected;
            public Int32 InGame;
            public QuestS Quest;

        }
        public struct QuestS
        {
            public int Act;
            public int QuestID;
            public int SubQuestID;
        }
        public struct sMovingState
	    {
            public Int32 State;
            public Int32 AttackRange;
            public Int32 MoneyMinAmount;
            public Int32 ItemMinQuality;
            public Int32 ItemMinLevel;
            public Int32 BlessedShrine;
            public Int32 FrenziedShrine;
            public Int32 FortuneShrine;
            public Int32 EnlightenedShrine;
            public Int32 OpenChests;
	    } 
        public struct D3Header
        {
            public ushort header;
            public GWHeaderType LParam;
            public GWHeaderType MParam;
            public GWHeaderType RParam;
            public GWHeaderType XParam;
            public GWHeaderType returnValue1;
            public GWHeaderType returnValue2;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public String StringBuffer;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public String StringEmail;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
	        public String StringPassword;
            public Unit Actor;
        }
        private D3Header SendD3Cmd(D3Header SendBuffer)
        {
            Thread.Sleep(5); // damit CPU nicht ausgelastet wird.
            lock (this)
            {
                object bufferOBJ = (object)SendBuffer;
                byte[] ByteStruct = new byte[Marshal.SizeOf(SendBuffer)];
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(SendBuffer));
                Marshal.StructureToPtr(SendBuffer, ptr, true);
                Marshal.Copy(ptr, ByteStruct, 0, Marshal.SizeOf(SendBuffer));
                Marshal.FreeHGlobal(ptr);
                NamedPipeClientStream pipeClient;
                SendBuffer.returnValue1.i = -1;
                SendBuffer.returnValue2.i = -1;
                SendBuffer.returnValue1.f = -1;
                SendBuffer.returnValue2.f = -1;
                try
                {
                    pipeClient = new NamedPipeClientStream(".", "D3_" + this.D3Process.Id, PipeDirection.InOut);
                    pipeClient.Connect(1000);
                    if (pipeClient.IsConnected)
                    {
                        pipeClient.Write(ByteStruct, 0, Marshal.SizeOf(SendBuffer));
                        pipeClient.Read(ByteStruct, 0, Marshal.SizeOf(SendBuffer));
                    }
                    IntPtr i = Marshal.AllocHGlobal(Marshal.SizeOf(bufferOBJ));
                    Marshal.Copy(ByteStruct, 0, i, Marshal.SizeOf(bufferOBJ));
                    bufferOBJ = Marshal.PtrToStructure(i, bufferOBJ.GetType());
                    Marshal.FreeHGlobal(i);
                    pipeClient.Close();
                }
                catch { }
                return (D3Header)bufferOBJ;

            }
        }
        public D3Header D3Cmd(COMMANDS s_header, String sBuffer)
        {
            D3Mail.header = Convert.ToUInt16(s_header);
            D3Mail.StringBuffer = sBuffer;
            return SendD3Cmd(D3Mail);
        }
        public D3Header D3Cmd(COMMANDS s_header, Single l_param, Single m_param, Single r_param, Single x_param)
        {
            D3Mail.header = Convert.ToUInt16(s_header);
            D3Mail.LParam.f = l_param;
            D3Mail.MParam.f = m_param;
            D3Mail.RParam.f = r_param;
            D3Mail.XParam.f = x_param;
            return SendD3Cmd(D3Mail);
        }
        public D3Header D3Cmd(COMMANDS s_header, Single l_param, Single m_param, Single r_param)
        {
            return D3Cmd(s_header, l_param, m_param, r_param, 0);
        }
        public D3Header D3Cmd(COMMANDS s_header, Single l_param, Single m_param)
        {
            return D3Cmd(s_header, l_param, m_param, 0, 0);
        }
        public D3Header D3Cmd(COMMANDS s_header, int l_param, int m_param, int r_param, int x_param)
        {
            D3Mail.header = Convert.ToUInt16(s_header);
            D3Mail.LParam.i = l_param;
            D3Mail.MParam.i = m_param;
            D3Mail.RParam.i = r_param;
            D3Mail.XParam.i = x_param;
            return SendD3Cmd(D3Mail);
        }
        public D3Header D3Login(String Email, String Password)
        {
            D3Mail.header = Convert.ToUInt16(COMMANDS.D3_Login);
            D3Mail.StringEmail = Email;
            D3Mail.StringPassword = Password;
            return SendD3Cmd(D3Mail);
        }
        public D3Header D3Cmd(COMMANDS s_header)
        {
            return D3Cmd(s_header, 0, 0, 0, 0);
        }
        public D3Header D3Cmd(COMMANDS s_header, int LParam)
        {
            return D3Cmd(s_header, LParam, 0, 0, 0);
        }
        public D3Header D3Cmd(COMMANDS s_header, int LParam, int MParam)
        {
            return D3Cmd(s_header, LParam, MParam, 0, 0);
        }
        public D3Header D3Cmd(COMMANDS s_header, int LParam, int MParam, int RParam)
        {
            return D3Cmd(s_header, LParam, MParam, RParam, 0);
        }
        public Unit GetUnit(Int32 ID)
        {
            return this.D3Cmd(COMMANDS.D3_GetACDActor, ID).Actor;
        }
    }
}
