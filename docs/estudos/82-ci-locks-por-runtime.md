# 82 — NU1004 no CI: separar locks de runtime

Data: 05/11/2026. Correção de infraestrutura para revisão; nova execução no GitHub pendente.

O workflow executa dotnet restore Trama.slnx --locked-mode sem runtime explícito. Quatro locks em src continham, além do grafo net10.0, o grafo net10.0/win-x64 introduzido por restauração/publicação para Windows. Reproduzimos o mesmo NU1004 localmente antes de editar. Em modo bloqueado, NuGet rejeita essa divergência; retirar a proteção esconderia a inconsistência.

## Correção

Os quatro locks foram normalizados para seus grafos genéricos originais, sem mudar versões, dependências ou contentHash. Locks dos testes permaneceram intactos. Directory.Build.props direciona NuGetLockFilePath para obj/nuget-locks/<runtime>/packages.lock.json quando há RuntimeIdentifier ou RuntimeIdentifiers. O primeiro cobre a propriedade usada por publish; o segundo cobre restore --runtime. O caminho é separado por projeto e já ignorado pelo Git por estar em obj.

Sem runtime explícito, o caminho padrão packages.lock.json permanece e o CI continua em --locked-mode. O workflow não foi alterado. Restauração de publicação não sobrescreve mais os locks genéricos. Locks transitórios de runtime em obj não são versionados; esta mudança não cria um pipeline de publicação reproduzível com runtime bloqueado. Publicações continuam sendo verificadas separadamente.

## Verificação real

No Windows: restore bloqueado falhou com NU1004 antes da correção; passou depois. Verificados restore --runtime win-x64 e restore -p:RuntimeIdentifier=win-x64, seguidos de restore genérico --locked-mode. Os quatro locks de runtime foram gerados em obj; comparação semântica confirmou todos os grafos genéricos originais e hashes preservados. Build Release e formatação passaram.

Os testes locais Windows encontraram o bloqueio de Trama.Desktop.dll pela política de Controle de Aplicativo, HRESULT 0x800711C7. A suíte não foi declarada aprovada no Windows. Relatório registrou 131 unitários aprovados e execução parcial de integração (302 aprovados, 113 falhos), com descoberta afetada pela DLL bloqueada. Um caso isolado confirmou o mesmo FileLoadException. Não foram alteradas políticas ou testes para contornar o bloqueio.

No WSL, o helper executou restore bloqueado, build, formatação e **865 testes aprovados: 131 unitários e 734 integrações**, zero falhas/ignorados. Arquivos C# anteriores intactos por hashes; nenhuma mudança funcional, migração ou dependência atualizada. CI remoto depende do commit/push do responsável e de nova execução; não inferir seu resultado do teste local.

```powershell
dotnet restore Trama.slnx --locked-mode
dotnet restore src/Trama.Desktop/Trama.Desktop.csproj --runtime win-x64
dotnet restore Trama.slnx --locked-mode
```

## Continuidade

O responsável opera Git: revisar o diff, commitar Directory.Build.props, os quatro locks e a documentação, fazer push na branch e acompanhar uma nova execução. Nenhum Git, publicação, banco pessoal ou manual/PDF foi alterado pelo agente. O próximo passo funcional continua sendo confirmação e tela da lixeira da Agenda.

Para estudar: por que um lock correto para publish pode divergir do restore genérico? Por que validar o ciclo runtime → genérico é mais forte que testar apenas um restore? Por que bloqueio local de DLL e inconsistência NuGet são problemas distintos?

[NU1004](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1004) · [NuGetLockFilePath e modo bloqueado](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#lock-file-extensibility) · [Relatório](../reviews/089-ci-locks-runtime-fix.md).
