# Level01 chính thức

## Mở và chơi

Mở `Assets/_GAME/Scenes/Level01.unity`, nhấn Play. Chọn một trong năm dấu xây, mua Cung rồi nhấn Start Wave cho từng đợt. Pause/Resume dừng/tiếp tục; Restart bắt đầu lượt mới. Không có nút về menu vì dự án chưa có scene menu được xác nhận hợp lệ.

## Bố cục bảo toàn

Tham chiếu `level01-layout.png`. Không tạo lại map, không sửa cây/đá/công trình/camera, không thêm điểm xây. Hai container đều chỉ có spline index 0:

- `route_left` → `EnemyPath_Spline 1`: (-8.994, -1.630) tới (2.446, 2.505), dài 15.109.
- `route_right` → `EnemyPath_Spline 2`: (9.590, -3.269) tới (2.473, 2.477), dài 16.214.

Cả hai đã đúng chiều tới ngưỡng cửa hầm; không đảo chiều, không ghép hai đường. `Level01-geometry-audit.txt` ghi tọa độ năm điểm và độ phủ. `Level01-preservation-check.txt` so với scene người dùng đã lưu trước chỉnh sửa.

`DATN/Integration/Configure Level01 Gameplay` hiện chỉ nối tham chiếu trên bố cục có sẵn, từ chối nếu không có đúng năm điểm hoặc hai container đơn spline. Không còn bảng tọa độ tám điểm. Lệnh này lưu scene hiện tại trước khi nối component.

## Dữ liệu

240 vàng, 20 mạng; thưởng hoàn tất wave 20 vàng đúng một lần. Cung có ngay; Đá chỉ mua sau khi hoàn tất wave 3. Bán hoàn floor(vốn thực chi × 7/10). TowerCatalog/TowerData là nguồn giá và chỉ số cho UI/gameplay.

| Tháp | Damage | Cooldown | Radius | Giá mua/nâng | Range |
|---|---:|---:|---:|---:|---:|
| Cung I | 16 | 0.8 | — | 100 | 3 |
| Cung II | 28 | 0.65 | — | 100 | 3 |
| Đá I | 30 | 2 | 1 | 140 | 3 |
| Đá II | 50 | 1.8 | 1.25 | 140 | 3 |

Normal: HP60, speed1.2, armor0, bounty8, leak1. Runner: HP40, speed1.9, armor0, bounty7, leak1. Runner dùng bản sao prefab Normal với controller Walk/Death nhiều frame đang có; không tuyên bố có bộ animation runner riêng. Cùng ngoại hình, khác tốc độ và data.

## Lịch wave

Mỗi hàng là một nhóm. `delay` tính sau lần spawn cuối nhóm trước; không đợi quái chết hết. Dữ liệu cũ không có groups vẫn đọc được qua fallback một nhóm normal/route_left; ba asset Level01 giữ GUID và được migrate sang groups.

| Wave | Enemy | Route | Count | Interval | Delay |
|---|---|---|---:|---:|---:|
| 1 | normal | route_left | 5 | 1.8 | 0 |
| 2 | normal | route_left | 8 | 1.5 | 0 |
| 3 | normal | route_left | 4 | 1.3 | 0 |
| 3 | runner | route_right | 3 | 1.3 | 2 |
| 3 | normal | route_right | 3 | 1.3 | 1 |
| 4 | normal | route_left | 6 | 0.8 | 0 |
| 4 | normal | route_right | 6 | 0.8 | 1 |
| 5 | normal | route_left | 5 | 1.1 | 0 |
| 5 | runner | route_right | 4 | 1.0 | 3 |
| 5 | normal | route_left | 5 | 1.1 | 1 |
| 6 | normal | route_left | 8 | 0.9 | 0 |
| 6 | runner | route_right | 4 | 0.9 | 1 |
| 6 | normal | route_right | 6 | 0.9 | 1 |

## Hợp đồng gameplay

WaveData chỉ chứa routeId; ánh xạ container nằm trên WaveManager trong scene. SpawnService khởi tạo đúng đường, đăng ký combat identity; WaveManager đăng ký alive trước BeginMovement. Một lịch duy nhất, scaled deltaTime; nhóm sau chỉ áp dụng delay riêng, không cộng interval nhóm cũ. Restart hủy lịch.

Targeting quét registry mỗi 0.1s, chọn ít khoảng cách còn lại dọc spline nhất, hòa thì SpawnId nhỏ nhất. SplineAnimate dùng speed không easing, do đó remaining=(1-normalizedTime)×chiều dài đường thực; chiều dài được cache theo container, không tính lại mỗi frame.

Đạn đá chốt impact lúc bắn, consumed trước xử lý, lấy quái sống cùng session tại lúc nổ, xét khoảng cách XY và HashSet identity, gửi HitRequest qua CombatResolver duy nhất. Không phụ thuộc route.

CombatResolver xử lý hit trước escape. GameSession drain hết batch, xét Defeat trước WaveManager; chỉ khi spawnFinished và alive=0 mới thưởng hoặc Victory. Các guard/restart hiện có được giữ lại.

## Kiểm chứng

Xem `Level01-playmode-results.txt` để phân biệt kiểm thử fixture tổng hợp và lượt chơi sáu wave bằng API gameplay thật, không thêm vàng/damage/spawn trong lượt chơi. Kiểm tra fixture không thay thế kiểm tra thao tác chuột trực tiếp và quan sát mọi khúc cua. Báo cáo cuối sẽ ghi rõ phần còn chưa kiểm chứng.

Lượt chơi tự động thực tế trên Unity 6000.3.11f1 dùng timeScale=1, chỉ chi vàng kiếm được. Bắt đầu Cung I ở điểm 02 và 03; sau wave 1 nâng điểm 03; sau wave 3 mua Đá tại điểm 04; sau wave 4 nâng Cung điểm 02; sau wave 5 nâng Đá điểm 04. Mỗi wave được gọi Start riêng qua cùng API nút HUD. Đây là automation qua service, không phải hoàn tất sáu wave bằng thao tác chuột thủ công.

| Wave | Vàng sau thưởng, trước mua/nâng tiếp | Mạng | Chết | Lọt |
|---|---:|---:|---:|---:|
| 1 | 100 | 20 | 5 | 0 |
| 2 | 84 | 20 | 8 | 0 |
| 3 | 181 | 20 | 10 | 0 |
| 4 | 157 | 20 | 12 | 0 |
| 5 | 185 | 20 | 14 | 0 |
| 6 | 205 | 20 | 18 | 0 |

Không đổi count, interval hoặc range để cân bằng. `Level01-six-wave-results.txt` là log lượt này; `Level01-playmode-results.txt` là lượt kiểm tra biên cuối trên HUD đã sửa. `Level01-layout-final.png` là Game View cuối với menu điểm 05; `Level01-final-gameview.png` ghi nhận Victory của lượt sáu wave (trước khi thu gọn HUD).
