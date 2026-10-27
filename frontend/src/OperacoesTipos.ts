export type OrigemOperacional = 'manual' | 'importado' | 'simulado';

export type TransitoMapa = {
  id: string;
  nome: string;
  cidade: string;
  referenciaMapa: string;
  latitude: number;
  longitude: number;
  veiculosUltimaHora: number;
  velocidadeMediaKmh: number;
  congestionamentoPercentual: number;
  amostraEm: string;
  origemDados: OrigemOperacional;
  veiculosPrevistosProximaHora: number;
  velocidadePrevistaKmh: number;
  congestionamentoPrevistoPercentual: number;
  amostrasHistoricas: number;
  confianca: 'media' | 'baixa' | 'muitoBaixa';
  horizontePrevisto: string;
};

export type AcidenteMapa = {
  id: string;
  localDescricao: string;
  cidade: string;
  latitude: number;
  longitude: number;
  grau: 'leve' | 'moderado' | 'grave' | 'critico';
  impactoTransito: 'nenhum' | 'lentidao' | 'bloqueioParcial' | 'interdicaoTotal';
  haFeridos: boolean;
  situacao: 'aberto' | 'atendimentoSolicitado' | 'encerrado';
  registradoEm: string;
  despachoSolicitadoEm: string | null;
  agenteDesignadoId: string | null;
  socorroSolicitadoEm: string | null;
};

export type AlertaOperacional = {
  tipo: string;
  severidade: 'critica' | 'alta' | 'media';
  titulo: string;
  descricao: string;
  recursoId: string | null;
  cidade: string;
  latitude: number | null;
  longitude: number | null;
  origemDados: OrigemOperacional;
  geradoEm: string;
};

export type AgenteOperacional = { id: string; nome: string; situacao: string };
