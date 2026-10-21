import './App.css';

const fases = [
  {
    numero: '01',
    titulo: 'Inventário inteligente',
    descricao: 'Equipamentos, agentes e dados geográficos em uma base confiável.',
    estado: 'A preparar',
  },
  {
    numero: '02',
    titulo: 'Central de operações',
    descricao: 'Mapa operacional, disponibilidade e mudanças observadas.',
    estado: 'Planejada',
  },
  {
    numero: '03',
    titulo: 'Cidade preditiva',
    descricao: 'Antecipação explicável, condicionada aos dados disponíveis.',
    estado: 'Planejada',
  },
];

export default function App() {
  return (
    <main className="shell">
      <header className="topbar">
        <a className="brand" href="#inicio" aria-label="F.O.G.O. início">
          <span className="brand-mark" aria-hidden="true">F</span>
          <span>F.O.G.O.<small>GESTÃO E OPERAÇÕES</small></span>
        </a>
        <span className="system-status"><i /> AMBIENTE DE DESENVOLVIMENTO</span>
      </header>

      <section className="hero" id="inicio">
        <div className="hero-copy">
          <p className="eyebrow"><span>MISSÃO 01</span> · MOBILIDADE URBANA</p>
          <h1>Uma cidade que<br /><em>pensa à frente.</em></h1>
          <p className="intro">
            Uma visão integrada começa com dados confiáveis. O F.O.G.O. está sendo
            preparado para conectar recursos, operação e decisões — sem confundir
            demonstração com realidade.
          </p>
          <div className="hero-note"><span>↳</span> Nenhum equipamento ou evento foi carregado nesta etapa.</div>
        </div>

        <div className="radar" aria-label="Visual ilustrativo, sem dados operacionais">
          <div className="radar-ring ring-one" />
          <div className="radar-ring ring-two" />
          <div className="radar-ring ring-three" />
          <div className="radar-cross horizontal" />
          <div className="radar-cross vertical" />
          <div className="radar-sweep" />
          <span className="radar-center" />
          <span className="radar-label label-top">VISÃO OPERACIONAL</span>
          <span className="radar-label label-bottom">AGUARDANDO DADOS</span>
          <span className="radar-coord">COORDENADAS<br />NÃO CONFIGURADAS</span>
          <span className="radar-range">SEM REGISTROS<br />PARA EXIBIR</span>
        </div>
      </section>

      <section className="mission" aria-labelledby="mission-title">
        <div className="section-heading">
          <div><p className="eyebrow">ROTEIRO DE DESENVOLVIMENTO</p><h2 id="mission-title">Três frentes. Uma visão.</h2></div>
          <span className="section-index">F.O.G.O. / 2026</span>
        </div>
        <div className="phase-grid">
          {fases.map((fase) => (
            <article className="phase-card" key={fase.numero}>
              <div className="card-top"><span className="phase-number">{fase.numero}</span><span className="phase-state">{fase.estado}</span></div>
              <h3>{fase.titulo}</h3>
              <p>{fase.descricao}</p>
              <div className="card-line" />
            </article>
          ))}
        </div>
      </section>

      <footer className="footer">
        <span>F.O.G.O. <span className="muted">/</span> FERRAMENTA ONISCIENTE DE GESTÃO E OPERAÇÕES</span>
        <span className="footer-right"><i /> DADOS OPERACIONAIS AINDA NÃO CONECTADOS</span>
      </footer>
    </main>
  );
}