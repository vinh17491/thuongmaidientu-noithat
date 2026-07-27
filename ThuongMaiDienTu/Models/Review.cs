using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("reviews")]
[Index("ProductId", "Status", Name = "IX_reviews_product_status")]
[Index("UserId", "OrderItemId", Name = "UQ_reviews_user_order_item", IsUnique = true)]
public partial class Review
{
    [Key]
    [Column("review_id")]
    public long ReviewId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("order_item_id")]
    public long? OrderItemId { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("comment")]
    [StringLength(1000)]
    public string? Comment { get; set; }

    [Column("status")]
    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("OrderItemId")]
    [InverseProperty("Reviews")]
    public virtual OrderItem? OrderItem { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("Reviews")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Reviews")]
    public virtual User User { get; set; } = null!;
}
