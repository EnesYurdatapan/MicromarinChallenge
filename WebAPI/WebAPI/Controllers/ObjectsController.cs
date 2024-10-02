using Business.Abstract;
using Business.Concrete;
using Entities;
using Entities.DTOs;
using Entities.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectsController : ControllerBase
    {
        private readonly IObjectSchemaService _schemaService;
        private readonly IDynamicTableService _dynamicTableService;

        public ObjectsController(IObjectSchemaService schemaService, IDynamicTableService dynamicTableService)
        {
            _schemaService = schemaService;
            _dynamicTableService = dynamicTableService;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateObjectSchema([FromBody] AddObjectSchemaDTO? schemaDto)
        {
            if (string.IsNullOrEmpty(schemaDto.ObjectType) || schemaDto.Fields == null || schemaDto.Fields.Count == 0)
            {
                return BadRequest("Invalid request. ObjectType or Fields are missing.");
            }

            // ObjectSchema'yı oluştur
            var schema = await _schemaService.CreateObjectSchemaAsync(schemaDto.ObjectType, schemaDto.Fields);


            return Ok(schema);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject requestData)
        {
            string objectType = requestData["objectType"]?.ToString();
            var fields = requestData["fields"]?.ToObject<Dictionary<string, object>>();

            if (string.IsNullOrEmpty(objectType) || fields == null)
            {
                return BadRequest("Invalid request. ObjectType or Data is missing.");
            }

            // Veriyi ilgili tabloya ekliyoruz
            await _dynamicTableService.InsertDataAsync(objectType, fields);

            return Ok("Data inserted successfully.");
        }

        [HttpGet("read/{objectType}/{id}")]
        public async Task<IActionResult> Read(string objectType, int id)
        {
            try
            {
                var result = await _dynamicTableService.GetDataById(objectType, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error fetching data: {ex.Message}");
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] JObject requestData)
        {
            try
            {
                string objectType = requestData["ObjectType"].ToString();
                int id = (int)requestData["Id"];
                var fields = requestData["Fields"].ToObject<Dictionary<string, object>>();

                // Tabloya update işlemi yap
                await _dynamicTableService.UpdateData(objectType, id, fields);
                return Ok("Data updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating data: {ex.Message}");
            }
        }

        [HttpDelete("delete/{objectType}/{id}")]
        public async Task<IActionResult> Delete(string objectType, int id)
        {
            try
            {
                // Tabloya delete işlemi yap
                await _dynamicTableService.DeleteData(objectType, id);
                return Ok("Data deleted successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting data: {ex.Message}");
            }
        }

        [HttpGet("{objectType}/{id?}")]
        public async Task<IActionResult> GetObjectByTypeAndFilters([FromRoute] string objectType, [FromRoute] int? id, [FromQuery] Dictionary<string, string> filters)
        {
            try
            {
                var result = await _dynamicTableService.GetObjectsByTypeAndFiltersAsync(objectType, id, filters);

                if (result == null || !result.Any())
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

}
