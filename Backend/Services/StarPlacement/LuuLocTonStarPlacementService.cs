namespace Backend.Services.StarPlacement;

using Backend.Services.Utilities;

/// <summary>
/// Service an sao Lưu Lộc Tồn
/// </summary>
public class LuuLocTonStarPlacementService : IStarPlacementService
{
    private readonly BranchStemService _branchStemService = new();

    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Lấy Can năm xem (đã là âm lịch)
        int yearCan = _branchStemService.GetYearCan(context.ViewYear);

        // Lưu Lộc Tồn an giống Lộc Tồn nhưng theo Can năm xem hạn
        // Giáp-Kỷ=Dần, Ất-Canh=Mão, Bính-Tân=Tỵ, Đinh-Nhâm=Ngọ, Mậu=Tỵ, Quý=Tý
        int luuLocTonPos = yearCan switch
        {
            1 => 3,    // Giáp -> Dần
            2 => 4,    // Ất -> Mão
            3 => 6,    // Bính -> Tỵ
            4 => 7,    // Đinh -> Ngọ
            5 => 6,    // Mậu -> Tỵ
            6 => 7,    // Kỷ -> Ngọ
            7 => 9,    // Canh -> Thân
            8 => 10,   // Tân -> Dậu
            9 => 12,   // Nhâm -> Hợi
            10 => 1,   // Quý -> Tý
            _ => 3
        };

        positions[luuLocTonPos].Add(112); // Lưu Lộc Tồn

        return positions;
    }
}