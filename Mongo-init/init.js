db = db.getSiblingDB('StationaryDB');

db.stationaryProducts.insertMany([
    {
        Name: "Blue Ballpoint Pen",
        Category: "Pen",
        Brand: "Bic",
        Price: 1.50,
        StockQuantity: 100
    },
    {
        Name: "A4 Lined Notebook",
        Category: "Notebook",
        Brand: "Moleskine",
        Price: 15.00,
        StockQuantity: 50
    },
    {
        Name: "HB Pencil",
        Category: "Pencil",
        Brand: "Faber-Castell",
        Price: 0.50,
        StockQuantity: 300
    }
]);

db.users.insertMany([
    {
        Username: "admin",
        PasswordHash: "admin@05",
        Role: "Admin"
    },
    {
        Username: "user",
        PasswordHash: "user123",
        Role: "User"
    }
]);
