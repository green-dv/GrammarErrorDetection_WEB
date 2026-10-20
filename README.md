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
| Teacher | Mis cursos, Inscripciones |
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

