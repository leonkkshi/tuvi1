namespace Backend.Services.StarPlacement;

/// <summary>
/// Service an 14 chính tinh theo bảng Tử Vi chuẩn.
/// </summary>
public class MainStarPlacementService : IStarPlacementService
{
    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        int nguHanhCuc = context.NguHanhCuc;
        int day = context.Day;

        // Tra bảng Tử Vi theo ngày sinh và cục
        int tuViPos = GetTuViPosition(nguHanhCuc, day);

        // An 14 chính tinh theo quy tắc từ Tử Vi
        // Nhóm 1: Từ Tử Vi đi ngược chiều kim đồng hồ
        positions[tuViPos].Add(1); // Tử Vi
        
        int thienCoPos = Prev(tuViPos, 1); // Thiên Cơ: bên cạnh Tử Vi (ngược 1)
        positions[thienCoPos].Add(2);
        
        int thaiDuongPos = Prev(thienCoPos, 2); // Thái Dương: cách Thiên Cơ 1 ô (bỏ qua 1 cung = ngược 2)
        positions[thaiDuongPos].Add(3);
        
        int vuKhucPos = Prev(thaiDuongPos, 1); // Vũ Khúc: cung tiếp theo (liền kề, ngược 1)
        positions[vuKhucPos].Add(4);
        
        int thienDongPos = Prev(vuKhucPos, 1); // Thiên Đồng: cung tiếp theo (liền kề, ngược 1)
        positions[thienDongPos].Add(5);
        
        int liemTrinhPos = Prev(thienDongPos, 3); // Liêm Trinh: cách Thiên Đồng 2 ô (bỏ qua 2 cung = ngược 3)
        positions[liemTrinhPos].Add(6);

        // Nhóm 2: Thiên Phủ đối xứng với Tử Vi qua trục Dần(3)-Thân(9)
        int thienPhuPos = GetSymmetricPosition(tuViPos);
        positions[thienPhuPos].Add(7); // Thiên Phủ
        
        // Từ Thiên Phủ đi thuận chiều kim đồng hồ
        int thaiAmPos = Next(thienPhuPos, 1); // Thái Âm
        positions[thaiAmPos].Add(8);
        
        int thamLangPos = Next(thaiAmPos, 1); // Tham Lang
        positions[thamLangPos].Add(9);
        
        int cuMonPos = Next(thamLangPos, 1); // Cự Môn
        positions[cuMonPos].Add(10);
        
        int thienTuongPos = Next(cuMonPos, 1); // Thiên Tướng
        positions[thienTuongPos].Add(11);
        
        int thienLuongPos = Next(thienTuongPos, 1); // Thiên Lương
        positions[thienLuongPos].Add(12);
        
        int thatSatPos = Next(thienLuongPos, 1); // Thất Sát
        positions[thatSatPos].Add(13);
        
        int phaQuanPos = Next(thatSatPos, 4); // Phá Quân: bỏ 3 cung (tức là +4)
        positions[phaQuanPos].Add(14);

        return positions;
    }

    /// <summary>
    /// Tra bảng Tử Vi dựa vào Cục và Ngày sinh
    /// Bảng tra cứu chuẩn từ Tử Vi học
    /// </summary>
    private int GetTuViPosition(int cuc, int day)
    {
        // Bảng tra cứu Tử Vi theo 5 cục
        // Mỗi cục có các ngày tương ứng với 12 cung (Tý=1, Sửu=2, ... Hợi=12)
        
        return cuc switch
        {
            2 => GetThuiNhiCuc(day),   // Thủy Nhị Cục
            3 => GetMocTamCuc(day),    // Mộc Tam Cục
            4 => GetKimTuCuc(day),     // Kim Tứ Cục
            5 => GetThoNguCuc(day),    // Thổ Ngũ Cục
            6 => GetHoaLucCuc(day),    // Hỏa Lục Cục
            _ => 1 // Mặc định Tý
        };
    }

    // Thủy Nhị Cục
    private int GetThuiNhiCuc(int day)
    {
        return day switch
        {
            2 or 3 => 1,      // Tý
            4 or 5 => 2,      // Sửu
            6 or 7 => 3,      // Dần
            8 or 9 => 4,      // Mão
            10 or 11 => 5,    // Thìn
            12 or 13 => 8,    // Mùi
            14 or 15 => 9,    // Thân
            16 or 17 => 10,   // Dậu
            18 or 19 => 11,   // Tuất
            20 or 21 => 12,   // Hợi
            22 or 23 => 6,    // Tị
            24 or 25 => 7,    // Ngọ
            26 or 27 => 8,    // Mùi
            28 or 29 => 9,    // Thân
            30 => 10,         // Dậu
            _ => 1
        };
    }

    // Mộc Tam Cục
    private int GetMocTamCuc(int day)
    {
        return day switch
        {
            3 or 4 or 5 => 1,       // Tý
            6 or 7 or 8 => 2,       // Sửu
            9 or 10 or 11 => 3,     // Dần
            12 or 13 or 14 => 4,    // Mão
            15 or 16 or 17 => 5,    // Thìn
            18 or 19 or 20 => 8,    // Mùi
            21 or 22 or 23 => 9,    // Thân
            24 or 25 or 26 => 10,   // Dậu
            27 or 28 or 29 => 11,   // Tuất
            30 => 12,               // Hợi
            1 or 2 => 12,           // Hợi
            _ => 1
        };
    }

    // Kim Tứ Cục
    private int GetKimTuCuc(int day)
    {
        return day switch
        {
            4 or 5 or 6 or 7 => 1,      // Tý
            8 or 9 or 10 or 11 => 2,    // Sửu
            12 or 13 or 14 or 15 => 3,  // Dần
            16 or 17 or 18 or 19 => 4,  // Mão
            20 or 21 or 22 or 23 => 5,  // Thìn
            24 or 25 or 26 or 27 => 8,  // Mùi
            28 or 29 or 30 => 9,        // Thân
            1 or 2 or 3 => 12,          // Hợi
            _ => 1
        };
    }

    // Thổ Ngũ Cục
    private int GetThoNguCuc(int day)
    {
        return day switch
        {
            5 or 6 or 7 or 8 or 9 => 1,         // Tý
            10 or 11 or 12 or 13 or 14 => 2,   // Sửu
            15 or 16 or 17 or 18 or 19 => 3,   // Dần
            20 or 21 or 22 or 23 or 24 => 4,   // Mão
            25 or 26 or 27 or 28 or 29 => 5,   // Thìn
            30 => 8,                            // Mùi
            1 or 2 or 3 or 4 => 12,             // Hợi
            _ => 1
        };
    }

    // Hỏa Lục Cục
    private int GetHoaLucCuc(int day)
    {
        return day switch
        {
            6 or 7 or 8 or 9 or 10 or 11 => 1,      // Tý
            12 or 13 or 14 or 15 or 16 or 17 => 2,  // Sửu
            18 or 19 or 20 or 21 or 22 or 23 => 3,  // Dần
            24 or 25 or 26 or 27 or 28 or 29 => 4,  // Mão
            30 => 5,                                // Thìn
            1 or 2 or 3 or 4 or 5 => 12,           // Hợi
            _ => 1
        };
    }

    // Helper: Đi ngược chiều kim đồng hồ
    private int Prev(int pos, int steps)
    {
        int result = pos - steps;
        while (result <= 0) result += 12;
        return result;
    }

    // Helper: Đi thuận chiều kim đồng hồ
    private int Next(int pos, int steps)
    {
        int result = pos + steps;
        while (result > 12) result -= 12;
        return result;
    }

    // Helper: Tính vị trí đối xứng qua trục Dần(3)-Thân(9)
    private int GetSymmetricPosition(int pos)
    {
        // Công thức đối xứng qua trục Dần(3)-Thân(9):
        // Nếu Tử Vi tại Dần(3) hoặc Thân(9) thì Thiên Phủ cũng tại đó (đồng cung)
        if (pos == 3 || pos == 9) return pos;
        
        // Các cặp đối xứng qua trục Dần-Thân (theo hướng dẫn):
        // Mão(4) <-> Sửu(2), Thìn(5) <-> Tý(1), Tỵ(6) <-> Hợi(12)
        // Ngọ(7) <-> Tuất(11), Mùi(8) <-> Dậu(10)
        return pos switch
        {
            1 => 5,   // Tý -> Thìn
            2 => 4,   // Sửu -> Mão
            4 => 2,   // Mão -> Sửu
            5 => 1,   // Thìn -> Tý
            6 => 12,  // Tỵ -> Hợi
            7 => 11,  // Ngọ -> Tuất
            8 => 10,  // Mùi -> Dậu
            10 => 8,  // Dậu -> Mùi
            11 => 7,  // Tuất -> Ngọ
            12 => 6,  // Hợi -> Tỵ
            _ => pos
        };
    }
}
