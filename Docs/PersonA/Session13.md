# Buổi 13 — Tháp đá và đạn tới điểm nổ

## Đầu vào và phạm vi

- Đã đọc mục Buổi 13 trong `Huong_dan_24_buoi_nguoi_A_DATN.docx` trước khi sửa. Không tìm thấy AGENTS.md trong project/các thư mục cha hoặc Docs/Contracts.md.
- Working tree có nhiều thay đổi của các buổi trước và bản triển khai rock chưa commit; giữ nguyên, hoàn thiện trên nền đó. Không commit/push.
- Buổi 12 còn chờ B tích hợp Level01, wave/session/kinh tế và kiểm thử M3. Buổi 13 chỉ chạy phần A độc lập trong CombatSandbox.
- Trách nhiệm TowerAttack hiện nằm trong `Tower.Update`, không có class TowerAttack riêng. Rock dùng cùng Tower, TowerTargeting, TowerFactory, TowerHandle, TowerData, TowerCatalog và registry projectile của CombatResolver.
- Không sửa/lưu Level01, không thay dữ liệu cung, không triển khai kinh tế/UI B hoặc sát thương vùng buổi 14.

## Code và cấu hình

- `Scripts/Projectiles/RockShotContext.cs`: struct readonly, chỉ chứa impactPosition/session/damage/radius/speed/lifetime; không giữ EnemyHandle hoặc TowerData.
- `Scripts/Projectiles/Projectile_Rock.cs`: kế thừa Projectile để dùng chung đăng ký/cleanup session; di chuyển tới điểm cố định bằng scaled deltaTime, chặn vượt đích, consumed trước debug/log/callback. Không RequestHit. Hết lifetime/cleanup không nổ.
- `Scripts/Towers/Tower.cs`: snapshot vị trí khi bắn đá một lần; chọn projectile từ data; validate lifetime đủ bay từ Muzzle tới rìa range. Cooldown và upgrade dùng cơ chế hiện có.
- `Scripts/Towers/TowerLevelStats.cs`, `TowerSnapshot.cs`: radius theo cấp, giá từ TowerData; catalog chung có arrow và rock.
- `Prefabs/Towers/Tower_Rock.prefab`: root Tower, child Body/SpriteRenderer và Muzzle; không tạo hệ Tower khác.
- `Prefabs/Projectiles/Projectile_Rock.prefab`: SpriteRenderer và Projectile_Rock, lifetime 3 giây.
- `Data/Towers/TowerData_Rock.asset`: I damage30/cooldown2/radius1; II damage50/cooldown1.8/radius1.25; range3/speed4/cost140 mỗi cấp.
- `Scripts/Debug/RockProjectileTest.cs`: kiểm thử tường minh trong Play, không tự chạy. Bao gồm mục tiêu di chuyển mỗi frame ra ngoài tầm, reentrant Explode, cooldown I/II, snapshot upgrade, pause, cleanup hai loại đạn và hồi quy cung.
- `Scenes/CombatSandbox.unity`: sửa RockSession13.factory đang trỏ nhầm Transform của CombatSystem sang TowerFactory của TowerSession11.

Các file triển khai rock, prefab/data và asset trên đã có trong working tree lúc bắt đầu. Lượt này sửa tham chiếu scene, hoàn thiện test và đổi field `consumed` riêng của Projectile_Rock thành `rockConsumed`: Unity thực tế báo trùng tên field giữa lớp con/lớp cha khi nạp prefab, dù field private. Không ghi đè lại các file triển khai đã đúng.

## Asset và tỷ lệ

- Tái sử dụng ảnh trong project; SHA256 khớp `stone-tower-game-assets/PNG/3.png` → Rock_I, `12.png` → Rock_II, `30.png` → đạn Rock. License kèm `Art/Towers/Rock/license.txt`.
- Sprite PPU100, pivot giữa. Muzzle local `(0, 0.65, 0)` giữa hai trụ phía trên ảnh, dùng chung hai cấp.
- Grid CombatSandbox cell 1×1, scale1; chọn range3 = ba ô, cùng quy mô tầm cung hiện có. Bán kính nổ debug 1/1.25 tương ứng 1/1.25 ô.
- Speed4 unit/giây, lifetime3 giây. Cận trên khoảng cách bay XY là range3 + offset Muzzle0.65 = 3.65 unit; thời gian tối đa 0.9125 giây, có dư lifetime. Gameplay chiếu điểm đích về z của Muzzle như đạn cung.
- Vòng vàng debug 96 đoạn, tâm impactPosition, bán kính lấy snapshot; bật Gizmos trong Scene/Game để quan sát. Không có damage vùng trong buổi này.

## Thử lại bằng Inspector

1. Mở CombatSandbox, chọn RockSession13. Gán Factory = TowerSession11/TowerFactory, Spawn Service = CombatSystem/EnemySpawnService, Path = EnemyPath_Spline. Build Position `(-2,0,0)`.
2. Kiểm TowerFactory dùng TowerCatalog (arrow + rock) và CombatSystem/CombatResolver. Tower_Rock có Data_Rock, Body, Muzzle; mỗi cấp data trỏ Projectile_Rock.
3. Play; menu component RockProjectileTest → `Session 13/Run All Rock Checks`. Không chạy bộ debug khác cùng lúc. Đọc Report và log RESULT sau khi Running kết thúc.
4. Chạy riêng ProjectileTest → `Session 10/Run Projectile Checks` để hồi quy cung. Thoát Play khi xong, không Apply các giá trị runtime.

## Kết quả thực chạy

Chạy thật bằng Unity MCP trong CombatSandbox ngày 12/11/2026, Unity 6000.3.11f1. `RockProjectileTest`: **23/23 đạt**, log thô ở `Session13-Rock-runtime.txt`. Cần bật `Application.runInBackground` trong phiên thử để Play tiếp tục khi cửa sổ công cụ chiếm focus; không sửa ProjectSettings.

| Ca | Kết quả thực tế |
|---|---|
| Factory rock I và II / ReadSnapshot | Đạt: I damage30/cooldown2/radius1/range3/speed4/cost140/next140; II damage50/cooldown1.8/radius1.25/cost140/max level |
| Create chưa Activate | Đạt: shot0, projectile0, inactive |
| Activate và cooldown | Đạt: I đo2.0002s, II đo1.8030s |
| Mục tiêu tiếp tục chạy ra ngoài range | Đạt: fixture di chuyển liên tục theo frame, nổ tại điểm cũ, HP60 |
| Mục tiêu chết sau khi bắn | Đạt: fixture giết bằng hit999 riêng; đạn vẫn bay và nổ một lần |
| Mục tiêu disable sau khi bắn | Đạt: vẫn nổ tại điểm cũ, HP60 |
| Mỗi viên Explode một lần | Đạt: consumed và count1 trước callback; callback gọi lại Explode không lặp; vị trí tới đích chính xác |
| HP không giảm do debug explosion | Đạt: HP60 sau nổ, kể cả disable/pause-resume |
| Pause/resume | Đạt: 0.6 giây thực giữ nguyên vị trí và RemainingLifetime; resume tới đích |
| Upgrade khi bay | Đạt: viên cũ giữ damage30/radius1/speed/lifetime/điểm cũ; viên mới damage50/radius1.25 |
| Suspend/Dispose | Đạt: Suspend không bắn mới; Dispose tháp vẫn cho viên đang bay nổ một lần |
| Hết lifetime | Đạt: fixture lifetime0.1s bị dọn, không có callback nổ |
| Cleanup/restart giữa đường, trong pause | Đạt: dọn cả rock và arrow; registry0; session mới không có callback cũ |
| Hồi quy cung qua factory | Đạt: cung vẫn gây damage16, HP60→44 |

| Vòng debug đúng tâm/bán kính hai cấp | Đạt qua Scene View: fixture I tâm(0,0), r1; II tâm(3,0), r1.25; so với grid1 unit, vòng II lớn hơn25%. Kiểm cả gizmo khi bay và Debug.DrawLine sau Explode |
| Hồi quy đầy đủ đạn cung | Đạt **23/23** bằng ProjectileTest, log `Session13-Arrow-runtime.txt`: HP60→44→28→12→0, pause/resume, target chết/hủy, lifetime, snapshot cấp, Dispose và cleanup/session cũ |
| Console cuối / compile / runtime | Không có lỗi C# hoặc runtime mới sau sửa. Còn cảnh báo Unity Account API không kết nối trong30s, đã có từ đầu phiên |

Ảnh thực chạy: `Session13-Screenshots/screenshot-20261112-022516.png` (gizmo khi bay, đã pause để quan sát) và `screenshot-20261112-022541.png` (vòng sau Explode của fixture hình). Hai fixture hình gọi Explode tường minh qua MCP, không thay cho test di chuyển/tới đích trong23 checks. Ảnh022300 là toàn cảnh sandbox trong phiên Play.

Kết thúc: đã thoát Play Mode, timeScale trả1, khôi phục runInBackground vềfalse; lưu CombatSandbox. Không lưu Level01, SHA256 Level01 và TowerData_Arrow khớp đầu phiên. Các file asset trong Assets/_GAME đều có .meta. Đã xem git diff; phần whitespace `m_Name: ` trong YAML là định dạng Unity/sẵn có, không quét sửa các thay đổi của người dùng. Unity AI package tự ghi thêm config/define vào ProjectSettings khi reload; đã bỏ riêng các thay đổi tự sinh đó để giữ phạm vi buổi13.

Vấn đề công cụ đã xử lý: Unity từng thiếu file RockProjectileTest trong source list dù AssetDatabase có MonoScript. Đăng ký lại bằng AssetDatabase.MoveAsset đi/về, giữ nguyên GUID, rồi compile đã có class. Không xóa Library hoặc thay package. Console ban đầu có cảnh báo/lỗi kết nối Unity Account; không phải lỗi C#.

## Bàn giao B

`towerId = "rock"`; giá mua I = 140; giá nâng I→II = 140. Đọc Cost/NextLevelCost từ snapshot, không hardcode ở caller. Factory tạo inactive; giao dịch thành công mới Activate, rollback Dispose. Dispose tháp không hủy projectile còn thuộc session hợp lệ; restart phải cleanup session.

Buổi 13 hoàn thành phần đường bay và điểm nổ; sát thương vùng chờ buổi 14. Tích hợp Level01/M3 vẫn chờ B.
