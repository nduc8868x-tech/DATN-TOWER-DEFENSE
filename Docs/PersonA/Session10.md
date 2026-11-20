# Buổi 10 — đạn cung và xác nhận trúng

Đọc mục Buổi 10 trong `Huong_dan_24_buoi_nguoi_A_DATN.docx`. Không tìm thấy AGENTS.md trong project/thư mục cha. Shared chỉ có EnemyResolved; chưa có Projectile/ShotContext trước buổi này. Nhịp bắn hiện nằm trong Tower, không có TowerAttack riêng.

## Luồng và API

- `Tower.Activate()` hoặc `ActivateTargeting()` bật bắn thật. `ActivateTargeting(true)` giữ chế độ đo/debug buổi 9; `Activate(true)` giữ preview thủ công buổi 8. Không chạy đồng thời hai cơ chế.
- Tower tạo một Projectile ở Muzzle, không parent vào tháp, rồi phát ShotFired để quan sát. Không đăng ký thêm một producer vào ShotFired vì Tower đã tạo đạn.
- ShotContext là readonly struct trong Scripts/Projectiles, chụp EnemyHandle (kèm identity), sessionId, damage, speed và lifetime. Không có effect hiện tại; đạn không đọc TowerData hoặc tham chiếu Tower sau Initialize.
- Projectile.Update kiểm validity/session/Alive, giảm lifetime bằng deltaTime và MoveTowards theo XY. Sprite hướng gốc +X; xoay theo atan2. Z chỉ dùng để hiển thị.
- Khi trong ngưỡng hoặc bước bay đủ tới target: consumed=true **trước** RequestHit. Chỉ có một đường xác nhận; không collider/trigger và không gọi TakeDamage từ Projectile. Đạn inactive và Destroy ngay sau enqueue; CombatResolver áp damage ở LateUpdate.
- CombatResolver giữ tập projectile theo instance/session. EnemySpawnService.CleanupSession gọi CleanupProjectiles, độc lập với target còn tồn tại hay không. BeginSession dọn đạn cũ và queue cũ; resolver loại yêu cầu trễ bằng identity/session.
- Chỉ CombatResolver.LateUpdate gọi ResolvePendingBatch; bỏ lời gọi thừa trong CombatTestRunner cũ. Producer Tower/Projectile chạy Update trước pha này.

## Asset và thông số

- Sprite nguồn: `craftpix-net-519305-tower-defense-2d-game-kit/archer-tower-game-assets/PNG/37.png`, đã xem ảnh xác nhận hướng sang phải; license nguồn đã có tại Art/Towers/Arrow/license.txt.
- Import duy nhất `Assets/_GAME/Art/Projectiles/Arrow.png`, 50 PPU; hình 31×10 px tương ứng khoảng 0.62×0.2 world unit, scale prefab 1.
- `Prefabs/Projectiles/Projectile_Arrow.prefab`: SpriteRenderer, Projectile; Sorting Layer Default, Order 10, không collider.
- Tốc độ 8 world unit/giây giữ từ data buổi 8; lifetime tự chọn 5 giây, hitRadius tự chọn 0.05 world unit. Cả hai cấp TowerData_Arrow dùng prefab này; damage vẫn 16/28, cooldown 0.8/0.65.
- Validate tên prefab, sprite, component active, lifetime/radius hữu hạn; Initialize kiểm speed/lifetime dương hữu hạn, damage dương, target và session. Tower kiểm Muzzle trước bắn.

## Chạy thử

1. Mở CombatSandbox, Play; EnemySpawnTest tạo session như trước.
2. Chọn TowerSession8 → ProjectileTest → menu **Session 10/Run Projectile Checks**.
3. Đợi khoảng 10 giây; xem report và Console. Không chạy cùng lúc bộ thử 7/8/9 vì chúng chia sẻ session.
4. Test sinh normal bằng EnemySpawnService, dùng quái đứng yên để tách kiểm tra hit khỏi việc tới đích; tạo tháp runtime tạm để kiểm bắn/nâng trực tiếp/dọn. Không cần UI kinh tế.
5. Hồi quy: TowerAttackTest → Session 9/Run All Checks; TowerDefinitionTest → Session 8/01 - Verify Definition And Lifecycle. Chạy từng bộ riêng.
6. finally/OnDisable của bài thử đưa timeScale về 1 và dọn fixture/đạn. Thoát Play khi hoàn tất.

## Kết quả ngày 10/11/2026

Unity 6000.3.11f1, CombatSandbox qua unityMCP: **23/23 đạt**, Console lượt buổi 10 **0 Error / 0 Warning**.

| Ca | Kết quả |
| --- | --- |
| 1–8: Mỗi lần bắn giữ HP lúc launch, giảm đúng 16 sau hit, viên đạn bị dọn | Đạt cả 4 viên |
| 9: Bốn hit chỉ một Dead | Đạt; HP 60 → 44 → 28 → 12 → 0 |
| 10: Hai viên đồng thời gây lethal | Đạt, một Dead |
| 11: Target chết trước đạn, không chuyển sang target khác | Đạt |
| 12: Target bị Destroy trước hit | Đạt, không lỗi đỏ |
| 13: Identity thay đổi làm handle invalid | Đạt, không damage |
| 14: Pause 2.5 giây thực | Đạt, position/lifetime/HP không đổi |
| 15: Resume | Đạt, tiếp tục bay và hit một lần |
| 16: Hết lifetime | Đạt, dọn không damage |
| 17: Chưa Activate | Đạt, không projectile |
| 18: Tower phát một projectile từ Muzzle | Đạt, HP chưa giảm lúc launch |
| 19: Đổi tháp từ cấp I sang II khi đạn bay | Đạt, viên cũ vẫn damage 16 |
| 20: Suspend | Đạt, không projectile mới |
| 21: Destroy tháp sau phát cấp II | Đạt, viên đã bay vẫn damage 28 |
| 22: Cleanup session trong pause, gồm target đã mất | Đạt, projectile registry về 0 |
| 23: Session mới | Đạt, dọn đạn cũ và từ chối cả hit queued/later mang identity cũ |

Hồi quy buổi 9: **17/17 đạt** (preview tường minh), buổi 8: **20/20 đạt**. Một ca bổ sung qua MCP cho target chạy spline thật đã ghi `MOVING_ARROW_HP=44; progress=0.02647419; moving=True`: đạn đuổi và hit quái di chuyển đúng 16 damage. Scene xác nhận chỉ một CombatResolver, timeScale=1 và Console cuối lượt 0 lỗi/cảnh báo.

Chưa kiểm build độc lập, UI bán/nâng của B, effect (chưa có), hiệu năng nhiều trăm đạn hoặc pooling. Các ca định lượng chính dùng target đứng yên có kiểm soát; ca bổ sung trên spline chưa thay thế kiểm thử dài trên toàn màn.
