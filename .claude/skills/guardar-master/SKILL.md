---
name: guardar-master
description: Guarda el trabajo en curso y lo sube directo a main (rama de producción de este repo), siguiendo la regla P5 (nunca --force). Usar cuando el usuario diga "guardar master"/"guardar main" o quiera subir cambios a producción/intranet. Si se invoca desde una rama de trabajo (no main), mergea esa rama a main automáticamente tras confirmación explícita — "guardar rama" solo guarda en la rama, "guardar master" guarda Y despliega a producción. Antes del push, aplica en la base de producción cualquier migración SQL manual nueva (Migrations_Manual/) — el deploy es automático al pushear, así que sin esto el código nuevo puede llegar a producción antes que su propio esquema. Pide confirmación explícita antes del push por ser la rama de producción. Solo opera sobre el repo en el que Claude Code está parado (backend o frontend) — si el usuario quiere ambos, se corre por separado en cada terminal.
---

# Guardar master

Guarda el trabajo y lo sube a `main` = intranet/producción (droplet, deploy automático al pushear — ver Program.cs/docker en el droplet). Es la única skill que puede pushear a `main`, y lo hace con más cuidado que "guardar rama" porque va directo a producción.

**Diferencia con "guardar rama":** "guardar rama" solo sube tu rama de trabajo a `origin/<rama>`, nunca toca `main`. "guardar master" además mergea esa rama a `main`, aplica sus migraciones SQL a producción y la despliega — es el paso que efectivamente lleva el trabajo a intranet/producción.

## Pasos (en orden, detenerse si alguno falla)

### 1. Verificar rama actual

```
git branch --show-current
```

**Si la rama es `main`:** continuar directo al paso 2, sin merge de ninguna otra rama (ya se está trabajando directo en main).

**Si la rama NO es `main`:** este es el caso "llevar mi rama de trabajo a producción". Guardar el nombre de esta rama como `<rama-origen>` y:

1. `git status --porcelain` — si hay cambios sin commitear en `<rama-origen>`, DETENERSE y responder:
   ```
   Tienes cambios sin guardar en <rama-origen>. Corre "guardar rama" primero
   para dejarla commiteada y subida, y después "guardar master" de nuevo.
   ```
   No continuar con ningún paso siguiente.

2. Si `<rama-origen>` ya está limpia, preguntar explícitamente al usuario:
   ```
   ¿Confirmas mergear <rama-origen> a main y subir esto a producción?
   ```
   Esperar un sí claro. No asumir confirmación implícita.

3. Si confirma: `git checkout main` y continuar al paso 2, recordando `<rama-origen>` para el paso 5 (donde se mergea). Si no confirma, DETENERSE sin hacer nada más.

### 2. Commit de cambios pendientes (solo si hay algo que guardar)

```
git status --porcelain
```

Si no hay salida, no hay nada que commitear — saltar a paso 3.

Si hay cambios:
1. `git add -A`
2. Analizar el diff (`git diff --cached --stat` y revisión rápida de archivos) para generar un mensaje de commit en formato **Conventional Commits** en español. No preguntar al usuario el mensaje.
3. `git commit -m "<mensaje generado>"`

### 3. Build obligatorio

Detectar el tipo de repo:
- Si existe `angular.json` en la raíz → `ng build`
- Si existe un `.csproj` o `.sln` en la raíz → `dotnet build`

Ejecutar el build correspondiente. Si falla:
- DETENERSE. No continuar a los pasos 4-7.
- Mostrar el error de build tal cual, sin intentar arreglarlo solo salvo que el usuario lo pida.

### 4. Actualizar CONTEXT.md

Agregar al final de `CONTEXT.md` una sección con el resumen de la sesión, siguiendo el formato existente del archivo (`## Sesión YYYY-MM-DD` o `## §N — Título (YYYY-MM-DD)`). Cubrir: qué se hizo, archivos clave, pendientes. Escribir directo, sin pedir aprobación antes.

```
git add CONTEXT.md
git commit -m "docs: actualiza CONTEXT.md con resumen de sesión"
```

### 5. Traer cambios remotos y mergear la rama de origen (si aplica)

```
git fetch origin
git merge origin/main
```

Si hay conflictos en este merge:
- DETENERSE. No hacer push.
- Listar los archivos en conflicto y pedir al usuario cómo resolverlos.

**Si el paso 1 identificó una `<rama-origen>`** (se venía de una rama de trabajo, no de main), mergearla ahora:

```
git merge <rama-origen>
```

Si hay conflictos en este merge:
- DETENERSE. No hacer push.
- Listar los archivos en conflicto y pedir al usuario cómo resolverlos. No resolver conflictos de forma automática sin confirmación.

### 6. Aplicar a producción las migraciones SQL nuevas (solo repo backend)

Salta este paso entero si el repo no tiene carpeta `Migrations_Manual/` (p. ej. el frontend).

El deploy a producción es automático al pushear (el droplet levanta la imagen nueva solo) — si el código que depende de una columna/tabla nueva llega antes que esa columna exista en `hyp_app`, producción se rompe al toque (pasó el 2026-09-24: `notificar does not exist` tumbó el login en cuanto se desplegó el código nuevo). Por eso este paso va **antes** del push, nunca después.

1. Detectar qué archivos de `Migrations_Manual/*.sql` son nuevos respecto a lo que ya está en `origin/main`:
   ```
   git diff origin/main..HEAD --name-only --diff-filter=A -- Migrations_Manual/
   ```
   Si no hay ninguno, saltar directo al paso 7 (nada que migrar).

2. Ordenar esa lista por nombre de archivo (el prefijo `YYYY-MM-DD_` ya los deja en orden cronológico) y avisar al usuario cuáles se van a aplicar.

3. Para cada archivo, en orden, copiarlo al droplet y ejecutarlo contra `hyp_app` — la contraseña de la base **nunca se escribe en esta skill ni en ningún archivo del repo**, se lee al vuelo desde el propio `appsettings.Production.json` del droplet:
   ```
   scp -q "Migrations_Manual/<archivo>.sql" "puente:/tmp/<archivo>.sql"
   ssh puente "PW=\$(grep -oP '(?<=Password=)[^;]+' /opt/hyp-contratistas/appsettings.Production.json | head -1 | xargs); PGPASSWORD=\$PW psql -h localhost -U hyp_app -d hyp_app -f /tmp/<archivo>.sql"
   ```
   El `xargs` al final del `grep` es obligatorio, no cosmético: la contraseña real del droplet tiene un espacio final antes del `;` en el connection string, y sin recortarlo `PGPASSWORD` queda mal y el login a Postgres falla con "password authentication failed" (pasó el 2026-09-24).
4. Si algún archivo tira un `ERROR:` real (no un `NOTICE:` de idempotencia tipo "already exists, skipping") — DETENERSE. No seguir con el resto de archivos ni con el push. Mostrar el error tal cual y preguntar cómo proceder; no reintentar solo ni "arreglar" la migración sin decírselo antes al usuario.
5. Si algún archivo referencia una tabla/columna que a su vez depende de una migración **anterior** que tampoco se ha corrido (como pasó con `lb_catalogo_valor` el 2026-09-24), avisar que probablemente producción está atrasada de antes y ofrecer revisar `Migrations_Manual/` completo contra las tablas que existen hoy en `hyp_app`, no solo los archivos nuevos de este commit.

### 6.5. Verificar que roles y permisos coincidan entre local y producción (solo repo backend)

`lb_permiso`/`lb_rol`/`lb_rol_permiso`/`lb_usuario_asignacion` son data de referencia (seed), no
código — nunca viajan solas por el flujo normal de migraciones, así que pueden divergir entre
`hyp_local` y `hyp_app` sin que nadie lo note hasta que alguien queda bloqueado de una pantalla que
sí debería poder usar (pasó el 2026-09-25: el admin de producción tenía asignado el rol
`GERENTE_GENERAL` en vez de `ADMIN` — nadie lo notó durante meses porque ningún endpoint validaba
permisos todavía). Correr esto cada vez que este paso 6 detecte migraciones nuevas relacionadas a
permisos, o cuando el usuario reporte "no tengo acceso a X aunque debería":

```
ssh puente "PW=\$(grep -oP '(?<=Password=)[^;]+' /opt/hyp-contratistas/appsettings.Production.json | head -1 | xargs); PGPASSWORD=\$PW psql -h localhost -U hyp_app -d hyp_app -c \"SELECT r.codigo AS rol, p.codigo AS permiso FROM lb_rol_permiso rp JOIN lb_rol r ON r.id=rp.rol_id JOIN lb_permiso p ON p.id=rp.permiso_id ORDER BY r.codigo, p.codigo;\""
```

Comparar esa salida contra la misma query en `hyp_local`. Si hay diferencias, mostrárselas al
usuario tal cual (rol/permiso de más o de menos en cada lado) y preguntar cómo reconciliar —
**nunca** aplicar el ajuste de data solo, esto es una decisión de negocio (qué rol debe tener qué
permiso), no un fix mecánico como sí lo es correr un `.sql` de esquema.

### 7. Confirmación antes de push (obligatoria — main es producción)

Mostrar al usuario:
```
git log origin/main..HEAD --oneline
git diff origin/main..HEAD --stat
```

Y preguntar explícitamente: "¿Confirmas subir estos commits a main?" — esperar un sí claro antes de continuar. No asumir confirmación implícita.

### 8. Push

Solo tras confirmación explícita:
```
git push origin main
```

**Regla P5 — nunca usar `--force` bajo ninguna circunstancia**, ni aunque el usuario lo pida sin dar una razón explícita y consciente del riesgo (esto pisaría trabajo de otra PC o sesión sin aviso).
