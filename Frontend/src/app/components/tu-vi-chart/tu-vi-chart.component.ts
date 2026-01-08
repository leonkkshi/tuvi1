import { Component, Input, OnChanges, SimpleChanges, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TuViChart, PalaceStar } from '../../models/tu-vi.models';
import { TuViService, PalaceInterpretationResult } from '../../services/tu-vi.service';
import { InterpretationResponse } from '../../models/interpretation.models';
import { PalaceInterpretationModalComponent } from '../palace-interpretation-modal/palace-interpretation-modal.component';
// import html2canvas from 'html2canvas'; // Lazy load để giảm bundle size

@Component({
  selector: 'app-tu-vi-chart',
  imports: [CommonModule, FormsModule, PalaceInterpretationModalComponent],
  templateUrl: './tu-vi-chart.component.html',
  styleUrl: './tu-vi-chart.component.css'
})
export class TuViChartComponent implements OnChanges {
  @Input() chart: TuViChart | null = null;
  @ViewChild('chartContainer') chartContainer!: ElementRef;

  // AI Interpretation properties
  interpretation: InterpretationResponse | null = null;
  isLoadingInterpretation = false;
  interpretationError = '';
  focusArea = 'general';

  // Palace interpretation modal
  selectedPalace: PalaceStar | null = null;
  isModalOpen = false;
  
  // Cache cho palace interpretations - lưu theo palaceName
  palaceInterpretationsCache: Map<string, PalaceInterpretationResult> = new Map();

  // Barriers
  tuanPairs: Set<string> = new Set();
  trietPairs: Set<string> = new Set();

  // Download state
  isDownloading = false;

  constructor(private tuViService: TuViService) {}

  // Theo dõi thay đổi của chart để clear cache khi lập lá số mới
  ngOnChanges(changes: SimpleChanges) {
    if (changes['chart']) {
      // Clear cache khi chart thay đổi (lập lá số mới)
      this.palaceInterpretationsCache.clear();
      this.interpretation = null;
      this.updateBarriers();
    }
  }

  // Mapping địa chi theo vị trí
  private branchNames = ['Tý', 'Sửu', 'Dần', 'Mão', 'Thìn', 'Tỵ', 'Ngọ', 'Mùi', 'Thân', 'Dậu', 'Tuất', 'Hợi'];

  private updateBarriers() {
    this.tuanPairs.clear();
    this.trietPairs.clear();
    
    if (!this.chart) return;

    // Parse Triet: "Thân-Dậu" -> 9-10
    if (this.chart.trietBetween) {
      const parts = this.chart.trietBetween.split('-');
      if (parts.length === 2) {
        const id1 = this.getPalaceIdByName(parts[0].trim());
        const id2 = this.getPalaceIdByName(parts[1].trim());
        if (id1 > 0 && id2 > 0) {
           this.trietPairs.add(`${id1}-${id2}`);
           this.trietPairs.add(`${id2}-${id1}`); // Add reverse just in case, though we primarily check forward
        }
      }
    }

    // Parse Tuan: "Thân,Dậu" -> 9-10
    if (this.chart.tuanPositions) {
       // Usually format is "A,B".
       const parts = this.chart.tuanPositions.split(',');
       if (parts.length === 2) {
         const id1 = this.getPalaceIdByName(parts[0].trim());
         const id2 = this.getPalaceIdByName(parts[1].trim());
         if (id1 > 0 && id2 > 0) {
             this.tuanPairs.add(`${id1}-${id2}`);
             this.tuanPairs.add(`${id2}-${id1}`);
         }
       }
    }
  }

  private getPalaceIdByName(name: string): number {
    const idx = this.branchNames.indexOf(name);
    return idx >= 0 ? idx + 1 : 0;
  }

  getBarrierLabel(id: number): string {
    // Check for barrier with "previous" neighbor (logically or visually earlier in DOM)
    // We attach labels to the "later" element to ensure it renders on top.
    
    // Top Row: 7, 8, 9 (Left barrier with 6, 7, 8)
    if (id === 7 && this.checkPair(6, 7)) return this.getLabelForPair(6, 7);
    if (id === 8 && this.checkPair(7, 8)) return this.getLabelForPair(7, 8);
    if (id === 9 && this.checkPair(8, 9)) return this.getLabelForPair(8, 9);

    // Right Column: 10, 11, 12 (Top barrier with 9, 10, 11)
    if (id === 10 && this.checkPair(9, 10)) return this.getLabelForPair(9, 10);
    if (id === 11 && this.checkPair(10, 11)) return this.getLabelForPair(10, 11);
    if (id === 12) {
       // 12 has Top barrier with 11
       if (this.checkPair(11, 12)) return this.getLabelForPair(11, 12);
       // 12 also needs to handle barrier with 1?
       // 12 is at R4C4. 1 is R4C3. 12 is to the right of 1.
       // 12 is later in DOM than 1. So 12 should handle 1-12 boundary (Left barrier).
       if (this.checkPair(12, 1)) return this.getLabelForPair(12, 1);
    } 

    // Bottom Row: 1, 2 (Left barrier with 2, 3) -> Wait
    // Order: 3, 2, 1, 12.
    // 1 is later than 2. Boundary 1-2. 1 is right of 2. 1 handles Left barrier.
    if (id === 1 && this.checkPair(1, 2)) return this.getLabelForPair(1, 2);
    // 2 is later than 3. Boundary 2-3. 2 is right of 3. 2 handles Left barrier.
    if (id === 2 && this.checkPair(2, 3)) return this.getLabelForPair(2, 3);

    // Left Column: 3, 4, 5 (Top barrier with 4, 5, 6) -> Wait
    // 3 (R4) vs 4 (R3). 3 is later in DOM. 
    // 3 is Below 4.
    // So 3 handles barrier with 4. Barrier is Top of 3.
    if (id === 3 && this.checkPair(3, 4)) return this.getLabelForPair(3, 4);
    
    // 4 vs 5. 4 (R3) vs 5 (R2). 4 is later. 4 Below 5.
    // 4 handles barrier with 5. Top of 4.
    if (id === 4 && this.checkPair(4, 5)) return this.getLabelForPair(4, 5);

    // 5 vs 6. 5 (R2) vs 6 (R1). 5 is later. 5 Below 6.
    // 5 handles barrier with 6. Top of 5.
    if (id === 5 && this.checkPair(5, 6)) return this.getLabelForPair(5, 6);

    return '';
  }

  private checkPair(id1: number, id2: number): boolean {
    const key = `${id1}-${id2}`;
    return this.tuanPairs.has(key) || this.trietPairs.has(key) || this.tuanPairs.has(`${id2}-${id1}`) || this.trietPairs.has(`${id2}-${id1}`);
  }

  private getLabelForPair(id1: number, id2: number): string {
    const key = `${id1}-${id2}`;
    const revKey = `${id2}-${id1}`;
    
    const hasTuan = this.tuanPairs.has(key) || this.tuanPairs.has(revKey);
    const hasTriet = this.trietPairs.has(key) || this.trietPairs.has(revKey);

    if (hasTuan && hasTriet) return 'Tuần - Triệt';
    if (hasTuan) return 'Tuần';
    if (hasTriet) return 'Triệt';
    return '';
  }

  getBarrierClass(id: number): string {
    // Return CSS class based on the barrier position relative to the cell
    // AND relative to the center of the chart (Inner Edge Anchoring)
    
    // Left Barriers
    // Top Row (6-7, 7-8, 8-9)
    if (id === 7 || id === 8 || id === 9) return 'barrier-ver-bottom'; // Left barrier, anchored Bottom
    
    if (id === 12) {
      // 12 can be Top (11-12) or Left (12-1)
      // Prioritize? Can a cell have both?
      // Usually Tuần/Triệt are adjacent. 
      // If 11-12 has barrier, and 12-1 has barrier -> That means 11,12,1 are all blocked?
      // Possible if Tuần at 11-12 and Triệt at 12-1.
      // But getBarrierLabel returns only one string.
      // My logic above returns first match.
      // 11-12 checked first -> Top.
      if (this.checkPair(11, 12)) return 'barrier-hor-left';
      if (this.checkPair(12, 1)) return 'barrier-ver-top';
    }

    if (id === 1 || id === 2) return 'barrier-ver-top'; // For 1-2, 2-3 (Left barrier, anchored Top)

    // Top Barriers
    // Right Column (9-10, 10-11)
    if (id === 10 || id === 11) return 'barrier-hor-left'; // Top barrier, anchored Left

    // Left Column (3-4, 4-5, 5-6)
    if (id === 3 || id === 4 || id === 5) return 'barrier-hor-right'; // Top barrier, anchored Right

    return '';
  }

  // Kiểm tra xem Triệt và Tuần có cùng 2 cung không
  areSamePositions(): boolean {
    if (!this.chart?.trietBetween || !this.chart?.tuanPositions) return false;
    
    // Triệt format: "Thân-Dậu", Tuần format: "Thân,Dậu"
    // Convert Tuần thành format giống Triệt để so sánh
    const tuanFormatted = this.chart.tuanPositions.replace(/,/g, '-');
    const isSame = this.chart.trietBetween === tuanFormatted;
    
    return isSame;
  }

  // Format Triệt để hiển thị (kiểm tra nếu chứa ký tự không phải chi)
  getTrietDisplay(): string {
    if (!this.chart?.trietBetween) return '';
    
    const branches = ['Tý', 'Sửu', 'Dần', 'Mão', 'Thìn', 'Tỵ', 'Ngọ', 'Mùi', 'Thân', 'Dậu', 'Tuất', 'Hợi'];
    const parts = this.chart.trietBetween.split('-');
    
    // Kiểm tra xem tất cả phần đều là chi hợp lệ
    const isValid = parts.every(part => branches.includes(part));
    
    if (isValid && parts.length === 2) {
      return this.chart.trietBetween;
    }
    
    return '';
  }

  // Format Tuần để hiển thị
  getTuanDisplay(): string {
    if (!this.chart?.tuanPositions) return '';
    
    const branches = ['Tý', 'Sửu', 'Dần', 'Mão', 'Thìn', 'Tỵ', 'Ngọ', 'Mùi', 'Thân', 'Dậu', 'Tuất', 'Hợi'];
    const parts = this.chart.tuanPositions.split(',');
    
    // Kiểm tra xem tất cả phần đều là chi hợp lệ
    const isValid = parts.every(part => branches.includes(part.trim()));
    
    if (isValid && parts.length === 2) {
      return this.chart.tuanPositions;
    }
    
    return '';
  }

  getPalaceByPosition(palaceId: number): PalaceStar | undefined {
    return this.chart?.palaceStars.find(p => p.palaceId === palaceId);
  }

  getPalaceBranchByPosition(palaceId: number): string {
    return this.branchNames[palaceId - 1] || '';
  }

  getStarClass(starName: string, element: string = '', type: string = ''): string {
    let classes = [];

    // Ưu tiên tô màu theo ngũ hành trước
    if (element) {
      switch (element) {
        case 'Kim': classes.push('star-element-kim'); break;
        case 'Mộc': classes.push('star-element-moc'); break;
        case 'Thủy': classes.push('star-element-thuy'); break;
        case 'Hỏa': classes.push('star-element-hoa'); break;
        case 'Thổ': classes.push('star-element-tho'); break;
      }
    }

    // Thêm font-weight cho các type đặc biệt và sao có chữ Hóa
    if (type === 'Lục sát' || type === 'Cát tinh' || type === 'Trung tinh' || starName.includes('Hóa')) {
      classes.push('star-bold');
    }

    // Nếu không có ngũ hành thì dùng màu mặc định theo loại
    if (classes.length === 0) {
      // Chính tinh - màu tím
      const mainStars = ['Tử Vi', 'Thiên Cơ', 'Thái Dương', 'Vũ Khúc', 'Thiên Đồng', 'Liêm Trinh', 
                         'Thiên Phủ', 'Thái Âm', 'Tham Lang', 'Cự Môn', 'Thiên Tướng', 'Thiên Lương', 'Thất Sát', 'Phá Quân'];
      
      // Tứ Hóa - màu đỏ
      const tuHoa = ['Hóa Lộc', 'Hóa Quyền', 'Hóa Khoa', 'Hóa Kỵ'];
      
      // Phụ tinh văn - màu xanh lá
      const vanTinh = ['Văn Xương', 'Văn Khúc', 'Tả Phù', 'Hữu Bật', 'Thiên Khôi', 'Thiên Việt'];
      
      // Hung tinh - màu đỏ đậm
      const hungTinh = ['Hỏa Tinh', 'Linh Tinh', 'Địa Không', 'Địa Kiếp', 'Thiên La', 'Địa Võng', 'Đà La',
                         'Lưu Lộc Tồn', 'Lưu Thiên Mã', 'Lưu Kình Dương', 'Lưu Đà La'];
      
      // Trường Sinh - màu cam/vàng
      const truongSinh = ['Trường Sinh', 'Mộc Dục', 'Quan Đới', 'Lâm Quan', 'Đế Vượng', 'Suy', 
                           'Bệnh', 'Tử', 'Mộ', 'Tuyệt', 'Thai', 'Dưỡng'];
      
      // Thái Tuế - màu xanh dương
      const thaiTue = ['Thái Tuế', 'Thiếu Dương', 'Tang Môn', 'Thiếu Âm', 'Quan Phù', 'Tử Phù', 
                       'Tuế Phá', 'Long Đức', 'Bạch Hổ', 'Phúc Đức', 'Điếu Khách', 'Trực Phù',
                       'Thiên Không', 'Long Trì', 'Nguyệt Đức', 'Thiên Hư', 'Thiên Đức',
                       'Thiên Khốc', 'Hoa Cái', 'Đào Hoa', 'Kiếp Sát',
                       'Lưu Thái Tuế', 'Lưu Thiên Khốc', 'Lưu Thiên Hư', 'Lưu Tang Môn', 'Lưu Bạch Hổ'];

      if (mainStars.includes(starName)) classes.push('star-main');
      else if (tuHoa.includes(starName)) classes.push('star-tuhoa');
      else if (vanTinh.includes(starName)) classes.push('star-van');
      else if (hungTinh.includes(starName)) classes.push('star-hung');
      else if (truongSinh.includes(starName)) classes.push('star-truongsinh');
      else if (thaiTue.includes(starName)) classes.push('star-thaitue');
    }

    return classes.join(' ');
  }

  formatTime(timeString: string): string {
    if (!timeString) return '';
    const time = timeString.split(':');
    return `${time[0]}:${time[1]}`;
  }

  getMenh(): string {
    // Tìm cung Mệnh và trả về thông tin
    const menhPalace = this.chart?.palaceStars.find(p => p.palaceName === 'Mệnh');
    if (menhPalace) {
      const mainStar = menhPalace.stars.find(s => 
        ['Tử Vi', 'Thiên Cơ', 'Thái Dương', 'Vũ Khúc', 'Thiên Đồng', 'Liêm Trinh', 
         'Thiên Phủ', 'Thái Âm', 'Tham Lang', 'Cự Môn', 'Thiên Tướng', 'Thiên Lương', 'Thất Sát', 'Phá Quân'].includes(s.starName)
      );
      return mainStar ? mainStar.starName : 'Chưa xác định';
    }
    return '';
  }

  getMenhBranch(): string {
    // Tìm cung Mệnh và trả về chi
    const menhPalace = this.chart?.palaceStars.find(p => p.palaceName === 'Mệnh');
    if (menhPalace) {
      return this.branchNames[menhPalace.palaceId - 1] || '';
    }
    return '';
  }

  getThanBranch(): string {
    // Trả về chi của cung Thân
    if (this.chart?.thanPalace) {
      return this.branchNames[this.chart.thanPalace - 1] || '';
    }
    return '';
  }

  isThanPalace(palaceId: number): boolean {
    // Kiểm tra xem palace này có phải là cung Thân không
    return this.chart?.thanPalace === palaceId;
  }

  getCuc(): string {
    if (!this.chart) return '';
    
    const cucNames: { [key: number]: string } = {
      2: 'Thủy Nhị Cục',
      3: 'Mộc Tam Cục',
      4: 'Kim Tứ Cục',
      5: 'Thổ Ngũ Cục',
      6: 'Hỏa Lục Cục'
    };
    
    return cucNames[this.chart.nguHanhCuc] || 'Chưa xác định';
  }

  getAmDuong(): string {
    if (!this.chart) return '';
    return this.chart.amDuong || '';
  }

  getMenhYear(): string {
    return this.chart?.napAm || '';
  }

  getNapAm(): string {
    return this.chart?.napAm || '';
  }

  // Lọc chính tinh (14 sao chính)
  getChinhTinh(palaceId: number) {
    const palace = this.getPalaceByPosition(palaceId);
    if (!palace) return [];
    return palace.stars.filter(star => star.type === 'Chính tinh');
  }

  // Lọc phụ tinh cát (không bao gồm Trường Sinh và Chính tinh)
  getPhuTinhCat(palaceId: number) {
    const palace = this.getPalaceByPosition(palaceId);
    if (!palace) return [];
    
    const truongSinhStars = ['Trường Sinh', 'Mộc Dục', 'Quan Đới', 'Lâm Quan', 'Đế Vượng', 'Suy', 
                              'Bệnh', 'Tử', 'Mộ', 'Tuyệt', 'Thai', 'Dưỡng'];
    
    return palace.stars.filter(star => 
      star.type !== 'Chính tinh' && 
      !truongSinhStars.includes(star.starName) &&
      star.nature === 'Cát'
    );
  }

  // Lọc phụ tinh hung (không bao gồm Trường Sinh và Chính tinh)
  getPhuTinhHung(palaceId: number) {
    const palace = this.getPalaceByPosition(palaceId);
    if (!palace) return [];
    
    const truongSinhStars = ['Trường Sinh', 'Mộc Dục', 'Quan Đới', 'Lâm Quan', 'Đế Vượng', 'Suy', 
                              'Bệnh', 'Tử', 'Mộ', 'Tuyệt', 'Thai', 'Dưỡng'];
    
    return palace.stars.filter(star => 
      star.type !== 'Chính tinh' && 
      !truongSinhStars.includes(star.starName) &&
      star.nature === 'Hung'
    );
  }

  // Lọc các sao Trường Sinh
  getTruongSinhStars(palaceId: number) {
    const palace = this.getPalaceByPosition(palaceId);
    if (!palace) return [];
    
    const truongSinhStars = ['Trường Sinh', 'Mộc Dục', 'Quan Đới', 'Lâm Quan', 'Đế Vượng', 'Suy', 
                              'Bệnh', 'Tử', 'Mộ', 'Tuyệt', 'Thai', 'Dưỡng'];
    
    return palace.stars.filter(star => truongSinhStars.includes(star.starName));
  }

  // Lọc các sao cát (không bao gồm Trường Sinh)
  getCatStars(palaceId: number) {
    const palace = this.getPalaceByPosition(palaceId);
    if (!palace) return [];
    
    const truongSinhStars = ['Trường Sinh', 'Mộc Dục', 'Quan Đới', 'Lâm Quan', 'Đế Vượng', 'Suy', 
                              'Bệnh', 'Tử', 'Mộ', 'Tuyệt', 'Thai', 'Dưỡng'];
    
    return palace.stars.filter(star => 
      !truongSinhStars.includes(star.starName) &&
      star.nature === 'Cát'
    );
  }

  // Lọc các sao hung (không bao gồm Trường Sinh)
  getHungStars(palaceId: number) {
    const palace = this.getPalaceByPosition(palaceId);
    if (!palace) return [];
    
    const truongSinhStars = ['Trường Sinh', 'Mộc Dục', 'Quan Đới', 'Lâm Quan', 'Đế Vượng', 'Suy', 
                              'Bệnh', 'Tử', 'Mộ', 'Tuyệt', 'Thai', 'Dưỡng'];
    
    return palace.stars.filter(star => 
      !truongSinhStars.includes(star.starName) &&
      star.nature === 'Hung'
    );
  }

  // AI Interpretation methods
  requestAIInterpretation(): void {
    if (!this.chart) return;

    this.isLoadingInterpretation = true;
    this.interpretationError = '';
    this.interpretation = null;

    this.tuViService.interpretChart({
      chart: this.chart,
      focusArea: this.focusArea
    }).subscribe({
      next: (response) => {
        this.interpretation = response;
        this.isLoadingInterpretation = false;
      },
      error: (error) => {
        console.error('Error getting AI interpretation:', error);
        
        // Xử lý các loại lỗi cụ thể
        if (error.status === 503) {
          // Hệ thống quá tải
          const errorData = error.error;
          this.interpretationError = errorData?.message || '😔 Hệ thống đang quá tải. Vui lòng thử lại sau vài phút.';
          
          if (errorData?.retryAfter) {
            this.interpretationError += ` (Thử lại sau ${errorData.retryAfter}s)`;
          }
          
          if (errorData?.queueStats) {
            const stats = errorData.queueStats;
            this.interpretationError += `\n\n📋 Trạng thái hàng đợi: ${stats.totalQueueSize} yêu cầu đang chờ xử lý.`;
          }
        } else if (error.status === 429) {
          // Rate limit
          this.interpretationError = '⚠️ Bạn đã gửi quá nhiều yêu cầu. Vui lòng chờ 1 phút rồi thử lại.';
        } else if (error.status === 0) {
          // Network error
          this.interpretationError = '❌ Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.';
        } else {
          // Generic error
          const errorMsg = error.error?.message || error.error?.error || error.message;
          this.interpretationError = errorMsg || 'Không thể luận giải lá số. Vui lòng kiểm tra cấu hình API.';
        }
        
        this.isLoadingInterpretation = false;
      }
    });
  }

  onFocusAreaChange(event: any): void {
    this.focusArea = event.target.value;
  }

  // Parse markdown text thành HTML
  parseMarkdown(text: string): string {
    if (!text) return '';
    
    let html = text;
    
    // Convert headers (phải làm trước để tránh conflict với bold)
    html = html.replace(/^######\s+(.+)$/gm, '<h6>$1</h6>');
    html = html.replace(/^#####\s+(.+)$/gm, '<h5>$1</h5>');
    html = html.replace(/^####\s+(.+)$/gm, '<h4>$1</h4>');
    html = html.replace(/^###\s+(.+)$/gm, '<h3>$1</h3>');
    html = html.replace(/^##\s+(.+)$/gm, '<h2>$1</h2>');
    html = html.replace(/^#\s+(.+)$/gm, '<h1>$1</h1>');
    
    // Convert **text** thành <strong>text</strong>
    html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    
    // Convert *text* thành <em>text</em>
    html = html.replace(/\*(.*?)\*/g, '<em>$1</em>');
    
    // Convert line breaks thành <br>
    html = html.replace(/\n/g, '<br>');
    
    // Convert - item thành list items (basic support)
    html = html.replace(/^- (.+)$/gm, '• $1');
    
    return html;
  }

  // Palace interpretation modal methods
  openPalaceInterpretation(palaceId: number) {
    const palace = this.getPalaceByPosition(palaceId);
    if (palace) {
      this.selectedPalace = palace;
      this.isModalOpen = true;
    }
  }

  closeModal() {
    this.isModalOpen = false;
    // Không xóa selectedPalace để giữ cache interpretation
  }

  // Lấy cached interpretation nếu có
  getCachedInterpretation(palaceName: string): PalaceInterpretationResult | null {
    return this.palaceInterpretationsCache.get(palaceName) || null;
  }

  // Lưu interpretation vào cache
  setCachedInterpretation(palaceName: string, result: PalaceInterpretationResult) {
    this.palaceInterpretationsCache.set(palaceName, result);
  }

  getDaiVan(palaceId: number): number | string {
    if (!this.chart || !this.chart.daiVan) return '';
    return this.chart.daiVan[palaceId] || '';
  }

  getTieuHan(palaceId: number): string {
    if (!this.chart || !this.chart.tieuHan) return '';
    return this.chart.tieuHan[palaceId] || '';
  }

  getNguyetHan(palaceId: number): number | string {
    if (!this.chart || !this.chart.nguyetHan) return '';
    return this.chart.nguyetHan[palaceId] || '';
  }

  getBrightnessText(brightness: number): string {
    if (brightness === undefined || brightness === null) return '';
    if (brightness >= 90) return '(M)';
    if (brightness >= 80) return '(V)';
    if (brightness >= 60) return '(Đ)';
    if (brightness >= 50) return '(B)';
    return '(H)';
  }

  getPalaceIcon(palaceName: string): string {
    const icons: {[key: string]: string} = {
      'Mệnh': '🎯',
      'Phụ Mẫu': '👨‍👩‍👦',
      'Phúc Đức': '🎭',
      'Điền Trạch': '🏠',
      'Quan Lộc': '💼',
      'Nô Bộc': '👥',
      'Thiên Di': '✈️',
      'Tật Ách': '⚕️',
      'Tài Bạch': '💰',
      'Tử Tức': '👶',
      'Phu Thê': '💑',
      'Huynh Đệ': '👫'
    };
    return icons[palaceName] || '⭐';
  }

  // Download lá số dưới dạng hình ảnh
  async downloadChart() {
    if (!this.chart || this.isDownloading) return;
    
    try {
      this.isDownloading = true;
      
      // Lazy load html2canvas để giảm bundle size
      const html2canvas = (await import('html2canvas')).default;
      
      // Tìm element chứa lá số (bao gồm cả header và chart)
      const element = document.querySelector('.tu-vi-chart') as HTMLElement;
      if (!element) {
        alert('Không tìm thấy lá số để tải xuống');
        return;
      }

      // Ẩn scroll hint và các nút không cần thiết trước khi chụp
      const scrollHint = element.querySelector('.chart-scroll-hint') as HTMLElement;
      const downloadBtn = element.querySelector('.download-btn') as HTMLElement;
      
      if (scrollHint) scrollHint.style.display = 'none';
      if (downloadBtn) downloadBtn.style.display = 'none';

      // Chụp ảnh
      const canvas = await html2canvas(element, {
        scale: 2, // Độ phân giải cao hơn
        useCORS: true,
        backgroundColor: '#fffef8',
        logging: false,
        windowWidth: element.scrollWidth,
        windowHeight: element.scrollHeight
      } as any);

      // Hiện lại các element đã ẩn
      if (scrollHint) scrollHint.style.display = '';
      if (downloadBtn) downloadBtn.style.display = '';

      // Convert canvas thành blob và tải xuống
      canvas.toBlob((blob) => {
        if (blob) {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          
          // Tạo tên file từ thông tin lá số
          const name = this.chart?.fullName || 'LasoTuVi';
          const date = new Date().toLocaleDateString('vi-VN').replace(/\//g, '-');
          link.download = `${name}_${date}.png`;
          
          link.href = url;
          link.click();
          
          // Cleanup
          URL.revokeObjectURL(url);
        }
      }, 'image/png');
      
    } catch (error) {
      console.error('Lỗi khi tải xuống lá số:', error);
      alert('Có lỗi xảy ra khi tải xuống lá số. Vui lòng thử lại.');
    } finally {
      this.isDownloading = false;
    }
  }
}

