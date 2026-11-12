# Serviço de reflexão por perfil

Data: 10/11/2026.

O contrato IAgendaReflectionActions reúne leitura e gravação para o perfil ativo. A proposta segue a composição da Agenda. DesktopComposition registra o serviço com o caminho do banco já inicializado, e cada operação usa um contexto próprio. A gravação chama SaveAgendaReflection com o relógio injetado.

Os sete testes verificam leitura após reabrir o serviço, edição com texto preservado, isolamento de dois perfis na composição, fechamento ausente, banco ausente com nova tentativa, data inválida, cancelamento e gravações simultâneas em semanas diferentes.

O modo ReadWrite deve impedir a criação de um banco vazio quando o arquivo estiver ausente. A consulta deve informar falha de persistência nesse cenário. A gravação deve retornar PersistenceFailed e permitir nova tentativa quando o perfil estiver disponível.

Build e formatação passaram no WSL. Os sete casos novos falharam pela ausência do serviço e do registro na composição. A execução com filtro inverso passou com 1.294 testes, sendo 224 unitários e 1.070 de integração.

A implementação do serviço depende da revisão dos testes e do contrato, conforme CODEX.md §9. A edição e a proteção do rascunho continuam na etapa da interface.
