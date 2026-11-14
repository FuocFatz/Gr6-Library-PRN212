/*
    KIỂM TRA DATABASE PRN212
    Chạy script này sau khi đã chạy PRN212_LibraryManagement_Final.sql.
    Script chỉ đọc dữ liệu và cấu trúc, không thay đổi database.
*/
USE PRN212_LibraryManagement;
GO

/* Kiểm tra tên database đang sử dụng. */
SELECT DB_NAME() AS TenDatabaseHienTai;
GO

/* Kiểm tra bốn bảng chính đã được tạo. */
SELECT TABLE_NAME AS TenBang
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = N'dbo'
  AND TABLE_TYPE = N'BASE TABLE'
ORDER BY TABLE_NAME;
GO

/* Kiểm tra các cột của bốn bảng. */
SELECT TABLE_NAME AS TenBang,
       COLUMN_NAME AS TenCot,
       DATA_TYPE AS KieuDuLieu,
       IS_NULLABLE AS ChoPhepNull
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'dbo'
  AND TABLE_NAME IN (N'Users', N'Books', N'BorrowRecords', N'Payments')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
GO

/* Đếm số dòng hiện có trong từng bảng. */
SELECT N'Users' AS TenBang, COUNT_BIG(*) AS SoDong FROM dbo.Users
UNION ALL
SELECT N'Books', COUNT_BIG(*) FROM dbo.Books
UNION ALL
SELECT N'BorrowRecords', COUNT_BIG(*) FROM dbo.BorrowRecords
UNION ALL
SELECT N'Payments', COUNT_BIG(*) FROM dbo.Payments;
GO
