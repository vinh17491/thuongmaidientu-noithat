using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("products")]
[Index("CategoryId", "Status", Name = "IX_products_category_status")]
[Index("StoreId", "Status", Name = "IX_products_store_status")]
[Index("StoreId", "Slug", Name = "UQ_products_store_slug", IsUnique = true)]
public partial class Product
{
    [Key]
    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("store_id")]
    public long StoreId { get; set; }

    [Column("category_id")]
    public long CategoryId { get; set; }

    [Column("product_name")]
    [StringLength(200)]
    public string ProductName { get; set; } = null!;

    [Column("slug")]
    [StringLength(220)]
    [Unicode(false)]
    public string Slug { get; set; } = null!;

    [Column("brand")]
    [StringLength(100)]
    public string? Brand { get; set; }

    [Column("short_description")]
    [StringLength(500)]
    public string? ShortDescription { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column("seo_title")]
    [StringLength(180)]
    public string? SeoTitle { get; set; }

    [Column("seo_description")]
    [StringLength(320)]
    public string? SeoDescription { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Products")]
    public virtual ProductCategory Category { get; set; } = null!;

    [InverseProperty("Product")]
    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    [NotMapped]
    public ProductImage? ProductImage => ProductImages
        .OrderByDescending(image => image.IsPrimary)
        .ThenBy(image => image.SortOrder)
        .ThenBy(image => image.ImageId)
        .FirstOrDefault();

    [InverseProperty("Product")]
    public virtual ICollection<ProductSku> ProductSkus { get; set; } = new List<ProductSku>();

    [InverseProperty("Product")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [ForeignKey("StoreId")]
    [InverseProperty("Products")]
    public virtual Store Store { get; set; } = null!;
}
