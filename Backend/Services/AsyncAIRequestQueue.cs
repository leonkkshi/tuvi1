using System.Collections.Concurrent;
using Backend.Models;
using Backend.Controllers;

namespace Backend.Services
{
    /// <summary>
    /// Service xử lý AI requests async với background queue
    /// </summary>
    public interface IAsyncAIRequestQueue
    {
        string EnqueueRequest(InterpretationRequest request);
        string EnqueuePalaceRequest(TuViChart chart, string palaceName);
        AIRequestStatus GetRequestStatus(string requestId);
        InterpretationResponse? GetResult(string requestId);
        PalaceInterpretationResult? GetPalaceResult(string requestId);
    }

    public class AsyncAIRequestQueue : IAsyncAIRequestQueue, IDisposable
    {
        private readonly ConcurrentDictionary<string, AIRequestStatus> _requestStatuses;
        private readonly ConcurrentDictionary<string, InterpretationResponse> _chartResults;
        private readonly ConcurrentDictionary<string, PalaceInterpretationResult> _palaceResults;
        private readonly ConcurrentQueue<(string requestId, InterpretationRequest request)> _chartQueue;
        private readonly ConcurrentQueue<(string requestId, TuViChart chart, string palaceName)> _palaceQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<AsyncAIRequestQueue> _logger;
        private readonly IAIRequestThrottler _throttler;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processingTask;

        public AsyncAIRequestQueue(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<AsyncAIRequestQueue> logger,
            IAIRequestThrottler throttler)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _throttler = throttler;
            _requestStatuses = new ConcurrentDictionary<string, AIRequestStatus>();
            _chartResults = new ConcurrentDictionary<string, InterpretationResponse>();
            _palaceResults = new ConcurrentDictionary<string, PalaceInterpretationResult>();
            _chartQueue = new ConcurrentQueue<(string, InterpretationRequest)>();
            _palaceQueue = new ConcurrentQueue<(string, TuViChart, string)>();
            _cancellationTokenSource = new CancellationTokenSource();

            // Start background processing
            _processingTask = Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token));
            
            _logger.LogInformation("AsyncAIRequestQueue initialized and background processing started");
        }

        public string EnqueueRequest(InterpretationRequest request)
        {
            var requestId = Guid.NewGuid().ToString();
            _requestStatuses[requestId] = new AIRequestStatus
            {
                RequestId = requestId,
                Status = "queued",
                QueuedAt = DateTime.UtcNow,
                RequestType = "chart"
            };
            
            _chartQueue.Enqueue((requestId, request));
            _logger.LogInformation("Chart request {RequestId} queued. Queue size: {QueueSize}", 
                requestId, _chartQueue.Count + _palaceQueue.Count);
            
            return requestId;
        }

        public string EnqueuePalaceRequest(TuViChart chart, string palaceName)
        {
            var requestId = Guid.NewGuid().ToString();
            _requestStatuses[requestId] = new AIRequestStatus
            {
                RequestId = requestId,
                Status = "queued",
                QueuedAt = DateTime.UtcNow,
                RequestType = "palace",
                PalaceName = palaceName
            };
            
            _palaceQueue.Enqueue((requestId, chart, palaceName));
            _logger.LogInformation("Palace request {RequestId} for {PalaceName} queued. Queue size: {QueueSize}", 
                requestId, palaceName, _chartQueue.Count + _palaceQueue.Count);
            
            return requestId;
        }

        public AIRequestStatus GetRequestStatus(string requestId)
        {
            if (_requestStatuses.TryGetValue(requestId, out var status))
            {
                return status;
            }
            
            return new AIRequestStatus
            {
                RequestId = requestId,
                Status = "not_found",
                Error = "Request ID không tồn tại hoặc đã hết hạn"
            };
        }

        public InterpretationResponse? GetResult(string requestId)
        {
            _chartResults.TryGetValue(requestId, out var result);
            return result;
        }

        public PalaceInterpretationResult? GetPalaceResult(string requestId)
        {
            _palaceResults.TryGetValue(requestId, out var result);
            return result;
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background queue processing started");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Process chart requests
                    if (_chartQueue.TryDequeue(out var chartItem))
                    {
                        await ProcessChartRequestAsync(chartItem.requestId, chartItem.request);
                    }
                    // Process palace requests
                    else if (_palaceQueue.TryDequeue(out var palaceItem))
                    {
                        await ProcessPalaceRequestAsync(palaceItem.requestId, palaceItem.chart, palaceItem.palaceName);
                    }
                    else
                    {
                        // No items in queue, wait a bit
                        await Task.Delay(500, cancellationToken);
                    }

                    // Cleanup old completed requests (older than 30 minutes)
                    CleanupOldRequests();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Queue processing cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in queue processing loop");
                    await Task.Delay(1000, cancellationToken); // Wait before retry
                }
            }
            
            _logger.LogInformation("Background queue processing stopped");
        }

        private async Task ProcessChartRequestAsync(string requestId, InterpretationRequest request)
        {
            try
            {
                _logger.LogInformation("Processing chart request {RequestId}", requestId);
                
                // Update status to processing
                if (_requestStatuses.TryGetValue(requestId, out var status))
                {
                    status.Status = "processing";
                    status.StartedAt = DateTime.UtcNow;
                }

                // Create a new scope to resolve scoped services
                using var scope = _serviceScopeFactory.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInterpretationService>();

                // Execute AI request with throttling
                var result = await _throttler.ExecuteAsync(async () =>
                {
                    return await aiService.InterpretChartAsync(request);
                });

                // Store result
                _chartResults[requestId] = result;
                
                // Update status to completed
                if (_requestStatuses.TryGetValue(requestId, out status))
                {
                    status.Status = "completed";
                    status.CompletedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Chart request {RequestId} completed successfully", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chart request {RequestId}", requestId);
                
                // Update status to failed
                if (_requestStatuses.TryGetValue(requestId, out var status))
                {
                    status.Status = "failed";
                    status.Error = ex.Message;
                    status.CompletedAt = DateTime.UtcNow;
                }
            }
        }

        private async Task ProcessPalaceRequestAsync(string requestId, TuViChart chart, string palaceName)
        {
            try
            {
                _logger.LogInformation("Processing palace request {RequestId} for {PalaceName}", requestId, palaceName);
                
                // Update status to processing
                if (_requestStatuses.TryGetValue(requestId, out var status))
                {
                    status.Status = "processing";
                    status.StartedAt = DateTime.UtcNow;
                }

                // Create a new scope to resolve scoped services
                using var scope = _serviceScopeFactory.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInterpretationService>();

                // Execute AI request with throttling
                var interpretation = await _throttler.ExecuteAsync(async () =>
                {
                    return await aiService.InterpretSinglePalaceAsync(chart, palaceName);
                });

                // Store result
                _palaceResults[requestId] = new PalaceInterpretationResult
                {
                    PalaceName = palaceName,
                    Interpretation = interpretation,
                    InfluencingStars = new List<string>()
                };
                
                // Update status to completed
                if (_requestStatuses.TryGetValue(requestId, out status))
                {
                    status.Status = "completed";
                    status.CompletedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Palace request {RequestId} for {PalaceName} completed successfully", 
                    requestId, palaceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing palace request {RequestId} for {PalaceName}", 
                    requestId, palaceName);
                
                // Update status to failed
                if (_requestStatuses.TryGetValue(requestId, out var status))
                {
                    status.Status = "failed";
                    status.Error = ex.Message;
                    status.CompletedAt = DateTime.UtcNow;
                }
            }
        }

        private void CleanupOldRequests()
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-30);
            var oldRequests = _requestStatuses
                .Where(kvp => kvp.Value.CompletedAt.HasValue && kvp.Value.CompletedAt.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var requestId in oldRequests)
            {
                _requestStatuses.TryRemove(requestId, out _);
                _chartResults.TryRemove(requestId, out _);
                _palaceResults.TryRemove(requestId, out _);
            }

            if (oldRequests.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old requests", oldRequests.Count);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                _processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for processing task to complete");
            }
            
            _cancellationTokenSource.Dispose();
            _logger.LogInformation("AsyncAIRequestQueue disposed");
        }
    }

    public class AIRequestStatus
    {
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = "queued"; // queued, processing, completed, failed, not_found
        public string RequestType { get; set; } = "chart"; // chart or palace
        public string? PalaceName { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
        
        public int? QueuePosition { get; set; }
        public double? EstimatedWaitSeconds { get; set; }
    }
}
