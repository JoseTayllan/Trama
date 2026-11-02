# Controles da escrita rica: RED para revisão

30/10/2026. Continuação autorizada para preparar os testes da próxima fatia. Novo arquivo: [RichTextEditorTests.cs](../../tests/Trama.IntegrationTests/Knowledge/RichTextEditorTests.cs), com 25 casos. Código de produção, contratos, pacotes, migrações e os testes anteriores permanecem intactos; manual/PDF sem alterações.

## Especificação

Uma barra compartilhada para tamanho/cor/destaque e limpar, atuando na seleção do bloco de prosa ativo. Casos de remoção individual, estados mistos, sincronização sem editar, seleção vazia, valor idêntico, foco no código, troca de bloco/página, teclado, histórico, salvar/autosave e renderização. Ver [especificação completa](../estudos/30-controles-de-formatacao-especificacao.md).

Os casos não dependem de APIs futuras de produção. Usam a view real, controles identificados por nome, opções com Tag estável, comandos existentes, pilha nativa e trechos visuais do AvaloniaEdit. As cores do teste são recursos sintéticos injetados na janela, sem exigir hexadecimal na View do produto. As duas proteções atuais de histórico de atributos também são exercitadas.

## Execução final

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

| Grupo | Passaram | Falharam | Total |
| --- | --- | --- | --- |
| Unitários anteriores | 77 | 0 | 77 |
| Integração anterior | 297 | 0 | 297 |
| Novos do editor | 2 | 23 | 25 |
| Total | 376 | 23 | 399 |

Restore travado e build Release aprovados, sem avisos/erros; formatação aprovada. Nenhum teste ignorado. Retorno 1 devido aos casos RED esperados. Controles ausentes explicam os casos de interação; os três casos de apresentação observaram tamanho 16 em vez de 24, primeiro plano padrão em vez do brush configurado e fundo nulo. Não houve NullReferenceException nem falha de montagem visual no resultado final.

Antes da execução final, foram corrigidos somente o uso do comando público de salvar no teste novo e a formatação de um inicializador. As tentativas interrompidas por compilação/formatação não foram consideradas RED. Houve uma execução completa bem-sucedida da infraestrutura, com as 23 falhas comportamentais acima; não houve repetição após esse resultado.

## Revisão e sequência

Artefatos externos: `trama-rich-editor-red/testes-e-plano.diff`, `audit.json`, `expected-failures.json`, `unit.trx` e `integration.trx`. Comparação por hashes preserva todos os fontes de produção, testes anteriores e manual/PDF. Sem leitura do banco pessoal ou alteração de segurança.

Aguardar aprovação explícita dos 25 casos, conforme seção 9 do CODEX. Depois implementar atributos/limpeza nas ações, barra compartilhada, histórico e renderer, mantendo os testes aprovados intactos. Os testes não substituem conferência visual de contraste, DPI, rolagem e altura de linhas; essa conferência acompanha o GREEN. A hierarquia Espaço → Caderno → Páginas permanece requisito da composição visual seguinte.

Esforço moderado: um agente, sem subagentes; preparação/revisão, dois ajustes preliminares e uma execução completa final de build/format/testes. Sem medição de tokens ou custo monetário. Economia na implementação: usar primeiro a classe focada e ampliar para a suíte completa apenas após o GREEN, reaproveitando fixtures e mapa existentes.
