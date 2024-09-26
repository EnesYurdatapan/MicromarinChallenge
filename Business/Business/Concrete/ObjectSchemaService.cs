using Business.Abstract;
using DataAccess.Abstract.EntityFramework.ObjectDataRepositories;
using DataAccess.Abstract.EntityFramework.ObjectSchemaRepositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Concrete
{
    public class ObjectSchemaService : IObjectSchemaService
    {
        private readonly IObjectSchemaWriteRepository _objectSchemaWriteRepository;
        private readonly IObjectSchemaReadRepository _objectSchemaReadRepository;

        public ObjectSchemaService(IObjectSchemaWriteRepository objectSchemaWriteRepository, IObjectSchemaReadRepository objectSchemaReadRepository)
        {
            _objectSchemaWriteRepository = objectSchemaWriteRepository;
            _objectSchemaReadRepository = objectSchemaReadRepository;
        }

        public async Task<bool> AddAsync(ObjectSchema objectSchema)
        {
           return await _objectSchemaWriteRepository.AddAsync(objectSchema);
        }

        public async Task<bool> AddRangeAsync(List<ObjectSchema> objectSchema)
        {
            return await _objectSchemaWriteRepository.AddRangeAsync(objectSchema);
        }

        public bool Delete(int id)
        {
            return _objectSchemaWriteRepository.Delete(id);
        }

        public List<ObjectSchema> GetAll()
        {
            return _objectSchemaReadRepository.GetAll().ToList();
        }

        public async Task<ObjectSchema> GetById(int id)
        {
            return await _objectSchemaReadRepository.GetByIdAsync(id);
        }

        public bool Update(ObjectSchema objectSchema)
        {
            return _objectSchemaWriteRepository.UpdateAsync(objectSchema);
        }
        public ObjectSchema GetObjectSchema(string objectType)
        {
            var objectSchema =  _objectSchemaReadRepository.GetWhere(o => o.ObjectType == objectType).FirstOrDefault();
            if (objectSchema == null)
            {
                throw new Exception("Schema not found.");
            }
            return objectSchema;
        }
    }
}
