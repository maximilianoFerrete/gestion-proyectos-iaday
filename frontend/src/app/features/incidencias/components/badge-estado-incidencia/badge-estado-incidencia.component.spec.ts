import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BadgeEstadoIncidenciaComponent, EstadoBadgeIncidencia } from './badge-estado-incidencia.component';

/** Tests de BadgeEstadoIncidenciaComponent: ícono de categoría (RF-005 CA-40). */
describe('BadgeEstadoIncidenciaComponent', () => {
  let fixture: ComponentFixture<BadgeEstadoIncidenciaComponent>;

  /** Renderiza el badge del estado indicado. */
  async function crear(estado: EstadoBadgeIncidencia): Promise<HTMLElement> {
    fixture = TestBed.createComponent(BadgeEstadoIncidenciaComponent);
    fixture.componentRef.setInput('estado', estado);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [BadgeEstadoIncidenciaComponent] });
  });

  // CA-40: categoría BLOQUEADA → candado con title
  it('con categoría BLOQUEADA muestra el ícono de candado con su title', async () => {
    const raiz = await crear({ nombre: 'Bloqueada', activo: true, esFinal: false, categoria: 'BLOQUEADA' });

    const icono = raiz.querySelector<HTMLElement>('.badge-icono')!;
    expect(icono.textContent).toBe('\u{1F512}');
    expect(icono.title).toBe('Categoría: Bloqueada');
  });

  // CA-40: categoría EN_ESPERA → reloj con title
  it('con categoría EN_ESPERA muestra el ícono de reloj con su title', async () => {
    const raiz = await crear({ nombre: 'Esperando', activo: true, esFinal: false, categoria: 'EN_ESPERA' });

    const icono = raiz.querySelector<HTMLElement>('.badge-icono')!;
    expect(icono.textContent).toBe('\u{1F552}');
    expect(icono.title).toBe('Categoría: En espera');
  });

  // CA-40: sin categoría no hay ícono
  it('sin categoría no muestra ícono', async () => {
    const raiz = await crear({ nombre: 'Por hacer', activo: true, esFinal: false, categoria: null });

    expect(raiz.querySelector('.badge-icono')).toBeNull();
    expect(raiz.textContent?.trim()).toBe('Por hacer');
  });

  // RF-002 DA-09: los finales mantienen el estilo diferenciado
  it('un estado final usa la clase badge-final', async () => {
    const raiz = await crear({ nombre: 'Hecho', activo: true, esFinal: true, categoria: null });

    expect(raiz.querySelector('.badge-final')).not.toBeNull();
  });
});
