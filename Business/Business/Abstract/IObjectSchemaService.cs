using Business.Concrete;
using Entities;
using Entities.DTOs;
using Entities.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface IObjectSchemaService
    {
        Task<ObjectSchema> CreateObjectSchemaAsync(string objectType, IList<Field> fields);
        Task<ObjectSchema> GetObjectSchemaAsync(int id);
        Task<List<ObjectSchema>> GetAllObjectSchemasAsync();
        Task UpdateObjectSchemaAsync(ObjectSchema objectSchema);
        Task DeleteObjectSchemaAsync(int id);
    }
}
