import { KeyboardEvent, useId, useMemo, useState } from 'react';
import './SeletorTipoEquipamento.css';

type Props = {
  value: string;
  options: string[];
  onChange: (value: string, confirmado: boolean) => void;
};

type OpcaoTipo = { valor: string; rotulo: string; novo: boolean };

const normalizar = (valor: string) => valor.trim().toLocaleLowerCase('pt-BR');

export default function SeletorTipoEquipamento({ value, options, onChange }: Props) {
  const id = useId();
  const [aberto, setAberto] = useState(false);
  const [indiceAtivo, setIndiceAtivo] = useState(0);
  const opcoes = useMemo<OpcaoTipo[]>(() => {
    const consulta = normalizar(value);
    const existentes = options
      .filter((tipo) => !consulta || normalizar(tipo).includes(consulta))
      .map((tipo) => ({ valor: tipo, rotulo: tipo, novo: false }));
    const tipoNovo = value.trim();
    const jaExiste = options.some((tipo) => normalizar(tipo) === normalizar(tipoNovo));
    return tipoNovo && !jaExiste
      ? [...existentes, { valor: tipoNovo, rotulo: `Adicionar “${tipoNovo}”`, novo: true }]
      : existentes;
  }, [options, value]);

  const selecionar = (opcao: OpcaoTipo) => {
    onChange(opcao.valor, true);
    setAberto(false);
  };

  const tratarTecla = (evento: KeyboardEvent<HTMLInputElement>) => {
    if (evento.key === 'Escape') {
      setAberto(false);
      return;
    }
    if (evento.key === 'ArrowDown' || evento.key === 'ArrowUp') {
      evento.preventDefault();
      setAberto(true);
      if (opcoes.length > 0) {
        setIndiceAtivo((atual) => evento.key === 'ArrowDown'
          ? (atual + 1) % opcoes.length
          : (atual - 1 + opcoes.length) % opcoes.length);
      }
      return;
    }
    if (evento.key === 'Enter' && aberto && opcoes.length > 0) {
      evento.preventDefault();
      selecionar(opcoes[indiceAtivo] ?? opcoes[0]);
    }
  };

  return (
    <div className="seletor-tipo-equipamento">
      <input
        id={`${id}-input`}
        role="combobox"
        aria-autocomplete="list"
        aria-expanded={aberto}
        aria-controls={`${id}-listbox`}
        aria-activedescendant={aberto && opcoes.length ? `${id}-opcao-${indiceAtivo}` : undefined}
        autoComplete="off"
        required
        maxLength={60}
        placeholder="Pesquise ou digite um novo tipo"
        value={value}
        onFocus={() => setAberto(true)}
        onBlur={() => setAberto(false)}
        onKeyDown={tratarTecla}
        onChange={(evento) => {
          onChange(evento.target.value, false);
          setIndiceAtivo(0);
          setAberto(true);
        }}
      />
      {aberto && <ul className="seletor-tipo-opcoes" id={`${id}-listbox`} role="listbox" aria-label="Tipos de equipamento">
        {opcoes.map((opcao, indice) => (
          <li
            id={`${id}-opcao-${indice}`}
            key={`${opcao.novo ? 'novo' : 'existente'}-${opcao.valor}`}
            role="option"
            aria-selected={indice === indiceAtivo}
          >
            <button
              type="button"
              className={indice === indiceAtivo ? 'tipo-opcao-ativa' : ''}
              onMouseDown={(evento) => evento.preventDefault()}
              onClick={() => selecionar(opcao)}
            >
              <span>{opcao.rotulo}</span>
              <small>{opcao.novo ? 'será incluído ao salvar' : 'cadastrado'}</small>
            </button>
          </li>
        ))}
        {opcoes.length === 0 && <li className="seletor-tipo-vazio" role="presentation">Nenhum tipo cadastrado. Digite para adicionar.</li>}
      </ul>}
    </div>
  );
}
