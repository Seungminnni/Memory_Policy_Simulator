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
        }        private void DrawBase(Core core, int windowSize, int dataLength)
        {
            g.Clear(Color.Black);
            for (int i = 0; i < dataLength; i++)
            {
                char data = core.pageHistory[i].data;
                Page.STATUS status = core.pageHistory[i].status;
                int loc = core.pageHistory[i].loc;               

                // 그리드 배경 및 데이터 표시
                for (int j = 0; j <= windowSize; j++)
                {
                    if (j == 0) DrawGridText(i, j, data);
                    else DrawGrid(i, j);
                }                // 프레임 스냅샷 표시
                var snapshot = core.framesHistory[i];
                for (int depth = 1; depth <= snapshot.Count; depth++)
                {
                    DrawGridText(i, depth, snapshot[depth - 1]);
                }
                
                // Status에 따른 처리
                if (status == Page.STATUS.HIT)
                {
                    // Hit인 경우: 레퍼런스 스트링과 실제 데이터가 있는 프레임 모두 강조 표시
                    DrawGridHighlight(i, 0, status); // 레퍼런스 스트링
                    DrawGridHighlight(i, loc, status); // 실제 메모리 프레임 내 위치
                }
                else
                {
                    // Hit가 아닌 경우: 레퍼런스 스트링만 강조 표시
                    DrawGridHighlight(i, loc, status);
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
                    highlighter.Color = Color.LimeGreen; // HIT는 밝은 녹색
                    break;
                case Page.STATUS.MIGRATION:
                    highlighter.Color = Color.Purple; // MIGRATION은 보라색
                    break;
                case Page.STATUS.PAGEFAULT:
                    highlighter.Color = Color.Red; // PAGEFAULT는 빨간색
                    break;
            }            // 사각형 영역을 해당 색상으로 채움
            g.FillRectangle(highlighter, new Rectangle(
                gridBaseX + (x * gridSpace),
                gridBaseY,
                gridSize,
                gridSize
                ));
                
            // 해당 영역의 테두리를 강조하기 위해 추가 (선명하게 보이도록)
            g.DrawRectangle(new Pen(highlighter.Color, 2), new Rectangle(
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

            if (this.tbQueryString.Text != "" || this.tbWindowSize.Text != "")
            {
                string data = this.tbQueryString.Text;
                int windowSize = int.Parse(this.tbWindowSize.Text);

                /* initalize */                // 선택된 정책 확인 (FIFO / LRU / LFU)
                Core.POLICY selectedPolicy;
                if (comboBox1.Text == "FIFO")
                    selectedPolicy = Core.POLICY.FIFO;
                else if (comboBox1.Text == "LRU")
                    selectedPolicy = Core.POLICY.LRU;
                else
                    selectedPolicy = Core.POLICY.LFU;
                
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
