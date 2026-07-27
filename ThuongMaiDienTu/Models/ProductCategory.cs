using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("product_categories")]
[Index("Slug", Name = "UQ__product___32DD1E4CADDDC17D", IsUnique = true)]
public partial class ProductCategory
{
    [Key]
    [Column("category_id")]
    public long CategoryId { get; set; }

    [Column("parent_id")]
    public long? ParentId { get; set; }

    [Column("category_name")]
    [StringLength(150)]
    public string CategoryName { get; set; } = null!;

    [Column("slug")]
    [StringLength(180)]
    [Unicode(false)]
    public string Slug { get; set; } = null!;

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("seo_title")]
    [StringLength(180)]
    public string? SeoTitle { get; set; }

    [Column("seo_description")]
    [StringLength(320)]
    public string? SeoDescription { get; set; }

    [Column("status")]
    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [InverseProperty("Parent")]
    public virtual ICollection<ProductCategory> InverseParent { get; set; } = new List<ProductCategory>();

    [ForeignKey("ParentId")]
    [InverseProperty("InverseParent")]
    public virtual ProductCategory? Parent { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
