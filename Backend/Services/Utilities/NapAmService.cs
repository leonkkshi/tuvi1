namespace Backend.Services.Utilities;

/// <summary>
/// Service xử lý Nạp Âm của 60 Hoa Giáp
/// </summary>
public class NapAmService
{
    private static readonly string[][] NapAmTable = new string[11][];

    static NapAmService()
    {
        // Initialize the table
        for (int i = 0; i <= 10; i++)
        {
            NapAmTable[i] = new string[13];
        }

        // Populate the Nạp Âm table based on the 60 Hoa Giáp cycle
        NapAmTable[1][1] = "Hải Trung Kim";   // Giáp Tý
        NapAmTable[2][2] = "Hải Trung Kim";   // Ất Sửu
        NapAmTable[3][3] = "Lư Trung Hỏa";    // Bính Dần
        NapAmTable[4][4] = "Lư Trung Hỏa";    // Đinh Mão
        NapAmTable[5][5] = "Đại Lâm Mộc";     // Mậu Thìn
        NapAmTable[6][6] = "Đại Lâm Mộc";     // Kỷ Tỵ
        NapAmTable[7][7] = "Lộ Bàng Thổ";     // Canh Ngọ
        NapAmTable[8][8] = "Lộ Bàng Thổ";     // Tân Mùi
        NapAmTable[9][9] = "Kiếm Phong Kim";  // Nhâm Thân
        NapAmTable[10][10] = "Kiếm Phong Kim"; // Quý Dậu
        NapAmTable[1][11] = "Sơn Đầu Hỏa";    // Giáp Tuất
        NapAmTable[2][12] = "Sơn Đầu Hỏa";    // Ất Hợi
        NapAmTable[3][1] = "Giản Hạ Thủy";    // Bính Tý
        NapAmTable[4][2] = "Giản Hạ Thủy";    // Đinh Sửu
        NapAmTable[5][3] = "Thành Đầu Thổ";   // Mậu Dần
        NapAmTable[6][4] = "Thành Đầu Thổ";   // Kỷ Mão
        NapAmTable[7][5] = "Bạch Lạp Kim";    // Canh Thìn
        NapAmTable[8][6] = "Bạch Lạp Kim";    // Tân Tỵ
        NapAmTable[9][7] = "Dương Liễu Mộc";  // Nhâm Ngọ
        NapAmTable[10][8] = "Dương Liễu Mộc"; // Quý Mùi
        NapAmTable[1][9] = "Tuyền Trung Thủy";  // Giáp Thân
        NapAmTable[2][10] = "Tuyền Trung Thủy"; // Ất Dậu
        NapAmTable[3][11] = "Ốc Thượng Thổ";   // Bính Tuất
        NapAmTable[4][12] = "Ốc Thượng Thổ";   // Đinh Hợi
        NapAmTable[5][1] = "Tích Lịch Hỏa";    // Mậu Tý
        NapAmTable[6][2] = "Tích Lịch Hỏa";    // Kỷ Sửu
        NapAmTable[7][3] = "Tùng Bách Mộc";    // Canh Dần
        NapAmTable[8][4] = "Tùng Bách Mộc";    // Tân Mão
        NapAmTable[9][5] = "Trường Lưu Thủy";  // Nhâm Thìn
        NapAmTable[10][6] = "Trường Lưu Thủy"; // Quý Tỵ
        NapAmTable[1][7] = "Sa Trung Kim";     // Giáp Ngọ
        NapAmTable[2][8] = "Sa Trung Kim";     // Ất Mùi
        NapAmTable[3][9] = "Sơn Hạ Hỏa";       // Bính Thân
        NapAmTable[4][10] = "Sơn Hạ Hỏa";      // Đinh Dậu
        NapAmTable[5][11] = "Bình Địa Mộc";   // Mậu Tuất
        NapAmTable[6][12] = "Bình Địa Mộc";   // Kỷ Hợi
        NapAmTable[7][1] = "Bích Thượng Thổ";  // Canh Tý
        NapAmTable[8][2] = "Bích Thượng Thổ";  // Tân Sửu
        NapAmTable[9][3] = "Kim Bạch Kim";      // Nhâm Dần
        NapAmTable[10][4] = "Kim Bạch Kim";     // Quý Mão
        NapAmTable[1][5] = "Phúc Đăng Hỏa";    // Giáp Thìn
        NapAmTable[2][6] = "Phúc Đăng Hỏa";    // Ất Tỵ
        NapAmTable[3][7] = "Thiên Hà Thủy";    // Bính Ngọ
        NapAmTable[4][8] = "Thiên Hà Thủy";    // Đinh Mùi
        NapAmTable[5][9] = "Đại Dịch Thổ";     // Mậu Thân
        NapAmTable[6][10] = "Đại Dịch Thổ";    // Kỷ Dậu
        NapAmTable[7][11] = "Thoa Xuyến Kim";  // Canh Tuất
        NapAmTable[8][12] = "Thoa Xuyến Kim";  // Tân Hợi
        NapAmTable[9][1] = "Tang Chi Mộc";     // Nhâm Tý
        NapAmTable[10][2] = "Tang Chi Mộc";    // Quý Sửu
        NapAmTable[1][3] = "Đại Khê Thủy";     // Giáp Dần
        NapAmTable[2][4] = "Đại Khê Thủy";     // Ất Mão
        NapAmTable[3][5] = "Sa Trung Thổ";     // Bính Thìn
        NapAmTable[4][6] = "Sa Trung Thổ";     // Đinh Tỵ
        NapAmTable[5][7] = "Thiên Thượng Hỏa"; // Mậu Ngọ
        NapAmTable[6][8] = "Thiên Thượng Hỏa"; // Kỷ Mùi
        NapAmTable[7][9] = "Thạch Lựu Mộc";    // Canh Thân
        NapAmTable[8][10] = "Thạch Lựu Mộc";   // Tân Dậu
        NapAmTable[9][11] = "Đại Hải Thủy";     // Nhâm Tuất
        NapAmTable[10][12] = "Đại Hải Thủy";    // Quý Hợi
    }

    /// <summary>
    /// Lấy Nạp Âm từ Thiên Can và Địa Chi
    /// </summary>
    /// <param name="can">Thiên Can (1-10)</param>
    /// <param name="branch">Địa Chi (1-12)</param>
    /// <returns>Tên Nạp Âm</returns>
    public string GetNapAm(int can, int branch)
    {
        if (can < 1 || can > 10 || branch < 1 || branch > 12)
        {
            return "";
        }
        return NapAmTable[can][branch] ?? "";
    }

    /// <summary>
    /// Lấy Nạp Âm từ năm âm lịch
    /// </summary>
    /// <param name="lunarYear">Năm âm lịch</param>
    /// <returns>Tên Nạp Âm</returns>
    public string GetNapAmFromYear(int lunarYear)
    {
        var branchStemService = new BranchStemService();
        int can = branchStemService.GetYearCan(lunarYear);
        int branch = branchStemService.GetYearBranch(lunarYear);
        return GetNapAm(can, branch);
    }
}