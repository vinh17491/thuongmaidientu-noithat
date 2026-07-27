using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("users")]
[Index("Email", Name = "UQ__users__AB6E6164A4369AC8", IsUnique = true)]
[Index("Phone", Name = "UQ__users__B43B145FF1D0D4D3", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("full_name")]
    [StringLength(120)]
    public string FullName { get; set; } = null!;

    [Column("email")]
    [StringLength(150)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [Column("password_hash")]
    [StringLength(255)]
    [Unicode(false)]
    public string PasswordHash { get; set; } = null!;

    [Column("role")]
    [StringLength(20)]
    [Unicode(false)]
    public string Role { get; set; } = null!;

    [Column("status")]
    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("User")]
    public virtual Cart? Cart { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("User")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [InverseProperty("OwnerUser")]
    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
}
