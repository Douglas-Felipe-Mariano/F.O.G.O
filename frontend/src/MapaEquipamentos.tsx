import { useEffect } from 'react';
import { MapContainer, Marker, Popup, TileLayer, useMap, useMapEvents } from 'react-leaflet';
import { divIcon, LatLngBounds } from 'leaflet';
import { CameraPublica } from './CamerasPublicas';
import { AcidenteMapa, TransitoMapa } from './OperacoesTipos';
import 'leaflet/dist/leaflet.css';
import './MapaEquipamentos.css';
import './MapaMarcadores.css';

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
  selecionarLocalAcidente?: boolean;
  onSelecionarLocalAcidente?: (coordenadas: LocalizacaoUsuario) => void;
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

export default function MapaEquipamentos({ equipamentos, agentes = [], cameras = [], transito = [], acidentes = [], selecionarLocalAcidente = false, onSelecionarLocalAcidente, localizacaoUsuario, enquadramento = 'usuario', onEditarEquipamento, onEditarAgente, onVerHistoricoAgente, onReproduzirCamera }: Props) {
  return (
    <div className={`mapa-equipamentos${selecionarLocalAcidente ? ' mapa-selecionando-acidente' : ''}`} aria-label="Mapa de equipamentos, agentes, trânsito e ocorrências georreferenciadas">
      <MapContainer center={centroTupa} zoom={12} scrollWheelZoom className="mapa-leaflet">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <AjustarEnquadramento equipamentos={equipamentos} agentes={agentes} cameras={cameras} transito={transito} acidentes={acidentes} localizacaoUsuario={localizacaoUsuario} enquadramento={enquadramento} />
        <CapturarLocalAcidente ativo={selecionarLocalAcidente} aoSelecionar={onSelecionarLocalAcidente} />
        {transito.map((item) => (
          <Marker key={`transito-${item.id}`} position={[item.latitude, item.longitude]} icon={criarIconeTransito(item.congestionamentoPercentual)}>
            <Popup><div className="popup-equipamento"><strong>Trânsito · {item.cidade}</strong><span>Volume demonstrativo: {item.veiculosUltimaHora} veículos na última hora.</span><span>Velocidade média: {item.velocidadeMediaKmh} km/h · congestionamento: {item.congestionamentoPercentual}%.</span><span>Próxima hora: {item.veiculosPrevistosProximaHora} veículos · {item.congestionamentoPrevistoPercentual}% projetado.</span><span>Confiança heurística: {item.confianca} · {item.amostrasHistoricas} amostras comparáveis.</span><strong className="popup-fonte">SIMULADO · {item.referenciaMapa}. Não é medição em tempo real.</strong></div></Popup>
          </Marker>
        ))}
        {acidentes.map((acidente) => (
          <Marker key={`acidente-${acidente.id}`} position={[acidente.latitude, acidente.longitude]} icon={iconeAcidente} zIndexOffset={1800}>
            <Popup><div className="popup-equipamento"><strong>Acidente · {acidente.grau}</strong><span>{acidente.localDescricao} · {acidente.cidade}</span><span>Impacto no trânsito: {acidente.impactoTransito} · feridos: {acidente.haFeridos ? 'informados' : 'não informados'}.</span><span>Situação: {acidente.situacao}.</span><span>Registro interno · não substitui comunicação aos serviços de emergência.</span></div></Popup>
          </Marker>
        ))}
        {equipamentos.map((equipamento) => (
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
                <span>{equipamento.urlFonte ? 'Posição mapeada · situação não verificada' : rotuloSituacao[equipamento.situacao] ?? equipamento.situacao}</span>
                {!equipamento.urlFonte && equipamento.caracteristicas && Object.keys(equipamento.caracteristicas).length > 0 && <span>Características: {Object.entries(equipamento.caracteristicas).map(([nome, valor]) => `${nome}: ${valor}`).join(' · ')}</span>}
                <span>{equipamento.rua}{equipamento.referencia ? ` · ${equipamento.referencia}` : ''}</span>
                <span>{equipamento.bairro}, {equipamento.cidade}/{equipamento.uf}</span>
                {!equipamento.urlFonte && equipamento.cep && <span>CEP {equipamento.cep}</span>}
                {!equipamento.urlFonte && equipamento.necessitaManutencao && <span>Manutenção: {equipamento.tipoManutencao === 'imediata' ? 'Imediata' : equipamento.tipoManutencao === 'agendada' ? 'Agendada' : equipamento.tipoManutencao === 'periodica' ? 'Periódica' : 'Indicada'}{equipamento.proximaManutencao ? ` · próxima em ${new Intl.DateTimeFormat('pt-BR').format(new Date(`${equipamento.proximaManutencao}T00:00:00`))}` : ''}</span>}
                {!equipamento.urlFonte && <span>Coordenadas: {equipamento.latitude}, {equipamento.longitude}</span>}
                {equipamento.origemDados === 'simulado' && <strong className="popup-aviso">DADO SIMULADO · não confirma radar ou equipamento real.</strong>}
                {equipamento.urlFonte && <strong className="popup-fonte">Fonte: {equipamento.fonte ?? 'OpenStreetMap'}{equipamento.velocidadeMaxima ? ` · Máx. ${equipamento.velocidadeMaxima}` : ''} · situação não verificada.</strong>}
                {equipamento.urlFonte && <a href={equipamento.urlFonte} target="_blank" rel="noreferrer">Ver objeto no OpenStreetMap ↗</a>}
                {!equipamento.urlFonte && onEditarEquipamento && <button type="button" className="mapa-acao-popup" onClick={() => onEditarEquipamento(equipamento.id)}>Editar equipamento</button>}
              </div>
            </Popup>
          </Marker>
        ))}
        {agentes.map((agente) => (
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
                {agente.origemDados === 'simulado' && <strong className="popup-fonte">Demonstração · posição ilustrativa, sem rastreamento real.</strong>}
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
                <span>{marcador.cidade}{marcador.aproximada ? ' · centro municipal aproximado; não é a posição individual das câmeras' : marcador.cameras[0].endereco ? ` · ${marcador.cameras[0].endereco}` : ''}</span>
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
      </MapContainer>
    </div>
  );
}
