import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChartRequest } from '../../models/tu-vi.models';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { LunarConverterService } from '../../services/lunar-converter.service';

@Component({
  selector: 'app-birth-form',
  imports: [CommonModule, FormsModule],
  templateUrl: './birth-form.component.html',
  styleUrl: './birth-form.component.css'
})
export class BirthFormComponent {
  @Output() chartGenerated = new EventEmitter<ChartRequest>();

  birthInfo: ChartRequest = {
    year: 2005,
    month: 8,
    day: 17,
    hour: 14,
    minute: 30,
    isMale: true,
    isLunar: true  // Luôn dùng âm lịch
  };

  fullName: string = 'Nguyễn Văn Tuấn';
  viewYear: number = 2026;
  selectedHourBranch: string = 'Mùi'; // Giờ 14:30
  errors: { [key: string]: string } = {};
  
  // Tab selection
  calendarType: 'lunar' | 'solar' = 'lunar';
  
  // Solar date inputs (for conversion)
  solarDate = {
    year: 2005,
    month: 8,
    day: 17
  };
  
  solarDays: number[] = [];
  lunarConversionText: string = '';

  // Danh sách giờ địa chi
  hourBranches = [
    { name: 'Tý', startHour: 23, endHour: 1, displayTime: '23:00 - 01:00' },
    { name: 'Sửu', startHour: 1, endHour: 3, displayTime: '01:00 - 03:00' },
    { name: 'Dần', startHour: 3, endHour: 5, displayTime: '03:00 - 05:00' },
    { name: 'Mão', startHour: 5, endHour: 7, displayTime: '05:00 - 07:00' },
    { name: 'Thìn', startHour: 7, endHour: 9, displayTime: '07:00 - 09:00' },
    { name: 'Tỵ', startHour: 9, endHour: 11, displayTime: '09:00 - 11:00' },
    { name: 'Ngọ', startHour: 11, endHour: 13, displayTime: '11:00 - 13:00' },
    { name: 'Mùi', startHour: 13, endHour: 15, displayTime: '13:00 - 15:00' },
    { name: 'Thân', startHour: 15, endHour: 17, displayTime: '15:00 - 17:00' },
    { name: 'Dậu', startHour: 17, endHour: 19, displayTime: '17:00 - 19:00' },
    { name: 'Tuất', startHour: 19, endHour: 21, displayTime: '19:00 - 21:00' },
    { name: 'Hợi', startHour: 21, endHour: 23, displayTime: '21:00 - 23:00' }
  ];

  // Danh sách năm gần đây
  years: number[] = [];
  months: number[] = Array.from({ length: 12 }, (_, i) => i + 1);
  days: number[] = Array.from({ length: 30 }, (_, i) => i + 1);

  constructor(
    private http: HttpClient,
    private lunarConverter: LunarConverterService
  ) {
    // Tạo danh sách năm từ 1920 đến hiện tại + 10 năm
    const currentYear = new Date().getFullYear();
    for (let year = currentYear + 10; year >= 1920; year--) {
      this.years.push(year);
    }
    this.updateDaysInMonth();
    this.updateSolarDaysInMonth();
    // Đặt giờ phút mặc định từ địa chi đã chọn
    this.onHourBranchChange();
  }

  switchCalendarType(type: 'lunar' | 'solar') {
    this.calendarType = type;
    this.birthInfo.isLunar = (type === 'lunar');
    
    if (type === 'solar') {
      // Sync solarDate với birthInfo
      this.solarDate.year = this.birthInfo.year;
      this.solarDate.month = this.birthInfo.month;
      this.solarDate.day = this.birthInfo.day;
      this.updateSolarDaysInMonth();
    }
  }

  onSolarDateChange() {
    this.updateSolarDaysInMonth();
    
    // Chuyển đổi dương lịch sang âm lịch ngay ở frontend
    const lunarResult = this.lunarConverter.convertSolarToLunar(
      this.solarDate.day,
      this.solarDate.month,
      this.solarDate.year
    );
    
    // Cập nhật birthInfo với ngày dương lịch (gửi lên backend)
    this.birthInfo.year = this.solarDate.year;
    this.birthInfo.month = this.solarDate.month;
    this.birthInfo.day = this.solarDate.day;
    
    // Hiển thị thông tin âm lịch tương ứng
    const leapText = lunarResult.isLeapMonth ? ' (Nhuận)' : '';
    this.lunarConversionText = `Âm lịch: ${lunarResult.day}/${lunarResult.month}/${lunarResult.year}${leapText}`;
  }

  updateSolarDaysInMonth() {
    const daysInMonth = new Date(this.solarDate.year, this.solarDate.month, 0).getDate();
    this.solarDays = Array.from({ length: daysInMonth }, (_, i) => i + 1);
    if (this.solarDate.day > daysInMonth) {
      this.solarDate.day = daysInMonth;
    }
  }

  onHourBranchChange() {
    const branch = this.hourBranches.find(b => b.name === this.selectedHourBranch);
    if (branch) {
      // Lấy giờ giữa khoảng
      this.birthInfo.hour = branch.startHour === 23 ? 0 : branch.startHour + 1;
      this.birthInfo.minute = 0;
    }
  }

  onMonthChange() {
    this.updateDaysInMonth();
  }

  updateDaysInMonth() {
    // Âm lịch luôn có 30 ngày
    this.days = Array.from({ length: 30 }, (_, i) => i + 1);
    if (this.birthInfo.day > 30) {
      this.birthInfo.day = 30;
    }
  }

  validateForm(): boolean {
    this.errors = {};
    let isValid = true;

    if (!this.fullName || this.fullName.trim().length < 2) {
      this.errors['fullName'] = 'Vui lòng nhập họ tên (ít nhất 2 ký tự)';
      isValid = false;
    }

    if (!this.birthInfo.year || this.birthInfo.year < 1900 || this.birthInfo.year > 2100) {
      this.errors['year'] = 'Năm sinh không hợp lệ';
      isValid = false;
    }

    if (!this.birthInfo.month || this.birthInfo.month < 1 || this.birthInfo.month > 12) {
      this.errors['month'] = 'Tháng không hợp lệ';
      isValid = false;
    }

    if (!this.birthInfo.day || this.birthInfo.day < 1 || this.birthInfo.day > 31) {
      this.errors['day'] = 'Ngày không hợp lệ';
      isValid = false;
    }

    if (!this.selectedHourBranch) {
      this.errors['hourBranch'] = 'Vui lòng chọn giờ sinh';
      isValid = false;
    }

    if (!this.viewYear || this.viewYear < 1900 || this.viewYear > 2100) {
      this.errors['viewYear'] = 'Năm xem không hợp lệ';
      isValid = false;
    }

    return isValid;
  }

  onSubmit() {
    if (this.validateForm()) {
      const request = {
        ...this.birthInfo,
        fullName: this.fullName
      };
      this.chartGenerated.emit(request);
    }
  }

  clearForm() {
    this.birthInfo = {
      year: 2005,
      month: 1,
      day: 1,
      hour: 0,
      minute: 0,
      isMale: true,
      isLunar: true
    };
    this.fullName = '';
    this.viewYear = new Date().getFullYear();
    this.selectedHourBranch = 'Tý';
    this.errors = {};
    // Đặt lại giờ phút từ địa chi
    this.onHourBranchChange();
  }
}
