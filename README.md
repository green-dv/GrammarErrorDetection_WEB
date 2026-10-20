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

### Infraestructura: GrammarErrorDetection.Infraestructure

Está destinada a las implementaciones de comunicación con sistemas externos, especialmente clientes HTTP para consumir la API y el manejo técnico de solicitudes, respuestas y autenticación. Los repositorios de acceso a la base de datos pertenecen al backend, en su proyecto independiente.

El cliente `ApiClient` implementa `IApiClient` y está registrado para inyección de dependencias en `Program.cs`. Expone tres operaciones:

```csharp
Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);
Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest data, CancellationToken cancellationToken = default);
Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest data, CancellationToken cancellationToken = default);
```

Las solicitudes POST y PUT envían JSON y las tres operaciones deserializan las respuestas JSON al tipo solicitado. Las respuestas exitosas sin contenido devuelven `default(TResponse)`; los códigos HTTP de error generan `HttpRequestException`. Los errores de red, cancelación y JSON inválido se propagan al consumidor para que pueda manejarlos.

Se pueden enviar URLs absolutas para consumir APIs distintas. Para usar rutas relativas, configurar el `BaseAddress` del `HttpClient` en `Program.cs` con la URL de la API. Actualmente conserva la dirección del frontend; el puerto `7178` corresponde a la interfaz web, no a la API externa. El cliente no añade credenciales ni tokens automáticamente.

### Modelos: GrammarErrorDetection.Models

Contiene los modelos de datos compartidos entre las capas del frontend: DTOs, solicitudes, respuestas y enumeraciones. No debe depender de la interfaz visual ni de las implementaciones de infraestructura.

