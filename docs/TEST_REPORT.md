# Test report

Ngày kiểm thử: 2026-07-31. Branch: `cai_tien_lan1`; baseline HEAD: `106a8f9`.

- Build: 0 warning, 0 error.
- Test suite: 69 discovered, 69 passed, 0 failed, 0 skipped.
- SQL integration filter: 6 discovered, 6 passed, 0 failed, 0 skipped.
- Database: `thuongmaidientu`; không drop dữ liệu và không tạo database khác.
- Data verification: 0 public mojibake marker, 0 public badminton product, 10 household products ACTIVE; 20 badminton products HIDDEN, 2 stores SUSPENDED, 8 categories INACTIVE.
- HTTP profile: `/`, `/san-pham`, Category, Store, Product, `/sitemap.xml`, `/robots.txt` và demo SVG đều 200.
- Chat smoke: greeting/search/anonymous isolation/customer-owned `DH000001` pass; rate-limit request 16 trả 429 trong automated test.

Script SQL được chạy lại bằng UTF-8 và verification/idempotent guards đã pass sau backup COPY_ONLY được VERIFY. Coverage gồm workflow marketplace, ownership, Catalog/slug route, Category OG/Breadcrumb, sitemap/robots, ảnh và SQL schema. Không có browser automation; xem [Known limitations](KNOWN_LIMITATIONS.md).
