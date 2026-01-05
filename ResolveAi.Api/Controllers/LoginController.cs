using Microsoft.AspNetCore.Mvc;
using ResolveAi.Api.Repositories;
using System.Globalization;
using System.Text;

namespace ResolveAi.Api.Controllers
{
    [ApiController]
    [Route("api/login")]
    [Produces("application/json")]
    public class LoginController : ControllerBase
    {
        private readonly UsuarioRepository _usuarioRepo;

        public LoginController(UsuarioRepository usuarioRepo)
        {
            _usuarioRepo = usuarioRepo;
        }

        // =====================================================
        // NORMALIZA APENAS A PALAVRA DE SEGURANÇA
        // =====================================================
        private static string NormalizarSeguranca(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            var normalized = valor
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (Char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString();
        }

        // =====================================================
        // LOGIN
        // =====================================================
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Senha) ||
                string.IsNullOrWhiteSpace(request.PalavraSeguranca))
            {
                return BadRequest("Dados inválidos.");
            }

            // ===============================
            // 1️⃣ BUSCAR USUÁRIO PELO EMAIL
            // ===============================
            var usuario = await _usuarioRepo.BuscarPorEmailAsync(request.Email.Trim());

            if (usuario == null)
                return Unauthorized("E-mail incorreto.");

            // ===============================
            // 2️⃣ VERIFICAR SENHA
            // ===============================
            if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
                return Unauthorized("Senha incorreta.");

            // ===============================
            // 3️⃣ VERIFICAR PALAVRA DE SEGURANÇA
            // ===============================
            var palavraNormalizada =
                NormalizarSeguranca(request.PalavraSeguranca);

            if (!BCrypt.Net.BCrypt.Verify(
                    palavraNormalizada,
                    usuario.PalavraSeguranca))
            {
                return Unauthorized("Palavra de segurança incorreta.");
            }

            // ===============================
            // 4️⃣ SUCESSO (CONTRATO FINAL)
            // ===============================
            return Ok(new
            {
                sucesso = true,
                statusCode = StatusCodes.Status200OK,
                mensagem = "Login realizado com sucesso.",

                // 🔥 DADOS VINDOS DO BANCO
                usuarioId = usuario.Id.ToString(),
                nome = usuario.Nome,     // coluna "nome"
                email = usuario.Email    // coluna "email"
            });
        }
    }

    // =====================================================
    // DTO
    // =====================================================
    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Senha { get; set; } = "";
        public string PalavraSeguranca { get; set; } = "";
    }
}
