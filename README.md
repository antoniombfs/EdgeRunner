# EdgeRunner

## Versão de entrega

A versão final de entrega deste projeto corresponde à tag **`v1.0-final-source-clean-v3`** (branch `final-source-clean`). Este repositório deve ser avaliado a partir dessa tag, e não a partir do estado geral de outras branches ou de protótipos mais antigos.

A branch `final-source-clean` parte da versão final fechada (tag `v1.0-final-submission-final`) e remove apenas conteúdo comprovadamente não utilizado pela demo (relatórios de avaliação, documentação interna de desenvolvimento, ferramentas auxiliares, telemetria automática do ML-Agents e um material de física não referenciado), sem alterar cenas, scripts ou modelos usados pela demo final.

## Sobre a demo

A demo final é **agent-first**: por defeito, a personagem é controlada pelo agente treinado (V5), a correr em modo de *inference* a partir de um modelo ONNX. Existe também um **modo Manual**, selecionável no menu, mas que serve apenas como **ferramenta auxiliar de validação** de mecânicas (colisões, power cells, Androids, goal lock, DeathZone) — não é o modo de utilização principal do projeto.

## Onde está a demo final

As cenas e scripts relevantes para a demo final de entrega encontram-se em:

- `Assets/EdgeRunner/Scenes/DemoFinal/`
- `Assets/EdgeRunner/Scripts/Demo/`
- `Assets/EdgeRunner/Editor/Demo/BuildER_FinalDemo.cs`

A build local usada na entrega foi `Builds/EdgeRunner_FinalDemo_Windows_manual_control_hotfix12.zip`.

## Sobre o resto do repositório

O repositório contém ainda protótipos, cenas de treino, modelos candidatos e código experimental, mantidos como **histórico técnico do processo de investigação e treino por aprendizagem por reforço** (evolução do agente até à versão V5, experiências de geração de níveis, avaliação de modelos, etc.). Este conteúdo não é removido, porque o projeto não usa `.asmdef` (todos os scripts de runtime compilam numa única unidade) e alguns scripts aparentemente antigos são, na prática, dependências de compilação de scripts usados pela demo final. Este conteúdo **não faz parte da demo final de entrega** — apenas o conteúdo indicado na secção anterior é relevante para essa avaliação.

## Relatório

O relatório do projeto é entregue separadamente, em formato PDF.
