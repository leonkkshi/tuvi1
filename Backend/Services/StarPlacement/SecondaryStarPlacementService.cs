namespace Backend.Services.StarPlacement;

/// <summary>
/// Service an các phụ tinh theo giờ, ngày, tháng, năm sinh.
/// (Giữ đúng các ID sao theo danh sách _stars trong TuViService.)
/// </summary>
public class SecondaryStarPlacementService : IStarPlacementService
{
    public Dictionary<int, List<int>> PlaceStars(StarPlacementContext context)
    {
        var positions = new Dictionary<int, List<int>>();
        for (int i = 1; i <= 12; i++) positions[i] = new List<int>();

        int yearCan = (context.Year - 3) % 10;
        if (yearCan <= 0) yearCan += 10;

        int yearBranch = (context.Year - 3) % 12;
        if (yearBranch <= 0) yearBranch += 12;

        // Sao cố định
        positions[5].Add(29);  // Thiên La ở Thìn
        positions[11].Add(30); // Địa Võng ở Tuất

        // Sao theo cung
        positions[context.NobocPalace].Add(31);   // Thiên Thương ở Nô Bộc
        positions[context.TatAchPalace].Add(32); // Thiên Sứ ở Tật Ách

        int hourBranch = GetHourBranch(context.Hour);

        // Văn Xương (15): giờ Tý ở Tuất, đi ngược
        int vanXuongPos = 11 - (hourBranch - 1);
        while (vanXuongPos <= 0) vanXuongPos += 12;
        positions[vanXuongPos].Add(15);

        // Văn Khúc (16): giờ Tý ở Thìn, đi thuận
        int vanKhucPos = 5 + (hourBranch - 1);
        while (vanKhucPos > 12) vanKhucPos -= 12;
        positions[vanKhucPos].Add(16);

        // Tả Phù (17): theo tháng sinh - Kể từ cung Thìn là tháng 1 đi thuận
        int taPhuPos = 5 + (context.Month - 1);
        while (taPhuPos > 12) taPhuPos -= 12;
        positions[taPhuPos].Add(17);

        // Hữu Bật (18): theo tháng sinh - Kể từ cung Tuất là tháng 1 đi nghịch
        int huuBatPos = 11 - (context.Month - 1);
        while (huuBatPos <= 0) huuBatPos += 12;
        positions[huuBatPos].Add(18);

        // Theo tháng
        int thienHinhPos = 10 + (context.Month - 1);
        while (thienHinhPos > 12) thienHinhPos -= 12;
        positions[thienHinhPos].Add(33);

        int thienRieuPos = 2 + (context.Month - 1);
        while (thienRieuPos > 12) thienRieuPos -= 12;
        positions[thienRieuPos].Add(35);

        int diaGiaiPos = 8 + (context.Month - 1);
        while (diaGiaiPos > 12) diaGiaiPos -= 12;
        positions[diaGiaiPos].Add(37);

        int thienGiaiPos = 9 + (context.Month - 1);
        while (thienGiaiPos > 12) thienGiaiPos -= 12;
        positions[thienGiaiPos].Add(36);

        // Tam Thai (101): theo ngày sinh - Kể từ vị trí sao Tả Phụ là ngày 1 đi thuận
        int tamThaiPos = taPhuPos + (context.Day - 1);
        while (tamThaiPos > 12) tamThaiPos -= 12;
        if (tamThaiPos == 0) tamThaiPos = 12;
        positions[tamThaiPos].Add(101);

        // Bát Tọa (102): theo ngày sinh - Kể từ vị trí sao Hữu Bật là ngày 1 đi ngược
        int batToaPos = huuBatPos - (context.Day - 1);
        while (batToaPos <= 0) batToaPos += 12;
        if (batToaPos == 0) batToaPos = 12;
        positions[batToaPos].Add(102);

        // Thiên Quý (103): Kể từ vị trí sao Văn Khúc là ngày 1 đi ngược, tới ngày sinh trừ đi 1
        int thienQuyPos = vanKhucPos - (context.Day - 2);
        while (thienQuyPos <= 0) thienQuyPos += 12;
        if (thienQuyPos == 0) thienQuyPos = 12;
        positions[thienQuyPos].Add(103);

        // Ân Quang (104): Kể từ vị trí sao Văn Xương là ngày 1 đi thuận, tới ngày sinh trừ đi 1
        int anQuangPos = vanXuongPos + (context.Day - 2);
        while (anQuangPos > 12) anQuangPos -= 12;
        if (anQuangPos == 0) anQuangPos = 12;
        positions[anQuangPos].Add(104);

        // Thai Phụ (38): giờ Tý ở Ngọ, đi thuận
        int thaiPhuPos = (7 + (hourBranch - 1) - 1) % 12 + 1;
        positions[thaiPhuPos].Add(38);

        int phongCaoPos = (3 + (hourBranch - 1) - 1) % 12 + 1;
        positions[phongCaoPos].Add(39);

        // Lộc Tồn (19): Theo Can năm sinh
        // Giáp-Kỷ=Dần, Ất-Canh=Mão, Bính-Tân=Tỵ, Đinh-Nhâm=Ngọ, Mậu=Tị, Quý=Tý
        int locTonPos = yearCan switch
        {
            1 => 3,    // Giáp -> Dần
            2 => 4,    // Ất -> Mão
            3 => 6,    // Bính -> Tỵ
            4 => 7,    // Đinh -> Ngọ
            5 => 6,    // Mậu -> Tị
            6 => 7,    // Kỷ -> Ngọ
            7 => 9,    // Canh -> Thân
            8 => 10,   // Tân -> Dậu
            9 => 12,   // Nhâm -> Hợi
            10 => 1,   // Quý -> Tý
            _ => 3
        };
        positions[locTonPos].Add(19);

        // Thiên Khôi (20): Theo Can năm sinh
        // Giáp=Sửu, Ất=Tý, Bính=Hợi, Đinh=Hợi, Mậu=Sửu, Kỷ=Tý, Canh=Ngọ, Tân=Ngọ, Nhâm=Mão, Quý=Mão
        int thienKhoiPos = yearCan switch
        {
            1 => 2,    // Giáp -> Sửu
            2 => 1,    // Ất -> Tý
            3 => 12,   // Bính -> Hợi
            4 => 12,   // Đinh -> Hợi
            5 => 2,    // Mậu -> Sửu
            6 => 1,    // Kỷ -> Tý
            7 => 7,    // Canh -> Ngọ
            8 => 7,    // Tân -> Ngọ
            9 => 4,    // Nhâm -> Mão
            10 => 4,   // Quý -> Mão
            _ => 2
        };
        positions[thienKhoiPos].Add(20);

        // Thiên Việt (21): Theo Can năm sinh
        // Giáp=Mùi, Ất=Thân, Bính=Dậu, Đinh=Dậu, Mậu=Mùi, Kỷ=Thân, Canh=Dần, Tân=Dần, Nhâm=Tị, Quý=Tị
        int thienVietPos = yearCan switch
        {
            1 => 8,    // Giáp -> Mùi
            2 => 9,    // Ất -> Thân
            3 => 10,   // Bính -> Dậu
            4 => 10,   // Đinh -> Dậu
            5 => 8,    // Mậu -> Mùi
            6 => 9,    // Kỷ -> Thân
            7 => 3,    // Canh -> Dần
            8 => 3,    // Tân -> Dần
            9 => 6,    // Nhâm -> Tị
            10 => 6,   // Quý -> Tị
            _ => 8
        };
        positions[thienVietPos].Add(21);

        // Lưu Hà (88): Theo Can năm
        int luuHaPos = yearCan switch
        {
            1 => 10,   // Giáp -> Dậu
            2 => 11,   // Ất -> Tuất
            3 => 8,    // Bính -> Mùi
            4 => 9,    // Đinh -> Thân
            5 => 6,    // Mậu -> Tỵ
            6 => 7,    // Kỷ -> Ngọ
            7 => 4,    // Canh -> Mão
            8 => 5,    // Tân -> Thìn
            9 => 12,   // Nhâm -> Hợi
            10 => 3,   // Quý -> Dần
            _ => 1
        };
        positions[luuHaPos].Add(88);

        // Theo Chi
        int thienMaPos = yearBranch switch
        {
            1 or 5 or 9 => 3,
            2 or 6 or 10 => 12,
            3 or 7 or 11 => 9,
            4 or 8 or 12 => 6,
            _ => 3
        };
        positions[thienMaPos].Add(24);

        // Giải Thần (87): Theo năm sinh (Branch) - Từ cung Tuất đi ngược
        int giaiThanPos = 11 - (yearBranch - 1);
        if (giaiThanPos <= 0) giaiThanPos += 12;
        positions[giaiThanPos].Add(87);

        // Cô Thần - Quả Tú theo Tam Hội Tuổi
        int coThanPos = 0, quaTuPos = 0;
        if (yearBranch == 12 || yearBranch == 1 || yearBranch == 2)
        {
            // Hợi – Tý – Sửu: Cô Thần=Dần, Quả Tú=Tuất
            coThanPos = 3; quaTuPos = 11;
        }
        else if (yearBranch == 3 || yearBranch == 4 || yearBranch == 5)
        {
            // Dần – Mão – Thìn: Cô Thần=Tị, Quả Tú=Sửu
            coThanPos = 6; quaTuPos = 2;
        }
        else if (yearBranch == 6 || yearBranch == 7 || yearBranch == 8)
        {
            // Tị – Ngọ – Mùi: Cô Thần=Thân, Quả Tú=Thìn
            coThanPos = 9; quaTuPos = 5;
        }
        else if (yearBranch == 9 || yearBranch == 10 || yearBranch == 11)
        {
            // Thân – Dậu – Tuất: Cô Thần=Hợi, Quả Tú=Mùi
            coThanPos = 12; quaTuPos = 8;
        }
        positions[coThanPos].Add(89);
        positions[quaTuPos].Add(90);

        // Địa Không (22): Kể từ cung Hợi là giờ Tý đi ngược chiều kim đồng hồ
        int diaKhongPos = 12 - (hourBranch - 1);
        while (diaKhongPos <= 0) diaKhongPos += 12;
        positions[diaKhongPos].Add(22);

        // Địa Kiếp (23): Kể từ cung Hợi là giờ Tý đi thuận chiều kim đồng hồ
        int diaKiepPos = 12 + (hourBranch - 1);
        while (diaKiepPos > 12) diaKiepPos -= 12;
        positions[diaKiepPos].Add(23);

        // Hỏa Tinh (40) và Linh Tinh (41) - Có điều kiện thuận/ngược theo Âm/Dương
        // Dương Nam: Nam + Can Dương (lẻ: 1,3,5,7,9)
        // Âm Nữ: Nữ + Can Âm (chẵn: 2,4,6,8,10)
        bool isDuongNam = context.IsMale && (yearCan % 2 == 1);
        bool isAmNu = !context.IsMale && (yearCan % 2 == 0);
        bool thuanChieu = isDuongNam || isAmNu;

        // Xác định cung khởi theo nhóm tuổi
        int hoaKhoi = 0;
        int linhKhoi = 0;

        // Tuổi Dần, Ngọ, Tuất (yearBranch = 3, 7, 11)
        if (yearBranch == 3 || yearBranch == 7 || yearBranch == 11)
        {
            hoaKhoi = 2; // Sửu
            linhKhoi = 4; // Mão
        }
        // Tuổi Thân, Tý, Thìn (yearBranch = 9, 1, 5)
        else if (yearBranch == 9 || yearBranch == 1 || yearBranch == 5)
        {
            hoaKhoi = 3; // Dần
            linhKhoi = 11; // Tuất
        }
        // Tuổi Tị, Dậu, Sửu (yearBranch = 6, 10, 2)
        else if (yearBranch == 6 || yearBranch == 10 || yearBranch == 2)
        {
            hoaKhoi = 4; // Mão
            linhKhoi = 11; // Tuất
        }
        // Tuổi Hợi, Mão, Mùi (yearBranch = 12, 4, 8)
        else
        {
            hoaKhoi = 10; // Dậu
            linhKhoi = 11; // Tuất
        }

        // Hỏa Tinh: Dương Nam – Âm Nữ thì đi thuận, Âm Nam – Dương Nữ thì đi ngược
        int hoaTinhPos;
        if (thuanChieu)
        {
            hoaTinhPos = hoaKhoi + (hourBranch - 1);
            while (hoaTinhPos > 12) hoaTinhPos -= 12;
        }
        else
        {
            hoaTinhPos = hoaKhoi - (hourBranch - 1);
            while (hoaTinhPos <= 0) hoaTinhPos += 12;
        }
        if (hoaTinhPos == 0) hoaTinhPos = 12;
        positions[hoaTinhPos].Add(40);

        // Linh Tinh: Dương Nam – Âm Nữ thì đi ngược, Âm Nam – Dương Nữ thì đi thuận
        int linhTinhPos;
        if (!thuanChieu)
        {
            linhTinhPos = linhKhoi + (hourBranch - 1);
            while (linhTinhPos > 12) linhTinhPos -= 12;
        }
        else
        {
            linhTinhPos = linhKhoi - (hourBranch - 1);
            while (linhTinhPos <= 0) linhTinhPos += 12;
        }
        if (linhTinhPos == 0) linhTinhPos = 12;
        positions[linhTinhPos].Add(41);

        // Vòng Lộc Tồn (12 sao): 75-86
        // Chiều thuận: Dương Nam, Âm Nữ
        // Chiều nghịch: Âm Nam, Dương Nữ
        int[] locTonStars = { 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86 };
        
        bool diThuan = isDuongNam || isAmNu;
        
        for (int i = 0; i < locTonStars.Length; i++)
        {
            int pos;
            if (diThuan)
            {
                // Đi thuận
                pos = (locTonPos + i) % 12;
                if (pos == 0) pos = 12;
            }
            else
            {
                // Đi nghịch
                pos = (locTonPos - i) % 12;
                if (pos <= 0) pos += 12;
            }
            positions[pos].Add(locTonStars[i]);
        }
        
        // Hồng Loan (91): Từ cung Mão là NĂM Tý đi ngược theo yearBranch
        int hongLoanPos = 4 - (yearBranch - 1);
        while (hongLoanPos <= 0) hongLoanPos += 12;
        positions[hongLoanPos].Add(91);

        // Thiên Hỉ (92): Từ cung Dậu là NĂM Tý đi ngược theo yearBranch
        int thienHiPos = 10 - (yearBranch - 1);
        while (thienHiPos <= 0) thienHiPos += 12;
        positions[thienHiPos].Add(92);

        // Kình Dương (93): Từ vị trí Lộc Tồn tiến 1 ô thuận
        int kinhDuongPos = (locTonPos % 12) + 1;
        positions[kinhDuongPos].Add(93);

        // Đà La (94): Từ vị trí Lộc Tồn tiến 1 ô ngược
        int daLaPos = locTonPos - 1;
        if (daLaPos <= 0) daLaPos += 12;
        positions[daLaPos].Add(94);

        // Thiên Y (95): theo tháng sinh - Kể từ cung Sửu là tháng 1 đi thuận
        int thienYPos = 2 + (context.Month - 1);
        while (thienYPos > 12) thienYPos -= 12;
        positions[thienYPos].Add(95);

        // Đường Phù (96): Từ vị trí Lộc Tồn tiến 5 ô thuận
        int duongPhuPos = (locTonPos + 5 - 1) % 12 + 1;
        positions[duongPhuPos].Add(96);

        // Quốc Ấn (97): Từ vị trí Lộc Tồn tiến 8 ô thuận
        int quocAnPos = (locTonPos + 8 - 1) % 12 + 1;
        positions[quocAnPos].Add(97);

        // Phá Toái (98): Theo nhóm tuổi
        int phaThoaiPos = 0;
        if (yearBranch == 3 || yearBranch == 9 || yearBranch == 6 || yearBranch == 12)
        {
            phaThoaiPos = 10; // Dần, Thân, Tị, Hợi -> Dậu
        }
        else if (yearBranch == 1 || yearBranch == 7 || yearBranch == 4 || yearBranch == 10)
        {
            phaThoaiPos = 6;  // Tý, Ngọ, Mão, Dậu -> Tị
        }
        else
        {
            phaThoaiPos = 2;  // Thìn, Tuất, Sửu, Mùi -> Sửu
        }
        positions[phaThoaiPos].Add(98);

        // Thiên Phúc (99): Theo Can năm
        int thienPhucPos = yearCan switch
        {
            1 => 10,   // Giáp -> Dậu
            2 => 9,    // Ất -> Thân
            3 => 1,    // Bính -> Tý
            4 => 12,   // Đinh -> Hợi
            5 => 4,    // Mậu -> Mão
            6 => 3,    // Kỷ -> Dần
            7 => 7,    // Canh -> Ngọ
            8 => 6,    // Tân -> Tị
            9 => 7,    // Nhâm -> Ngọ
            10 => 6,   // Quý -> Tị
            _ => 1
        };
        positions[thienPhucPos].Add(99);

        // Đẩu Quân (100): Từ vị trí Thái Tuế (= địa chi năm sinh) đặt là tháng 1 đi ngược tới tháng sinh, 
        // sau đó từ đó đặt giờ Tý đi thuận tới giờ sinh
        int thaiTuePos = yearBranch;  // Thái Tuế ở cung có địa chi = yearBranch
        int duuQuanThangPos = thaiTuePos - (context.Month - 1);
        while (duuQuanThangPos <= 0) duuQuanThangPos += 12;
        int duuQuanPos = duuQuanThangPos + (hourBranch - 1);
        while (duuQuanPos > 12) duuQuanPos -= 12;
        if (duuQuanPos == 0) duuQuanPos = 12;
        positions[duuQuanPos].Add(100);

        // Tính Cung Thân dựa vào giờ sinh (cố định tại một trong 6 cung)
        int thanPalace = context.ThanPalace;

        // Thiên Tài (105): Từ cung Mệnh là năm Tý đi thuận tới năm sinh
        int thienTaiPos = context.MenhPalace + (yearBranch - 1);
        while (thienTaiPos > 12) thienTaiPos -= 12;
        if (thienTaiPos == 0) thienTaiPos = 12;
        positions[thienTaiPos].Add(105);

        // Thiên Thọ (106): Từ cung Thân là năm Tý đi thuận tới năm sinh
        int thienThoPos = thanPalace + (yearBranch - 1);
        while (thienThoPos > 12) thienThoPos -= 12;
        if (thienThoPos == 0) thienThoPos = 12;
        positions[thienThoPos].Add(106);

        return positions;
    }

    private static int GetHourBranch(int hour)
    {
        if (hour >= 23 || hour < 1) return 1;
        if (hour >= 1 && hour < 3) return 2;
        if (hour >= 3 && hour < 5) return 3;
        if (hour >= 5 && hour < 7) return 4;
        if (hour >= 7 && hour < 9) return 5;
        if (hour >= 9 && hour < 11) return 6;
        if (hour >= 11 && hour < 13) return 7;
        if (hour >= 13 && hour < 15) return 8;
        if (hour >= 15 && hour < 17) return 9;
        if (hour >= 17 && hour < 19) return 10;
        if (hour >= 19 && hour < 21) return 11;
        return 12;
    }
}
