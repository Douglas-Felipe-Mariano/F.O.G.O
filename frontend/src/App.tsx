import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import MapaEquipamentos, { LocalizacaoEquipamento } from './MapaEquipamentos';
import './App.css';
import './Visoes.css';

type SituacaoEquipamento = 'aindaNaoInstalado' | 'operacional' | 'mauFuncionamento' | 'emManutencao' | 'desativado' | 'furtado';
type TipoManutencao = 'imediata' | 'agendada' | 'periodica';
type OrigemRegistro = 'manual' | 'importado' | 'simulado';

type Equipamento = {
  id: string;
  tipo: string;
  numeroSerie: string;
  marca: string | null;
  modelo: string | null;
  cep: string;
  bairro: string;
  cidade: string;
  uf: string;
  rua: string;
  referencia: string | null;
  latitude: number | null;
  longitude: number | null;
  situacao: SituacaoEquipamento;
  necessitaManutencao: boolean;
  tipoManutencao: TipoManutencao | null;
  proximaManutencao: string | null;
  origemDados: OrigemRegistro;
  criadoEm: string;
  atualizadoEm: string;
};

type EquipamentoRequisicao = Omit<Equipamento, 'id' | 'criadoEm' | 'atualizadoEm'>;
type PaginaEquipamentos = { pagina: number; tamanhoPagina: number; totalRegistros: number; itens: Equipamento[] };
type Indicadores = { totalCadastrado: number; porSituacao: { situacao: SituacaoEquipamento; quantidade: number }[]; necessitamManutencao: number };
type RespostaToken = { tokenAcesso: string; expiraEm: string };

type Filtros = { tipo: string; situacao: string; bairro: string };
type RespostaMapa = { totalComCoordenadas: number; limite: number; resultadoTruncado: boolean; itens: LocalizacaoEquipamento[] };

const estados: { valor: SituacaoEquipamento; rotulo: string }[] = [
  { valor: 'aindaNaoInstalado', rotulo: 'Ainda não instalado' },
  { valor: 'operacional', rotulo: 'Operacional' },
  { valor: 'mauFuncionamento', rotulo: 'Mau funcionamento' },
  { valor: 'emManutencao', rotulo: 'Em manutenção' },
  { valor: 'desativado', rotulo: 'Desativado' },
  { valor: 'furtado', rotulo: 'Furtado' },
];
const tiposManutencao: { valor: TipoManutencao; rotulo: string }[] = [
  { valor: 'imediata', rotulo: 'Imediata' },
  { valor: 'agendada', rotulo: 'Agendada' },
  { valor: 'periodica', rotulo: 'Periódica' },
];
const situacaoLabel = new Map(estados.map(({ valor, rotulo }) => [valor, rotulo]));
const origemLabel: Record<OrigemRegistro, string> = { manual: 'Manual', importado: 'Importado', simulado: 'Simulado' };
const vazioFiltros: Filtros = { tipo: '', situacao: '', bairro: '' };
const vazioFormulario = (): EquipamentoRequisicao => ({
  tipo: '', numeroSerie: '', marca: '', modelo: '', cep: '', bairro: '', cidade: '', uf: '', rua: '', referencia: '',
  latitude: null, longitude: null, situacao: 'operacional', necessitaManutencao: false,
  tipoManutencao: null, proximaManutencao: null, origemDados: 'manual',
});

function App() {
  const [token, setToken] = useState<string | null>(null);
  const [itens, setItens] = useState<Equipamento[]>([]);
  const [indicadores, setIndicadores] = useState<Indicadores | null>(null);
  const [mapa, setMapa] = useState<RespostaMapa | null>(null);
  const [carregandoMapa, setCarregandoMapa] = useState(false);
  const [visao, setVisao] = useState<'inventario' | 'mapa'>('inventario');
  const [pagina, setPagina] = useState(1);
  const [total, setTotal] = useState(0);
  const [filtros, setFiltros] = useState<Filtros>(vazioFiltros);
  const [pesquisa, setPesquisa] = useState<Filtros>(vazioFiltros);
  const [erro, setErro] = useState('');
  const [mensagem, setMensagem] = useState('');
  const [carregando, setCarregando] = useState(false);
  const [modalAberto, setModalAberto] = useState(false);
  const [editando, setEditando] = useState<Equipamento | null>(null);
  const [formulario, setFormulario] = useState<EquipamentoRequisicao>(vazioFormulario);
  const [credenciais, setCredenciais] = useState({ identificador: '', senha: '' });

  const chamarApi = useCallback(async (url: string, init: RequestInit = {}) => {
    const cabecalhos = new Headers(init.headers);
    if (init.body) cabecalhos.set('Content-Type', 'application/json');
    if (token) cabecalhos.set('Authorization', `Bearer ${token}`);
    const resposta = await fetch(url, { ...init, headers: cabecalhos, credentials: 'include' });
    if (!resposta.ok) {
      if (resposta.status === 401 && token) {
        setToken(null);
      }
      const detalhes = await resposta.json().catch(() => null) as { title?: string; errors?: Record<string, string[]> } | null;
      const errosCampos = detalhes?.errors ? Object.values(detalhes.errors).flat().join(' ') : '';
      throw new Error(errosCampos || detalhes?.title || `Falha na requisição (${resposta.status}).`);
    }
    return resposta;
  }, [token]);

  const carregarDados = useCallback(async () => {
    if (!token) return;
    setCarregando(true);
    setErro('');
    try {
      const parametros = new URLSearchParams({ pagina: String(pagina), tamanhoPagina: '25' });
      if (pesquisa.tipo) parametros.set('tipo', pesquisa.tipo);
      if (pesquisa.situacao) parametros.set('situacao', pesquisa.situacao);
      if (pesquisa.bairro) parametros.set('bairro', pesquisa.bairro);
      const [respostaLista, respostaIndicadores] = await Promise.all([
        chamarApi(`/api/equipamentos?${parametros}`),
        chamarApi('/api/indicadores/equipamentos'),
      ]);
      const lista = await respostaLista.json() as PaginaEquipamentos;
      setItens(lista.itens);
      setTotal(lista.totalRegistros);
      setIndicadores(await respostaIndicadores.json() as Indicadores);
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível carregar os equipamentos.');
    } finally {
      setCarregando(false);
    }
  }, [chamarApi, pagina, pesquisa, token]);

  const carregarMapa = useCallback(async (limite = 500) => {
    if (!token) return;
    setCarregandoMapa(true);
    setErro('');
    try {
      const resposta = await chamarApi(`/api/equipamentos/localizacoes?limite=${limite}`);
      setMapa(await resposta.json() as RespostaMapa);
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível carregar as localizações.');
    } finally {
      setCarregandoMapa(false);
    }
  }, [chamarApi, token]);

  useEffect(() => { void carregarDados(); }, [carregarDados]);

  useEffect(() => {
    if (visao === 'mapa' && token) void carregarMapa();
  }, [carregarMapa, token, visao]);

  useEffect(() => {
    let cancelado = false;
    void (async () => {
      try {
        await fetch('/api/autenticacao/csrf', { credentials: 'include' });
        if (cancelado || token) return;
        const csrf = lerCookie('fogo.csrf');
        if (!csrf) return;
        const resposta = await fetch('/api/autenticacao/atualizar', {
          method: 'POST', credentials: 'include', headers: { 'X-CSRF-TOKEN': csrf },
        });
        if (!resposta.ok) return;
        const dados = await resposta.json() as RespostaToken;
        if (!cancelado) setToken(dados.tokenAcesso);
      } catch { /* API indisponível; a página de login permanece acessível. */ }
    })();

    return () => { cancelado = true; };
  }, [token]);

  const bairros = useMemo(() => [...new Set(itens.map((item) => item.bairro))].sort(), [itens]);

  async function entrar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setErro('');
    try {
      const csrf = lerCookie('fogo.csrf');
      if (!csrf) throw new Error('Não foi possível iniciar a proteção CSRF. Atualize a página.');
      const resposta = await fetch('/api/autenticacao/entrar', {
        method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
        body: JSON.stringify(credenciais),
      });
      if (!resposta.ok) throw new Error(resposta.status === 401 ? 'Usuário ou senha inválidos, ou e-mail ainda não confirmado.' : 'Não foi possível entrar. Confira a configuração da API.');
      const dados = await resposta.json() as RespostaToken;
      setToken(dados.tokenAcesso);
      setMensagem('Sessão iniciada.');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Falha ao entrar.');
    }
  }

  async function sair() {
    try {
      const csrf = lerCookie('fogo.csrf');
      const headers = new Headers();
      if (csrf) headers.set('X-CSRF-TOKEN', csrf);
      await chamarApi('/api/autenticacao/sair', {
        method: 'POST', headers,
      });
    } catch { /* Limpa o token de curta duração mesmo se a API estiver indisponível. */ }
    setToken(null);
    setItens([]);
    setIndicadores(null);
  }

  function lerCookie(nome: string) {
    return document.cookie.split('; ').find((parte) => parte.startsWith(`${nome}=`))?.split('=').slice(1).join('=');
  }

  function abrirNovo() {
    setEditando(null);
    setFormulario(vazioFormulario());
    setModalAberto(true);
  }

  function abrirEdicao(equipamento: Equipamento) {
    setEditando(equipamento);
    setFormulario({
      tipo: equipamento.tipo, numeroSerie: equipamento.numeroSerie, marca: equipamento.marca ?? '', modelo: equipamento.modelo ?? '',
      cep: equipamento.cep, bairro: equipamento.bairro, cidade: equipamento.cidade, uf: equipamento.uf, rua: equipamento.rua,
      referencia: equipamento.referencia ?? '', latitude: equipamento.latitude, longitude: equipamento.longitude,
      situacao: equipamento.situacao, necessitaManutencao: equipamento.necessitaManutencao,
      tipoManutencao: equipamento.tipoManutencao, proximaManutencao: equipamento.proximaManutencao,
      origemDados: equipamento.origemDados,
    });
    setModalAberto(true);
  }

  async function salvar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setErro('');
    setMensagem('');
    const corpo = {
      ...formulario,
      marca: formulario.marca || null,
      modelo: formulario.modelo || null,
      referencia: formulario.referencia || null,
      latitude: formulario.latitude === null || Number.isNaN(formulario.latitude) ? null : formulario.latitude,
      longitude: formulario.longitude === null || Number.isNaN(formulario.longitude) ? null : formulario.longitude,
    };
    try {
      await chamarApi(editando ? `/api/equipamentos/${editando.id}` : '/api/equipamentos', {
        method: editando ? 'PUT' : 'POST', body: JSON.stringify(corpo),
      });
      setMensagem(editando ? 'Equipamento atualizado.' : 'Equipamento cadastrado.');
      setModalAberto(false);
      await carregarDados();
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível salvar o equipamento.');
    }
  }

  function atualizarCampo<K extends keyof EquipamentoRequisicao>(campo: K, valor: EquipamentoRequisicao[K]) {
    setFormulario((atual) => ({ ...atual, [campo]: valor }));
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <a className="brand" href="#inicio" aria-label="F.O.G.O. início">
          <span className="brand-mark" aria-hidden="true">F</span>
          <span>F.O.G.O.<small>GESTÃO E OPERAÇÕES</small></span>
        </a>
        <div className="top-actions">
          <span className={`system-status ${token ? 'connected' : ''}`}><i />{token ? 'SESSÃO PROTEGIDA' : 'API AGUARDANDO SESSÃO'}</span>
          {token && <button className="button button-quiet" onClick={() => void sair()}>Encerrar sessão</button>}
        </div>
      </header>

      {!token ? (
        <section className="login-layout" id="inicio">
          <div className="login-copy">
            <p className="eyebrow"><span>PLATAFORMA OPERACIONAL</span> · ACESSO RESTRITO</p>
            <h1>Dados reais.<br /><em>Decisões melhores.</em></h1>
            <p className="intro">Acesse o inventário operacional do F.O.G.O. O cadastro é controlado; solicite acesso ao administrador do sistema.</p>
            <div className="login-notice"><span aria-hidden="true">↳</span> Nenhum dado de demonstração está sendo exibido.</div>
          </div>
          <form className="login-card" onSubmit={(evento) => void entrar(evento)}>
            <div className="card-kicker">01 / AUTENTICAÇÃO</div>
            <h2>Entrar na plataforma</h2>
            <label>Usuário ou e-mail<input autoComplete="username" required value={credenciais.identificador} onChange={(e) => setCredenciais({ ...credenciais, identificador: e.target.value })} /></label>
            <label>Senha<input type="password" autoComplete="current-password" required value={credenciais.senha} onChange={(e) => setCredenciais({ ...credenciais, senha: e.target.value })} /></label>
            {erro && <p className="alert error" role="alert">{erro}</p>}
            <button className="button button-primary" type="submit">Entrar <span aria-hidden="true">↗</span></button>
            <p className="form-hint">A sessão usa token de acesso curto e cookie de atualização protegido.</p>
          </form>
        </section>
      ) : (
        <section className="workspace" id="inicio">
          <div className="workspace-heading">
            <div>
              <p className="eyebrow"><span>INVENTÁRIO</span> · DADOS PERSISTIDOS</p>
              <h1>Visão operacional<span className="heading-period">.</span></h1>
              <p className="intro">Consulte e mantenha os equipamentos cadastrados. Indicadores refletem registros persistidos; não há dados de demonstração.</p>
            </div>
            <button className="button button-primary create-button" onClick={abrirNovo}>＋ Novo equipamento</button>
          </div>

          {erro && <p className="alert error" role="alert">{erro}</p>}
          {mensagem && <p className="alert success" role="status">{mensagem}</p>}

          <div className="metric-grid">
            <article className="metric-card"><span className="metric-label">EQUIPAMENTOS CADASTRADOS</span><strong>{indicadores?.totalCadastrado ?? '—'}</strong><small>total persistido</small></article>
            <article className="metric-card"><span className="metric-label">OPERACIONAIS</span><strong>{indicadores?.porSituacao.find((item) => item.situacao === 'operacional')?.quantidade ?? 0}</strong><small>situação declarada</small></article>
            <article className="metric-card"><span className="metric-label">NECESSITAM MANUTENÇÃO</span><strong>{indicadores?.necessitamManutencao ?? '—'}</strong><small>conforme cadastro</small></article>
          </div>

          <nav className="view-tabs" aria-label="Visões do inventário">
            <button type="button" aria-pressed={visao === 'inventario'} className={visao === 'inventario' ? 'view-tab active' : 'view-tab'} onClick={() => setVisao('inventario')}>Lista de equipamentos</button>
            <button type="button" aria-pressed={visao === 'mapa'} className={visao === 'mapa' ? 'view-tab active' : 'view-tab'} onClick={() => setVisao('mapa')}>Mapa operacional</button>
          </nav>

          {visao === 'inventario' ? <section className="inventory-panel" aria-labelledby="inventory-title">
            <div className="panel-heading">
              <div><p className="eyebrow">BASE DE ATIVOS</p><h2 id="inventory-title">Equipamentos <span>({total})</span></h2></div>
              <button className="button button-quiet refresh-button" onClick={() => void carregarDados()} disabled={carregando}>↻ Atualizar</button>
            </div>
            <form className="filters" onSubmit={(evento) => { evento.preventDefault(); setPagina(1); setPesquisa(filtros); }}>
              <label>Tipo<input placeholder="Ex.: radar" value={filtros.tipo} onChange={(e) => setFiltros({ ...filtros, tipo: e.target.value })} /></label>
              <label>Situação<select value={filtros.situacao} onChange={(e) => setFiltros({ ...filtros, situacao: e.target.value })}><option value="">Todas</option>{estados.map((estado) => <option value={estado.valor} key={estado.valor}>{estado.rotulo}</option>)}</select></label>
              <label>Bairro<input list="bairros-cadastrados" value={filtros.bairro} onChange={(e) => setFiltros({ ...filtros, bairro: e.target.value })} /><datalist id="bairros-cadastrados">{bairros.map((bairro) => <option key={bairro}>{bairro}</option>)}</datalist></label>
              <button className="button button-secondary" type="submit">Aplicar filtros</button>
              <button className="button button-quiet" type="button" onClick={() => { setFiltros(vazioFiltros); setPesquisa(vazioFiltros); setPagina(1); }}>Limpar</button>
            </form>
            {carregando ? <div className="empty-state"><span className="spinner" />Carregando dados persistidos…</div> : itens.length === 0 ? (
              <div className="empty-state"><span className="empty-icon">⌁</span><h3>{total === 0 ? 'Nenhum equipamento cadastrado' : 'Nenhum resultado nesta página'}</h3><p>{total === 0 ? 'O inventário está vazio. Cadastre um equipamento para começar; nenhum dado fictício foi incluído.' : 'Altere os filtros ou navegue para outra página.'}</p>{total === 0 && <button className="button button-primary" onClick={abrirNovo}>Cadastrar primeiro equipamento</button>}</div>
            ) : (
              <div className="table-wrap"><table><thead><tr><th>Equipamento</th><th>Localização</th><th>Situação</th><th>Manutenção</th><th>Origem</th><th><span className="sr-only">Ação</span></th></tr></thead><tbody>{itens.map((item) => (
                <tr key={item.id}>
                  <td><strong>{item.tipo}</strong><span className="cell-secondary">Série {item.numeroSerie}</span><span className="cell-secondary">{[item.marca, item.modelo].filter(Boolean).join(' · ') || 'Marca/modelo não informados'}</span></td>
                  <td>{item.bairro}, {item.cidade}/{item.uf}<span className="cell-secondary">{item.rua}{item.latitude !== null && item.longitude !== null ? ` · ${item.latitude}, ${item.longitude}` : ' · sem coordenadas'}</span></td>
                  <td><span className={`status-pill status-${item.situacao.toLowerCase()}`}>{situacaoLabel.get(item.situacao) ?? item.situacao}</span></td>
                  <td>{item.necessitaManutencao ? <><strong>{tiposManutencao.find((tipo) => tipo.valor === item.tipoManutencao)?.rotulo ?? item.tipoManutencao}</strong><span className="cell-secondary">{item.proximaManutencao ? dataLocal(item.proximaManutencao) : 'sem data prevista'}</span></> : 'Não indicada'}</td>
                  <td><span className={`origin-tag origin-${item.origemDados}`}>{origemLabel[item.origemDados]}</span></td>
                  <td><button className="icon-button" aria-label={`Editar ${item.tipo} ${item.numeroSerie}`} title="Editar equipamento" onClick={() => abrirEdicao(item)}>↗</button></td>
                </tr>
              ))}</tbody></table></div>
            )}
            <div className="pagination"><span>{total ? `${(pagina - 1) * 25 + 1}–${Math.min(pagina * 25, total)} de ${total}` : '0 registros'}</span><div><button className="button button-quiet" disabled={pagina <= 1 || carregando} onClick={() => setPagina((atual) => atual - 1)}>← Anterior</button><span>Página {pagina}</span><button className="button button-quiet" disabled={pagina * 25 >= total || carregando} onClick={() => setPagina((atual) => atual + 1)}>Próxima →</button></div></div>
          </section> : <section className="painel-mapa" aria-labelledby="mapa-title">
            <div className="mapa-cabecalho">
              <div>
                <p className="eyebrow">DISTRIBUIÇÃO GEOGRÁFICA</p>
                <h2 id="mapa-title">Mapa operacional <span>({mapa?.totalComCoordenadas ?? '—'} com coordenadas)</span></h2>
                <p className="mapa-descricao">Exibe registros persistidos que possuem latitude e longitude. Selecione um marcador para consultar os detalhes.</p>
              </div>
              <button className="button button-quiet" onClick={() => void carregarMapa()} disabled={carregandoMapa}>↻ Atualizar mapa</button>
            </div>
            {mapa?.resultadoTruncado && <div className="mapa-alerta" role="status">
              <span>Exibindo {mapa.itens.length} de {mapa.totalComCoordenadas} equipamentos com coordenadas.</span>
              {mapa.limite < 1000
                ? <button className="button button-secondary" onClick={() => void carregarMapa(1000)} disabled={carregandoMapa}>Carregar até 1.000 pontos</button>
                : <span>O limite máximo desta consulta foi atingido.</span>}
            </div>}
            {carregandoMapa ? <div className="mapa-vazio"><strong>Carregando localizações…</strong><span>Consultando registros persistidos.</span></div> : mapa?.itens.length ? <MapaEquipamentos equipamentos={mapa.itens} /> : (
              <div className="mapa-vazio"><strong>Nenhuma localização cadastrada</strong><span>O mapa mostra somente equipamentos persistidos que tenham latitude e longitude; nenhum marcador demonstrativo foi criado.</span></div>
            )}
          </section>}
        </section>
      )}

      <footer className="footer"><span>F.O.G.O. <span className="muted">/</span> FERRAMENTA ONISCIENTE DE GESTÃO E OPERAÇÕES</span><span className="footer-right"><i /> DADOS VINCULADOS À API</span></footer>

      {modalAberto && <div className="modal-backdrop" role="presentation" onMouseDown={(evento) => { if (evento.target === evento.currentTarget) setModalAberto(false); }}>
        <section className="equipment-modal" role="dialog" aria-modal="true" aria-labelledby="equipment-modal-title">
          <div className="modal-heading"><div><p className="eyebrow">INVENTÁRIO / {editando ? 'EDIÇÃO' : 'NOVO REGISTRO'}</p><h2 id="equipment-modal-title">{editando ? 'Editar equipamento' : 'Cadastrar equipamento'}</h2></div><button className="icon-button close-button" aria-label="Fechar" onClick={() => setModalAberto(false)}>×</button></div>
          <form className="equipment-form" onSubmit={(evento) => void salvar(evento)}>
            <div className="form-section-title">IDENTIFICAÇÃO</div>
            <div className="form-grid">
              <label>Tipo<input required maxLength={60} placeholder="Radar, câmera…" value={formulario.tipo} onChange={(e) => atualizarCampo('tipo', e.target.value)} /></label>
              <label>Número de série<input required maxLength={100} value={formulario.numeroSerie} onChange={(e) => atualizarCampo('numeroSerie', e.target.value)} /></label>
              <label>Marca<input maxLength={100} value={formulario.marca ?? ''} onChange={(e) => atualizarCampo('marca', e.target.value)} /></label>
              <label>Modelo<input maxLength={120} value={formulario.modelo ?? ''} onChange={(e) => atualizarCampo('modelo', e.target.value)} /></label>
            </div>
            <div className="form-section-title">LOCALIZAÇÃO</div>
            <div className="form-grid">
              <label>CEP<input required maxLength={20} value={formulario.cep} onChange={(e) => atualizarCampo('cep', e.target.value)} /></label>
              <label>Bairro<input required maxLength={120} value={formulario.bairro} onChange={(e) => atualizarCampo('bairro', e.target.value)} /></label>
              <label>Cidade<input required maxLength={120} value={formulario.cidade} onChange={(e) => atualizarCampo('cidade', e.target.value)} /></label>
              <label>UF<input required maxLength={2} value={formulario.uf} onChange={(e) => atualizarCampo('uf', e.target.value)} /></label>
              <label className="span-two">Rua<input required maxLength={180} value={formulario.rua} onChange={(e) => atualizarCampo('rua', e.target.value)} /></label>
              <label className="span-two">Referência<input maxLength={240} value={formulario.referencia ?? ''} onChange={(e) => atualizarCampo('referencia', e.target.value)} /></label>
              <label>Latitude<input type="number" step="any" min="-90" max="90" value={formulario.latitude ?? ''} onChange={(e) => atualizarCampo('latitude', e.target.value === '' ? null : Number(e.target.value))} /></label>
              <label>Longitude<input type="number" step="any" min="-180" max="180" value={formulario.longitude ?? ''} onChange={(e) => atualizarCampo('longitude', e.target.value === '' ? null : Number(e.target.value))} /></label>
            </div>
            <div className="form-section-title">SITUAÇÃO E MANUTENÇÃO</div>
            <div className="form-grid">
              <label>Situação<select required value={formulario.situacao} onChange={(e) => atualizarCampo('situacao', e.target.value as SituacaoEquipamento)}>{estados.map((estado) => <option key={estado.valor} value={estado.valor}>{estado.rotulo}</option>)}</select></label>
              <label>Origem dos dados<select required value={formulario.origemDados} onChange={(e) => atualizarCampo('origemDados', e.target.value as OrigemRegistro)}><option value="manual">Manual</option><option value="importado">Importado</option><option value="simulado">Simulado</option></select></label>
              <label className="checkbox-label"><input type="checkbox" checked={formulario.necessitaManutencao} onChange={(e) => setFormulario((atual) => ({ ...atual, necessitaManutencao: e.target.checked, tipoManutencao: e.target.checked ? 'imediata' : null, proximaManutencao: e.target.checked ? atual.proximaManutencao : null }))} /> Necessita manutenção</label>
              {formulario.necessitaManutencao && <>
                <label>Tipo de manutenção<select required value={formulario.tipoManutencao ?? ''} onChange={(e) => setFormulario((atual) => ({ ...atual, tipoManutencao: e.target.value as TipoManutencao, proximaManutencao: e.target.value === 'imediata' ? null : atual.proximaManutencao }))}>{tiposManutencao.map((tipo) => <option key={tipo.valor} value={tipo.valor}>{tipo.rotulo}</option>)}</select></label>
                {formulario.tipoManutencao !== 'imediata' && <label>Próxima manutenção<input type="date" required value={formulario.proximaManutencao ?? ''} onChange={(e) => atualizarCampo('proximaManutencao', e.target.value || null)} /></label>}
              </>}
            </div>
            {erro && <p className="alert error" role="alert">{erro}</p>}
            <div className="modal-actions"><button type="button" className="button button-quiet" onClick={() => setModalAberto(false)}>Cancelar</button><button className="button button-primary" type="submit">{editando ? 'Salvar alterações' : 'Cadastrar equipamento'}</button></div>
          </form>
        </section>
      </div>}
    </main>
  );
}

function dataLocal(valor: string) {
  const [ano, mes, dia] = valor.split('-').map(Number);
  return new Intl.DateTimeFormat('pt-BR').format(new Date(ano, mes - 1, dia));
}

export default App;
