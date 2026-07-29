# Database

Database duy nhất: `thuongmaidientu`. Script duy nhất: `thuongmaidientu.sql`; script hỗ trợ legacy upgrade/idempotency/verification, không dùng EF Migration.

## Bảng theo nhóm

- Identity: `users`.
- Marketplace: `stores`, `products`, `product_skus`, `product_images`, `product_categories`, `reviews`.
- Cart/order: `carts`, `cart_items`, `orders`, `order_items`, `store_orders`, `order_status_histories`.
- Shipping: `shipping_providers`, `shipping_services`, `store_shipping_services`, `shipping_rate_rules`, `shipping_quotes`, `shipments`, `shipment_status_histories`.
- Audit: `audit_logs`.

Foreign keys, unique/index/check constraints, money invariants và stock/promotion rules nằm trong SQL hiện hữu. Không fresh-create bằng database tên khác trong phase này.
