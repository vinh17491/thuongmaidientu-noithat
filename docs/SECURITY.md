# Security notes

Chatbox dùng một FormData submit handler, anti-forgery, `textContent`, khóa gửi trùng và input tối đa 500 ký tự. ASP.NET Core RateLimiter policy `ChatAssistant` chỉ áp dụng POST `/tro-ly/hoi`, giới hạn 15 request/phút/IP và trả 429. Order lookup yêu cầu NameIdentifier claim và luôn lọc `Orders.UserId`; anonymous không truy vấn Order.

Cookie authentication và role authorization bảo vệ vùng private. Ownership services scope Seller/Carrier/Customer queries. POST workflow dùng antiforgery và ViewModels tránh mass assignment. Password hash dùng BCrypt; không ghi credential/token/connection secret. SEO URLs không tin Request.Host/Scheme. AuditLog/status histories lưu thay đổi workflow. SQL test fallback local chỉ Development và không log đầy đủ connection string.
