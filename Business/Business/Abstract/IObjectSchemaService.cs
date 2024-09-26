using Business.Concrete;
using Entities;
using Entities.DTOs;
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
        Task<bool> AddAsync(AddObjectSchemaDTO addObjectSchemaDTO);
        Task<bool> AddRangeAsync(List<ObjectSchema> objectSchema);
        bool Update(ObjectSchema objectSchema);
        bool Delete(int id);
        Task<ObjectSchema> GetById(int id);
        List<ObjectSchema> GetAll();
        JObject GetObjectSchema(string objectType);
    }
}
