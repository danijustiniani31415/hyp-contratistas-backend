---
name: actualizar-rama
description: Trae los cambios de origin/master hacia la rama de trabajo actual (no push), y de paso deja actualizado el master local. Usar cuando el usuario diga "actualizar rama" o quiera empezar sesión de trabajo trayendo lo último de master antes de tocar código. Solo opera sobre el repo en el que Claude Code está parado (backend o frontend) — si el usuario quiere ambos, se corre por separado en cada terminal.
---

# Actualizar rama

Trae los cambios de `master` hacia la rama de trabajo actual, sin subir nada a `origin`. Es la contraparte de inicio de sesión de `guardar-rama` (que es de cierre).

## Pasos (en orden, detenerse si alguno falla)

### 1. Verificar rama actual

```
git branch --show-current
```

Si la rama es `master`, DETENERSE inmediatamente y responder:

```
Estás en master, no en una rama de trabajo. Para traer lo último a master, corré "actualizar master" en su lugar.
```

No continuar con ningún paso siguiente.

### 2. Traer todo del remoto

```
git fetch --prune
```

Esto actualiza `origin/master` y `origin/<rama-actual>` tal como los ve el repo local, sin tocar ninguna rama local todavía.

### 3. Traer cambios propios de la rama actual

```
git pull origin <rama-actual>
```

Por si hay commits de otra sesión o de otra PC que no se bajaron todavía.

### 4. Mergear master — SIEMPRE contra origin/master, nunca contra master a secas

```
git merge origin/master
```

**Nunca usar `git merge master`** (la rama local `master` puede estar desactualizada durante meses si no se la toca a mano, y el merge no traería nada real aunque `origin/master` tenga cambios nuevos — este fue el error que causó confusión en la sesión del 2026-07-25).

Si hay conflictos:
- DETENERSE. No continuar a los pasos 5-6.
- Listar los archivos en conflicto (`git status`) y pedir al usuario cómo resolverlos, uno por uno. No resolver conflictos de forma automática sin diagnóstico y confirmación explícita — para cada archivo en conflicto, explicar qué cambió de cada lado (HEAD vs master) antes de proponer una fusión.

### 5. Build de chequeo (solo si el merge trajo algo nuevo)

Si el paso 4 dio "Already up to date", saltar este paso — no hay nada nuevo que verificar.

Si trajo cambios, detectar el tipo de repo:
- Si existe `angular.json` en la raíz → `ng build`
- Si existe un `.csproj` o `.sln` en la raíz → `dotnet build`

Ejecutar el build correspondiente. Si falla, mostrar el error tal cual al usuario, sin intentar arreglarlo solo a menos que lo pida explícitamente. No hacer push bajo ninguna circunstancia en este punto — eso es trabajo de `guardar-rama` más tarde.

### 6. Dejar master local actualizado de paso

Sin salir de la rama de trabajo (no hace falta `checkout` a `master` para esto):

```
git fetch origin master:master
```

Esto actualiza la referencia local de `master` para que apunte a lo mismo que `origin/master`, sin necesidad de pararse en esa rama. Si el comando falla porque `master` local tiene commits que `origin/master` no tiene (no debería pasar en el flujo normal del proyecto, ya que nadie commitea directo en `master` local sin pasar por `guardar-master`), avisar al usuario y no forzar nada.

## Al terminar

Confirmar al usuario: qué se trajo (resumen de `git log --oneline HEAD@{1}..HEAD` si hubo merge), si el build quedó limpio, y que no se hizo ningún push — el próximo paso natural es seguir trabajando y cerrar más tarde con `guardar-rama`.
