# EdgeRunner V5 EnemyAware

## Objetivo

A variante `EdgeRunnerAgentV5EnemyAware` mantém a navegação V5 como base, mas adiciona observações explícitas de Androids/inimigos para o agente aprender a evitar perigos no caminho até ao Goal.

## Diferenças para a V5 normal

- Behavior Name: `EdgeRunnerV5Enemies`
- Observation Space Size: `63`
- Acoes discretas: `[3, 2, 2]`
  - Movimento horizontal: esquerda, parar, direita
  - Salto: sem salto, salto
  - Sprint: sem sprint, sprint
- Observacoes extra: `8` valores de inimigos
  - 2 slots de inimigo
  - Por slot: `dx`, `dy`, distancia, flag perigoso/ativo

## Cena de treino

Gerar a cena EnemyIntro no Unity:

`EdgeRunner > Training > Build ER_V5_Enemies_Train`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_Train.unity`

## Curriculo

### NavWarmup

Fase sem inimigos perigosos para reaprender navegacao basica com observation space `63`.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build NavWarmup`

Comando de treino:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_navwarmup.yaml --run-id=ER_V5_EnemyAware_NavWarmup01 --force
```

### StaticIntroJumpCue

Fase didatica entre NavWarmup e StaticIntro normal. Serve para ensinar uma reacao explicita ao inimigo antes da colisao: quando ha Android perigoso a frente, o agente recebe sinal para saltar/evitar, depois recompensa por passar sem tocar.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build StaticIntroJumpCue`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroJumpCue.unity`

Comando de treino:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro_jumpcue.yaml --run-id=ER_V5_EnemyAware_StaticIntroJumpCue01 --initialize-from=ER_V5_EnemyAware_NavWarmup01 --force
```

O `--initialize-from` tambem e valido aqui porque NavWarmup e StaticIntroJumpCue usam o mesmo Behavior Name `EdgeRunnerV5Enemies`, observation space `63` e branches `[3, 2, 2]`.

### StaticIntroForcedJump

Fase ainda mais didatica do que StaticIntroJumpCue. Existe porque StaticIntro e JumpCue nao foram suficientes para quebrar a politica herdada da NavWarmup de correr sempre em frente. Nesta fase, quando ha Android perigoso a frente e alinhado para colisao, o reward de progresso normal fica desligado nessa janela; o agente recebe sinal forte para saltar/evitar, recompensa maior por passar sem tocar e penalizacao maior por `EnemyHit`.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build StaticIntroForcedJump`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroForcedJump.unity`

Comando de treino:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro_forced_jump.yaml --run-id=ER_V5_EnemyAware_StaticIntroForcedJump01 --initialize-from=ER_V5_EnemyAware_NavWarmup01 --force
```

O `--initialize-from` tambem e valido aqui porque NavWarmup e StaticIntroForcedJump usam o mesmo Behavior Name `EdgeRunnerV5Enemies`, observation space `63` e branches `[3, 2, 2]`.

### AvoidanceMicro

Fase micro e controlada para isolar uma unica competencia: ver Android perigoso a frente, saltar/evitar e passar sem tocar. Foi criada porque StaticIntroForcedJump e Behavioral Cloning nao foram suficientes para quebrar a tendencia de correr em frente contra o Android.

Layout:

- plataforma unica, plana e larga;
- agente nasce poucos metros antes do Android;
- Android estatico perigoso no caminho;
- Goal fica logo depois do Android;
- sem gaps, outros inimigos, score, stomp, HUD ou controller manual.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build AvoidanceMicro`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicro.unity`

Comando de treino do zero recomendado:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicro01 --force
```

Comando alternativo a partir da NavWarmup, secundario:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicro_FromNav01 --initialize-from=ER_V5_EnemyAware_NavWarmup01 --force
```

### AvoidanceMicroMasked

Fase tutorial baseada na AvoidanceMicro. Foi criada porque a AvoidanceMicro normal, treinada do zero, nao gerou exploracao suficiente da acao de salto perto do Android. Nesta fase, uma action mask impede o agente de correr diretamente contra o Android enquanto ele esta no chao, perto e alinhado verticalmente com o perigo.

A primeira versao usava uma janela de mask demasiado grande, o que podia bloquear o avanco logo desde o spawn. A versao atual usa uma janela mais curta (`enemyActionMaskWindowX = 2.5`) e deixa a janela de reward um pouco maior (`enemyAvoidanceWindowX = 3.5`), permitindo aproximacao antes do salto. O Android fica em `x = 6` e o Goal em `x = 11`.

A mask:

- bloqueia apenas a acao de movimento para a frente perto do Android;
- nao bloqueia salto;
- nao bloqueia left/stop;
- deixa right voltar quando o agente esta no ar ou depois de passar o Android;
- serve para ensinar a competencia inicial, nao necessariamente para ser a politica final.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build AvoidanceMicroMasked`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroMasked.unity`

Comando de treino do zero:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_masked.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicroMasked01 --force
```

Se funcionar, fine-tune seguinte sem mask:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicro_UnmaskedFT01 --initialize-from=ER_V5_EnemyAware_AvoidanceMicroMasked01 --force
```

### AvoidanceMicroEnemyRays

Fase experimental para treinar do zero com percepcao espacial mais explicita do Android. Foi criada porque as observacoes abstratas `dx/dy/dist/danger` e as masks anteriores nao foram suficientes para ensinar a evasao de forma consistente.

Esta fase mantem o contrato do modelo:

- Behavior Name: `EdgeRunnerV5Enemies`
- Observation Space Size: `63`
- Acoes: `[3, 2, 2]`

A diferenca e apenas no conteudo das 8 observacoes extra de inimigo. Em vez de 2 slots com `dx/dy/dist/danger`, o agente usa 4 enemy rays especializados. Cada ray adiciona:

- `enemyHitFlag`
- `enemyDistanceNormalized`

Total: `4 rays x 2 valores = 8 observacoes`.

Rays:

- `front_low`: frente, altura baixa/media-baixa, para detetar Android ao nivel do corpo/pernas;
- `front_mid`: frente, altura media, para detetar Android no caminho;
- `back_mid`: atras, altura media, para consciencia de Androids que ficaram para tras ou podem patrulhar de volta;
- `down_forward`: diagonal baixo/frente, para preparar uma futura fase Stomp/ScoreAttack em que cair em cima do Android podera ser util.

Nesta fase ainda nao ha stomp nem score: Android continua perigoso e contacto lateral/normal gera `EnemyHit`/reset. O `down_forward` existe so para preparar percecao futura, sem alterar mecanicas. A action mask usa apenas ameaca frontal (`front_low`/`front_mid`), para nao bloquear o avanco por causa de `back_mid`; o jump cue pode usar ameaca frontal ou `down_forward`.

Os rays usam `EdgeRunnerEnemyMarker` e tambem verificam bounds do collider do marker, para nao depender apenas de tag/layer. O Android precisa de `Collider2D` e `EdgeRunnerEnemyMarker` no proprio objeto ou num parent. Se `debugEnemyRayObservations=true`, os rays sao desenhados com gizmos e o log `[ENEMY RAYS]` mostra `frontHits`, `backHits`, `downHits` e distancias minimas sem spam por frame.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build AvoidanceMicroEnemyRays`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRays.unity`

Comando de treino do zero:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicroEnemyRays02 --force
```

### AvoidanceMicroEnemyRaysForcedJump

Fase tutorial criada depois de `AvoidanceMicroEnemyRays` confirmar que a percepcao do Android funciona, mas que PPO ainda nao descobria de forma consistente a acao de salto. O objetivo e isolar a competencia minima:

`ver Android a frente -> saltar enquanto avanca -> passar sem tocar`.

Esta fase mantem o contrato do modelo:

- Behavior Name: `EdgeRunnerV5Enemies`
- Observation Space Size: `63`
- Acoes: `[3, 2, 2]`

A diferenca e curricular: quando `front_low` ou `front_mid` detetam uma ameaca frontal proxima e o agente esta no chao/quase no chao, a action mask bloqueia apenas `NoJumpAction`. Isto obriga o branch de salto a escolher `JumpAction`.

A primeira versao ativava a forced mask tarde demais, com distancias perto de 2 unidades ou menos. A versao atual usa `enemyForcedJumpWindowX = 4.5` e `enemyAvoidanceWindowX = 4.5`, para forcar o salto mais cedo e ainda deixar tempo para passar por cima do `BoxCollider2D`.

A run `ForcedJump02` mostrou que forcar salto numa janela continua e larga ensina o agente a saltar, mas nao necessariamente a saltar no timing certo: ele pode saltar cedo demais, aterrar antes do Android e entrar em comportamento de pogo. A versao atual usa uma janela sweet spot:

- `enemyForcedJumpMinDistance = 2.6`
- `enemyForcedJumpMaxDistance = 3.8`
- `forceJumpOnlyOncePerEnemy = true`

Assim, se o ray frontal estiver acima de `3.8`, ainda e cedo demais e o salto nao e forcado. Se estiver abaixo de `2.6`, ja e tarde demais e a mask deixa de insistir no salto. Dentro da sweet spot, apenas `NoJumpAction` fica indisponivel. A fase continua a nao bloquear `right`, porque o objetivo e ensinar `right + jump`, nao travar o agente.

Importante:

- Nao mascara `right`, para permitir `right + jump`;
- Nao bloqueia `left`, `stop` nem `sprint`;
- Nao forca salto no ar;
- Nao fica a forcar saltos repetidos contra o mesmo Android no mesmo episodio;
- Nao usa `back_mid` para forcar salto;
- `down_forward` continua apenas como preparacao futura para Stomp/ScoreAttack;
- Nao ha stomp nem score nesta variante: Android continua perigo e contacto gera `EnemyHit`/reset.

Esta fase nao e a politica final. Serve para ensinar uma reacao basica em curriculo. Se funcionar, a ideia e fazer fine-tune de volta para a fase `AvoidanceMicroEnemyRays` sem forced-jump mask.

Diagnostico util com `debugTrainingActionStats=true`:

```text
[EA ACTIONS] decisions=... jumps=... jumpsNearEnemy=... forcedMask=... maskedNoJump=... jumpAttempts=... jumpsApplied=... jumpBlockedNotGrounded=... earlyJumps=... sweetSpotJumps=... lateJumps=...
```

Com `debugForcedJumpTiming=true`, o agente mostra:

```text
[ENEMY JUMP TIMING] dist=... zone=too_early/sweet_spot/too_late forced=... enemy=...
```

Interpretacao rapida:

- `forcedMask` e `maskedNoJump` aumentam, mas `jumps` nao aumenta: problema na aplicacao da mask/discrete actions;
- `forcedMask`, `maskedNoJump` e `jumps` aumentam, mas `jumpsApplied` fica 0: problema no grounded/coyote ou na aplicacao fisica do salto;
- `jumpsApplied` aumenta, mas o agente bate no Android: problema de timing/fisica/layout, nao de politica.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build AvoidanceMicroEnemyRaysForcedJump`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysForcedJump.unity`

Comando de treino do zero:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays_forced_jump.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicroEnemyRaysForcedJump01 --force
```

Comando recomendado apos a retunagem da janela para 4.5:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays_forced_jump.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicroEnemyRaysForcedJump02 --force
```

Comando recomendado apos a janela sweet spot:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays_forced_jump.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicroEnemyRaysForcedJump03 --force
```

Fine-tune sugerido se a fase funcionar:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays.yaml --run-id=ER_V5_EnemyAware_AvoidanceMicroEnemyRaysFT01 --initialize-from=ER_V5_EnemyAware_AvoidanceMicroEnemyRaysForcedJump01 --force
```

### AvoidanceMicroEnemyRaysSweetSpotStart

Fase ainda mais curta criada depois de `ForcedJump03`: nessa fase o agente ja via rays, a forced mask funcionava e a sweet spot aparecia em debug, mas a politica aprendeu a evitar aproximar-se da zona perigosa. `SweetSpotStart` remove o problema da aproximacao inicial e coloca o agente ja perto da distancia correta de salto.

Objetivo desta fase:

`spawn na sweet spot -> right + jump -> passar Android -> chegar ao Goal`.

Layout:

- spawn em `x = 2.8`, com Y ajustado para nascer grounded;
- Android perigoso estatico em `x = 6`;
- Goal em `x = 10.5`;
- plataforma plana e larga;
- sem gaps, score, stomp, HUD ou controller manual.

No spawn, o Android fica aproximadamente `3.2` unidades a frente do agente, dentro da sweet spot (`2.6` a `3.8`). A fase mantem `useEnemyRayObservations = true`, `forceJumpActionNearEnemy = true`, `forceJumpOnlyOncePerEnemy = true` e continua sem bloquear `right`.

Como e uma fase tutorial, os rewards sao menos punitivos e mais orientados para sucesso:

- `enemyHitPenalty = -8.0`;
- `enemyPassReward = 6.0`;
- `enemyJumpCueReward = 1.0`;
- `enemyApproachPenalty = 0.0`;
- `enemyDangerProximityPenalty = 0.0`;
- `disableProgressRewardNearEnemy = false`.

Tambem ha protecoes anti-fuga/stall so nesta fase:

- `enableRetreatPenalty = true`, com `retreatPenalty = -0.02` se recuar mais de `2.0` unidades para tras do spawn;
- `enableShortMicroTimeout = true`, com timeout de `6.0s` e `microTimeoutPenalty = -2.0` se ainda nao passou o Android.

Esta fase continua sem stomp/score. O `down_forward` permanece reservado para uma futura fase Stomp/ScoreAttack.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build AvoidanceMicroEnemyRaysSweetSpotStart`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysSweetSpotStart.unity`

Comando de treino:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays_sweet_spot_start.yaml --run-id=ER_V5_EnemyAware_SweetSpotStart01 --force
```

Se funcionar, fine-tune para a fase com spawn mais longe:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays_forced_jump.yaml --run-id=ER_V5_EnemyAware_ForcedJumpFT_FromSweetSpot01 --initialize-from=ER_V5_EnemyAware_SweetSpotStart01 --force
```

### AvoidanceMicroEnemyRaysJumpCommitTutorial

Fase tutorial mais guiada criada depois de `SweetSpotStart`: nessa fase o agente ja comecava na distancia correta, mas ainda podia fugir, parar ou andar para tras, aprendendo uma politica inutil em vez da sequencia essencial `right + jump`.

`JumpCommitTutorial` remove essa liberdade apenas dentro da sweet spot frontal. Quando `front_low` ou `front_mid` veem o Android entre `2.6` e `3.8` unidades, e o agente esta no chao/quase no chao, a action mask de commit:

- bloqueia `left`;
- bloqueia `stop`;
- deixa `right` disponivel;
- bloqueia `no-jump`;
- deixa `jump` disponivel;
- nao altera o branch de sprint.

Isto forca a combinacao `right + jump` nessa janela. O diagnostico seguinte mostrou que isso ja era aplicado corretamente, mas que o agente perdia `right` no ar quando o front ray deixava de ver o Android. Por isso esta fase tambem usa um `air commit` curto: depois de aplicar `right + jump`, mantem `right` no ar ate passar o Android ou ate acabar a duracao. Isto continua a ser uma ajuda curricular/tutorial, nao uma politica final.

A mask de commit inicial nao usa `back_mid` e nao usa `down_forward` sozinho. O `down_forward` continua reservado para uma fase futura de Stomp/ScoreAttack.

Layout:

- spawn em `x = 2.8`, com Y ajustado para nascer grounded;
- Android perigoso estatico em `x = 6`;
- Goal em `x = 10.5`;
- plataforma plana e larga;
- sem gaps, score, stomp, HUD ou controller manual.

Configuracao curricular da fase:

- `useEnemyRayObservations = true`;
- `enableJumpCommitMask = true`;
- `jumpCommitMinDistance = 2.6`;
- `jumpCommitMaxDistance = 3.8`;
- `forceJumpActionNearEnemy = false`;
- `maskForwardActionNearEnemy = false`;
- `disableProgressRewardNearEnemy = false`;
- `enemyJumpCueReward = 1.0`;
- `enemyPassReward = 8.0`;
- `enemyHitPenalty = -5.0`;
- `enableRetreatPenalty = true`;
- `retreatPenalty = -0.05`;
- `enableShortMicroTimeout = true`;
- `microTimeoutSeconds = 4.0`;
- `microTimeoutPenalty = -3.0`;
- `waitUntilGroundedOnEpisodeStart = true`;
- `episodeStartSettleMaxSeconds = 1.0`;
- `episodeStartSettleFreezeMovement = true`;
- `enableAirCommitAfterJump = true`;
- `airCommitDuration = 0.75`;
- `airCommitUntilEnemyPassed = true`.

Esta fase nao e a politica final. Serve para ensinar a trajetoria base. Se funcionar, a sequencia recomendada e fazer fine-tune primeiro para `SweetSpotStart` e depois para `ForcedJump` normal, removendo gradualmente a restricao mais forte.

Continuam a nao existir stomp nem score nesta variante EnemyAware: Android e perigo e contacto gera `EnemyHit`/reset.

Diagnostico util com `debugTrainingActionStats=true`:

```text
[EA ACTIONS] decisions=... jumps=... jumpsApplied=... jumpCommitMask=... jumpCommitApplied=... rightJumpActions=... airCommitStarts=... airCommitActiveSteps=... airCommitEndsPassed=... airCommitEndsDuration=... airCommitEndsHit=... retreatActions=... stallActions=... microTimeouts=... commitMaskActive=... commitMaskButMoveNotRight=... commitMaskButJumpNotSelected=... commitMaskRightJumpSelected=... commitMaskRightJumpApplied=...
```

Com `debugJumpCommitMask=true`, a fase deve mostrar:

```text
[ENEMY JUMP COMMIT MASK] forced right+jump dist=... enemy=...
[JUMP COMMIT MASK ACTIVE]
dist=...
masked branch0 left=true
masked branch0 stop=true
branch0 right available=true
masked branch1 noJump=true
branch1 jump available=true
[ENEMY JUMP COMMIT] right+jump applied dist=...
```

Com `debugAirCommit=true`, a fase deve mostrar:

```text
[ENEMY AIR COMMIT START] enemy=... duration=...
[ENEMY AIR COMMIT ACTIVE] remaining=... enemy=...
[ENEMY AIR COMMIT END] reason=passed/duration/hit/reset enemy=...
```

Se `JumpCommitTutorial` continuar a falhar, diagnosticar antes de criar novas fases:

1. Ativar `debugActionTrace=true` e deixar `debugActionTraceInterval=0.25`.
2. Se necessario, ativar `debugEpisodeStartSettle=true`.
3. Confirmar em `[EA TRACE]` que, enquanto `grounded=false`, `episodeStartSettling=true` e o agente nao avanca.
4. Confirmar que o primeiro trace util depois do settle mostra `grounded=true`, `episodeStartSettling=false` e `rbVel` sem velocidade horizontal inicial.
5. Confirmar que `[JUMP COMMIT MASK ACTIVE]` aparece dentro da sweet spot.
6. Confirmar em `[EA TRACE]` que `commitMaskActive=true`, `moveAction=2`, `jumpAction=1` e `jumpsApplied` aumenta.
7. Confirmar que `[ENEMY AIR COMMIT START]` aparece depois do `right + jump`.
8. Confirmar em `[EA TRACE]` que, mesmo quando `frontThreat=false` no ar, `airCommitActive=true`, `airCommitRemaining>0` e `moveAction=2`.
9. Confirmar que `rbVel.x` fica positivo quando `moveAction=2`; se aparecer `[EA PHYSICS WARNING]`, investigar colisao/layout.
10. Se a mask estiver correta mas a manobra continuar a falhar, ativar temporariamente `useRuleBasedCommitTest=true` apenas para teste de layout/fisica. Esse modo ignora a politica so durante o diagnostico: espera grounded antes de avancar; dentro da sweet spot aplica `right + jump`; durante o air commit mantem `right`; fora disso avanca para o Goal. Deve ficar `false` no treino normal.

Nota de diagnostico: foi descoberto que algumas fases micro nasciam na sweet spot em X, mas com o agente ainda no ar. Enquanto caia, a politica ja podia avancar para a direita; quando finalmente ficava grounded, ja estava demasiado perto do Android. Por isso as fases `AvoidanceMicroEnemyRays`, `ForcedJump`, `SweetSpotStart` e `JumpCommitTutorial` usam spawn Y grounded e `waitUntilGroundedOnEpisodeStart`. Recomeçar treino so depois de confirmar que o primeiro trace util tem `grounded=true`.

Nota de diagnostico adicional: depois de corrigir o spawn/settle, foi confirmado que `right + jump` era aplicado (`rbVel=(8.00,12.80)`), mas no ar o front ray podia deixar de ver o Android, a politica voltava a `stop`, e o agente caia contra o inimigo. O `air commit` existe so para manter o movimento horizontal nessa janela curta.

Antes de avançar para Stomp/ScoreAttack, validar esta manobra base: ver Android a frente, saltar, passar sem tocar e chegar ao Goal.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build AvoidanceMicroEnemyRaysJumpCommitTutorial`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysJumpCommitTutorial.unity`

Comando de treino:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays_jump_commit_tutorial.yaml --run-id=ER_V5_EnemyAware_JumpCommitTutorial02 --force
```

Fine-tune para SweetSpotStart se funcionar:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays_sweet_spot_start.yaml --run-id=ER_V5_EnemyAware_SweetSpotStartFT_FromCommit01 --initialize-from=ER_V5_EnemyAware_JumpCommitTutorial01 --force
```

Depois:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_avoidance_micro_enemy_rays_forced_jump.yaml --run-id=ER_V5_EnemyAware_ForcedJumpFT_FromSweetSpot01 --initialize-from=ER_V5_EnemyAware_SweetSpotStartFT_FromCommit01 --force
```

### StaticIntroEnemyRays

Proximo degrau depois de `AvoidanceMicroEnemyRaysFT_FromForcedJump01`. Esta fase aumenta a distancia inicial e remove as ajudas fortes usadas no tutorial micro, mas mantem `useEnemyRayObservations = true`. Isto e importante porque o modelo anterior aprendeu com EnemyRays: o observation space continua `63`, mas as ultimas 8 observacoes representam 4 enemy rays x 2 valores, nao os antigos valores abstratos `dx/dy/dist/danger`.

Layout:

- spawn em `x = 0`, com Y ajustado para nascer grounded;
- Android perigoso estatico em `x = 7.5`;
- Goal em `x = 15`;
- plataforma plana e larga;
- sem gaps, score, stomp, HUD ou controller manual.

Configuracao principal:

- `useEnemyRayObservations = true`;
- `enableEnemyAwareness = true`;
- `forceJumpActionNearEnemy = false`;
- `enableJumpCommitMask = false`;
- `enableAirCommitAfterJump = false`;
- `maskForwardActionNearEnemy = false`;
- `useRuleBasedCommitTest = false`;
- `disableProgressRewardNearEnemy = false`;
- `enemyHitPenalty = -8.0`;
- `enemyPassReward = 6.0`;
- `enemyJumpCueReward = 0.5`;
- `enemyApproachPenalty = 0.0`;
- `enemyDangerProximityPenalty = 0.0`;
- `rewardPassedEnemies = true`;
- `enableRetreatPenalty = true`;
- `retreatPenalty = -0.02`;
- `retreatEndDistance = 2.0`;
- `enableShortMicroTimeout = false`.

Esta fase ainda nao tem stomp nem score. O Android continua a ser perigo EnemyAware: contacto gera `EnemyHit`/reset.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build StaticIntroEnemyRays`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRays.unity`

Comando de treino recomendado:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro_enemy_rays.yaml --run-id=ER_V5_EnemyAware_StaticIntroEnemyRays01 --initialize-from=ER_V5_EnemyAware_AvoidanceMicroEnemyRaysFT_FromForcedJump01 --force
```

### StaticIntroEnemyRaysApproachGate

Fase intermédia criada depois de iniciar `StaticIntroEnemyRays01`: o modelo vindo de `AvoidanceMicroEnemyRaysFT_FromForcedJump01` ja sabia saltar o Android, mas ficou enviesado para saltar logo no inicio do episodio. Em `StaticIntroEnemyRays`, esse salto prematuro fazia o agente chegar mal ao Android e bater.

`ApproachGate` ensina a sequencia correta em duas partes:

- longe do Android, bloqueia `Branch 1 / jump` para evitar saltos prematuros;
- na sweet spot frontal (`2.6` a `3.8` unidades), permite salto e usa `JumpCommitMask` para aplicar `right + jump`;
- no ar, usa `AirCommit` curto para manter `right` ate passar o Android ou acabar a duracao.

Esta continua a ser uma ajuda curricular, nao a politica final. Se funcionar, a transicao seguinte deve ser fine-tune para `StaticIntroEnemyRays` sem `ApproachGate`, `JumpCommitMask` ou `AirCommit`.

Layout:

- spawn em `x = 0`, com Y ajustado para nascer grounded;
- Android perigoso estatico em `x = 7.5`;
- Goal em `x = 15`;
- plataforma plana e larga;
- sem gaps, score, stomp, HUD ou controller manual.

Configuracao principal:

- `useEnemyRayObservations = true`;
- `maskPrematureEnemyJumps = true`;
- `prematureJumpMinThreatDistance = 2.6`;
- `prematureJumpMaxThreatDistance = 3.8`;
- `enableJumpCommitMask = true`;
- `enableJumpCommitReward = true`;
- `enableAirCommitAfterJump = true`;
- `airCommitDuration = 0.75`;
- `airCommitUntilEnemyPassed = true`;
- `forceJumpActionNearEnemy = false`;
- `maskForwardActionNearEnemy = false`;
- `useRuleBasedCommitTest = false`;
- `disableProgressRewardNearEnemy = false`;
- `maskUselessJumps = false`;
- `enemyHitPenalty = -6.0`;
- `enemyPassReward = 8.0`;
- `enemyJumpCueReward = 1.0`;
- `jumpCommitReward = 1.0`;
- `enemyApproachPenalty = 0.0`;
- `enemyDangerProximityPenalty = 0.0`;
- `rewardPassedEnemies = true`;
- `enableRetreatPenalty = true`;
- `retreatPenalty = -0.02`;
- `retreatEndDistance = 2.0`;
- `enableShortMicroTimeout = true`;
- `microTimeoutSeconds = 8.0`;
- `microTimeoutPenalty = -3.0`.

Debug opcional:

- `debugPrematureJumpMask = true` deve mostrar `[ENEMY PREMATURE JUMP MASK]` quando o salto e bloqueado por `no_threat` ou `too_early`;
- `debugJumpCommitMask = true` deve mostrar o commit na sweet spot;
- `debugAirCommit = true` deve mostrar o commit horizontal no ar;
- `debugTrainingActionStats = true` inclui `prematureJumpMasks`, `prematureJumpAttempts` e `allowedSweetSpotJumps` em `[EA ACTIONS]`.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build StaticIntroEnemyRaysApproachGate`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRaysApproachGate.unity`

Comando de treino recomendado:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro_enemy_rays_approach_gate.yaml --run-id=ER_V5_EnemyAware_StaticIntroEnemyRaysApproachGate01 --initialize-from=ER_V5_EnemyAware_AvoidanceMicroEnemyRaysFT_FromForcedJump01 --force
```

Depois, se funcionar, fine-tune sem as ajudas:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro_enemy_rays.yaml --run-id=ER_V5_EnemyAware_StaticIntroEnemyRaysFT_FromApproachGate01 --initialize-from=ER_V5_EnemyAware_StaticIntroEnemyRaysApproachGate01 --force
```

Continuam a nao existir stomp nem score nesta variante EnemyAware: Android e perigo e contacto gera `EnemyHit`/reset.

### StaticIntroEnemyRaysPrematureAir

Fase intermédia criada depois de `StaticIntroEnemyRaysFT_FromApproachGate01`: ao remover `PrematureJumpMask`, `JumpCommitMask` e `AirCommit` ao mesmo tempo, o agente voltou ao vicio de saltar logo no inicio e chegar mal ao Android.

`PrematureAir` remove apenas a `JumpCommitMask`. O agente passa a ter de escolher o salto por si proprio, mas ainda:

- nao pode saltar longe do Android, porque `PrematureJumpMask` continua ligada;
- recebe `AirCommit` se executar um salto valido na sweet spot frontal;
- mantem `right` no ar depois desse salto valido para evitar cair contra o Android.

Isto torna a transicao mais gradual antes de voltar a `StaticIntroEnemyRays` sem ajudas.

Layout igual ao `ApproachGate`:

- spawn em `x = 0`, com Y ajustado para nascer grounded;
- Android perigoso estatico em `x = 7.5`;
- Goal em `x = 15`;
- plataforma plana e larga;
- sem gaps, score, stomp, HUD ou controller manual.

Configuracao principal:

- `useEnemyRayObservations = true`;
- `maskPrematureEnemyJumps = true`;
- `prematureJumpMinThreatDistance = 2.6`;
- `prematureJumpMaxThreatDistance = 3.8`;
- `enableJumpCommitMask = false`;
- `enableJumpCommitReward = false`;
- `enableAirCommitAfterJump = true`;
- `airCommitDuration = 0.75`;
- `airCommitUntilEnemyPassed = true`;
- `forceJumpActionNearEnemy = false`;
- `maskForwardActionNearEnemy = false`;
- `useRuleBasedCommitTest = false`;
- `disableProgressRewardNearEnemy = false`;
- `maskUselessJumps = false`;
- `enemyHitPenalty = -6.0`;
- `enemyPassReward = 8.0`;
- `enemyJumpCueReward = 1.0`;
- `jumpCommitReward = 0.0`;
- `enemyApproachPenalty = 0.0`;
- `enemyDangerProximityPenalty = 0.0`;
- `rewardPassedEnemies = true`;
- `enableRetreatPenalty = true`;
- `retreatPenalty = -0.02`;
- `retreatEndDistance = 2.0`;
- `enableShortMicroTimeout = true`;
- `microTimeoutSeconds = 8.0`;
- `microTimeoutPenalty = -3.0`.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build StaticIntroEnemyRaysPrematureAir`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRaysPrematureAir.unity`

Comando de treino recomendado:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro_enemy_rays_premature_air.yaml --run-id=ER_V5_EnemyAware_StaticIntroEnemyRaysPrematureAir01 --initialize-from=ER_V5_EnemyAware_StaticIntroEnemyRaysApproachGate01 --force
```

Depois, se funcionar, fine-tune sem as ajudas:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro_enemy_rays.yaml --run-id=ER_V5_EnemyAware_StaticIntroEnemyRaysFT_FromPrematureAir01 --initialize-from=ER_V5_EnemyAware_StaticIntroEnemyRaysPrematureAir01 --force
```

Continuam a nao existir stomp nem score nesta variante EnemyAware: Android e perigo e contacto gera `EnemyHit`/reset.

## Demonstracoes

A cena `StaticIntroForcedJump` ja fica preparada para gravar demonstracoes humanas se o pacote ML-Agents disponibilizar o componente `DemonstrationRecorder`. O recorder e adicionado ao `Player_V5_Enemies` gerado pelo builder com:

- `Record = false`
- `Demonstration Name = EnemyAware_ForcedJump_Demo`
- `Demonstration Directory = Assets/EdgeRunner/Demos`

Processo recomendado:

1. Gerar a cena no Unity com `EdgeRunner > Training > EnemyAware > Build StaticIntroForcedJump`.
2. Selecionar o `Player_V5_Enemies` da cena gerada.
3. Mudar `Behavior Type` para `Heuristic Only`.
4. No `Demonstration Recorder`, ativar `Record`.
5. Jogar 10 a 20 episodios bem-sucedidos, passando o Android sem tocar.
6. Parar Play Mode e confirmar o ficheiro `.demo` em `Assets/EdgeRunner/Demos/`.

Nota: esta versao do ML-Agents pode sanitizar e truncar o nome do ficheiro de demonstracao ao guardar. Se o ficheiro gerado nao se chamar exatamente `EnemyAware_ForcedJump_Demo.demo`, renomear o ficheiro ou atualizar o `demo_path` no YAML abaixo.

YAML com Behavioral Cloning:

`Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_enemies_static_intro_forced_jump_bc.yaml`

Comando de treino com BC inicializando da NavWarmup:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro_forced_jump_bc.yaml --run-id=ER_V5_EnemyAware_StaticIntroForcedJumpBC01 --initialize-from=ER_V5_EnemyAware_NavWarmup01 --force
```

Para testar BC do zero, remover `--initialize-from=ER_V5_EnemyAware_NavWarmup01` do comando.

### StaticIntro

Fase com 1 inimigo estatico facil em plataforma larga. O inimigo continua a ser perigo EnemyAware: contacto gera `EnemyHit`/reset. Nao ha stomp nem score nesta variante.

Gerar a cena no Unity:

`EdgeRunner > Training > EnemyAware > Build StaticIntro`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntro.unity`

Comando de treino:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies_static_intro.yaml --run-id=ER_V5_EnemyAware_StaticIntro01 --initialize-from=ER_V5_EnemyAware_NavWarmup01 --force
```

O `--initialize-from` e valido aqui porque NavWarmup e StaticIntro usam o mesmo Behavior Name `EdgeRunnerV5Enemies`, observation space `63` e branches `[3, 2, 2]`.

## Treino

Comando esperado:

```powershell
mlagents-learn Assets\EdgeRunner\ML\Config\V5\edgerunner_v5_enemies.yaml --run-id=ER_V5_EnemyAware_EnemyIntro01 --force
```

## Velocidade de treino

- Para treino no Editor, usar `time_scale: 10` como valor seguro inicial.
- Se a fisica continuar estavel, pode-se testar `time_scale: 15`.
- Evitar valores muito altos sem validacao, porque podem afetar colisoes e saltos.
- Quando uma fase ja atinge sucesso consistente, nao e necessario treinar ate ao `max_steps`; guardar o modelo e avancar para a fase seguinte.

## Nota sobre modelos V5 existentes

`EdgeRunnerV5Enemies` usa observation space `63`. Nao inicializar diretamente a partir de modelos V5 antigos com observation space `55`.

## Proximos passos

- Validar que `debugEnemyObservations=true` mostra os inimigos detetados.
- Confirmar que contacto perigoso termina episodio com `EnemyHit`.
- Separar, numa fase seguinte, uma variante ScoreAttack com stomp, score e recolha de Energy Cells.
