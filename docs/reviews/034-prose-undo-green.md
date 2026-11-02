# Desfazer/refazer enriquecido GREEN - 29/10/2026

## Resultado para revisão

Treze casos aprovados explicitamente pelo responsável e preservados. Implementação concentrada em NotebookProseEditor: texto e estilos passam a integrar a mesma pilha nativa de histórico. Build Release sem avisos/erros, formatação e **325/325 testes** aprovados (49 unitários, 276 de integração).

Revisão humana do diff e uso interativo do novo pacote Windows continuam pendentes. O manual/PDF e seu gerador foram preservados conforme pedido; somente documentação técnica foi atualizada.

## Implementação

- Operação de formatação armazena intervalos antes/depois e restaura seleção. Os intervalos são copiados; o conteúdo original não é alterado por compartilhar uma lista mutável.
- Operação opcional de metadados acompanha o grupo nativo de texto. Replay prepara os estilos para o TextChanged final sem registrar outra edição normal.
- Replay também é consumido quando o grupo termina com a mesma string, mas o estado de formatação muda.
- Troca de bloco/página limpa o histórico; operações verificam a identidade do bloco vinculado.
- Ctrl+Z, Ctrl+Y e Ctrl+Shift+Z tratam texto/estilos pelo mesmo caminho e respeitam CanEditContent.
- Limite de 200 grupos nativos por bloco de prosa. Não é limite de memória por bytes.

Sem novas dependências de biblioteca, mudanças em ViewModels, banco, migrações ou formatos de backup. A integração com autosave usa as notificações de edição existentes.

## Verificações

1. Os treze casos passaram na rodada focada, sem alterar a especificação.
2. Revisão identificou o caso de grupo com texto final igual e metadados diferentes; a aplicação do replay deixou de depender apenas da diferença de strings.
3. Suíte completa depois desse ajuste passou os 325 casos, com build e formatação aprovados.
4. Harness externo Headless/Skia com SQLite isolado confirmou atalhos, seleção, exclusão, replay do grupo com string igual, autosave do estado desfeito/refeito e reabertura com estilos preservados. Página nova e página reaberta não herdaram a pilha anterior.
5. Publicação Windows Release win-x64 self-contained preparada em pasta isolada, sem substituir o pacote anterior nem apontar para o banco pessoal. O lançador Testar-Historico.cmd usa windows-test-profile.
   A inicialização nativa apresentou janela Trama respondendo e criou banco de teste; não equivale à revisão humana dos atalhos nessa publicação.
6. Auditoria compara os testes aprovados, fontes do pacote publicado, migrações, locks e hashes do manual com o snapshot anterior. Relatório e diff ficam na pasta externa trama-undo-green dos artefatos locais do Codex.

## Limites e riscos

Histórico de sessão por bloco de prosa; não é versionamento persistente. Operações estruturais de blocos ficam fora deste recorte. Históricos de título/código permanecem nativos. O limite por grupos não garante consumo máximo fixo de memória. Duas instâncias editando o mesmo perfil continuam sem controle de conflito. Assinatura digital e teste independente do colega continuam pendentes. O defeito de clipboard WSL não foi retomado.

O harness automatizado não substitui validação humana de navegação, seleção e atalhos na máquina do usuário. O pacote deve ser revisado antes de distribuição como versão aprovada.

## Esforço

Um agente, sem delegação. Esforço moderado: consulta focada do contrato da pilha nativa, um ciclo de implementação, revisão do replay agrupado, suíte completa e harness SQLite/Skia. Sem medição de tokens ou custo monetário. Economia: reutilizar os treze casos e o harness em mudanças futuras; evitar repetir exploração do editor e executar a suíte ampla somente na entrega ou diante de novos riscos.

Veja o [estudo da implementação](../estudos/25-desfazer-refazer-implementado.md).
