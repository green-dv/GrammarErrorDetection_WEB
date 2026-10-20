# GrammarErrorDetection — Portal académico CETI

## Descripción

Sistema web para el CETI que ofrece un portal académico para estudiantes, profesores y coordinadores. Su aporte principal es la implementación de modelos de procesamiento de lenguaje natural (NLP) para analizar errores gramaticales en párrafos escritos por los estudiantes y apoyar su proceso de aprendizaje.

Este repositorio contiene el frontend desarrollado con Blazor WebAssembly y .NET 10. La API y los modelos de ML se mantienen en proyectos independientes.

## Clonar el proyecto

```bash
git clone https://github.com/green-dv/GrammarErrorDetection_WEB.git GrammarErrorDetection.WEB
cd GrammarErrorDetection.WEB
```

## Cómo iniciar el proyecto

### Requisitos

- SDK de .NET 10.
- Un navegador actualizado.
- Conexión a Internet para descargar las dependencias y cargar Tailwind CSS desde el CDN.

Ejecutar los siguientes comandos desde la raíz del repositorio:

```bash
dotnet restore GrammarErrorDetection.WEB.slnx
dotnet build GrammarErrorDetection.WEB.slnx
dotnet run --project GrammarErrorDetection.WEB/GrammarErrorDetection.WEB.csproj --launch-profile https
```

Abrir [https://localhost:7178](https://localhost:7178) en el navegador.

Si el certificado HTTPS de desarrollo todavía no es de confianza, ejecutar una vez:

```bash
dotnet dev-certs https --trust
```

Para desarrollar con recarga automática:

```bash
dotnet watch --project GrammarErrorDetection.WEB/GrammarErrorDetection.WEB.csproj run --launch-profile https
```

La interfaz puede iniciarse de forma independiente. Las funciones que consuman el backend requerirán iniciar la API y los servicios de ML correspondientes, y configurar su conexión cuando se implemente la integración.

### Tailwind CSS

Tailwind CSS 4 se importa desde `GrammarErrorDetection.WEB/wwwroot/index.html` mediante el CDN oficial documentado. Las clases se pueden utilizar directamente en los componentes `.razor`:

```razor
<h1 class="text-3xl font-bold text-blue-700">Portal académico CETI</h1>
```

Esta integración no requiere Node.js ni comandos adicionales para generar los estilos. El CDN está destinado al desarrollo; antes de publicar en producción debe sustituirse por CSS compilado mediante Tailwind CLI u otra integración de compilación. Consultar la [documentación oficial de Tailwind CSS](https://tailwindcss.com/docs/installation/play-cdn).

Bootstrap y los estilos existentes siguen presentes mientras se adaptan las páginas a Tailwind. Al combinar ambos frameworks, revisar los estilos de los componentes porque pueden existir clases coincidentes y estilos globales que afecten su apariencia.

## Arquitectura del sistema

El frontend se organiza por capas para separar la presentación, la coordinación de operaciones, la comunicación HTTP y los modelos de datos.

```text
GrammarErrorDetection.WEB/
├── GrammarErrorDetection.WEB/             # Presentación en Blazor WebAssembly
│   ├── Pages/                            # Páginas y rutas
│   ├── Layout/                           # Estructura visual y navegación
│   └── wwwroot/                          # Archivos estáticos y estilos
├── GrammarErrorDetection.Services/        # Servicios e interfaces del frontend
├── GrammarErrorDetection.Infraestructure/ # Integraciones y clientes HTTP
├── GrammarErrorDetection.Models/          # Modelos, DTOs, solicitudes y respuestas
└── GrammarErrorDetection.WEB.slnx
```

### Presentación: GrammarErrorDetection.WEB

Contiene las páginas, componentes, formularios y navegación para estudiantes, profesores y coordinadores. Se ejecuta en el navegador y muestra la información proporcionada por los servicios. No debe acceder directamente a la base de datos ni almacenar secretos del backend.

### Servicios: GrammarErrorDetection.Services

Está destinada a los servicios e interfaces que coordinan las operaciones del frontend, como solicitar el análisis de un párrafo o consultar resultados. Permite mantener esa coordinación fuera de los componentes visuales. Las reglas de negocio que requieran validación autoritativa deben ejecutarse en la API.

Las interfaces se ubican en `GrammarErrorDetection.Services/Interfaces/`; las implementaciones de autenticación y su estado temporal, en `GrammarErrorDetection.Services/Services/Auth/`.

### Infraestructura: GrammarErrorDetection.Infraestructure

Está destinada a las implementaciones de comunicación con sistemas externos, especialmente clientes HTTP para consumir la API y el manejo técnico de solicitudes, respuestas y autenticación. Los repositorios de acceso a la base de datos pertenecen al backend, en su proyecto independiente.

El cliente `ApiClient` implementa `IApiClient` y está registrado para inyección de dependencias en `Program.cs`. Expone tres operaciones:

```csharp
Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);
Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest data, CancellationToken cancellationToken = default);
Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest data, CancellationToken cancellationToken = default);
```

Las solicitudes POST y PUT envían JSON y las tres operaciones deserializan las respuestas JSON al tipo solicitado. Las respuestas exitosas sin contenido devuelven `default(TResponse)`; los códigos HTTP de error generan `HttpRequestException`. Los errores de red, cancelación y JSON inválido se propagan al consumidor para que pueda manejarlos.

Se pueden enviar URLs absolutas para consumir APIs distintas. Las rutas relativas del cliente de infraestructura usan `Api:BaseUrl`, configurada en `GrammarErrorDetection.WEB/wwwroot/appsettings.json` como `https://localhost:7028/`. El `HttpClient` general del frontend conserva su propia dirección para archivos estáticos. El cliente no añade credenciales ni tokens automáticamente.

### Modelos: GrammarErrorDetection.Models

Contiene los modelos de datos compartidos entre las capas del frontend: DTOs, solicitudes, respuestas y enumeraciones. No debe depender de la interfaz visual ni de las implementaciones de infraestructura.

## Prueba del login base

### Registro inicial

En `/auth/register` se solicitan nombres, apellidos, CI, teléfono opcional, correo, contraseña y confirmación. El formulario utiliza validación propia, errores compactos y lógica de entrada en `Views/Auth/Register.razor.cs`. Envía únicamente `email`, `purpose: "signup"` y `password` a `POST /api/Auth/SendCode`; los datos personales y la confirmación de contraseña no se envían en esta etapa. La contraseña conserva su valor original y HTTPS protege el transporte.

Al recibir una respuesta válida, abre `/auth/register/verification`. `RegistrationFlowState` conserva el desafío y una copia de los datos completos del usuario, incluida la contraseña, únicamente en memoria; no usa almacenamiento del navegador. Al pulsar «Verificar y crear cuenta», envía `firstName`, `lastName`, `ci`, `email`, `password`, `phone`, `challengeId` y `code` a `POST /api/Auth/Signup`. El backend valida el código antes de crear la cuenta, en la misma operación. Los errores permiten corregir el código y reintentar mientras el desafío siga vigente. El estado temporal se limpia al completar, cancelar o vencer el registro; recargar la página también pierde estos datos. Si la respuesta contiene una sesión válida, se conserva la sesión en `sessionStorage` y se abre `/home`.

Los estilos específicos están en `wwwroot/css/auth/register.css`. El layout compartido compacta el encabezado en pantallas pequeñas para dar espacio al formulario.

### Inicio de sesión

Abrir [https://localhost:7178/auth/login](https://localhost:7178/auth/login). El formulario también está disponible en `/` y `/login`.

El flujo es `Login.razor → IAuthService → IApiClient → POST https://localhost:7028/api/Auth/Login`. El cuerpo JSON contiene únicamente `email` y `password`.

- Si las credenciales son válidas y `data.isTotpEnabled` es `true`, abre `/auth/methods` para elegir correo electrónico o aplicación autenticadora.
- Si `data.isTotpEnabled` es `false`, solicita automáticamente el código por correo y abre `/auth/verify`.
- Al elegir correo manualmente, también llama a `POST /api/Auth/SendCode`, con `email`, `password` y `purpose: "login"`. Un envío correcto libera la contraseña temporal y conserva el desafío hasta su vencimiento.
- La pantalla `/auth/verify` envía únicamente `challengeId` y `code` a `POST /api/Auth/Login/Verify`. El código se maneja como texto para conservar ceros iniciales.
- La opción de autenticador usa `POST /api/Auth/Login/VerifyTotp`, con `email`, `password` y `code`, sin llamar a `SendCode`.
- El estado pendiente vive en memoria y se limpia al completar, cancelar o vencer el flujo. Recargar la página requiere volver al login.
- Los errores de validación, credenciales, límite de intentos, red y respuestas inválidas se muestran juntos en un panel arriba de los campos. La validación nativa HTML está desactivada.

### Sesión tras verificar

Login por correo, TOTP y registro comparten `AuthSessionResponse` (`accessToken`, `tokenType`, `expiresAt`, `user`). `AuthSessionState` acepta la respuesta solo si contiene un token Bearer, una fecha vigente y el usuario esperado. La sesión se guarda en `sessionStorage` y se recupera antes de evaluar las rutas al recargar. Al verificar correctamente, abre `/home`. Cerrar sesión o vencer la sesión exige autenticarse de nuevo. El JWT queda disponible en `AuthSessionState.Current` para las siguientes integraciones con endpoints protegidos.

En el backend actual, `Login/Verify` acepta desafíos `login_email`; `Signup` valida los desafíos `signup_email` y crea el usuario en la misma operación, devolviendo la misma estructura de sesión. El registro conserva este endpoint para no consumir un desafío de registro en una operación de login.

Para comprobar el rol recibido, `SessionRolePreview` muestra «Rol del JWT · comprobación» en `/home` y en `/auth/login` si ya existe una sesión. Lee `role`, `roles` o el claim URI de .NET desde el payload, siguiendo el formato de claims JSON del [estándar JWT](https://www.rfc-editor.org/rfc/rfc7519.html). Si no hay un rol legible muestra «No informado». Esta lectura es solo visual; no valida la firma ni concede permisos. No se muestra el token completo.

### Archivos recuperados de Failed

Se reutilizaron `Layout/AuthLayout.razor`, `Views/Auth/Components/AuthHeader.razor`, `Views/Auth/Components/AuthPasswordField.razor`, `wwwroot/images/ceti-logo.png` y `wwwroot/images/auth-globe.png`.

`Views/Auth/Login.razor` y `Views/Auth/SelectVerificationMethod.razor` adaptan esas vistas al contrato real de la API. `wwwroot/css/auth.css` importa cinco archivos en `wwwroot/css/auth/`: `theme.css` (colores), `layout.css` (estructura visual), `form.css` (campos y mensajes), `methods.css` (selección de verificación) y `register.css` (registro). Estos estilos recuperan las reglas necesarias de `Failed`; no hay estilos inline en `index.html`. No se importaron el registro, la verificación de códigos, el proveedor de autenticación de prueba ni los estilos de las otras páginas.

### Sidebar, perfil y TOTP

El sidebar recupera el diseño de `Failed`: navegación compacta o expandida, búsqueda, menú de cuenta y cierre de sesión. `Navigation/PortalNavigation.cs` centraliza estas reglas del JWT:

| Rol | Opciones específicas |
| --- | --- |
| Student | Mis inscripciones, Mis cursos |
| Teacher | Cursos (solo propios), Inscripciones |
| Coordinator | Usuarios, Cursos, Inscripciones |

Inicio, material de apoyo, leaderboard, procesamiento y perfil están disponibles para las sesiones autenticadas. `PortalRouteView` también comprueba los permisos al introducir una URL directamente. Las secciones académicas todavía no implementadas muestran un aviso, sin datos simulados. La autorización de operaciones y la validación de la firma del JWT siguen a cargo de la API.

En `/profile`, la pestaña «Configuración TOTP» ejecuta `POST /api/Auth/Totp/Setup` sin cuerpo y genera el QR localmente con [QRCoder](https://www.nuget.org/packages/QRCoder/1.8.1), a partir de `data.otpAuthUri`. También ofrece una clave manual. El URI, el QR y la clave no se guardan en almacenamiento del navegador ni se envían a generadores de QR externos.

La confirmación usa `POST /api/Auth/Totp/Confirm` con únicamente `code`, conservando los ceros iniciales. Solo `data: true` sin error activa el estado visual de TOTP y elimina la clave y el QR temporales. Una respuesta `false` permite corregir el código y reintentar. Un `401` limpia la sesión y vuelve al login.

`ApiAuthorizationHandler` agrega `Authorization: Bearer <accessToken>` únicamente a requests HTTPS del host y puerto de `Api:BaseUrl`. La API debe permitir el encabezado `Authorization` en CORS. Las llamadas a otros orígenes no reciben el JWT.

### HTTPS y contraseña

La sesión autenticada (JWT, usuario, expiración y estado TOTP) se guarda en `sessionStorage` y se restaura antes de evaluar las rutas protegidas. Esto permite recargar la misma pestaña sin volver al login. Al cerrar sesión o detectar su expiración se elimina del almacenamiento. Los datos pendientes del registro, las contraseñas y los códigos permanecen únicamente en memoria. Si el navegador bloquea el almacenamiento, la sesión funciona solo en memoria.

La configuración exige HTTPS para la API y utiliza la validación de certificados del navegador. El campo `password` se envía tal cual en el JSON, sin cifrar, hashear ni transformar en el frontend. La conexión HTTPS sigue protegiendo el transporte. No se guarda en almacenamiento del navegador, no se añade a URLs y se limpia del formulario al terminar la solicitud. Un cifrado adicional del campo requeriría acordar un protocolo de claves y descifrado con la API. Consultar la [guía de TLS de OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Transport_Layer_Security_Cheat_Sheet.html).

### CORS en el backend

Como el frontend y la API usan puertos distintos, la API debe permitir el origen exacto `https://localhost:7178`, los métodos utilizados y los encabezados `Content-Type` y `Authorization`. Si se necesita configurar una política en el proyecto externo de la API, registrar antes de `builder.Build()`:

```csharp
builder.Services.AddCors(options => options.AddPolicy("CetiWeb", policy =>
    policy.WithOrigins("https://localhost:7178")
          .WithMethods("POST")
          .WithHeaders("Content-Type", "Authorization")));
```

Y agregar después de `app.UseHttpsRedirection()` y antes de autenticación/autorización:

```csharp
app.UseCors("CetiWeb");
```

Reiniciar la API después de aplicar esta configuración. No se requiere permitir cualquier origen ni desactivar la validación HTTPS.

### Listado de cursos

La acción Ver abre `/courses/{classId}` (también `/teaching-courses/{classId}`), adaptando la vista de detalle de `Failed`: módulo, paralelo, docente, aula, horario, fechas y tarjetas con el total inscrito y promedios de exámenes disponibles. La media de ambos exámenes es su promedio aritmético y solo aparece si existen ambas notas. Reutiliza `Views/Users/UserList` con un contexto `Course`, que impone `filter.classId` en cada `POST /api/User/search`, incluso al buscar, limpiar, paginar o reintentar. No carga los catálogos globales ni ofrece cambios de cuentas desde el curso. Coordinadores pueden consultar cualquier curso; Teacher debe ser su docente tanto al cargar el detalle como al consultar sus estudiantes. El listado global `/users` sigue reservado a coordinadores. Las notas e inscripción individuales se muestran solo si `currentCourse.classId` coincide con el curso seleccionado, para no atribuir notas de otro periodo. No se simulan reportes gramaticales ni calificaciones no incluidas en la respuesta.

«Generar cursos automáticamente» abre `GenerateCoursesModal` para coordinadores. Solicita año, bimestre y fechas de destino, muestra el periodo anterior de origen (incluyendo el salto de año) y valida fechas ordenadas dentro del año elegido. Envía únicamente esos cuatro campos a `POST /api/Registration/generate-next-bimester`, con el JWT del cliente API. Bloquea doble envío y cierre durante la operación. Conserva el resultado en el modal con cantidades de cursos creados/reutilizados, inscripciones creadas y motivos de las omisiones; no vuelve a enviar desde esa pantalla. Al completarse, consulta la primera página del periodo de destino. Ante un resultado incierto, pide revisar el listado antes de reenviar porque la operación puede haber guardado cambios parciales.

Editar reutiliza el mismo modal con los datos completos precargados. Lee todas las páginas de `GET /api/Registration?Filter.ClassId=...&Filter.Active=true` y completa los nombres mediante elegibles asignados del módulo y periodo original antes de habilitar el guardado. Conserva a todos los inscritos aunque no estén en la página visible. Permite añadir elegibles, trasladar desde otra clase con confirmación y quitar actuales con confirmación de bajas; `studentIds: []` permite quitar a todos en edición. Envía `PUT /api/Class/with-students` con el `id`, todos los campos del curso y la lista final explícita. Verifica el ID de respuesta y las asignaciones finales, y lee `removedRegistrationIds` para informar de las bajas. Una carga incompleta de inscripciones bloquea el guardado. La edición está disponible solo para coordinadores y respeta el filtro fijo de Teacher.

El botón «Crear curso» del coordinador abre `Views/Courses/CreateCourseModal.razor`, con los datos de módulo, periodo, profesor, aula, paralelo, fechas y horario, y un panel de estudiantes elegibles. Consulta `GET /api/Class/eligible-students` con módulo, año, bimestre, paginación y filtros opcionales de nombre, apellido y CI. Conserva la selección entre páginas y búsquedas, y la reinicia al cambiar módulo o periodo. Los estudiantes con `AlreadyAssigned` o `isAssigned` muestran su asignación y requieren confirmación de traslado. Envía `POST /api/Class/with-students` con datos del curso activo y `studentIds` de usuarios existentes; no crea usuarios. Al confirmar la respuesta, cierra el modal y actualiza el listado. Valida fechas, horario y selección, bloquea envíos duplicados y distingue el éxito de creación de un fallo posterior al refrescar. Ante una respuesta de creación incierta, pide revisar el listado antes de reenviar.

El rol `Teacher` también puede abrir `/courses` y la ruta anterior `/teaching-courses`. Su filtro de profesor muestra el ID del usuario de la sesión como solo lectura; antes de cada consulta se impone ese mismo ID, incluyendo búsqueda, limpieza de filtros, paginación y reintentos. No se carga el catálogo de profesores para este rol. `Coordinator` conserva el selector editable. Esta restricción de interfaz debe complementarse con autorización del lado de la API.

`/courses` recupera el diseño de `Failed` para el administrador (rol `Coordinator`). `Views/Courses/CourseList.razor` reutiliza los estilos compartidos de tablas y `wwwroot/css/courses.css` agrega la distribución adaptable. `ICourseService` consulta `GET /api/Class` con `PageNumber`, `PageSize` y filtros opcionales `Filter.Id`, `Filter.TeacherId`, `Filter.ModuleNameId`, `Filter.Bimester`, `Filter.Year`, `Filter.StartDate`, `Filter.EndDate`, `Filter.Parallel`, `Filter.ClassroomId`, `Filter.StartTime`, `Filter.EndTime`, `Filter.CreatedAt` y `Filter.Active`. Omite campos vacíos, conserva `false` y codifica los valores de query; no envía cuerpo.

Los filtros se aplican al pulsar Buscar o Enter. Los selectores recorren sus catálogos en páginas de 100: módulos mediante `GET /api/ModuleName`, profesores mediante `POST /api/User/search` con `filter.roleId = f2700000-0000-4000-8000-000000000002` y aulas mediante `GET /api/Classroom` con `PageNumber` y `PageSize` en query. Cada catálogo puede reintentarse sin bloquear los que sí cargaron. La paginación consulta al servidor. En el backend local, `ClassController` devuelve `count = data.Count`, por lo que se muestran el rango y la página actual; Siguiente se habilita al recibir una página completa y conserva la anterior si la siguiente está vacía. Las tarjetas resumen únicamente la página cargada, sin presentar esos cálculos como estadísticas globales. Las notas conservan decimales, los nulos aparecen como pendientes y las horas aceptan la terminación `Z` sin convertir la zona horaria. Se reutiliza el JWT del cliente API y se manejan carga, errores, reintentos, cancelación y expiración de sesión. La creación, edición y generación automática están habilitadas para coordinadores. Ver abre los estudiantes del curso para coordinadores y su docente; enviar a papelera sigue deshabilitado.

### Listado de usuarios y tablas

`/users` está disponible para el rol `Coordinator` del JWT. La página comprueba el rol incluso al acceder por URL, y el menú muestra Usuarios únicamente para coordinadores. `Views/Users/UserList.razor` define la tabla y `UserList.razor.cs` coordina filtros, carga y paginación. `IUserService` y `Services/Users/UserService` consumen `POST /api/User/search`, enviando siempre `pageNumber` y `pageSize` (20 por defecto); `filter` y sus campos nulos se omiten cuando no se usan. Se admiten nombre, apellido, CI, teléfono, correo, fecha de registro, rol, estado y módulo actual. El estado Inactivo se envía como `false`, sin confundirlo con el filtro vacío.

Los selectores de roles y módulos consultan `GET /api/Role` y `GET /api/ModuleName`, recorriendo sus páginas de 100 elementos. La búsqueda se ejecuta al pulsar Buscar o Enter; al cambiar filtros o tamaño de página vuelve a la primera página. La paginación consulta al servidor, sin filtrar ni paginar otra vez en memoria. En el backend actual, `count` representa la cantidad de filas devueltas en esa página: no se muestra un total de páginas inventado. Siguiente se habilita cuando llega una página completa; si la siguiente está vacía, se conserva la página anterior y se indica el final del listado.

`ApiAuthorizationHandler` agrega el JWT como Bearer únicamente a solicitudes HTTPS dirigidas al host y puerto de `Api:BaseUrl`. Un `401` limpia la sesión y vuelve al login; los demás errores permiten reintentar desde el listado. Se cancelan las consultas al abandonar la vista y se ignoran respuestas anteriores. Los cursos y horarios ausentes muestran un texto alternativo, y la nota conserva sus decimales.

Ver navega a `/users/{id}`, una ruta reservada para el futuro perfil (todavía sin implementar). Editar abre un modal con nombre, apellido, CI, correo, teléfono, rol, estado y solicitud de cambio de contraseña. `UserEditForm` compara los valores con el usuario original: `PUT /api/User` envía únicamente `id` y los campos modificados, omitiendo las propiedades nulas y conservando valores explícitos como `false` o teléfono vacío. Guardar permanece deshabilitado si no hay diferencias; el servicio también rechaza un PUT sin cambios. No se editan ni se reenvían contraseñas, secretos TOTP o campos de cursos. La respuesta de actualización se deserializa con `UserUpdateResponse`, que conserva solo los datos personales y de estado del usuario.

La papelera solicita confirmación y envía exclusivamente `{ id, active: false }`. Los usuarios inactivos muestran una acción de reactivación que envía `{ id, active: true }`; no hay borrado físico. Tras una actualización correcta se consulta de nuevo la página con los filtros aplicados. Si falla esa recarga, se distingue el guardado exitoso del error de consulta para evitar repetir el PUT. Los modales utilizan el elemento nativo `dialog`, mantienen el foco dentro de la ventana y bloquean envíos duplicados mientras guardan. Sus estilos compartidos están en `wwwroot/css/modals.css`.

`wwwroot/css/tables.css` centraliza el estilo reutilizable de los listados. Usar `data-table-card` como contenedor, `data-table-scroll` alrededor de una `table.data-table`, `data-table-toolbar` para filtros y `data-table-footer` para paginación. `data-table-primary` y `data-table-secondary` agrupan dos líneas en una celda. También incluye estilos para avatares, módulos, notas, estados vacíos y botones. `Components/Tables/RowActions.razor` ofrece las acciones Ver, Editar y Eliminar con etiquetas accesibles. `wwwroot/css/users.css` contiene solo las reglas de distribución específicas de usuarios.
