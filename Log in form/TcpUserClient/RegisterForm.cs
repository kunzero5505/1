using System;
using System.Drawing;
using System.Windows.Forms;

namespace TcpUserClient
{
    public partial class RegisterForm : Form
    {
        private bool isShowingError = false;
        private readonly object errorLock = new object();

        private void ShowMessage(string text, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                try 
                {
                    this.Invoke(new Action(() => ShowMessage(text, caption, buttons, icon)));
                }
                catch { }
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
        private readonly TcpClientHelper tcpClient;
        private Label? statusLabel;
        private System.Windows.Forms.Timer? statusTimer;
        private TextBox? txtUsername;
        private TextBox? txtPassword;
        private TextBox? txtConfirmPassword;
        private TextBox? txtEmail;
        private TextBox? txtFullName;
        private CheckBox? chkShowPassword;
        private Button? btnRegister;
        private LinkLabel? linkLogin;        public RegisterForm()
        {
            InitializeComponent();
            tcpClient = new TcpClientHelper("127.0.0.1", 9000);
            tcpClient.MessageReceived += TcpClient_MessageReceived;
            // Chỉ đăng ký xử lý lỗi khi thực sự cần kết nối
            this.FormClosing += RegisterForm_FormClosing;
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Cấu hình Form
            this.Text = "Đăng ký tài khoản";
            this.Size = new Size(450, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 244, 248);



            Panel mainPanel = new Panel
            {
                Size = new Size(400, 490),
                Location = new Point(25, 20),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            mainPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 220, 220), 2))
                {
                    e.Graphics.DrawRectangle(pen,
                        new Rectangle(0, 0, mainPanel.Width - 1, mainPanel.Height - 1));
                }
            };
            
            Label titleLabel = new Label
            {
                Text = "ĐĂNG KÝ TÀI KHOẢN",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                AutoSize = true,
                Location = new Point(80, 20)
            };

            // Username
            Label usernameLabel = new Label
            {
                Text = "Tên đăng nhập: *",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(30, 70)
            };

            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(340, 30),
                Location = new Point(30, 95),
                MaxLength = 50
            };

            // Email
            var emailLabel = new Label
            {
                Text = "Email: *",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(30, 135)
            };

            txtEmail = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(340, 30),
                Location = new Point(30, 160),
                MaxLength = 100
            };

            // Full name
            var fullNameLabel = new Label
            {
                Text = "Họ và tên:",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(30, 200)
            };

            txtFullName = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(340, 30),
                Location = new Point(30, 225),
                MaxLength = 100
            };
            // Email và Fullname đã bị xóa vì không cần thiết trong phiên bản mới

            
            Label passwordLabel = new Label
            {
                Text = "Mật khẩu: * (tối thiểu 6 ký tự)",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(30, 265)
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(340, 30),
                Location = new Point(30, 290),
                MaxLength = 100,
                UseSystemPasswordChar = true
            };

            
            Label confirmPasswordLabel = new Label
            {
                Text = "Xác nhận mật khẩu: *",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(30, 330)
            };

            txtConfirmPassword = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(340, 30),
                Location = new Point(30, 355),
                MaxLength = 100,
                UseSystemPasswordChar = true
            };

            
            chkShowPassword = new CheckBox
            {
                Text = "Hiển thị mật khẩu",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(30, 390)
            };

            chkShowPassword.CheckedChanged += ChkShowPassword_CheckedChanged;

        
            btnRegister = new Button
            {
                Text = "ĐĂNG KÝ",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(340, 40),
                Location = new Point(30, 420),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += BtnRegister_Click;

            // Login Link
            linkLogin = new LinkLabel
            {
                Text = "Đã có tài khoản? Đăng nhập",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(115, 465),
                LinkColor = Color.FromArgb(46, 204, 113)
            };
            linkLogin.LinkClicked += LinkLogin_LinkClicked;

            // Thêm các control vào panel
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(usernameLabel);
            mainPanel.Controls.Add(txtUsername);
            mainPanel.Controls.Add(emailLabel);
            mainPanel.Controls.Add(txtEmail);
            mainPanel.Controls.Add(fullNameLabel);
            mainPanel.Controls.Add(txtFullName);
            mainPanel.Controls.Add(passwordLabel);
            mainPanel.Controls.Add(txtPassword);
            mainPanel.Controls.Add(confirmPasswordLabel);
            mainPanel.Controls.Add(txtConfirmPassword);
            mainPanel.Controls.Add(chkShowPassword);
            mainPanel.Controls.Add(btnRegister);
            mainPanel.Controls.Add(linkLogin);

            // Status label for transient messages
            statusLabel = new Label
            {
                Name = "statusLabel",
                AutoSize = false,
                Size = new Size(340, 20),
                Location = new Point(30, mainPanel.Height - 30),
                ForeColor = Color.DarkRed,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = string.Empty
            };
            mainPanel.Controls.Add(statusLabel);

            statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Interval = 5000;
            statusTimer.Tick += (s, e) =>
            {
                statusTimer.Stop();
                if (statusLabel != null) statusLabel.Text = string.Empty;
            };

            // Thêm panel vào form
            this.Controls.Add(mainPanel);

            // Xử lý phím Enter để chuyển TextBox
            txtUsername.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    txtEmail!.Focus();
                    e.Handled = true;
                }
            };

            txtEmail.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    txtFullName!.Focus();
                    e.Handled = true;
                }
            };

            txtFullName.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    txtPassword!.Focus();
                    e.Handled = true;
                }
            };

            txtPassword.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    txtConfirmPassword!.Focus();
                    e.Handled = true;
                }
            };

            txtConfirmPassword.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    BtnRegister_Click(btnRegister!, EventArgs.Empty);
                    e.Handled = true;
                }
            };
        }

        private void ChkShowPassword_CheckedChanged(object? sender, EventArgs e)
        {
            if (txtPassword != null && chkShowPassword != null)
            {
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            }
            if (txtConfirmPassword != null && chkShowPassword != null)
            {
                txtConfirmPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            }
        }

        private async void BtnRegister_Click(object? sender, EventArgs e)
        {
            try
            {
                // Lấy dữ liệu từ form
                string username = txtUsername!.Text.Trim();
                string email = txtEmail!.Text.Trim();
                string fullName = txtFullName!.Text.Trim();
                string password = txtPassword!.Text;
                string confirmPassword = txtConfirmPassword!.Text;

                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrWhiteSpace(username))
                {
                    ShowMessage("Vui lòng nhập tên đăng nhập.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowMessage("Vui lòng nhập email.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowMessage("Vui lòng nhập mật khẩu.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ShowMessage("Vui lòng xác nhận mật khẩu.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPassword.Focus();
                    return;
                }

                if (password != confirmPassword)
                {
                    ShowMessage("Mật khẩu và xác nhận mật khẩu không khớp!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPassword.Clear();
                    txtConfirmPassword.Focus();
                    return;
                }

                if (!DatabaseHelper.IsValidEmail(email))
                {
                    ShowMessage("Email không đúng định dạng.\nVí dụ: example@domain.com", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                // Thử kết nối tới server trước khi gửi yêu cầu đăng ký
                if (!tcpClient.IsConnected)
                {
                    // Đăng ký các event handler chỉ khi cần kết nối
                    tcpClient.ErrorOccurred += TcpClient_ErrorOccurred;
                    tcpClient.Disconnected += TcpClient_Disconnected;

                    try
                    {
                        await tcpClient.ConnectAsync();
                    }
                    catch (Exception)
                    {
                        ShowMessage("Không thể kết nối đến server. Vui lòng thử lại sau!",
                            "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        
                        // Hủy đăng ký event handler nếu kết nối thất bại
                        tcpClient.ErrorOccurred -= TcpClient_ErrorOccurred;
                        tcpClient.Disconnected -= TcpClient_Disconnected;
                        return;
                    }
                }

                // Send REGISTER request to server: REGISTER|username|password|email|fullName
                string registerRequest = $"REGISTER|{username}|{password}|{email}|{fullName}";
                await tcpClient.SendMessageAsync(registerRequest);

                // The TcpClient_MessageReceived handler will show server response.
            }
            catch (Exception ex)
            {
                ShowMessage($"Đã xảy ra lỗi khi gửi yêu cầu đăng ký: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkLogin_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(450, 550);
            this.Name = "RegisterForm";
            this.ResumeLayout(false);
        }

        // TCP client event handlers (simple defaults)
        private void TcpClient_MessageReceived(object? sender, string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object?, string>(TcpClient_MessageReceived), sender, message);
                return;
            }

            string[] parts = message.Split('|');
            if (parts.Length < 2)
            {
                ShowMessage(message, "Server Response", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string command = parts[0];
            string status = parts[1];

            switch (command)
            {
                case "REGISTER":
                    if (status == "SUCCESS")
                    {
                        ShowMessage("Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.", 
                            "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        try
                        {
                            tcpClient.ErrorOccurred -= TcpClient_ErrorOccurred;
                            tcpClient.Disconnected -= TcpClient_Disconnected;
                            tcpClient.Disconnect();
                        }
                        catch { }

                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        string error = parts.Length > 2 ? parts[2] : "Đăng ký thất bại";
                        ShowMessage(error, "Đăng ký thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;

                default:
                    ShowMessage(message, "Server Response", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
        }

        private void TcpClient_ErrorOccurred(object? sender, Exception ex)
        {
            // Không làm gì cả, để tránh hiển thị thông báo trùng lặp
        }

        private void TcpClient_Disconnected(object? sender, EventArgs e)
        {
            // Không làm gì cả, để tránh hiển thị thông báo trùng lặp
        }

        private void RegisterForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (tcpClient != null && tcpClient.IsConnected)
            {
                tcpClient.Disconnect();
            }
        }
    }

}
