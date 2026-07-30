# Demo flow

1. Giới thiệu Nội Thất Hub là marketplace đa Store.
2. Home và Catalog hiển thị 10 sản phẩm nội thất/gia dụng với ảnh SVG; tìm “bàn làm việc”.
3. Category, Store, Product canonical routes và SEO metadata.
4. Customer chọn SKU, Cart và checkout nhiều Store.
5. Customer Orders và StoreOrder tách theo Store.
6. Seller đăng ký Store PENDING và quản lý Product/SKU/ảnh sau duyệt.
7. Admin duyệt Store và xem audit.
8. Carrier xem Shipment thuộc mình và cập nhật tracking/status.
9. Kết luận bằng database groups, SQL script duy nhất và test commands.

Chuẩn bị dữ liệu hợp lệ; không ghi password production.

Chatbox: thử “xin chào”, “tìm bàn làm việc” và “tra đơn DH000001”. Anonymous nhận yêu cầu đăng nhập; customer demo chỉ nhận đơn thuộc claim của mình. Đây là local assistant, không phải external AI.

Kiểm tra thêm `/sitemap.xml`: không có store/category/product cầu lông legacy.
