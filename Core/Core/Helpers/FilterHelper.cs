using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Helpers
{
    public static class FilterHelper
    {
        public static bool IsMatch(JObject data, JObject filters)
        {
            foreach (var filter in filters)
            {
                string propertyName = filter.Key;
                dynamic filterValue = filter.Value;

                if (data[propertyName] != null)
                {
                    if (filterValue is JObject condition)
                    {
                        if (!CheckConditions(data[propertyName], condition))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!SimpleMatch(data[propertyName], filterValue))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CheckConditions(JToken dataValue, JObject condition)
        {
            foreach (var conditionItem in condition)
            {
                string conditionType = conditionItem.Key;
                dynamic conditionValue = conditionItem.Value;

                if (conditionType == "gt" && (double)dataValue <= (double)conditionValue)
                    return false;

                if (conditionType == "lt" && (double)dataValue >= (double)conditionValue)
                    return false;

                if (conditionType == "startsWith" && !((string)dataValue).StartsWith((string)conditionValue))
                    return false;
            }
            return true;
        }

        private static bool SimpleMatch(JToken dataValue, dynamic filterValue)
        {
            return (string)dataValue == (string)filterValue;
        }
    }

}
