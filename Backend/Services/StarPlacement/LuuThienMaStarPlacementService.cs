namespace Backend.Services.StarPlacement;

using Backend.Services.Utilities;

/// <summary>
/// Service an sao Lưu Thiên Mã
/// </summary>
public class LuuThienMaStarPlacementService : IStarPlacementService
{
    private readonly BranchStemService _branchStemService = new();

    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Lấy Chi năm xem (đã là âm lịch)
        int yearBranch = _branchStemService.GetYearBranch(context.ViewYear);

        // Lưu Thiên Mã an giống Thiên Mã nhưng theo Chi năm xem hạn (tam hợp)
        int luuThienMaPos = yearBranch switch
        {
            1 or 5 or 9 => 3,    // Tý, Thìn, Thân -> Dần
            2 or 6 or 10 => 12,  // Sửu, Tỵ, Dậu -> Hợi
            3 or 7 or 11 => 9,   // Dần, Ngọ, Tuất -> Thân
            4 or 8 or 12 => 6,   // Mão, Mùi, Hợi -> Tỵ
            _ => 3
        };

        positions[luuThienMaPos].Add(113); // Lưu Thiên Mã

        return positions;
    }
}