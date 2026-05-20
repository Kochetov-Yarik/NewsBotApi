using Npgsql;
using NewsBotApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsBotApi.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = Constants.DatabaseConnection;
        public async Task AddSiteAsync(FavoriteSite site)
        {
            string sql = "INSERT INTO favorite_sites (site_name, site_url) VALUES (@name, @url)";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("name", site.SiteName);
            command.Parameters.AddWithValue("url", site.SiteUrl);

            await command.ExecuteNonQueryAsync();
        }
        public async Task<List<FavoriteSite>> GetSitesAsync()
        {
            var sites = new List<FavoriteSite>();

            string sql = "SELECT id, site_name, site_url FROM favorite_sites ORDER BY id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                sites.Add(new FavoriteSite
                {
                    Id = reader.GetInt32(0),
                    SiteName = reader.GetString(1),
                    SiteUrl = reader.GetString(2)
                });
            }
            return sites;
        }
        public async Task DeleteSiteAsync(int id)
        {
            string sql = "DELETE FROM favorite_sites WHERE id = @id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}