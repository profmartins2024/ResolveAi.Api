using ResolveAi.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace ResolveAi.Api.Controllers
{
    [ApiController]
    [Route("api/redefinir-senha")]
    public class RedefinirSenhaController : ControllerBase
    {
        private readonly UsuarioRepository _repo;

        // contadores simples em memória (por email)
        private static Dictionary<string, int> TentativasPalavra = new();
        private static Dictionary<string, int> TentativasPin = new();

        public RedefinirSenhaController(UsuarioRepository repo)
        {
            _repo = repo;
        }

        // =====================================================
        // NORMALIZAÇÃO (IGUAL AO CADASTRO)
        // =====================================================
        private static string Normalizar(string valor)
        {
            return valor
                .Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD)
                .Where(c => Char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .Aggregate("", (a, b) => a + b);
        }

        // =====================================================
        // REDEFINIR SENHA (FLUXO COMPLETO)
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaCompletaRequest request)
        {
            if (request == null)
                return BadRequest("Dados inválidos.");

            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.SenhaAtual) ||
                string.IsNullOrWhiteSpace(request.NovaSenha) ||
                string.IsNullOrWhiteSpace(request.ConfirmarNovaSenha))
                return BadRequest("Preencha todos os campos.");

            if (request.NovaSenha != request.ConfirmarNovaSenha)
                return BadRequest("As senhas não coincidem.");

            if (request.NovaSenha.Length < 8)
                return BadRequest("Senha muito curta.");

            var email = request.Email.Trim().ToLowerInvariant();

            var usuario = await _repo.BuscarPorEmailAsync(email);
            if (usuario == null)
                return Unauthorized("Usuário não encontrado.");

            // =====================================================
            // 1️⃣ VALIDAR SENHA ATUAL
            // =====================================================
            bool senhaOk = BCrypt.Net.BCrypt.Verify(
                request.SenhaAtual.Trim(),
                usuario.Senha
            );

            if (!senhaOk)
                return Unauthorized("Senha atual incorreta.");

            // =====================================================
            // 2️⃣ VALIDAR PALAVRA OU PIN
            // =====================================================
            bool validacaoOk = false;

            // --- PALAVRA DE SEGURANÇA ---
            if (!string.IsNullOrWhiteSpace(request.PalavraSeguranca))
            {
                var palavra = Normalizar(request.PalavraSeguranca);

                bool palavraOk = BCrypt.Net.BCrypt.Verify(
                    palavra,
                    usuario.PalavraSeguranca
                );

                if (!palavraOk)
                {
                    TentativasPalavra[email] = TentativasPalavra.GetValueOrDefault(email) + 1;

                    if (TentativasPalavra[email] >= 3)
                        return StatusCode(423, "Palavra bloqueada. Informe o PIN.");

                    return Unauthorized("Palavra de segurança incorreta.");
                }

                validacaoOk = true;
            }
            // --- PIN ---
            else if (!string.IsNullOrWhiteSpace(request.Pin))
            {
                var pin = Normalizar(request.Pin);

                bool pinOk = BCrypt.Net.BCrypt.Verify(
                    pin,
                    usuario.Pin
                );

                if (!pinOk)
                {
                    TentativasPin[email] = TentativasPin.GetValueOrDefault(email) + 1;

                    if (TentativasPin[email] >= 3)
                        return StatusCode(423, "PIN bloqueado. Retorne ao login.");

                    return Unauthorized("PIN incorreto.");
                }

                validacaoOk = true;
            }
            else
            {
                return BadRequest("Informe palavra de segurança ou PIN.");
            }

            if (!validacaoOk)
                return Unauthorized("Falha na validação.");

            // =====================================================
            // 3️⃣ ATUALIZAR SENHA
            // =====================================================
            var novaSenhaHash = BCrypt.Net.BCrypt.HashPassword(
                request.NovaSenha.Trim()
            );

            await _repo.AtualizarSenhaAsync(usuario.Id, novaSenhaHash);

            // limpa tentativas
            TentativasPalavra.Remove(email);
            TentativasPin.Remove(email);

            return Ok("Senha redefinida com sucesso.");
        }
    }

    // =====================================================
    // DTO
    // =====================================================
    public class RedefinirSenhaCompletaRequest
    {
        public string Email { get; set; } = "";
        public string SenhaAtual { get; set; } = "";
        public string NovaSenha { get; set; } = "";
        public string ConfirmarNovaSenha { get; set; } = "";
        public string? PalavraSeguranca { get; set; }
        public string? Pin { get; set; }
    }
}
