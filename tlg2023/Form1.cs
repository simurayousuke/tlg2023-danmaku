using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace tlg2023
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        private int latest = 0;
        private bool initialized = false;
        private int speed = 3;
        private int fontSize = 24;

        private Random random = new Random();

        public Form1()
        {
            InitializeComponent();

            // 设置窗口状态为最大化
            this.WindowState = FormWindowState.Maximized;

            // 去除窗口边框
            this.FormBorderStyle = FormBorderStyle.None;

            // 设置窗口大小和位置以覆盖整个屏幕
            this.Bounds = Screen.PrimaryScreen.Bounds;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IntPtr hWnd = this.Handle;
            int style = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            fontSize = GetFontSizeBasedOnResolution();

            using (WebClient client = new WebClient())
            {
                string latestId = client.DownloadString("http://yangdns.net:8080/latestchatid");
                //latest = Int32.Parse(latestId);
                initialized = true;
                timer1.Enabled = true;
            }
        }

        private async void ProcessComment()
        {
            using (WebClient client = new WebClient())
            {
                string commentsJson = client.DownloadString("http://yangdns.net:8080/comments?latest=" + latest.ToString());
                Comment comments = JsonConvert.DeserializeObject<Comment>(commentsJson);
                latest = comments.Latest;

                if (!initialized)
                {
                    initialized = true;
                    return;
                }

                foreach (string str in comments.Data)
                {
                    int delay = random.Next(500);
                    await Task.Delay(delay);
                    CreateMovingLabel(str);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ProcessComment();
        }

        private void CreateMovingLabel(string text)
        {
            TransparentLabel label = new TransparentLabel();
            label.Text = text;
            label.AutoSize = true;
            label.Font = new Font(label.Font.FontFamily, fontSize);
            label.BackColor = Color.Transparent;
            this.Controls.Add(label);

            // 确保 Label 完全位于屏幕内
            int maxY = this.Height - label.Height;
            int randomY = random.Next(0, maxY);

            label.Location = new Point(this.Width, randomY); // 初始位置在屏幕右侧，Y坐标随机

            Timer timer = new Timer();
            timer.Interval = 16; // 定时器间隔
            timer.Tick += (sender, e) => MoveLabel(sender, e, label);
            timer.Start();
        }

        private void MoveLabel(object sender, EventArgs e, Label label)
        {
            if (label.Left + label.Width < 0)
            {
                this.Controls.Remove(label);
                label.Dispose();
                (sender as Timer).Stop();
                (sender as Timer).Dispose();
            }
            else
            {
                label.Left -= speed; // 根据需要调整移动速度
            }
        }

        private int GetFontSizeBasedOnResolution()
        {
            var screenWidth = Screen.PrimaryScreen.Bounds.Width;
            var screenHeight = Screen.PrimaryScreen.Bounds.Height;

            // 这里是一个简单的例子，根据需要调整规则
            if (screenWidth > 1920 && screenHeight > 1080)
                return 24; // 大屏幕分辨率
            else if (screenWidth > 1280 && screenHeight > 720)
                return 16; // 中等屏幕分辨率
            else
                return 12; // 小屏幕分辨率
        }
    }
}
