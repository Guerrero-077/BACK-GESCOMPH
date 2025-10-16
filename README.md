# GESCOMPH - Backend

API REST desarrollada con ASP.NET Core 8 para la gestión de contratos de arrendamiento y procesos administrativos. El backend cuenta con una arquitectura en capas, autenticación JWT, y características en tiempo real vía SignalR.

Este `README` contiene instrucciones para levantar el backend de forma local para desarrollo y pruebas. Para la orquestación completa del entorno con Docker, consulta el `README` en el repositorio principal de `GESCOMPH`.

## Requisitos Previos

Asegúrate de tener instaladas las siguientes herramientas:

- [Git](https://git-scm.com/): Para clonar el repositorio.
- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download): Incluye el runtime y las herramientas de desarrollo.
- [Herramienta de EF Core](https://learn.microsoft.com/en-us/ef/core/cli/dotnet):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Instalación y Ejecución en Local

Sigue estos pasos para ejecutar la aplicación directamente en tu máquina.

1.  **Clonar el repositorio:**

    ```bash
    git clone https://github.com/Guerrero-077/BACK-GESCOMPH BACK-GESCOMPH
    cd BACK-GESCOMPH
    ```

2.  **Configurar una base de datos SQL Server:**

    - Asegúrate de tener una instancia de SQL Server accesible.
    - Crea una base de datos llamada `gescomph`.

3.  **Configurar la cadena de conexión:**

    - Abre el archivo `GESCOMPH/WebGESCOMPH/appsettings.Development.json`.
    - Modifica la cadena de conexión `SqlServer` con tus credenciales:
      ```json
      {
        "ConnectionStrings": {
          "SqlServer": "Server=TU_SERVIDOR;Database=gescomph;User Id=TU_USUARIO;Password=TU_CONTRASEÑA;Encrypt=False;TrustServerCertificate=True"
        }
      }
      ```

4.  **Navegar a la carpeta de la solución:**

    ```bash
    cd GESCOMPH
    ```

5.  **Restaurar dependencias y aplicar migraciones:**
    Desde la carpeta `GESCOMPH` (la raíz de la solución .NET), ejecuta:

    ```bash
    dotnet restore GESCOMPH.sln
    dotnet ef database update --project Entity --startup-project WebGESCOMPH
    ```

6.  **Ejecutar la API:**

    - **Desde la línea de comandos:**
      ```bash
      dotnet run --project WebGESCOMPH
      ```
    - **Desde un IDE (Visual Studio, Rider):**
      Asegúrate de establecer `WebGESCOMPH` como el proyecto de inicio antes de ejecutar.

    La API quedará disponible en `http://localhost:8080` (o el puerto configurado en `launchSettings.json`).

## Comandos de Desarrollo

### Gestión de Migraciones

Cuando realices cambios en los modelos de datos (`Entity/Domain`), necesitarás generar una nueva migración.

```bash
# Generar una nueva migración
dotnet ef migrations add NombreDeLaMigracion --project Entity --startup-project WebGESCOMPH

# Aplicar la migración a la base de datos
dotnet ef database update --project Entity --startup-project WebGESCOMPH

# Eliminar la última migración (si no se ha aplicado)
dotnet ef migrations remove --project Entity --startup-project WebGESCOMPH
```

### Testing

```bash
# Ejecutar todas las pruebas
dotnet test Test/Test.csproj

# Ejecutar pruebas con cobertura
dotnet test Test/Test.csproj --collect:"XPlat Code Coverage"

# Ejecutar una clase de prueba específica
dotnet test Test/Test.csproj --filter "ClassName=TuClaseDePrueba"
```

### Build y Deployment

```bash
# Construir la solución
dotnet build GESCOMPH.sln --configuration Release

# Construir imagen Docker
docker build -f WebGESCOMPH/Dockerfile -t gescomph-api .

# Ejecutar con Docker Compose (si está disponible en el repo principal)
docker compose up
```

## Arquitectura del Proyecto

### Estructura de Proyectos

- **WebGESCOMPH**: Proyecto principal de la API (controladores, middleware, configuración)
- **Business**: Capa de servicios con lógica de negocio e interfaces
- **Data**: Implementación del patrón Repository y acceso a datos
- **Entity**: Modelos de dominio, DTOs, contexto de Entity Framework y configuraciones
- **Utilities**: Funcionalidades transversales y clases auxiliares
- **Templates**: Gestión de plantillas (probablemente para generación de PDF)
- **Test**: Pruebas unitarias e integración

### Patrones Arquitectónicos

- **Arquitectura en Capas**: Separación clara entre capas de presentación, negocio, datos y entidades
- **Patrón Repository**: Abstracción del acceso a datos en la capa Data
- **Unit of Work**: Gestión centralizada de transacciones
- **Patrón DTO**: Objetos de transferencia de datos para contratos de la API
- **Inyección de Dependencias**: Uso extensivo en todas las capas

### Base de Datos

La aplicación utiliza **SQL Server** como sistema de base de datos. La cadena de conexión se configura en `appsettings.json` bajo `ConnectionStrings:SqlServer`.

> **Nota**: El código incluye soporte preparado para PostgreSQL y MySQL, pero actualmente solo se utiliza SQL Server.

### Autenticación y Autorización

- **Autenticación JWT Bearer**: Basada en cookies con cookies HttpOnly
- **Protección CSRF**: Patrón double-submit cookie
- **Autorización Basada en Roles**: Sistema de permisos granular
- **Recuperación de Contraseña**: Verificación basada en códigos por email

### Características en Tiempo Real

- **SignalR**: Notificaciones de contratos y actualizaciones en tiempo real vía `ContractsHub`
- **Hangfire**: Procesamiento de trabajos en segundo plano para vencimientos de contratos y obligaciones

## Configuración Importante

### Registro de Servicios

Los servicios se registran mediante métodos de extensión en `WebGESCOMPH/Extensions/`:

- `AddApplicationServices()`: Servicios de capas de negocio y datos
- `AddInfrastructure()`: JWT, CORS, base de datos, Cloudinary, Hangfire
- `AddPresentationControllers()`: Controladores y validación

### Orden del Pipeline de Middleware

Orden crítico del middleware en `Program.cs`:

1. `UseForwardedHeaders()` - Soporte para proxy
2. `ExceptionMiddleware` - Manejo global de excepciones
3. `UseStaticFiles()` - Contenido estático
4. `UseCors()` - Solicitudes de origen cruzado
5. `UseHttpsRedirection()` - Aplicación de HTTPS
6. `UseAuthentication()` - Validación JWT
7. `UseAuthorization()` - Acceso basado en roles

## Entidades de Dominio

### Entidades de Negocio Principales

- **Contract**: Entidad principal de contratos de arrendamiento
- **ObligationMonth**: Obligaciones de pago mensual con cálculos UVT
- **Establishment**: Locales/ubicaciones de arrendamiento
- **Plaza**: Gestión de plazas comerciales/mercados
- **Appointment**: Sistema de programación de citas

### Entidades de Seguridad

- **User/Person**: Gestión de usuarios con información personal
- **Rol/Permission**: Control de acceso basado en roles
- **RolFormPermission**: Permisos granulares de formularios de UI

## Framework de Testing

- **xUnit**: Framework principal de testing
- **FluentAssertions**: Biblioteca de aserciones
- **Moq**: Framework de mocking
- **Base de Datos InMemory**: Proveedor en memoria de EF Core para pruebas unitarias

## Guías de Desarrollo

### Migraciones de Entity Framework

- Ejecuta siempre las migraciones desde la raíz de la solución (directorio `GESCOMPH/`)
- Usa `--project Entity --startup-project WebGESCOMPH` para todos los comandos EF
- Cada migración debe tener un nombre descriptivo

### Desarrollo de API

- Los controladores heredan de `BaseController<TGet, TCreate, TUpdate>` para operaciones CRUD estándar
- Usa el atributo `[Authorize]` para endpoints protegidos
- Implementa lógica de negocio específica en métodos de servicio dedicados

### Configuración de Base de Datos

- La aplicación utiliza `ApplicationDbContext` para SQL Server
- Configura la cadena de conexión en `ConnectionStrings:SqlServer`
- Para cambios de esquema, genera y aplica migraciones de Entity Framework

## Servicios en Segundo Plano

### Trabajos de Hangfire

- **Vencimiento de Contratos**: Actualizaciones automáticas del estado de contratos
- **Generación de Obligaciones**: Creación de obligaciones de pago mensual
- **Dashboard**: Disponible en `/hangfire` (en desarrollo)

### Configuración

- **Programación Cron**: Configurable vía `Contracts:Expiration:Cron` en appsettings
- **Zona Horaria**: Usa zona horaria de Colombia (`America/Bogota`)
