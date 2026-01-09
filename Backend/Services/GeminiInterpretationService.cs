using Backend.Models;
using Backend.Services.Helpers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services
{
    public class GeminiInterpretationService : IAIInterpretationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly ILogger<GeminiInterpretationService> _logger;
        private readonly IAIRequestThrottler _throttler;
        private readonly IMemoryCache _cache;

        public GeminiInterpretationService(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            ILogger<GeminiInterpretationService> logger,
            IAIRequestThrottler throttler,
            IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient();
            _model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            _logger = logger;
            _throttler = throttler;
            _cache = cache;
        }

        public async Task<InterpretationResponse> InterpretChartAsync(InterpretationRequest request, string apiKey, string provider)
        {
            if (provider != "Gemini")
            {
                throw new ArgumentException("This service only supports Gemini provider");
            }

            // Tạo cache key từ request
            var cacheKey = $"gemini_chart_{request.Chart.GetHashCode()}_vi";
            
            // Kiểm tra cache trước
            if (_cache.TryGetValue(cacheKey, out InterpretationResponse cachedResponse))
            {
                _logger.LogInformation("Cache hit for chart interpretation");
                return cachedResponse;
            }

            // Sử dụng throttler để giới hạn concurrent requests
            var result = await _throttler.ExecuteAsync(async () =>
            {
                return await ExecuteInterpretationAsync(request, apiKey);
            });

            // Cache kết quả trong 6 giờ (tăng để giảm tải AI)
            _cache.Set(cacheKey, result, TimeSpan.FromHours(6));
            _logger.LogInformation("Cached new chart interpretation result");

            return result;
        }

        public async Task<string> InterpretSinglePalaceAsync(TuViChart chart, string palaceName, string apiKey, string provider)
        {
            if (provider != "Gemini")
            {
                throw new ArgumentException("This service only supports Gemini provider");
            }

            // Tạo cache key từ chart và palace
            var cacheKey = $"gemini_palace_{chart.GetHashCode()}_{palaceName}";
            
            // Kiểm tra cache trước
            if (_cache.TryGetValue(cacheKey, out string cachedResult))
            {
                _logger.LogInformation("Cache hit for palace interpretation: {PalaceName}", palaceName);
                return cachedResult;
            }

            // Sử dụng throttler để giới hạn concurrent requests
            var result = await _throttler.ExecuteAsync(async () =>
            {
                return await ExecuteSinglePalaceInterpretationAsync(chart, palaceName, apiKey);
            });

            // Cache kết quả trong 6 giờ (tăng để giảm tải AI)
            _cache.Set(cacheKey, result, TimeSpan.FromHours(6));
            _logger.LogInformation("Cached new palace interpretation result for: {PalaceName}", palaceName);

            return result;
        }

        private async Task<InterpretationResponse> ExecuteInterpretationAsync(InterpretationRequest request, string apiKey)
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
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={apiKey}";
                
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

        private async Task<string> ExecuteSinglePalaceInterpretationAsync(TuViChart chart, string palaceName, string apiKey)
        {
            try
            {
                // Tìm cung theo tên, trim khoảng trắng để tránh lỗi
                var palace = chart.PalaceStars.FirstOrDefault(p => 
                    p.PalaceName.Trim().Equals(palaceName.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (palace == null)
                {
                    _logger.LogWarning($"Không tìm thấy cung '{palaceName}'. Danh sách cung có: {string.Join(", ", chart.PalaceStars.Select(p => $"'{p.PalaceName}'"))}");
                    return $"Không tìm thấy cung {palaceName} trong lá số. Vui lòng kiểm tra lại tên cung.";
                }

                _logger.LogInformation($"Đang luận giải cung {palace.PalaceName} (ID: {palace.PalaceId})");
                

                // Xây dựng prompt cho một cung cụ thể
                var systemPrompt = GetSystemPrompt();
                var userPrompt = BuildSinglePalacePrompt(palace, chart);
                var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";

                // Gọi Gemini API
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
                        maxOutputTokens = 8000,
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

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={apiKey}";
                
                _logger.LogInformation("Calling Gemini API for single palace: {PalaceName}", palaceName);
                
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

                return result.Candidates[0].Content.Parts[0].Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Gemini API cho cung {PalaceName}", palaceName);
                return $"Xin lỗi, hiện không thể luận giải cung {palaceName}. Vui lòng thử lại sau. Lỗi: {ex.Message}";
            }
        }

        private string BuildSinglePalacePrompt(PalaceStar palace, TuViChart chart)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== THÔNG TIN LÁ SỐ TỬ VI ===");
            sb.AppendLine($"Ngày sinh: {chart.BirthDate:dd/MM/yyyy}");
            sb.AppendLine($"Giờ sinh: {chart.BirthTime}");
            sb.Append("Giới tính: ");
            sb.AppendLine(chart.IsMale ? "Nam" : "Nữ");
            sb.AppendLine($"Âm Dương: {chart.AmDuong}");
            sb.AppendLine($"Ngũ Hành Cục: {GetNguHanhCucName(chart.NguHanhCuc)}");
            sb.AppendLine($"Năm âm lịch: {chart.LunarYear}");
            sb.AppendLine();

            // Thông tin cung Mệnh và Thân để có context
            var menhPalace = chart.PalaceStars.FirstOrDefault(p => p.PalaceName == "Mệnh");
            var thanPalace = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == chart.ThanPalace);
            
            if (menhPalace != null)
            {
                sb.AppendLine($"Cung Mệnh: {TuViChartAnalyzer.GetBranchName(menhPalace.PalaceId)} - Sao chính: {TuViChartAnalyzer.FormatStarList(menhPalace.Stars)}");
            }
            if (thanPalace != null)
            {
                sb.AppendLine($"Cung Thân: {thanPalace.PalaceName} ({TuViChartAnalyzer.GetBranchName(chart.ThanPalace)}) - Sao chính: {TuViChartAnalyzer.FormatStarList(thanPalace.Stars)}");
            }
            sb.AppendLine();

            // Chi tiết cung cần luận giải
            var palaceInfo = GetPalaceInfo(palace.PalaceName);
            sb.AppendLine($"=== LUẬN GIẢI CHI TIẾT CUNG {palace.PalaceName.ToUpper()} ===");
            sb.AppendLine($"{palaceInfo.Icon} {palaceInfo.Meaning}");
            sb.AppendLine();

            // Nhị hợp chỉ cho Mệnh và Thân, nhưng giáp cung cho TẤT CẢ các cung khi luận riêng
            bool isMenhOrThan = palace.PalaceName == "Mệnh" || palace.PalaceId == chart.ThanPalace;
            sb.AppendLine(TuViChartAnalyzer.BuildPalaceAnalysis(palace, chart, includeNhiHop: isMenhOrThan, includeGiapCung: true));
            
            sb.AppendLine();
            sb.AppendLine("=== YÊU CẦU LUẬN GIẢI ===");
            sb.AppendLine($"Hãy luận giải chi tiết và toàn diện cung {palace.PalaceName} với:");
            sb.AppendLine("1. Phân tích các sao trong bản cung và ý nghĩa của chúng");
            sb.AppendLine("2. ⚠️ BẮT BUỘC: Ảnh hưởng của tam phương tứ chính (Đối cung, Tam hợp trái, Tam hợp phải)");
            if (isMenhOrThan)
            {
                sb.AppendLine("3. ⚠️ ĐẶC BIỆT (Mệnh/Thân): Tác động của nhị hợp (cung hợp khí)");
                sb.AppendLine("4. Ảnh hưởng từ cung liền kề");
                sb.AppendLine("5. Tổng hợp và kết luận về cung này");
                sb.AppendLine($"6. {palaceInfo.Requirement}");
            }
            else
            {
                sb.AppendLine("3. Ảnh hưởng từ cung liền kề");
                sb.AppendLine("4. Tổng hợp và kết luận về cung này");
                sb.AppendLine($"5. {palaceInfo.Requirement}");
            }
            sb.AppendLine();
            sb.AppendLine("Trả lời chi tiết, rõ ràng, dễ hiểu cho người không chuyên.");

            return sb.ToString();
        }

        private string GetSystemPrompt()
        {
            return @"Bạn là Thầy Tử Vi - một chuyên gia Tử Vi Đẩu Số hàng đầu với hơn 40 năm kinh nghiệm.

NGUYÊN TẮC LUẬN ĐOÁN TỬ VI - QUAN TRỌNG:

1. TAM PHƯƠNG TỨ CHÍNH (BẮT BUỘC cho MỌI cung):
   - Bản cung: Cung đang xét
   - Đối cung: Cung đối diện (cách 6 vị trí) - ảnh hưởng mạnh nhất
   - Tam hợp trái: Cung cách 4 vị trí
   - Tam hợp phải: Cung cách 8 vị trí
   ⚠️ CỰC KỲ QUAN TRỌNG: Các sao ở tam phương tứ chính CHIẾU vào bản cung, ảnh hưởng trực tiếp
   ⚠️ TUYỆT ĐỐI KHÔNG được luận riêng bản cung mà bỏ qua tam phương tứ chính!
   ⚠️ Khi nào xem hạn mới luận các sao lưu
   ⚠️ Phải phân tích đầy đủ 4 cung (bản cung + 3 cung tam phương) mới có luận đoán chính xác

2. NHỊ HỢP (ĐẶC BIỆT quan trọng cho cung Mệnh & Thân):
   - Cặp đôi địa chi hợp khí theo ngũ hành (Tý-Sửu, Dần-Hợi, Mão-Tuất, Thìn-Dậu, Tỵ-Thân, Ngọ-Mùi)
   - Ảnh hưởng đến bản chất sâu xa, vận khí tổng thể
   - Đối với Mệnh và Thân: PHẢI xét nhị hợp để hiểu đầy đủ cục diện
   - Đối với các cung khác: Không cần thiết phải xét nhị hợp

3. CUNG LIỀN KỀ:
   - 2 cung kề bên (trước và sau)
   - Ảnh hưởng đến hoàn cảnh, môi trường sống
   - Sao ở đây tác động gần gũi, thường trực

4. LUẬN ĐOÁN SAO:
   - Sao chính tinh: Xương cốt cung, quyết định bản chất
   - Sao phụ tinh: Tăng giảm cát hung, biến hóa ý nghĩa
   - Sao Hóa (Lộc, Quyền, Khoa, Kỵ): Rất quan trọng, thay đổi cục diện
   - Chú ý cả vòng thái tuế
   - Độ sáng sao: Sao sáng (Miếu, Vượng) mạnh, sao tối (Hãm, Bình) yếu

5. QUY TẮC TỔNG HỢP:
   - Cát + Cát = Đại cát
   - Cát + Hung = Bình hòa, xem sao nào mạnh hơn
   - Hung + Hung = Đại hung
   - Hóa Lộc, Hóa Quyền, Hóa Khoa giải hung
   - Hóa Kỵ làm hung thêm
   - Trường hợp đặc biệt, Tham Lang gặp Hỏa Tinh, Linh Tinh, Địa Không, Địa Kiếp là phú quý, Phá Quân gặp Không Kiếp là cách cục bạo phát, kể cả hội từ tam phương tứ chính.

HÃY LUẬN GIẢI:
- Chi tiết, cụ thể từng cung
- ⚠️ BẮT BUỘC: Phải liên hệ tam phương tứ chính rõ ràng cho MỌI cung
- ⚠️ ĐẶC BIỆT: Với cung Mệnh & Thân nhất định phải xét thêm nhị hợp và cung liền kề
- Dùng thuật ngữ Tử Vi chính xác, nghiên cứu thêm từ internet nếu cần
- Giải thích dễ hiểu, thiết thực
- Đưa ra lời khuyên cụ thể

Phong cách: Chuyên nghiệp nhưng gần gũi, tận tâm như thầy hướng dẫn trò.";
        }

        private string BuildPrompt(InterpretationRequest request)
        {
            var sb = new StringBuilder();

            // 1. Thông tin cơ bản
            sb.Append(BuildBasicInfo(request.Chart));
            
            // 2. Luận giải chi tiết TẤT CẢ 12 cung (bao gồm Mệnh và Thân)
            sb.Append(BuildAllPalacesPrompt(request.Chart));

          
       
            return sb.ToString();
        }

        private string BuildBasicInfo(TuViChart chart)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== THÔNG TIN LÁ SỐ TỬ VI ===");
            sb.AppendLine($"Ngày sinh: {chart.BirthDate:dd/MM/yyyy}");
            sb.AppendLine($"Giờ sinh: {chart.BirthTime}");
            sb.Append("Giới tính: ");
            sb.AppendLine(chart.IsMale ? "Nam" : "Nữ");
            sb.AppendLine($"Âm Dương: {chart.AmDuong}");
            sb.AppendLine($"Ngũ Hành Cục: {GetNguHanhCucName(chart.NguHanhCuc)}");
            sb.AppendLine($"Năm âm lịch: {chart.LunarYear}");
            
            var thanPalace = chart.PalaceStars.FirstOrDefault(p => p.PalaceId == chart.ThanPalace);
            if (thanPalace != null)
            {
                sb.AppendLine($"Cung Thân: {thanPalace.PalaceName} (Địa chi: {TuViChartAnalyzer.GetBranchName(chart.ThanPalace)})");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private string BuildAllPalacesPrompt(TuViChart chart)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== LUẬN GIẢI CHI TIẾT TẤT CẢ 12 CUNG ===");
            sb.AppendLine();

            // Luận tất cả 12 cung, Mệnh và Thân có thêm nhị hợp + cung liền kề
            foreach (var palace in chart.PalaceStars.OrderBy(p => p.PalaceId))
            {
                sb.Append(BuildPalacePrompt(palace, chart));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string BuildPalacePrompt(PalaceStar palace, TuViChart chart)
        {
            var sb = new StringBuilder();
            
            // Icon và ý nghĩa cho từng cung
            var palaceInfo = GetPalaceInfo(palace.PalaceName);
            sb.AppendLine($"▶ {palaceInfo.Icon} CUNG {palace.PalaceName.ToUpper()} - {palaceInfo.Meaning}");
            
            // Chỉ Mệnh và Thân mới cần xét nhị hợp
            bool includeNhiHop = palace.PalaceName == "Mệnh" || palace.PalaceId == chart.ThanPalace;
            sb.AppendLine(TuViChartAnalyzer.BuildPalaceAnalysis(palace, chart, includeNhiHop));
            
            // Yêu cầu luận giải cụ thể cho từng cung
            sb.AppendLine();
            sb.AppendLine($"📋 YÊU CẦU: {palaceInfo.Requirement}");
            sb.AppendLine();
            
            return sb.ToString();
        }

        private (string Icon, string Meaning, string Requirement) GetPalaceInfo(string palaceName)
        {
            return palaceName switch
            {
                "Mệnh" => ("🎯", "Bản ngã, tính cách, số mệnh", 
                    "Luận về tính cách, vận mệnh, hướng phát triển. Xét tam phương tứ chính, nhị hợp, cung liền kề."),
                
                "Phụ Mẫu" => ("👨‍👩‍👦", "Quan hệ với cha mẹ, học vấn, bề trên", 
                    "Luận về quan hệ cha mẹ, duyên phận với gia đình, học vấn, quan hệ với bề trên. Xét tam phương tứ chính."),
                
                "Phúc Đức" => ("🎭", "Tinh thần, tâm hồn, sở thích", 
                    "Luận về tinh thần, tư tưởng, sở thích, hưởng thụ, phúc đức tích lũy. Xét tam phương tứ chính."),
                
                "Điền Trạch" => ("🏠", "Nhà cửa, tài sản, môi trường sống", 
                    "Luận về nhà cửa, đất đai, tài sản bất động sản, môi trường sống. Xét tam phương tứ chính."),
                
                "Quan Lộc" => ("💼", "Sự nghiệp, công việc, địa vị", 
                    "Luận về sự nghiệp, công danh, nghề nghiệp phù hợp, địa vị xã hội. Xét tam phương tứ chính."),
                
                "Nô Bộc" => ("👥", "Bạn bè, nhân duyên, nhân viên", 
                    "Luận về bạn bè, đồng nghiệp, nhân duyên quý nhân, cấp dưới. Xét tam phương tứ chính."),
                
                "Thiên Di" => ("✈️", "Di chuyển, du lịch, môi trường bên ngoài", 
                    "Luận về di chuyển, đi xa, môi trường bên ngoài, hoạt động xã hội. Xét tam phương tứ chính."),
                
                "Tật Ách" => ("⚕️", "Sức khỏe, bệnh tật, tai nạn", 
                    "Luận về sức khỏe, bệnh tật, tai nạn, điều cần lưu ý. Xét tam phương tứ chính."),
                
                "Tài Bạch" => ("💰", "Tài chính, tiền bạc, của cải", 
                    "Luận về tài lộc, khả năng kiếm tiền, tiêu tiền, đầu tư. Xét tam phương tứ chính."),
                
                "Tử Tức" => ("👶", "Con cái, tình duyên với con", 
                    "Luận về con cái, duyên phận với con, khả năng sinh sản, giáo dục. Xét tam phương tứ chính."),
                
                "Phu Thê" => ("💑", "Tình duyên, hôn nhân, vợ chồng", 
                    "Luận về tình duyên, hôn nhân, đối tượng phù hợp, đời sống vợ chồng. Xét tam phương tứ chính."),
                
                "Huynh Đệ" => ("👫", "Anh em, bạn bè thân thiết", 
                    "Luận về anh em ruột, bạn bè thân, hỗ trợ lẫn nhau. Xét tam phương tứ chính."),
                
                _ => ("⭐", "Cung chưa xác định", "Luận giải theo tam phương tứ chính.")
            };
        }

       
        // Helper methods đã được di chuyển vào TuViChartAnalyzer

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

        private InterpretationResponse ParseAIResponse(string aiResponse, TuViChart chart)
        {
            var response = new InterpretationResponse
            {
                OverallInterpretation = aiResponse, // Giữ toàn bộ luận giải từ AI
                PalaceInterpretations = new(),
                KeyInsights = new(),
                Warnings = new(),
                Recommendations = new()
            };

            try
            {
                var sections = aiResponse.Split(new[] { "###", "##" }, StringSplitOptions.RemoveEmptyEntries);
                string currentPalace = "";
                var palaceContent = new StringBuilder();
                
                foreach (var section in sections)
                {
                    var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 0) continue;

                    var title = lines[0].Trim().ToLower();
                    var content = string.Join("\n", lines.Skip(1));
                    
                    // Parse từng cung
                    if (title.Contains("cung "))
                    {
                        // Lưu cung trước đó nếu có
                        if (!string.IsNullOrEmpty(currentPalace))
                        {
                            response.PalaceInterpretations.Add(new PalaceInterpretation
                            {
                                PalaceName = currentPalace,
                                Interpretation = palaceContent.ToString().Trim()
                            });
                            palaceContent.Clear();
                        }
                        
                        // Xác định tên cung mới
                        currentPalace = ExtractPalaceName(title);
                        palaceContent.Append(content);
                    }
                    else if (!string.IsNullOrEmpty(currentPalace))
                    {
                        // Nội dung tiếp theo của cung hiện tại
                        palaceContent.AppendLine();
                        palaceContent.Append(content);
                    }
                    else if (title.Contains("tổng quan"))
                    {
                        response.OverallInterpretation = content;
                    }
                    else if (title.Contains("chú ý") || title.Contains("điểm nổi bật") || title.Contains("đặc biệt"))
                    {
                        response.KeyInsights.AddRange(lines.Skip(1)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .Select(l => l.Trim('-', ' ', '*').Trim()));
                    }
                    else if (title.Contains("cảnh báo") || title.Contains("lưu ý"))
                    {
                        response.Warnings.AddRange(lines.Skip(1)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .Select(l => l.Trim('-', ' ', '*').Trim()));
                    }
                    else if (title.Contains("khuyến nghị") || title.Contains("lời khuyên"))
                    {
                        response.Recommendations.AddRange(lines.Skip(1)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .Select(l => l.Trim('-', ' ', '*').Trim()));
                    }
                }
                
                // Lưu cung cuối cùng
                if (!string.IsNullOrEmpty(currentPalace))
                {
                    response.PalaceInterpretations.Add(new PalaceInterpretation
                    {
                        PalaceName = currentPalace,
                        Interpretation = palaceContent.ToString().Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể parse chi tiết từ Gemini response");
            }

            return response;
        }

        private string ExtractPalaceName(string title)
        {
            var palaceNames = new[] { "Mệnh", "Phụ Mẫu", "Phúc Đức", "Điền Trạch", "Quan Lộc", 
                                      "Nô Bộc", "Thiên Di", "Tật Ách", "Tài Bạch", 
                                      "Tử Tức", "Phu Thê", "Huynh Đệ" };
            
            foreach (var name in palaceNames)
            {
                if (title.Contains(name.ToLower()))
                    return name;
            }
            
            return "Unknown";
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
