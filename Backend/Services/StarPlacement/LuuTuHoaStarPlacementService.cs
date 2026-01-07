namespace Backend.Services.StarPlacement;

/// <summary>
/// Service an Lưu Tứ Hóa (Lưu Hóa Lộc, Lưu Hóa Quyền, Lưu Hóa Khoa, Lưu Hóa Kỵ)
/// </summary>
public class LuuTuHoaStarPlacementService : IStarPlacementService
{
    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        if (context.MainStarPositions == null)
            return positions;

        // Tính Can năm xem hạn
        int yearCan = (context.ViewYear + 6) % 10 + 1;

        // Bảng Lưu Tứ Hóa theo 10 Can (giống Tứ Hóa gốc)
        // [Can][0]=Lưu Hóa Lộc, [Can][1]=Lưu Hóa Quyền, [Can][2]=Lưu Hóa Khoa, [Can][3]=Lưu Hóa Kỵ
        int[,] luuTuHoaTable =
        {
            {0,0,0,0},
            {6,14,4,3},  // Giáp
            {2,12,1,8},  // Ất
            {5,2,15,6},  // Bính
            {8,5,2,10},  // Đinh
            {9,8,18,2},  // Mậu
            {4,9,12,16}, // Kỷ
            {3,4,5,8},   // Canh
            {10,3,16,15},// Tân
            {12,1,17,4}, // Nhâm
            {14,10,8,9}  // Quý
        };
        
        // Tìm vị trí của các sao chính để gắn Lưu Tứ Hóa
        for (int cung = 1; cung <= 12; cung++)
        {
            if (!context.MainStarPositions.ContainsKey(cung))
                continue;

            foreach (var starId in context.MainStarPositions[cung])
            {
                // Lưu Hóa Lộc (120)
                if (starId == luuTuHoaTable[yearCan, 0]) positions[cung].Add(120);
                // Lưu Hóa Quyền (121)
                if (starId == luuTuHoaTable[yearCan, 1]) positions[cung].Add(121);
                // Lưu Hóa Khoa (122)
                if (starId == luuTuHoaTable[yearCan, 2]) positions[cung].Add(122);
                // Lưu Hóa Kỵ (123)
                if (starId == luuTuHoaTable[yearCan, 3]) positions[cung].Add(123);
            }
        }

        return positions;
    }
}
