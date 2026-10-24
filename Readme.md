# Phần Mềm Quản Lý Thư Viện

Phần mềm Quản lý Thư viện (Library Management System) là một ứng dụng WPF (Windows Presentation Foundation) được phát triển bằng Visual Studio, sử dụng Microsoft SQL Server 2019 làm cơ sở dữ liệu.

## 1. Tổng Quan Hệ Thống

Hệ thống được thiết kế để phục vụ ba đối tượng người dùng chính:
*   **Admin (Quản trị viên):** Có toàn quyền kiểm soát hệ thống, quản lý người dùng, thiết lập quy định và theo dõi toàn bộ hoạt động.
*   **Thủ thư:** Chịu trách nhiệm quản lý kho sách, thực hiện các nghiệp vụ mượn/trả sách, và hỗ trợ khách hàng.
*   **Khách hàng (Người mượn):** Tìm kiếm sách, thực hiện quy trình mượn sách, thanh toán phí (nếu có), và theo dõi lịch sử mượn/trả của bản thân.

## 2. Luồng Nghiệp Vụ Chính

### 2.1. Quản Lý Sách (CRUD Sách) - Dành cho Admin/Thủ Thư

Luồng này cho phép thủ thư và quản trị viên duy trì danh mục sách cập nhật.

1.  **Thêm sách mới (Create):** Nhập thông tin chi tiết về sách (Tiêu đề, Tác giả, Thể loại, ISBN, Số lượng, Vị trí kệ, v.v.) vào cơ sở dữ liệu.
2.  **Xem danh sách (Read):** Hiển thị danh sách toàn bộ sách trong thư viện, với khả năng tìm kiếm, lọc và phân loại theo nhiều tiêu chí khác nhau.
3.  **Cập nhật thông tin (Update):** Chỉnh sửa thông tin của một cuốn sách hiện có (ví dụ: cập nhật số lượng khi có sách mới nhập về hoặc sách bị hỏng).
4.  **Xóa sách (Delete):** Xóa thông tin sách khỏi hệ thống (thường áp dụng cho sách đã bị thanh lý hoặc mất hoàn toàn).

### 2.2. Luồng Mượn Sách - Dành cho Khách hàng/Thủ Thư

Đây là luồng nghiệp vụ cốt lõi, mô tả quá trình từ lúc khách hàng chọn sách đến khi hoàn tất việc mượn.

1.  **Tìm kiếm và chọn sách:** Khách hàng (hoặc thủ thư hỗ trợ khách) tra cứu sách trên hệ thống.
2.  **Yêu cầu mượn:** Khách hàng chọn sách muốn mượn và tạo yêu cầu mượn. Hệ thống kiểm tra tình trạng sách (còn trống hay không) và trạng thái tài khoản của khách (có đang bị phạt hoặc mượn quá số lượng cho phép không).
3.  **Xác nhận và Thanh toán (nếu có):**
    *   Hệ thống tính toán phí mượn hoặc phí cọc (nếu áp dụng theo quy định của thư viện).
    *   Khách hàng thực hiện thanh toán (tiền mặt, chuyển khoản, v.v.).
    *   Thủ thư xác nhận thanh toán trên hệ thống.
4.  **Ghi nhận Lịch sử mượn:** Sau khi thanh toán/xác nhận thành công, hệ thống sẽ:
    *   Cập nhật trạng thái sách thành "Đang mượn".
    *   Tạo một bản ghi mới trong bảng "Lịch sử mượn" của khách hàng, ghi rõ ngày mượn, hạn trả dự kiến.

### 2.3. Luồng Trả Sách - Dành cho Khách hàng/Thủ Thư

1.  **Tiếp nhận sách trả:** Khách hàng mang sách đến trả. Thủ thư tìm kiếm thông tin phiếu mượn tương ứng trên hệ thống.
2.  **Kiểm tra tình trạng sách:** Thủ thư kiểm tra xem sách có bị hư hỏng hay trả quá hạn không.
3.  **Xử lý vi phạm (nếu có):** Nếu trả trễ hoặc sách hư hỏng, hệ thống sẽ tính phí phạt dựa trên quy định. Khách hàng tiến hành thanh toán phí phạt.
4.  **Hoàn tất trả sách:**
    *   Hệ thống cập nhật trạng thái sách thành "Sẵn sàng".
    *   Cập nhật ngày trả thực tế vào bản ghi tương ứng trong "Lịch sử mượn".

## 3. Cấu Trúc Cơ Sở Dữ Liệu Đề Xuất (SQL Server 2019)

Để đáp ứng các luồng trên, cơ sở dữ liệu cần tối thiểu các bảng sau:

*   **Users:** Quản lý thông tin tài khoản (Admin, Thủ thư, Khách hàng), bao gồm RoleID để phân quyền.
*   **Books:** Lưu trữ thông tin chi tiết về sách (CRUD sách).
*   **BorrowRecords:** Bảng trung tâm để ghi nhận lịch sử mượn/trả. Bảng này sẽ liên kết với `Users` (ai mượn) và `Books` (mượn sách nào), chứa thông tin ngày mượn, hạn trả, ngày trả thực tế, trạng thái.
*   **Payments:** Ghi nhận các giao dịch thanh toán (phí mượn, cọc, phí phạt), liên kết với `BorrowRecords`.