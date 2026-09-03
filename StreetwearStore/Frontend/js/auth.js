function mostrarErro(mensagem) {
  const caixa = document.querySelector('[data-erro]');
  if (!caixa) return;
  caixa.textContent = mensagem;
  caixa.classList.add('visivel');
}

function esconderErro() {
  const caixa = document.querySelector('[data-erro]');
  if (caixa) caixa.classList.remove('visivel');
}

const formularioLogin = document.querySelector('[data-form-login]');
if (formularioLogin) {
  formularioLogin.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    esconderErro();

    const email = formularioLogin.email.value.trim();
    const senha = formularioLogin.senha.value;

    try {
      const resposta = await chamarApi('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, senha }),
      });
      salvarSessao(resposta.token, resposta.nome, resposta.email);
      window.location.href = 'index.html';
    } catch (erro) {
      mostrarErro(erro.message);
    }
  });
}

const formularioCadastro = document.querySelector('[data-form-cadastro]');
if (formularioCadastro) {
  formularioCadastro.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    esconderErro();

    const nome = formularioCadastro.nome.value.trim();
    const email = formularioCadastro.email.value.trim();
    const senha = formularioCadastro.senha.value;
    const confirmarSenha = formularioCadastro.confirmarSenha.value;

    if (senha !== confirmarSenha) {
      mostrarErro('As senhas não conferem.');
      return;
    }

    try {
      const resposta = await chamarApi('/auth/cadastrar', {
        method: 'POST',
        body: JSON.stringify({ nome, email, senha }),
      });
      salvarSessao(resposta.token, resposta.nome, resposta.email);
      window.location.href = 'index.html';
    } catch (erro) {
      mostrarErro(erro.message);
    }
  });
}
