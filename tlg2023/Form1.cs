using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
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

        private const int GwlExStyle = -20;
        private const int WsExLayered = 0x80000;
        private const int WsExTransparent = 0x20;

        private int _latest = 0;
        private bool _initialized = false;

        private int _speed = 3;
        private int _fontSize = 48;
        private bool _fromLatest = true;
        private int _updateInterval = 60;

        private NotifyIcon _trayIcon;
        private readonly Random random = new Random();

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

        private void Initialize()
        {
            if (_fromLatest)
            {
                using (var client = new WebClient())
                {
                    var latestId = client.DownloadString("http://yangdns.net:8080/latestchatid");
                    _latest = int.Parse(latestId);
                }
            }
            else
            {
                _latest = 0;
            }

            _initialized = true;
            timer1.Enabled = true;
        }

        private void LoadConfig(bool restart = false)
        {
            // check if the config file exist. if not create one
            if (!File.Exists("config.json"))
            {
                var defaultConfig = new Config { Speed = 3, FontSize = 48, FromLatest = true, FPS = 60 };
                var defaultConfigJson = JsonConvert.SerializeObject(defaultConfig);
                File.WriteAllText("config.json", defaultConfigJson);
            }

            var configJson = File.ReadAllText("config.json");
            var config = JsonConvert.DeserializeObject<Config>(configJson);

            _updateInterval = (int)(1.0 / config.FPS * 1000.0);
            _speed = (int)(config.Speed * _updateInterval / 16);
            _fontSize = config.FontSize;
            _fromLatest = config.FromLatest;

            if (restart)
                Initialize();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IntPtr hWnd = this.Handle;
            int style = GetWindowLong(hWnd, GwlExStyle);
            SetWindowLong(hWnd, GwlExStyle, style | WsExLayered | WsExTransparent);

            LoadConfig(true);

            //Initialize(false);

            // add tray icon when right click show a menu
            _trayIcon = new NotifyIcon();
            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("ReloadConfig", (senderObject, eventArgs) => LoadConfig());
            trayMenu.MenuItems.Add("Restart", (senderObject, eventArgs) => Initialize());
            trayMenu.MenuItems.Add("Exit", (senderObject, eventArgs) => Application.Exit());
            _trayIcon.Text = "tlg2023-danmaku";
            _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.ContextMenu = trayMenu;
            _trayIcon.Visible = true;
        }

        private async void ProcessComment()
        {
            using (WebClient client = new WebClient())
            {
                string commentsJson = client.DownloadString("http://yangdns.net:8080/comments?latest=" + _latest.ToString());
                Comment comments = JsonConvert.DeserializeObject<Comment>(commentsJson);
                _latest = comments.Latest;

                if (!_initialized)
                {
                    _initialized = true;
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

            label.Font = new Font(label.Font.FontFamily, _fontSize);
            label.BackColor = Color.Transparent;
            this.Controls.Add(label);

            // 确保 Label 完全位于屏幕内
            int maxY = this.Height - label.Height;
            int randomY = random.Next(0, maxY);

            label.Location = new Point(this.Width, randomY); // 初始位置在屏幕右侧，Y坐标随机

            Timer timer = new Timer();
            timer.Interval = _updateInterval; // 定时器间隔
            timer.Tick += (sender, e) => MoveLabel(sender, e, label);
            timer.Start();
        }

        private void MoveLabel(object sender, EventArgs e, Label label)
        {
            var timer = sender as Timer;
            if (timer != null && timer.Interval != _updateInterval)
            {
                timer.Interval = _updateInterval;
            }

            if (label.Left + label.Width < 0)
            {
                this.Controls.Remove(label);
                label.Dispose();

                timer?.Stop();
                timer?.Dispose();
            }
            else
            {
                label.Left -= _speed; // 根据需要调整移动速度
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        //private int GetFontSizeBasedOnResolution()
        //{
        //    return 48;
        //    var screenWidth = Screen.PrimaryScreen.Bounds.Width;
        //    var screenHeight = Screen.PrimaryScreen.Bounds.Height;

        //    // 这里是一个简单的例子，根据需要调整规则
        //    if (screenWidth > 1920 && screenHeight > 1080)
        //        return 48; // 大屏幕分辨率
        //    else if (screenWidth > 1280 && screenHeight > 720)
        //        return 20; // 中等屏幕分辨率
        //    else
        //        return 12; // 小屏幕分辨率
        //}
    }

    internal class Config
    {
        public int Speed { get; set; }
        public int FontSize { get; set; }
        public bool FromLatest { get; set; }
        public float FPS { get; set; }
    }
}
