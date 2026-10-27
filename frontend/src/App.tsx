import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import AgentesView from './AgentesView';
import MapaEquipamentos, { CameraMarcador, LocalizacaoAgente, LocalizacaoEquipamento } from './MapaEquipamentos';
import { CameraPublica, CatalogoCameras, VideoCameraModal } from './CamerasPublicas';
import HistoricoAgenteModal, { PaginaHistoricoAgente } from './HistoricoAgenteModal';
import PainelOperacoes from './PainelOperacoes';
import { AcidenteMapa, TransitoMapa } from './OperacoesTipos';
import './App.css';
import './Visoes.css';
import './FilaManutencao.css';
import './MapaInicio.css';
import './MapaAlertas.css';
import './PainelOperacional.css';

type SituacaoEquipamento = 'aindaNaoInstalado' | 'operacional' | 'mauFuncionamento' | 'emManutencao' | 'desativado' | 'furtado';
type TipoManutencao = 'imediata' | 'agendada' | 'periodica';
type OrigemRegistro = 'manual' | 'importado' | 'simulado';

type Equipamento = {
  id: string;
  tipo: string;
  numeroSerie: string;
  marca: string | null;
  modelo: string | null;
  caracteristicas: Record<string, string>;
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
type ResumoLocalidade = { bairro: string; cidade: string; uf: string; totalCadastrado: number; totalInstalado: number; operacionais: number; indisponiveis: number; manutencoesImediatas: number; manutencoesVencidas: number; manutencoesProximas: number };
type ProjecaoManutencao = { bairro: string; cidade: string; uf: string; data: string; quantidade: number };
type Indicadores = { totalCadastrado: number; porSituacao: { situacao: SituacaoEquipamento; quantidade: number }[]; necessitamManutencao: number; manutencoesImediatas: number; totalInstalados: number; operacionais: number; indisponiveis: number; percentualDisponibilidade: number | null; manutencoesVencidas: number; manutencoesProximas: number; porLocalidade: ResumoLocalidade[]; projecaoManutencao: ProjecaoManutencao[]; inicioProjecao: string; fimProjecao: string };
type IndicadoresAgentes = { totalCadastrado: number; emAtividadeDeclarada: number; porSituacao: { situacao: string; quantidade: number }[] };
type RespostaToken = { tokenAcesso: string; expiraEm: string };
type EventoEquipamentoAlterado = { equipamentoId: string; operacao: 'criado' | 'atualizado'; atualizadoEm: string };

type Filtros = { tipo: string; situacao: string; bairro: string };
type RespostaMapaBase<T> = { totalComCoordenadas: number; limite: number; resultadoTruncado: boolean; itens: T[] };
type RespostaMapa = RespostaMapaBase<LocalizacaoEquipamento>;
type RespostaMapaAgentes = RespostaMapaBase<LocalizacaoAgente>;
type RespostaRadaresOpenStreetMap = { disponivel: boolean; fonte: string; aviso: string; itens: LocalizacaoEquipamento[] };
type RespostaCamerasPublicas = { total: number; itens: CameraPublica[] };
type LocalizacaoUsuario = { latitude: number; longitude: number };
type AcaoLinkEmail = 'confirmar' | 'redefinir';
type ModoAutenticacao = 'entrar' | 'cadastro' | 'recuperacao' | 'confirmar' | 'redefinir';
const tamanhoLoteMapa = 1000;
const centrosMunicipaisCameras: Record<string, [number, number]> = {
  'Avaré': [-23.104521, -48.925910],
  'Bastos': [-21.922247, -50.731773],
  'Cambará': [-23.042273, -50.075281],
  'Cerqueira César': [-23.035314, -49.165052],
  'Cândido Mota': [-22.743496, -50.389240],
  'Espírito Santo do Turvo': [-22.693972, -49.429507],
  'Jaú': [-22.293303, -48.559343],
  'Ourinhos': [-22.955274, -49.857775],
  'Paraguaçu Paulista': [-22.420350, -50.579210],
  'Quatá': [-22.247098, -50.700362],
  'Ribeirão do Sul': [-22.786181, -49.928526],
  'São José da Boa Vista': [-23.917315, -49.650601],
  'Taguaí': [-23.453431, -49.405445],
  'Taquarituba': [-23.533730, -49.245254],
  'Tupã': [-21.934913, -50.514126],
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
  tipo: '', numeroSerie: '', marca: '', modelo: '', caracteristicas: {}, cep: '', bairro: '', cidade: '', uf: '', rua: '', referencia: '',
  latitude: null, longitude: null, situacao: 'operacional', necessitaManutencao: false,
  tipoManutencao: null, proximaManutencao: null, origemDados: 'manual',
});

function App() {
  const [linkEmail] = useState(() => {
    const url = new URL(window.location.href);
    const acao = url.searchParams.get('acao');
    const email = url.searchParams.get('email');
    const tokenLink = url.searchParams.get('token');
    if ((acao === 'confirmar' || acao === 'redefinir') && email && tokenLink) {
      return { acao: acao as AcaoLinkEmail, email, token: tokenLink };
    }
    return null;
  });
  const [modoAutenticacao, setModoAutenticacao] = useState<ModoAutenticacao>(() =>
    linkEmail?.acao === 'confirmar' ? 'confirmar' : linkEmail?.acao === 'redefinir' ? 'redefinir' : 'entrar');
  const [token, setToken] = useState<string | null>(null);
  const [preparandoSessao, setPreparandoSessao] = useState(true);
  const [itens, setItens] = useState<Equipamento[]>([]);
  const [indicadores, setIndicadores] = useState<Indicadores | null>(null);
  const [indicadoresAgentes, setIndicadoresAgentes] = useState<IndicadoresAgentes | null>(null);
  const [mapa, setMapa] = useState<RespostaMapa | null>(null);
  const [mapaAgentes, setMapaAgentes] = useState<RespostaMapaAgentes | null>(null);
  const [radaresOpenStreetMap, setRadaresOpenStreetMap] = useState<RespostaRadaresOpenStreetMap | null>(null);
  const [camerasPublicas, setCamerasPublicas] = useState<CameraPublica[]>([]);
  const [cameraEmReproducao, setCameraEmReproducao] = useState<CameraPublica | null>(null);
  const [catalogoCamerasAberto, setCatalogoCamerasAberto] = useState(false);
  const [agenteHistorico, setAgenteHistorico] = useState<{ id: string; nome: string; origemDados: OrigemRegistro } | null>(null);
  const [agenteParaEditar, setAgenteParaEditar] = useState<string | null>(null);
  const [localizacaoUsuario, setLocalizacaoUsuario] = useState<LocalizacaoUsuario | null>(null);
  const [estadoLocalizacaoUsuario, setEstadoLocalizacaoUsuario] = useState<'naoSolicitada' | 'aguardando' | 'localizada' | 'negada' | 'indisponivel'>('naoSolicitada');
  const [enquadramentoMapa, setEnquadramentoMapa] = useState<'usuario' | 'todos'>('todos');
  const [erroCameras, setErroCameras] = useState('');
  const [transitoMapa, setTransitoMapa] = useState<TransitoMapa[]>([]);
  const [acidentesMapa, setAcidentesMapa] = useState<AcidenteMapa[]>([]);
  const [selecionandoLocalAcidente, setSelecionandoLocalAcidente] = useState(false);
  const [localAcidenteSelecionado, setLocalAcidenteSelecionado] = useState<LocalizacaoUsuario | null>(null);
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
  const [caracteristicasJson, setCaracteristicasJson] = useState('{}');
  const [credenciais, setCredenciais] = useState({ identificador: '', senha: '' });
  const [emailAutenticacao, setEmailAutenticacao] = useState(linkEmail?.email ?? '');
  const [senhaNova, setSenhaNova] = useState('');
  const [confirmacaoSenha, setConfirmacaoSenha] = useState('');
  const emissaoCsrfEmAndamento = useRef<Promise<string> | null>(null);

  useEffect(() => {
    if (linkEmail) window.history.replaceState(null, '', `${window.location.pathname}${window.location.hash}`);
  }, [linkEmail]);

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
          throw new Error('Não foi possível iniciar a proteção da sessão. Atualize a página e tente novamente.');
        }

        const csrf = lerCookie('fogo.csrf');
        if (!csrf) {
          throw new Error('O navegador não conseguiu iniciar a sessão protegida. Confira as configurações de privacidade e tente novamente.');
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
      const [respostaLista, respostaIndicadores, respostaIndicadoresAgentes] = await Promise.all([
        chamarApi(`/api/equipamentos?${parametros}`),
        chamarApi('/api/indicadores/equipamentos'),
        chamarApi('/api/agentes/indicadores'),
      ]);
      const lista = await respostaLista.json() as PaginaEquipamentos;
      setItens(lista.itens);
      setTotal(lista.totalRegistros);
      setIndicadores(await respostaIndicadores.json() as Indicadores);
      setIndicadoresAgentes(await respostaIndicadoresAgentes.json() as IndicadoresAgentes);
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
          .then(async (resposta) => {
            setErroCameras('');
            return await resposta.json() as RespostaCamerasPublicas;
          })
          .catch((falha: unknown) => {
            setErroCameras(falha instanceof Error ? falha.message : 'Não foi possível carregar o catálogo de câmeras.');
            return { total: 0, itens: [] };
          }),
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
    if (localizacaoUsuario) {
      setEnquadramentoMapa('usuario');
      return;
    }

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
  }, [localizacaoUsuario]);

  const atualizacaoTempoReal = useRef({ carregarDados, carregarMapa, visao });
  useEffect(() => {
    atualizacaoTempoReal.current = { carregarDados, carregarMapa, visao };
  }, [carregarDados, carregarMapa, visao]);

  useEffect(() => { void carregarDados(); }, [carregarDados]);

  useEffect(() => {
    if (token) return;
    setCameraEmReproducao(null);
    setCatalogoCamerasAberto(false);
    setAgenteHistorico(null);
  }, [token]);

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
  const camerasMapa = useMemo(() => {
    const marcadoresIndividuais: CameraMarcador[] = camerasPublicas
      .filter((camera) => camera.latitude != null && camera.longitude != null)
      .map((camera) => ({
        chave: `camera-${camera.id}`,
        cidade: camera.cidade,
        latitude: camera.latitude!,
        longitude: camera.longitude!,
        aproximada: false,
        cameras: [camera],
      }));
    const agrupadas = new Map<string, CameraPublica[]>();
    for (const camera of camerasPublicas) {
      if (camera.latitude != null && camera.longitude != null) continue;
      agrupadas.set(camera.cidade, [...(agrupadas.get(camera.cidade) ?? []), camera]);
    }
    const marcadoresMunicipais = [...agrupadas].flatMap(([cidade, cameras]): CameraMarcador[] => {
      const centro = centrosMunicipaisCameras[cidade];
      return centro ? [{ chave: `municipio-${cidade}`, cidade, latitude: centro[0], longitude: centro[1], aproximada: true, cameras }] : [];
    });
    return [...marcadoresIndividuais, ...marcadoresMunicipais];
  }, [camerasPublicas]);
  const camerasSemReferencia = camerasPublicas.filter((camera) => (camera.latitude == null || camera.longitude == null) && !centrosMunicipaisCameras[camera.cidade]).length;
  const camerasComPosicaoIndividual = camerasPublicas.filter((camera) => camera.latitude != null && camera.longitude != null).length;
  const camerasComReferenciaMunicipal = camerasMapa.filter((marcador) => marcador.aproximada).reduce((total, marcador) => total + marcador.cameras.length, 0);

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
          ? 'Não foi possível validar esta solicitação. Atualize a página e tente novamente.'
          : 'Não foi possível entrar. Tente novamente em instantes.');
      const dados = await resposta.json() as RespostaToken;
      setToken(dados.tokenAcesso);
      setMensagem('Sessão iniciada.');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Falha ao entrar.');
    }
  }

  async function enviarFormularioAutenticacao(caminho: string, dados: Record<string, string>) {
    const csrf = await obterTokenCsrf();
    const resposta = await fetch(`/api/autenticacao/${caminho}`, {
      method: 'POST', credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
      body: JSON.stringify(dados),
    });
    const corpo = await resposta.json().catch(() => null) as { mensagem?: string; title?: string; errors?: Record<string, string[]> } | null;
    if (!resposta.ok) {
      const errosCampos = corpo?.errors ? Object.values(corpo.errors).flat().join(' ') : '';
      throw new Error(errosCampos || corpo?.mensagem || corpo?.title || (resposta.status === 403
        ? 'Não foi possível validar esta solicitação. Atualize a página e tente novamente.'
        : 'Não foi possível concluir a solicitação. Confira os dados e tente novamente.'));
    }
    return corpo;
  }

  async function cadastrar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setErro('');
    setMensagem('');
    if (senhaNova !== confirmacaoSenha) {
      setErro('As senhas não coincidem.');
      return;
    }
    try {
      const resposta = await enviarFormularioAutenticacao('cadastro', { email: emailAutenticacao, senha: senhaNova });
      setMensagem(resposta?.mensagem ?? 'Se o cadastro puder ser concluído, enviaremos as instruções para o endereço informado.');
      setSenhaNova('');
      setConfirmacaoSenha('');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível criar a conta.');
    }
  }

  async function recuperarSenha(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setErro('');
    setMensagem('');
    try {
      const resposta = await enviarFormularioAutenticacao('esqueci-senha', { email: emailAutenticacao });
      setMensagem(resposta?.mensagem ?? 'Se houver uma conta associada ao endereço, enviaremos as instruções de recuperação.');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível solicitar a recuperação.');
    }
  }

  async function reenviarConfirmacao(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setErro('');
    setMensagem('');
    try {
      const resposta = await enviarFormularioAutenticacao('reenviar-confirmacao', { email: emailAutenticacao });
      setMensagem(resposta?.mensagem ?? 'Se houver uma conta pendente, enviaremos novas instruções.');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível reenviar a confirmação.');
    }
  }

  async function confirmarEmail(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    if (!linkEmail) return;
    setErro('');
    setMensagem('');
    try {
      await enviarFormularioAutenticacao('confirmar-email', { email: linkEmail.email, token: linkEmail.token });
      setMensagem('E-mail confirmado. Agora você já pode entrar.');
      setModoAutenticacao('entrar');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'O link de confirmação é inválido ou expirou.');
    }
  }

  async function redefinirSenha(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    if (!linkEmail || linkEmail.acao !== 'redefinir') return;
    setErro('');
    setMensagem('');
    if (senhaNova !== confirmacaoSenha) {
      setErro('As senhas não coincidem.');
      return;
    }
    try {
      await enviarFormularioAutenticacao('redefinir-senha', {
        email: linkEmail.email, token: linkEmail.token, novaSenha: senhaNova,
      });
      setSenhaNova('');
      setConfirmacaoSenha('');
      setMensagem('Senha alterada. Entre novamente com a nova senha.');
      setModoAutenticacao('entrar');
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível redefinir a senha.');
    }
  }

  function trocarModoAutenticacao(modo: ModoAutenticacao) {
    setErro('');
    setMensagem('');
    setModoAutenticacao(modo);
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
    setCaracteristicasJson('{}');
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
    setCaracteristicasJson(JSON.stringify(equipamento.caracteristicas ?? {}, null, 2));
    setFormulario({
      tipo: equipamento.tipo, numeroSerie: equipamento.numeroSerie, marca: equipamento.marca ?? '', modelo: equipamento.modelo ?? '',
      caracteristicas: equipamento.caracteristicas ?? {},
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

  const abrirHistoricoAgenteDoMapa = useCallback((id: string) => {
    const agente = agentesMapa.find((item) => item.id === id);
    if (agente) {
      setAgenteHistorico({ id: agente.id, nome: agente.nome, origemDados: agente.origemDados ?? 'manual' });
    }
  }, [agentesMapa]);

  const listarHistoricoAgente = useCallback(async (
    id: string,
    inicio: string,
    fim: string,
    deslocamento: number,
    limite: number,
  ): Promise<PaginaHistoricoAgente> => {
    const parametros = new URLSearchParams({ deslocamento: String(deslocamento), limite: String(limite) });
    if (inicio) parametros.set('inicio', inicio);
    if (fim) parametros.set('fim', fim);
    const resposta = await chamarApi(`/api/agentes/${id}/historico-localizacoes?${parametros}`);
    return await resposta.json() as PaginaHistoricoAgente;
  }, [chamarApi]);

  const fecharVideoCamera = useCallback(() => setCameraEmReproducao(null), []);
  const fecharCatalogoCameras = useCallback(() => setCatalogoCamerasAberto(false), []);

  async function salvar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setErro('');
    setMensagem('');
    let caracteristicas: Record<string, string>;
    let parsed: unknown;
    try {
      parsed = JSON.parse(caracteristicasJson || '{}');
    } catch {
      setErro('Características inválidas: informe um objeto JSON, por exemplo {"visão noturna":"sim"}.');
      return;
    }
    if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)
      || Object.values(parsed).some((valor) => typeof valor !== 'string')) {
      setErro('Use um objeto JSON com nomes e valores de texto.');
      return;
    }
    caracteristicas = parsed as Record<string, string>;
    const corpo = {
      ...formulario,
      caracteristicas,
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
          <span className={`system-status ${token ? 'connected' : ''}`}><i />{token ? 'SESSÃO PROTEGIDA' : 'AGUARDANDO ACESSO'}</span>
          {token && <button className="button button-quiet" onClick={() => void sair()}>Encerrar sessão</button>}
        </div>
      </header>

      {!token ? (
        <section className="login-layout" id="inicio">
          <div className="login-copy">
            <p className="eyebrow"><span>PLATAFORMA OPERACIONAL</span> · ACESSO SEGURO</p>
            <h1>Visão integrada.<br /><em>Decisões melhores.</em></h1>
            <p className="intro">Acesse ou crie sua conta para consultar o inventário operacional do F.O.G.O. O login é liberado após a confirmação do e-mail.</p>
            <div className="login-notice"><span aria-hidden="true">↳</span> Confirme seu e-mail para ativar a conta antes do primeiro acesso.</div>
          </div>
          <div className="login-card">
            <div className="card-kicker">01 / AUTENTICAÇÃO</div>
            <h2>{modoAutenticacao === 'entrar' ? 'Entrar na plataforma'
              : modoAutenticacao === 'cadastro' ? 'Criar uma conta'
                : modoAutenticacao === 'recuperacao' ? 'Recuperar senha'
                  : modoAutenticacao === 'confirmar' ? 'Confirmar endereço de e-mail'
                    : 'Definir nova senha'}</h2>
            {modoAutenticacao === 'entrar' && <form onSubmit={(evento) => void entrar(evento)}>
              <label>Usuário ou e-mail<input autoComplete="username" required value={credenciais.identificador} onChange={(e) => setCredenciais({ ...credenciais, identificador: e.target.value })} /></label>
              <label>Senha<input type="password" autoComplete="current-password" required value={credenciais.senha} onChange={(e) => setCredenciais({ ...credenciais, senha: e.target.value })} /></label>
              {erro && <p className="alert error" role="alert">{erro}</p>}
              {mensagem && <p className="alert success" role="status">{mensagem}</p>}
              <button className="button button-primary" type="submit" disabled={preparandoSessao}>{preparandoSessao ? 'Preparando acesso…' : <>Entrar <span aria-hidden="true">↗</span></>}</button>
              <div className="login-links"><button className="auth-link" type="button" onClick={() => trocarModoAutenticacao('recuperacao')}>Esqueci minha senha</button><button className="auth-link" type="button" onClick={() => trocarModoAutenticacao('cadastro')}>Criar conta</button></div>
              <p className="form-hint">Confirmação de conta e recuperação de acesso são enviadas ao e-mail cadastrado.</p>
            </form>}
            {modoAutenticacao === 'cadastro' && <form onSubmit={(evento) => void cadastrar(evento)}>
              <label>E-mail<input type="email" autoComplete="email" maxLength={256} required value={emailAutenticacao} onChange={(e) => setEmailAutenticacao(e.target.value)} /></label>
              <label>Senha<input type="password" autoComplete="new-password" minLength={12} required value={senhaNova} onChange={(e) => setSenhaNova(e.target.value)} /></label>
              <label>Confirme a senha<input type="password" autoComplete="new-password" minLength={12} required value={confirmacaoSenha} onChange={(e) => setConfirmacaoSenha(e.target.value)} /></label>
              <p className="form-hint">Use pelo menos 12 caracteres, com maiúscula, minúscula, número e símbolo. O link de confirmação expira em 2 horas.</p>
              {erro && <p className="alert error" role="alert">{erro}</p>}{mensagem && <p className="alert success" role="status">{mensagem}</p>}
              <button className="button button-primary" type="submit">Enviar confirmação</button>
              <div className="login-links"><button className="auth-link" type="button" onClick={() => trocarModoAutenticacao('entrar')}>Voltar para entrar</button></div>
            </form>}
            {modoAutenticacao === 'recuperacao' && <form onSubmit={(evento) => void recuperarSenha(evento)}>
              <p className="form-hint">Informe o e-mail da conta. Se ela existir, enviaremos um link válido por 2 horas.</p>
              <label>E-mail<input type="email" autoComplete="email" maxLength={256} required value={emailAutenticacao} onChange={(e) => setEmailAutenticacao(e.target.value)} /></label>
              {erro && <p className="alert error" role="alert">{erro}</p>}{mensagem && <p className="alert success" role="status">{mensagem}</p>}
              <button className="button button-primary" type="submit">Enviar instruções</button>
              <div className="login-links"><button className="auth-link" type="button" onClick={() => trocarModoAutenticacao('entrar')}>Voltar para entrar</button></div>
            </form>}
            {modoAutenticacao === 'confirmar' && <form onSubmit={(evento) => linkEmail ? void confirmarEmail(evento) : void reenviarConfirmacao(evento)}>
              {linkEmail ? <p className="form-hint">Confirme o endereço {linkEmail.email} para habilitar o acesso.</p> : <><p className="form-hint">O link não está disponível. Informe seu e-mail para solicitar outro.</p><label>E-mail<input type="email" autoComplete="email" maxLength={256} required value={emailAutenticacao} onChange={(e) => setEmailAutenticacao(e.target.value)} /></label></>}
              {erro && <p className="alert error" role="alert">{erro}</p>}{mensagem && <p className="alert success" role="status">{mensagem}</p>}
              <button className="button button-primary" type="submit">{linkEmail ? 'Confirmar e-mail' : 'Reenviar confirmação'}</button>
              <div className="login-links"><button className="auth-link" type="button" onClick={() => trocarModoAutenticacao('entrar')}>Voltar para entrar</button></div>
            </form>}
            {modoAutenticacao === 'redefinir' && <form onSubmit={(evento) => void redefinirSenha(evento)}>
              {linkEmail ? <><p className="form-hint">Defina uma nova senha para {linkEmail.email}. O link é válido por 2 horas.</p>
                <label>Nova senha<input type="password" autoComplete="new-password" minLength={12} required value={senhaNova} onChange={(e) => setSenhaNova(e.target.value)} /></label>
                <label>Confirme a nova senha<input type="password" autoComplete="new-password" minLength={12} required value={confirmacaoSenha} onChange={(e) => setConfirmacaoSenha(e.target.value)} /></label></>
                : <p className="form-hint">Link ausente ou inválido. Solicite uma nova recuperação de senha.</p>}
              {erro && <p className="alert error" role="alert">{erro}</p>}{mensagem && <p className="alert success" role="status">{mensagem}</p>}
              {linkEmail && <button className="button button-primary" type="submit">Salvar nova senha</button>}
              <div className="login-links"><button className="auth-link" type="button" onClick={() => trocarModoAutenticacao(linkEmail ? 'entrar' : 'recuperacao')}>{linkEmail ? 'Voltar para entrar' : 'Solicitar novo link'}</button></div>
            </form>}
          </div>
        </section>
      ) : (
        <section className={`workspace ${visao === 'mapa' ? 'workspace-map' : ''}`} id="inicio">
          <div className={`workspace-heading ${visao === 'mapa' ? 'workspace-heading-map' : ''}`}>
            <div>
              <p className="eyebrow"><span>{visao === 'mapa' ? 'MAPA OPERACIONAL' : 'INVENTÁRIO'}</span> · OPERAÇÃO DA CIDADE</p>
              <h1>{visao === 'mapa' ? <>Mapa geográfico<span className="heading-period">.</span></> : <>Visão operacional<span className="heading-period">.</span></>}</h1>
              {visao !== 'mapa' && <p className="intro">Consulte e mantenha os equipamentos cadastrados. Indicadores refletem o inventário; registros simulados são identificados como tal.</p>}
            </div>
            <button className="button button-primary create-button" onClick={abrirNovo}>＋ Novo equipamento</button>
          </div>

          {erro && <p className="alert error" role="alert">{erro}</p>}
          {mensagem && <p className="alert success" role="status">{mensagem}</p>}

          {visao !== 'mapa' && <div className="metric-grid">
            <article className="metric-card"><span className="metric-label">EQUIPAMENTOS CADASTRADOS</span><strong>{indicadores?.totalCadastrado ?? '—'}</strong><small>no inventário</small></article>
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
            {carregando ? <div className="empty-state"><span className="spinner" />Consultando equipamentos…</div> : itens.length === 0 ? (
              <div className="empty-state"><span className="empty-icon">⌁</span><h3>{total > 0 ? 'Nenhum resultado nesta página' : filaSemOutrosFiltros ? 'Nenhuma manutenção indicada' : possuiFiltrosInventario ? 'Nenhum resultado para os filtros' : 'Nenhum equipamento cadastrado'}</h3><p>{total > 0 ? 'Altere os filtros ou navegue para outra página.' : filaSemOutrosFiltros ? 'Nenhum equipamento tem manutenção indicada.' : possuiFiltrosInventario ? 'Revise ou limpe os filtros para consultar outros equipamentos.' : 'O inventário está vazio. Cadastre um equipamento para começar.'}</p>{total === 0 && !possuiFiltrosInventario && <button className="button button-primary" onClick={abrirNovo}>Cadastrar primeiro equipamento</button>}</div>
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
                <h2 id="mapa-title">Mapa operacional <span>({mapa?.totalComCoordenadas ?? '—'} equipamentos · {mapaAgentes?.totalComCoordenadas ?? '—'} agentes · {camerasMapa.reduce((total, marcador) => total + marcador.cameras.length, 0)} câmeras no mapa · {radaresOpenStreetMap?.itens.length ?? '—'} radares OSM)</span></h2>
                <p className="mapa-descricao">Câmeras com coordenadas próprias têm marcador individual; as demais ficam agrupadas no centro municipal aproximado até a posição exata ser validada. A localização do navegador só é usada nesta sessão.</p>
                <p className="mapa-descricao">Câmeras: {camerasPublicas.length} no catálogo · {camerasComPosicaoIndividual} posições individuais · {camerasComReferenciaMunicipal} referências municipais · {camerasSemReferencia} sem referência geográfica. Sem posição válida: {Math.max(0, (indicadores?.totalCadastrado ?? 0) - (mapa?.totalComCoordenadas ?? 0))} equipamento(s) e {Math.max(0, (indicadoresAgentes?.totalCadastrado ?? 0) - (mapaAgentes?.totalComCoordenadas ?? 0))} agente(s) não podem ser plotados.</p>
                <div className="mapa-indicadores-situacao" aria-label="Quantidade de equipamentos cadastrados por situação">
                  <span className="mapa-indicadores-titulo">EQUIPAMENTOS POR SITUAÇÃO · CADASTRO</span>
                  {estados.map((estado) => <span key={estado.valor}><i className={`situacao-indicador situacao-${estado.valor}`} />{estado.rotulo}<strong>{indicadores?.porSituacao.find((item) => item.situacao === estado.valor)?.quantidade ?? 0}</strong></span>)}
                </div>
                <div className="mapa-kpis" aria-label="Indicadores operacionais declarados">
                  <span><small>INSTALADOS</small><strong>{indicadores?.totalInstalados ?? '—'}</strong></span>
                  <span><small>OPERACIONAIS</small><strong>{indicadores?.operacionais ?? '—'}</strong></span>
                  <span><small>INDISPONÍVEIS</small><strong>{indicadores?.indisponiveis ?? '—'}</strong></span>
                  <span><small>DISPONIBILIDADE DECLARADA</small><strong>{indicadores?.percentualDisponibilidade == null ? '—' : `${indicadores.percentualDisponibilidade}%`}</strong></span>
                  <span><small>AGENTES DISPONÍVEIS/EM ATENDIMENTO · INFORMADO</small><strong>{indicadoresAgentes?.emAtividadeDeclarada ?? '—'} / {indicadoresAgentes?.totalCadastrado ?? '—'}</strong></span>
                </div>
                <p className="mapa-indicadores-nota">Disponibilidade = operacionais ÷ equipamentos instalados (não inclui “ainda não instalado”). Situações e atividade dos agentes são informações declaradas manualmente, não telemetria.</p>
              </div>
              <div className="mapa-acoes">
                <div className="mapa-legenda" aria-label="Legenda do mapa"><span><i className="legenda-equipamento" />Equipamentos</span><span><i className="legenda-agente" />Agentes</span><span><i className="legenda-camera" />Câmeras · tracejado = referência municipal</span><span><i className="legenda-transito" />Trânsito</span><span><i className="legenda-acidente" />Acidentes</span></div>
                <span className="mapa-legenda-fonte">Radares OSM em verde-água · dados simulados são identificados nos marcadores.</span>
                <span className={`mapa-status-localizacao ${estadoLocalizacaoUsuario === 'localizada' ? 'localizacao-ok' : ''}`}>
                  {estadoLocalizacaoUsuario === 'localizada' ? 'Localização autorizada' : estadoLocalizacaoUsuario === 'aguardando' ? 'Localizando você…' : estadoLocalizacaoUsuario === 'negada' ? 'Localização não autorizada' : estadoLocalizacaoUsuario === 'naoSolicitada' ? 'Localização ainda não solicitada' : 'Localização indisponível'}
                </span>
                <button className="button button-quiet" onClick={() => { setEnquadramentoMapa('todos'); }} disabled={!mapa?.itens.length && !mapaAgentes?.itens.length && !radaresOpenStreetMap?.itens.length && !camerasMapa.length}>Enquadrar pontos</button>
                <button className="button button-quiet" onClick={() => setCatalogoCamerasAberto(true)}>Câmeras ao vivo ({camerasPublicas.length})</button>
                <button className="button button-quiet" onClick={solicitarLocalizacaoUsuario} disabled={estadoLocalizacaoUsuario === 'aguardando'}>Minha localização</button>
                <button className="button button-quiet" onClick={() => void carregarMapa()} disabled={carregandoMapa}>↻ Atualizar mapa</button>
              </div>
            </div>
            {indicadores && (indicadores.manutencoesImediatas + indicadores.manutencoesVencidas + indicadores.manutencoesProximas > 0) && <div className="mapa-alerta mapa-alerta-manutencao" role="status"><span>{indicadores.manutencoesImediatas > 0 && <strong>{indicadores.manutencoesImediatas} imediata(s) declarada(s). </strong>}{indicadores.manutencoesVencidas > 0 && <strong>{indicadores.manutencoesVencidas} agendada(s) vencida(s). </strong>}{indicadores.manutencoesProximas > 0 && <strong>{indicadores.manutencoesProximas} prevista(s) nos próximos 7 dias.</strong>}</span><button type="button" onClick={abrirFilaManutencao}>Consultar fila →</button></div>}
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
            {erroCameras && <div className="mapa-alerta mapa-alerta-fonte" role="alert">Não foi possível consultar o catálogo de câmeras: {erroCameras}</div>}
            {camerasSemReferencia > 0 && <div className="mapa-alerta" role="status">{camerasSemReferencia} câmera(s) não têm coordenadas nem referência municipal cadastrada e ainda não podem ser colocadas no mapa.</div>}
            {indicadores?.porLocalidade.length ? <details className="painel-insights"><summary>Consolidação por localidade · bairro/cidade</summary><p>Resumo agrupado pelo bairro, cidade e UF informados no cadastro.</p><div className="painel-insights-tabela"><table><thead><tr><th>Localidade</th><th>Operacionais / instalados</th><th>Indisponíveis</th><th>Imediatas</th><th>Vencidas</th><th>Próx. 7 dias</th></tr></thead><tbody>{indicadores.porLocalidade.map((localidade) => <tr key={`${localidade.cidade}-${localidade.uf}-${localidade.bairro}`}><td>{localidade.bairro}, {localidade.cidade}/{localidade.uf}</td><td>{localidade.operacionais} / {localidade.totalInstalado}</td><td>{localidade.indisponiveis}</td><td>{localidade.manutencoesImediatas}</td><td>{localidade.manutencoesVencidas}</td><td>{localidade.manutencoesProximas}</td></tr>)}</tbody></table></div></details> : null}
            {indicadores && <details className="painel-insights"><summary>Projeção de manutenção · próximos 30 dias ({indicadores.projecaoManutencao.reduce((totalProgramado, item) => totalProgramado + item.quantidade, 0)} programada(s))</summary><p>Projeção determinística baseada nas datas de manutenção cadastradas entre {dataLocal(indicadores.inicioProjecao)} e {dataLocal(indicadores.fimProjecao)}. Não é previsão de falhas ou ocorrências.</p>{indicadores.projecaoManutencao.length ? <div className="painel-insights-tabela"><table><thead><tr><th>Data prevista</th><th>Localidade</th><th>Equipamentos</th></tr></thead><tbody>{indicadores.projecaoManutencao.map((item, indice) => <tr key={`${item.data}-${item.cidade}-${item.bairro}-${indice}`}><td>{dataLocal(item.data)}</td><td>{item.bairro}, {item.cidade}/{item.uf}</td><td>{item.quantidade}</td></tr>)}</tbody></table></div> : <p>Nenhuma manutenção agendada para o horizonte apresentado.</p>}</details>}
            <PainelOperacoes
              chamarApi={chamarApi}
              obterTokenCsrf={obterTokenCsrf}
              localSelecionado={localAcidenteSelecionado}
              selecionandoLocal={selecionandoLocalAcidente}
              onSolicitarSelecao={setSelecionandoLocalAcidente}
              onTransitoAtualizado={setTransitoMapa}
              onAcidentesAtualizados={setAcidentesMapa}
            />
            <div className="mapa-conteudo">
              <MapaEquipamentos
                equipamentos={equipamentosMapa}
                agentes={agentesMapa}
                cameras={camerasMapa}
                transito={transitoMapa}
                acidentes={acidentesMapa}
                selecionarLocalAcidente={selecionandoLocalAcidente}
                onSelecionarLocalAcidente={(coordenadas) => { setLocalAcidenteSelecionado(coordenadas); setSelecionandoLocalAcidente(false); }}
                localizacaoUsuario={localizacaoUsuario}
                enquadramento={enquadramentoMapa}
                onEditarEquipamento={(id) => void editarEquipamentoDoMapa(id)}
                onEditarAgente={abrirEdicaoAgenteDoMapa}
                onVerHistoricoAgente={abrirHistoricoAgenteDoMapa}
                onReproduzirCamera={setCameraEmReproducao}
              />
              {carregandoMapa && <div className="mapa-mensagem-overlay"><strong>Carregando localizações…</strong><span>Buscando equipamentos e agentes cadastrados.</span></div>}
              {!carregandoMapa && mapa && mapaAgentes && mapa.itens.length === 0 && mapaAgentes.itens.length === 0 && !camerasMapa.length && !radaresOpenStreetMap?.itens.length && !transitoMapa.length && !acidentesMapa.length && (
                <div className="mapa-mensagem-overlay"><strong>Nenhum ponto georreferenciado ainda</strong><span>Adicione registros com coordenadas para vê-los aqui. Os pontos demonstrativos aparecem identificados como simulados.</span></div>
              )}
            </div>
          </section> : <AgentesView token={token} revisao={revisaoAgentes} agenteIdParaEditar={agenteParaEditar} onAgenteEditado={() => setAgenteParaEditar(null)} />}
        </section>
      )}

      <footer className="footer"><span>F.O.G.O. <span className="muted">/</span> FERRAMENTA ONISCIENTE DE GESTÃO E OPERAÇÕES</span><span className="footer-right"><i />{token ? `TEMPO REAL ${statusTempoReal.toUpperCase()}` : 'PLATAFORMA OPERACIONAL'}</span></footer>

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
            <label className="characteristics-field">Características específicas (JSON)<textarea rows={4} maxLength={18000} spellCheck={false} value={caracteristicasJson} onChange={(evento) => setCaracteristicasJson(evento.target.value)} placeholder={'{"visão noturna":"sim","resolução":"4 MP"}'} /></label>
            <p className="form-hint">Use pares nome/valor em texto. Os atributos variam conforme o equipamento; os valores cadastrados aparecem no mapa.</p>
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
      {agenteHistorico && <HistoricoAgenteModal agente={agenteHistorico} listar={listarHistoricoAgente} onFechar={() => setAgenteHistorico(null)} />}
    </main>
  );
}

function dataLocal(valor: string) {
  const [ano, mes, dia] = valor.split('-').map(Number);
  return new Intl.DateTimeFormat('pt-BR').format(new Date(ano, mes - 1, dia));
}

export default App;
