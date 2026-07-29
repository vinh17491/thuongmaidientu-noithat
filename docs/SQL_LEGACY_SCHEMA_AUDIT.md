# SQL legacy schema audit

Audit read-only thực hiện trên `thuongmaidientu`, SQL Server 2025 Developer
17.0.1000.7. Thời điểm audit: 2026-07-29 (Asia/Saigon).

## Baseline

- Branch `cai_tien_lan1`, working tree sạch trước audit.
- Build: 0 warning, 0 error.
- Tests: 24 discovered, 24 passed, 0 failed, 0 skipped.
- Repository chỉ theo dõi `thuongmaidientu.sql`.

## Inventory

Database có 22 bảng: `brands`, `cart_items`, `carts`, `order_items`,
`order_status_histories`, `orders`, `product_categories`, `product_images`,
`product_skus`, `product_specifications`, `products`, `promotion_skus`,
`promotions`, `reviews`, `shipments`, `shipping_providers`,
`shipping_services`, `sku_price_histories`, `store_orders`,
`store_shipping_services`, `stores`, `users`.

Các bảng canonical còn thiếu hoàn toàn: `shipping_rate_rules`,
`shipping_quotes`, `shipment_status_histories`, `audit_logs`.

## Legacy row counts

| Table | Rows |
|---|---:|
| shipping_providers | 2 |
| shipping_services | 4 |
| store_shipping_services | 8 |
| store_orders | 1 |
| shipments | 0 |
| order_status_histories | 1 |

## Legacy → canonical mapping

| Object | Legacy | Canonical code expects | Safe action |
|---|---|---|---|
| Provider owner | `carrier_user_id bigint NULL` | `owner_user_id bigint NOT NULL` | Rename after inventory/drop-and-recreate dependent FK and filtered unique index; validate no null/orphan first |
| Provider natural key | `code varchar(50)` | `slug varchar(220)` | Rename; recreate unique constraint/index under canonical name |
| Provider phone | `contact_phone varchar(20)` | `phone varchar(30)` | Rename then widen |
| Provider metadata | absent | `email`, `description`, `created_at`, `updated_at` | Add nullable/defaulted columns safely |
| Provider status | `ACTIVE/INACTIVE` | `PENDING/ACTIVE/SUSPENDED` | Preserve `ACTIVE`; map legacy `INACTIVE` to `SUSPENDED`; replace trusted check |
| Service PK | `shipping_service_id bigint IDENTITY` | `service_id bigint IDENTITY` | Rename only after dropping/recreating all dependent FKs/PK metadata |
| Service code | `code varchar(50)` | `service_code varchar(80)` | Rename, widen, recreate provider+code unique constraint |
| Service ETA | `estimated_days int` | `estimated_min_days`, `estimated_max_days` | Add canonical columns and backfill both from legacy value; retain legacy column for compatibility until all dependencies are removed |
| Service max weight | absent | `max_weight decimal(18,3)` | Add nullable |
| Store service relation | `(store_id, shipping_service_id, is_enabled, fee_override)` | `StoreShippingService` | Preserve; rename FK column with service PK; map `is_enabled` to canonical `status` in EF or add/backfill status |
| StoreOrder shipping service | `shipping_service_id` | no current C# property | Preserve column/FK; optionally expose compatibility property |
| OrderItem store snapshot | absent | `store_name_snapshot` | Add nullable, backfill from SKU→Product→Store |
| Shipment service FK | `shipping_service_id` | `service_id` | Rename together with service PK dependency |
| Shipment pickup time | `shipped_at` | `picked_up_at` | Rename (same meaning for legacy flow) |
| Shipment metadata | absent | fee, addresses, ETA, created time, row version | Add nullable/defaulted, backfill from StoreOrder/Order/Store where determinable |
| Order history time | `changed_at` | `created_at` | Rename after checking dependencies |
| Order history actor | nullable | non-null in current model | Existing row must be inspected; do not invent actor |

## Keys and dependencies

### Primary/unique

- `shipping_providers`: PK `provider_id`; unique `code`; filtered unique
  `carrier_user_id` when not null.
- `shipping_services`: PK `shipping_service_id`; unique
  `(provider_id, code)`.
- `store_shipping_services`: composite PK
  `(store_id, shipping_service_id)`.
- `store_orders`: PK `store_order_id`; unique `store_order_code`; unique
  `(order_id, store_id)`.
- `shipments`: PK `shipment_id`; unique `store_order_id`; unique tracking.

### Foreign keys that block blind rename

- `FK_shipping_providers_carrier_user`.
- `FK_shipping_services_providers`.
- `FK_store_shipping_services_services`.
- `FK_store_orders_shipping_services`.
- `FK_shipments_services`.
- `FK_shipments_providers`.
- `FK_shipments_store_orders`.

All audited FKs are enabled and trusted. Delete actions include CASCADE from
StoreOrder→Shipment and Store/Service→StoreShippingService; other shipping
relationships are NO ACTION.

### Checks

- Provider legacy status allows only `ACTIVE/INACTIVE`.
- Service fee requires `base_fee > 0` (canonical permits zero).
- Service ETA requires `estimated_days > 0`.
- Store-service override requires null or `> 0`.
- StoreOrder money constraint already enforces
  `total=subtotal-discount+shipping`.
- Shipment legacy status includes `RETURNED`, but lacks
  `READY_FOR_PICKUP` and `CANCELLED`.

All audited checks are enabled and trusted. No audited index is disabled.

## Modules and expression dependencies

No view/procedure/function body was found referencing the searched legacy or
canonical shipping identifiers. `sys.sql_expression_dependencies` reports only
constraints/table dependencies, notably provider `carrier_user_id` and the
legacy checks. This means renames still require rebuilding constraints and
indexes, but no persisted SQL module text needs rewriting at audit time.

## Data-quality checks

All returned zero:

- provider owner orphan;
- service provider orphan;
- store-service store orphan;
- store-service service orphan;
- duplicate provider code;
- duplicate service code within provider;
- duplicate store+service mapping.

`carrier_user_id` is nullable by schema but the two current rows have valid
owners. No ID may be synthesized during upgrade.

## Upgrade conclusion

The database is a coherent earlier marketplace schema, not a disposable or
partially corrupt schema. `store_shipping_services` must be retained and used
by quote selection. A safe upgrade must inventory and explicitly rebuild the
named dependent keys/indexes around canonical renames, backfill new columns,
validate before `NOT NULL`, and run in a transaction. `sp_rename` alone is not
sufficient because the filtered/unique indexes block provider renames and the
service PK is referenced by three business relations.

Raw audit output was stored temporarily outside the repository and is not a
project artifact.
