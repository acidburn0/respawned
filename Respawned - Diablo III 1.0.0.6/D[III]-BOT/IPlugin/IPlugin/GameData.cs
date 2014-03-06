using System;
using System.Xml.Serialization;

namespace IPlugin
{
    public enum GameState { None, Active, Running, Scheduler, Disconnected, Paused };
    [Serializable()]
    public abstract class GameData : GameCommunicator
    {
        [XmlIgnore] public Int32 DefaultStuckValue = 100;
        [XmlIgnore] public Int32 DefaultHangValue = 300;
        [XmlIgnore] public DateTime StartTime;
        [XmlIgnore] public Int32 Count_Deads;
        [XmlIgnore] public Int32 Count_Disconnects;
        [XmlIgnore] public Int32 Count_D3_Crash;
        [XmlIgnore] public Int32 Count_Stucks;

        abstract public void StartProfile();
        abstract public void StopProfile(bool scheduler = false);
        abstract public void StopRunning(Boolean WithoutLeaveingGame);
        abstract public void PlayQuest(Object StartIndex);
    }
}
