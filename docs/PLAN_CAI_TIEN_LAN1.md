# Kế hoạch cải tiến lần 1

1. Phase 1: gom current user/ownership; scope product, order, review, dashboard; chỉ Admin CRUD category; thêm test quyền.
2. Phase 2: mở rộng duy nhất `thuongmaidientu.sql` và EF model cho StoreOrder, lịch sử, provider/service/rate/quote/shipment, price history, audit log; backfill idempotent.
3. Phase 3–4: cart đa store, quote nội bộ, checkout transaction tạo parent/store orders/shipments; state machine order và shipment.
4. Phase 5–6: nhiều SKU, gallery ảnh, price history, store page và portal seller/carrier.
5. Phase 7–8: marketplace UI, filter/pagination/SEO và security hardening.
6. Phase 9: xUnit unit/integration tests; không dùng EF InMemory cho transaction/constraint.
7. Phase 10 chỉ sau khi Phase 1–9 ổn định; chat local fallback nếu còn phù hợp.
8. Phase 11: tài liệu setup, kiến trúc, database, role, state machines, test report và giới hạn.

Sau mỗi phase: build và test; không sang phase tiếp nếu còn lỗi nghiêm trọng. Cuối cùng clean/restore/build/test, kiểm tra diff, SQL duy nhất, secret/artifact, commit theo nhóm và chỉ push `cai_tien_lan1`.
