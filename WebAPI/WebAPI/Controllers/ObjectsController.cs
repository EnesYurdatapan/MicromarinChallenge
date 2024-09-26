using Business.Abstract;
using Entities;
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
        public async Task<IActionResult> CreateObjectSchema([FromBody] ObjectSchema objectSchema)
        {
            await _schemaService.AddAsync(objectSchema);
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ObjectData objectData)
        {
            await _dataService.AddAsync(objectData);
            return Ok();
        }
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] ObjectData objectData)
        {
            _dataService.Update(objectData);
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
        public async Task<IActionResult> FilterData([FromBody] FilterRequest filterRequest)
        {
            var filteredData = await _dataService.GetFilteredDataAsync(filterRequest.ObjectType, filterRequest.Filters);
            return Ok(filteredData);
        }

        // Filtreleme isteği için model sınıfı
        public class FilterRequest
        {
            public string ObjectType { get; set; }  // Product, Order vs.
            public dynamic Filters { get; set; }    // Dinamik filtreler (JSON formatında)
        }
    }
}


