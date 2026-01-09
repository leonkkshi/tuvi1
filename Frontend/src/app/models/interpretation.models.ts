import { TuViChart } from './tu-vi.models';

export interface InterpretationRequest {
  chart: TuViChart;
  apiKey: string;
  provider: 'Gemini';
}

export interface InterpretationResponse {
  overallInterpretation: string;
  palaceInterpretations: PalaceInterpretation[];
  keyInsights: string[];
  warnings: string[];
  recommendations: string[];
}

export interface PalaceInterpretation {
  palaceName: string;
  interpretation: string;
  influencingStars: string[];
}

// Re-export from existing models
export * from './tu-vi.models';
