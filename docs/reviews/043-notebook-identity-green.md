# Identidade e hierarquia: GREEN para revisão

30/10/2026. Os 15 casos e a direção de composição foram aprovados antes da implementação. Testes aprovados e anteriores preservados integralmente. Mudanças restritas a apresentação, propriedades derivadas/notificações e recursos visuais; sem mudança de banco, pacotes ou logo. Manual/PDF intactos.

## Resultado

Contexto explícito Espaço → Caderno → Página, título em edição e estados vazios; renomeação/movimentação acompanha o conteúdo realmente aberto. Nomes longos têm tooltip e agrupamento adaptável. Lateral com recuos e página contínua com margem; título, status e ações de inserção se reorganizam em largura compacta. Cabeçalho estático substituído por Biblioteca pessoal, preservando o binding e testes da fundação.

Comando final:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

Restore travado, build Release com zero avisos/erros e formatação aprovados. **414 aprovados, zero falhas/ignorados**: 77 unitários e 337 de integração, incluindo os 15 aprovados intactos.

Harness SQLite/Skia confirmou contexto, título vivo, renomeação sem recriar prosa, limites/tooltips em janela compacta, texto/código/imagem, popup/foco, teclado, autosave/nova sessão, cores dinâmicas e altura de texto. Capturas ampla/compacta/anexo/nomes longos inspecionadas. Contraste mínimo 5,882:1 em 42 combinações avaliadas da paleta padrão.

Pacote Windows win-x64 self-contained gerado em cópia/saída separadas. Smoke nativo: título Trama, Responding=true, fechamento concluído em perfil sintético. A cópia de dados de teste usou backup SQLite. Não houve acesso ao banco pessoal, alteração de segurança ou substituição da publicação habitual.

## Revisão

Revisão focada em getters sem efeitos colaterais, título derivado do editor, notificações das seleções/renomeações, manutenção de IDs e preservação da instância/histórico. Inspeção inicial identificou título e status disputando espaço; ambos passaram a reflow e foram conferidos novamente. Não foram alteradas regras de navegação para fazer o contexto funcionar.

Artefatos externos em `trama-notebook-identity-green`: `implementacao.diff`, `audit.json`, `unit.trx`, `integration.trx`, `contrast.json`, `qa/qa-success.txt`, quatro capturas, `native-smoke-final.json`, `windows/Trama.Desktop.exe` e `Testar-Caderno.cmd`. O launcher usa perfil sintético separado. Revisão humana do diff, aprovação estética e DPI/uso interativo permanecem pendentes.

Esforço moderado: um agente, sem subagentes; uma rodada focada com 15 aprovados, duas verificações completas e duas conferências visuais por causa dos ajustes finais de título/status. Sem medição de tokens/custo. Economia seguinte: reutilizar as evidências atuais e concentrar ajustes no feedback visual do responsável, sem reexplorar o modelo/persistência estabilizados.
