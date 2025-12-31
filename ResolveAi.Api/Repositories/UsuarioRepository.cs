using MySqlConnector;
using Microsoft.Extensions.Configuration;
using ResolveAi.Api.Models;

namespace ResolveAi.Api.Repositories;


    public class UsuarioRepository
    {
        private readonly IConfiguration _config;

        public UsuarioRepository(IConfiguration config)
        {
            _config = config;
        }

        // =====================================================
        // CRIAR CONEXÃO MYSQL (CORRETO PARA MYSQL REMOTO)
        // =====================================================
        private MySqlConnection CriarConexao()
        {
            var db = _config.GetSection("Database");

            var connectionString =
                $"Server={db["Server"]};" +
                $"Port={db["Port"] ?? "3306"};" +
                $"Database={db["Name"]};" +
                $"Uid={db["User"]};" +
                $"Pwd={db["Password"]};" +
                $"SslMode={db["SslMode"] ?? "None"};" +
                $"Connection Timeout={db["ConnectionTimeout"] ?? "10"};" +
                $"Allow User Variables=True;";

            return new MySqlConnection(connectionString);
        }

        // =====================================================
        // BUSCAR USUÁRIO POR E-MAIL
        // =====================================================
        public async Task<User?> BuscarPorEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            email = email.Trim().ToLowerInvariant();

            await using var conn = CriarConexao();

            try
            {
                await conn.OpenAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao conectar ao banco de dados MySQL.", ex);
            }

            await using var cmd = new MySqlCommand(@"
                SELECT
                    id,
                    nome,
                    email,
                    senha,
                    palavra_seguranca,
                    pin,
                    criado_em
                FROM login
                WHERE email = @email
                LIMIT 1
            ", conn);

            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new User
            {
                Id = reader.GetInt32("id"),
                Nome = reader.GetString("nome"),
                Email = reader.GetString("email"),
                Senha = reader.GetString("senha"),
                PalavraSeguranca = reader.GetString("palavra_seguranca"),
                Pin = reader.GetString("pin"),
                CriadoEm = reader.GetDateTime("criado_em")
            };
        }

        // =====================================================
        // ATUALIZAR SENHA
        // =====================================================
        public async Task AtualizarSenhaAsync(int idUsuario, string novaSenhaHash)
        {
            await using var conn = CriarConexao();
            await conn.OpenAsync();

            await using var cmd = new MySqlCommand(@"
                UPDATE login
                SET senha = @senha
                WHERE id = @id
            ", conn);

            cmd.Parameters.AddWithValue("@senha", novaSenhaHash);
            cmd.Parameters.AddWithValue("@id", idUsuario);

            await cmd.ExecuteNonQueryAsync();
        }
    }
