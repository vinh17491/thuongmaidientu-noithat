using System.ComponentModel.DataAnnotations;
namespace ThuongMaiDienTu.ViewModels;
public sealed class ChatAssistantRequest { [Required, StringLength(500, MinimumLength = 1)] public string Message { get; set; } = string.Empty; }
public sealed record ChatProductSuggestion(string ProductName,string ProductSlug,decimal CurrentPrice,decimal? OriginalPrice,string StoreName,string StoreSlug,string CategoryName,string CategorySlug,bool InStock,string Url);
public sealed record ChatStoreSuggestion(string StoreName,string StoreSlug,string Url);
public sealed record ChatCategorySuggestion(string CategoryName,string CategorySlug,string Url);
public sealed record ChatOrderSummary(string OrderCode,DateTime CreatedAt,string Status,decimal Total);
public sealed record ChatAssistantResponse(string Message,string Intent,IReadOnlyList<ChatProductSuggestion> Products,IReadOnlyList<ChatStoreSuggestion> Stores,IReadOnlyList<ChatCategorySuggestion> Categories,ChatOrderSummary? Order,IReadOnlyList<string> SuggestedActions);
