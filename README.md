# GESCOMPH - Backend

API REST desarrollada con ASP.NET Core 8 para la gestión de contratos de arrendamiento y procesos administrativos.

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
      "ConnectionStrings": {
        "SqlServer": "Server=TU_SERVIDOR;Database=gescomph;User Id=TU_USUARIO;Password=TU_CONTRASEÑA;Encrypt=False;TrustServerCertificate=True"
      }
      ```

4.  **Restaurar dependencias y aplicar migraciones:**
    Desde la carpeta `GESCOMPH` (la raíz de la solución .NET), ejecuta:
    ```bash
    dotnet restore GESCOMPH.sln
    dotnet ef database update --project Entity --startup-project WebGESCOMPH
    ```

5.  **Ejecutar la API:**

    - **Desde la línea de comandos:**
      ```bash
      dotnet run --project WebGESCOMPH
      ```
    - **Desde un IDE (Visual Studio, Rider):**
      Asegúrate de establecer `WebGESCOMPH` como el proyecto de inicio antes de ejecutar.

    La API quedará disponible en `http://localhost:8080` (o el puerto configurado en `launchSettings.json`).

## Gestión de Migraciones (Desarrollo)

Cuando realices cambios en los modelos de datos (`Entity/Domain`), necesitarás generar una nueva migración.

1.  **Generar una nueva migración:**
    Desde la carpeta `GESCOMPH` (la raíz de la solución .NET), ejecuta:
    ```bash
    dotnet ef migrations add NombreDeLaMigracion --project Entity --startup-project WebGESCOMPH
    ```

2.  **Aplicar la migración a la base de datos:**
    ```bash
    dotnet ef database update --project Entity --startup-project WebGESCOMPH
    ```
