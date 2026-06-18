# EdgeRunner Cleanup Report

Date/time: 2026-05-27 00:38:05 +01:00

Scope:
- No permanent deletion was performed.
- No code, scenes, prefabs, YAML, rewards, observations, sensors, behavior parameters, or training scripts were changed.
- Only items explicitly named as archive candidates were moved.
- Items that looked old but were not explicitly listed were left in place and listed below for safety.

Archive folders:
- `Assets\EdgeRunner\ML\Models\Archive\Deprecated_20260527`
- `results\Archive\Deprecated_20260527`
  - Note: Windows resolved this under the existing `results\archive` directory.

## Models Moved

Moved from `Assets\EdgeRunner\ML\Models\Candidates` to `Assets\EdgeRunner\ML\Models\Archive\Deprecated_20260527`:

- `ER_V4_FastPhysics_ChainSafer_Sensors01_50001_Test.onnx`
- `ER_V4_FastPhysics_ChainSafer_Sensors01_50001_Test.onnx.meta`
- `ER_V4_EasierCurriculum_50009_Test.onnx`
- `ER_V4_EasierCurriculum_50009_Test.onnx.meta`
- `ER_V4_EasierCurriculum_47442_Test.onnx`
- `ER_V4_EasierCurriculum_47442_Test.onnx.meta`

## Runs Moved

Moved from `results` to `results\Archive\Deprecated_20260527`:

- `ER_V4_FastPhysics_ChainSafer_Phase01_01`
- `ER_V4_FastPhysics_ChainSafer_Sensors01_01`
- `ER_V4_FastPhysics_EasierCurriculum01`
- `ER_V4_FastPhysics_EasierCurriculum_JumpAssist01`

Important note:
- `ER_V4_FastPhysics_EasierCurriculum_JumpAssist01` was archived as failed/buggy because it was contaminated by the jump buffer bunny hopping issue before the fix.

## Models Preserved

The following important model files were verified and kept in `Assets\EdgeRunner\ML\Models\Candidates`:

- `ER_V3_Mixed04_Phase01_Candidate.onnx`
- `ER_V3_Mixed04_Phase01_Candidate.onnx.meta`
- `ER_V3_Phase11_6GapsWidth28_Jump1035_Candidate.onnx`
- `ER_V3_Phase11_6GapsWidth28_Jump1035_Candidate.onnx.meta`
- `ER_V4_Coyote01_Easier_Candidate.onnx`
- `ER_V4_Coyote01_Easier_Candidate.onnx.meta`
- `ER_V4_EasierPlus_Coyote01_Candidate.onnx`
- `ER_V4_EasierPlus_Coyote01_Final_Test.onnx`
- `ER_V4_EasierPlus_Coyote01_Final_Test.onnx.meta`
- `ER_V4_MidCurriculum_Coyote01_Final_Test.onnx`
- `ER_V4_MidCurriculum_Coyote01_Final_Test.onnx.meta`

Warnings:
- `ER_V4_EasierPlus_Coyote01_Candidate.onnx` exists, but `ER_V4_EasierPlus_Coyote01_Candidate.onnx.meta` was not present during cleanup.

## Runs Preserved

The following important runs were verified and kept in `results`:

- `ER_V3_Mixed04_Phase01_01`
- `ER_V4_FastPhysics_EasierCurriculum_Coyote01`
- `ER_V4_FastPhysics_EasierPlus_Coyote01`
- `ER_V4_FastPhysics_MidCurriculum_Coyote01`

The following final model/checkpoint files were verified and preserved:

- `results\ER_V4_FastPhysics_EasierPlus_Coyote01\EdgeRunnerV3.onnx`
- `results\ER_V4_FastPhysics_EasierPlus_Coyote01\EdgeRunnerV3\checkpoint.pt`
- `results\ER_V4_FastPhysics_MidCurriculum_Coyote01\EdgeRunnerV3.onnx`
- `results\ER_V4_FastPhysics_MidCurriculum_Coyote01\EdgeRunnerV3\checkpoint.pt`

## Explicit Archive Items Not Found

These items were explicitly listed as archive candidates, but were not present in `Assets\EdgeRunner\ML\Models\Candidates` at cleanup time:

- `DEBUG_Physics_A_20`
- `DEBUG_Physics_B_20`
- `DEBUG_Physics_C_20`
- `DEBUG_PhysicsB_ChainTuned_20`
- `DEBUG_PhysicsB_ChainSafer_20`
- `DEBUG_SeamFix_20`
- `DEBUG_ConfigSnapshot_10`
- `ER_V4_FastPhysics_ChainSafer_0959_Test`
- `ER_V4_FastPhysics_ChainSafer_1996_Test`
- `ER_V4_FastPhysics_ChainSafer_2968_Test`
- `ER_V4_FastPhysics_ChainSafer_9481_Test`
- `ER_V4_FastPhysics_ChainSafer_10016_Test`
- `ER_V4_MidCurriculum_Coyote01_48942_Test`

All explicitly listed archive run directories were found and moved.

## Reports/Evaluations

Found:
- `EvaluationReports`

Status:
- Not moved. The folder contains 113 report files and was left in place because no specific report cleanup rule was provided.

## Old-Looking Candidate Models Not Moved

These candidate ONNX files looked old/intermediate, but were not moved because they were not explicitly named as safe to archive:

- `ER_V2_Gap01_Candidate.onnx`
- `ER_V2_Gap02_Candidate.onnx`
- `ER_V2_Gap05Like01_2992_Candidate.onnx`
- `ER_V2_Gap05Like02_3992_Candidate.onnx`
- `ER_V2_Gap05Like03_3993_Candidate.onnx`
- `ER_V2_Gap06_Max3525_Stabilize01_2492_Test.onnx`
- `ER_V2_Gap06_Max3525_Stabilize01_2984_Test.onnx`
- `ER_V2_Gap06_Max3525_Stabilize01_3048_Test.onnx`
- `ER_V2_Gap06_Max3525_Stabilize01_984_Test.onnx`
- `ER_V2_Gap06Like01_3961_Candidate.onnx`
- `ER_V2_Gap06Like02_3941_Candidate.onnx`
- `ER_V2_Gap06Like03_Smooth01_3961_Candidate.onnx`
- `ER_V2_GapCurriculum_Gap03Like01_Candidate.onnx`
- `ER_V2_GapCurriculum_Gap03Like01.onnx`
- `ER_V2_GapCurriculum01_Candidate.onnx`
- `ER_V3_Mixed04_Phase01_01_Test.onnx`
- `ER_V3_Mixed05_Phase01_01_Test.onnx`
- `ER_V3_Phase00_Flat_07_Candidate.onnx`
- `ER_V3_Phase01_1GapEasy_01_Candidate.onnx`
- `ER_V3_Phase01_1GapSmall_02_Candidate.onnx`
- `ER_V3_Phase02_2GapsSmall_01_Candidate.onnx`
- `ER_V3_Phase03_3GapsSmall_01_Candidate.onnx`
- `ER_V3_Phase04_4GapsSmall_01_Candidate.onnx`
- `ER_V3_Phase05_5GapsSmall_01_Candidate.onnx`
- `ER_V3_Phase06_6GapsSmall_01_Candidate.onnx`
- `ER_V3_Phase07_6GapsWidth22_01_Candidate.onnx`
- `ER_V3_Phase08_6GapsWidth24_01_Candidate.onnx`
- `ER_V3_Phase09_6GapsWidth26_01_Candidate.onnx`
- `ER_V3_Phase09_6GapsWidth26_NoFriction_Robust02_Test.onnx`
- `ER_V3_Phase09_6GapsWidth26_Robust01_Candidate.onnx`
- `ER_V3_Phase10_6GapsWidth265_01_Candidate.onnx`
- `ER_V3_Phase10_6GapsWidth265_Clean01_Test.onnx`
- `ER_V3_Phase11_6GapsWidth275_Jump1035_01_Test.onnx`
- `ER_V4_Coyote01_Final_Test.onnx`
- `ER_V4_EasierCurriculum_47442_Candidate.onnx`
- `G5_2992.onnx`
- `G5L2_1964.onnx`
- `G5L2_3992.onnx`
- `G5L3_3993.onnx`
- `G6L1_3961.onnx`
- `G6L2_2990.onnx`
- `G6L2_3941.onnx`
- `G6L3S1_2983.onnx`
- `G6L3S1_3961.onnx`
- `G6L4S1_1985.onnx`
- `G6L4S1_2968.onnx`
- `G6L4S1_5947.onnx`
- `RF_14962.onnx`
- `RF_18975.onnx`
- `RF_19963.onnx`
- `RF_20027.onnx`
- `RF_9969.onnx`

## Old-Looking Result Runs Not Moved

These run directories looked old/intermediate, but were not moved because they were not explicitly named as safe to archive:

- `ER_V2_Gap04Like01_Easy01`
- `ER_V2_Gap04Like01_Easy02`
- `ER_V2_Gap04Like01_Easy03`
- `ER_V2_Gap04Like01_Short01`
- `ER_V2_Gap04Like01_Short02`
- `ER_V2_Gap04Like02`
- `ER_V2_Gap04Like02_RewardFix01`
- `ER_V2_Gap04Like02_Smooth01`
- `ER_V2_Gap05Like01`
- `ER_V2_Gap05Like01_2992_Base`
- `ER_V2_Gap05Like02`
- `ER_V2_Gap05Like02_3992_Base`
- `ER_V2_Gap05Like03`
- `ER_V2_Gap05Like03_3993_Base`
- `ER_V2_Gap06_Max3525_Stabilize01`
- `ER_V2_Gap06Like01`
- `ER_V2_Gap06Like01_3961_Base`
- `ER_V2_Gap06Like02`
- `ER_V2_Gap06Like02_3941_Base`
- `ER_V2_Gap06Like03`
- `ER_V2_Gap06Like03_Consolidate01`
- `ER_V2_Gap06Like03_Smooth01`
- `ER_V2_Gap06Like03_Smooth01_3961_Base`
- `ER_V2_Gap06Like04`
- `ER_V2_Gap06Like04_FineTune01`
- `ER_V2_Gap06Like04_Smooth01`
- `ER_V2_Gap06Like04_Smooth01_1985_Base`
- `ER_V2_GapCurriculum_Fresh01`
- `ER_V2_GapCurriculum_Fresh02`
- `ER_V2_GapCurriculum_Gap03Like01`
- `ER_V2_GapCurriculum_Gap04Like01`
- `ER_V2_GapCurriculum01`
- `ER_V3_Mixed05_Phase01_01`
- `ER_V3_Phase00_Flat_01`
- `ER_V3_Phase00_Flat_02`
- `ER_V3_Phase00_Flat_03`
- `ER_V3_Phase00_Flat_04`
- `ER_V3_Phase00_Flat_05`
- `ER_V3_Phase00_Flat_06`
- `ER_V3_Phase00_Flat_07`
- `ER_V3_Phase00_Flat_07_Base`
- `ER_V3_Phase01_1GapEasy_01`
- `ER_V3_Phase01_1GapEasy_01_Base`
- `ER_V3_Phase01_1GapSmall_01`
- `ER_V3_Phase01_1GapSmall_02`
- `ER_V3_Phase01_1GapSmall_02_Base`
- `ER_V3_Phase02_2GapsSmall_01`
- `ER_V3_Phase02_2GapsSmall_01_Base`
- `ER_V3_Phase03_3GapsSmall_01`
- `ER_V3_Phase03_3GapsSmall_01_Base`
- `ER_V3_Phase04_4GapsSmall_01`
- `ER_V3_Phase04_4GapsSmall_01_Base`
- `ER_V3_Phase05_5GapsSmall_01`
- `ER_V3_Phase05_5GapsSmall_01_Base`
- `ER_V3_Phase06_6GapsSmall_01`
- `ER_V3_Phase06_6GapsSmall_01_Base`
- `ER_V3_Phase07_6GapsWidth22_01`
- `ER_V3_Phase07_6GapsWidth22_01_Base`
- `ER_V3_Phase08_6GapsWidth24_01`
- `ER_V3_Phase08_6GapsWidth24_01_Base`
- `ER_V3_Phase09_6GapsWidth26_01`
- `ER_V3_Phase09_6GapsWidth26_01_Base`
- `ER_V3_Phase09_6GapsWidth26_Consolidate01`
- `ER_V3_Phase09_6GapsWidth26_NoFriction_Robust02`
- `ER_V3_Phase09_6GapsWidth26_NoFriction_Robust03Short`
- `ER_V3_Phase09_6GapsWidth26_Robust01`
- `ER_V3_Phase09_6GapsWidth26_Robust01_Base`
- `ER_V3_Phase10_6GapsWidth265_01`
- `ER_V3_Phase10_6GapsWidth265_01_Base`
- `ER_V3_Phase10_6GapsWidth265_Clean01`
- `ER_V3_Phase10_6GapsWidth265_Consolidate01`
- `ER_V3_Phase10_6GapsWidth27_01`
- `ER_V3_Phase10_6GapsWidth27_AdaptiveJump01`
- `ER_V3_Phase10_6GapsWidth27_AdaptiveJump02`
- `ER_V3_Phase11_6GapsWidth275_Jump1035_01`

## Recommended Current Usage

Recommended model for current V4 testing:
- `Assets\EdgeRunner\ML\Models\Candidates\ER_V4_MidCurriculum_Coyote01_Final_Test.onnx`

Safer fallback model:
- `Assets\EdgeRunner\ML\Models\Candidates\ER_V4_EasierPlus_Coyote01_Final_Test.onnx`

Recommended run/checkpoint to continue training:
- `results\ER_V4_FastPhysics_MidCurriculum_Coyote01\EdgeRunnerV3\checkpoint.pt`

Fallback checkpoint if MidCurriculum regresses:
- `results\ER_V4_FastPhysics_EasierPlus_Coyote01\EdgeRunnerV3\checkpoint.pt`

