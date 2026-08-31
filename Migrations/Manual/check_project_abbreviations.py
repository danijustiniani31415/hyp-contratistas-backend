import json
import psycopg2

with open(r"C:\Users\sjustiniani\Proyectos de Progra\Abril_Backend\appsettings.local.json", encoding="utf-8") as f:
    config = json.load(f)

conn_str = config["Database"]["PostgreSQL"]
parts = dict(kv.split("=", 1) for kv in conn_str.split(";") if "=" in kv)

conn = psycopg2.connect(
    host=parts["Host"],
    port=parts["Port"],
    dbname=parts["Database"],
    user=parts["Username"],
    password=parts["Password"],
    sslmode="disable",
)
cur = conn.cursor()

cur.execute("SELECT project_id, abbreviation, project_description FROM project ORDER BY project_id")
rows = cur.fetchall()
print(f"--- total proyectos: {len(rows)} ---")
for r in rows:
    print(r)

cur.close()
conn.close()
