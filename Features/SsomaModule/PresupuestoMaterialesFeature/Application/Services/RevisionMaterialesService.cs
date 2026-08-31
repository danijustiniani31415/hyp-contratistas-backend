using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class RevisionMaterialesService : IRevisionMaterialesService
{
    private readonly IConsumoRepository _consumoRepo;
    private readonly IEstandarizacionRepository _estandarizacionRepo;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IEmailService _email;

    public RevisionMaterialesService(
        IConsumoRepository consumoRepo,
        IEstandarizacionRepository estandarizacionRepo,
        IDbContextFactory<AppDbContext> factory,
        IEmailService email)
    {
        _consumoRepo = consumoRepo;
        _estandarizacionRepo = estandarizacionRepo;
        _factory = factory;
        _email = email;
    }

    public async Task<List<MaterialPendienteDto>> ObtenerPendientesAsync(int projectId) =>
        await _consumoRepo.ObtenerPendientesRevisionAsync(projectId);

    public async Task<List<MaterialPendienteGlobalDto>> ObtenerPendientesGlobalAsync() =>
        await _consumoRepo.ObtenerPendientesRevisionGlobalAsync();

    public async Task<List<MaterialNoSsomaDto>> ObtenerNoSsomaAsync() =>
        await _consumoRepo.ObtenerNoSsomaAsync();

    /// <summary>
    /// Igual que <see cref="ProcesarRevisionAsync"/> pero sin exigir un solo ProjectId compartido:
    /// cada línea puede pertenecer a un proyecto distinto (vista global del Catálogo de Materiales).
    /// Los rechazados se agrupan por proyecto para notificar a Oficina Técnica de cada uno.
    /// </summary>
    public async Task<RevisionResultDto> ProcesarRevisionGlobalAsync(List<RevisionDecisionDto> decisiones, int usuarioId)
    {
        var resultado = new RevisionResultDto();
        var rechazadosPorProyecto = new Dictionary<int, List<(string RecursoCrudo, string? Motivo)>>();
        List<MaterialPendienteGlobalDto>? pendientesSnapshot = null;

        foreach (var decision in decisiones)
        {
            try
            {
                var linea = await _consumoRepo.ObtenerLineaPorIdAsync(decision.LineaId);
                if (linea == null)
                {
                    resultado.Errores.Add($"Línea {decision.LineaId}: no encontrada.");
                    continue;
                }

                if (decision.Decision == "AUTORIZADO")
                {
                    await _consumoRepo.ActualizarRevisionAsync(decision.LineaId, "AUTORIZADO", decision.ItemIdConfirmado);

                    if (decision.ItemIdConfirmado.HasValue)
                    {
                        var textoNorm = TextoNormalizador.Normalizar(linea.RecursoCrudo);
                        await _estandarizacionRepo.CrearAliasAsync(linea.RecursoCrudo, textoNorm,
                            decision.ItemIdConfirmado.Value, "FUZZY_CONFIRMADO", 1.0m);

                        pendientesSnapshot ??= await _consumoRepo.ObtenerPendientesRevisionGlobalAsync();
                        resultado.AplicadosRetroactivamente += await AplicarRetroactivamenteAsync(
                            "AUTORIZADO", textoNorm, decision.LineaId, decision.ItemIdConfirmado, pendientesSnapshot);
                    }
                    resultado.Autorizados++;
                }
                else if (decision.Decision == "RECHAZADO")
                {
                    await _consumoRepo.ActualizarRevisionAsync(decision.LineaId, "RECHAZADO", null);
                    // Aprender: la próxima vez que aparezca este mismo texto (este u otro proyecto),
                    // se rechaza solo, sin volver a pasar por Revisión.
                    var textoNormRechazo = TextoNormalizador.Normalizar(linea.RecursoCrudo);
                    await _estandarizacionRepo.CrearAliasRechazoAsync(linea.RecursoCrudo, textoNormRechazo);

                    pendientesSnapshot ??= await _consumoRepo.ObtenerPendientesRevisionGlobalAsync();
                    resultado.AplicadosRetroactivamente += await AplicarRetroactivamenteAsync(
                        "RECHAZADO", textoNormRechazo, decision.LineaId, null, pendientesSnapshot);

                    if (!rechazadosPorProyecto.TryGetValue(linea.ProjectId, out var lista))
                        rechazadosPorProyecto[linea.ProjectId] = lista = [];
                    lista.Add((linea.RecursoCrudo, decision.MotivoRechazo));
                    resultado.Rechazados++;
                }
                else
                {
                    resultado.Errores.Add($"Línea {decision.LineaId}: decisión inválida '{decision.Decision}'. Use AUTORIZADO o RECHAZADO.");
                }
            }
            catch (Exception ex)
            {
                resultado.Errores.Add($"Línea {decision.LineaId}: {ex.Message}");
            }
        }

        foreach (var (projectId, rechazados) in rechazadosPorProyecto)
            resultado.NotificacionesEnviadas += await NotificarOficinaTecnicaAsync(projectId, rechazados);

        return resultado;
    }

    /// <summary>
    /// Otras líneas pendientes (cualquier proyecto, del snapshot tomado al inicio del lote) cuyo texto
    /// crudo normaliza IGUAL al que el usuario acaba de decidir: aplicarles la misma decisión evita
    /// pedirle que confirme/rechace el mismo material línea por línea cada vez que aparece repetido.
    /// Nunca vuelve a notificar a Oficina Técnica por estas — ya se avisó con la línea original.
    /// </summary>
    private async Task<int> AplicarRetroactivamenteAsync(
        string decision, string textoNorm, long lineaIdOrigen, int? itemIdConfirmado,
        List<MaterialPendienteGlobalDto> pendientesSnapshot)
    {
        var afectadas = 0;
        bool? perteneceSsoma = null;
        foreach (var otra in pendientesSnapshot)
        {
            if (otra.LineaId == lineaIdOrigen) continue;
            if (TextoNormalizador.Normalizar(otra.RecursoCrudo) != textoNorm) continue;

            if (decision == "AUTORIZADO" && itemIdConfirmado.HasValue)
            {
                perteneceSsoma ??= await _estandarizacionRepo.ObtenerPerteneceSsomaDeItemAsync(itemIdConfirmado.Value);
                await _consumoRepo.ActualizarLineaEstandarizadaAsync(
                    otra.LineaId, itemIdConfirmado.Value, perteneceSsoma.Value, "ALIAS_RETROACTIVO", 1.0m, null);
                afectadas++;
            }
            else if (decision == "RECHAZADO")
            {
                await _consumoRepo.ActualizarRevisionAsync(otra.LineaId, "RECHAZADO", null);
                afectadas++;
            }
        }
        return afectadas;
    }

    public async Task<RevisionResultDto> ProcesarRevisionAsync(RevisionLoteDto dto, int usuarioId)
    {
        var resultado = new RevisionResultDto();
        var rechazadosParaNotificar = new List<(string RecursoCrudo, string? Motivo)>();
        List<MaterialPendienteGlobalDto>? pendientesSnapshot = null;

        foreach (var decision in dto.Decisiones)
        {
            try
            {
                var linea = await _consumoRepo.ObtenerLineaPorIdAsync(decision.LineaId);
                if (linea == null || linea.ProjectId != dto.ProjectId)
                {
                    resultado.Errores.Add($"Línea {decision.LineaId}: no encontrada o no pertenece al proyecto.");
                    continue;
                }

                if (decision.Decision == "AUTORIZADO")
                {
                    await _consumoRepo.ActualizarRevisionAsync(decision.LineaId, "AUTORIZADO", decision.ItemIdConfirmado);

                    // Aprender: si confirmó un item diferente al sugerido, crear alias para esa corrección
                    if (decision.ItemIdConfirmado.HasValue)
                    {
                        var textoNorm = TextoNormalizador.Normalizar(linea.RecursoCrudo);
                        await _estandarizacionRepo.CrearAliasAsync(linea.RecursoCrudo, textoNorm,
                            decision.ItemIdConfirmado.Value, "FUZZY_CONFIRMADO", 1.0m);

                        pendientesSnapshot ??= await _consumoRepo.ObtenerPendientesRevisionGlobalAsync();
                        resultado.AplicadosRetroactivamente += await AplicarRetroactivamenteAsync(
                            "AUTORIZADO", textoNorm, decision.LineaId, decision.ItemIdConfirmado, pendientesSnapshot);
                    }
                    resultado.Autorizados++;
                }
                else if (decision.Decision == "RECHAZADO")
                {
                    await _consumoRepo.ActualizarRevisionAsync(decision.LineaId, "RECHAZADO", null);
                    // Aprender: la próxima vez que aparezca este mismo texto (este u otro proyecto),
                    // se rechaza solo, sin volver a pasar por Revisión.
                    var textoNormRechazo = TextoNormalizador.Normalizar(linea.RecursoCrudo);
                    await _estandarizacionRepo.CrearAliasRechazoAsync(linea.RecursoCrudo, textoNormRechazo);

                    pendientesSnapshot ??= await _consumoRepo.ObtenerPendientesRevisionGlobalAsync();
                    resultado.AplicadosRetroactivamente += await AplicarRetroactivamenteAsync(
                        "RECHAZADO", textoNormRechazo, decision.LineaId, null, pendientesSnapshot);

                    rechazadosParaNotificar.Add((linea.RecursoCrudo, decision.MotivoRechazo));
                    resultado.Rechazados++;
                }
                else
                {
                    resultado.Errores.Add($"Línea {decision.LineaId}: decisión inválida '{decision.Decision}'. Use AUTORIZADO o RECHAZADO.");
                }
            }
            catch (Exception ex)
            {
                resultado.Errores.Add($"Línea {decision.LineaId}: {ex.Message}");
            }
        }

        // Notificar a Oficina Técnica por los rechazados
        if (rechazadosParaNotificar.Count > 0)
        {
            var enviados = await NotificarOficinaTecnicaAsync(dto.ProjectId, rechazadosParaNotificar);
            resultado.NotificacionesEnviadas = enviados;
        }

        return resultado;
    }

    /// <summary>
    /// Substring + similarity trigram (misma técnica que la Etapa 4 automática) — un Contains
    /// exacto obligaba a escribir el nombre del catálogo palabra por palabra ("BARRA EXTENSIBLE"
    /// no encontraba "BARRA EXPANDIBLE" aunque sea, en la práctica, el mismo material).
    /// </summary>
    public async Task<List<BuscarItemDto>> BuscarItemsAsync(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Length < 3)
            return [];

        var textoNorm = TextoNormalizador.Normalizar(texto);
        var resultados = await _estandarizacionRepo.BuscarItemsSimilaresAsync(textoNorm);
        return resultados.Select(r => new BuscarItemDto
        {
            Id = r.ItemId,
            Nombre = r.NombreItem,
            NombreFamilia = r.NombreFamilia,
            TipoMaterial = r.TipoMaterial ?? "",
            PerteneceSsoma = r.PerteneceSsoma,
        }).ToList();
    }

    private async Task<int> NotificarOficinaTecnicaAsync(int projectId, List<(string RecursoCrudo, string? Motivo)> rechazados)
    {
        try
        {
            using var ctx = _factory.CreateDbContext();
            var proyecto = await ctx.Project.FindAsync(projectId);
            if (proyecto == null) return 0;

            // Oficina Técnica y Almacenero DEL PROYECTO, no un contacto fijo del proyecto: se
            // resuelve por vinculación vigente (fecha_fin null) en worker_vinculaciones — no el
            // genérico Project.StaffEmail/EmailResidente, que no distinguía cargo y mandaba a
            // quien fuera que tuviera ese campo lleno, sin importar su proyecto real.
            // El puesto se busca en dos fuentes porque coexisten: WorkerVinculacion.Puesto es
            // texto congelado al vincularse (vinculaciones viejas) y puede venir vacío en las
            // nuevas, que ya usan el catálogo normalizado Worker.PuestoId -> Puesto.Nombre (el
            // campo de presentación real, el que se muestra en pantalla/PDFs/correos).
            var destinatarios = await ctx.WorkerVinculacion
                .Include(v => v.Worker)
                    .ThenInclude(w => w!.PuestoCatalogo)
                .Where(v => v.ProyectoId == projectId && v.FechaFin == null
                    && v.Worker != null && v.Worker.EmailCorporativo != null
                    && (
                        (v.Puesto != null
                            && (EF.Functions.ILike(v.Puesto, "%OFICINA TECNICA%") || EF.Functions.ILike(v.Puesto, "%OFICINA TÉCNICA%")
                                || EF.Functions.ILike(v.Puesto, "%ALMACEN%") || EF.Functions.ILike(v.Puesto, "%ALMACÉN%")))
                        || (v.Worker.PuestoCatalogo != null
                            && (EF.Functions.ILike(v.Worker.PuestoCatalogo.Nombre, "%OFICINA TECNICA%") || EF.Functions.ILike(v.Worker.PuestoCatalogo.Nombre, "%OFICINA TÉCNICA%")
                                || EF.Functions.ILike(v.Worker.PuestoCatalogo.Nombre, "%ALMACEN%") || EF.Functions.ILike(v.Worker.PuestoCatalogo.Nombre, "%ALMACÉN%")))
                    ))
                .Select(v => v.Worker!.EmailCorporativo!)
                .Distinct()
                .ToListAsync();
            if (destinatarios.Count == 0) return 0;

            var filas = rechazados.Select(r =>
                $"<tr><td style='padding:6px;border:1px solid #ddd'>{System.Net.WebUtility.HtmlEncode(r.RecursoCrudo)}</td>" +
                $"<td style='padding:6px;border:1px solid #ddd'>{System.Net.WebUtility.HtmlEncode(r.Motivo ?? "No corresponde a material SSOMA")}</td></tr>");

            var body = $"""
                <p>Estimado equipo de <strong>{proyecto.ProjectDescription}</strong>,</p>
                <p>Los siguientes materiales del reporte S10 fueron revisados por SSOMA y <strong>no serán contabilizados</strong> en el presupuesto de materiales de seguridad:</p>
                <table style='border-collapse:collapse;width:100%;font-family:Arial,sans-serif;font-size:13px'>
                  <thead>
                    <tr style='background:#f0f0f0'>
                      <th style='padding:6px;border:1px solid #ddd;text-align:left'>Material S10</th>
                      <th style='padding:6px;border:1px solid #ddd;text-align:left'>Motivo de rechazo</th>
                    </tr>
                  </thead>
                  <tbody>{string.Join("", filas)}</tbody>
                </table>
                <p style='margin-top:16px;color:#666;font-size:12px'>Este es un mensaje automático del Sistema SSOMA — Grupo Abril.</p>
                """;

            await _email.SendAsync(
                destinatarios,
                $"[SSOMA] Materiales no contabilizados — {proyecto.ProjectDescription}",
                body,
                isHtml: true);

            return destinatarios.Count;
        }
        catch
        {
            return 0;
        }
    }
}
