using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("carts")]
[Index("UserId", Name = "UQ__carts__B9BE370EEC424543", IsUnique = true)]
public partial class Cart
{
    [Key]
    [Column("cart_id")]
    public long CartId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Cart")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [ForeignKey("UserId")]
    [InverseProperty("Cart")]
    public virtual User User { get; set; } = null!;
}
