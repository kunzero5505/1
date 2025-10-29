using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpUserClient
{
    public class TcpClientHelper
    {
        private TcpClient? tcpClient;
        private NetworkStream? networkStream;
        private readonly string serverIp;
        private readonly int serverPort;

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<Exception>? ErrorOccurred;
        public event EventHandler? Disconnected;

        public bool IsConnected => tcpClient?.Connected ?? false;

        public TcpClientHelper(string serverIp, int serverPort)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;
        }

        public async Task ConnectAsync()
        {
            if (tcpClient?.Connected == true)
            {
                return; // Already connected
            }

            try
            {
                if (tcpClient != null)
                {
                    try
                    {
                        tcpClient.Close();
                        tcpClient.Dispose();
                    }
                    catch { }
                    tcpClient = null;
                }

                tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(serverIp, serverPort);
                
                // Add 3 second timeout
                if (await Task.WhenAny(connectTask, Task.Delay(3000)) != connectTask)
                {
                    // Throw a timeout containing a generic message (we also handle display in the UI)
                    throw new TimeoutException("Không thể kết nối đến server. Vui lòng thử lại sau!");
                }
                
                await connectTask; // Ensure any connection exceptions are thrown

                if (!tcpClient.Connected)
                {
                    throw new Exception("Kết nối không thành công");
                }

                networkStream = tcpClient.GetStream();
                
                // Start listening for responses
                _ = ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (IsConnected && networkStream != null)
                {
                    int bytesRead = await networkStream.ReadAsync(buffer);
                    if (bytesRead == 0) break; // Server closed connection

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                Disconnect();
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected || networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            try
            {
                byte[] messageData = Encoding.UTF8.GetBytes(message);
                await networkStream.WriteAsync(messageData);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (networkStream != null)
                {
                    networkStream.Close();
                    networkStream.Dispose();
                    networkStream = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient.Dispose();
                    tcpClient = null;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}