# Kiểm tra chuột trong Unity Game View

2026-11-16, Unity 6000.3.11f1, Game View Free Aspect khoảng 1100×500.

- Đã click dấu xây 01, 02, 03, 04, 05; mỗi lần HUD hiển thị đúng ID được chọn.
- Điểm 05 không còn bị panel đáy che; menu ở góc trái dưới.
- Tại điểm 05 click Buy Arrow: 240 → 140 vàng, tháp hiện đúng chân dấu xây.
- Click Upgrade: 140 → 40 vàng, tháp II, vốn thực chi 200.
- Click Sell: 40 → 180 vàng, hoàn 140, tháp biến mất, ô trống.
- Click UI giữ lựa chọn điểm 05, không xuyên xuống map. Vị trí Buy không trùng Upgrade sau chuyển menu.
- Đã nhìn thấy Victory cuối lượt tự động sáu wave: 205 vàng, 20 mạng, wave6/6, alive0.

Phần chưa xác nhận bằng quan sát thủ công đầy đủ: từng frame tại mọi khúc cua của cả hai tuyến, mọi tỷ lệ màn hình, cả sáu wave chỉ bằng click tay. Lượt sáu wave trong log là automation gọi API gameplay thật, không cheat. Mua/nâng/bán đủ năm điểm đã kiểm bằng service thật; thao tác chuột mua/nâng/bán được thử trực tiếp tại điểm sát đáy 05.

Không thấy yêu cầu phải thay tọa độ hay range qua kết quả hiện tại. Cần người chơi đánh giá thêm cảm giác nhịp độ/độ khó; chưa chỉnh balance.
