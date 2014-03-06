using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoPlugin
{
    [Export(typeof(IPlugin.IPlugin))]
    public class RadarPlugin : IPlugin.IPlugin
    {
        public override string Autor
        {
            get { return "Daniel"; }
        }

        public override string Description
        {
            get { return "This plugin shows all objects on screen.."; }
        }

        public override string PluginName
        {
            get { return "Radar"; }
        }

        public override string Version
        {
            get { return "0.1"; }
        }

        public override void Exit()
        {
            
        }
        public PictureBox RadarBox = new PictureBox();
        public override void Init()
        {
            this.PluginTabPage = new TabPage();
            this.PluginTabPage.Controls.Add(RadarBox);
            RadarBox.BackColor = Color.LightGreen;
            RadarBox.Dock = DockStyle.Fill;
            RadarBox.BorderStyle = BorderStyle.FixedSingle;
            System.Windows.Forms.Timer Call = new System.Windows.Forms.Timer();
            Call.Tick += new System.EventHandler(DrawRadar);
            Call.Interval = 20;
            Call.Enabled = true;
        }
        private void DrawRadar(object sender, EventArgs e)
        {
            if (this.GameDataList.Count.Equals(0)) return;
            float RadarZoom = 1f;
            Bitmap RadarBitmap = new Bitmap(RadarBox.Width, RadarBox.Height);
               
            Graphics G = Graphics.FromImage(RadarBitmap);

            G.Clear(RadarBox.BackColor);
            G.Dispose();
            IPlugin.GameData.sD3Info Info = this.GameDataList[this.SelectedProfileIndex].D3Mail.D3Info;
            if (Info.Actor != null && this.GameDataList[this.SelectedProfileIndex].State.Equals(IPlugin.GameState.Running))
            {
                for (int i = 0; (i < Info.Actor.Length); ++i)
                {

                    switch ((IPlugin.UnitType)Info.Actor[i].Type)
                    {
                        case IPlugin.UnitType.Gizmo:
                            RadarBitmap = DrawUnit(RadarBitmap, Color.Green, (Info.X - Info.Actor[i].X) * RadarZoom + RadarBox.Width / 2, (Info.Y - Info.Actor[i].Y) * RadarZoom + RadarBox.Height / 2, "");
                            break;
                        case IPlugin.UnitType.Monster:
                            RadarBitmap = DrawUnit(RadarBitmap, Color.Red, (Info.X - Info.Actor[i].X) * RadarZoom + RadarBox.Width / 2, (Info.Y - Info.Actor[i].Y) * RadarZoom + RadarBox.Height / 2, "");
                            break;
                        case IPlugin.UnitType.Item:
                            RadarBitmap = DrawUnit(RadarBitmap, Color.Yellow, (Info.X - Info.Actor[i].X) * RadarZoom + RadarBox.Width / 2, (Info.Y - Info.Actor[i].Y) * RadarZoom + RadarBox.Height / 2, "");
                            break;
                    }
                }
                RadarBitmap = DrawUnit(RadarBitmap, Color.Gray, RadarBox.Width / 2, RadarBox.Height / 2, "");
            }
            RadarBox.Image = RadarBitmap;
        }
        private Bitmap DrawUnit(Bitmap img, Color UnitColor, float XPos, float YPos, string strName)
        {
            Graphics G = Graphics.FromImage(img);
            G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            SolidBrush RadarBrush = new SolidBrush(UnitColor);
            Pen RadarPen = new Pen(Color.Black, 1f);

            G.ResetTransform();
            G.TranslateTransform(-XPos, -YPos, System.Drawing.Drawing2D.MatrixOrder.Append);

            G.TranslateTransform(XPos, YPos, System.Drawing.Drawing2D.MatrixOrder.Append);
            try
            {
                G.DrawString(strName, new Font("Arial", 8, FontStyle.Bold), new SolidBrush(Color.Black), new Point(Convert.ToInt32(XPos - 5 - (strName.Length * 4) / 2), Convert.ToInt32(YPos - 30 / 2)));
                G.FillEllipse(RadarBrush, XPos - 5 / 2, YPos - 5 / 2, 5, 5);
            }
            catch { }
            G.Dispose();
            RadarBrush.Dispose();
            RadarPen.Dispose();
            return img;
        }
    }
}
