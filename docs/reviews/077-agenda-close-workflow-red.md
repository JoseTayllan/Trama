# Fechamento coordenado — RED

**Atualização de 04/11/2026:** os 35 casos foram aprovados e implementados, preservados. Suíte completa com 738 aprovados; build/formatação passaram. [Entrega GREEN](078-agenda-close-workflow-green.md). O texto abaixo preserva o histórico RED.

**Resultado final:** 35 casos descobertos, 33 falhas esperadas e duas guardas passando, zero ignorados. Vinte e seis falhas vêm das operações ausentes, quatro de controles ainda inexistentes e três do fechamento nativo ainda ignorar o coordenador. As asserções posteriores às entradas vazias não foram alcançadas; não declarar segurança/diálogo implementados. Separadamente, os 703 casos anteriores passaram (117 unitários e 586 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram. A suíte completa atual contém RED novo e não está GREEN.

Data: 04/11/2026. Especificação para revisão antes da implementação.

Contratos mínimos novos para diálogo, decisão, coordenador descartável, prompt e propriedade opcional no host. Janela sem conteúdo, métodos não implementados e nenhuma ligação ao App/MainWindow atual. Banco, serviço e migrações intactos.

Os testes especificam três escolhas com seleção manual, Cancelar seguro, X, layout compacto, consulta após rascunhos, falhas/reentrada, metadados reais depois da decisão e proteção dos dois editores. Há proteção específica para autosave durante o aviso e entre a aprovação do descarte e o fechamento real, além de impedir novas confirmações depois de gravar conclusões.

Uma compilação inicial detectou falta de using Avalonia no teste de geometria; corrigido antes de contar RED. A janela vazia recebeu NameScope mínimo para que a falta dos controles seja constatada por asserção, sem erro de infraestrutura. Depois houve duas ampliações antes da revisão: proteção do descarte aprovado contra autosave e ausência de reconfirmação após gravar. Nenhuma dessas falhas de compilação é apresentada como comportamento RED.

Riscos para o GREEN: descartar editor antes do commit, retomar autosave de conteúdo descartado, reabrir confirmação após concluir tarefas, ignorar o coordenador no caminho nativo da janela e usar dia UTC ou instante anterior à decisão. Fechamento forçado/crash não está coberto como conclusão automática.

Nenhum teste antigo alterado ou perfil pessoal acessado. Sem migração, dependência, log, segredo literal, chamada externa, Git ou publicação. Manual/PDF e README preservados. A inspeção de segredos limita-se aos arquivos envolvidos.

Esforço moderado, sem novos subagentes: quatro execuções focadas durante o refinamento dos riscos e da infraestrutura mínima, com validação do RED final somente depois dessas correções, e uma rodada da base anterior. Reutilizar fixtures, serviço e diálogo de alterações existentes reduz duplicação; tokens/custo exato não foram medidos. Revisão humana dos casos obrigatória e aceite estético posterior.

[Estudo](../estudos/70-fechamento-coordenado-especificacao.md).
