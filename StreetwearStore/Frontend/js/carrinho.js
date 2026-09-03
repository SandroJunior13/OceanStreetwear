const listaCarrinho = document.querySelector('[data-lista-carrinho]');

if (listaCarrinho) {
  function renderizarCarrinho() {
    const carrinho = lerCarrinho();

    if (carrinho.length === 0) {
      listaCarrinho.innerHTML = '<p>Sua sacola está vazia. <a href="index.html">Ver produtos</a></p>';
      document.querySelector('[data-resumo-pedido]')?.style.setProperty('display', 'none');
      return;
    }

    listaCarrinho.innerHTML = carrinho.map((item, indice) => `
      <div class="item-carrinho">
        <div class="item-carrinho-imagem"></div>
        <div>
          <strong>${item.nome}</strong><br>
          <span style="color: var(--cinza-texto);">Tamanho: ${item.tamanho}</span>
          <div class="seletor-quantidade" style="margin-top:8px;">
            <button type="button" data-diminuir-item="${indice}">−</button>
            <input type="number" min="1" value="${item.quantidade}" data-quantidade-item="${indice}">
            <button type="button" data-aumentar-item="${indice}">+</button>
          </div>
        </div>
        <strong>${formatarPreco(item.preco * item.quantidade)}</strong>
        <button type="button" class="item-carrinho-remover" data-remover-item="${indice}">Remover</button>
      </div>
    `).join('');

    const total = carrinho.reduce((soma, item) => soma + item.preco * item.quantidade, 0);
    const resumo = document.querySelector('[data-resumo-pedido]');
    if (resumo) {
      resumo.style.display = '';
      resumo.querySelector('[data-total-pedido]').textContent = formatarPreco(total);
    }

    listaCarrinho.querySelectorAll('[data-remover-item]').forEach(botao => {
      botao.addEventListener('click', () => {
        const carrinhoAtual = lerCarrinho();
        carrinhoAtual.splice(Number(botao.dataset.removerItem), 1);
        salvarCarrinho(carrinhoAtual);
        renderizarCarrinho();
      });
    });

    listaCarrinho.querySelectorAll('[data-aumentar-item]').forEach(botao => {
      botao.addEventListener('click', () => {
        const carrinhoAtual = lerCarrinho();
        carrinhoAtual[Number(botao.dataset.aumentarItem)].quantidade += 1;
        salvarCarrinho(carrinhoAtual);
        renderizarCarrinho();
      });
    });

    listaCarrinho.querySelectorAll('[data-diminuir-item]').forEach(botao => {
      botao.addEventListener('click', () => {
        const carrinhoAtual = lerCarrinho();
        const indice = Number(botao.dataset.diminuirItem);
        carrinhoAtual[indice].quantidade = Math.max(1, carrinhoAtual[indice].quantidade - 1);
        salvarCarrinho(carrinhoAtual);
        renderizarCarrinho();
      });
    });
  }

  renderizarCarrinho();

  document.querySelector('[data-ir-checkout]')?.addEventListener('click', () => {
    if (!estaLogado()) {
      window.location.href = 'login.html?depois=checkout';
      return;
    }
    window.location.href = 'checkout.html';
  });
}
