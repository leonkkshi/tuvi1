export interface Palace {
  id: number;
  name: string;
  description: string;
  position: number;
}

export interface Star {
  id: number;
  name: string;
  type: string;
  element: string;
  nature: string;
  description: string;
  brightness: number;
}

export interface StarInPalace {
  starId: number;
  starName: string;
  brightness: number;
  element: string; // Kim, Mộc, Thủy, Hỏa, Thổ
  nature: string; // Cát, Hung
  type: string; // Chính tinh, Phụ tinh, etc.
  hoa?: string; // Hóa Lộc, Hóa Quyền, Hóa Khoa, Hóa Kỵ (optional)
}

export interface PalaceStar {
  palaceId: number;
  palaceName: string;
  stars: StarInPalace[];
  hasTuan: boolean; // Đánh dấu cung có Tuần
  hasTriet: boolean; // Đánh dấu cung có Triệt
}

export interface TuViChart {
  id: string;
  fullName: string;
  birthDate: string;
  birthTime: string;
  isMale: boolean;
  lunarYear: string;
  lunarMonth: string;
  lunarDay: string;
  nguHanhCuc: number; // 2=Thủy Nhị, 3=Mộc Tam, 4=Kim Tứ, 5=Thổ Ngũ, 6=Hỏa Lục
  amDuong: string; // Dương Nam, Âm Nam, Dương Nữ, Âm Nữ
  thanPalace: number; // Cung Thân (1-12)
  palaceStars: PalaceStar[];
  trietBetween: string; // VD: "Thân-Dậu" (2 cung)
  tuanPositions: string; // VD: "Thân,Dậu" (2 cung)
  daiVan: { [key: number]: number }; // Key = Palace Position (1-12), Value = Tuổi bắt đầu Đại Vận
  tieuHan: { [key: number]: string }; // Key = Palace Position (1-12), Value = Chi năm tiểu hạn
  nguyetHan: { [key: number]: number }; // Key = Palace Position (1-12), Value = Tháng âm lịch
}

export interface ChartRequest {
  fullName?: string;
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
  isMale: boolean;
  isLunar: boolean;
  viewYear?: number; // Năm xem hạn
}
