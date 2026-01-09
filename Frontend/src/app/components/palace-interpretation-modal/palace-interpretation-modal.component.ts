import { Component, EventEmitter, Input, Output, SimpleChanges, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TuViChart, PalaceStar } from '../../models/tu-vi.models';
import { TuViService, PalaceInterpretationResult } from '../../services/tu-vi.service';

@Component({
  selector: 'app-palace-interpretation-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './palace-interpretation-modal.component.html',
  styleUrl: './palace-interpretation-modal.component.css'
})
export class PalaceInterpretationModalComponent implements OnChanges {
  @Input() chart: TuViChart | null = null;
  @Input() palace: PalaceStar | null = null;
  @Input() isOpen: boolean = false;
  @Input() cachedInterpretation: PalaceInterpretationResult | null = null;
  @Input() apiKey: string = '';
  @Input() provider: 'Gemini' | 'OpenAI' = 'Gemini';
  @Output() close = new EventEmitter<void>();
  @Output() interpretationLoaded = new EventEmitter<PalaceInterpretationResult>();

  interpretation: PalaceInterpretationResult | null = null;
  isLoading: boolean = false;
  error: string = '';
  private lastLoadedPalaceName: string = '';

  constructor(private tuViService: TuViService) {}

  ngOnChanges(changes: SimpleChanges) {
    console.log('[Modal] ngOnChanges', {
      isOpen: this.isOpen,
      palace: this.palace?.palaceName,
      hasCache: !!this.cachedInterpretation,
      changes: Object.keys(changes)
    });

    // Reset khi modal đóng
    if (!this.isOpen) {
      return;
    }

    // Khi modal mở và có palace
    if (this.isOpen && this.palace && this.chart) {
      const currentPalaceName = this.palace.palaceName;
      
      // Kiểm tra cache trước
      if (this.cachedInterpretation && this.cachedInterpretation.palaceName === currentPalaceName) {
        console.log('[Modal] Using cached interpretation for:', currentPalaceName);
        this.interpretation = this.cachedInterpretation;
        this.isLoading = false;
        this.error = '';
        this.lastLoadedPalaceName = currentPalaceName;
        return;
      }
      
      // Nếu chưa load cung này thì load
      if (this.lastLoadedPalaceName !== currentPalaceName) {
        console.log('[Modal] Need to load interpretation for:', currentPalaceName);
        this.loadInterpretation();
      }
    }
  }

  loadInterpretation() {
    if (!this.chart || !this.palace) {
      console.log('[Modal] Cannot load: missing chart or palace');
      return;
    }
    
    if (this.isLoading) {
      console.log('[Modal] Already loading, skip');
      return;
    }

    const palaceName = this.palace.palaceName;
    this.lastLoadedPalaceName = palaceName;
    this.isLoading = true;
    this.error = '';
    this.interpretation = null;

    console.log(`[Modal] Starting API call for: ${palaceName}`);

    this.tuViService.interpretPalace(this.chart, palaceName, this.apiKey, this.provider).subscribe({
      next: (result) => {
        console.log(`[Modal] API response for ${palaceName}:`, result);
        // Chỉ cập nhật nếu vẫn đang xem cùng cung
        if (this.palace && this.palace.palaceName === palaceName) {
          this.interpretation = result;
          this.isLoading = false;
          // Emit để parent component cache lại
          this.interpretationLoaded.emit(result);
          console.log(`[Modal] Updated state - isLoading: ${this.isLoading}, interpretation:`, this.interpretation);
        } else {
          console.log(`[Modal] Palace changed, ignoring result for ${palaceName}`);
        }
      },
      error: (err) => {
        console.error('[Modal] API error:', err);
        if (this.palace && this.palace.palaceName === palaceName) {
          // Xử lý các loại lỗi cụ thể
          if (err.status === 503) {
            // Hệ thống quá tải
            const errorData = err.error;
            this.error = errorData?.message || '😔 Hệ thống đang quá tải. Vui lòng thử lại sau vài phút.';
            
            if (errorData?.retryAfter) {
              this.error += ` (Thử lại sau ${errorData.retryAfter}s)`;
            }
          } else if (err.status === 429) {
            this.error = '⚠️ Bạn đã gửi quá nhiều yêu cầu. Vui lòng chờ 1 phút.';
          } else if (err.status === 0) {
            this.error = '❌ Không thể kết nối đến server.';
          } else {
            const errorMsg = err.error?.message || err.error?.error;
            this.error = errorMsg || 'Không thể tải luận giải cho cung này. Vui lòng thử lại.';
          }
          
          this.isLoading = false;
        }
      }
    });
  }

  closeModal() {
    // Không xóa interpretation để giữ cache khi đóng modal
    this.close.emit();
  }

  handleBackdropClick(event: MouseEvent) {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.closeModal();
    }
  }

  getBranchName(palaceId: number): string {
    const branches = ['Tý', 'Sửu', 'Dần', 'Mão', 'Thìn', 'Tị', 'Ngọ', 'Mùi', 'Thân', 'Dậu', 'Tuất', 'Hợi'];
    return branches[palaceId - 1] || '';
  }

  // Parse markdown text thành HTML
  parseMarkdown(text: string): string {
    if (!text) return '';
    
    let html = text;
    
    // Convert headers
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
    
    // Convert - item thành list items
    html = html.replace(/^- (.+)$/gm, '• $1');
    
    return html;
  }
}
