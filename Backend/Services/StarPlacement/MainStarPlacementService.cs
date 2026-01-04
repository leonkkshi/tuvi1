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
    /// Tính vị trí sao Tử Vi dựa vào Cục và Ngày sinh
    /// Quy tắc:
    /// 1. Chia ngày sinh cho số cục
    /// 2. Nếu chia hết: Đếm từ Dần (3) là số 1, thuận chiều kim đồng hồ
    /// 3. Nếu không chia hết: Mượn số gần nhất để chia hết
    ///    - Mượn số chẵn: tiến thêm
    ///    - Mượn số lẻ: lùi lại
    /// </summary>
    private int GetTuViPosition(int cuc, int day)
    {
        int quotient = day / cuc;
        int remainder = day % cuc;
        int borrowed = 0;
        
        // Nếu không chia hết, mượn số gần nhất
        if (remainder != 0)
        {
            borrowed = cuc - remainder;
            quotient = (day + borrowed) / cuc;
        }
        
        // Đếm từ Dần (3) là số 1, thuận chiều kim đồng hồ
        // Dần=3, Mão=4, Thìn=5, Tỵ=6, Ngọ=7, Mùi=8, Thân=9, Dậu=10, Tuất=11, Hợi=12, Tý=1, Sửu=2
        int position = 3; // Bắt đầu từ Dần (3)
        for (int i = 1; i < quotient; i++)
        {
            position = Next(position, 1);
        }
        
        // Nếu có mượn, điều chỉnh vị trí
        if (borrowed > 0)
        {
            if (borrowed % 2 == 0) // Mượn số chẵn: tiến thêm
            {
                position = Next(position, borrowed);
            }
            else // Mượn số lẻ: lùi lại
            {
                position = Prev(position, borrowed);
            }
        }
        
        return position;
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
