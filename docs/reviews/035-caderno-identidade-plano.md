# Caderno: planejamento de identidade e formatação

30/10/2026. Entrega exclusivamente documental, com prévia visual estática externa ao produto. Não altera código, testes, pacotes, banco ou manual/PDF.

Pedido: manter o plano do Trama, priorizando tamanhos de texto, cores das letras, marca-texto e identidade própria para os cadernos. A [proposta técnica](../estudos/26-identidade-e-formatacao-dos-cadernos.md) registra escopo, contratos, compatibilidade, riscos e ordem TDD. CODEX, README, índice de estudos e roadmap apontam para a nova prioridade.

Achados: estilos atuais têm apenas flags negrito/itálico; a view repete ferramentas por bloco; SQLite e backup reconhecem conteúdo 1/2. A evolução precisa versionar atributos para impedir que uma versão antiga os descarte silenciosamente. Não se propõe trocar a stack nem o motor do editor nesta etapa.

Direção: página contínua, margem característica, hierarquia de texto e uma barra por editor. Código mantém distinção visual e realce; imagens permanecem no fluxo. Paletas personalizadas continuam uma evolução separada, com recursos semânticos preservados desde agora. Não foi realizada análise jurídica.

Verificações desta entrega: comparação documental, links locais, preservação por hashes dos fontes/testes/manual e inspeção visual da prévia. Testes do produto não são aplicáveis a esta mudança e não foram reexecutados. Os 325 aprovados são a evidência histórica da entrega anterior.

Riscos para implementação: atributos perdidos em gravação/backup, histórico incompleto, foco/seleção na barra, contraste entre temas e crescimento de linhas com tamanhos mistos. Próxima ação: testes RED do modelo; revisão explícita antes do GREEN, conforme seção 9 do CODEX. A proposta visual permanece para revisão do responsável.

Esforço: moderado, executado por um agente, sem subagentes. Exploração focada e uma rodada de ajuste da prévia; sem medição de tokens ou custo monetário. Economia na próxima etapa: trabalhar primeiro apenas no contrato de formatação e seus testes, reutilizando este mapa antes de integrar persistência e interface.
