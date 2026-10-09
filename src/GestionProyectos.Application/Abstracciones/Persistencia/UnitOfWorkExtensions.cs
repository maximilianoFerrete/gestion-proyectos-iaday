namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Atajo para ejecutar una operación dentro de una transacción con commit o rollback.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega la variante que devuelve un resultado (RF-002)
/// </remarks>
public static class UnitOfWorkExtensions
{
    /// <summary>Ejecuta la operación en transacción; revierte y relanza ante cualquier error.</summary>
    public static async Task EjecutarEnTransaccionAsync(this IUnitOfWork unitOfWork,
        Func<Task> operacion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operacion);
        await unitOfWork.EjecutarEnTransaccionAsync(async () =>
        {
            await operacion();
            return true;
        }, ct);
    }

    /// <summary>Ejecuta la operación en transacción y devuelve su resultado; revierte ante error.</summary>
    public static async Task<T> EjecutarEnTransaccionAsync<T>(this IUnitOfWork unitOfWork,
        Func<Task<T>> operacion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(operacion);

        await unitOfWork.IniciarAsync(ct);
        try
        {
            var resultado = await operacion();
            await unitOfWork.ConfirmarAsync(ct);
            return resultado;
        }
        catch
        {
            await unitOfWork.RevertirAsync(CancellationToken.None);
            throw;
        }
    }
}
