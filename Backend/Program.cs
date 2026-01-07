using Backend.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register Tử Vi service
builder.Services.AddSingleton<ITuViService, TuViService>();

// Add Memory Cache để cache AI responses (giảm calls và memory)
builder.Services.AddMemoryCache();

// Configure Kestrel limits
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 5000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

// Add Rate Limiting để bảo vệ AI endpoints
builder.Services.AddRateLimiter(options =>
{
    // Policy cho AI endpoints: tối đa 20 requests/phút mỗi IP
    options.AddFixedWindowLimiter("ai-limit", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10; // Queue thêm 10 requests
    });
    
    // Policy chung: 100 requests/phút
    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 20;
    });
    
    options.RejectionStatusCode = 429; // Too Many Requests
});

// Register AI Interpretation service - chọn provider dựa trên config
builder.Services.AddHttpClient().ConfigureHttpClientDefaults(http =>
{
    // Timeout để tránh requests bị treo lâu chiếm memory
    http.ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(120); // 2 phút timeout
    });
});

// Register AI Request Throttler để giới hạn concurrent AI calls
builder.Services.AddSingleton<IAIRequestThrottler, AIRequestThrottler>();

// Register Async AI Request Queue để xử lý background jobs
builder.Services.AddSingleton<IAsyncAIRequestQueue, AsyncAIRequestQueue>();

var aiProvider = builder.Configuration["AI:Provider"] ?? "OpenAI";
if (aiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IAIInterpretationService, GeminiInterpretationService>();
}
else
{
    builder.Services.AddScoped<IAIInterpretationService, OpenAIInterpretationService>();
}

// Add CORS for Angular frontend and network access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    
    options.AddPolicy("AllowNgrok",
        policy =>
        {
            policy.SetIsOriginAllowed(origin => 
                {
                    if (string.IsNullOrEmpty(origin)) return false;
                    
                    return origin.StartsWith("http://localhost") || 
                           origin.StartsWith("http://192.168.") ||
                           origin.StartsWith("http://10.") ||
                           (origin.StartsWith("https://") && origin.Contains(".ngrok")) ||
                           (origin.StartsWith("https://") && origin.Contains(".ngrok-free.app")) ||
                           origin == "https://tuvi1.vercel.app" ||
                           (origin.StartsWith("https://tuvi1") && origin.Contains(".vercel.app")) ||
                           // Facebook domains
                           origin.Contains("facebook.com") ||
                           origin.Contains("fb.com") ||
                           origin.Contains("fbcdn.net") ||
                           origin.Contains("messenger.com") ||
                           origin.Contains("m.me") ||
                           // Zalo domains
                           origin.Contains("zalo.me") ||
                           origin.Contains("zaloapp.com") ||
                           origin.Contains("zdn.vn");
                })
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseCors("AllowNgrok");
app.UseRateLimiter(); // Bật rate limiting
app.UseAuthorization();
app.MapControllers();

app.Run();
