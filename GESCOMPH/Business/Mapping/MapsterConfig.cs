using System.Reflection;
using Business.Mapping.Registers;
using Mapster;

namespace Business.Mapping
{
    /// <summary>
    /// Clase responsable de la configuración global de los mapeos de objetos 
    /// utilizando Mapster. 
    /// 
    /// Centraliza el registro de todos los perfiles de mapeo (clases que 
    /// implementan <see cref="IRegister"/>) dentro del ensamblado de la capa 
    /// Business, evitando así tener que registrar cada clase de mapeo manualmente.
    /// </summary>
    public static class MapsterConfig
    {
        /// <summary>
        /// Registra todas las configuraciones de mapeo definidas en el ensamblado
        /// actual. 
        /// 
        /// Utiliza el método <see cref="TypeAdapterConfig.Scan(Assembly)"/> para 
        /// buscar automáticamente todas las clases que implementan la interfaz 
        /// <see cref="IRegister"/> y aplicar sus reglas de mapeo. 
        /// 
        /// Esto permite una configuración más escalable y mantenible, ya que 
        /// evita tener que instanciar manualmente cada clase de mapeo como, por 
        /// ejemplo, <c>new AdministrationSystemMapping().Register(config);</c>.
        /// </summary>
        /// <returns>
        /// Instancia de <see cref="TypeAdapterConfig"/> con todas las 
        /// configuraciones de mapeo registradas.
        /// </returns>
        public static TypeAdapterConfig Register()
        {
            // Obtiene la configuración global de Mapster.
            var config = TypeAdapterConfig.GlobalSettings;

            // Ejemplo de registro manual (ya no necesario al usar Scan):
            // new AdministrationSystemMapping().Register(config);

            // Registra automáticamente todas las clases de mapeo (IRegister)
            // contenidas en el ensamblado actual.
            config.Scan(typeof(AdministrationSystemMapping).GetTypeInfo().Assembly);

            return config;
        }
    }
}
