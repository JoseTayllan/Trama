# Modelo de atributos: GREEN para revisão

30/10/2026. Os 28 casos foram aprovados pelo responsável antes da implementação. Permaneceram intactos. Único fonte de produção alterado: [InlineTextDocument.cs](../../src/Trama.Application/Knowledge/InlineTextDocument.cs).

## Resultado

O modelo representa e valida tamanho, cor e destaque opcionais, preserva os atributos na edição e só une intervalos com formatação inteiramente igual. Cor/tamanho/destaque sem negrito ou itálico são válidos. O construtor posicional e a deconstrução de `TextStyleSpan` permanecem; propriedades opcionais nulas são omitidas do JSON legado.

Comando executado:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

Restore travado aprovado; build Release com zero erros e zero avisos; formatação aprovada. Suíte: **353 aprovados, zero falhas, zero ignorados** — 77 unitários (incluindo os 28 aprovados) e 276 de integração. Uma execução completa nesta implementação. TRXs preservados nos artefatos externos `trama-rich-formatting-green/unit.trx` e `integration.trx`.

## Revisão e escopo

Revisão focou preservação dos três atributos em `SetStyle` e `ReplaceText`, validação antes de publicar o documento, intervalos sem flags, comparação de todos os atributos e serialização de campos nulos. `StyleAt` mantém seu retorno de flags para consumidores anteriores, usando a consulta interna de intervalo completo. Não foi adicionada dependência externa nem código de log.

Testes anteriores e aprovados, pacotes/locks, migrações e manual/PDF permanecem sem alteração. Não houve acesso ao banco pessoal, nova publicação de executável ou validação visual nova: o recorte é o modelo em memória.

O diff unificado e a auditoria ficam nos artefatos da sessão: `trama-rich-formatting-green/implementacao.diff` e `audit.json`. A revisão humana do diff permanece pendente. Não confundir esta entrega com formatação já disponível no aplicativo.

## Proteções seguintes

Persistência precisa declarar e validar a nova versão de conteúdo antes de conectar os controles. Depois, testar renderização, contraste, seleção/foco, altura das linhas, limpeza e histórico completo. Os testes atuais não comprovam esses fluxos. A hierarquia Espaço → Caderno → Páginas continua registrada como requisito da nova interface.

Esforço baixo a moderado: um agente, sem subagentes, uma rodada de implementação/revisão e uma execução completa de verificações. Sem medição de tokens ou custo monetário. Para economizar na próxima fatia, reaproveitar o mapa de persistência/backup e começar pelos testes de compatibilidade de versões, sem reexplorar toda a solução.
