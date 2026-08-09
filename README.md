# Nội Thất Hub — ThuongMaiDienTu

Dự án ASP.NET Core MVC .NET 8, EF Core và SQL Server.

## Cấu trúc

```text
ThuongMaiDienTu-Clean/
├── ThuongMaiDienTu.slnx
├── run.ps1
├── src/ThuongMaiDienTu/             # Project web duy nhất
├── tests/ThuongMaiDienTu.Tests/     # Project test
├── database/thuongmaidientu.sql     # SQL duy nhất
├── docs/
└── scripts/clean-before-share.ps1
```

`src/ThuongMaiDienTu` là project web. Thư mục gốc là solution/repository, không phải một project web thứ hai.

## Chuẩn bị database

1. Mở SQL Server Management Studio.
2. Mở `database/thuongmaidientu.sql`.
3. Chọn đúng SQL Server mà connection string đang trỏ tới.
4. Chạy **toàn bộ file từ đầu đến cuối**, không chỉ chạy phần đầu.
5. Kiểm tra:

```sql
USE thuongmaidientu;
SELECT OBJECT_ID(N'dbo.shipping_providers', N'U') AS shipping_providers_object_id;
```

Kết quả phải khác `NULL`. Nếu bằng `NULL`, database đang dùng schema cũ hoặc file SQL chưa được chạy hết.

Connection string mặc định nằm tại:

```text
src/ThuongMaiDienTu/appsettings.json
```

## Chạy nhanh

Trong PowerShell tại thư mục gốc:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\run.ps1
```

Hoặc chạy thủ công:

```powershell
dotnet restore .\ThuongMaiDienTu.slnx
dotnet build .\ThuongMaiDienTu.slnx
dotnet run --project .\src\ThuongMaiDienTu\ThuongMaiDienTu.csproj --launch-profile http
```

Mở `http://localhost:5205`.

## Tài khoản demo

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@homegoods.vn` | `Demo@123` |
| Seller | `seller@gdviet.vn` | `Demo@123` |
| Customer | `customer@example.com` | `Demo@123` |

## Lỗi `Invalid object name 'shipping_providers'`

Đây là lỗi database không đồng bộ với source code. Ứng dụng đã khởi động, nhưng database cũ chưa có module vận chuyển.

Cách sửa đúng:

1. Dừng ứng dụng bằng `Ctrl+C`.
2. Backup database đang dùng nếu có dữ liệu quan trọng.
3. Chạy lại **toàn bộ** `database/thuongmaidientu.sql` trên database `thuongmaidientu`.
4. Chạy câu kiểm tra `OBJECT_ID` ở trên.
5. Chạy lại `run.ps1`.

Không sửa bằng cách xóa truy vấn `ShippingProviders` khỏi code vì các chức năng Carrier, shipping service, quote và shipment đều cần schema này.

## Trước khi gửi ZIP

```powershell
.\scripts\clean-before-share.ps1
```

Không gửi `.git`, `.vs`, `bin`, `obj`, log, `.bak` hoặc file `.csproj.user`.
