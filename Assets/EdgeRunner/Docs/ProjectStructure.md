# EdgeRunner Project Structure

## Scripts

`Assets/EdgeRunner/Scripts/Agents`
- Agentes ML-Agents.
- `EdgeRunnerAgentV2.cs` e a baseline estavel.
- `EdgeRunnerAgentV3.cs` e experimental e deve ser ligada manualmente em prefabs/cenas novas ou duplicadas.

`Assets/EdgeRunner/Scripts/Environment`
- Geracao e triggers de treino.
- `GapGenerator.cs` cria episodios procedurais.
- `GoalTrigger.cs`, `TrainingGoal.cs`, `TrainingDeathZone.cs` notificam agentes quando chegam ao goal ou caem.

`Assets/EdgeRunner/Scripts/Gameplay`
- Controlos e camara do modo manual.
- Nao misturar mecanicas humanas com recompensas de treino sem motivo claro.

`Assets/EdgeRunner/Scripts/UI`
- UI do modo manual e debug.

`Assets/EdgeRunner/Scripts/Utils`
- Utilitarios como controlo de velocidade da simulacao.

## ML

`Assets/EdgeRunner/ML/Config`
- Configuracoes antigas mantidas no sitio por seguranca.
- Novas configuracoes devem ficar em subpastas por versao, por exemplo `Config/V3`.

`Assets/EdgeRunner/ML/Models/Runtime`
- Apenas modelos escolhidos para demonstracao/build.
- Evitar substituir um runtime sem registar a decisao.

`Assets/EdgeRunner/ML/Models/Candidates`
- Modelos promissores que ainda precisam de validacao.

`Assets/EdgeRunner/ML/Models/Archive`
- Modelos antigos, falhados ou substituidos.

`Assets/EdgeRunner/ML/Models/V2_Baseline`
- Pasta reservada para baselines V2, se forem movidas manualmente com `.meta`.

`Assets/EdgeRunner/ML/Models/V3_Experimental`
- Pasta reservada para variantes e comparacoes da V3.

## Docs

`Assets/EdgeRunner/Docs/ProjectStructure.md`
- Este documento.

`Assets/EdgeRunner/Docs/V3_TrainingPlan.md`
- Curriculum inicial da V3.

`Assets/EdgeRunner/Docs/ModelRegistry.md`
- Historico dos modelos importantes e resultados conhecidos.

## Regras de compatibilidade

- Nao apagar `EdgeRunnerAgentV2.cs`.
- Nao substituir modelos V2 por modelos V3.
- Nao usar modelos V2 em `EdgeRunnerAgentV3`, porque o vetor de observacao mudou.
- Nao usar modelos V3 em `EdgeRunnerAgentV2`.
- Mudar `Behavior Name` sempre que a arquitetura de observacoes muda.
- Validar `Vector Observation Space Size` no Inspector antes de treinar.

## Regras para mover assets Unity

- Mover sempre o ficheiro e o `.meta` correspondente juntos.
- Se nao houver certeza sobre referencias em scenes/prefabs, deixar o asset no sitio e documentar a sugestao.
- Evitar mover scenes e prefabs fora do Unity Editor.
- Depois de qualquer reorganizacao, abrir a cena no Unity e procurar missing scripts/references.

## Politica de modelos

- `Runtime`: modelo ativo para demonstracao.
- `Candidates`: modelo bom o suficiente para revalidar.
- `Archive`: modelo mau, antigo ou substituido.
- `V2_Baseline`: baselines V2 preservadas.
- `V3_Experimental`: testes V3 antes de promover para candidate/runtime.
