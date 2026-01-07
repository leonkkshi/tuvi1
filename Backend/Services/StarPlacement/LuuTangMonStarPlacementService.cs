namespace Backend.Services.StarPlacement;

using Backend.Services.Utilities;

/// <summary>
/// Service an sao Lưu Tang Môn
/// </summary>
public class LuuTangMonStarPlacementService : IStarPlacementService
{
    private readonly BranchStemService _branchStemService = new();

    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Lấy Chi năm xem (đã là âm lịch)
        int yearBranch = _branchStemService.GetYearBranch(context.ViewYear);

        // Lưu Tang Môn an giống Tang Môn trong vòng Thái Tuế nhưng theo Chi năm xem hạn
        // Tang Môn là sao thứ 3 trong vòng Thái Tuế (Thái Tuế + 2)
        int luuTangMonPos = yearBranch + 2;
        if (luuTangMonPos > 12) luuTangMonPos -= 12;

        positions[luuTangMonPos].Add(118); // Lưu Tang Môn

        return positions;
    }
}