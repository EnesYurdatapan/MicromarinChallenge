using Business.Abstract;
using DataAccess.Abstract.EntityFramework.ObjectDataRepositories;
using DataAccess.Abstract.EntityFramework.ObjectSchemaRepositories;
using Entities;
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
        private readonly IObjectSchemaReadRepository _objectSchemaReadRepository;
        private readonly IObjectSchemaService _objectSchemaService;

        public ObjectDataService(IObjectDataWriteRepository objectDataWriteRepository, IObjectDataReadRepository objectDataReadRepository, IObjectSchemaReadRepository objectSchemaReadRepository, IObjectSchemaService objectSchemaService)
        {
            _objectDataWriteRepository = objectDataWriteRepository;
            _objectDataReadRepository = objectDataReadRepository;
            _objectSchemaReadRepository = objectSchemaReadRepository;
            _objectSchemaService = objectSchemaService;
        }
        //[ServiceFilter(typeof(TransactionInterceptor))]

        public async Task<bool> AddAsync(ObjectData objectData)
        {
            // Ana ObjectSchema'yı alıyoruz
            var objectSchema = _objectSchemaService.GetObjectSchema(objectData.ObjectType);

            if (objectSchema == null)
            {
                throw new Exception("Schema not found for object type.");
            }

            JObject schema = JObject.Parse(objectSchema.Schema.ToString());
            JObject data = JObject.Parse(objectData.Data.ToString());

            // Validasyon hataları için bir liste oluşturuyoruz
            List<string> validationErrors = new List<string>();

            // Ana nesne validasyonu
            validationErrors.AddRange(ValidateDataAgainstSchema(data, schema));

            // Sub-object'leri kontrol edelim
            // Eklemek için sub-object'leri tutacağımız bir liste
            var subObjectsToAdd = ValidateSubObjects(data, validationErrors).Result;
            // Eğer hatalar varsa, bunları fırlatıyoruz
            if (validationErrors.Any())
            {
                throw new Exception(string.Join(", ", validationErrors));
            }

            // Validasyon geçtiyse, ana veriyi kaydediyoruz
            var masterObjectAdded = await _objectDataWriteRepository.AddAsync(objectData);

            // Alt nesneleri de veritabanına kaydedelim
            foreach (var subObject in subObjectsToAdd)
            {
                await _objectDataWriteRepository.AddAsync(subObject);
            }

            // İşlem başarılıysa true döndürüyoruz
            return masterObjectAdded && subObjectsToAdd.Count == 0;
        }




        private async Task<List<ObjectData>> ValidateSubObjects(JObject data, List<string> validationErrors)
        {
            List<ObjectData> subObjectsToAdd = new List<ObjectData>();
            foreach (var property in data.Properties())
            {
                var propertyValue = property.Value;

                if (propertyValue.Type == JTokenType.Array) // Eğer sub-object bir array ise (örneğin Products array'i)
                {
                    var subObjects = (JArray)propertyValue;

                    foreach (var subObject in subObjects)
                    {
                        if (subObject["ObjectType"] != null && subObject["Data"] != null)
                        {
                            // Sub-object'in schema'sını bulalım
                            var subObjectSchema = await _objectSchemaReadRepository
                                .GetWhere(o => o.ObjectType == subObject["ObjectType"].ToString())
                                .FirstOrDefaultAsync();

                            if (subObjectSchema == null)
                            {
                                validationErrors.Add($"Schema not found for sub-object type: {property.Name}");
                                continue;
                            }

                            JObject subSchema = JObject.Parse(subObjectSchema.Schema.ToString());
                            JObject subData = JObject.Parse(subObject["Data"].ToString());

                            // Sub-object validasyonunu yapalım ve hataları listeye ekleyelim
                            validationErrors.AddRange(ValidateDataAgainstSchema(subData, subSchema));

                            // Sub-object verisini kaydetmek için ObjectData'ya ekleyelim
                            var newSubObjectData = new ObjectData
                            {
                                ObjectType = subObject["ObjectType"].ToString(),
                                Data = subObject["Data"].ToString() // Sub-object data'sını kaydediyoruz
                            };
                            subObjectsToAdd.Add(newSubObjectData); // Sub-object'leri eklemek için listeye ekliyoruz
                        }
                    }
                }
                else if (propertyValue.Type == JTokenType.Object) // Eğer sub-object bir nesne ise
                {
                    var subObject = (JObject)propertyValue;

                    if (subObject["ObjectType"] != null && subObject["Data"] != null)
                    {
                        // Sub-object'in schema'sını bulalım
                        var subObjectSchema = await _objectSchemaReadRepository
                            .GetWhere(o => o.ObjectType == subObject["ObjectType"].ToString())
                            .FirstOrDefaultAsync();

                        if (subObjectSchema == null)
                        {
                            validationErrors.Add($"Schema not found for sub-object type: {property.Name}");
                            continue;
                        }

                        JObject subSchema = JObject.Parse(subObjectSchema.Schema.ToString());
                        JObject subData = JObject.Parse(subObject["Data"].ToString());

                        // Sub-object validasyonunu yapalım ve hataları listeye ekleyelim
                        validationErrors.AddRange(ValidateDataAgainstSchema(subData, subSchema));

                        // Sub-object verisini kaydetmek için ObjectData'ya ekleyelim
                        var newSubObjectData = new ObjectData
                        {
                            ObjectType = subObject["ObjectType"].ToString(),
                            Data = subObject["Data"].ToString() // Sub-object data'sını kaydediyoruz
                        };
                        subObjectsToAdd.Add(newSubObjectData); // Sub-object'leri eklemek için listeye ekliyoruz
                    }
                }
            }
            return subObjectsToAdd;
        }




        public async Task<bool> AddRangeAsync(List<ObjectData> objectData)
        {
            return await _objectDataWriteRepository.AddRangeAsync(objectData);
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

        public bool Update(ObjectData updatedObjectData)
        {
            var existingObjectData = GetExistingObjectData(updatedObjectData.Id);
            JObject objectSchema = GetObjectSchema(updatedObjectData.ObjectType);
            JObject updatedData = ParseDynamicData(updatedObjectData.Data);

            ValidateData(updatedData, objectSchema);

            UpdateSubObjects(updatedData);
            UpdateExistingObjectData(existingObjectData, updatedObjectData.Data);

            return true;
        }

        private ObjectData GetExistingObjectData(int id)
        {
            var existingObjectData = _objectDataReadRepository
                .GetWhere(o => o.Id == id)
                .FirstOrDefault();

            if (existingObjectData == null)
            {
                throw new Exception("Object not found.");
            }

            return existingObjectData;
        }

        private JObject GetObjectSchema(string objectType)
        {
            var objectSchema = _objectSchemaReadRepository
                .GetWhere(o => o.ObjectType == objectType)
                .FirstOrDefault();

            if (objectSchema == null)
            {
                throw new Exception("Schema not found.");
            }

            return JObject.Parse(objectSchema.Schema.ToString());
        }

        private JObject ParseDynamicData(dynamic data)
        {
            // Eğer data bir JObject ise, direkt olarak dönebiliriz.
            if (data is JObject jObject)
            {
                return jObject;
            }
            // Eğer string ise, JSON olarak parse edelim
            else if (data is string jsonString)
            {
                return JObject.Parse(jsonString);
            }

            throw new InvalidOperationException("Invalid data format.");
        }

        private void ValidateData(JObject updatedData, JObject schema)
        {
            var validationErrors = ValidateDataAgainstSchema(updatedData, schema);
            if (validationErrors.Any())
            {
                throw new Exception(string.Join(", ", validationErrors));
            }
        }

        private void UpdateSubObjects(JObject updatedData)
        {
            foreach (var property in updatedData.Properties())
            {
                if (IsSubObject(property.Value))
                {
                    var subObjectData = property.Value.ToObject<ObjectData>();
                    UpdateOrCreateSubObject(subObjectData);
                }
            }
        }

        private void UpdateOrCreateSubObject(ObjectData subObjectData)
        {
            var existingSubObject = _objectDataReadRepository
                .GetWhere(o => o.ObjectType == subObjectData.ObjectType && o.Id == subObjectData.Id)
                .FirstOrDefault();

            if (existingSubObject != null)
            {
                existingSubObject.Data = subObjectData.Data;
                _objectDataWriteRepository.UpdateAsync(existingSubObject);
            }
            else
            {
                // Yeni nesneyi ekle
                _objectDataWriteRepository.AddAsync(subObjectData);
            }
        }

        private void UpdateExistingObjectData(ObjectData existingObjectData, dynamic newData)
        {
            existingObjectData.Data = newData; // Burada Data'nın türü dynamic
            _objectDataWriteRepository.UpdateAsync(existingObjectData);
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
        private List<string> ValidateDataAgainstSchema(JObject data, JObject schema)
        {
            List<string> errors = new List<string>();

            // 1. Required alanları kontrol et
            ValidateRequiredFields(data, schema, errors);

            // 2. Property'lere göre validasyon yap
            ValidateProperties(data, schema, errors);

            return errors;
        }

        private void ValidateRequiredFields(JObject data, JObject schema, List<string> errors)
        {
            if (schema["required"] == null) return;

            JArray requiredFields = (JArray)schema["required"];
            foreach (var field in requiredFields)
            {
                string fieldName = field.ToString();
                if (!data.ContainsKey(fieldName))
                {
                    errors.Add($"{fieldName} is required.");
                }
            }
        }

        private void ValidateProperties(JObject data, JObject schema, List<string> errors)
        {
            if (schema["properties"] == null) return;

            JObject properties = (JObject)schema["properties"];
            foreach (var property in properties)
            {
                string propertyName = property.Key;
                JObject propertyRules = (JObject)property.Value;

                if (data.ContainsKey(propertyName))
                {
                    var fieldValue = data[propertyName];

                    ValidateType(propertyName, fieldValue, propertyRules, errors);
                    ValidateMaxLength(propertyName, fieldValue, propertyRules, errors);
                    ValidateMinLength(propertyName, fieldValue, propertyRules, errors);
                }
            }
        }

        private void ValidateType(string propertyName, JToken fieldValue, JObject propertyRules, List<string> errors)
        {
            if (!propertyRules.ContainsKey("type")) return;

            string expectedType = propertyRules["type"].ToString();
            if (!IsTypeValid(fieldValue, expectedType))
            {
                errors.Add($"{propertyName} must be of type {expectedType}.");
            }
        }

        private void ValidateMaxLength(string propertyName, JToken fieldValue, JObject propertyRules, List<string> errors)
        {
            if (!propertyRules.ContainsKey("maxLength")) return;

            int maxLength = (int)propertyRules["maxLength"];
            if (fieldValue.Type == JTokenType.String && fieldValue.ToString().Length > maxLength)
            {
                errors.Add($"{propertyName} exceeds maximum length of {maxLength}.");
            }
        }

        private void ValidateMinLength(string propertyName, JToken fieldValue, JObject propertyRules, List<string> errors)
        {
            if (!propertyRules.ContainsKey("minLength")) return;

            int minLength = (int)propertyRules["minLength"];
            if (fieldValue.Type == JTokenType.String && fieldValue.ToString().Length < minLength)
            {
                errors.Add($"{propertyName} is shorter than minimum length of {minLength}.");
            }
        }

        private bool IsTypeValid(JToken fieldValue, string expectedType)
        {
            return expectedType switch
            {
                "string" => fieldValue.Type == JTokenType.String,
                "number" => fieldValue.Type == JTokenType.Float || fieldValue.Type == JTokenType.Integer,
                "boolean" => fieldValue.Type == JTokenType.Boolean,
                "object" => fieldValue.Type == JTokenType.Object,
                "array" => fieldValue.Type == JTokenType.Array,
                _ => false
            };
        }
    }
}