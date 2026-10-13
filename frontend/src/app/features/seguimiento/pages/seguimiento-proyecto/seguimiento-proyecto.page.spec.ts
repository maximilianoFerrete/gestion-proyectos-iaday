import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { ErrorApi } from '../../../../core/models/error-api.model';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { EstadosIncidenciaService } from '../../../configuracion-incidencias/services/estados-incidencia.service';
import { NivelesIncidenciaService } from '../../../configuracion-incidencias/services/niveles-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { IncidenciasService } from '../../../incidencias/services/incidencias.service';
import { AgrupacionesService } from '../../../proyectos/services/agrupaciones.service';
import {
  ConfiguracionTableroDto,
  IndicadoresDto,
  ItemTableroDto,
  SeguimientoDto,
  TableroIncidenciasDto,
} from '../../models/seguimiento.model';
import { SeguimientoService } from '../../services/seguimiento.service';
import { SeguimientoProyectoPage } from './seguimiento-proyecto.page';

const INDICADORES: IndicadoresDto = {
  total: 3,
  abiertas: 2,
  finalizadas: 1,
  vencidas: 1,
  bloqueadas: 1,
  enEspera: 0,
  sinResponsable: 1,
  porEstado: [
    {
      estado: { id: 2, nombre: 'En progreso', orden: 20, activo: true, categoria: null, esFinal: false },
      cantidad: 2,
    },
    {
      estado: { id: 3, nombre: 'Hecho', orden: 30, activo: true, categoria: null, esFinal: true },
      cantidad: 1,
    },
  ],
};

const ALCANCE = { todosLosNiveles: true, niveles: [], todosLosTipos: true, tipos: [] };

/** Resumen del proyecto (o de una agrupación si se indica). */
function resumen(archivado = false, agrupacion = false): SeguimientoDto {
  return {
    fechaReferencia: '2026-10-10',
    proyecto: { id: 1, codigo: 'PRJ-0001', nombre: 'Portal clientes', archivado },
    ambito: agrupacion ? 'agrupacion' : 'proyecto',
    agrupacion: agrupacion
      ? { id: 8, nombre: 'Pagos', descripcion: 'Incidencias del circuito de pagos.' }
      : null,
    alcance: ALCANCE,
    indicadores: INDICADORES,
    agrupaciones: [],
  };
}

const CONFIGURACION: ConfiguracionTableroDto = {
  proyectoId: 1,
  personalizada: false,
  mostrarEstado: true,
  mostrarEstimacion: true,
  mostrarResponsable: true,
  fechaModificacion: null,
};

/** Ítem del tablero. */
function item(numero: number): ItemTableroDto {
  return {
    id: numero,
    codigo: `PRJ-0001-${numero}`,
    numero,
    titulo: `Incidencia ${numero}`,
    tipo: { id: 2, nombre: 'Historia de Usuario', activo: true, nivelNumero: 2 },
    estado: { id: 2, nombre: 'En progreso', orden: 20, activo: true, esFinal: false, categoria: null },
    estimacion: 4,
    vencida: false,
    responsable: null,
  };
}

/** Respuesta del tablero con los ítems indicados. */
function tablero(items: ItemTableroDto[], archivado = false): TableroIncidenciasDto {
  return {
    fechaReferencia: '2026-10-10',
    proyecto: { id: 1, codigo: 'PRJ-0001', nombre: 'Portal clientes', archivado },
    alcance: ALCANCE,
    filtros: { indicador: null, estado: null, texto: null },
    configuracion: CONFIGURACION,
    total: items.length,
    items,
  };
}

/** Tests de SeguimientoProyectoPage (T2, RF-005 CA-22 a CA-24, CA-34, CA-48, CA-49, RN-24). */
describe('SeguimientoProyectoPage', () => {
  let fixture: ComponentFixture<SeguimientoProyectoPage>;
  let router: Router;
  const permisos = signal<ReadonlySet<string>>(new Set());
  const seguimiento = {
    seguimiento: vi.fn(),
    tablero: vi.fn(),
    guardarConfiguracionTablero: vi.fn(),
  };
  const incidencias = { arbol: vi.fn() };
  const notificacion = { exito: vi.fn() };

  /** Crea la página del proyecto 1 con los query params indicados. */
  async function crear(params: Record<string, string | string[]> = {}): Promise<HTMLElement> {
    fixture = TestBed.createComponent(SeguimientoProyectoPage);
    fixture.componentRef.setInput('id', '1');
    for (const [clave, valor] of Object.entries(params)) {
      fixture.componentRef.setInput(clave, valor);
    }
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Query params de la última navegación. */
  function ultimosQueryParams(): unknown {
    const llamadas = vi.mocked(router.navigate).mock.calls;
    return (llamadas[llamadas.length - 1][1] as { queryParams: unknown }).queryParams;
  }

  /** Botón por texto. */
  function boton(raiz: HTMLElement, texto: string): HTMLButtonElement | undefined {
    return Array.from(raiz.querySelectorAll<HTMLButtonElement>('button')).find(
      (b) => b.textContent?.trim() === texto,
    );
  }

  /** Configura la respuesta del tablero. */
  function tableroDevuelve(respuesta: Observable<TableroIncidenciasDto>): void {
    seguimiento.tablero.mockReturnValue(respuesta);
  }

  beforeEach(() => {
    vi.clearAllMocks();
    permisos.set(new Set());
    seguimiento.seguimiento.mockReturnValue(of(resumen()));
    tableroDevuelve(of(tablero([item(15), item(16)])));
    incidencias.arbol.mockReturnValue(of([]));
    TestBed.configureTestingModule({
      imports: [SeguimientoProyectoPage],
      providers: [
        provideRouter([]),
        { provide: SeguimientoService, useValue: seguimiento },
        { provide: IncidenciasService, useValue: incidencias },
        { provide: AgrupacionesService, useValue: { listar: () => of([]) } },
        { provide: NivelesIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: TiposIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: EstadosIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: NotificacionService, useValue: notificacion },
        {
          provide: UsuarioActualService,
          useValue: { tienePermiso: (p: string) => permisos().has(p) },
        },
      ],
    });
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  // CA-49, DA-16: las pestañas son solo Resumen, Tablero y Jerarquía
  it('muestra solo las pestañas Resumen, Tablero y Jerarquía', async () => {
    const raiz = await crear();

    const pestanias = Array.from(raiz.querySelectorAll('.pestanias a')).map((a) =>
      a.textContent?.trim(),
    );
    expect(pestanias).toEqual(['Resumen', 'Tablero', 'Jerarquía']);
  });

  // CA-49: vista=lista abre el Resumen (no pide el tablero)
  it('con vista=lista abre la vista Resumen', async () => {
    const raiz = await crear({ vista: 'lista' });

    expect(raiz.querySelector('#titulo-vista-resumen')).not.toBeNull();
    expect(raiz.querySelector('.pestania-activa')?.textContent?.trim()).toBe('Resumen');
    expect(seguimiento.tablero).not.toHaveBeenCalled();
  });

  // RN-07, CA-49: el Resumen ignora los filtros del tablero de la URL
  it('el Resumen se pide sin los filtros del tablero', async () => {
    await crear({ indicador: 'vencidas', texto: 'pago', nivelId: '2' });

    expect(seguimiento.seguimiento).toHaveBeenCalledTimes(1);
    const consulta = seguimiento.seguimiento.mock.calls[0][1] as Record<string, unknown>;
    expect(consulta).not.toHaveProperty('indicador');
    expect(consulta).not.toHaveProperty('texto');
    expect(consulta['nivelIds']).toEqual([2]);
  });

  // RN-24, CA-24: clic en la tarjeta Vencidas navega al Tablero con indicador y alcance
  it('clic en la tarjeta Vencidas navega a vista=tablero con indicador=vencidas', async () => {
    const raiz = await crear({ nivelId: '2' });

    raiz.querySelectorAll<HTMLButtonElement>('.tarjeta-indicador')[2].click();

    expect(ultimosQueryParams()).toEqual({ nivelId: ['2'], vista: 'tablero', indicador: 'vencidas' });
  });

  // RN-24: clic en la tarjeta Total navega al Tablero sin filtros
  it('clic en la tarjeta Total navega a vista=tablero sin filtros del tablero', async () => {
    const raiz = await crear({ estadoId: '3' });

    raiz.querySelectorAll<HTMLButtonElement>('.tarjeta-indicador')[0].click();

    expect(ultimosQueryParams()).toEqual({ vista: 'tablero' });
  });

  // RN-24, CA-24: clic en un estado del conteo navega al Tablero con estadoId y sin indicador
  it('clic en "En progreso" del conteo navega a vista=tablero con estadoId', async () => {
    const raiz = await crear({ indicador: 'vencidas', agrupacionId: '8' });

    raiz.querySelector<HTMLButtonElement>('[aria-label="En progreso: Ver en el tablero"]')!.click();

    expect(ultimosQueryParams()).toEqual({ agrupacionId: '8', vista: 'tablero', estadoId: '2' });
  });

  // CA-48: la vista Tablero pide la grilla con ámbito, alcance y filtros de la URL
  it('vista tablero pide el tablero con los filtros de la URL', async () => {
    await crear({ vista: 'tablero', agrupacionId: '8', nivelId: '2', indicador: 'bloqueadas', texto: 'pago' });

    expect(seguimiento.tablero).toHaveBeenCalledWith(
      1,
      expect.objectContaining({
        agrupacionId: 8,
        nivelIds: [2],
        indicador: 'bloqueadas',
        estadoId: null,
        texto: 'pago',
      }),
    );
  });

  // CA-24: contador con filtros "N incidencias (filtradas)"
  it('con filtros muestra el contador "(filtradas)"', async () => {
    const raiz = await crear({ vista: 'tablero', indicador: 'vencidas' });

    expect(raiz.querySelector('.contador-tablero')?.textContent?.trim()).toBe(
      '2 incidencias (filtradas)',
    );
  });

  // CA-48: vacío con filtros muestra el mensaje y "Limpiar filtros"
  it('vacío con filtros muestra "No hay incidencias que coincidan con los filtros."', async () => {
    tableroDevuelve(of(tablero([])));

    const raiz = await crear({ vista: 'tablero', indicador: 'vencidas', estadoId: '3' });

    expect(raiz.textContent).toContain('No hay incidencias que coincidan con los filtros.');
    expect(raiz.querySelectorAll('app-tabla-tablero').length).toBe(0);
  });

  // "Vista Tablero": vacío sin filtros
  it('vacío sin filtros muestra "No hay incidencias en este ámbito y alcance."', async () => {
    tableroDevuelve(of(tablero([])));

    const raiz = await crear({ vista: 'tablero' });

    expect(raiz.textContent).toContain('No hay incidencias en este ámbito y alcance.');
  });

  // CA-48: "Limpiar filtros" conserva ámbito, alcance y vista
  it('limpiar filtros del tablero conserva ámbito, alcance y vista', async () => {
    await crear({ vista: 'tablero', sinAgrupacion: 'true', nivelId: '3', indicador: 'vencidas', texto: 'x' });

    fixture.componentInstance.limpiarFiltrosTablero();

    expect(ultimosQueryParams()).toEqual({ sinAgrupacion: 'true', nivelId: ['3'], vista: 'tablero' });
  });

  // RN-33, CA-48: cambiar filtros del tablero actualiza la URL (el orden no se guarda)
  it('cambiar los filtros del tablero los escribe en la URL sin orden', async () => {
    await crear({ vista: 'tablero', indicador: 'bloqueadas' });

    fixture.componentInstance.cambiarFiltrosTablero({ indicador: 'bloqueadas', estadoId: null, texto: 'pago' });

    expect(ultimosQueryParams()).toEqual({ vista: 'tablero', indicador: 'bloqueadas', texto: 'pago' });
  });

  // RN-01: cambiar el ámbito conserva alcance, vista y filtros
  it('cambiar el ámbito conserva alcance, vista y filtros del tablero', async () => {
    await crear({ vista: 'tablero', nivelId: '2', indicador: 'vencidas' });

    fixture.componentInstance.cambiarAmbito({ agrupacionId: 9, sinAgrupacion: false });

    expect(ultimosQueryParams()).toEqual({
      agrupacionId: '9',
      nivelId: ['2'],
      vista: 'tablero',
      indicador: 'vencidas',
    });
  });

  // CA-34: sin el permiso no aparece el botón "Columnas"
  it('sin permiso no muestra el botón Columnas', async () => {
    const raiz = await crear({ vista: 'tablero' });

    expect(boton(raiz, 'Columnas')).toBeUndefined();
  });

  // CA-32: con el permiso y proyecto no archivado aparece "Columnas"
  it('con permiso y proyecto no archivado muestra el botón Columnas', async () => {
    permisos.set(new Set([PERMISOS.proyectosConfigurarTablero]));

    const raiz = await crear({ vista: 'tablero' });

    expect(boton(raiz, 'Columnas')).toBeDefined();
  });

  // CA-22: proyecto archivado → sin "Columnas" aunque tenga permiso, con banner
  it('proyecto archivado muestra el banner y no el botón Columnas', async () => {
    permisos.set(new Set([PERMISOS.proyectosConfigurarTablero]));
    seguimiento.seguimiento.mockReturnValue(of(resumen(true)));
    tableroDevuelve(of(tablero([item(15)], true)));

    const raiz = await crear({ vista: 'tablero' });

    expect(raiz.querySelector('.banner-archivado')).not.toBeNull();
    expect(boton(raiz, 'Columnas')).toBeUndefined();
  });

  // CA-32: guardar columnas aplica la configuración sin recargar el tablero y muestra el toast
  it('guardar columnas oculta Estimación sin volver a pedir el tablero', async () => {
    permisos.set(new Set([PERMISOS.proyectosConfigurarTablero]));
    seguimiento.guardarConfiguracionTablero.mockReturnValue(
      of({ ...CONFIGURACION, personalizada: true, mostrarEstimacion: false }),
    );
    const raiz = await crear({ vista: 'tablero' });

    fixture.componentInstance.guardarColumnas({
      mostrarEstado: true,
      mostrarEstimacion: false,
      mostrarResponsable: true,
    });
    await fixture.whenStable();

    const encabezados = Array.from(raiz.querySelectorAll('app-tabla-tablero thead th')).map((th) =>
      th.textContent?.replace(/[▲▼]/g, '').trim(),
    );
    expect(encabezados).not.toContain('Estimación');
    expect(seguimiento.tablero).toHaveBeenCalledTimes(1);
    expect(notificacion.exito).toHaveBeenCalledWith('Configuración del tablero guardada.');
  });

  // "Vista Tablero": error 403/409 del guardado se muestra dentro del diálogo
  it('un 409 al guardar columnas deja el detail como error del diálogo', async () => {
    permisos.set(new Set([PERMISOS.proyectosConfigurarTablero]));
    seguimiento.guardarConfiguracionTablero.mockReturnValue(
      throwError(() => new ErrorApi(409, 'PROYECTO_ARCHIVADO', 'El proyecto PRJ-0001 está archivado.')),
    );
    await crear({ vista: 'tablero' });

    fixture.componentInstance.abrirColumnas();
    fixture.componentInstance.guardarColumnas({
      mostrarEstado: true,
      mostrarEstimacion: true,
      mostrarResponsable: true,
    });

    expect(fixture.componentInstance.errorColumnas()).toBe('El proyecto PRJ-0001 está archivado.');
    expect(fixture.componentInstance.dialogoColumnas()).toBe(true);
  });

  // DA-08, CA-22: ámbito agrupación no archivado → nombre, descripción y "Editar agrupación"
  it('en ámbito agrupación muestra nombre, descripción y Editar agrupación', async () => {
    seguimiento.seguimiento.mockReturnValue(of(resumen(false, true)));

    const raiz = await crear({ agrupacionId: '8' });

    expect(raiz.querySelector('.detalle-agrupacion')?.textContent).toContain('Pagos');
    expect(raiz.textContent).toContain('Incidencias del circuito de pagos.');
    expect(raiz.textContent).toContain('Editar agrupación');
  });

  // CA-22: proyecto archivado → banner sin "Editar agrupación"
  it('proyecto archivado en ámbito agrupación no muestra Editar agrupación', async () => {
    seguimiento.seguimiento.mockReturnValue(of(resumen(true, true)));

    const raiz = await crear({ agrupacionId: '8' });

    expect(raiz.querySelector('.banner-archivado')).not.toBeNull();
    expect(raiz.textContent).not.toContain('Editar agrupación');
  });

  // CA-23: proyecto inexistente → "No se encontró el recurso"
  it('404 del proyecto muestra "No se encontró el recurso"', async () => {
    seguimiento.seguimiento.mockReturnValue(
      throwError(() => new ErrorApi(404, 'PROYECTO_NO_ENCONTRADO', 'No existe.')),
    );

    const raiz = await crear();

    expect(raiz.textContent).toContain('No se encontró el recurso');
  });

  // "Pantallas": 404 de agrupación → mensaje propio y botón "Todo el proyecto"
  it('404 de agrupación muestra el mensaje y vuelve a todo el proyecto', async () => {
    seguimiento.seguimiento.mockReturnValue(
      throwError(() => new ErrorApi(404, 'AGRUPACION_NO_ENCONTRADA', 'No existe.')),
    );
    const raiz = await crear({ agrupacionId: '20', nivelId: '2' });

    expect(raiz.textContent).toContain('La agrupación no existe en este proyecto.');
    boton(raiz, 'Todo el proyecto')!.click();

    expect(ultimosQueryParams()).toEqual({ nivelId: ['2'] });
  });

  // "Vista Tablero": error 400 (estadoId inexistente en la URL) → detail y "Limpiar filtros"
  it('un 400 del tablero muestra el detail con Limpiar filtros', async () => {
    tableroDevuelve(
      throwError(() => new ErrorApi(400, 'VALIDACION', 'Hay errores de validación.', {
        estadoId: ['El estado no existe.'],
      })),
    );

    const raiz = await crear({ vista: 'tablero', estadoId: '9999' });

    const alerta = raiz.querySelector<HTMLElement>('#titulo-vista-tablero ~ [role="alert"]')!;
    expect(alerta.textContent).toMatch(/Hay errores de validación\.|El estado no existe\./);
    expect(boton(alerta, 'Limpiar filtros')).toBeDefined();
  });

  // RN-22, CA-28: la vista Jerarquía pide el árbol del ámbito
  it('vista jerarquía pide el árbol con el ámbito de la URL', async () => {
    await crear({ vista: 'jerarquia', agrupacionId: '8', indicador: 'vencidas' });

    expect(incidencias.arbol).toHaveBeenCalledWith(1, {
      proyectoId: 1,
      agrupacionId: 8,
      sinAgrupacion: false,
    });
    expect(seguimiento.tablero).not.toHaveBeenCalled();
  });
});
