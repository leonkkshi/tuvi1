namespace Backend.Services.Utilities;

/// <summary>
/// Service tính toán các cung và vị trí liên quan
/// </summary>
public class PalaceCalculationService
{
    /// <summary>
    /// Chuyển đổi giờ sang địa chi (1=Tý, 2=Sửu, ..., 12=Hợi)
    /// </summary>
    public int GetHourBranch(int hour)
    {
        if (hour >= 23 || hour < 1) return 1;       // Tý
        if (hour >= 1 && hour < 3) return 2;        // Sửu
        if (hour >= 3 && hour < 5) return 3;        // Dần
        if (hour >= 5 && hour < 7) return 4;        // Mão
        if (hour >= 7 && hour < 9) return 5;        // Thìn
        if (hour >= 9 && hour < 11) return 6;       // Tỵ
        if (hour >= 11 && hour < 13) return 7;      // Ngọ
        if (hour >= 13 && hour < 15) return 8;      // Mùi
        if (hour >= 15 && hour < 17) return 9;      // Thân
        if (hour >= 17 && hour < 19) return 10;     // Dậu
        if (hour >= 19 && hour < 21) return 11;     // Tuất
        return 12;                                   // Hợi
    }

    /// <summary>
    /// An cung Mệnh: tháng 1 tại Dần (3), đếm thuận theo tháng, rồi nghịch theo giờ
    /// </summary>
    public int CalculateMenhPalace(int lunarMonth, int hourBranch)
    {
        int monthPos = NormalizePos(3 + (lunarMonth - 1));
        int menhPos = NormalizePos(monthPos - (hourBranch - 1));
        return menhPos;
    }

    /// <summary>
    /// Xây dựng mapping từ vị trí cung (1-12) sang tên cung
    /// </summary>
    public Dictionary<int, string> BuildPalaceNameByPosition(int menhPalace)
    {
        string[] palaceNames = new[]
        {
            "Mệnh", "Phụ Mẫu", "Phúc Đức", "Điền Trạch", "Quan Lộc", "Nô Bộc",
            "Thiên Di", "Tật Ách", "Tài Bạch", "Tử Tức", "Phu Thê", "Huynh Đệ"
        };

        var result = new Dictionary<int, string>();
        for (int i = 0; i < 12; i++)
        {
            int pos = NormalizePos(menhPalace + i);
            result[pos] = palaceNames[i];
        }

        return result;
    }

    /// <summary>
    /// Tính Âm Dương theo năm sinh và giới tính
    /// </summary>
    public string CalculateAmDuong(int lunarYear, bool isMale)
    {
        int yearCan = (lunarYear - 3) % 10;
        if (yearCan <= 0) yearCan += 10;

        bool isDuongCan = (yearCan == 1 || yearCan == 3 || yearCan == 5 || yearCan == 7 || yearCan == 9);

        if (isMale && isDuongCan) return "Dương Nam";
        if (isMale && !isDuongCan) return "Âm Nam";
        if (!isMale && isDuongCan) return "Dương Nữ";
        return "Âm Nữ";
    }

    /// <summary>
    /// Tính Ngũ Hành Cục theo năm sinh và vị trí cung Mệnh
    /// Bảng tra theo Can năm và vị trí Mệnh:
    /// - Cột 1: Tý-Sửu (1-2)
    /// - Cột 2: Dần-Mão (3-4)
    /// - Cột 3: Thìn-Tỵ (5-6)
    /// - Cột 4: Ngọ-Mùi (7-8)
    /// - Cột 5: Thân-Dậu (9-10)
    /// - Cột 6: Tuất-Hợi (11-12)
    /// Giá trị: 2=Thủy nhị, 3=Mộc tam, 4=Kim tứ, 5=Thổ ngũ, 6=Hỏa lục
    /// </summary>
    public int CalculateNguHanhCuc(int lunarYear, int menhPalace)
    {
        int yearCan = (lunarYear - 3) % 10;
        if (yearCan <= 0) yearCan += 10;

        // Bảng Cục theo Can năm và Mệnh cung
        // Hàng: Can năm (1=Giáp, 2=Ất, 3=Bính, 4=Đinh, 5=Mậu, 6=Kỷ, 7=Canh, 8=Tân, 9=Nhâm, 10=Quý)
        // Cột: Tý-Sửu, Dần-Mão, Thìn-Tỵ, Ngọ-Mùi, Thân-Dậu, Tuất-Hợi
        int[,] cucTable = {
            {2, 6, 3, 5, 4, 6}, // Giáp (1)
            {6, 5, 4, 3, 2, 5}, // Ất (2)
            {5, 3, 2, 4, 6, 3}, // Bính (3)
            {3, 4, 6, 2, 5, 4}, // Đinh (4)
            {4, 2, 5, 6, 3, 2}, // Mậu (5)
            {2, 6, 3, 5, 4, 6}, // Kỷ (6)
            {6, 5, 4, 3, 2, 5}, // Canh (7)
            {5, 3, 2, 4, 6, 3}, // Tân (8)
            {3, 4, 6, 2, 5, 4}, // Nhâm (9)
            {4, 2, 5, 6, 3, 2}  // Quý (10)
        };

        // Xác định cột index theo vị trí Mệnh
        int colIndex = 0;
        if (menhPalace == 1 || menhPalace == 2) colIndex = 0;      // Tý-Sửu
        else if (menhPalace == 3 || menhPalace == 4) colIndex = 1; // Dần-Mão
        else if (menhPalace == 5 || menhPalace == 6) colIndex = 2; // Thìn-Tỵ
        else if (menhPalace == 7 || menhPalace == 8) colIndex = 3; // Ngọ-Mùi
        else if (menhPalace == 9 || menhPalace == 10) colIndex = 4; // Thân-Dậu
        else if (menhPalace == 11 || menhPalace == 12) colIndex = 5; // Tuất-Hợi

        return cucTable[yearCan - 1, colIndex];
    }

    /// <summary>
    /// Chuẩn hóa vị trí về khoảng 1-12
    /// </summary>
    public int NormalizePos(int pos)
    {
        pos %= 12;
        if (pos <= 0) pos += 12;
        return pos;
    }
}
