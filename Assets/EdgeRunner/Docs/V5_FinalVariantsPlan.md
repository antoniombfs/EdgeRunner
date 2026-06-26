# EdgeRunner V5 Final Variants Plan

Objetivo de entrega: consolidar duas variantes finais principais sem destruir historico nem quebrar a demo manual.

## 1. GoalRunner

### Objetivo

Chegar ao Goal em niveis random/controlados.

### Cena de treino

Gerar no Unity:

`EdgeRunner > Training > V5 > Build GoalRunnerRandom`

Cena:

`Assets/EdgeRunner/Scenes/Training/ER_V5_GoalRunnerRandom.unity`

### YAML

`Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_goalrunner_random.yaml`

### Agente

- `EdgeRunnerAgentV5`
- Behavior Name: `EdgeRunnerV5`
- Observation Space Size: `55`
- Actions/Branches: `[3,2,2]`
- Gerador: `MixedLevelGenerator` com regras V5 controladas.

### Rewards

- Recompensa final por Goal.
- Recompensa por progresso em direcao ao Goal.
- Penalizacao pequena por step.
- Penalizacao por queda/stall/no progress.

### Avaliacao

Usar `EdgeRunnerEvaluationManager` na cena, ativando `enableEvaluation` no Inspector.

Metricas:

- successRate;
- avgTime;
- avgDistanceToGoalOnFail;
- avgEpisodeReward;
- falls/collisions.

Comando sugerido:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_goalrunner_random.yaml --run-id=ER_V5_GoalRunnerRandom01 --force
```

## 2. SpeedRunner

### Objetivo

Chegar ao Goal o mais depressa possivel.

### Cena de treino

Gerar no Unity:

`EdgeRunner > Training > V5 > Build SpeedRunnerRandom`

Cena:

`Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunnerRandom.unity`

### YAML

`Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_speedrunner_random.yaml`

### Diferencas face ao GoalRunner

- Maior penalizacao por step/tempo.
- Maior incentivo a progresso rapido.
- Maior recompensa por velocidade horizontal util.
- NoProgress/Stuck/Timeout mais curtos.
- Sem moedas obrigatorias.
- Sem Androids obrigatorios.
- Sem Goal bloqueado.

### Avaliacao

Metricas:

- successRate;
- avgTime;
- bestTime;
- avgVelocity/progressRate, se disponivel;
- avgEpisodeReward.

Comando sugerido:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_speedrunner_random.yaml --run-id=ER_V5_SpeedRunnerRandom01 --initialize-from=ER_V5_GoalRunnerRandom01 --force
```

Se o melhor modelo base continuar a ser `ER_V5_GoalRelative_V5Gen_EasyBridge01_Final_Test`, usar esse resultado como referencia no relatorio antes de treinar SpeedRunner.

## 3. ScoreAttack

### Objetivo

Apanhar todas as moedas e matar todos os Androids antes de chegar ao Goal.

O Goal so deve contar como sucesso quando:

- `coinsRemaining == 0`;
- `enemiesRemaining == 0`.

### Componentes

- `ScoreAttackManager`
  - Conta moedas restantes.
  - Conta Androids restantes.
  - Aplica recompensas.
  - Bloqueia Goal prematuro.
  - Reseta objetos no inicio de episodio.
- `ScoreAttackCoin`
  - Trigger apanhavel.
  - Da reward e desativa visual/collider.
- `ScoreAttackAndroid`
  - Stomp por cima mata.
  - Contacto lateral aplica `EnemyHit`.
  - Da bounce no Player ao stomp.
- `ScoreAttackGoalLock`
  - Encaminha o contacto com o Goal para o agente.
  - O `ScoreAttackManager` decide se o Goal esta desbloqueado.

### Cenas curriculares

#### ScoreAttackIntro

Menu:

`EdgeRunner > Training > V5 > Build ScoreAttackIntro`

Cena:

`Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackIntro.unity`

YAML:

`Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoreattack_intro.yaml`

Layout:

- plataforma simples;
- 1 moeda;
- 1 Android;
- Goal depois dos objetivos.

#### ScoreAttackEasy

Menu:

`EdgeRunner > Training > V5 > Build ScoreAttackEasy`

Cena:

`Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackEasy.unity`

YAML:

`Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoreattack_easy.yaml`

Layout:

- 2 moedas;
- 1 Android;
- Goal bloqueado.

#### ScoreAttackRandomControlled

Menu:

`EdgeRunner > Training > V5 > Build ScoreAttackRandomControlled`

Cena:

`Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackRandomControlled.unity`

YAML:

`Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoreattack_random_controlled.yaml`

Layout:

- plataforma larga;
- moedas e Androids reposicionados em ranges seguros a cada episodio;
- Goal bloqueado ate completar objetivos.

### Curriculo sugerido

```text
ScoreAttackIntro -> ScoreAttackEasy -> ScoreAttackRandomControlled
```

### Avaliacao

Metricas:

- successRate;
- avgCoinsCollected;
- avgEnemiesKilled;
- fullCompletionRate;
- prematureGoalTouches;
- avgTime;
- avgEpisodeReward.

O `ScoreAttackManager` ja expoe `CoinsCollected`, `EnemiesKilled`, `PrematureGoalTouches`, `CoinsRemaining` e `EnemiesRemaining`. O proximo passo, se houver tempo, e ligar estes valores ao `EdgeRunnerEvaluationManager` para relatorios CSV/TXT automaticos.

Comando inicial:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_scoreattack_intro.yaml --run-id=ER_V5_ScoreAttackIntro01 --initialize-from=ER_V5_GoalRunnerRandom01 --force
```

## 4. ScoreMax

### Objetivo final

ScoreMax e a variante final de pontuacao maxima:

- apanhar todas as moedas;
- matar todos os Androids por stomp;
- so depois chegar ao Goal;
- maximizar objetivos completos antes de terminar o nivel.

Esta variante nao usa `EdgeRunnerAgentV5` diretamente como agente final, porque as 55 observacoes da V5 base nao descrevem a missao de score. Tambem nao usa `EdgeRunnerAgentV5EnemyAware`, porque essa linha foi pensada para evitar inimigos com observation space 63.

### Agente

- `EdgeRunnerAgentV5ScoreMax`
- Behavior Name: `EdgeRunnerV5ScoreMax`
- Observation Space Size: `83`
- Actions/Branches: `[3,2,2]`

O agente reutiliza a navegacao da V5 e acrescenta 28 observacoes especificas:

- estado da missao: moedas restantes, Androids restantes e Goal desbloqueado;
- moeda viva mais proxima;
- Android vivo mais proximo;
- Goal;
- proximo objetivo recomendado;
- rays simples para Android/stomp (`front_low`, `front_mid`, `down_forward`, `back_mid`).

### Next Objective

O `ScoreAttackManager` expoe dados para o agente ScoreMax:

- `CoinsRemaining`;
- `EnemiesRemaining`;
- `ObjectivesComplete`;
- Goal;
- listas de moedas e Androids;
- moeda viva mais proxima;
- Android vivo mais proximo;
- proximo objetivo recomendado.

Regra de curriculo para o proximo objetivo:

1. se ainda houver moedas vivas, seguir a moeda viva mais proxima;
2. se nao houver moedas e ainda houver Androids vivos, seguir o Android vivo mais proximo;
3. quando tudo estiver completo, seguir o Goal.

A logica de jogo continua a aceitar qualquer ordem real: matar Android antes de moedas e valido, mas o Goal so desbloqueia quando moedas e Androids chegam a zero.

### Rewards

ScoreMax usa recompensas mais fortes de objetivo:

- moeda: `+2.0`;
- stomp em Android: `+4.0`;
- Goal final completo: `+10.0`;
- contacto lateral com Android: `-6.0`;
- queda: `-5.0`;
- Goal bloqueado: `-1.0`;
- step penalty pequeno;
- progresso controlado para o proximo objetivo.

ScoreMax nao forca sprint. O agente pode aprender a usar sprint, mas essa decisao nao e imposta como no SpeedRunner.

### Curriculo ScoreMax

Menus:

- `EdgeRunner > Training > V5 > Build ScoreMaxCoinIntro`
- `EdgeRunner > Training > V5 > Build ScoreMaxStompIntro`
- `EdgeRunner > Training > V5 > Build ScoreMaxIntro`
- `EdgeRunner > Training > V5 > Build ScoreMaxEasy`
- `EdgeRunner > Training > V5 > Build ScoreMaxFinalRandom`

Cenas:

- `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxCoinIntro.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxStompIntro.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxIntro.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxEasy.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxFinalRandom.unity`

YAMLs:

- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoremax_coin_intro.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoremax_stomp_intro.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoremax_intro.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoremax_easy.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoremax_final_random.yaml`

Curriculo recomendado:

```text
ScoreMaxCoinIntro -> ScoreMaxStompIntro -> ScoreMaxIntro -> ScoreMaxEasy -> ScoreMaxFinalRandom
```

Nao usar `--initialize-from` com modelos `EdgeRunnerV5` de 55 observacoes, porque `EdgeRunnerV5ScoreMax` usa 83 observacoes.

## Estado EnemyAware

EnemyAware nao e uma das duas variantes finais principais. Deve ficar como experiencia secundaria:

- Behavior Name: `EdgeRunnerV5Enemies`
- Observation Space Size: `63`
- EnemyRays ativos nas fases recentes.
- `StaticIntroEnemyRaysPrematureAir` funciona de forma consistente.
- PrematureJumpMask pode ser descrita como action masking de seguranca.

Nao transformar EnemyAware em ScoreAttack.

## Ledge / Edge Unstuck Safety

### Problema observado

Antes de treinar o `GoalRunnerRandom`, em Heuristic foi observado que saltos curtos podiam deixar o agente preso na quina/lateral de uma plataforma. Isto e uma falha fisica do contacto com a borda, nao uma decisao de navegacao que o agente deva aprender.

Se isto entrar no treino, o agente pode aprender contra um ambiente injusto: a tentativa de salto e aproximacao pode estar correta, mas a colisao lateral impede recuperacao.

### Solucao implementada

`EdgeRunnerAgentV5` tem um mecanismo opcional:

- `enableLedgeUnstuck`
- `debugLedgeUnstuck`
- `ledgeStuckMinTime`
- `ledgeFrontCheckDistance`
- `ledgeFootClearCheckDistance`
- `ledgeUnstuckHorizontalNudge`
- `ledgeUnstuckVerticalNudge`
- `ledgeUnstuckCooldown`
- `ledgeMaxUnstucksPerEpisode`

Defaults no agente: desligado por seguranca.

Quando ativo, o agente so aplica um pequeno nudge se:

- esta a tentar mover-se horizontalmente;
- nao esta claramente grounded;
- a velocidade horizontal ficou quase zero;
- ha bloqueio lateral baixo/corpo contra Ground;
- a zona superior a frente esta livre;
- respeita cooldown e limite por episodio.

Logs opcionais:

- `[LEDGE STUCK] side=right/left vel=... grounded=... stuckTime=...`
- `[LEDGE UNSTUCK] applied nudge=...`

Isto nao deve resolver gaps impossiveis nem atravessar paredes. Serve apenas para evitar ficar colado a quinas artificiais.

### Onde esta ativo

No builder `BuildER_V5_FinalVariants.cs`:

- `GoalRunnerRandom`: `enableLedgeUnstuck = true`
- `SpeedRunnerRandom`: `enableLedgeUnstuck = true`
- `ScoreAttack`: `enableLedgeUnstuck = false` por agora, para nao interferir com stomp/Androids.

O builder tambem cria/reutiliza `Assets/EdgeRunner/Physics/NoFriction2D.physicsMaterial2D` com:

- `friction = 0`
- `bounciness = 0`

Esse material e aplicado so a instancias de treino geradas para GoalRunner/SpeedRunner e as plataformas geradas pelo `MixedLevelGenerator`.

## Geracao procedural justa

Depois de testar o `GoalRunnerRandom` com `maxGapWidth = 3.2`, foi observado que algumas plataformas/segmentos curtos podiam ser saltados por completo quando o agente chegava com velocidade. Isto nao avalia bem navegacao ou decisao; e um artefacto injusto da geracao procedural.

O `MixedLevelGenerator` foi ajustado para distinguir plataformas normais de plataformas de aterragem:

- `minPlatformWidth` define o minimo geral das plataformas;
- `minLandingPlatformWidth` define o minimo das plataformas criadas depois de gaps;
- `minRecoveryPlatformWidth` define o minimo das plataformas de recuperacao apos segmentos dificeis;
- `minDistanceBetweenGaps` evita sequencias de gaps demasiado proximos;
- `minRunupBeforeGap` garante espaco antes de um gap;
- `minLandingAfterGap` garante aterragem limpa depois de um gap;
- `safeEdgeMargin` e `goalEdgeMargin` reservam margem nas extremidades para colocacao segura do Goal.

No builder `BuildER_V5_FinalVariants.cs`, GoalRunnerRandom e SpeedRunnerRandom usam:

- `minPlatformWidth = 4.8`;
- `minLandingPlatformWidth = 5.0`;
- `minRecoveryPlatformWidth = 5.0`;
- `minGapWidth = 2.2`;
- `maxGapWidth = 3.2` no GoalRunnerRandom;
- `maxGapWidth = 3.0` no SpeedRunnerRandom;
- `minDistanceBetweenGaps = 5.0`;
- `minRunupBeforeGap = 2.5`;
- `minLandingAfterGap = 5.0`;
- `safeEdgeMargin = 1.0`;
- `finalGoalPlatformWidth = 10.0`;
- `finalGoalSafeRunup = 5.0`;
- `goalEdgeMargin = 2.0`;
- `forceRecoveryPlatformAfterHardSegment = true`;
- `avoidRepeatedGaps = true`;
- `avoidHardGapIntoStepUp = true`.

Micro-gaps foram removidos porque criavam saltos artificiais e pouco informativos: o agente podia atravessar buracos pequenos sem uma decisao real de salto, ou saltar por cima de plataformas inteiras por excesso de velocidade. Se um gap calculado ficar abaixo do minimo configurado ou demasiado perto do gap anterior, o gerador converte o trecho em plataforma normal/continua.

O Goal tambem passou a ser tratado como zona de chegada ampla nas cenas GoalRunnerRandom e SpeedRunnerRandom:

- ha uma plataforma final larga antes do Goal;
- o Goal fica afastado das bordas;
- a trigger do Goal e configurada na instancia gerada como uma zona alta e larga (`2.5 x 7.0`), funcionando como linha de chegada.

Assim os niveis continuam a ter gaps medios, variacao vertical controlada e necessidade de saltar, mas evitam landing zones minimas, sequencias do tipo Gap -> plataforma curta -> Gap e falhas por o agente saltar por cima do Goal.

## Cenas finais por segmentos

Para a entrega final foram adicionados builders separados para niveis com aspeto mais proximo de platformer completo, sem substituir os builders curriculares existentes.

### SpeedRunnerFinalRandom

Menu:

`EdgeRunner > Training > V5 > Build SpeedRunnerFinalRandom`

Cena:

`Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunnerFinalRandom.unity`

Objetivo:

- chegar ao Goal o mais rapido possivel;
- sem moedas obrigatorias;
- sem Androids obrigatorios;
- Goal numa plataforma final ampla.

A cena e gerada por blocos/segmentos:

- `StartFlat`;
- `FlatRun`;
- `MediumGap`;
- `SafeDrop`;
- `PlatformChain`;
- `RecoveryPlatform`;
- `FinalGoalPlatform`.

O builder cria um layout left-to-right com gaps medios, safe drops suaves, plataformas separadas por gaps claros, plataformas de aterragem largas e plataformas de recuperacao depois de trechos mais dificeis. `StepUp` e `Staircase` ficam fora do SpeedRunnerFinalRandom por agora, porque retangulos colados com diferenca de altura criavam paredes verticais onde a V5 base podia ficar presa.

Para evitar costuras fisicas no meio do chao, trechos que parecem plataforma continua sao criados como uma unica plataforma/collider. O SpeedRunnerFinalRandom nao deve criar pecas encostadas no mesmo Y; plataformas separadas continuam separadas apenas quando ha um gap real.

Como esta variante e focada em tempo, o builder configura a instancia do `EdgeRunnerAgentV5` com modo SpeedRunner:

- `enableSpeedRunnerMode = true`;
- `forceSprintInSpeedRunner = true`;
- `speedRunnerSprintReward = 0.0015`;
- `debugSpeedRunnerSprint = false`.

O comportamento normal da V5 continua igual por defeito, porque estas flags ficam desligadas no agente base. A cena SpeedRunnerFinalRandom tambem adiciona `DemoSprintVisual` e `TrailRenderer` ao `Player_V5` instanciado para mostrar visualmente quando o sprint esta ativo.

### ScoreMaxFinalRandom

Menu:

`EdgeRunner > Training > V5 > Build ScoreMaxFinalRandom`

Cena:

`Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxFinalRandom.unity`

Objetivo:

- apanhar todas as moedas;
- matar todos os Androids por stomp;
- so depois chegar ao Goal bloqueado;
- usar a logica `ScoreAttackManager`, `ScoreAttackCoin`, `ScoreAttackAndroid` e `ScoreAttackGoalLock` ja existente.

A cena usa segmentos estaveis e zonas de objetivos:

- `FlatRun_CoinLine`;
- `MediumGap_AndroidLanding_A`;
- `SafeDrop_CoinPocket`;
- `PlatformChain_CoinArc`;
- `AndroidPlatform_FinalApproach`;
- `RecoveryPlatform`;
- `FinalLockedGoalPlatform`.

As moedas sao colocadas acima das plataformas com margem segura de borda e espacamento minimo. Os Androids ficam em plataformas largas, afastados das bordas e longe do Goal final. O Goal so deve contar como sucesso depois de `coinsRemaining == 0` e `enemiesRemaining == 0`.

Para a versao final random, `Staircase` e sequencias de `StepUp/StepDown` foram removidas/reduzidas. O layout privilegia plataformas planas, gaps medios, safe drops suaves, platform chains controladas e recuperacoes largas. Os limites de episodio da instancia `ScoreMaxFinalRandom` tambem foram alargados para dar mais margem antes de `NoProgress`, `Stuck` e `Timeout`.

### Parametros dos niveis finais

Os builders finais usam:

- `minPlatformWidth = 4.8`;
- `minLandingPlatformWidth = 5.0`;
- `minRecoveryPlatformWidth = 5.0`;
- `minGapWidth = 2.8`;
- `maxGapWidth = 3.2` no SpeedRunnerFinalRandom;
- `maxGapWidth = 3.0` no ScoreMaxFinalRandom;
- `maxVerticalStep = 1.2`;
- `finalGoalPlatformWidth = 10.0`;
- `goalTriggerSize = 2.5 x 7.0`;
- `safeEdgeMargin = 1.0`.

Estas regras existem para que o treino avalie navegacao, timing e decisao, e nao artefactos injustos de geracao procedural como micro-gaps, landings demasiado curtas, Goal imediatamente depois de gap ou objetivos colados a bordas.

## Limites e riscos

- ScoreAttack fica como base mecanica validada; ScoreMax e a variante final com observacoes explicitas de objetivos.
- SpeedRunner continua a ser a variante mais segura para entrega porque reutiliza diretamente a V5 base ja validada.
- Nao treinar ate `max_steps` se uma fase ja atingir sucesso consistente; guardar modelo e avancar no curriculo.
