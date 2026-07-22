using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Stationary.DMO
{
    [BsonIgnoreExtraElements]
    public class StationaryProduct
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }
}
