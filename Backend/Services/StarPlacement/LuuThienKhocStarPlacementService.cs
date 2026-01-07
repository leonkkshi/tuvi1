namespace Backend.Services.StarPlacement;

using Backend.Services.Utilities;

/// <summary>
/// Service an sao Lưu Thiên Khốc
/// </summary>
public class LuuThienKhocStarPlacementService : IStarPlacementService
{
    private readonly BranchStemService _branchStemService = new();

    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Lấy Chi năm xem (đã là âm lịch)
        int yearBranch = _branchStemService.GetYearBranch(context.ViewYear);

        // Lưu Thiên Khốc an giống Thiên Khốc nhưng theo Chi năm xem hạn
        // Công thức: Ngọ là Tý đi ngược
        int luuThienKhocPos = 7 - (yearBranch - 1);
        if (luuThienKhocPos <= 0) luuThienKhocPos += 12;

        positions[luuThienKhocPos].Add(116); // Lưu Thiên Khốc

        return positions;
    }
}