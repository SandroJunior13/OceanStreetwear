# CONCRETO — Loja Streetwear (projeto completo)

Site de loja de roupas streetwear com **backend em C# (ASP.NET Core)** e
**frontend em HTML, CSS e JavaScript puro**. Tem cadastro, login, vitrine de
produtos, sacola de compras e pagamento de verdade via **Pix e Cartão de
crédito** usando o **Mercado Pago**.

```
StreetwearStore/
├── Backend/StreetwearApi/   → API em C# (.NET 8)
└── Frontend/                → HTML/CSS/JS (servido pela própria API)
```

## 1. O que você precisa instalar

- **.NET 8 SDK** — baixe em https://dotnet.microsoft.com/download (se já usa
  Visual Studio, é só confirmar que a versão 8 está instalada).
- Não precisa instalar banco de dados: o projeto usa **SQLite**, que é só um
  arquivo (`loja.db`) criado automaticamente na primeira execução.

## 2. Configurar as chaves antes de rodar

Abra `Backend/StreetwearApi/appsettings.json` e troque:

- `"Jwt:ChaveSecreta"` → qualquer texto longo e aleatório só seu (usado para
  assinar o login). Não precisa decorar, só não pode ficar vazio.
- `"MercadoPago:AccessToken"` → seu **Access Token** do Mercado Pago
  (comece com o de teste). Pegue em:
  https://www.mercadopago.com.br/developers/panel/app → sua aplicação →
  "Credenciais de teste".

Depois, abra `Frontend/js/checkout.js` e troque `MP_PUBLIC_KEY` pela sua
**Public Key** (na mesma tela do Mercado Pago, mas essa pode ficar exposta no
site, é diferente do Access Token).

> Enquanto você não configurar as chaves reais, cadastro/login e a vitrine
> funcionam normalmente — só o pagamento (Pix/Cartão) vai falhar, porque
> precisa de credenciais válidas do Mercado Pago.

## 3. Rodar o projeto

No terminal (ou Visual Studio, abrindo `StreetwearApi.csproj`):

```bash
cd Backend/StreetwearApi
dotnet restore
dotnet run
```

O terminal vai mostrar algo como `Now listening on: https://localhost:7xxx`.
Abra esse endereço no navegador — **o próprio backend já serve o site**
(a pasta `Frontend` inteira), então você não precisa configurar mais nada
separado.

Na primeira execução, o arquivo `loja.db` é criado sozinho, já com 4 produtos
de exemplo cadastrados (você pode editar/adicionar produtos direto no banco,
ou depois construir uma tela de administração).

## 4. Como o projeto é organizado

**Backend (`Backend/StreetwearApi`)**
- `Models/` — as "tabelas" do sistema: `Usuario`, `Produto`, `Pedido`, `ItemPedido`.
- `Data/AppDbContext.cs` — configuração do banco de dados (Entity Framework Core).
- `Services/SenhaService.cs` — gera e confere hash de senha (a senha nunca é
  guardada em texto puro).
- `Services/TokenService.cs` — gera o token de login (JWT).
- `Services/MercadoPagoService.cs` — fala com a API do Mercado Pago para
  cobrar Pix e Cartão.
- `Program.cs` — onde tudo se conecta: as rotas da API (`/api/...`).

**Frontend (`Frontend/`)**
- `index.html` — vitrine da loja.
- `produto.html` — página de um produto (escolher tamanho/quantidade).
- `carrinho.html` — sacola de compras.
- `login.html` / `cadastro.html` — autenticação.
- `checkout.html` — pagamento (Pix ou Cartão).
- `js/api.js` — funções compartilhadas de acesso à API e de sessão do usuário.

## 5. Segurança e boas práticas já aplicadas

- A senha do cliente nunca é enviada nem guardada em texto puro (usamos
  hash + salt com PBKDF2).
- O número do cartão de crédito **nunca chega ao seu backend**: ele é
  transformado em um "token" pelo próprio Mercado Pago, direto no navegador
  do cliente, e é esse token que a API recebe.
- O login usa JWT (token com validade de 7 dias); troque a chave secreta
  antes de colocar em produção.

## 6. Próximos passos possíveis (não incluídos aqui)

- Tela de administração para cadastrar/editar produtos e fotos reais.
- E-mail de confirmação de pedido.
- Webhook do Mercado Pago para atualizar o status do Pix automaticamente
  quando o cliente pagar (hoje o pedido fica como "AguardandoPagamento" até
  você conferir manualmente ou implementar esse webhook).
- Publicar o site em um serviço de hospedagem (Azure App Service, Railway,
  Render, etc.) para ficar acessível na internet.
