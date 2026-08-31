# Lineamientos generales — Backend

## Información del proyecto general

Este proyecto busca acelerar los procesos internos que se dan dentro de la empresa Abril Grupo Inmobiliario, como automatizar la generación de contratos, acelerar el registro de trabajadores mediante el uso de consultas a APIs tercerizadas de RENIEC, acelerar el registro de subcontratistas mediante el uso de consultas a APIs tercerizadas de SUNAT, entre otras cosas
El proyecto cuenta con un frontend en Angular 21, un backend .NET 10, una base de datos PostgreSQL 17, entre otros servicios como el uso de https://cron-job.org/ para recordatorios automatizados.
Tanto el frontend, backend y base de datos están hosteados en una VPS y se cuenta con un deploy.yml para subir los cambios.

## Estructura del backend

El proyecto usa **arquitectura por features**. Cada funcionalidad es independiente y vive en su propia carpeta. Las carpetas raíz `Controllers/`, `Application/` e `Infrastructure/`
están **deprecadas** y no deben usarse para código nuevo bajo ningún motivo.

## Organización de carpetas

```
Abril-Backend/
├── Features/
│ ├── CostsModule/
│ │ ├── Features/
│ │ │ ├── AdjudicacionesFeature/
│ │ │ │ ├── Application/
│ │ │ │ │ ├── Dtos/
│ │ │ │ │ ├── Interfaces/
│ │ │ │ │ └── Services/
│ │ │ │ ├── Infrastructure/
│ │ │ │ │ ├── Interfaces/
│ │ │ │ │ ├── Models/
│ │ │ │ │ └── Repositories/
│ │ │ │ └── Presentation/
│ │ │ └── ConfigurationFeature/
│ │ └── Shared/ ← archivos compartidos solo dentro del módulo
│ │ └── Models/
│ ├── ContractorsModule/
│ └── MicrosoftAuth/
├── Shared/ ← servicios reutilizables globales
│ ├── Models/
│ └── Services/
│ ├── Email/
│ ├── SharePoint/
│ └── Sunat/
├── Infrastructure/ ← DEPRECADO, no usar
├── Controllers/ ← DEPRECADO, no usar
└── Application/ ← DEPRECADO, no usar

## ¿Qué es una feature?
Una feature corresponde a una funcionalidad específica que aparece como ítem
en el sidebar lateral (o en el sidebar del header en algunos casos) de la aplicación frontend. Cada entrada del sidebar
corresponde exactamente a una feature y tiene su propia carpeta dentro del
módulo al que pertenece.
Ejemplos:
- "Adjudicaciones" → `AdjudicacionesFeature/`
- "Homologación de Contratistas" → `ContractorRegistrationFeature/`
- "Gestión de Contratistas" → `ContractorManagementFeature/`
No se considera feature a lógica interna de una funcionalidad (como validaciones,
helpers o modelos secundarios): esos viven dentro de la carpeta de su feature.
## Reglas
### ✅ Código nuevo
- Cada feature va dentro de `Features/{Modulo}/Features/{NombreFeature}/`
- Si el módulo es simple y no agrupa sub-features, puede vivir directamente
  en `Features/{NombreFeature}/`
- El namespace debe coincidir exactamente con la ruta de la carpeta:
  - Ruta: `Features/CostsModule/Features/AdjudicacionesFeature/Application/Services/`
  - Namespace: `Abril_Backend.Features.CostsModule.Features.AdjudicacionesFeature.Application.Services`
- Cada módulo se registra en su propio archivo `{Modulo}Module.cs`
- Dentro de cada feature hay **estructura libre**, aunque se recomienda
  mantener y poner casi siempre las subcarpetas `Application / Infrastructure / Presentation` dentro de dicha feature
### ❌ No hacer
- No agregar controllers, servicios ni repositorios en las carpetas raíz
  `Controllers/`, `Application/` o `Infrastructure/` — están deprecadas
- No poner lógica de negocio en los controllers
- No compartir archivos entre features o módulos salvo que 2 o más los usen.
  En ese caso: si los usan features del mismo módulo → mover a
  `Features/{Modulo}/Shared/`. Si los usan módulos distintos → mover a `Shared/`
## `Shared/` global
Contiene servicios reutilizables por cualquier módulo:
| Carpeta | Descripción |
|---|---|
| `Shared/Services/Email/` | Envío de correos delegados vía Graph API |
| `Shared/Services/SharePoint/` | Subida de archivos a SharePoint/OneDrive |
| `Shared/Services/Sunat/` | Consulta de RUC a Sunat |
| `Shared/Models/` | Modelos de BD compartidos entre módulos |
Para agregar un nuevo servicio compartido, crear la carpeta en
`Shared/Services/{NombreServicio}/` con subcarpetas `Interfaces/` y `Services/`.
## `Shared/` por módulo
Para archivos compartidos **solo dentro de un módulo**, usar
`Features/{Modulo}/Shared/` en vez del Shared global.
## Registro de dependencias
Cada módulo expone un método de extensión y se registra en `Program.cs`:

// Features/CostsModule/CostsModule.cs
public static IServiceCollection AddCostsModule(this IServiceCollection services)
{
services.AddScoped<IAlgunaInterface, AlgunaImplementacion>();
return services;
}

// Program.cs
builder.Services.AddCostsModule();
builder.Services.AddContractorsModule();

## Consideraciones sobre la base de datos
Puedes conectarte a la base de datos de desarrollo siempre que quieras y ejecutar las sentencias que quieras libremente.
Para el caso de la base de datos de producción solo deberás ejecutar sentencias select y no otro tipo de sentencia sql y siempre después de terminar una funcionalidad deberás de escribirme las sentencias sql necesarias por el chat de claude para yo ejecutarlas en producción. Para saber las credenciales puedes revisar sin problema en los archivos del backend appsettings.Development.json (que contiene las credenciales de dev), appsettings.Local.json (que contiene las credenciales de producción, no asumas que porque se llama Local tendrá las credenciales de dev) y appsettings.Production.json (que también contiene las credenciales de producción).
Los nombres de los modelos en su mayoría se mapean automáticamente a su version snake_case, a menos que se especifique otro mapeo para casos específicos (como User => app_user).
El campo state se usa para soft delete. Ningún campo puede ser eliminado de la base de datos para futura auditoría. En caso en la tabla no puedan haber duplicados (ya sea por nombre en la mayoría de casos o alguna otra condición), tiene que haber un index/validación en la base de datos para que puedan haber múltiples registros con state en false, pero solo 1 registro con state en true.
El campo active se usa para que dicho registro no aparezca en filtros/desplegables.
Para conectarse a la base de datos de producción, como dicha bd esta en la VPS como localhost, tengo que abrir siempre un túnel con ssh -L. Lo digo porque ese túnel a veces se cierra solo por inactividad así que en caso hubieras requerido hacer un select pero no pudiste puede que haya sido por esto que te digo. Avísame en esos casos para reabrir el túnel. Para conectarse a la base de datos de desarrollo no deberían haber problemas porque allí no se necesita de un túnel con ssh -L.
Al momento de crear campos, tablas, llaves foráneas, entre otras cosas relacionadas a la bd, debes de priorizar que este normalizada. Además, al agregar un campo como estado o tipos o algún dato 'predefinido', dichos datos se deben de encontrar registrados en una tabla. Por ejemplo si tengo una tabla contratistas que puede tener los estados de 'APROBADO' y 'DESAPROBADO', ambos registros deben de encontrarse en otra tabla (por ejemplo contratistas_estados) y la tabla contratistas debe de apuntar a esa tabla contratistas_estados. En estos casos nunca pongas los estados/tipos como texto plano en el mismo registro/fila de dicha tabla.
La base de datos de desarrollo/dev va a estar bastante desfasada en comparación a la base de datos de producción que siempre va a estar actualizada. Por ello en caso sucedan errores debido a que faltan campos en algunas tablas de la bd de desarrollo, consulta la bd de producción para encontrar las tablas/columnas/index/constraints/etc faltantes y que alinees tu mismo la base de datos de desarrollo.
Los campos de created_date_time y updated_date_time tienen que ser siempre timestampz (o algún equivalente a esto) y guardarse en la base de datos como UTC-0. Los datos al servirse al frontend (en caso se requiera) se tienen que servir en UTC-5.

## Consideraciones sobre la creación de querys a la base de datos
Se debe de priorizar el uso óptimo de conexiones que brinda la base de datos. Por tal motivo en el caso del backend, se deberán hacer la menor cantidad posible de roundtrips a la base de datos. Si por ejemplo al entrar a una funcionalidad se tienen que cargar los datos de los filtros y los datos de una tabla, pues se deberá hacer un solo endpoint que devuelva esos datos. Al entrar a ver un detalle de algo se deberá hacer otro endpoint que devuelva de una vez todos los detalles y no hacer endpoints  innecesarios. Al entrar a un dashboard se deberán devolver todos los datos de todos los gráficos en una sola petición HTTP. Solo se pueden hacer excepciones si es que la funcionalidad lo requiere o en funcionalidades muy especiales/específicas.
Cada acción de la página que necesite de datos debe de pedir los datos justos y necesarios. Por ejemplo al cargar un componente/página, el backend debe de devolver en un solo endpoint todos los datos (como los datos de filtros y los datos de una tabla), pero si el usuario va a usar los filtros pues se debe de crear otro endpoint que solo devuelva los datos de la tabla filtrados (puesto que ya no es necesario traer los datos de los filtros de nuevo).
Nunca usar Task.WhenAll con la base de datos puesto que usa más conexiones innecesarias. Solo usar Task.WhenAll en 'servicios más grandes' como lo es Microsoft Graph Api.
Evitar el problema N+1.
Para evitar los roundtrips a la base de datos puedes usar Dapper.

## Consideraciones para pruebas
Cuando termines de hacer/escribir código compila el proyecto para buscar posibles errores y corrígelos. No hagas previews, ni insertes datos/registros de prueba, ni trates de testear, yo testearé para verificar si está bien o no. Al momento de compilar asume que Abril-Backend.exe está bloqueado porque yo normalmente tengo el backend corriendo.
```