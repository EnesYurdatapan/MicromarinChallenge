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
            var fields = schemaDto.Fields.Select(f => new Field
            {
                FieldName = f.FieldName,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired
            }).ToList();

            var schema = await _schemaService.CreateObjectSchemaAsync(schemaDto.ObjectType, fields);
            return Ok(schema);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject requestData)
        {
            string objectType = requestData["objectType"]?.ToString();
            var fields = requestData["data"]?.ToObject<Dictionary<string, object>>();

            if (string.IsNullOrEmpty(objectType) || fields == null)
            {
                return BadRequest("Invalid request. ObjectType or Data is missing.");
            }

            // Veritabanında tablo olup olmadığını kontrol ediyoruz
            bool tableExists = await _dynamicTableService.TableExistsAsync(objectType);

            if (!tableExists)
            {
                // Eğer tablo daha önce oluşturulmamışsa, tabloyu oluşturuyoruz
                await _dynamicTableService.CreateTableFromSchemaAsync(objectType, fields);
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
    }

}
