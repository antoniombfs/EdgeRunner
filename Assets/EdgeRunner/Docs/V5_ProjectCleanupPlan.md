# EdgeRunner V5 Project Cleanup Plan

Documento de auditoria seguro. Nao apagar nem mover ficheiros automaticamente a partir deste plano.

## Regras

- Nao apagar modelos antigos.
- Nao apagar `results/` sem autorizacao explicita.
- Nao mexer na demo manual estavel `Assets/EdgeRunner/Scenes/Prototypes/ER_V5_DemoScene.unity`.
- Nao mexer no prefab original `Player_V5`.
- Nao mexer em V3/V4.
- Manter `.meta` junto de qualquer asset se um arquivo manual for feito mais tarde.
- Preferir documentar como experimental em vez de apagar.

## Cenas principais atuais

### Manter como principais

- `Assets/EdgeRunner/Scenes/Prototypes/ER_V5_DemoScene.unity`
  - Demo manual estavel.
  - Nao alterar durante a consolidacao das variantes finais.
- `Assets/EdgeRunner/Scenes/Train/TrainingScene_V5_Mixed01.unity`
  - Cena historica V5 mixed.
  - Candidata a comparacao/relatorio.
- `Assets/EdgeRunner/Scenes/Train/ER_V5_GoalRelative_V5Gen_EasyPlus01.unity`
  - Cena V5 GoalRelative antiga.
  - Manter para comparacao ate a linha GoalRunnerRandom estar validada.
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRaysPrematureAir.unity`
  - Melhor fase EnemyAware atual segundo os testes recentes.
  - Manter como evidencia de action masking de seguranca.

### Cenas novas de entrega

Geradas pelo menu `EdgeRunner > Training > V5`.

- `Assets/EdgeRunner/Scenes/Training/ER_V5_GoalRunnerRandom.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunnerRandom.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackIntro.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackEasy.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackRandomControlled.unity`

### EnemyAware experimental / curriculo

Manter para relatorio e comparacao, mas nao tratar como ScoreAttack.

- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_NavWarmup.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicro.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroMasked.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRays.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysForcedJump.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysSweetSpotStart.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysJumpCommitTutorial.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntro.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroJumpCue.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroForcedJump.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRays.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRaysApproachGate.unity`
- `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRaysPrematureAir.unity`

### Candidatas a arquivo mais tarde

Nao mover agora. Arquivar apenas depois de confirmar que nao entram no relatorio.

- `Assets/EdgeRunner/Scenes/Recovery/0.unity`
- `Assets/EdgeRunner/Scenes/Prototypes/SampleScene.unity`
- `Assets/EdgeRunner/Scenes/Prototypes/Prototype01.unity`
- `Assets/EdgeRunner/Scenes/Prototypes/Level_Prototype.unity`
- Cenas antigas V1/V2/V3/V4 em `Assets/EdgeRunner/Scenes/Test`, `Train` e `Validation` que nao forem usadas em comparacao.

## YAMLs principais

### Novos para entrega

- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_goalrunner_random.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_speedrunner_random.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoreattack_intro.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoreattack_easy.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoreattack_random_controlled.yaml`

### Manter

- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_basic.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_enemies_navwarmup.yaml`
- `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_enemies_static_intro_enemy_rays_premature_air.yaml`
- YAMLs EnemyAware usados no relatorio para mostrar curriculo e tentativas.

### Experimentais / obsoletos

- YAMLs EnemyAware antigos que falharam sem EnemyRays ou sem masking suficiente.
- YAMLs de V1/V2/V3 mantidos apenas para historico.

## Scripts principais

### Agentes

- `Assets/EdgeRunner/Scripts/Agents/EdgeRunnerAgentV5.cs`
  - Base de GoalRunner, SpeedRunner e ScoreAttack.
  - Recebeu apenas hooks opcionais para ScoreAttack.
- `Assets/EdgeRunner/Scripts/Agents/EdgeRunnerAgentV5EnemyAware.cs`
  - Linha EnemyAware com observation space 63 e EnemyRays.
  - Nao e ScoreAttack.
- `Assets/EdgeRunner/Scripts/Agents/EdgeRunnerAgentV5EnemiesTransfer.cs`
  - Variante experimental de transferencia.
  - Manter para referencia.
- `Assets/EdgeRunner/Scripts/Agents/EdgeRunnerAgentV3.cs`, `EdgeRunnerAgentV2.cs`, `EdgeRunnerAgent.cs`
  - Nao mexer.

### Builders

- `Assets/EdgeRunner/Editor/Demo/BuildER_V5_DemoScene.cs`
  - Demo manual estavel.
  - Nao alterar nesta consolidacao.
- `Assets/EdgeRunner/Editor/Training/BuildER_V5_EnemiesTrainScene.cs`
  - Builder EnemyAware.
  - Manter para curriculo EnemyRays/PrematureAir.
- `Assets/EdgeRunner/Editor/Training/BuildER_V5_FinalVariants.cs`
  - Builder novo das linhas GoalRunner, SpeedRunner e ScoreAttack.

### Gameplay / ScoreAttack

- `Assets/EdgeRunner/Scripts/Game/ScoreAttackManager.cs`
- `Assets/EdgeRunner/Scripts/Game/ScoreAttackCoin.cs`
- `Assets/EdgeRunner/Scripts/Game/ScoreAttackAndroid.cs`
- `Assets/EdgeRunner/Scripts/Game/ScoreAttackGoalLock.cs`

### Demo manual

- `Assets/EdgeRunner/Scripts/Demo/DemoManualPlayerController.cs`
- `Assets/EdgeRunner/Scripts/Demo/StompableAndroidEnemy.cs`
- `Assets/EdgeRunner/Scripts/Demo/StompableAndroidStompZone.cs`
- `Assets/EdgeRunner/Scripts/Demo/StompableAndroidSideHazard.cs`
- `Assets/EdgeRunner/Scripts/Game/EdgeRunnerScoreManager.cs`

Manter. Estes scripts suportam a demo manual, nao devem ser confundidos com ScoreAttack de treino.

## Modelos ONNX candidatos

### Manter como bons/candidatos

- `Assets/EdgeRunner/ML/Models/Candidates/ER_V5_GoalRelative_V5Gen_EasyStart02_Final_Test.onnx`
- `Assets/EdgeRunner/ML/Models/Candidates/ER_V5_GoalRelative_V5Gen_EasyBridge01_Final_Test.onnx`
- `Assets/EdgeRunner/ML/Models/Candidates/ER_V5_EnemyAware_NavWarmup01.onnx`
- `Assets/EdgeRunner/ML/Models/Candidates/ER_V5_EnemyAware_AvoidanceMicroEnemyRaysFT_FromForcedJump01.onnx`
- `Assets/EdgeRunner/ML/Models/Candidates/ER_V5_EnemyAware_StaticIntroEnemyRaysApproachGate01.onnx`
- `Assets/EdgeRunner/ML/Models/Candidates/ER_V5_EnemyAware_StaticIntroEnemyRaysPrematureAir01.onnx`
- `Assets/EdgeRunner/ML/Models/Candidates/ER_V4_EasierPlus_Coyote01_Final_Test.onnx`

### Experimentais ou falhados

- Modelos em `Assets/EdgeRunner/ML/Models/Archive/Failed_*`.
- Modelos V1/V2 antigos em `Archive` e `Runtime`.

Nao apagar. Sao uteis para relatorio, comparacao e fallback.

## Safe to archive later

Arquivar apenas com confirmacao manual:

- Timers antigos em `Assets/ML-Agents/Timers/*`.
- Cenas de prototipo nao usadas.
- Cenas Recovery.
- YAMLs de fases EnemyAware que foram abandonadas, se ja estiverem descritas no relatorio.
- Modelos falhados duplicados, se a respetiva run ja estiver documentada.

## Recomendacao final

Para a entrega, deixar visiveis apenas estas linhas no relatorio:

- GoalRunner: V5 base + `MixedLevelGenerator`.
- SpeedRunner: V5 base + rewards de tempo/velocidade.
- ScoreAttack: V5 base + objetivos obrigatorios de moedas/Androids + Goal bloqueado.

EnemyAware deve ser apresentado como experiencia secundaria de avoidance/action masking, nao como ScoreAttack.
