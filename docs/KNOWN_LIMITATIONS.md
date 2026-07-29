# Known limitations

- Integration tests dùng database `thuongmaidientu` hiện hữu; không tạo fresh database tên khác.
- Local SQL fallback `Encrypt=False` chỉ dành cho Development; remote/production phải cung cấp encryption phù hợp.
- Browser end-to-end automation chưa được chạy trong phase này.
- Chatbox hiện là local fallback; chưa tích hợp external AI provider.
- Không có payment gateway production; BankTransfer hiện là mô phỏng nếu source vẫn giữ trạng thái đó.
