-- Tạo database
CREATE DATABASE UserManagementDB;
GO

USE UserManagementDB;
GO

-- Tạo bảng Users
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(256) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    FullName NVARCHAR(100),
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastLoginDate DATETIME NULL
);
GO

-- Tạo index cho tìm kiếm nhanh
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
GO

-- Tạo bảng Tokens (tùy chọn)
CREATE TABLE UserTokens (
    TokenId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId INT NOT NULL,
    ExpireAt DATETIME NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO