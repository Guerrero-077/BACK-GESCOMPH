using Business.Interfaces.Implements.Utilities;
using Business.Repository;
using Data.Interfaz.IDataImplement.Utilities;
using Entity.Domain.Models.Implements.Utilities;
using Entity.DTOs.Implements.Utilities.Images;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Utilities.Exceptions;
using Utilities.Helpers.CloudinaryHelper;

namespace Business.Services.Utilities
{
    /// <summary>
    /// Servicio encargado de manejar imágenes utilizando Cloudinary como proveedor de almacenamiento.
    /// </summary>
    public sealed class ImageService :
        BusinessGeneric<ImageSelectDto, ImageCreateDto, ImageUpdateDto, Images>, IImagesService
    {
        private readonly IImagesRepository _imagesRepository;
        private readonly CloudinaryUtility _cloudinary;
        private readonly ILogger<ImageService> _logger;

        private const int MaxParallelUploads = 3;
        private const int MaxFilesPerRequest = 5;

        public ImageService(
            IImagesRepository imagesRepository,
            CloudinaryUtility cloudinary,
            IMapper mapper,
            ILogger<ImageService> logger
        ) : base(imagesRepository, mapper)
        {
            _imagesRepository = imagesRepository;
            _cloudinary = cloudinary;
            _logger = logger;
        }

        /// <summary>
        /// Agrega múltiples imágenes asociadas a un establecimiento.
        /// Valida formato, controla la concurrencia y realiza rollback en caso de error.
        /// </summary>
        /// <param name="establishmentId">Identificador del establecimiento.</param>
        /// <param name="files">Colección de archivos a subir.</param>
        /// <returns>Lista de imágenes creadas.</returns>
        /// <exception cref="BusinessException">Cuando no se reciben archivos válidos o hay un error en el proceso.</exception>
        public async Task<List<ImageSelectDto>> AddImagesAsync(int establishmentId, IFormFileCollection files)
        {
            if (files is null || files.Count == 0)
                throw new BusinessException("Debe adjuntar al menos un archivo.");

            var allowedMime = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "image/jpeg", "image/png", "image/webp" };

            var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp" };

            var filesToUpload = files.Take(MaxFilesPerRequest)
                .Where(f => f?.Length > 0)
                .Where(f =>
                {
                    var contentType = f.ContentType ?? string.Empty;
                    var ext = Path.GetExtension(f.FileName) ?? string.Empty;
                    if (!allowedMime.Contains(contentType) || !allowedExt.Contains(ext))
                        throw new BusinessException($"El archivo '{f.FileName}' tiene un formato no permitido. Solo se permiten JPG, PNG o WEBP.");
                    return true;
                })
                .ToList();

            if (filesToUpload.Count == 0)
                throw new BusinessException("No se recibieron archivos válidos.");

            var uploadedEntities = new List<Images>();
            var uploadedPublicIds = new List<string>();
            using var semaphore = new SemaphoreSlim(MaxParallelUploads);

            try
            {
                var tasks = filesToUpload.Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var result = await _cloudinary.UploadImageAsync(file, establishmentId);
                        lock (uploadedEntities)
                        {
                            uploadedEntities.Add(new Images
                            {
                                FileName = file.FileName,
                                FilePath = result.SecureUrl.AbsoluteUri,
                                PublicId = result.PublicId,
                                EstablishmentId = establishmentId
                            });
                            uploadedPublicIds.Add(result.PublicId);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                await _imagesRepository.AddRangeAsync(uploadedEntities);

                _logger.LogInformation("Subidas {Count} imágenes para establecimiento {Id}", uploadedEntities.Count, establishmentId);
                return _mapper.Map<List<ImageSelectDto>>(uploadedEntities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la subida/persistencia de imágenes para establecimiento {Id}. Ejecutando rollback...", establishmentId);

                var deletes = uploadedPublicIds.Select(pid => _cloudinary.DeleteAsync(pid));
                await Task.WhenAll(deletes);

                throw new BusinessException("Error al adjuntar imágenes al establecimiento.", ex);
            }
        }

        /// <summary>
        /// Elimina una imagen identificada por su PublicId tanto en Cloudinary como en la base de datos.
        /// </summary>
        /// <param name="publicId">Identificador público en Cloudinary.</param>
        public async Task DeleteByPublicIdAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new BusinessException("PublicId requerido.");

            await _cloudinary.DeleteAsync(publicId);
            await _imagesRepository.DeleteByPublicIdAsync(publicId);
        }

        /// <summary>
        /// Elimina una imagen por su identificador interno en la base de datos.
        /// También elimina el archivo remoto en Cloudinary.
        /// </summary>
        /// <param name="id">Identificador interno de la imagen.</param>
        public async Task DeleteByIdAsync(int id)
        {
            var img = await _imagesRepository.GetByIdAsync(id);
            if (img is null)
                return; // Idempotente

            await _cloudinary.DeleteAsync(img.PublicId);
            await _imagesRepository.DeleteAsync(id);
        }

        /// <summary>
        /// Obtiene todas las imágenes asociadas a un establecimiento.
        /// </summary>
        /// <param name="establishmentId">Identificador del establecimiento.</param>
        /// <returns>Lista de imágenes relacionadas.</returns>
        public async Task<List<ImageSelectDto>> GetImagesByEstablishmentIdAsync(int establishmentId)
        {
            var images = await _imagesRepository.GetByEstablishmentIdAsync(establishmentId);
            return _mapper.Map<List<ImageSelectDto>>(images);
        }
    }
}
