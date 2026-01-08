namespace Backend.Services.StarPlacement;

/// <summary>
/// Service an sao Thiên Quan theo thiên can của năm sinh
/// </summary>
public class ThienQuanStarPlacementService : IStarPlacementService
{
    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Tính Can năm
        int yearCan = (context.Year - 3) % 10;
        if (yearCan <= 0) yearCan += 10;

        // Vị trí sao Thiên Quan theo Can năm
        int thienQuanPos = yearCan switch
        {
            1 => 8,    // Giáp: Mùi
            2 => 5,   // Ất: Thìn
            3 => 6,   // Bính: Tỵ
            4 => 3,    // Đinh: Dần
            5 => 4,    // Mậu: Mão
            6 => 10,   // Kỷ: Dậu
            7 => 12,   // Canh: Hợi
            8 => 10,   // Tân: Dậu
            9 => 11,   // Nhâm: Tuất
            10 => 7,   // Quý: Ngọ
            _ => 8
        };

        positions[thienQuanPos].Add(110); // Thiên Quan

        return positions;
    }
}