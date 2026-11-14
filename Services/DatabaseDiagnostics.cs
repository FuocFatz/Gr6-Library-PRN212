using Microsoft.EntityFrameworkCore;
using PRN212.LibraryManagement.Data;
using System.Data;

namespace PRN212.LibraryManagement.Services;

/// <summary>
/// Kiểm tra kết nối và cấu trúc database trước khi thực hiện đăng nhập/đăng ký.
/// Giúp phát hiện trường hợp ứng dụng đang kết nối nhầm database hoặc dùng schema cũ.
/// </summary>
public static class DatabaseDiagnostics
{
    private static readonly Dictionary<string, string[]> RequiredColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Users"] = ["UserId", "Username", "PasswordHash", "FullName", "Email", "Role"],
        ["Books"] = ["BookId", "BookTitle", "BookCategory", "TotalQuantity", "AvailableQuantity"],
        ["BorrowRecords"] = ["BorrowId", "UserId", "BookId", "BorrowDate", "DueDate", "ReturnDate", "Status"],
        ["Payments"] = ["PaymentId", "BorrowId", "Amount", "PaymentWay", "PaymentDate", "PaymentStatus"]
    };

    /// <summary>
    /// Trả về null nếu kết nối được và đủ bảng/cột; ngược lại trả về thông báo dễ hiểu.
    /// </summary>
    public static async Task<string?> ValidateSchemaAsync(LibraryDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        bool shouldClose = connection.State != ConnectionState.Open;

        try
        {
            if (shouldClose)
                await db.Database.OpenConnectionAsync();

            if (!string.Equals(connection.Database, "PRN212_LibraryManagement", StringComparison.OrdinalIgnoreCase))
            {
                return $"Ứng dụng đang kết nối đến database '{connection.Database}', không phải 'PRN212_LibraryManagement'. " +
                       "Hãy kiểm tra Initial Catalog/Database trong appsettings.json.";
            }

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TABLE_NAME, COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = N'dbo'
                  AND TABLE_NAME IN (N'Users', N'Books', N'BorrowRecords', N'Payments');
                """;

            var actual = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string table = reader.GetString(0);
                string column = reader.GetString(1);
                if (!actual.TryGetValue(table, out var columns))
                {
                    columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    actual[table] = columns;
                }
                columns.Add(column);
            }

            var problems = new List<string>();
            foreach (var requiredTable in RequiredColumns)
            {
                if (!actual.TryGetValue(requiredTable.Key, out var actualColumns))
                {
                    problems.Add($"- Thiếu bảng dbo.{requiredTable.Key}.");
                    continue;
                }

                string[] missing = requiredTable.Value
                    .Where(column => !actualColumns.Contains(column))
                    .ToArray();

                if (missing.Length > 0)
                    problems.Add($"- Bảng dbo.{requiredTable.Key} thiếu cột: {string.Join(", ", missing)}.");
            }

            if (problems.Count == 0)
                return null;

            return "Ứng dụng đã kết nối được SQL Server nhưng database hiện tại không đúng cấu trúc 4 bảng mà project yêu cầu.\n" +
                   "Database cần có tên PRN212_LibraryManagement và các cột sau:\n" +
                   string.Join("\n", problems) +
                   "\n\nHãy mở file Sql/PRN212_LibraryManagement_Final.sql và chạy trên đúng SQL Server/database. " +
                   "Nếu database cũ có dữ liệu cần giữ, hãy sao lưu trước khi tạo database mới.";
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await db.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// Lấy thông báo sâu nhất của lỗi để không che mất nguyên nhân SQL Server trả về.
    /// </summary>
    public static string GetDetailedMessage(Exception exception)
    {
        Exception root = exception.GetBaseException();
        string detail = root.Message;
        string lower = detail.ToLowerInvariant();

        if (lower.Contains("cannot insert duplicate key") || lower.Contains("duplicate key") ||
            lower.Contains("unique index") || lower.Contains("unique constraint"))
            return "Username hoặc Email đã tồn tại trong database. Hãy dùng thông tin khác.\n\n" + detail;

        if (lower.Contains("invalid object name"))
            return "Không tìm thấy bảng được yêu cầu. Có thể bạn đang kết nối nhầm database hoặc chưa chạy file SQL bản 4 bảng.\n\n" + detail;

        if (lower.Contains("invalid column name"))
            return "Tên cột trong database không khớp với source code. Có thể database đang dùng phiên bản SQL cũ.\n\n" + detail;

        if (lower.Contains("string or binary data would be truncated"))
            return "Một giá trị nhập vào dài hơn giới hạn cột SQL Server. Hãy kiểm tra độ dài Username, Email hoặc Họ tên.\n\n" + detail;

        if (lower.Contains("cannot insert the value null"))
            return "Ứng dụng đang gửi thiếu dữ liệu cho một cột bắt buộc (NOT NULL). Hãy kiểm tra các ô nhập và Entity User.\n\n" + detail;

        if (lower.Contains("check constraint") || lower.Contains("ck_users_role"))
            return "Dữ liệu không đáp ứng điều kiện CHECK trong SQL Server. Hãy kiểm tra Role (Admin, Librarian, Customer) hoặc trạng thái được gửi lên.\n\n" + detail;

        if (lower.Contains("cannot open database") || lower.Contains("login failed") ||
            lower.Contains("server was not found") || lower.Contains("network-related or instance-specific"))
            return "Không thể truy cập SQL Server/database. Hãy kiểm tra Server, tên database và chế độ xác thực trong appsettings.json.\n\n" + detail;

        // Giữ lại cả thông tin loại lỗi lẫn nguyên nhân sâu nhất để tiện gửi khi cần hỗ trợ.
        return $"{exception.GetType().Name}: {detail}";
    }
}
