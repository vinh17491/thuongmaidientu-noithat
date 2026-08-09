using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("product_images")]
public partial class ProductImage
{
    [Key]
    [Column("image_id")]
    public long ImageId { get; set; }

    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("image_url")]
    [StringLength(500)]
    [Unicode(false)]
    public string ImageUrl { get; set; } = null!;

    [Column("alt_text")]
    [StringLength(180)]
    public string? AltText { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductImages")]
    public virtual Product Product { get; set; } = null!;
}
