using Microsoft.AspNetCore.Mvc;
using ResolveAi.Api.Repositories;
using System.Globalization;
using System.Text;

namespace ResolveAi.Api.Controllers
{
    [ApiController]
    [Route("api/login")]
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

            var sb = new StringBuilder();

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
            // 1️⃣ BUSCA USUÁRIO
            // ===============================
            var usuario = await _usuarioRepo.BuscarPorEmailAsync(request.Email);

            if (usuario == null)
                return Unauthorized("E-mail incorreto.");

            // ===============================
            // 2️⃣ VERIFICA SENHA
            // ===============================
            if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
                return Unauthorized("Senha incorreta.");

            // ===============================
            // 3️⃣ VERIFICA PALAVRA DE SEGURANÇA
            // ===============================
            var palavraNormalizada = NormalizarSeguranca(request.PalavraSeguranca);

            if (!BCrypt.Net.BCrypt.Verify(palavraNormalizada, usuario.PalavraSeguranca))
                return Unauthorized("Palavra de segurança incorreta.");

            // ===============================
            // SUCESSO
            // ===============================
            return Ok(new
            {
                usuarioId = usuario.Id,     // 🔥 ID do banco
                nome = usuario.Nome,        // 🔥 coluna "nome" do banco
                email = usuario.Email       // 🔥 email usado no login
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
