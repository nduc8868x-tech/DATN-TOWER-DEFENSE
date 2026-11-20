# Người A — Buổi 8: dữ liệu và prefab tháp cung

## Tài liệu và phạm vi

- Đọc `Huong_dan_24_buoi_nguoi_A_DATN.docx`, mục **Buổi 08 Tạo dữ liệu và prefab tháp cung**, cùng hợp đồng phối hợp A/B ở đầu tài liệu.
- Đối chiếu `Quy_trinh_chi_tiet_DATN_Tower_Defense.docx`, mục 12.1–12.2 (B quản lý giao dịch), 18.1–18.3 (data tĩnh/runtime/validation), 25.1 (phân công). Tài liệu tổng quát có cả vai trò C; hướng dẫn 24 buổi cụ thể hóa phối hợp A/B và được dùng làm phạm vi cho buổi này.
- Không tìm thấy AGENTS.md ở project hoặc các thư mục cha đã kiểm tra. Shared hiện có `EnemyResolved`; không thêm kiểu hoặc hợp đồng mới vào Shared.
- Chỉ thay CombatSandbox, thêm module tháp và công cụ debug của A. Giữ nguyên Level01, script buổi 7 và thay đổi IDE/tài liệu/tài nguyên có sẵn của người dùng.

## Asset và dữ liệu

Nguồn: `craftpix-net-519305-tower-defense-2d-game-kit/archer-tower-game-assets`.
Không tìm thấy tháp cung đã import trong Assets trước buổi này.

Chỉ import `PNG/2.png`, `4.png`, `38.png` đến `43.png` và `TXT/license.txt` vào `Assets/_GAME/Art/Towers/Arrow`.
2/4 là thân tháp gỗ cấp I/II; 38–43 là các frame cung thủ. Tạo `Arrow_Idle.anim`, `Arrow_Preview.anim` và `Arrow.controller` từ các frame này. Không import FLA, AI, EPS hoặc toàn bộ kit.

`Assets/_GAME/Data/Towers/TowerData_Arrow.asset` dùng towerId `arrow` và prefab `Tower_Arrow`:

| Cấp | Damage | Cooldown | Range | Projectile speed | Cost |
| --- | ---: | ---: | ---: | ---: | ---: |
| I | 16 | 0.8 s | 3 world unit | 8 | 100 mua |
| II | 28 | 0.65 s | 3 world unit | 8 | 100 nâng |

Projectile speed 8 là cấu hình khởi đầu, chưa cân bằng. `projectilePrefab` để trống có chủ đích vì đạn thật thuộc buổi 10. B có thể đọc `TryGetLevel(level, out stats, out error)` để lấy giá; Tower không trừ tiền.

Prefab có root scale 1, con Body, Archer và Muzzle. Sprite dùng 100 pixels/unit, pivot đáy. Muzzle local `(0.2, 1.36, 0)` nằm ở cây cung phía trên thân tháp. Hai instance trong nhóm `TowerSession8` đặt ngoài spline; không đổi scale map hoặc camera.

## API và runtime

```csharp
bool ok = tower.Initialize(data, 1, resolver.Registry, resolver, out string error);
// Chỉ cache ở Awake. Initialize reset trạng thái riêng và để tháp chưa active.
var snapshot = tower.ReadSnapshot();

// Buổi 8: phải chọn preview rõ ràng khi chưa có projectilePrefab.
bool active = tower.Activate(preview: true);
bool previewed = tower.TryPreviewShot(); // animation + tối đa 3 log/Initialize; không damage
tower.Suspend();
```

`TowerLevelStats` là struct cấu hình, được sao chép vào từng instance. Level, cooldown còn lại, số phát preview và activeCombat ở Tower, không ghi vào asset. ReadSnapshot là kiểu lồng trong Tower, không phải hợp đồng Shared mới.

Activate mặc định từ chối nếu thiếu projectilePrefab; gọi `TryValidateForActivation(out error)` để đọc lý do. Preview cho phép bỏ qua projectile nhưng vẫn đòi Initialize hợp lệ và Muzzle. Suspend dừng timer và chặn phát mới; animation preview đã bắt đầu có thể kết thúc bình thường.

Không có tự chọn mục tiêu, tự bắn theo registry, đạn, damage, factory, mua/nâng/bán hoặc UI. Cooldown hiện chỉ phục vụ kiểm chứng trạng thái riêng qua lời gọi preview của debug caller; targeting/nhịp bắn hoàn chỉnh là buổi 9.

## Tự chạy thử

1. Mở `Assets/_GAME/Scenes/CombatSandbox.unity` rồi Play. `EnemySpawnTest` có sẵn khởi tạo session; `TowerDefinitionTest` chờ một frame rồi Initialize A cấp I và B cấp II, cả hai suspended.
2. Chọn `TowerSession8`, mở menu component `TowerDefinitionTest` → **Session 8/01 - Verify Definition And Lifecycle**. Đợi khoảng 20 giây; đọc Report trong Inspector hoặc dòng TOTAL ở Console. Không chạy bộ thử buổi 7 cùng lúc vì cả hai dùng chung session.
3. Để thử tay: **02 - Activate Preview A**, **03 - Preview Shot A (No Damage)**, **04 - Suspend A**, **05 - Read Snapshots**. Gọi 03 trước 02 sẽ không tạo phát; gọi liên tiếp trong cooldown cũng không tạo phát mới.
4. Trong Scene View bật Gizmos: vòng cyan khi suspended, vàng khi active, bán kính 3 quanh chân tháp; điểm đỏ là Muzzle. Bộ thử cho quái normal đi hết spline, xác nhận cả vào/ngoài vòng và còn 60 HP cho tới khi tới đích.
5. Hồi quy buổi 7: chọn CombatSystem → EnemySpawnTest → **Debug/07 - Verify Session 7**, đợi kết quả 15/15. Không cần thay đổi data hay scene.
6. Thoát Play khi xong. Các giá trị runtime/test không cần Apply vào prefab.

## Giới hạn kiểm chứng

Không kiểm bản build độc lập hoặc tích hợp UI/giao dịch người B trong buổi này. Không kiểm projectile/damage/targeting thực vì thuộc buổi 9–10. Cảnh báo WebSocket của package MCP có thể xuất hiện khi domain reload, phải phân biệt với lỗi gameplay trong lượt thử.

## Kết quả thực tế ngày 10/11/2026

Unity 6000.3.11f1, CombatSandbox, Play Mode qua unityMCP. Bản cuối: **20/20 đạt**, Console trong lượt thử **0 Error / 0 Warning**. Lượt trước khi bổ sung hai ca cuối đã đạt 18/18. Hồi quy EnemySpawnTest buổi 7: **15/15 đạt**.

| Ca | Nội dung | Kết quả |
| --- | --- | --- |
| 1 | Arrow data và tham chiếu prefab hợp lệ | Đạt |
| 2 | Cấp I: 16 / 0.8 / range 3 / giá mua 100 | Đạt |
| 3 | Cấp II: 28 / 0.65 / range 3 / giá nâng 100 | Đạt |
| 4 | Yêu cầu cấp không tồn tại trả lỗi có cấp và tên asset | Đạt |
| 5 | Data thiếu cấp II báo tên asset và từ chối | Đạt |
| 6 | Hai instance cùng data Initialize đều suspended | Đạt |
| 7 | Muzzle là con, nằm ở vị trí cung phía trên thân tháp | Đạt |
| 8 | Chưa Activate không có phát preview hay cooldown | Đạt |
| 9 | Activate thường từ chối projectilePrefab trống | Đạt |
| 10 | Activate preview A không kích hoạt B | Đạt |
| 11 | Phát preview chạy animation frame cung thủ | Đạt |
| 12 | Cooldown và số phát ở A/B độc lập | Đạt |
| 13 | B bắt đầu cooldown riêng, không reset A | Đạt |
| 14 | Suspend khóa phát mới và giữ cooldown của A | Đạt |
| 15 | Initialize B cấp II reset B, A vẫn cấp I | Đạt |
| 16 | Trong/ngoài biên 2.99/3.01 world unit khớp tâm Gizmo | Đạt |
| 17 | Thiếu Muzzle/body không Initialize/Activate được | Đạt |
| 18 | Quái normal sinh và bắt đầu chạy qua service hiện có | Đạt |
| 19 | Quái đi qua cả trong/ngoài range, tới đích, không mất HP | Đạt |
| 20 | Toàn bộ thao tác không đổi JSON cấu hình TowerData | Đạt |

Đã quan sát trực tiếp ảnh Game View và Scene View qua MCP: hai tháp có cung thủ, vòng cyan cắt tuyến spline, điểm Muzzle nằm phía trên tháp. Ảnh kiểm tra tạm ở `Temp/Session8`, không phải tài nguyên cần commit.
