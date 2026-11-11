# Integration notes — basic Level01 loop

Updated 2026-11-12. The basic loop is integrated in the existing project and was exercised in Unity Play Mode. No new Unity project, package upgrade, push or merge into main.

## Git preservation and integration

- Initial branch: main at 01d8979. origin/hiep was not integrated; it contained B's session/economy/build/input/fakes and FlowSandbox.
- Created integration/level01-basic-loop. Checkpoint b09b53c preserves all tracked edits plus A runtime scripts/prefabs/data, notes/screenshots and existing local project changes. backup/pre-b-integration-20261112 points to it.
- Fetched hiep from the requested repository; its head was a94eb82. Merge commit 735bbb0 integrates it. Three add/add conflicts in Shared.meta, EnemyResolved.cs and its .meta were resolved explicitly, retaining A's identity payload and GUIDs; B caller compatibility was added afterwards.
- The 1.9 GB source CraftPix directory, three source DOCX files and Word lock file remain on disk, untracked and untouched. The first broad staging attempt was interrupted; the stale index lock was removed only after no git process remained, then staging was limited to project work. No reset --hard, clean or whole-tree ours/theirs was used.
- All feature work AFTER the merge remains uncommitted for review. main still points at 01d8979. No push.

## Completed sequence and wiring

1. Read A/B DOCX guides, coordination guide and existing A session/integration notes. No applicable AGENTS.md or existing Docs/Contracts.md was found; Contracts.md is now the current common contract.
2. Merged B, removed duplicate tower contracts and kept A's GUIDs/API overloads. Shared real/fake providers compile together.
3. Real combat smoke in Level01: runtime-only placed Arrow at (-4.5, 0), five normal, 1.5 s interval. Observed 16 shots, 3 kills, 2 escapes, gold 284, lives 18, alive 0, manual-start waiting state. The placed test tower was never saved.
4. Added ONE build point and saved Canvas, then tested real purchase, duplicate rejection, upgrade and sale (refund 140). Only after this succeeded, added eight points.
5. Ran the opt-in integration verification suite against saved Level01, including full three-wave real gameplay; detailed per-test evidence is Docs/IntegrationTestResults.txt. Additional UI event/raycast checks and sandbox validation are recorded below.

Saved Level01 keeps the original map, EnemyPath_Spline, camera and 2D light. MovementTest is disabled and old placed test enemies are inactive (retained, not deleted). Its Systems object contains exactly one EconomyManager, GameSession, CombatResolver, EnemySpawnService, TowerFactory, WaveManager, FlowBootstrapper, BuildManager and BuildInputController.

Assigned references:

- EnemySpawnService -> EnemyCatalog_Main.asset + Systems CombatResolver.
- TowerFactory -> existing TowerCatalog.asset (arrow/rock) + same CombatResolver.
- FlowBootstrapper -> real EnemySpawnService/TowerFactory + same session/economy/resolver/waves.
- WaveManager -> original EnemyPath_Spline + Data/Waves/Wave01, Wave02, Wave03 (5/7/10, interval 1.5).
- BuildManager -> same session/economy/bootstrapper + TowerCatalog. BuildInput -> Main Camera + session, default-layer point colliders.
- Eight points at (-7.5,-0.5), (-4.6,0), (-4.7,2.5), (-2.8,2.5), (-2,-0.6), (0.1,1.5), (2.7,0.4), (0.9,3.1); all reuse existing UI marker sprite, off the spline, and cover useful path segments.
- LevelHUD Canvas -> same managers/input and serialized UI controls. One EventSystem with InputSystemUIInputModule.
- No fake provider or debug combat runner is active in Level01; only CombatResolver calls ResolvePendingBatch.

Arrow data was already purchase 100 / upgrade 100, so no price or stat changes were needed. Rock assets/progress remain intact; buying is restricted to arrow. Three waves are integration data, not final balancing.

## Changed files for review

- Scenes/Level01.unity: real service wiring, disabled old debug flow, eight build points, Canvas, EventSystem; original scene GUID/prefab references retained.
- Scripts/Core/GameSession.cs, FlowBootstrapper.cs: registered results, shared session ID, batch ordering, single restart path.
- Scripts/Economy/EconomyManager.cs; Scripts/Building/BuildManager.cs, BuildInputController.cs: reserved transactions, TowerData prices, state/reentry gates and UI raycast blocking. Existing BuildPoint implementation is reused.
- Scripts/Combat/CombatResolver.cs; Scripts/Enemies/EnemySpawnService.cs: execution order, pause/invalidation gate, common spawn interface and provider failure reason. Existing registry, health, movement and projectiles reused.
- Scripts/Towers/TowerFactory.cs, TowerHandle.cs, TowerSnapshot.cs; Scripts/Shared/EnemyResolved.cs, IEnemySpawnService.cs: common real/fake contracts; duplicate Shared TowerHandle/TowerSnapshot files and their unused type metas removed.
- Scripts/Debug/FakeEnemySpawnService.cs, FakeTowerFactory.cs, FlowSandboxDebugPanel.cs: update to bool lifecycle, registered identities and catalog price API; only Arrow purchase offered.
- New Scripts/Waves/WaveData.cs, WaveManager.cs, Data/Waves/Wave01-03.asset, Scripts/UI/LevelHUD.cs, Editor/Level01Setup.cs and their .meta files.
- New opt-in Scripts/Debug/LevelIntegrationChecks.cs (.meta): automated Play Mode checks, not attached to saved scenes.
- Docs/Contracts.md, IntegrationNotes.md, IntegrationTestResults.txt.
- Unity regenerated solution/editor-project entry and some settings for already-installed packages; no manifest/lock upgrade during feature work. Earlier local package edits are preserved in the checkpoint.

## Running the game

1. Open Assets/_GAME/Scenes/Level01.unity with the existing Unity 6000.3.11f1.
2. Press Play. HUD shows 240 gold / 20 lives / wave 1 of 3. Select a marked point, then Buy Arrow (100).
3. For a repeatable winning run, buy at BuildPoint_01 and BuildPoint_02; press Start Wave. After wave 1, upgrade the first arrow when affordable. Start wave 2 manually, then upgrade the second when affordable. Start wave 3.
4. Select occupied points to upgrade/sell. Each wave needs Start; Pause switches to Resume. Restart works while preparing, fighting, paused or on the result panel.
5. Victory/Defeat displays a modal panel. Restart returns to initial 240/20 with all points empty.

To rerun verification: open Level01, enter Play, choose DATN > Integration > Run Level01 Checks (Play Mode). Let it finish; the runtime-only test object logs each case and writes Docs/IntegrationTestResults.txt. It uses failure/result/last-life injections only where labeled, and runs the complete normal gameplay match without forced kills. The separate lethal-boundary run uses timeScale 8 for scheduling and injects the last-life value. Stop Play afterwards; never save runtime test values to scene/data. DATN > Integration > Configure Level01 Gameplay can repair saved wiring/UI against the existing Systems object outside Play Mode.

## Verification and limits

See the per-case report for gold/lives, buy/upgrade/sell failures, duplicate/foreign results, registered identities, wave gates, Start spam, lethal final batch, pause and restart, listener/manager counts and real three-wave Victory. The first complete suite was 42 PASS / 0 FAIL; the final report also includes bounded spawn failure checks.

Additional executed UI checks: EventSystem RaycastAll at BuyArrow returned BuyArrow and BuildMenu. Pointer-click events on Buy/Upgrade/Sell/Pause/Restart executed their bound actions (purchase gold 140, upgrade II, pause state, reset 240/20). ScreenCapture screenshot verified the Canvas labels, markers and menu layout. This is in-editor event automation, not an OS-level physical mouse/touch test. Mobile aspect ratios and a standalone player build have not been tested.

Unity MCP occasionally emitted internal PlayerLoop recursion messages during screenshot/tool pumping, and the existing account integration logged an Account API timeout. These are recorded separately from the gameplay checks; no script compilation error was reported. No API purchase/login change was attempted.

No 10-wave expansion, SaveService, complete main menu, new combat mechanic or final balance work was added.

## Sandbox checks executed after integration

- FlowSandbox path is Assets/_GAME/Tests/FlowSandbox/FlowSandbox.unity (B's original location). Assigned the shared TowerCatalog to its BuildManager and saved. Missing scripts: 0. Play Mode: real shared price lookup with fake creation; buy accepted, injected fake upgrade rejected with 140 gold and level I unchanged, next upgrade accepted, sale refund 140. Registered fake normal death gave gold 188; duplicate stayed 188; shared restart returned 240/20. Its legacy debug total-wave label remains 10; no ten-wave gameplay was created.
- CombatSandbox loaded with missing scripts 0 and scriptCompilationFailed false. Existing A FakeBuildCaller regression ran 20/20 PASS, including cooldown ratio, old arrow damage snapshot and safe disposal. Detailed output: CombatSandboxFactoryResults.txt.
- Final Level01 integration suite: 45 PASS / 0 FAIL, including bounded spawn failure and restart after it. See IntegrationTestResults.txt for every individual assertion.
- HUD evidence: Level01-HUD.png, captured in Play Mode after clean reset and selecting an empty point. No test runtime state was saved back to scene/prefab/data.

Final UI follow-up: separated Buy and Upgrade screen regions so a double click cannot buy then unintentionally upgrade. A fresh EventSystem raycast after the first click hits BuildMenu, not Upgrade: one tower, level I, gold 140. Result is IntegrationUIResults.txt; screenshot Level01-ArrowHUD.png shows actual arrow and upgrade/sale menu. The initial synthetic UI probe needed Canvas.ForceUpdateCanvases after enabling the menu; the corrected probe passed. Physical OS mouse/touch input remains outside this automated check.

Existing ProjectileTest also completed 23/23 PASS (CombatSandboxProjectileResults.txt), including pause/resume, independent projectiles after tower disposal, old shot damage snapshots, cleanup and old-session rejection. Combined automated suites: Level01 45/45, A factory/cooldown 20/20, A projectiles 23/23. Final editor compilation status: scriptCompilationFailed false. Unity was stopped on Level01; no test runner or purchased tower is saved in it.
