# Roles and permissions

| Chức năng | Customer | Seller | Carrier | Admin |
|---|---|---|---|---|
| Catalog/product/store | Public | Public | Public | Public |
| Cart/checkout/orders/review | Của mình | - | - | - |
| Store/product/SKU | - | Store của mình | - | Toàn sàn |
| StoreOrder | - | Store của mình | - | Toàn sàn |
| Provider/service/rate/shipment | - | - | Của mình | Toàn sàn |
| Store approval/category/audit | - | - | - | Toàn sàn |

Enforcement: role claims, `[Authorize]`, current-user/ownership services, query scoping, antiforgery và ViewModel chống mass-assignment.
