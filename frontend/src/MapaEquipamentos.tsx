import { useEffect } from 'react';
import { CircleMarker, MapContainer, Popup, TileLayer, useMap } from 'react-leaflet';
import { LatLngBounds } from 'leaflet';
import 'leaflet/dist/leaflet.css';
import './MapaEquipamentos.css';

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
};

export type LocalizacaoAgente = {
  id: string;
  nome: string;
  categoriaProfissional: string;
  baseOperacional: string;
  latitude: number;
  longitude: number;
  situacao: string;
};

type Props = { equipamentos: LocalizacaoEquipamento[]; agentes?: LocalizacaoAgente[] };

const centroBrasil: [number, number] = [-15.78, -47.93];
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

function AjustarEnquadramento({ equipamentos, agentes = [] }: Props) {
  const mapa = useMap();

  useEffect(() => {
    const pontos = [
      ...equipamentos.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
      ...agentes.map(({ latitude, longitude }) => [latitude, longitude] as [number, number]),
    ];
    if (pontos.length === 0) {
      mapa.setView(centroBrasil, 4);
      return;
    }

    if (pontos.length === 1) {
      mapa.setView(pontos[0], 13);
      return;
    }

    mapa.fitBounds(new LatLngBounds(pontos), { padding: [36, 36], maxZoom: 13 });
  }, [agentes, equipamentos, mapa]);

  return null;
}

export default function MapaEquipamentos({ equipamentos, agentes = [] }: Props) {
  return (
    <div className="mapa-equipamentos" aria-label="Mapa de equipamentos e agentes com coordenadas">
      <MapContainer center={centroBrasil} zoom={4} scrollWheelZoom className="mapa-leaflet">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <AjustarEnquadramento equipamentos={equipamentos} agentes={agentes} />
        {equipamentos.map((equipamento) => (
          <CircleMarker
            key={equipamento.id}
            center={[equipamento.latitude, equipamento.longitude]}
            radius={8}
            pathOptions={{
              color: '#101716',
              weight: 2,
              fillColor: corSituacao[equipamento.situacao] ?? '#c8c9ee',
              fillOpacity: 0.95,
            }}
          >
            <Popup>
              <div className="popup-equipamento">
                <strong>{equipamento.tipo}</strong>
                <span>Série {equipamento.numeroSerie}</span>
                <span>{rotuloSituacao[equipamento.situacao] ?? equipamento.situacao}</span>
                <span>{equipamento.rua}{equipamento.referencia ? ` · ${equipamento.referencia}` : ''}</span>
                <span>{equipamento.bairro}, {equipamento.cidade}/{equipamento.uf}</span>
              </div>
            </Popup>
          </CircleMarker>
        ))}
        {agentes.map((agente) => (
          <CircleMarker
            key={`agente-${agente.id}`}
            center={[agente.latitude, agente.longitude]}
            radius={7}
            pathOptions={{ color: '#101716', weight: 2, fillColor: '#86b8ff', fillOpacity: 0.95 }}
          >
            <Popup>
              <div className="popup-equipamento">
                <strong>Agente · {agente.nome}</strong>
                <span>{agente.categoriaProfissional}</span>
                <span>{agente.situacao === 'disponivel' ? 'Disponível' : agente.situacao === 'emAtendimento' ? 'Em atendimento' : 'Indisponível'} · situação informada</span>
                <span>{agente.baseOperacional}</span>
              </div>
            </Popup>
          </CircleMarker>
        ))}
      </MapContainer>
    </div>
  );
}
