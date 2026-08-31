using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class CatalogoMaterialesService : ICatalogoMaterialesService
{
    private readonly ICatalogoMaterialesRepository _repo;

    public CatalogoMaterialesService(ICatalogoMaterialesRepository repo)
    {
        _repo = repo;
    }

    public Task<List<FamiliaCatalogoDto>> ListarFamiliasAsync(string? q, int? tipoId, bool? perteneceSsoma)
        => _repo.ListarFamiliasDetalladoAsync(q, tipoId, perteneceSsoma);

    public Task ActualizarFamiliaAsync(int id, ActualizarFamiliaDto dto)
        => _repo.ActualizarFamiliaAsync(id, dto);

    public async Task<BuscarItemDto> CrearItemAsync(CrearItemCatalogoDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new Abril_Backend.Application.Exceptions.AbrilException("El nombre es obligatorio.", 400);

        var normalizado = TextoNormalizador.Normalizar(nombre);
        var (item, nombreFamilia, nombreTipo, perteneceSsoma) =
            await _repo.CrearItemManualAsync(nombre, normalizado, dto.FamiliaId);

        return new BuscarItemDto
        {
            Id = item.Id,
            Nombre = item.Nombre,
            NombreFamilia = nombreFamilia,
            TipoMaterial = nombreTipo,
            PerteneceSsoma = perteneceSsoma,
        };
    }

    public async Task<FamiliaCatalogoDto> CrearFamiliaAsync(CrearFamiliaCatalogoDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new Abril_Backend.Application.Exceptions.AbrilException("El nombre es obligatorio.", 400);
        if (string.IsNullOrWhiteSpace(dto.VariableBase))
            throw new Abril_Backend.Application.Exceptions.AbrilException("\"Se calcula por\" es obligatorio.", 400);

        var normalizado = TextoNormalizador.Normalizar(nombre);
        return await _repo.CrearFamiliaManualAsync(nombre, normalizado, dto.TipoId, dto.VariableBase, dto.UnidadMedida, dto.PerteneceSsoma);
    }

    public async Task<List<TipoMaterialDto>> ListarTiposAsync()
    {
        var tipos = await _repo.GetTiposAsync();
        return tipos.Select(t => new TipoMaterialDto { Id = t.Id, Nombre = t.Nombre })
            .OrderBy(t => t.Nombre).ToList();
    }

    public async Task<SeedCatalogoResultDto> SeedCatalogoAsync(SeedCatalogoRequestDto request)
    {
        var resultado = new SeedCatalogoResultDto();
        var tiposCache = new Dictionary<string, int>();

        foreach (var fila in request.Items)
        {
            if (string.IsNullOrWhiteSpace(fila.Recurso) || string.IsNullOrWhiteSpace(fila.NomStd1)
                || string.IsNullOrWhiteSpace(fila.NomStd2) || string.IsNullOrWhiteSpace(fila.TipoMaterial))
            {
                resultado.Advertencias.Add($"Fila incompleta omitida: '{fila.Recurso}'");
                continue;
            }

            // Tipo (normalizado: "Varios"/"VARIOS" colapsan al mismo)
            var tipoNorm = TextoNormalizador.Normalizar(fila.TipoMaterial);
            if (!tiposCache.TryGetValue(tipoNorm, out var tipoId))
            {
                var tipo = await _repo.GetOrCreateTipoAsync(tipoNorm);
                tipoId = tipo.Id;
                tiposCache[tipoNorm] = tipoId;
                resultado.TiposCreados++;
            }

            // Familia = NomStd2 (nivel de agrupación/ratio)
            var familiaNombreNorm = TextoNormalizador.Normalizar(fila.NomStd2);
            var variableBaseNorm = TextoNormalizador.Normalizar(fila.VariableBase);
            var (familia, familiaCreada) = await _repo.GetOrCreateFamiliaAsync(
                fila.NomStd2.Trim(), familiaNombreNorm, tipoId, variableBaseNorm, fila.PerteneceSsoma);

            if (familiaCreada) resultado.FamiliasCreadas++;
            else resultado.FamiliasExistentes++;

            // Item = NomStd1, con talla/dimensión extraídas para no fragmentar la familia
            var itemNombreNorm = TextoNormalizador.Normalizar(fila.NomStd1);
            var (sinTalla, talla) = TextoNormalizador.ExtraerTalla(itemNombreNorm);
            var (sinDimension, dimensionNorm) = TextoNormalizador.ExtraerDimension(sinTalla);
            var noUsar = TextoNormalizador.TieneNoUsar(fila.Recurso) || TextoNormalizador.TieneNoUsar(fila.NomStd1);

            var (item, itemCreado) = await _repo.GetOrCreateItemAsync(
                fila.NomStd1.Trim(), sinDimension, familia.Id, talla, dimensionNorm, noUsar);

            if (itemCreado) resultado.ItemsCreados++;
            else resultado.ItemsExistentes++;

            // Alias: el texto crudo original (Recurso del S10) -> item estandarizado
            var recursoNorm = TextoNormalizador.Normalizar(fila.Recurso);
            var aliasCreado = await _repo.CreateAliasIfNotExistsAsync(
                fila.Recurso.Trim(), recursoNorm, item.Id, "SEED", confianza: 1.0m,
                factorConversion: fila.CantidadComprada);

            if (aliasCreado) resultado.AliasCreados++;
        }

        return resultado;
    }
}
