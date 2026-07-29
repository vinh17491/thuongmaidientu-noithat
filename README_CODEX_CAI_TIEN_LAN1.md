# README CODEX — CẢI TIẾN LẦN 1

## 1. State

Repository mục tiêu:

```text
https://github.com/vinh17491/thuongmaidientu-noithat
```

Mục tiêu là nâng cấp dự án hiện tại thành **sàn thương mại điện tử đồ gia dụng/nội thất đa cửa hàng**, có bốn actor:

- `ADMIN`
- `SELLER`
- `CUSTOMER`
- `CARRIER`

Dự án hiện đã có nền tảng ASP.NET Core MVC, EF Core, SQL Server, đăng nhập, catalog, giỏ hàng, checkout, đơn hàng, review và dashboard cơ bản. Tuy nhiên, hệ thống vẫn gần với một cửa hàng đơn lẻ và còn các lỗi nghiêm trọng về phân quyền seller, order đa cửa hàng, shipping/carrier, SKU, ảnh sản phẩm, giao diện và test.

Sau khi hoàn thành, toàn bộ thay đổi phải được commit và push lên branch:

```text
cai_tien_lan1
```

Không push trực tiếp lên `main`.

---

## 2. Tailor — Stack và ràng buộc

### 2.1 Giữ nguyên stack

- ASP.NET Core MVC.
- .NET 8.
- Entity Framework Core.
- SQL Server.
- Razor Views.
- Bootstrap/CSS hiện tại.
- Cookie Authentication.
- BCrypt.

Không chuyển sang React, Vue, Angular hoặc framework khác.

### 2.2 Phạm vi code

- Chỉ sửa trong repository `thuongmaidientu-noithat`.
- Không tạo solution mới ngoài repository.
- Không rewrite toàn bộ nếu có thể refactor an toàn.
- Không xóa chức năng đang chạy nếu có thể migrate.
- Không hardcode `UserId`, `StoreId`, `CarrierId`, giá, phí giao hàng hoặc trạng thái.

### 2.3 Ràng buộc database

Chỉ được dùng database:

```text
thuongmaidientu
```

Chỉ được quản lý schema trong file SQL hiện hữu:

```text
thuongmaidientu.sql
```

Nghiêm cấm:

- Tạo thêm file `.sql`.
- Tạo `schema.sql`, `upgrade.sql`, `seed.sql`, `migration.sql` hoặc bản `v2/final/fixed`.
- Tạo database tên khác.
- Tạo EF Migration mới.
- Chạy `dotnet ef migrations add`.
- Chia database thành nhiều script.

Mọi DDL, DML, seed, index, constraint, view, procedure và verification query phải đặt trong `thuongmaidientu.sql`.

### 2.4 Git safety

- Không commit lên `main`.
- Không force push.
- Không reset hoặc rebase phá lịch sử.
- Không xóa branch đã tồn tại.
- Không commit `.vs`, `bin`, `obj`, log, `.env`, secret, password hoặc API key.
- Nếu branch `cai_tien_lan1` đã tồn tại local hoặc remote, dừng và báo rõ; không tự ghi đè.

---

## 3. Evaluate — Audit trước khi sửa

Trước khi code, phải đọc toàn repository và xác nhận lại các lỗi sau:

1. `ProductAdminController` cho Seller xem toàn bộ sản phẩm.
2. Tạo sản phẩm đang lấy store active đầu tiên thay vì store của seller.
3. Seller có thể sửa/ẩn sản phẩm của seller khác.
4. Seller có thể CRUD danh mục toàn sàn.
5. Seller có thể xem và đổi trạng thái toàn bộ đơn hàng.
6. Seller có thể xem/ẩn review của cửa hàng khác.
7. Seller dashboard đang hiển thị số liệu toàn sàn.
8. Chưa có module `CARRIER` thực tế.
9. `ShippingFee` đang hardcode bằng `0`.
10. Chưa có `store_orders` để tách đơn theo cửa hàng.
11. Catalog/cart/checkout chỉ chấp nhận SKU có ID nhỏ nhất.
12. Product–ProductImage đang map gần như one-to-one.
13. Trang Home vẫn là template mặc định ASP.NET.
14. Branding đang giống một cửa hàng “Gia Dụng Việt”, chưa phải marketplace.
15. Chưa có store page.
16. Chưa có pagination đầy đủ.
17. Chưa có order history, shipment history, price history và audit log.
18. Customer demo có thể đang dùng password placeholder không phải BCrypt.
19. Chưa có automated test project.
20. Chưa có README bàn giao, test report và CI.
21. Chưa có chatbot tư vấn.
22. SEO mới có title/description, chưa có slug route, canonical, sitemap và robots.

Tạo hai file:

```text
docs/AUDIT_CAI_TIEN_LAN1.md
docs/PLAN_CAI_TIEN_LAN1.md
```

Audit phải ghi:

- Kiến trúc hiện tại.
- Controllers/actions/routes.
- Roles và quyền hiện tại.
- Entity/table mapping.
- Bug theo mức Critical/High/Medium/Low.
- File bị ảnh hưởng.
- Kế hoạch sửa theo phase.
- Baseline build/test.

Không sửa schema lớn trước khi audit xong.

---

## 4. Plan — Quy trình Git ban đầu

Chạy:

```bash
git status
git branch --show-current
git remote -v
git fetch origin
```

Nếu working tree không sạch:

- Không reset.
- Không xóa thay đổi.
- Báo file đang thay đổi và dừng.

Đồng bộ `main`:

```bash
git checkout main
git pull --ff-only origin main
```

Kiểm tra branch:

```bash
git branch --list cai_tien_lan1
git ls-remote --heads origin cai_tien_lan1
```

Nếu chưa tồn tại:

```bash
git checkout -b cai_tien_lan1
```

Sau đó chạy baseline:

```bash
dotnet restore
dotnet build
dotnet test
```

Nếu chưa có test project, ghi đúng là chưa có test; không báo giả là test pass.

---

# PHASE 1 — Sửa phân quyền và data ownership

Đây là phase ưu tiên cao nhất.

## 1.1 Current user và ownership services

Tạo các service phù hợp:

```text
CurrentUserService
StoreOwnershipService
CarrierOwnershipService
```

Cần lấy được:

- Current UserId.
- Current Role.
- StoreId thuộc seller.
- ProviderId thuộc carrier.

Không parse claim lặp lại ở nhiều controller nếu có thể gom lại.

## 1.2 Product ownership

Sửa `ProductAdminController`:

- Admin thấy toàn bộ sản phẩm.
- Seller chỉ thấy product có `Store.OwnerUserId == currentUserId`.
- Seller Create phải tự gắn đúng store của seller.
- Admin Create được chọn store.
- Edit/Details/ToggleStatus/Delete đều kiểm tra ownership ở server.
- Seller A gửi ProductId của Seller B phải nhận 404 hoặc 403 nhất quán.
- Xóa logic lấy store active đầu tiên.

## 1.3 Category authorization

Sửa `ProductCategoriesController`:

- Chỉ `ADMIN` được CRUD.
- Seller chỉ đọc category active để chọn khi tạo product.

## 1.4 Order authorization

Trong kiến trúc cũ:

- Seller chỉ được xem order chứa sản phẩm thuộc store mình.
- Seller không được update parent order đa store.
- Chỉ Admin được cập nhật toàn order trước khi có `store_orders`.

Sau Phase 2, Seller chỉ quản lý `store_orders` của store mình.

## 1.5 Review authorization

- Admin moderation toàn sàn.
- Seller chỉ xem review của product thuộc store mình.
- Seller không được toggle visible/hidden.
- Seller chỉ được phản hồi review nếu triển khai.

## 1.6 Dashboard scope

- Admin dashboard toàn sàn.
- Seller dashboard chỉ theo store.
- Carrier dashboard chỉ theo provider.
- Không dùng view thiếu `StoreId` để hiển thị số liệu cho Seller.

## Acceptance tests

- Seller A không xem Product B.
- Seller A không edit Product B.
- Seller A không toggle Product B.
- Seller A không xem doanh thu Seller B.
- Seller A không moderation Review B.
- Seller không CRUD Category.
- Admin vẫn quản lý toàn sàn.

Không chuyển phase nếu các test quyền chưa pass.

---

# PHASE 2 — Nâng cấp schema marketplace

Chỉ sửa `thuongmaidientu.sql`.

## 2.1 Bổ sung bảng

### `store_orders`

```text
store_order_id
order_id
store_id
store_order_code
subtotal
discount_amount
shipping_fee
total_amount
status
created_at
updated_at
row_version
```

### `order_items`

Bổ sung:

```text
store_order_id
store_name_snapshot
```

Giữ snapshot:

- Product name.
- SKU code.
- Unit price.
- Store name.

### `order_status_histories`

```text
history_id
store_order_id
old_status
new_status
changed_by_user_id
note
created_at
```

### `shipping_providers`

```text
provider_id
owner_user_id
provider_name
slug
phone
email
description
status
created_at
updated_at
```

### `shipping_services`

```text
service_id
provider_id
service_code
service_name
base_fee
estimated_min_days
estimated_max_days
max_weight
status
```

### `shipping_rate_rules`

```text
rule_id
service_id
origin_area
destination_area
min_weight
max_weight
fee
extra_fee_per_kg
supports_cod
status
```

### `shipping_quotes`

```text
quote_id
provider_id
service_id
fee
estimated_min_days
estimated_max_days
expires_at
status
selected_at
```

### `shipments`

```text
shipment_id
store_order_id
provider_id
service_id
shipping_fee
tracking_code
status
pickup_address
delivery_address
estimated_delivery_at
picked_up_at
delivered_at
created_at
updated_at
row_version
```

### `shipment_status_histories`

```text
history_id
shipment_id
old_status
new_status
changed_by_user_id
note
created_at
```

### `product_price_histories`

```text
history_id
sku_id
old_price
new_price
old_sale_price
new_sale_price
changed_by_user_id
reason
created_at
```

### `audit_logs`

```text
audit_id
actor_user_id
action
entity_name
entity_id
before_json
after_json
ip_address
created_at
```

## 2.2 Safe upgrade

- Không drop bảng nghiệp vụ.
- Dùng `IF OBJECT_ID`.
- Dùng `COL_LENGTH`.
- Thêm nullable trước.
- Backfill dữ liệu.
- Sau đó mới thêm `NOT NULL`, FK, check và index.
- Dùng transaction, `TRY...CATCH`, `XACT_STATE()` và `THROW`.
- Seed bằng `IF NOT EXISTS`.

## 2.3 Backfill order cũ

- Order chỉ có một store: tạo một `store_order`.
- Order có nhiều store: tạo một `store_order` cho từng store và gắn item đúng store.
- Tính subtotal từ item.
- Ghi rõ cách phân bổ shipping fee cũ.
- Verification phải bảo đảm mọi order item có `store_order_id`.

## Gate

- SQL chạy trên database mới.
- SQL chạy trên bản sao database cũ.
- Chạy lần hai không duplicate.
- Không có file SQL mới.
- EF model khớp schema.

---

# PHASE 3 — Multi-store cart, checkout và shipping

## 3.1 Cart

- Cho phép sản phẩm nhiều store.
- Group UI theo StoreId.
- Hiển thị subtotal từng store.
- Hiển thị tổng toàn giỏ.
- Xóa toàn bộ logic `MIN(SkuId)`.

## 3.2 Shipping quote

Mỗi store group phải:

1. Lấy pickup address của store.
2. Lấy delivery address của customer.
3. Lấy shipping service phù hợp.
4. Tính phí theo rate rule.
5. Cho customer chọn service.
6. Validate lại quote khi place order.
7. Chặn quote hết hạn.

Nếu chưa tích hợp API ngoài, dùng shipping provider/rate rule nội bộ. Không hardcode phí bằng 0.

## 3.3 Place order

Trong một transaction:

1. Đọc lại cart.
2. Validate store/category/product/SKU.
3. Validate giá và stock.
4. Validate shipping quote.
5. Group item theo StoreId.
6. Tạo một parent `Order`.
7. Tạo N `StoreOrder`.
8. Tạo `OrderItem` gắn đúng StoreOrder.
9. Tạo N Shipment.
10. Trừ tồn kho.
11. Xóa cart item đã checkout.
12. Commit.

Nếu một nhóm lỗi phải rollback toàn bộ.

## Tổng tiền

```text
StoreOrder.Total = Subtotal - Discount + ShippingFee
Order.Total = SUM(StoreOrder.Total)
```

Không tin total từ client.

## Gate

- Giỏ có sản phẩm của hai store tạo một parent order và hai store order.
- Tạo shipment riêng cho từng store.
- Mỗi seller chỉ thấy phần của mình.
- Shipping fee không hardcode 0.
- Quote hết hạn bị chặn.
- Stock giảm đúng.
- Lỗi giữa chừng rollback.

---

# PHASE 4 — Order và shipment workflow

## StoreOrder state machine

```text
PENDING -> CONFIRMED hoặc CANCELLED
CONFIRMED -> PROCESSING hoặc CANCELLED nếu chưa pickup
PROCESSING -> SHIPPING
SHIPPING -> DELIVERED
```

Không cho chuyển ngược tùy tiện.

## Parent order aggregation

Tính trạng thái parent order từ store orders, không cho Seller đổi trực tiếp parent order.

## Cancel

- Customer/Seller chỉ hủy theo rule.
- Hoàn kho đúng một lần.
- Không hủy sau khi carrier pickup.
- Ghi `order_status_histories`.

## Shipment state machine

```text
CREATED
READY_FOR_PICKUP
PICKED_UP
IN_TRANSIT
DELIVERED
FAILED
CANCELLED
```

- Carrier chỉ cập nhật shipment của provider mình.
- Tracking code unique.
- Customer được theo dõi lịch sử.

---

# PHASE 5 — SKU, ảnh và giá

## Multiple SKU

Xóa logic dùng SKU đầu tiên hoặc `MIN(SkuId)` để đại diện duy nhất.

Product details phải:

- Load tất cả SKU active.
- Cho chọn biến thể.
- Hiển thị giá/tồn kho theo SKU.
- Gửi đúng `SkuId` vào cart.

## Multiple images

Sửa EF mapping:

```text
Product 1 — N ProductImages
```

`Product` phải có collection `ProductImages`.

UI cần:

- Ảnh chính.
- Thumbnail gallery.
- Sort order.
- Alt text.

Nếu upload ảnh:

- Validate MIME, extension và kích thước.
- Chống path traversal.
- Tạo tên file an toàn.
- Lưu dưới `wwwroot/uploads/products`.

## Price history

Khi đổi price/sale phải ghi `product_price_histories`.

Dùng một `ProductPricingService` chung cho catalog, cart, checkout và order snapshot.

---

# PHASE 6 — Store, Seller và Carrier portal

## Store page

Route:

```text
/cua-hang/{storeSlug}
```

Hiển thị:

- Logo/banner.
- Store name.
- Mô tả.
- Rating.
- Catalog riêng.
- Filter/sort/pagination.

## Seller onboarding

- Seller tạo store.
- Store mới `PENDING`.
- Admin duyệt.
- Store chưa duyệt không được bán.

## Carrier onboarding

- Carrier tạo provider.
- Provider mới `PENDING`.
- Admin duyệt.
- Sau khi duyệt mới quản lý service/rate.

## Dashboard

Admin: dữ liệu toàn sàn.

Seller: chỉ store của mình.

Carrier: chỉ provider của mình.

---

# PHASE 7 — UI/UX và SEO

## Branding

Đổi tên toàn sàn sang một tên trung lập. “Gia Dụng Việt” chỉ được dùng như tên một seller/store, không phải tên marketplace.

## Home page

Xóa nội dung template:

```text
Welcome
Learn about building Web apps with ASP.NET Core
```

Trang chủ cần:

- Logo sàn.
- Search bar.
- Category navigation.
- Hero banner.
- Sản phẩm mới.
- Sản phẩm khuyến mãi.
- Store nổi bật.
- Carrier nổi bật.
- Cam kết của sàn.
- Footer đầy đủ.
- Responsive.

## Catalog

Bổ sung:

- Search.
- Category.
- Store.
- Brand.
- Price range.
- Rating.
- In-stock.
- Sort newest/price/rating.
- Pagination server-side.

Không `ToListAsync()` trước filter/pagination.

## SEO

- Product slug route.
- Store slug route.
- Category slug route.
- Canonical.
- Open Graph.
- JSON-LD Product/Breadcrumb.
- `robots.txt`.
- `sitemap.xml`.
- `noindex` cho admin/account/cart/checkout.

---

# PHASE 8 — Security hardening

- Sửa placeholder password demo.
- Seed BCrypt hợp lệ cho Admin, Seller, Customer và Carrier ở Development.
- Không reset password account thật.
- Login rate limiting.
- Cookie `SecurePolicy.Always` ở Production.
- Anti-forgery cho mọi state-changing POST.
- Không dùng GET để update/delete/toggle.
- Không mass assignment.
- Validate price, sale, stock, quantity, rating, slug và upload.
- Audit các thao tác quan trọng.
- Dùng UTC/`DateTimeOffset` hoặc `TimeProvider` cho code mới.

---

# PHASE 9 — Tests

Tạo xUnit test project.

## Unit tests

- Pricing.
- Promotion window.
- StoreOrder state machine.
- Parent order aggregation.
- Shipment state machine.
- Shipping quote.
- Cart total.

## Integration tests

- Customer ownership.
- Seller ownership.
- Carrier ownership.
- Multi-store checkout.
- Stock decrement.
- Rollback transaction.
- Cancel hoàn kho đúng một lần.
- Review chỉ sau delivered.
- Duplicate review blocked.
- Expired quote blocked.

Không dùng EF InMemory cho test transaction/constraint quan trọng.

---

# PHASE 10 — Chatbox

Chỉ làm sau khi Phase 1–9 ổn định.

- Nút chat nổi responsive.
- `IChatAssistantService`.
- Local fallback không cần API.
- Tìm product active.
- Hướng dẫn mua hàng.
- Tra cứu order của đúng customer.
- Không cho AI đổi giá, kho, order hoặc shipment.
- Không hardcode API key.
- Timeout, rate limit, sanitize input/output.
- Chat lỗi không làm hỏng trang.

---

# PHASE 11 — Tài liệu bàn giao

Tạo/cập nhật:

```text
README.md
docs/ARCHITECTURE.md
docs/DATABASE.md
docs/ROLES_AND_PERMISSIONS.md
docs/ORDER_STATE_MACHINE.md
docs/SHIPPING_STATE_MACHINE.md
docs/TEST_CASES.md
docs/TEST_REPORT.md
docs/KNOWN_LIMITATIONS.md
```

README phải có cách:

- Setup môi trường.
- Tạo database bằng `thuongmaidientu.sql`.
- Cấu hình connection string.
- Chạy project.
- Chạy test.
- Dùng demo accounts.
- Demo luồng từng role.
- Biết các giới hạn còn lại.

---

## 5. Dữ liệu demo tối thiểu

- 1 Admin.
- 2 Seller.
- 3 Store.
- 2 Customer.
- 2 Carrier.
- 2 Shipping Provider.
- Mỗi provider có ít nhất 2 service.
- 5 Category.
- 15 Product.
- Một số product có nhiều SKU.
- Một số product có nhiều ảnh.
- Một order nhiều store.
- StoreOrder và Shipment demo.
- Review demo.

Seed phải idempotent.

---

## 6. Definition of Done

Chỉ báo hoàn thành khi:

- `dotnet build` thành công.
- `dotnet test` pass.
- Chỉ dùng database `thuongmaidientu`.
- Chỉ có một file SQL `thuongmaidientu.sql`.
- Không có EF Migration mới.
- Seller data isolation đúng.
- Carrier data isolation đúng.
- Customer ownership đúng.
- Category chỉ Admin CRUD.
- Product tạo đúng store.
- Multi-store checkout hoạt động.
- Shipping fee không hardcode 0.
- StoreOrder và Shipment hoạt động.
- Multiple SKU hoạt động.
- Multiple images hoạt động.
- Review chỉ sau delivered.
- Dashboard scope đúng.
- Home không còn template.
- Branding là marketplace.
- Catalog có filter/sort/pagination.
- Không có secret.
- Có README và test report.
- Không có button chết hoặc TODO giả hoàn thành.

---

## 7. Build và kiểm tra cuối

Chạy:

```bash
dotnet clean
dotnet restore
dotnet build
dotnet test
```

Kiểm tra Git:

```bash
git status
git diff --check
git diff --name-only main...HEAD
git ls-files "*.sql"
```

`git ls-files "*.sql"` chỉ được hiển thị file SQL chính của dự án.

Kiểm tra secret:

```bash
git grep -n -I -E "password|api[_-]?key|secret|token|connectionstring"
```

Review từng kết quả, không xóa mù quáng.

---

## 8. Commit strategy

Commit theo nhóm dễ review:

```text
fix: enforce seller and admin data isolation
feat: add multi-store order schema
feat: add carrier shipping workflow
feat: support multi-sku products and image gallery
feat: redesign marketplace home and catalog
test: add marketplace authorization and checkout tests
docs: add setup and handoff documentation
```

Không commit khi build fail.

---

## 9. Push branch

Xác nhận:

```bash
git branch --show-current
```

Kết quả bắt buộc:

```text
cai_tien_lan1
```

Sau khi build/test/verification đạt yêu cầu:

```bash
git push -u origin cai_tien_lan1
```

Không push `main`.

Branch URL dự kiến:

```text
https://github.com/vinh17491/thuongmaidientu-noithat/tree/cai_tien_lan1
```

---

## 10. Báo cáo cuối bắt buộc

Codex phải trả về:

### Audit

- Lỗi đã xác nhận.
- Lỗi không còn tồn tại.
- File đã sửa.

### Database

- Bảng/cột/index/constraint đã thêm.
- Backfill đã thực hiện.
- Xác nhận chỉ sửa `thuongmaidientu.sql`.
- Xác nhận không tạo database/migration khác.

### Authorization

- Admin.
- Seller.
- Customer.
- Carrier.
- Ownership tests.

### Marketplace flow

- Multi-store cart.
- Parent order.
- Store order.
- Shipping quote.
- Shipment/tracking.

### UI/SEO

- Home.
- Catalog.
- Store page.
- Slug/canonical/sitemap.

### Build/Test

- Lệnh đã chạy.
- Số test pass/fail.
- Blocker thật sự.

### Git

- Branch hiện tại.
- Commit list.
- Push result.
- Branch URL.
- Xác nhận không push main.

### Remaining limitations

- Chỉ ghi vấn đề thật còn lại.
- Không che giấu blocker.
- Không báo hoàn thành giả.

---

## 11. Thứ tự ưu tiên khi thiếu thời gian

### P0

1. Seller data isolation.
2. Product đúng store.
3. Category chỉ Admin.
4. StoreOrder.
5. Multi-store checkout.
6. Carrier/Shipment.
7. Shipping fee thật.
8. Build và test.

### P1

9. Multiple SKU.
10. Multiple images.
11. Store page.
12. Home marketplace.
13. Pagination.
14. Documentation.

### P2

15. SEO nâng cao.
16. Chatbox.
17. Export report.
18. Notification.

Không làm P2 trước khi P0 hoàn tất.

---

## 12. Lệnh bắt đầu

Bắt đầu theo đúng thứ tự:

```text
1. Kiểm tra Git và tạo branch cai_tien_lan1.
2. Audit toàn repository.
3. Chạy baseline build/test.
4. Tạo AUDIT và PLAN.
5. Triển khai PHASE 1.
6. Build và test sau từng phase.
7. Chỉ push khi hoàn thành kiểm tra cuối.
```

Không chỉ lập kế hoạch rồi dừng. Sau audit phải tiếp tục triển khai.
