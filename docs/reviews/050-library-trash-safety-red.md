# Lixeira: RED de segurança e compatibilidade

31/10/2026. Dez casos complementares preparados após GREEN dos 18 aprovados. **Três falhas comportamentais e sete proteções passando**: backup aceita conteúdo inválido na lixeira, backup aceita raiz sem origem e busca retorna uma nota legada excluída. Migração, pacotes anteriores, backup válido com lotes, cancelamento, rollback e debounce real passam.

Os 444 casos da primeira fatia tinham passado integralmente antes destes testes; os arquivos de produção e os testes anteriores foram congelados por hash ao iniciar a segunda fatia. As execuções completa anterior e focada complementar são evidências distintas. Total definido agora: 454 testes; a árvore atual está em RED, sem declaração de produto pronto.

Preparação teve ajuste de acesso a tipos internos e formatação no novo teste, sem mudar visibilidade ou comportamento do produto. Falhas de compilação/formatação não contam como RED. Resultado final e mensagens estão nos TRX/`failures.json` de `trama-library-trash-safety-red`; a primeira implementação tem diff separado em `trama-library-trash-green`.

Não foi antecipada a filtragem legada nem a validação adicional de backups. A seção 9 do CODEX exige revisão explícita dos testes antes dessas correções. Ver [especificação](../estudos/41-lixeira-seguranca-especificacao.md). Permanecem revisão do diff e aparência do diálogo, além do pacote final posterior ao GREEN completo.

Esforço moderado, um agente e nenhuma delegação; sete cenários já protegem a implementação e três orientam correções pontuais. Economia seguinte: corrigir somente o laço legado e o validador, começando pelos dez casos focados e ampliando para a suíte ao passar.
