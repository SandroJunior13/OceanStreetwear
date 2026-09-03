namespace StreetwearApi.Models;

public enum FormaPagamento
{
    Pix,
    Cartao
}

public enum StatusPedido
{
    AguardandoPagamento,
    Pago,
    Recusado,
    Cancelado,
    Enviado
}

// Um pedido feito por um usuário, com um ou mais itens
public class Pedido
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public List<ItemPedido> Itens { get; set; } = new();

    public decimal ValorTotal { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public StatusPedido Status { get; set; } = StatusPedido.AguardandoPagamento;

    // Dados retornados pelo Mercado Pago (id do pagamento, QR Code do Pix, etc.)
    public string? PagamentoExternoId { get; set; }
    public string? PixQrCode { get; set; }
    public string? PixQrCodeBase64 { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// Um item dentro de um pedido (produto + quantidade + tamanho escolhido)
public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public string Tamanho { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
