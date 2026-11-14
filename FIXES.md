# Những điểm đã sửa trong bản FIXED

1. Thêm nút kiểm tra SQL Server ngay tại màn hình đăng nhập.
2. Trước khi đăng nhập/đăng ký, ứng dụng kiểm tra đang kết nối đúng database `PRN212_LibraryManagement` và có đủ 4 bảng/cột bắt buộc.
3. Khi lưu tài khoản thất bại, hiển thị lỗi gốc SQL Server (trùng Username/Email, sai schema, thiếu cột, sai Role, dữ liệu quá dài...) thay vì chỉ hiện thông báo EF Core chung chung.
4. Kiểm tra độ dài FullName, Email, Username; kiểm tra định dạng Email; kiểm tra mật khẩu và xác nhận mật khẩu.
5. Vô hiệu hóa nút Đăng ký trong lúc xử lý để tránh nhấn nhiều lần.
6. Bổ sung kiểm tra schema cho thao tác đăng nhập và đăng ký.
7. Cập nhật mapping EF Core để khớp kiểu dữ liệu và giá trị mặc định của SQL.
8. Các màn hình CRUD và danh sách cũng hiển thị lỗi chi tiết hơn.
9. Bổ sung `Sql/Verify_Database.sql` để kiểm tra database đang dùng có đúng schema hay không.
10. Bổ sung hướng dẫn tạo `appsettings.json` trên từng máy; file này được .gitignore để tránh commit mật khẩu SQL Server lên GitHub.

## Kiểm tra đã thực hiện trong môi trường đóng gói

- Tất cả XAML được parse như XML thành công.
- Tất cả event handler được khai báo trong XAML đều tìm thấy hàm tương ứng trong code-behind.
- `appsettings.json` và `appsettings.example.json` là JSON hợp lệ.
- Tên thuộc tính 4 Entity được đối chiếu với 4 bảng SQL.

**Giới hạn:** Môi trường đóng gói không có .NET SDK/Visual Studio nên chưa thể chạy `dotnet build` hoặc kiểm thử kết nối SQL Server thật. Cần build và chạy trên Windows để xác nhận theo SQL Server instance của máy bạn.
