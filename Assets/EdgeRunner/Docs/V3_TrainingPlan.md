# EdgeRunner V3 Training Plan

Este plano assume que a V3 usa `Behavior Name = EdgeRunnerV3`, duas branches discretas e `Vector Observation Space Size = 27` com `terrainSampleCount = 11`.

## Regras gerais

- Nao treinar a V3 com modelos da V1/V2: a V3 tem observacoes diferentes.
- Guardar como `Candidate` apenas modelos que passem a fase atual de forma repetivel.
- Arquivar modelos maus quando falham claramente a mesma validacao apos 2 ou 3 retreinos curtos.
- Subir dificuldade so depois de validacao manual em pelo menos 20 episodios.
- Nao treinar quando ha missing references, Behavior Name errado, observation size errado, ou sensores/gizmos desalinhados com o terreno.

## Fase 1 - Chao plano + goal

Objetivo: aprender a andar para a direita sem saltos desnecessarios.

GapGenerator:
- Min Gaps: 0
- Max Gaps: 0
- Min Platform Width: 8
- Max Platform Width: 12
- Min Gap Width: 1.5
- Max Gap Width: 2

Criterio de aprovacao: 18/20 chegadas ao goal.

Run-id sugerido: `ER_V3_Phase01_Flat_01`

## Fase 2 - 1 gap pequeno

Objetivo: aprender timing basico de salto com aterragem visivel.

GapGenerator:
- Min Gaps: 1
- Max Gaps: 1
- Min Platform Width: 4
- Max Platform Width: 7
- Min Gap Width: 1.5
- Max Gap Width: 2.25

Criterio de aprovacao: 16/20 no minimo, idealmente 18/20.

Run-id sugerido: `ER_V3_Phase02_Gap01Small_01`

## Fase 3 - 2 a 3 gaps

Objetivo: generalizar entre sequencias curtas e manter momentum.

GapGenerator:
- Min Gaps: 2
- Max Gaps: 3
- Min Platform Width: 3.5
- Max Platform Width: 6
- Min Gap Width: 1.75
- Max Gap Width: 2.75

Criterio de aprovacao: 16/20.

Run-id sugerido: `ER_V3_Phase03_Gap03LowMid_01`

## Fase 4 - 4 a 6 gaps

Objetivo: estabilidade em niveis gap-only de dificuldade media.

GapGenerator:
- Min Gaps: 4
- Max Gaps: 6
- Min Platform Width: 2.9
- Max Platform Width: 4.5
- Min Gap Width: 2.5
- Max Gap Width: 3.5

Criterio de aprovacao: 16/20 para continuar, 18/20 para guardar como candidato forte.

Run-id sugerido: `ER_V3_Phase04_Gap06Mid_01`

## Fase 5 - Drops seguros e pequenas diferencas de altura

Objetivo: testar a utilidade dos sensores de `landingDeltaY`, `safeDropAhead` e perfil do terreno.

GapGenerator:
- Usar uma cena manual ou uma versao futura do gerador com plataformas ligeiramente acima/abaixo.
- Diferenca vertical sugerida: -0.5 a +0.5
- Gaps sugeridos: 2 a 5
- Largura dos gaps: 1.75 a 3.25

Criterio de aprovacao: 16/20 em pelo menos duas seeds/cenas.

Run-id sugerido: `ER_V3_Phase05_DropsHeight_01`

## Fase 6 - Nivel misto para demonstracao

Objetivo: validar comportamento em gaps pequenos, medios, grandes, drops seguros, plataformas variadas e obstaculos simples.

Configuracao sugerida:
- 4 a 6 desafios por episodio.
- Misturar gaps de 1.5 a 3.55.
- Incluir plataformas largas e curtas.
- Incluir alguns trechos planos para confirmar que o agente nao salta sem necessidade.
- Incluir obstaculos simples/paredes apenas depois de a locomocao estar estavel.

Criterio de aprovacao: 18/20 antes de promover para `Runtime`.

Run-id sugerido: `ER_V3_Phase06_MixedDemo_01`

## Promocao de modelos

- `Candidates`: modelos com resultado promissor na fase atual.
- `V3_Experimental`: modelos usados para comparar variantes da V3.
- `Runtime`: apenas o modelo escolhido para demonstracao ou build.
- `Archive`: modelos falhados, obsoletos ou substituidos.
