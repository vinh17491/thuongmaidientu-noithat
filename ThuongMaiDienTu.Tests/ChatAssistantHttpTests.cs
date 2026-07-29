using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ThuongMaiDienTu.Tests;

public sealed class ChatAssistantHttpTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    public ChatAssistantHttpTests(WebApplicationFactory<Program> factory) => this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing").UseSetting("ConnectionStrings:DefaultConnection", "Server=localhost;Database=thuongmaidientu;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"));
    [Fact] public async Task GetEndpointIsNotChatOperation() { using var client=factory.CreateClient(); var response=await client.GetAsync("/tro-ly/hoi"); Assert.NotEqual(HttpStatusCode.OK,response.StatusCode); }
    [Fact] public async Task PostWithoutAntiforgeryTokenIsRejected() { using var client=factory.CreateClient(); using var form=new MultipartFormDataContent { { new StringContent("xin chào"), "Message" } }; Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/tro-ly/hoi",form)).StatusCode); }
    [Fact] public async Task UiFormDataWithTokenAcceptsGreeting() { using var client=factory.CreateClient(); var page=await client.GetStringAsync("/Account/Login"); var match=Regex.Match(page,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\""); Assert.True(match.Success); using var form=new MultipartFormDataContent { { new StringContent("xin chào"), "Message" }, { new StringContent(match.Groups[1].Value), "__RequestVerificationToken" } }; var response=await client.PostAsync("/tro-ly/hoi",form); Assert.Equal(HttpStatusCode.OK,response.StatusCode); Assert.Contains("application/json",response.Content.Headers.ContentType?.MediaType); }
    [Fact] public async Task EmptyMessageIsRejected() { using var client=factory.CreateClient(); using var form=new MultipartFormDataContent { { new StringContent(""), "Message" } }; Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/tro-ly/hoi",form)).StatusCode); }
    [Fact] public async Task OversizedMessageIsRejected() { using var client=factory.CreateClient(); using var form=new MultipartFormDataContent { { new StringContent(new string('x',501)), "Message" } }; Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/tro-ly/hoi",form)).StatusCode); }
}
