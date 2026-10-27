import { FormEvent, useCallback, useEffect, useState } from 'react';
import { AcidenteMapa, AgenteOperacional, AlertaOperacional, TransitoMapa } from './OperacoesTipos';
import './PainelOperacoes.css';

type Props = {
  chamarApi: (url: string, init?: RequestInit) => Promise<Response>;
  obterTokenCsrf: () => Promise<string>;
  localSelecionado: { latitude: number; longitude: number } | null;
  selecionandoLocal: boolean;
  onSolicitarSelecao: (selecionar: boolean) => void;
  onTransitoAtualizado: (itens: TransitoMapa[]) => void;
  onAcidentesAtualizados: (itens: AcidenteMapa[]) => void;
};

type RespostaAgentes = { itens: AgenteOperacional[] };
type FormularioAcidente = {
  localDescricao: string;
  cidade: string;
  latitude: string;
  longitude: string;
  grau: AcidenteMapa['grau'];
  impactoTransito: AcidenteMapa['impactoTransito'];
  haFeridos: boolean;
};
const formularioInicial = (): FormularioAcidente => ({ localDescricao: '', cidade: 'Tupã', latitude: '', longitude: '', grau: 'leve', impactoTransito: 'nenhum', haFeridos: false });
const rotuloGrau: Record<AcidenteMapa['grau'], string> = { leve: 'Leve', moderado: 'Moderado', grave: 'Grave', critico: 'Crítico' };
const rotuloImpacto: Record<AcidenteMapa['impactoTransito'], string> = { nenhum: 'Sem impacto', lentidao: 'Lentidão', bloqueioParcial: 'Bloqueio parcial', interdicaoTotal: 'Interdição total' };

export default function PainelOperacoes({ chamarApi, obterTokenCsrf, localSelecionado, selecionandoLocal, onSolicitarSelecao, onTransitoAtualizado, onAcidentesAtualizados }: Props) {
  const [transito, setTransito] = useState<TransitoMapa[]>([]);
  const [alertas, setAlertas] = useState<AlertaOperacional[]>([]);
  const [acidentes, setAcidentes] = useState<AcidenteMapa[]>([]);
  const [agentes, setAgentes] = useState<AgenteOperacional[]>([]);
  const [agentesPorAcidente, setAgentesPorAcidente] = useState<Record<string, string>>({});
  const [formulario, setFormulario] = useState<FormularioAcidente>(formularioInicial);
  const [formularioAberto, setFormularioAberto] = useState(false);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState('');
  const [mensagem, setMensagem] = useState('');

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const [respostaTransito, respostaAlertas, respostaAcidentes, respostaAgentes] = await Promise.all([
        chamarApi('/api/operacoes/transito'),
        chamarApi('/api/operacoes/alertas'),
        chamarApi('/api/operacoes/acidentes'),
        chamarApi('/api/agentes?pagina=1&tamanhoPagina=100'),
      ]);
      const [itensTransito, itensAlertas, itensAcidentes, listaAgentes] = await Promise.all([
        respostaTransito.json() as Promise<TransitoMapa[]>,
        respostaAlertas.json() as Promise<AlertaOperacional[]>,
        respostaAcidentes.json() as Promise<AcidenteMapa[]>,
        respostaAgentes.json() as Promise<RespostaAgentes>,
      ]);
      setTransito(itensTransito);
      setAlertas(itensAlertas);
      setAcidentes(itensAcidentes);
      setAgentes(listaAgentes.itens);
      onTransitoAtualizado(itensTransito);
      onAcidentesAtualizados(itensAcidentes);
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível carregar os dados de operações.');
    } finally {
      setCarregando(false);
    }
  }, [chamarApi, onAcidentesAtualizados, onTransitoAtualizado]);

  useEffect(() => { void carregar(); }, [carregar]);

  useEffect(() => {
    if (!localSelecionado) return;
    setFormulario((atual) => ({ ...atual, latitude: localSelecionado.latitude.toFixed(6), longitude: localSelecionado.longitude.toFixed(6) }));
  }, [localSelecionado]);

  const enviarPost = async (rota: string, corpo?: unknown) => {
    const csrf = await obterTokenCsrf();
    return chamarApi(rota, {
      method: 'POST',
      headers: { 'X-CSRF-TOKEN': csrf },
      ...(corpo === undefined ? {} : { body: JSON.stringify(corpo) }),
    });
  };

  const registrarAcidente = async (evento: FormEvent<HTMLFormElement>) => {
    evento.preventDefault();
    setErro('');
    setMensagem('');
    const latitude = Number(formulario.latitude);
    const longitude = Number(formulario.longitude);
    if (!formulario.localDescricao.trim() || !formulario.cidade.trim() || !Number.isFinite(latitude) || !Number.isFinite(longitude)) {
      setErro('Informe local, cidade e selecione no mapa ou digite coordenadas válidas.');
      return;
    }
    try {
      await enviarPost('/api/operacoes/acidentes', { ...formulario, latitude, longitude });
      setFormulario(formularioInicial());
      setFormularioAberto(false);
      onSolicitarSelecao(false);
      setMensagem('Ocorrência registrada no sistema. Nenhum serviço externo foi contatado.');
      await carregar();
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível registrar o acidente.');
    }
  };

  const executarAcao = async (rota: string, confirmacao: string) => {
    setErro('');
    setMensagem('');
    try {
      await enviarPost(rota);
      setMensagem(confirmacao);
      await carregar();
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível registrar a ação.');
    }
  };

  return (
    <section className="painel-operacoes" aria-label="Trânsito, alertas e acidentes">
      <header className="operacoes-cabecalho">
        <div><p className="eyebrow">CAMADA OPERACIONAL · TRÁFEGO E OCORRÊNCIAS</p><h3>Trânsito, previsões e alertas</h3><p>Histórico de trânsito e previsões atualmente são sintéticos para demonstração; não representam medições em tempo real.</p></div>
        <button type="button" className="button button-quiet" onClick={() => void carregar()} disabled={carregando}>↻ Atualizar</button>
      </header>
      {erro && <p className="alert error operacoes-feedback" role="alert">{erro}</p>}
      {mensagem && <p className="operacoes-feedback operacoes-sucesso" role="status">{mensagem}</p>}
      <div className="operacoes-grade">
        <section className="operacoes-bloco" aria-labelledby="transito-operacoes-title">
          <div className="operacoes-bloco-cabecalho"><div><span className="operacoes-kicker">VOLUME E VELOCIDADE</span><h4 id="transito-operacoes-title">Trânsito por município</h4></div><span className="origem-tag origin-simulado">SIMULADO</span></div>
          {transito.length === 0 ? <p className="operacoes-vazio">{carregando ? 'Carregando histórico…' : 'Sem histórico disponível.'}</p> : <div className="transito-lista">{transito.map((item) => (
            <article className="transito-resumo" key={item.id}>
              <div><strong>{item.cidade}</strong><span>{item.veiculosUltimaHora} veículos/h · {item.velocidadeMediaKmh} km/h</span></div>
              <div className="transito-previsto"><b>{item.congestionamentoPercentual}% → {item.congestionamentoPrevistoPercentual}%</b><span>atual → próxima hora · confiança {item.confianca}</span></div>
            </article>
          ))}</div>}
          <p className="operacoes-nota">Predição heurística: média das observações históricas do mesmo dia da semana e hora local. Não é modelo validado.</p>
        </section>

        <section className="operacoes-bloco" aria-labelledby="alertas-operacoes-title">
          <div className="operacoes-bloco-cabecalho"><div><span className="operacoes-kicker">MANUTENÇÃO · ANOMALIAS · PROJEÇÕES</span><h4 id="alertas-operacoes-title">Alertas ({alertas.length})</h4></div></div>
          {alertas.length === 0 ? <p className="operacoes-vazio">Nenhum alerta ativo pela regra atual.</p> : <div className="alertas-operacionais-lista">{alertas.map((alerta, indice) => (
            <article className={`alerta-operacional alerta-${alerta.severidade}`} key={`${alerta.tipo}-${alerta.recursoId ?? indice}`}>
              <span className="alerta-sinal" aria-hidden="true">!</span><div><strong>{alerta.titulo}</strong><p>{alerta.descricao}</p><small>{alerta.cidade} · origem {alerta.origemDados}</small></div>
            </article>
          ))}</div>}
          <p className="operacoes-nota">Alertas são calculados ao consultar os dados; compare com a situação real antes de agir.</p>
        </section>

        <section className="operacoes-bloco operacoes-acidentes" aria-labelledby="acidentes-operacoes-title">
          <div className="operacoes-bloco-cabecalho"><div><span className="operacoes-kicker">REGISTRO PERSISTENTE</span><h4 id="acidentes-operacoes-title">Acidentes ({acidentes.length})</h4></div><button type="button" className="button button-secondary" onClick={() => setFormularioAberto((aberto) => !aberto)}>{formularioAberto ? 'Cancelar' : '+ Registrar acidente'}</button></div>
          {formularioAberto && <form className="acidente-formulario" onSubmit={(evento) => void registrarAcidente(evento)}>
            <label>Local<input required maxLength={240} value={formulario.localDescricao} onChange={(evento) => setFormulario({ ...formulario, localDescricao: evento.target.value })} placeholder="Rua, cruzamento ou referência" /></label>
            <div className="acidente-form-linha"><label>Cidade<input required maxLength={120} value={formulario.cidade} onChange={(evento) => setFormulario({ ...formulario, cidade: evento.target.value })} /></label><button type="button" className={`button ${selecionandoLocal ? 'button-primary' : 'button-quiet'}`} onClick={() => onSolicitarSelecao(!selecionandoLocal)}>{selecionandoLocal ? 'Clique no mapa…' : 'Escolher no mapa'}</button></div>
            <p className="form-hint">{formulario.latitude && formulario.longitude ? `Coordenadas: ${formulario.latitude}, ${formulario.longitude}` : 'Selecione um ponto no mapa ou preencha latitude e longitude.'}</p>
            <div className="acidente-form-linha"><label>Latitude<input required type="number" step="any" min="-90" max="90" value={formulario.latitude} onChange={(evento) => setFormulario({ ...formulario, latitude: evento.target.value })} /></label><label>Longitude<input required type="number" step="any" min="-180" max="180" value={formulario.longitude} onChange={(evento) => setFormulario({ ...formulario, longitude: evento.target.value })} /></label></div>
            <div className="acidente-form-linha"><label>Grau<select value={formulario.grau} onChange={(evento) => setFormulario({ ...formulario, grau: evento.target.value as FormularioAcidente['grau'] })}>{Object.entries(rotuloGrau).map(([valor, rotulo]) => <option key={valor} value={valor}>{rotulo}</option>)}</select></label><label>Impacto no trânsito<select value={formulario.impactoTransito} onChange={(evento) => setFormulario({ ...formulario, impactoTransito: evento.target.value as FormularioAcidente['impactoTransito'] })}>{Object.entries(rotuloImpacto).map(([valor, rotulo]) => <option key={valor} value={valor}>{rotulo}</option>)}</select></label></div>
            <label className="acidente-feridos"><input type="checkbox" checked={formulario.haFeridos} onChange={(evento) => setFormulario({ ...formulario, haFeridos: evento.target.checked })} /> Há feridos informados</label>
            <button type="submit" className="button button-primary">Salvar ocorrência</button>
          </form>}
          {acidentes.length === 0 ? <p className="operacoes-vazio">Nenhum acidente registrado.</p> : <div className="acidentes-lista">{acidentes.map((acidente) => (
            <article className="acidente-item" key={acidente.id}>
              <div className="acidente-item-titulo"><strong>{acidente.localDescricao}</strong><span className={`acidente-grau acidente-grau-${acidente.grau}`}>{rotuloGrau[acidente.grau]}</span></div>
              <p>{acidente.cidade} · {rotuloImpacto[acidente.impactoTransito]} · {acidente.haFeridos ? 'feridos informados' : 'sem feridos informados'} · {acidente.situacao}</p>
              <label className="acidente-agente-select">Agente<select value={agentesPorAcidente[acidente.id] ?? acidente.agenteDesignadoId ?? ''} onChange={(evento) => setAgentesPorAcidente({ ...agentesPorAcidente, [acidente.id]: evento.target.value })}><option value="">Sem designação</option>{agentes.map((agente) => <option key={agente.id} value={agente.id}>{agente.nome} · {agente.situacao}</option>)}</select></label>
              <div className="acidente-acoes">
                <button type="button" className="button button-secondary" disabled={Boolean(acidente.despachoSolicitadoEm) || acidente.situacao === 'encerrado'} onClick={() => void (async () => { try { await enviarPost(`/api/operacoes/acidentes/${acidente.id}/despachar`, { agenteId: agentesPorAcidente[acidente.id] || null }); setMensagem('Solicitação de despacho registrada internamente; nenhum agente foi contatado por sistema externo.'); await carregar(); } catch (falha) { setErro(falha instanceof Error ? falha.message : 'Não foi possível registrar o despacho.'); } })()}>{acidente.despachoSolicitadoEm ? 'Despacho registrado' : 'Registrar envio de agente'}</button>
                <button type="button" className="button button-quiet" disabled={Boolean(acidente.socorroSolicitadoEm) || acidente.situacao === 'encerrado'} onClick={() => void executarAcao(`/api/operacoes/acidentes/${acidente.id}/acionar-socorro`, 'Solicitação de socorro registrada internamente; nenhum serviço externo foi contatado.')}>{acidente.socorroSolicitadoEm ? 'Socorro registrado' : 'Registrar acionamento de socorro'}</button>
                {acidente.situacao !== 'encerrado' && <button type="button" className="button button-quiet" onClick={() => void executarAcao(`/api/operacoes/acidentes/${acidente.id}/encerrar`, 'Ocorrência encerrada no sistema.')}>Encerrar</button>}
              </div>
            </article>
          ))}</div>}
          <p className="operacoes-nota">As ações de despacho e socorro registram solicitações no F.O.G.O.; não enviam equipes nem acionam serviços de emergência.</p>
        </section>
      </div>
    </section>
  );
}
