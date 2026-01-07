using Backend.Models;
using Backend.Services.StarPlacement;
using Backend.Services.Utilities;

namespace Backend.Services;

public class TuViService : ITuViService
{
    private readonly List<Palace> _palaces;
    private readonly List<Star> _stars;
    private readonly Dictionary<int, Star> _starById;

    // Star placement services
    private readonly MainStarPlacementService _mainStarService = new();
    private readonly SecondaryStarPlacementService _secondaryStarService = new();
    private readonly TruongSinhStarPlacementService _truongSinhStarService = new();
    private readonly ThaiTueStarPlacementService _thaiTueStarService = new();
    private readonly TuHoaStarPlacementService _tuHoaStarService = new();

    // Utility services
    private readonly LunarCalendarService _lunarCalendarService = new();
    private readonly PalaceCalculationService _palaceCalculationService = new();
    private readonly BranchStemService _branchStemService = new();
    private readonly CatalogService _catalogService = new();

    public TuViService()
    {
        _palaces = _catalogService.BuildPalaceCatalog();
        _stars = _catalogService.BuildStarCatalog();
        _starById = _stars.ToDictionary(s => s.Id, s => s);
    }

    public List<Palace> GetAllPalaces() => _palaces;

    public List<Star> GetAllStars() => _stars;

    public TuViChart GenerateChart(ChartRequest request)
    {
        // Resolve lunar birth date (for calculations)
        int lunarYear;
        int lunarMonth;
        int lunarDay;

        DateTime birthDate;
        if (request.IsLunar)
        {
            lunarYear = request.Year;
            lunarMonth = request.Month;
            lunarDay = request.Day;
            birthDate = new DateTime(request.Year, request.Month, request.Day);
        }
        else
        {
            birthDate = new DateTime(request.Year, request.Month, request.Day);
            var lunar = _lunarCalendarService.ConvertSolar2Lunar(request.Day, request.Month, request.Year);
            lunarDay = lunar[0];
            lunarMonth = lunar[1];
            lunarYear = lunar[2];
        }

        var birthTime = new TimeSpan(request.Hour, request.Minute, 0);

        int hourBranch = _palaceCalculationService.GetHourBranch(request.Hour);
        int menhPalace = _palaceCalculationService.CalculateMenhPalace(lunarMonth, hourBranch);

        // Tính Cung Thân: dựa vào giờ sinh cố định tại một trong 6 cung (tương đối từ Mệnh)
        int thanPalaceOffset = hourBranch switch
        {
            1 or 7 => 0,      // Giờ Tý (1), Ngọ (7) -> Cung Mệnh (offset = 0)
            4 or 10 => 6,     // Giờ Mão (4), Dậu (10) -> Cung Thiên Di (offset = 6)
            3 or 9 => 4,      // Giờ Dần (3), Thân (9) -> Cung Quan Lộc (offset = 4)
            5 or 11 => 8,     // Giờ Thìn (5), Tuất (11) -> Cung Tài Bạch (offset = 8)
            6 or 12 => 10,    // Giờ Tỵ (6), Hợi (12) -> Cung Phu Thê (offset = 10)
            2 or 8 => 2,      // Giờ Sửu (2), Mùi (8) -> Cung Phúc Đức (offset = 2)
            _ => 0
        };
        int thanPalace = _palaceCalculationService.NormalizePos(menhPalace + thanPalaceOffset);

        var palaceNameByPos = _palaceCalculationService.BuildPalaceNameByPosition(menhPalace);
        int nobocPalace = palaceNameByPos.First(kvp => kvp.Value == "Nô Bộc").Key;
        int tatAchPalace = palaceNameByPos.First(kvp => kvp.Value == "Tật Ách").Key;

        string amDuong = _palaceCalculationService.CalculateAmDuong(lunarYear, request.IsMale);
        int nguHanhCuc = _palaceCalculationService.CalculateNguHanhCuc(lunarYear, menhPalace);

        var ctx = new StarPlacementContext
        {
            Year = lunarYear,
            Month = lunarMonth,
            Day = lunarDay,
            Hour = request.Hour,
            IsMale = request.IsMale,
            MenhPalace = menhPalace,
            ThanPalace = thanPalace,
            NobocPalace = nobocPalace,
            TatAchPalace = tatAchPalace,
            AmDuong = amDuong,
            NguHanhCuc = nguHanhCuc,
        };

        var mainStars = _mainStarService.PlaceStars(ctx);
        ctx.MainStarPositions = mainStars;

        var secondaryStars = _secondaryStarService.PlaceStars(ctx);
        var truongSinhStars = _truongSinhStarService.PlaceStars(ctx);
        var thaiTueStars = _thaiTueStarService.PlaceStars(ctx);
        var tuHoaStars = _tuHoaStarService.PlaceStars(ctx);

        // Tính Triệt và Tuần
        var trietBetween = CalculateTriet(lunarYear);
        var tuanPositions = CalculateTuan(lunarYear);
        
        // Parse Triệt và Tuần để xác định các cung
        var trietPalaces = ParseBranchPositions(trietBetween);
        var tuanPalaces = ParseBranchPositions(tuanPositions);

        var palaceStars = new List<PalaceStar>();
        for (int pos = 1; pos <= 12; pos++)
        {
            var starIds = new List<int>();
            AddUnique(starIds, mainStars[pos]);
            AddUnique(starIds, secondaryStars[pos]);
            AddUnique(starIds, thaiTueStars[pos]);
            AddUnique(starIds, tuHoaStars[pos]);
            AddUnique(starIds, truongSinhStars[pos]);

            var starsInPalace = starIds
                .Select(id => ToStarInPalace(id, pos))
                .Where(s => s != null)
                .Cast<StarInPalace>()
                .ToList();

            palaceStars.Add(new PalaceStar
            {
                PalaceId = pos,
                PalaceName = palaceNameByPos[pos],
                Stars = starsInPalace,
                HasTriet = trietPalaces.Contains(pos),
                HasTuan = tuanPalaces.Contains(pos)
            });
        }

        // Tính Đại Vận
        var daiVan = CalculateDaiVan(menhPalace, nguHanhCuc, amDuong);

        // Tính Tiểu Hạn
        var tieuHan = CalculateTieuHan(lunarYear, request.IsMale);

        // Tính Nguyệt Hạn (cho năm xem hạn, mặc định là năm hiện tại)
        int viewYear = request.ViewYear ?? DateTime.Now.Year;
        var nguyetHan = CalculateNguyetHan(tieuHan, viewYear, lunarMonth, hourBranch);

        return new TuViChart
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName ?? string.Empty,
            BirthDate = birthDate,
            BirthTime = birthTime,
            IsMale = request.IsMale,
            LunarYear = _branchStemService.BuildCanChiYear(lunarYear),
            LunarMonth = lunarMonth.ToString(),
            LunarDay = lunarDay.ToString(),
            NguHanhCuc = nguHanhCuc,
            AmDuong = amDuong,
            ThanPalace = thanPalace,
            PalaceStars = palaceStars,
            DaiVan = daiVan,
            TieuHan = tieuHan,
            NguyetHan = nguyetHan,
            TrietBetween = trietBetween, 
            TuanPositions = tuanPositions
        };
    }

    public string InterpretPalace(int palaceId, List<StarInPalace> stars)
    {
        var palace = _palaces.FirstOrDefault(p => p.Id == palaceId);
        if (palace == null) return "Không tìm thấy cung";

        var result = $"Cung {palace.Name}: {palace.Description}\n";
        result += $"Các sao: {string.Join(", ", stars.Select(s => s.StarName))}\n";
        return result;
    }

    private StarInPalace? ToStarInPalace(int starId, int palacePosition)
    {
        if (!_starById.TryGetValue(starId, out var star))
        {
            return new StarInPalace
            {
                StarId = starId,
                StarName = $"Sao#{starId}",
                Brightness = 0,
                Element = string.Empty,
                Nature = string.Empty,
                Type = string.Empty
            };
        }

        // Điều chỉnh độ sáng theo cung
        int adjustedBrightness = _catalogService.GetAdjustedBrightness(star.Name, palacePosition, star.Brightness);

        return new StarInPalace
        {
            StarId = star.Id,
            StarName = star.Name,
            Brightness = adjustedBrightness,
            Element = star.Element,
            Nature = star.Nature,
            Type = star.Type
        };
    }

    private static void AddUnique(List<int> target, List<int> source)
    {
        foreach (var id in source)
        {
            if (!target.Contains(id)) target.Add(id);
        }
    }

    public int[] TestConvertSolar2Lunar(int day, int month, int year)
    {
        return _lunarCalendarService.ConvertSolar2Lunar(day, month, year);
    }

    // Tính Triệt dựa vào Thiên Can năm sinh
    private string CalculateTriet(int lunarYear)
    {
        int yearCan = (lunarYear - 3) % 10;
        if (yearCan <= 0) yearCan += 10;

        string[] branchNames = ["Tý", "Sửu", "Dần", "Mão", "Thìn", "Tị", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi"];

        // Triệt dựa vào Thiên Can năm sinh
        return yearCan switch
        {
            1 or 6 => "Thân-Dậu",      // Giáp hoặc Kỷ
            2 or 7 => "Ngọ-Mùi",       // Ất hoặc Canh
            3 or 8 => "Thìn-Tị",       // Bính hoặc Tân
            4 or 9 => "Dần-Mão",       // Đinh hoặc Nhâm
            5 or 10 => "Tý-Sửu",       // Mậu hoặc Quý
            _ => ""
        };
    }

    // Tính Tuần dựa vào Thiên Can năm sinh
    private string CalculateTuan(int lunarYear)
    {
        int yearCan = (lunarYear - 3) % 10;
        if (yearCan <= 0) yearCan += 10;

        int yearBranch = (lunarYear - 3) % 12;
        if (yearBranch <= 0) yearBranch += 12;

        string[] branchNames = ["Tý", "Sửu", "Dần", "Mão", "Thìn", "Tị", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi"];

        // Từ cung địa chi năm sinh, đặt là Can Giáp
        // Chạy ngược chiều kim đồng hồ qua các Can: Giáp(1) → Ất(2) → Bính(3) → ...
        // Tìm vị trí Can của năm sinh, vị trí đó + 2 cung ngược là Tuần (chỉ 2 cung)
        
        // Khởi từ cung địa chi của năm sinh
        int startPos = yearBranch;  // 1-12 (Tý=1, Sửu=2, ..., Hợi=12)
        
        // Can năm sinh ở vị trí: startPos - (yearCan - 1)
        int canYearPos = startPos - (yearCan - 1);
        while (canYearPos <= 0) canYearPos += 12;
        
        // Tuần gồm 2 cung tiếp theo ngược: canYearPos và 1 cung tiếp theo
        int pos1 = canYearPos - 1; if (pos1 <= 0) pos1 += 12;
        int pos2 = pos1 - 1; if (pos2 <= 0) pos2 += 12;
        
        return $"{branchNames[pos1 - 1]},{branchNames[pos2 - 1]}";
    }

    // Parse địa chi từ string thành list vị trí cung
    private List<int> ParseBranchPositions(string branchString)
    {
        if (string.IsNullOrEmpty(branchString)) return new List<int>();
        
        string[] branchNames = ["Tý", "Sửu", "Dần", "Mão", "Thìn", "Tỵ", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi"];
        var positions = new List<int>();
        
        // Split theo dấu - hoặc ,
        var parts = branchString.Replace("-", ",").Split(',');
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var index = Array.IndexOf(branchNames, trimmed);
            if (index >= 0)
            {
                positions.Add(index + 1); // Cung 1-12
            }
        }
        
        return positions;
    }

    // Tính Đại Vận theo Cục và Âm Dương
    private Dictionary<int, int> CalculateDaiVan(int menhPalace, int nguHanhCuc, string amDuong)
    {
        var daiVan = new Dictionary<int, int>();
        
        // Số tuổi khởi đầu dựa vào Cục
        // Thủy Nhị = 2, Mộc Tam = 3, Kim Tứ = 4, Thổ Ngũ = 5, Hỏa Lục = 6
        int startAge = nguHanhCuc;
        
        // Dương Nam, Âm Nữ: đi thuận (chiều kim đồng hồ)
        // Âm Nam, Dương Nữ: đi nghịch (ngược chiều kim đồng hồ)
        bool thuanChieu = amDuong == "Dương Nam" || amDuong == "Âm Nữ";
        
        for (int i = 0; i < 12; i++)
        {
            int palacePos;
            if (thuanChieu)
            {
                // Đi thuận từ Mệnh
                palacePos = menhPalace + i;
                if (palacePos > 12) palacePos -= 12;
            }
            else
            {
                // Đi nghịch từ Mệnh
                palacePos = menhPalace - i;
                if (palacePos <= 0) palacePos += 12;
            }
            
            // Mỗi cung cách nhau 10 tuổi
            daiVan[palacePos] = startAge + (i * 10);
        }
        
        return daiVan;
    }

    // Tính Tiểu Hạn
    private Dictionary<int, string> CalculateTieuHan(int lunarYear, bool isMale)
    {
        var tieuHan = new Dictionary<int, string>();
        string[] branchNames = ["Tý", "Sửu", "Dần", "Mão", "Thìn", "Tị", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi"];

        // 1. Xác định Chi năm sinh
        int birthBranch = (lunarYear - 3) % 12;
        if (birthBranch <= 0) birthBranch += 12;

        // 2. Xác định cung khởi Tiểu Hạn
        // Dần (3), Ngọ (7), Tuất (11) -> Khởi tại Thìn (5)
        // Thân (9), Tý (1), Thìn (5) -> Khởi tại Tuất (11)
        // Tị (6), Dậu (10), Sửu (2) -> Khởi tại Mùi (8)
        // Hợi (12), Mão (4), Mùi (8) -> Khởi tại Sửu (2)
        
        int startPalacePos = 0;
        if (birthBranch == 3 || birthBranch == 7 || birthBranch == 11) startPalacePos = 5;
        else if (birthBranch == 9 || birthBranch == 1 || birthBranch == 5) startPalacePos = 11;
        else if (birthBranch == 6 || birthBranch == 10 || birthBranch == 2) startPalacePos = 8;
        else if (birthBranch == 12 || birthBranch == 4 || birthBranch == 8) startPalacePos = 2;

        // 3. An Tiểu Hạn
        // Nam thuận, Nữ nghịch
        bool thuanChieu = isMale;

        // Bắt đầu từ năm sinh (birthBranch), an vào cung khởi (startPalacePos)
        // Viết tiếp theo chiều thuận của địa chi (năm sau = năm trước + 1)
        // Nhưng vị trí cung thì theo chiều Nam/Nữ
        
        int currentBranch = birthBranch;
        int currentPalace = startPalacePos;

        for (int i = 0; i < 12; i++)
        {
            // Ghi nhận
            tieuHan[currentPalace] = branchNames[currentBranch - 1];

            // Tăng năm (luôn tăng)
            currentBranch++;
            if (currentBranch > 12) currentBranch = 1;

            // Di chuyển cung tương ứng
            if (thuanChieu)
            {
                currentPalace++;
                if (currentPalace > 12) currentPalace = 1;
            }
            else
            {
                currentPalace--;
                if (currentPalace < 1) currentPalace = 12;
            }
        }

        return tieuHan;
    }

    // Tính Nguyệt Hạn
    // Luật: Từ cung Tiểu Vận (của năm xem hạn), nghịch tháng (đến tháng sinh), thuận giờ (đến giờ sinh) -> Tháng 1
    // Sau đó thuận hành mỗi cung 1 tháng
    private Dictionary<int, int> CalculateNguyetHan(Dictionary<int, string> tieuHan, int viewYear, int birthMonth, int birthHourBranch)
    {
        var nguyetHan = new Dictionary<int, int>();
        string[] branchNames = ["Tý", "Sửu", "Dần", "Mão", "Thìn", "Tị", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi"];

        // 1. Tìm Chi của năm xem hạn
        int viewYearBranch = (viewYear - 3) % 12;
        if (viewYearBranch <= 0) viewYearBranch += 12;
        string viewBranchName = branchNames[viewYearBranch - 1];

        // 2. Tìm cung Tiểu Vận (Tiểu Hạn của năm đó)
        // tieuHan map: PalaceId -> BranchName
        int tieuVanPalace = tieuHan.FirstOrDefault(x => x.Value == viewBranchName).Key;
        
        // Nếu không tìm thấy (lỗi logic nào đó), return empty
        if (tieuVanPalace == 0) return nguyetHan;

        // 3. Tính vị trí tháng 1
        // Từ Tiểu Vận, nghịch tháng (bắt đầu là tháng 1, lùi về birthMonth)
        // => count backward (birthMonth - 1) steps
        int posAfterMonth = tieuVanPalace - (birthMonth - 1);
        while (posAfterMonth <= 0) posAfterMonth += 12;

        // Từ đó, thuận giờ (bắt đầu là giờ Tý, tiến tới birthHourBranch)
        // => count forward (birthHourBranch - 1) steps
        int month1Pos = posAfterMonth + (birthHourBranch - 1);
        while (month1Pos > 12) month1Pos -= 12;

        // 4. An Nguyệt Hạn (Thuận hành)
        for (int m = 1; m <= 12; m++)
        {
            int currentPos = month1Pos + (m - 1);
            while (currentPos > 12) currentPos -= 12;
            
            nguyetHan[currentPos] = m;
        }

        return nguyetHan;
    }
}
