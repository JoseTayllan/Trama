# Caderno em blocos — especificação RED

Data: 27 de outubro de 2026. Aguardando revisão dos testes antes de implementar.

## Alteração para revisar

- [NotebookDraftTests.cs](../../tests/Trama.UnitTests/Knowledge/NotebookDraftTests.cs): 15 casos novos, incluindo variações parametrizadas.
- [NotebookDraft.cs](../../src/Trama.Application/Knowledge/NotebookDraft.cs): contratos mínimos, sem comportamento funcional, não utilizados pelo desktop.
- [Proposta e explicação do caderno](../estudos/08-caderno-em-blocos.md).

## Resultado executado

```powershell
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/notebook-build --logger "trx;LogFilePrefix=notebook-red" --results-directory artifacts/notebook-red/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

O teste terminou com código 1: 14 unitários antigos e 43 de integração passaram; os 15 casos novos falharam. Não houve falha de compilação. Formatação terminou com código 0. Os arquivos TRX estão em `artifacts/notebook-red/results`.

| Grupo de casos | Quantidade | Motivo atual da falha |
| --- | --- | --- |
| Rascunho inicial | 1 | Blocks ainda lança NotImplementedException |
| Ordem mista, texto longo, código e imagem | 4 | Insert ainda lança NotImplementedException |
| Conteúdo composto apenas de espaços | 2 | Insert ainda lança NotImplementedException |
| Atualização, movimento e remoção | 4 | A preparação por Insert ainda lança NotImplementedException |
| Identidade duplicada | 1 | A primeira inserção ainda não está implementada |
| Índice inválido e imagem sem referência | 3 | Esperada validação específica; recebido NotImplementedException |

Essas falhas registram contratos vazios. As asserções específicas de preservação e ordenação só serão avaliadas após implementar os métodos. Não apresentar o RED como verificação de suporte a imagens ou de interface.

## Limites e riscos

Nenhum teste anterior foi editado. Nenhuma dependência, migração, regra de criação de nota ou tela foi alterada. A suíte completa ficará intencionalmente vermelha enquanto esta especificação aguarda aprovação. Os métodos novos não devem ser ligados ao desktop antes do GREEN.

Imagem nesta fatia é apenas referência de anexo em memória. Arquivos reais, importação, remoção física, banco, compatibilidade das notas antigas e realce de sintaxe exigem especificações e verificações adicionais. Os testes atuais não cobrem operações com identidade ausente nem todos os argumentos inválidos; esses contratos precisarão ser definidos antes de expô-los pela interface.

## Esforço

Trabalho realizado por um único agente, sem delegação e sem novas dependências. Um ciclo da suíte completa e uma verificação de formatação; não há medição de tokens disponível por tarefa. Para reduzir consumo, manter as próximas fatias limitadas a estrutura, persistência/anexos e interface, reutilizando este mapeamento.
