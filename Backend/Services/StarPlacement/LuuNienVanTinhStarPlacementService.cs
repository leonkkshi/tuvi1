namespace Backend.Services.StarPlacement;

/// <summary>
/// Service an sao Lưu Niên Văn Tinh theo thiên can của năm sinh
/// </summary>
public class LuuNienVanTinhStarPlacementService : IStarPlacementService
{
    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Tính Can năm
        int yearCan = (context.Year - 3) % 10;
        if (yearCan <= 0) yearCan += 10;

        // Vị trí sao Lưu Niên Văn Tinh theo Can năm
        int palacePos = yearCan switch
        {
            1 => 6,   // Giáp: Tị
            2 => 7,   // Ất: Ngọ
            3 => 9,   // Bính: Thân
            4 => 10,  // Đinh: Dậu
            5 => 9,   // Mậu: Thân
            6 => 10,  // Kỷ: Dậu
            7 => 12,  // Canh: Hợi
            8 => 1,   // Tân: Tý
            9 => 3,   // Nhâm: Dần
            10 => 4,  // Quý: Mão
            _ => 6
        };

        positions[palacePos].Add(109); // Lưu Niên Văn Tinh

        return positions;
    }
}