import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TuViChart, PalaceStar } from '../../models/tu-vi.models';
import { TuViService, PalaceInterpretationResult } from '../../services/tu-vi.service';
import { InterpretationResponse } from '../../models/interpretation.models';
import { PalaceInterpretationModalComponent } from '../palace-interpretation-modal/palace-interpretation-modal.component';

@Component({
  selector: 'app-tu-vi-chart',
  imports: [CommonModule, FormsModule, PalaceInterpretationModalComponent],
  templateUrl: './tu-vi-chart.component.html',
  styleUrl: './tu-vi-chart.component.css'
})
export class TuViChartComponent implements OnChanges {
  @Input() chart: TuViChart | null = null;

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

  constructor(private tuViService: TuViService) {}

  // Theo dõi thay đổi của chart để clear cache khi lập lá số mới
  ngOnChanges(changes: SimpleChanges) {
    if (changes['chart'] && !changes['chart'].firstChange) {
      // Clear cache khi chart thay đổi (lập lá số mới)
      this.palaceInterpretationsCache.clear();
      this.interpretation = null;
    }
  }

  // Mapping địa chi theo vị trí
  private branchNames = ['Tý', 'Sửu', 'Dần', 'Mão', 'Thìn', 'Tỵ', 'Ngọ', 'Mùi', 'Thân', 'Dậu', 'Tuất', 'Hợi'];

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
    
    const branches = ['Tý', 'Sửu', 'Dần', 'Mão', 'Thìn', 'Tị', 'Ngọ', 'Mùi', 'Thân', 'Dậu', 'Tuất', 'Hợi'];
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
    
    const branches = ['Tý', 'Sửu', 'Dần', 'Mão', 'Thìn', 'Tị', 'Ngọ', 'Mùi', 'Thân', 'Dậu', 'Tuất', 'Hợi'];
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

  getStarClass(starName: string, element: string = ''): string {
    // Ưu tiên tô màu theo ngũ hành trước
    if (element) {
      switch (element) {
        case 'Kim': return 'star-element-kim';
        case 'Mộc': return 'star-element-moc';
        case 'Thủy': return 'star-element-thuy';
        case 'Hỏa': return 'star-element-hoa';
        case 'Thổ': return 'star-element-tho';
      }
    }

    // Nếu không có ngũ hành thì dùng màu mặc định theo loại
    // Chính tinh - màu tím
    const mainStars = ['Tử Vi', 'Thiên Cơ', 'Thái Dương', 'Vũ Khúc', 'Thiên Đồng', 'Liêm Trinh', 
                       'Thiên Phủ', 'Thái Âm', 'Tham Lang', 'Cự Môn', 'Thiên Tướng', 'Thiên Lương', 'Thất Sát', 'Phá Quân'];
    
    // Tứ Hóa - màu đỏ
    const tuHoa = ['Hóa Lộc', 'Hóa Quyền', 'Hóa Khoa', 'Hóa Kỵ'];
    
    // Phụ tinh văn - màu xanh lá
    const vanTinh = ['Văn Xương', 'Văn Khúc', 'Tả Phù', 'Hữu Bật', 'Thiên Khôi', 'Thiên Việt'];
    
    // Hung tinh - màu đỏ đậm
    const hungTinh = ['Hỏa Tinh', 'Linh Tinh', 'Địa Không', 'Địa Kiếp', 'Thiên La', 'Địa Võng', 'Đà La'];
    
    // Trường Sinh - màu cam/vàng
    const truongSinh = ['Trường Sinh', 'Mộc Dục', 'Quan Đới', 'Lâm Quan', 'Đế Vượng', 'Suy', 
                         'Bệnh', 'Tử', 'Mộ', 'Tuyệt', 'Thai', 'Dưỡng'];
    
    // Thái Tuế - màu xanh dương
    const thaiTue = ['Thái Tuế', 'Thiếu Dương', 'Tang Môn', 'Thiếu Âm', 'Quan Phù', 'Tử Phù', 
                     'Tuế Phá', 'Long Đức', 'Bạch Hổ', 'Phúc Đức', 'Điếu Khách', 'Trực Phù',
                     'Thiên Không', 'Long Trì', 'Nguyệt Đức', 'Thiên Hư', 'Thiên Đức',
                     'Thiên Khốc', 'Hoa Cái', 'Đào Hoa', 'Kiếp Sát'];

    if (mainStars.includes(starName)) return 'star-main';
    if (tuHoa.includes(starName)) return 'star-tuhoa';
    if (vanTinh.includes(starName)) return 'star-van';
    if (hungTinh.includes(starName)) return 'star-hung';
    if (truongSinh.includes(starName)) return 'star-truongsinh';
    if (thaiTue.includes(starName)) return 'star-thaitue';
    
    return 'star-secondary';
  }

  formatTime(timeString: string): string {
    if (!timeString) return '';
    const time = timeString.split(':');
    return `${time[0]}:${time[1]}`;
  }

  getAmDuong(): string {
    if (!this.chart) return '';
    return this.chart.amDuong || '';
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
        this.interpretationError = 'Không thể luận giải lá số. Vui lòng kiểm tra cấu hình OpenAI API Key.';
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
}

