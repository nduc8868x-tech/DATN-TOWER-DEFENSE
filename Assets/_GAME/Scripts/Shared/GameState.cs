public enum GameState
{
    Preparing,  // Đang chuẩn bị (xây tháp, mở settings, chuẩn bị bấm Start Wave)
    Running,    // Đang chiến đấu (quái đang chạy, tháp đang bắn)
    Paused,     // Tạm dừng game
    Victory,    // Thắng trận (hết 10 wave và còn mạng)
    Defeat      // Thua trận (hết mạng)
}
