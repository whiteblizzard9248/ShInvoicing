using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ShInvoicing.Models;

namespace ShInvoicing.Data;

public class UserRepository
{
    private readonly DatabaseService _db;

    public UserRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<ApplicationUser>> GetUsersAsync()
    {
        using var conn = _db.GetConnection();
        var users = await conn.QueryAsync<ApplicationUser>("SELECT * FROM Users ORDER BY Username");
        return users.AsList();
    }

    public async Task<ApplicationUser?> GetByUsernameAsync(string username)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<ApplicationUser>("SELECT * FROM Users WHERE Username = @Username", new { Username = username });
    }

    public async Task<ApplicationUser?> GetByIdAsync(int id)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<ApplicationUser>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task AddOrUpdateUserAsync(ApplicationUser user)
    {
        using var conn = _db.GetConnection();
        if (user.Id == 0)
        {
            // Insert
            var sql = @"INSERT INTO Users (Username, DisplayName, PasswordHash, RoleId, CreatedAt)
                        VALUES (@Username, @DisplayName, @PasswordHash, @RoleId, @CreatedAt);
                        SELECT last_insert_rowid();";
            user.Id = await conn.ExecuteScalarAsync<int>(sql, user);
        }
        else
        {
            // Update
            var sql = @"UPDATE Users SET Username = @Username, DisplayName = @DisplayName, PasswordHash = @PasswordHash,
                        RoleId = @RoleId
                        WHERE Id = @Id";
            await conn.ExecuteAsync(sql, user);
        }
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        // Not implemented in schema
    }

    public async Task DeleteUserAsync(int id)
    {
        // Not implemented, perhaps soft delete not in schema
    }
}