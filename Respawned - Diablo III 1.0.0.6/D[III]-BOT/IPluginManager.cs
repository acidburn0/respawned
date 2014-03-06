using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Respawned
{
    class IPluginManager
    {
        [ImportMany(typeof(IPlugin.IPlugin))]
        public List<IPlugin.IPlugin> allPlugins;

        public IPluginManager()
        {
            DirectoryCatalog Search = new DirectoryCatalog(Environment.CurrentDirectory + "\\DIIIBData\\Plugins\\");
            CompositionContainer Woker = new CompositionContainer(Search);
            Woker.ComposeParts(this);
        }

        public void LoadModules(TabControl Dummy)
        {
            foreach (var Item in allPlugins)
            {
                for (int i = 0; i < Profile.MyProfiles.Count; ++i)
                {
                    Item.GameDataList.Add(Profile.MyProfiles[i]);
                }
                Item.Init();
                if (Item.PluginTabPage != null)
                {
                    Item.PluginTabPage.Text = Item.PluginName;
                    Dummy.Controls.Add(Item.PluginTabPage);
                }
            }
        }
        public void UnloadModules()
        {
            foreach (var Item in allPlugins)
            {
                Item.Exit();
            }
        }
    }
}
