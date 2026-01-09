using Backend.Models;

namespace Backend.Services
{
    public interface IAIInterpretationService
    {
        Task<InterpretationResponse> InterpretChartAsync(InterpretationRequest request, string apiKey, string provider);
        Task<string> InterpretSinglePalaceAsync(TuViChart chart, string palaceName, string apiKey, string provider);
    }
}
