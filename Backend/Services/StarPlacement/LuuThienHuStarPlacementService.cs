namespace Backend.Services.StarPlacement;

using Backend.Services.Utilities;

/// <summary>
/// Service an sao Lưu Thiên Hư
/// </summary>
public class LuuThienHuStarPlacementService : IStarPlacementService
{
    private readonly BranchStemService _branchStemService = new();

    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Lấy Chi năm xem (đã là âm lịch)
        int yearBranch = _branchStemService.GetYearBranch(context.ViewYear);

        // Lưu Thiên Hư an giống Thiên Hư (cùng Tuế Phá) nhưng theo Chi năm xem hạn
        // Thiên Hư cùng Tuế Phá (Thái Tuế + 6)
        int luuThienHuPos = yearBranch + 6;
        if (luuThienHuPos > 12) luuThienHuPos -= 12;

        positions[luuThienHuPos].Add(117); // Lưu Thiên Hư

        return positions;
    }
}