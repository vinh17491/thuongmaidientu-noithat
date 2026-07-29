# Architecture

ASP.NET Core MVC .NET 8 dùng Controllers, Services, EF Core DbContext, Models, ViewModels và Razor Views. Luồng: Browser → Controller → Service/DbContext → SQL Server → ViewModel → Razor.

ADMIN quản trị toàn sàn; SELLER chỉ Store của mình; CUSTOMER chỉ Order của mình; CARRIER chỉ Provider/Shipment của mình. Enforcement dùng cookie claims, Authorize, ownership services, query scoping và antiforgery.

Checkout tách Parent Order thành StoreOrder. Shipment thuộc StoreOrder và Carrier. AuditLog lưu thay đổi workflow. SEO dùng slug canonical, JSON-LD, sitemap/robots và SeoUrlService; không dùng Host header. SQL duy nhất là `thuongmaidientu.sql`, không dùng EF Migration.

Chatbox dùng `IChatAssistantService`/`LocalChatAssistantService`, controller POST `/tro-ly/hoi`, projection read-only và claim-scoped order lookup; không thay đổi cart, order, shipment, role hoặc trạng thái nghiệp vụ.
