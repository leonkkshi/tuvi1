using Backend.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    public class GeminiInterpretationService : IAIInterpretationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<GeminiInterpretationService> _logger;
        private readonly IAIRequestThrottler _throttler;

        public GeminiInterpretationService(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            ILogger<GeminiInterpretationService> logger,
            IAIRequestThrottler throttler)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key không được cấu hình");
            _model = configuration["Gemini:Model"] ?? "gemini-pro";
            _logger = logger;
            _throttler = throttler;
        }

        public async Task<InterpretationResponse> InterpretChartAsync(InterpretationRequest request)
        {
            // Sử dụng throttler để giới hạn concurrent requests
            return await _throttler.ExecuteAsync(async () =>
            {
                return await ExecuteInterpretationAsync(request);
            });
        }

        private async Task<InterpretationResponse> ExecuteInterpretationAsync(InterpretationRequest request)
        {
            try
            {
                // Xây dựng prompt cho AI
                var systemPrompt = GetSystemPrompt();
                var userPrompt = BuildPrompt(request);
                var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";

                // Gọi Gemini API với format đúng
                var geminiRequest = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 32000,
                        topK = 40,
                        topP = 0.95
                    },
                    safetySettings = new[]
                    {
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                    }
                };

                var json = JsonSerializer.Serialize(geminiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // API từ AI Studio cần dùng v1beta
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
                
                _logger.LogInformation("Calling Gemini API with model: {Model}", _model);
                
                var response = await _httpClient.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                    throw new Exception($"Gemini API trả về lỗi {response.StatusCode}: {responseJson}");
                }

                var result = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (result?.Candidates == null || result.Candidates.Length == 0)
                {
                    throw new Exception("Không nhận được phản hồi từ Gemini AI");
                }

                var interpretation = result.Candidates[0].Content.Parts[0].Text;

                return ParseAIResponse(interpretation, request.Chart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Gemini API");
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

        private string GetSystemPrompt()
        {
            return @"Bạn là Thầy Tử Vi - một chuyên gia Tử Vi Đẩu Số hàng đầu với hơn 40 năm kinh nghiệm.

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

5. QUY TẮC TỔNG HỢP:
   - Cát + Cát = Đại cát
   - Cát + Hung = Bình hòa, xem sao nào mạnh hơn
   - Hung + Hung = Đại hung
   - Hóa Lộc, Hóa Quyền, Hóa Khoa giải hung
   - Hóa Kỵ làm hung thêm

HÃY LUẬN GIẢI:
- Chi tiết, cụ thể từng cung
- Liên hệ tam phương tứ chính rõ ràng
- Với Mệnh & Thân: nhất định phải xét nhị hợp và cung liền kề
- Dùng thuật ngữ Tử Vi chính xác
- Giải thích dễ hiểu, thiết thực
- Đưa ra lời khuyên cụ thể

Phong cách: Chuyên nghiệp nhưng gần gũi, tận tâm như thầy hướng dẫn trò.";
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
            
            // Tìm tên cung Thân
            var thanPalace = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == chart.ThanPalace);
            if (thanPalace != null)
            {
                sb.AppendLine($"Cung Thân: {thanPalace.PalaceName} (Địa chi: {GetBranchName(chart.ThanPalace)})");
            }
            
            sb.AppendLine();

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

                var tamPhuong = GetTamPhuongTuChinh(palace.PalaceId, chart);
                sb.AppendLine($"\n  Tam phương tứ chính:");
                sb.AppendLine($"    - Đối cung ({tamPhuong.DoiCung.Name}): {FormatStarList(tamPhuong.DoiCung.Stars)}");
                sb.AppendLine($"    - Tam hợp trái ({tamPhuong.TamHopTrai.Name}): {FormatStarList(tamPhuong.TamHopTrai.Stars)}");
                sb.AppendLine($"    - Tam hợp phải ({tamPhuong.TamHopPhai.Name}): {FormatStarList(tamPhuong.TamHopPhai.Stars)}");

                if (palace.PalaceName == "Mệnh" || palace.PalaceId == chart.ThanPalace)
                {
                    var nhiHop = GetNhiHop(palace.PalaceId, chart);
                    sb.AppendLine($"\n  Nhị hợp (cặp đôi hợp khí):");
                    sb.AppendLine($"    - Cung hợp ({nhiHop.CungTruoc.Name}): {FormatStarList(nhiHop.CungTruoc.Stars)}");
                    
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
            sb.AppendLine("Hãy phân tích và luận giải lá số theo cấu trúc:");
            sb.AppendLine("1. TỔNG QUAN LÁ SỐ");
            sb.AppendLine("2. LUẬN CHI TIẾT 12 CUNG (nhớ xem tam phương tứ chính, nhị hợp, cung liền kề)");
            sb.AppendLine("3. ĐIỂM ĐẶC BIỆT");
            sb.AppendLine("4. CẢNH BÁO");
            sb.AppendLine("5. KHUYẾN NGHỊ");

            return sb.ToString();
        }

        private TamPhuongTuChinh GetTamPhuongTuChinh(int palaceId, TuViChart chart)
        {
            var doiCungId = ((palaceId + 5) % 12) + 1;
            var tamHopTraiId = ((palaceId + 3) % 12) + 1;
            var tamHopPhaiId = ((palaceId + 7) % 12) + 1;

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
            var nhiHopPairs = new Dictionary<int, int>
            {
                { 1, 2 }, { 2, 1 }, { 3, 12 }, { 12, 3 }, { 4, 11 }, { 11, 4 },
                { 5, 10 }, { 10, 5 }, { 6, 9 }, { 9, 6 }, { 7, 8 }, { 8, 7 }
            };

            int nhiHopId = nhiHopPairs[palaceId];
            var nhiHopCung = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == nhiHopId);

            return new NhiHop
            {
                CungTruoc = new CungInfo { Name = nhiHopCung?.PalaceName ?? "", Stars = nhiHopCung?.Stars ?? new List<StarInPalace>() },
                CungSau = new CungInfo { Name = "", Stars = new List<StarInPalace>() }
            };
        }

        private CungLienKe GetCungLienKe(int palaceId, TuViChart chart)
        {
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
            var branches = new[] { "Tý", "Sửu", "Dần", "Mão", "Thìn", "Tị", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi" };
            return branches[palaceId - 1];
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

        private InterpretationResponse ParseAIResponse(string aiResponse, TuViChart chart)
        {
            var response = new InterpretationResponse
            {
                OverallInterpretation = aiResponse,
                PalaceInterpretations = new(),
                KeyInsights = new(),
                Warnings = new(),
                Recommendations = new()
            };

            try
            {
                var sections = aiResponse.Split(new[] { "###", "##" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var section in sections)
                {
                    var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 0) continue;

                    var title = lines[0].Trim().ToLower();
                    
                    if (title.Contains("chú ý") || title.Contains("điểm nổi bật") || title.Contains("đặc biệt"))
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
                _logger.LogWarning(ex, "Không thể parse chi tiết từ Gemini response");
            }

            return response;
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
            public CungInfo CungTruoc { get; set; } = new();
            public CungInfo CungSau { get; set; } = new();
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

        // Gemini API response models
        private class GeminiResponse
        {
            public Candidate[] Candidates { get; set; } = Array.Empty<Candidate>();
        }

        private class Candidate
        {
            public Content Content { get; set; } = new();
        }

        private class Content
        {
            public Part[] Parts { get; set; } = Array.Empty<Part>();
        }

        private class Part
        {
            public string Text { get; set; } = string.Empty;
        }
    }
}
