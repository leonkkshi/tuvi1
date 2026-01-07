namespace Backend.Services.StarPlacement;

using Backend.Services.Utilities;

/// <summary>
/// Service an sao Lưu Kình Dương
/// </summary>
public class LuuKinhDuongStarPlacementService : IStarPlacementService
{
    private readonly BranchStemService _branchStemService = new();

    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        // Lấy Can năm xem (đã là âm lịch)
        int yearCan = _branchStemService.GetYearCan(context.ViewYear);

        // Lưu Kình Dương an giống Kình Dương nhưng theo Can năm xem hạn
        // Tính vị trí Lưu Lộc Tồn trước
        int luuLocTonPos = yearCan switch
        {
            1 => 3,    // Giáp -> Dần
            2 => 4,    // Ất -> Mão
            3 => 6,    // Bính -> Tị
            4 => 7,    // Đinh -> Ngọ
            5 => 6,    // Mậu -> Tị
            6 => 7,    // Kỷ -> Ngọ
            7 => 9,    // Canh -> Thân
            8 => 10,   // Tân -> Dậu
            9 => 12,   // Nhâm -> Hợi
            10 => 1,   // Quý -> Tý
            _ => 3
        };

        // Lưu Kình Dương từ vị trí Lưu Lộc Tồn tiến 1 ô thuận
        int luuKinhDuongPos = luuLocTonPos + 1;
        if (luuKinhDuongPos > 12) luuKinhDuongPos -= 12;

        positions[luuKinhDuongPos].Add(114); // Lưu Kình Dương

        return positions;
    }
}