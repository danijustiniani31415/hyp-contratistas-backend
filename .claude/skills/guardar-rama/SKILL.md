---
name: guardar-rama
description: Guarda el trabajo en curso en la rama actual (no master), actualiza CONTEXT.md con un resumen de la sesión, y sube los cambios a origin. Usar cuando el usuario diga "guardar rama", "guarda mis cambios" o quiera cerrar sesión de trabajo en una rama feature. Solo opera sobre el repo en el que Claude Code está parado (backend o frontend) — si el usuario quiere ambos, se corre por separado en cada terminal.
---

# Guardar rama

Guarda el trabajo de la sesión en la rama actual y lo sube a `origin`. Nunca toca `master`.

## Pasos (en orden, detenerse si alguno falla)

### 1. Verificar rama actual

```
git branch --show-current
```

Si la rama es `master`, DETENERSE inmediatamente y responder:

```
Estás en master, no en una rama de trabajo. Para usar "guardar rama":

1. Si tienes cambios sin guardar en esta rama, corre "guardar rama" primero.
2. git checkout <nombre-de-tu-rama>   (o git checkout -b <nombre> si es nueva)
3. Corre "guardar rama" de nuevo.
```

No continuar con ningún paso siguiente.

### 2. Commit de cambios pendientes (solo si hay algo que guardar)

```
git status --porcelain
```

Si no hay salida, no hay nada que commitear — saltar a paso 3.

Si hay cambios:
1. `git add -A`
2. Analizar el diff (`git diff --cached --stat` y una revisión rápida de los archivos) para generar un mensaje de commit en formato **Conventional Commits** (`feat:`, `fix:`, `refactor:`, `style:`, `chore:`, etc. + descripción corta en español). No preguntar al usuario el mensaje, generarlo directamente.
3. `git commit -m "<mensaje generado>"`

### 3. Build obligatorio

Detectar el tipo de repo:
- Si existe `angular.json` en la raíz → `ng build`
- Si existe un `.csproj` o `.sln` en la raíz → `dotnet build`

Ejecutar el build correspondiente. Si falla:
- DETENERSE. No continuar a los pasos 4-6.
- Mostrar el error de build al usuario tal cual, sin intentar arreglarlo solo (a menos que el usuario lo pida explícitamente).

### 4. Actualizar CONTEXT.md

Agregar al final de `CONTEXT.md` una nueva sección con el resumen de la sesión, siguiendo el formato ya usado en el archivo (`## Sesión YYYY-MM-DD` o `## §N — Título (YYYY-MM-DD)`). El resumen debe cubrir, en base a lo trabajado en esta sesión:
- Qué se hizo (features, fixes, cambios de arquitectura)
- Archivos clave tocados
- Bugs pendientes o próximos pasos, si los hay

Escribir el resumen directamente en el archivo, sin mostrarlo antes al usuario para aprobación.

Luego:
```
git add CONTEXT.md
git commit -m "docs: actualiza CONTEXT.md con resumen de sesión"
```

### 5. Traer cambios remotos

```
git fetch origin
git merge origin/<rama-actual>
```

Si hay conflictos:
- DETENERSE. No hacer push.
- Listar los archivos en conflicto y pedir al usuario cómo resolverlos. No resolver conflictos de forma automática sin confirmación.

### 6. Push

```
git push origin <rama-actual>
```

Sin `--force` bajo ninguna circunstancia. Confirmar al usuario qué se subió (commits + archivos).
