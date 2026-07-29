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

Manual demo không được báo PASS nếu chưa chạy.

CHAT-01: HTTP Chatbox POST, anti-forgery, validation và local fallback — automated.
