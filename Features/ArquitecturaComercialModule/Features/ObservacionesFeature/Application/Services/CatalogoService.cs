using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Services;

public class CatalogoService : ICatalogoService
{
    private readonly ICatalogoRepository _repo;

    public CatalogoService(ICatalogoRepository repo) => _repo = repo;

    private static string NormalizarTipo(string tipo)
    {
        var normalizado = tipo.Trim().ToLowerInvariant() switch
        {
            "partidas" => AcCatalogoTipo.Partida,
            "areas-responsables" => AcCatalogoTipo.AreaResponsable,
            "lugares-revision" => AcCatalogoTipo.LugarRevision,
            _ => tipo,
        };
        if (!AcCatalogoTipo.EsValido(normalizado))
            throw new AbrilException("Tipo de catálogo inválido. Use 'partidas', 'areas-responsables' o 'lugares-revision'.", 400);
        return normalizado;
    }

    public async Task<List<CatalogoItemDTO>> GetByTipo(string tipo, bool soloActivos)
    {
        var items = await _repo.GetByTipo(NormalizarTipo(tipo), soloActivos);
        return items.Select(Map).ToList();
    }

    public async Task<CatalogoItemDTO> Create(string tipo, CreateCatalogoItemDTO body)
    {
        if (string.IsNullOrWhiteSpace(body.Nombre))
            throw new AbrilException("El nombre es obligatorio.", 400);
        var item = await _repo.Create(NormalizarTipo(tipo), body.Nombre.Trim());
        return Map(item);
    }

    public async Task<CatalogoItemDTO?> Update(int id, UpdateCatalogoItemDTO body)
    {
        if (string.IsNullOrWhiteSpace(body.Nombre))
            throw new AbrilException("El nombre es obligatorio.", 400);
        var item = await _repo.Update(id, body.Nombre.Trim(), body.Activo);
        return item == null ? null : Map(item);
    }

    public Task<bool> Delete(int id) => _repo.Delete(id);

    private static CatalogoItemDTO Map(AcCatalogoItem i) => new()
    {
        Id = i.Id,
        Nombre = i.Nombre,
        Orden = i.Orden,
        Activo = i.Activo,
    };
}
