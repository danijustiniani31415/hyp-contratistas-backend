using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Interfaces;
using Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Dtos;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;

namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Services
{
    public class AdjudicacionOneDriveStorage : IAdjudicacionOneDriveStorage
    {
        private readonly IGraphSharePointService _graph;
        private readonly IProjectSubContractorRepository _repository;

        // Carpeta padre obligatoria dentro de la carpeta 04_OBRAS configurada: la cadena
        // Especialidad/Partida/Contratista/Adjudicación se crea siempre dentro de ella.
        private const string ObrasIntranetFolderName = "ADJUDICACIONES DE INTRANET";

        // Mismo criterio que la sincronización de partidas: quita prefijos numéricos de orden
        // ("4. ELEVADORES" → "ELEVADORES", "1.2 PISOS" → "PISOS", "3) MUROS" → "MUROS").
        private static readonly Regex _folderPrefixRegex =
            new(@"^\s*\d+(?:\s*[.)\-–:]\s*\d+)*\s*[.)\-–:]?\s*", RegexOptions.Compiled);

        // Alias de carpetas de especialidad: una especialidad canónica (la que queda en BD) acepta
        // también carpetas históricas con otro nombre que significan lo mismo. Se priorizan los alias
        // (carpetas ya existentes) antes de crear la carpeta canónica. Claves/valores se comparan con NormalizeKey.
        private static readonly Dictionary<string, string[]> _especialidadFolderAliases = new()
        {
            // "OBRAS PRELIMINARES Y PROVISIONALES" reconoce también la carpeta antigua "OBRAS PROVISIONALES".
            ["OBRAS PRELIMINARES Y PROVISIONALES"] = new[] { "OBRAS PROVISIONALES" },
        };

        public AdjudicacionOneDriveStorage(
            IGraphSharePointService graph,
            IProjectSubContractorRepository repository)
        {
            _graph = graph;
            _repository = repository;
        }

        public async Task<SharePointUploadResultDto> UploadAsync(
            int projectSubContractorId,
            AdjudicacionDocumentType documentType,
            string fileName,
            Stream content,
            string contentType,
            bool autoRenameOnLock = false)
        {
            var pathData = await _repository.GetPathDataAsync(projectSubContractorId);
            return await UploadAsync(pathData, documentType, fileName, content, contentType, autoRenameOnLock);
        }

        public async Task<SharePointUploadResultDto> UploadAsync(
            AdjudicacionPathDataDto pathData,
            AdjudicacionDocumentType documentType,
            string fileName,
            Stream content,
            string contentType,
            bool autoRenameOnLock = false)
        {
            var isScanned = documentType is AdjudicacionDocumentType.ScannedDoc1
                or AdjudicacionDocumentType.ScannedDoc2
                or AdjudicacionDocumentType.ScannedDoc3;

            // El contrato firmado escaneado (paso 7) va SOLO a 07_OT/BACK UP PROYECTO (solo Oficina
            // Central); todo lo demás va a 04_OBRAS (visible también para Oficina Técnica).
            var (rootDriveId, rootFolderId) = isScanned
                ? RequireBackupFolder(pathData)
                : RequireObrasFolder(pathData);

            // En 04_OBRAS toda la estructura cuelga de una carpeta intermedia
            // "ADJUDICACIONES DE INTRANET" dentro de la carpeta configurada; si no existe se crea.
            // No aplica a 07_OT/BACK UP PROYECTO.
            if (!isScanned)
                rootFolderId = await FindOrCreateExactAsync(rootDriveId, rootFolderId, ObrasIntranetFolderName);

            var (driveId, folderId) = await EnsureFolderAsync(pathData, documentType, rootDriveId, rootFolderId);

            var result = await _graph.UploadToOneDriveFolderAsync(
                driveId, folderId, fileName, content, contentType, autoRenameOnLock)
                ?? throw new AbrilException("No se pudo subir el archivo a OneDrive.");

            if (string.IsNullOrEmpty(result.WebUrl))
                throw new AbrilException("No se pudo obtener la URL del archivo subido a OneDrive.");

            return result;
        }

        /// <summary>
        /// Valida que la ruta de 04_OBRAS se pueda resolver con los datos recibidos, sin tocar
        /// OneDrive ni requerir que la adjudicación exista todavía. Permite fallar antes de
        /// insertarla en vez de dejarla creada y sin archivos.
        /// </summary>
        public void ValidateUploadPath(AdjudicacionPathDataDto pathData)
        {
            RequireObrasFolder(pathData);
            RequireWorkSpecialty(pathData);
        }

        public Task<byte[]> DownloadByWebUrlAsync(string webUrl)
            => _graph.DownloadOneDriveFileByWebUrlAsync(webUrl);

        public async Task<Dictionary<string, byte[]>> DownloadMultipleAsPdfAsync(
            int projectSubContractorId,
            IReadOnlyList<(string ItemId, bool AlreadyPdf)> items)
        {
            var pathData = await _repository.GetPathDataAsync(projectSubContractorId);
            var driveId = RequireObrasFolder(pathData).DriveId;
            return await _graph.DownloadMultipleAsPdfFromOneDriveAsync(driveId, items);
        }

        // ── Resolución de la cadena de carpetas en OneDrive ──────────────────────

        /// <summary>
        /// Asegura {CarpetaConfigurada}/{Especialidad}/{Partida}/{RUC - Razón social}/{ADJUDICACIÓN N° X}/{Subcarpeta}
        /// dentro de la raíz indicada y devuelve (driveId, itemId de la subcarpeta final).
        /// </summary>
        private async Task<(string DriveId, string FolderId)> EnsureFolderAsync(
            AdjudicacionPathDataDto data, AdjudicacionDocumentType documentType,
            string driveId, string contratosFolderId)
        {
            // Base de la estructura: para 07_OT/BACK UP es la carpeta configurada tal cual;
            // para 04_OBRAS es la subcarpeta "ADJUDICACIONES DE INTRANET" ya resuelta por el caller.

            // 1) Especialidad — obligatoria para saber en qué rama guardar.
            RequireWorkSpecialty(data);

            var especialidadId = await FindOrCreateEspecialidadFolderAsync(
                driveId, contratosFolderId, data.WorkSpecialtyDescription);

            // 3) Partida (NO 'partida de control') — coincide con la descripción del work item.
            var partidaId = await FindOrCreateByNormalizedKeyAsync(
                driveId, especialidadId, data.WorkItemDescription);

            // 4) Carpeta del contratista: "{RUC} - {Razón social}". Se busca por prefijo de RUC
            //    (tolera variaciones en la razón social) y si no existe se crea.
            var contratistaName = Sanitize($"{data.ContributorRuc} - {data.ContributorName}");
            var contratistaId = await FindOrCreateContractorAsync(
                driveId, partidaId, data.ContributorRuc, contratistaName);

            // 5) Carpeta de la adjudicación: "ADJUDICACIÓN N° X", autoincremental dentro de la carpeta
            //    del contratista (ya acotada a especialidad+partida+contratista por la ruta).
            var adjudicacionId = await EnsureAdjudicacionFolderAsync(driveId, contratistaId, data);

            // 6) Subcarpeta del tipo de documento.
            var subfolderId = await FindOrCreateExactAsync(
                driveId, adjudicacionId, GetSubfolderName(documentType));

            return (driveId, subfolderId);
        }

        private static void RequireWorkSpecialty(AdjudicacionPathDataDto data)
        {
            if (string.IsNullOrWhiteSpace(data.WorkSpecialtyDescription))
                throw new AbrilException(
                    "La adjudicación no tiene una especialidad asignada. Asigne una especialidad en el paso 1 " +
                    "antes de guardar documentos.", 400);
        }

        private static (string DriveId, string FolderId) RequireObrasFolder(AdjudicacionPathDataDto data)
        {
            if (string.IsNullOrWhiteSpace(data.ObrasDriveId) || string.IsNullOrWhiteSpace(data.ObrasFolderId))
                throw new AbrilException(
                    $"El proyecto '{data.ProjectDescription}' no tiene configurada una carpeta de adjudicaciones de tipo 04_OBRAS. " +
                    "Configúrela en Configuración → Carpeta de adjudicaciones y vuelva a intentarlo.", 422);

            return (data.ObrasDriveId!, data.ObrasFolderId!);
        }

        private static (string DriveId, string FolderId) RequireBackupFolder(AdjudicacionPathDataDto data)
        {
            if (string.IsNullOrWhiteSpace(data.BackupDriveId) || string.IsNullOrWhiteSpace(data.BackupFolderId))
                throw new AbrilException(
                    $"El proyecto '{data.ProjectDescription}' no tiene configurada una carpeta de adjudicaciones de tipo 07_OT/BACK UP PROYECTO. " +
                    "Configúrela en Configuración → Carpeta de adjudicaciones y vuelva a intentarlo.", 422);

            return (data.BackupDriveId!, data.BackupFolderId!);
        }

        // Reconoce carpetas "ADJUDICACIÓN N° 1", "ADJUDICACION N°2", "ADJUDICACIÓN Nº 3", etc.
        private static readonly Regex _adjudicacionFolderRegex =
            new(@"^\s*ADJUDICACI[ÓO]N\s*N[°º]?\s*(\d+)\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Devuelve (o crea) la carpeta de la adjudicación. Si ya tiene un nombre asignado se reutiliza;
        /// si no, se calcula el siguiente "ADJUDICACIÓN N° X" según las carpetas existentes en la carpeta
        /// del contratista y se persiste para que el resto de documentos vayan a la misma carpeta.
        /// </summary>
        private async Task<string> EnsureAdjudicacionFolderAsync(
            string driveId, string contratistaId, AdjudicacionPathDataDto data)
        {
            // Caso 1: la adjudicación ya tiene carpeta asignada → reutilizarla.
            if (!string.IsNullOrWhiteSpace(data.AdjudicacionFolderName))
                return await FindOrCreateExactAsync(driveId, contratistaId, data.AdjudicacionFolderName!);

            // Releer de BD por si otra subida de la MISMA adjudicación (mismo request) ya asignó la carpeta
            // pero con un pathData cargado en memoria antes de esa asignación.
            var persisted = await _repository.GetAdjudicacionFolderNameAsync(data.ProjectSubContractorId);
            if (!string.IsNullOrWhiteSpace(persisted))
            {
                data.AdjudicacionFolderName = persisted;
                return await FindOrCreateExactAsync(driveId, contratistaId, persisted!);
            }

            // Caso 2: asignar el siguiente número disponible dentro de la carpeta del contratista.
            var children = await _graph.GetChildFoldersByItemIdAsync(driveId, contratistaId);
            var maxNumber = 0;
            foreach (var child in children)
            {
                var m = _adjudicacionFolderRegex.Match(child.Name ?? "");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > maxNumber)
                    maxNumber = n;
            }

            var folderName = $"ADJUDICACIÓN N° {maxNumber + 1}";

            // Persistir ANTES de crear la carpeta, para que subidas posteriores reutilicen este nombre.
            await _repository.SetAdjudicacionFolderNameAsync(data.ProjectSubContractorId, folderName);
            data.AdjudicacionFolderName = folderName;

            return await _graph.EnsureChildFolderAsync(driveId, contratistaId, folderName);
        }

        /// <summary>
        /// Resuelve la carpeta de especialidad reconociendo nombres alias (carpetas históricas equivalentes).
        /// Prioridad: 1) carpeta con nombre alias si existe; 2) carpeta canónica existente; 3) crear la canónica.
        /// </summary>
        private async Task<string> FindOrCreateEspecialidadFolderAsync(string driveId, string parentId, string name)
        {
            var canonicalKey = NormalizeKey(name);
            var aliasKeys = _especialidadFolderAliases.TryGetValue(canonicalKey, out var aliases)
                ? aliases.Select(NormalizeKey).ToHashSet()
                : new HashSet<string>();

            var children = await _graph.GetChildFoldersByItemIdAsync(driveId, parentId);

            // 1) Preferir una carpeta con nombre alias histórico (p. ej. "OBRAS PROVISIONALES") si existe.
            if (aliasKeys.Count > 0)
            {
                var aliasMatch = children.FirstOrDefault(f => aliasKeys.Contains(NormalizeKey(f.Name ?? "")));
                if (aliasMatch is not null) return aliasMatch.ItemId;
            }

            // 2) Si no, usar la carpeta canónica existente.
            var canonicalMatch = children.FirstOrDefault(f => NormalizeKey(f.Name ?? "") == canonicalKey);
            if (canonicalMatch is not null) return canonicalMatch.ItemId;

            // 3) Si no existe ninguna, crear la canónica.
            return await _graph.EnsureChildFolderAsync(driveId, parentId, Sanitize(name));
        }

        /// <summary>Busca una subcarpeta cuya clave normalizada coincida; si no existe, la crea con el nombre dado.</summary>
        private async Task<string> FindOrCreateByNormalizedKeyAsync(string driveId, string parentId, string name)
        {
            var key = NormalizeKey(name);
            var children = await _graph.GetChildFoldersByItemIdAsync(driveId, parentId);
            var match = children.FirstOrDefault(f => NormalizeKey(f.Name ?? "") == key);
            if (match is not null) return match.ItemId;

            return await _graph.EnsureChildFolderAsync(driveId, parentId, Sanitize(name));
        }

        /// <summary>Busca una subcarpeta por nombre exacto (case-insensitive); si no existe, la crea.</summary>
        private async Task<string> FindOrCreateExactAsync(string driveId, string parentId, string name)
        {
            var children = await _graph.GetChildFoldersByItemIdAsync(driveId, parentId);
            var match = children.FirstOrDefault(f =>
                string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match.ItemId;

            return await _graph.EnsureChildFolderAsync(driveId, parentId, name);
        }

        /// <summary>Busca la carpeta del contratista por prefijo de RUC; si no existe, la crea como "{RUC} - {Razón social}".</summary>
        private async Task<string> FindOrCreateContractorAsync(
            string driveId, string parentId, string ruc, string fullName)
        {
            var children = await _graph.GetChildFoldersByItemIdAsync(driveId, parentId);
            var prefix = ruc + " - ";
            var match = children.FirstOrDefault(f =>
                (f.Name ?? "").StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(f.Name, fullName, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match.ItemId;

            return await _graph.EnsureChildFolderAsync(driveId, parentId, fullName);
        }

        // ── Nombres de subcarpeta por tipo de documento (estructura interna de la adjudicación) ──

        private static string GetSubfolderName(AdjudicacionDocumentType documentType) => documentType switch
        {
            AdjudicacionDocumentType.Contract           => "Contrato",
            AdjudicacionDocumentType.SummarySheet       => "Hoja Resumen",
            AdjudicacionDocumentType.Budget             => "Presupuesto",
            AdjudicacionDocumentType.Schedule           => "Cronograma",
            AdjudicacionDocumentType.AttachedQuotation  => "Cotizacion Adjunta",
            AdjudicacionDocumentType.ServiceOrder       => "Orden de Servicio",
            AdjudicacionDocumentType.InitialQuotation   => "Cotizaciones",
            AdjudicacionDocumentType.InitialComparative => "Comparativo",
            AdjudicacionDocumentType.PromissoryNote     => "Pagaré",
            AdjudicacionDocumentType.ScPackage          => "Paquete SC",
            AdjudicacionDocumentType.ScannedDoc1        => "Escaneados",
            AdjudicacionDocumentType.ScannedDoc2        => "Escaneados",
            AdjudicacionDocumentType.ScannedDoc3        => "Escaneados",
            AdjudicacionDocumentType.ContractPackage    => "Contrato completo",
            AdjudicacionDocumentType.Instructivo           => "Instructivos",
            AdjudicacionDocumentType.NonConformingOutput   => "Salidas No Conforme",
            AdjudicacionDocumentType.ToleranceChart        => "Cuadro de Tolerancias",
            AdjudicacionDocumentType.FinishProtection      => "Proteccion de Acabados",
            AdjudicacionDocumentType.FichaTecnica          => "Ficha Tecnica",
            AdjudicacionDocumentType.Anexo                 => "Anexos",
            _ => throw new ArgumentOutOfRangeException(nameof(documentType))
        };

        // ── Helpers de normalización (mismo criterio que WorkItemService.SyncPartidasAsync) ──

        private static string StripPrefix(string name)
            => _folderPrefixRegex.Replace(name ?? "", "").Trim();

        private static string NormalizeKey(string name)
        {
            var stripped = StripPrefix(name);
            var noDiacritics = RemoveDiacritics(stripped);
            var collapsed = Regex.Replace(noDiacritics, @"\s+", " ").Trim();
            return collapsed.ToUpperInvariant();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>Elimina caracteres que OneDrive/SharePoint no aceptan en nombres de carpeta/archivo.</summary>
        private static string Sanitize(string name)
        {
            var invalid = new HashSet<char> { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '#', '%' };
            var result = string.Concat(name.Select(c => invalid.Contains(c) ? '-' : c)).Trim();
            if (result.Length > 60) result = result[..60];
            // OneDrive/SharePoint rechaza nombres que terminan en punto o espacio (p. ej. "... S.A.C.").
            return result.TrimEnd(' ', '.');
        }
    }
}
