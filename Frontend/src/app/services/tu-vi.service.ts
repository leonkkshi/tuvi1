import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Palace, Star, TuViChart, ChartRequest } from '../models/tu-vi.models';
import { InterpretationRequest, InterpretationResponse } from '../models/interpretation.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TuViService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getPalaces(): Observable<Palace[]> {
    return this.http.get<Palace[]>(`${this.apiUrl}/palaces`);
  }

  getStars(): Observable<Star[]> {
    return this.http.get<Star[]>(`${this.apiUrl}/stars`);
  }

  generateChart(request: ChartRequest): Observable<TuViChart> {
    return this.http.post<TuViChart>(`${this.apiUrl}/generate-chart`, request);
  }

  interpretChart(request: InterpretationRequest): Observable<InterpretationResponse> {
    return this.http.post<InterpretationResponse>(`${this.apiUrl}/ai-interpret`, request);
  }

  interpretPalace(chart: TuViChart, palaceName: string, apiKey: string, provider: 'Gemini' | 'OpenAI'): Observable<PalaceInterpretationResult> {
    return this.http.post<PalaceInterpretationResult>(`${this.apiUrl}/ai-interpret-palace`, {
      Chart: chart,
      PalaceName: palaceName,
      ApiKey: apiKey,
      Provider: provider
    });
  }
}

export interface PalaceInterpretationResult {
  palaceName: string;
  interpretation: string;
  influencingStars: string[];
}
