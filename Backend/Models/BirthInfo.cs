namespace Backend.Models
{
    public class BirthInfo
    {
        public DateTime BirthDate { get; set; }
        public TimeSpan BirthTime { get; set; }
        public bool IsMale { get; set; }
    }

    public class ChartRequest
    {
        public string? FullName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public bool IsMale { get; set; }
        public bool IsLunar { get; set; } = true; // Mặc định là âm lịch
    }
}
