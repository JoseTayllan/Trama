# Lixeira dos três níveis: RED para revisão

31/10/2026. O responsável pediu incluir espaços e seguir. Esta fatia contém apenas `LibraryTrashWorkflowTests.cs` e documentos. Nenhum contrato, migração ou código de exclusão foi introduzido; banco pessoal e instância WSL de uso preservados.

## Resultado

**18 casos descobertos: 16 falhas esperadas e duas proteções aprovadas.** Os 426 anteriores passam sem alterações: 77 unitários e 349 de integração. Resultado completo nesta etapa: 444 casos, 428 aprovados, 16 falhas, zero ignorados. Build Release sem avisos/erros e formatação aprovados. Um ajuste de formatação no novo arquivo foi feito antes da execução comportamental; não foi contado como RED.

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

Os 16 casos param na ausência das ações visíveis de enviar à lixeira. Não há falta de tipos/serviços para compilar, erro de banco ou código de produto criado como stub. As verificações posteriores de restauração, filtragem e integridade estão escritas, mas só serão alcançadas no GREEN; não representam falhas internas isoladamente reproduzidas agora. As duas proteções confirmam a fixture real de conteúdo rico e a identidade de cadernos homônimos.

## Contrato de fluxo para aprovação

Excluir e restaurar espaço/caderno/página; cancelar confirmação; preservar estilos, código, tags e anexos; respeitar rascunhos; ocultar excluídos da busca/listas; manter exclusões numa nova composição; recusar snapshot atrasado; restaurar somente os descendentes pertencentes à mesma operação; bloquear filhos com pais ainda excluídos. As ações esperadas e os pontos de confirmação/restauração têm nomes de controles nos testes, sem desenho novo de coordenadas/cores.

Ver [especificação e tabela dos casos](../estudos/39-lixeira-especificacao.md). Backup da lixeira, migração, falhas/cancelamento de persistência e corrida real com autosave exigem cobertura complementar antes da entrega final e seguem revisão própria. Não considerar a lixeira concluída ao passar somente estes fluxos.

Artefatos em `trama-library-trash-red`: `unit.trx`, `integration.trx`, `implementacao.diff`, `audit.json` e `failures.json`. A auditoria confirma hashes de fontes e testes anteriores, links locais e uma varredura limitada por segredos nas adições. Nenhuma publicação de produto foi gerada em estado RED.

Pendência obrigatória: revisão humana dos testes, conforme seção 9 do CODEX. Uma aprovação permite implementar os fluxos especificados; não autoriza apagar definitivamente dados ou enfraquecer testes anteriores. Esforço moderado, um agente sem subagentes. Reutilizar a fixture e executar os casos focados primeiro na implementação reduz exploração e execução repetidas.
