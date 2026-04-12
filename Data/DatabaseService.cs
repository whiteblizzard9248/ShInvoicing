using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ShInvoicing.Data;

public class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Path.Combine(AppContext.BaseDirectory, "invoices.db");
        Initialize();
    }

    public SqliteConnection GetConnection()
    {
        return new SqliteConnection($"Data Source={_dbPath}");
    }

    private void Initialize()
    {
        using var connection = GetConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Roles (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    DisplayName TEXT,
    PasswordHash TEXT NOT NULL,
    RoleId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(RoleId) REFERENCES Roles(Id)
);

CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Unit TEXT,
    HSNSACCode TEXT,
    PurchaseRate REAL NOT NULL DEFAULT 0,
    SaleRate REAL NOT NULL DEFAULT 0,
    GSTPercent REAL NOT NULL DEFAULT 0,
    StockQty REAL NOT NULL DEFAULT 0,
    MinStockQty REAL NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS StockTransactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    ProductCode TEXT,
    Quantity REAL NOT NULL,
    TransactionType TEXT NOT NULL,
    ReferenceNo TEXT,
    Remarks TEXT,
    CreatedBy TEXT,
    TransactionDate TEXT NOT NULL,
    FOREIGN KEY(ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS VendorSettings (
    Id INTEGER PRIMARY KEY,
    VendorName TEXT,
    VendorAddress TEXT,
    GSTIN TEXT,
    PANNo TEXT,
    BankAccountNo TEXT,
    IFSC TEXT,
    MobileNumber TEXT,
    Email TEXT
);

CREATE TABLE IF NOT EXISTS Invoices (
    InvoiceNo TEXT PRIMARY KEY,
    InvoiceDate TEXT,
    InvoiceType INTEGER NOT NULL DEFAULT 1,
    CounterpartyName TEXT,
    CounterpartyAddress TEXT,
    CounterpartyGSTIN TEXT,
    CounterpartyState TEXT,
    CreatedBy TEXT,
    CreatedAt TEXT,
    ModifiedAt TEXT,
    ModifiedBy TEXT,
    CustomerName TEXT,
    CustomerAddress TEXT,
    CustomerGSTIN TEXT,
    CustomerState TEXT
);

CREATE TABLE IF NOT EXISTS InvoiceItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceNo TEXT,
    ProductCode TEXT,
    ProductName TEXT,
    Description TEXT,
    HSNSACCode TEXT,
    Units TEXT,
    Quantity INTEGER,
    Rate REAL,
    TaxableValue REAL,
    CGSTPercent REAL,
    CGSTAmount REAL,
    SGSTPercent REAL,
    SGSTAmount REAL,
    IGSTPercent REAL,
    IGSTAmount REAL,
    LineTotal REAL,
    UNIQUE(InvoiceNo, ProductCode, Description, Rate)
);
";
        cmd.ExecuteNonQuery();

        EnsureColumnExists(connection, "Invoices", "InvoiceType", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumnExists(connection, "Invoices", "CounterpartyName", "TEXT");
        EnsureColumnExists(connection, "Invoices", "CounterpartyAddress", "TEXT");
        EnsureColumnExists(connection, "Invoices", "CounterpartyGSTIN", "TEXT");
        EnsureColumnExists(connection, "Invoices", "CounterpartyState", "TEXT");
        EnsureColumnExists(connection, "Invoices", "CreatedBy", "TEXT");
        EnsureColumnExists(connection, "Invoices", "CreatedAt", "TEXT");
        EnsureColumnExists(connection, "Invoices", "ModifiedAt", "TEXT");
        EnsureColumnExists(connection, "Invoices", "ModifiedBy", "TEXT");
        EnsureColumnExists(connection, "InvoiceItems", "ProductCode", "TEXT");
        EnsureColumnExists(connection, "InvoiceItems", "ProductName", "TEXT");
        EnsureColumnExists(connection, "InvoiceItems", "Units", "TEXT");
        EnsureColumnExists(connection, "InvoiceItems", "LineTotal", "REAL");
        EnsureColumnExists(connection, "Products", "MinStockQty", "REAL NOT NULL DEFAULT 0");
        EnsureColumnExists(connection, "Products", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumnExists(connection, "Products", "UpdatedDate", "TEXT");

        EnsureSeedData(connection);
    }

    private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string definition)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = cmd.ExecuteReader();

        var exists = false;
        while (reader.Read())
        {
            if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }

        reader.Close();

        if (!exists)
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
            try
            {
                alter.ExecuteNonQuery();
            }
            catch
            {
                // Column may already exist or schema may be different
            }
        }
    }

    private static void EnsureSeedData(SqliteConnection connection)
    {
        using var roleCountCmd = connection.CreateCommand();
        roleCountCmd.CommandText = "SELECT COUNT(1) FROM Roles;";
        var roleCount = Convert.ToInt32(roleCountCmd.ExecuteScalar() ?? 0);

        if (roleCount == 0)
        {
            using var insertRole = connection.CreateCommand();
            insertRole.CommandText = @"
INSERT INTO Roles (Id, Name) VALUES
(1, 'Admin'),
(2, 'Staff'),
(3, 'User');
";
            insertRole.ExecuteNonQuery();
        }

        using var userCountCmd = connection.CreateCommand();
        userCountCmd.CommandText = "SELECT COUNT(1) FROM Users;";
        var userCount = Convert.ToInt32(userCountCmd.ExecuteScalar() ?? 0);

        if (userCount == 0)
        {
            using var insertUser = connection.CreateCommand();
            insertUser.CommandText = @"
INSERT INTO Users (Username, DisplayName, PasswordHash, RoleId, CreatedAt)
VALUES (@Username, @DisplayName, @PasswordHash, @RoleId, @CreatedAt);
";
            insertUser.Parameters.AddWithValue("@Username", "admin");
            insertUser.Parameters.AddWithValue("@DisplayName", "Administrator");
            insertUser.Parameters.AddWithValue("@PasswordHash", CreatePasswordHash("admin123"));
            insertUser.Parameters.AddWithValue("@RoleId", 1);
            insertUser.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
            insertUser.ExecuteNonQuery();
        }
    }

    private static string CreatePasswordHash(string password)
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
}
