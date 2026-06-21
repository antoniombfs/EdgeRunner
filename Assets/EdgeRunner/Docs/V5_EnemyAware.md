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

Gerar a cena no Unity:

`EdgeRunner > Training > Build ER_V5_Enemies_Train`

Cena gerada:

`Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_Train.unity`

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
