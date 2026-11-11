# Buổi 12 — Ghép lần đầu với B

Trạng thái: **Phần A đã kiểm tra; chờ tích hợp Level01**. Chưa đạt buổi 12/M3 tích hợp thật.

## Đầu vào

- Nhánh main, HEAD `01d8979092cfb7126def15d1e0ce30a641aaf4d9`.
- Có thay đổi chưa commit của buổi 10–11: Projectile/ShotContext, Tower, resolver/cleanup, TowerFactory/Handle/Snapshot/Catalog, các bộ debug, CombatSandbox và data đạn cung. Cả .vscode/settings.json, tài liệu DOCX, asset nguồn và slnx có sẵn được giữ nguyên. HEAD chưa đại diện toàn bộ code được thử.
- Đã đọc mục Buổi 12 trong Huong_dan_24_buoi_nguoi_A_DATN.docx và các nội dung phối hợp/M3 trong Quy_trinh_chi_tiet_DATN_Tower_Defense.docx. Không tìm thấy AGENTS.md hoặc Docs/Contracts.md trong project. Không tạo Shared thứ hai hoặc tự sửa Shared do B sở hữu.
- Level01 được kiểm tra bằng YAML trên đĩa, không mở/sửa/lưu: có bản đồ, EnemyPath_Spline, EnemyMovementTest/EnemyMovement. Chưa có cấu hình combat tích hợp. Tìm code trong Assets không thấy GameSession, WaveManager hoặc Wallet; chưa có ô xây/mua bán và công cụ duplicate của B để chạy kinh tế.

## Cấu hình B cần gán

1. Một CombatResolver duy nhất. Gọi BeginSession trước spawn/create; lấy SessionId từ resolver, không tự đoán ID.
2. EnemySpawnService: EnemyCatalog_Main và cùng resolver. Truyền SplineContainer của tuyến đường thật vào TrySpawn. Catalog normal dùng EnemyData_Normal (60 HP, bounty8, leak1).
3. TowerFactory: TowerCatalog.asset ánh xạ arrow → TowerData_Arrow; cùng resolver. Tháp và đạn lấy prefab từ data hiện có, không cần copy script.
4. B lưu Level01; tắt EnemyMovementTest, EnemySpawnTest/fake spawn, FakeBuildCaller, TowerDefinitionTest, TowerAttackTest, ProjectileTest và CombatTestRunner trong scene tích hợp.

**Khác tài liệu cần chốt:** runner hiện tại là `CombatResolver.LateUpdate`, không phải CombatTestRunner. Tìm caller trong Assets chỉ thấy resolver tự gọi ResolvePendingBatch. B không thêm runner gọi lần nữa. Producer gửi hit/escape trong Update. B có thể gom EnemySpawnService.Resolved vào buffer rồi xử lý ở LateUpdate với Script Execution Order sau CombatResolver; cập nhật toàn bộ kết quả trước khi xét thua/wave/thắng. Nếu muốn chuyển quyền runner sang B, hai bên phải đổi đồng bộ; A chưa đổi API một phía.

## Hợp đồng gọi

```csharp
if (spawnService.TrySpawn(resolver.SessionId, waveId, "normal", path, out var enemy))
{
    // B lưu enemy.Identity/handle, tăng spawned và alive trước khi cho chạy.
    spawnService.BeginMovement(enemy);
}
if (factory.TryCreate("arrow", 1, position, out var tower))
{
    tower.ReadSnapshot(out var snapshot); // Cost từ data chung; chưa bắn
    if (transactionSucceeded) tower.Activate();
    else tower.Dispose();
}
```

B xử lý Activate/BeginMovement trả false như lỗi giao dịch/kích hoạt, không để bộ đếm hoặc ô xây treo. A không trừ tiền. TryApplyLevel chỉ nhận cấp kế tiếp; Dispose gọi lặp an toàn. B giữ các handle để Suspend/Dispose khi bán hoặc restart.

EnemyResolved có Identity(SessionId,SpawnId), WaveId, State, Bounty, LeakDamage. B lọc session/wave, kiểm ID thuộc wave và dedup bằng Identity trước khi cập nhật vàng/mạng/alive. Dead cộng Bounty, Escaped trừ LeakDamage. Sau toàn bộ batch: xét thua trước, wave chỉ xong khi spawnFinished && alive == 0. Không suy ra spawnFinished từ registry rỗng.

Restart: B khóa giao dịch và dừng lịch spawn; Suspend/Dispose mọi tháp; gọi spawnService.CleanupSession(oldSession) trước resolver.BeginSession; xóa buffer/dedup/counters cũ rồi khởi tạo wave mới. Cleanup dọn cả enemy Death và projectile, không phát Dead/Escaped. BeginSession một mình không thay cho CleanupSession. Subscribe/Unsubscribe Resolved cân bằng một lần theo vòng đời, tránh đăng ký mỗi wave mà không bỏ.

## Ma trận kiểm thử

| Ca | Level01 | Sandbox/phân tích A |
|---|---|---|
| A: 5 normal + cung, wave hoàn tất đúng | Chưa kiểm tra: thiếu wave/session B | Bộ spawn được chạy riêng; không thay thế wave tích hợp với cung |
| B: 240→140, ô xây, rollback | Chưa kiểm tra: thiếu giao dịch/ô xây B | FakeBuildCaller 20/20 đạt: inactive trước Activate, rollback, vòng đời/nâng cấp |
| C: thưởng8 và duplicate không cộng lại | Chưa kiểm tra: thiếu GameSession và công cụ duplicate B | A phát payload và chống kết quả trùng; không chứng minh dedup của B |
| D: leak1, kill thắng escape cùng batch | Chưa kiểm tra mạng/vàng B | Kiểm resolver hit trước escape, CanResolve yêu cầu Alive; bộ spawn kiểm payload |
| E: restart đạn bay/Death | Chưa kiểm tra restart trận B | Có bộ ProjectileTest kiểm cleanup/session cũ; cần chạy và ghi riêng, không gọi là restart Level01 |

FakeBuildCaller chạy lại trong lượt buổi12: 20/20 đạt. Cooldown 0.399394/0.8→0.324508/0.65; nhịp cấp II đo0.652138 giây. Chi tiết tiêu chí ở Docs/PersonA/Session11.md.

EnemySpawnTest chạy lại: **15/15 đạt**. Năm SpawnId riêng; thời điểm sinh 21.9323, 22.7345, 23.5393, 24.34138, 25.14303 giây. Wave giả không có tháp active: Dead0, Escaped5, registry cuối0; không phải ca A Level01. Kiểm TrySpawn đứng chờ, BeginMovement, duplicate hit/escape cho một Dead(bounty8/leak0), duplicate escape cho một Escaped(bounty0/leak1), cleanup giữa lịch sinh không phát kết quả đều đạt.

ProjectileTest chạy lại: **23/23 đạt**, HP60→44→28→12→0; pause/resume, target hủy/chết, đạn sống sau dọn tháp, cleanup đạn và chặn session cũ đạt. Restart thật khi đang animation Death, dedup kinh tế và event của B vẫn **chưa kiểm tra**. Kết thúc đã Stop Play. Không thay scene/prefab/script trong buổi12, không có .meta mới cần tạo (chỉ tài liệu ngoài Assets).

## Dependency còn thiếu / bàn giao lỗi

- Scene Level01, input wave5 normal + cung: mong có luồng trận và alive cuối wave; thực tế chưa có GameSession/wave/cấu hình dịch vụ. Tái hiện bằng kiểm component YAML và tìm class trong Assets. B bổ sung và lưu scene; đây là dependency thiếu, chưa kết luận bug runtime B.
- Input mua cung100 từ240: chưa có caller giao dịch/ô xây nên chưa đo140. B triển khai caller theo thứ tự Create→commit→Activate, rollback Dispose và trả ô xây.
- Input gửi lặp EnemyResolved: chưa có công cụ B; B cần nút/test gửi cùng Identity hai lần và kiểm vàng/alive không đổi lần hai.
- Thứ tự batch: B phải xử lý đủ buffer trước kiểm endgame. Tài liệu yêu cầu runner thật nhưng code dùng resolver tự chạy; hướng dẫn trên tránh runner thừa.
- Chưa phát hiện lỗi A cần đổi API trong lần rà soát này. A sẽ kiểm lại Level01 sau khi B gán xong; không viết thay kinh tế/wave.

## Checklist M3

- [x] Factory và vòng đời tháp A chạy thật trong sandbox.
- [x] Hướng dẫn dependency, API và thứ tự batch/restart.
- [ ] B lưu Level01 và chốt commit B; hai bên thống nhất snapshot code A chưa commit.
- [ ] Chạy Level01 A–E, ghi SpawnId/Dead/Escaped/alive và vàng/mạng.
- [ ] Kiểm duplicate ở GameSession, restart khi Death, đăng ký event/runner sau nhiều lần restart.
- [ ] Video minh chứng M3: chưa quay, không có video bàn giao.
- [ ] Xác nhận đạt buổi12 bằng tích hợp thật.

Tự chạy A: mở CombatSandbox, Play, lần lượt FakeBuildCaller → Session11/Run All Checks; EnemySpawnTest → Debug/07; ProjectileTest → Session10/Run Projectile Checks. Chờ bộ trước xong, không chạy đồng thời. B dùng các bước cấu hình ở trên, sau đó chạy A–E trong Level01 và gửi report cho A.
