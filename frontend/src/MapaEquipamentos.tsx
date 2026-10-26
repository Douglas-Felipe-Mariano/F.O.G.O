import { useEffect } from 'react';
import { MapContainer, Marker, Polyline, Popup, TileLayer, useMap } from 'react-leaflet';
import { divIcon, LatLngBounds } from 'leaflet';
import { CameraPublica } from './CamerasPublicas';
import 'leaflet/dist/leaflet.css';
import './MapaEquipamentos.css';
import './MapaMarcadores.css';

export type LocalizacaoEquipamento = {
  id: string;
  tipo: string;
  numeroSerie: string;
  bairro: string;
  cidade: string;
  uf: string;
  rua: string;
  referencia: string | null;
  latitude: number;
  longitude: number;
  situacao: string;
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
export type PontoHistoricoAgente = { latitude: number; longitude: number; registradaEm: string };
type Props = {
  equipamentos: LocalizacaoEquipamento[];
  agentes?: LocalizacaoAgente[];
  cameras?: CameraPublica[];
  localizacaoUsuario?: LocalizacaoUsuario | null;
  enquadramento?: 'usuario' | 'todos';
  trajetoAgente?: { nomeAgente: string; localizacoes: PontoHistoricoAgente[] } | null;
  onEditarEquipamento?: (id: string) => void;
  onEditarAgente?: (id: string) => void;
  onVerTrajetoAgente?: (id: string) => void;
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
const iconeCameraSvg = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 7h10a2 2 0 0 1 2 2v1l4-2v8l-4-2v1a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2Z"/><circle cx="9" cy="12" r="2.5"/></svg>';

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

const iconeAgente = divIcon({
  className: 'icone-leaflet-container',
  html: `<span class="marcador-mapa marcador-agente">${iconeAgenteSvg}</span>`,
  iconSize: [36, 44],
  iconAnchor: [18, 40],
  popupAnchor: [0, -38],
});
const iconeCamera = divIcon({
  className: 'icone-leaflet-container',
  html: `<span class="marcador-mapa marcador-camera">${iconeCameraSvg}</span>`,
  iconSize: [36, 44],
  iconAnchor: [18, 40],
  popupAnchor: [0, -38],
});
const iconeUsuario = divIcon({
  className: 'icone-leaflet-container',
  html: '<span class="marcador-usuario"><i></i></span>',
  iconSize: [32, 32],
  iconAnchor: [16, 16],
});

function AjustarEnquadramento({ equipamentos, agentes = [], cameras = [], localizacaoUsuario, enquadramento = 'usuario', trajetoAgente }: Props) {
  const mapa = useMap();

  useEffect(() => {
    const pontos = [
      ...equipamentos.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
      ...agentes.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
      ...cameras.filter((camera) => camera.latitude !== null && camera.longitude !== null)
        .map((camera) => [camera.latitude!, camera.longitude!] as [number, number]),
      ...(trajetoAgente?.localizacoes.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]) ?? []),
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
  }, [agentes, cameras, enquadramento, equipamentos, localizacaoUsuario, mapa, trajetoAgente]);

  return null;
}

export default function MapaEquipamentos({ equipamentos, agentes = [], cameras = [], localizacaoUsuario, enquadramento = 'usuario', trajetoAgente, onEditarEquipamento, onEditarAgente, onVerTrajetoAgente, onReproduzirCamera }: Props) {
  return (
    <div className="mapa-equipamentos" aria-label="Mapa de equipamentos e agentes com coordenadas">
      <MapContainer center={centroTupa} zoom={12} scrollWheelZoom className="mapa-leaflet">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <AjustarEnquadramento equipamentos={equipamentos} agentes={agentes} cameras={cameras} localizacaoUsuario={localizacaoUsuario} enquadramento={enquadramento} trajetoAgente={trajetoAgente} />
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
                <span>{equipamento.urlFonte ? 'Posição mapeada · situação não verificada' : rotuloSituacao[equipamento.situacao] ?? equipamento.situacao}</span>
                <span>{equipamento.rua}{equipamento.referencia ? ` · ${equipamento.referencia}` : ''}</span>
                <span>{equipamento.bairro}, {equipamento.cidade}/{equipamento.uf}</span>
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
            icon={iconeAgente}
          >
            <Popup>
              <div className="popup-equipamento">
                <strong>Agente · {agente.nome}</strong>
                <span>{agente.categoriaProfissional}</span>
                <span>{agente.situacao === 'disponivel' ? 'Disponível' : agente.situacao === 'emAtendimento' ? 'Em atendimento' : 'Indisponível'} · situação informada</span>
                <span>{agente.baseOperacional}</span>
                {agente.origemDados === 'simulado' && <strong className="popup-aviso">DADO SIMULADO · agente ilustrativo, não representa equipe real.</strong>}
                {onEditarAgente && <button type="button" className="mapa-acao-popup" onClick={() => onEditarAgente(agente.id)}>Editar agente</button>}
                {onVerTrajetoAgente && <button type="button" className="mapa-acao-popup mapa-acao-secundaria" onClick={() => onVerTrajetoAgente(agente.id)}>Ver posições da data escolhida</button>}
              </div>
            </Popup>
          </Marker>
        ))}
        {cameras.filter((camera) => camera.latitude !== null && camera.longitude !== null).map((camera) => (
          <Marker key={`camera-${camera.id}`} position={[camera.latitude!, camera.longitude!]} icon={iconeCamera}>
            <Popup>
              <div className="popup-equipamento">
                <strong>{camera.nome}</strong>
                <span>{camera.cidade}{camera.endereco ? ` · ${camera.endereco}` : ''}</span>
                {onReproduzirCamera && <button type="button" className="mapa-acao-popup mapa-camera-play" onClick={() => onReproduzirCamera(camera)}>▶ Assistir ao vivo</button>}
              </div>
            </Popup>
          </Marker>
        ))}
        {trajetoAgente && trajetoAgente.localizacoes.length > 0 && (
          <Polyline
            positions={trajetoAgente.localizacoes.map(({ latitude, longitude }) => [latitude, longitude] as [number, number])}
            pathOptions={{ color: '#f0ae62', weight: 4, opacity: 0.9 }}
          />
        )}
        {localizacaoUsuario && (
          <Marker position={[localizacaoUsuario.latitude, localizacaoUsuario.longitude]} icon={iconeUsuario} zIndexOffset={1000}>
            <Popup><div className="popup-equipamento"><strong>Você está aqui</strong><span>Sua localização atual, obtida pelo navegador e visível somente nesta sessão.</span></div></Popup>
          </Marker>
        )}
      </MapContainer>
    </div>
  );
}
