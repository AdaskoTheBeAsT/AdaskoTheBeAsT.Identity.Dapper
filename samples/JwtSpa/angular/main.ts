import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { bootstrapApplication } from '@angular/platform-browser';
import { AuthDemo, isDeliveryMethod } from '../shared/auth';

@Component({
  selector: 'identity-demo',
  imports: [FormsModule],
  templateUrl: './app.html',
})
class App {
  readonly auth = new AuthDemo();
  readonly view = signal(this.auth.snapshot());
  readonly isDeliveryMethod = isDeliveryMethod;
  email = '';
  password = '';
  code = '';
  method = '';
  registering = false;

  constructor() {
    inject(DestroyRef).onDestroy(this.auth.subscribe(() => this.view.set(this.auth.snapshot())));
  }

  async submitCredentials() {
    const password = this.password;
    this.password = '';
    if (this.registering) {
      await this.auth.register(this.email, password);
      if (!this.view().error) this.registering = false;
    } else {
      this.method = '';
      await this.auth.login(this.email, password);
    }
  }

  submitCode(method?: string) {
    const code = this.code;
    this.code = '';
    return method ? this.auth.verify(method, code) : this.auth.confirm(code);
  }
}

void bootstrapApplication(App);
