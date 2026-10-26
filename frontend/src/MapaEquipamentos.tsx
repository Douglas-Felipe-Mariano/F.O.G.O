import { useEffect } from 'react';
import { MapContainer, Marker, Popup, TileLayer, useMap } from 'react-leaflet';
import { divIcon, LatLngBounds } from 'leaflet';
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
type Props = {
  equipamentos: LocalizacaoEquipamento[];
  agentes?: LocalizacaoAgente[];
  localizacaoUsuario?: LocalizacaoUsuario | null;
  enquadramento?: 'usuario' | 'todos';
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
const iconeUsuario = divIcon({
  className: 'icone-leaflet-container',
  html: '<span class="marcador-usuario"><i></i></span>',
  iconSize: [32, 32],
  iconAnchor: [16, 16],
});

function AjustarEnquadramento({ equipamentos, agentes = [], localizacaoUsuario, enquadramento = 'usuario' }: Props) {
  const mapa = useMap();

  useEffect(() => {
    const pontos = [
      ...equipamentos.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
      ...agentes.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
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
  }, [agentes, enquadramento, equipamentos, localizacaoUsuario, mapa]);

  return null;
}

export default function MapaEquipamentos({ equipamentos, agentes = [], localizacaoUsuario, enquadramento = 'usuario' }: Props) {
  return (
    <div className="mapa-equipamentos" aria-label="Mapa de equipamentos e agentes com coordenadas">
      <MapContainer center={centroTupa} zoom={12} scrollWheelZoom className="mapa-leaflet">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <AjustarEnquadramento equipamentos={equipamentos} agentes={agentes} localizacaoUsuario={localizacaoUsuario} enquadramento={enquadramento} />
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
