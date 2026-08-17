import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from './api.config';
import { Reclamation, CreateReclamationRequest, RespondReclamationRequest, ReclamationStatus } from '../models/reclamation.models';
import { PaginatedResponse } from '../models/article.models';

@Injectable({ providedIn: 'root' })
export class ReclamationService {
    constructor(private http: HttpClient) { }

    createReclamation(request: CreateReclamationRequest): Observable<Reclamation> {
        return this.http.post<Reclamation>(`${API_URL}/reclamations`, request);
    }

    getMyReclamations(): Observable<Reclamation[]> {
        return this.http.get<Reclamation[]>(`${API_URL}/reclamations/me`);
    }

    getMyReclamation(id: number): Observable<Reclamation> {
        return this.http.get<Reclamation>(`${API_URL}/reclamations/me/${id}`);
    }

    getReclamations(params?: {
        status?: ReclamationStatus;
        page?: number;
        pageSize?: number;
    }): Observable<PaginatedResponse<Reclamation>> {
        let httpParams = new HttpParams();
        if (params) {
            if (params.status) httpParams = httpParams.set('status', params.status);
            if (params.page) httpParams = httpParams.set('page', params.page);
            if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
        }
        return this.http.get<PaginatedResponse<Reclamation>>(`${API_URL}/reclamations`, { params: httpParams });
    }

    getReclamation(id: number): Observable<Reclamation> {
        return this.http.get<Reclamation>(`${API_URL}/reclamations/${id}`);
    }

    respondReclamation(id: number, request: RespondReclamationRequest): Observable<void> {
        return this.http.put<void>(`${API_URL}/reclamations/${id}`, request);
    }

    deleteReclamation(id: number): Observable<void> {
        return this.http.delete<void>(`${API_URL}/reclamations/${id}`);
    }
}
