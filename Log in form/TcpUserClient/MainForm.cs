using System;
using System.Drawing;
using System.Windows.Forms;

namespace TcpUserClient
{
    public partial class MainForm : Form
    {
        private readonly int userId;
        private readonly string username;
        private readonly string email;
        private readonly string fullName;
        private readonly TcpClientHelper? tcpClient;
        private bool isShowingError = false;
        private readonly object errorLock = new object();

        // Accept the existing TcpClientHelper so the main form can send a LOGOUT message
        public MainForm(int userId, string username, string email, string fullName, TcpClientHelper? tcpClient = null)
        {
            InitializeComponent();
            this.userId = userId;
            this.username = username;
            this.email = email;
            this.fullName = fullName;
            this.tcpClient = tcpClient;
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Cấu hình Form
            this.Text = "Trang chủ - User Management System";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 244, 248);

          
            Panel headerPanel = new Panel
            {
                Size = new Size(700, 80),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(52, 73, 94),
                Dock = DockStyle.Top
            };

            Label headerLabel = new Label
            {
                Text = "QUẢN LÝ NGƯỜI DÙNG",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(30, 25)
            };

            headerPanel.Controls.Add(headerLabel);

         
            Panel userInfoPanel = new Panel
            {
                Size = new Size(640, 280),
                Location = new Point(30, 110),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            userInfoPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 220), 2),
                    new Rectangle(0, 0, userInfoPanel.Width - 1, userInfoPanel.Height - 1));
            };

            // Tiêu đề thông tin
            Label infoTitleLabel = new Label
            {
                Text = "THÔNG TIN CÁ NHÂN",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            
            Label avatarLabel = new Label
            {
                Text = "👤",
                Font = new Font("Segoe UI", 60),
                AutoSize = true,
                Location = new Point(270, 60)
            };

            Label userIdLabel = new Label
            {
                Text = "ID Người dùng:",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 150)
            };

            Label userIdValue = new Label
            {
                Text = userId.ToString(),
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                Location = new Point(200, 150),
                ForeColor = Color.FromArgb(52, 152, 219)
            };

           
            Label usernameLabel = new Label
            {
                Text = "Tên đăng nhập:",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 180)
            };

            Label usernameValue = new Label
            {
                Text = username,
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                Location = new Point(200, 180),
                ForeColor = Color.FromArgb(52, 152, 219)
            };

           
            Label emailLabel = new Label
            {
                Text = "Email:",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 210)
            };

            Label emailValue = new Label
            {
                Text = email,
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                Location = new Point(200, 210),
                ForeColor = Color.FromArgb(52, 152, 219)
            };

            
            Label fullNameLabel = new Label
            {
                Text = "Họ và tên:",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 240)
            };

            Label fullNameValue = new Label
            {
                Text = string.IsNullOrEmpty(fullName) ? "(Chưa cập nhật)" : fullName,
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                Location = new Point(200, 240),
                ForeColor = string.IsNullOrEmpty(fullName) ? Color.Gray : Color.FromArgb(52, 152, 219)
            };

            
            userInfoPanel.Controls.Add(infoTitleLabel);
            userInfoPanel.Controls.Add(avatarLabel);
            userInfoPanel.Controls.Add(userIdLabel);
            userInfoPanel.Controls.Add(userIdValue);
            userInfoPanel.Controls.Add(usernameLabel);
            userInfoPanel.Controls.Add(usernameValue);
            userInfoPanel.Controls.Add(emailLabel);
            userInfoPanel.Controls.Add(emailValue);
            userInfoPanel.Controls.Add(fullNameLabel);
            userInfoPanel.Controls.Add(fullNameValue);

            
            Button logoutButton = new Button
            {
                Text = "ĐĂNG XUẤT",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(200, 45),
                Location = new Point(250, 410),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.Click += async (s, e) => await LogoutButton_ClickAsync(s, e);

            // Thêm các control vào form
            this.Controls.Add(headerPanel);
            this.Controls.Add(userInfoPanel);
            this.Controls.Add(logoutButton);

            // Xử lý sự kiện đóng form (gán với main form) 
            this.FormClosing += MainForm_FormClosing;
        }

        private async Task LogoutButton_ClickAsync(object? sender, EventArgs e)
        {
            DialogResult result = ShowDialog(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Tell server we're logging out so it can close gracefully
                try
                {
                    if (tcpClient != null && tcpClient.IsConnected)
                    {
                        // Best-effort logout message; don't block UI too long
                        await tcpClient.SendMessageAsync($"LOGOUT|{userId}");
                        // Give the message a short moment to be sent
                        await Task.Delay(100);
                        tcpClient.Disconnect();
                    }
                }
                catch
                {
                    // ignore errors during logout - we'll still close the form
                }

                ShowMessage("Đã đăng xuất thành công!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Đảm bảo không đóng ứng dụng khi đóng MainForm
            Application.Exit();
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

        private DialogResult ShowDialog(string text, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            if (this.InvokeRequired)
            {
                return (DialogResult)this.Invoke(new Func<DialogResult>(() => ShowDialog(text, caption, buttons, icon)));
            }

            lock (errorLock)
            {
                isShowingError = true;
                try
                {
                    return MessageBox.Show(this, text, caption, buttons, icon);
                }
                finally
                {
                    isShowingError = false;
                }
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(700, 500);
            this.Name = "MainForm";
            this.ResumeLayout(false);
        }
    }
}