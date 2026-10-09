import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { usuarioActualInterceptor } from './core/interceptors/usuario-actual.interceptor';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([usuarioActualInterceptor, errorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('crea la aplicación', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('muestra el título y el selector "Usuario actual"', async () => {
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    http.match(() => true).forEach((req) => req.flush([]));
    await fixture.whenStable();
    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.titulo-app')?.textContent).toContain('Gestión de Proyectos');
    expect(compiled.querySelector('label[for="usuario-actual"]')?.textContent).toContain(
      'Usuario actual',
    );
  });
});
