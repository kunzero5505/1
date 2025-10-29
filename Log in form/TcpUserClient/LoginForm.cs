using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace TcpUserClient
{
    public partial class LoginForm : Form
    {
        private readonly TcpClientHelper tcpClient;
        private bool isShowingError = false;
        private readonly object errorLock = new object();
        private TextBox? usernameTextBox;
        private TextBox? passwordTextBox;
        private Button? loginButton;
        private CheckBox? showPasswordCheckBox;
        private LinkLabel? registerLink;
    private Label? statusLabel;
    private System.Windows.Forms.Timer? statusTimer;
        
        // Note: event handler implementations moved/implemented later in the file (single consistent set)

        public LoginForm()
        {
            InitializeComponent();
            tcpClient = new TcpClientHelper("127.0.0.1", 9000);
            tcpClient.MessageReceived += TcpClient_MessageReceived;
            tcpClient.ErrorOccurred += TcpClient_ErrorOccurred;
            tcpClient.Disconnected += TcpClient_Disconnected;
            InitializeCustomComponents();
            
            // Try to connect when form loads (show status inside form instead of modal MessageBox)
            this.Load += async (s, e) =>
            {
                try
                {
                    await tcpClient.ConnectAsync();
                }
                catch (Exception)
                {
                    if (statusLabel != null)
                    {
                        // Use unified connection error message for status label as well
                        statusLabel.Text = "Không thể kết nối đến server. Vui lòng thử lại sau!";
                        statusTimer?.Stop();
                        statusTimer?.Start();
                    }
                }
            };
        }

        private void ShowMessage(string text, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowMessage(text, caption, buttons, icon)));
                return;
            }

            lock (errorLock)
            {
                if (!isShowingError)
                {
                    isShowingError = true;
                    try
                    {
                        MessageBox.Show(this, text, caption, buttons, icon);
                    }
                    finally
                    {
                        isShowingError = false;
                    }
                }
            }
        }

        private void InitializeCustomComponents()
        {
            // Cấu hình Form
            this.Text = "Đăng nhập hệ thống";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 244, 248);

   
            Panel mainPanel = new Panel
            {
                Size = new Size(400, 320),
                Location = new Point(25, 20),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

          
            mainPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 220), 2),
                    new Rectangle(0, 0, mainPanel.Width - 1, mainPanel.Height - 1));
            };

      
            Label titleLabel = new Label
            {
                Text = "ĐĂNG NHẬP",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                AutoSize = true,
                Location = new Point(130, 20)
            };

   
            Label usernameLabel = new Label
            {
                Text = "Tên đăng nhập:",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(30, 80)
            };


            usernameTextBox = new TextBox
            {
                Name = "txtUsername",
                Font = new Font("Segoe UI", 11),
                Size = new Size(340, 30),
                Location = new Point(30, 105),
                MaxLength = 50
            };

      
            Label passwordLabel = new Label
            {
                Text = "Mật khẩu:",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(30, 145)
            };

 
            passwordTextBox = new TextBox
            {
                Name = "txtPassword",
                Font = new Font("Segoe UI", 11),
                Size = new Size(340, 30),
                Location = new Point(30, 170),
                MaxLength = 100,
                UseSystemPasswordChar = true
            };

           
            showPasswordCheckBox = new CheckBox
            {
                Text = "Hiển thị mật khẩu",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(30, 205)
            };

            showPasswordCheckBox.CheckedChanged += (s, e) =>
            {
                passwordTextBox.UseSystemPasswordChar = !showPasswordCheckBox.Checked;
            };

            loginButton = new Button
            {
                Text = "ĐĂNG NHẬP",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(340, 40),
                Location = new Point(30, 235),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            
            loginButton.Click += async (s, e) =>
            {
                try
                {
                    if (!tcpClient.IsConnected)
                    {
                        try
                        {
                            await tcpClient.ConnectAsync();
                        }
                        catch (Exception)
                        {
                            ShowMessage("Không thể kết nối đến server. Vui lòng thử lại sau!",
                                "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    string username = usernameTextBox?.Text?.Trim() ?? string.Empty;
                    string password = passwordTextBox?.Text ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(username))
                    {
                        ShowMessage("Vui lòng nhập tên đăng nhập.", "Thông báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        usernameTextBox?.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        ShowMessage("Vui lòng nhập mật khẩu.", "Thông báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        passwordTextBox?.Focus();
                        return;
                    }

                    loginButton.Enabled = false;
                    try
                    {
                        await tcpClient.SendMessageAsync($"LOGIN|{username}|{password}");
                    }
                    finally
                    {
                        loginButton.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    // If we get here, show an explicit connection/send error
                    ShowMessage($"Lỗi khi đăng nhập: {ex.Message}", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    loginButton.Enabled = true;
                }
            };
            loginButton.FlatAppearance.BorderSize = 0;

            
            registerLink = new LinkLabel
            {
                Text = "Chưa có tài khoản? Đăng ký ngay",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(105, 285),
                LinkColor = Color.FromArgb(41, 128, 185)
            };
            registerLink.LinkClicked += RegisterLink_LinkClicked;

            // Status label for showing non-blocking messages (connection/info)
            statusLabel = new Label
            {
                Name = "statusLabel",
                AutoSize = false,
                Size = new Size(340, 22),
                Location = new Point(30, 255),
                ForeColor = Color.DarkRed,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = string.Empty
            };

            statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Interval = 5000;
            statusTimer.Tick += (snd, evt) =>
            {
                statusTimer.Stop();
                if (statusLabel != null) statusLabel.Text = string.Empty;
            };

            // Thêm các control vào panel 
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(usernameLabel);
            mainPanel.Controls.Add(usernameTextBox);
            mainPanel.Controls.Add(passwordLabel);
            mainPanel.Controls.Add(passwordTextBox);
            mainPanel.Controls.Add(showPasswordCheckBox);
            mainPanel.Controls.Add(loginButton);
            mainPanel.Controls.Add(statusLabel);
            mainPanel.Controls.Add(registerLink);

            this.Controls.Add(mainPanel);

            // Xử lý Enter button nhấn 
            usernameTextBox.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    passwordTextBox?.Focus();
                    e.Handled = true;
                }
            };

            passwordTextBox.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    loginButton?.PerformClick();
                    e.Handled = true;
                }
            };
        }

        private async void LoginButton_Click(object sender, EventArgs e)
        {
            try 
            {
                if (!tcpClient.IsConnected)
                {
                    await tcpClient.ConnectAsync();
                }
                if (usernameTextBox != null && passwordTextBox != null)
                {
                    await LoginButton_ClickAsync(usernameTextBox, passwordTextBox);
                }
            }
            catch (Exception ex)
            {
                if (statusLabel != null)
                {
                    statusLabel.Text = $"Lỗi kết nối: {ex.Message}";
                    statusTimer?.Stop();
                    statusTimer?.Start();
                }
            }
        }

        private async Task LoginButton_ClickAsync(TextBox usernameTextBox, TextBox passwordTextBox)
        {
            string username = usernameTextBox.Text.Trim();
            string password = passwordTextBox.Text;

            // check dữ liệu input 
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowMessage("Vui lòng nhập tên đăng nhập.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                usernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowMessage("Vui lòng nhập mật khẩu.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                passwordTextBox.Focus();
                return;
            }

            try
            {
                // Gửi yêu cầu đăng nhập đến server
                string loginRequest = $"LOGIN|{username}|{password}";
                await tcpClient.SendMessageAsync(loginRequest);
            }
            catch (Exception ex)
            {
                if (statusLabel != null)
                {
                    statusLabel.Text = $"Lỗi kết nối: {ex.Message}";
                    statusTimer?.Stop();
                    statusTimer?.Start();
                }
            }
        }

        private void RegisterLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            RegisterForm registerForm = new RegisterForm();
            registerForm.FormClosed += (s, args) => this.Show();
            this.Hide();
            registerForm.Show();
        }

        private void TcpClient_MessageReceived(object? sender, string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object?, string>(TcpClient_MessageReceived), sender, message);
                return;
            }

            string[] parts = message.Split('|');
            if (parts.Length < 2) return;

            string command = parts[0];
            string status = parts[1];

            switch (command)
            {
                case "LOGIN":
                    if (status == "SUCCESS")
                    {
                        if (parts.Length >= 6)
                        {
                            int userId = int.Parse(parts[2]);
                            string username = parts[3].Trim();
                            string email = parts[4].Trim();
                            string fullName = parts[5].Trim();

                            // Prefer showing full name when available; fallback to username
                            string displayName = string.IsNullOrWhiteSpace(fullName) ? username : fullName;

                            ShowMessage($"Đăng nhập thành công!\nChào mừng {displayName}!",
                                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Mở form chính với đầy đủ thông tin người dùng
                            // Pass the existing tcpClient so MainForm can notify server on logout
                            MainForm mainForm = new MainForm(userId, username, email, fullName, tcpClient);
                            mainForm.FormClosed += (s, args) => this.Show();
                            this.Hide();
                            mainForm.Show();
                        }
                    }
                    else if (status == "ERROR")
                    {
                        string errorMessage = parts.Length > 2 ? parts[2] : "Đăng nhập thất bại";
                        ShowMessage(errorMessage, "Lỗi đăng nhập",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
            }
        }

        private void TcpClient_ErrorOccurred(object? sender, Exception ex)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object?, Exception>(TcpClient_ErrorOccurred), sender, ex);
                return;
            }

            if (statusLabel != null)
            {
                statusLabel.Text = $"Lỗi kết nối: {ex.Message}";
                statusTimer?.Stop();
                statusTimer?.Start();
            }
        }

        private void TcpClient_Disconnected(object? sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object?, EventArgs>(TcpClient_Disconnected), sender, e);
                return;
            }

            if (statusLabel != null)
            {
                statusLabel.Text = "Mất kết nối với máy chủ";
                statusTimer?.Stop();
                statusTimer?.Start();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(450, 380);
            this.Name = "LoginForm";
            this.ResumeLayout(false);
        }
    }
}