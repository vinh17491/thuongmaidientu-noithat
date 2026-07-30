# Known limitations

- Integration tests dùng database `thuongmaidientu` hiện hữu; không tạo fresh database tên khác.
- Local SQL fallback `Encrypt=False` chỉ dành cho Development; remote/production phải cung cấp encryption phù hợp.
- Đã chạy HTTP smoke thật nhưng chưa chạy browser end-to-end/visual automation; không tuyên bố browser PASS.
- Chatbox hiện là local fallback; chưa tích hợp external AI provider.
- Không có payment gateway production; BankTransfer hiện là mô phỏng nếu source vẫn giữ trạng thái đó.
- Rate limit dùng remote IP của ứng dụng; khi triển khai sau reverse proxy phải cấu hình Forwarded Headers/proxy tin cậy trước khi dùng địa chỉ client.
