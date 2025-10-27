import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { Publish } from './publish/publish';
import { RouterOutlet } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [Publish, FormsModule, HttpClientModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  title = 'servicebus-app';
  constructor(private toastr: ToastrService) {
  
  }
  showSuccess(){
    this.toastr.success("Success Toast!", 'Toast Title', {
      timeOut: 10000,
      progressBar: true,
      closeButton: true
    });
  }
 }
