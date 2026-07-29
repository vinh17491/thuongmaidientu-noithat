# Order state machine

Theo `OrderWorkflowHelper`: `PENDING → CONFIRMED → PROCESSING → SHIPPING → DELIVERED`; `PENDING → CANCELLED`. Transition được kiểm tra server-side, lịch sử ghi vào `order_status_histories`, audit ghi vào `audit_logs`. Parent Order tổng hợp StoreOrder và xử lý mixed delivered/cancelled theo workflow hiện tại.
