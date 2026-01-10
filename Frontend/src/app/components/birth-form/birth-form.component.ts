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
    year: 2008,
    month: 5,
    day: 15,
    hour: 10,
    minute: 0,
    isMale: true,
    isLunar: true  // Luôn dùng âm lịch
  };

  fullName: string = 'Tử Vi';
  viewYear: number = 2026;
  displayHourBranchName: string = '';  // Hiển thị địa chi từ giờ nhập vào
  errors: { [key: string]: string } = {};
  
  // Tab selection
  calendarType: 'lunar' | 'solar' = 'lunar';
  
  // Track trạng thái giờ Tý để xử lý âm lịch
  private lastHourWasNight: boolean = false;  // True = giờ Tý (23h-1h)
  private lastLunarDay: number = 15;  // Lưu lại ngày âm lịch gốc
  private lastLunarMonth: number = 5;
  private lastLunarYear: number = 2008;
  
  // Solar date inputs (for conversion)
  solarDate = {
    year: 2008,
    month: 5,
    day: 15
  };
  
  solarDays: number[] = [];
  lunarConversionText: string = '';

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
    // Update display địa chi từ giờ mặc định
    this.updateDisplayHourBranch();
  }

  switchCalendarType(type: 'lunar' | 'solar') {
    this.calendarType = type;
    this.birthInfo.isLunar = (type === 'lunar');
    
    // Reset tracking state khi chuyển tab
    this.lastHourWasNight = false;
    
    if (type === 'solar') {
      // Sync solarDate với birthInfo
      this.solarDate.year = this.birthInfo.year;
      this.solarDate.month = this.birthInfo.month;
      this.solarDate.day = this.birthInfo.day;
      this.updateSolarDaysInMonth();
    } else if (type === 'lunar') {
      // Cập nhật tracking state khi quay về âm lịch
      this.lastLunarDay = this.birthInfo.day;
      this.lastLunarMonth = this.birthInfo.month;
      this.lastLunarYear = this.birthInfo.year;
    }
  }

  onSolarDateChange() {
    this.updateSolarDaysInMonth();
    this.updateSolarToLunarConversion();
  }

  /**
   * Thực hiện chuyển đổi từ dương lịch sang âm lịch, có xét đến giờ Tý
   */
  private updateSolarToLunarConversion() {
    // Đảm bảo các giá trị là number (vì select trả về string)
    const day = Number(this.solarDate.day);
    const month = Number(this.solarDate.month);
    const year = Number(this.solarDate.year);
    
    // Chuyển đổi dương lịch sang âm lịch với xét đến giờ sinh
    // FIX: Khi sinh giờ Tý (23h-1h), ngày âm lịch phải cộng thêm 1
    const lunarResult = this.lunarConverter.convertSolarToLunarWithHour(day, month, year, this.birthInfo.hour);
    
    // CẬP NHẬT birthInfo với ngày ÂM LỊCH đã chuyển đổi
    // Vì backend trên production không có Node.js để chuyển đổi
    this.birthInfo.year = lunarResult.year;
    this.birthInfo.month = lunarResult.month;
    this.birthInfo.day = lunarResult.day;
    
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

  /**
   * Xử lý khi giờ/phút thay đổi
   */
  onTimeChange() {
    // Validate giờ phút
    this.birthInfo.hour = Math.max(0, Math.min(23, this.birthInfo.hour || 0));
    this.birthInfo.minute = Math.max(0, Math.min(59, this.birthInfo.minute || 0));
    
    // Update display địa chi
    this.updateDisplayHourBranch();
    
    // Xử lý giờ Tý (23h-1h)
    const isNightHour = this.birthInfo.hour >= 23 || this.birthInfo.hour < 1;
    
    if (this.calendarType === 'solar') {
      // Dương lịch: cập nhật conversion sang âm lịch
      this.updateSolarToLunarConversion();
    } else if (this.calendarType === 'lunar') {
      // Âm lịch: xử lý giờ Tý
      this.handleLunarHourChange(isNightHour);
    }
  }

  /**
   * Xử lý thay đổi giờ cho âm lịch
   * Khi giờ Tý (23h-1h), ngày âm lịch phải cộng thêm 1
   */
  private handleLunarHourChange(isNightHour: boolean) {
    // Nếu trạng thái giờ Tý không thay đổi, không cần xử lý
    if (isNightHour === this.lastHourWasNight) {
      return;
    }

    // Trạng thái giờ Tý đã thay đổi
    if (isNightHour) {
      // Chuyển từ giờ khác sang giờ Tý -> cộng 1 ngày
      // Lưu ngày gốc trước khi cộng
      this.lastLunarDay = this.birthInfo.day;
      this.lastLunarMonth = this.birthInfo.month;
      this.lastLunarYear = this.birthInfo.year;

      // Cộng 1 ngày
      this.birthInfo.day++;
      if (this.birthInfo.day > 30) {
        this.birthInfo.day = 1;
        this.birthInfo.month++;
        if (this.birthInfo.month > 12) {
          this.birthInfo.month = 1;
          this.birthInfo.year++;
        }
      }
    } else {
      // Chuyển từ giờ Tý sang giờ khác -> trừ 1 ngày (khôi phục)
      this.birthInfo.day = this.lastLunarDay;
      this.birthInfo.month = this.lastLunarMonth;
      this.birthInfo.year = this.lastLunarYear;
    }

    this.lastHourWasNight = isNightHour;
  }
  
  /**
   * Cập nhật hiển thị địa chi từ giờ sinh
   */
  private updateDisplayHourBranch() {
    const hour = this.birthInfo.hour;
    
    // Tìm địa chi dựa vào giờ
    if (hour >= 23 || hour < 1) {
      this.displayHourBranchName = 'Tý';
    } else if (hour >= 1 && hour < 3) {
      this.displayHourBranchName = 'Sửu';
    } else if (hour >= 3 && hour < 5) {
      this.displayHourBranchName = 'Dần';
    } else if (hour >= 5 && hour < 7) {
      this.displayHourBranchName = 'Mão';
    } else if (hour >= 7 && hour < 9) {
      this.displayHourBranchName = 'Thìn';
    } else if (hour >= 9 && hour < 11) {
      this.displayHourBranchName = 'Tỵ';
    } else if (hour >= 11 && hour < 13) {
      this.displayHourBranchName = 'Ngọ';
    } else if (hour >= 13 && hour < 15) {
      this.displayHourBranchName = 'Mùi';
    } else if (hour >= 15 && hour < 17) {
      this.displayHourBranchName = 'Thân';
    } else if (hour >= 17 && hour < 19) {
      this.displayHourBranchName = 'Dậu';
    } else if (hour >= 19 && hour < 21) {
      this.displayHourBranchName = 'Tuất';
    } else if (hour >= 21 && hour < 23) {
      this.displayHourBranchName = 'Hợi';
    } else {
      this.displayHourBranchName = '';
    }
  }

  onMonthChange() {
    this.updateDaysInMonth();
    // Reset tracking state khi thay đổi ngày/tháng/năm
    this.resetLunarTracking();
  }

  updateDaysInMonth() {
    // Âm lịch luôn có 30 ngày
    this.days = Array.from({ length: 30 }, (_, i) => i + 1);
    if (this.birthInfo.day > 30) {
      this.birthInfo.day = 30;
    }
    // Reset tracking state khi thay đổi ngày
    this.resetLunarTracking();
  }

  /**
   * Reset tracking state cho âm lịch
   * Gọi khi user thay đổi ngày/tháng/năm âm lịch
   */
  resetLunarTracking() {
    if (this.calendarType === 'lunar') {
      this.lastLunarDay = this.birthInfo.day;
      this.lastLunarMonth = this.birthInfo.month;
      this.lastLunarYear = this.birthInfo.year;
      // Reset flag giờ Tý
      this.lastHourWasNight = false;
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

    if (this.birthInfo.hour === null || this.birthInfo.hour === undefined || 
        this.birthInfo.hour < 0 || this.birthInfo.hour > 23) {
      this.errors['hour'] = 'Giờ sinh không hợp lệ (0-23)';
      isValid = false;
    }
    
    if (this.birthInfo.minute === null || this.birthInfo.minute === undefined || 
        this.birthInfo.minute < 0 || this.birthInfo.minute > 59) {
      this.errors['minute'] = 'Phút sinh không hợp lệ (0-59)';
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
      // Khi ở tab dương lịch: birthInfo đã được chuyển đổi sang âm lịch trong onSolarDateChange
      // Vì vậy luôn gửi isLunar = true (backend không cần chuyển đổi nữa)
      const request = {
        year: Number(this.birthInfo.year),
        month: Number(this.birthInfo.month),
        day: Number(this.birthInfo.day),
        hour: Number(this.birthInfo.hour),
        minute: Number(this.birthInfo.minute),
        isMale: this.birthInfo.isMale,
        isLunar: true, // Luôn gửi true vì đã chuyển đổi ở frontend
        fullName: this.fullName,
        viewYear: Number(this.viewYear)
      };
      console.log('Submitting request:', request); // Debug
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
    this.errors = {};
    // Update display địa chi
    this.updateDisplayHourBranch();
  }
}