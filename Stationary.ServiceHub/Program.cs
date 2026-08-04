using Stationary.Context;
using Stationary.DTO;
using Stationary.DMO;
using Common.Library.Models;
using Common.Library.DTOs;
using Common.Library.Data;
using Common.Library.Extensions;
using Common.Library.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// DB Config
var connectionString = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27018";
var databaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "StationaryDB";

Console.WriteLine("====================================================");
Console.WriteLine($"[STARTUP] Using MongoDB Connection: {connectionString}");
Console.WriteLine($"[STARTUP] Using Database Name: {databaseName}");
Console.WriteLine("====================================================");

builder.Services.AddSingleton<StationaryRepository>(sp => 
{
    try 
    {
        return new StationaryRepository(connectionString, databaseName);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FATAL ERROR] Could not connect to MongoDB (StationaryRepository). Error: {ex.Message}");
        throw;
    }
});

builder.Services.AddSingleton<UserRepository>(sp => 
{
    try 
    {
        return new UserRepository(connectionString, databaseName);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FATAL ERROR] Could not connect to MongoDB (UserRepository). Error: {ex.Message}");
        throw;
    }
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsAVerySecretKeyForJwtAuthenticationWhichNeedsToBeLongEnough";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddCommonJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        builder => builder.WithOrigins("http://localhost:4202")
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAngularDev");

app.UseAuthentication();
app.UseAuthorization();

// Login Endpoint
app.MapPost("/api/auth/login", async (LoginRequest request, [Microsoft.AspNetCore.Mvc.FromServices] UserRepository userRepo) =>
{
    var user = await userRepo.GetUserAsync(request.Username, request.Password);
    if (user == null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = builder.Configuration["Jwt:Issuer"] ?? "Stationary.ServiceHub",
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new AuthResponse { Token = tokenHandler.WriteToken(token), Role = user.Role, Username = user.Username });
});

// Get Products (Accessible by both Admin and User, returning role-based fields)
app.MapGet("/api/stationary/products", async ([Microsoft.AspNetCore.Mvc.FromServices] StationaryRepository repository, ClaimsPrincipal user) =>
{
    var products = await repository.GetAllProductsAsync();
    bool isAdmin = user.IsInRole("Admin") ||
                   user.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type.ToLower() == "role") &&
                                      c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));

    if (isAdmin)
    {
        var response = products.Select(p => new StationaryProductAdminResponse
        {
            ProductId = p.Id.ToString(),
            Name = p.Name ?? "Unknown Product",
            Category = p.Category,
            Brand = p.Brand,
            Price = p.Price,
            StockQuantity = p.StockQuantity
        }).ToList();
        return Results.Ok(response);
    }
    else
    {
        var response = products.Select(p => new StationaryProductResponse
        {
            ProductId = p.Id.ToString(),
            Name = p.Name ?? "Unknown Product",
            Category = p.Category,
            Brand = p.Brand,
            Price = p.Price
        }).ToList();
        return Results.Ok(response);
    }
}).AllowAnonymous();

// Add Product (Admin Only)
app.MapPost("/api/stationary/products", async (StationaryProductRequest request, [Microsoft.AspNetCore.Mvc.FromServices] StationaryRepository repository) =>
{
    var product = new StationaryProduct
    {
        Name = request.Name,
        Category = request.Category,
        Brand = request.Brand,
        Price = request.Price,
        StockQuantity = request.StockQuantity
    };
    await repository.AddProductAsync(product);

    var commonServiceUrl = builder.Configuration["ServiceUrls:CommonService"];

    // Live Sync to Common-Service Master Inventory
    _ = ProductSyncClient.SyncProductToCommonAsync(new ProductSyncPayload
    {
        OriginalId = product.Id.ToString(),
        Name = product.Name,
        Category = string.IsNullOrEmpty(product.Category) ? "Stationary" : product.Category,
        Price = (decimal)product.Price,
        StockQuantity = product.StockQuantity,
        SourceService = "Stationary",
        ActionType = "Add"
    }, commonServiceUrl);

    return Results.Ok(product);
}).RequireAuthorization("AdminOnly");

// Update Product (Admin Only)
app.MapPut("/api/stationary/products/{id}", async (string id, StationaryProductRequest request, [Microsoft.AspNetCore.Mvc.FromServices] StationaryRepository repository) =>
{
    var commonServiceUrl = builder.Configuration["ServiceUrls:CommonService"];
    var existing = await repository.GetProductAsync(id);
    if (existing == null)
    {
        existing = new StationaryProduct
        {
            Name = request.Name,
            Category = request.Category,
            Brand = request.Brand,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };
        await repository.AddProductAsync(existing);
    }
    else
    {
        existing.Name = request.Name;
        existing.Category = request.Category;
        existing.Brand = request.Brand;
        existing.Price = request.Price;
        existing.StockQuantity = request.StockQuantity;
        await repository.UpdateProductAsync(id, existing);
    }

    // Live Sync Update to Common-Service Master Inventory
    _ = ProductSyncClient.SyncProductToCommonAsync(new ProductSyncPayload
    {
        OriginalId = id,
        Name = existing.Name,
        Category = string.IsNullOrEmpty(existing.Category) ? "Stationary" : existing.Category,
        Price = (decimal)existing.Price,
        StockQuantity = existing.StockQuantity,
        SourceService = "Stationary",
        ActionType = "Update"
    }, commonServiceUrl);

    return Results.Ok(existing);
}).RequireAuthorization("AdminOnly");

// Delete Product (Admin Only)
app.MapDelete("/api/stationary/products/{id}", async (string id, [Microsoft.AspNetCore.Mvc.FromServices] StationaryRepository repository) =>
{
    var commonServiceUrl = builder.Configuration["ServiceUrls:CommonService"];
    var existing = await repository.GetProductAsync(id);
    if (existing == null) return Results.NotFound();

    await repository.DeleteProductAsync(id);

    // Live Sync Delete to Common-Service Master Inventory
    _ = ProductSyncClient.SyncProductToCommonAsync(new ProductSyncPayload
    {
        OriginalId = id,
        Name = existing.Name,
        Category = string.IsNullOrEmpty(existing.Category) ? "Stationary" : existing.Category,
        SourceService = "Stationary",
        ActionType = "Delete"
    }, commonServiceUrl);

    return Results.Ok(new { message = "Product deleted successfully" });
}).RequireAuthorization("AdminOnly");

app.Run();
