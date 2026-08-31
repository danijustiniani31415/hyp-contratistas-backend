"""
Genera el SQL de import histórico de Observaciones de Arquitectura Comercial
a partir del CSV exportado de la lista SharePoint + el xlsx de la biblioteca
de fotos. No toca la BD directamente — solo escribe un .sql para correr en
pgAdmin, según el flujo de este proyecto.

El proyecto se resuelve por el campo IDProyecto del CSV (que es el ID estable
de la lista maestra "DBProyectos" de SharePoint), mapeado a mano contra los
project_id reales de la BD via SP_ID_TO_PROJECT_ID — confirmado cruzando
DBProyectos (1).csv (Nombre, ID/IDProyecto, Codigo) con el nombre real de
cada proyecto en la tabla `project`. NO se usa el prefijo del código de la
observación (tiene errores de tipeo humanos y es menos confiable).

Uso:
    py generate_import_observaciones.py

Requiere: openpyxl (pip install openpyxl)
"""
import csv
import datetime
import openpyxl

CSV_PATH = r"D:\Users\sjustiniani\OneDrive - Abril Grupo Inmobiliario\Descargas\ObservacionesArqCom.csv"
XLSX_PATH = r"D:\Users\sjustiniani\OneDrive - Abril Grupo Inmobiliario\Descargas\Bibliotecaarq.xlsx"
OUT_PATH = r"C:\Users\sjustiniani\Proyectos de Progra\Abril_Backend\Migrations\Manual\20260713_ImportObservacionesHistorico.sql"

SP_HOST = "abrilinmob.sharepoint.com"
SP_LIBRARY_PATH = "sites/gestionobservaciones/BObservacionesArqComercial"

ESTADO_MAP = {
    "Completado": "Completado",
    "En proceso": "En Proceso",
    "Pendiente": "Pendiente",
}

# IDProyecto (SharePoint, lista DBProyectos) -> project_id real en la BD Postgres.
# Verificado por nombre de proyecto, no por abreviatura (varias no calzan 1:1).
SP_ID_TO_PROJECT_ID = {
    22: 4,   # Sauco
    40: 5,   # Amaranta
    41: 3,   # Lilas
    43: 7,   # Kaurí
    44: 13,  # Bugambilias
    46: 6,   # Camelia
    47: 11,  # Máximo Abril
    48: 8,   # Cedro 33
    62: 12,  # Bosque Real
    64: 14,  # Eucalipto
    66: 10,  # Gran Manzano
    68: 9,   # Sauce Zen
    78: 17,  # Capulí
    79: 16,  # 9 Nogales
}


def sql_str(value):
    if value is None:
        return "NULL"
    return "'" + str(value).replace("'", "''") + "'"


def sql_date(value, fmt="%d/%m/%Y"):
    if not value or not str(value).strip():
        return "NULL"
    try:
        d = datetime.datetime.strptime(str(value).strip(), fmt)
        return "'" + d.strftime("%Y-%m-%d") + "'"
    except ValueError:
        return "NULL"


def sql_datetime(value, fmt="%d/%m/%Y %H:%M"):
    if not value or not str(value).strip():
        return "NULL"
    try:
        d = datetime.datetime.strptime(str(value).strip(), fmt)
        return "'" + d.strftime("%Y-%m-%d %H:%M:%S") + "'"
    except ValueError:
        return "NULL"


def load_csv_rows():
    with open(CSV_PATH, encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        return list(reader)


def load_fotos():
    """IDObservacion -> list of (tipo, url, orden)"""
    wb = openpyxl.load_workbook(XLSX_PATH, data_only=True, read_only=True)
    ws = wb.worksheets[0]
    rows = list(ws.iter_rows(min_row=2, values_only=True))
    # columnas: Nombre, IDObservacion, IDProyecto, Proyecto, Codigo,
    #           NuevoLevantamiento, Modificado, Modificado por, ID, Fecha,
    #           Creado, Tipo de elemento, Ruta de acceso
    fotos = {}
    for r in rows:
        nombre = r[0]
        id_obs = r[1]
        estado_lib = r[5]  # "Abierto" | "Cerrado"
        modificado = r[6]
        if id_obs is None or nombre is None:
            continue
        tipo = "Observacion" if estado_lib == "Abierto" else "Levantamiento"
        url = f"https://{SP_HOST}/{SP_LIBRARY_PATH}/{nombre}"
        fotos.setdefault(int(id_obs), []).append((tipo, url, modificado))
    result = {}
    for id_obs, items in fotos.items():
        items.sort(key=lambda t: t[2] or datetime.datetime.min)
        obs_list = []
        orden_obs = 0
        orden_lev = 0
        for tipo, url, _mod in items:
            if tipo == "Observacion":
                obs_list.append((tipo, url, orden_obs))
                orden_obs += 1
            else:
                obs_list.append((tipo, url, orden_lev))
                orden_lev += 1
        result[id_obs] = obs_list
    return result


def main():
    rows = load_csv_rows()
    fotos_por_obs = load_fotos()

    skipped_no_idproyecto = []
    skipped_unmapped_idproyecto = {}
    obs_values = []
    foto_values = []

    for r in rows:
        obs_id = int(r["ID"])
        idproyecto_raw = r["IDProyecto"].strip()
        if not idproyecto_raw:
            skipped_no_idproyecto.append(obs_id)
            continue

        idproyecto = int(idproyecto_raw)
        proyecto_id = SP_ID_TO_PROJECT_ID.get(idproyecto)
        if proyecto_id is None:
            skipped_unmapped_idproyecto.setdefault(idproyecto, []).append(obs_id)
            continue

        codigo = r["Codigo"].strip()
        descripcion = r["Descripcion"].strip() or "(sin descripción)"
        estado = ESTADO_MAP.get(r["Estado"].strip(), "Pendiente")

        obs_values.append(
            "(%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, 'Importado', %s)"
            % (
                obs_id,  # id explícito para poder relacionar fotos sin una segunda pasada
                proyecto_id,
                sql_str(codigo),
                sql_date(r["Fecha"]),
                sql_str(r["PersonaReporta"].strip() or None),
                sql_str(r["EmpresaReporta"].strip() or None),
                sql_str(r["Lugar"].strip() or None),
                sql_str(descripcion),
                sql_date(r["PlazoLevantamiento"]),
                sql_str(r["PartidaReportada"].strip() or None),
                sql_str(estado),
                sql_str(r["TipoObservacion"].strip() or None),
                sql_str(r["AreaResponsable"].strip() or None),
                sql_str(r["Ejecutor"].strip() or None),
                sql_str(r["Creado por"].strip() or None),
                sql_datetime(r["Creado"]),
            )
        )

        for tipo, url, orden in fotos_por_obs.get(obs_id, []):
            foto_values.append(
                "(%s, %s, %s, %s)" % (obs_id, sql_str(tipo), sql_str(url), orden)
            )

    with open(OUT_PATH, "w", encoding="utf-8") as out:
        out.write("-- Import histórico de Observaciones de Arquitectura Comercial\n")
        out.write("-- Generado automáticamente desde ObservacionesArqCom.csv + Bibliotecaarq.xlsx\n")
        out.write("-- Proyecto resuelto por IDProyecto (mapeo verificado por nombre, no por abreviatura).\n")
        out.write("-- Ejecutar en pgAdmin. Es idempotente: primero limpia lo 'Importado' antes de insertar.\n\n")

        out.write("DELETE FROM ac_observacion_fotos WHERE observacion_id IN (SELECT id FROM ac_observaciones WHERE origen = 'Importado');\n")
        out.write("DELETE FROM ac_observaciones WHERE origen = 'Importado';\n\n")

        out.write("-- Se usa un id explícito (igual al ID original de SharePoint) para poder\n")
        out.write("-- relacionar las fotos sin una segunda pasada; se ajusta la secuencia al final.\n\n")

        out.write(
            "INSERT INTO ac_observaciones "
            "(id, proyecto_id, codigo, fecha, persona_reporta, empresa_reporta, lugar, descripcion, "
            "plazo_levantamiento, partida_reportada, estado, tipo_observacion, area_responsable, ejecutor, "
            "creado_por, origen, created_at)\nVALUES\n"
        )
        out.write(",\n".join(obs_values))
        out.write(";\n\n")

        if foto_values:
            out.write("INSERT INTO ac_observacion_fotos (observacion_id, tipo, url, orden)\nVALUES\n")
            out.write(",\n".join(foto_values))
            out.write(";\n\n")

        out.write("-- Realinea la secuencia del id autoincremental tras insertar ids explícitos\n")
        out.write("SELECT setval(pg_get_serial_sequence('ac_observaciones', 'id'), COALESCE((SELECT MAX(id) FROM ac_observaciones), 1));\n")
        out.write("SELECT setval(pg_get_serial_sequence('ac_observacion_fotos', 'id'), COALESCE((SELECT MAX(id) FROM ac_observacion_fotos), 1));\n")

    print(f"OK: {len(obs_values)} observaciones, {len(foto_values)} fotos escritas en:\n{OUT_PATH}")
    print(f"Omitidas por IDProyecto vacío: {len(skipped_no_idproyecto)} -> {skipped_no_idproyecto}")
    print(f"Omitidas por IDProyecto sin mapeo conocido: {skipped_unmapped_idproyecto}")


if __name__ == "__main__":
    main()
