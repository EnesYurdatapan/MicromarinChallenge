using Business.Abstract;
using Entities;
using Entities.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectsController : ControllerBase
    {
        private readonly IObjectSchemaService _schemaService;
        private readonly IObjectDataService _dataService;

        public ObjectsController(IObjectSchemaService schemaService, IObjectDataService dataService)
        {
            _schemaService = schemaService;
            _dataService = dataService;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateObjectSchema([FromBody] AddObjectSchemaDTO addObjectSchemaDTO)
        {
            await _schemaService.AddAsync(addObjectSchemaDTO);
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AddObjectDataDTO addObjectDataDTO)
        {
            await _dataService.AddAsync(addObjectDataDTO);
            return Ok();
        }
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] UpdateObjectDataDTO updateObjectDataDTO)
        {
            _dataService.Update(updateObjectDataDTO);
            return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            _dataService.Delete(id);
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = _dataService.GetAll();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            _dataService.GetById(id);
            return Ok();
        }

        [HttpPost("filter")]
        public async Task<IActionResult> FilterData([FromBody] FilteredObjectDTO filteredObjectDTO)
        {
            var filteredData = await _dataService.GetFilteredDataAsync(filteredObjectDTO.ObjectType, filteredObjectDTO.Filters);
            return Ok(filteredData);
        }

        // Filtreleme isteği için model sınıfı
    
    }
}


