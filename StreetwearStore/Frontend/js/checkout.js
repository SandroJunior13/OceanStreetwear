// CHAVE PÚBLICA do Mercado Pago (pode ficar exposta no frontend, é diferente do Access Token).
// Pegue a sua em: https://www.mercadopago.com.br/developers/panel/app
const MP_PUBLIC_KEY = 'COLOQUE_AQUI_SUA_PUBLIC_KEY_DE_TESTE_OU_PRODUCAO';

const painelCheckout = document.querySelector('[data-checkout]');

if (painelCheckout) {
  if (!estaLogado()) {
    window.location.href = 'login.html?depois=checkout';
  }

  const carrinho = lerCarrinho();
  if (carrinho.length === 0) {
    window.location.href = 'carrinho.html';
  }

  const total = carrinho.reduce((soma, item) => soma + item.preco * item.quantidade, 0);
  document.querySelector('[data-total-checkout]').textContent = formatarPreco(total);

  let mp = null;
  let cardForm = null;
  if (window.MercadoPago) {
    mp = new MercadoPago(MP_PUBLIC_KEY, { locale: 'pt-BR' });
  }

  // ---------- Alternar entre as abas Pix / Cartão ----------
  document.querySelectorAll('[data-aba-pagamento]').forEach(aba => {
    aba.addEventListener('click', () => {
      document.querySelectorAll('[data-aba-pagamento]').forEach(a => a.classList.remove('ativa'));
      document.querySelectorAll('[data-painel-pagamento]').forEach(p => p.classList.remove('ativo'));
      aba.classList.add('ativa');
      document.querySelector(`[data-painel-pagamento="${aba.dataset.abaPagamento}"]`).classList.add('ativo');

      if (aba.dataset.abaPagamento === 'cartao' && mp && !cardForm) {
        montarFormularioCartao();
      }
    });
  });

  function montarFormularioCartao() {
    cardForm = mp.cardForm({
      amount: String(total.toFixed(2)),
      form: {
        id: 'form-cartao',
        cardNumber: { id: 'campo-numero-cartao', placeholder: 'Número do cartão' },
        expirationDate: { id: 'campo-validade-cartao', placeholder: 'MM/AA' },
        securityCode: { id: 'campo-cvv-cartao', placeholder: 'CVV' },
        cardholderName: { id: 'campo-nome-cartao', placeholder: 'Nome impresso no cartão' },
        issuer: { id: 'campo-banco-emissor' },
        installments: { id: 'campo-parcelas' },
        identificationType: { id: 'campo-tipo-documento' },
        identificationNumber: { id: 'campo-cpf-cartao', placeholder: 'CPF do titular' },
        cardholderEmail: { id: 'campo-email-cartao', placeholder: 'E-mail' },
      },
      callbacks: {
        onFormMounted: erro => {
          if (erro) console.warn('Erro ao montar formulário de cartão:', erro);
        },
        onError: erro => console.warn('Erro no formulário de cartão:', erro),
      },
    });
  }

  // ---------- Envio: Pix ----------
  document.querySelector('[data-pagar-pix]')?.addEventListener('click', async (ev) => {
    const botao = ev.currentTarget;
    const mensagem = document.querySelector('[data-mensagem-checkout]');
    const email = document.querySelector('[data-email-pix]').value.trim();
    const cpf = document.querySelector('[data-cpf-pix]').value.trim();

    if (!email || !cpf) {
      mensagem.textContent = 'Preencha e-mail e CPF para gerar o Pix.';
      mensagem.style.color = 'var(--ferrugem)';
      return;
    }

    botao.disabled = true;
    botao.textContent = 'Gerando Pix…';

    try {
      const pedido = await enviarPedido('Pix', { emailPagador: email, cpfPagador: cpf });
      mostrarQrCodePix(pedido);
    } catch (erro) {
      mensagem.textContent = erro.message;
      mensagem.style.color = 'var(--ferrugem)';
      botao.disabled = false;
      botao.textContent = 'Gerar Pix';
    }
  });

  function mostrarQrCodePix(pedido) {
    document.querySelector('[data-painel-pagamento="pix"]').innerHTML = `
      <div class="caixa-pix">
        <p><strong>Escaneie o QR Code no app do seu banco</strong></p>
        ${pedido.pixQrCodeBase64
          ? `<img class="qr-pix" src="data:image/png;base64,${pedido.pixQrCodeBase64}" alt="QR Code Pix">`
          : '<div class="qr-pix"></div>'}
        <p>Ou use o Pix Copia e Cola:</p>
        <div class="codigo-copia-cola">${pedido.pixQrCode || 'Código indisponível'}</div>
        <p style="font-size:0.85rem;color:var(--cinza-texto);">Assim que o pagamento for confirmado pelo seu banco, o pedido #${pedido.id} é atualizado automaticamente.</p>
      </div>
    `;
    localStorage.removeItem('loja_carrinho');
  }

  // ---------- Envio: Cartão ----------
  document.querySelector('[data-pagar-cartao]')?.addEventListener('click', async () => {
    const mensagem = document.querySelector('[data-mensagem-checkout]');

    if (!cardForm) {
      mensagem.textContent = 'Preencha os dados do cartão antes de continuar.';
      mensagem.style.color = 'var(--ferrugem)';
      return;
    }

    try {
      const { token, paymentMethodId, issuerId, installments, cardholderEmail, identificationNumber } =
        cardForm.getCardFormData();

      const pedido = await enviarPedido('Cartao', {
        cartaoToken: token,
        cartaoParcelas: installments,
        cartaoEmissorId: issuerId,
        cartaoMetodoPagamentoId: paymentMethodId,
        emailPagador: cardholderEmail,
        cpfPagador: identificationNumber,
      });

      if (pedido.status === 'Pago') {
        document.querySelector('[data-painel-pagamento="cartao"]').innerHTML =
          `<p><strong>Pagamento aprovado!</strong> Pedido #${pedido.id} confirmado.</p>`;
        localStorage.removeItem('loja_carrinho');
      } else {
        mensagem.textContent = 'O pagamento foi recusado pela operadora do cartão. Tente outro cartão.';
        mensagem.style.color = 'var(--ferrugem)';
      }
    } catch (erro) {
      mensagem.textContent = erro.message;
      mensagem.style.color = 'var(--ferrugem)';
    }
  });

  async function enviarPedido(formaPagamento, dadosPagamento) {
    const itens = carrinho.map(item => ({
      produtoId: item.produtoId,
      tamanho: item.tamanho,
      quantidade: item.quantidade,
    }));

    return chamarApi('/pedidos', {
      method: 'POST',
      body: JSON.stringify({
        itens,
        formaPagamento,
        ...dadosPagamento,
      }),
    });
  }
}
