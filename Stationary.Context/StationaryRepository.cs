using System.Collections.Generic;
using System.Threading.Tasks;
using Stationary.DMO;
using MongoDB.Driver;

namespace Stationary.Context
{
    public class StationaryRepository
    {
        private readonly IMongoCollection<StationaryProduct> _stationaryProducts;

        public StationaryRepository(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _stationaryProducts = database.GetCollection<StationaryProduct>("stationaryProducts");
            SeedProductsIfEmpty();
        }

        private void SeedProductsIfEmpty()
        {
            if (_stationaryProducts.CountDocuments(Builders<StationaryProduct>.Filter.Empty) == 0)
            {
                _stationaryProducts.InsertMany(new[]
                {
                    new StationaryProduct { Name = "Blue Ballpoint Pen", Category = "Pen", Brand = "Bic", Price = 1.50m, StockQuantity = 100 },
                    new StationaryProduct { Name = "A4 Lined Notebook", Category = "Notebook", Brand = "Moleskine", Price = 15.00m, StockQuantity = 50 },
                    new StationaryProduct { Name = "HB Pencil", Category = "Pencil", Brand = "Faber-Castell", Price = 0.50m, StockQuantity = 300 }
                });
            }
        }

        public async Task<List<StationaryProduct>> GetAllProductsAsync()
        {
            return await _stationaryProducts.Find(Builders<StationaryProduct>.Filter.Empty).ToListAsync();
        }

        public async Task<StationaryProduct> GetProductAsync(string id)
        {
            return await _stationaryProducts.Find(p => p.Id == MongoDB.Bson.ObjectId.Parse(id)).FirstOrDefaultAsync();
        }

        public async Task AddProductAsync(StationaryProduct product)
        {
            await _stationaryProducts.InsertOneAsync(product);
        }

        public async Task UpdateProductAsync(string id, StationaryProduct product)
        {
            product.Id = MongoDB.Bson.ObjectId.Parse(id);
            await _stationaryProducts.ReplaceOneAsync(p => p.Id == product.Id, product);
        }

        public async Task DeleteProductAsync(string id)
        {
            await _stationaryProducts.DeleteOneAsync(p => p.Id == MongoDB.Bson.ObjectId.Parse(id));
        }
    }
}
