import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning';

export interface ToastMessage {
  id: number;
  type: ToastType;
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly messages = signal<ToastMessage[]>([]);
  private nextId = 1;

  success(text: string): void {
    this.show('success', text);
  }

  error(text: string): void {
    this.show('error', text);
  }

  warning(text: string): void {
    this.show('warning', text);
  }

  dismiss(id: number): void {
    this.messages.update((list) => list.filter((m) => m.id !== id));
  }

  private show(type: ToastType, text: string): void {
    const id = this.nextId++;
    this.messages.update((list) => [...list, { id, type, text }]);
    setTimeout(() => this.dismiss(id), 3500);
  }
}
