using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace ThuongMaiDienTu.Tests;

public sealed class ChatAssistantHttpTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    public ChatAssistantHttpTests(WebApplicationFactory<Program> factory) => this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing").UseSetting("ConnectionStrings:DefaultConnection", "Server=localhost;Database=thuongmaidientu;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"));

    [Fact]
    public async Task GetEndpointIsNotChatOperation() { using var client = factory.CreateClient(); var response = await client.GetAsync("/tro-ly/hoi"); Assert.NotEqual(HttpStatusCode.OK, response.StatusCode); }

    [Fact]
    public async Task PostWithoutAntiforgeryTokenIsRejected() { using var client = factory.CreateClient(); var response = await client.PostAsync("/tro-ly/hoi", new FormUrlEncodedContent(new[] { new KeyValuePair<string,string>("Message", "xin chào") })); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); }

    [Fact]
    public async Task ValidTokenAcceptsGreeting() { using var client = factory.CreateClient(); var page = await client.GetStringAsync("/Account/Login"); var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\""); Assert.True(match.Success); var response = await client.PostAsync("/tro-ly/hoi", new FormUrlEncodedContent(new[] { new KeyValuePair<string,string>("Message", "xin chào"), new KeyValuePair<string,string>("__RequestVerificationToken", match.Groups[1].Value) })); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType); }
}
