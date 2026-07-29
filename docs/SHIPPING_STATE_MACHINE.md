# Shipping state machine

Provider/Service/StoreShippingService/RateRule phải ACTIVE để tạo quote. Quote có expiry; Shipment gắn StoreOrder và Carrier ownership. Carrier cập nhật status qua workflow, ghi `shipment_status_histories`, tracking timestamps và đồng bộ StoreOrder/Parent Order.
