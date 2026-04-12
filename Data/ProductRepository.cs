using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ShInvoicing.Models;

namespace ShInvoicing.Data;

public class ProductRepository
{
    private readonly DatabaseService _db;

    public ProductRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        using var conn = _db.GetConnection();
        var products = await conn.QueryAsync<Product>("SELECT * FROM Products WHERE IsActive = 1 ORDER BY Name");
        return products.AsList();
    }

    public async Task<Product?> GetByCodeAsync(string code)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE Code = @Code AND IsActive = 1", new { Code = code });
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE Id = @Id", new { Id = id });
    }

    public async Task AddOrUpdateProductAsync(Product product)
    {
        using var conn = _db.GetConnection();
        if (product.Id == 0)
        {
            // Insert
            var sql = @"INSERT INTO Products (Code, Name, Unit, HSNSACCode, PurchaseRate, SaleRate, GSTPercent, StockQty, MinStockQty)
                        VALUES (@Code, @Name, @Unit, @HSNSACCode, @PurchaseRate, @SaleRate, @GSTPercent, @StockQty, @MinStockQty);
                        SELECT last_insert_rowid();";
            product.Id = await conn.ExecuteScalarAsync<int>(sql, product);
        }
        else
        {
            // Update
            var sql = @"UPDATE Products SET Code = @Code, Name = @Name, Unit = @Unit, HSNSACCode = @HSNSACCode,
                        PurchaseRate = @PurchaseRate, SaleRate = @SaleRate, GSTPercent = @GSTPercent,
                        StockQty = @StockQty, MinStockQty = @MinStockQty
                        WHERE Id = @Id";
            await conn.ExecuteAsync(sql, product);
        }
    }

    public async Task UpdateStockAsync(int productId, decimal newStock)
    {
        using var conn = _db.GetConnection();
        await conn.ExecuteAsync("UPDATE Products SET StockQty = @StockQty WHERE Id = @Id",
            new { Id = productId, StockQty = newStock });
    }

    public async Task AddStockTransactionAsync(StockTransaction transaction)
    {
        using var conn = _db.GetConnection();
        var sql = @"INSERT INTO StockTransactions (ProductId, ProductCode, Quantity, TransactionType, ReferenceNo, CreatedBy, TransactionDate)
                    VALUES (@ProductId, @ProductCode, @Quantity, @TransactionType, @ReferenceNo, @CreatedBy, @TransactionDate)";
        await conn.ExecuteAsync(sql, transaction);
    }

    public async Task DeleteProductAsync(int id)
    {
        using var conn = _db.GetConnection();
        await conn.ExecuteAsync("UPDATE Products SET IsActive = 0, UpdatedDate = @UpdatedDate WHERE Id = @Id",
            new { Id = id, UpdatedDate = DateTime.UtcNow.ToString("o") });
    }

    public async Task<List<StockTransaction>> GetStockTransactionsAsync(int productId)
    {
        using var conn = _db.GetConnection();
        var transactions = await conn.QueryAsync<StockTransaction>(
            "SELECT * FROM StockTransactions WHERE ProductId = @ProductId ORDER BY TransactionDate DESC",
            new { ProductId = productId });
        return transactions.AsList();
    }
}