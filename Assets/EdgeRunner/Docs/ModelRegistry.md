# EdgeRunner Model Registry

## V2 Baseline

Modelo:
`ER_V2_Gap06Like03_Smooth01_3961_Candidate`

Configuracao:
- Min Gaps = 6
- Max Gaps = 6
- Min Platform Width = 2.9
- Max Platform Width = 4.5
- Min Gap Width = 2.95
- Max Gap Width = 3.5

Resultado:
`19/20`

Notas:
- Baseline estavel.
- Nao mexer.
- Nao usar para V3 porque a V3 tera observacoes diferentes.
- Manter como fallback/demonstracao de locomocao gap-only.

## V3 Experimental

Espaco reservado para futuros modelos V3.

Registar cada modelo com:
- Nome do modelo.
- Run-id.
- Config YAML.
- Fase do curriculum.
- Parametros do GapGenerator.
- Resultado em 20 tentativas.
- Observacoes sobre falhas.
- Decisao: `Runtime`, `Candidate`, `V3_Experimental` ou `Archive`.
