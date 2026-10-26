import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import AgentesView from './AgentesView';
import MapaEquipamentos, { LocalizacaoAgente, LocalizacaoEquipamento, PontoHistoricoAgente } from './MapaEquipamentos';
import { CameraPublica, CatalogoCameras, VideoCameraModal } from './CamerasPublicas';
import './App.css';
import './Visoes.css';
import './FilaManutencao.css';
import './MapaInicio.css';
import './MapaTrajeto.css';

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
type EventoEquipamentoAlterado = { equipamentoId: string; operacao: 'criado' | 'atualizado'; atualizadoEm: string };

type Filtros = { tipo: string; situacao: string; bairro: string };
type RespostaMapaBase<T> = { totalComCoordenadas: number; limite: number; resultadoTruncado: boolean; itens: T[] };
type RespostaMapa = RespostaMapaBase<LocalizacaoEquipamento>;
type RespostaMapaAgentes = RespostaMapaBase<LocalizacaoAgente>;
type RespostaRadaresOpenStreetMap = { disponivel: boolean; fonte: string; aviso: string; itens: LocalizacaoEquipamento[] };
type RespostaCamerasPublicas = { total: number; itens: CameraPublica[] };
type TrajetoAgente = { agenteId: string; nomeAgente: string; data: string; historicoDisponivel: boolean; mensagem: string; localizacoes: PontoHistoricoAgente[] };
type LocalizacaoUsuario = { latitude: number; longitude: number };
const tamanhoLoteMapa = 1000;
const dataHojeLocal = () => {
  const hoje = new Date();
  hoje.setMinutes(hoje.getMinutes() - hoje.getTimezoneOffset());
  return hoje.toISOString().slice(0, 10);
};

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
const semLocalizacoesEquipamentos: LocalizacaoEquipamento[] = [];
const semLocalizacoesAgentes: LocalizacaoAgente[] = [];
const vazioFormulario = (): EquipamentoRequisicao => ({
  tipo: '', numeroSerie: '', marca: '', modelo: '', cep: '', bairro: '', cidade: '', uf: '', rua: '', referencia: '',
  latitude: null, longitude: null, situacao: 'operacional', necessitaManutencao: false,
  tipoManutencao: null, proximaManutencao: null, origemDados: 'manual',
});

function App() {
  const [token, setToken] = useState<string | null>(null);
  const [preparandoSessao, setPreparandoSessao] = useState(true);
  const [itens, setItens] = useState<Equipamento[]>([]);
  const [indicadores, setIndicadores] = useState<Indicadores | null>(null);
  const [mapa, setMapa] = useState<RespostaMapa | null>(null);
  const [mapaAgentes, setMapaAgentes] = useState<RespostaMapaAgentes | null>(null);
  const [radaresOpenStreetMap, setRadaresOpenStreetMap] = useState<RespostaRadaresOpenStreetMap | null>(null);
  const [camerasPublicas, setCamerasPublicas] = useState<CameraPublica[]>([]);
  const [cameraEmReproducao, setCameraEmReproducao] = useState<CameraPublica | null>(null);
  const [catalogoCamerasAberto, setCatalogoCamerasAberto] = useState(false);
  const [trajetoAgente, setTrajetoAgente] = useState<TrajetoAgente | null>(null);
  const [dataHistoricoAgente, setDataHistoricoAgente] = useState(dataHojeLocal);
  const [carregandoTrajeto, setCarregandoTrajeto] = useState(false);
  const [agenteParaEditar, setAgenteParaEditar] = useState<string | null>(null);
  const [localizacaoUsuario, setLocalizacaoUsuario] = useState<LocalizacaoUsuario | null>(null);
  const [estadoLocalizacaoUsuario, setEstadoLocalizacaoUsuario] = useState<'aguardando' | 'localizada' | 'negada' | 'indisponivel'>('aguardando');
  const [enquadramentoMapa, setEnquadramentoMapa] = useState<'usuario' | 'todos'>('usuario');
  const [carregandoMapa, setCarregandoMapa] = useState(false);
  const [statusTempoReal, setStatusTempoReal] = useState<'conectando' | 'conectado' | 'reconectando' | 'desconectado'>('desconectado');
  const [visao, setVisao] = useState<'inventario' | 'mapa' | 'agentes'>('mapa');
  const [revisaoAgentes, setRevisaoAgentes] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [total, setTotal] = useState(0);
  const [filtrosMapa, setFiltrosMapa] = useState<Filtros>(vazioFiltros);
  const [pesquisaMapa, setPesquisaMapa] = useState<Filtros>(vazioFiltros);
  const [filtros, setFiltros] = useState<Filtros>(vazioFiltros);
  const [pesquisa, setPesquisa] = useState<Filtros>(vazioFiltros);
  const [filtroManutencao, setFiltroManutencao] = useState('');
  const [pesquisaManutencao, setPesquisaManutencao] = useState('');
  const [erro, setErro] = useState('');
  const [mensagem, setMensagem] = useState('');
  const [carregando, setCarregando] = useState(false);
  const [modalAberto, setModalAberto] = useState(false);
  const [editando, setEditando] = useState<Equipamento | null>(null);
  const [formulario, setFormulario] = useState<EquipamentoRequisicao>(vazioFormulario);
  const [credenciais, setCredenciais] = useState({ identificador: '', senha: '' });
  const emissaoCsrfEmAndamento = useRef<Promise<string> | null>(null);

  const obterTokenCsrf = useCallback((renovar = false): Promise<string> => {
    const existente = lerCookie('fogo.csrf');
    if (existente && !renovar) return Promise.resolve(existente);

    if (!emissaoCsrfEmAndamento.current) {
      emissaoCsrfEmAndamento.current = (async () => {
        const resposta = await fetch('/api/autenticacao/csrf', {
          credentials: 'include',
          cache: 'no-store',
        });
        if (!resposta.ok) {
          throw new Error(`A API não conseguiu iniciar a proteção CSRF (HTTP ${resposta.status}).`);
        }

        const csrf = lerCookie('fogo.csrf');
        if (!csrf) {
          throw new Error('A API respondeu, mas o navegador não recebeu o cookie CSRF. Atualize a página e confira a conexão local.');
        }
        return csrf;
      })().finally(() => { emissaoCsrfEmAndamento.current = null; });
    }

    return emissaoCsrfEmAndamento.current;
  }, []);

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
      if (pesquisaManutencao) parametros.set('necessitaManutencao', pesquisaManutencao);
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
  }, [chamarApi, pagina, pesquisa, pesquisaManutencao, token]);

  const carregarTodasLocalizacoes = useCallback(async <T,>(
    rota: string,
    filtrosRota: URLSearchParams,
  ): Promise<RespostaMapaBase<T>> => {
    const consultarPagina = async (deslocamento: number) => {
      const parametros = new URLSearchParams(filtrosRota);
      parametros.set('limite', String(tamanhoLoteMapa));
      parametros.set('deslocamento', String(deslocamento));
      const resposta = await chamarApi(`${rota}?${parametros}`);
      return await resposta.json() as RespostaMapaBase<T>;
    };

    const primeiraPagina = await consultarPagina(0);
    const deslocamentos: number[] = [];
    for (let deslocamento = tamanhoLoteMapa; deslocamento < primeiraPagina.totalComCoordenadas; deslocamento += tamanhoLoteMapa) {
      deslocamentos.push(deslocamento);
    }

    const itens = [...primeiraPagina.itens];
    for (let indice = 0; indice < deslocamentos.length; indice += 4) {
      const paginas = await Promise.all(deslocamentos.slice(indice, indice + 4).map(consultarPagina));
      itens.push(...paginas.flatMap((pagina) => pagina.itens));
    }

    return {
      ...primeiraPagina,
      itens,
      resultadoTruncado: itens.length < primeiraPagina.totalComCoordenadas,
    };
  }, [chamarApi]);

  const carregarMapa = useCallback(async () => {
    if (!token) return;
    setCarregandoMapa(true);
    setErro('');
    try {
      const parametros = new URLSearchParams();
      if (pesquisaMapa.tipo) parametros.set('tipo', pesquisaMapa.tipo);
      if (pesquisaMapa.situacao) parametros.set('situacao', pesquisaMapa.situacao);
      if (pesquisaMapa.bairro) parametros.set('bairro', pesquisaMapa.bairro);
      const [respostaEquipamentos, respostaAgentes, radaresPublicos, catalogoCameras] = await Promise.all([
        carregarTodasLocalizacoes<LocalizacaoEquipamento>('/api/equipamentos/localizacoes', parametros),
        carregarTodasLocalizacoes<LocalizacaoAgente>('/api/agentes/localizacoes', new URLSearchParams()),
        chamarApi('/api/radares/openstreetmap')
          .then(async (resposta) => await resposta.json() as RespostaRadaresOpenStreetMap)
          .catch(() => ({
            disponivel: false,
            fonte: 'OpenStreetMap / Overpass API',
            aviso: 'A consulta pública de radares está temporariamente indisponível. Os dados demonstrativos continuam visíveis.',
            itens: [],
          })),
        chamarApi('/api/cameras-publicas')
          .then(async (resposta) => await resposta.json() as RespostaCamerasPublicas)
          .catch(() => ({ total: 0, itens: [] })),
      ]);
      setMapa(respostaEquipamentos);
      setMapaAgentes(respostaAgentes);
      setRadaresOpenStreetMap(radaresPublicos);
      setCamerasPublicas(catalogoCameras.itens);
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível carregar as localizações.');
    } finally {
      setCarregandoMapa(false);
    }
  }, [carregarTodasLocalizacoes, pesquisaMapa, token]);

  const solicitarLocalizacaoUsuario = useCallback(() => {
    if (!navigator.geolocation) {
      setEstadoLocalizacaoUsuario('indisponivel');
      return;
    }

    setEstadoLocalizacaoUsuario('aguardando');
    navigator.geolocation.getCurrentPosition(
      (posicao) => {
        setLocalizacaoUsuario({ latitude: posicao.coords.latitude, longitude: posicao.coords.longitude });
        setEstadoLocalizacaoUsuario('localizada');
        setEnquadramentoMapa('usuario');
      },
      (falha) => {
        setEstadoLocalizacaoUsuario(falha.code === falha.PERMISSION_DENIED ? 'negada' : 'indisponivel');
      },
      { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 },
    );
  }, []);

  const atualizacaoTempoReal = useRef({ carregarDados, carregarMapa, visao });
  useEffect(() => {
    atualizacaoTempoReal.current = { carregarDados, carregarMapa, visao };
  }, [carregarDados, carregarMapa, visao]);

  useEffect(() => { void carregarDados(); }, [carregarDados]);

  useEffect(() => {
    if (token) return;
    setCameraEmReproducao(null);
    setCatalogoCamerasAberto(false);
    setTrajetoAgente(null);
  }, [token]);

  useEffect(() => {
    if (token) solicitarLocalizacaoUsuario();
  }, [solicitarLocalizacaoUsuario, token]);

  useEffect(() => {
    if (visao === 'mapa' && token) void carregarMapa();
  }, [carregarMapa, token, visao]);

  useEffect(() => {
    if (!token) {
      setStatusTempoReal('desconectado');
      return;
    }

    let cancelado = false;
    const conexao = new HubConnectionBuilder()
      .withUrl('/hubs/equipamentos', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();
    const atualizarDados = () => {
      void atualizacaoTempoReal.current.carregarDados();
      if (atualizacaoTempoReal.current.visao === 'mapa') {
        void atualizacaoTempoReal.current.carregarMapa();
      }
    };

    conexao.on('equipamentoAlterado', (_evento: EventoEquipamentoAlterado) => {
      if (cancelado) return;
      atualizarDados();
      setMensagem('Dados atualizados em tempo real.');
    });
    conexao.on('agenteAlterado', (_evento: { agenteId: string; operacao: 'criado' | 'atualizado'; atualizadoEm: string }) => {
      if (cancelado) return;
      setRevisaoAgentes((revisao) => revisao + 1);
      if (atualizacaoTempoReal.current.visao === 'mapa') {
        void atualizacaoTempoReal.current.carregarMapa();
      }
      setMensagem('Quadro de agentes atualizado em tempo real.');
    });
    conexao.onreconnecting(() => { if (!cancelado) setStatusTempoReal('reconectando'); });
    conexao.onreconnected(() => {
      if (!cancelado) {
        setStatusTempoReal('conectado');
        atualizarDados();
      }
    });
    conexao.onclose(() => { if (!cancelado) setStatusTempoReal('desconectado'); });

    setStatusTempoReal('conectando');
    void conexao.start()
      .then(() => { if (!cancelado) setStatusTempoReal('conectado'); })
      .catch(() => { if (!cancelado) setStatusTempoReal('desconectado'); });

    return () => {
      cancelado = true;
      void conexao.stop();
    };
  }, [token]);

  useEffect(() => {
    let cancelado = false;
    setPreparandoSessao(true);
    void (async () => {
      try {
        const csrf = await obterTokenCsrf();
        if (cancelado || token) return;
        const resposta = await fetch('/api/autenticacao/atualizar', {
          method: 'POST', credentials: 'include', headers: { 'X-CSRF-TOKEN': csrf },
        });
        if (!resposta.ok) {
          if (resposta.status === 401) await obterTokenCsrf(true);
          return;
        }
        const dados = await resposta.json() as RespostaToken;
        if (!cancelado) setToken(dados.tokenAcesso);
      } catch { /* API indisponível; a página de login permanece acessível. */ }
      finally { if (!cancelado) setPreparandoSessao(false); }
    })();

    return () => { cancelado = true; };
  }, [obterTokenCsrf, token]);

  const bairros = useMemo(() => [...new Set(itens.map((item) => item.bairro))].sort(), [itens]);
  const possuiFiltrosInventario = Boolean(pesquisa.tipo || pesquisa.situacao || pesquisa.bairro || pesquisaManutencao);
  const filaSemOutrosFiltros = pesquisaManutencao === 'true' && !pesquisa.tipo && !pesquisa.situacao && !pesquisa.bairro;
  const equipamentosMapa = useMemo(() => [
    ...(mapa?.itens ?? semLocalizacoesEquipamentos),
    ...(radaresOpenStreetMap?.itens ?? semLocalizacoesEquipamentos),
  ], [mapa?.itens, radaresOpenStreetMap?.itens]);
  const agentesMapa = mapaAgentes?.itens ?? semLocalizacoesAgentes;
  const camerasMapa = useMemo(() => camerasPublicas.filter((camera) => camera.latitude !== null && camera.longitude !== null), [camerasPublicas]);

  async function entrar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setErro('');
    try {
      const csrf = await obterTokenCsrf();
      const resposta = await fetch('/api/autenticacao/entrar', {
        method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
        body: JSON.stringify(credenciais),
      });
      if (!resposta.ok) throw new Error(resposta.status === 401
        ? 'Usuário ou senha inválidos, ou e-mail ainda não confirmado.'
        : resposta.status === 403
          ? 'A API recusou a origem ou a proteção CSRF. Atualize a página; se persistir, confira FOGO_ORIGINS.'
          : 'Não foi possível entrar. Confira se a API está disponível.');
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

  function abrirFilaManutencao() {
    setFiltros(vazioFiltros);
    setPesquisa(vazioFiltros);
    setFiltroManutencao('true');
    setPesquisaManutencao('true');
    setPagina(1);
    setVisao('inventario');
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

  const editarEquipamentoDoMapa = useCallback(async (id: string) => {
    try {
      const resposta = await chamarApi(`/api/equipamentos/${id}`);
      abrirEdicao(await resposta.json() as Equipamento);
      setVisao('inventario');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível consultar este equipamento.');
    }
  }, [chamarApi]);

  const abrirEdicaoAgenteDoMapa = useCallback((id: string) => {
    setAgenteParaEditar(id);
    setVisao('agentes');
  }, []);

  const carregarTrajetoAgente = useCallback(async (id: string) => {
    if (!dataHistoricoAgente) {
      setErro('Selecione uma data para consultar o histórico de posições.');
      return;
    }
    setCarregandoTrajeto(true);
    setErro('');
    setTrajetoAgente(null);
    try {
      const parametros = new URLSearchParams({ data: dataHistoricoAgente });
      const resposta = await chamarApi(`/api/agentes/${id}/trajeto?${parametros}`);
      setTrajetoAgente(await resposta.json() as TrajetoAgente);
      setEnquadramentoMapa('todos');
      setVisao('mapa');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível consultar as posições registradas.');
    } finally {
      setCarregandoTrajeto(false);
    }
  }, [chamarApi, dataHistoricoAgente]);

  const fecharVideoCamera = useCallback(() => setCameraEmReproducao(null), []);
  const fecharCatalogoCameras = useCallback(() => setCatalogoCamerasAberto(false), []);

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
      await Promise.all([carregarDados(), ...(visao === 'mapa' ? [carregarMapa()] : [])]);
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
            <button className="button button-primary" type="submit" disabled={preparandoSessao}>{preparandoSessao ? 'Preparando acesso…' : <>Entrar <span aria-hidden="true">↗</span></>}</button>
            <p className="form-hint">A sessão usa token de acesso curto e cookie de atualização protegido.</p>
          </form>
        </section>
      ) : (
        <section className={`workspace ${visao === 'mapa' ? 'workspace-map' : ''}`} id="inicio">
          <div className={`workspace-heading ${visao === 'mapa' ? 'workspace-heading-map' : ''}`}>
            <div>
              <p className="eyebrow"><span>{visao === 'mapa' ? 'MAPA OPERACIONAL' : 'INVENTÁRIO'}</span> · DADOS PERSISTIDOS</p>
              <h1>{visao === 'mapa' ? <>Mapa geográfico<span className="heading-period">.</span></> : <>Visão operacional<span className="heading-period">.</span></>}</h1>
              {visao !== 'mapa' && <p className="intro">Consulte e mantenha os equipamentos cadastrados. Indicadores refletem registros persistidos; pontos simulados estão identificados como tal.</p>}
            </div>
            <button className="button button-primary create-button" onClick={abrirNovo}>＋ Novo equipamento</button>
          </div>

          {erro && <p className="alert error" role="alert">{erro}</p>}
          {mensagem && <p className="alert success" role="status">{mensagem}</p>}

          {visao !== 'mapa' && <div className="metric-grid">
            <article className="metric-card"><span className="metric-label">EQUIPAMENTOS CADASTRADOS</span><strong>{indicadores?.totalCadastrado ?? '—'}</strong><small>total persistido</small></article>
            <article className="metric-card"><span className="metric-label">OPERACIONAIS</span><strong>{indicadores?.porSituacao.find((item) => item.situacao === 'operacional')?.quantidade ?? 0}</strong><small>situação declarada</small></article>
            <article className="metric-card"><span className="metric-label">NECESSITAM MANUTENÇÃO</span><strong>{indicadores?.necessitamManutencao ?? '—'}</strong><small>conforme cadastro</small><button type="button" className="maintenance-shortcut" onClick={abrirFilaManutencao}>Abrir fila →</button></article>
          </div>}

          <nav className="view-tabs" aria-label="Visões do inventário">
            <button type="button" aria-pressed={visao === 'mapa'} className={visao === 'mapa' ? 'view-tab active' : 'view-tab'} onClick={() => setVisao('mapa')}>Mapa operacional</button>
            <button type="button" aria-pressed={visao === 'inventario'} className={visao === 'inventario' ? 'view-tab active' : 'view-tab'} onClick={() => setVisao('inventario')}>Equipamentos</button>
            <button type="button" aria-pressed={visao === 'agentes'} className={visao === 'agentes' ? 'view-tab active' : 'view-tab'} onClick={() => setVisao('agentes')}>Agentes</button>
          </nav>

          {visao === 'inventario' ? <section className="inventory-panel" aria-labelledby="inventory-title">
            <div className="panel-heading">
              <div><p className="eyebrow">{pesquisaManutencao === 'true' ? 'MANUTENÇÃO' : 'BASE DE ATIVOS'}</p><h2 id="inventory-title">{pesquisaManutencao === 'true' ? 'Fila de manutenção' : 'Equipamentos'} <span>({total})</span></h2></div>
              <button className="button button-quiet refresh-button" onClick={() => void carregarDados()} disabled={carregando}>↻ Atualizar</button>
            </div>
            <form className="filters inventory-filters" onSubmit={(evento) => { evento.preventDefault(); setPagina(1); setPesquisa(filtros); setPesquisaManutencao(filtroManutencao); }}>
              <label>Tipo<input placeholder="Ex.: radar" value={filtros.tipo} onChange={(e) => setFiltros({ ...filtros, tipo: e.target.value })} /></label>
              <label>Situação<select value={filtros.situacao} onChange={(e) => setFiltros({ ...filtros, situacao: e.target.value })}><option value="">Todas</option>{estados.map((estado) => <option value={estado.valor} key={estado.valor}>{estado.rotulo}</option>)}</select></label>
              <label>Bairro<input list="bairros-cadastrados" value={filtros.bairro} onChange={(e) => setFiltros({ ...filtros, bairro: e.target.value })} /><datalist id="bairros-cadastrados">{bairros.map((bairro) => <option key={bairro}>{bairro}</option>)}</datalist></label>
              <label>Necessita manutenção<select value={filtroManutencao} onChange={(e) => setFiltroManutencao(e.target.value)}><option value="">Todas</option><option value="true">Sim</option><option value="false">Não</option></select></label>
              <button className="button button-secondary" type="submit">Aplicar filtros</button>
              <button className="button button-quiet" type="button" onClick={() => { setFiltros(vazioFiltros); setPesquisa(vazioFiltros); setFiltroManutencao(''); setPesquisaManutencao(''); setPagina(1); }}>Limpar</button>
            </form>
            {carregando ? <div className="empty-state"><span className="spinner" />Carregando dados persistidos…</div> : itens.length === 0 ? (
              <div className="empty-state"><span className="empty-icon">⌁</span><h3>{total > 0 ? 'Nenhum resultado nesta página' : filaSemOutrosFiltros ? 'Nenhuma manutenção indicada' : possuiFiltrosInventario ? 'Nenhum resultado para os filtros' : 'Nenhum equipamento cadastrado'}</h3><p>{total > 0 ? 'Altere os filtros ou navegue para outra página.' : filaSemOutrosFiltros ? 'Nenhum registro persistido está marcado como necessitando manutenção.' : possuiFiltrosInventario ? 'Revise ou limpe os filtros para consultar outros equipamentos.' : 'O inventário está vazio. Cadastre um equipamento para começar.'}</p>{total === 0 && !possuiFiltrosInventario && <button className="button button-primary" onClick={abrirNovo}>Cadastrar primeiro equipamento</button>}</div>
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
          </section> : visao === 'mapa' ? <section className="painel-mapa" aria-labelledby="mapa-title">
            <div className="mapa-cabecalho">
              <div>
                <p className="eyebrow">DISTRIBUIÇÃO GEOGRÁFICA · TODOS OS REGISTROS COM COORDENADAS</p>
                <h2 id="mapa-title">Mapa operacional <span>({mapa?.totalComCoordenadas ?? '—'} equipamentos · {mapaAgentes?.totalComCoordenadas ?? '—'} agentes · {camerasPublicas.length} câmeras · {radaresOpenStreetMap?.itens.length ?? '—'} radares OSM)</span></h2>
                <p className="mapa-descricao">Mapa interativo com dados cadastrados e pontos públicos mapeados. A localização do navegador só é usada nesta sessão, após sua autorização.</p>
              </div>
              <div className="mapa-acoes">
                <div className="mapa-legenda" aria-label="Legenda do mapa"><span><i className="legenda-equipamento" />Equipamentos</span><span><i className="legenda-agente" />Agentes</span></div>
                <span className="mapa-legenda-fonte">Radares OSM em verde-água · dados simulados são identificados nos marcadores.</span>
                <label className="mapa-data-trajeto">Data do histórico de posições<input required type="date" value={dataHistoricoAgente} onChange={(evento) => setDataHistoricoAgente(evento.target.value)} /></label>
                <span className={`mapa-status-localizacao ${estadoLocalizacaoUsuario === 'localizada' ? 'localizacao-ok' : ''}`}>
                  {estadoLocalizacaoUsuario === 'localizada' ? 'Você está no mapa' : estadoLocalizacaoUsuario === 'aguardando' ? 'Localizando você…' : estadoLocalizacaoUsuario === 'negada' ? 'Localização não autorizada' : 'Localização indisponível'}
                </span>
                <button className="button button-quiet" onClick={() => { setEnquadramentoMapa('todos'); }} disabled={!mapa?.itens.length && !mapaAgentes?.itens.length && !radaresOpenStreetMap?.itens.length}>Enquadrar pontos</button>
                <button className="button button-quiet" onClick={() => setCatalogoCamerasAberto(true)}>Câmeras ao vivo ({camerasPublicas.length})</button>
                <button className="button button-quiet" onClick={solicitarLocalizacaoUsuario} disabled={estadoLocalizacaoUsuario === 'aguardando'}>Minha localização</button>
                <button className="button button-quiet" onClick={() => void carregarMapa()} disabled={carregandoMapa}>↻ Atualizar mapa</button>
              </div>
            </div>
            {trajetoAgente && <div className="mapa-alerta mapa-trajeto-status" role="status"><span><strong>{trajetoAgente.nomeAgente}</strong> · {new Intl.DateTimeFormat('pt-BR').format(new Date(`${trajetoAgente.data}T00:00:00`))} · {trajetoAgente.mensagem} {trajetoAgente.historicoDisponivel && `${trajetoAgente.localizacoes.length} posições registradas.`}</span><button type="button" onClick={() => setTrajetoAgente(null)}>Limpar trajeto</button></div>}
            {carregandoTrajeto && <div className="mapa-alerta" role="status">Consultando posições registradas do agente…</div>}
            <form className="filters map-filters" onSubmit={(evento) => { evento.preventDefault(); setPesquisaMapa(filtrosMapa); }}>
              <label>Tipo<input placeholder="Ex.: radar" value={filtrosMapa.tipo} onChange={(evento) => setFiltrosMapa({ ...filtrosMapa, tipo: evento.target.value })} /></label>
              <label>Situação<select value={filtrosMapa.situacao} onChange={(evento) => setFiltrosMapa({ ...filtrosMapa, situacao: evento.target.value })}><option value="">Todas</option>{estados.map((estado) => <option key={estado.valor} value={estado.valor}>{estado.rotulo}</option>)}</select></label>
              <label>Bairro<input value={filtrosMapa.bairro} onChange={(evento) => setFiltrosMapa({ ...filtrosMapa, bairro: evento.target.value })} /></label>
              <button className="button button-secondary" type="submit">Aplicar filtros</button>
              <button className="button button-quiet" type="button" onClick={() => { setFiltrosMapa(vazioFiltros); setPesquisaMapa(vazioFiltros); }}>Limpar</button>
            </form>
            {mapa?.resultadoTruncado && <div className="mapa-alerta" role="status">
              <span>Exibindo {mapa.itens.length} de {mapa.totalComCoordenadas} equipamentos com coordenadas.</span>
              <span>Atualize o mapa para buscar novamente todos os registros.</span>
            </div>}
            {mapaAgentes?.resultadoTruncado && <div className="mapa-alerta" role="status"><span>Alguns agentes não foram carregados. Atualize o mapa para tentar novamente.</span></div>}
            {radaresOpenStreetMap && !radaresOpenStreetMap.disponivel && <div className="mapa-alerta mapa-alerta-fonte" role="status">{radaresOpenStreetMap.aviso}</div>}
            <div className="mapa-conteudo">
              <MapaEquipamentos
                equipamentos={equipamentosMapa}
                agentes={agentesMapa}
                cameras={camerasMapa}
                localizacaoUsuario={localizacaoUsuario}
                enquadramento={enquadramentoMapa}
                trajetoAgente={trajetoAgente}
                onEditarEquipamento={(id) => void editarEquipamentoDoMapa(id)}
                onEditarAgente={abrirEdicaoAgenteDoMapa}
                onVerTrajetoAgente={(id) => void carregarTrajetoAgente(id)}
                onReproduzirCamera={setCameraEmReproducao}
              />
              {carregandoMapa && <div className="mapa-mensagem-overlay"><strong>Carregando localizações…</strong><span>Consultando os registros persistidos.</span></div>}
              {!carregandoMapa && mapa && mapaAgentes && mapa.itens.length === 0 && mapaAgentes.itens.length === 0 && !camerasMapa.length && !radaresOpenStreetMap?.itens.length && (
                <div className="mapa-mensagem-overlay"><strong>Nenhum ponto georreferenciado ainda</strong><span>Adicione registros com coordenadas para vê-los aqui. Os pontos demonstrativos aparecem identificados como simulados.</span></div>
              )}
            </div>
          </section> : <AgentesView token={token} revisao={revisaoAgentes} agenteIdParaEditar={agenteParaEditar} onAgenteEditado={() => setAgenteParaEditar(null)} />}
        </section>
      )}

      <footer className="footer"><span>F.O.G.O. <span className="muted">/</span> FERRAMENTA ONISCIENTE DE GESTÃO E OPERAÇÕES</span><span className="footer-right"><i /> DADOS VINCULADOS À API{token && ` · TEMPO REAL ${statusTempoReal.toUpperCase()}`}</span></footer>

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
      {catalogoCamerasAberto && <CatalogoCameras cameras={camerasPublicas} onPlay={(camera) => { setCatalogoCamerasAberto(false); setCameraEmReproducao(camera); }} onFechar={fecharCatalogoCameras} />}
      <VideoCameraModal camera={cameraEmReproducao} onFechar={fecharVideoCamera} />
    </main>
  );
}

function dataLocal(valor: string) {
  const [ano, mes, dia] = valor.split('-').map(Number);
  return new Intl.DateTimeFormat('pt-BR').format(new Date(ano, mes - 1, dia));
}

export default App;
