# Seller onboarding và Store approval

## Baseline audit

- Branch: `cai_tien_lan1`, HEAD bắt đầu phase: `363e3b9`.
- Schema `stores` thực tế chỉ có `owner_user_id`, `store_name`, `slug`,
  `description`, `status`, SEO fields và `created_at`.
- Không có phone/email/address/logo/banner hoặc bảng store approval history.
- `StoreOwnershipService` trước phase chỉ hỗ trợ scope product/order/review và
  lấy active store; chưa có create/edit store hay status workflow.
- Public `ProductsController` và `HomeController` đã lọc `Store.Status = ACTIVE`.
- Cart và Checkout cũng đã chặn SKU/product/store không ACTIVE.

## Đã triển khai

- Seller `SellerStoresController`: đăng ký store của chính user, không nhận
  `OwnerUserId`/`Status` từ client; store mới luôn `PENDING`; chặn store thứ hai.
- Seller có thể xem/sửa profile của chính mình qua ViewModel, không bind entity.
- `StoreSlugService` chuẩn hóa slug không dấu, chữ thường, dấu gạch ngang và
  tạo suffix unique an toàn.
- `StoreWorkflowService` giới hạn transition:
  `PENDING -> ACTIVE/REJECTED`, `ACTIVE -> SUSPENDED`,
  `SUSPENDED -> ACTIVE`, `REJECTED -> PENDING`.
- Mọi status change của Admin yêu cầu POST, anti-forgery, reason và ghi
  `AuditLog` với actor/before/after/reason.
- Admin `AdminStoresController` có search tên/email, filter status, sắp xếp mới
  nhất, product count và action status qua POST.
- Navigation đã thêm “Gian hàng của tôi” và “Duyệt gian hàng”.
- Product admin của Seller không tạo/sửa/bật product khi store không ACTIVE.
- SQL hiện hữu bổ sung check status canonical và unique owner index; dữ liệu
  legacy `INACTIVE` được chuyển nghĩa an toàn thành `SUSPENDED` trong transaction.

## Kiểm thử

- Build: 0 warning, 0 error.
- Tests: 40 discovered, 40 passed, 0 failed, 0 skipped.
- SQL upgrade trên `thuongmaidientu`: PASS.
- SQL idempotency lần hai: PASS.
- Không tạo database, migration, file SQL mới hoặc xóa dữ liệu nghiệp vụ.

## Hạn chế thực tế

- Schema hiện tại không có các field phone/email/address/logo/banner nên phase
  này không giả lập chúng bằng cột khác.
- Chưa có store status history riêng; `AuditLog` là nguồn lịch sử status.
- Các kịch bản HTTP end-to-end với nhiều user cần fixture cô lập để kiểm tra mà
  không làm thay đổi dữ liệu demo hiện tại; unit/workflow và SQL verification đã
  được bổ sung an toàn.
