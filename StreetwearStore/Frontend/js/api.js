// Endereço do backend. Se você rodar o backend com "dotnet run", a própria
// API já serve este site (então isso funciona como está). Se preferir rodar
// o frontend separado, troque pela URL mostrada no terminal do backend.
const API_BASE = '/api';

function pegarToken() {
  return localStorage.getItem('loja_token');
}

function salvarSessao(token, nome, email) {
  localStorage.setItem('loja_token', token);
  localStorage.setItem('loja_nome', nome);
  localStorage.setItem('loja_email', email);
}

function encerrarSessao() {
  localStorage.removeItem('loja_token');
  localStorage.removeItem('loja_nome');
  localStorage.removeItem('loja_email');
}

function estaLogado() {
  return !!pegarToken();
}

// Faz uma chamada à API, já incluindo o token de login quando existir.
async function chamarApi(caminho, opcoes = {}) {
  const cabecalhos = { 'Content-Type': 'application/json', ...(opcoes.headers || {}) };
  const token = pegarToken();
  if (token) cabecalhos['Authorization'] = `Bearer ${token}`;

  const resposta = await fetch(`${API_BASE}${caminho}`, { ...opcoes, headers: cabecalhos });
  const dados = await resposta.json().catch(() => null);

  if (!resposta.ok) {
    const mensagem = dados?.erro || 'Não foi possível completar a operação.';
    throw new Error(mensagem);
  }
  return dados;
}

function formatarPreco(valor) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

// Atualiza o número mostrado no ícone do carrinho, em todas as páginas
function atualizarContadorCarrinho() {
  const carrinho = JSON.parse(localStorage.getItem('loja_carrinho') || '[]');
  const total = carrinho.reduce((soma, item) => soma + item.quantidade, 0);
  document.querySelectorAll('[data-contador-carrinho]').forEach(el => (el.textContent = total));
}

// Atualiza a área de login/usuário do cabeçalho, em todas as páginas
function atualizarAreaUsuario() {
  const area = document.querySelector('[data-area-usuario]');
  if (!area) return;

  if (estaLogado()) {
    const nome = localStorage.getItem('loja_nome') || 'Minha conta';
    area.innerHTML = `<span>Olá, ${nome.split(' ')[0]}</span> · <a href="#" data-sair>Sair</a>`;
    area.querySelector('[data-sair]').addEventListener('click', (ev) => {
      ev.preventDefault();
      encerrarSessao();
      window.location.href = 'index.html';
    });
  } else {
    area.innerHTML = `<a href="login.html">Entrar</a> · <a href="cadastro.html">Cadastrar</a>`;
  }
}

document.addEventListener('DOMContentLoaded', () => {
  atualizarContadorCarrinho();
  atualizarAreaUsuario();
});
