# CI — correção de NU1004 por runtime

Data: 05/11/2026. Verificações locais abaixo.

Aceite posterior, 06/11/2026: o responsável confirmou “deu certo” para a correção do CI. Execução remota não consultada pelo agente; o restante do relatório registra as evidências e limites da entrega anterior. Nenhum novo teste foi executado apenas para registrar esse aceite.

Reproduzido NU1004 com o mesmo restore bloqueado do workflow. Quatro locks de src continham win-x64 enquanto a solução não declara runtime na execução do CI. Removidos somente os grafos específicos; comparação semântica mantém versões, dependências e hashes genéricos. Directory.Build.props isola locks quando RuntimeIdentifier ou RuntimeIdentifiers estão presentes, em obj por projeto/runtime. Workflow e --locked-mode intactos.

Windows: restore bloqueado e ciclo runtime → genérico passaram; ambos os formatos de propriedade foram exercitados. Build Release sem avisos/erros e formatação passaram. Execução local dos testes afetada pela política que bloqueia Trama.Desktop.dll (0x800711C7), confirmada num caso isolado. Não declarar suíte Windows ou CI remoto aprovados. Relatórios locais: 131 unitários aprovados, integrações parciais 302 aprovadas/113 falhas com descoberta afetada.

WSL: restore bloqueado, build, formatação e **865 testes aprovados (131 unitários e 734 integrações)**, zero falhas/ignorados. C# e testes existentes intactos. Não foram criados testes artificiais de infraestrutura: a falha real foi reproduzida e o ciclo de restauração executado. Sem nova regra funcional, migração, dependência, executável ou perfil pessoal.

A primeira tentativa de isolamento cobria só RuntimeIdentifier; o ciclo real identificou que restore --runtime usa RuntimeIdentifiers. Condição ampliada, locks genéricos recuperados da base preservada e ciclo repetido com sucesso. Consulta auxiliar de propriedades MSBuild não foi usada como evidência, pois a avaliação isolada de Avalonia falhou antes do build normal, que passou. Nenhuma proteção do Windows foi alterada.

Riscos/limites: locks de runtime em obj não são versionados e não estabelecem publicação bloqueada reproduzível. CI remoto deve ser confirmado após push. Nenhum Git executado pelo agente, manual/PDF ou guia de operação alterado. Inspeção limitada dos arquivos alterados não encontrou segredo literal ou logging novo.

Esforço moderado, sem subagentes: reprodução, ciclos de restore Windows, build/formatação, suíte local Windows com diagnóstico isolado e suíte completa WSL. Próxima verificação deve ser a nova execução do CI após o commit, evitando repetir testes locais sem mudança ou erro novo. Tokens/custo exato não medidos. Revisão humana do diff pendente.

[Estudo](../estudos/82-ci-locks-por-runtime.md).
