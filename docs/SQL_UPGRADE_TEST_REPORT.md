# SQL upgrade test report

Ngày kiểm thử: 2026-07-29 (Asia/Saigon).

## Môi trường

- Instance: `localhost`, Windows integrated authentication.
- Database duy nhất được sử dụng: `thuongmaidientu`.
- SQL Server 2025 Developer, 17.0.1000.7.
- Backup `COPY_ONLY`: `C:\tmp\thuongmaidientu_pre_canonical_20260729.bak`.
- Backup hoàn tất lúc 16:46:10 và đã pass `RESTORE VERIFYONLY`.
- File backup và log chạy thử nằm ngoài repository, không được commit.

## Legacy và mapping

Schema ban đầu có 22 bảng. Các bảng vận chuyển có 2 provider, 4 service,
8 store-service mapping, 1 StoreOrder, 0 Shipment và 1 order history.
Không có orphan hoặc duplicate trong các quan hệ legacy được nâng cấp.

Script đã:

- đổi `carrier_user_id` thành `owner_user_id`;
- đổi provider `code` thành `slug` và `contact_phone` thành `phone`;
- đổi `shipping_service_id` thành `service_id` trên service và các FK phụ thuộc;
- đổi service `code` thành `service_code`;
- đổi `shipped_at` thành `picked_up_at`;
- đổi history `changed_at` thành `created_at`;
- thêm, backfill và kiểm tra các cột canonical;
- giữ nguyên `store_shipping_services` và backfill mapping ACTIVE còn thiếu cho
  các store/service ACTIVE bằng `NOT EXISTS`;
- tạo bốn bảng canonical còn thiếu;
- tái tạo FK, unique key, check constraint và index bằng tên canonical.

Chi tiết dependency trước nâng cấp nằm trong
`docs/SQL_LEGACY_SCHEMA_AUDIT.md`.

## Kết quả chạy thực tế

| Test | Thời gian | Kết quả |
|---|---|---|
| Backup + verify | 16:46:10 | PASS |
| Legacy database upgrade | 16:48:11–16:48:13 | PASS, exit 0 |
| Second run/idempotency | 16:48:26–16:48:27 | PASS, exit 0 |
| Verification mở rộng | 16:51:13–16:51:15 | PASS, exit 0 |

Hai lỗi idempotency phát hiện trong quá trình thử (`carrier_user_id` bị compile
sớm và check constraint trùng tên) đã được sửa trước lượt PASS cuối.

## Row/object counts

| Object | Trước | Sau |
|---|---:|---:|
| Tables | 22 | 26 |
| shipping_providers | 2 | 2 |
| shipping_services | 4 | 4 |
| store_shipping_services | 8 | 8 |
| store_orders | 1 | 1 |
| shipments | 0 | 0 |
| order_status_histories | 1 | 1 |
| shipping_rate_rules | không tồn tại | 0 |
| shipping_quotes | không tồn tại | 0 |
| shipment_status_histories | không tồn tại | 0 |
| audit_logs | không tồn tại | 0 |

Sau lượt hai:

- duplicate store/service mapping: 0;
- orphan store/service mapping: 0;
- legacy shipping columns còn lại: 0;
- FK disabled/untrusted: 0;
- check constraint disabled/untrusted: 0;
- index disabled: 0.

Order history legacy duy nhất có actor `NULL`. Script giữ nguyên lịch sử này,
không gán ID giả; EF mapping đã được sửa thành nullable.

## EF và integration

- Thêm entity/DbSet/relationship `StoreShippingService`.
- Quote selection chỉ dùng mapping được store bật, provider/service/mapping
  ACTIVE và rate ACTIVE; `fee_override` được ưu tiên.
- Integration test SQL Server đọc toàn bộ canonical shipping model, kiểm tra
  quote thuộc mapping của store, và kiểm tra constraint/index thực tế.
- Integration test đã phát hiện và giúp sửa lỗi LINQ projection không dịch
  được sang SQL.
- Build: 0 warning, 0 error.
- Tests: 27 discovered, 27 passed, 0 failed, 0 skipped.

## Giới hạn đã biết

Không tạo database mới hoặc bản sao database mang tên khác: yêu cầu chỉ dùng
database `thuongmaidientu`, đồng thời cấm drop database/bảng nghiệp vụ, xung
đột trực tiếp với Test A/Test B dạng isolated database. Vì vậy lượt legacy
được chạy trên database hiện tại sau backup đã verify; không báo cáo nhầm rằng
fresh-database/copy test đã được thực hiện.

Bộ integration hiện tập trung vào schema/EF/quote path liên quan trực tiếp đến
nâng cấp. Các kịch bản HTTP authorization, checkout rollback và fault injection
toàn transaction vẫn cần fixture dữ liệu cô lập trước khi có thể kiểm thử mà
không sửa dữ liệu nghiệp vụ hiện tại.

## Cấu hình integration test

Integration tests ưu tiên connection string từ biến môi trường
`THUONGMAIDIENTU_TEST_CONNECTION`. Giá trị phải trỏ tới đúng database
`thuongmaidientu`; helper không ghi toàn bộ connection string ra log.

PowerShell:

```powershell
$env:THUONGMAIDIENTU_TEST_CONNECTION="Server=localhost;Database=thuongmaidientu;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test
```

Nếu biến môi trường không tồn tại, test local Development dùng fallback
Windows integrated authentication tới `localhost/thuongmaidientu`. Fallback
không chứa username, password, API key hoặc secret và không thay đổi connection
string của ứng dụng chính.
