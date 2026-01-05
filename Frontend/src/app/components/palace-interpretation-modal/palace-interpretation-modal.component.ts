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
  @Output() close = new EventEmitter<void>();
  @Output() interpretationLoaded = new EventEmitter<PalaceInterpretationResult>();

  interpretation: PalaceInterpretationResult | null = null;
  isLoading: boolean = false;
  error: string = '';
  private lastLoadedPalaceName: string = '';

  constructor(private tuViService: TuViService) {}

  ngOnChanges(changes: SimpleChanges) {
    // Chỉ xử lý khi modal được mở và có palace
    if (!this.isOpen || !this.chart || !this.palace) {
      return;
    }

    // Kiểm tra nếu palace thay đổi hoặc modal mới mở
    const palaceChanged = changes['palace'] && this.palace;
    const modalOpened = changes['isOpen'] && this.isOpen;

    if (palaceChanged || modalOpened) {
      // Kiểm tra có cached interpretation không
      if (this.cachedInterpretation && this.cachedInterpretation.PalaceName === this.palace.palaceName) {
        // Dùng cache
        this.interpretation = this.cachedInterpretation;
        this.isLoading = false;
        this.error = '';
        this.lastLoadedPalaceName = this.palace.palaceName;
      } else if (this.lastLoadedPalaceName !== this.palace.palaceName && !this.isLoading) {
        // Chỉ load nếu chưa load cung này và không đang loading
        this.loadInterpretation();
      }
    }
  }

  loadInterpretation() {
    if (!this.chart || !this.palace || this.isLoading) return;

    const palaceName = this.palace.palaceName;
    this.lastLoadedPalaceName = palaceName;
    this.isLoading = true;
    this.error = '';
    this.interpretation = null;

    console.log(`[Modal] Loading interpretation for: ${palaceName}`);

    this.tuViService.interpretPalace(this.chart, palaceName).subscribe({
      next: (result) => {
        console.log(`[Modal] Received result:`, result);
        // Chỉ cập nhật nếu vẫn đang xem cùng cung
        if (this.palace && this.palace.palaceName === palaceName) {
          this.interpretation = result;
          this.isLoading = false;
          // Emit để parent component cache lại
          this.interpretationLoaded.emit(result);
          console.log(`[Modal] Set interpretation, isLoading=${this.isLoading}, hasInterpretation=${!!this.interpretation}`);
        }
      },
      error: (err) => {
        console.error('[Modal] Error loading palace interpretation:', err);
        if (this.palace && this.palace.palaceName === palaceName) {
          this.error = 'Không thể tải luận giải cho cung này. Vui lòng thử lại.';
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
