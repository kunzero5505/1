using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
// Microsoft.Data.SqlClient used for database access; removed duplicate System.Data.SqlClient

namespace TcpUserServer
{
#nullable enable
    public partial class ServerForm : Form
    {
        private TcpListener? tcpListener;
        private List<TcpClient> clients = new List<TcpClient>();
        private bool isRunning;
        private CancellationTokenSource? cancellationTokenSource;
    // Allow trusting the server certificate for local dev to avoid SSL Provider trust errors.
    private const string ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=UserManagementDB;Integrated Security=True;TrustServerCertificate=True";

        // Form controls
        protected TextBox portTextBox = null!;
        protected Button startButton = null!;
        protected Button stopButton = null!;
        protected Button clearLogButton = null!;
        protected TextBox logTextBox = null!;
        protected Label portLabel = null!;
        protected Label logLabel = null!;
        
        private class UserInfo
        {
            public int Id { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string? Email { get; set; }
            public string? FullName { get; set; }

            public UserInfo()
            {
                Username = string.Empty;
                Password = string.Empty;
                Email = string.Empty;
                FullName = string.Empty;
            }
        }

        private bool LoginUser(string username, string password, out string errorMessage, out UserInfo? userInfo)
        {
            errorMessage = string.Empty;
            userInfo = null;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SELECT UserId, Username, Password, Email, FullName FROM Users WHERE Username = @Username", connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var storedPassword = reader["Password"].ToString();
                                if (storedPassword != null && VerifyPassword(password, storedPassword))
                                {
                                    var usernameStr = reader["Username"].ToString();
                                    if (usernameStr != null)
                                    {
                                        userInfo = new UserInfo
                                        {
                                            Id = Convert.ToInt32(reader["UserId"]),
                                            Username = usernameStr,
                                            Password = storedPassword,
                                            Email = reader["Email"].ToString(),
                                            FullName = reader["FullName"].ToString()
                                        };
                                        return true;
                                    }
                                }
                                errorMessage = "Invalid password";
                                return false;
                            }
                            errorMessage = "User not found";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Database error: {ex.Message}";
                return false;
            }
        }

        private bool RegisterUser(string username, string password, string email, string fullName, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // Check if username already exists
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username", connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        int count = (int)command.ExecuteScalar();
                        if (count > 0)
                        {
                            errorMessage = "Username already exists";
                            return false;
                        }
                    }

                    // Insert new user
                    using (var command = new SqlCommand(@"INSERT INTO Users (Username, Password, Email, FullName) 
                        VALUES (@Username, @Password, @Email, @FullName)", connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", HashPassword(password));
                        // Use the email/fullName passed into this method
                        command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                        command.Parameters.AddWithValue("@FullName", string.IsNullOrEmpty(fullName) ? (object)DBNull.Value : fullName);
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Database error: {ex.Message}";
                return false;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            // First check Base64 (current format)
            try
            {
                if (HashPassword(password) == hashedPassword)
                    return true;
            }
            catch
            {
                // ignore and try other formats
            }

            // Also accept old hex-encoded SHA256 hashes (64 hex chars)
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    var sb = new System.Text.StringBuilder();
                    foreach (byte b in hashedBytes)
                        sb.Append(b.ToString("x2"));
                    string hex = sb.ToString();
                    if (string.Equals(hex, hashedPassword, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }
        public ServerForm()
        {
            InitializeComponent();
                clients = new List<TcpClient>();
        }

        private void InitializeComponent()
        {
            this.portTextBox = new TextBox();
            this.startButton = new Button();
            this.stopButton = new Button();
            this.clearLogButton = new Button();
            this.logTextBox = new TextBox();
            this.portLabel = new Label();
            this.logLabel = new Label();

            // Port Label
            this.portLabel.Text = "Port :";
            this.portLabel.Location = new System.Drawing.Point(20, 20);
            this.portLabel.Size = new System.Drawing.Size(50, 20);
            // Match client login tone: blue accent for labels
            this.portLabel.ForeColor = System.Drawing.Color.FromArgb(41, 128, 185);

            // Port TextBox
            this.portTextBox.Location = new System.Drawing.Point(80, 20);
            this.portTextBox.Size = new System.Drawing.Size(100, 20);
            this.portTextBox.Text = "9000";
            // Input fields use white background like client panel
            this.portTextBox.BackColor = System.Drawing.Color.White;
            this.portTextBox.ForeColor = System.Drawing.Color.FromArgb(10, 50, 100);
            this.portTextBox.BorderStyle = BorderStyle.FixedSingle;

            // Start Button
            this.startButton.Location = new System.Drawing.Point(20, 60);
            this.startButton.Size = new System.Drawing.Size(100, 30);
            this.startButton.Text = "Start";
            this.startButton.Click += new EventHandler(StartButton_Click);
            // Use the same primary blue as the client login form
            this.startButton.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            this.startButton.ForeColor = System.Drawing.Color.White;
            this.startButton.FlatStyle = FlatStyle.Flat;
            this.startButton.FlatAppearance.BorderSize = 0;

            // Stop Button
            this.stopButton.Location = new System.Drawing.Point(20, 100);
            this.stopButton.Size = new System.Drawing.Size(100, 30);
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new EventHandler(StopButton_Click);
            this.stopButton.Enabled = false;
            this.stopButton.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            this.stopButton.ForeColor = System.Drawing.Color.White;
            this.stopButton.FlatStyle = FlatStyle.Flat;
            this.stopButton.FlatAppearance.BorderSize = 0;

            // Log Label
            this.logLabel.Text = "Log :";
            this.logLabel.Location = new System.Drawing.Point(200, 20);
            this.logLabel.Size = new System.Drawing.Size(50, 20);
            this.logLabel.ForeColor = System.Drawing.Color.FromArgb(41, 128, 185);

            // Log TextBox
            this.logTextBox.Location = new System.Drawing.Point(200, 50);
            this.logTextBox.Multiline = true;
            this.logTextBox.ScrollBars = ScrollBars.Vertical;
            this.logTextBox.Size = new System.Drawing.Size(400, 300);
            this.logTextBox.ReadOnly = true;
            // Log area uses white panel look like client
            this.logTextBox.BackColor = System.Drawing.Color.White;
            this.logTextBox.ForeColor = System.Drawing.Color.FromArgb(10, 50, 100);
            this.logTextBox.BorderStyle = BorderStyle.FixedSingle;

            // Clear Log Button
            this.clearLogButton.Location = new System.Drawing.Point(20, 140);
            this.clearLogButton.Size = new System.Drawing.Size(100, 30);
            this.clearLogButton.Text = "Clear Log";
            this.clearLogButton.Click += new EventHandler(ClearLogButton_Click);
            this.clearLogButton.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            this.clearLogButton.ForeColor = System.Drawing.Color.White;
            this.clearLogButton.FlatStyle = FlatStyle.Flat;
            this.clearLogButton.FlatAppearance.BorderSize = 0;

            // Form
            this.ClientSize = new System.Drawing.Size(620, 380);
            this.Controls.AddRange(new Control[] {
                this.portLabel,
                this.portTextBox,
                this.startButton,
                this.stopButton,
                this.clearLogButton,
                this.logLabel,
                this.logTextBox
            });
            this.Text = "Server";
            // Match client background tone
            this.BackColor = System.Drawing.Color.FromArgb(240, 244, 248);
            this.ForeColor = System.Drawing.Color.FromArgb(10, 50, 100);
        }



        private async void StartButton_Click(object? sender, EventArgs e)
        {
                startButton.Enabled = false;
                portTextBox.Enabled = false;
            
            if (int.TryParse(portTextBox.Text, out int port))
            {
                try
                {
                    // Test DB connection before starting the server
                    if (!TestDatabaseConnection(out string dbError))
                    {
                        LogMessage($"Database connection failed: {dbError}");
                        MessageBox.Show($"Cannot connect to database:\n{dbError}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        startButton.Enabled = true;
                        portTextBox.Enabled = true;
                        return;
                    }

                    tcpListener = new TcpListener(IPAddress.Any, port);
                    tcpListener.Start();
                    isRunning = true;
                    
                    cancellationTokenSource = new CancellationTokenSource();
                    LogMessage($"Server started on port {port}");
                    
                    stopButton.Enabled = true;

                    await AcceptClientsAsync(cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error starting server: {ex.Message}");
                    MessageBox.Show($"Error starting server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        startButton.Enabled = true;
                        portTextBox.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid port number", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    startButton.Enabled = true;
                    portTextBox.Enabled = true;
            }
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && tcpListener != null)
                {
                    TcpClient client = await tcpListener.AcceptTcpClientAsync();
                    clients.Add(client);
                    var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    var address = endpoint?.Address.ToString() ?? "unknown";
                    LogMessage($"New client connected from {address}");
                    
                    // Handle client in separate task
                    _ = HandleClientAsync(client, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogMessage($"Error accepting clients: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            var address = endpoint?.Address.ToString() ?? "unknown";

            try
            {
                using NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0) break; // Client disconnected
                    
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    LogMessage($"Received from {address}: {message}");
                    
                    // Process message and send response
                    string response = ProcessClientMessage(message);
                    byte[] responseData = Encoding.UTF8.GetBytes(response);

                    // Log what we're about to send so we can debug connection issues from the server side
                    LogMessage($"Sending to {address}: {response}");

                    try
                    {
                        await stream.WriteAsync(responseData, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Log the full exception for diagnostics and break the receive loop
                        LogMessage($"Error sending to {address}: {ex}");
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log full exception details to help debugging network/socket issues
                LogMessage($"Error handling client: {ex}");
            }
            finally
            {
                clients.Remove(client);
                client.Dispose();
                LogMessage($"Client disconnected from {address}");
            }
        }

        private string ProcessClientMessage(string message)
        {
            try
            {
                string[] parts = message.Split('|');
                if (parts.Length < 1) return "ERROR|Invalid message format";

                string command = parts[0];
                LogMessage($"Processing command: {command}");

                switch (command)
                {
                    case "LOGIN":
                        if (parts.Length < 3)
                            return "LOGIN|ERROR|Missing username or password";

                        string username = parts[1];
                        string password = parts[2];

                        try
                        {
                            bool loginSuccess = LoginUser(username, password, out string errorMessage, out UserInfo? userInfo);
                            if (loginSuccess && userInfo != null)
                            {
                                return $"LOGIN|SUCCESS|{userInfo.Id}|{userInfo.Username}|{userInfo.Email}|{userInfo.FullName}";
                            }
                            else
                            {
                                return $"LOGIN|ERROR|{errorMessage}";
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Login error: {ex.Message}");
                            return "LOGIN|ERROR|Internal server error";
                        }

                    // Handle client-initiated logout
                    case "LOGOUT":
                        // Expected format: LOGOUT|userId (userId optional)
                        try
                        {
                            string logoutUser = parts.Length > 1 ? parts[1] : "(unknown)";
                            LogMessage($"Processing command: LOGOUT for user {logoutUser}");
                            // Return a clear acknowledgement so client can close gracefully
                            return $"LOGOUT|SUCCESS|{logoutUser}";
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Logout error: {ex.Message}");
                            return "LOGOUT|ERROR|Internal server error";
                        }

                    // Add other commands here (REGISTER, etc.)
                    case "REGISTER":
                        // Expected format: REGISTER|username|password|email|fullName
                        // username and password are required; email and fullName are optional
                        if (parts.Length < 3)
                            return "REGISTER|ERROR|Missing registration fields";

                        string regUsername = parts.Length > 1 ? parts[1] : string.Empty;
                        string regPassword = parts.Length > 2 ? parts[2] : string.Empty;
                        string regEmail = parts.Length > 3 ? parts[3] : string.Empty;
                        string regFullName = parts.Length > 4 ? parts[4] : string.Empty;

                        try
                        {
                            bool registerSuccess = RegisterUser(regUsername, regPassword, regEmail, regFullName, out string regError);
                            if (registerSuccess)
                            {
                                return "REGISTER|SUCCESS";
                            }
                            else
                            {
                                return $"REGISTER|ERROR|{regError}";
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Register error: {ex.Message}");
                            return "REGISTER|ERROR|Internal server error";
                        }
                    
                    default:
                        return $"ERROR|Unknown command: {command}";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error processing message: {ex.Message}");
                return "ERROR|Error processing request";
            }
        }

        private void StopButton_Click(object? sender, EventArgs e)
        {
            StopServer();
        }

        private void StopServer()
        {
            try
            {
                isRunning = false;
                cancellationTokenSource?.Cancel();
                
                // Close all client connections
                foreach (var client in clients.ToArray())
                {
                    client.Close();
                }
                clients.Clear();

                tcpListener?.Stop();
                LogMessage("Server stopped");
                
                startButton.Enabled = true;
                stopButton.Enabled = false;
                portTextBox.Enabled = true;
            }
            catch (Exception ex)
            {
                LogMessage($"Error stopping server: {ex.Message}");
            }
        }

        private void ClearLogButton_Click(object? sender, EventArgs e)
        {
            logTextBox.Clear();
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRunning)
            {
                StopServer();
            }
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
                return;
            }

            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            logTextBox.ScrollToCaret();
        }

        /// <summary>
        /// Try opening a connection to the database to validate configuration.
        /// Returns true when successful; otherwise false and an error message.
        /// </summary>
        private bool TestDatabaseConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    // quick query to ensure DB exists and has Users table
                    using (var cmd = new SqlCommand("SELECT TOP 1 1 FROM Users", conn))
                    {
                        try
                        {
                            var _ = cmd.ExecuteScalar();
                        }
                        catch (Exception ex)
                        {
                            errorMessage = ex.Message;
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}