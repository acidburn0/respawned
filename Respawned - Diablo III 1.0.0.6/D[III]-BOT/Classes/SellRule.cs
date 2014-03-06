using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Respawned
{
    public class SellRule
    {
        private string _name;
        private int _itemType;
        private int _minIlvl;
        private SellRuleCriteria[] _criterias;
        private int _priority = -1;

        public SellRule() { }

        public SellRule(string name, int itemType, int minIlvl, SellRuleCriteria[] criterias)
        {
            Name = name;
            ItemType = itemType;
            MinIlvl = minIlvl;
            Criterias = criterias;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int ItemType
        {
            get { return _itemType; }
            set { _itemType = value; }
        }

        public int MinIlvl
        {
            get { return _minIlvl; }
            set { _minIlvl = value; }
        }

        public SellRuleCriteria[] Criterias
        {
            get { return _criterias; }
            set { _criterias = value; }
        }

        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public override string ToString()//return the string to send to the API here
        {
            string value = Name + ";" + ItemType + ";" + MinIlvl + ";" + Priority + ":";
            for (int i = 0; i < _criterias.Length; i++)
            {
                value += _criterias[i].StatID + ";";
                value += _criterias[i].MinAmount;
                if (i < _criterias.Length - 1)
                    value += ";";
            }
            return value;
        }
    }
}
