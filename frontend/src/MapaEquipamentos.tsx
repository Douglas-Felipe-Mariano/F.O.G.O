import { useEffect, useRef, useState } from 'react';
import { CircleMarker, MapContainer, Marker, Popup, TileLayer, useMap, useMapEvents } from 'react-leaflet';
import { divIcon, DomEvent, LatLngBounds } from 'leaflet';
import { CameraPublica } from './CamerasPublicas';
import { AcidenteMapa, PontoCalor, TransitoMapa } from './OperacoesTipos';
import 'leaflet/dist/leaflet.css';
import './MapaEquipamentos.css';
import './MapaMarcadores.css';
import './MapaCalor.css';

export type LocalizacaoEquipamento = {
  id: string;
  tipo: string;
  numeroSerie: string;
  marca?: string | null;
  modelo?: string | null;
  caracteristicas?: Record<string, string>;
  cep?: string;
  bairro: string;
  cidade: string;
  uf: string;
  rua: string;
  referencia: string | null;
  latitude: number;
  longitude: number;
  situacao: string;
  necessitaManutencao?: boolean;
  tipoManutencao?: 'imediata' | 'agendada' | 'periodica' | null;
  proximaManutencao?: string | null;
  origemDados?: 'manual' | 'importado' | 'simulado';
  fonte?: string;
  urlFonte?: string;
  velocidadeMaxima?: string | null;
};

export type LocalizacaoAgente = {
  id: string;
  nome: string;
  categoriaProfissional: string;
  baseOperacional: string;
  latitude: number;
  longitude: number;
  situacao: string;
  origemDados?: 'manual' | 'importado' | 'simulado';
};

type LocalizacaoUsuario = { latitude: number; longitude: number };
export type CameraMarcador = {
  chave: string;
  cidade: string;
  latitude: number;
  longitude: number;
  aproximada: boolean;
  cameras: CameraPublica[];
};
type Props = {
  equipamentos: LocalizacaoEquipamento[];
  agentes?: LocalizacaoAgente[];
  cameras?: CameraMarcador[];
  transito?: TransitoMapa[];
  acidentes?: AcidenteMapa[];
  localAcidenteSelecionado?: LocalizacaoUsuario | null;
  pontosCalor?: PontoCalor[];
  selecionarLocalAcidente?: boolean;
  onSelecionarLocalAcidente?: (coordenadas: LocalizacaoUsuario) => void;
  onSolicitarLocalizacao?: () => void;
  localizacaoUsuario?: LocalizacaoUsuario | null;
  enquadramento?: 'usuario' | 'todos';
  onEditarEquipamento?: (id: string) => void;
  onEditarAgente?: (id: string) => void;
  onVerHistoricoAgente?: (id: string) => void;
  onReproduzirCamera?: (camera: CameraPublica) => void;
};

const centroTupa: [number, number] = [-21.9335, -50.519];
const rotuloSituacao: Record<string, string> = {
  aindaNaoInstalado: 'Ainda não instalado',
  operacional: 'Operacional',
  mauFuncionamento: 'Mau funcionamento',
  emManutencao: 'Em manutenção',
  desativado: 'Desativado',
  furtado: 'Furtado',
};
const corSituacao: Record<string, string> = {
  aindaNaoInstalado: '#a6b7e8',
  operacional: '#c8f169',
  mauFuncionamento: '#f0ae62',
  emManutencao: '#70c7d9',
  desativado: '#87928c',
  furtado: '#ef7d7d',
};
const iconeEquipamentoSvg = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 5.5 12 3l4 2.5v3l3 1.7v8.3H5v-8.3l3-1.7v-3Z"/><path d="M9 8h6M9 13h6M9 16h2m3 0h1"/></svg>';
const iconeAgenteSvg = '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="8" r="3.2"/><path d="M5.5 20c.4-4 2.6-6 6.5-6s6.1 2 6.5 6H5.5Z"/></svg>';
const iconeRadarSvg = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20V9m-4 11h8M9 9a3 3 0 0 1 6 0M6 7a6 6 0 0 1 12 0M3.5 5a8.5 8.5 0 0 1 17 0"/><circle cx="12" cy="7" r="1"/></svg>';
const iconeCameraSvg = '<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="2.5" y="6.5" width="13" height="11" rx="2"/><path d="m15.5 10 6-3v10l-6-3z"/></svg>';

function criarIconeEquipamento(situacao: string, tipo: string, origemExterna: boolean) {
  const classeSituacao = corSituacao[situacao] ? ` situacao-${situacao}` : '';
  const icone = tipo.toLocaleLowerCase('pt-BR').includes('radar') ? iconeRadarSvg : iconeEquipamentoSvg;
  const classeOrigem = origemExterna ? ' marcador-radar-externo' : '';
  return divIcon({
    className: 'icone-leaflet-container',
    html: `<span class="marcador-mapa marcador-equipamento${classeSituacao}${classeOrigem}">${icone}</span>`,
    iconSize: [36, 44],
    iconAnchor: [18, 40],
    popupAnchor: [0, -38],
  });
}

function criarIconeAgente(situacao: string) {
  const classeSituacao = situacao === 'disponivel'
    ? ' agente-disponivel'
    : situacao === 'emAtendimento'
      ? ' agente-em-atendimento'
      : ' agente-indisponivel';
  return divIcon({
    className: 'icone-leaflet-container',
    html: `<span class="marcador-mapa marcador-agente${classeSituacao}">${iconeAgenteSvg}</span>`,
    iconSize: [36, 44],
    iconAnchor: [18, 40],
    popupAnchor: [0, -38],
  });
}
function criarIconeCamera(aproximada: boolean, quantidade: number) {
  return divIcon({
    className: 'icone-leaflet-container',
    html: `<span class="marcador-mapa marcador-camera${aproximada ? ' marcador-camera-aproximada' : ''}">${iconeCameraSvg}${quantidade > 1 || aproximada ? `<b>${quantidade}</b>` : ''}</span>`,
    iconSize: [42, 42],
    iconAnchor: [21, 21],
    popupAnchor: [0, -22],
  });
}
const iconeUsuario = divIcon({
  className: 'icone-leaflet-container',
  html: '<span class="marcador-usuario"><i></i></span>',
  iconSize: [32, 32],
  iconAnchor: [16, 16],
});

function CapturarLocalAcidente({ ativo, aoSelecionar }: { ativo?: boolean; aoSelecionar?: (coordenadas: LocalizacaoUsuario) => void }) {
  useMapEvents({
    click(evento) {
      if (ativo) aoSelecionar?.({ latitude: evento.latlng.lat, longitude: evento.latlng.lng });
    },
  });
  return null;
}

function criarIconeTransito(percentual: number) {
  const nivel = percentual >= 75 ? 'alto' : percentual >= 45 ? 'medio' : 'baixo';
  return divIcon({
    className: 'icone-leaflet-container',
    html: `<span class="marcador-transito transito-${nivel}">${percentual}%</span>`,
    iconSize: [42, 42],
    iconAnchor: [21, 37],
    popupAnchor: [0, -34],
  });
}

const iconeAcidente = divIcon({
  className: 'icone-leaflet-container',
  html: '<span class="marcador-acidente"><strong>!</strong></span>',
  iconSize: [40, 40],
  iconAnchor: [20, 20],
  popupAnchor: [0, -18],
});

function AjustarEnquadramento({ equipamentos, agentes = [], cameras = [], transito = [], acidentes = [], localizacaoUsuario, enquadramento = 'usuario' }: Props) {
  const mapa = useMap();

  useEffect(() => {
    const pontos = [
      ...equipamentos.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
      ...agentes.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
      ...cameras.map((camera) => [camera.latitude, camera.longitude] as [number, number]),
      ...transito.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
      ...acidentes.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
      ...(localizacaoUsuario ? [[localizacaoUsuario.latitude, localizacaoUsuario.longitude] as [number, number]] : []),
    ];
    if (enquadramento === 'usuario' && localizacaoUsuario) {
      mapa.setView([localizacaoUsuario.latitude, localizacaoUsuario.longitude], 14);
      return;
    }

    if (pontos.length === 0) {
      mapa.setView(centroTupa, 12);
      return;
    }

    if (pontos.length === 1) {
      mapa.setView(pontos[0], 13);
      return;
    }

    mapa.fitBounds(new LatLngBounds(pontos), { padding: [36, 36], maxZoom: 13 });
  }, [agentes, acidentes, cameras, enquadramento, equipamentos, localizacaoUsuario, mapa, transito]);

  return null;
}

function ControlesMapa({
  localizacaoUsuario,
  solicitarLocalizacao,
  mostrarMapaCalor,
  alternarMapaCalor,
}: {
  localizacaoUsuario?: LocalizacaoUsuario | null;
  solicitarLocalizacao?: () => void;
  mostrarMapaCalor: boolean;
  alternarMapaCalor: () => void;
}) {
  const mapa = useMap();
  const controleRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (controleRef.current) DomEvent.disableClickPropagation(controleRef.current);
  }, []);

  return (
    <div className="leaflet-top leaflet-right mapa-controles-fogo" ref={controleRef}>
      <div className="leaflet-control mapa-controles-fogo-botoes">
        <button type="button" className={`mapa-controle-botao${mostrarMapaCalor ? ' ativo' : ''}`} aria-pressed={mostrarMapaCalor} onClick={alternarMapaCalor}>🔥 Mapa de Calor</button>
        <button type="button" className="mapa-controle-botao" onClick={() => {
          if (!localizacaoUsuario) {
            solicitarLocalizacao?.();
            return;
          }
          mapa.flyTo([localizacaoUsuario.latitude, localizacaoUsuario.longitude], 15, { duration: 0.7 });
        }}>⌖ Focar minha localização</button>
      </div>
    </div>
  );
}

function CamadaCalor({ pontosCalor, ativa }: { pontosCalor: PontoCalor[]; ativa: boolean }) {
  if (!ativa) return null;
  return <>{pontosCalor.map((ponto) => {
    const intensidade = Math.max(0, Math.min(100, ponto.peso));
    const cor = intensidade >= 75 ? '#ef5148' : intensidade >= 50 ? '#f39448' : intensidade >= 25 ? '#e5d45f' : '#61c98a';
    return (
      <CircleMarker
        key={`${ponto.latitude}:${ponto.longitude}`}
        center={[ponto.latitude, ponto.longitude]}
        radius={8 + intensidade * 0.2}
        pathOptions={{ color: cor, weight: 1, fillColor: cor, fillOpacity: 0.18 + intensidade * 0.004 }}
      >
        <Popup><div className="popup-equipamento"><strong>Índice de atenção: {intensidade}/100</strong><span>Composição de trânsito, manutenção de equipamentos e ocorrências recentes.</span></div></Popup>
      </CircleMarker>
    );
  })}</>;
}

export default function MapaEquipamentos({ equipamentos, agentes = [], cameras = [], transito = [], acidentes = [], localAcidenteSelecionado, pontosCalor = [], selecionarLocalAcidente = false, onSelecionarLocalAcidente, onSolicitarLocalizacao, localizacaoUsuario, enquadramento = 'usuario', onEditarEquipamento, onEditarAgente, onVerHistoricoAgente, onReproduzirCamera }: Props) {
  const [mostrarMapaCalor, setMostrarMapaCalor] = useState(false);
  const equipamentosVisiveis = equipamentos.filter((equipamento) => equipamento.situacao !== 'desativado');
  const agentesVisiveis = agentes.filter((agente) => agente.situacao !== 'inativo');
  return (
    <div className={`mapa-equipamentos${selecionarLocalAcidente ? ' mapa-selecionando-acidente' : ''}`} aria-label="Mapa de equipamentos, agentes, trânsito e ocorrências georreferenciadas">
      <MapContainer center={centroTupa} zoom={12} scrollWheelZoom className="mapa-leaflet">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <ControlesMapa
          localizacaoUsuario={localizacaoUsuario}
          solicitarLocalizacao={onSolicitarLocalizacao}
          mostrarMapaCalor={mostrarMapaCalor}
          alternarMapaCalor={() => setMostrarMapaCalor((atual) => !atual)}
        />
        <CamadaCalor pontosCalor={pontosCalor} ativa={mostrarMapaCalor} />
        <AjustarEnquadramento equipamentos={equipamentosVisiveis} agentes={agentesVisiveis} cameras={cameras} transito={transito} acidentes={acidentes} localizacaoUsuario={localizacaoUsuario} enquadramento={enquadramento} />
        <CapturarLocalAcidente ativo={selecionarLocalAcidente} aoSelecionar={onSelecionarLocalAcidente} />
        {transito.map((item) => (
          <Marker key={`transito-${item.id}`} position={[item.latitude, item.longitude]} icon={criarIconeTransito(item.congestionamentoPercentual)}>
            <Popup><div className="popup-equipamento"><strong>Trânsito · {item.cidade}</strong><span>{item.veiculosUltimaHora} veículos na última hora.</span><span>Velocidade média: {item.velocidadeMediaKmh} km/h · congestionamento: {item.congestionamentoPercentual}%.</span><span>Projeção para a próxima hora: {item.veiculosPrevistosProximaHora} veículos · {item.congestionamentoPrevistoPercentual}%.</span><span>Nível de confiança: {item.confianca} · {item.amostrasHistoricas} amostras comparáveis.</span><strong className="popup-fonte">DADOS DE DEMONSTRAÇÃO · {item.referenciaMapa}.</strong></div></Popup>
          </Marker>
        ))}
        {acidentes.map((acidente) => (
          <Marker key={`acidente-${acidente.id}`} position={[acidente.latitude, acidente.longitude]} icon={iconeAcidente} zIndexOffset={1800}>
            <Popup><div className="popup-equipamento"><strong>Acidente · {acidente.grau}</strong><span>{acidente.localDescricao} · {acidente.cidade}</span><span>Impacto no trânsito: {acidente.impactoTransito} · feridos: {acidente.haFeridos ? 'informados' : 'não informados'}.</span><span>Situação: {acidente.situacao}.</span><span>Registro no F.O.G.O. · integração com serviços externos não configurada.</span></div></Popup>
          </Marker>
        ))}
        {equipamentosVisiveis.map((equipamento) => (
          <Marker
            key={equipamento.id}
            position={[equipamento.latitude, equipamento.longitude]}
            icon={criarIconeEquipamento(equipamento.situacao, equipamento.tipo, Boolean(equipamento.urlFonte))}
          >
            <Popup>
              <div className="popup-equipamento">
                <strong>{equipamento.tipo}</strong>
                <span>Série {equipamento.numeroSerie}</span>
                {!equipamento.urlFonte && <span>{[equipamento.marca, equipamento.modelo].filter(Boolean).join(' · ') || 'Marca/modelo não informados'}</span>}
                <span>{equipamento.urlFonte ? 'Posição georreferenciada · fonte OpenStreetMap' : rotuloSituacao[equipamento.situacao] ?? equipamento.situacao}</span>
                {!equipamento.urlFonte && equipamento.caracteristicas && Object.keys(equipamento.caracteristicas).length > 0 && <span>Características: {Object.entries(equipamento.caracteristicas).map(([nome, valor]) => `${nome}: ${valor}`).join(' · ')}</span>}
                <span>{equipamento.rua}{equipamento.referencia ? ` · ${equipamento.referencia}` : ''}</span>
                <span>{equipamento.bairro}, {equipamento.cidade}/{equipamento.uf}</span>
                {!equipamento.urlFonte && equipamento.cep && <span>CEP {equipamento.cep}</span>}
                {!equipamento.urlFonte && equipamento.necessitaManutencao && <span>Manutenção: {equipamento.tipoManutencao === 'imediata' ? 'Imediata' : equipamento.tipoManutencao === 'agendada' ? 'Agendada' : equipamento.tipoManutencao === 'periodica' ? 'Periódica' : 'Indicada'}{equipamento.proximaManutencao ? ` · próxima em ${new Intl.DateTimeFormat('pt-BR').format(new Date(`${equipamento.proximaManutencao}T00:00:00`))}` : ''}</span>}
                {!equipamento.urlFonte && <span>Coordenadas: {equipamento.latitude}, {equipamento.longitude}</span>}
                {equipamento.origemDados === 'simulado' && <strong className="popup-aviso">EQUIPAMENTO DEMONSTRATIVO</strong>}
                {equipamento.urlFonte && <strong className="popup-fonte">Fonte: {equipamento.fonte ?? 'OpenStreetMap'}{equipamento.velocidadeMaxima ? ` · Máx. ${equipamento.velocidadeMaxima}` : ''} · status informado pela fonte.</strong>}
                {equipamento.urlFonte && <a href={equipamento.urlFonte} target="_blank" rel="noreferrer">Ver objeto no OpenStreetMap ↗</a>}
                {!equipamento.urlFonte && onEditarEquipamento && <button type="button" className="mapa-acao-popup" onClick={() => onEditarEquipamento(equipamento.id)}>Editar equipamento</button>}
              </div>
            </Popup>
          </Marker>
        ))}
        {agentesVisiveis.map((agente) => (
          <Marker
            key={`agente-${agente.id}`}
            position={[agente.latitude, agente.longitude]}
            icon={criarIconeAgente(agente.situacao)}
          >
            <Popup>
              <div className="popup-equipamento">
                <strong>Agente · {agente.nome}</strong>
                <span>{agente.categoriaProfissional}</span>
                <span>{agente.situacao === 'disponivel' ? 'Disponível' : agente.situacao === 'emAtendimento' ? 'Em atendimento' : 'Indisponível'} · situação informada</span>
                <span>{agente.baseOperacional}</span>
                {agente.origemDados === 'simulado' && <strong className="popup-fonte">REGISTRO DEMONSTRATIVO · posição de referência cadastrada.</strong>}
                {onEditarAgente && <button type="button" className="mapa-acao-popup" onClick={() => onEditarAgente(agente.id)}>Editar agente</button>}
                {onVerHistoricoAgente && <button type="button" className="mapa-acao-popup mapa-acao-secundaria" onClick={() => onVerHistoricoAgente(agente.id)}>Ver histórico de localizações</button>}
              </div>
            </Popup>
          </Marker>
        ))}
        {cameras.map((marcador) => (
          <Marker key={`camera-${marcador.chave}`} position={[marcador.latitude, marcador.longitude]} icon={criarIconeCamera(marcador.aproximada, marcador.cameras.length)}>
            <Popup>
              <div className="popup-equipamento">
                <strong>{marcador.aproximada ? `${marcador.cameras.length} câmera(s) · referência municipal` : marcador.cameras[0].nome}</strong>
                <span>{marcador.cidade}{marcador.aproximada ? ' · referência geográfica aproximada do município' : marcador.cameras[0].endereco ? ` · ${marcador.cameras[0].endereco}` : ''}</span>
                {marcador.cameras.map((camera) => <div className="popup-camera-item" key={camera.id}><span>{camera.nome}</span>{onReproduzirCamera && <button type="button" className="mapa-acao-popup mapa-camera-play" onClick={() => onReproduzirCamera(camera)}>▶ Assistir</button>}</div>)}
              </div>
            </Popup>
          </Marker>
        ))}
        {localizacaoUsuario && (
          <Marker position={[localizacaoUsuario.latitude, localizacaoUsuario.longitude]} icon={iconeUsuario} zIndexOffset={1000}>
            <Popup><div className="popup-equipamento"><strong>Você está aqui</strong><span>Sua localização atual, obtida pelo navegador e visível somente nesta sessão.</span></div></Popup>
          </Marker>
        )}
        {localAcidenteSelecionado && <CircleMarker center={[localAcidenteSelecionado.latitude, localAcidenteSelecionado.longitude]} radius={11} pathOptions={{ color: '#ffffff', weight: 3, fillColor: '#f0ae62', fillOpacity: 0.95 }}><Popup><div className="popup-equipamento"><strong>Ponto do acidente selecionado</strong><span>Este local ainda não foi salvo.</span><span>{localAcidenteSelecionado.latitude.toFixed(6)}, {localAcidenteSelecionado.longitude.toFixed(6)}</span></div></Popup></CircleMarker>}
      </MapContainer>
      {selecionarLocalAcidente && <div className="mapa-selecao-acidente-banner" role="status">Clique no mapa para marcar o local do acidente</div>}
    </div>
  );
}
