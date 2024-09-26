using Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business
{
    public interface IValidationService
    {
        void ValidateData(JObject updatedData, JObject schema);
        List<string> ValidateDataAgainstSchema(JObject data, JObject schema);
        List<ObjectData> ValidateSubObjects(JObject data);
    }
}
