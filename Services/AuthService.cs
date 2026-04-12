using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ShInvoicing.Data;
using ShInvoicing.Models;

namespace ShInvoicing.Services;

public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        var salt = new byte[16];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);

        var actual = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            32);

        return actual.SequenceEqual(expected);
    }
}

public class AuthService
{
    private readonly UserRepository _userRepository;

    public AuthService()
    {
        _userRepository = new UserRepository(new DatabaseService());
    }

    public async Task<ApplicationUser?> ValidateUserAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _userRepository.GetByUsernameAsync(username.Trim());
        if (user == null)
            return null;

        return PasswordHelper.VerifyPassword(password, user.PasswordHash ?? string.Empty) ? user : null;
    }
}

public class InventoryService
{
    private readonly ProductRepository _productRepository;

    public InventoryService()
    {
        _productRepository = new ProductRepository(new DatabaseService());
    }

    public async Task<bool> AdjustStockAsync(string productCode, decimal quantity, string transactionType, string referenceNo, string createdBy)
    {
        var product = await _productRepository.GetByCodeAsync(productCode);
        if (product == null)
            return false;

        var updatedStock = product.StockQty + quantity;
        if (updatedStock < 0)
            return false;

        await _productRepository.UpdateStockAsync(product.Id, updatedStock);

        await _productRepository.AddStockTransactionAsync(new StockTransaction
        {
            ProductId = product.Id,
            ProductCode = product.Code,
            Quantity = quantity,
            TransactionType = transactionType,
            ReferenceNo = referenceNo,
            CreatedBy = createdBy,
            TransactionDate = DateTime.UtcNow
        });

        return true;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _productRepository.GetProductsAsync();
    }

    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        return await _productRepository.GetByCodeAsync(code);
    }

    public async Task AddProductAsync(Product product)
    {
        await _productRepository.AddOrUpdateProductAsync(product);
    }
}
