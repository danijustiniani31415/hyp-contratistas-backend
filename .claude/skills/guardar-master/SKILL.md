---
name: guardar-master
description: Guarda el trabajo en curso y lo sube directo a master, siguiendo la regla P5 (nunca --force). Usar cuando el usuario diga "guardar master" o quiera subir cambios a producción/intranet. Si se invoca desde una rama de trabajo (no master), mergea esa rama a master automáticamente tras confirmación explícita — "guardar rama" solo guarda en la rama, "guardar master" guarda Y despliega a producción. Pide confirmación explícita antes del push por ser la rama de producción. Solo opera sobre el repo en el que Claude Code está parado (backend o frontend) — si el usuario quiere ambos, se corre por separado en cada terminal.
---

# Guardar master

Guarda el trabajo y lo sube a `master` = intranet/producción. Es la única skill que puede pushear a `master`, y lo hace con más cuidado que "guardar rama" porque va directo a producción.

**Diferencia con "guardar rama":** "guardar rama" solo sube tu rama de trabajo a `origin/<rama>`, nunca toca `master`. "guardar master" además mergea esa rama a `master` y la despliega — es el paso que efectivamente lleva el trabajo a intranet/producción.

## Pasos (en orden, detenerse si alguno falla)

### 1. Verificar rama actual

```
git branch --show-current
```

**Si la rama es `master`:** continuar directo al paso 2, sin merge de ninguna otra rama (ya se está trabajando directo en master).

**Si la rama NO es `master`:** este es el caso "llevar mi rama de trabajo a producción". Guardar el nombre de esta rama como `<rama-origen>` y:

1. `git status --porcelain` — si hay cambios sin commitear en `<rama-origen>`, DETENERSE y responder:
   ```
   Tienes cambios sin guardar en <rama-origen>. Corre "guardar rama" primero
   para dejarla commiteada y subida, y después "guardar master" de nuevo.
   ```
   No continuar con ningún paso siguiente.

2. Si `<rama-origen>` ya está limpia, preguntar explícitamente al usuario:
   ```
   ¿Confirmas mergear <rama-origen> a master y subir esto a producción?
   ```
   Esperar un sí claro. No asumir confirmación implícita.

3. Si confirma: `git checkout master` y continuar al paso 2, recordando `<rama-origen>` para el paso 5 (donde se mergea). Si no confirma, DETENERSE sin hacer nada más.

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
git merge origin/master
```

Si hay conflictos en este merge:
- DETENERSE. No hacer push.
- Listar los archivos en conflicto y pedir al usuario cómo resolverlos.

**Si el paso 1 identificó una `<rama-origen>`** (se venía de una rama de trabajo, no de master), mergearla ahora:

```
git merge <rama-origen>
```

Si hay conflictos en este merge:
- DETENERSE. No hacer push.
- Listar los archivos en conflicto y pedir al usuario cómo resolverlos. No resolver conflictos de forma automática sin confirmación.

### 6. Confirmación antes de push (obligatoria — master es producción)

Mostrar al usuario:
```
git log origin/master..HEAD --oneline
git diff origin/master..HEAD --stat
```

Y preguntar explícitamente: "¿Confirmas subir estos commits a master?" — esperar un sí claro antes de continuar. No asumir confirmación implícita.

### 7. Push

Solo tras confirmación explícita:
```
git push origin master
```

**Regla P5 — nunca usar `--force` bajo ninguna circunstancia**, ni aunque el usuario lo pida sin dar una razón explícita y consciente del riesgo (esto pisaría trabajo de otra PC o sesión sin aviso).
