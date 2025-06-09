using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Memory_Policy_Simulator
{
    public partial class Form1 : Form
    {
        Graphics g;
        PictureBox pbPlaceHolder;
        Bitmap bResultImage;

        public Form1()
        {
            InitializeComponent();
            this.pbPlaceHolder = new PictureBox();
            this.bResultImage = new Bitmap(2048, 2048);
            this.pbPlaceHolder.Size = new Size(2048, 2048);
            g = Graphics.FromImage(this.bResultImage);
            pbPlaceHolder.Image = this.bResultImage;
            this.pImage.Controls.Add(this.pbPlaceHolder);
            this.tbConsole.Multiline = true;
            this.tbConsole.ScrollBars = ScrollBars.Vertical;
        }

        private void DrawBase(Core core, int windowSize, int dataLength)
        {
            g.Clear(Color.Black);

            if (core.policy == Core.POLICY.FIFO)
            {
                var psudoQueue = new Queue<char>();
                for (int i = 0; i < dataLength; i++)
                {
                    int psudoCursor = core.pageHistory[i].loc;
                    char data = core.pageHistory[i].data;
                    Page.STATUS status = core.pageHistory[i].status;

                    switch (status)
                    {
                        case Page.STATUS.PAGEFAULT:
                            psudoQueue.Enqueue(data);
                            break;
                        case Page.STATUS.MIGRATION:
                            psudoQueue.Dequeue();
                            psudoQueue.Enqueue(data);
                            break;
                    }

                    for (int j = 0; j <= windowSize; j++)
                    {
                        if (j == 0)
                            DrawGridText(i, j, data);
                        else
                            DrawGrid(i, j);
                    }

                    DrawGridHighlight(i, psudoCursor, status);
                    int depth = 1;
                    foreach (char t in psudoQueue)
                        DrawGridText(i, depth++, t);
                }
            }
        }
            else // LRU, MFU
            {
                // 각 시점의 프레임 상태를 Core의 내부 리스트에서 추적
                List<char> frameSnapshot = new List<char>();
                Dictionary<char, int> freq = new Dictionary<char, int>();
                Dictionary<char, int> order = new Dictionary<char, int>();
                int orderCount = 0;

                for (int i = 0; i < dataLength; i++)
                {
                    int loc = core.pageHistory[i].loc;
                    char data = core.pageHistory[i].data;
                    Page.STATUS status = core.pageHistory[i].status;

                    int actualLoc = loc; // 기본값은 Core에서 계산된 loc

                    if (status == Page.STATUS.HIT)
                    {
                        if (core.policy == Core.POLICY.LRU)
                        {
                            frameSnapshot.Remove(data);
                            frameSnapshot.Add(data);
                            // LRU에서 HIT 시 마지막 위치로 이동
                            actualLoc = frameSnapshot.Count;
                        }
                        else // MFU
                        {
                            if (!freq.ContainsKey(data))
                            {
                                freq[data] = 0;
                                order[data] = orderCount++;
                            }
                            freq[data]++;
                            // MFU에서 HIT 시 위치 변경 없음
                            actualLoc = frameSnapshot.IndexOf(data) + 1;
                        }
                    }
                    else if (status == Page.STATUS.PAGEFAULT)
                    {
                        frameSnapshot.Add(data);
                        actualLoc = frameSnapshot.Count;

                        if (core.policy == Core.POLICY.MFU)
                        {
                            freq[data] = 1;
                            order[data] = orderCount++;
                        }
                    }
                    else if (status == Page.STATUS.MIGRATION)
                    {
                        if (core.policy == Core.POLICY.LRU)
                        {
                            char victim = frameSnapshot[0];
                            frameSnapshot.RemoveAt(0);
                            if (freq.ContainsKey(victim))
                            {
                                freq.Remove(victim);
                                order.Remove(victim);
                            }
                            frameSnapshot.Add(data);
                            actualLoc = frameSnapshot.Count;
                        }
                        else // MFU
                        {
                            int victimIndex = 0;
                            char victimChar = frameSnapshot[0];
                            int maxFreq = freq.ContainsKey(victimChar) ? freq[victimChar] : 0;
                            int latest = order.ContainsKey(victimChar) ? order[victimChar] : 0;

                            for (int v = 0; v < frameSnapshot.Count; v++)
                            {
                                char candidate = frameSnapshot[v];
                                int f = freq.ContainsKey(candidate) ? freq[candidate] : 0;
                                int ins = order.ContainsKey(candidate) ? order[candidate] : 0;
                                if (f > maxFreq || (f == maxFreq && ins > latest))
                                {
                                    maxFreq = f;
                                    latest = ins;
                                    victimIndex = v;
                                    victimChar = candidate;
                                }
                            }

                            frameSnapshot.RemoveAt(victimIndex);
                            if (freq.ContainsKey(victimChar))
                            {
                                freq.Remove(victimChar);
                                order.Remove(victimChar);
                            }

                            frameSnapshot.Add(data);
                            freq[data] = 1;
                            order[data] = orderCount++;
                            actualLoc = frameSnapshot.Count;
                        }
                    }

                    for (int j = 0; j <= windowSize; j++)
                    {
                        if (j == 0)
                            DrawGridText(i, j, data);
                        else
                            DrawGrid(i, j);
                    }

                    DrawGridHighlight(i, actualLoc, status);
                    for (int k = 0; k < frameSnapshot.Count && k < windowSize; k++)
                        DrawGridText(i, k + 1, frameSnapshot[k]);
                }
            }
        }


        private void DrawGrid(int x, int y)
        {
            int gridSize = 30;
            int gridSpace = 5;
            int gridBaseX = x * gridSize;
            int gridBaseY = y * gridSize;

            g.DrawRectangle(new Pen(Color.White), new Rectangle(
                gridBaseX + (x * gridSpace),
                gridBaseY,
                gridSize,
                gridSize
                ));
        }        private void DrawGridHighlight(int x, int y, Page.STATUS status)
        {
            int gridSize = 30;
            int gridSpace = 5;
            int gridBaseX = x * gridSize;
            int gridBaseY = y * gridSize;

            SolidBrush highlighter = new SolidBrush(Color.LimeGreen);

            switch (status)
            {
                case Page.STATUS.HIT:
                    highlighter.Color = Color.LimeGreen;  // HIT는 녹색으로 표시
                    break;
                case Page.STATUS.MIGRATION:
                    highlighter.Color = Color.Purple;
                    break;
                case Page.STATUS.PAGEFAULT:
                    highlighter.Color = Color.Red;
                    break;
            }

            g.FillRectangle(highlighter, new Rectangle(
                gridBaseX + (x * gridSpace),
                gridBaseY,
                gridSize,
                gridSize
                ));
        }

        private void DrawGridText(int x, int y, char value)
        {
            int gridSize = 30;
            int gridSpace = 5;
            int gridBaseX = x * gridSize;
            int gridBaseY = y * gridSize;

            g.DrawString(
                value.ToString(), 
                new Font(FontFamily.GenericMonospace, 8), 
                new SolidBrush(Color.White), 
                new PointF(
                    gridBaseX + (x * gridSpace) + gridSize / 3,
                    gridBaseY + gridSize / 4));
        }

        private void btnOperate_Click(object sender, EventArgs e)
        {
            this.tbConsole.Clear();

            if (this.tbQueryString.Text != "" && this.tbWindowSize.Text != "")
            {                string data = this.tbQueryString.Text;
                int windowSize = int.Parse(this.tbWindowSize.Text);

                /* initalize */
                Core.POLICY selectedPolicy = Core.POLICY.FIFO;
                switch (this.comboBox1.Text)
                {
                    case "FIFO": selectedPolicy = Core.POLICY.FIFO; break;
                    case "LRU":  selectedPolicy = Core.POLICY.LRU;  break;
                    case "MFU":  selectedPolicy = Core.POLICY.MFU;  break;
                }
                var window = new Core(windowSize, selectedPolicy);

                foreach ( char element in data )
                {
                    var status = window.Operate(element);
                    this.tbConsole.Text += "DATA " + element + " is " + 
                        ((status == Page.STATUS.PAGEFAULT) ? "Page Fault" : status == Page.STATUS.MIGRATION ? "Migrated" : "Hit")
                        + "\r\n";
                }

                DrawBase(window, windowSize, data.Length);
                this.pbPlaceHolder.Refresh();

                int total = window.hit + window.fault;
                /* 차트 생성 */
                chart1.Series.Clear();
                Series resultChartContent = chart1.Series.Add("Statics");
                resultChartContent.ChartType = SeriesChartType.Pie;
                resultChartContent.IsVisibleInLegend = true;
                resultChartContent.Points.AddXY("Hit", window.hit);
                resultChartContent.Points.AddXY("Fault", window.fault);
                resultChartContent.Points[0].IsValueShownAsLabel = true;
                resultChartContent.Points[0].LegendText = $"Hit {window.hit}";
                resultChartContent.Points[1].IsValueShownAsLabel = true;
                resultChartContent.Points[1].LegendText = $"Fault {window.fault} (Migrated {window.migration})";

                this.lbPageFaultRatio.Text = Math.Round(((float)window.fault / total), 2) * 100 + "%";
            }
            else
            {
            }

        }

        private void pbPlaceHolder_Paint(object sender, PaintEventArgs e)
        {
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void tbWindowSize_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void tbWindowSize_KeyPress(object sender, KeyPressEventArgs e)
        {
                if (!(Char.IsDigit(e.KeyChar)) && e.KeyChar != 8)
                {
                    e.Handled = true;
                }
        }

        private void btnRand_Click(object sender, EventArgs e)
        {
            Random rd = new Random();

            int count = rd.Next(5, 50);
            StringBuilder sb = new StringBuilder();


            for ( int i = 0; i < count; i++ )
            {
                sb.Append((char)rd.Next(65, 90));
            }

            this.tbQueryString.Text = sb.ToString();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            bResultImage.Save("./result.jpg");
        }
    }
}
