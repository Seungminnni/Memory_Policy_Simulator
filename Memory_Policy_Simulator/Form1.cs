﻿using System;
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
                        DrawGridText(i, depth++, t);                }
            }
            else if (core.policy == Core.POLICY.LRU || core.policy == Core.POLICY.MFU)
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
            else // NEW
            {
                List<char> frameSnapshot = new List<char>();

                for (int i = 0; i < dataLength; i++)
                {
                    var page = core.pageHistory[i];
                    char data = page.data;

                    if (page.status == Page.STATUS.HIT)
                    {
                        frameSnapshot.Remove(data);
                        frameSnapshot.Insert(0, data);
                    }
                    else if (page.status == Page.STATUS.PAGEFAULT)
                    {
                        frameSnapshot.Insert(0, data);
                    }
                    else if (page.status == Page.STATUS.MIGRATION)
                    {
                        if (frameSnapshot.Count >= windowSize)
                            frameSnapshot.RemoveAt(frameSnapshot.Count - 1);
                        frameSnapshot.Insert(0, data);
                    }

                    for (int j = 0; j <= windowSize; j++)
                    {
                        if (j == 0)
                            DrawGridText(i, j, data);
                        else
                            DrawGrid(i, j);
                    }

                    DrawGridHighlight(i, page.loc, page.status);
                    for (int k = 0; k < frameSnapshot.Count && k < windowSize; k++)
                        DrawGridText(i, k + 1, frameSnapshot[k]);
                }

                // ensure final snapshot matches internal state
                var finalFrames = core.GetCurrentFrames();
                int row = dataLength - 1;
                for (int k = 0; k < finalFrames.Count && k < windowSize; k++)
                    DrawGridText(row, k + 1, finalFrames[k]);
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
                gridSize                ));
        }

        private void DrawGridHighlight(int x, int y, Page.STATUS status)
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

            if (this.tbQueryString.Text != string.Empty && this.tbWindowSize.Text != string.Empty)
            {
                string data = this.tbQueryString.Text;
                int frameSize = int.Parse(this.tbWindowSize.Text);

                Core.POLICY selectedPolicy = Core.POLICY.FIFO;
                switch (this.comboBox1.Text)
                {
                    case "FIFO": selectedPolicy = Core.POLICY.FIFO; break;
                    case "LRU":  selectedPolicy = Core.POLICY.LRU;  break;
                    case "MFU":  selectedPolicy = Core.POLICY.MFU;  break;
                    case "NEW":  selectedPolicy = Core.POLICY.NEW;  break;
                }
                int phaseWindow = 5;
                double threshold = 0.5;
                if (int.TryParse(this.tbPhaseWindow.Text, out int tmpW)) phaseWindow = tmpW;
                if (double.TryParse(this.tbThreshold.Text, out double tmpT)) threshold = tmpT;                Core sim = new Core(frameSize, selectedPolicy, phaseWindow, threshold, data.ToList());

                foreach (char element in data)
                {
                    var status = sim.Operate(element);
                    this.tbConsole.AppendText(
                        $"DATA {element} is " +
                        (status == Page.STATUS.PAGEFAULT ? "Page Fault" : status == Page.STATUS.MIGRATION ? "Migrated" : "Hit") +
                        "\r\n");
                }

                DrawBase(sim, frameSize, data.Length);
                this.pbPlaceHolder.Refresh();

                /* 차트 생성 */
                chart1.Series.Clear();
                Series resultChartContent = chart1.Series.Add("Statistics");
                resultChartContent.ChartType = SeriesChartType.Pie;
                resultChartContent.IsVisibleInLegend = true;
                resultChartContent.Points.AddXY("Hit", sim.hit);
                resultChartContent.Points.AddXY("Fault", sim.fault);
                resultChartContent.Points[0].IsValueShownAsLabel = true;
                resultChartContent.Points[0].LegendText = $"Hit {sim.hit}";
                resultChartContent.Points[1].IsValueShownAsLabel = true;
                resultChartContent.Points[1].LegendText = $"Fault {sim.fault} (Migrated {sim.migration})";

                int total = sim.hit + sim.fault;
                if (total > 0)
                    this.lbPageFaultRatio.Text = Math.Round(((float)sim.fault / total) * 100, 2) + "%";
                else
                    this.lbPageFaultRatio.Text = "0%";
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

        private void tbPhaseWindow_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsDigit(e.KeyChar)) && e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }

        private void tbThreshold_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsDigit(e.KeyChar) || e.KeyChar == '.' || e.KeyChar == 8))
            {
                e.Handled = true;
            }
        }

        private void btnRand_Click(object sender, EventArgs e)
        {
            Random rd = new Random();

            int count = rd.Next(5, 50);
            StringBuilder sb = new StringBuilder();            for (int i = 0; i < count; i++)
            {
                sb.Append((char)rd.Next(65, 90));
            }

            this.tbQueryString.Text = sb.ToString();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            bResultImage.Save("./result.jpg");
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comboBox1.Text == "NEW")
            {
                if (string.IsNullOrWhiteSpace(this.tbPhaseWindow.Text))
                    this.tbPhaseWindow.Text = "5";
                if (string.IsNullOrWhiteSpace(this.tbThreshold.Text))
                    this.tbThreshold.Text = "3";
            }
            else
            {
                this.tbPhaseWindow.Text = string.Empty;
                this.tbThreshold.Text = string.Empty;
            }
        }
    }
}
