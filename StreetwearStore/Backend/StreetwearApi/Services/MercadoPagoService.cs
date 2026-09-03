using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using StreetwearApi.Models;

namespace StreetwearApi.Services;

// Resultado padronizado que devolvemos pro resto da aplicação, não importa a forma de pagamento
public class ResultadoPagamento
{
    public bool Aprovado { get; set; }
    public string? IdPagamentoExterno { get; set; }
    public string? StatusDetalhe { get; set; }
    public string? PixQrCode { get; set; }
    public string? PixQrCodeBase64 { get; set; }
}

// Fala diretamente com a API REST do Mercado Pago (https://api.mercadopago.com).
// Não usa o SDK oficial em NuGet para não depender de um pacote extra: é só HttpClient.
public class MercadoPagoService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(HttpClient http, IConfiguration config, ILogger<MercadoPagoService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;

        var accessToken = _config["MercadoPago:AccessToken"];
        _http.BaseAddress = new Uri("https://api.mercadopago.com/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<ResultadoPagamento> PagarComPixAsync(Pedido pedido, string emailPagador, string cpfPagador)
    {
        var corpo = new
        {
            transaction_amount = pedido.ValorTotal,
            description = $"Pedido #{pedido.Id} - Loja Streetwear",
            payment_method_id = "pix",
            payer = new
            {
                email = emailPagador,
                identification = new { type = "CPF", number = SomenteNumeros(cpfPagador) }
            }
        };

        var resposta = await _http.PostAsJsonAsync("v1/payments", corpo);
        var json = await resposta.Content.ReadFromJsonAsync<MpPaymentResponse>();

        if (!resposta.IsSuccessStatusCode || json is null)
        {
            _logger.LogWarning("Falha ao criar pagamento Pix no Mercado Pago: {Status}", resposta.StatusCode);
            return new ResultadoPagamento { Aprovado = false, StatusDetalhe = "erro_ao_gerar_pix" };
        }

        var pix = json.point_of_interaction?.transaction_data;

        return new ResultadoPagamento
        {
            // Pix fica "pending" até o cliente pagar o QR Code; não é recusa
            Aprovado = json.status is "approved" or "pending" or "in_process",
            IdPagamentoExterno = json.id?.ToString(),
            StatusDetalhe = json.status_detail,
            PixQrCode = pix?.qr_code,
            PixQrCodeBase64 = pix?.qr_code_base64
        };
    }

    public async Task<ResultadoPagamento> PagarComCartaoAsync(Pedido pedido, string cartaoToken, int parcelas, string metodoPagamentoId, string emailPagador, string cpfPagador)
    {
        var corpo = new
        {
            transaction_amount = pedido.ValorTotal,
            token = cartaoToken,
            description = $"Pedido #{pedido.Id} - Loja Streetwear",
            installments = parcelas <= 0 ? 1 : parcelas,
            payment_method_id = metodoPagamentoId,
            payer = new
            {
                email = emailPagador,
                identification = new { type = "CPF", number = SomenteNumeros(cpfPagador) }
            }
        };

        var resposta = await _http.PostAsJsonAsync("v1/payments", corpo);
        var json = await resposta.Content.ReadFromJsonAsync<MpPaymentResponse>();

        if (json is null)
        {
            return new ResultadoPagamento { Aprovado = false, StatusDetalhe = "erro_ao_processar_cartao" };
        }

        return new ResultadoPagamento
        {
            Aprovado = json.status == "approved",
            IdPagamentoExterno = json.id?.ToString(),
            StatusDetalhe = json.status_detail
        };
    }

    private static string SomenteNumeros(string valor) => new(valor.Where(char.IsDigit).ToArray());

    // Classes só para ler a resposta JSON do Mercado Pago
    private class MpPaymentResponse
    {
        public long? id { get; set; }
        public string? status { get; set; }
        public string? status_detail { get; set; }
        public MpPointOfInteraction? point_of_interaction { get; set; }
    }

    private class MpPointOfInteraction
    {
        public MpTransactionData? transaction_data { get; set; }
    }

    private class MpTransactionData
    {
        public string? qr_code { get; set; }
        public string? qr_code_base64 { get; set; }
    }
}
