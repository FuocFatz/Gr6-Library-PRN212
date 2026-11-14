/*
    PRN212 - HỆ THỐNG QUẢN LÝ THƯ VIỆN
    Tên cơ sở dữ liệu: PRN212_LibraryManagement
    Thiết kế đơn giản gồm 4 bảng, phù hợp với project WPF của nhóm 2 thành viên.

    Ghi chú:
    - Bảng Users lưu cả ba loại tài khoản: Admin, Librarian và Customer.
    - Mỗi dòng trong BorrowRecords đại diện cho một cuốn sách được mượn.
    - Payments liên kết đến BorrowRecords; phiếu mượn có thể được tạo trước khi thanh toán.
    - PasswordHash phải chứa mật khẩu đã được băm trong ứng dụng, không lưu mật khẩu thuần.
*/

IF DB_ID(N'PRN212_LibraryManagement') IS NULL
BEGIN
    CREATE DATABASE PRN212_LibraryManagement;
END;
GO

USE PRN212_LibraryManagement;
GO

/*
    Kiểm tra an toàn:
    Script này dành cho cơ sở dữ liệu mới hoặc cơ sở dữ liệu chưa có bảng.
    Nếu đã tồn tại bảng, script sẽ dừng để tránh trộn cấu trúc cũ với cấu trúc 4 bảng mới.
*/
IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
BEGIN
    THROW 50001,
        N'Cơ sở dữ liệu PRN212_LibraryManagement đã có bảng. Hãy sao lưu dữ liệu cần giữ và tạo cơ sở dữ liệu trống trước khi chạy script bản chốt.',
        1;
END;
GO

/* ============================================================
   1. USERS - Tài khoản Admin, Thủ thư và Khách hàng
   ============================================================ */
CREATE TABLE dbo.Users
(
    UserId       INT IDENTITY(1,1) NOT NULL,
    Username     NVARCHAR(50)  NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName     NVARCHAR(150) NOT NULL,
    Email        NVARCHAR(150) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL,

    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Librarian', N'Customer'))
);
GO

/* ============================================================
   2. BOOKS - Thông tin sách và số lượng trong thư viện
   ============================================================ */
CREATE TABLE dbo.Books
(
    BookId            INT IDENTITY(1,1) NOT NULL,
    BookTitle         NVARCHAR(150) NOT NULL,
    BookCategory      NVARCHAR(100) NOT NULL,
    TotalQuantity     INT NOT NULL CONSTRAINT DF_Books_TotalQuantity DEFAULT (0),
    AvailableQuantity INT NOT NULL CONSTRAINT DF_Books_AvailableQuantity DEFAULT (0),

    CONSTRAINT PK_Books PRIMARY KEY (BookId),
    CONSTRAINT CK_Books_TotalQuantity CHECK (TotalQuantity >= 0),
    CONSTRAINT CK_Books_AvailableQuantity CHECK
        (AvailableQuantity >= 0 AND AvailableQuantity <= TotalQuantity)
);
GO

/* ============================================================
   3. BORROW RECORDS - Mỗi dòng là một cuốn sách được mượn
   ============================================================ */
CREATE TABLE dbo.BorrowRecords
(
    BorrowId   INT IDENTITY(1,1) NOT NULL,
    UserId     INT NOT NULL,
    BookId     INT NOT NULL,
    BorrowDate DATE NOT NULL
        CONSTRAINT DF_BorrowRecords_BorrowDate DEFAULT (CONVERT(DATE, GETDATE())),
    DueDate    DATE NOT NULL,
    ReturnDate DATE NULL,
    Status     NVARCHAR(20) NOT NULL
        CONSTRAINT DF_BorrowRecords_Status DEFAULT (N'Borrowed'),

    CONSTRAINT PK_BorrowRecords PRIMARY KEY (BorrowId),
    CONSTRAINT FK_BorrowRecords_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_BorrowRecords_Books FOREIGN KEY (BookId)
        REFERENCES dbo.Books(BookId),
    CONSTRAINT CK_BorrowRecords_DueDate CHECK (DueDate >= BorrowDate),
    CONSTRAINT CK_BorrowRecords_ReturnDate CHECK
        (ReturnDate IS NULL OR ReturnDate >= BorrowDate),
    CONSTRAINT CK_BorrowRecords_Status CHECK
        (Status IN (N'Borrowed', N'Returned', N'Cancelled')),
    CONSTRAINT CK_BorrowRecords_ReturnStatus CHECK
        (
            (Status = N'Returned' AND ReturnDate IS NOT NULL)
            OR
            (Status IN (N'Borrowed', N'Cancelled') AND ReturnDate IS NULL)
        )
);
GO

/* ============================================================
   4. PAYMENTS - Giao dịch thanh toán cho một lượt mượn
   ============================================================ */
CREATE TABLE dbo.Payments
(
    PaymentId     INT IDENTITY(1,1) NOT NULL,
    BorrowId      INT NOT NULL,
    Amount        DECIMAL(18,2) NOT NULL,
    PaymentWay    NVARCHAR(30) NOT NULL,
    PaymentDate   DATETIME2(0) NOT NULL
        CONSTRAINT DF_Payments_PaymentDate DEFAULT (SYSDATETIME()),
    PaymentStatus NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Payments_PaymentStatus DEFAULT (N'Completed'),

    CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
    CONSTRAINT UQ_Payments_BorrowId UNIQUE (BorrowId),
    CONSTRAINT FK_Payments_BorrowRecords FOREIGN KEY (BorrowId)
        REFERENCES dbo.BorrowRecords(BorrowId),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_PaymentWay CHECK
        (PaymentWay IN (N'Cash', N'BankTransfer')),
    CONSTRAINT CK_Payments_PaymentStatus CHECK
        (PaymentStatus IN (N'Pending', N'Completed', N'Failed'))
);
GO

/* Tạo chỉ mục để tìm lịch sử mượn theo khách hàng hoặc theo sách nhanh hơn. */
CREATE INDEX IX_BorrowRecords_UserId
    ON dbo.BorrowRecords(UserId);
GO

CREATE INDEX IX_BorrowRecords_BookId
    ON dbo.BorrowRecords(BookId);
GO

/*
    DỮ LIỆU SÁCH MẪU: Dùng để thử chức năng xem danh sách và CRUD sách.
    Có thể xóa các dòng dữ liệu mẫu này nếu không cần.
*/
INSERT INTO dbo.Books (BookTitle, BookCategory, TotalQuantity, AvailableQuantity)
VALUES
    (N'Clean Code', N'Programming', 5, 5),
    (N'Database System Concepts', N'Database', 4, 4),
    (N'Introduction to C#', N'Programming', 6, 6);
GO

/* KIỂM TRA: Xem dữ liệu trong 4 bảng sau khi tạo cơ sở dữ liệu. */
SELECT * FROM dbo.Users;
SELECT * FROM dbo.Books;
SELECT * FROM dbo.BorrowRecords;
SELECT * FROM dbo.Payments;
GO
