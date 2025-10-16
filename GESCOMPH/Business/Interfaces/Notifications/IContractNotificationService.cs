namespace Business.Interfaces.Notifications
{
    /// <summary>
    /// Define un contrato para el servicio de notificaciones relacionadas con 
    /// eventos del ciclo de vida de un contrato.
    /// 
    /// Esta interfaz abstrae la lógica de envío de notificaciones cuando un 
    /// contrato es creado, expirado, eliminado o cambia de estado.
    /// 
    /// Su propósito es desacoplar la lógica de negocio de la capa de 
    /// comunicación o mensajería, permitiendo diferentes implementaciones 
    /// (por ejemplo, correo electrónico, SMS, colas de eventos, etc.).
    /// </summary>
    public interface IContractNotificationService
    {
        /// <summary>
        /// Envía una notificación cuando se crea un nuevo contrato.
        /// </summary>
        /// <param name="contractId">Identificador único del contrato creado.</param>
        /// <param name="personId">Identificador de la persona asociada al contrato.</param>
        /// <remarks>
        /// Este método puede utilizarse para informar al usuario o al sistema 
        /// de terceros que un nuevo contrato ha sido registrado exitosamente.
        /// </remarks>
        Task NotifyContractCreated(int contractId, int personId);

        /// <summary>
        /// Envía una notificación cuando un contrato ha expirado.
        /// </summary>
        /// <param name="contractId">Identificador único del contrato expirado.</param>
        /// <param name="personId">Identificador de la persona asociada al contrato.</param>
        /// <remarks>
        /// Ideal para alertar al usuario o a otros sistemas de que el contrato 
        /// ha llegado a su fecha de vencimiento y puede requerir renovación.
        /// </remarks>
        Task NotifyContractExpired(int contractId, int personId);

        /// <summary>
        /// Envía una notificación cuando cambia el estado activo de un contrato.
        /// </summary>
        /// <param name="contractId">Identificador del contrato cuyo estado ha cambiado.</param>
        /// <param name="active">
        /// Valor booleano que indica el nuevo estado: 
        /// <c>true</c> si el contrato está activo, <c>false</c> si fue desactivado.
        /// </param>
        /// <remarks>
        /// Útil para sincronizar cambios de estado entre sistemas o notificar 
        /// al cliente de una activación o suspensión del contrato.
        /// </remarks>
        Task NotifyContractStatusChanged(int contractId, bool active, int personId);

        /// <summary>
        /// Envía una notificación cuando un contrato ha sido eliminado.
        /// </summary>
        /// <param name="contractId">Identificador único del contrato eliminado.</param>
        /// <remarks>
        /// Este evento puede ser utilizado para realizar auditorías, 
        /// limpiar datos relacionados o informar a terceros sobre la eliminación.
        /// </remarks>
        Task NotifyContractDeleted(int contractId, int personId);
    }
}
