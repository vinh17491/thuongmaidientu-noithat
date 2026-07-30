# Nội Thất Hub

Nội Thất Hub là sàn thương mại điện tử nội thất đa gian hàng: Seller sở hữu Store riêng, Customer mua từ nhiều Store, Admin quản trị toàn sàn và Carrier quản lý vận chuyển.

## Công nghệ

ASP.NET Core MVC .NET 8, EF Core 8.0.29, SQL Server, Razor, Bootstrap, BCrypt.Net-Next và xUnit.

## Chạy

```text
dotnet restore
dotnet build
dotnet run --project ThuongMaiDienTu/ThuongMaiDienTu.csproj
```

Database duy nhất là `thuongmaidientu`; SQL duy nhất là `thuongmaidientu.sql`. Cấu hình bí mật qua User Secrets/environment variables, không ghi password thật.

Chatbox là trợ lý mua sắm local tại POST `/tro-ly/hoi`; không dùng external AI API. Hỗ trợ greeting, tách intent/search term (có dấu hoặc không dấu), tối đa 5 kết quả public và order lookup theo claim server-side. Endpoint dùng anti-forgery và ASP.NET Core RateLimiter policy riêng 15 request/phút/IP.

Seed idempotent duy trì 10 sản phẩm gia dụng/nội thất public có SKU, giá/tồn kho và ảnh SVG nội bộ. Dữ liệu cầu lông legacy được giữ lịch sử nhưng Product/Store/Category bị ẩn bằng trạng thái.

## Tests

```text
dotnet test
dotnet test --filter "FullyQualifiedName~SqlServerMarketplaceIntegrationTests"
```

SQL integration tests yêu cầu SQL Server và database `thuongmaidientu`; không tự tạo database khác hoặc drop dữ liệu. Local fallback `Encrypt=False;TrustServerCertificate=True` chỉ dành cho Development.

## Public routes

`/`, `/san-pham`, `/san-pham/{productSlug}`, `/danh-muc/{categorySlug}`, `/cua-hang/{storeSlug}`, `/sitemap.xml`, `/robots.txt`.

## Tài liệu

- [Architecture](docs/ARCHITECTURE.md)
- [Database](docs/DATABASE.md)
- [Roles and permissions](docs/ROLES_AND_PERMISSIONS.md)
- [Order state machine](docs/ORDER_STATE_MACHINE.md)
- [Shipping state machine](docs/SHIPPING_STATE_MACHINE.md)
- [Test cases](docs/TEST_CASES.md)
- [Test report](docs/TEST_REPORT.md)
- [Demo flow](docs/DEMO_FLOW.md)
- [Known limitations](docs/KNOWN_LIMITATIONS.md)
