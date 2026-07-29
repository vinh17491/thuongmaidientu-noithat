# Audit cải tiến lần 1

## Baseline

- Branch: `cai_tien_lan1`, commit khởi đầu `fee871c`.
- `dotnet restore`: thành công.
- `dotnet build --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test --no-build`: exit code 0 nhưng không có test được khám phá; repository chưa có test project.

## Kiến trúc hiện tại

Ứng dụng ASP.NET Core MVC .NET 8, Razor Views, EF Core SQL Server, cookie authentication và BCrypt. `ThuongMaiDienTuDbContext` ánh xạ trực tiếp schema do `thuongmaidientu.sql` quản lý. Các actor hiện hữu trong schema là ADMIN, SELLER, CUSTOMER và CARRIER, nhưng chưa có nghiệp vụ carrier.

Controllers chính: Account (đăng nhập/đăng ký), Products (catalog), Cart, Checkout, Orders/Reviews của customer; ProductAdmin, ProductCategories, OrderAdmin, ReviewAdmin và Dashboard cho back office.

## Phân quyền và ownership

### Critical

- `ProductAdminController.Index`, Details, Edit và ToggleStatus không scope theo `Store.OwnerUserId`; seller có thể đọc/sửa seller khác.
- Create product chọn store ACTIVE có `StoreId` nhỏ nhất thay vì store thuộc seller.
- `ProductCategoriesController` cho cả ADMIN và SELLER CRUD danh mục toàn sàn.
- `OrderAdminController` cho seller đọc và cập nhật mọi parent order.
- `ReviewAdminController` cho seller đọc và moderation mọi review.
- `DashboardController` hiển thị toàn bộ số liệu, dùng cả view tổng hợp không có StoreId.
- Checkout đặt `ShippingFee = 0`, không có quote/provider/shipment.
- Chưa có `store_orders`; parent order chưa tách ownership theo store.

### High

- Cart, checkout, catalog và dashboard giới hạn SKU qua `Min(SkuId)`.
- EF map `ProductImage` bằng `WithOne`; Product chỉ có một `ProductImage`, dù bảng cho phép nhiều ảnh không-primary.
- Chưa có shipping provider/service/rate/quote/shipment/tracking.
- Chưa có order status history, shipment history, price history và audit log.
- Chưa có automated test project.

### Medium/Low

- Home vẫn là template ASP.NET mặc định; branding là một cửa hàng, chưa phải marketplace.
- Chưa có store page, pagination đầy đủ, canonical, sitemap và robots.
- Development initializer sửa placeholder password thành BCrypt khi chạy Development; seed SQL vẫn cần được kiểm tra và chuẩn hóa.
- Các controller lặp lại việc parse claim thay vì dùng một current-user service.

## Entity/table mapping hiện tại

`users` 1-N `stores`; `stores` 1-N `products`; category 1-N product; product 1-N SKU; product-image đang bị EF cấu hình 1-1; cart 1-N item; order 1-N item; review gắn user/product/order item. Không có StoreOrder hay các entity shipping.

## File bị ảnh hưởng dự kiến

- Controllers, Services, Models, ViewModels, Razor Views, `Program.cs`, DbContext.
- Duy nhất `thuongmaidientu.sql` cho mọi thay đổi database.
- Test project .NET (không tạo SQL/migration).

## Gate audit

Các lỗi được xác nhận bằng đọc source và tìm kiếm repository. Không có EF migration. Các tệp `app-test.*.log`, `.vs`, `bin`, `obj` hiện có trên đĩa nhưng phải giữ ngoài commit.
