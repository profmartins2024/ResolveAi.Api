using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Globalization;
using System.Text;

namespace ResolveAi.Api.Controllers
{
    [ApiController]
    [Route("api/cadastro")]
    public class CadastroController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CadastroController(IConfiguration config)
        {
            _config = config;
        }

        // ==========================================
        // NORMALIZAÇÃO (USADA APENAS NA PALAVRA)
        // ==========================================
        private static string NormalizarSeguranca(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            var normalized = valor
                .Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                if (Char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString();
        }

        // ==========================================
        // CADASTRO
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Cadastrar([FromBody] CadastroRequest request)
        {
            if (request == null)
                return BadRequest("Dados inválidos.");

            if (string.IsNullOrWhiteSpace(request.NomeCompleto) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Documento) ||
                string.IsNullOrWhiteSpace(request.Senha) ||
                string.IsNullOrWhiteSpace(request.PalavraSeguranca) ||
                string.IsNullOrWhiteSpace(request.Pin))
            {
                return BadRequest("Todos os campos são obrigatórios.");
            }

            // ===========================
            // DADOS
            // ===========================
            var nome = request.NomeCompleto.Trim();
            var email = request.Email.Trim().ToLowerInvariant();
            var documento = request.Documento.Trim();

            // ❌ SENHA NÃO NORMALIZA
            var senha = request.Senha;

            // ✅ NORMALIZA SÓ A PALAVRA
            var palavra = NormalizarSeguranca(request.PalavraSeguranca);

            // ❌ PIN NÃO NORMALIZA
            var pin = request.Pin.Trim();

            try
            {
                var db = _config.GetSection("Database");

                var connectionString =
                    $"Server={db["Server"]};" +
                    $"Port=3306;" +
                    $"Database={db["Name"]};" +
                    $"User ID={db["User"]};" +
                    $"Password={db["Password"]};" +
                    $"SslMode=None;";

                await using var conn = new MySqlConnection(connectionString);
                await conn.OpenAsync();

                // ===========================
                // VERIFICA E-MAIL
                // ===========================
                var checkCmd = new MySqlCommand(
                    "SELECT 1 FROM login WHERE email = @email LIMIT 1",
                    conn
                );
                checkCmd.Parameters.AddWithValue("@email", email);

                if (await checkCmd.ExecuteScalarAsync() != null)
                    return Conflict("E-mail já cadastrado.");

                // ===========================
                // HASH
                // ===========================
                var senhaHash = BCrypt.Net.BCrypt.HashPassword(senha);
                var palavraHash = BCrypt.Net.BCrypt.HashPassword(palavra);
                var pinHash = BCrypt.Net.BCrypt.HashPassword(pin);

                // ===========================
                // INSERT
                // ===========================
                var cmd = new MySqlCommand(@"
                    INSERT INTO login
                    (nome, email, documento, senha, palavra_seguranca, pin)
                    VALUES
                    (@nome, @email, @documento, @senha, @palavra, @pin)
                ", conn);

                cmd.Parameters.AddWithValue("@nome", nome);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@documento", documento);
                cmd.Parameters.AddWithValue("@senha", senhaHash);
                cmd.Parameters.AddWithValue("@palavra", palavraHash);
                cmd.Parameters.AddWithValue("@pin", pinHash);

                await cmd.ExecuteNonQueryAsync();

                return Ok("Cadastro realizado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Erro interno.");
            }
        }
    }

    // ==========================================
    // DTO
    // ==========================================
    public class CadastroRequest
    {
        public string NomeCompleto { get; set; } = "";
        public string Email { get; set; } = "";
        public string Documento { get; set; } = "";
        public string Senha { get; set; } = "";
        public string PalavraSeguranca { get; set; } = "";
        public string Pin { get; set; } = "";
    }
}
