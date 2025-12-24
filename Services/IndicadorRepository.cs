// Services/IndicadorRepository.cs
using Indicadores.Models;
using Microsoft.Data.Sqlite;

namespace Indicadores.Services
{
    public class IndicadorRepository
    {
        private readonly string _connectionString = "Data Source=indicadores.db";

        private async Task InicializarBaseDeDatos()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS Indicadores (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Codigo TEXT NOT NULL,
                    Fecha TEXT NOT NULL,
                    Valor REAL NOT NULL,
                    Fuente TEXT NOT NULL,
                    ActualizadoEn TEXT DEFAULT (datetime('now')),
                    UNIQUE(Codigo, Fecha)
                );
                CREATE INDEX IF NOT EXISTS IX_Indicadores_Codigo ON Indicadores(Codigo);
                CREATE INDEX IF NOT EXISTS IX_Indicadores_Fecha ON Indicadores(Fecha);
            ", connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<decimal?> ObtenerValor(string codigo, DateTime fecha)
        {
            await InicializarBaseDeDatos();
            const string sql = "SELECT Valor FROM Indicadores WHERE Codigo = $Codigo AND Fecha = $Fecha";
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("$Codigo", codigo);
            cmd.Parameters.AddWithValue("$Fecha", fecha.ToString("yyyy-MM-dd"));
            var result = await cmd.ExecuteScalarAsync();
            return result is double d ? (decimal?)Convert.ToDecimal(d) : null;
        }

        public async Task GuardarIndicadores(List<IndicadorDiario> indicadores)
        {
            await InicializarBaseDeDatos();
            const string sql = @"
                INSERT INTO Indicadores (Codigo, Fecha, Valor, Fuente)
                VALUES ($Codigo, $Fecha, $Valor, $Fuente)
                ON CONFLICT(Codigo, Fecha) DO NOTHING;";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            foreach (var ind in indicadores)
            {
                if (ind.Valor <= 0) continue;
                using var cmd = new SqliteCommand(sql, connection);
                cmd.Parameters.AddWithValue("$Codigo", ind.Codigo);
                cmd.Parameters.AddWithValue("$Fecha", ind.Fecha.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$Valor", (double)ind.Valor);
                cmd.Parameters.AddWithValue("$Fuente", ind.Fuente);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}