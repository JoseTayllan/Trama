# Correção da composição visual para revisão

30/10/2026. A entrega anterior foi considerada distante da referência pelo responsável. Esta entrega corrige a apresentação das funcionalidades já aprovadas. Não adiciona regras de domínio nem altera os testes aprovados. Revisão humana do diff e do visual pendentes.

## Resultado

Acervo e busca na lateral, criação/filtros recolhíveis, hierarquia com recuos, contexto e Salvar na mesma faixa, barra plana com histórico e escrita em superfície contínua com margem numerada. Backup acessível no rodapé lateral; editor sem biblioteca conserva o salvamento próprio. Todos os controles essenciais continuam acessíveis por teclado.

## Evidências

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

Restore travado, build Release sem avisos/erros e formatação aprovados. Resultado final: **414 testes aprovados** (77 unitários e 337 de integração), zero falhas/ignorados. Os 15 testes de identidade aprovados e os 399 anteriores permanecem intactos.

Conferência SQLite/Skia: cabeçalho e título em edição, renomeação sem recriar prosa, limites/tooltips em largura compacta, expansão dos controles de criação/filtros, seleção/foco em popups, botões e atalhos de histórico, autosave/reabertura, imagem, altura de texto e cores dinâmicas. Capturas ampla, compacta, anexo e nomes longos inspecionadas. Contraste dos textos da paleta: mínimo 5,882:1 em 42 combinações avaliadas; não é uma auditoria completa de acessibilidade.

Publicação Windows self-contained em cópia separada dos fontes e perfil sintético preparado por backup SQLite. Smoke nativo: título Trama, Responding=true e fechamento normal. Publicação habitual e banco pessoal preservados. Nenhuma configuração de segurança foi alterada.

Artefatos externos na pasta `trama-reference-alignment`: `implementacao.diff`, `audit.json`, resultados TRX, `contrast.json`, capturas em `qa/`, `native-smoke-final.json` e `Testar-Referencia.cmd`, que abre o executável com perfil fictício separado.

## Revisão e limites

Revisão focada em comandos reaproveitados, contexto vivo, assinatura/cancelamento de eventos, compatibilidade do editor independente, preservação dos testes e ausência de mudanças em contratos, migrações e pacotes. A primeira captura revelou caixas nativas nos expansores, seletores truncados e fallback inadequado de fonte do código no Linux; esses pontos foram ajustados e conferidos novamente.

Riscos remanescentes: aparência em outros DPI/fontes, descoberta de ações recolhidas e conforto em uso prolongado precisam de avaliação humana. A lateral conserva as três listas filtradas existentes; não implementa uma árvore global expansível. O clipboard do WSL continua fora do escopo desta correção. Manual/PDF intactos.

Esforço moderado: um agente, sem subagentes; duas execuções completas da suíte por alterações finais e três conferências visuais bem-sucedidas, além de um ajuste de compilação no harness externo. Uma captura adicional usa o mesmo assunto e conteúdo de exemplo da referência, com ajuste posterior de foco para mostrar o estado de escrita. Sem medição de tokens ou custo monetário. Economia para a próxima etapa: concentrar o feedback nesta captura e reutilizar o harness, sem reexplorar modelo e persistência.
