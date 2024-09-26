using Business.Abstract;
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
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
            });

            // Alt nesneleri de veritabanına kaydedelim
            foreach (var subObject in subObjectsToAdd)
            {
                await _objectDataWriteRepository.AddAsync(subObject);
            }

            // İşlem başarılıysa true döndürüyoruz
            return masterObjectAdded && subObjectsToAdd.Count == 0;
        }


        public bool Delete(int id)
        {
            return _objectDataWriteRepository.Delete(id);
        }

        public List<ObjectData> GetAll()
        {
            return _objectDataReadRepository.GetAll().ToList();
        }

        public async Task<ObjectData> GetById(int id)
        {
            return await _objectDataReadRepository.GetByIdAsync(id);
        }

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

        public async Task<List<ObjectData>> GetFilteredDataAsync(string objectType, dynamic filters)
        {
            // Veritabanından ObjectData objelerini alıyoruz
            var objectDataList = await _objectDataReadRepository.GetWhere(o => o.ObjectType == objectType).ToListAsync();

            var filteredDataList = new List<ObjectData>();

            foreach (var objectData in objectDataList)
            {
                JObject data = JObject.Parse(objectData.Data.ToString());
                bool match = true;

                // Filtrelerin her birini kontrol et
                foreach (var filter in (JObject)JObject.Parse(filters.ToString())) // String olarak parse ediyoruz
                {
                    string propertyName = filter.Key; // Örneğin "Price" veya "Name"
                    dynamic filterValue = filter.Value; // Filtredeki koşul

                    // Şimdi veride ilgili alanı kontrol ediyoruz
                    if (data[propertyName] != null)
                    {
                        // Örneğin, filterValue bir nesne ise (gt: 150 gibi)
                        if (filterValue is JObject condition)
                        {
                            // Koşulu çözüyoruz (örneğin, gt, lt gibi işlemler burada yapılabilir)
                            foreach (var conditionItem in condition)
                            {
                                var conditionType = conditionItem.Key;
                                var conditionValue = conditionItem.Value;

                                // Örneğin, "gt" koşulunu uygula
                                if (conditionType == "gt" && (double)data[propertyName] <= (double)conditionValue)
                                {
                                    match = false;
                                    break;
                                }
                                if (conditionType == "lt" && (double)data[propertyName] >= (double)conditionValue)
                                {
                                    match = false;
                                    break;
                                }
                                // Diğer koşullar (eq, startsWith vb.)
                                if (conditionType == "startsWith" && !((string)data[propertyName]).StartsWith((string)conditionValue))
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Basit eşleşme koşulu (örneğin, tam eşleşme)
                            if ((string)data[propertyName] != (string)filterValue)
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Eğer veride property yoksa, eşleşmeyi false yapıyoruz
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    filteredDataList.Add(objectData);
                }
            }
            foreach (var item in filteredDataList)
            {
                item.Data = JObject.Parse(item.Data.ToString()); // Data'yı JObject'e çeviriyoruz
            }
            return filteredDataList;
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