using Microsoft.AspNetCore.ResponseCompression;
using System.Net;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
ServicePointManager.DefaultConnectionLimit = 10;
var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:8332/");
var authValue = new AuthenticationHeaderValue(
    "Basic", Convert.ToBase64String(
        System.Text.ASCIIEncoding.ASCII.GetBytes(
            $"{Environment.GetEnvironmentVariable("btcrpcuser")}:{Environment.GetEnvironmentVariable("btcrpcpw")}")));
client.DefaultRequestHeaders.Authorization = authValue;
builder.Services.AddSingleton<HttpClient>(client);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
