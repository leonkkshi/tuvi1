import { Injectable } from '@angular/core';

// Import lunar calendar for use in services
declare const require: any;

@Injectable({
  providedIn: 'root'
})
export class LunarCalendarService {
  private lunar = require('vietnamese-lunar-calendar');

  convertSolar2Lunar(day: number, month: number, year: number): {
    lunarDay: number;
    lunarMonth: number;
    lunarYear: number;
    leapMonth: number;
  } {
    try {
      const { LunarDate } = this.lunar;
      const lunarDate = new LunarDate(year, month, day);

      return {
        lunarDay: lunarDate.date,
        lunarMonth: lunarDate.month,
        lunarYear: lunarDate.year,
        leapMonth: 0  // This library doesn't expose leap month info
      };
    } catch (error) {
      console.error('Lunar conversion error:', error);
      return {
        lunarDay: day,
        lunarMonth: month,
        lunarYear: year,
        leapMonth: 0
      };
    }
  }
}