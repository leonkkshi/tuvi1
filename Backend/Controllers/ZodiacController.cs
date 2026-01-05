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

        public TuViController(
            ITuViService tuViService, 
            IAIInterpretationService aiInterpretationService,
            IAIRequestThrottler throttler)
        {
            _tuViService = tuViService;
            _aiInterpretationService = aiInterpretationService;
            _throttler = throttler;
        }

        [HttpGet("health")]
        public ActionResult<object> GetHealth()
        {
            var stats = _throttler.GetStats();
            var process = System.Diagnostics.Process.GetCurrentProcess();
            
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
                    rejectedRequests = stats.RejectedRequests
                },
                cpu = new
                {
                    totalProcessorTime = process.TotalProcessorTime.TotalSeconds,
                    threads = process.Threads.Count
                }
            });
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
                
                var interpretation = await _aiInterpretationService.InterpretChartAsync(request);
                
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
                    request.PalaceName);
                
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
    }

    public class PalaceInterpretationResult
    {
        public string PalaceName { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty;
        public List<string> InfluencingStars { get; set; } = new();
    }
}
