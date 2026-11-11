# Gameplay contracts — Level01 integration

Updated 2026-11-12. Applies to A, B, real providers and sandbox callers.

## Ownership and canonical types

- GameSession owns session state, lives, current wave, registered enemy identities, pending results and deduplication. EconomyManager is the only gold balance.
- WaveManager owns one spawn timer, successful spawn count, spawnFinished and the one-time completion reward guard. It uses GameSession.AliveEnemies, not registry emptiness, for completion.
- EnemySpawnService, CombatResolver/EnemyRegistry, TowerFactory/TowerHandle and actual tower/projectile prefabs remain A's implementations. BuildManager/BuildPoint, EconomyManager, GameSession and FlowBootstrapper continue B's implementations.
- One EnemyResolved in Shared carries Identity(SessionId, SpawnId), WaveId, State, Bounty and LeakDamage. SessionId/SpawnId/Reason and the integer constructor are compatibility views for B's sandbox; there is no second result queue or payload implementation.
- One TowerHandle and TowerSnapshot live under Scripts/Towers. Duplicate Shared versions were removed; A's original GUIDs were retained. Handle exposes bool Activate/Suspend/TryApplyLevel, idempotent Dispose, value ReadSnapshot() and Try-style ReadSnapshot(out snapshot). FakeTowerFactory derives a sandbox handle from this contract.
- IEnemySpawnService.TrySpawn(EnemySpawnRequest, out EnemyHandle), bool BeginMovement, CleanupSession, Resolved and LastError are common to real and fake services. A's positional TrySpawn overload remains for existing CombatSandbox tools.
- Canonical tower IDs follow the catalog: arrow and rock. No stone alias in callers. Rock progress/assets remain; BuildManager opens only arrow for purchase. Magic is not offered.

## Data and transactions

- TowerCatalog -> TowerData -> one-based level stats is the only purchase/upgrade price source, including HUD and FlowSandbox labels. Arrow currently has cost 100 at I and cost 100 for the upgrade to II; this matches the A/B guide. No arrow combat/data values were changed during integration.
- Start 240 gold and 20 lives. Normal data: 60 HP, bounty 8, leak 1. Three integration waves are data assets with 5 / 7 / 10 normal and 1.5 seconds interval. The user's requested 7 overrides the older general guide's 8. These are test data, not final balance.
- TryCreate returns an initialized, suspended tower. BuildManager locks the point/manager, creates, silently reserves gold, commits occupancy/investment, then Activate. On false creation/activation it suspends/disposes the candidate, clears occupancy and cancels the money reservation. UI receives transaction events only after a final outcome.
- Upgrade reserves gold BEFORE TryApplyLevel. Real TryApplyLevel validates the next level and all combat references before changing level/stats/sprite/cooldown. On false, cancel reservation; no attempt to downgrade/rollback using TryApplyLevel(oldLevel). There is no fallible spend after successful stat mutation.
- Each synchronous transaction locks against reentry/double purchase. Completed purchase occupies the point, so the next request fails without a second charge.
- Sale calls Suspend before Dispose, clears the point and returns integer floor(totalInvested * 7 / 10). Invested tracks actual accepted charges: Arrow I 100 -> refund 70; Arrow II 200 -> refund 140.
- Service layer rejects buy/upgrade/sell outside Preparing/Running, independently of disabled UI. Paused, Victory, Defeat and invalidated sessions cannot transact.
- Existing proportional remaining cooldown conversion and immutable shot snapshots are preserved. Selling a tower does not retroactively change launched shots; session cleanup cancels them.

## Spawn, combat and result order

1. Resolver.BeginSession produces the authoritative session ID; Flow initializes GameSession with that same ID.
2. Successful TrySpawn registers the combat handle; WaveManager then calls GameSession.RegisterEnemySpawned(handle, waveId) and increments Spawned before BeginMovement.
3. Producers enqueue hits/escapes. CombatResolver is the ONLY runner, in LateUpdate at execution order 10000. It resolves hits before escapes, guards one resolution per frame, and defers requests added during a batch to the next batch. FlowBootstrapper does NOT call ResolvePendingBatch.
4. FlowBootstrapper at order 11000 drains GameSession's whole result queue. Only valid terminal state + current session + current wave + previously registered identity + unprocessed spawn ID are accepted. Registration is checked before dedup insertion, so a rejected wrong-wave payload cannot consume a valid result later.
5. Bookkeeping for the whole batch finishes before notification. A callback that restarts invalidates the remainder through session-ID guards. Dead adds bounty; Escaped subtracts leak; each accepted identity decrements alive exactly once.
6. After draining, check lives <= 0 -> Defeat first. Only if still alive may WaveManager complete when spawnFinished && alive == 0. Reward 20 once, then Preparing for the next manual Start, or Victory after final wave. No wave reward on a lethal final batch.

## Scheduling, pause and restart

- StartNextWave switches state and claims its timer synchronously. Repeated Start creates no extra schedule. Spawned/alive only change on successful creation/registration.
- Timer uses scaled deltaTime, emits at most one enemy/frame and resets its interval without catch-up. Pause freezes spawn, spline movement, projectiles and tower cooldown; Resume restores the previous state.
- Each wave permits at most three failed spawn attempts. LastError includes provider reason and attempt count; HUD shows it. Exhaustion stops the schedule and enters Defeat (integration configuration failure; Restart is available), never rewards an incomplete wave. Registration/movement failure aborts immediately.
- All HUD restart buttons and sandbox restart call FlowBootstrapper.StartNewSession. No dependency on debug panels.
- Restart first invalidates GameSession and CombatResolver (blocks gameplay and old results/hits); cancels the wave timer and clears input selection; suspends/disposes tracked towers and clears points; calls CleanupSession(oldId), including Death/escaped objects and tracked projectiles, without Dead/Escaped events; begins a new combat/session, restores timeScale 1, 240/20, wave 1 and all counters/guards.
- Service subscription is replaced symmetrically when rewiring and removed on destruction. Wave subscriptions detach before reinitializing. HUD removes its own button and snapshot/selection/wave listeners on disable. No manager is DontDestroyOnLoad.
- Victory/Defeat freeze simulation; Restart is still operable through the Input System UI.

## UI

- Saved LevelHUD Canvas reads GetSnapshot immediately on bind, then SnapshotChanged. It stores no mutable gold/lives balance. Purchase/upgrade labels query BuildManager's TowerData prices; sale label derives from tracked investment.
- One InputSystemUIInputModule EventSystem. BuildInputController checks the EventSystem's current pointer and performs a fresh RaycastAll at the clicked position before world picking, preventing same-frame UI click-through.
- Actual sprites from the existing UI kit mark eight points. Screen Space Overlay HUD contains gold/lives/wave/alive, Start, Pause/Resume, Restart, selection/buy/upgrade/sell and a modal result panel.

Buy and Upgrade occupy separate screen regions. A rapid second click at the purchase position must hit menu background, not initiate an upgrade; both the service duplicate guard and UI raycast transition were verified.
