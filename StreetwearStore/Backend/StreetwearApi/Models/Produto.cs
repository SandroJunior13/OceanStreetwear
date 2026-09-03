namespace StreetwearApi.Models;

// Um item vendido na loja (camiseta, moletom, boné, etc.)
public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty; // ex: "Camiseta", "Moletom", "Boné"
    public decimal Preco { get; set; }
    public string ImagemUrl { get; set; } = string.Empty;

    // Tamanhos disponíveis, separados por vírgula: "P,M,G,GG"
    public string TamanhosDisponiveis { get; set; } = "P,M,G,GG";

    public int EstoqueTotal { get; set; }
    public bool Ativo { get; set; } = true;
}
