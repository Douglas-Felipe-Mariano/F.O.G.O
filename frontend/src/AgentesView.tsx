import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import './Agentes.css';

type SituacaoAgente = 'disponivel' | 'emAtendimento' | 'indisponivel';
type OrigemRegistro = 'manual' | 'importado' | 'simulado';
type Agente = {
  id: string;
  nome: string;
  categoriaProfissional: string;
  baseOperacional: string;
  latitude: number | null;
  longitude: number | null;
  situacao: SituacaoAgente;
  origemDados: OrigemRegistro;
  criadoEm: string;
  atualizadoEm: string;
};
type AgenteRequisicao = Omit<Agente, 'id' | 'criadoEm' | 'atualizadoEm'>;
type PaginaAgentes = { pagina: number; tamanhoPagina: number; totalRegistros: number; itens: Agente[] };
type IndicadoresAgentes = { totalCadastrado: number; porSituacao: { situacao: SituacaoAgente; quantidade: number }[] };

const situacoes: { valor: SituacaoAgente; rotulo: string }[] = [
  { valor: 'disponivel', rotulo: 'Disponível' },
  { valor: 'emAtendimento', rotulo: 'Em atendimento' },
  { valor: 'indisponivel', rotulo: 'Indisponível' },
];
const origens: { valor: OrigemRegistro; rotulo: string }[] = [
  { valor: 'manual', rotulo: 'Manual' },
  { valor: 'importado', rotulo: 'Importado' },
  { valor: 'simulado', rotulo: 'Simulado' },
];
const rotuloSituacao = new Map(situacoes.map((situacao) => [situacao.valor, situacao.rotulo]));
const vazioFormulario = (): AgenteRequisicao => ({
  nome: '', categoriaProfissional: '', baseOperacional: '', latitude: null, longitude: null,
  situacao: 'disponivel', origemDados: 'manual',
});

export default function AgentesView({ token, revisao, agenteIdParaEditar, onAgenteEditado }: { token: string; revisao: number; agenteIdParaEditar?: string | null; onAgenteEditado?: () => void }) {
  const [pagina, setPagina] = useState(1);
  const [categoria, setCategoria] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pesquisaCategoria, setPesquisaCategoria] = useState('');
  const [pesquisaSituacao, setPesquisaSituacao] = useState('');
  const [dados, setDados] = useState<PaginaAgentes>({ pagina: 1, tamanhoPagina: 25, totalRegistros: 0, itens: [] });
  const [indicadores, setIndicadores] = useState<IndicadoresAgentes | null>(null);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState('');
  const [mensagem, setMensagem] = useState('');
  const [modalAberto, setModalAberto] = useState(false);
  const [editando, setEditando] = useState<Agente | null>(null);
  const [formulario, setFormulario] = useState<AgenteRequisicao>(vazioFormulario);

  const chamarApi = useCallback(async (url: string, init?: RequestInit) => {
    const resposta = await fetch(url, {
      ...init,
      credentials: 'include',
      headers: { Authorization: `Bearer ${token}`, ...(init?.body ? { 'Content-Type': 'application/json' } : {}), ...init?.headers },
    });
    if (!resposta.ok) {
      const problema = await resposta.json().catch(() => null) as { title?: string; errors?: Record<string, string[]> } | null;
      const detalhe = problema?.errors ? Object.values(problema.errors).flat().join(' ') : problema?.title;
      throw new Error(detalhe || `Falha ao consultar agentes (${resposta.status}).`);
    }
    return resposta;
  }, [token]);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const parametros = new URLSearchParams({ pagina: String(pagina), tamanhoPagina: '25' });
      if (pesquisaCategoria) parametros.set('categoriaProfissional', pesquisaCategoria);
      if (pesquisaSituacao) parametros.set('situacao', pesquisaSituacao);
      const [respostaLista, respostaIndicadores] = await Promise.all([
        chamarApi(`/api/agentes?${parametros}`),
        chamarApi('/api/agentes/indicadores'),
      ]);
      setDados(await respostaLista.json() as PaginaAgentes);
      setIndicadores(await respostaIndicadores.json() as IndicadoresAgentes);
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível carregar o quadro de agentes.');
    } finally {
      setCarregando(false);
    }
  }, [chamarApi, pagina, pesquisaCategoria, pesquisaSituacao]);

  useEffect(() => { void carregar(); }, [carregar, revisao]);

  const categorias = useMemo(() => [...new Set(dados.itens.map((agente) => agente.categoriaProfissional))].sort(), [dados.itens]);

  function abrirNovo() {
    setEditando(null);
    setFormulario(vazioFormulario());
    setModalAberto(true);
    setErro('');
  }

  function abrirEdicao(agente: Agente) {
    setEditando(agente);
    setFormulario({
      nome: agente.nome,
      categoriaProfissional: agente.categoriaProfissional,
      baseOperacional: agente.baseOperacional,
      latitude: agente.latitude,
      longitude: agente.longitude,
      situacao: agente.situacao,
      origemDados: agente.origemDados,
    });
    setModalAberto(true);
    setErro('');
  }

  useEffect(() => {
    if (!agenteIdParaEditar) return;
    let cancelado = false;
    const carregarAgenteParaEditar = async () => {
      try {
        let agente = dados.itens.find((item) => item.id === agenteIdParaEditar);
        if (!agente) {
          const resposta = await chamarApi(`/api/agentes/${agenteIdParaEditar}`);
          agente = await resposta.json() as Agente;
        }
        if (cancelado) return;
        abrirEdicao(agente);
        onAgenteEditado?.();
      } catch (falha) {
        if (!cancelado) {
          setErro(falha instanceof Error ? falha.message : 'Não foi possível consultar este agente.');
          onAgenteEditado?.();
        }
      }
    };
    void carregarAgenteParaEditar();
    return () => { cancelado = true; };
  }, [agenteIdParaEditar, chamarApi, dados.itens, onAgenteEditado]);

  async function salvar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setErro('');
    setMensagem('');
    try {
      const resposta = await chamarApi(editando ? `/api/agentes/${editando.id}` : '/api/agentes', {
        method: editando ? 'PUT' : 'POST',
        body: JSON.stringify(formulario),
      });
      const agente = await resposta.json() as Agente;
      setMensagem(editando ? `Cadastro de ${agente.nome} atualizado.` : `${agente.nome} incluído no quadro.`);
      setModalAberto(false);
      await carregar();
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível salvar o cadastro.');
    }
  }

  const quantidade = (valor: SituacaoAgente) => indicadores?.porSituacao.find((item) => item.situacao === valor)?.quantidade ?? 0;

  return (
    <section className="agentes-view" aria-labelledby="agentes-title">
      <div className="agentes-heading">
        <div>
          <p className="eyebrow"><span>EQUIPE</span> · BASE OPERACIONAL</p>
          <h2 id="agentes-title">Agentes</h2>
          <p className="agentes-intro">Consulte a equipe e atualize a situação operacional informada.</p>
        </div>
        <button className="button button-primary" onClick={abrirNovo}>＋ Novo agente</button>
      </div>

      {erro && <p className="alert error" role="alert">{erro}</p>}
      {mensagem && <p className="alert success" role="status">{mensagem}</p>}

      <div className="agentes-indicadores">
        <article><span>CADASTRADOS</span><strong>{indicadores?.totalCadastrado ?? '—'}</strong></article>
        {situacoes.map((item) => <article key={item.valor}><span>{item.rotulo.toUpperCase()}</span><strong>{quantidade(item.valor)}</strong></article>)}
      </div>

      <section className="agentes-panel" aria-label="Lista de agentes">
        <form className="filters agentes-filtros" onSubmit={(evento) => { evento.preventDefault(); setPagina(1); setPesquisaCategoria(categoria); setPesquisaSituacao(situacao); }}>
          <label>Categoria profissional<input list="categorias-agentes" value={categoria} onChange={(evento) => setCategoria(evento.target.value)} /><datalist id="categorias-agentes">{categorias.map((item) => <option key={item}>{item}</option>)}</datalist></label>
          <label>Situação<select value={situacao} onChange={(evento) => setSituacao(evento.target.value)}><option value="">Todas</option>{situacoes.map((item) => <option key={item.valor} value={item.valor}>{item.rotulo}</option>)}</select></label>
          <button className="button button-secondary" type="submit">Aplicar filtros</button>
          <button className="button button-quiet" type="button" onClick={() => { setCategoria(''); setSituacao(''); setPesquisaCategoria(''); setPesquisaSituacao(''); setPagina(1); }}>Limpar</button>
          <button className="button button-quiet" type="button" onClick={() => void carregar()} disabled={carregando}>↻ Atualizar</button>
        </form>
        {carregando ? <div className="empty-state">Atualizando o quadro…</div> : dados.itens.length === 0 ? (
          <div className="empty-state"><span className="empty-icon">⌁</span><h3>{dados.totalRegistros ? 'Nenhum agente nesta página' : 'Nenhum agente cadastrado'}</h3><p>{dados.totalRegistros ? 'Altere os filtros ou navegue para outra página.' : 'Cadastre a equipe para acompanhar a situação operacional informada. Registros demonstrativos aparecem identificados como simulados.'}</p>{dados.totalRegistros === 0 && <button className="button button-primary" onClick={abrirNovo}>Cadastrar primeiro agente</button>}</div>
        ) : (
          <div className="table-wrap"><table><thead><tr><th>Agente</th><th>Base operacional</th><th>Situação informada</th><th>Origem</th><th><span className="sr-only">Ação</span></th></tr></thead><tbody>
            {dados.itens.map((agente) => <tr key={agente.id}>
              <td><strong>{agente.nome}</strong><span className="cell-secondary">{agente.categoriaProfissional}</span></td>
              <td>{agente.baseOperacional}<span className="cell-secondary">{agente.latitude !== null && agente.longitude !== null ? `${agente.latitude}, ${agente.longitude}` : 'Localização não informada'}</span></td>
              <td><span className={`status-pill status-${agente.situacao.toLowerCase()}`}>{rotuloSituacao.get(agente.situacao) ?? agente.situacao}</span></td>
              <td><span className={`origin-tag origin-${agente.origemDados}`}>{origens.find((origem) => origem.valor === agente.origemDados)?.rotulo}</span></td>
              <td><button className="icon-button" aria-label={`Editar cadastro de ${agente.nome}`} onClick={() => abrirEdicao(agente)}>↗</button></td>
            </tr>)}
          </tbody></table></div>
        )}
        <div className="pagination"><span>{dados.totalRegistros ? `${(pagina - 1) * 25 + 1}–${Math.min(pagina * 25, dados.totalRegistros)} de ${dados.totalRegistros}` : '0 registros'}</span><div><button className="button button-quiet" disabled={pagina <= 1 || carregando} onClick={() => setPagina((atual) => atual - 1)}>← Anterior</button><span>Página {pagina}</span><button className="button button-quiet" disabled={pagina * 25 >= dados.totalRegistros || carregando} onClick={() => setPagina((atual) => atual + 1)}>Próxima →</button></div></div>
      </section>

      {modalAberto && <div className="modal-backdrop" role="presentation" onMouseDown={(evento) => { if (evento.target === evento.currentTarget) setModalAberto(false); }}>
        <section className="equipment-modal agente-modal" role="dialog" aria-modal="true" aria-labelledby="agente-modal-title">
          <div className="modal-heading"><div><p className="eyebrow">EQUIPE OPERACIONAL</p><h2 id="agente-modal-title">{editando ? 'Atualizar cadastro' : 'Novo agente'}</h2></div><button className="icon-button" aria-label="Fechar" onClick={() => setModalAberto(false)}>×</button></div>
          <form className="equipment-form" onSubmit={(evento) => void salvar(evento)}>
            <label>Nome<input required maxLength={160} value={formulario.nome} onChange={(evento) => setFormulario({ ...formulario, nome: evento.target.value })} /></label>
            <label>Categoria profissional<input required maxLength={120} placeholder="Ex.: categoria informada pela equipe" value={formulario.categoriaProfissional} onChange={(evento) => setFormulario({ ...formulario, categoriaProfissional: evento.target.value })} /></label>
            <label>Base operacional<input required maxLength={240} value={formulario.baseOperacional} onChange={(evento) => setFormulario({ ...formulario, baseOperacional: evento.target.value })} /></label>
            <div className="form-grid">
              <label>Latitude<input type="number" step="any" min="-90" max="90" value={formulario.latitude ?? ''} onChange={(evento) => setFormulario({ ...formulario, latitude: evento.target.value === '' ? null : Number(evento.target.value) })} /></label>
              <label>Longitude<input type="number" step="any" min="-180" max="180" value={formulario.longitude ?? ''} onChange={(evento) => setFormulario({ ...formulario, longitude: evento.target.value === '' ? null : Number(evento.target.value) })} /></label>
            </div>
            <label>Situação operacional<select value={formulario.situacao} onChange={(evento) => setFormulario({ ...formulario, situacao: evento.target.value as SituacaoAgente })}>{situacoes.map((item) => <option key={item.valor} value={item.valor}>{item.rotulo}</option>)}</select></label>
            <label>Origem do registro<select value={formulario.origemDados} onChange={(evento) => setFormulario({ ...formulario, origemDados: evento.target.value as OrigemRegistro })}>{origens.map((item) => <option key={item.valor} value={item.valor}>{item.rotulo}</option>)}</select></label>
            {erro && <p className="alert error" role="alert">{erro}</p>}
            <p className="form-hint">A situação é informada manualmente; o sistema não rastreia atividade nem localização em tempo real.</p>
            <div className="modal-actions"><button className="button button-quiet" type="button" onClick={() => setModalAberto(false)}>Cancelar</button><button className="button button-primary" type="submit">{editando ? 'Salvar alterações' : 'Cadastrar agente'}</button></div>
          </form>
        </section>
      </div>}
    </section>
  );
}
