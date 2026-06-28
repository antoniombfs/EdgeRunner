# EdgeRunner Project Status

Estado auditado em 2026-06-27. Este documento classifica assets; nao autoriza apagar,
mover ou substituir ficheiros. `results/` nao foi inspecionado nem alterado.

## Stable / Final Candidates

### SpeedRunner V5

- Agente: `Assets/EdgeRunner/Scripts/Agents/EdgeRunnerAgentV5.cs`.
- Prefab protegido: `Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab`.
- Contrato: Behavior `EdgeRunnerV5`, 55 observacoes, actions `[3,2,2]`.
- Demo estavel: `Assets/EdgeRunner/Scenes/Prototypes/ER_V5_DemoScene.unity`.
- Cena final validada: `Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunnerFinalRandom.unity`.
- Modelo final principal: `Assets/EdgeRunner/ML/Models/FinalCandidates/FINAL_GoalRunner_Base_EasyBridge.onnx`.
- Modelo de origem/fallback validado: `Assets/EdgeRunner/ML/Models/Candidates/ER_V5_GoalRelative_V5Gen_EasyBridge01_Final_Test.onnx`.

### FinalCandidates

- Todo o conteudo de `Assets/EdgeRunner/ML/Models/FinalCandidates/` esta protegido.
- Os modelos ScoreMax e EnemyAware nesta pasta representam resultados preservados
  das respetivas linhas experimentais; nao implicam compatibilidade com ObjectAware.
- Nunca mover ou substituir um `.onnx` sem o respetivo `.onnx.meta` e confirmacao explicita.

## ObjectAware Active Development

- Novo agente: `Assets/EdgeRunner/Scripts/Agents/EdgeRunnerAgentV5ScoreMaxObjectAware.cs`.
- Behavior Name: `EdgeRunnerV5ScoreMaxObjectAware`.
- Contrato inicial: 111 observacoes e actions `[3,2,2]`.
- Blocos: V5 base (55), estado global (8), nextObjective (10), moeda baixa (6),
  moeda alta (6), Android/stomp (12) e contexto de salto/gap (14).
- Primeira cena: `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_TraversalBase.unity`.
- Primeiro YAML: `Assets/EdgeRunner/ML/Config/ObjectAware/scoremax_oa_traversal_base.yaml`.
- TraversalBase nao contem moedas nem Androids; valida locomocao, gaps e Goal no agente novo.
- `ScoreMaxOA LowCoinRun`: duas moedas baixas apanhaveis a correr, sem Androids;
  cena `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_LowCoinRun.unity` e
  YAML `Assets/EdgeRunner/ML/Config/ObjectAware/scoremax_oa_low_coin_run.yaml`.
- `ScoreMaxOA HighCoinJump`: duas moedas altas para ensinar timing de salto, sem Androids;
  cena `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_HighCoinJump.unity` e
  YAML `Assets/EdgeRunner/ML/Config/ObjectAware/scoremax_oa_high_coin_jump.yaml`.
- `ScoreMaxOA StaticAndroidAvoid`: um Android estatico funciona como perigo opcional;
  o objetivo principal continua a ser o Goal, contacto lateral termina o episodio e
  stomp e permitido sem ser obrigatorio. Cena
  `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_StaticAndroidAvoid.unity` e YAML
  `Assets/EdgeRunner/ML/Config/ObjectAware/scoremax_oa_static_android_avoid.yaml`.
  Esta fase deve ser treinada a partir de `TraversalBase02`.
- `ScoreMaxOA StaticAndroidStomp`: um Android estatico e objetivo obrigatorio;
  o agente deve aproxima-lo, mata-lo por stomp e so depois seguir para o Goal bloqueado.
  Cena `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_StaticAndroidStomp.unity`
  e YAML `Assets/EdgeRunner/ML/Config/ObjectAware/scoremax_oa_static_android_stomp.yaml`.
  Deve ser treinada preferencialmente a partir de `TraversalBase02`, nao de
  `StaticAndroidAvoid01`, para nao herdar a politica de simplesmente saltar por cima.
- `ScoreMaxOA MixedWarmup`: fase controlada de juncao com a sequencia obrigatoria
  LowCoin -> HighCoin -> AndroidStomp -> Goal. Cena
  `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_MixedWarmup.unity` e YAML
  `Assets/EdgeRunner/ML/Config/ObjectAware/scoremax_oa_mixed_warmup.yaml`.
  Deve ser treinada apenas depois das fases individuais ObjectAware estarem validadas.
- `ScoreMaxOA MixedRandomWarmup`: fase intermédia de randomizacao controlada com
  a sequencia LowCoin -> HighCoin -> AndroidStomp -> Goal, plataforma segura e
  posicoes variadas em runtime a cada episodio. Cena
  `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_MixedRandomWarmup.unity` e YAML
  `Assets/EdgeRunner/ML/Config/ObjectAware/scoremax_oa_mixed_random_warmup.yaml`.
  Deve ser treinada a partir de `MixedWarmup01`, antes de `FinalRandom`.
- `ScoreMaxOA FinalRandom`: primeira fase final de randomizacao controlada com
  1-2 moedas baixas, 1-2 moedas altas, gaps simples/medios, plataformas largas de
  recuperacao e um Android estatico obrigatorio antes do Goal bloqueado. Cena
  `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_FinalRandom.unity` e YAML
  `Assets/EdgeRunner/ML/Config/ObjectAware/scoremax_oa_final_random.yaml`.
  Deve ser treinada a partir de `MixedRandomWarmup02`; esta primeira versao ainda
  nao inclui Android movel.
- LowCoinRun e HighCoinJump so devem ser treinadas depois de TraversalBase estar
  validada e treinada; ambas mantem Behavior `EdgeRunnerV5ScoreMaxObjectAware`,
  111 observacoes e actions `[3,2,2]`.
- `ScoreMaxObjectAware` nao deve inicializar a partir de modelos ScoreMax antigos de
  83 observacoes, nem de qualquer outro modelo com contrato/semantica incompativel.
- `SpeedRunnerObjectAware` permanece como futura variante de navegacao com percecao de objetos.

## Experimental / Legacy

### ScoreMax atual, 83 observacoes

- Agente: `Assets/EdgeRunner/Scripts/Agents/EdgeRunnerAgentV5ScoreMax.cs`.
- Contrato preservado: Behavior `EdgeRunnerV5ScoreMax`, 83 observacoes, actions `[3,2,2]`.
- Cenas: `Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMax*.unity`.
- YAMLs: `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_scoremax_*.yaml`.
- Inclui CoinIntro, StompIntro, Intro, Easy, RandomWarmup e FinalRandom.
- Estado: funcional e preservado como referencia/prototipo; nao e a futura arquitetura ObjectAware.

### EnemyAware antigo, 63 observacoes

- Agentes: `EdgeRunnerAgentV5EnemyAware.cs` e `EdgeRunnerAgentV5EnemiesTransfer.cs`.
- Prefab: `Assets/EdgeRunner/Prefabs/Agent/Player_V5_Enemies.prefab`.
- Contrato: Behavior `EdgeRunnerV5Enemies`, 63 observacoes, actions `[3,2,2]`.
- Cenas: `Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_*.unity`.
- YAMLs: `Assets/EdgeRunner/ML/Config/V5/edgerunner_v5_enemies*.yaml`.
- Inclui NavWarmup, AvoidanceMicro, Masked, EnemyRays, ForcedJump,
  JumpCommitTutorial, SweetSpotStart, StaticIntro e variantes de diagnostico.
- Estado: Legacy/Prototype. Manter, nao apagar e nao usar como base automatica para ObjectAware.

### Outros assets antigos ou experimentais

- Predecessor ScoreAttack: cenas `ER_V5_ScoreAttack*.unity` e YAMLs
  `edgerunner_v5_scoreattack_*.yaml`.
- Geradores/cenas V5 anteriores: `ER_V5_GoalRunnerRandom`, `ER_V5_SpeedRunnerRandom`
  e `Scenes/Train/ER_V5_GoalRelative_V5Gen_EasyPlus01.unity`.
- Cenas antigas em `Scenes/Train/`, `Scenes/Test/`, `Scenes/Validation/` e `Scenes/Recovery/`.
- Configs gerais antigas: `ML/Config/edgerunner_basic.yaml`,
  `edgerunner_finetune.yaml`, `edgerunner_stabilize.yaml` e `V5/edgerunner_v5_basic.yaml`.
- Modelos em `ML/Models/Archive/`, `Runtime/`, `V2_Baseline/` e `V3_Experimental/`.
- `ML/Models/Candidates/` contem candidatos preservados; o modelo EasyBridge citado
  na secao Stable e especialmente protegido.
- Nao mover estes assets para `ML/Models/Legacy/` sem revisao e confirmacao explicita.

## Do Not Touch

- V3 e V4: scripts, prefabs, scenes, YAMLs e modelos.
- `Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab`.
- `Assets/EdgeRunner/Scenes/Prototypes/ER_V5_DemoScene.unity`.
- `Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunnerFinalRandom.unity`.
- `Assets/EdgeRunner/ML/Models/FinalCandidates/`.
- Os dois modelos EasyBridge indicados na secao Stable.
- `jumpForce`, Observation Spaces, Actions e Behavior Names existentes.
- Qualquer ficheiro `.onnx` ou `.onnx.meta`.
- `results/`.

## Training Runs Recommended / Ignored

### Recommended

- Para demonstracao SpeedRunner estavel, usar
  `FINAL_GoalRunner_Base_EasyBridge.onnx` com `EdgeRunnerV5` (55 observacoes).
- Para reproduzir a linha ScoreMax 83 existente, manter a sequencia curricular
  CoinIntro -> StompIntro -> Intro -> Easy -> RandomWarmup -> FinalRandom.
- Os modelos `FINAL_ScoreMax_CoinIntro03`, `FINAL_ScoreMax_StompIntro01`,
  `FINAL_ScoreMax_Intro01`, `FINAL_ScoreMax_Easy03` e
  `FINAL_ScoreMax_RandomWarmup02` podem servir de checkpoints dessa linha,
  mas nao devem inicializar ObjectAware sem contrato compativel confirmado.

### Ignored for new ObjectAware training

- Runs EnemyAware `ER_V5_EnemyAware_*`, incluindo variantes ForcedJump,
  EnemyRays, JumpCommit, PrematureAir, ApproachGate e SweetSpot.
- Runs ScoreMax FinalRandom anteriores que praticaram saltos repetidos ou objetivos falhados.
- Runs antigas V1/V2/V3/V4, `Failed_*`, `Old_*` e modelos em `ML/Models/Archive/`.
- Runs ScoreAttack anteriores ao contrato ScoreMax 83.
- "Ignored" significa nao selecionar como base da nova linha; nada deve ser apagado.

## Configuration Audit

- `Player_V5.prefab`: Behavior `EdgeRunnerV5`, 55 observacoes, branches `[3,2,2]`,
  Behavior Type `Default` e Model `None`.
- `ER_V5_SpeedRunnerFinalRandom.unity` herda esse contrato do prefab.
- Cenas ScoreMax auditadas substituem apenas Behavior por `EdgeRunnerV5ScoreMax`
  e observacoes por 83; mantem branches `[3,2,2]` e Model `None` para treino.
- `Player_V5_Enemies.prefab`: Behavior `EdgeRunnerV5Enemies`, 63 observacoes,
  branches `[3,2,2]`, Behavior Type `Default` e Model `None`.
- Flags de diagnostico persistentes foram desligadas nas cenas de treino
  ScoreMaxCoinIntro e ScoreMaxFinalRandom; os sistemas de debug continuam disponiveis.
