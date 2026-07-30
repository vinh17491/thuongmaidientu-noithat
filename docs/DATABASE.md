# Database

Database duy nhất: `thuongmaidientu`. Script duy nhất: `thuongmaidientu.sql`; script hỗ trợ legacy upgrade/idempotency/verification, không dùng EF Migration.

## Bảng theo nhóm

- Identity: `users`.
- Marketplace: `stores`, `products`, `product_skus`, `product_images`, `product_categories`, `reviews`.
- Cart/order: `carts`, `cart_items`, `orders`, `order_items`, `store_orders`, `order_status_histories`.
- Shipping: `shipping_providers`, `shipping_services`, `store_shipping_services`, `shipping_rate_rules`, `shipping_quotes`, `shipments`, `shipment_status_histories`.
- Audit: `audit_logs`.

Foreign keys, unique/index/check constraints, money invariants và stock/promotion rules nằm trong SQL hiện hữu. Không fresh-create bằng database tên khác trong phase này.

Seed cuối duy trì 10 sản phẩm ACTIVE thuộc `gia-dung-viet`, mỗi sản phẩm có SKU ACTIVE và primary SVG/alt text. Script dùng natural key, sửa mojibake demo theo slug và có verification cho số lượng public, ảnh bắt buộc và dữ liệu cầu lông không còn public. Hai store cầu lông được `SUSPENDED`, 20 product `HIDDEN`, 8 category `INACTIVE`; Order/OrderItem/lịch sử không bị xóa.

Trước cập nhật ngày 2026-07-31 đã tạo và VERIFY backup COPY_ONLY tại `D:\ThuongMaiDienTu\DatabaseBackups\thuongmaidientu_pre_final_fix_20260731_013658.bak`.
