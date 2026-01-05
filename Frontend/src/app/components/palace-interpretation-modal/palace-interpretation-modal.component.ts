import { Component, EventEmitter, Input, Output } from '@angular/core';
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
export class PalaceInterpretationModalComponent {
  @Input() chart: TuViChart | null = null;
  @Input() palace: PalaceStar | null = null;
  @Input() isOpen: boolean = false;
  @Output() close = new EventEmitter<void>();

  interpretation: PalaceInterpretationResult | null = null;
  isLoading: boolean = false;
  error: string = '';

  constructor(private tuViService: TuViService) {}

  ngOnChanges() {
    if (this.isOpen && this.chart && this.palace && !this.interpretation) {
      this.loadInterpretation();
    }
  }

  loadInterpretation() {
    if (!this.chart || !this.palace) return;

    this.isLoading = true;
    this.error = '';

    this.tuViService.interpretPalace(this.chart, this.palace.palaceName).subscribe({
      next: (result) => {
        this.interpretation = result;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading palace interpretation:', err);
        this.error = 'Không thể tải luận giải cho cung này. Vui lòng thử lại.';
        this.isLoading = false;
      }
    });
  }

  closeModal() {
    this.interpretation = null;
    this.error = '';
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
