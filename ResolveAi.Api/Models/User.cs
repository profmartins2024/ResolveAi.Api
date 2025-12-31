namespace ResolveAi.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Hash da senha (coluna: senha)
    public string Senha { get; set; } = string.Empty;

    // Hash da palavra de segurança (coluna: palavra_seguranca)
    public string PalavraSeguranca { get; set; } = string.Empty;

    // Hash do PIN (coluna: pin)
    public string Pin { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; }
}
