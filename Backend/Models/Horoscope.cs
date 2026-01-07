namespace Backend.Models
{
    // Sao trong Tử Vi Đẩu Số
    public class Star
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Tử Vi, Thiên Cơ, Thái Dương, etc.
        public string Type { get; set; } = string.Empty; // Chính tinh, Phụ tinh, Lưu tinh
        public string Element { get; set; } = string.Empty; // Kim, Mộc, Thủy, Hỏa, Thổ
        public string Nature { get; set; } = string.Empty; // Cát,흉
        public string Description { get; set; } = string.Empty;
        public int Brightness { get; set; } // Độ sáng (0-100)
    }

    // Lá số Tử Vi của một người
    public class TuViChart
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public TimeSpan BirthTime { get; set; }
        public bool IsMale { get; set; }
        public string LunarYear { get; set; } = string.Empty;
        public string LunarMonth { get; set; } = string.Empty;
        public string LunarDay { get; set; } = string.Empty;
        public int NguHanhCuc { get; set; } // 2=Thủy Nhị, 3=Mộc Tam, 4=Kim Tứ, 5=Thổ Ngũ, 6=Hỏa Lục
        public string AmDuong { get; set; } = string.Empty; // Dương Nam, Âm Nam, Dương Nữ, Âm Nữ
        public int ThanPalace { get; set; } // Cung Thân (1-12)
        public List<PalaceStar> PalaceStars { get; set; } = new();
        public Dictionary<int, int> DaiVan { get; set; } = new(); // Key = Palace Position (1-12), Value = Tuổi bắt đầu Đại Vận
        public Dictionary<int, string> TieuHan { get; set; } = new(); // Key = Palace Position (1-12), Value = Chi năm tiểu hạn (Tý, Sửu...)
        public Dictionary<int, int> NguyetHan { get; set; } = new(); // Key = Palace Position (1-12), Value = Tháng âm lịch (1-12)

        // Triệt và Tuần - kẹp giữa 2 cung (không phải sao)
        public string TrietBetween { get; set; } = string.Empty; // VD: "Thân-Dậu" (2 cung)
        public string TuanPositions { get; set; } = string.Empty; // VD: "Thân,Dậu" (2 cung)
    }

    // Sao trong cung
    public class PalaceStar
    {
        public int PalaceId { get; set; }
        public string PalaceName { get; set; } = string.Empty;
        public List<StarInPalace> Stars { get; set; } = new();
        public bool HasTuan { get; set; } = false; // Đánh dấu cung có Tuần
        public bool HasTriet { get; set; } = false; // Đánh dấu cung có Triệt
    }

    public class StarInPalace
    {
        public int StarId { get; set; }
        public string StarName { get; set; } = string.Empty;
        public int Brightness { get; set; }
        public string Element { get; set; } = string.Empty; // Kim, Mộc, Thủy, Hỏa, Thổ
        public string Nature { get; set; } = string.Empty; // Cát, Hung
        public string Type { get; set; } = string.Empty; // Chính tinh, Phụ tinh, etc.
        public string Hoa { get; set; } = string.Empty; // Hóa Lộc, Hóa Quyền, Hóa Khoa, Hóa Kỵ (nếu có)
    }
}
