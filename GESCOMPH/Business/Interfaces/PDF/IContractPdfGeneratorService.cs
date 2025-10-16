using Entity.DTOs.Implements.Business.Contract;
using System.Threading.Tasks;

namespace Business.Interfaces.PDF
{
    /// <summary>
    /// Define un contrato para los servicios encargados de generar documentos PDF 
    /// asociados a contratos.
    /// 
    /// Este servicio abstrae la lógica de generación, formateo y renderizado de PDFs, 
    /// permitiendo distintas implementaciones (por ejemplo, usando iText, QuestPDF, 
    /// DinkToPdf, o cualquier motor compatible).
    /// </summary>
    public interface IContractPdfGeneratorService
    {
        /// <summary>
        /// Genera un archivo PDF basado en los datos del contrato especificado.
        /// </summary>
        /// <param name="contract">
        /// Objeto <see cref="ContractSelectDto"/> que contiene toda la información 
        /// necesaria para construir el documento (detalles del contrato, partes 
        /// involucradas, fechas, condiciones, etc.).
        /// </param>
        /// <returns>
        /// Un arreglo de bytes (<see cref="byte[]"/>) que representa el contenido 
        /// binario del archivo PDF generado.
        /// </returns>
        /// <remarks>
        /// Este método se espera que sea *idempotente*: generar múltiples veces el 
        /// mismo contrato debería producir resultados equivalentes (salvo diferencias 
        /// menores de metadatos o marcas de tiempo). 
        /// 
        /// El resultado binario puede ser almacenado, transmitido o devuelto como 
        /// parte de una respuesta HTTP (por ejemplo, como <c>application/pdf</c>).
        /// </remarks>
        Task<byte[]> GeneratePdfAsync(ContractSelectDto contract);

        /// <summary>
        /// Realiza una precarga o inicialización de recursos necesarios para la 
        /// generación de PDFs.
        /// </summary>
        /// <returns>
        /// Una tarea asincrónica que representa la operación de inicialización.
        /// </returns>
        /// <remarks>
        /// Este método puede utilizarse para inicializar motores de renderizado, 
        /// fuentes, plantillas o servicios externos, mejorando el tiempo de respuesta 
        /// de la primera llamada a <see cref="GeneratePdfAsync(ContractSelectDto)"/>.
        /// 
        /// Es especialmente útil en entornos donde la generación de PDFs es intensiva 
        /// o sensible al rendimiento (por ejemplo, microservicios o sistemas con 
        /// generación masiva de documentos).
        /// </remarks>
        Task WarmupAsync();
    }
}
