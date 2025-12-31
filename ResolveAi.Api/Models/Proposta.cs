namespace ResolveAi.Api.Models;

public class Proposta
{
    public Guid Id { get; set; }

    public Guid ProblemaId { get; set; }

    public Problema? Problema { get; set; }

    public Guid ProfissionalId { get; set; }

    public User? Profissional { get; set; }

    public decimal Valor { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
