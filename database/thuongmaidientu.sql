/* ============================================================================
   THUONG MAI DIEN TU - DATABASE INSTALLATION AND SAFE UPGRADE
   This is the single source of truth for database schema, demo data and checks.
   The script is rerunnable and never drops business tables or existing rows.
   ============================================================================ */

/* ============================================================================
   1. CREATE DATABASE IF IT DOES NOT EXIST
   ============================================================================ */
IF DB_ID(N'thuongmaidientu') IS NULL
BEGIN
    CREATE DATABASE thuongmaidientu;
END;
GO

USE thuongmaidientu;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ============================================================================
   2. CREATE OR UPGRADE TABLES
   New tables are created in dependency order. Existing tables are preserved.
   ============================================================================ */
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.users', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.users
        (
            user_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_users PRIMARY KEY,
            full_name NVARCHAR(120) NOT NULL,
            email VARCHAR(150) NOT NULL,
            phone VARCHAR(20) NULL,
            password_hash VARCHAR(255) NOT NULL,
            role VARCHAR(20) NOT NULL,
            status VARCHAR(20) NOT NULL CONSTRAINT DF_users_status DEFAULT ('ACTIVE'),
            created_at DATETIME2 NOT NULL CONSTRAINT DF_users_created_at DEFAULT (SYSDATETIME()),
            updated_at DATETIME2 NULL
        );
    END;

    IF OBJECT_ID(N'dbo.stores', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.stores
        (
            store_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_stores PRIMARY KEY,
            owner_user_id BIGINT NOT NULL,
            store_name NVARCHAR(150) NOT NULL,
            slug VARCHAR(180) NOT NULL,
            description NVARCHAR(1000) NULL,
            status VARCHAR(20) NOT NULL CONSTRAINT DF_stores_status DEFAULT ('ACTIVE'),
            seo_title NVARCHAR(180) NULL,
            seo_description NVARCHAR(320) NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_stores_created_at DEFAULT (SYSDATETIME())
        );
    END;

    IF OBJECT_ID(N'dbo.product_categories', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.product_categories
        (
            category_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_product_categories PRIMARY KEY,
            parent_id BIGINT NULL,
            category_name NVARCHAR(150) NOT NULL,
            slug VARCHAR(180) NOT NULL,
            description NVARCHAR(500) NULL,
            seo_title NVARCHAR(180) NULL,
            seo_description NVARCHAR(320) NULL,
            status VARCHAR(20) NOT NULL CONSTRAINT DF_product_categories_status DEFAULT ('ACTIVE'),
            sort_order INT NOT NULL CONSTRAINT DF_product_categories_sort_order DEFAULT (0)
        );
    END;

    IF OBJECT_ID(N'dbo.products', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.products
        (
            product_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_products PRIMARY KEY,
            store_id BIGINT NOT NULL,
            category_id BIGINT NOT NULL,
            product_name NVARCHAR(200) NOT NULL,
            slug VARCHAR(220) NOT NULL,
            brand NVARCHAR(100) NULL,
            short_description NVARCHAR(500) NULL,
            description NVARCHAR(MAX) NULL,
            status VARCHAR(20) NOT NULL CONSTRAINT DF_products_status DEFAULT ('ACTIVE'),
            seo_title NVARCHAR(180) NULL,
            seo_description NVARCHAR(320) NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_products_created_at DEFAULT (SYSDATETIME()),
            updated_at DATETIME2 NULL
        );
    END;

    IF OBJECT_ID(N'dbo.product_skus', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.product_skus
        (
            sku_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_product_skus PRIMARY KEY,
            product_id BIGINT NOT NULL,
            sku_code VARCHAR(80) NOT NULL,
            price DECIMAL(18,2) NOT NULL,
            sale_price DECIMAL(18,2) NULL,
            sale_start DATETIME2 NULL,
            sale_end DATETIME2 NULL,
            stock_quantity INT NOT NULL CONSTRAINT DF_product_skus_stock_quantity DEFAULT (0),
            status VARCHAR(20) NOT NULL CONSTRAINT DF_product_skus_status DEFAULT ('ACTIVE')
        );
    END;

    IF OBJECT_ID(N'dbo.product_images', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.product_images
        (
            image_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_product_images PRIMARY KEY,
            product_id BIGINT NOT NULL,
            image_url VARCHAR(500) NOT NULL,
            alt_text NVARCHAR(180) NULL,
            is_primary BIT NOT NULL CONSTRAINT DF_product_images_is_primary DEFAULT (0),
            sort_order INT NOT NULL CONSTRAINT DF_product_images_sort_order DEFAULT (0)
        );
    END;

    IF OBJECT_ID(N'dbo.carts', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.carts
        (
            cart_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_carts PRIMARY KEY,
            user_id BIGINT NOT NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_carts_created_at DEFAULT (SYSDATETIME()),
            updated_at DATETIME2 NULL
        );
    END;

    IF OBJECT_ID(N'dbo.cart_items', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.cart_items
        (
            cart_item_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_cart_items PRIMARY KEY,
            cart_id BIGINT NOT NULL,
            sku_id BIGINT NOT NULL,
            quantity INT NOT NULL CONSTRAINT DF_cart_items_quantity DEFAULT (1),
            added_at DATETIME2 NOT NULL CONSTRAINT DF_cart_items_added_at DEFAULT (SYSDATETIME())
        );
    END;

    IF OBJECT_ID(N'dbo.orders', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.orders
        (
            order_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_orders PRIMARY KEY,
            order_code VARCHAR(30) NOT NULL,
            user_id BIGINT NOT NULL,
            receiver_name NVARCHAR(120) NOT NULL,
            receiver_phone VARCHAR(20) NOT NULL,
            shipping_address NVARCHAR(300) NOT NULL,
            subtotal DECIMAL(18,2) NOT NULL,
            discount_amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_orders_discount_amount DEFAULT (0),
            shipping_fee DECIMAL(18,2) NOT NULL CONSTRAINT DF_orders_shipping_fee DEFAULT (0),
            total_amount DECIMAL(18,2) NOT NULL,
            payment_method VARCHAR(20) NOT NULL CONSTRAINT DF_orders_payment_method DEFAULT ('COD'),
            payment_status VARCHAR(20) NOT NULL CONSTRAINT DF_orders_payment_status DEFAULT ('UNPAID'),
            order_status VARCHAR(30) NOT NULL CONSTRAINT DF_orders_order_status DEFAULT ('PENDING'),
            note NVARCHAR(500) NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_orders_created_at DEFAULT (SYSDATETIME()),
            updated_at DATETIME2 NULL
        );
    END;

    IF OBJECT_ID(N'dbo.order_items', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.order_items
        (
            order_item_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_order_items PRIMARY KEY,
            order_id BIGINT NOT NULL,
            sku_id BIGINT NOT NULL,
            product_name NVARCHAR(200) NOT NULL,
            sku_code VARCHAR(80) NOT NULL,
            unit_price DECIMAL(18,2) NOT NULL,
            quantity INT NOT NULL,
            line_total AS (unit_price * quantity) PERSISTED
        );
    END;

    IF OBJECT_ID(N'dbo.reviews', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.reviews
        (
            review_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_reviews PRIMARY KEY,
            user_id BIGINT NOT NULL,
            product_id BIGINT NOT NULL,
            order_item_id BIGINT NULL,
            rating INT NOT NULL,
            comment NVARCHAR(1000) NULL,
            status VARCHAR(20) NOT NULL CONSTRAINT DF_reviews_status DEFAULT ('VISIBLE'),
            created_at DATETIME2 NOT NULL CONSTRAINT DF_reviews_created_at DEFAULT (SYSDATETIME())
        );
    END;

    /* Example of a safe additive upgrade for databases created by older versions. */
    IF COL_LENGTH(N'dbo.users', N'updated_at') IS NULL
        ALTER TABLE dbo.users ADD updated_at DATETIME2 NULL;

    IF COL_LENGTH(N'dbo.products', N'updated_at') IS NULL
        ALTER TABLE dbo.products ADD updated_at DATETIME2 NULL;

    IF COL_LENGTH(N'dbo.carts', N'updated_at') IS NULL
        ALTER TABLE dbo.carts ADD updated_at DATETIME2 NULL;

    IF COL_LENGTH(N'dbo.orders', N'updated_at') IS NULL
        ALTER TABLE dbo.orders ADD updated_at DATETIME2 NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* ============================================================================
   3. BACKFILL DATA BEFORE CONSTRAINTS
   Only repairs nullable/invalid legacy values that would block constraints.
   ============================================================================ */
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE dbo.users SET status = 'ACTIVE' WHERE status IS NULL;
    UPDATE dbo.stores SET status = 'ACTIVE' WHERE status IS NULL;
    UPDATE dbo.product_categories SET status = 'ACTIVE' WHERE status IS NULL;
    UPDATE dbo.products SET status = 'ACTIVE' WHERE status IS NULL;
    UPDATE dbo.product_skus SET stock_quantity = 0 WHERE stock_quantity IS NULL OR stock_quantity < 0;
    UPDATE dbo.product_skus SET status = 'ACTIVE' WHERE status IS NULL;
    UPDATE dbo.product_images SET is_primary = 0 WHERE is_primary IS NULL;
    UPDATE dbo.product_images SET sort_order = 0 WHERE sort_order IS NULL;
    UPDATE dbo.cart_items SET quantity = 1 WHERE quantity IS NULL OR quantity <= 0;
    UPDATE dbo.orders SET discount_amount = 0 WHERE discount_amount IS NULL OR discount_amount < 0;
    UPDATE dbo.orders SET shipping_fee = 0 WHERE shipping_fee IS NULL OR shipping_fee < 0;
    UPDATE dbo.reviews SET status = 'VISIBLE' WHERE status IS NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* ============================================================================
   4. FOREIGN KEYS, CHECK CONSTRAINTS AND INDEXES
   Each object is added only when absent.
   ============================================================================ */
BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_stores_users')
        ALTER TABLE dbo.stores WITH CHECK ADD CONSTRAINT FK_stores_users
            FOREIGN KEY (owner_user_id) REFERENCES dbo.users(user_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_product_categories_parent')
        ALTER TABLE dbo.product_categories WITH CHECK ADD CONSTRAINT FK_product_categories_parent
            FOREIGN KEY (parent_id) REFERENCES dbo.product_categories(category_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_products_stores')
        ALTER TABLE dbo.products WITH CHECK ADD CONSTRAINT FK_products_stores
            FOREIGN KEY (store_id) REFERENCES dbo.stores(store_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_products_categories')
        ALTER TABLE dbo.products WITH CHECK ADD CONSTRAINT FK_products_categories
            FOREIGN KEY (category_id) REFERENCES dbo.product_categories(category_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_product_skus_products')
        ALTER TABLE dbo.product_skus WITH CHECK ADD CONSTRAINT FK_product_skus_products
            FOREIGN KEY (product_id) REFERENCES dbo.products(product_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_product_images_products')
        ALTER TABLE dbo.product_images WITH CHECK ADD CONSTRAINT FK_product_images_products
            FOREIGN KEY (product_id) REFERENCES dbo.products(product_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_carts_users')
        ALTER TABLE dbo.carts WITH CHECK ADD CONSTRAINT FK_carts_users
            FOREIGN KEY (user_id) REFERENCES dbo.users(user_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_cart_items_carts')
        ALTER TABLE dbo.cart_items WITH CHECK ADD CONSTRAINT FK_cart_items_carts
            FOREIGN KEY (cart_id) REFERENCES dbo.carts(cart_id) ON DELETE CASCADE;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_cart_items_skus')
        ALTER TABLE dbo.cart_items WITH CHECK ADD CONSTRAINT FK_cart_items_skus
            FOREIGN KEY (sku_id) REFERENCES dbo.product_skus(sku_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_orders_users')
        ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT FK_orders_users
            FOREIGN KEY (user_id) REFERENCES dbo.users(user_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_order_items_orders')
        ALTER TABLE dbo.order_items WITH CHECK ADD CONSTRAINT FK_order_items_orders
            FOREIGN KEY (order_id) REFERENCES dbo.orders(order_id) ON DELETE CASCADE;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_order_items_skus')
        ALTER TABLE dbo.order_items WITH CHECK ADD CONSTRAINT FK_order_items_skus
            FOREIGN KEY (sku_id) REFERENCES dbo.product_skus(sku_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_reviews_users')
        ALTER TABLE dbo.reviews WITH CHECK ADD CONSTRAINT FK_reviews_users
            FOREIGN KEY (user_id) REFERENCES dbo.users(user_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_reviews_products')
        ALTER TABLE dbo.reviews WITH CHECK ADD CONSTRAINT FK_reviews_products
            FOREIGN KEY (product_id) REFERENCES dbo.products(product_id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_reviews_order_items')
        ALTER TABLE dbo.reviews WITH CHECK ADD CONSTRAINT FK_reviews_order_items
            FOREIGN KEY (order_item_id) REFERENCES dbo.order_items(order_item_id);

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_users_role')
        ALTER TABLE dbo.users WITH CHECK ADD CONSTRAINT CK_users_role CHECK (role IN ('CUSTOMER','SELLER','ADMIN','CARRIER'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_users_status')
        ALTER TABLE dbo.users WITH CHECK ADD CONSTRAINT CK_users_status CHECK (status IN ('ACTIVE','BLOCKED'));
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_stores_status')
        ALTER TABLE dbo.stores DROP CONSTRAINT CK_stores_status;
    UPDATE dbo.stores SET status = 'SUSPENDED' WHERE status = 'INACTIVE';
    ALTER TABLE dbo.stores WITH CHECK ADD CONSTRAINT CK_stores_status CHECK (status IN ('PENDING','ACTIVE','REJECTED','SUSPENDED'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_product_categories_status')
        ALTER TABLE dbo.product_categories WITH CHECK ADD CONSTRAINT CK_product_categories_status CHECK (status IN ('ACTIVE','INACTIVE'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_products_status')
        ALTER TABLE dbo.products WITH CHECK ADD CONSTRAINT CK_products_status CHECK (status IN ('DRAFT','ACTIVE','HIDDEN'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_product_skus_price')
        ALTER TABLE dbo.product_skus WITH CHECK ADD CONSTRAINT CK_product_skus_price CHECK (price > 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_product_skus_sale_price')
        ALTER TABLE dbo.product_skus WITH CHECK ADD CONSTRAINT CK_product_skus_sale_price CHECK (sale_price IS NULL OR (sale_price > 0 AND sale_price <= price));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_product_skus_sale_time')
        ALTER TABLE dbo.product_skus WITH CHECK ADD CONSTRAINT CK_product_skus_sale_time CHECK (sale_end IS NULL OR sale_start IS NULL OR sale_end > sale_start);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_product_skus_stock')
        ALTER TABLE dbo.product_skus WITH CHECK ADD CONSTRAINT CK_product_skus_stock CHECK (stock_quantity >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_product_skus_status')
        ALTER TABLE dbo.product_skus WITH CHECK ADD CONSTRAINT CK_product_skus_status CHECK (status IN ('ACTIVE','INACTIVE'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_cart_items_quantity')
        ALTER TABLE dbo.cart_items WITH CHECK ADD CONSTRAINT CK_cart_items_quantity CHECK (quantity > 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_money')
        ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_money CHECK (subtotal >= 0 AND discount_amount >= 0 AND shipping_fee >= 0 AND total_amount >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_payment_method')
        ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_payment_method CHECK (payment_method IN ('COD','BANK_TRANSFER'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_payment_status')
        ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_payment_status CHECK (payment_status IN ('UNPAID','PAID','FAILED'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_status')
        ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_status CHECK (order_status IN ('PENDING','CONFIRMED','PROCESSING','SHIPPING','DELIVERED','CANCELLED'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_order_items_price')
        ALTER TABLE dbo.order_items WITH CHECK ADD CONSTRAINT CK_order_items_price CHECK (unit_price >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_order_items_quantity')
        ALTER TABLE dbo.order_items WITH CHECK ADD CONSTRAINT CK_order_items_quantity CHECK (quantity > 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_reviews_rating')
        ALTER TABLE dbo.reviews WITH CHECK ADD CONSTRAINT CK_reviews_rating CHECK (rating BETWEEN 1 AND 5);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_reviews_status')
        ALTER TABLE dbo.reviews WITH CHECK ADD CONSTRAINT CK_reviews_status CHECK (status IN ('VISIBLE','HIDDEN'));

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.users') AND name = N'UQ_users_email')
        CREATE UNIQUE INDEX UQ_users_email ON dbo.users(email);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.users') AND name = N'UQ_users_phone')
        CREATE UNIQUE INDEX UQ_users_phone ON dbo.users(phone) WHERE phone IS NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.stores') AND name = N'UQ_stores_slug')
        CREATE UNIQUE INDEX UQ_stores_slug ON dbo.stores(slug);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.stores') AND name = N'UQ_stores_owner_user')
        CREATE UNIQUE INDEX UQ_stores_owner_user ON dbo.stores(owner_user_id);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.product_categories') AND name = N'UQ_product_categories_slug')
        CREATE UNIQUE INDEX UQ_product_categories_slug ON dbo.product_categories(slug);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.products') AND name = N'UQ_products_store_slug')
        CREATE UNIQUE INDEX UQ_products_store_slug ON dbo.products(store_id, slug);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.product_skus') AND name = N'UQ_product_skus_sku_code')
        CREATE UNIQUE INDEX UQ_product_skus_sku_code ON dbo.product_skus(sku_code);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.carts') AND name = N'UQ_carts_user_id')
        CREATE UNIQUE INDEX UQ_carts_user_id ON dbo.carts(user_id);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.cart_items') AND name = N'UQ_cart_items_cart_sku')
        CREATE UNIQUE INDEX UQ_cart_items_cart_sku ON dbo.cart_items(cart_id, sku_id);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.orders') AND name = N'UQ_orders_order_code')
        CREATE UNIQUE INDEX UQ_orders_order_code ON dbo.orders(order_code);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.reviews') AND name = N'UQ_reviews_user_order_item')
        CREATE UNIQUE INDEX UQ_reviews_user_order_item ON dbo.reviews(user_id, order_item_id) WHERE order_item_id IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.products') AND name = N'IX_products_category_status')
        CREATE INDEX IX_products_category_status ON dbo.products(category_id, status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.products') AND name = N'IX_products_store_status')
        CREATE INDEX IX_products_store_status ON dbo.products(store_id, status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.product_skus') AND name = N'IX_product_skus_product_status')
        CREATE INDEX IX_product_skus_product_status ON dbo.product_skus(product_id, status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.orders') AND name = N'IX_orders_user_created')
        CREATE INDEX IX_orders_user_created ON dbo.orders(user_id, created_at DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.orders') AND name = N'IX_orders_status')
        CREATE INDEX IX_orders_status ON dbo.orders(order_status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.order_items') AND name = N'IX_order_items_order')
        CREATE INDEX IX_order_items_order ON dbo.order_items(order_id);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.reviews') AND name = N'IX_reviews_product_status')
        CREATE INDEX IX_reviews_product_status ON dbo.reviews(product_id, status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.product_images') AND name = N'UX_product_images_one_primary')
        CREATE UNIQUE INDEX UX_product_images_one_primary ON dbo.product_images(product_id) WHERE is_primary = 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* ============================================================================
   5. IDEMPOTENT DEMO DATA
   Natural keys are used; no identity value is assumed.
   ============================================================================ */
BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = 'admin@homegoods.vn')
        INSERT dbo.users(full_name,email,phone,password_hash,role,status)
        VALUES(N'Nguyễn Văn Admin','admin@homegoods.vn','0900000001','demo_hash_admin','ADMIN','ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = 'seller@gdviet.vn')
        INSERT dbo.users(full_name,email,phone,password_hash,role,status)
        VALUES(N'Cửa hàng Gia Dụng Việt','seller@gdviet.vn','0900000002','demo_hash_seller','SELLER','ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = 'customer@example.com')
        INSERT dbo.users(full_name,email,phone,password_hash,role,status)
        VALUES(N'Trần Thị Khách Hàng','customer@example.com','0900000003','demo_hash_customer','CUSTOMER','ACTIVE');

    DECLARE @SellerId BIGINT = (SELECT user_id FROM dbo.users WHERE email = 'seller@gdviet.vn');
    DECLARE @CustomerId BIGINT = (SELECT user_id FROM dbo.users WHERE email = 'customer@example.com');

    IF NOT EXISTS (SELECT 1 FROM dbo.stores WHERE slug = 'gia-dung-viet')
        INSERT dbo.stores(owner_user_id,store_name,slug,description,status,seo_title,seo_description)
        VALUES(@SellerId,N'Gia Dụng Việt','gia-dung-viet',N'Cửa hàng chuyên bán sản phẩm gia dụng phục vụ gia đình.','ACTIVE',
               N'Gia Dụng Việt - Đồ gia dụng chất lượng',N'Mua đồ gia dụng chính hãng với mức giá phù hợp.');

    IF NOT EXISTS (SELECT 1 FROM dbo.product_categories WHERE slug = 'nha-bep')
        INSERT dbo.product_categories(parent_id,category_name,slug,description,seo_title,seo_description,status,sort_order)
        VALUES(NULL,N'Nhà bếp','nha-bep',N'Thiết bị và đồ dùng nhà bếp.',N'Đồ gia dụng nhà bếp',N'Nồi cơm, bếp điện, máy xay và ấm siêu tốc.','ACTIVE',1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_categories WHERE slug = 've-sinh-nha-cua')
        INSERT dbo.product_categories(parent_id,category_name,slug,description,seo_title,seo_description,status,sort_order)
        VALUES(NULL,N'Vệ sinh nhà cửa','ve-sinh-nha-cua',N'Sản phẩm hỗ trợ vệ sinh nhà cửa.',N'Dụng cụ vệ sinh nhà cửa',N'Máy hút bụi và dụng cụ vệ sinh gia đình.','ACTIVE',2);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_categories WHERE slug = 'dien-gia-dung')
        INSERT dbo.product_categories(parent_id,category_name,slug,description,seo_title,seo_description,status,sort_order)
        VALUES(NULL,N'Điện gia dụng','dien-gia-dung',N'Thiết bị điện gia dụng.',N'Điện gia dụng',N'Thiết bị điện sử dụng trong gia đình.','ACTIVE',3);

    DECLARE @StoreId BIGINT = (SELECT store_id FROM dbo.stores WHERE slug = 'gia-dung-viet');
    DECLARE @KitchenId BIGINT = (SELECT category_id FROM dbo.product_categories WHERE slug = 'nha-bep');
    DECLARE @CleaningId BIGINT = (SELECT category_id FROM dbo.product_categories WHERE slug = 've-sinh-nha-cua');
    DECLARE @ElectricalId BIGINT = (SELECT category_id FROM dbo.product_categories WHERE slug = 'dien-gia-dung');

    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='noi-com-dien-sharp-18l')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@KitchenId,N'Nồi cơm điện Sharp 1.8L','noi-com-dien-sharp-18l',N'Sharp',N'Nồi cơm điện phù hợp gia đình 4-6 người.',N'Lòng nồi chống dính, giữ ấm tốt và dễ sử dụng.','ACTIVE',N'Nồi cơm điện Sharp 1.8L',N'Nồi cơm điện Sharp chính hãng, giá tốt.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='may-xay-sinh-to-philips')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@KitchenId,N'Máy xay sinh tố Philips','may-xay-sinh-to-philips',N'Philips',N'Máy xay sinh tố công suất mạnh.',N'Phù hợp xay trái cây và thực phẩm mềm.','ACTIVE',N'Máy xay sinh tố Philips',N'Máy xay Philips bền và dễ sử dụng.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='bep-dien-tu-sunhouse')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@KitchenId,N'Bếp điện từ Sunhouse','bep-dien-tu-sunhouse',N'Sunhouse',N'Bếp điện từ mặt kính chịu lực.',N'Nhiều chế độ nấu, an toàn và dễ vệ sinh.','ACTIVE',N'Bếp điện từ Sunhouse',N'Bếp điện từ Sunhouse chính hãng.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='am-sieu-toc-locklock-17l')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@ElectricalId,N'Ấm siêu tốc Lock&Lock 1.7L','am-sieu-toc-locklock-17l',N'Lock&Lock',N'Ấm siêu tốc tự ngắt khi sôi.',N'Thiết kế inox, đun nước nhanh và an toàn.','ACTIVE',N'Ấm siêu tốc Lock&Lock 1.7L',N'Ấm siêu tốc Lock&Lock chính hãng.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='may-hut-bui-cam-tay-deerma')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@CleaningId,N'Máy hút bụi cầm tay Deerma','may-hut-bui-cam-tay-deerma',N'Deerma',N'Máy hút bụi nhỏ gọn cho gia đình.',N'Lực hút tốt, dễ sử dụng và bảo quản.','ACTIVE',N'Máy hút bụi cầm tay Deerma',N'Máy hút bụi cầm tay giá tốt.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='ban-lam-viec-go-soi')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@ElectricalId,N'Bàn làm việc gỗ sồi','ban-lam-viec-go-soi',N'Nội Thất Hub',N'Bàn làm việc gọn gàng cho góc học tập.',N'Mặt bàn vân gỗ, khung chắc chắn và dễ lắp đặt.','ACTIVE',N'Bàn làm việc gỗ sồi',N'Bàn làm việc gỗ sồi cho gia đình.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='ghe-an-boc-ni')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@ElectricalId,N'Ghế ăn bọc nỉ','ghe-an-boc-ni',N'Nội Thất Hub',N'Ghế ăn bọc nỉ êm ái, khung gỗ.',N'Kiểu dáng tối giản phù hợp bàn ăn gia đình.','ACTIVE',N'Ghế ăn bọc nỉ',N'Ghế ăn bọc nỉ hiện đại.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='den-ban-led-chong-can')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@ElectricalId,N'Đèn bàn LED chống cận','den-ban-led-chong-can',N'Rạng Đông',N'Đèn bàn LED điều chỉnh độ sáng.',N'Ánh sáng dịu, tiết kiệm điện và phù hợp bàn học.','ACTIVE',N'Đèn bàn LED chống cận',N'Đèn bàn LED bảo vệ mắt.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='ke-sach-go-ba-tang')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@ElectricalId,N'Kệ sách gỗ ba tầng','ke-sach-go-ba-tang',N'Nội Thất Hub',N'Kệ sách ba tầng nhỏ gọn.',N'Kệ gỗ đa năng cho phòng khách hoặc góc làm việc.','ACTIVE',N'Kệ sách gỗ ba tầng',N'Kệ sách gỗ nhỏ gọn.');
    IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE store_id=@StoreId AND slug='chao-chong-dinh-28cm')
        INSERT dbo.products(store_id,category_id,product_name,slug,brand,short_description,description,status,seo_title,seo_description)
        VALUES(@StoreId,@KitchenId,N'Chảo chống dính 28 cm','chao-chong-dinh-28cm',N'Sunhouse',N'Chảo chống dính dùng cho bếp gia đình.',N'Lòng chảo bền, tay cầm cách nhiệt và dễ vệ sinh.','ACTIVE',N'Chảo chống dính 28 cm',N'Chảo chống dính Sunhouse 28 cm.');

    DECLARE @RiceCookerId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='noi-com-dien-sharp-18l');
    DECLARE @BlenderId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='may-xay-sinh-to-philips');
    DECLARE @CooktopId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='bep-dien-tu-sunhouse');
    DECLARE @KettleId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='am-sieu-toc-locklock-17l');
    DECLARE @VacuumId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='may-hut-bui-cam-tay-deerma');
    DECLARE @DeskId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='ban-lam-viec-go-soi');
    DECLARE @ChairId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='ghe-an-boc-ni');
    DECLARE @LampId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='den-ban-led-chong-can');
    DECLARE @ShelfId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='ke-sach-go-ba-tang');
    DECLARE @PanId BIGINT=(SELECT product_id FROM dbo.products WHERE store_id=@StoreId AND slug='chao-chong-dinh-28cm');

    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='NC-SHARP-18L')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@RiceCookerId,'NC-SHARP-18L',900000,799000,'2026-01-01','2026-12-31',25,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='MX-PHILIPS-450W')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@BlenderId,'MX-PHILIPS-450W',650000,590000,'2026-01-01','2026-12-31',18,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='BDT-SUNHOUSE-2000W')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@CooktopId,'BDT-SUNHOUSE-2000W',1200000,1050000,'2026-01-01','2026-12-31',12,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='AST-LOCKLOCK-17L')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@KettleId,'AST-LOCKLOCK-17L',520000,NULL,NULL,NULL,30,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='MHB-DEERMA-CAMTAY')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@VacuumId,'MHB-DEERMA-CAMTAY',850000,765000,'2026-01-01','2026-12-31',15,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='BLV-GOSOI-120')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@DeskId,'BLV-GOSOI-120',2190000,1990000,'2026-01-01','2026-12-31',10,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='GA-NI-XAM')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@ChairId,'GA-NI-XAM',12000,NULL,NULL,NULL,20,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='DB-LED-RD01')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@LampId,'DB-LED-RD01',490000,439000,'2026-01-01','2026-12-31',24,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='KS-GO-3T')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@ShelfId,'KS-GO-3T',1290000,NULL,NULL,NULL,11,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.product_skus WHERE sku_code='CHAO-SH-28')
        INSERT dbo.product_skus(product_id,sku_code,price,sale_price,sale_start,sale_end,stock_quantity,status) VALUES(@PanId,'CHAO-SH-28',620000,559000,'2026-01-01','2026-12-31',32,'ACTIVE');

    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@RiceCookerId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@RiceCookerId,'/images/products/noi-com-dien-sharp-18l.svg',N'Nồi cơm điện Sharp 1.8L',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@BlenderId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@BlenderId,'/images/products/may-xay-sinh-to-philips.svg',N'Máy xay sinh tố Philips',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@CooktopId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@CooktopId,'/images/products/bep-dien-tu-sunhouse.svg',N'Bếp điện từ Sunhouse',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@KettleId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@KettleId,'/images/products/am-sieu-toc-locklock-17l.svg',N'Ấm siêu tốc Lock&Lock 1.7L',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@VacuumId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@VacuumId,'/images/products/may-hut-bui-cam-tay-deerma.svg',N'Máy hút bụi cầm tay Deerma',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@DeskId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@DeskId,'/images/products/ban-lam-viec-go-soi.svg',N'Bàn làm việc gỗ sồi',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@ChairId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@ChairId,'/images/products/ghe-an-boc-ni.svg',N'Ghế ăn bọc nỉ',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@LampId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@LampId,'/images/products/den-ban-led-chong-can.svg',N'Đèn bàn LED chống cận',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@ShelfId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@ShelfId,'/images/products/ke-sach-go-ba-tang.svg',N'Kệ sách gỗ ba tầng',1,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.product_images WHERE product_id=@PanId AND is_primary=1)
        INSERT dbo.product_images(product_id,image_url,alt_text,is_primary,sort_order) VALUES(@PanId,'/images/products/chao-chong-dinh-28cm.svg',N'Chảo chống dính 28 cm',1,1);

    /* Restore the household demo and retire the legacy badminton catalog without deleting history. */
    UPDATE dbo.stores SET status='ACTIVE' WHERE slug='gia-dung-viet';
    UPDATE dbo.product_categories SET status='ACTIVE' WHERE slug IN('nha-bep','ve-sinh-nha-cua','dien-gia-dung');
    UPDATE dbo.products SET status='ACTIVE'
    WHERE store_id=@StoreId AND slug IN('noi-com-dien-sharp-18l','may-xay-sinh-to-philips','bep-dien-tu-sunhouse',
        'am-sieu-toc-locklock-17l','may-hut-bui-cam-tay-deerma','ban-lam-viec-go-soi','ghe-an-boc-ni',
        'den-ban-led-chong-can','ke-sach-go-ba-tang','chao-chong-dinh-28cm');
    /* Natural-key corrections are deliberate and make a UTF-8 seed rerun repair prior mojibake. */
    UPDATE p SET product_name=v.product_name,brand=v.brand,short_description=v.short_description,
        description=v.description,seo_title=v.seo_title,seo_description=v.seo_description
    FROM dbo.products p
    JOIN (VALUES
        ('ban-lam-viec-go-soi',N'Bàn làm việc gỗ sồi',N'Nội Thất Hub',N'Bàn làm việc gọn gàng cho góc học tập.',N'Mặt bàn vân gỗ, khung chắc chắn và dễ lắp đặt.',N'Bàn làm việc gỗ sồi',N'Bàn làm việc gỗ sồi cho gia đình.'),
        ('ghe-an-boc-ni',N'Ghế ăn bọc nỉ',N'Nội Thất Hub',N'Ghế ăn bọc nỉ êm ái, khung gỗ.',N'Kiểu dáng tối giản phù hợp bàn ăn gia đình.',N'Ghế ăn bọc nỉ',N'Ghế ăn bọc nỉ hiện đại.'),
        ('den-ban-led-chong-can',N'Đèn bàn LED chống cận',N'Rạng Đông',N'Đèn bàn LED điều chỉnh độ sáng.',N'Ánh sáng dịu, tiết kiệm điện và phù hợp bàn học.',N'Đèn bàn LED chống cận',N'Đèn bàn LED bảo vệ mắt.'),
        ('ke-sach-go-ba-tang',N'Kệ sách gỗ ba tầng',N'Nội Thất Hub',N'Kệ sách ba tầng nhỏ gọn.',N'Kệ gỗ đa năng cho phòng khách hoặc góc làm việc.',N'Kệ sách gỗ ba tầng',N'Kệ sách gỗ nhỏ gọn.'),
        ('chao-chong-dinh-28cm',N'Chảo chống dính 28 cm',N'Sunhouse',N'Chảo chống dính dùng cho bếp gia đình.',N'Lòng chảo bền, tay cầm cách nhiệt và dễ vệ sinh.',N'Chảo chống dính 28 cm',N'Chảo chống dính Sunhouse 28 cm.')
    ) v(slug,product_name,brand,short_description,description,seo_title,seo_description)
        ON v.slug=p.slug AND p.store_id=@StoreId;
    UPDATE i SET alt_text=p.product_name
    FROM dbo.product_images i JOIN dbo.products p ON p.product_id=i.product_id
    WHERE p.store_id=@StoreId AND i.is_primary=1;
    UPDATE dbo.product_images SET image_url=REPLACE(image_url,'.jpg','.svg')
    WHERE product_id IN(@RiceCookerId,@BlenderId,@CooktopId,@KettleId,@VacuumId) AND is_primary=1;
    UPDATE dbo.product_categories SET status='INACTIVE'
    WHERE slug IN('vot-cau-long','giay-cau-long','qua-cau-long','day-cuoc-cau-long','quan-can-grip',
        'tui-bao-vot','quan-ao-cau-long','phu-kien-cau-long');
    UPDATE p SET status='HIDDEN'
    FROM dbo.products p
    JOIN dbo.stores s ON s.store_id=p.store_id
    JOIN dbo.product_categories c ON c.category_id=p.category_id
    WHERE s.slug IN('smash-sport','shuttle-corner') OR c.slug IN('vot-cau-long','giay-cau-long','qua-cau-long',
        'day-cuoc-cau-long','quan-can-grip','tui-bao-vot','quan-ao-cau-long','phu-kien-cau-long');
    UPDATE dbo.stores SET status='SUSPENDED' WHERE slug IN('smash-sport','shuttle-corner');

    IF EXISTS(SELECT 1 FROM dbo.products p JOIN dbo.stores s ON s.store_id=p.store_id JOIN dbo.product_categories c ON c.category_id=p.category_id
        WHERE p.status='ACTIVE' AND s.status='ACTIVE' AND c.status='ACTIVE'
          AND (s.slug IN('smash-sport','shuttle-corner') OR c.slug LIKE '%cau-long%'))
        THROW 50401,N'Badminton demo data is still public.',1;
    IF (SELECT COUNT(*) FROM dbo.products p JOIN dbo.stores s ON s.store_id=p.store_id JOIN dbo.product_categories c ON c.category_id=p.category_id
        WHERE p.status='ACTIVE' AND s.status='ACTIVE' AND c.status='ACTIVE') NOT BETWEEN 8 AND 12
        THROW 50402,N'Public household demo must contain 8 to 12 active products.',1;
    IF EXISTS(SELECT 1 FROM dbo.products p JOIN dbo.stores s ON s.store_id=p.store_id JOIN dbo.product_categories c ON c.category_id=p.category_id
        WHERE p.status='ACTIVE' AND s.status='ACTIVE' AND c.status='ACTIVE'
          AND NOT EXISTS(SELECT 1 FROM dbo.product_images i WHERE i.product_id=p.product_id AND i.is_primary=1
              AND NULLIF(i.image_url,'') IS NOT NULL AND NULLIF(i.alt_text,N'') IS NOT NULL))
        THROW 50403,N'Public demo product is missing a primary image or alt text.',1;

    IF NOT EXISTS (SELECT 1 FROM dbo.carts WHERE user_id=@CustomerId)
        INSERT dbo.carts(user_id) VALUES(@CustomerId);
    DECLARE @CartId BIGINT=(SELECT cart_id FROM dbo.carts WHERE user_id=@CustomerId);
    DECLARE @RiceSkuId BIGINT=(SELECT sku_id FROM dbo.product_skus WHERE sku_code='NC-SHARP-18L');
    DECLARE @KettleSkuId BIGINT=(SELECT sku_id FROM dbo.product_skus WHERE sku_code='AST-LOCKLOCK-17L');
    IF NOT EXISTS (SELECT 1 FROM dbo.cart_items WHERE cart_id=@CartId AND sku_id=@RiceSkuId)
        INSERT dbo.cart_items(cart_id,sku_id,quantity) VALUES(@CartId,@RiceSkuId,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.cart_items WHERE cart_id=@CartId AND sku_id=@KettleSkuId)
        INSERT dbo.cart_items(cart_id,sku_id,quantity) VALUES(@CartId,@KettleSkuId,2);

    IF NOT EXISTS (SELECT 1 FROM dbo.orders WHERE order_code='DH000001')
        INSERT dbo.orders(order_code,user_id,receiver_name,receiver_phone,shipping_address,subtotal,discount_amount,shipping_fee,total_amount,payment_method,payment_status,order_status,note,created_at)
        VALUES('DH000001',@CustomerId,N'Trần Thị Khách Hàng','0900000003',N'123 Nguyễn Trãi, Quận 1, TP.HCM',1319000,0,30000,1349000,'COD','PAID','DELIVERED',N'Giao giờ hành chính','2026-07-10');
    DECLARE @OrderId BIGINT=(SELECT order_id FROM dbo.orders WHERE order_code='DH000001');
    IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id=@OrderId AND sku_id=@RiceSkuId)
        INSERT dbo.order_items(order_id,sku_id,product_name,sku_code,unit_price,quantity) VALUES(@OrderId,@RiceSkuId,N'Nồi cơm điện Sharp 1.8L','NC-SHARP-18L',799000,1);
    IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id=@OrderId AND sku_id=@KettleSkuId)
        INSERT dbo.order_items(order_id,sku_id,product_name,sku_code,unit_price,quantity) VALUES(@OrderId,@KettleSkuId,N'Ấm siêu tốc Lock&Lock 1.7L','AST-LOCKLOCK-17L',520000,1);
    DECLARE @OrderItemId BIGINT=(SELECT order_item_id FROM dbo.order_items WHERE order_id=@OrderId AND sku_id=@RiceSkuId);
    IF NOT EXISTS (SELECT 1 FROM dbo.reviews WHERE user_id=@CustomerId AND order_item_id=@OrderItemId)
        INSERT dbo.reviews(user_id,product_id,order_item_id,rating,comment,status)
        VALUES(@CustomerId,@RiceCookerId,@OrderItemId,5,N'Sản phẩm sử dụng tốt, đóng gói cẩn thận.','VISIBLE');
    /* Repair demo snapshots created by an older non-UTF-8 sqlcmd invocation. */
    UPDATE dbo.orders SET receiver_name=N'Trần Thị Khách Hàng',shipping_address=N'123 Nguyễn Trãi, Quận 1, TP.HCM',note=N'Giao giờ hành chính'
    WHERE order_code='DH000001';
    UPDATE dbo.order_items SET product_name=N'Nồi cơm điện Sharp 1.8L' WHERE order_id=@OrderId AND sku_code='NC-SHARP-18L';
    UPDATE dbo.order_items SET product_name=N'Ấm siêu tốc Lock&Lock 1.7L' WHERE order_id=@OrderId AND sku_code='AST-LOCKLOCK-17L';
    UPDATE dbo.reviews SET comment=N'Sản phẩm sử dụng tốt, đóng gói cẩn thận.' WHERE user_id=@CustomerId AND order_item_id=@OrderItemId;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* ============================================================================
   6. VIEWS
   CREATE OR ALTER is safe because views do not own business data.
   ============================================================================ */
CREATE OR ALTER VIEW dbo.vw_product_catalog AS
SELECT p.product_id,p.product_name,p.slug,p.brand,p.short_description,
       c.category_id,c.category_name,s.store_id,s.store_name,
       sku.sku_id,sku.sku_code,sku.price,sku.sale_price,sku.sale_start,sku.sale_end,
       CASE WHEN sku.sale_price IS NOT NULL
                  AND (sku.sale_start IS NULL OR SYSDATETIME() >= sku.sale_start)
                  AND (sku.sale_end IS NULL OR SYSDATETIME() <= sku.sale_end)
            THEN sku.sale_price ELSE sku.price END AS display_price,
       CASE WHEN sku.sale_price IS NOT NULL AND sku.price > 0
                  AND (sku.sale_start IS NULL OR SYSDATETIME() >= sku.sale_start)
                  AND (sku.sale_end IS NULL OR SYSDATETIME() <= sku.sale_end)
            THEN CAST(ROUND((sku.price-sku.sale_price)*100.0/sku.price,0) AS INT) ELSE 0 END AS discount_percent,
       sku.stock_quantity,img.image_url,img.alt_text,
       ISNULL(rv.average_rating,0) AS average_rating,ISNULL(rv.review_count,0) AS review_count
FROM dbo.products p
JOIN dbo.product_categories c ON c.category_id=p.category_id
JOIN dbo.stores s ON s.store_id=p.store_id
JOIN dbo.product_skus sku ON sku.product_id=p.product_id
LEFT JOIN dbo.product_images img ON img.product_id=p.product_id AND img.is_primary=1
LEFT JOIN
(
    SELECT product_id,AVG(CAST(rating AS DECIMAL(3,2))) AS average_rating,COUNT(*) AS review_count
    FROM dbo.reviews WHERE status='VISIBLE' GROUP BY product_id
) rv ON rv.product_id=p.product_id
WHERE p.status='ACTIVE' AND c.status='ACTIVE' AND s.status='ACTIVE' AND sku.status='ACTIVE';
GO

CREATE OR ALTER VIEW dbo.vw_best_selling_products AS
SELECT oi.sku_id,oi.product_name,SUM(oi.quantity) AS total_quantity_sold,SUM(oi.line_total) AS total_revenue
FROM dbo.order_items oi JOIN dbo.orders o ON o.order_id=oi.order_id
WHERE o.order_status='DELIVERED'
GROUP BY oi.sku_id,oi.product_name;
GO

CREATE OR ALTER VIEW dbo.vw_revenue_by_month AS
SELECT YEAR(created_at) AS revenue_year,MONTH(created_at) AS revenue_month,
       COUNT(*) AS total_orders,SUM(total_amount) AS total_revenue
FROM dbo.orders WHERE order_status='DELIVERED'
GROUP BY YEAR(created_at),MONTH(created_at);
GO

/* ============================================================================
   7. STORED PROCEDURES
   ============================================================================ */
CREATE OR ALTER PROCEDURE dbo.sp_UpdateSkuPrice
    @SkuId BIGINT,@Price DECIMAL(18,2),@SalePrice DECIMAL(18,2)=NULL,
    @SaleStart DATETIME2=NULL,@SaleEnd DATETIME2=NULL,
    @StockQuantity INT=NULL,@Status VARCHAR(20)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.product_skus WHERE sku_id=@SkuId) THROW 50001,N'SKU không tồn tại.',1;
    IF @Price<=0 THROW 50002,N'Giá bán phải lớn hơn 0.',1;
    IF @SalePrice IS NOT NULL AND (@SalePrice<=0 OR @SalePrice>@Price) THROW 50003,N'Giá khuyến mãi không hợp lệ.',1;
    IF @SaleEnd IS NOT NULL AND @SaleStart IS NOT NULL AND @SaleEnd<=@SaleStart THROW 50004,N'Thời gian khuyến mãi không hợp lệ.',1;
    IF @StockQuantity IS NOT NULL AND @StockQuantity<0 THROW 50005,N'Tồn kho không được nhỏ hơn 0.',1;
    IF @Status IS NOT NULL AND @Status NOT IN('ACTIVE','INACTIVE') THROW 50006,N'Trạng thái SKU không hợp lệ.',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        UPDATE dbo.product_skus
        SET price=@Price,sale_price=@SalePrice,sale_start=@SaleStart,sale_end=@SaleEnd,
            stock_quantity=COALESCE(@StockQuantity,stock_quantity),status=COALESCE(@Status,status)
        WHERE sku_id=@SkuId;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateOrderStatus
    @OrderId BIGINT,@NewStatus VARCHAR(30),@PaymentStatus VARCHAR(20)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    THROW 50014,N'Không được cập nhật trạng thái trực tiếp. Hãy dùng marketplace workflow service.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.orders WHERE order_id=@OrderId) THROW 50011,N'Đơn hàng không tồn tại.',1;
    IF @NewStatus NOT IN('PENDING','CONFIRMED','PROCESSING','SHIPPING','DELIVERED','CANCELLED') THROW 50012,N'Trạng thái đơn hàng không hợp lệ.',1;
    IF @PaymentStatus IS NOT NULL AND @PaymentStatus NOT IN('UNPAID','PAID','FAILED') THROW 50013,N'Trạng thái thanh toán không hợp lệ.',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        UPDATE dbo.orders
        SET order_status=@NewStatus,payment_status=COALESCE(@PaymentStatus,payment_status),updated_at=SYSDATETIME()
        WHERE order_id=@OrderId;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* ============================================================================
   8. VERIFICATION QUERIES
   These are read-only and return compact deployment evidence.
   ============================================================================ */
SELECT DB_NAME() AS database_name,
       (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo')) AS table_count,
       (SELECT COUNT(*) FROM sys.views WHERE schema_id=SCHEMA_ID(N'dbo')) AS view_count,
       (SELECT COUNT(*) FROM sys.procedures WHERE schema_id=SCHEMA_ID(N'dbo')) AS procedure_count;

SELECT N'users' AS entity,COUNT(*) AS row_count FROM dbo.users
UNION ALL SELECT N'stores',COUNT(*) FROM dbo.stores
UNION ALL SELECT N'product_categories',COUNT(*) FROM dbo.product_categories
UNION ALL SELECT N'products',COUNT(*) FROM dbo.products
UNION ALL SELECT N'product_skus',COUNT(*) FROM dbo.product_skus
UNION ALL SELECT N'orders',COUNT(*) FROM dbo.orders
UNION ALL SELECT N'order_items',COUNT(*) FROM dbo.order_items
UNION ALL SELECT N'reviews',COUNT(*) FROM dbo.reviews;

IF EXISTS
(
    SELECT email FROM dbo.users GROUP BY email HAVING COUNT(*)>1
    UNION ALL SELECT slug FROM dbo.stores GROUP BY slug HAVING COUNT(*)>1
    UNION ALL SELECT sku_code FROM dbo.product_skus GROUP BY sku_code HAVING COUNT(*)>1
    UNION ALL SELECT order_code FROM dbo.orders GROUP BY order_code HAVING COUNT(*)>1
)
    THROW 50100,N'Verification failed: duplicate natural keys detected.',1;

SELECT TOP (20) * FROM dbo.vw_product_catalog ORDER BY product_id;
SELECT TOP (20) * FROM dbo.vw_best_selling_products ORDER BY total_quantity_sold DESC;
SELECT TOP (20) * FROM dbo.vw_revenue_by_month ORDER BY revenue_year,revenue_month;
GO

/* ============================================================================
   9. SAFE LEGACY -> CANONICAL MARKETPLACE UPGRADE
   ============================================================================ */
SET XACT_ABORT ON;

/* Stage A: normalize legacy key names with all audited dependencies. */
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.shipping_providers',N'carrier_user_id') IS NOT NULL
       AND COL_LENGTH(N'dbo.shipping_providers',N'owner_user_id') IS NULL
    BEGIN
        EXEC sys.sp_executesql N'
            IF EXISTS(SELECT 1 FROM dbo.shipping_providers WHERE carrier_user_id IS NULL)
                THROW 50201,N''Cannot canonicalize provider: carrier_user_id contains NULL.'',1;
            IF EXISTS(SELECT 1 FROM dbo.shipping_providers p LEFT JOIN dbo.users u ON u.user_id=p.carrier_user_id WHERE u.user_id IS NULL)
                THROW 50202,N''Cannot canonicalize provider: orphan carrier owner.'',1;';
        IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipping_providers_carrier_user')
            ALTER TABLE dbo.shipping_providers DROP CONSTRAINT FK_shipping_providers_carrier_user;
        IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.shipping_providers') AND name=N'UX_shipping_providers_carrier_user')
            DROP INDEX UX_shipping_providers_carrier_user ON dbo.shipping_providers;
        EXEC sys.sp_rename N'dbo.shipping_providers.carrier_user_id',N'owner_user_id',N'COLUMN';
    END;

    IF COL_LENGTH(N'dbo.shipping_providers',N'code') IS NOT NULL
       AND COL_LENGTH(N'dbo.shipping_providers',N'slug') IS NULL
    BEGIN
        IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.shipping_providers') AND name=N'UX_shipping_providers_code')
            ALTER TABLE dbo.shipping_providers DROP CONSTRAINT UX_shipping_providers_code;
        EXEC sys.sp_rename N'dbo.shipping_providers.code',N'slug',N'COLUMN';
    END;
    IF COL_LENGTH(N'dbo.shipping_providers',N'contact_phone') IS NOT NULL
       AND COL_LENGTH(N'dbo.shipping_providers',N'phone') IS NULL
        EXEC sys.sp_rename N'dbo.shipping_providers.contact_phone',N'phone',N'COLUMN';

    IF COL_LENGTH(N'dbo.shipping_services',N'shipping_service_id') IS NOT NULL
       AND COL_LENGTH(N'dbo.shipping_services',N'service_id') IS NULL
    BEGIN
        IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_store_shipping_services_services')
            ALTER TABLE dbo.store_shipping_services DROP CONSTRAINT FK_store_shipping_services_services;
        IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_store_orders_shipping_services')
            ALTER TABLE dbo.store_orders DROP CONSTRAINT FK_store_orders_shipping_services;
        IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipments_services')
            ALTER TABLE dbo.shipments DROP CONSTRAINT FK_shipments_services;
        IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name=N'PK_store_shipping_services')
            ALTER TABLE dbo.store_shipping_services DROP CONSTRAINT PK_store_shipping_services;
        IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name=N'UX_shipping_services_provider_code')
            ALTER TABLE dbo.shipping_services DROP CONSTRAINT UX_shipping_services_provider_code;
        IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name=N'PK_shipping_services')
            ALTER TABLE dbo.shipping_services DROP CONSTRAINT PK_shipping_services;
        EXEC sys.sp_rename N'dbo.shipping_services.shipping_service_id',N'service_id',N'COLUMN';
        EXEC sys.sp_rename N'dbo.store_shipping_services.shipping_service_id',N'service_id',N'COLUMN';
        EXEC sys.sp_rename N'dbo.store_orders.shipping_service_id',N'service_id',N'COLUMN';
        EXEC sys.sp_rename N'dbo.shipments.shipping_service_id',N'service_id',N'COLUMN';
    END;

    IF COL_LENGTH(N'dbo.shipping_services',N'code') IS NOT NULL
       AND COL_LENGTH(N'dbo.shipping_services',N'service_code') IS NULL
        EXEC sys.sp_rename N'dbo.shipping_services.code',N'service_code',N'COLUMN';
    IF COL_LENGTH(N'dbo.shipments',N'shipped_at') IS NOT NULL
       AND COL_LENGTH(N'dbo.shipments',N'picked_up_at') IS NULL
        EXEC sys.sp_rename N'dbo.shipments.shipped_at',N'picked_up_at',N'COLUMN';
    IF COL_LENGTH(N'dbo.order_status_histories',N'changed_at') IS NOT NULL
       AND COL_LENGTH(N'dbo.order_status_histories',N'created_at') IS NULL
        EXEC sys.sp_rename N'dbo.order_status_histories.changed_at',N'created_at',N'COLUMN';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Stage B: create canonical objects and add columns. */
BEGIN TRY
    BEGIN TRANSACTION;
    /* Marketplace tables introduced after the original single-store schema. */
    IF COL_LENGTH(N'dbo.order_items',N'store_order_id') IS NULL
        ALTER TABLE dbo.order_items ADD store_order_id BIGINT NULL;
    IF OBJECT_ID(N'dbo.store_orders',N'U') IS NULL
        CREATE TABLE dbo.store_orders
        (store_order_id BIGINT IDENTITY PRIMARY KEY, order_id BIGINT NOT NULL, store_id BIGINT NOT NULL,
         store_order_code VARCHAR(40) NOT NULL, subtotal DECIMAL(18,2) NOT NULL,
         discount_amount DECIMAL(18,2) NOT NULL DEFAULT(0), shipping_fee DECIMAL(18,2) NOT NULL DEFAULT(0),
         total_amount DECIMAL(18,2) NOT NULL, status VARCHAR(30) NOT NULL DEFAULT('PENDING'), service_id BIGINT NULL,
         created_at DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), updated_at DATETIME2 NULL, row_version ROWVERSION);
    IF OBJECT_ID(N'dbo.shipments',N'U') IS NULL
        CREATE TABLE dbo.shipments
        (shipment_id BIGINT IDENTITY PRIMARY KEY, store_order_id BIGINT NOT NULL, provider_id BIGINT NOT NULL,
         service_id BIGINT NOT NULL, shipping_fee DECIMAL(18,2) NOT NULL DEFAULT(0), tracking_code VARCHAR(100) NOT NULL,
         status VARCHAR(30) NOT NULL DEFAULT('CREATED'), pickup_address NVARCHAR(500) NOT NULL,
         delivery_address NVARCHAR(500) NOT NULL, estimated_delivery_at DATETIME2 NULL, picked_up_at DATETIME2 NULL,
         delivered_at DATETIME2 NULL, created_at DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), updated_at DATETIME2 NULL,
         row_version ROWVERSION);
    IF OBJECT_ID(N'dbo.order_status_histories',N'U') IS NULL
        CREATE TABLE dbo.order_status_histories
        (history_id BIGINT IDENTITY PRIMARY KEY, store_order_id BIGINT NOT NULL, old_status VARCHAR(30) NULL,
         new_status VARCHAR(30) NOT NULL, changed_by_user_id BIGINT NULL, note NVARCHAR(500) NULL,
         created_at DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()));

    INSERT dbo.store_orders(order_id,store_id,store_order_code,subtotal,discount_amount,shipping_fee,total_amount,status)
    SELECT o.order_id, s.store_id, CONCAT(o.order_code,'-1'), o.subtotal, o.discount_amount, o.shipping_fee,
           o.total_amount, CASE WHEN o.order_status='CANCELLED' THEN 'CANCELLED' ELSE 'PENDING' END
    FROM dbo.orders o CROSS APPLY (SELECT TOP 1 store_id FROM dbo.stores WHERE status='ACTIVE' ORDER BY store_id) s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.store_orders so WHERE so.order_id=o.order_id);
    UPDATE oi SET store_order_id=so.store_order_id
    FROM dbo.order_items oi JOIN dbo.store_orders so ON so.order_id=oi.order_id
    WHERE oi.store_order_id IS NULL;

    IF OBJECT_ID(N'dbo.shipping_providers',N'U') IS NULL
        CREATE TABLE dbo.shipping_providers(provider_id BIGINT IDENTITY PRIMARY KEY,owner_user_id BIGINT NOT NULL,provider_name NVARCHAR(200) NOT NULL,slug VARCHAR(220) NOT NULL,phone VARCHAR(30) NULL,email VARCHAR(255) NULL,description NVARCHAR(MAX) NULL,status VARCHAR(20) NOT NULL DEFAULT('PENDING'),created_at DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),updated_at DATETIME2 NULL);
    IF OBJECT_ID(N'dbo.shipping_services',N'U') IS NULL
        CREATE TABLE dbo.shipping_services(service_id BIGINT IDENTITY PRIMARY KEY,provider_id BIGINT NOT NULL,service_code VARCHAR(80) NOT NULL,service_name NVARCHAR(200) NOT NULL,base_fee DECIMAL(18,2) NOT NULL,estimated_min_days INT NOT NULL,estimated_max_days INT NOT NULL,max_weight DECIMAL(18,3) NULL,status VARCHAR(20) NOT NULL DEFAULT('ACTIVE'));
    IF OBJECT_ID(N'dbo.store_shipping_services',N'U') IS NULL
        CREATE TABLE dbo.store_shipping_services(store_id BIGINT NOT NULL,service_id BIGINT NOT NULL,is_enabled BIT NOT NULL DEFAULT(1),fee_override DECIMAL(18,2) NULL,status VARCHAR(20) NOT NULL DEFAULT('ACTIVE'));

    IF COL_LENGTH(N'dbo.shipping_providers',N'email') IS NULL ALTER TABLE dbo.shipping_providers ADD email VARCHAR(255) NULL;
    IF COL_LENGTH(N'dbo.shipping_providers',N'description') IS NULL ALTER TABLE dbo.shipping_providers ADD description NVARCHAR(MAX) NULL;
    IF COL_LENGTH(N'dbo.shipping_providers',N'created_at') IS NULL ALTER TABLE dbo.shipping_providers ADD created_at DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.shipping_providers',N'updated_at') IS NULL ALTER TABLE dbo.shipping_providers ADD updated_at DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.shipping_services',N'estimated_min_days') IS NULL ALTER TABLE dbo.shipping_services ADD estimated_min_days INT NULL;
    IF COL_LENGTH(N'dbo.shipping_services',N'estimated_max_days') IS NULL ALTER TABLE dbo.shipping_services ADD estimated_max_days INT NULL;
    IF COL_LENGTH(N'dbo.shipping_services',N'max_weight') IS NULL ALTER TABLE dbo.shipping_services ADD max_weight DECIMAL(18,3) NULL;
    IF COL_LENGTH(N'dbo.store_shipping_services',N'status') IS NULL ALTER TABLE dbo.store_shipping_services ADD status VARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.store_orders',N'row_version') IS NULL ALTER TABLE dbo.store_orders ADD row_version ROWVERSION;
    IF COL_LENGTH(N'dbo.order_items',N'store_name_snapshot') IS NULL ALTER TABLE dbo.order_items ADD store_name_snapshot NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.shipments',N'shipping_fee') IS NULL ALTER TABLE dbo.shipments ADD shipping_fee DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.shipments',N'pickup_address') IS NULL ALTER TABLE dbo.shipments ADD pickup_address NVARCHAR(500) NULL;
    IF COL_LENGTH(N'dbo.shipments',N'delivery_address') IS NULL ALTER TABLE dbo.shipments ADD delivery_address NVARCHAR(500) NULL;
    IF COL_LENGTH(N'dbo.shipments',N'estimated_delivery_at') IS NULL ALTER TABLE dbo.shipments ADD estimated_delivery_at DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.shipments',N'created_at') IS NULL ALTER TABLE dbo.shipments ADD created_at DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.shipments',N'row_version') IS NULL ALTER TABLE dbo.shipments ADD row_version ROWVERSION;

    IF OBJECT_ID(N'dbo.shipping_rate_rules',N'U') IS NULL
        CREATE TABLE dbo.shipping_rate_rules(rule_id BIGINT IDENTITY PRIMARY KEY,service_id BIGINT NOT NULL,origin_area NVARCHAR(120) NOT NULL,destination_area NVARCHAR(120) NOT NULL,min_weight DECIMAL(18,3) NOT NULL,max_weight DECIMAL(18,3) NOT NULL,fee DECIMAL(18,2) NOT NULL,extra_fee_per_kg DECIMAL(18,2) NOT NULL DEFAULT(0),supports_cod BIT NOT NULL DEFAULT(1),status VARCHAR(20) NOT NULL DEFAULT('ACTIVE'));
    IF OBJECT_ID(N'dbo.shipping_quotes',N'U') IS NULL
        CREATE TABLE dbo.shipping_quotes(quote_id UNIQUEIDENTIFIER PRIMARY KEY,provider_id BIGINT NOT NULL,service_id BIGINT NOT NULL,fee DECIMAL(18,2) NOT NULL,estimated_min_days INT NOT NULL,estimated_max_days INT NOT NULL,expires_at DATETIME2 NOT NULL,status VARCHAR(20) NOT NULL DEFAULT('ACTIVE'),selected_at DATETIME2 NULL);
    IF OBJECT_ID(N'dbo.shipment_status_histories',N'U') IS NULL
        CREATE TABLE dbo.shipment_status_histories(history_id BIGINT IDENTITY PRIMARY KEY,shipment_id BIGINT NOT NULL,old_status VARCHAR(30) NULL,new_status VARCHAR(30) NOT NULL,changed_by_user_id BIGINT NOT NULL,note NVARCHAR(500) NULL,created_at DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()));
    IF OBJECT_ID(N'dbo.audit_logs',N'U') IS NULL
        CREATE TABLE dbo.audit_logs(audit_id BIGINT IDENTITY PRIMARY KEY,actor_user_id BIGINT NULL,action VARCHAR(100) NOT NULL,entity_name VARCHAR(100) NOT NULL,entity_id VARCHAR(100) NULL,before_json NVARCHAR(MAX) NULL,after_json NVARCHAR(MAX) NULL,ip_address VARCHAR(64) NULL,created_at DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()));
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Stage C: backfill, validate, and rebuild canonical constraints/indexes. */
BEGIN TRY
    BEGIN TRANSACTION;
    UPDATE dbo.shipping_providers SET status='SUSPENDED' WHERE status='INACTIVE';
    UPDATE dbo.shipping_providers SET created_at=COALESCE(created_at,SYSUTCDATETIME());
    IF COL_LENGTH(N'dbo.shipping_services',N'estimated_days') IS NOT NULL
        EXEC sys.sp_executesql N'UPDATE dbo.shipping_services SET estimated_min_days=COALESCE(estimated_min_days,estimated_days),estimated_max_days=COALESCE(estimated_max_days,estimated_days);';
    UPDATE dbo.shipping_services SET estimated_min_days=COALESCE(estimated_min_days,1),estimated_max_days=COALESCE(estimated_max_days,estimated_min_days,1);
    UPDATE dbo.store_shipping_services SET status=CASE WHEN is_enabled=1 THEN 'ACTIVE' ELSE 'INACTIVE' END WHERE status IS NULL;
    UPDATE oi SET store_name_snapshot=COALESCE(oi.store_name_snapshot,s.store_name)
    FROM dbo.order_items oi JOIN dbo.product_skus sku ON sku.sku_id=oi.sku_id JOIN dbo.products p ON p.product_id=sku.product_id JOIN dbo.stores s ON s.store_id=p.store_id;
    UPDATE sh SET shipping_fee=COALESCE(sh.shipping_fee,so.shipping_fee),pickup_address=COALESCE(sh.pickup_address,st.store_name),delivery_address=COALESCE(sh.delivery_address,o.shipping_address),created_at=COALESCE(sh.created_at,so.created_at)
    FROM dbo.shipments sh JOIN dbo.store_orders so ON so.store_order_id=sh.store_order_id JOIN dbo.stores st ON st.store_id=so.store_id JOIN dbo.orders o ON o.order_id=so.order_id;

    /* Keep legacy marketplace rows consistent with a cancelled parent order. */
    UPDATE so
    SET status='CANCELLED',updated_at=COALESCE(o.updated_at,SYSUTCDATETIME())
    FROM dbo.store_orders so
    JOIN dbo.orders o ON o.order_id=so.order_id
    WHERE o.order_status='CANCELLED' AND so.status<>'CANCELLED';
    UPDATE sh
    SET status='CANCELLED',updated_at=COALESCE(o.updated_at,SYSUTCDATETIME())
    FROM dbo.shipments sh
    JOIN dbo.store_orders so ON so.store_order_id=sh.store_order_id
    JOIN dbo.orders o ON o.order_id=so.order_id
    WHERE o.order_status='CANCELLED' AND sh.status<>'CANCELLED';
    UPDATE dbo.orders
    SET shipping_fee=0,total_amount=0
    WHERE order_status='CANCELLED' AND (shipping_fee<>0 OR total_amount<>0);

    /* A fresh marketplace must have at least one usable checkout service. */
    DECLARE @DefaultProviderId BIGINT;
    SELECT @DefaultProviderId=provider_id FROM dbo.shipping_providers WHERE slug='noi-that-hub-delivery';
    IF @DefaultProviderId IS NULL
    BEGIN
        DECLARE @DefaultProviderOwnerId BIGINT=(
            SELECT TOP 1 user_id FROM dbo.users
            WHERE status='ACTIVE'
            ORDER BY CASE role WHEN 'CARRIER' THEN 0 WHEN 'ADMIN' THEN 1 ELSE 2 END,user_id);
        IF @DefaultProviderOwnerId IS NULL THROW 50214,N'Cannot seed checkout shipping without an active user.',1;
        INSERT dbo.shipping_providers(owner_user_id,provider_name,slug,phone,email,description,status)
        VALUES(@DefaultProviderOwnerId,N'Nội Thất Hub Delivery','noi-that-hub-delivery','19001000','delivery@noithathub.vn',N'Dịch vụ giao hàng mặc định của marketplace.','ACTIVE');
        SET @DefaultProviderId=SCOPE_IDENTITY();
    END;
    UPDATE dbo.shipping_providers
    SET provider_name=N'Nội Thất Hub Delivery',description=N'Dịch vụ giao hàng mặc định của marketplace.',status='ACTIVE'
    WHERE provider_id=@DefaultProviderId;

    DECLARE @DefaultServiceId BIGINT;
    SELECT @DefaultServiceId=service_id FROM dbo.shipping_services
    WHERE provider_id=@DefaultProviderId AND service_code='STANDARD';
    IF @DefaultServiceId IS NULL
    BEGIN
        INSERT dbo.shipping_services(provider_id,service_code,service_name,base_fee,estimated_min_days,estimated_max_days,max_weight,status)
        VALUES(@DefaultProviderId,'STANDARD',N'Giao hàng tiêu chuẩn',30000,2,5,50,'ACTIVE');
        SET @DefaultServiceId=SCOPE_IDENTITY();
    END;
    UPDATE dbo.shipping_services
    SET service_name=N'Giao hàng tiêu chuẩn',base_fee=30000,estimated_min_days=2,estimated_max_days=5,max_weight=50,status='ACTIVE'
    WHERE service_id=@DefaultServiceId;

    ;WITH DuplicateDefaultRules AS
    (
        SELECT rule_id,ROW_NUMBER() OVER(ORDER BY rule_id) AS duplicate_number
        FROM dbo.shipping_rate_rules
        WHERE service_id=@DefaultServiceId AND min_weight=0 AND max_weight=50 AND fee=30000 AND extra_fee_per_kg=5000
    )
    DELETE FROM DuplicateDefaultRules WHERE duplicate_number>1;
    IF NOT EXISTS(SELECT 1 FROM dbo.shipping_rate_rules WHERE service_id=@DefaultServiceId AND min_weight=0 AND max_weight=50 AND fee=30000 AND extra_fee_per_kg=5000)
        INSERT dbo.shipping_rate_rules(service_id,origin_area,destination_area,min_weight,max_weight,fee,extra_fee_per_kg,supports_cod,status)
        VALUES(@DefaultServiceId,N'Toàn quốc',N'Toàn quốc',0,50,30000,5000,1,'ACTIVE');
    UPDATE dbo.shipping_rate_rules
    SET origin_area=N'Toàn quốc',destination_area=N'Toàn quốc',supports_cod=1,status='ACTIVE'
    WHERE service_id=@DefaultServiceId AND min_weight=0 AND max_weight=50 AND fee=30000 AND extra_fee_per_kg=5000;

    INSERT dbo.store_shipping_services(store_id,service_id,is_enabled,fee_override,status)
    SELECT st.store_id,sv.service_id,1,NULL,'ACTIVE'
    FROM dbo.stores st CROSS JOIN dbo.shipping_services sv
    WHERE st.status='ACTIVE' AND sv.status='ACTIVE'
      AND NOT EXISTS(SELECT 1 FROM dbo.store_shipping_services x WHERE x.store_id=st.store_id AND x.service_id=sv.service_id);

    IF EXISTS(SELECT 1 FROM dbo.shipping_providers WHERE owner_user_id IS NULL) THROW 50211,N'Provider owner backfill failed.',1;
    IF EXISTS(SELECT 1 FROM dbo.shipping_services WHERE estimated_min_days IS NULL OR estimated_max_days IS NULL) THROW 50212,N'Service ETA backfill failed.',1;
    IF EXISTS(SELECT 1 FROM dbo.shipments WHERE shipping_fee IS NULL OR pickup_address IS NULL OR delivery_address IS NULL OR created_at IS NULL) THROW 50213,N'Shipment backfill failed.',1;

    ALTER TABLE dbo.shipping_providers ALTER COLUMN owner_user_id BIGINT NOT NULL;
    ALTER TABLE dbo.shipping_providers ALTER COLUMN slug VARCHAR(220) NOT NULL;
    ALTER TABLE dbo.shipping_providers ALTER COLUMN phone VARCHAR(30) NULL;
    ALTER TABLE dbo.shipping_providers ALTER COLUMN created_at DATETIME2 NOT NULL;
    ALTER TABLE dbo.shipping_services ALTER COLUMN service_code VARCHAR(80) NOT NULL;
    ALTER TABLE dbo.shipping_services ALTER COLUMN service_name NVARCHAR(200) NOT NULL;
    ALTER TABLE dbo.shipping_services ALTER COLUMN estimated_min_days INT NOT NULL;
    ALTER TABLE dbo.shipping_services ALTER COLUMN estimated_max_days INT NOT NULL;
    ALTER TABLE dbo.store_shipping_services ALTER COLUMN status VARCHAR(20) NOT NULL;
    ALTER TABLE dbo.shipments ALTER COLUMN tracking_code VARCHAR(100) NOT NULL;
    ALTER TABLE dbo.shipments ALTER COLUMN shipping_fee DECIMAL(18,2) NOT NULL;
    ALTER TABLE dbo.shipments ALTER COLUMN pickup_address NVARCHAR(500) NOT NULL;
    ALTER TABLE dbo.shipments ALTER COLUMN delivery_address NVARCHAR(500) NOT NULL;
    ALTER TABLE dbo.shipments ALTER COLUMN created_at DATETIME2 NOT NULL;

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.shipping_services') AND type='PK') ALTER TABLE dbo.shipping_services ADD CONSTRAINT PK_shipping_services PRIMARY KEY(service_id);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name=N'PK_store_shipping_services') ALTER TABLE dbo.store_shipping_services ADD CONSTRAINT PK_store_shipping_services PRIMARY KEY(store_id,service_id);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name=N'UQ_shipping_providers_slug') ALTER TABLE dbo.shipping_providers ADD CONSTRAINT UQ_shipping_providers_slug UNIQUE(slug);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name=N'UQ_shipping_services_code') ALTER TABLE dbo.shipping_services ADD CONSTRAINT UQ_shipping_services_code UNIQUE(provider_id,service_code);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipping_providers_users') ALTER TABLE dbo.shipping_providers WITH CHECK ADD CONSTRAINT FK_shipping_providers_users FOREIGN KEY(owner_user_id) REFERENCES dbo.users(user_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipping_services_providers') ALTER TABLE dbo.shipping_services WITH CHECK ADD CONSTRAINT FK_shipping_services_providers FOREIGN KEY(provider_id) REFERENCES dbo.shipping_providers(provider_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_store_shipping_services_stores') ALTER TABLE dbo.store_shipping_services WITH CHECK ADD CONSTRAINT FK_store_shipping_services_stores FOREIGN KEY(store_id) REFERENCES dbo.stores(store_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_store_shipping_services_services') ALTER TABLE dbo.store_shipping_services WITH CHECK ADD CONSTRAINT FK_store_shipping_services_services FOREIGN KEY(service_id) REFERENCES dbo.shipping_services(service_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_store_orders_shipping_services') ALTER TABLE dbo.store_orders WITH CHECK ADD CONSTRAINT FK_store_orders_shipping_services FOREIGN KEY(service_id) REFERENCES dbo.shipping_services(service_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipments_services') ALTER TABLE dbo.shipments WITH CHECK ADD CONSTRAINT FK_shipments_services FOREIGN KEY(service_id) REFERENCES dbo.shipping_services(service_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipping_rate_rules_services') ALTER TABLE dbo.shipping_rate_rules WITH CHECK ADD CONSTRAINT FK_shipping_rate_rules_services FOREIGN KEY(service_id) REFERENCES dbo.shipping_services(service_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipping_quotes_provider') ALTER TABLE dbo.shipping_quotes WITH CHECK ADD CONSTRAINT FK_shipping_quotes_provider FOREIGN KEY(provider_id) REFERENCES dbo.shipping_providers(provider_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipping_quotes_service') ALTER TABLE dbo.shipping_quotes WITH CHECK ADD CONSTRAINT FK_shipping_quotes_service FOREIGN KEY(service_id) REFERENCES dbo.shipping_services(service_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipment_histories_shipments') ALTER TABLE dbo.shipment_status_histories WITH CHECK ADD CONSTRAINT FK_shipment_histories_shipments FOREIGN KEY(shipment_id) REFERENCES dbo.shipments(shipment_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_shipment_histories_users') ALTER TABLE dbo.shipment_status_histories WITH CHECK ADD CONSTRAINT FK_shipment_histories_users FOREIGN KEY(changed_by_user_id) REFERENCES dbo.users(user_id);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_audit_logs_users') ALTER TABLE dbo.audit_logs WITH CHECK ADD CONSTRAINT FK_audit_logs_users FOREIGN KEY(actor_user_id) REFERENCES dbo.users(user_id);

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_shipping_providers_status') ALTER TABLE dbo.shipping_providers DROP CONSTRAINT CK_shipping_providers_status;
    ALTER TABLE dbo.shipping_providers WITH CHECK ADD CONSTRAINT CK_shipping_providers_status CHECK(status IN('PENDING','ACTIVE','SUSPENDED'));
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_shipping_services_fee') ALTER TABLE dbo.shipping_services DROP CONSTRAINT CK_shipping_services_fee;
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_shipping_services_days') ALTER TABLE dbo.shipping_services DROP CONSTRAINT CK_shipping_services_days;
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_shipping_services_status') ALTER TABLE dbo.shipping_services DROP CONSTRAINT CK_shipping_services_status;
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_shipping_services_values') ALTER TABLE dbo.shipping_services DROP CONSTRAINT CK_shipping_services_values;
    ALTER TABLE dbo.shipping_services WITH CHECK ADD CONSTRAINT CK_shipping_services_values CHECK(base_fee>=0 AND estimated_min_days>0 AND estimated_max_days>=estimated_min_days AND (max_weight IS NULL OR max_weight>0));
    ALTER TABLE dbo.shipping_services WITH CHECK ADD CONSTRAINT CK_shipping_services_status CHECK(status IN('ACTIVE','INACTIVE'));
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_shipments_status') ALTER TABLE dbo.shipments DROP CONSTRAINT CK_shipments_status;
    ALTER TABLE dbo.shipments WITH CHECK ADD CONSTRAINT CK_shipments_status CHECK(status IN('CREATED','READY_FOR_PICKUP','PICKED_UP','IN_TRANSIT','DELIVERED','FAILED','CANCELLED'));
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_store_shipping_services_values') ALTER TABLE dbo.store_shipping_services DROP CONSTRAINT CK_store_shipping_services_values;
    ALTER TABLE dbo.store_shipping_services WITH CHECK ADD CONSTRAINT CK_store_shipping_services_values CHECK(status IN('ACTIVE','INACTIVE') AND (fee_override IS NULL OR fee_override>=0));
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_shipping_rate_rules_values') ALTER TABLE dbo.shipping_rate_rules DROP CONSTRAINT CK_shipping_rate_rules_values;
    ALTER TABLE dbo.shipping_rate_rules WITH CHECK ADD CONSTRAINT CK_shipping_rate_rules_values CHECK(status IN('ACTIVE','INACTIVE') AND min_weight>=0 AND max_weight>=min_weight AND fee>=0 AND extra_fee_per_kg>=0);
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_shipping_quotes_values') ALTER TABLE dbo.shipping_quotes DROP CONSTRAINT CK_shipping_quotes_values;
    ALTER TABLE dbo.shipping_quotes WITH CHECK ADD CONSTRAINT CK_shipping_quotes_values CHECK(status IN('ACTIVE','SELECTED','EXPIRED','CANCELLED') AND fee>=0 AND estimated_min_days>0 AND estimated_max_days>=estimated_min_days);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.shipping_providers') AND name=N'UX_shipping_providers_owner') CREATE UNIQUE INDEX UX_shipping_providers_owner ON dbo.shipping_providers(owner_user_id);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.store_shipping_services') AND name=N'UX_store_shipping_services_store_service') CREATE UNIQUE INDEX UX_store_shipping_services_store_service ON dbo.store_shipping_services(store_id,service_id);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.shipments') AND name=N'IX_shipments_provider_status') CREATE INDEX IX_shipments_provider_status ON dbo.shipments(provider_id,status,created_at DESC);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Canonical verification: fail deployment with a specific invariant. */
IF COL_LENGTH(N'dbo.shipping_providers',N'owner_user_id') IS NULL THROW 50301,N'Missing shipping_providers.owner_user_id.',1;
IF COL_LENGTH(N'dbo.shipping_services',N'service_id') IS NULL THROW 50302,N'Missing shipping_services.service_id.',1;
IF COL_LENGTH(N'dbo.store_shipping_services',N'service_id') IS NULL THROW 50303,N'Missing store_shipping_services.service_id.',1;
IF EXISTS(SELECT 1 FROM dbo.shipping_providers p LEFT JOIN dbo.users u ON u.user_id=p.owner_user_id WHERE u.user_id IS NULL) THROW 50304,N'Orphan provider owner.',1;
IF EXISTS(SELECT 1 FROM dbo.shipping_services s LEFT JOIN dbo.shipping_providers p ON p.provider_id=s.provider_id WHERE p.provider_id IS NULL) THROW 50305,N'Orphan shipping service.',1;
IF EXISTS(SELECT 1 FROM dbo.store_shipping_services x LEFT JOIN dbo.stores s ON s.store_id=x.store_id LEFT JOIN dbo.shipping_services v ON v.service_id=x.service_id WHERE s.store_id IS NULL OR v.service_id IS NULL) THROW 50306,N'Orphan store shipping service.',1;
IF EXISTS(SELECT 1 FROM dbo.shipping_rate_rules r LEFT JOIN dbo.shipping_services s ON s.service_id=r.service_id WHERE s.service_id IS NULL) THROW 50307,N'Orphan shipping rate.',1;
IF EXISTS(SELECT 1 FROM dbo.shipping_quotes q LEFT JOIN dbo.shipping_services s ON s.service_id=q.service_id LEFT JOIN dbo.shipping_providers p ON p.provider_id=q.provider_id WHERE s.service_id IS NULL OR p.provider_id IS NULL) THROW 50308,N'Orphan shipping quote.',1;
IF EXISTS(SELECT 1 FROM dbo.shipping_quotes q JOIN dbo.shipping_services s ON s.service_id=q.service_id WHERE q.provider_id<>s.provider_id) THROW 50320,N'Shipping quote provider does not own service.',1;
IF EXISTS(SELECT 1 FROM dbo.shipments sh LEFT JOIN dbo.store_orders so ON so.store_order_id=sh.store_order_id LEFT JOIN dbo.shipping_services sv ON sv.service_id=sh.service_id LEFT JOIN dbo.shipping_providers p ON p.provider_id=sh.provider_id WHERE so.store_order_id IS NULL OR sv.service_id IS NULL OR p.provider_id IS NULL) THROW 50309,N'Orphan shipment.',1;
IF EXISTS(SELECT 1 FROM dbo.shipments sh JOIN dbo.shipping_services sv ON sv.service_id=sh.service_id WHERE sh.provider_id<>sv.provider_id) THROW 50321,N'Shipment provider does not own service.',1;
IF EXISTS(SELECT 1 FROM dbo.shipment_status_histories h LEFT JOIN dbo.shipments sh ON sh.shipment_id=h.shipment_id LEFT JOIN dbo.users u ON u.user_id=h.changed_by_user_id WHERE sh.shipment_id IS NULL OR u.user_id IS NULL) THROW 50322,N'Orphan shipment history.',1;
IF EXISTS(SELECT 1 FROM dbo.order_status_histories h LEFT JOIN dbo.store_orders so ON so.store_order_id=h.store_order_id LEFT JOIN dbo.users u ON u.user_id=h.changed_by_user_id WHERE so.store_order_id IS NULL OR (h.changed_by_user_id IS NOT NULL AND u.user_id IS NULL)) THROW 50323,N'Orphan order history.',1;
IF EXISTS(SELECT slug FROM dbo.shipping_providers GROUP BY slug HAVING COUNT(*)>1) THROW 50310,N'Duplicate provider slug.',1;
IF EXISTS(SELECT provider_id,service_code FROM dbo.shipping_services GROUP BY provider_id,service_code HAVING COUNT(*)>1) THROW 50311,N'Duplicate provider service code.',1;
IF EXISTS(SELECT store_id,service_id FROM dbo.store_shipping_services GROUP BY store_id,service_id HAVING COUNT(*)>1) THROW 50312,N'Duplicate store service mapping.',1;
IF EXISTS(SELECT tracking_code FROM dbo.shipments GROUP BY tracking_code HAVING COUNT(*)>1) THROW 50313,N'Duplicate tracking code.',1;
IF EXISTS(SELECT 1 FROM dbo.store_orders WHERE total_amount<>subtotal-discount_amount+shipping_fee) THROW 50314,N'Invalid StoreOrder total.',1;
IF EXISTS(
    SELECT 1
    FROM dbo.orders o
    OUTER APPLY(SELECT COALESCE(SUM(CASE WHEN so.status<>'CANCELLED' THEN so.total_amount ELSE 0 END),0) payable FROM dbo.store_orders so WHERE so.order_id=o.order_id) x
    WHERE o.total_amount<>x.payable
) THROW 50324,N'Parent Order total differs from payable StoreOrders.',1;
IF EXISTS(SELECT 1 FROM dbo.order_items WHERE store_order_id IS NULL) THROW 50315,N'OrderItem without StoreOrder.',1;
IF EXISTS(SELECT product_id FROM dbo.product_images WHERE is_primary=1 GROUP BY product_id HAVING COUNT(*)>1) THROW 50316,N'Multiple primary product images.',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 50317,N'Disabled or untrusted foreign key.',1;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) THROW 50318,N'Disabled or untrusted check constraint.',1;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE is_disabled=1) THROW 50319,N'Disabled index.',1;

SELECT N'shipping_providers' entity,COUNT(*) row_count FROM dbo.shipping_providers
UNION ALL SELECT N'shipping_services',COUNT(*) FROM dbo.shipping_services
UNION ALL SELECT N'store_shipping_services',COUNT(*) FROM dbo.store_shipping_services
UNION ALL SELECT N'shipping_rate_rules',COUNT(*) FROM dbo.shipping_rate_rules
UNION ALL SELECT N'shipping_quotes',COUNT(*) FROM dbo.shipping_quotes
UNION ALL SELECT N'shipments',COUNT(*) FROM dbo.shipments;
GO
