using Business.Abstract;
using Entities;
using Entities.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
            var result = await _schemaService.AddAsync(addObjectSchemaDTO);
            if (result==true)
                return Ok(result);

            return BadRequest(result);
            
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AddObjectDataDTO addObjectDataDTO)
        {
            var result = await _dataService.AddAsync(addObjectDataDTO);
            if (result == true)
                return Ok(result);
            
            return BadRequest(result);

        }
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] UpdateObjectDataDTO updateObjectDataDTO)
        {
            var result = _dataService.Update(updateObjectDataDTO);
            if (result==true)
                return Ok();
          
            return BadRequest(result);
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
            if (result!=null)
                return Ok(result);

            return BadRequest(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var result = _dataService.GetById(id);
            if (result != null)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("filter")]
        public async Task<IActionResult> FilterData([FromBody] FilteredObjectDTO filteredObjectDTO)
        {
            var filteredData = await _dataService.GetFilteredDataAsync(filteredObjectDTO.ObjectType, filteredObjectDTO.Filters);
            if (filteredData!=null)
                return Ok(filteredData);

            return BadRequest(filteredData);
        }
    }
}


