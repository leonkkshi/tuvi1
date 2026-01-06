import { Injectable } from '@angular/core';

// Import thư viện chuyển đổi âm lịch
// @ts-ignore
import { LunarDate } from 'vietnamese-lunar-calendar';

export interface LunarDateResult {
  day: number;
  month: number;
  year: number;
}

@Injectable({
  providedIn: 'root'
})
export class LunarConverterService {

  /**
   * Chuyển đổi từ dương lịch sang âm lịch
   * @param solarDay Ngày dương lịch (1-31)
   * @param solarMonth Tháng dương lịch (1-12)
   * @param solarYear Năm dương lịch
   * @returns Ngày tháng năm âm lịch
   */
  convertSolarToLunar(solarDay: number, solarMonth: number, solarYear: number): LunarDateResult {
    try {
      const lunar = new LunarDate(solarYear, solarMonth, solarDay);
      return {
        day: lunar.date,
        month: lunar.month,
        year: lunar.year
      };
    } catch (error) {
      console.error('Error converting solar to lunar:', error);
      // Fallback: trả về ngày dương lịch nếu lỗi
      return {
        day: solarDay,
        month: solarMonth,
        year: solarYear
      };
    }
  }
}
