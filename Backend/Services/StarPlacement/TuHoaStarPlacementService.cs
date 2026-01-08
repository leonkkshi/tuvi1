namespace Backend.Services.StarPlacement;

/// <summary>
/// Service an Tứ Hóa (Hóa Lộc, Hóa Quyền, Hóa Khoa, Hóa Kỵ)
/// </summary>
public class TuHoaStarPlacementService : IStarPlacementService
{
    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        if (context.MainStarPositions == null)
            return positions;

        int yearCan = (context.Year + 6) % 10 + 1;

        // Bảng Tứ Hóa theo 10 Can
        // [Can][0]=Hóa Lộc, [Can][1]=Hóa Quyền, [Can][2]=Hóa Khoa, [Can][3]=Hóa Kỵ
        int[,] tuHoaTable =
        {
            {0,0,0,0},
            {6,14,4,3},  // Giáp
            {2,12,1,8},  // Ất
            {5,2,15,6},  // Bính
            {8,5,2,10},  // Đinh
            {9,8,18,2},  // Mậu
            {4,9,12,16}, // Kỷ
            {3,4,8,5}, // Canh - Thái Âm hóa Khoa, Thiên Đồng hóa Kỵ
            {10,3,16,15},// Tân
            {12,1,17,4}, // Nhâm
            {14,10,8,9}  // Quý
        };
        
        // Tìm vị trí của các sao chính để gắn Tứ Hóa
        for (int cung = 1; cung <= 12; cung++)
        {
            if (!context.MainStarPositions.ContainsKey(cung))
                continue;

            foreach (var starId in context.MainStarPositions[cung])
            {
                if (starId == tuHoaTable[yearCan, 0]) positions[cung].Add(25);
                if (starId == tuHoaTable[yearCan, 1]) positions[cung].Add(26);
                if (starId == tuHoaTable[yearCan, 2]) positions[cung].Add(27);
                if (starId == tuHoaTable[yearCan, 3]) positions[cung].Add(28);
            }
        }

        return positions;
    }
}
