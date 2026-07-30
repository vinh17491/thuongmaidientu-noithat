# Test cases

| ID | Nhóm | Mục tiêu | Loại |
|---|---|---|---|
| AUTH-01 | Authentication | Login tạo claims đúng role | Automated |
| OWN-01 | Ownership | Seller/Carrier chỉ thấy resource của mình | Automated |
| STORE-01 | Store workflow | Store PENDING được Admin duyệt/khóa/mở | Automated/SQL |
| CAT-01 | Catalog | Search/filter/sort/pagination | Automated/SQL |
| SEO-01 | Canonical | Slug routes và legacy redirect | Automated |
| SEO-02 | JSON-LD | Product/Breadcrumb parse được | Automated |
| SEO-03 | Sitemap | XML chỉ chứa public routes | Automated/SQL |
| SHIP-01 | Shipping | Quote/Shipment ownership/status | Automated/SQL |
| ORDER-01 | Order | Checkout tách StoreOrder | Automated/SQL |
| DB-01 | SQL | Constraint/index/schema queryable | SQL integration |
| DB-02 | Seed | Rerun idempotent, 10 demo ACTIVE, không mojibake/cầu lông public | Automated/SQL |
| IMG-01 | Asset | Mọi demo public có primary SVG/alt và HTTP 200 | Automated/HTTP |
| CHAT-02 | Search | Tách “tìm” khỏi “bàn làm việc”, hỗ trợ không dấu, tối đa 5 ACTIVE | Automated/HTTP |
| CHAT-03 | Security | Anonymous không đọc Order; customer chỉ đọc Order thuộc mình | Automated/HTTP |
| CHAT-04 | Rate limit | Request thứ 16/phút trả 429 | Automated HTTP |
| SEO-04 | Category | OG/canonical riêng và đúng một Breadcrumb JSON-LD parse được | Automated/HTTP |

Manual demo không được báo PASS nếu chưa chạy.

CHAT-01: HTTP Chatbox POST FormData, một submit handler, anti-forgery, validation và local fallback — automated.
