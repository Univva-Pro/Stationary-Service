using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Stationary.DMO;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Stationary.Context
{
    public class StationaryRepository
    {
        private readonly IMongoCollection<StationaryProduct>? _stationaryProducts;

        private static MongoClient CreateClient(string connStr)
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString(connStr);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
                settings.ConnectTimeout = TimeSpan.FromSeconds(2);
                settings.SocketTimeout = TimeSpan.FromSeconds(2);
                settings.SslSettings = new SslSettings
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                };
                return new MongoClient(settings);
            }
            catch
            {
                return new MongoClient(connStr);
            }
        }

        public StationaryRepository(string connectionString, string databaseName)
        {
            try
            {
                var client = CreateClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _stationaryProducts = database.GetCollection<StationaryProduct>("stationaryProducts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STATIONARY REPO INIT WARNING] {ex.Message}");
            }
        }

        public async Task<List<StationaryProduct>> GetAllProductsAsync()
        {
            if (_stationaryProducts == null) return new List<StationaryProduct>();
            try
            {
                using var cts = new CancellationTokenSource(1500);
                return await _stationaryProducts.Find(_ => true).ToListAsync(cts.Token);
            }
            catch
            {
                return new List<StationaryProduct>();
            }
        }

        public async Task<StationaryProduct?> GetProductAsync(string id)
        {
            if (ObjectId.TryParse(id, out var oid))
            {
                if (_stationaryProducts != null)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(1500);
                        return await _stationaryProducts.Find(x => x.Id == oid).FirstOrDefaultAsync(cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STATIONARY REPO GET ERR] {ex.Message}");
                    }
                }
            }
            return null;
        }

        public async Task AddProductAsync(StationaryProduct product)
        {
            if (_stationaryProducts != null)
            {
                try
                {
                    using var cts = new CancellationTokenSource(1500);
                    await _stationaryProducts.InsertOneAsync(product, cancellationToken: cts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[STATIONARY REPO ADD ERR] {ex.Message}");
                }
            }
        }

        public async Task UpdateProductAsync(string id, StationaryProduct product)
        {
            if (ObjectId.TryParse(id, out var oid))
            {
                product.Id = oid;
                if (_stationaryProducts != null)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(1500);
                        await _stationaryProducts.ReplaceOneAsync(p => p.Id == oid, product, cancellationToken: cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STATIONARY REPO UPDATE ERR] {ex.Message}");
                    }
                }
            }
        }

        public async Task DeleteProductAsync(string id)
        {
            if (ObjectId.TryParse(id, out var oid))
            {
                if (_stationaryProducts != null)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(1500);
                        await _stationaryProducts.DeleteOneAsync(p => p.Id == oid, cancellationToken: cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STATIONARY REPO DELETE ERR] {ex.Message}");
                    }
                }
            }
        }
    }
}
