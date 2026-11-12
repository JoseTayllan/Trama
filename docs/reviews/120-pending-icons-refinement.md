# Pendências — refino dos ícones

Data: 10/11/2026. Solicitação: melhorar visualmente os ícones das pendências.

Alteração concentrada no template de AgendaRetrospectivePanelView.axaml: ícone vetorial por categoria, símbolos de estado maiores, alinhamento vertical e colunas consistentes com divisor. Título, categoria, estado, bindings e dados capturados preservados. Nenhuma regra, teste, migração ou dependência nova.

Revisão: recursos vetoriais e semânticos reaproveitados, sem cor fixa ou imagem nova; rótulos textuais continuam junto aos símbolos. Título com quebra de linha preservada para a janela compacta. Sem segredo literal, logging, acesso ao perfil pessoal ou edição do manual/PDF. Sem Git/publicação.

QA usa o harness SQLite/Skia existente em perfil fictício isolado. Capturas da parte inferior ampla e da janela compacta são inspecionadas; aceite estético e execução nativa Windows pendentes.

Esforço pequeno, sem subagentes: um template alterado, uma suíte e capturas reutilizadas. Sem testes novos que espelhem estilos; tokens/custo exatos não medidos.

[Estudo](../estudos/113-icones-das-pendencias.md). Evidências: trama-pending-icons-refinement.

Verificação final: 1.236 aprovados (199 unitários e 1.037 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Hashes confirmam todos os testes e arquivos anteriores de src intactos, exceto o único XAML declarado. Capturas inferior ampla e compacta inspecionadas: ícones e estados legíveis, alinhados e sem corte.
