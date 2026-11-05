# Aviso e fechamento coordenado — implementados

**Aceite de uso em 05/11/2026:** o responsável confirmou “fluxo validado”. Aviso e fechamento coordenado validados no uso. A confirmação não especifica ambiente e não equivale a aceite estético ou revisão formal do diff. Nenhuma mudança de código ou nova execução de testes nesta atualização; resultados abaixo são históricos da implementação.

Data: 04/11/2026. Entrega para revisão humana e validação de uso; Agenda completa ainda em desenvolvimento.

Diálogo modal de seleção, Cancelar seguro, X como cancelamento e três escolhas integrados ao App e MainWindow. Coordenador resolve os dois rascunhos antes da conclusão, usa relógio local após decisão e mantém editor/pausa de autosave até o fechamento real. Library.PrepareCloseAsync preserva o editor; RequestCloseAsync antigo mantém a semântica. OnClosed descarta editor antes do coordenador. Nenhuma reconfirmação interna ocorre depois de gravar conclusões.

35 casos aprovados preservados e passando. Suíte completa: **738 aprovados — 117 unitários e 621 integrações**, zero falhas/ignorados, build Release sem avisos/erros e formatação aprovados. Depois de ajustar apenas o singular/plural do contador, os 35 casos e a formatação passaram novamente; sem nova rodada completa após esse texto.

QA SQLite/Skia real em perfil fictício: candidatas, seleção, cancelamento, scroll, janela compacta, prompts de rascunho, autosave suspenso além de dois segundos e fechamento que conclui só a seleção sem persistir descarte. Capturas finais inspecionadas. Uma tentativa de captura teve erro de XAML por builds Avalonia concorrentes; reconstrução sequencial corrigiu o artefato, sem mudança funcional. Evitar compilar o mesmo projeto por dois fluxos em paralelo.

Windows self-contained isolado gerado com fontes conferidos e perfil fictício fechado, sem sobrescrever artifacts/publish/windows. Validação interativa desse executável não realizada pelo agente. Sem nova migração/dependência/formato, teste antigo ou dado pessoal alterado; Git, README e manual/PDF preservados. Sem novos logs, chamadas externas ou segredo literal no diff; inspeção limitada aos arquivos envolvidos.

## Limites e revisão

- Aceite visual, execução Windows e revisão humana do diff pendentes.
- Saída normal coberta; crash/término forçado não concluem tarefas automaticamente.
- Um impedimento externo ao fechamento depois do commit não desfaz uma conclusão já confirmada; as decisões internas canceláveis precedem a gravação.
- Último encerramento fica separado do planejamento, sem histórico completo nem duração trabalhada.
- Consulta inicial e lista de candidatas são materializadas em memória, sem benchmark de grande volume.

Esforço moderado, sem novos subagentes: duas rodadas focadas dos 35 casos, uma completa e QA visual dirigido, com recuperação sequencial da captura. Reutilizar esse protocolo e fixtures na próxima etapa, e manter builds sequenciais, reduz uso e retrabalho. Tokens/custo exato não foram medidos.

[Estudo](../estudos/71-fechamento-coordenado-implementado.md) · [Histórico RED](077-agenda-close-workflow-red.md).
