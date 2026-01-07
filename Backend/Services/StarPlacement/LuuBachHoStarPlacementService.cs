namespace Backend.Services.StarPlacement;

using Backend.Services.Utilities;

/// <summary>
/// Service an sao Lưu Bạch Hổ
/// </summary>
public class LuuBachHoStarPlacementService : IStarPlacementService
{
    private readonly BranchStemService _branchStemService = new();

    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Lấy Chi năm xem (đã là âm lịch)
        int yearBranch = _branchStemService.GetYearBranch(context.ViewYear);

        // Lưu Bạch Hổ an giống Bạch Hổ trong vòng Thái Tuế nhưng theo Chi năm xem hạn
        // Bạch Hổ là sao thứ 9 trong vòng Thái Tuế (Thái Tuế + 8)
        int luuBachHoPos = yearBranch + 8;
        if (luuBachHoPos > 12) luuBachHoPos -= 12;

        positions[luuBachHoPos].Add(119); // Lưu Bạch Hổ

        return positions;
    }
}