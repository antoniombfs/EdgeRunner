# EdgeRunner Reorganization Plan
Date: 2026-04-02

## Current repository notes
- The Unity project already has local uncommitted changes in scenes, prefab, YAML, and scripts.
- Because of that, reorganization inside `Assets/` should be done from the Unity Editor, not from File Explorer or PowerShell.
- The runtime model references found in scenes currently point to V1 assets under `Assets/EdgeRunner/ML/Models/V1`.
- `Assets/Demonstrations/GapJump.demo` is referenced both by the ML config and by the `DemonstrationRecorder` on `Player.prefab`.
- There are duplicate or stray scenes outside the main scene tree:
  - `Assets/TrainingScene_Flat.unity`
  - `Assets/Scenes/TrainingScene_Flat.unity`
  - `Assets/_Recovery/0.unity`

## 1. Final target structure
```text
Assets/
  EdgeRunner/
    Docs/
      ExperimentLogs/
      TrainingNotes/
    ML/
      Config/
      Models/
        Runtime/
        Candidates/
        Archive/
      Notes/
        Timers/
    Prefabs/
      Agent/
      Environment/
      UI/
    Scenes/
      Prototype/
      Train/
      Validation/
      Test/
      Recovery/
    Scripts/
      Agents/
      Environment/
      Gameplay/
      Utils/

Assets/
  Demonstrations/                 # keep here for now

results/
  active/
  archive/
    2026-04-pre-reorg/
```

## 2. What to move or rename
- Move `Assets/EdgeRunner/ML/Notes/*.json` into `Assets/EdgeRunner/ML/Notes/Timers/`.
- Keep only currently used `.onnx` models in `Assets/EdgeRunner/ML/Models/Runtime/`.
- Move experimental V2 `.onnx` files into `Assets/EdgeRunner/ML/Models/Candidates/`.
- Move superseded `.onnx` files into `Assets/EdgeRunner/ML/Models/Archive/`.
- Consolidate the duplicate `TrainingScene_Flat.unity` after comparing both copies.
- Move `Assets/_Recovery/0.unity` to `Assets/EdgeRunner/Scenes/Recovery/` if you want to keep it.

## 3. Do not touch or touch carefully
- Do not move assets inside `Assets/` from PowerShell or Explorer.
- Do not rename the behavior `EdgeRunnerBasic` unless you update both:
  - `Assets/EdgeRunner/ML/Config/edgerunner_basic.yaml`
  - `Behavior Parameters` in scenes and prefab
- Do not move `Assets/Demonstrations/GapJump.demo` yet.
- Validate these references before any model rename:
  - `TrainingScene_Flat.unity` -> `EdgeRunnerBasic.onnx`
  - `EdgeRunnerBasic_Height01.unity` -> `EdgeRunnerBasic_Height01_jumpPenalty_01.onnx`
  - `TrainingScene_MiniLevel01_Easy.unity` -> `EdgeRunnerBasic_MiniLevel01_Good.onnx`
  - `TrainingScene_MiniLevel02.unity` -> `EdgeRunnerBasic_MiniLevel01_Good.onnx`
  - `TrainingScene_MiniLevel03.unity` -> `EdgeRunnerBasic_MiniLevel03_Good.onnx`
  - `TrainingScene_SafeDrop01.unity` -> `EdgeRunnerBasic_SafeDrop_Good.onnx`

## 4. Naming convention
- Runs: `ER_{AgentVersion}_{Task}_{Method}_{RunNN}`
- Runtime models: `ER_{AgentVersion}_{Task}_Runtime.onnx`
- Candidate models: `ER_{AgentVersion}_{Task}_Candidate.onnx`
- Demos: `Demo_{Task}_{Variant}.demo`
- Avoid names like `Good`, `Final`, `Teste`, `Novo`, `Copia`

## 5. Exact reorganization steps
1. Commit or back up the project first.
2. Close Unity.
3. Reorganize only `results/` from PowerShell.
4. Reopen Unity.
5. Create `Runtime`, `Candidates`, `Archive`, and `Timers` folders inside `Assets/EdgeRunner`.
6. Move `timers.json` files in Unity.
7. Review which `.onnx` files are actively assigned in scenes.
8. Move only unreferenced candidate models first.
9. Compare duplicate `TrainingScene_Flat` scenes and pick one canonical copy.
10. Move `_Recovery/0.unity` into `Scenes/Recovery` or delete it later after comparison.
11. Only then rename ambiguous model names one by one in Unity.
12. Open each main scene and verify `Behavior Parameters`.

## 6. What to do in the Unity Editor
- Create the new folders under `Assets/EdgeRunner/ML/Models` and `Assets/EdgeRunner/ML/Notes`.
- Move all `.onnx`, `.demo`, `.unity`, and `.json` assets from inside Unity.
- Check `Player.prefab` and each scene for:
  - `Behavior Parameters > Model`
  - `Behavior Name`
  - `Demonstration Recorder`
- Decide whether `TrainingScene_MiniLevel02.unity` intentionally reuses `EdgeRunnerBasic_MiniLevel01_Good.onnx`.

## 7. What to do in PowerShell
- Use PowerShell only for `results/` housekeeping.
- Recommended keep shortlist:
  - `EdgeRunner_V2_Gap01_04`
  - `EdgeRunnerBasic_Gap02_BC_01`
  - `EdgeRunnerBasic_Gap03_Bridge01`
  - `EdgeRunnerBasic_Gap03_From50092_02`
  - `EdgeRunnerBasic_GapDropBridge_02`
  - `EdgeRunnerBasic_GapJump_BC_05`
  - `EdgeRunnerBasic_SafeDrop_01`
- Recommended archive shortlist:
  - `EdgeRunner_V2_Test_01`
  - `EdgeRunner_V2_Gap01_01`
  - `EdgeRunner_V2_Gap01_02`
  - `EdgeRunner_V2_Gap01_03`
  - `EdgeRunnerBasic_01`
  - `EdgeRunnerBasic_gap_01`
  - `EdgeRunnerBasic_gap_02`
  - `EdgeRunnerBasic_Gap03_01`
  - `EdgeRunnerBasic_Gap03_02`
  - `EdgeRunnerBasic_Gap03_From50092_01`
  - `EdgeRunnerBasic_GapDropBridge_01`
  - `EdgeRunnerBasic_Height01`
  - `EdgeRunnerBasic_jumpPenalty_01`
  - `EdgeRunnerBasic_MiniLevel01_fromScratch_jump003_01`
  - `EdgeRunnerBasic_MiniLevel01_groundAhead_01`
  - `EdgeRunnerBasic_MiniLevel01_jump003_01`
  - `EdgeRunnerBasic_MiniLevel01_unnecessaryJumpPenalty_01`
  - `EdgeRunnerBasic_MiniLevel01Easy_jump003_01`
  - `EdgeRunnerBasic_MiniLevel02_01`
  - `EdgeRunnerBasic_MiniLevel03_01`
  - `EdgeRunnerBasic_MiniLevel04_01`
  - `EdgeRunnerBasic_MiniLevel04_dropAware_01`
  - `EdgeRunner_V2_Flat01_01`
  - `EdgeRunner_V2_Flat01_NoJump_01`

Example commands to run manually in a fresh shell:
```powershell
New-Item -ItemType Directory -Force results\active
New-Item -ItemType Directory -Force results\archive\2026-04-pre-reorg

Move-Item results\EdgeRunner_V2_Gap01_04 results\active
Move-Item results\EdgeRunnerBasic_Gap02_BC_01 results\active
Move-Item results\EdgeRunnerBasic_Gap03_Bridge01 results\active
Move-Item results\EdgeRunnerBasic_Gap03_From50092_02 results\active
Move-Item results\EdgeRunnerBasic_GapDropBridge_02 results\active
Move-Item results\EdgeRunnerBasic_GapJump_BC_05 results\active
Move-Item results\EdgeRunnerBasic_SafeDrop_01 results\active

Move-Item results\EdgeRunner_V2_Test_01 results\archive\2026-04-pre-reorg
Move-Item results\EdgeRunner_V2_Gap01_01 results\archive\2026-04-pre-reorg
Move-Item results\EdgeRunner_V2_Gap01_02 results\archive\2026-04-pre-reorg
Move-Item results\EdgeRunner_V2_Gap01_03 results\archive\2026-04-pre-reorg
```

## 8. Minimum cleanup now vs ideal later
### Minimum safe now
- Keep all current Unity asset paths unchanged.
- Reorganize only `results/`.
- Move `timers.json` into `ML/Notes/Timers`.
- Flag duplicate scenes and recovery scene for manual review.

### Ideal later
- Move demonstrations to `Assets/EdgeRunner/ML/Demonstrations/`.
- Update YAML and `DemonstrationRecorder` to the new demo path.
- Rename ambiguous `.onnx` names to explicit runtime names.
- Split models cleanly into `Runtime`, `Candidates`, and `Archive`.
- Add a simple mapping table: `scene -> assigned model -> source run`.

## Session note
- An automated attempt to reorganize `results/` from this session hit `Access denied` on some directories.
- If `results/active` or `results/archive` exist only as placeholder folders, delete them manually and rerun the move commands from a clean PowerShell window.
