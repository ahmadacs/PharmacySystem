import { Component, input, output, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [MatButton, MatIcon, MatProgressBar],
  template: `
    <div class="upload-zone" [class.drag-over]="dragOver()" (dragover)="onDragOver($event)" (dragleave)="dragOver.set(false)" (drop)="onDrop($event)">
      <mat-icon>cloud_upload</mat-icon>
      <p>Drag & drop or click to select (jpeg/png/pdf, max 5MB)</p>
      <input #input type="file" hidden [accept]="accept()" (change)="onSelected($event)" />
      <button mat-stroked-button type="button" (click)="input.click()" [disabled]="uploading()">Select file</button>
      @if (selectedName()) {
        <p class="file-name">{{ selectedName() }} ({{ selectedSizeKb() }} KB)</p>
      }
      @if (uploading()) {
        <mat-progress-bar mode="indeterminate"></mat-progress-bar>
      }
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
    </div>
  `,
  styles: [`
    .upload-zone { border: 2px dashed #ccc; border-radius: 8px; padding: 24px; text-align: center; display: flex; flex-direction: column; align-items: center; gap: 8px; }
    .upload-zone.drag-over { border-color: #1976d2; background: #e3f2fd; }
    .file-name { font-size: 12px; color: #555; }
    .error { color: #d32f2f; font-size: 12px; }
  `]
})
export class FileUploadComponent {
  accept = input<string>('image/jpeg,image/png,application/pdf');
  maxSizeBytes = input<number>(5 * 1024 * 1024);
  fileSelected = output<File>();

  dragOver = signal(false);
  uploading = signal(false);
  selectedName = signal<string | null>(null);
  selectedSizeKb = signal<number>(0);
  error = signal<string | null>(null);

  onDragOver(e: DragEvent) { e.preventDefault(); this.dragOver.set(true); }
  onDrop(e: DragEvent) {
    e.preventDefault(); this.dragOver.set(false);
    const file = e.dataTransfer?.files[0];
    if (file) this.validate(file);
  }
  onSelected(e: Event) {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file) this.validate(file);
  }

  private validate(file: File) {
    this.error.set(null);
    const allowed = ['image/jpeg', 'image/png', 'application/pdf'];
    if (!allowed.includes(file.type)) { this.error.set('Only jpeg, png, pdf allowed.'); return; }
    if (file.size > this.maxSizeBytes()) { this.error.set('File exceeds 5MB.'); return; }
    this.selectedName.set(file.name);
    this.selectedSizeKb.set(Math.round(file.size / 1024));
    this.fileSelected.emit(file);
  }
}
