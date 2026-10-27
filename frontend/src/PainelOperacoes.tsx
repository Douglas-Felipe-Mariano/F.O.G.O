import { FormEvent, useCallback, useEffect, useState } from 'react';
import { AcidenteMapa, AgenteOperacional, AlertaOperacional, PontoCalor, PredicoesOperacionais, TransitoMapa } from './OperacoesTipos';
import './PainelOperacoes.css';
import './PainelPredicoes.css';
import './AcidenteMapa.css';

type Props = {
  chamarApi: (url: string, init?: RequestInit) => Promise<Response>;
  obterTokenCsrf: () => Promise<string>;
  localSelecionado: { latitude: number; longitude: number } | null;
  selecionandoLocal: boolean;
  onSolicitarSelecao: (selecionar: boolean) => void;
  onLimparLocalSelecionado: () => void;
  onTransitoAtualizado: (itens: TransitoMapa[]) => void;
  onAcidentesAtualizados: (itens: AcidenteMapa[]) => void;
  onPontosCalorAtualizados: (itens: PontoCalor[]) => void;
};

type RespostaAgentes = { itens: AgenteOperacional[] };
type FormularioAcidente = {
  localDescricao: string;
  cidade: string;
  grau: AcidenteMapa['grau'];
  impactoTransito: AcidenteMapa['impactoTransito'];
  haFeridos: boolean;
};
const formularioInicial = (): FormularioAcidente => ({ localDescricao: '', cidade: 'Tupã', grau: 'leve', impactoTransito: 'nenhum', haFeridos: false });
const rotuloGrau: Record<AcidenteMapa['grau'], string> = { leve: 'Leve', moderado: 'Moderado', grave: 'Grave', critico: 'Crítico' };
const rotuloImpacto: Record<AcidenteMapa['impactoTransito'], string> = { nenhum: 'Sem impacto', lentidao: 'Lentidão', bloqueioParcial: 'Bloqueio parcial', interdicaoTotal: 'Interdição total' };
const acidentesEmAberto = (itens: AcidenteMapa[]) => itens.filter((acidente) => acidente.situacao !== 'encerrado');

export default function PainelOperacoes({ chamarApi, obterTokenCsrf, localSelecionado, selecionandoLocal, onSolicitarSelecao, onLimparLocalSelecionado, onTransitoAtualizado, onAcidentesAtualizados, onPontosCalorAtualizados }: Props) {
  const [transito, setTransito] = useState<TransitoMapa[]>([]);
  const [alertas, setAlertas] = useState<AlertaOperacional[]>([]);
  const [acidentes, setAcidentes] = useState<AcidenteMapa[]>([]);
  const [predicoes, setPredicoes] = useState<PredicoesOperacionais | null>(null);
  const [agentes, setAgentes] = useState<AgenteOperacional[]>([]);
  const [agentesPorAcidente, setAgentesPorAcidente] = useState<Record<string, string>>({});
  const [formulario, setFormulario] = useState<FormularioAcidente>(formularioInicial);
  const [formularioAberto, setFormularioAberto] = useState(false);
  const [carregando, setCarregando] = useState(false);
  const [salvandoAcidente, setSalvandoAcidente] = useState(false);
  const [erro, setErro] = useState('');
  const [mensagem, setMensagem] = useState('');

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const carregarAcidentes = (async () => {
        const resposta = await chamarApi('/api/operacoes/acidentes');
        const itens = await resposta.json() as AcidenteMapa[];
        setAcidentes(itens);
        onAcidentesAtualizados(acidentesEmAberto(itens));
      })();
      const [respostaTransito, respostaAlertas, respostaAgentes, respostaPredicoes] = await Promise.all([
        chamarApi('/api/operacoes/transito'),
        chamarApi('/api/operacoes/alertas'),
        chamarApi('/api/agentes?pagina=1&tamanhoPagina=100'),
        chamarApi('/api/operacoes/predicoes').catch(() => null),
        carregarAcidentes,
      ]);
      const [itensTransito, itensAlertas, listaAgentes, resultadoPredicoes] = await Promise.all([
        respostaTransito.json() as Promise<TransitoMapa[]>,
        respostaAlertas.json() as Promise<AlertaOperacional[]>,
        respostaAgentes.json() as Promise<RespostaAgentes>,
        respostaPredicoes ? respostaPredicoes.json() as Promise<PredicoesOperacionais> : Promise.resolve(null),
      ]);
      setTransito(itensTransito);
      setAlertas(itensAlertas);
      setAgentes(listaAgentes.itens);
      setPredicoes(resultadoPredicoes);
      onTransitoAtualizado(itensTransito);
      onPontosCalorAtualizados(resultadoPredicoes?.pontosCalor ?? []);
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível carregar os dados de operações.');
    } finally {
      setCarregando(false);
    }
  }, [chamarApi, onAcidentesAtualizados, onPontosCalorAtualizados, onTransitoAtualizado]);

  useEffect(() => { void carregar(); }, [carregar]);

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
    if (!formulario.localDescricao.trim() || !formulario.cidade.trim() || !localSelecionado) {
      setErro('Informe o local, a cidade e selecione o ponto do acidente no mapa.');
      return;
    }
    setSalvandoAcidente(true);
    try {
      const resposta = await enviarPost('/api/operacoes/acidentes', {
        ...formulario,
        latitude: localSelecionado.latitude,
        longitude: localSelecionado.longitude,
      });
      const acidenteCriado = await resposta.json() as AcidenteMapa;
      const acidentesAtualizados = [acidenteCriado, ...acidentes.filter((acidente) => acidente.id !== acidenteCriado.id)];
      setAcidentes(acidentesAtualizados);
      onAcidentesAtualizados(acidentesEmAberto(acidentesAtualizados));
      setFormulario(formularioInicial());
      setFormularioAberto(false);
      onSolicitarSelecao(false);
      onLimparLocalSelecionado();
      setMensagem('Ocorrência registrada no sistema. Nenhum serviço externo foi contatado.');
      void carregar();
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : 'Não foi possível registrar o acidente.');
    } finally {
      setSalvandoAcidente(false);
    }
  };

  const alternarFormularioAcidente = () => {
    const abrir = !formularioAberto;
    setFormularioAberto(abrir);
    setFormulario(formularioInicial());
    setErro('');
    onSolicitarSelecao(false);
    onLimparLocalSelecionado();
  };

  const solicitarPontoNoMapa = () => {
    const selecionar = !selecionandoLocal;
    onSolicitarSelecao(selecionar);
    if (selecionar) {
      window.requestAnimationFrame(() => document.getElementById('mapa-operacional')?.scrollIntoView({ behavior: 'smooth', block: 'center' }));
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
        <div><p className="eyebrow">CAMADA OPERACIONAL · TRÁFEGO E OCORRÊNCIAS</p><h3>Trânsito, previsões e alertas</h3><p>Acompanhe indicadores de circulação, projeções e ocorrências por região.</p></div>
        <button type="button" className="button button-quiet" onClick={() => void carregar()} disabled={carregando}>↻ Atualizar</button>
      </header>
      {erro && <p className="alert error operacoes-feedback" role="alert">{erro}</p>}
      {mensagem && <p className="operacoes-feedback operacoes-sucesso" role="status">{mensagem}</p>}
      <div className="operacoes-grade">
        <section className="operacoes-bloco operacoes-predicoes" aria-labelledby="predicoes-operacoes-title">
          <div className="operacoes-bloco-cabecalho"><div><span className="operacoes-kicker">PROMETHEUS · ANTECIPAÇÃO OPERACIONAL</span><h4 id="predicoes-operacoes-title">Inteligência Preditiva PROMETHEUS</h4></div><span className="origem-tag origin-simulado">ÍNDICE DE RISCO</span></div>
          <p className="predicoes-metodologia">{predicoes?.metodologia ?? (carregando ? 'Calculando indicadores…' : 'Sem dados preditivos disponíveis.')}</p>
          <div className="predicoes-grade">
            <section aria-labelledby="ranking-risco-title">
              <h5 id="ranking-risco-title">Ranking de risco por região</h5>
              {!predicoes?.indiceRiscoPorRegiao.length ? <p className="operacoes-vazio">Nenhuma região com dados suficientes.</p> : <ol className="predicoes-ranking">{predicoes.indiceRiscoPorRegiao.slice(0, 8).map((regiao) => (
                <li key={`${regiao.cidade}-${regiao.bairro}`}>
                  <div className="predicao-regiao"><strong>{regiao.bairro}, {regiao.cidade}</strong><span className={`predicao-classificacao predicao-${regiao.classificacao.toLocaleLowerCase('pt-BR').normalize('NFD').replace(/[\u0300-\u036f]/g, '')}`}>{regiao.classificacao}</span></div>
                  <div className="predicao-metrica"><span><i style={{ width: `${regiao.indiceRisco}%` }} /></span><b>{regiao.indiceRisco}/100</b></div>
                  <small>{regiao.congestionamentoPrevistoPercentual}% trânsito previsto · {regiao.equipamentosComFalhaOuManutencao}/{regiao.totalEquipamentos} equipamentos em atenção · {regiao.acidentesRecentes} acidente(s) recentes{regiao.dadosSimulados ? ' · contém dado simulado' : ''}</small>
                </li>
              ))}</ol>}
            </section>
            <section aria-labelledby="reforco-preventivo-title">
              <h5 id="reforco-preventivo-title">Recomendações preventivas de reforço</h5>
              {!predicoes?.recomendacoesReforco.length ? <p className="operacoes-vazio">Nenhuma recomendação de reforço pelos critérios atuais.</p> : <div className="predicoes-recomendacoes">{predicoes.recomendacoesReforco.slice(0, 6).map((recomendacao, indice) => (
                <article className="predicao-reforco" key={`${recomendacao.cidade}-${recomendacao.regiao}-${indice}`}>
                  <div><strong>{recomendacao.regiao} · {recomendacao.cidade}</strong><span>Índice {recomendacao.indiceRisco}/100 · {recomendacao.agentesSugeridos} agente(s) sugerido(s)</span></div>
                  <p>{recomendacao.justificativa}</p>
                  <small>{recomendacao.referencia} · próxima hora ({new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' }).format(new Date(recomendacao.inicioPrevisto))}–{new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' }).format(new Date(recomendacao.fimPrevisto))})</small>
                </article>
              ))}</div>}
            </section>
          </div>
          <p className="operacoes-nota">Indicador de apoio à priorização operacional. Recomendações devem ser avaliadas pela equipe responsável.</p>
        </section>

        <section className="operacoes-bloco" aria-labelledby="transito-operacoes-title">
          <div className="operacoes-bloco-cabecalho"><div><span className="operacoes-kicker">VOLUME E VELOCIDADE</span><h4 id="transito-operacoes-title">Trânsito por município</h4></div><span className="origem-tag origin-simulado">SIMULADO</span></div>
          {transito.length === 0 ? <p className="operacoes-vazio">{carregando ? 'Carregando histórico…' : 'Sem histórico disponível.'}</p> : <div className="transito-lista">{transito.map((item) => (
            <article className="transito-resumo" key={item.id}>
              <div><strong>{item.cidade}</strong><span>{item.veiculosUltimaHora} veículos/h · {item.velocidadeMediaKmh} km/h</span></div>
              <div className="transito-previsto"><b>{item.congestionamentoPercentual}% → {item.congestionamentoPrevistoPercentual}%</b><span>atual → próxima hora · nível {item.confianca}</span></div>
            </article>
          ))}</div>}
          <p className="operacoes-nota">Projeção calculada pela média histórica do mesmo dia da semana e horário local.</p>
        </section>

        <section className="operacoes-bloco" aria-labelledby="alertas-operacoes-title">
          <div className="operacoes-bloco-cabecalho"><div><span className="operacoes-kicker">MANUTENÇÃO · ANOMALIAS · PROJEÇÕES</span><h4 id="alertas-operacoes-title">Alertas ({alertas.length})</h4></div></div>
          {alertas.length === 0 ? <p className="operacoes-vazio">Nenhum alerta ativo pela regra atual.</p> : <div className="alertas-operacionais-lista">{alertas.map((alerta, indice) => (
            <article className={`alerta-operacional alerta-${alerta.severidade}`} key={`${alerta.tipo}-${alerta.recursoId ?? indice}`}>
              <span className="alerta-sinal" aria-hidden="true">!</span><div><strong>{alerta.titulo}</strong><p>{alerta.descricao}</p><small>{alerta.cidade} · origem {alerta.origemDados}</small></div>
            </article>
          ))}</div>}
          <p className="operacoes-nota">Alertas atualizados conforme os registros operacionais cadastrados.</p>
        </section>

        <section className="operacoes-bloco operacoes-acidentes" aria-labelledby="acidentes-operacoes-title">
          <div className="operacoes-bloco-cabecalho"><div><span className="operacoes-kicker">REGISTRO PERSISTENTE</span><h4 id="acidentes-operacoes-title">Acidentes ({acidentes.length})</h4></div><button type="button" className="button button-secondary" onClick={alternarFormularioAcidente}>{formularioAberto ? 'Cancelar' : '+ Registrar acidente'}</button></div>
          {formularioAberto && <form className="acidente-formulario" onSubmit={(evento) => void registrarAcidente(evento)}>
            <label>Local<input required maxLength={240} value={formulario.localDescricao} onChange={(evento) => setFormulario({ ...formulario, localDescricao: evento.target.value })} placeholder="Rua, cruzamento ou referência" /></label>
            <label>Cidade<input required maxLength={120} value={formulario.cidade} onChange={(evento) => setFormulario({ ...formulario, cidade: evento.target.value })} /></label>
            <button type="button" className={`button ${selecionandoLocal ? 'button-primary' : 'button-quiet'}`} onClick={solicitarPontoNoMapa}>{selecionandoLocal ? 'Clique no mapa para marcar o acidente…' : localSelecionado ? 'Alterar ponto no mapa' : 'Selecionar ponto no mapa'}</button>
            <div className="acidente-coordenadas" role="status" aria-live="polite">{localSelecionado ? <><strong>Ponto selecionado</strong><span>Latitude {localSelecionado.latitude.toFixed(6)} · Longitude {localSelecionado.longitude.toFixed(6)}</span></> : <span>As coordenadas serão preenchidas ao clicar no local do acidente no mapa.</span>}</div>
            <div className="acidente-form-linha"><label>Grau<select value={formulario.grau} onChange={(evento) => setFormulario({ ...formulario, grau: evento.target.value as FormularioAcidente['grau'] })}>{Object.entries(rotuloGrau).map(([valor, rotulo]) => <option key={valor} value={valor}>{rotulo}</option>)}</select></label><label>Impacto no trânsito<select value={formulario.impactoTransito} onChange={(evento) => setFormulario({ ...formulario, impactoTransito: evento.target.value as FormularioAcidente['impactoTransito'] })}>{Object.entries(rotuloImpacto).map(([valor, rotulo]) => <option key={valor} value={valor}>{rotulo}</option>)}</select></label></div>
            <label className="acidente-feridos"><input type="checkbox" checked={formulario.haFeridos} onChange={(evento) => setFormulario({ ...formulario, haFeridos: evento.target.checked })} /> Há feridos informados</label>
            <button type="submit" className="button button-primary" disabled={salvandoAcidente || !localSelecionado}>{salvandoAcidente ? 'Salvando…' : 'Salvar ocorrência'}</button>
          </form>}
          {acidentes.length === 0 ? <p className="operacoes-vazio">Nenhum acidente registrado.</p> : <div className="acidentes-lista">{acidentes.map((acidente) => (
            <article className="acidente-item" key={acidente.id}>
              <div className="acidente-item-titulo"><strong>{acidente.localDescricao}</strong><span className={`acidente-grau acidente-grau-${acidente.grau}`}>{rotuloGrau[acidente.grau]}</span></div>
              <p>{acidente.cidade} · {rotuloImpacto[acidente.impactoTransito]} · {acidente.haFeridos ? 'feridos informados' : 'sem feridos informados'} · {acidente.situacao}</p>
              <label className="acidente-agente-select">Agente<select value={agentesPorAcidente[acidente.id] ?? acidente.agenteDesignadoId ?? ''} onChange={(evento) => setAgentesPorAcidente({ ...agentesPorAcidente, [acidente.id]: evento.target.value })}><option value="">Sem designação</option>{agentes.map((agente) => <option key={agente.id} value={agente.id}>{agente.nome} · {agente.situacao}</option>)}</select></label>
              <div className="acidente-acoes">
                <button type="button" className="button button-secondary" disabled={Boolean(acidente.despachoSolicitadoEm) || acidente.situacao === 'encerrado'} onClick={() => void (async () => { try { await enviarPost(`/api/operacoes/acidentes/${acidente.id}/despachar`, { agenteId: agentesPorAcidente[acidente.id] || null }); setMensagem('Solicitação registrada no F.O.G.O. · integração externa não configurada.'); await carregar(); } catch (falha) { setErro(falha instanceof Error ? falha.message : 'Não foi possível registrar o despacho.'); } })()}>{acidente.despachoSolicitadoEm ? 'Despacho registrado' : 'Registrar envio de agente'}</button>
                <button type="button" className="button button-quiet" disabled={Boolean(acidente.socorroSolicitadoEm) || acidente.situacao === 'encerrado'} onClick={() => void executarAcao(`/api/operacoes/acidentes/${acidente.id}/acionar-socorro`, 'Solicitação registrada no F.O.G.O. · integração externa não configurada.')}>{acidente.socorroSolicitadoEm ? 'Socorro registrado' : 'Registrar acionamento de socorro'}</button>
                {acidente.situacao !== 'encerrado' && <button type="button" className="button button-quiet" onClick={() => void executarAcao(`/api/operacoes/acidentes/${acidente.id}/encerrar`, 'Ocorrência encerrada no sistema.')}>Encerrar</button>}
              </div>
            </article>
          ))}</div>}
          <p className="operacoes-nota">As solicitações ficam registradas no histórico do F.O.G.O. · Integração com serviços externos não configurada.</p>
        </section>
      </div>
    </section>
  );
}
