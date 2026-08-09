using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("stores")]
[Index("Slug", Name = "UQ__stores__32DD1E4CACD5552A", IsUnique = true)]
public partial class Store
{
    [Key]
    [Column("store_id")]
    public long StoreId { get; set; }

    [Column("owner_user_id")]
    public long OwnerUserId { get; set; }

    [Column("store_name")]
    [StringLength(150)]
    public string StoreName { get; set; } = null!;

    [Column("slug")]
    [StringLength(180)]
    [Unicode(false)]
    public string Slug { get; set; } = null!;

    [Column("description")]
    [StringLength(1000)]
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

    [ForeignKey("OwnerUserId")]
    [InverseProperty("Stores")]
    public virtual User OwnerUser { get; set; } = null!;

    [InverseProperty("Store")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
