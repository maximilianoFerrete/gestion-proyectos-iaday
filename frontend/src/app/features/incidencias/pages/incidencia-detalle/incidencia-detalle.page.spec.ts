import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ErrorApi } from '../../../../core/models/error-api.model';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { FlujosIncidenciaService } from '../../../configuracion-incidencias/services/flujos-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { IncidenciaDto } from '../../models/incidencia.model';
import { IncidenciasService } from '../../services/incidencias.service';
import { IncidenciaDetallePage } from './incidencia-detalle.page';

/** HU PRJ-0001-15 bajo una Épica, con 3 hijas y tipos de hija permitidos (CA-35). */
const HU: IncidenciaDto = {
  id: 15,
  codigo: 'PRJ-0001-15',
  numero: 15,
  proyecto: { id: 1, codigo: 'PRJ-0001', nombre: 'Portal clientes', archivado: false },
  titulo: 'Pagar con tarjeta',
  descripcion: 'Como cliente quiero pagar con tarjeta.',
  tipo: { id: 2, nombre: 'Historia de Usuario', activo: true, nivelNumero: 2 },
  estado: { id: 2, nombre: 'En progreso', activo: true, esFinal: false },
  prioridad: { id: 3, nombre: 'Alta', orden: 30, activo: true },
  responsable: { id: 2, nombre: 'Ana Gómez', activo: true },
  fechaVencimiento: '2026-11-30',
  agrupacion: { id: 8, nombre: 'Módulo pagos' },
  agrupacionHeredada: true,
  padre: {
    id: 3,
    codigo: 'PRJ-0001-3',
    titulo: 'Checkout',
    tipo: { id: 1, nombre: 'Épica', activo: true, nivelNumero: 1 },
    estado: { id: 2, nombre: 'En progreso', activo: true, esFinal: false },
    responsable: null,
    fechaVencimiento: '2026-12-15',
  },
  hijas: [16, 17, 18].map((id) => ({
    id,
    codigo: `PRJ-0001-${id}`,
    titulo: `Hija ${id}`,
    tipo: { id: 3, nombre: 'Subtarea', activo: true, nivelNumero: 3 },
    estado: { id: 1, nombre: 'Por hacer', activo: true, esFinal: false },
    responsable: null,
    fechaVencimiento: null,
  })),
  tiposHijaPermitidos: [
    { id: 3, nombre: 'Subtarea' },
    { id: 4, nombre: 'Caso de Prueba' },
    { id: 5, nombre: 'Bug' },
  ],
  fechaCreacion: '2026-10-09T14:00:00Z',
  fechaModificacion: null,
};

const ARCHIVADA: IncidenciaDto = { ...HU, proyecto: { ...HU.proyecto, archivado: true } };

/** Tests de IncidenciaDetallePage (I3, CA-21, CA-23, CA-35, CA-36, CA-38). */
describe('IncidenciaDetallePage', () => {
  let fixture: ComponentFixture<IncidenciaDetallePage>;
  const incidencias = { obtener: vi.fn(), cambiarTipo: vi.fn() };
  const tipos = { listar: vi.fn() };
  const flujos = { listar: vi.fn() };
  const permisos = signal<ReadonlySet<string>>(new Set());

  /** Crea la página para la incidencia indicada y la renderiza. */
  async function crear(incidencia: IncidenciaDto): Promise<HTMLElement> {
    incidencias.obtener.mockReturnValue(of(incidencia));
    fixture = TestBed.createComponent(IncidenciaDetallePage);
    fixture.componentRef.setInput('id', String(incidencia.proyecto.id));
    fixture.componentRef.setInput('incidenciaId', String(incidencia.id));
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Busca un botón o enlace por su texto visible. */
  function accion(raiz: HTMLElement, texto: string): HTMLElement | undefined {
    return Array.from(raiz.querySelectorAll<HTMLElement>('a, button')).find(
      (e) => e.textContent?.trim() === texto,
    );
  }

  beforeEach(() => {
    vi.clearAllMocks();
    permisos.set(new Set());
    tipos.listar.mockReturnValue(of([]));
    flujos.listar.mockReturnValue(of([]));
    TestBed.configureTestingModule({
      imports: [IncidenciaDetallePage],
      providers: [
        provideRouter([]),
        { provide: IncidenciasService, useValue: incidencias },
        { provide: TiposIncidenciaService, useValue: tipos },
        { provide: FlujosIncidenciaService, useValue: flujos },
        {
          provide: UsuarioActualService,
          useValue: { tienePermiso: (p: string) => permisos().has(p) },
        },
      ],
    });
  });

  // CA-21, RN-36: proyecto archivado → banner y sin Editar, Crear hija ni Cambiar tipo
  it('proyecto archivado muestra el banner y oculta Editar, Crear hija y Cambiar tipo', async () => {
    permisos.set(new Set([PERMISOS.incidenciasCambiarTipo]));

    const raiz = await crear(ARCHIVADA);

    expect(raiz.querySelector('.banner-archivado')?.textContent).toContain(
      'Proyecto archivado: solo lectura.',
    );
    expect(accion(raiz, 'Editar')).toBeUndefined();
    expect(accion(raiz, 'Crear hija')).toBeUndefined();
    expect(accion(raiz, 'Cambiar tipo')).toBeUndefined();
  });

  // CA-36, RN-02: sin permiso Incidencias.CambiarTipo no se muestra "Cambiar tipo"
  it('sin permiso CambiarTipo no muestra el botón Cambiar tipo pero sí Editar', async () => {
    const raiz = await crear(HU);

    expect(accion(raiz, 'Cambiar tipo')).toBeUndefined();
    expect(accion(raiz, 'Editar')).toBeDefined();
  });

  // RN-02, CA-37: con el permiso se muestra "Cambiar tipo"
  it('con permiso CambiarTipo muestra el botón Cambiar tipo', async () => {
    permisos.set(new Set([PERMISOS.incidenciasCambiarTipo]));

    const raiz = await crear(HU);

    expect(accion(raiz, 'Cambiar tipo')).toBeDefined();
  });

  // I3: sin tiposHijaPermitidos no se muestra "Crear hija"
  it('sin tipos de hija permitidos no muestra Crear hija', async () => {
    const raiz = await crear({ ...HU, tiposHijaPermitidos: [] });

    expect(accion(raiz, 'Crear hija')).toBeUndefined();
  });

  // CA-23: "Crear hija" lleva al alta con padreId
  it('Crear hija enlaza a /proyectos/1/incidencias/nueva?padreId=15', async () => {
    const raiz = await crear(HU);

    expect(accion(raiz, 'Crear hija')?.getAttribute('href')).toBe(
      '/proyectos/1/incidencias/nueva?padreId=15',
    );
  });

  // CA-35: se ven el padre (Épica) y las 3 hijas con enlaces
  it('muestra el padre y las tres hijas con enlaces al detalle', async () => {
    const raiz = await crear(HU);

    const hrefs = Array.from(raiz.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(raiz.textContent).toContain('PRJ-0001-3');
    expect(hrefs).toContain('/proyectos/1/incidencias/3');
    expect(hrefs).toEqual(
      expect.arrayContaining([
        '/proyectos/1/incidencias/16',
        '/proyectos/1/incidencias/17',
        '/proyectos/1/incidencias/18',
      ]),
    );
  });

  // CA-38, spec 0.2: 409 TIPO_INCOMPATIBLE_CON_HIJAS muestra los códigos como enlaces
  it('409 con extensión incidencias al cambiar tipo muestra los códigos como enlaces', async () => {
    permisos.set(new Set([PERMISOS.incidenciasCambiarTipo]));
    incidencias.cambiarTipo.mockReturnValue(
      throwError(
        () =>
          new ErrorApi(
            409,
            'TIPO_INCOMPATIBLE_CON_HIJAS',
            "El tipo 'HU técnica' no admite como hijas a: PRJ-0001-17.",
            {},
            ['PRJ-0001-17'],
          ),
      ),
    );
    const raiz = await crear(HU);
    accion(raiz, 'Cambiar tipo')?.click();
    await fixture.whenStable();

    fixture.componentInstance.cambiarTipo(HU, { tipoId: 10, estadoId: null });
    await fixture.whenStable();

    const enlace = Array.from(raiz.querySelectorAll('.incidencias-afectadas a')).find(
      (a) => a.textContent?.trim() === 'PRJ-0001-17',
    );
    expect(enlace?.getAttribute('href')).toBe('/proyectos/1/incidencias/17');
    expect(raiz.textContent).toContain('no admite como hijas a: PRJ-0001-17');
  });

  // I3: éxito del cambio de tipo → recarga el detalle
  it('cambio de tipo exitoso recarga el detalle', async () => {
    permisos.set(new Set([PERMISOS.incidenciasCambiarTipo]));
    incidencias.cambiarTipo.mockReturnValue(of(HU));
    await crear(HU);

    fixture.componentInstance.cambiarTipo(HU, { tipoId: 5, estadoId: null });
    await fixture.whenStable();

    expect(incidencias.cambiarTipo).toHaveBeenCalledWith(1, 15, { tipoId: 5, estadoId: null });
    expect(incidencias.obtener).toHaveBeenCalledTimes(2);
  });

  // CA-20: 404 al cargar → "No se encontró el recurso"
  it('incidencia inexistente o de otro proyecto muestra "No se encontró el recurso"', async () => {
    incidencias.obtener.mockReturnValue(
      throwError(() => new ErrorApi(404, 'INCIDENCIA_NO_ENCONTRADA', 'No existe.')),
    );
    fixture = TestBed.createComponent(IncidenciaDetallePage);
    fixture.componentRef.setInput('id', '1');
    fixture.componentRef.setInput('incidenciaId', '40');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'No se encontró el recurso',
    );
  });
});
