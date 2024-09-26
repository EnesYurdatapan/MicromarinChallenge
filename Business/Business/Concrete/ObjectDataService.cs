using Business.Abstract;
using Core.Helpers;
using DataAccess.Abstract.EntityFramework.ObjectDataRepositories;
using DataAccess.Abstract.EntityFramework.ObjectSchemaRepositories;
using Entities;
using Entities.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Business.Concrete
{
    public class ObjectDataService : IObjectDataService
    {
        private readonly IObjectDataWriteRepository _objectDataWriteRepository;
        private readonly IObjectDataReadRepository _objectDataReadRepository;
        private readonly IObjectSchemaService _objectSchemaService;
        private readonly IValidationService _validationService;

        public ObjectDataService(IObjectDataWriteRepository objectDataWriteRepository, IObjectDataReadRepository objectDataReadRepository, IObjectSchemaService objectSchemaService, IValidationService validationService)
        {
            _objectDataWriteRepository = objectDataWriteRepository;
            _objectDataReadRepository = objectDataReadRepository;
            _objectSchemaService = objectSchemaService;
            _validationService = validationService;
        }

        [ServiceFilter(typeof(TransactionInterceptor))]
        public async Task<bool> AddAsync(AddObjectDataDTO addObjectDataDTO)
        {
            // Ana ObjectSchema'yı alıyoruz
            var objectSchema = _objectSchemaService.GetObjectSchema(addObjectDataDTO.ObjectType);

            if (objectSchema == null)
            {
                throw new Exception("Schema not found for object type.");
            }

            //JObject schema = JObject.Parse(objectSchema.Schema.ToString());
            JObject data = JObject.Parse(addObjectDataDTO.Data.ToString());

            // Validasyon hataları için bir liste oluşturuyoruz
            List<string> validationErrors = new List<string>();

            // Ana nesne validasyonu
            validationErrors.AddRange(_validationService.ValidateDataAgainstSchema(data, JObject.Parse(objectSchema.ToString())));

            // Sub-object'leri kontrol edelim
            var subObjectsToAdd = _validationService.ValidateSubObjects(data);
            // Eğer hatalar varsa, bunları fırlatıyoruz
            if (validationErrors.Any())
            {
                throw new Exception(string.Join(", ", validationErrors));
            }

            // Validasyon geçtiyse, ana veriyi kaydediyoruz
            var masterObjectAdded = await _objectDataWriteRepository.AddAsync(new ObjectData()
            {
                ObjectType = addObjectDataDTO.ObjectType,
                Data = addObjectDataDTO.Data,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
            });

            // Alt nesneleri de veritabanına kaydedelim
            foreach (var subObject in subObjectsToAdd)
            {
                await _objectDataWriteRepository.AddAsync(subObject);
            }

            // İşlem başarılıysa true döndürüyoruz
            return masterObjectAdded && subObjectsToAdd.Count == 0;
        }

        [ServiceFilter(typeof(TransactionInterceptor))]
        public void Delete(int id)
        {
            // 1. Ana ObjectData nesnesini bul
            var mainObjectData = _objectDataReadRepository.GetWhere(o => o.Id == id).FirstOrDefault();

            if (mainObjectData == null)
            {
                throw new Exception("Object not found.");
            }

            // 2. Alt nesneleri bul ve sil
            DeleteSubObjects(mainObjectData);

            // 3. Ana nesneyi sil
            _objectDataWriteRepository.Delete(mainObjectData.Id);
        }

        [ServiceFilter(typeof(TransactionInterceptor))]
        public bool Update(UpdateObjectDataDTO updateObjectDataDTO)
        {
            var existingObjectData = _objectDataReadRepository
                .GetWhere(o => o.Id == updateObjectDataDTO.Id)
                .FirstOrDefault();

            if (existingObjectData == null)
            {
                throw new Exception("Object not found.");
            }
            JObject objectSchema = _objectSchemaService.GetObjectSchema(updateObjectDataDTO.ObjectType);
            JObject updatedData = JObject.Parse(updateObjectDataDTO.Data.ToString());
            //ParseDynamicData(updatedObjectData.Data);

            _validationService.ValidateData(updatedData, objectSchema);


            _validationService.ValidateSubObjects(updatedData);

            foreach (var property in updatedData.Properties())
            {
                if (IsSubObject(property.Value))
                {
                    var subObjectData = property.Value.ToObject<ObjectData>();
                    UpdateOrCreateSubObject(updateObjectDataDTO);
                }
            }

            existingObjectData.Data = updatedData;
            _objectDataWriteRepository.UpdateAsync(new ObjectData()
            {
                ObjectType = updateObjectDataDTO.ObjectType,
                Data = updateObjectDataDTO.Data,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
            });

            return true;
        }

        public List<ObjectData> GetAll()
        {
            return _objectDataReadRepository.GetAll().ToList();
        }

        public async Task<ObjectData> GetById(int id)
        {
            return await _objectDataReadRepository.GetByIdAsync(id);
        }

        public async Task<List<ObjectData>> GetFilteredDataAsync(string objectType, dynamic filters)
        {
            var objectDataList = await _objectDataReadRepository
                .GetWhere(o => o.ObjectType == objectType)
                .ToListAsync();

            var filteredDataList = new List<ObjectData>();
            JObject parsedFilters = JObject.Parse(filters.ToString());

            foreach (var objectData in objectDataList)
            {
                JObject data = JObject.Parse(objectData.Data.ToString());

                if (FilterHelper.IsMatch(data, parsedFilters))
                {
                    filteredDataList.Add(objectData);
                }
            }

            return filteredDataList;
        }

        private void DeleteSubObjects(ObjectData mainObjectData)
        {
            // Ana veriyi dynamic olarak parse ediyoruz
            JObject mainData = JObject.Parse(mainObjectData.Data.ToString());

            // Alt nesneleri JSON içinden alalım ve silelim
            foreach (var property in mainData.Properties())
            {
                // Alt nesne array ise (örneğin birden fazla alt nesne mevcut)
                if (property.Value.Type == JTokenType.Array)
                {
                    var subObjects = (JArray)property.Value;

                    foreach (var subObject in subObjects)
                    {
                        DeleteSingleSubObject(JObject.Parse(subObject.ToString()));
                    }
                }
                // Tek bir alt nesne ise
                else if (property.Value.Type == JTokenType.Object)
                {
                    var subObject = (JObject)property.Value;
                    DeleteSingleSubObject(subObject);
                }
            }
        }

        private void DeleteSingleSubObject(JObject subObject)
        {
            var subObjectId = subObject["Id"]?.Value<int>();

            if (subObjectId.HasValue)
            {
                // Alt nesneyi veritabanından bul ve sil
                var subObjectData = _objectDataReadRepository.GetWhere(o => o.Id == subObjectId.Value).FirstOrDefault();

                if (subObjectData != null)
                {
                    // Silme işlemini gerçekleştir
                    _objectDataWriteRepository.Delete(subObjectData.Id);
                }
            }
        }

        private void UpdateOrCreateSubObject(UpdateObjectDataDTO updateObjectDataDTO)
        {
            var existingSubObject = _objectDataReadRepository
                .GetWhere(o => o.ObjectType == updateObjectDataDTO.ObjectType && o.Id == updateObjectDataDTO.Id)
                .FirstOrDefault();

            if (existingSubObject != null)
            {
                existingSubObject.Data = updateObjectDataDTO.Data;
                _objectDataWriteRepository.UpdateAsync(new ObjectData()
                {
                    ObjectType = updateObjectDataDTO.ObjectType,
                    Data = updateObjectDataDTO.Data,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                });
            }
            else
            {
                // Yeni nesneyi ekle
                _objectDataWriteRepository.AddAsync(new ObjectData()
                {
                    ObjectType = updateObjectDataDTO.ObjectType,
                    Data = updateObjectDataDTO.Data,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                });
            }
        }

        private bool IsSubObject(JToken token)
        {
            // Alt nesne, ObjectType ve Data içermelidir
            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;
                return obj["ObjectType"] != null && obj["Data"] != null;
            }
            return false;
        }

    }
}