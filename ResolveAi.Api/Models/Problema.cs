namespace ResolveAi.Api.Models;

public class Problema
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public Guid MoradorId { get; set; }

    public User? Morador { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public bool Aberto { get; set; } = true;
}
