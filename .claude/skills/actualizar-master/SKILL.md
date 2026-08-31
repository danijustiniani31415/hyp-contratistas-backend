---
name: actualizar-master
description: Trae los cambios de origin/master hacia el master local, para cuando se va a trabajar directo en master. Usar cuando el usuario diga "actualizar master" o quiera asegurarse de que master está al día antes de un cambio directo ahí (sin pasar por una rama feature). Solo opera sobre el repo en el que Claude Code está parado (backend o frontend) — si el usuario quiere ambos, se corre por separado en cada terminal.
---

# Actualizar master

Trae los cambios de `origin/master` hacia el `master` local, sin subir nada. Es la contraparte de inicio de sesión de `guardar-master`, para cuando se va a trabajar directo en `master` en vez de en una rama feature.

## Pasos (en orden, detenerse si alguno falla)

### 1. Verificar rama actual

```
git branch --show-current
```

Si la rama NO es `master`, DETENERSE inmediatamente y responder:

```
Estás en <rama-actual>, no en master. Para traer lo último a tu rama de trabajo, corré "actualizar rama" en su lugar.
```

No continuar con ningún paso siguiente.

### 2. Traer cambios del remoto

```
git fetch origin
git pull origin master
```

En el flujo normal esto no debería generar conflictos (nadie más commitea directo en `master` local salvo vía `guardar-master`, que ya se encarga de traer lo remoto antes de pushear). Si aun así aparece un conflicto:
- DETENERSE. No continuar al paso 3.
- Listar los archivos en conflicto y pedir al usuario cómo resolverlos, con el mismo criterio de diagnóstico que en `actualizar-rama` — explicar qué cambió de cada lado antes de proponer una fusión.

### 3. Build de chequeo

Detectar el tipo de repo:
- Si existe `angular.json` en la raíz → `ng build`
- Si existe un `.csproj` o `.sln` en la raíz → `dotnet build`

Ejecutar el build correspondiente y confirmar 0 errores. Si falla, mostrar el error tal cual, sin intentar arreglarlo solo a menos que el usuario lo pida.

## Al terminar

Confirmar al usuario qué se trajo (`git log --oneline` de los commits nuevos, si los hubo) y que el build quedó limpio. No se hace push en ningún punto de esta skill.
