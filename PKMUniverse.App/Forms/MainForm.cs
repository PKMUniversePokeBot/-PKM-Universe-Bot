// PKM Universe Bot - Main Form
// Written by PKM Universe - 2025

using System.Diagnostics;
using System.Drawing.Drawing2D;
using PKHeX.Core;
using PKMUniverse.Core.Config;
using PKMUniverse.Core.Logging;
using PKMUniverse.Trade.Executor;

namespace PKMUniverse.App.Forms;

public partial class MainForm : Form
{
    // Theme colors
    private static readonly Color AccentColor = Color.FromArgb(138, 43, 226);
    private static readonly Color DarkBg = Color.FromArgb(18, 18, 24);
    private static readonly Color CardBg = Color.FromArgb(28, 28, 36);
    private static readonly Color TextColor = Color.FromArgb(240, 240, 245);
    private static readonly Color TextSecondary = Color.FromArgb(160, 160, 170);

    // Core components
    private ProgramConfig _config;
    private TradeBotRunner _runner;
    private readonly DateTime _startTime = DateTime.Now;
    private int _totalTrades;
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    // Navigation
    private Panel _navPanel = null!;
    private Button _btnBots = null!;
    private Button _btnHub = null!;
    private Button _btnLogs = null!;
    private Button _btnTools = null!;
    private Panel _navIndicator = null!;

    // Content panels
    private Panel _headerPanel = null!;
    private Panel _botsPanel = null!;
    private Panel _hubPanel = null!;
    private Panel _logsPanel = null!;
    private Panel _toolsPanel = null!;

    // Header controls
    private Label _lblTitle = null!;
    private Label _lblQueueCount = null!;
    private Label _lblTradeCount = null!;
    private Label _lblUptime = null!;

    // Bots panel controls
    private FlowLayoutPanel _botCardsFlow = null!;
    private Button _btnAddBot = null!;
    private Button _btnStartAll = null!;
    private Button _btnStopAll = null!;

    // Hub panel controls
    private PropertyGrid _hubPropertyGrid = null!;

    // Logs panel controls
    private RichTextBox _logTextBox = null!;

    // Tools panel controls
    private Label _lblCpu = null!;
    private Label _lblMemory = null!;

    // Timer
    private System.Windows.Forms.Timer _updateTimer = null!;

    public MainForm()
    {
        InitializeComponent();
        LoadConfig();
        InitializeUI();
        SetupRunner();
        SetupTimer();
    }

    private void LoadConfig()
    {
        _config = ProgramConfig.Load();
    }

    private void SetupRunner()
    {
        _runner = new TradeBotRunner(_config.Hub.MaxQueueSize);
        _runner.OnTradeComplete += (pokemon, trainer, success) =>
        {
            if (success) _totalTrades++;
            LogMessage($"Trade {(success ? "completed" : "failed")}: {pokemon} to {trainer}");
        };
        _runner.OnBotStatusChanged += (bot, status) =>
        {
            LogMessage($"[{bot}] {status}");
        };

        Logger.OnLog += (source, message, level) =>
        {
            LogMessage($"[{source}] {message}");
        };

        Logger.Info("App", "PKM Universe Bot initialized");
    }

    private void SetupTimer()
    {
        _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _updateTimer.Tick += (s, e) => UpdateUI();
        _updateTimer.Start();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 750);
        Text = "PKM Universe Bot";
        BackColor = DarkBg;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 600);

        ResumeLayout(false);
    }

    private void InitializeUI()
    {
        CreateNavigation();
        CreateHeader();
        CreateBotsPanel();
        CreateHubPanel();
        CreateLogsPanel();
        CreateToolsPanel();
        ShowPanel(_botsPanel, _btnBots);
    }

    private void CreateNavigation()
    {
        _navPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = CardBg,
            Padding = new Padding(10)
        };

        var logoLabel = new Label
        {
            Text = "PKM Universe",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = AccentColor,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _navIndicator = new Panel
        {
            Width = 4,
            Height = 40,
            BackColor = AccentColor,
            Location = new Point(0, 60)
        };

        _btnBots = CreateNavButton("Bots", 60);
        _btnHub = CreateNavButton("Hub", 110);
        _btnLogs = CreateNavButton("Logs", 160);
        _btnTools = CreateNavButton("Tools", 210);

        _btnBots.Click += (s, e) => ShowPanel(_botsPanel, _btnBots);
        _btnHub.Click += (s, e) => ShowPanel(_hubPanel, _btnHub);
        _btnLogs.Click += (s, e) => ShowPanel(_logsPanel, _btnLogs);
        _btnTools.Click += (s, e) => ShowPanel(_toolsPanel, _btnTools);

        _navPanel.Controls.AddRange(new Control[] { logoLabel, _navIndicator, _btnBots, _btnHub, _btnLogs, _btnTools });
        Controls.Add(_navPanel);
    }

    private Button CreateNavButton(string text, int top)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            ForeColor = TextColor,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 11),
            Size = new Size(180, 40),
            Location = new Point(10, top),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(15, 0, 0, 0),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(40, 40, 50) }
        };
    }

    private void CreateHeader()
    {
        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = CardBg,
            Padding = new Padding(20, 15, 20, 15)
        };
        _headerPanel.Paint += (s, e) =>
        {
            using var brush = new LinearGradientBrush(
                _headerPanel.ClientRectangle,
                Color.FromArgb(45, 45, 60),
                CardBg,
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, _headerPanel.ClientRectangle);
        };

        _lblTitle = new Label
        {
            Text = "PKM Universe Bot",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = TextColor,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _lblQueueCount = new Label
        {
            Text = "Queue: 0",
            Font = new Font("Segoe UI", 10),
            ForeColor = TextSecondary,
            AutoSize = true,
            Location = new Point(800, 15)
        };

        _lblTradeCount = new Label
        {
            Text = "Trades: 0",
            Font = new Font("Segoe UI", 10),
            ForeColor = TextSecondary,
            AutoSize = true,
            Location = new Point(800, 35)
        };

        _lblUptime = new Label
        {
            Text = "Uptime: 00:00:00",
            Font = new Font("Segoe UI", 10),
            ForeColor = TextSecondary,
            AutoSize = true,
            Location = new Point(800, 55)
        };

        _headerPanel.Controls.AddRange(new Control[] { _lblTitle, _lblQueueCount, _lblTradeCount, _lblUptime });
        Controls.Add(_headerPanel);
    }

    private void CreateBotsPanel()
    {
        _botsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkBg,
            Padding = new Padding(20),
            Visible = false
        };

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.Transparent
        };

        _btnAddBot = CreateButton("Add Bot", 0);
        _btnStartAll = CreateButton("Start All", 120);
        _btnStopAll = CreateButton("Stop All", 240);

        _btnAddBot.Click += (s, e) => AddBot();
        _btnStartAll.Click += (s, e) => _runner.Start();
        _btnStopAll.Click += (s, e) => _runner.Stop();

        toolbar.Controls.AddRange(new Control[] { _btnAddBot, _btnStartAll, _btnStopAll });

        _botCardsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        _botsPanel.Controls.Add(_botCardsFlow);
        _botsPanel.Controls.Add(toolbar);
        Controls.Add(_botsPanel);
    }

    private Button CreateButton(string text, int left)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(110, 35),
            Location = new Point(left, 5),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
    }

    private void CreateHubPanel()
    {
        _hubPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkBg,
            Padding = new Padding(20),
            Visible = false
        };

        var titleLabel = new Label
        {
            Text = "Hub Configuration",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 40
        };

        _hubPropertyGrid = new PropertyGrid
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            LineColor = Color.FromArgb(50, 50, 60),
            CategoryForeColor = AccentColor,
            ViewForeColor = TextColor,
            ViewBackColor = CardBg,
            SelectedObject = _config.Hub
        };

        var saveButton = new Button
        {
            Text = "Save Configuration",
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Bottom,
            Height = 40,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        saveButton.Click += (s, e) =>
        {
            _config.Save();
            LogMessage("Configuration saved");
        };

        _hubPanel.Controls.Add(_hubPropertyGrid);
        _hubPanel.Controls.Add(saveButton);
        _hubPanel.Controls.Add(titleLabel);
        Controls.Add(_hubPanel);
    }

    private void CreateLogsPanel()
    {
        _logsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkBg,
            Padding = new Padding(20),
            Visible = false
        };

        var titleLabel = new Label
        {
            Text = "Logs",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 40
        };

        _logTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            ForeColor = TextColor,
            Font = new Font("Consolas", 10),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };

        var clearButton = new Button
        {
            Text = "Clear Logs",
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Bottom,
            Height = 40,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        clearButton.Click += (s, e) => _logTextBox.Clear();

        _logsPanel.Controls.Add(_logTextBox);
        _logsPanel.Controls.Add(clearButton);
        _logsPanel.Controls.Add(titleLabel);
        Controls.Add(_logsPanel);
    }

    private void CreateToolsPanel()
    {
        _toolsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkBg,
            Padding = new Padding(20),
            Visible = false
        };

        var titleLabel = new Label
        {
            Text = "System Tools",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 40
        };

        var statsPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = CardBg
        };

        _lblCpu = new Label
        {
            Text = "CPU: 0%",
            Font = new Font("Segoe UI", 12),
            ForeColor = TextColor,
            Location = new Point(20, 20),
            AutoSize = true
        };

        _lblMemory = new Label
        {
            Text = "Memory: 0 MB",
            Font = new Font("Segoe UI", 12),
            ForeColor = TextColor,
            Location = new Point(20, 50),
            AutoSize = true
        };

        statsPanel.Controls.AddRange(new Control[] { _lblCpu, _lblMemory });

        _toolsPanel.Controls.Add(statsPanel);
        _toolsPanel.Controls.Add(titleLabel);
        Controls.Add(_toolsPanel);
    }

    private void ShowPanel(Panel panel, Button button)
    {
        _botsPanel.Visible = false;
        _hubPanel.Visible = false;
        _logsPanel.Visible = false;
        _toolsPanel.Visible = false;

        panel.Visible = true;
        panel.BringToFront();

        _navIndicator.Top = button.Top;
    }

    private void AddBot()
    {
        using var dialog = new AddBotDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var config = dialog.BotConfig;
            _config.Bots.Add(config);
            _config.Save();
            _ = _runner.AddBotAsync(config);
            UpdateBotCards();
        }
    }

    private void UpdateBotCards()
    {
        _botCardsFlow.Controls.Clear();
        foreach (var status in _runner.GetBotStatuses())
        {
            var card = CreateBotCard(status);
            _botCardsFlow.Controls.Add(card);
        }
    }

    private Panel CreateBotCard(BotStatus status)
    {
        var card = new Panel
        {
            Size = new Size(300, 150),
            BackColor = CardBg,
            Margin = new Padding(10)
        };

        var nameLabel = new Label
        {
            Text = status.Name,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = TextColor,
            Location = new Point(15, 15),
            AutoSize = true
        };

        var statusLabel = new Label
        {
            Text = status.IsProcessing ? $"Trading: {status.CurrentTrainer}" :
                   status.IsConnected ? "Idle" : "Disconnected",
            Font = new Font("Segoe UI", 10),
            ForeColor = status.IsConnected ? Color.LimeGreen : Color.Red,
            Location = new Point(15, 45),
            AutoSize = true
        };

        var tradesLabel = new Label
        {
            Text = $"Trades: {status.TradeCount}",
            Font = new Font("Segoe UI", 10),
            ForeColor = TextSecondary,
            Location = new Point(15, 70),
            AutoSize = true
        };

        var removeButton = new Button
        {
            Text = "Remove",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(180, 60, 60),
            ForeColor = Color.White,
            Size = new Size(80, 30),
            Location = new Point(15, 105),
            FlatAppearance = { BorderSize = 0 }
        };
        removeButton.Click += (s, e) =>
        {
            _runner.RemoveBot(status.Name);
            UpdateBotCards();
        };

        card.Controls.AddRange(new Control[] { nameLabel, statusLabel, tradesLabel, removeButton });
        return card;
    }

    private void UpdateUI()
    {
        _lblQueueCount.Text = $"Queue: {_runner.QueueSize}";
        _lblTradeCount.Text = $"Trades: {_totalTrades}";

        var uptime = DateTime.Now - _startTime;
        _lblUptime.Text = $"Uptime: {uptime:hh\\:mm\\:ss}";

        UpdateBotCards();

        if (_toolsPanel.Visible)
        {
            try
            {
                _currentProcess.Refresh();
                var memoryMB = _currentProcess.WorkingSet64 / 1024 / 1024;
                _lblMemory.Text = $"Memory: {memoryMB} MB";
                _lblCpu.Text = $"CPU: {_currentProcess.TotalProcessorTime.TotalSeconds:F1}s";
            }
            catch { }
        }
    }

    private void LogMessage(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => LogMessage(message));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logTextBox.AppendText($"[{timestamp}] {message}\n");
        _logTextBox.ScrollToCaret();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _updateTimer?.Stop();
        _runner?.Stop();
        _config?.Save();
        base.OnFormClosing(e);
    }
}

public class AddBotDialog : Form
{
    public SwitchBotConfig BotConfig { get; private set; } = new();

    private TextBox _txtName = null!;
    private TextBox _txtIP = null!;
    private NumericUpDown _numPort = null!;
    private ComboBox _cmbGame = null!;

    public AddBotDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Add Bot";
        Size = new Size(350, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(28, 28, 36);

        var lblName = CreateLabel("Bot Name:", 20);
        _txtName = CreateTextBox(45);
        _txtName.Text = "Bot1";

        var lblIP = CreateLabel("IP Address:", 80);
        _txtIP = CreateTextBox(105);
        _txtIP.Text = "192.168.1.1";

        var lblPort = CreateLabel("Port:", 140);
        _numPort = new NumericUpDown
        {
            Location = new Point(20, 165),
            Size = new Size(290, 25),
            Minimum = 1,
            Maximum = 65535,
            Value = 6000,
            BackColor = Color.FromArgb(40, 40, 50),
            ForeColor = Color.White
        };

        var btnOK = new Button
        {
            Text = "Add",
            DialogResult = DialogResult.OK,
            Location = new Point(130, 200),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(138, 43, 226),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOK.FlatAppearance.BorderSize = 0;
        btnOK.Click += (s, e) =>
        {
            BotConfig = new SwitchBotConfig
            {
                Name = _txtName.Text,
                IP = _txtIP.Text,
                Port = (int)_numPort.Value,
                Game = Core.Config.GameVersion.LegendsZA
            };
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(220, 200),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(60, 60, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        Controls.AddRange(new Control[] { lblName, _txtName, lblIP, _txtIP, lblPort, _numPort, btnOK, btnCancel });
    }

    private Label CreateLabel(string text, int top)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.White,
            Location = new Point(20, top),
            AutoSize = true
        };
    }

    private TextBox CreateTextBox(int top)
    {
        return new TextBox
        {
            Location = new Point(20, top),
            Size = new Size(290, 25),
            BackColor = Color.FromArgb(40, 40, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
    }
}
