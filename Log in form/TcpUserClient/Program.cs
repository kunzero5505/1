using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TcpUserClient
{
    static class Program
    {
        /// <summary>
        /// Entry point chính của app
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var loginForm = new LoginForm();
                Application.Run(loginForm);
            }
            catch (SocketException)
            {
                // Show a unified, user-friendly connection error message
                MessageBox.Show(
                    "Không thể kết nối đến server. Vui lòng thử lại sau!",
                    "Lỗi kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn:\n\n{ex.Message}\n\nỨng dụng sẽ đóng.",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}