# Security notes

Cookie authentication và role authorization bảo vệ vùng private. Ownership services scope Seller/Carrier/Customer queries. POST workflow dùng antiforgery và ViewModels tránh mass assignment. Password hash dùng BCrypt; không ghi credential/token/connection secret. SEO URLs không tin Request.Host/Scheme. AuditLog/status histories lưu thay đổi workflow. SQL test fallback local chỉ Development và không log đầy đủ connection string.
