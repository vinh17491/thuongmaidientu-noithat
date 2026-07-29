using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ThuongMaiDienTu.Controllers;
using ThuongMaiDienTu.ViewModels;
using Xunit;
namespace ThuongMaiDienTu.Tests;
public sealed class ChatAssistantContractTests
{
 [Fact] public void RequestRequiresBoundedMessage(){var r=new ChatAssistantRequest{Message=new string('x',501)};Assert.False(Validator.TryValidateObject(r,new ValidationContext(r),new List<ValidationResult>(),true));}
 [Fact] public void EndpointIsPostOnly(){var method=typeof(ChatAssistantController).GetMethod(nameof(ChatAssistantController.Ask));Assert.NotNull(method);Assert.Contains(method!.GetCustomAttributes(typeof(HttpPostAttribute),false).Cast<HttpPostAttribute>(),a=>a.Template=="/tro-ly/hoi");}
}
