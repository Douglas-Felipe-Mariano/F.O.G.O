import { FormEvent, useCallback, useEffect, useState } from 'react';
import './HistoricoAgenteModal.css';

export type OrigemHistoricoAgente = 'manual' | 'importado' | 'simulado';
export type RegistroHistoricoAgente = {
  id: string;
  agenteId: string;
  latitude: number;
  longitude: number;
  registradaEm: string;
};
export type PaginaHistoricoAgente = {
  agenteId: string;
  nomeAgente: string;
  origemDados: OrigemHistoricoAgente;
  deslocamento: number;
  limite: number;
  totalRegistros: number;
  resultadoTruncado: boolean;
  mensagem: string;
  localizacoes: RegistroHistoricoAgente[];
};
type FiltrosHistorico = { inicio: string; fim: string };

type Props = {
  agente: { id: string; nome: string; origemDados: OrigemHistoricoAgente };
  onFechar: () => void;
  listar: (agenteId: string, inicio: string, fim: string, deslocamento: number, limite: number) => Promise<PaginaHistoricoAgente>;
};

const limitePagina = 100;
const origemLabel: Record<OrigemHistoricoAgente, string> = {
  manual: 'Manual',
  importado: 'Importado',
  simulado: 'Simulado',
};

export default function HistoricoAgenteModal({ agente, onFechar, listar }: Props) {
  const [filtros, setFiltros] = useState<FiltrosHistorico>({ inicio: '', fim: '' });
  const [filtrosAplicados, setFiltrosAplicados] = useState<FiltrosHistorico>({ inicio: '', fim: '' });
  const [pagina, setPagina] = useState<PaginaHistoricoAgente | null>(null);
  const [deslocamento, setDeslocamento] = useState(0);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState('');

  const carregar = useCallback(async (periodo: FiltrosHistorico, novoDeslocamento: number) => {
    setCarregando(true);
    setErro('');
    try {
      const resultado = await listar(agente.id, periodo.inicio, periodo.fim, novoDeslocamento, limitePagina);
      setPagina(resultado);
      setDeslocamento(novoDeslocamento);
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível consultar o histórico deste agente.');
    } finally {
      setCarregando(false);
    }
  }, [agente.id, listar]);

  useEffect(() => {
    void carregar({ inicio: '', fim: '' }, 0);
  }, [carregar]);

  useEffect(() => {
    const fecharComEscape = (evento: KeyboardEvent) => {
      if (evento.key === 'Escape') onFechar();
    };
    window.addEventListener('keydown', fecharComEscape);
    return () => window.removeEventListener('keydown', fecharComEscape);
  }, [onFechar]);

  function aplicarFiltros(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    if (filtros.inicio && filtros.fim && filtros.inicio > filtros.fim) {
      setErro('A data inicial deve ser anterior ou igual à data final.');
      return;
    }
    setFiltrosAplicados(filtros);
    void carregar(filtros, 0);
  }

  const total = pagina?.totalRegistros ?? 0;
  const inicioExibido = total === 0 ? 0 : deslocamento + 1;
  const fimExibido = Math.min(deslocamento + (pagina?.localizacoes.length ?? 0), total);

  return (
    <div className="historico-agente-fundo" role="presentation" onMouseDown={(evento) => { if (evento.target === evento.currentTarget) onFechar(); }}>
      <section className="historico-agente-modal" role="dialog" aria-modal="true" aria-labelledby="historico-agente-titulo">
        <header className="historico-agente-cabecalho">
          <div>
            <span>REGISTROS ARMAZENADOS · {origemLabel[agente.origemDados].toUpperCase()}</span>
            <h2 id="historico-agente-titulo">Localizações de {agente.nome}</h2>
            <p>Consulte todos os pontos registrados e filtre pelo período. Os registros não representam rastreamento em tempo real.</p>
          </div>
          <button type="button" className="historico-agente-fechar" aria-label="Fechar histórico" onClick={onFechar}>×</button>
        </header>

        <form className="historico-agente-filtros" onSubmit={aplicarFiltros}>
          <label>Data inicial<input type="date" value={filtros.inicio} onChange={(evento) => setFiltros((atual) => ({ ...atual, inicio: evento.target.value }))} /></label>
          <label>Data final<input type="date" value={filtros.fim} onChange={(evento) => setFiltros((atual) => ({ ...atual, fim: evento.target.value }))} /></label>
          <button className="button button-secondary" type="submit" disabled={carregando}>Aplicar filtros</button>
          <button className="button button-quiet" type="button" disabled={carregando && !filtros.inicio && !filtros.fim} onClick={() => {
            const limpos = { inicio: '', fim: '' };
            setFiltros(limpos);
            setFiltrosAplicados(limpos);
            void carregar(limpos, 0);
          }}>Limpar</button>
        </form>

        {erro && <p className="historico-agente-erro" role="alert">{erro}</p>}
        <div className="historico-agente-resumo" aria-live="polite">
          <strong>{carregando ? 'Consultando registros…' : `${total} registro(s)`}</strong>
          {!carregando && total > 0 && <span>Exibindo {inicioExibido}–{fimExibido} · ordem cronológica</span>}
        </div>
        {pagina?.origemDados === 'simulado' && <p className="historico-agente-aviso">{pagina.mensagem}</p>}
        {!carregando && pagina?.localizacoes.length === 0 && <div className="historico-agente-vazio">{pagina.mensagem}</div>}
        <div className="historico-agente-tabela-wrap">
          {pagina && pagina.localizacoes.length > 0 && <table className="historico-agente-tabela">
            <thead><tr><th>Data e hora</th><th>Latitude</th><th>Longitude</th></tr></thead>
            <tbody>{pagina.localizacoes.map((registro) => <tr key={registro.id}>
              <td>{new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'medium' }).format(new Date(registro.registradaEm))}</td>
              <td>{registro.latitude.toFixed(6)}</td>
              <td>{registro.longitude.toFixed(6)}</td>
            </tr>)}</tbody>
          </table>}
        </div>
        <footer className="historico-agente-rodape">
          <span>As posições exibidas são os registros persistidos no sistema.</span>
          <div>
            <button type="button" className="button button-quiet" disabled={carregando || deslocamento === 0} onClick={() => void carregar(filtrosAplicados, Math.max(0, deslocamento - limitePagina))}>Anterior</button>
            <button type="button" className="button button-quiet" disabled={carregando || !pagina?.resultadoTruncado} onClick={() => void carregar(filtrosAplicados, deslocamento + limitePagina)}>Próxima</button>
            <button type="button" className="button button-secondary" onClick={onFechar}>Fechar</button>
          </div>
        </footer>
      </section>
    </div>
  );
}
