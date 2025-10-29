# Log-In-Form-By-Using-WIndow-Form-and-C-
Coding by C# and using Winform. Log In and Register In by using SQL Client package and SQL database, string connection

**THÔNG TIN THÀNH VIÊN**
Hồ Hoàng Tiến: 24521762 

Nguyễn Lê Ngọc Ánh: 24520105 

Trần Minh Toàn: 24521798

Vũ Cao Thạch: 24521593

Ngô Văn Tĩnh: 24521791

**HƯỚNG DẪN ĐẦY ĐỦ THÔNG TIN DỰ ÁN**
1. Sử dụng Visual Studio Code - dự án build dotnet 
2. Thực hiện mở terminal, chú ý đã cài đặt đặt đủ .Net và C# để sử dụng 
3. Thực hiện lệnh dẫn để cd đến Log in form folder rồi đến  UserManagementSystem folder 
4. Chạy terminal theo lệnh: "dotnet run" và bắt đầu sử dụng 
![alt text](image.png) 

**Nếu chưa thể chạy do môi trường, cần xem xét các bước bên dưới**
______________________________________
BƯỚC 1: KIỂM TRA HỆ THỐNG
# Kiểm tra .NET SDK
dotnet --version

# Kiểm tra SQL Server services
Get-Service -Name "*SQL*" | Select-Object Name, Status

BƯỚC 2: TẠO DATABASE 
# Tạo database từ file SQL
sqlcmd -S .\SQLEXPRESS -E -i "CreateDatabase.sql"

# Kiểm tra database đã tạo
sqlcmd -S .\SQLEXPRESS -E -Q "SELECT name FROM sys.databases WHERE name = 'UserManagementDB'"

BƯỚC 3: TẠO PROJECT 
# Tạo project WinForms mới
dotnet new winforms -n UserManagementSystem

# Di chuyển vào thư mục project
cd UserManagementSystem

BƯỚC 4: THÊM PACKAGES SQL CLIENT 
# Thêm package SQL Server
dotnet add package System.Data.SqlClient

BƯỚC 6: CẤU HÌNH CONNECTION STRING (NẾU CÓ LỖI)

BƯỚC 7: DOTNET BUILD
# Build project
dotnet build
# Chạy ứng dụng
dotnet run

KIỂM TRA DATABASE NẾU CẦN XÁC THỰC:
# Xem dữ liệu người dùng
sqlcmd -S .\SQLEXPRESS -E -Q "USE UserManagementDB; SELECT UserId, Username, Email, FullName, CreatedDate FROM Users"

# Xem cấu trúc bảng
sqlcmd -S .\SQLEXPRESS -E -Q "USE UserManagementDB; SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users'"

_________________________________
**Trong trường hợp nếu có lỗi phảt sinh và debug, copy lệnh sau và thực thi:
# 1. Dừng ứng dụng
taskkill /F /IM UserManagementSystem.exe

# 2. Di chuyển vào thư mục project
cd UserManagementSystem

# 3. Clean project
dotnet clean

# 4. Xóa thư mục build
rmdir /s /q bin
rmdir /s /q obj

# 5. Restore và build
dotnet restore
dotnet build

# 6. Chạy ứng dụng
dotnet run





_________________________________
**TEST CASES**
Test Case 1: Đăng ký thành công
Input:

Username: testuser
Email: test@example.com
Password: 123456
Confirm: 123456
Expected: Đăng ký thành công, chuyển về login

Test Case 2: Password không khớp
Input:

Username: testuser
Email: test@example.com
Password: 123456
Confirm: 654321
Expected:  "Mật khẩu và xác nhận mật khẩu không khớp!"

Test Case 3: Email sai định dạng
Input:

Email: invalid-email
Expected: "Email không đúng định dạng"

Test Case 4: Username đã tồn tại
Input:

Username: admin (đã có trong DB)
Expected: "Tên đăng nhập đã tồn tại"

Test Case 5: Đăng nhập thành công
Input:

Username: admin
Password: 123456
Expected: Đăng nhập thành công, hiển thị MainForm

Test Case 6: Đăng nhập sai mật khẩu
Input:

Username: admin
Password: wrongpass
Expected: "Mật khẩu không chính xác"
____________________________________
**CHECKLIST HOÀN THÀNH**
 Tạo database và bảng Users
 
 Class PasswordHelper (SHA-256)

 Class DatabaseHelper
 
 LoginForm với validation
 
 RegisterForm với validation
 
 MainForm hiển thị thông tin
 
 Mã hóa mật khẩu
 
 Kiểm tra username, email trùng 
 
 Validation email format
 
 Password confirmation
 
 Show/Hide password
 
 Exception handling
 
 Thông báo lỗi thân thiện
 
 Giao diện đẹp, dễ sử dụng
 Enter key navigation
 Connection string config
 Documentation đầy đủ

