# Logs centralizados — Grafana Loki

UI web para ver y buscar los logs del backend (y de cualquier contenedor) en
producción, sin SSH. Pensado para que **tú y tu jefe** entren desde el navegador.

```
┌────────────┐    stdout/stderr    ┌──────────┐   push   ┌──────┐   query   ┌─────────┐
│ abril-back │ ──────────────────► │ Promtail │ ───────► │ Loki │ ◄──────── │ Grafana │ ◄── navegador
│  (Docker)  │   (vía Docker API)  └──────────┘          └──────┘           └─────────┘
└────────────┘
```

- **Promtail** lee los logs de todos los contenedores por el socket de Docker.
- **Loki** los guarda con retención de 30 días e índice por etiquetas.
- **Grafana** es la pantalla donde se consultan/filtran.

No hay que tocar el código del backend: ya escribe a `stdout`, que es justo lo
que Promtail recolecta.

---

## 1. Desplegar en la VPS

```bash
# 1) Copiar esta carpeta a la VPS (desde tu máquina)
scp -r ops/logging usuario@TU_VPS:/opt/abril/logging

# 2) En la VPS
cd /opt/abril/logging
cp .env.example .env
nano .env          # define GF_ADMIN_PASSWORD y GF_ROOT_URL

# 3) Levantar el stack
docker compose --env-file .env up -d

# 4) Verificar
docker compose ps
docker compose logs -f loki promtail   # Ctrl+C para salir
```

A los pocos segundos Promtail ya estará enviando logs a Loki.

---

## 2. Publicar Grafana como subpath de intranet.abril.pe/logs (Nginx)

Grafana escucha solo en `127.0.0.1:3000` y está configurado para servirse bajo
`/logs` (`GF_SERVER_SERVE_FROM_SUB_PATH=true` + `GF_ROOT_URL=.../logs/`).

**No necesitas DNS ni certificado nuevos**: se reutiliza el `server` que ya
sirve `intranet.abril.pe`. Solo agrega este `location` dentro de ese bloque
(antes del `location /` del frontend Angular, que es menos específico):

```nginx
# Dentro del server { server_name intranet.abril.pe; ... } existente
location /logs/ {
    # SIN barra final en proxy_pass: se conserva el prefijo /logs/ para que
    # Grafana (serve_from_sub_path=true) lo maneje correctamente.
    proxy_pass         http://127.0.0.1:3000;
    proxy_set_header   Host              $host;
    proxy_set_header   X-Real-IP         $remote_addr;
    proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Proto $scheme;
    # Soporte de WebSocket para el "live tail" de Grafana
    proxy_set_header   Upgrade           $http_upgrade;
    proxy_set_header   Connection        "upgrade";
}
```

Aplica con:

```bash
sudo nginx -t && sudo systemctl reload nginx
```

Luego entra a **https://intranet.abril.pe/logs**. (Si dejas `GF_ROOT_URL` con
otro valor que no termine en `/logs/`, los enlaces y el login fallarán.)

---

## 3. Ver los logs (lo que harán tú y tu jefe)

1. Entrar a `https://intranet.abril.pe/logs` y loguear con el usuario/clave del `.env`.
2. Menú **Explore** (brújula) → fuente de datos **Loki** (ya viene configurada).
3. Escribir una consulta LogQL:

   | Quiero ver… | Consulta |
   |---|---|
   | Todo el backend | `{container="abril-backend"}` |
   | Solo errores | `{container="abril-backend"} \|~ "(?i)error\|exception\|fail"` |
   | Una funcionalidad concreta | `{container="abril-backend"} \|= "Adjudicacion"` |
   | Solo stderr | `{container="abril-backend", stream="stderr"}` |

4. Arriba a la derecha se elige el rango de tiempo y se activa **Live** para ver
   en vivo.

> Tip: crea un **usuario Viewer** para tu jefe (Administration → Users) para que
> solo consulte y no pueda cambiar configuración.

### Dashboard sugerido
Crea un dashboard con un panel tipo **Logs** y la query `{container="abril-backend"}`,
y otro panel **Stat/Time series** con `count_over_time({container="abril-backend"} |~ "(?i)error" [5m])`
para ver picos de errores. Así tu jefe entra a una sola pantalla ya armada.

---

## 4. Seguridad (importante)

- **Nunca** expongas los puertos de Loki (3100) ni Promtail (9080) a internet:
  en este compose quedan solo en la red interna del stack. No les agregues
  `ports:` públicos.
- Los logs contienen datos sensibles (correos, RUC, tokens en stack traces).
  El acceso queda protegido por el login de Grafana + HTTPS. Usa contraseñas
  fuertes y crea cuentas individuales en vez de compartir el admin.
- El `.env` (con la clave de Grafana) está en `.gitignore`; no lo subas al repo.

---

## 5. Mantenimiento

- **Retención**: 30 días (`retention_period: 720h` en `loki-config.yml`). Si el
  disco se ajusta, bájala; si necesitas más historial y hay espacio, súbela.
- **Uso de disco**: `docker system df -v` y revisa el volumen `abril-logging_loki-data`.
- **Actualizar versiones**: cambia los tags de imagen en `docker-compose.yml` y
  `docker compose up -d`.
- **Nota**: Promtail está en modo mantenimiento; Grafana recomienda migrar a
  **Grafana Alloy** a futuro. La migración es directa (mismo concepto de scrape)
  cuando quieras modernizar.

---

## 6. Relación con el backend

El `deploy.yml` del backend ya incluye **rotación de logs de Docker**
(`--log-opt max-size=10m --log-opt max-file=5`) para que el disco no se llene.
Loki mantiene el historial **más allá de cada despliegue** (cuando el contenedor
`abril-backend` se recrea, sus logs anteriores ya están guardados en Loki).
