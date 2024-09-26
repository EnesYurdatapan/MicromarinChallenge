using DataAccess.Abstract.EntityFramework.ObjectSchemaRepositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business
{
    public class ValidationService:IValidationService
    {
        private readonly IObjectSchemaReadRepository _objectSchemaReadRepository;

        public ValidationService(IObjectSchemaReadRepository objectSchemaReadRepository)
        {
            _objectSchemaReadRepository = objectSchemaReadRepository;
        }

        public void ValidateData(JObject updatedData, JObject schema)
        {
            var validationErrors = ValidateDataAgainstSchema(updatedData, schema);
            if (validationErrors.Any())
            {
                throw new Exception(string.Join(", ", validationErrors));
            }
        }

        public List<string> ValidateDataAgainstSchema(JObject data, JObject schema)
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

        public List<ObjectData> ValidateSubObjects(JObject data)
        {
            List<ObjectData> subObjectsToAdd = new List<ObjectData>();
            List<string> validationErrors = new List<string>();
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
                            var subObjectSchema =  _objectSchemaReadRepository
                                .GetWhere(o => o.ObjectType == subObject["ObjectType"].ToString())
                                .FirstOrDefault();

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
                        var subObjectSchema = _objectSchemaReadRepository
                            .GetWhere(o => o.ObjectType == subObject["ObjectType"].ToString())
                            .FirstOrDefault();

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
            if (validationErrors.Any())
            {
                throw new Exception(string.Join(", ", validationErrors));
            }
            return subObjectsToAdd;
        }
    }

}
