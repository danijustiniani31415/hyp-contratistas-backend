using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Services
{
    public class EvEvaluacionResidenteService : IEvEvaluacionResidenteService
    {
        private readonly IEvEvaluacionResidenteRepository _repo;
        private readonly IEvPeriodoRepository _periodoRepo;

        public EvEvaluacionResidenteService(
            IEvEvaluacionResidenteRepository repo,
            IEvPeriodoRepository periodoRepo)
        {
            _repo = repo;
            _periodoRepo = periodoRepo;
        }

        public async Task<EvEvaluacionResidenteResponseDto> CreateAsync(EvEvaluacionCreateDto dto, int evaluadorUserId)
        {
            var periodo = await _periodoRepo.GetActivoAsync()
                ?? throw new AbrilException("No hay período activo.", 400);

            if (DateTime.Today < periodo.FechaApertura.ToDateTime(TimeOnly.MinValue) ||
                DateTime.Today > periodo.FechaCierre.ToDateTime(TimeOnly.MinValue))
                throw new AbrilException("El período de evaluación no está activo.", 400);

            if (!dto.NoAplica && dto.Detalles.Any(d => !d.EsNa && (d.Puntaje is null or < 1 or > 5)))
                throw new AbrilException("El puntaje debe estar entre 1 y 5.", 400);

            var existe = await _repo.ExisteAsync(periodo.Id, evaluadorUserId, dto.EvaluadoUserId, dto.AreaNombre);
            if (existe)
                throw new AbrilException("Ya evaluaste a este residente en esta área este período.", 409);

            var eval = new EvEvaluacionResidente
            {
                PeriodoId = periodo.Id,
                EvaluadorUserId = evaluadorUserId,
                EvaluadoUserId = dto.EvaluadoUserId,
                ProjectId = dto.ProjectId,
                AreaNombre = dto.AreaNombre,
                Comentario = dto.Comentario,
                NoAplica = dto.NoAplica,
                NoAplicaMotivo = dto.NoAplicaMotivo
            };
            var detalles = dto.Detalles.Select(d => new EvEvaluacionResidenteDetalle
            {
                PlantillaId = d.PlantillaId,
                Criterio = d.Criterio,
                Puntaje = d.Puntaje,
                EsNa = d.EsNa
            }).ToList();

            await _repo.CreateAsync(eval, detalles);

            return await _repo.GetDetalleAsync(eval.Id)
                ?? throw new AbrilException("Error al recuperar la evaluación creada.", 500);
        }

        /// <summary>
        /// Corrige una evaluación ya enviada: solo el mismo evaluador, solo dentro de las 24h
        /// siguientes a haberla creado, solo mientras el período siga activo. Reemplaza la nota
        /// (no queda historial) y no deja ninguna marca de que fue editada.
        /// </summary>
        public async Task<EvEvaluacionResidenteResponseDto> UpdateAsync(int id, EvEvaluacionCreateDto dto, int evaluadorUserId)
        {
            var periodo = await _periodoRepo.GetActivoAsync()
                ?? throw new AbrilException("No hay período activo.", 400);

            if (DateTime.Today < periodo.FechaApertura.ToDateTime(TimeOnly.MinValue) ||
                DateTime.Today > periodo.FechaCierre.ToDateTime(TimeOnly.MinValue))
                throw new AbrilException("El período de evaluación no está activo.", 400);

            if (!dto.NoAplica && dto.Detalles.Any(d => !d.EsNa && (d.Puntaje is null or < 1 or > 5)))
                throw new AbrilException("El puntaje debe estar entre 1 y 5.", 400);

            var existente = await _repo.GetByIdAsync(id)
                ?? throw new AbrilException("Evaluación no encontrada.", 404);

            if (existente.EvaluadorUserId != evaluadorUserId)
                throw new AbrilException("Solo puedes corregir evaluaciones que registraste tú.", 403);

            if (DateTime.UtcNow - existente.CreatedAt > TimeSpan.FromHours(24))
                throw new AbrilException("Ya pasaron más de 24 horas desde que la registraste — no se puede corregir.", 400);

            var puntajesValidos = dto.Detalles
                .Where(d => !d.EsNa && d.Puntaje.HasValue)
                .Select(d => d.Puntaje!.Value)
                .ToList();
            var nota = puntajesValidos.Count != 0 ? Math.Round((decimal)puntajesValidos.Average() * 4, 2) : 0;

            var detalles = dto.Detalles.Select(d => new EvEvaluacionResidenteDetalle
            {
                PlantillaId = d.PlantillaId,
                Criterio = d.Criterio,
                Puntaje = d.Puntaje,
                EsNa = d.EsNa
            }).ToList();

            await _repo.UpdateAsync(id, nota, dto.Comentario, dto.NoAplica, dto.NoAplicaMotivo, detalles);

            return await _repo.GetDetalleAsync(id)
                ?? throw new AbrilException("Error al recuperar la evaluación corregida.", 500);
        }

        public async Task<List<EvEvaluacionResidenteResponseDto>> GetByEvaluadorAsync(int evaluadorUserId, int periodoId)
            => await _repo.GetByEvaluadorAsync(evaluadorUserId, periodoId);

        public async Task<List<EvEvaluacionResidenteResponseDto>> GetByEvaluadoAsync(int evaluadoUserId, int periodoId)
            => await _repo.GetByEvaluadoAsync(evaluadoUserId, periodoId);
    }
}
