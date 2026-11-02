# Identidade e hierarquia: RED para revisão

30/10/2026. Continuação autorizada após escrita rica. Novo arquivo: [NotebookIdentityTests.cs](../../tests/Trama.IntegrationTests/Knowledge/NotebookIdentityTests.cs), com 15 casos. Produção, contratos, pacotes, migrações, testes anteriores e manual/PDF permanecem intactos.

## Especificação

Contexto explícito Espaço → Caderno → Página com rótulos e valores reais, título em edição/sem título, renomeações, mudança de espaço com cadernos homônimos, outra página, não organizadas, espaço sem caderno, navegação cancelada/página indisponível e janela compacta com nomes longos/tooltips. Proteções atuais de seleção por identidade e histórico ao redimensionar. Ver [plano e critérios](../estudos/32-identidade-e-hierarquia-especificacao.md).

Os casos usam LibraryWorkspaceView e composição real com SQLite temporário. Autosave fica desativado na fixture para distinguir atualização visual de gravação indevida. Não foram criados métodos de produção vazios ou usados erros de compilação como RED. O contexto é informativo; não muda as regras de navegação existentes.

## Execução final

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

| Grupo | Passaram | Falharam | Total |
| --- | --- | --- | --- |
| Unitários anteriores | 77 | 0 | 77 |
| Integração anterior | 322 | 0 | 322 |
| Novos de hierarquia | 2 | 13 | 15 |
| Total | 401 | 13 | 414 |

Restore travado, build Release com zero avisos/erros e formatação aprovados. Nenhum ignorado. As 13 falhas comportamentais são a ausência dos TextBlocks do contexto; as duas proteções existentes passam. Antes dessa execução, foram corrigidos o namespace do Avalonia para medição e a formatação de um inicializador no arquivo novo. Tentativas interrompidas por compilação/formatação não foram consideradas RED.

Artefatos externos da sessão: `trama-notebook-identity-red/testes-e-plano.diff`, `audit.json`, `expected-failures.json`, `unit.trx` e `integration.trx`. Comparação por hashes verifica fontes, testes anteriores, versões/locks e manual/PDF. Sem acesso ao perfil pessoal ou nova publicação.

## Próximo passo e limites

Revisar os 15 casos antes da implementação, conforme seção 9 do CODEX. Após aprovação, derivar contexto com notificações corretas, aplicar composição própria e manter os casos congelados. Não substituir o editor ao renomear pais ou redimensionar; manter caminho do conteúdo realmente aberto quando houver cancelamento/falha.

Os testes protegem comportamento e limites do contexto, sem validar sozinhos a identidade visual completa. O GREEN deve incluir conferência visual ampla/compacta com texto, código e imagens, teclado e captura final. Não há novo formato de dados ou mudança de logo/stack nesta especificação.

Esforço moderado: um agente, sem subagentes; preparação/revisão, dois ajustes preliminares e uma execução completa final. Sem medição de tokens/custo. Economia no GREEN: reutilizar fixture e harness existentes, manter armazenamento/editor estabilizados e começar pelos casos focados antes da suíte final.
