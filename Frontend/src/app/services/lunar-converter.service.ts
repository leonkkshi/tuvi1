import { Injectable } from '@angular/core';

export interface LunarDateResult {
  day: number;
  month: number;
  year: number;
  isLeapMonth: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class LunarConverterService {

  /**
   * Thuật toán chuyển đổi âm dương lịch của Hồ Ngọc Đức
   * Nguồn: https://www.informatik.uni-leipzig.de/~duc/amlich/
   */

  private readonly PI = Math.PI;

  private jdFromDate(dd: number, mm: number, yy: number): number {
    const a = Math.floor((14 - mm) / 12);
    const y = yy + 4800 - a;
    const m = mm + 12 * a - 3;
    let jd = dd + Math.floor((153 * m + 2) / 5) + 365 * y + Math.floor(y / 4) - Math.floor(y / 100) + Math.floor(y / 400) - 32045;
    if (jd < 2299161) {
      jd = dd + Math.floor((153 * m + 2) / 5) + 365 * y + Math.floor(y / 4) - 32083;
    }
    return jd;
  }

  private jdToDate(jd: number): { day: number; month: number; year: number } {
    let a, b, c;
    if (jd > 2299160) {
      a = jd + 32044;
      b = Math.floor((4 * a + 3) / 146097);
      c = a - Math.floor((b * 146097) / 4);
    } else {
      b = 0;
      c = jd + 32082;
    }
    const d = Math.floor((4 * c + 3) / 1461);
    const e = c - Math.floor((1461 * d) / 4);
    const m = Math.floor((5 * e + 2) / 153);
    const day = e - Math.floor((153 * m + 2) / 5) + 1;
    const month = m + 3 - 12 * Math.floor(m / 10);
    const year = b * 100 + d - 4800 + Math.floor(m / 10);
    return { day, month, year };
  }

  private NewMoon(k: number): number {
    const T = k / 1236.85;
    const T2 = T * T;
    const T3 = T2 * T;
    const dr = this.PI / 180;
    let Jd1 = 2415020.75933 + 29.53058868 * k + 0.0001178 * T2 - 0.000000155 * T3;
    Jd1 = Jd1 + 0.00033 * Math.sin((166.56 + 132.87 * T - 0.009173 * T2) * dr);
    const M = 359.2242 + 29.10535608 * k - 0.0000333 * T2 - 0.00000347 * T3;
    const Mpr = 306.0253 + 385.81691806 * k + 0.0107306 * T2 + 0.00001236 * T3;
    const F = 21.2964 + 390.67050646 * k - 0.0016528 * T2 - 0.00000239 * T3;
    let C1 = (0.1734 - 0.000393 * T) * Math.sin(M * dr) + 0.0021 * Math.sin(2 * dr * M);
    C1 = C1 - 0.4068 * Math.sin(Mpr * dr) + 0.0161 * Math.sin(dr * 2 * Mpr);
    C1 = C1 - 0.0004 * Math.sin(dr * 3 * Mpr);
    C1 = C1 + 0.0104 * Math.sin(dr * 2 * F) - 0.0051 * Math.sin(dr * (M + Mpr));
    C1 = C1 - 0.0074 * Math.sin(dr * (M - Mpr)) + 0.0004 * Math.sin(dr * (2 * F + M));
    C1 = C1 - 0.0004 * Math.sin(dr * (2 * F - M)) - 0.0006 * Math.sin(dr * (2 * F + Mpr));
    C1 = C1 + 0.001 * Math.sin(dr * (2 * F - Mpr)) + 0.0005 * Math.sin(dr * (2 * Mpr + M));
    let deltat;
    if (T < -11) {
      deltat = 0.001 + 0.000839 * T + 0.0002261 * T2 - 0.00000845 * T3 - 0.000000081 * T * T3;
    } else {
      deltat = -0.000278 + 0.000265 * T + 0.000262 * T2;
    }
    const JdNew = Jd1 + C1 - deltat;
    return JdNew;
  }

  private getSunLongitude(jdn: number, timeZone: number): number {
    const T = (jdn - 2451545.5 - timeZone / 24) / 36525;
    const T2 = T * T;
    const dr = this.PI / 180;
    const M = 357.5291 + 35999.0503 * T - 0.0001559 * T2 - 0.00000048 * T * T2;
    const L0 = 280.46645 + 36000.76983 * T + 0.0003032 * T2;
    let DL = (1.9146 - 0.004817 * T - 0.000014 * T2) * Math.sin(dr * M);
    DL = DL + (0.019993 - 0.000101 * T) * Math.sin(dr * 2 * M) + 0.00029 * Math.sin(dr * 3 * M);
    let L = L0 + DL;
    L = L * dr;
    L = L - this.PI * 2 * Math.floor(L / (this.PI * 2));
    return Math.floor((L / this.PI) * 6);
  }

  private getNewMoonDay(k: number, timeZone: number): number {
    return Math.floor(this.NewMoon(k) + 0.5 + timeZone / 24);
  }

  private getLunarMonth11(yy: number, timeZone: number): number {
    const off = this.jdFromDate(31, 12, yy) - 2415021;
    const k = Math.floor(off / 29.530588853);
    let nm = this.getNewMoonDay(k, timeZone);
    const sunLong = this.getSunLongitude(nm, timeZone);
    if (sunLong >= 9) {
      nm = this.getNewMoonDay(k - 1, timeZone);
    }
    return nm;
  }

  private getLeapMonthOffset(a11: number, timeZone: number): number {
    const k = Math.floor((a11 - 2415021.076998695) / 29.530588853 + 0.5);
    let last = 0;
    let i = 1;
    let arc = this.getSunLongitude(this.getNewMoonDay(k + i, timeZone), timeZone);
    do {
      last = arc;
      i++;
      arc = this.getSunLongitude(this.getNewMoonDay(k + i, timeZone), timeZone);
    } while (arc !== last && i < 14);
    return i - 1;
  }

  /**
   * Chuyển đổi từ dương lịch sang âm lịch
   * @param dd Ngày dương lịch (1-31)
   * @param mm Tháng dương lịch (1-12)
   * @param yy Năm dương lịch
   * @returns Ngày tháng năm âm lịch
   */
  convertSolarToLunar(dd: number, mm: number, yy: number): LunarDateResult {
    try {
      const timeZone = 7; // GMT+7 cho Việt Nam
      const dayNumber = this.jdFromDate(dd, mm, yy);
      const k = Math.floor((dayNumber - 2415021.076998695) / 29.530588853);
      let monthStart = this.getNewMoonDay(k + 1, timeZone);
      
      if (monthStart > dayNumber) {
        monthStart = this.getNewMoonDay(k, timeZone);
      }
      
      let a11 = this.getLunarMonth11(yy, timeZone);
      let b11 = a11;
      let lunarYear: number;
      
      if (a11 >= monthStart) {
        lunarYear = yy;
        a11 = this.getLunarMonth11(yy - 1, timeZone);
      } else {
        lunarYear = yy + 1;
        b11 = this.getLunarMonth11(yy + 1, timeZone);
      }
      
      const lunarDay = dayNumber - monthStart + 1;
      const diff = Math.floor((monthStart - a11) / 29);
      let lunarLeap = false;
      let lunarMonth = diff + 11;
      
      if (b11 - a11 > 365) {
        const leapMonthDiff = this.getLeapMonthOffset(a11, timeZone);
        if (diff >= leapMonthDiff) {
          lunarMonth = diff + 10;
          if (diff === leapMonthDiff) {
            lunarLeap = true;
          }
        }
      }
      
      if (lunarMonth > 12) {
        lunarMonth = lunarMonth - 12;
      }
      if (lunarMonth >= 11 && diff < 4) {
        lunarYear -= 1;
      }
      
      return {
        day: Math.floor(lunarDay),
        month: Math.floor(lunarMonth),
        year: Math.floor(lunarYear),
        isLeapMonth: lunarLeap
      };
    } catch (error) {
      console.error('Error converting solar to lunar:', error);
      return {
        day: dd,
        month: mm,
        year: yy,
        isLeapMonth: false
      };
    }
  }
}
