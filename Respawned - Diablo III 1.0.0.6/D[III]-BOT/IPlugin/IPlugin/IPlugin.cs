using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IPlugin
{

    public enum UnitType { Invalid, Monster, Gizmo, Client_Effect, Server_Prop, Environment, Critter, Player, Item, Axe_Symbol, Projectile, Custom_Brain };
   
    
    public abstract class IPlugin
    {
        public List<GameData> GameDataList = new List<GameData>();
        public Int32 SelectedProfileIndex;
        public TabPage PluginTabPage;
        abstract public void Init();
        abstract public void Exit();
        abstract public String PluginName { get; }
        abstract public String Autor { get; }
        abstract public String Version { get; }
        abstract public String Description { get; }
    }
}
