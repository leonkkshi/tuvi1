using Backend.Models;

namespace Backend.Services.Utilities;

/// <summary>
/// Service build danh mục các cung và sao
/// </summary>
public class CatalogService
{
    public List<Palace> BuildPalaceCatalog() => new()
    {
        MakePalace(1, "Mệnh", "Cung thể hiện bản chất, tính cách, ngoại hình"),
        MakePalace(2, "Phụ Mẫu", "Cung quan hệ cha mẹ, thầy giáo"),
        MakePalace(3, "Phúc Đức", "Cung phúc lộc, tinh thần, sở thích"),
        MakePalace(4, "Điền Trạch", "Cung tài sản, nhà cửa"),
        MakePalace(5, "Quan Lộc", "Cung sự nghiệp, công danh"),
        MakePalace(6, "Nô Bộc", "Cung bạn bè, đồng nghiệp"),
        MakePalace(7, "Thiên Di", "Cung di chuyển, xuất ngoại"),
        MakePalace(8, "Tật Ách", "Cung sức khỏe, bệnh tật"),
        MakePalace(9, "Tài Bạch", "Cung tài lộc, tiền bạc"),
        MakePalace(10, "Tử Tức", "Cung con cái, tình duyên"),
        MakePalace(11, "Phu Thê", "Cung vợ chồng, hôn nhân"),
        MakePalace(12, "Huynh Đệ", "Cung anh chị em, bạn bè thân thiết")
    };

    public List<Star> BuildStarCatalog()
    {
        var stars = new List<Star>
        {
            // 14 Chính Tinh (1-14)
            MakeStar(1, "Tử Vi", "Chính tinh", "Thổ", "Cát", 100),
            MakeStar(2, "Thiên Cơ", "Chính tinh", "Mộc", "Cát", 90),
            MakeStar(3, "Thái Dương", "Chính tinh", "Hỏa", "Cát", 95),
            MakeStar(4, "Vũ Khúc", "Chính tinh", "Âm Kim", "Cát", 85),
            MakeStar(5, "Thiên Đồng", "Chính tinh", "Thủy", "Cát", 88),
            MakeStar(6, "Liêm Trinh", "Chính tinh", "Hỏa", "Hung", 80),
            MakeStar(7, "Thiên Phủ", "Chính tinh", "Thổ", "Cát", 92),
            MakeStar(8, "Thái Âm", "Chính tinh", "Thủy", "Cát", 90),
            MakeStar(9, "Tham Lang", "Chính tinh", "Thủy", "Hung", 75),
            MakeStar(10, "Cự Môn", "Chính tinh", "Thủy", "Hung", 70),
            MakeStar(11, "Thiên Tướng", "Chính tinh", "Thủy", "Cát", 82),
            MakeStar(12, "Thiên Lương", "Chính tinh", "Mộc", "Cát", 87),
            MakeStar(13, "Thất Sát", "Chính tinh", "Kim", "Hung", 78),
            MakeStar(14, "Phá Quân", "Chính tinh", "Thủy", "Hung", 72),

            // Văn tinh (15-18)
            MakeStar(15, "Văn Xương", "Phụ tinh", "Kim", "Cát", 85),
            MakeStar(16, "Văn Khúc", "Phụ tinh", "Thủy", "Cát", 85),
            MakeStar(17, "Tả Phù", "Phụ tinh", "Thổ", "Cát", 80),
            MakeStar(18, "Hữu Bật", "Phụ tinh", "Thủy", "Cát", 80),

            // Hung tinh chính (19-24)
            MakeStar(19, "Lộc Tồn", "Phụ tinh", "Thổ", "Cát", 75),
            MakeStar(20, "Thiên Khôi", "Phụ tinh", "Hỏa", "Cát", 70),
            MakeStar(21, "Thiên Việt", "Phụ tinh", "Hỏa", "Cát", 70),
            MakeStar(22, "Địa Không", "Phụ tinh", "", "Hung", 45),
            MakeStar(23, "Địa Kiếp", "Phụ tinh", "", "Hung", 45),
            MakeStar(24, "Thiên Mã", "Phụ tinh", "Hỏa", "Cát", 65),

            // Tứ Hóa (25-28)
            MakeStar(25, "Hóa Lộc", "Tứ Hóa", "", "Cát", 95),
            MakeStar(26, "Hóa Quyền", "Tứ Hóa", "", "Cát", 90),
            MakeStar(27, "Hóa Khoa", "Tứ Hóa", "", "Cát", 85),
            MakeStar(28, "Hóa Kỵ", "Tứ Hóa", "", "Hung", 50),

            // Sao cố định (29-30)
            MakeStar(29, "Thiên La", "Phụ tinh", "", "Hung", 40),
            MakeStar(30, "Địa Võng", "Phụ tinh", "", "Hung", 40),

            // Sao theo cung (31-32)
            MakeStar(31, "Thiên Thương", "Phụ tinh", "Thủy", "Hung", 55),
            MakeStar(32, "Thiên Sứ", "Phụ tinh", "Thủy", "Hung", 55),

            // Sao theo tháng (33-37)
            MakeStar(33, "Thiên Hình", "Phụ tinh", "Hỏa", "Hung", 50),
            MakeStar(34, "Thiên Y", "Phụ tinh", "Thổ", "Cát", 60),
            MakeStar(35, "Thiên Riêu", "Phụ tinh", "", "Hung", 50),
            MakeStar(36, "Thiên Giải", "Phụ tinh", "", "Cát", 60),
            MakeStar(37, "Địa Giải", "Phụ tinh", "", "Cát", 60),

            // Sao theo giờ (38-41)
            MakeStar(38, "Thai Phụ", "Phụ tinh", "Thổ", "Cát", 60),
            MakeStar(39, "Phong Cáo", "Phụ tinh", "", "Cát", 50),
            MakeStar(40, "Hỏa Tinh", "Phụ tinh", "Hỏa", "Hung", 60),
            MakeStar(41, "Linh Tinh", "Phụ tinh", "Hỏa", "Hung", 60),

            // 12 Sao Trường Sinh (42-53)
            MakeStar(42, "Trường Sinh", "Trường Sinh", "", "Cát", 70),
            MakeStar(43, "Mộc Dục", "Trường Sinh", "", "Cát", 65),
            MakeStar(44, "Quan Đới", "Trường Sinh", "", "Cát", 75),
            MakeStar(45, "Lâm Quan", "Trường Sinh", "", "Cát", 85),
            MakeStar(46, "Đế Vượng", "Trường Sinh", "", "Cát", 95),
            MakeStar(47, "Suy", "Trường Sinh", "", "Hung", 50),
            MakeStar(48, "Bệnh", "Trường Sinh", "", "Hung", 45),
            MakeStar(49, "Tử", "Trường Sinh", "", "Hung", 40),
            MakeStar(50, "Mộ", "Trường Sinh", "", "Hung", 42),
            MakeStar(51, "Tuyệt", "Trường Sinh", "", "Hung", 38),
            MakeStar(52, "Thai", "Trường Sinh", "", "Cát", 60),
            MakeStar(53, "Dưỡng", "Trường Sinh", "", "Cát", 68),

            // 12 Sao Thái Tuế (54-65)
            MakeStar(54, "Thái Tuế", "Thái Tuế", "", "Hung", 55),
            MakeStar(55, "Thiếu Dương", "Thái Tuế", "", "Cát", 60),
            MakeStar(56, "Tang Môn", "Thái Tuế", "", "Hung", 45),
            MakeStar(57, "Thiếu Âm", "Thái Tuế", "", "Cát", 60),
            MakeStar(58, "Quan Phù", "Thái Tuế", "", "Hung", 50),
            MakeStar(59, "Tử Phù", "Thái Tuế", "", "Hung", 45),
            MakeStar(60, "Tuế Phá", "Thái Tuế", "", "Hung", 40),
            MakeStar(61, "Long Đức", "Thái Tuế", "", "Cát", 65),
            MakeStar(62, "Bạch Hổ", "Thái Tuế", "", "Hung", 50),
            MakeStar(63, "Phúc Đức", "Thái Tuế", "", "Cát", 70),
            MakeStar(64, "Điếu Khách", "Thái Tuế", "", "Hung", 45),
            MakeStar(65, "Trực Phù", "Thái Tuế", "", "Hung", 48),

            // Sao đi cùng Thái Tuế (66-74)
            MakeStar(66, "Thiên Không", "Phụ tinh", "", "Hung", 45),
            MakeStar(67, "Long Trì", "Phụ tinh", "", "Cát", 60),
            MakeStar(68, "Nguyệt Đức", "Phụ tinh", "", "Cát", 65),
            MakeStar(69, "Thiên Hư", "Phụ tinh", "", "Hung", 42),
            MakeStar(70, "Thiên Đức", "Phụ tinh", "", "Cát", 68),
            MakeStar(71, "Thiên Khốc", "Phụ tinh", "", "Hung", 48),
            MakeStar(72, "Hoa Cái", "Phụ tinh", "", "Cát", 55),
            MakeStar(73, "Đào Hoa", "Phụ tinh", "", "Cát", 58),
            MakeStar(74, "Kiếp Sát", "Phụ tinh", "", "Hung", 50),

            // 12 Sao vòng Lộc Tồn (75-86)
            MakeStar(75, "Bác Sĩ", "Phụ tinh", "", "Cát", 60),
            MakeStar(76, "Lực Sĩ", "Phụ tinh", "", "Cát", 62),
            MakeStar(77, "Thanh Long", "Phụ tinh", "", "Cát", 70),
            MakeStar(78, "Tiểu Hao", "Phụ tinh", "", "Hung", 45),
            MakeStar(79, "Tướng Quân", "Phụ tinh", "", "Cát", 68),
            MakeStar(80, "Tấu Thư", "Phụ tinh", "", "Cát", 65),
            MakeStar(81, "Phi Liêm", "Phụ tinh", "", "Hung", 48),
            MakeStar(82, "Hỷ Thần", "Phụ tinh", "", "Cát", 72),
            MakeStar(83, "Bệnh Phù", "Phụ tinh", "", "Hung", 42),
            MakeStar(84, "Đại Hao", "Phụ tinh", "", "Hung", 40),
            MakeStar(85, "Phục Binh", "Phụ tinh", "", "Hung", 46),
            MakeStar(86, "Quan Phủ", "Phụ tinh", "", "Hung", 50),

            // Các sao khác (87-104)
            MakeStar(87, "Giải Thần", "Phụ tinh", "", "Cát", 62),
            MakeStar(88, "Lưu Hà", "Phụ tinh", "", "Hung", 48),
            MakeStar(89, "Cô Thần", "Phụ tinh", "", "Hung", 40),
            MakeStar(90, "Quả Tú", "Phụ tinh", "", "Hung", 40),
            MakeStar(91, "Hồng Loan", "Phụ tinh", "", "Cát", 50),
            MakeStar(92, "Thiên Hỉ", "Phụ tinh", "", "Cát", 55),
            MakeStar(93, "Kình Dương", "Phụ tinh", "", "Hung", 50),
            MakeStar(94, "Đà La", "Phụ tinh", "", "Hung", 45),
            MakeStar(95, "Thiên Y", "Phụ tinh", "", "Cát", 52),
            MakeStar(96, "Đường Phù", "Phụ tinh", "", "Cát", 48),
            MakeStar(97, "Quốc Ấn", "Phụ tinh", "", "Cát", 50),
            MakeStar(98, "Phá Toái", "Phụ tinh", "", "Hung", 45),
            MakeStar(99, "Thiên Phúc", "Phụ tinh", "", "Cát", 55),
            MakeStar(100, "Đẩu Quân", "Phụ tinh", "", "Hung", 48),
            MakeStar(101, "Tam Thai", "Phụ tinh", "", "Cát", 50),
            MakeStar(102, "Bát Tọa", "Phụ tinh", "", "Hung", 48),
            MakeStar(103, "Thiên Quý", "Phụ tinh", "", "Cát", 52),
            MakeStar(104, "Ân Quang", "Phụ tinh", "", "Cát", 55),
            MakeStar(105, "Thiên Tài", "Phụ tinh", "", "Cát", 60),
            MakeStar(106, "Thiên Thọ", "Phụ tinh", "", "Cát", 65),
            MakeStar(107, "Triệt", "Phụ tinh", "", "Hung", 40),  // Kẹp giữa 2 cung
            MakeStar(108, "Tuần", "Phụ tinh", "", "Hung", 40)   // Kẹp giữa 2 cung
        };

        return stars;
    }

    private Palace MakePalace(int id, string name, string description) =>
        new() { Id = id, Name = name, Description = description };

    private Star MakeStar(int id, string name, string type, string element, string nature, int brightness) =>
        new()
        {
            Id = id,
            Name = name,
            Type = type,
            Element = element,
            Nature = nature,
            Brightness = brightness
        };

    /// <summary>
    /// Điều chỉnh độ sáng của sao theo cung (Miếu Vượng, Đắc địa, Bình hòa, Hãm địa)
    /// </summary>
    /// <param name="starName">Tên sao</param>
    /// <param name="palacePosition">Vị trí cung (1-12): 1=Tý, 2=Sửu, 3=Dần, 4=Mão, 5=Thìn, 6=Tỵ, 7=Ngọ, 8=Mùi, 9=Thân, 10=Dậu, 11=Tuất, 12=Hợi</param>
    /// <param name="baseBrightness">Độ sáng cơ bản của sao</param>
    /// <returns>Độ sáng đã được điều chỉnh</returns>
    public int GetAdjustedBrightness(string starName, int palacePosition, int baseBrightness)
    {
        // Ánh xạ vị trí cung sang tên địa chi
        var branchNames = new[] { "", "Tý", "Sửu", "Dần", "Mão", "Thìn", "Tỵ", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi" };
        if (palacePosition < 1 || palacePosition > 12) return baseBrightness;
        
        var branch = branchNames[palacePosition];

        return starName switch
        {
            "Tử Vi" => branch switch
            {
                // Miếu địa: Tỵ, Ngọ, Dần, Thân
                "Tỵ" or "Ngọ" or "Dần" or "Thân" => 100,
                // Vượng địa: Thìn, Tuất
                "Thìn" or "Tuất" => 80,
                // Đắc địa: Sửu, Mùi
                "Sửu" or "Mùi" => 60,
                // Bình hòa: Hợi, Tý, Mão, Dậu
                "Hợi" or "Tý" or "Mão" or "Dậu" => 50,
                _ => baseBrightness
            },
            "Thiên Phủ" => branch switch
            {
                // Miếu địa: Dần, Thân, Tý, Ngọ
                "Dần" or "Thân" or "Tý" or "Ngọ" => 100,
                // Vượng địa: Thìn, Tuất
                "Thìn" or "Tuất" => 80,
                // Đắc địa: Tỵ, Hợi, Mùi
                "Tỵ" or "Hợi" or "Mùi" => 60,
                // Bình hòa: Mão, Dậu, Sửu
                "Mão" or "Dậu" or "Sửu" => 50,
                _ => baseBrightness
            },
            "Vũ Khúc" => branch switch
            {
                // Miếu địa: Thìn, Tuất, Sửu, Mùi
                "Thìn" or "Tuất" or "Sửu" or "Mùi" => 100,
                // Vượng địa: Dần, Thân, Tý, Ngọ
                "Dần" or "Thân" or "Tý" or "Ngọ" => 80,
                // Đắc địa: Mão, Dậu
                "Mão" or "Dậu" => 60,
                // Hãm địa: Tỵ, Hợi
                "Tỵ" or "Hợi" => 30,
                _ => baseBrightness
            },
            "Thiên Tướng" => branch switch
            {
                // Miếu địa: Dần, Thân
                "Dần" or "Thân" => 100,
                // Vượng địa: Thìn, Tuất, Tý, Ngọ
                "Thìn" or "Tuất" or "Tý" or "Ngọ" => 80,
                // Đắc địa: Sửu, Mùi, Tỵ, Hợi
                "Sửu" or "Mùi" or "Tỵ" or "Hợi" => 60,
                // Hãm địa: Mão, Dậu
                "Mão" or "Dậu" => 30,
                _ => baseBrightness
            },
            "Thất Sát" => branch switch
            {
                // Miếu địa: Dần, Thân, Tý, Ngọ
                "Dần" or "Thân" or "Tý" or "Ngọ" => 100,
                // Vượng địa: Tỵ, Hợi
                "Tỵ" or "Hợi" => 80,
                // Đắc địa: Sửu, Mùi
                "Sửu" or "Mùi" => 60,
                // Hãm địa: Mão, Dậu, Thìn, Tuất
                "Mão" or "Dậu" or "Thìn" or "Tuất" => 30,
                _ => baseBrightness
            },
            "Phá Quân" => branch switch
            {
                // Miếu địa: Tý, Ngọ
                "Tý" or "Ngọ" => 100,
                // Vượng địa: Sửu, Mùi
                "Sửu" or "Mùi" => 80,
                // Đắc địa: Thìn, Tuất
                "Thìn" or "Tuất" => 60,
                // Hãm địa: Mão, Dậu, Dần, Thân, Tỵ, Hợi
                "Mão" or "Dậu" or "Dần" or "Thân" or "Tỵ" or "Hợi" => 30,
                _ => baseBrightness
            },
            "Liêm Trinh" => branch switch
            {
                // Miếu địa: Thìn, Tuất
                "Thìn" or "Tuất" => 100,
                // Vượng địa: Tý, Ngọ, Dần, Thân
                "Tý" or "Ngọ" or "Dần" or "Thân" => 80,
                // Đắc địa: Sửu, Mùi
                "Sửu" or "Mùi" => 60,
                // Hãm địa: Tỵ, Hợi, Mão, Dậu
                "Tỵ" or "Hợi" or "Mão" or "Dậu" => 30,
                _ => baseBrightness
            },
            "Tham Lang" => branch switch
            {
                // Miếu địa: Sửu, Mùi
                "Sửu" or "Mùi" => 100,
                // Vượng địa: Thìn, Tuất
                "Thìn" or "Tuất" => 80,
                // Đắc địa: Dần, Thân
                "Dần" or "Thân" => 60,
                // Hãm địa: Tỵ, Hợi, Tý, Ngọ, Mão, Dậu
                "Tỵ" or "Hợi" or "Tý" or "Ngọ" or "Mão" or "Dậu" => 30,
                _ => baseBrightness
            },
            "Thiên Cơ" => branch switch
            {
                // Miếu địa: Thìn, Tuất, Mão, Dậu
                "Thìn" or "Tuất" or "Mão" or "Dậu" => 100,
                // Vượng địa: Tỵ, Thân
                "Tỵ" or "Thân" => 80,
                // Đắc địa: Tý, Ngọ, Sửu, Mùi
                "Tý" or "Ngọ" or "Sửu" or "Mùi" => 60,
                // Hãm địa: Dần, Hợi
                "Dần" or "Hợi" => 30,
                _ => baseBrightness
            },
            "Thái Âm" => branch switch
            {
                // Miếu địa: Dậu, Tuất, Hợi
                "Dậu" or "Tuất" or "Hợi" => 100,
                // Vượng địa: Thân, Tý
                "Thân" or "Tý" => 80,
                // Đắc địa: Sửu, Mùi
                "Sửu" or "Mùi" => 60,
                // Hãm địa: Dần, Mão, Thìn, Tỵ, Ngọ
                "Dần" or "Mão" or "Thìn" or "Tỵ" or "Ngọ" => 30,
                _ => baseBrightness
            },
            "Thiên Đồng" => branch switch
            {
                // Miếu địa: Dần, Thân
                "Dần" or "Thân" => 100,
                // Vượng địa: Tý
                "Tý" => 80,
                // Đắc địa: Mão, Tỵ, Hợi
                "Mão" or "Tỵ" or "Hợi" => 60,
                // Hãm địa: Thìn, Tuất, Sửu, Mùi, Ngọ, Dậu
                "Thìn" or "Tuất" or "Sửu" or "Mùi" or "Ngọ" or "Dậu" => 30,
                _ => baseBrightness
            },
            "Thiên Lương" => branch switch
            {
                // Miếu địa: Ngọ, Thìn, Tuất
                "Ngọ" or "Thìn" or "Tuất" => 100,
                // Vượng địa: Tý, Mão, Dần, Thân
                "Tý" or "Mão" or "Dần" or "Thân" => 80,
                // Đắc địa: Sửu, Mùi
                "Sửu" or "Mùi" => 60,
                // Hãm địa: Dậu, Tỵ, Hợi
                "Dậu" or "Tỵ" or "Hợi" => 30,
                _ => baseBrightness
            },
            "Cự Môn" => branch switch
            {
                // Miếu địa: Mão, Dậu
                "Mão" or "Dậu" => 100,
                // Vượng địa: Tý, Ngọ, Dần
                "Tý" or "Ngọ" or "Dần" => 80,
                // Đắc địa: Thân, Hợi
                "Thân" or "Hợi" => 60,
                // Hãm địa: Thìn, Tuất, Sửu, Mùi, Tỵ
                "Thìn" or "Tuất" or "Sửu" or "Mùi" or "Tỵ" => 30,
                _ => baseBrightness
            },
            "Thái Dương" => branch switch
            {
                // Miếu địa: Tỵ, Ngọ
                "Tỵ" or "Ngọ" => 100,
                // Vượng địa: Dần, Mão, Thìn
                "Dần" or "Mão" or "Thìn" => 80,
                // Đắc địa: Sửu, Mùi
                "Sửu" or "Mùi" => 60,
                // Hãm địa: Thân, Dậu, Tuất, Hợi, Tý
                "Thân" or "Dậu" or "Tuất" or "Hợi" or "Tý" => 30,
                _ => baseBrightness
            },
            _ => baseBrightness
        };
    }
}
