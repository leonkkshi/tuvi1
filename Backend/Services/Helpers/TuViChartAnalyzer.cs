using Backend.Models;

namespace Backend.Services.Helpers
{
    /// <summary>
    /// Helper class để phân tích các mối quan hệ giữa các cung trong lá số Tử Vi
    /// Bao gồm: Tam phương tứ chính, Nhị hợp, Cung liền kề
    /// </summary>
    public static class TuViChartAnalyzer
    {
        /// <summary>
        /// Lấy thông tin Tam phương tứ chính của một cung
        /// Tam phương tứ chính gồm: Đối cung, Tam hợp trái, Tam hợp phải
        /// </summary>
        public static TamPhuongTuChinh GetTamPhuongTuChinh(int palaceId, TuViChart chart)
        {
            // Đối cung: cách 6 vị trí (đối diện)
            var doiCungId = ((palaceId + 5) % 12) + 1;
            
            // Tam hợp trái: cách 4 vị trí
            var tamHopTraiId = ((palaceId + 3) % 12) + 1;
            
            // Tam hợp phải: cách 8 vị trí
            var tamHopPhaiId = ((palaceId + 7) % 12) + 1;

            var doiCung = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == doiCungId);
            var tamHopTrai = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == tamHopTraiId);
            var tamHopPhai = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == tamHopPhaiId);

            return new TamPhuongTuChinh
            {
                DoiCung = new CungInfo 
                { 
                    PalaceId = doiCungId,
                    Name = doiCung?.PalaceName ?? "", 
                    Branch = GetBranchName(doiCungId),
                    Stars = doiCung?.Stars ?? new List<StarInPalace>() 
                },
                TamHopTrai = new CungInfo 
                { 
                    PalaceId = tamHopTraiId,
                    Name = tamHopTrai?.PalaceName ?? "", 
                    Branch = GetBranchName(tamHopTraiId),
                    Stars = tamHopTrai?.Stars ?? new List<StarInPalace>() 
                },
                TamHopPhai = new CungInfo 
                { 
                    PalaceId = tamHopPhaiId,
                    Name = tamHopPhai?.PalaceName ?? "", 
                    Branch = GetBranchName(tamHopPhaiId),
                    Stars = tamHopPhai?.Stars ?? new List<StarInPalace>() 
                }
            };
        }

        /// <summary>
        /// Lấy thông tin Nhị hợp (cặp đôi hợp khí) của một cung
        /// Dùng cho cung Mệnh và cung Thân
        /// </summary>
        public static NhiHop GetNhiHop(int palaceId, TuViChart chart)
        {
            // Các cặp nhị hợp theo địa chi
            var nhiHopPairs = new Dictionary<int, int>
            {
                { 1, 2 },   // Tý - Sửu hợp Thổ
                { 2, 1 },   // Sửu - Tý hợp Thổ
                { 3, 12 },  // Dần - Hợi hợp Mộc
                { 12, 3 },  // Hợi - Dần hợp Mộc
                { 4, 11 },  // Mão - Tuất hợp Hỏa
                { 11, 4 },  // Tuất - Mão hợp Hỏa
                { 5, 10 },  // Thìn - Dậu hợp Kim
                { 10, 5 },  // Dậu - Thìn hợp Kim
                { 6, 9 },   // Tị - Thân hợp Thủy
                { 9, 6 },   // Thân - Tị hợp Thủy
                { 7, 8 },   // Ngọ - Mùi hợp Thái Dương/Thái Âm
                { 8, 7 }    // Mùi - Ngọ hợp Thái Dương/Thái Âm
            };

            int nhiHopId = nhiHopPairs[palaceId];
            var nhiHopCung = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == nhiHopId);

            return new NhiHop
            {
                CungHop = new CungInfo 
                { 
                    PalaceId = nhiHopId,
                    Name = nhiHopCung?.PalaceName ?? "", 
                    Branch = GetBranchName(nhiHopId),
                    Stars = nhiHopCung?.Stars ?? new List<StarInPalace>() 
                }
            };
        }

        /// <summary>
        /// Lấy thông tin 2 cung liền kề (trước và sau)
        /// Ảnh hưởng đến hoàn cảnh, môi trường sống
        /// </summary>
        public static CungLienKe GetCungLienKe(int palaceId, TuViChart chart)
        {
            var cungTruocId = palaceId == 1 ? 12 : palaceId - 1;
            var cungSauId = palaceId == 12 ? 1 : palaceId + 1;

            var cungTruoc = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == cungTruocId);
            var cungSau = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == cungSauId);

            return new CungLienKe
            {
                CungTruoc = new CungInfo 
                { 
                    PalaceId = cungTruocId,
                    Name = cungTruoc?.PalaceName ?? "", 
                    Branch = GetBranchName(cungTruocId),
                    Stars = cungTruoc?.Stars ?? new List<StarInPalace>() 
                },
                CungSau = new CungInfo 
                { 
                    PalaceId = cungSauId,
                    Name = cungSau?.PalaceName ?? "", 
                    Branch = GetBranchName(cungSauId),
                    Stars = cungSau?.Stars ?? new List<StarInPalace>() 
                }
            };
        }

        /// <summary>
        /// Format danh sách sao thành chuỗi dễ đọc
        /// </summary>
        public static string FormatStarList(List<StarInPalace> stars)
        {
            if (!stars.Any()) return "(Không có sao chính)";
            
            return string.Join(", ", stars.Select(s => 
            {
                var hoaInfo = !string.IsNullOrEmpty(s.Hoa) ? $"-{s.Hoa}" : "";
                return $"{s.StarName}{hoaInfo}({s.Nature})";
            }));
        }

        /// <summary>
        /// Format chi tiết sao với đầy đủ thông tin
        /// </summary>
        public static string FormatStarDetailList(List<StarInPalace> stars)
        {
            if (!stars.Any()) return "(Không có sao chính)";
            
            return string.Join(", ", stars.Select(s => 
            {
                var hoaInfo = !string.IsNullOrEmpty(s.Hoa) ? $" - {s.Hoa}" : "";
                return $"{s.StarName}{hoaInfo} ({s.Type}, {s.Element}, {s.Nature}, Độ sáng: {s.Brightness})";
            }));
        }

        /// <summary>
        /// Lấy tên địa chi từ PalaceId
        /// </summary>
        public static string GetBranchName(int palaceId)
        {
            var branches = new[] { "Tý", "Sửu", "Dần", "Mão", "Thìn", "Tị", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi" };
            return branches[palaceId - 1];
        }

        /// <summary>
        /// Xây dựng thông tin đầy đủ về một cung
        /// Bao gồm: Bản cung, Tam phương tứ chính, Nhị hợp (nếu là Mệnh/Thân), Cung liền kề (nếu là Mệnh/Thân)
        /// </summary>
        public static string BuildPalaceAnalysis(PalaceStar palace, TuViChart chart, bool includeNhiHop = false)
        {
            return BuildPalaceAnalysis(palace, chart, includeNhiHop, includeNhiHop);
        }

        /// <summary>
        /// Xây dựng thông tin đầy đủ về một cung với điều khiển riêng cho nhị hợp và giáp cung
        /// </summary>
        public static string BuildPalaceAnalysis(PalaceStar palace, TuViChart chart, bool includeNhiHop, bool includeGiapCung)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"【{palace.PalaceName}】 (Vị trí: {GetBranchName(palace.PalaceId)})");
            
            // Hiển thị Tuần và Triệt nếu có
            var specialMarks = new List<string>();
            if (palace.HasTuan) specialMarks.Add("Tuần");
            if (palace.HasTriet) specialMarks.Add("Triệt");
            if (specialMarks.Any())
            {
                sb.AppendLine($"  ⚠️ Đặc điểm: {string.Join(", ", specialMarks)}");
            }
            
            // Bản cung
            sb.AppendLine("  Bản cung:");
            if (palace.Stars.Any())
            {
                foreach (var star in palace.Stars)
                {
                    var hoaInfo = !string.IsNullOrEmpty(star.Hoa) ? $" - {star.Hoa}" : "";
                    sb.AppendLine($"    - {star.StarName}{hoaInfo} ({star.Type}, {star.Element}, {star.Nature}, Độ sáng: {star.Brightness})");
                }
            }
            else
            {
                sb.AppendLine("    (Không có sao chính)");
            }

            // Tam phương tứ chính
            var tamPhuong = GetTamPhuongTuChinh(palace.PalaceId, chart);
            sb.AppendLine($"\n  Tam phương tứ chính:");
            sb.AppendLine($"    - Đối cung ({tamPhuong.DoiCung.Name} - {tamPhuong.DoiCung.Branch}): {FormatStarList(tamPhuong.DoiCung.Stars)}");
            sb.AppendLine($"    - Tam hợp trái ({tamPhuong.TamHopTrai.Name} - {tamPhuong.TamHopTrai.Branch}): {FormatStarList(tamPhuong.TamHopTrai.Stars)}");
            sb.AppendLine($"    - Tam hợp phải ({tamPhuong.TamHopPhai.Name} - {tamPhuong.TamHopPhai.Branch}): {FormatStarList(tamPhuong.TamHopPhai.Stars)}");

            // Nhị hợp (chỉ cho Mệnh và Thân)
            if (includeNhiHop)
            {
                var nhiHop = GetNhiHop(palace.PalaceId, chart);
                sb.AppendLine($"\n  Nhị hợp (cặp đôi hợp khí):");
                sb.AppendLine($"    - Cung hợp ({nhiHop.CungHop.Name} - {nhiHop.CungHop.Branch}): {FormatStarList(nhiHop.CungHop.Stars)}");
            }

            // Cung liền kề (giáp cung)
            if (includeGiapCung)
            {
                var lienKe = GetCungLienKe(palace.PalaceId, chart);
                sb.AppendLine($"\n  Cung liền kề (2 bên):");
                sb.AppendLine($"    - Cung trước ({lienKe.CungTruoc.Name} - {lienKe.CungTruoc.Branch}): {FormatStarList(lienKe.CungTruoc.Stars)}");
                sb.AppendLine($"    - Cung sau ({lienKe.CungSau.Name} - {lienKe.CungSau.Branch}): {FormatStarList(lienKe.CungSau.Stars)}");
            }

            return sb.ToString();
        }
    }

    #region Helper Models

    public class TamPhuongTuChinh
    {
        public CungInfo DoiCung { get; set; } = new();
        public CungInfo TamHopTrai { get; set; } = new();
        public CungInfo TamHopPhai { get; set; } = new();
    }

    public class NhiHop
    {
        public CungInfo CungHop { get; set; } = new();
    }

    public class CungLienKe
    {
        public CungInfo CungTruoc { get; set; } = new();
        public CungInfo CungSau { get; set; } = new();
    }

    public class CungInfo
    {
        public int PalaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public List<StarInPalace> Stars { get; set; } = new();
    }

    #endregion
}
