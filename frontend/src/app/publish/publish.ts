import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { MessageService } from '../message';
import { Details } from '../details';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-publish',
  standalone: true,
  imports: [FormsModule, CommonModule, HttpClientModule],
  templateUrl: './publish.html',
  styleUrls: ['./publish.css']
})
export class Publish {
  type: string = '';
  vmsIntegration: string = '';
  id: string = '';
  customerId: string = '';
  videoIntegrationType: string = '';
  sequenceNumber: string = '';
  deviceIds: string = '';
  isScheduledTab: boolean = false;
  isScheduledMode = signal(false);
  interval: number = 1
  duration: number = 1; 

  messageStatus: string = '';

  constructor(
    private messageService: MessageService,
    private toastr: ToastrService,
    private http: HttpClient
  ) { }

  publishMessage() {

    const nestedField2 = {
      id: this.id,
      customerId: this.customerId,
      videoIntegrationType: this.videoIntegrationType
    }
    const details: Details = {
      type: this.type,
      vmsIntegration: nestedField2,
      sequenceNumber: this.sequenceNumber,
      deviceIds: this.deviceIds
    };

    this.messageService.publish(details).subscribe({
      next: response => {
        this.toastr.success('Message published successfully.', 'Success!', {
          positionClass: 'toast-bottom-right',
          progressBar: true
        });
        this.messageStatus = 'Message published successfully!';
      },
      error: error => {
        const errorMessage = `Failed to publish message: ${error.message || 'Unknown Error'}`
        this.messageService.logError(errorMessage).subscribe();
        this.toastr.error('Message was unable to be published.', 'Error',{
          positionClass: 'toast-bottom-right',
          progressBar: true
        });
        this.messageStatus = 'Failed to publish message.';
      }
    });
  }

  fillWithDefaults() {
    this.type = 'REFRESH';
    this.id = '2c5924a6-3854-40b5-b402-77e1e507af07';
    this.customerId = 'aware.onprem';
    this.videoIntegrationType = 'SALIENTCOMPLETEVIEW';
    this.sequenceNumber = "1703247656355";
  }

  clearInputs() {
    this.type = '';
    this.id = '';
    this.customerId = '';
    this.videoIntegrationType = '';
    this.sequenceNumber = '';
    this.deviceIds = '';
  }

  stopScheduling() {
    console.log('[Publish] stopScheduling() called');

    // POST directly to your backend endpoint (use the curl URL you provided)
    const url = 'https://localhost:7268/Message/stop';

    this.http.post<{ message?: string }>(url, null).subscribe({
      next: (resp) => {
        console.log('[Publish] stop next:', resp);
        const msg = (resp && (resp as any).message) ?? 'Schedule stopped.';
        this.toastr.info(msg, 'Stopped', { positionClass: 'toast-bottom-right', progressBar: true });
        this.messageStatus = msg;
        try { this.isScheduledMode.set(false); } catch { /* noop */ }
      },
      error: (err) => {
        console.error('[Publish] stop error:', err);
        const serverMsg = err?.error?.message ?? err?.message ?? 'Unknown error';
        if (err?.status === 400) {
          this.toastr.warning(serverMsg, 'Nothing to stop', { positionClass: 'toast-bottom-right', progressBar: true });
          this.messageStatus = `No active schedule: ${serverMsg}`;
        } else {
          this.toastr.error(serverMsg, 'Error', { positionClass: 'toast-bottom-right', progressBar: true });
          this.messageStatus = `Failed to stop schedule (${err?.status || 'no-status'})`;
        }
      },
      complete: () => console.log('[Publish] stop complete')
    });
  }
  scheduleMessage() {
    const nestedField2 = {
      id: this.id,
      customerId: this.customerId,
      videoIntegrationType: this.videoIntegrationType
    };

    const payload = {
      details: {
        type: this.type,
        vmsIntegration: nestedField2,
        sequenceNumber: this.sequenceNumber,
        deviceIds: this.deviceIds
      },
      intervalMinutes: Number(this.interval),
      durationHours: Number(this.duration) // <-- added field
    };

    // Log the payload before sending (and the JSON that will be sent)
    console.log('Scheduling payload (JSON):', JSON.stringify(payload));

    this.messageService.schedule(payload).subscribe({
      next: response => {
        console.log('Schedule response:', response);
        this.toastr.success('Message scheduled successfully.', 'Success!', {
          positionClass: 'toast-bottom-right',
          progressBar: true
        });
        this.messageStatus = 'Message scheduled successfully!';
      },
      error: error => {
        // More verbose logging so you can inspect server reply
        console.error('Schedule request failed:', error);
        console.error('HTTP status:', error?.status);
        console.error('Response body:', error?.error);
        const errorMessage = `Failed to schedule message: ${error?.message || 'Unknown Error'}`;
        this.messageService.logError(errorMessage).subscribe();
        this.toastr.error('Message was unable to be scheduled.', 'Error', {
          positionClass: 'toast-bottom-right',
          progressBar: true
        });
        this.messageStatus = 'Failed to schedule message.';
      }
    });
  }

  // New helper to call GET /print
  printTest(): void {
    const url = 'https://localhost:7268/Message/print';
    this.http.get(url, { responseType: 'text' }).subscribe({
      next: (res) => {
        console.log('[Publish] print response:', res);
        this.toastr.success('Print request completed.', 'Print', { positionClass: 'toast-bottom-right', progressBar: true });
      },
      error: (err) => {
        console.error('[Publish] print error:', err);
        const msg = err?.error ?? err?.message ?? 'Unknown error';
        this.toastr.error(String(msg), 'Print error', { positionClass: 'toast-bottom-right', progressBar: true });
      }
    });
  }
}

