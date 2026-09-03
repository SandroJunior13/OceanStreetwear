using StreetwearApi.Models;

namespace StreetwearApi.Dtos;

// ---- Autenticação ----
public record CadastroDto(string Nome, string Email, string Senha);
public record LoginDto(string Email, string Senha);
public record TokenRespostaDto(string Token, string Nome, string Email);

// ---- Pedido / Checkout ----
public record ItemCarrinhoDto(int ProdutoId, string Tamanho, int Quantidade);

// Dados de cartão são recebidos como TOKEN, nunca como número de cartão puro.
// Esse token é gerado no FRONTEND pelo SDK público do Mercado Pago (mercadopago.js),
// então o backend nunca vê o número do cartão, CVV, etc.
public record NovoPedidoDto(
    List<ItemCarrinhoDto> Itens,
    FormaPagamento FormaPagamento,
    string? CartaoToken,
    string? CartaoParcelas,
    string? CartaoEmissorId,
    string? CartaoMetodoPagamentoId,
    string CpfPagador,
    string EmailPagador
);

public record PedidoRespostaDto(
    int Id,
    decimal ValorTotal,
    FormaPagamento FormaPagamento,
    StatusPedido Status,
    string? PixQrCode,
    string? PixQrCodeBase64
);
