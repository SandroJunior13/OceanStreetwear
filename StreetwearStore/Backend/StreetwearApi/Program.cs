using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using StreetwearApi.Data;
using StreetwearApi.Dtos;
using StreetwearApi.Models;
using StreetwearApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Banco de dados (SQLite - cria um arquivo loja.db sozinho) ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Padrao") ?? "Data Source=loja.db"));

// ---------- Serviços da aplicação ----------
builder.Services.AddScoped<SenhaService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHttpClient<MercadoPagoService>();

// ---------- Autenticação por token (JWT) ----------
var chaveJwt = builder.Configuration["Jwt:ChaveSecreta"] ?? "troque-esta-chave-em-producao-0123456789";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt))
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS: libera o frontend acessar a API ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Cria o banco e aplica as migrações automaticamente ao iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Cria o arquivo loja.db e as tabelas automaticamente, sem precisar rodar
    // "dotnet ef migrations" manualmente — ótimo para começar rápido.
    // Se você evoluir o projeto e quiser migrações de verdade, troque por
    // db.Database.Migrate() e crie as migrations com a ferramenta dotnet-ef.
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

// Serve o site (HTML/CSS/JS) que está na pasta Frontend, um nível acima do backend
var pastaFrontend = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "Frontend");
if (Directory.Exists(pastaFrontend))
{
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = new PhysicalFileProvider(pastaFrontend) });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(pastaFrontend) });
}

app.UseAuthentication();
app.UseAuthorization();

// ======================= ENDPOINTS =======================

var api = app.MapGroup("/api");

// ---------- Autenticação ----------
api.MapPost("/auth/cadastrar", async (CadastroDto dto, AppDbContext db, SenhaService senhaService, TokenService tokenService) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Senha))
        return Results.BadRequest(new { erro = "Preencha nome, e-mail e senha." });

    if (dto.Senha.Length < 6)
        return Results.BadRequest(new { erro = "A senha precisa ter pelo menos 6 caracteres." });

    if (await db.Usuarios.AnyAsync(u => u.Email == dto.Email))
        return Results.BadRequest(new { erro = "Já existe uma conta com esse e-mail." });

    var (hash, salt) = senhaService.GerarHash(dto.Senha);
    var usuario = new Usuario { Nome = dto.Nome, Email = dto.Email, SenhaHash = hash, SenhaSalt = salt };

    db.Usuarios.Add(usuario);
    await db.SaveChangesAsync();

    var token = tokenService.GerarToken(usuario);
    return Results.Ok(new TokenRespostaDto(token, usuario.Nome, usuario.Email));
});

api.MapPost("/auth/login", async (LoginDto dto, AppDbContext db, SenhaService senhaService, TokenService tokenService) =>
{
    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (usuario is null || !senhaService.VerificarSenha(dto.Senha, usuario.SenhaHash, usuario.SenhaSalt))
        return Results.Json(new { erro = "E-mail ou senha inválidos." }, statusCode: 401);

    var token = tokenService.GerarToken(usuario);
    return Results.Ok(new TokenRespostaDto(token, usuario.Nome, usuario.Email));
});

// ---------- Produtos (vitrine da loja, público) ----------
api.MapGet("/produtos", async (AppDbContext db, string? categoria) =>
{
    var query = db.Produtos.Where(p => p.Ativo);
    if (!string.IsNullOrWhiteSpace(categoria))
        query = query.Where(p => p.Categoria == categoria);

    return Results.Ok(await query.ToListAsync());
});

api.MapGet("/produtos/{id:int}", async (int id, AppDbContext db) =>
    await db.Produtos.FindAsync(id) is Produto produto ? Results.Ok(produto) : Results.NotFound());

// ---------- Pedidos / Checkout (precisa estar logado) ----------
api.MapPost("/pedidos", async (
        NovoPedidoDto dto,
        ClaimsPrincipal usuarioLogado,
        AppDbContext db,
        MercadoPagoService pagamentos) =>
    {
        var usuarioId = int.Parse(usuarioLogado.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (dto.Itens is null || dto.Itens.Count == 0)
            return Results.BadRequest(new { erro = "O carrinho está vazio." });

        var pedido = new Pedido { UsuarioId = usuarioId, FormaPagamento = dto.FormaPagamento };

        foreach (var itemDto in dto.Itens)
        {
            var produto = await db.Produtos.FindAsync(itemDto.ProdutoId);
            if (produto is null)
                return Results.BadRequest(new { erro = $"Produto {itemDto.ProdutoId} não encontrado." });

            pedido.Itens.Add(new ItemPedido
            {
                ProdutoId = produto.Id,
                Tamanho = itemDto.Tamanho,
                Quantidade = itemDto.Quantidade,
                PrecoUnitario = produto.Preco
            });
        }

        pedido.ValorTotal = pedido.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(); // salva primeiro pra ter o Id do pedido

        // Cobra de verdade no Mercado Pago
        var resultado = dto.FormaPagamento == FormaPagamento.Pix
            ? await pagamentos.PagarComPixAsync(pedido, dto.EmailPagador, dto.CpfPagador)
            : await pagamentos.PagarComCartaoAsync(
                pedido,
                dto.CartaoToken ?? "",
                int.TryParse(dto.CartaoParcelas, out var p) ? p : 1,
                dto.CartaoMetodoPagamentoId ?? "",
                dto.EmailPagador,
                dto.CpfPagador);

        pedido.PagamentoExternoId = resultado.IdPagamentoExterno;
        pedido.PixQrCode = resultado.PixQrCode;
        pedido.PixQrCodeBase64 = resultado.PixQrCodeBase64;
        pedido.Status = resultado.Aprovado
            ? (dto.FormaPagamento == FormaPagamento.Pix ? StatusPedido.AguardandoPagamento : StatusPedido.Pago)
            : StatusPedido.Recusado;

        await db.SaveChangesAsync();

        return Results.Ok(new PedidoRespostaDto(pedido.Id, pedido.ValorTotal, pedido.FormaPagamento, pedido.Status, pedido.PixQrCode, pedido.PixQrCodeBase64));
    })
    .RequireAuthorization();

api.MapGet("/pedidos/meus", async (ClaimsPrincipal usuarioLogado, AppDbContext db) =>
{
    var usuarioId = int.Parse(usuarioLogado.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var pedidos = await db.Pedidos
        .Where(p => p.UsuarioId == usuarioId)
        .Include(p => p.Itens)
        .OrderByDescending(p => p.CriadoEm)
        .ToListAsync();

    return Results.Ok(pedidos);
}).RequireAuthorization();

app.Run();
