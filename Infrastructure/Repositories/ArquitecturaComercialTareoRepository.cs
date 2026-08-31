using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs.ArquitecturaComercial;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Infrastructure.Repositories
{
    public class ArquitecturaComercialTareoRepository : IArquitecturaComercialTareoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        // Zona horaria Perú: fija (sin DST), evita depender de la config del SO del servidor.
        private static readonly TimeZoneInfo PeruTz = TimeZoneInfo.CreateCustomTimeZone(
            "PERU", TimeSpan.FromHours(-5), "Hora de Perú", "Hora de Perú");

        private static readonly string[] TiposValidos =
            ["INICIO_JORNADA", "INICIO_ALMUERZO", "RETORNO", "FIN_JORNADA"];

        public ArquitecturaComercialTareoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<int> ResolverWorkerId(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var person = await ctx.Person.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new AbrilException("No tienes un perfil de persona asociado a tu usuario.", 403);

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.PersonId == person.PersonId)
                ?? throw new AbrilException("No tienes un perfil de trabajador registrado en el sistema.", 403);

            return worker.Id;
        }

        public async Task<TareoEnrolamientoEstadoDTO> GetEnrolamientoEstado(int workerId)
        {
            using var ctx = _factory.CreateDbContext();
            var enrolamiento = await ctx.AcTareoEnrolamiento
                .Where(e => e.WorkerId == workerId && e.Activo)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            return new TareoEnrolamientoEstadoDTO
            {
                Enrolado = enrolamiento != null,
                FechaEnrolamiento = enrolamiento?.CreatedAt,
            };
        }

        /// <summary>Ids de los "obreros de Arquitectura Comercial": el criterio es que la
        /// vinculación activa más reciente del trabajador (FechaFin == null, mismo criterio que
        /// ArquitecturaComercialRepository.GetSupervisoresAc/HabTrabajadorRepository) apunte a un
        /// Project cuyo ProjectDescription sea "arquitectura comercial". Este es el universo tanto
        /// para la pantalla de enrolamiento del coordinador como para la identificación 1:N —
        /// deliberadamente chico (~40 personas) para que el 1:N sea rápido y preciso.</summary>
        private static IQueryable<int> GetObrerosAcWorkerIdsQuery(AppDbContext ctx)
        {
            var vinculacionActivaPorWorker = ctx.WorkerVinculacion
                .Where(v => v.FechaFin == null)
                .Where(v => !ctx.WorkerVinculacion.Any(otra =>
                    otra.WorkerId == v.WorkerId &&
                    otra.FechaFin == null &&
                    (otra.CreatedAt > v.CreatedAt || (otra.CreatedAt == v.CreatedAt && otra.Id > v.Id))));

            return vinculacionActivaPorWorker
                .Where(v => v.ProyectoId != null)
                .Join(ctx.Project.Where(p => p.ProjectDescription != null && p.ProjectDescription.ToLower() == "arquitectura comercial"),
                      v => v.ProyectoId, p => p.ProjectId, (v, p) => v.WorkerId);
        }

        public async Task<List<TareoTrabajadorEnrolamientoDTO>> GetTrabajadoresParaEnrolar()
        {
            using var ctx = _factory.CreateDbContext();
            var obrerosIds = GetObrerosAcWorkerIdsQuery(ctx);

            var workers = await ctx.Worker
                .Where(w => w.WorkersEstadoId == WorkersEstadoIds.Activo && obrerosIds.Contains(w.Id))
                .OrderBy(w => w.Person != null ? w.Person.FullName : null)
                .Select(w => new { w.Id, Nombre = w.Person != null ? w.Person.FullName : null })
                .ToListAsync();

            var workerIds = workers.Select(w => w.Id).ToList();
            var enrolamientos = await ctx.AcTareoEnrolamiento
                .Where(e => workerIds.Contains(e.WorkerId) && e.Activo)
                .ToListAsync();
            var enrolamientoMap = enrolamientos.ToDictionary(e => e.WorkerId, e => e.CreatedAt);

            var autorizaciones = await ctx.AcTareoAutorizacion
                .Where(a => workerIds.Contains(a.WorkerId))
                .ToListAsync();
            var autorizacionMap = autorizaciones.ToDictionary(a => a.WorkerId, a => a);

            return workers.Select(w => new TareoTrabajadorEnrolamientoDTO
            {
                WorkerId = w.Id,
                Nombre = w.Nombre ?? $"Worker {w.Id}",
                Enrolado = enrolamientoMap.ContainsKey(w.Id),
                FechaEnrolamiento = enrolamientoMap.GetValueOrDefault(w.Id),
                AutorizacionSubida = autorizacionMap.ContainsKey(w.Id),
                AutorizacionSubidaEn = autorizacionMap.GetValueOrDefault(w.Id)?.SubidoEn,
                AutorizacionUrl = autorizacionMap.GetValueOrDefault(w.Id)?.UrlDocumento,
            }).ToList();
        }

        /// <summary>Mismo universo que GetTrabajadoresParaEnrolar, pero para la pantalla de
        /// autoservicio de la cuenta compartida de campo (operarios): solo trabajadores con el
        /// SSO-FO-150 ya subido por el coordinador, y sin exponer AutorizacionUrl (documento de
        /// terceros) a esa cuenta.</summary>
        public async Task<List<TareoTrabajadorEnrolamientoDTO>> GetTrabajadoresDisponiblesParaEnrolar()
        {
            var todos = await GetTrabajadoresParaEnrolar();
            return todos
                .Where(t => t.AutorizacionSubida)
                .Select(t => new TareoTrabajadorEnrolamientoDTO
                {
                    WorkerId = t.WorkerId,
                    Nombre = t.Nombre,
                    Enrolado = t.Enrolado,
                    FechaEnrolamiento = t.FechaEnrolamiento,
                    AutorizacionSubida = t.AutorizacionSubida,
                    AutorizacionSubidaEn = t.AutorizacionSubidaEn,
                    AutorizacionUrl = null,
                })
                .ToList();
        }

        public async Task<List<TareoProyectoGeoDTO>> GetProyectosGeo()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Project
                .Where(p => p.Active)
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new TareoProyectoGeoDTO
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    Lat = p.Lat,
                    Lng = p.Lng,
                    RadioGeofenceMetros = p.RadioGeofenceMetros,
                })
                .ToListAsync();
        }

        public async Task SetProyectoGeo(int projectId, TareoProyectoGeoUpdateDTO dto)
        {
            using var ctx = _factory.CreateDbContext();
            var project = await ctx.Project.FirstOrDefaultAsync(p => p.ProjectId == projectId)
                ?? throw new AbrilException("Proyecto no encontrado.", 404);

            project.Lat = dto.Lat;
            project.Lng = dto.Lng;
            if (dto.RadioGeofenceMetros.HasValue)
                project.RadioGeofenceMetros = dto.RadioGeofenceMetros.Value;

            await ctx.SaveChangesAsync();
        }

        public async Task<TareoAutorizacionDetalleDTO> GetAutorizacionDetalle(int workerId)
        {
            using var ctx = _factory.CreateDbContext();
            var worker = await ctx.Worker.Include(w => w.Person).FirstOrDefaultAsync(w => w.Id == workerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            return new TareoAutorizacionDetalleDTO
            {
                WorkerId = worker.Id,
                Nombre = worker.Person?.FullName ?? $"Worker {worker.Id}",
                Dni = worker.Person?.DocumentIdentityCode,
            };
        }

        public async Task<bool> TieneAutorizacion(int workerId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.AcTareoAutorizacion.AnyAsync(a => a.WorkerId == workerId);
        }

        public async Task SetAutorizacion(int workerId, string urlDocumento, int? subidoPorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            var existente = await ctx.AcTareoAutorizacion.FirstOrDefaultAsync(a => a.WorkerId == workerId);

            if (existente != null)
            {
                existente.UrlDocumento = urlDocumento;
                existente.SubidoPorUserId = subidoPorUserId;
                existente.SubidoEn = DateTime.UtcNow;
            }
            else
            {
                ctx.AcTareoAutorizacion.Add(new AcTareoAutorizacion
                {
                    WorkerId = workerId,
                    UrlDocumento = urlDocumento,
                    SubidoPorUserId = subidoPorUserId,
                    SubidoEn = DateTime.UtcNow,
                });
            }

            await ctx.SaveChangesAsync();
        }

        /// <summary>Identificación 1:N: busca, entre los enrolados que son "obreros AC", cuál
        /// tiene el embedding más parecido al capturado. Doble umbral de seguridad — nunca basta
        /// con el mejor score solo: (1) el mejor debe superar el umbral de match, Y (2) debe
        /// sacarle una ventaja clara al segundo lugar (margen), para evitar identificar a la
        /// persona equivocada cuando dos rostros dan scores parecidos.</summary>
        public async Task<TareoIdentificacionDTO> IdentificarPorEmbedding(float[] embedding)
        {
            const decimal umbralMatch = 0.5m;
            const decimal margenMinimo = 0.08m;

            using var ctx = _factory.CreateDbContext();
            var obrerosIds = await GetObrerosAcWorkerIdsQuery(ctx).ToListAsync();

            var enrolamientos = await ctx.AcTareoEnrolamiento
                .Where(e => e.Activo && obrerosIds.Contains(e.WorkerId))
                .ToListAsync();

            var candidatos = enrolamientos
                .Select(e => new { e.WorkerId, Score = (decimal)CompararEmbeddings(e.Embedding, embedding) })
                .OrderByDescending(c => c.Score)
                .ToList();

            var mejor = candidatos.FirstOrDefault();
            if (mejor == null || mejor.Score < umbralMatch)
                return new TareoIdentificacionDTO { Identificado = false };

            var segundo = candidatos.Skip(1).FirstOrDefault();
            if (segundo != null && (mejor.Score - segundo.Score) < margenMinimo)
                return new TareoIdentificacionDTO { Identificado = false };

            var worker = await ctx.Worker.Include(w => w.Person).FirstOrDefaultAsync(w => w.Id == mejor.WorkerId);
            return new TareoIdentificacionDTO
            {
                Identificado = true,
                WorkerId = mejor.WorkerId,
                Nombre = worker?.Person?.FullName ?? $"Worker {mejor.WorkerId}",
            };
        }

        public async Task<bool> EsObreroAc(int workerId)
        {
            using var ctx = _factory.CreateDbContext();
            return await GetObrerosAcWorkerIdsQuery(ctx).ContainsAsync(workerId);
        }

        public async Task EnrolarWorker(int workerId, string fotoUrl, float[] embedding)
        {
            using var ctx = _factory.CreateDbContext();
            var existente = await ctx.AcTareoEnrolamiento.FirstOrDefaultAsync(e => e.WorkerId == workerId);

            if (existente != null)
            {
                existente.Embedding = embedding;
                existente.FotoUrl = fotoUrl;
                existente.Activo = true;
                existente.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                ctx.AcTareoEnrolamiento.Add(new AcTareoEnrolamiento
                {
                    WorkerId = workerId,
                    Embedding = embedding,
                    FotoUrl = fotoUrl,
                    ConsentimientoEn = DateTime.UtcNow,
                    Activo = true,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<TareoRegistroDTO> Marcar(
            int workerId, Guid idempotencyKey, TareoMarcarRequestDTO body,
            string fotoUrl, string fotoHash, string? ipOrigen)
        {
            if (!TiposValidos.Contains(body.Tipo))
                throw new AbrilException("Tipo de marcación inválido.", 400);

            using var ctx = _factory.CreateDbContext();

            // Idempotencia: si esta misma clave ya se procesó (reintento de red), devolver el resultado ya guardado.
            var existentePorKey = await ctx.AcTareoRegistro.FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);
            if (existentePorKey != null)
                return await ToDto(ctx, existentePorKey, yaExistia: true);

            var horaServidor = DateTime.UtcNow;
            var fechaPeru = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(horaServidor, PeruTz));

            // Enrolamiento
            var enrolamiento = await ctx.AcTareoEnrolamiento
                .Where(e => e.WorkerId == workerId && e.Activo)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            // Geofencing: proyecto activo más cercano dentro de su radio configurado.
            int? projectId = null;
            decimal? distanciaMetros = null;
            var motivos = new List<string>();

            if (body.Lat.HasValue && body.Lng.HasValue)
            {
                var proyectosConGeo = await ctx.Project
                    .Where(p => p.Lat.HasValue && p.Lng.HasValue && p.Active)
                    .Select(p => new { p.ProjectId, p.Lat, p.Lng, p.RadioGeofenceMetros })
                    .ToListAsync();

                var candidatos = proyectosConGeo
                    .Select(p => new
                    {
                        p.ProjectId,
                        p.RadioGeofenceMetros,
                        Distancia = HaversineMetros((double)body.Lat.Value, (double)body.Lng.Value, (double)p.Lat!.Value, (double)p.Lng!.Value),
                    })
                    .OrderBy(p => p.Distancia)
                    .ToList();

                var masCercano = candidatos.FirstOrDefault();
                if (masCercano != null)
                {
                    projectId = masCercano.ProjectId;
                    distanciaMetros = (decimal)Math.Round(masCercano.Distancia, 2);
                    if (masCercano.Distancia > (double)masCercano.RadioGeofenceMetros)
                        motivos.Add($"Fuera del radio del proyecto asignado ({Math.Round(masCercano.Distancia)}m, límite {masCercano.RadioGeofenceMetros}m)");
                }
                else
                {
                    motivos.Add("Ningún proyecto activo tiene geolocalización configurada");
                }
            }
            else
            {
                motivos.Add("Sin datos de GPS (permiso denegado o no disponible)");
            }

            // Foto duplicada (screenshot reciclado / foto de galería vieja)
            var fotoDuplicada = await ctx.AcTareoRegistro.AnyAsync(r => r.FotoHash == fotoHash);
            if (fotoDuplicada)
                motivos.Add("La foto ya fue usada en otro registro");

            // La similitud SIEMPRE se calcula acá, nunca se confía en un score que mande el cliente.
            decimal? faceMatchScore = (enrolamiento != null && body.Embedding is { Length: 128 })
                ? (decimal)CompararEmbeddings(enrolamiento.Embedding, body.Embedding)
                : null;

            string estado;
            if (enrolamiento == null)
            {
                estado = "SIN_ENROLAR";
                motivos.Insert(0, "Trabajador aún no enrolado para reconocimiento facial");
            }
            else if (faceMatchScore is null or < 0.5m)
            {
                estado = "REVISAR";
                motivos.Insert(0, "No se pudo verificar el rostro con confianza suficiente");
            }
            else if (motivos.Count > 0)
            {
                estado = "REVISAR";
            }
            else
            {
                estado = "VERIFICADO";
            }

            var registro = new AcTareoRegistro
            {
                WorkerId = workerId,
                Tipo = body.Tipo,
                Fecha = fechaPeru,
                HoraServidor = horaServidor,
                HoraDispositivo = body.HoraDispositivo,
                FotoUrl = fotoUrl,
                FotoHash = fotoHash,
                IdempotencyKey = idempotencyKey,
                Lat = body.Lat,
                Lng = body.Lng,
                PrecisionMetros = body.PrecisionMetros,
                ProjectId = projectId,
                DistanciaMetros = distanciaMetros,
                FaceMatchScore = faceMatchScore,
                Estado = estado,
                MotivoRevision = motivos.Count > 0 ? string.Join(" · ", motivos) : null,
                IpOrigen = ipOrigen,
                CreatedAt = DateTime.UtcNow,
            };

            ctx.AcTareoRegistro.Add(registro);

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // El índice único (worker_id, fecha, tipo) es la garantía real ante doble-click/concurrencia,
                // más allá de cualquier validación que se haga en memoria antes de este punto.
                throw new AbrilException($"Ya registraste tu {LabelTipo(body.Tipo)} de hoy.", 409);
            }

            return await ToDto(ctx, registro, yaExistia: false);
        }

        public async Task<TareoMiTareoHoyDTO> GetMiTareoHoy(int workerId)
        {
            using var ctx = _factory.CreateDbContext();
            var horaServidor = DateTime.UtcNow;
            var fechaPeru = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(horaServidor, PeruTz));

            var registros = await ctx.AcTareoRegistro
                .Where(r => r.WorkerId == workerId && r.Fecha == fechaPeru)
                .ToListAsync();

            async Task<TareoRegistroDTO?> Buscar(string tipo)
            {
                var r = registros.FirstOrDefault(x => x.Tipo == tipo);
                return r == null ? null : await ToDto(ctx, r, yaExistia: true);
            }

            return new TareoMiTareoHoyDTO
            {
                InicioJornada = await Buscar("INICIO_JORNADA"),
                InicioAlmuerzo = await Buscar("INICIO_ALMUERZO"),
                Retorno = await Buscar("RETORNO"),
                FinJornada = await Buscar("FIN_JORNADA"),
            };
        }

        public async Task<TareoRegistroListResponseDTO> GetRegistros(TareoFiltroDTO filtro)
        {
            using var ctx = _factory.CreateDbContext();
            var query = ctx.AcTareoRegistro.AsQueryable();

            if (filtro.WorkerId.HasValue) query = query.Where(r => r.WorkerId == filtro.WorkerId.Value);
            if (filtro.ProyectoId.HasValue) query = query.Where(r => r.ProjectId == filtro.ProyectoId.Value);
            if (filtro.Desde.HasValue) query = query.Where(r => r.Fecha >= filtro.Desde.Value);
            if (filtro.Hasta.HasValue) query = query.Where(r => r.Fecha <= filtro.Hasta.Value);
            if (!string.IsNullOrWhiteSpace(filtro.Estado)) query = query.Where(r => r.Estado == filtro.Estado);

            var total = await query.CountAsync();
            var pagina = Math.Max(1, filtro.Pagina);
            var porPagina = Math.Clamp(filtro.PorPagina, 1, 200);

            var registros = await query
                .OrderByDescending(r => r.HoraServidor)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            var workerIds = registros.Select(r => r.WorkerId).Distinct().ToList();
            var workers = await ctx.Worker.Where(w => workerIds.Contains(w.Id)).Include(w => w.Person).ToListAsync();
            var workerMap = workers.ToDictionary(w => w.Id, w => w.Person?.FullName ?? $"Worker {w.Id}");

            var projectIds = registros.Where(r => r.ProjectId.HasValue).Select(r => r.ProjectId!.Value).Distinct().ToList();
            var projectMap = await ctx.Project.Where(p => projectIds.Contains(p.ProjectId))
                .ToDictionaryAsync(p => p.ProjectId, p => p.ProjectDescription);

            return new TareoRegistroListResponseDTO
            {
                Total = total,
                Pagina = pagina,
                PorPagina = porPagina,
                Items = registros.Select(r => new TareoRegistroListaDTO
                {
                    Id = r.Id,
                    WorkerId = r.WorkerId,
                    WorkerNombre = workerMap.GetValueOrDefault(r.WorkerId, $"Worker {r.WorkerId}"),
                    Tipo = r.Tipo,
                    Fecha = r.Fecha,
                    HoraServidor = r.HoraServidor,
                    FotoUrl = r.FotoUrl,
                    ProjectNombre = r.ProjectId.HasValue ? projectMap.GetValueOrDefault(r.ProjectId.Value) : null,
                    DistanciaMetros = r.DistanciaMetros,
                    FaceMatchScore = r.FaceMatchScore,
                    Estado = r.Estado,
                    MotivoRevision = r.MotivoRevision,
                }).ToList(),
            };
        }

        public async Task<bool> Revisar(int id, int revisorUserId, TareoRevisarRequestDTO body)
        {
            using var ctx = _factory.CreateDbContext();
            var registro = await ctx.AcTareoRegistro.FirstOrDefaultAsync(r => r.Id == id);
            if (registro == null) return false;

            registro.Estado = body.Aprobar ? "VERIFICADO" : "RECHAZADO";
            registro.MotivoRevision = body.Comentario ?? registro.MotivoRevision;
            registro.RevisadoPor = revisorUserId;
            registro.RevisadoEn = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<List<TareoReporteSemanalDTO>> GetReporteSemanal(int? proyectoId, DateOnly semanaLunes)
        {
            using var ctx = _factory.CreateDbContext();
            var semanaDomingo = semanaLunes.AddDays(6);

            var query = ctx.AcTareoRegistro
                .Where(r => r.Fecha >= semanaLunes && r.Fecha <= semanaDomingo && r.Estado != "RECHAZADO");
            if (proyectoId.HasValue)
                query = query.Where(r => r.ProjectId == proyectoId.Value);

            var registros = await query.ToListAsync();

            var workerIds = registros.Select(r => r.WorkerId).Distinct().ToList();
            var workers = await ctx.Worker.Where(w => workerIds.Contains(w.Id)).Include(w => w.Person).ToListAsync();
            var workerMap = workers.ToDictionary(w => w.Id, w => w.Person?.FullName ?? $"Worker {w.Id}");

            return registros
                .GroupBy(r => r.WorkerId)
                .Select(g =>
                {
                    var dias = g.GroupBy(r => r.Fecha).Select(dg =>
                    {
                        DateTime? Hora(string tipo) => dg.FirstOrDefault(x => x.Tipo == tipo)?.HoraServidor;
                        var inicio = Hora("INICIO_JORNADA");
                        var almInicio = Hora("INICIO_ALMUERZO");
                        var almFin = Hora("RETORNO");
                        var fin = Hora("FIN_JORNADA");

                        decimal? totalHoras = null;
                        if (inicio.HasValue && fin.HasValue)
                        {
                            var bruto = fin.Value - inicio.Value;
                            var almuerzo = (almInicio.HasValue && almFin.HasValue) ? almFin.Value - almInicio.Value : TimeSpan.Zero;
                            totalHoras = (decimal)(bruto - almuerzo).TotalHours;
                        }

                        return new TareoReporteDiaDTO
                        {
                            Fecha = dg.Key,
                            InicioJornada = inicio,
                            InicioAlmuerzo = almInicio,
                            Retorno = almFin,
                            FinJornada = fin,
                            TotalHoras = totalHoras,
                        };
                    }).OrderBy(d => d.Fecha).ToList();

                    return new TareoReporteSemanalDTO
                    {
                        WorkerId = g.Key,
                        WorkerNombre = workerMap.GetValueOrDefault(g.Key, $"Worker {g.Key}"),
                        Dias = dias,
                        TotalHorasSemana = dias.Sum(d => d.TotalHoras ?? 0),
                    };
                })
                .OrderBy(w => w.WorkerNombre)
                .ToList();
        }

        private static async Task<TareoRegistroDTO> ToDto(AppDbContext ctx, AcTareoRegistro r, bool yaExistia)
        {
            string? projectNombre = null;
            if (r.ProjectId.HasValue)
            {
                projectNombre = await ctx.Project
                    .Where(p => p.ProjectId == r.ProjectId.Value)
                    .Select(p => p.ProjectDescription)
                    .FirstOrDefaultAsync();
            }

            return new TareoRegistroDTO
            {
                Id = r.Id,
                WorkerId = r.WorkerId,
                Tipo = r.Tipo,
                Fecha = r.Fecha,
                HoraServidor = r.HoraServidor,
                FotoUrl = r.FotoUrl,
                Lat = r.Lat,
                Lng = r.Lng,
                ProjectId = r.ProjectId,
                ProjectNombre = projectNombre,
                DistanciaMetros = r.DistanciaMetros,
                FaceMatchScore = r.FaceMatchScore,
                Estado = r.Estado,
                MotivoRevision = r.MotivoRevision,
                YaExistia = yaExistia,
            };
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("ux_tareo_worker_fecha_tipo", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true;

        private static string LabelTipo(string tipo) => tipo switch
        {
            "INICIO_JORNADA" => "inicio de jornada",
            "INICIO_ALMUERZO" => "inicio de almuerzo",
            "RETORNO" => "retorno de almuerzo",
            "FIN_JORNADA" => "fin de jornada",
            _ => tipo,
        };

        /// <summary>Similitud 0-1 entre dos embeddings faciales (128 floats de face-api.js), misma
        /// fórmula que usa el cliente (FaceRecognitionService) solo para preview — el valor que
        /// decide el estado del registro es SIEMPRE este cálculo del servidor.</summary>
        private static double CompararEmbeddings(float[] a, float[] b)
        {
            const double distanciaMatch = 0.6;
            double sumaCuadrados = 0;
            for (int i = 0; i < a.Length; i++)
            {
                var diff = a[i] - b[i];
                sumaCuadrados += diff * diff;
            }
            var distancia = Math.Sqrt(sumaCuadrados);
            var score = Math.Max(0, 1 - distancia / (distanciaMatch * 2));
            return Math.Round(score, 3);
        }

        /// <summary>Distancia en metros entre dos coordenadas (fórmula de Haversine).</summary>
        private static double HaversineMetros(double lat1, double lng1, double lat2, double lng2)
        {
            const double radioTierraM = 6371000;
            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return radioTierraM * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
