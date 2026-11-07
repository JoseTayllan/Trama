# Tela semanal — especificação RED

Data: 07/11/2026. Aguardar aprovação explícita conforme CODEX.md §9.

Somente AgendaWeekUiTests.cs adicionado. Sem alteração do produto, testes antigos, migrações, locks ou dependências. A interface semanal está ausente; testes acessam controles por nome/classes e falham em asserções explícitas, preservando compilação e runner funcionais.

32 casos propostos: sete dias, navegação, Hoje pelo relógio local injetado, filtros, continuidade, rascunhos, salvar e exclusão visível, estados com símbolo/rótulo/categoria, paleta dinâmica, erros/carregamento, limites do calendário, acessibilidade e geometria compacta. Duas proteções verificam construtor diário sem semana e HH:mm sem TimePicker.

Revisão da especificação: usa SQLite real temporário, prompt nativo e controles Avalonia headless. Esperas por Stopwatch, sem perfis pessoais ou horário da máquina como dado esperado. A coleção de cartões não é contagem de tarefas; casos protegem a contagem única e horários de cada dia. Navegação e mudança de modo não recriam o formulário.

Riscos para revisão: compactação permite rolagem vertical para alcançar ações e formulário; dia selecionado do formulário permanece independente da semana navegada. Hoje usa o relógio substituível, não UTC arbitrário. Glifos, contraste e estética precisam de capturas além destes testes; nenhuma imagem nova foi gerada nesta fatia.

Inspeção não encontrou segredo literal ou logging novo. Esforço moderado, sem subagentes: uma rodada focada e uma regressão completa. Reutilizar a integração e o formulário reduz custo; não adicionar regras de negócio ao code-behind. Tokens/custo exatos não medidos.

**Verificação:** 30 falhas esperadas por controles ausentes e duas proteções passando; 982 anteriores aprovados (167 unitários e 815 integrações), sem falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Hashes confirmam todos os arquivos anteriores de src/tests intactos. A suíte incluindo a classe nova permanece em RED.

[Estudo](../estudos/91-tela-semanal-especificacao.md). Evidências/diffs: trama-agenda-week-ui-red. Revisão dos testes pendente.
