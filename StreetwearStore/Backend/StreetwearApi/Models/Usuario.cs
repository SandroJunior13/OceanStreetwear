namespace StreetwearApi.Models;

// Representa um cliente cadastrado na loja
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Nunca guardamos a senha em texto puro: só o hash + o "sal" usado para gerá-lo
    public string SenhaHash { get; set; } = string.Empty;
    public string SenhaSalt { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public List<Pedido> Pedidos { get; set; } = new();
}
