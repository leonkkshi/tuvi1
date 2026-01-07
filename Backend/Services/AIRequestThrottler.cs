using System.Collections.Concurrent;

namespace Backend.Services
{
    /// <summary>
    /// Service để giới hạn số lượng concurrent AI requests để bảo vệ memory
    /// </summary>
    public interface IAIRequestThrottler
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> aiCall, CancellationToken cancellationToken = default);
        AIThrottlerStats GetStats();
    }

    public class AIRequestThrottler : IAIRequestThrottler
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<AIRequestThrottler> _logger;
        private readonly ConcurrentDictionary<string, DateTime> _activeRequests;
        private int _totalRequests = 0;
        private int _rejectedRequests = 0;

        public AIRequestThrottler(IConfiguration configuration, ILogger<AIRequestThrottler> logger)
        {
            // Giới hạn số lượng concurrent AI requests (default: 3)
            var maxConcurrent = configuration.GetValue<int>("AI:MaxConcurrentRequests", 3);
            _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            _logger = logger;
            _activeRequests = new ConcurrentDictionary<string, DateTime>();
            
            _logger.LogInformation("AIRequestThrottler initialized with max concurrent requests: {MaxConcurrent}", maxConcurrent);
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> aiCall, CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid().ToString();
            Interlocked.Increment(ref _totalRequests);

            // Timeout nếu chờ quá 60 giây
            var waitTimeout = TimeSpan.FromSeconds(60);
            var waitSuccess = await _semaphore.WaitAsync(waitTimeout, cancellationToken);

            if (!waitSuccess)
            {
                Interlocked.Increment(ref _rejectedRequests);
                _logger.LogWarning("Request {RequestId} rejected: timeout waiting for semaphore", requestId);
                throw new InvalidOperationException("Hệ thống đang quá tải, vui lòng thử lại sau");
            }

            _activeRequests.TryAdd(requestId, DateTime.UtcNow);
            _logger.LogInformation("Request {RequestId} started. Active requests: {ActiveCount}", 
                requestId, _activeRequests.Count);

            try
            {
                var result = await aiCall();
                return result;
            }
            finally
            {
                _activeRequests.TryRemove(requestId, out _);
                _semaphore.Release();
                
                _logger.LogInformation("Request {RequestId} completed. Active requests: {ActiveCount}", 
                    requestId, _activeRequests.Count);
                
                // Trigger garbage collection nếu memory cao
                var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
                if (memoryMB > 1500) // Nếu > 1.5GB
                {
                    _logger.LogWarning("High memory usage: {MemoryMB}MB. Triggering GC", memoryMB);
                    GC.Collect(2, GCCollectionMode.Optimized, false);
                }
            }
        }

        public AIThrottlerStats GetStats()
        {
            return new AIThrottlerStats
            {
                ActiveRequests = _activeRequests.Count,
                TotalRequests = _totalRequests,
                RejectedRequests = _rejectedRequests,
                CurrentMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
                AvailableSlots = _semaphore.CurrentCount
            };
        }
    }

    public class AIThrottlerStats
    {
        public int ActiveRequests { get; set; }
        public int TotalRequests { get; set; }
        public int RejectedRequests { get; set; }
        public long CurrentMemoryMB { get; set; }
        public int AvailableSlots { get; set; }
    }
}
