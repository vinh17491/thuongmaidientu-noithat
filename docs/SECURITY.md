# Security notes

Chatbox dùng anti-forgery, input tối đa 500 ký tự, rate limit 15 request/phút/IP và không trả EF entity/order nhạy cảm.

Cookie authentication và role authorization bảo vệ vùng private. Ownership services scope Seller/Carrier/Customer queries. POST workflow dùng antiforgery và ViewModels tránh mass assignment. Password hash dùng BCrypt; không ghi credential/token/connection secret. SEO URLs không tin Request.Host/Scheme. AuditLog/status histories lưu thay đổi workflow. SQL test fallback local chỉ Development và không log đầy đủ connection string.
