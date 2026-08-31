using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.OptFeature.Infrastructure.Models;
using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PetsFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.PetsFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PetsFeature.Infrastructure.Repositories;

public class PetsRepository : IPetsRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public PetsRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<PetListItemDto>> GetListAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaPet
            .OrderBy(p => p.Nombre)
            .Select(p => new PetListItemDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Codigo = p.Codigo,
                Activo = p.Activo,
                TotalPasos = p.Pasos.Count(x => x.Activo),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    // Secciones narrativas: un solo bloque de texto cada una (no árbol).
    private static readonly string[] SeccionesTextoKeys =
        ["introduccion", "alcance", "objetivo", "definiciones", "restricciones"];

    private static readonly string[] RolesFirma = ["elaborado", "revisado", "aprobado"];

    private static PetFirmaDto MapFirma(string rol, SsomaPetFirma? f) => new()
    {
        Rol = rol,
        Nombre = f?.Nombre,
        Cargo = f?.Cargo,
        Fecha = f?.Fecha,
        FirmaUrl = f?.FirmaUrl
    };

    private static PetPasoDto MapPaso(SsomaPetPaso x) => new()
    {
        Id = x.Id,
        ParentId = x.ParentId,
        Tipo = x.Tipo,
        Descripcion = x.Descripcion,
        ImagenUrl = x.ImagenUrl,
        Orden = x.Orden
    };

    private static PetItemSeleccionadoDto MapSeleccion(SsomaPetItemSeleccionado x) => new()
    {
        Id = x.Id,
        Grupo = x.Grupo,
        Tipo = x.Tipo,
        CatalogoItemId = x.CatalogoItemId,
        Descripcion = x.CatalogoItem?.Descripcion ?? x.DescripcionPersonalizada ?? string.Empty,
        EsPersonalizado = x.CatalogoItemId == null,
        Orden = x.Orden
    };

    public async Task<PetDetalleDto?> GetDetalleAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FirstOrDefaultAsync(p => p.Id == id);
        if (pet == null) return null;

        var todosPasos = await ctx.SsomaPetPaso
            .Where(x => x.PetId == id && x.Activo)
            .OrderBy(x => x.Orden)
            .ToListAsync();

        var pasosPorSeccion = todosPasos.GroupBy(x => x.Seccion)
            .ToDictionary(g => g.Key, g => g.Select(MapPaso).ToList());

        var textosPorSeccion = await ctx.SsomaPetSeccionTexto
            .Where(x => x.PetId == id)
            .ToDictionaryAsync(x => x.Seccion, x => x.Contenido);

        var seleccionadosEntidades = await ctx.SsomaPetItemSeleccionado
            .Include(x => x.CatalogoItem)
            .Where(x => x.PetId == id && x.Activo)
            .OrderBy(x => x.Orden)
            .ToListAsync();
        var seleccionados = seleccionadosEntidades.Select(MapSeleccion).ToList();

        var anexos = await ctx.SsomaPetAnexo
            .Where(x => x.PetId == id && x.Activo)
            .OrderBy(x => x.Orden)
            .Select(x => new PetAnexoDto { Id = x.Id, Nombre = x.Nombre, ArchivoUrl = x.ArchivoUrl, Orden = x.Orden })
            .ToListAsync();

        var firmasPorRol = await ctx.SsomaPetFirma
            .Where(x => x.PetId == id)
            .ToDictionaryAsync(x => x.Rol);

        return new PetDetalleDto
        {
            Id = pet.Id,
            Nombre = pet.Nombre,
            Codigo = pet.Codigo,
            SharepointUrl = pet.SharepointUrl,
            Activo = pet.Activo,
            Pasos = pasosPorSeccion.GetValueOrDefault("procedimiento") ?? [],
            Responsabilidades = pasosPorSeccion.GetValueOrDefault("responsabilidades") ?? [],
            SeccionesTexto = SeccionesTextoKeys.ToDictionary(s => s, s => textosPorSeccion.GetValueOrDefault(s) ?? ""),
            MarcoLegal = seleccionados.Where(x => x.Grupo == "marco_legal").ToList(),
            Epp = seleccionados.Where(x => x.Grupo == "epp").ToList(),
            Recursos = seleccionados.Where(x => x.Grupo == "recurso").ToList(),
            Anexos = anexos,
            Firmas = RolesFirma.ToDictionary(r => r, r => MapFirma(r, firmasPorRol.GetValueOrDefault(r)))
        };
    }

    public async Task<List<PetPasoDto>> GetPasosAsync(int petId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaPetPaso
            .Where(x => x.PetId == petId && x.Activo && x.Seccion == "procedimiento")
            .OrderBy(x => x.Orden)
            .Select(x => new PetPasoDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Tipo = x.Tipo,
                Descripcion = x.Descripcion,
                ImagenUrl = x.ImagenUrl,
                Orden = x.Orden
            })
            .ToListAsync();
    }

    public async Task<int> CrearAsync(CrearPetRequest request)
    {
        using var ctx = _factory.CreateDbContext();
        var pet = new SsomaPet
        {
            Nombre = request.Nombre,
            Codigo = request.Codigo,
            SharepointUrl = request.SharepointUrl,
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaPet.Add(pet);
        await ctx.SaveChangesAsync();
        return pet.Id;
    }

    public async Task ActualizarAsync(int id, ActualizarPetRequest request)
    {
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FindAsync(id)
            ?? throw new AbrilException("PETS no encontrado.", 404);

        pet.Nombre = request.Nombre;
        pet.Codigo = request.Codigo;
        pet.SharepointUrl = request.SharepointUrl;
        pet.Activo = request.Activo;
        pet.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    private static readonly HashSet<string> TiposValidos = ["subtitulo", "paso", "letra", "guion"];

    // Solo estas dos secciones usan el árbol de pasos — el resto (Introducción,
    // Alcance, Objetivo, Definiciones, Restricciones) son bloques de texto único.
    private static readonly HashSet<string> SeccionesValidas = ["procedimiento", "responsabilidades"];

    private static string ValidarTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return "paso";
        if (!TiposValidos.Contains(tipo))
            throw new AbrilException($"Tipo de paso inválido: '{tipo}'.", 400);
        return tipo;
    }

    private static string ValidarSeccion(string? seccion)
    {
        if (string.IsNullOrWhiteSpace(seccion)) return "procedimiento";
        if (!SeccionesValidas.Contains(seccion))
            throw new AbrilException($"Sección inválida: '{seccion}'.", 400);
        return seccion;
    }

    public async Task<int> AgregarPasoAsync(int petId, CrearPetPasoRequest request)
    {
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FindAsync(petId)
            ?? throw new AbrilException("PETS no encontrado.", 404);

        var seccion = ValidarSeccion(request.Seccion);

        if (request.ParentId.HasValue)
        {
            var padreExiste = await ctx.SsomaPetPaso.AnyAsync(p => p.Id == request.ParentId.Value && p.PetId == petId && p.Seccion == seccion && p.Activo);
            if (!padreExiste) throw new AbrilException("El subtítulo padre no existe.", 404);
        }

        // "Hermanos": solo los pasos con el MISMO ParentId dentro de la MISMA sección —
        // cada grupo se ordena independiente, insertar/reordenar dentro de un subtítulo
        // no toca a otro, ni a otra sección.
        var hermanos = await ctx.SsomaPetPaso
            .Where(p => p.PetId == petId && p.Activo && p.Seccion == seccion && p.ParentId == request.ParentId)
            .OrderBy(p => p.Orden)
            .ToListAsync();

        var maxPosicion = hermanos.Count + 1;
        var posicion = request.Posicion.HasValue && request.Posicion.Value >= 1 && request.Posicion.Value <= maxPosicion
            ? request.Posicion.Value
            : maxPosicion;

        foreach (var p in hermanos.Where(p => p.Orden >= posicion))
        {
            p.Orden += 1;
            p.UpdatedAt = DateTime.UtcNow;
        }

        var nuevo = new SsomaPetPaso
        {
            PetId = petId,
            Seccion = seccion,
            ParentId = request.ParentId,
            Tipo = ValidarTipo(request.Tipo),
            Descripcion = request.Descripcion,
            Orden = posicion,
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaPetPaso.Add(nuevo);
        await ctx.SaveChangesAsync();
        return nuevo.Id;
    }

    public async Task ActualizarPasoAsync(int petId, int pasoId, ActualizarPetPasoRequest request)
    {
        using var ctx = _factory.CreateDbContext();
        var paso = await ctx.SsomaPetPaso.FirstOrDefaultAsync(p => p.Id == pasoId && p.PetId == petId)
            ?? throw new AbrilException("Paso no encontrado.", 404);

        paso.Descripcion = request.Descripcion;
        paso.Tipo = ValidarTipo(request.Tipo);
        paso.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task EliminarPasoAsync(int petId, int pasoId)
    {
        using var ctx = _factory.CreateDbContext();
        var paso = await ctx.SsomaPetPaso.FirstOrDefaultAsync(p => p.Id == pasoId && p.PetId == petId)
            ?? throw new AbrilException("Paso no encontrado.", 404);

        var tieneHijos = await ctx.SsomaPetPaso.AnyAsync(p => p.ParentId == pasoId && p.Activo);
        if (tieneHijos)
            throw new AbrilException("Este subtítulo tiene pasos dentro — elimínalos primero.", 400);

        paso.Activo = false;
        paso.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task ReordenarPasosAsync(int petId, ReordenarPasosRequest request)
    {
        using var ctx = _factory.CreateDbContext();
        var seccion = ValidarSeccion(request.Seccion);
        var pasos = await ctx.SsomaPetPaso
            .Where(p => p.PetId == petId && p.Activo && p.Seccion == seccion && p.ParentId == request.ParentId)
            .ToListAsync();

        var porId = pasos.ToDictionary(p => p.Id);
        for (int i = 0; i < request.PasoIds.Count; i++)
        {
            if (porId.TryGetValue(request.PasoIds[i], out var paso))
            {
                paso.Orden = i + 1;
                paso.UpdatedAt = DateTime.UtcNow;
            }
        }
        await ctx.SaveChangesAsync();
    }

    public async Task SetImagenPasoAsync(int petId, int pasoId, string? imagenUrl)
    {
        using var ctx = _factory.CreateDbContext();
        var paso = await ctx.SsomaPetPaso.FirstOrDefaultAsync(p => p.Id == pasoId && p.PetId == petId)
            ?? throw new AbrilException("Paso no encontrado.", 404);

        paso.ImagenUrl = imagenUrl;
        paso.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    // Usado al reimportar un Word corregido: borra (desactiva) todo lo que había
    // en esa sección antes de insertar los pasos nuevos, en vez de acumularlos.
    public async Task DesactivarSeccionAsync(int petId, string seccion)
    {
        seccion = ValidarSeccion(seccion);
        using var ctx = _factory.CreateDbContext();
        var pasos = await ctx.SsomaPetPaso
            .Where(p => p.PetId == petId && p.Seccion == seccion && p.Activo)
            .ToListAsync();

        foreach (var p in pasos)
        {
            p.Activo = false;
            p.UpdatedAt = DateTime.UtcNow;
        }
        await ctx.SaveChangesAsync();
    }

    // ── Secciones de texto único (Introducción / Alcance / Objetivo / Definiciones / Restricciones) ──

    private static string ValidarSeccionTexto(string seccion)
    {
        if (!SeccionesTextoKeys.Contains(seccion))
            throw new AbrilException($"Sección de texto inválida: '{seccion}'.", 400);
        return seccion;
    }

    public async Task UpsertSeccionTextoAsync(int petId, string seccion, string contenido)
    {
        seccion = ValidarSeccionTexto(seccion);
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FindAsync(petId)
            ?? throw new AbrilException("PETS no encontrado.", 404);

        var fila = await ctx.SsomaPetSeccionTexto.FirstOrDefaultAsync(x => x.PetId == petId && x.Seccion == seccion);
        if (fila == null)
        {
            ctx.SsomaPetSeccionTexto.Add(new SsomaPetSeccionTexto
            {
                PetId = petId,
                Seccion = seccion,
                Contenido = contenido,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            fila.Contenido = contenido;
            fila.UpdatedAt = DateTime.UtcNow;
        }
        await ctx.SaveChangesAsync();
    }

    // ── Catálogo (Marco Legal / EPP / Recursos) ──────────────────────────────────

    private static readonly HashSet<string> GruposValidos = ["marco_legal", "epp", "recurso"];
    private static readonly Dictionary<string, HashSet<string>> TiposPorGrupo = new()
    {
        ["epp"] = ["basico", "especifico", "emergencia"],
        ["recurso"] = ["equipo", "herramienta", "material"]
    };

    private static void ValidarGrupoTipo(string grupo, string? tipo)
    {
        if (!GruposValidos.Contains(grupo))
            throw new AbrilException($"Grupo de catálogo inválido: '{grupo}'.", 400);

        if (TiposPorGrupo.TryGetValue(grupo, out var tiposValidos))
        {
            if (string.IsNullOrWhiteSpace(tipo) || !tiposValidos.Contains(tipo))
                throw new AbrilException($"Tipo inválido para el grupo '{grupo}'.", 400);
        }
        else if (!string.IsNullOrWhiteSpace(tipo))
        {
            throw new AbrilException($"El grupo '{grupo}' no admite tipo.", 400);
        }
    }

    public async Task<List<CatalogoItemDto>> GetCatalogoAsync(string grupo, string? tipo)
    {
        ValidarGrupoTipo(grupo, tipo);
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaCatalogoItem
            .Where(x => x.Grupo == grupo && x.Tipo == tipo && x.Activo)
            .OrderBy(x => x.Orden).ThenBy(x => x.Descripcion)
            .Select(x => new CatalogoItemDto { Id = x.Id, Grupo = x.Grupo, Tipo = x.Tipo, Descripcion = x.Descripcion, Activo = x.Activo, Orden = x.Orden })
            .ToListAsync();
    }

    public async Task<int> CrearCatalogoItemAsync(CrearCatalogoItemRequest request)
    {
        ValidarGrupoTipo(request.Grupo, request.Tipo);
        using var ctx = _factory.CreateDbContext();
        var item = new SsomaCatalogoItem
        {
            Grupo = request.Grupo,
            Tipo = request.Tipo,
            Descripcion = request.Descripcion,
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaCatalogoItem.Add(item);
        await ctx.SaveChangesAsync();
        return item.Id;
    }

    public async Task DesactivarCatalogoItemAsync(int catalogoItemId)
    {
        using var ctx = _factory.CreateDbContext();
        var item = await ctx.SsomaCatalogoItem.FindAsync(catalogoItemId)
            ?? throw new AbrilException("Ítem de catálogo no encontrado.", 404);

        item.Activo = false;
        item.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<int> SeleccionarCatalogoItemAsync(int petId, SeleccionarItemCatalogoRequest request)
    {
        ValidarGrupoTipo(request.Grupo, request.Tipo);
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FindAsync(petId)
            ?? throw new AbrilException("PETS no encontrado.", 404);

        var catalogoItem = await ctx.SsomaCatalogoItem.FirstOrDefaultAsync(x => x.Id == request.CatalogoItemId && x.Activo)
            ?? throw new AbrilException("Ítem de catálogo no encontrado.", 404);

        var yaSeleccionado = await ctx.SsomaPetItemSeleccionado
            .AnyAsync(x => x.PetId == petId && x.CatalogoItemId == request.CatalogoItemId && x.Activo);
        if (yaSeleccionado) throw new AbrilException("Este ítem ya está seleccionado.", 400);

        var maxOrden = await ctx.SsomaPetItemSeleccionado
            .Where(x => x.PetId == petId && x.Grupo == request.Grupo && x.Activo)
            .Select(x => (int?)x.Orden).MaxAsync() ?? 0;

        var nuevo = new SsomaPetItemSeleccionado
        {
            PetId = petId,
            Grupo = request.Grupo,
            Tipo = catalogoItem.Tipo,
            CatalogoItemId = catalogoItem.Id,
            Orden = maxOrden + 1,
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaPetItemSeleccionado.Add(nuevo);
        await ctx.SaveChangesAsync();
        return nuevo.Id;
    }

    public async Task<int> AgregarItemPersonalizadoAsync(int petId, AgregarItemPersonalizadoRequest request)
    {
        ValidarGrupoTipo(request.Grupo, request.Tipo);
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FindAsync(petId)
            ?? throw new AbrilException("PETS no encontrado.", 404);

        int? catalogoItemId = null;
        if (request.AgregarAlCatalogoGlobal)
        {
            var catalogoItem = new SsomaCatalogoItem
            {
                Grupo = request.Grupo,
                Tipo = request.Tipo,
                Descripcion = request.Descripcion,
                Activo = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.SsomaCatalogoItem.Add(catalogoItem);
            await ctx.SaveChangesAsync();
            catalogoItemId = catalogoItem.Id;
        }

        var maxOrden = await ctx.SsomaPetItemSeleccionado
            .Where(x => x.PetId == petId && x.Grupo == request.Grupo && x.Activo)
            .Select(x => (int?)x.Orden).MaxAsync() ?? 0;

        var nuevo = new SsomaPetItemSeleccionado
        {
            PetId = petId,
            Grupo = request.Grupo,
            Tipo = request.Tipo,
            CatalogoItemId = catalogoItemId,
            DescripcionPersonalizada = catalogoItemId == null ? request.Descripcion : null,
            Orden = maxOrden + 1,
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaPetItemSeleccionado.Add(nuevo);
        await ctx.SaveChangesAsync();
        return nuevo.Id;
    }

    public async Task EliminarSeleccionAsync(int petId, int seleccionId)
    {
        using var ctx = _factory.CreateDbContext();
        var seleccion = await ctx.SsomaPetItemSeleccionado.FirstOrDefaultAsync(x => x.Id == seleccionId && x.PetId == petId)
            ?? throw new AbrilException("Selección no encontrada.", 404);

        seleccion.Activo = false;
        await ctx.SaveChangesAsync();
    }

    // ── Anexos ────────────────────────────────────────────────────────────────

    public async Task<int> AgregarAnexoAsync(int petId, string nombre, string archivoUrl)
    {
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FindAsync(petId)
            ?? throw new AbrilException("PETS no encontrado.", 404);

        var maxOrden = await ctx.SsomaPetAnexo
            .Where(x => x.PetId == petId && x.Activo)
            .Select(x => (int?)x.Orden).MaxAsync() ?? 0;

        var nuevo = new SsomaPetAnexo
        {
            PetId = petId,
            Nombre = nombre,
            ArchivoUrl = archivoUrl,
            Orden = maxOrden + 1,
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaPetAnexo.Add(nuevo);
        await ctx.SaveChangesAsync();
        return nuevo.Id;
    }

    public async Task EliminarAnexoAsync(int petId, int anexoId)
    {
        using var ctx = _factory.CreateDbContext();
        var anexo = await ctx.SsomaPetAnexo.FirstOrDefaultAsync(x => x.Id == anexoId && x.PetId == petId)
            ?? throw new AbrilException("Anexo no encontrado.", 404);

        anexo.Activo = false;
        await ctx.SaveChangesAsync();
    }

    // ── Firmas (Elaborado por / Revisado por / Aprobado por) ────────────────────

    private static string ValidarRolFirma(string rol)
    {
        if (!RolesFirma.Contains(rol))
            throw new AbrilException($"Rol de firma inválido: '{rol}'.", 400);
        return rol;
    }

    public async Task UpsertFirmaAsync(int petId, string rol, string? nombre, string? cargo, DateOnly? fecha)
    {
        rol = ValidarRolFirma(rol);
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FindAsync(petId)
            ?? throw new AbrilException("PETS no encontrado.", 404);

        var fila = await ctx.SsomaPetFirma.FirstOrDefaultAsync(x => x.PetId == petId && x.Rol == rol);
        if (fila == null)
        {
            ctx.SsomaPetFirma.Add(new SsomaPetFirma
            {
                PetId = petId,
                Rol = rol,
                Nombre = nombre,
                Cargo = cargo,
                Fecha = fecha,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            fila.Nombre = nombre;
            fila.Cargo = cargo;
            fila.Fecha = fecha;
            fila.UpdatedAt = DateTime.UtcNow;
        }
        await ctx.SaveChangesAsync();
    }

    public async Task SetFirmaUrlAsync(int petId, string rol, string firmaUrl)
    {
        rol = ValidarRolFirma(rol);
        using var ctx = _factory.CreateDbContext();
        var pet = await ctx.SsomaPet.FindAsync(petId)
            ?? throw new AbrilException("PETS no encontrado.", 404);

        var fila = await ctx.SsomaPetFirma.FirstOrDefaultAsync(x => x.PetId == petId && x.Rol == rol);
        if (fila == null)
        {
            ctx.SsomaPetFirma.Add(new SsomaPetFirma { PetId = petId, Rol = rol, FirmaUrl = firmaUrl, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            fila.FirmaUrl = firmaUrl;
            fila.UpdatedAt = DateTime.UtcNow;
        }
        await ctx.SaveChangesAsync();
    }
}
