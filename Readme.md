# PRN212 – Library Management System (WPF .NET 8)

Bộ source dành cho phần việc của thành viên phụ trách **đăng nhập, đăng ký tài khoản demo, Admin, Thủ thư và CRUD sách**. Source sử dụng WPF, C#, EF Core 8 và SQL Server.

> Source được viết theo hướng code-behind để sinh viên dễ đọc, chạy thử và giải thích khi bảo vệ. Nghiệp vụ tạo lượt mượn, xác nhận trả và thanh toán sẽ được thành viên còn lại tích hợp.

## 1. Phần đã có

### Đăng nhập và đăng ký

- Đăng nhập bằng Username và Password.
- Điều hướng theo Role: `Admin`, `Librarian`, `Customer`.
- Đăng ký tài khoản demo cho cả ba vai trò.
- Mật khẩu được băm PBKDF2-SHA256 trước khi lưu vào `Users.PasswordHash`.
- Kiểm tra Username/Email trùng và kiểm tra định dạng/độ dài dữ liệu.
- Có nút **Kiểm tra kết nối database** ngay tại màn hình đăng nhập.
- Khi SQL Server từ chối lưu dữ liệu, ứng dụng hiển thị nguyên nhân sâu nhất thay vì chỉ báo lỗi chung.

**Lưu ý:** Cho phép tự chọn Admin/Thủ thư chỉ phục vụ demo bài tập. Ứng dụng thực tế cần giới hạn đăng ký công khai ở Customer; Admin tạo tài khoản nhân viên từ khu vực quản trị.

### Admin

- Xem danh sách tài khoản.
- Xóa tài khoản chưa phát sinh lịch sử mượn; không thể xóa tài khoản đang đăng nhập.
- Quản lý sách: xem, tìm kiếm, thêm, sửa, xóa.

### Thủ thư

- Quản lý sách: xem, tìm kiếm, thêm, sửa, xóa.
- Xem danh sách mượn – trả.
- Phần tạo lượt mượn, xác nhận trả và thanh toán chi tiết sẽ được tích hợp cùng thành viên phụ trách nghiệp vụ.

### Khách hàng

- Xem danh mục sách.
- Xem lịch sử mượn của chính tài khoản đang đăng nhập.
- Phần tạo lượt mượn và thanh toán sẽ được tích hợp sau.

## 2. Cấu trúc database – 4 bảng đã chốt

Tên database: `PRN212_LibraryManagement`.

| Bảng | Chức năng |
|---|---|
| `Users` | Tài khoản Admin, Librarian, Customer |
| `Books` | Thông tin sách và số lượng |
| `BorrowRecords` | Mỗi dòng là một cuốn sách được mượn; lưu ngày mượn, hạn trả, ngày trả, trạng thái |
| `Payments` | Một giao dịch thanh toán cho một BorrowRecord |

Quan hệ:

- `Users (1) – (n) BorrowRecords`.
- `Books (1) – (n) BorrowRecords`.
- `BorrowRecords (1) – (0..1) Payments`.

**Quy ước quan trọng:** phiên bản 4 bảng không gom nhiều cuốn sách thành một phiếu mượn chung. Nếu khách mượn 3 cuốn trong cùng một lần đến thư viện thì tạo 3 dòng `BorrowRecords` (cùng `UserId`, khác `BookId`). Đây là lựa chọn đơn giản hóa cho nhóm hai người.

## 3. Công nghệ và package

- .NET 8 WPF (`net8.0-windows`).
- Entity Framework Core SQL Server 8.0.18.
- Microsoft.Extensions.Configuration.Json 9.0.7.

Các package đã khai báo trong `.csproj`; Visual Studio sẽ restore khi mở project.

## 4. Cách chạy trên máy Windows

### Bước 1 – Chuẩn bị công cụ

- Visual Studio 2022 với workload **.NET desktop development**.
- .NET 8 SDK.
- SQL Server và SQL Server Management Studio (SSMS).

### Bước 2 – Tạo database

1. Mở SSMS và kết nối đúng SQL Server instance.
2. Mở `Sql/PRN212_LibraryManagement_Final.sql`.
3. Chạy toàn bộ script.
4. Kiểm tra database `PRN212_LibraryManagement` có đủ 4 bảng.
5. Có thể chạy tiếp `Sql/Verify_Database.sql` để kiểm tra tên database, bảng, cột và số dòng.

Script tạo ba sách mẫu. Script sẽ dừng nếu database đã có bảng để tránh trộn schema cũ và schema mới. Nếu database cũ có dữ liệu cần giữ, hãy sao lưu trước; chỉ xóa/tạo lại database khi chắc chắn không cần dữ liệu cũ.

### Bước 3 – Tạo file cấu hình kết nối

File `appsettings.json` được `.gitignore` bỏ qua để tránh đẩy thông tin kết nối riêng lên GitHub. Khi clone repository, hãy sao chép `appsettings.example.json` thành `appsettings.json`, sau đó sửa tên SQL Server trên máy mình.

Ví dụ nếu dùng SQL Server Express và Windows Authentication:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=PRN212_LibraryManagement;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Nếu dùng SQL Server mặc định, có thể đổi `Server=.\\SQLEXPRESS` thành `Server=(local)` hoặc tên instance thực tế.

Nếu dùng SQL Server Authentication, dùng mẫu sau và thay tên server/mật khẩu của riêng máy:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TEN_SQL_SERVER;Database=PRN212_LibraryManagement;User Id=sa;Password=MAT_KHAU_CUA_BAN;TrustServerCertificate=True;"
  }
}
```

Không commit `appsettings.json` chứa mật khẩu thật lên GitHub. Trong Visual Studio, chọn file `appsettings.json` và đặt **Copy to Output Directory** thành `Copy if newer` hoặc `Copy always`.

### Bước 4 – Mở và chạy WPF

1. Mở `PRN212.LibraryManagement.sln` hoặc `.csproj` bằng Visual Studio 2022.
2. Chờ NuGet restore hoàn tất.
3. Kiểm tra `appsettings.json` đã trỏ đến đúng SQL Server/database.
4. Build project, sau đó chạy.
5. Tại màn hình đăng nhập, nhấn **KIỂM TRA KẾT NỐI DATABASE**. Chỉ đăng ký tài khoản sau khi ứng dụng báo kết nối thành công và tìm thấy đủ bốn bảng.
6. Tạo lần lượt tài khoản Admin, Librarian và Customer để demo.

## 5. Nếu đăng ký báo lỗi khi lưu tài khoản

Ứng dụng đã kiểm tra schema trước khi đăng ký và hiển thị thông báo lỗi SQL chi tiết. Đối chiếu theo thông báo:

- **Đang kết nối sai database:** kiểm tra `Database=PRN212_LibraryManagement` trong `appsettings.json`.
- **Thiếu bảng Users hoặc thiếu cột:** database đang là schema cũ/khác. Chạy `Sql/Verify_Database.sql` và đối chiếu. Không chạy lại script tạo bảng trên database có dữ liệu nếu chưa sao lưu.
- **Username/Email đã tồn tại:** đăng ký bằng thông tin khác.
- **Role không hợp lệ:** chỉ sử dụng đúng `Admin`, `Librarian`, `Customer`.
- **Login failed / Cannot open database:** kiểm tra tên SQL Server, phương thức xác thực và quyền truy cập.

## 6. Quy tắc dữ liệu khi tích hợp mượn – trả

Thành viên phụ trách mượn – trả cần cập nhật số lượng trong cùng transaction:

- Khi mượn thành công: kiểm tra `AvailableQuantity > 0`, thêm `BorrowRecords`, giảm `Books.AvailableQuantity`.
- Khi trả: chỉ cho trả bản ghi đang `Borrowed`; cập nhật `ReturnDate`, đổi `Status` thành `Returned`, tăng `Books.AvailableQuantity`.
- Khi thanh toán: thêm `Payments` với `BorrowId` hợp lệ. Mỗi BorrowRecord chỉ có tối đa một Payments do ràng buộc UNIQUE.

Nếu một tài khoản hoặc sách đã có lịch sử mượn, giao diện hiện tại chặn xóa để giữ khóa ngoại và lịch sử.

## 7. Phân chia công việc

**Thành viên 1 – source hiện tại:** Login/Register, phân quyền, Admin, Thủ thư, CRUD Books, quản lý tài khoản cơ bản.

**Thành viên 2 – nghiệp vụ mượn/trả:** tạo BorrowRecords, xác nhận trả, cập nhật số lượng, tạo Payments, hoàn thiện lịch sử và màn hình nghiệp vụ.

Hai người dùng chung Entities và `LibraryDbContext`. Không tự ý đổi tên bảng/cột hoặc kiểu dữ liệu nếu chưa thống nhất.

## 8. Kiểm tra kỹ thuật và giới hạn

- XAML và JSON được rà soát cú pháp.
- Các event handler trong XAML đã được đối chiếu với code-behind.
- Mapping EF Core được đối chiếu với SQL 4 bảng.
- Môi trường tạo gói không có .NET SDK/Visual Studio, vì vậy chưa thể chạy `dotnet build` hoặc kiểm thử WPF trực tiếp. Cần build lần đầu trên Windows; nếu có lỗi compiler/runtime, gửi nguyên thông báo để xử lý tiếp.
