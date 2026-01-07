namespace Backend.Services.StarPlacement;

/// <summary>
/// Interface cơ sở cho các service an sao
/// </summary>
public interface IStarPlacementService
{
    /// <summary>
    /// An sao vào các cung
    /// </summary>
    /// <returns>Dictionary với key là vị trí cung (1-12), value là list các ID sao</returns>
    Dictionary<int, List<int>> PlaceStars(StarPlacementContext context);
}

/// <summary>
/// Context chứa thông tin cần thiết để an sao
/// </summary>
public class StarPlacementContext
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public bool IsMale { get; set; }
    
    public int MenhPalace { get; set; }
    public int ThanPalace { get; set; }
    public int NobocPalace { get; set; }
    public int TatAchPalace { get; set; }
    
    public string AmDuong { get; set; } = "";
    public int NguHanhCuc { get; set; }
    
    // Năm xem hạn
    public int ViewYear { get; set; }
    
    // Vị trí các sao chính (dùng cho Tứ Hóa)
    public Dictionary<int, List<int>>? MainStarPositions { get; set; }
}
