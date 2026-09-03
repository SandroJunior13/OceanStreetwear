// ---------- Carrinho (guardado no navegador, junta-se ao pedido só no checkout) ----------
function lerCarrinho() {
  return JSON.parse(localStorage.getItem('loja_carrinho') || '[]');
}

function salvarCarrinho(carrinho) {
  localStorage.setItem('loja_carrinho', JSON.stringify(carrinho));
  atualizarContadorCarrinho();
}

function adicionarAoCarrinho(produto, tamanho, quantidade) {
  const carrinho = lerCarrinho();
  const existente = carrinho.find(i => i.produtoId === produto.id && i.tamanho === tamanho);

  if (existente) {
    existente.quantidade += quantidade;
  } else {
    carrinho.push({
      produtoId: produto.id,
      nome: produto.nome,
      preco: produto.preco,
      tamanho,
      quantidade,
    });
  }

  salvarCarrinho(carrinho);
}

// ---------- Vitrine (index.html) ----------
const grade = document.querySelector('[data-grade-produtos]');
if (grade) {
  let categoriaAtual = '';

  async function carregarProdutos() {
    grade.innerHTML = '<p>Carregando produtos…</p>';
    try {
      const query = categoriaAtual ? `?categoria=${encodeURIComponent(categoriaAtual)}` : '';
      const produtos = await chamarApi(`/produtos${query}`);
      renderizarProdutos(produtos);
    } catch (erro) {
      grade.innerHTML = `<p>Não foi possível carregar os produtos agora. Tente recarregar a página.</p>`;
    }
  }

  function renderizarProdutos(produtos) {
    if (produtos.length === 0) {
      grade.innerHTML = '<p>Nenhum produto encontrado nessa categoria.</p>';
      return;
    }

    grade.innerHTML = produtos.map(p => `
      <a class="cartao-produto" href="produto.html?id=${p.id}">
        <div class="cartao-produto-imagem">${p.nome}</div>
        <div class="cartao-produto-corpo">
          <span class="cartao-produto-categoria">${p.categoria}</span>
          <span class="cartao-produto-nome">${p.nome}</span>
          <span class="cartao-produto-preco">${formatarPreco(p.preco)}</span>
        </div>
      </a>
    `).join('');
  }

  document.querySelectorAll('[data-filtro-categoria]').forEach(botao => {
    botao.addEventListener('click', () => {
      document.querySelectorAll('[data-filtro-categoria]').forEach(b => b.classList.remove('ativo'));
      botao.classList.add('ativo');
      categoriaAtual = botao.dataset.filtroCategoria;
      carregarProdutos();
    });
  });

  carregarProdutos();
}

// ---------- Página de produto (produto.html) ----------
const painelProduto = document.querySelector('[data-painel-produto]');
if (painelProduto) {
  const idProduto = new URLSearchParams(window.location.search).get('id');
  let produtoAtual = null;
  let tamanhoSelecionado = null;

  async function carregarProduto() {
    try {
      produtoAtual = await chamarApi(`/produtos/${idProduto}`);
      renderizarProduto();
    } catch (erro) {
      painelProduto.innerHTML = '<p>Produto não encontrado.</p>';
    }
  }

  function renderizarProduto() {
    const tamanhos = produtoAtual.tamanhosDisponiveis.split(',');
    document.title = `${produtoAtual.nome} — Loja Streetwear`;

    painelProduto.innerHTML = `
      <div class="produto-imagem-grande">${produtoAtual.nome}</div>
      <div class="produto-info">
        <span class="cartao-produto-categoria">${produtoAtual.categoria}</span>
        <h1>${produtoAtual.nome}</h1>
        <p class="produto-descricao">${produtoAtual.descricao}</p>
        <div class="produto-preco-grande">${formatarPreco(produtoAtual.preco)}</div>

        <div class="seletor-tamanho" data-seletor-tamanho>
          ${tamanhos.map(t => `<button type="button" class="opcao-tamanho" data-tamanho="${t}">${t}</button>`).join('')}
        </div>

        <div class="linha-acoes">
          <div class="seletor-quantidade">
            <button type="button" data-diminuir>−</button>
            <input type="number" min="1" value="1" data-quantidade>
            <button type="button" data-aumentar>+</button>
          </div>
          <button type="button" class="botao botao--sinalizacao" data-adicionar-carrinho>Adicionar à sacola</button>
        </div>
        <p data-mensagem-produto style="font-weight:700;"></p>
      </div>
    `;

    painelProduto.querySelectorAll('[data-tamanho]').forEach(botao => {
      botao.addEventListener('click', () => {
        painelProduto.querySelectorAll('[data-tamanho]').forEach(b => b.classList.remove('selecionado'));
        botao.classList.add('selecionado');
        tamanhoSelecionado = botao.dataset.tamanho;
      });
    });

    const inputQuantidade = painelProduto.querySelector('[data-quantidade]');
    painelProduto.querySelector('[data-aumentar]').addEventListener('click', () => {
      inputQuantidade.value = Math.max(1, parseInt(inputQuantidade.value || '1') + 1);
    });
    painelProduto.querySelector('[data-diminuir]').addEventListener('click', () => {
      inputQuantidade.value = Math.max(1, parseInt(inputQuantidade.value || '1') - 1);
    });

    painelProduto.querySelector('[data-adicionar-carrinho]').addEventListener('click', () => {
      const mensagem = painelProduto.querySelector('[data-mensagem-produto]');
      if (!tamanhoSelecionado) {
        mensagem.textContent = 'Escolha um tamanho antes de adicionar à sacola.';
        mensagem.style.color = 'var(--ferrugem)';
        return;
      }
      const quantidade = Math.max(1, parseInt(inputQuantidade.value || '1'));
      adicionarAoCarrinho(produtoAtual, tamanhoSelecionado, quantidade);
      mensagem.textContent = 'Adicionado à sacola!';
      mensagem.style.color = 'var(--tinta)';
    });
  }

  carregarProduto();
}
