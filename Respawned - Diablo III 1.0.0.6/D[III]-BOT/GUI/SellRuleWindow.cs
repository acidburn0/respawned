using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Respawned
{
    public partial class SellRuleWindow : Form
    {
        private const int LINE_OFFSET = 30;
        private const string STAT_LABEL_NAME = "lblStat";
        private const string GRTR_LABEL_NAME = "lblGrtr";
        private const string STAT_CBO_NAME = "cboStat";
        private const string STAT_NUD_NAME = "nudStat";
        private const string REMOVE_LINE_BTN_NAME = "btnRemoveLine";

        private Dictionary<int, string> _stats = new Dictionary<int, string>()
        {
            {0, "Dexterity"},
            {1, "Intelligence"},
            {2, "Sockets"},
            {3, "Strength"},
            {4, "Vitality"},
            {5, "Weapon DPS"},
            {6, "Crit Chance %"},
            {7, "Crit Damage %"},
            {8, "Attacks per second"},
            {9, "Attack speed %"},
            {10, "Damage Min"},
            {11, "Damage Max"},
            {12, "Armor"},
            {13, "All Resistance"},
            {14, "Physical Resistance"},
            {15, "Cold Resistance"},
            {16, "Fire Resistance"},
            {17, "Lightning Resistance"},
            {18, "Poison Resistance"},
            {19, "Arcane Resistance"},
            {20, "Block Chance %"},
            {21, "Min Block Amount"},
            {22, "Max Block Amount"},
            {23, "Life Per Second"},
            {24, "Life On Hit"},
            {25, "Life Per Kill"},
            {26, "Life Steal %"},
            {27, "Life Bonus %"},
            {28, "Magid Find"},
            {29, "Gold Find"},
            {30, "Movement Speed"},
            {31, "Missile Damage Reduction %"},
            {32, "Melee Damage Reduction %"},
            {33, "Life per Spirit Spent"},
            {34, "Maximum Arcane Power"},
            {35, "Arcane Power on Crit"},
            {36, "Maximum Fury"},
            {37, "Life Per Fury"},
            {38, "Maximum Discipline"},
            {39, "Hatred Regenerated Per Second"},
            {40, "Maximum Mana"},
            {41, "Mana Regenerated Per Second"},
            {42, "Spirit Regenerated Per Second"},
            {43, "DPS % Against Elites"},
            {44, "DPS % Poison"},
            {45, "DPS % Fire"},
            {46, "DPS % Cold"},
            {47, "DPS % Lightning"},
            {48, "DPS % Arcane"},
            {49, "DPS % Holy"},
            {50, "Experience Bonus %"},
            {51, "Experience Per Kill"}
        };

        private Dictionary<int, string> _itemTypes = new Dictionary<int, string>()
        {
            {0, "Amulet"},
            {1, "Boots"},
            {2, "Bracer"},
            {3, "Chest"},
            {4, "Gloves"},
            {5, "Legs"},
            {6, "Head"},
            {7, "Ring"},
            {8, "Shoulder"},
            {9, "Waist"},
            {10, "One-Handed Weapon"},
            {11, "Two-Handed Weapon"},
            {12, "Off-Hand"},
            {13, "Follower Special"}
        };

        private int _lineCount = 1;
        private SellRule _sellRule = new SellRule();

        public SellRuleWindow()
        {
            InitializeComponent();
            PopulateItemTypeCombo();
            PopulateStatCombo(cboStat1);
        }

        public SellRuleWindow(SellRule s):this()
        {
            txtRuleName.Text = s.Name;
            nudMinIlvl.Value = s.MinIlvl;
            SelectValue(s.ItemType, cboItemTypes);
            PopulateCriterias(s);
        }

        private void PopulateCriterias(SellRule s)
        {
            SelectValue(s.Criterias[0].StatID, cboStat1);
            nudStat1.Value = s.Criterias[0].MinAmount;

            for (int i = 1; i < s.Criterias.Length; i++)
            {
                AddLine(null, new EventArgs());
                ComboBox lastCbo = (ComboBox)pnlRuleDef.Controls.Find(STAT_CBO_NAME + (i + 1), true)[0];
                NumericUpDown lastNud = (NumericUpDown)pnlRuleDef.Controls.Find(STAT_NUD_NAME + (i + 1), true)[0];
                SelectValue(s.Criterias[i].StatID, lastCbo);
                lastNud.Value = s.Criterias[i].MinAmount;
            }
        }

        private void SelectValue(int value, ComboBox cbo)
        {
            for (int i = 0; i < cbo.Items.Count; i++)
            {
                ComboBoxItem item = (ComboBoxItem)cbo.Items[i];
                if (item.Value == value)
                {
                    cbo.SelectedIndex = i;
                    break;
                }
            }
        }

        private void PopulateItemTypeCombo()
        {
            cboItemTypes.Items.Clear();
            foreach (KeyValuePair<int, string> k in _itemTypes)
            {
                cboItemTypes.Items.Add(new ComboBoxItem(k.Value, k.Key));
            }
            cboItemTypes.SelectedIndex = 0;
        }

        private void PopulateStatCombo(ComboBox cbo)
        {
            cbo.Items.Clear();
            foreach(KeyValuePair<int, string> k in _stats)
            {
                cbo.Items.Add(new ComboBoxItem(k.Value, k.Key));
            }
            cbo.SelectedIndex = 0;
        }

        private void AddLine(object sender, EventArgs e)
        {
            //int lineCount = pnlRuleDef.Controls.OfType<ComboBox>().Count() - 1;
            try
            {
                if (_lineCount >= 8)
                    return;
                Label lastStatLabel = (Label)pnlRuleDef.Controls.Find(STAT_LABEL_NAME + _lineCount, true)[0];
                Label lastGrtrLabel = (Label)pnlRuleDef.Controls.Find(GRTR_LABEL_NAME + _lineCount, true)[0];
                ComboBox lastCbo = (ComboBox)pnlRuleDef.Controls.Find(STAT_CBO_NAME + _lineCount, true)[0];
                NumericUpDown lastNud = (NumericUpDown)pnlRuleDef.Controls.Find(STAT_NUD_NAME + _lineCount, true)[0];
                Button lastRemoveLineBtn = (Button)pnlRuleDef.Controls.Find(REMOVE_LINE_BTN_NAME + _lineCount, true)[0];

                _lineCount++;

                Label lblStat = new Label();
                lblStat.Text = lastStatLabel.Text;
                lblStat.Size = lastStatLabel.Size;
                lblStat.Location = new Point(lastStatLabel.Location.X, lastStatLabel.Location.Y + LINE_OFFSET);
                lblStat.Name = STAT_LABEL_NAME + (_lineCount);

                Label lblGrtr = new Label();
                lblGrtr.Text = lastGrtrLabel.Text;
                lblGrtr.Size = lastGrtrLabel.Size;
                lblGrtr.Location = new Point(lastGrtrLabel.Location.X, lastGrtrLabel.Location.Y + LINE_OFFSET);
                lblGrtr.Name = GRTR_LABEL_NAME + (_lineCount);


                ComboBox cboStat = new ComboBox();
                cboStat.Size = lastCbo.Size;
                cboStat.Location = new Point(lastCbo.Location.X, lastCbo.Location.Y + LINE_OFFSET);
                cboStat.Name = STAT_CBO_NAME + (_lineCount);
                cboStat.ValueMember = cboStat1.ValueMember;
                cboStat.DisplayMember = cboStat1.DisplayMember;
                cboStat.DropDownStyle = cboStat1.DropDownStyle;
                cboStat.FlatStyle = cboStat1.FlatStyle;
                cboStat.TabIndex = 3 + (_lineCount*3);
                cboStat.Sorted = true;
                PopulateStatCombo(cboStat);
                cboStat.SelectedIndexChanged += cboStat_SelectedIndexChanged;

                NumericUpDown nudStat = new NumericUpDown();
                nudStat.Size = lastNud.Size;
                nudStat.Location = new Point(lastNud.Location.X, lastNud.Location.Y + LINE_OFFSET);
                nudStat.Name = STAT_NUD_NAME + (_lineCount);
                nudStat.Maximum = lastNud.Maximum;
                nudStat.TabIndex = 4 + (_lineCount * 3);

                Button btnRemoveLine = new Button();
                btnRemoveLine.Size = lastRemoveLineBtn.Size;
                btnRemoveLine.Location = new Point(lastRemoveLineBtn.Location.X, lastRemoveLineBtn.Location.Y + LINE_OFFSET);
                btnRemoveLine.Name = REMOVE_LINE_BTN_NAME + (_lineCount);
                btnRemoveLine.Text = lastRemoveLineBtn.Text;
                btnRemoveLine.FlatStyle = lastRemoveLineBtn.FlatStyle;
                btnRemoveLine.Click += RemoveLine;
                btnRemoveLine.Visible = true;
                btnRemoveLine.TabIndex = 5 + (_lineCount * 3);

                pnlRuleDef.Controls.Add(lblStat);
                pnlRuleDef.Controls.Add(lblGrtr);
                pnlRuleDef.Controls.Add(cboStat);
                pnlRuleDef.Controls.Add(nudStat);
                pnlRuleDef.Controls.Add(btnRemoveLine);

                btnAddLine.Enabled = _lineCount < 8;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void RemoveLine(object sender, EventArgs e)
        {
            try
            {
                int lineNumber = int.Parse(((Button)sender).Name.Replace(REMOVE_LINE_BTN_NAME, ""));

                Label lineStatLabel = (Label)pnlRuleDef.Controls.Find(STAT_LABEL_NAME + lineNumber, true)[0];
                Label lineGrtrLabel = (Label)pnlRuleDef.Controls.Find(GRTR_LABEL_NAME + lineNumber, true)[0];
                ComboBox lineCbo = (ComboBox)pnlRuleDef.Controls.Find(STAT_CBO_NAME + lineNumber, true)[0];
                NumericUpDown lineNud = (NumericUpDown)pnlRuleDef.Controls.Find(STAT_NUD_NAME + lineNumber, true)[0];
                Button lineRemoveLineBtn = (Button)pnlRuleDef.Controls.Find(REMOVE_LINE_BTN_NAME + lineNumber, true)[0];

                pnlRuleDef.Controls.Remove(lineStatLabel);
                pnlRuleDef.Controls.Remove(lineGrtrLabel);
                pnlRuleDef.Controls.Remove(lineCbo);
                pnlRuleDef.Controls.Remove(lineNud);
                pnlRuleDef.Controls.Remove(lineRemoveLineBtn);

                lineStatLabel.Dispose();
                lineGrtrLabel.Dispose();
                lineCbo.Dispose();
                lineNud.Dispose();
                lineRemoveLineBtn.Dispose();

                foreach (Control c in pnlRuleDef.Controls)
                {
                    int controlLine = GetControlLine(c.Name);
                    if (controlLine > lineNumber)
                    {
                        c.Location = new Point(c.Location.X, c.Location.Y - LINE_OFFSET);
                        c.Name = c.Name.Replace(controlLine.ToString(), (controlLine - 1).ToString());
                    }
                }
                _lineCount--;
                btnAddLine.Enabled = _lineCount < 8;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        
        private int GetControlLine(string controlName)
        {
            if (controlName.StartsWith(STAT_LABEL_NAME))
            {
                return int.Parse(controlName.Replace(STAT_LABEL_NAME, ""));
            }
            
            if (controlName.StartsWith(GRTR_LABEL_NAME))
            {
                return int.Parse(controlName.Replace(GRTR_LABEL_NAME, ""));
            }

            if (controlName.StartsWith(STAT_CBO_NAME))
            {
                return int.Parse(controlName.Replace(STAT_CBO_NAME, ""));
            }

            if (controlName.StartsWith(STAT_NUD_NAME))
            {
                return int.Parse(controlName.Replace(STAT_NUD_NAME, ""));
            }

            if (controlName.StartsWith(REMOVE_LINE_BTN_NAME))
            {
                return int.Parse(controlName.Replace(REMOVE_LINE_BTN_NAME, ""));
            }
            return -1;
        }

        public SellRule SellRule
        {
            get { return _sellRule; }
            set { _sellRule = value; }
        }

        private SellRuleCriteria[] GetCriterias()
        {
            SellRuleCriteria[] criterias = new SellRuleCriteria[_lineCount];

            for (int i = 1; i <= _lineCount; i++)
            {
                ComboBox selectedStatCbo = (ComboBox)pnlRuleDef.Controls.Find(STAT_CBO_NAME + i, true)[0];
                NumericUpDown minAmountNud = (NumericUpDown)pnlRuleDef.Controls.Find(STAT_NUD_NAME + i, true)[0];

                criterias[i-1] = new SellRuleCriteria(((ComboBoxItem)selectedStatCbo.SelectedItem).Value, minAmountNud.Value);
            }
            return criterias;
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                if (String.IsNullOrEmpty(txtRuleName.Text))
                {
                    MessageBox.Show(this, "Please enter a rule name.", "Incorrect rule name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SellRule s = new SellRule();
                s.Name = txtRuleName.Text;
                s.MinIlvl = (int)nudMinIlvl.Value;
                s.ItemType = ((ComboBoxItem)cboItemTypes.SelectedItem).Value;
                s.Criterias = GetCriterias();
                SellRule = s;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
        }

        private void cboStat_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cbo = (ComboBox) sender;
            ComboBoxItem item = (ComboBoxItem)cbo.SelectedItem;

            int controlLine = GetControlLine(cbo.Name);
            if (item.Text.Contains("%"))
            {
                ((NumericUpDown)pnlRuleDef.Controls.Find(STAT_NUD_NAME + controlLine, true)[0]).DecimalPlaces = 2;
            }
            else
            {
                ((NumericUpDown)pnlRuleDef.Controls.Find(STAT_NUD_NAME + controlLine, true)[0]).DecimalPlaces = 0;
            }
        }

    }
    public class ComboBoxItem
    {
        public ComboBoxItem(string text, int value)
        {
            Text = text;
            Value = value;
        }
        public int Value { get; set; }
        public string Text { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }
}
