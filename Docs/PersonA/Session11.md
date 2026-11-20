# Buổi 11 — Factory tháp và nâng cấp không dính kinh tế

Đã đọc đúng mục “Buổi 11 Factory tháp và nâng cấp không dính kinh tế” trong `Huong_dan_24_buoi_nguoi_A_DATN.docx` (trích XML từ DOCX đang được Word mở). Không tìm thấy AGENTS.md. Giữ toàn bộ thay đổi chưa commit của buổi 10 và của người dùng; không commit/push theo yêu cầu hiện tại, dù tài liệu có bước commit.

## Thay đổi riêng của buổi 11

- `Assets/_GAME/Scripts/Towers/Tower.cs`: thêm kiểm tra dữ liệu combat, nâng cấp nguyên tử và giữ tỷ lệ cooldown. Không thay API Initialize/ReadSnapshot/Activate cũ; nhịp bắn vẫn chỉ có trong Tower, không tạo thêm TowerAttack trùng chức năng.
- Thêm `TowerCatalog.cs`, `TowerFactory.cs`, `TowerHandle.cs`, `TowerSnapshot.cs` trong Scripts/Towers và các .meta. Scripts/Shared chỉ có EnemyResolved, chưa có hợp đồng tháp trùng tên/chức năng.
- Thêm `Scripts/Debug/FakeBuildCaller.cs` và .meta.
- Thêm `Data/Towers/TowerCatalog.asset` và .meta, ánh xạ `arrow` tới TowerData_Arrow đang có.
- CombatSandbox thêm root `TowerSession11`: TowerFactory + FakeBuildCaller, gán resolver/spawn service/path hiện có.
- Không đổi prefab, hình ảnh, số liệu tháp/đạn trong buổi 11. Tái sử dụng Tower_Arrow, Projectile_Arrow và sprite Craftpix đã import. Không đổi Level01/phần B.

## Hợp đồng cho B

Factory phải được gán catalog và CombatResolver đã BeginSession. Level đánh số từ 1, ID phân biệt hoa thường. Tất cả thao tác dùng trên main thread Unity. Các thao tác có thể thất bại trả bool; `ReadSnapshot(out snapshot)` trả false và default nếu handle mất hiệu lực. Dispose là void, gọi lặp an toàn. Handle là class để các bên giữ cùng trạng thái Dispose; kiểm tra instance và session ở mỗi thao tác.

```csharp
TowerHandle handle;
if (factory.TryCreate("arrow", 1, position, out handle))
{
    TowerSnapshot snapshot;
    handle.ReadSnapshot(out snapshot); // inactive; Cost từ TowerData
    if (transactionSucceeded) // B quyết định giao dịch/ô xây
    {
        if (!handle.Activate()) handle.Dispose(); // B xử lý thất bại giao dịch nếu cần
    }
    else handle.Dispose();
}
// Nâng: B kiểm tra giá/giao dịch; A chỉ đổi combat nếu hợp lệ.
bool upgraded = handle != null && handle.TryApplyLevel(2);
// B quản lý hoàn floor(0.7 * tổng vốn); A không thay tiền.
handle?.Suspend();
handle?.Dispose();
```

Snapshot chỉ chứa giá trị readonly: TowerId, InstanceId, Level, Damage, Range, Cooldown, ProjectileSpeed, RemainingCooldown, Active, ShotCount, Cost, NextLevelCost, CanUpgrade. Không trả tham chiếu cấu hình gốc. Cost là giá cấp hiện tại; không phải tổng vốn. Factory không sở hữu tiền hoặc ô xây, B sở hữu vòng đời handle.

TryCreate kiểm ID/level, position hữu hạn, dependency/session, data, prefab Tower, Muzzle/body/sprite, prefab Projectile và lifetime trước Instantiate. Nếu bước Initialize sau Instantiate thất bại thì inactive và Destroy object đó. Tháp mới chưa bắn.

TryApplyLevel chỉ nhận level hiện tại + 1; kiểm tra đầy đủ trước khi đổi stats/level/sprite/cooldown. Không gọi Initialize để nâng vì Initialize reset trạng thái. Công thức `Clamp01(remainingOld / oldCooldown) * newCooldown`; cooldown đã được kiểm hữu hạn và dương. Không đổi Active/Suspend, không sửa ScriptableObject, reset lựa chọn mục tiêu để dùng range mới. Dispose ngừng bắn và inactive ngay, Destroy hoàn tất cuối frame.

Projectile/ShotContext giữ nguyên cơ chế buổi 10: snapshot damage/speed/lifetime khi bắn, không parent vào tháp, hit qua resolver kiểm target/identity/session; nâng hoặc dọn tháp không sửa đạn cũ.

## Kết quả Unity/MCP thực tế

Unity 6000.3.11f1, CombatSandbox, Play Mode. Lượt cuối `Run All Checks`: **20/20 PASS**, Time.timeScale=1 sau cleanup.

1. ID sai, cấp 0/99, position NaN: false, số tháp không tăng.
2. Thiếu resolver: false trước tạo object.
3. Giao dịch giả thất bại: Dispose dọn tháp, gọi lặp an toàn.
4. Handle Dispose: ReadSnapshot/Activate/Suspend/TryApplyLevel đều false.
5. Create chưa Activate: 0 shot, 0 projectile, quái 60 HP.
6. Snapshot ID arrow, Cost/NextLevelCost=100/100, CanUpgrade đúng.
7. Activate thành công.
8. Tháp tạo đạn thật trước khi HP giảm.
9. Nâng I→II khi đạn cũ đang bay thành công.
10. Nâng đang active: vẫn active, damage28, range3, cooldown0.65; tỷ lệ 0.8/0.8→0.65/0.65 đúng.
11. Đạn trước nâng cấp gây16: HP60→44.
12. Tháp thứ hai không đổi runtime.
13. Shared data cấp I vẫn damage16/cooldown0.8.
14. Nâng cấp lùi/cùng cấp/99: giữ level, stats, timer, active nguyên trạng.
15. Đạn sau nâng cấp gây28 dù tháp bị Dispose: HP44→16.
16. Sau Suspend/Dispose không bắn vô hình; HP giữ16, projectile0.
17. Nâng lúc Suspend giữa cooldown: **0.399034/0.8→0.324215/0.65 giây**; đúng công thức (mẫu lý tưởng0.4→0.325).
18. Sau nâng vẫn Suspend, timer không chạy.
19. Instance bị Destroy bên ngoài: mọi thao tác từ chối an toàn, Dispose không exception.
20. Đo bắn thật sau nâng giữa chu kỳ: chờ **0.324409 giây**, kỳ vọng **0.324066**; khoảng giữa hai phát cấp II **0.650302 giây**, kỳ vọng0.65. Sai số trong một frame, không bắn ngay do nâng.

Bốn ca bổ sung qua execute_code MCP, dùng bản sao data/catalog tạm, không sửa asset gốc: cooldown cấp II=0 bị từ chối và giữ runtime; factory gặp stats lỗi không thêm object; thiếu Projectile prefab bị từ chối; thiếu Tower prefab bị từ chối. Đều PASS. Bản sao và tháp fixture đã dọn, catalog gốc được khôi phục trong finally.

Hồi quy ProjectileTest buổi 10: **23/23 PASS**, gồm HP60→44→28→12→0, hit một lần, hai hit kết liễu chỉ một Dead, target chết/hủy/đổi identity, pause2.5 giây/resume, lifetime, inactive/Suspend, đạn sống sau dọn tháp, cleanup session và từ chối session cũ.

Console lượt chạy cuối: **0 error, 0 warning**. Có warning WebSocket của MCP khi domain reload trước đó; kết nối phục hồi và chạy được toàn bộ kiểm thử. Một lệnh thiết lập Editor ban đầu dùng tên Object mơ hồ bị trình biên dịch execute_code từ chối; đã sửa UnityEngine.Object rồi thực hiện thành công, không phải lỗi script dự án.

Chưa kiểm chứng: tích hợp giao dịch thật/UI/ô xây của B và standalone build (ngoài phạm vi buổi 11). Các ca dữ liệu lỗi bổ sung chạy qua MCP, chưa có nút Inspector tự động cho từng biến thể cấu hình lỗi. Không triển khai buổi 12.

## Tự thử trong Inspector

1. Mở CombatSandbox, Play, chờ session khởi tạo. Không chạy đồng thời các bộ kiểm tra khác.
2. Chọn TowerSession11 → menu ba chấm của FakeBuildCaller → Session 11 → Run All Checks. Xem Report và Console (khoảng 6 giây). Fixture được dọn sau lượt thử.
3. Thử thủ công các mục 1–7: Fake success, Fake failure, Upgrade II, Invalid upgrade, Sell, Dispose again, Upgrade after Dispose. Muốn quan sát bắn, dùng EnemySpawnTest hiện có tạo quái đi trong tầm tại buildPosition (-2,0,0).
4. Xem Gizmos tầm bắn, runtime tháp trong Inspector. Dữ liệu giữ nguyên range3 cho cả hai cấp nên nâng cấp không làm vòng tầm lớn hơn.
5. Stop Play; không Apply dữ liệu fixture trong Play vào prefab/data. Scene và catalog đã lưu ngoài Play.
