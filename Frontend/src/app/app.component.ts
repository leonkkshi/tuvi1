import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { BirthFormComponent } from './components/birth-form/birth-form.component';
import { TuViChartComponent } from './components/tu-vi-chart/tu-vi-chart.component';
import { TuViService } from './services/tu-vi.service';
import { ChartRequest, TuViChart } from './models/tu-vi.models';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, BirthFormComponent, TuViChartComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  providers: [TuViService]
})
export class AppComponent implements OnInit {
  title = 'Tử Vi Đẩu Số';
  chart: TuViChart | null = null;
  loading = false;
  error: string | null = null;

  greetings = [
    'Năm mới Bính Ngọ - Mã Đáo Thành Công 🐎',
    'Tấn Tài Tấn Lộc - Vạn Sự Như Ý 🧧',
    'Phúc Lộc Thọ Tài - An Khang Thịnh Vượng 🌸',
    'Cung Chúc Tân Xuân - Tiền Vào Như Nước 💰',
    'Xuân Sang Đắc Lộc - Gia Đạo Bình An 🏠'
  ];
  currentGreeting = this.greetings[0];

  constructor(private tuViService: TuViService) {}

  ngOnInit() {
    let i = 0;
    setInterval(() => {
      i = (i + 1) % this.greetings.length;
      this.currentGreeting = this.greetings[i];
    }, 4000);
  }

  onChartGenerated(request: ChartRequest) {
    this.loading = true;
    this.error = null;
    
    this.tuViService.generateChart(request).subscribe({
      next: (chart) => {
        this.chart = chart;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Không thể tạo lá số. Vui lòng kiểm tra kết nối API.';
        this.loading = false;
        console.error('Error generating chart:', err);
      }
    });
  }
}
