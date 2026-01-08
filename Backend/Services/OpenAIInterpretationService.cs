using Backend.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services
{
    public class OpenAIInterpretationService : IAIInterpretationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<OpenAIInterpretationService> _logger;
        private readonly IAIRequestThrottler _throttler;
        private readonly IMemoryCache _cache;

        public OpenAIInterpretationService(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            ILogger<OpenAIInterpretationService> logger,
            IAIRequestThrottler throttler,
            IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key không được cấu hình");
            _model = configuration["OpenAI:Model"] ?? "gpt-4";
            _logger = logger;
            _throttler = throttler;
            _cache = cache;
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<InterpretationResponse> InterpretChartAsync(InterpretationRequest request)
        {
            // Tạo cache key từ request
            var cacheKey = $"openai_chart_{request.Chart.GetHashCode()}_vi";
            
            // Kiểm tra cache trước
            if (_cache.TryGetValue(cacheKey, out InterpretationResponse cachedResponse))
            {
                _logger.LogInformation("Cache hit for OpenAI chart interpretation");
                return cachedResponse;
            }

            // Sử dụng throttler để giới hạn concurrent requests
            var result = await _throttler.ExecuteAsync(async () =>
            {
                return await ExecuteInterpretationAsync(request);
            });

            // Cache kết quả trong 6 giờ (tăng để giảm tải AI)
            _cache.Set(cacheKey, result, TimeSpan.FromHours(6));
            _logger.LogInformation("Cached new OpenAI chart interpretation result");

            return result;
        }

        public async Task<string> InterpretSinglePalaceAsync(TuViChart chart, string palaceName)
        {
            // Sử dụng throttler để giới hạn concurrent requests
            return await _throttler.ExecuteAsync(async () =>
            {
                return await Task.FromResult($"Luận giải cung {palaceName} chưa được implement cho OpenAI service. Vui lòng sử dụng Gemini service.");
            });
        }

        private async Task<InterpretationResponse> ExecuteInterpretationAsync(InterpretationRequest request)
        {
            try
            {
                // Xây dựng prompt cho AI
                var prompt = BuildPrompt(request);

                // Gọi OpenAI API
                var chatRequest = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = @"Bạn là Thầy Tử Vi - một chuyên gia Tử Vi Đẩu Số hàng đầu với hơn 40 năm kinh nghiệm.

NGUYÊN TẮC LUẬN ĐOÁN TỬ VI:

1. TAM PHƯƠNG TỨ CHÍNH (Bắt buộc cho mọi cung):
   - Bản cung: Cung đang xét
   - Đối cung: Cung đối diện (cách 6 vị trí) - ảnh hưởng mạnh nhất
   - Tam hợp trái & phải: 2 cung tạo tam giác (cách 4 và 8 vị trí)
   - Các sao ở tam phương tứ chính CHIẾU vào bản cung, ảnh hưởng trực tiếp
   - Không được luận riêng bản cung mà bỏ qua tam phương!

2. NHỊ HỢP (Đặc biệt cho Mệnh & Thân):
   - Cặp đôi địa chi hợp khí theo ngũ hành
   - Ảnh hưởng đến bản chất sâu xa, vận khí tổng thể

3. CUNG LIỀN KỀ (Đặc biệt cho Mệnh & Thân):
   - 2 cung kề bên (trước và sau)
   - Ảnh hưởng đến hoàn cảnh, môi trường sống
   - Sao ở đây tác động gần gũi, thường trực

4. LUẬN ĐOÁN SAO:
   - Sao chính tinh: Xương cốt cung, quyết định bản chất
   - Sao phụ tinh: Tăng giảm cát hung, biến hóa ý nghĩa
   - Sao Hóa (Lộc, Quyền, Khoa, Kỵ): Rất quan trọng, thay đổi cục diện
   - Độ sáng sao: Sao sáng (Miếu, Vượng) mạnh, sao tối (Hãm, Bình) yếu

4. QUY TẮC TỔNG HỢP:
   - Cát + Cát = Đại cát
   - Cát + Hung = Bình hòa, xem sao nào mạnh hơn
   - Hung + Hung = Đại hung
   - Hóa Lộc, Hóa Quyền, Hóa Khoa giải hung
   - Hóa Kỵ làm hung thêm

HÃY LUẬN GIẢI:
- Chi tiết, cụ thể từng cung
- Liên hệ tam phương tứ chính rõ ràng
- Với Mệnh & Thân: nhất định phải xét nhị hợp
- Dùng thuật ngữ Tử Vi chính xác
- Giải thích dễ hiểu, thiết thực
- Đưa ra lời khuyên cụ thể

Phong cách: Chuyên nghiệp nhưng gần gũi, tận tâm như thầy hướng dẫn trò."
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.7,
                    max_tokens = 4000
                };

                var json = JsonSerializer.Serialize(chatRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenAIResponse>(responseJson);

                if (result?.Choices == null || result.Choices.Length == 0)
                {
                    throw new Exception("Không nhận được phản hồi từ AI");
                }

                var interpretation = result.Choices[0].Message.Content;

                // Parse kết quả từ AI (giả sử AI trả về theo format structured)
                return ParseAIResponse(interpretation, request.Chart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi OpenAI API");
                return new InterpretationResponse
                {
                    OverallInterpretation = "Xin lỗi, hiện không thể luận giải lá số. Vui lòng thử lại sau.",
                    PalaceInterpretations = new(),
                    KeyInsights = new() { "Lỗi: " + ex.Message },
                    Warnings = new(),
                    Recommendations = new()
                };
            }
        }

        private string BuildPrompt(InterpretationRequest request)
        {
            var chart = request.Chart;
            var sb = new StringBuilder();

            sb.AppendLine("=== THÔNG TIN LÁ SỐ TỬ VI ===");
            sb.AppendLine($"Ngày sinh: {chart.BirthDate:dd/MM/yyyy}");
            sb.AppendLine($"Giờ sinh: {chart.BirthTime}");
            sb.AppendLine($"Giới tính: {(chart.IsMale ? "Nam" : "Nữ")}");
            sb.AppendLine($"Âm Dương: {chart.AmDuong}");
            sb.AppendLine($"Ngũ Hành Cục: {GetNguHanhCucName(chart.NguHanhCuc)}");
            sb.AppendLine($"Năm âm lịch: {chart.LunarYear}");
            sb.AppendLine();

            // Xác định vị trí cung Mệnh và Thân
            var menhPalace = chart.PalaceStars.FirstOrDefault(p => p.PalaceName == "Mệnh");
            var thanPalace = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == chart.ThanPalace);

            sb.AppendLine("=== PHÂN BỐ SAO TRONG 12 CUNG VỚI TAM PHƯƠNG TỨ CHÍNH ===");
            foreach (var palace in chart.PalaceStars.OrderBy(p => p.PalaceId))
            {
                sb.AppendLine($"\n【{palace.PalaceName}】 (Vị trí: {GetBranchName(palace.PalaceId)})");
                
                // Hiển thị Tuần và Triệt nếu có
                var specialMarks = new List<string>();
                if (palace.HasTuan) specialMarks.Add("Tuần");
                if (palace.HasTriet) specialMarks.Add("Triệt");
                if (specialMarks.Any())
                {
                    sb.AppendLine($"  ⚠️ Đặc điểm: {string.Join(", ", specialMarks)}");
                }
                
                // Sao trong bản cung
                if (palace.Stars.Any())
                {
                    sb.AppendLine("  Bản cung:");
                    foreach (var star in palace.Stars)
                    {
                        var hoaInfo = !string.IsNullOrEmpty(star.Hoa) ? $" - {star.Hoa}" : "";
                        sb.AppendLine($"    - {star.StarName}{hoaInfo} ({star.Type}, {star.Element}, {star.Nature}, Độ sáng: {star.Brightness})");
                    }
                }
                else
                {
                    sb.AppendLine("  Bản cung: (Không có sao chính)");
                }

                // Tam phương tứ chính
                var tamPhuong = GetTamPhuongTuChinh(palace.PalaceId, chart);
                sb.AppendLine($"\n  Tam phương tứ chính:");
                sb.AppendLine($"    - Đối cung ({tamPhuong.DoiCung.Name}): {FormatStarList(tamPhuong.DoiCung.Stars)}");
                sb.AppendLine($"    - Tam hợp trái ({tamPhuong.TamHopTrai.Name}): {FormatStarList(tamPhuong.TamHopTrai.Stars)}");
                sb.AppendLine($"    - Tam hợp phải ({tamPhuong.TamHopPhai.Name}): {FormatStarList(tamPhuong.TamHopPhai.Stars)}");

                // Nhị hợp cho cung Mệnh và Thân
                if (palace.PalaceName == "Mệnh" || palace.PalaceId == chart.ThanPalace)
                {
                    var nhiHop = GetNhiHop(palace.PalaceId, chart);
                    sb.AppendLine($"\n  Nhị hợp (cặp đôi hợp khí):");
                    sb.AppendLine($"    - Cung hợp ({nhiHop.CungHop.Name}): {FormatStarList(nhiHop.CungHop.Stars)}");
                    
                    var lienKe = GetCungLienKe(palace.PalaceId, chart);
                    sb.AppendLine($"\n  Cung liền kề (2 bên):");
                    sb.AppendLine($"    - Cung trước ({lienKe.CungTruoc.Name}): {FormatStarList(lienKe.CungTruoc.Stars)}");
                    sb.AppendLine($"    - Cung sau ({lienKe.CungSau.Name}): {FormatStarList(lienKe.CungSau.Stars)}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== YÊU CẦU LUẬN GIẢI CHI TIẾT ===");
            sb.AppendLine($"Lĩnh vực tập trung: {GetFocusAreaName(request.FocusArea)}");
            sb.AppendLine();
            sb.AppendLine("QUAN TRỌNG - Phương pháp luận giải theo Tử Vi Đẩu Số chính thống:");
            sb.AppendLine();
            sb.AppendLine("1. XEM TAM PHƯƠNG TỨ CHÍNH:");
            sb.AppendLine("   - Mỗi cung PHẢI xem kết hợp với 3 cung tam phương tứ chính (đối cung + 2 cung tam hợp)");
            sb.AppendLine("   - Các sao ở tam phương tứ chính chiếu sáng vào bản cung, ảnh hưởng mạnh mẽ");
            sb.AppendLine("   - Sao cát ở tam phương giúp cát, sao hung ở tam phương làm hung thêm");
            sb.AppendLine();
            sb.AppendLine("2. XEM NHỊ HỢP CHO CUNG MỆNH VÀ THÂN:");
            sb.AppendLine("   - Nhị hợp là cặp đôi địa chi hợp khí: Tý-Sửu, Dần-Hợi, Mão-Tuất, Thìn-Dậu, Tỵ-Thân, Ngọ-Mùi");
            sb.AppendLine("   - Sao ở cung nhị hợp ảnh hưởng đến tính cách, số mệnh, vận khí");
            sb.AppendLine("   - Đặc biệt quan trọng cho cung Mệnh và cung Thân, tạo nên bản chất sâu xa");
            sb.AppendLine();
            sb.AppendLine("3. XEM CUNG LIỀN KỀ (2 BÊN):");
            sb.AppendLine("   - Cung trước và cung sau (kề bên trái phải)");
            sb.AppendLine("   - Sao ở cung liền kề tác động gần gũi, hỗ trợ hoặc cản trở");
            sb.AppendLine("   - Ảnh hưởng đến hoàn cảnh xung quanh, môi trường sống");
            sb.AppendLine();
            sb.AppendLine("4. XÉT TƯƠNG QUAN SAO:");
            sb.AppendLine("   - Sao chính tinh + phụ tinh tương trợ như thế nào");
            sb.AppendLine("   - Sao hóa (Hóa Lộc, Hóa Quyền, Hóa Khoa, Hóa Kỵ) ảnh hưởng ra sao");
            sb.AppendLine("   - Các sao hung cát tương tác");
            sb.AppendLine();
            sb.AppendLine("Hãy phân tích và luận giải theo cấu trúc sau:");
            sb.AppendLine();
            sb.AppendLine("1. TỔNG QUAN LÁ SỐ:");
            sb.AppendLine("   - Đánh giá tổng quát cục diện lá số");
            sb.AppendLine("   - Điểm mạnh, điểm yếu chủ chốt");
            sb.AppendLine("   - Xu hướng vận mệnh tổng thể");
            sb.AppendLine();
            sb.AppendLine("2. LUẬN GIẢI CHI TIẾT 12 CUNG:");
            sb.AppendLine("   Mỗi cung cần luận theo thứ tự:");
            sb.AppendLine("   a) Bản cung có sao gì (chính tinh, phụ tinh, hóa)");
            sb.AppendLine("   b) Tam phương tứ chính chiếu đến những sao nào");
            sb.AppendLine("   c) (Riêng Mệnh & Thân) Nhị hợp có sao gì, cung liền kề có sao gì");
            sb.AppendLine("   d) Tổng hợp luận đoán về khía cạnh đời sống tương ứng");
            sb.AppendLine();
            sb.AppendLine("3. ĐIỂM ĐẶC BIỆT:");
            sb.AppendLine("   - Các cách cục đặc biệt (nếu có)");
            sb.AppendLine("   - Điểm nổi bật về cát hung");
            sb.AppendLine("   - Những tương tác sao quan trọng");
            sb.AppendLine();
            sb.AppendLine("4. CẢNH BÁO:");
            sb.AppendLine("   - Những khía cạnh cần thận trọng");
            sb.AppendLine("   - Các vấn đề tiềm ẩn cần chú ý");
            sb.AppendLine();
            sb.AppendLine("5. KHUYẾN NGHỊ:");
            sb.AppendLine("   - Lời khuyên cụ thể cho từng khía cạnh");
            sb.AppendLine("   - Hướng đi phù hợp với lá số");

            return sb.ToString();
        }

        private TamPhuongTuChinh GetTamPhuongTuChinh(int palaceId, TuViChart chart)
        {
            // Tam phương tứ chính: Bản cung + Đối cung + 2 cung tam hợp
            // Đối cung: cách 6 vị trí (180 độ)
            // Tam hợp trái: cách 4 vị trí (120 độ ngược chiều)
            // Tam hợp phải: cách 8 vị trí (120 độ thuận chiều)
            
            var doiCungId = ((palaceId + 5) % 12) + 1; // Cách 6 cung
            var tamHopTraiId = ((palaceId + 3) % 12) + 1; // Cách 4 cung
            var tamHopPhaiId = ((palaceId + 7) % 12) + 1; // Cách 8 cung

            var doiCung = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == doiCungId);
            var tamHopTrai = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == tamHopTraiId);
            var tamHopPhai = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == tamHopPhaiId);

            return new TamPhuongTuChinh
            {
                DoiCung = new CungInfo { Name = doiCung?.PalaceName ?? "", Stars = doiCung?.Stars ?? new List<StarInPalace>() },
                TamHopTrai = new CungInfo { Name = tamHopTrai?.PalaceName ?? "", Stars = tamHopTrai?.Stars ?? new List<StarInPalace>() },
                TamHopPhai = new CungInfo { Name = tamHopPhai?.PalaceName ?? "", Stars = tamHopPhai?.Stars ?? new List<StarInPalace>() }
            };
        }

        private NhiHop GetNhiHop(int palaceId, TuViChart chart)
        {
            // Nhị hợp: Các cặp địa chi hợp với nhau
            // Tý(1)-Sửu(2), Dần(3)-Hợi(12), Mão(4)-Tuất(11), Thìn(5)-Dậu(10), Tỵ(6)-Thân(9), Ngọ(7)-Mùi(8)
            
            var nhiHopPairs = new Dictionary<int, int>
            {
                { 1, 2 },   // Tý ↔ Sửu
                { 2, 1 },   // Sửu ↔ Tý
                { 3, 12 },  // Dần ↔ Hợi
                { 12, 3 },  // Hợi ↔ Dần
                { 4, 11 },  // Mão ↔ Tuất
                { 11, 4 },  // Tuất ↔ Mão
                { 5, 10 },  // Thìn ↔ Dậu
                { 10, 5 },  // Dậu ↔ Thìn
                { 6, 9 },   // Tỵ ↔ Thân
                { 9, 6 },   // Thân ↔ Tỵ
                { 7, 8 },   // Ngọ ↔ Mùi
                { 8, 7 }    // Mùi ↔ Ngọ
            };

            int nhiHopId = nhiHopPairs[palaceId];
            var nhiHopCung = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == nhiHopId);

            return new NhiHop
            {
                CungHop = new CungInfo { Name = nhiHopCung?.PalaceName ?? "", Stars = nhiHopCung?.Stars ?? new List<StarInPalace>() }
            };
        }

        private CungLienKe GetCungLienKe(int palaceId, TuViChart chart)
        {
            // Cung liền kề: 2 cung kề bên (trước và sau)
            var cungTruocId = palaceId == 1 ? 12 : palaceId - 1;
            var cungSauId = palaceId == 12 ? 1 : palaceId + 1;

            var cungTruoc = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == cungTruocId);
            var cungSau = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == cungSauId);

            return new CungLienKe
            {
                CungTruoc = new CungInfo { Name = cungTruoc?.PalaceName ?? "", Stars = cungTruoc?.Stars ?? new List<StarInPalace>() },
                CungSau = new CungInfo { Name = cungSau?.PalaceName ?? "", Stars = cungSau?.Stars ?? new List<StarInPalace>() }
            };
        }

        private string FormatStarList(List<StarInPalace> stars)
        {
            if (!stars.Any()) return "(Không có sao chính)";
            return string.Join(", ", stars.Select(s => 
            {
                var hoaInfo = !string.IsNullOrEmpty(s.Hoa) ? $"-{s.Hoa}" : "";
                return $"{s.StarName}{hoaInfo}({s.Nature})";
            }));
        }

        private string GetBranchName(int palaceId)
        {
            var branches = new[] { "Tý", "Sửu", "Dần", "Mão", "Thìn", "Tỵ", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi" };
            return branches[palaceId - 1];
        }

        // Helper classes
        private class TamPhuongTuChinh
        {
            public CungInfo DoiCung { get; set; } = new();
            public CungInfo TamHopTrai { get; set; } = new();
            public CungInfo TamHopPhai { get; set; } = new();
        }

        private class NhiHop
        {
            public CungInfo CungHop { get; set; } = new();
        }

        private class CungLienKe
        {
            public CungInfo CungTruoc { get; set; } = new();
            public CungInfo CungSau { get; set; } = new();
        }

        private class CungInfo
        {
            public string Name { get; set; } = string.Empty;
            public List<StarInPalace> Stars { get; set; } = new();
        }

        private InterpretationResponse ParseAIResponse(string aiResponse, TuViChart chart)
        {
            // Đây là phiên bản đơn giản - parse text từ AI
            // Bạn có thể cải thiện bằng cách yêu cầu AI trả về JSON format
            
            var response = new InterpretationResponse
            {
                OverallInterpretation = aiResponse, // Giữ toàn bộ luận giải từ AI
                PalaceInterpretations = new(),
                KeyInsights = new(),
                Warnings = new(),
                Recommendations = new()
            };

            // Parse sections (simple implementation)
            try
            {
                var sections = aiResponse.Split(new[] { "###", "##" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var section in sections)
                {
                    var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 0) continue;

                    var title = lines[0].Trim().ToLower();
                    
                    if (title.Contains("chú ý") || title.Contains("điểm nổi bật"))
                    {
                        response.KeyInsights.AddRange(lines.Skip(1).Select(l => l.Trim('-', ' ', '*').Trim()));
                    }
                    else if (title.Contains("cảnh báo") || title.Contains("lưu ý"))
                    {
                        response.Warnings.AddRange(lines.Skip(1).Select(l => l.Trim('-', ' ', '*').Trim()));
                    }
                    else if (title.Contains("khuyến nghị") || title.Contains("lời khuyên"))
                    {
                        response.Recommendations.AddRange(lines.Skip(1).Select(l => l.Trim('-', ' ', '*').Trim()));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể parse chi tiết từ AI response");
            }

            return response;
        }

        private string GetNguHanhCucName(int cuc)
        {
            return cuc switch
            {
                2 => "Thủy Nhị Cục",
                3 => "Mộc Tam Cục",
                4 => "Kim Tứ Cục",
                5 => "Thổ Ngũ Cục",
                6 => "Hỏa Lục Cục",
                _ => "Không xác định"
            };
        }

        private string GetFocusAreaName(string focusArea)
        {
            return focusArea.ToLower() switch
            {
                "career" => "Sự nghiệp",
                "love" => "Tình duyên",
                "health" => "Sức khỏe",
                "wealth" => "Tài lộc",
                _ => "Tổng quan"
            };
        }

        // Model cho OpenAI response
        private class OpenAIResponse
        {
            public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        }

        private class Choice
        {
            public Message Message { get; set; } = new();
        }

        private class Message
        {
            public string Content { get; set; } = string.Empty;
        }
    }
}
