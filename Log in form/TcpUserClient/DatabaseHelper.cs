using System;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace TcpUserClient
{
    public class DatabaseHelper
    {
    // Connection string (editable later via UI if needed)
    // Add TrustServerCertificate=True for local development to avoid SSL provider trust errors
    private const string ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=UserManagementDB;Integrated Security=True;TrustServerCertificate=True";

        public static bool TestConnection()
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RegisterUser(string username, string password, string email, string fullName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!ValidateRegistrationData(username, password, email, out errorMessage))
                return false;

            try
            {
                if (IsUsernameExists(username))
                {
                    errorMessage = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";
                    return false;
                }

                if (IsEmailExists(email))
                {
                    errorMessage = "Email đã được đăng ký. Vui lòng sử dụng email khác.";
                    return false;
                }

                string hashedPassword = PasswordHelper.HashPassword(password);

                using var connection = new SqlConnection(ConnectionString);
                const string query = @"INSERT INTO Users (Username, Password, Email, FullName, CreatedDate) 
                                       VALUES (@Username, @Password, @Email, @FullName, @CreatedDate)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", hashedPassword);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@FullName", string.IsNullOrWhiteSpace(fullName) ? (object)DBNull.Value : fullName);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                connection.Open();
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi khi đăng ký: {ex.Message}";
                return false;
            }
        }

        public static bool ValidateRegistrationData(string username, string password, string email, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Tên đăng nhập không được để trống";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Mật khẩu không được để trống";
                return false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Email không được để trống";
                return false;
            }

            // Validate email format
            if (!IsValidEmail(email))
            {
                errorMessage = "Email không đúng định dạng";
                return false;
            }

            return true;
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsUsernameExists(string username)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username", connection);
            command.Parameters.AddWithValue("@Username", username);
            connection.Open();
            return (int)command.ExecuteScalar() > 0;
        }

        public static bool IsEmailExists(string email)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);
            connection.Open();
            return (int)command.ExecuteScalar() > 0;
        }
    }
}
