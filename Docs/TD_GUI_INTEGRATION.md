# TD GUI integration

Implemented with the supplied CraftPix PNGs. Shop, Achievement and the permanent Upgrades scene are excluded.

## Navigation

- MainMenu: Play, Registration (local profile), Settings, Help, music/SFX toggles.
- Levels: saved best stars; one playable Level01; future levels stay unavailable until their scenes are assigned and included in the build.
- Selecting an available level opens Difficulty, then loads the match through an asynchronous loading overlay.
- Settings, Difficulty and Help are separate authored scenes. Pause, gameplay settings/help, Victory and Defeat are overlays on the existing HUD Canvas.
- Registration saves first/last name and optional email on this device. It is not an online account. Password and social-login controls are hidden/disabled.

## Rules and saved data

- Save key: `DATN.PlayerProgress.v1` in Unity PlayerPrefs; schema version 1.
- Music/SFX volumes, quality setting, selected difficulty, local profile and best level stars persist.
- Victory rating: 3 stars for at least 80% starting lives, 2 for at least 40%, otherwise 1. Only the best rating is retained. Replaying does not award the same stars again.
- Easy/Normal/Hard enemy HP multipliers: 0.8 / 1.0 / 1.3. Enemy speed, counts, spawn intervals, prices, bounty and cooldown data are unchanged.
- Difficulty is captured at each match start/restart. Tower stats remain value copies and existing shots keep their original contexts.
- Missing Level02 means the Victory Next button is disabled. No extra gameplay scenes are fabricated.

## Inspector

- Theme: `Assets/_GAME/Resources/TdGuiTheme.asset`. The sprite map uses full PNG rectangles without changing existing sliced imports.
- Assign a background-music AudioClip to the theme's `Music` field. The project currently contains no music file. UI click audio is generated at runtime and respects SFX volume.
- Screen controllers: `TdGuiScreen` on each GUI scene. Existing MainMenu/Registration layouts are retained.
- Game integration: `GameGuiCoordinator` on LevelHUD. GameSession continues to own Pause, Resume and timeScale.
- Future levels: assign their scene names in LevelsMenu and add those scenes to Build Settings.

## Verification

Play Mode checks cover navigation through every GUI, settings callback lifecycle/persistence, local-profile validation, star award deduplication, difficulty HP, Pause -> Settings -> Resume at 2x, Defeat retry, Victory exit, unavailable-next-level guard and one EventSystem/audio service after navigation.

Real mouse checks cover Settings volume, level/difficulty selection, Pause/Resume and Defeat retry. Layouts have been visually inspected in the Unity Game View at 16:9.

The full regression passed all six automatic waves at 2x: counts 5, 8, 10, 12, 14, 18 (67 unique enemies). Automatic Victory saved three stars; restart removed result/help overlays. The final audit found no missing scripts across seven remaining scenes and no missing references among 170 theme sprites. MainMenu is the first enabled build scene. Original player preferences were restored after testing.

The six-wave regression resolves enemies through CombatResolver to validate wave counts and automatic Victory, rather than measuring gameplay balance or player skill. Background music, real device audio output, other aspect ratios, cloud accounts and future gameplay levels remain outside those checks.

The permanent Upgrade scene and its menu links were removed. Best stars remain level ratings, and tower upgrades during a match are unchanged. Legacy saved permanent-rank fields are ignored; profiles and best ratings are retained.
