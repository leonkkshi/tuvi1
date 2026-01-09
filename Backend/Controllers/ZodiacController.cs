using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TuViController : ControllerBase
    {
        private readonly ITuViService _tuViService;
        private readonly IAIInterpretationService _aiInterpretationService;
        private readonly IAIRequestThrottler _throttler;
        private readonly IAsyncAIRequestQueue _asyncQueue;
        private readonly ILogger<TuViController> _logger;

        public TuViController(
            ITuViService tuViService, 
            IAIInterpretationService aiInterpretationService,
            IAIRequestThrottler throttler,
            IAsyncAIRequestQueue asyncQueue,
            ILogger<TuViController> logger)
        {
            _tuViService = tuViService;
            _aiInterpretationService = aiInterpretationService;
            _throttler = throttler;
            _asyncQueue = asyncQueue;
            _logger = logger;
        }

        [HttpGet("health")]
        public ActionResult<object> GetHealth()
        {
            var stats = _throttler.GetStats();
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var queueStatus = _asyncQueue.GetRequestStatus("__health__"); // Dummy call to check queue
            
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                memory = new
                {
                    currentMB = stats.CurrentMemoryMB,
                    workingSetMB = process.WorkingSet64 / 1024 / 1024,
                    privateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024
                },
                aiThrottler = new
                {
                    activeRequests = stats.ActiveRequests,
                    availableSlots = stats.AvailableSlots,
                    totalRequests = stats.TotalRequests,
                    rejectedRequests = stats.RejectedRequests,
                    utilizationPercent = stats.AvailableSlots > 0 ? 
                        Math.Round((double)stats.ActiveRequests / (stats.ActiveRequests + stats.AvailableSlots) * 100, 1) : 100,
                    isOverloaded = stats.AvailableSlots == 0
                },
                cpu = new
                {
                    totalProcessorTime = process.TotalProcessorTime.TotalSeconds,
                    threads = process.Threads.Count
                }
            });
        }

        [HttpPost("ai-interpret-async")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ai-sliding")]
        public ActionResult<object> AIInterpretChartAsync([FromBody] InterpretationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ApiKey))
                {
                    return BadRequest(new { error = "API key không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(request.Provider) || 
                    (request.Provider != "Gemini" && request.Provider != "OpenAI"))
                {
                    return BadRequest(new { error = "Provider phải là 'Gemini' hoặc 'OpenAI'" });
                }

                var requestId = _asyncQueue.EnqueueRequest(request);
                return Ok(new
                {
                    requestId,
                    status = "queued",
                    message = "Yêu cầu đã được đưa vào hàng đợi xử lý",
                    statusEndpoint = $"/api/tuvi/ai-status/{requestId}",
                    resultEndpoint = $"/api/tuvi/ai-result/{requestId}"
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("quá tải"))
            {
                _logger.LogWarning("AI request rejected due to throttling: {Message}", ex.Message);
                return StatusCode(503, new { 
                    error = "Hệ thống đang quá tải", 
                    message = "Quá nhiều yêu cầu đồng thời. Vui lòng thử lại sau vài phút.",
                    retryAfter = 60,
                    queueStats = _asyncQueue.GetQueueStats()
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("hàng đợi"))
            {
                _logger.LogWarning("Queue is full: {Message}", ex.Message);
                return StatusCode(503, new { 
                    error = "Hàng đợi đã đầy", 
                    message = "Hệ thống đang xử lý quá nhiều yêu cầu. Vui lòng thử lại sau 2-3 phút.",
                    retryAfter = 120,
                    queueStats = _asyncQueue.GetQueueStats()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi xếp hàng yêu cầu", details = ex.Message });
            }
        }

        [HttpPost("ai-interpret-palace-async")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ai-sliding")]
        public ActionResult<object> AIInterpretPalaceAsync([FromBody] PalaceInterpretationRequest request)
        {
            try
            {
                if (request.Chart == null || string.IsNullOrWhiteSpace(request.PalaceName))
                {
                    return BadRequest(new { error = "Thông tin lá số hoặc tên cung không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(request.ApiKey))
                {
                    return BadRequest(new { error = "API key không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(request.Provider) || 
                    (request.Provider != "Gemini" && request.Provider != "OpenAI"))
                {
                    return BadRequest(new { error = "Provider phải là 'Gemini' hoặc 'OpenAI'" });
                }

                var requestId = _asyncQueue.EnqueuePalaceRequest(request.Chart, request.PalaceName, request.ApiKey, request.Provider);
                return Ok(new
                {
                    requestId,
                    palaceName = request.PalaceName,
                    status = "queued",
                    message = "Yêu cầu đã được đưa vào hàng đợi xử lý",
                    statusEndpoint = $"/api/tuvi/ai-status/{requestId}",
                    resultEndpoint = $"/api/tuvi/ai-palace-result/{requestId}"
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("quá tải") || ex.Message.Contains("queue"))
            {
                _logger.LogWarning("Palace request rejected: {Message}", ex.Message);
                return StatusCode(503, new { 
                    error = "Hệ thống đang quá tải", 
                    message = "Quá nhiều yêu cầu đồng thời. Vui lòng thử lại sau vài phút.",
                    retryAfter = 60,
                    queueStats = _asyncQueue.GetQueueStats()
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("hàng đợi"))
            {
                _logger.LogWarning("Queue is full for palace request: {Message}", ex.Message);
                return StatusCode(503, new { 
                    error = "Hàng đợi đã đầy", 
                    message = "Hệ thống đang xử lý quá nhiều yêu cầu. Vui lòng thử lại sau 2-3 phút.",
                    retryAfter = 120,
                    queueStats = _asyncQueue.GetQueueStats()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing palace request");
                return StatusCode(500, new { error = "Lỗi khi xếp hàng yêu cầu", details = ex.Message });
            }
        }

        [HttpGet("ai-status/{requestId}")]
        public ActionResult<AIRequestStatus> GetAIRequestStatus(string requestId)
        {
            try
            {
                var status = _asyncQueue.GetRequestStatus(requestId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi lấy trạng thái", details = ex.Message });
            }
        }

        [HttpGet("ai-result/{requestId}")]
        public ActionResult<object> GetAIResult(string requestId)
        {
            try
            {
                var status = _asyncQueue.GetRequestStatus(requestId);
                
                if (status.Status == "not_found")
                {
                    return NotFound(new { error = "Request ID không tồn tại hoặc đã hết hạn" });
                }
                
                if (status.Status == "failed")
                {
                    return StatusCode(500, new { error = "Xử lý thất bại", details = status.Error });
                }
                
                if (status.Status != "completed")
                {
                    return Ok(new
                    {
                        status = status.Status,
                        message = "Yêu cầu đang được xử lý",
                        queuedAt = status.QueuedAt,
                        startedAt = status.StartedAt
                    });
                }

                var result = _asyncQueue.GetResult(requestId);
                if (result is null)
                {
                    return NotFound(new { error = "Không tìm thấy kết quả" });
                }

                return Ok(new
                {
                    status = "completed",
                    result,
                    completedAt = status.CompletedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi lấy kết quả", details = ex.Message });
            }
        }

        [HttpGet("ai-palace-result/{requestId}")]
        public ActionResult<object> GetAIPalaceResult(string requestId)
        {
            try
            {
                var status = _asyncQueue.GetRequestStatus(requestId);
                
                if (status.Status == "not_found")
                {
                    return NotFound(new { error = "Request ID không tồn tại hoặc đã hết hạn" });
                }
                
                if (status.Status == "failed")
                {
                    return StatusCode(500, new { error = "Xử lý thất bại", details = status.Error });
                }
                
                if (status.Status != "completed")
                {
                    return Ok(new
                    {
                        status = status.Status,
                        palaceName = status.PalaceName,
                        message = "Yêu cầu đang được xử lý",
                        queuedAt = status.QueuedAt,
                        startedAt = status.StartedAt
                    });
                }

                var result = _asyncQueue.GetPalaceResult(requestId);
                if (result is null)
                {
                    return NotFound(new { error = "Không tìm thấy kết quả" });
                }

                return Ok(new
                {
                    status = "completed",
                    result,
                    completedAt = status.CompletedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi lấy kết quả", details = ex.Message });
            }
        }

        [HttpGet("palaces")]
        public ActionResult<List<Palace>> GetAllPalaces()
        {
            return Ok(_tuViService.GetAllPalaces());
        }

        [HttpGet("stars")]
        public ActionResult<List<Star>> GetAllStars()
        {
            return Ok(_tuViService.GetAllStars());
        }

        [HttpPost("generate-chart")]
        public ActionResult<TuViChart> GenerateChart([FromBody] ChartRequest request)
        {
            var chart = _tuViService.GenerateChart(request);
            return Ok(chart);
        }

        [HttpPost("interpret")]
        public ActionResult<string> InterpretPalace([FromBody] InterpretRequest request)
        {
            var interpretation = _tuViService.InterpretPalace(request.PalaceId, request.Stars);
            return Ok(new { interpretation });
        }

        [HttpPost("ai-interpret")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ai-limit")]
        public async Task<ActionResult<InterpretationResponse>> AIInterpretChart([FromBody] InterpretationRequest request)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                var interpretation = await _aiInterpretationService.InterpretChartAsync(request, request.ApiKey, request.Provider);
                
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                // Log performance để monitor
                Console.WriteLine($"[AI Request] Duration: {duration}ms, Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                
                return Ok(interpretation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi luận giải lá số", details = ex.Message });
            }
        }

        [HttpPost("ai-interpret-palace")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ai-limit")]
        public async Task<ActionResult<PalaceInterpretationResult>> AIInterpretPalace([FromBody] PalaceInterpretationRequest request)
        {
            try
            {
                if (request.Chart == null || string.IsNullOrWhiteSpace(request.PalaceName))
                {
                    return BadRequest(new { error = "Thông tin lá số hoặc tên cung không hợp lệ" });
                }

                var startTime = DateTime.UtcNow;
                
                Console.WriteLine($"[AI Palace Request] Bắt đầu luận giải cung: {request.PalaceName}");
                
                // Sử dụng method riêng để luận giải một cung với đầy đủ tam phương tứ chính
                // Đối với Mệnh và Thân thì có thêm nhị hợp
                var interpretation = await _aiInterpretationService.InterpretSinglePalaceAsync(
                    request.Chart, 
                    request.PalaceName,
                    request.ApiKey,
                    request.Provider);
                
                var result = new PalaceInterpretationResult
                {
                    PalaceName = request.PalaceName,
                    Interpretation = interpretation,
                    InfluencingStars = new List<string>() // TODO: Parse từ response nếu cần
                };
                
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                Console.WriteLine($"[AI Palace Request] Hoàn thành cung {request.PalaceName}, Duration: {duration}ms");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI Palace Request] Lỗi: {ex.Message}");
                return StatusCode(500, new { error = $"Lỗi khi luận giải cung {request.PalaceName}", details = ex.Message });
            }
        }

        [HttpGet("test-lunar/{day}/{month}/{year}")]
        public ActionResult<object> TestLunarConversion(int day, int month, int year)
        {
            var lunar = _tuViService.TestConvertSolar2Lunar(day, month, year);
            return Ok(new
            {
                solar = $"{day}/{month}/{year}",
                lunar = $"{lunar[0]}/{lunar[1]}/{lunar[2]}",
                leapMonth = lunar[3]
            });
        }
        
        [HttpGet("solar-to-lunar")]
        public ActionResult<object> SolarToLunar([FromQuery] int year, [FromQuery] int month, [FromQuery] int day)
        {
            try
            {
                var lunar = _tuViService.TestConvertSolar2Lunar(day, month, year);
                return Ok(new
                {
                    solarDay = day,
                    solarMonth = month,
                    solarYear = year,
                    lunarDay = lunar[0],
                    lunarMonth = lunar[1],
                    lunarYear = lunar[2],
                    leapMonth = lunar[3]
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Không thể chuyển đổi ngày tháng", details = ex.Message });
            }
        }
    }

    public class InterpretRequest
    {
        public int PalaceId { get; set; }
        public List<StarInPalace> Stars { get; set; } = new();
    }

    public class PalaceInterpretationRequest
    {
        public TuViChart Chart { get; set; } = null!;
        public string PalaceName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Provider { get; set; } = "Gemini"; // Gemini or OpenAI
    }

    public class PalaceInterpretationResult
    {
        public string PalaceName { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty;
        public List<string> InfluencingStars { get; set; } = new();
    }
}
