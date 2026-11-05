# Agenda — campos de datas e horários implementados

Data: 05/11/2026. Para revisão do diff e aceite visual.

Aceite posterior em 05/11: o responsável confirmou “validado” para os campos de datas e horários. Validação no uso registrada; ambiente não informado, sem inferir validação específica no Windows ou revisão formal do diff. Os limites abaixo descrevem a entrega anterior ao aceite. Atualização somente documental; sem novos testes ou publicação.

Formulário recebe dia final opcional e horários digitáveis HH:mm, sem TimePicker visível. Rascunho inclui data final e texto parcial; carga/save/limpeza e propriedades de horário anteriores permanecem compatíveis. Lista contextualiza intervalos entre dias. Diálogo mantém Agora e permite instante informado por calendário e texto, preservando planejamento e ações de saída.

33 casos aprovados intactos; duas linhas antigas de navegação ajustadas como autorizadas. Ajuste adicional de um seletor no helper de três testes do diálogo aprovado pelo responsável. Aplicado usando Assert.Single(collection, predicate), exigido pelo analisador, com o mesmo filtro completion-task e a mesma exigência de unicidade. Todas as outras verificações preservadas.

Verificação WSL: **844 aprovados (131 unitários, 713 integrações)**; build Release sem avisos/erros e formatação aprovados. QA SQLite/Skia externo passou, com perfil fictício e capturas ampla/compacta, digitação, rejeição de entrada parcial, planejamento persistido e conclusão real informada. Primeira rodada 32/33 apontou largura mínima do calendário, corrigida; primeira suíte 841/844 apontou o seletor antigo, ajustado após aprovação. Os testes não foram enfraquecidos para obter GREEN.

Revisão do código: texto parcial integra baseline, evita conversão silenciosa para null e bloqueia Save. Pausa de sincronização evita ciclos entre texto e TimeSpan; carregamento explícito limpa texto inválido anterior. Snapshot preserva data final; tratamento de erro e edição mais recente existente é mantido. Cancelar/Manter abertas ignoram entrada manual inválida; validação temporal do coordenador permanece. Inspeção limitada não encontrou segredo literal ou logging novo.

Limites: aceite estético e uso nativo Windows pendentes; sem novo executável publicado. Calendários continuam com seu seletor nativo; horários são texto HH:mm. Consulta diária continua pelo dia inicial, sem expansão semanal. Confirmação/tela da lixeira ainda pendentes. Sem nova migração, dependência, perfil pessoal, Git ou manual/PDF.

Esforço moderado, sem novos subagentes: duas rodadas focadas, três suítes e duas conferências SQLite/Skia; a última repetição conferiu os rótulos encurtados após a inspeção das capturas. Houve uma compilação intermediária recusada pelo analisador até usar o overload apropriado. Reutilizar o parser e os fixtures na próxima etapa reduz custo. Tokens/custo exato não medidos. Revisão humana do diff pendente.

[Estudo](../estudos/79-campos-de-datas-e-horarios-implementados.md) · [RED](085-agenda-date-controls-red.md).
