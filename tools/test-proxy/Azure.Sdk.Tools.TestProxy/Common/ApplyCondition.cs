﻿using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Azure.Sdk.Tools.TestProxy.Common
{
    public class ApplyCondition
    {
        private Regex _regex;
        public string UriRegex { 
            get
            {
                return _regex.ToString();
            }
            set
            {
                _regex = new Regex(value);
            }
        }

        private JsonProperty GetProp(string name, JsonElement jsonElement)
        {
            return jsonElement.EnumerateObject()
                        .FirstOrDefault(p => string.Compare(p.Name, "UriRegex", StringComparison.OrdinalIgnoreCase) == 0);
        }

        public ApplyCondition() { }

        /// <summary>
        /// This constructor is used to abstract the creation of an ApplyCondition from API input.
        /// This is a separate function to allow context-sensitive setting. EG, if setting a condition,
        /// one of the trigger properties should be populated! This is a bit immaterial when only dealing with 
        /// a single property, but we might as well start this way.
        /// </summary>
        /// <param name="jsonElement">The contents of "condition" key with the body of a request to /Admin/AddSanitizer or /Admin/AddTransform.</param>
        public ApplyCondition(JsonElement jsonElement)
        {
            var conditionDefined = false;
            try
            {
                var uriProp = GetProp("UriRegex", jsonElement);

                if(uriProp.Value.ValueKind != JsonValueKind.Undefined)
                {
                    UriRegex = uriProp.Value.GetString();
                    conditionDefined = true;
                }

                // ... body/header condition support goes here.
            }
            catch(Exception e)
            {
                throw new HttpException(
                    HttpStatusCode.BadRequest,
                    $"An unexpected error occured during parse of condition body. Condition Definition: {jsonElement.GetRawText()}. Exception detail: {e.Message}."
                );
            }
            
            if (!conditionDefined)
            {
                throw new HttpException(
                    HttpStatusCode.BadRequest, 
                    $"This request defined a condition. The definition of said condition is invalid. At least one trigger regex must be present. Condition Definition: {jsonElement.GetRawText()}."
                );
            }
        }

        // sanitizers run against RecordEntries
        public bool IsApplicable(RecordEntry entry)
        {
            var result = _regex.Match(entry.RequestUri);
            return result.Success;
        }

        // transforms are used directly on the request
        public bool IsApplicable(HttpRequest request)
        {
            var requestString = request.HttpContext.Features.Get<IHttpRequestFeature>().RawTarget;
            var requestUri = request.Scheme + "://" + request.Host + requestString;

            var result = _regex.Match(requestUri);
            return result.Success;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<pre>{");

            sb.Append($"\n   \"UriRegex\": \"{UriRegex}\",");
            // ... body/header condition support goes here.

            sb.Append("\n}</pre>");
            return sb.ToString();
        }
    }
}