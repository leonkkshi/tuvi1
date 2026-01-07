namespace Backend.Services.StarPlacement;

using Backend.Services.Utilities;

/// <summary>
/// Service an sao Lưu Thái Tuế
/// </summary>
public class LuuThaiTueStarPlacementService : IStarPlacementService
{
    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Tính Chi năm xem
        int yearBranch = ((context.ViewYear - 3) % 12);
        if (yearBranch <= 0) yearBranch += 12;

        // Lưu Thái Tuế an tại cung Chi năm xem
        positions[yearBranch].Add(111); // Lưu Thái Tuế

        return positions;
    }
}