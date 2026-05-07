using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ImageFilterDto filter)
        {
            var result = await _imageService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var image = await _imageService.GetByIdAsync(id);

            if (image == null)
                return NotFound("Imagen no encontrada");

            return Ok(image);
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, [FromForm] List<string>? names)
        {
            if (files == null || !files.Any())
                return BadRequest("Debe seleccionar al menos un archivo");

            var filesToUpload = new List<(byte[] FileContent, string FileName, string Name)>();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (file.Length == 0)
                    continue;

                using var ms = new System.IO.MemoryStream();
                await file.CopyToAsync(ms);
                
                // Nombre por archivo (coincide por indice)
                var customName = names != null && names.Count > i && !string.IsNullOrEmpty(names[i]) 
                    ? names[i] 
                    : file.FileName;

                filesToUpload.Add((ms.ToArray(), file.FileName, customName));
            }

            var result = await _imageService.UploadAsync(filesToUpload);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _imageService.DeleteAsync(id);

            if (!success)
                return NotFound("Imagen no encontrada");

            return Ok(new { Message = "Imagen eliminada correctamente" });
        }
    }
}