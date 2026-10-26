import { useEffect, useMemo, useRef, useState } from 'react';
import Hls from 'hls.js';
import './CamerasPublicas.css';

export type CameraPublica = {
  id: string;
  cidade: string;
  nome: string;
  endereco: string | null;
  latitude: number | null;
  longitude: number | null;
  urlStream: string;
};

export function VideoCameraModal({ camera, onFechar }: { camera: CameraPublica | null; onFechar: () => void }) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [erro, setErro] = useState('');

  useEffect(() => {
    if (!camera || !videoRef.current) return;
    const video = videoRef.current;
    setErro('');
    let hls: Hls | null = null;

    if (Hls.isSupported()) {
      hls = new Hls({ enableWorker: true, lowLatencyMode: true });
      hls.loadSource(camera.urlStream);
      hls.attachMedia(video);
      hls.on(Hls.Events.ERROR, (_event, data) => {
        if (data.fatal) setErro('Não foi possível carregar esta transmissão. Verifique se o stream está disponível.');
      });
    } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
      video.src = camera.urlStream;
      video.addEventListener('error', () => setErro('Não foi possível carregar esta transmissão. Verifique se o stream está disponível.'));
    } else {
      setErro('Este navegador não oferece suporte à reprodução HLS.');
    }

    const fecharComEscape = (evento: KeyboardEvent) => {
      if (evento.key === 'Escape') onFechar();
    };
    window.addEventListener('keydown', fecharComEscape);
    return () => {
      window.removeEventListener('keydown', fecharComEscape);
      hls?.destroy();
      video.pause();
      video.removeAttribute('src');
      video.load();
    };
  }, [camera, onFechar]);

  useEffect(() => {
    if (!camera) return;
    const anterior = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const temporizador = window.setTimeout(() => videoRef.current?.focus(), 0);
    return () => {
      window.clearTimeout(temporizador);
      anterior?.focus();
    };
  }, [camera]);

  if (!camera) return null;
  return (
    <div className="camera-modal-fundo" role="presentation" onMouseDown={(evento) => { if (evento.target === evento.currentTarget) onFechar(); }}>
      <section className="camera-modal" role="dialog" aria-modal="true" aria-labelledby="camera-modal-titulo">
        <header className="camera-modal-cabecalho">
          <div><span>TRANSMISSÃO AO VIVO</span><h2 id="camera-modal-titulo">{camera.nome}</h2><p>{camera.cidade}{camera.endereco ? ` · ${camera.endereco}` : ''}</p></div>
          <button type="button" className="camera-modal-fechar" aria-label="Fechar vídeo" onClick={onFechar}>×</button>
        </header>
        <div className="camera-video-wrap">
          <video ref={videoRef} controls playsInline aria-label={`Vídeo ao vivo: ${camera.nome}`} />
          {erro && <p className="camera-video-erro" role="alert">{erro}</p>}
        </div>
        <footer className="camera-modal-acoes"><span>A reprodução depende da disponibilidade da transmissão.</span><span>Stream ao vivo · somente visualização</span></footer>
      </section>
    </div>
  );
}

export function CatalogoCameras({ cameras, onPlay, onFechar }: { cameras: CameraPublica[]; onPlay: (camera: CameraPublica) => void; onFechar: () => void }) {
  const cidades = useMemo(() => [...new Set(cameras.map((camera) => camera.cidade))].sort((a, b) => a.localeCompare(b, 'pt-BR')), [cameras]);
  const [cidade, setCidade] = useState('');
  const filtradas = cidade ? cameras.filter((camera) => camera.cidade === cidade) : cameras;

  useEffect(() => {
    const fecharComEscape = (evento: KeyboardEvent) => { if (evento.key === 'Escape') onFechar(); };
    window.addEventListener('keydown', fecharComEscape);
    return () => window.removeEventListener('keydown', fecharComEscape);
  }, [onFechar]);

  return (
    <div className="camera-modal-fundo camera-catalogo-fundo" role="presentation" onMouseDown={(evento) => { if (evento.target === evento.currentTarget) onFechar(); }}>
      <section className="camera-modal camera-catalogo" role="dialog" aria-modal="true" aria-labelledby="camera-catalogo-titulo">
        <header className="camera-modal-cabecalho"><div><span>CATÁLOGO DE CÂMERAS PÚBLICAS</span><h2 id="camera-catalogo-titulo">Transmissões disponíveis <small>{cameras.length}</small></h2><p>Selecione uma cidade e reproduza uma das transmissões listadas.</p></div><button type="button" className="camera-modal-fechar" aria-label="Fechar catálogo" onClick={onFechar}>×</button></header>
        <label className="camera-filtro-cidade">Cidade<select value={cidade} onChange={(evento) => setCidade(evento.target.value)}><option value="">Todas as cidades</option>{cidades.map((item) => <option key={item} value={item}>{item}</option>)}</select></label>
        <div className="camera-lista-catalogo">{filtradas.map((camera) => <article key={camera.id} className="camera-item-catalogo"><div><strong>{camera.nome}</strong><span>{camera.cidade}{camera.endereco ? ` · ${camera.endereco}` : ''}</span>{camera.latitude === null && <small>Localização no mapa não disponível</small>}</div><button type="button" className="camera-botao-play" aria-label={`Reproduzir ${camera.nome}, ${camera.cidade}`} onClick={() => onPlay(camera)}>▶ <span>Assistir</span></button></article>)}</div>
        <footer className="camera-modal-acoes"><span>As transmissões são carregadas sob demanda.</span><button type="button" className="button button-quiet" onClick={onFechar}>Fechar</button></footer>
      </section>
    </div>
  );
}
