﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Rest.Generator.ClientModel;
using Microsoft.Rest.Generator.Ruby.TemplateModels;
using Microsoft.Rest.Generator.Utilities;

namespace Microsoft.Rest.Generator.Ruby
{
    /// <summary>
    /// The model object for regular Ruby methods.
    /// </summary>
    public class MethodTemplateModel : Method
    {
        /// <summary>
        /// Initializes a new instance of the class MethodTemplateModel.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="serviceClient">The service client.</param>
        public MethodTemplateModel(Method source, ServiceClient serviceClient)
        {
            this.LoadFrom(source);
            ParameterTemplateModels = new List<ParameterTemplateModel>();
            source.Parameters.ForEach(p => ParameterTemplateModels.Add(new ParameterTemplateModel(p)));

            LogicalParameterTemplateModels = new List<ParameterTemplateModel>();
            source.LogicalParameters.ForEach(p => LogicalParameterTemplateModels.Add(new ParameterTemplateModel(p)));

            ServiceClient = serviceClient;
        }

        /// <summary>
        /// Gets the return type name for the underlying interface method
        /// </summary>
        public virtual string OperationResponseReturnTypeString
        {
            get
            {
                return "MsRest::HttpOperationResponse";
            }
        }

        /// <summary>
        /// Gets the type for operation exception
        /// </summary>
        public virtual string OperationExceptionTypeString
        {
            get
            {
                return "MsRest::HttpOperationError";
            }
        }

        /// <summary>
        /// Gets the code required to initialize response body.
        /// </summary>
        public virtual string InitializeResponseBody
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets the list of namespaces where we look for classes that need to
        /// be instantiated dynamically due to polymorphism.
        /// </summary>
        public virtual List<string> ClassNamespaces
        {
            get
            {
                return new List<string> { };
            }
        }

        /// <summary>
        /// Gets the path parameters as a Ruby dictionary string
        /// </summary>
        public virtual string PathParamsRbDict
        {
            get
            {
                return ParamsToRubyDict(EncodingPathParams);
            }
        }

        /// <summary>
        /// Gets the skip encoding path parameters as a Ruby dictionary string
        /// </summary>
        public virtual string SkipEncodingPathParamsRbDict
        {
            get
            {
                return ParamsToRubyDict(SkipEncodingPathParams);
            }
        }

        /// <summary>
        /// Gets the query parameters as a Ruby dictionary string
        /// </summary>
        public virtual string QueryParamsRbDict
        {
            get
            {
                return ParamsToRubyDict(EncodingQueryParams);
            }
        }

        /// <summary>
        /// Gets the skip encoding query parameters as a Ruby dictionary string
        /// </summary>
        public virtual string SkipEncodingQueryParamsRbDict
        {
            get
            {
                return ParamsToRubyDict(SkipEncodingQueryParams);
            }
        }

        /// <summary>
        /// Gets the path parameters not including the params that skip encoding
        /// </summary>
        public virtual IEnumerable<ParameterTemplateModel> EncodingPathParams
        {
            get
            {
                return AllPathParams.Where(p => !(p.Extensions.ContainsKey(Generator.Extensions.SkipUrlEncodingExtension) &&
                  String.Equals(p.Extensions[Generator.Extensions.SkipUrlEncodingExtension].ToString(), "true", StringComparison.OrdinalIgnoreCase)));
            }
        }

        /// <summary>
        /// Gets the skip encoding path parameters
        /// </summary>
        public virtual IEnumerable<ParameterTemplateModel> SkipEncodingPathParams
        {
            get
            {
                return AllPathParams.Where(p =>
                    (p.Extensions.ContainsKey(Generator.Extensions.SkipUrlEncodingExtension) &&
                    String.Equals(p.Extensions[Generator.Extensions.SkipUrlEncodingExtension].ToString(), "true", StringComparison.OrdinalIgnoreCase) &&
                    !p.Extensions.ContainsKey("hostParameter")));
            }
        }

        /// <summary>
        /// Gets all path parameters
        /// </summary>
        public virtual IEnumerable<ParameterTemplateModel> AllPathParams
        {
            get { return LogicalParameterTemplateModels.Where(p => p.Location == ParameterLocation.Path); }
        }

        /// <summary>
        /// Gets the skip encoding query parameters
        /// </summary>
        public virtual IEnumerable<ParameterTemplateModel> SkipEncodingQueryParams
        {
            get { return AllQueryParams.Where(p => p.Extensions.ContainsKey(Generator.Extensions.SkipUrlEncodingExtension)); }
        }

        /// <summary>
        /// Gets the query parameters not including the params that skip encoding
        /// </summary>
        public virtual IEnumerable<ParameterTemplateModel> EncodingQueryParams
        {
            get { return AllQueryParams.Where(p => !p.Extensions.ContainsKey(Generator.Extensions.SkipUrlEncodingExtension)); }
        }

        /// <summary>
        /// Gets all of the query parameters
        /// </summary>
        public virtual IEnumerable<ParameterTemplateModel> AllQueryParams
        {
            get
            {
                return LogicalParameterTemplateModels.Where(p => p.Location == ParameterLocation.Query);
            }
        }

        /// <summary>
        /// Gets the list of middelwares required for HTTP requests.
        /// </summary>
        public virtual IList<string> FaradayMiddlewares
        {
            get
            {
                return new List<string>()
                {
                    "[MsRest::RetryPolicyMiddleware, times: 3, retry: 0.02]",
                    "[:cookie_jar]"
                };
            }
        }

        /// <summary>
        /// Gets the expression for default header setting.
        /// </summary>
        public virtual string SetDefaultHeaders
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the reference to the service client object.
        /// </summary>
        public ServiceClient ServiceClient { get; set; }

        /// <summary>
        /// Gets the list of method paramater templates.
        /// </summary>
        public List<ParameterTemplateModel> ParameterTemplateModels { get; private set; }

        private List<ParameterTemplateModel> LogicalParameterTemplateModels { get; set; }

        /// <summary>
        /// Gets the list of parameter which need to be included into HTTP header.
        /// </summary>
        public IEnumerable<Parameter> Headers
        {
            get
            {
                return Parameters.Where(p => p.Location == ParameterLocation.Header);
            }
        }

        /// <summary>
        /// Gets the URL without query parameters.
        /// </summary>
        public string UrlWithoutParameters
        {
            get
            {
                return this.Url.Split('?').First();
            }
        }

        /// <summary>
        /// Get the predicate to determine of the http operation status code indicates success
        /// </summary>
        public string SuccessStatusCodePredicate
        {
            get
            {
                if (Responses.Any())
                {
                    List<string> predicates = new List<string>();
                    foreach (var responseStatus in Responses.Keys)
                    {
                        predicates.Add(string.Format("status_code == {0}", GetStatusCodeReference(responseStatus)));
                    }

                    return string.Join(" || ", predicates);
                }

                return "status_code >= 200 && status_code < 300";
            }
        }

        /// <summary>
        /// Gets the method parameter declaration parameters list.
        /// </summary>
        public string MethodParameterDeclaration
        {
            get
            {
                List<string> declarations = new List<string>();
                foreach (var parameter in LocalParameters.Where(p => !p.IsConstant))
                {
                    string format = "{0}";
                    if (!parameter.IsRequired)
                    {
                        format = "{0} = nil";
                        if (parameter.DefaultValue != null && parameter.Type is PrimaryType)
                        {
                            PrimaryType type = parameter.Type as PrimaryType;
                            if (type != null)
                            {
                                if (type.Type == KnownPrimaryType.Boolean || type.Type == KnownPrimaryType.Double ||
                                    type.Type == KnownPrimaryType.Int || type.Type == KnownPrimaryType.Long || type.Type == KnownPrimaryType.String)
                                {
                                    format = "{0} = " + parameter.DefaultValue;
                                }
                            }
                        }
                    }
                    declarations.Add(string.Format(format, parameter.Name));
                }

                declarations.Add("custom_headers = nil");

                return string.Join(", ", declarations);
            }
        }

        /// <summary>
        /// Gets the method parameter invocation parameters list.
        /// </summary>
        public string MethodParameterInvocation
        {
            get
            {
                var invocationParams = LocalParameters.Where(p => !p.IsConstant).Select(p => p.Name).ToList();
                invocationParams.Add("custom_headers");

                return string.Join(", ", invocationParams);
            }
        }

        /// <summary>
        /// Get the parameters that are actually method parameters in the order they appear in the method signature
        /// exclude global parameters
        /// </summary>
        public IEnumerable<ParameterTemplateModel> LocalParameters
        {
            get
            {
                //Omit parameter group parameters for now since AutoRest-Ruby doesn't support them
                return
                    ParameterTemplateModels.Where(
                        p => p != null && p.ClientProperty == null && !string.IsNullOrWhiteSpace(p.Name))
                        .OrderBy(item => !item.IsRequired);
            }
        }

        /// <summary>
        /// Get the method's request body (or null if there is no request body)
        /// </summary>
        public ParameterTemplateModel RequestBody
        {
            get { return LogicalParameterTemplateModels.FirstOrDefault(p => p.Location == ParameterLocation.Body); }
        }

        /// <summary>
        /// Generate a reference to the ServiceClient
        /// </summary>
        public string UrlReference
        {
            get { return Group == null ? "@base_url" : "@client.base_url"; }
        }

        /// <summary>
        /// Generate a reference to the ServiceClient
        /// </summary>
        public string ClientReference
        {
            get { return Group == null ? "self" : "@client"; }
        }

        /// <summary>
        /// Gets the flag indicating whether URL contains path parameters.
        /// </summary>
        public bool UrlWithPath
        {
            get
            {
                return ParameterTemplateModels.Any(p => p.Location == ParameterLocation.Path);
            }
        }

        /// <summary>
        /// Creates a code in form of string which deserializes given input variable of given type.
        /// </summary>
        /// <param name="inputVariable">The input variable.</param>
        /// <param name="type">The type of input variable.</param>
        /// <param name="outputVariable">The output variable.</param>
        /// <returns>The deserialization string.</returns>
        public virtual string CreateDeserializationString(string inputVariable, IType type, string outputVariable)
        {
            var builder = new IndentedStringBuilder("  ");
            var tempVariable = "parsed_response";

            // Firstly parsing the input json file into temporay variable.
            builder.AppendLine("{0} = {1}.to_s.empty? ? nil : JSON.load({1})", tempVariable, inputVariable);

            // Secondly parse each js object into appropriate Ruby type (DateTime, Byte array, etc.)
            // and overwrite temporary variable variable value.
            string deserializationLogic = GetDeserializationString(type, outputVariable, tempVariable);
            builder.AppendLine(deserializationLogic);

            // Assigning value of temporary variable to the output variable.
            return builder.ToString();
        }

        /// <summary>
        /// Saves url items from the URL into collection.
        /// </summary>
        /// <param name="hashName">The name of the collection save url items to.</param>
        /// <param name="variableName">The URL variable.</param>
        /// <returns>Generated code of saving url items.</returns>
        public virtual string SaveExistingUrlItems(string hashName, string variableName)
        {
            var builder = new IndentedStringBuilder("  ");

            // Saving existing URL properties into properties hash.
            builder
                .AppendLine("unless {0}.query.nil?", variableName)
                .Indent()
                    .AppendLine("{0}.query.split('&').each do |url_item|", variableName)
                    .Indent()
                        .AppendLine("url_items_parts = url_item.split('=')")
                        .AppendLine("{0}[url_items_parts[0]] = url_items_parts[1]", hashName)
                    .Outdent()
                    .AppendLine("end")
                .Outdent()
                .AppendLine("end");

            return builder.ToString();
        }

        /// <summary>
        /// Ensures that there is no duplicate forward slashes in the url.
        /// </summary>
        /// <param name="urlVariableName">The url variable.</param>
        /// <returns>Updated url.</returns>
        public virtual string RemoveDuplicateForwardSlashes(string urlVariableName)
        {
            var builder = new IndentedStringBuilder("  ");

            // Removing duplicate forward slashes.
            builder.AppendLine(@"corrected_url = {0}.to_s.gsub(/([^:])\/\//, '\1/')", urlVariableName);
            builder.AppendLine(@"{0} = URI.parse(corrected_url)", urlVariableName);

            return builder.ToString();
        }

        /// <summary>
        /// Generate code to build the URL from a url expression and method parameters
        /// </summary>
        /// <param name="variableName">The variable to store the url in.</param>
        /// <returns></returns>
        public virtual string BuildUrl(string variableName)
        {
            var builder = new IndentedStringBuilder("  ");
            BuildPathParameters(variableName, builder);

            return builder.ToString();
        }

        /// <summary>
        /// Build parameter mapping from parameter grouping transformation.
        /// </summary>
        /// <returns></returns>
        public virtual string BuildInputParameterMappings()
        {
            var builder = new IndentedStringBuilder("  ");
            if (InputParameterTransformation.Count > 0)
            {
                builder.Indent();
                foreach (var transformation in InputParameterTransformation)
                {
                    if (transformation.OutputParameter.Type is CompositeType &&
                        transformation.OutputParameter.IsRequired)
                    {
                        builder.AppendLine("{0} = {1}.new",
                            transformation.OutputParameter.Name,
                            transformation.OutputParameter.Type.Name);
                    }
                    else
                    {
                        builder.AppendLine("{0} = nil", transformation.OutputParameter.Name);
                    }
                }
                foreach (var transformation in InputParameterTransformation)
                {
                    builder.AppendLine("unless {0}", BuildNullCheckExpression(transformation))
                           .AppendLine().Indent();
                    var outputParameter = transformation.OutputParameter;
                    if (transformation.ParameterMappings.Any(m => !string.IsNullOrEmpty(m.OutputParameterProperty)) &&
                        transformation.OutputParameter.Type is CompositeType)
                    {
                        //required outputParameter is initialized at the time of declaration
                        if (!transformation.OutputParameter.IsRequired)
                        {
                            builder.AppendLine("{0} = {1}.new",
                                transformation.OutputParameter.Name,
                                transformation.OutputParameter.Type.Name);
                        }
                    }

                    foreach (var mapping in transformation.ParameterMappings)
                    {
                        builder.AppendLine("{0}{1}", transformation.OutputParameter.Name, mapping);
                    }

                    builder.Outdent().AppendLine("end");
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Gets the formatted status code.
        /// </summary>
        /// <param name="code">The status code.</param>
        /// <returns>Formatted status code.</returns>
        public string GetStatusCodeReference(HttpStatusCode code)
        {
            return string.Format("{0}", (int)code);
        }

        /// <summary>
        /// Generate code to replace path parameters in the url template with the appropriate values
        /// </summary>
        /// <param name="variableName">The variable name for the url to be constructed</param>
        /// <param name="builder">The string builder for url construction</param>
        protected virtual void BuildPathParameters(string variableName, IndentedStringBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            IEnumerable<Parameter> pathParameters = LogicalParameters.Where(p => p.Extensions.ContainsKey("hostParameter") && p.Location == ParameterLocation.Path);

            foreach (var pathParameter in pathParameters)
            {
                var pathReplaceFormat = "{0} = {0}.gsub('{{{1}}}', {2})";
                var urlPathName = UrlPathNameFromPathPattern(pathParameter.SerializedName);
                builder.AppendLine(pathReplaceFormat, variableName, urlPathName, pathParameter.GetFormattedReferenceValue());
            }
        }

        /// <summary>
        /// Builds the parameters as a Ruby dictionary string
        /// </summary>
        /// <param name="parameters">The enumerable of parameters to be turned into a Ruby dictionary.</param>
        /// <returns>ruby dictionary as a string</returns>
        protected string ParamsToRubyDict(IEnumerable<ParameterTemplateModel> parameters)
        {
            var encodedParameters = new List<string>();
            foreach (var param in parameters)
            {
                string variableName = param.Name;
                string urlPathName = UrlPathNameFromPathPattern(param.SerializedName);
                encodedParameters.Add(string.Format("'{0}' => {1}", urlPathName, param.GetFormattedReferenceValue()));
            }
            return string.Format(CultureInfo.InvariantCulture, "{{{0}}}", string.Join(",", encodedParameters));
        }

        /// <summary>
        /// Builds the url path parameter from the pattern if exists
        /// </summary>
        /// <param name="urlPathParamName">Name of the path parameter to match.</param>
        /// <returns>url path parameter as a string</returns>
        private string UrlPathNameFromPathPattern(string urlPathParamName)
        {
            string pat = @".*\{" + urlPathParamName + @"(\:\w+)\}";
            Regex r = new Regex(pat);
            Match m = r.Match(Url);
            if (m.Success)
            {
                urlPathParamName += m.Groups[1].Value;
            }
            return urlPathParamName;
        }

        /// <summary>
        /// Constructs mapper for the request body.
        /// </summary>
        /// <param name="outputVariable">Name of the output variable.</param>
        /// <returns>Mapper for the request body as string.</returns>
        public string ConstructRequestBodyMapper(string outputVariable = "request_mapper")
        {
            var builder = new IndentedStringBuilder("  ");
            if (RequestBody.Type is CompositeType)
            {
                builder.AppendLine("{0} = {1}.mapper()", outputVariable, RequestBody.Type.Name);
            }
            else
            {
                builder.AppendLine("{0} = {{{1}}}", outputVariable,
                    RequestBody.Type.ConstructMapper(RequestBody.SerializedName, RequestBody, false));
            }
            return builder.ToString();
        }

        /// <summary>
        /// Creates deserialization logic for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Type for which deserialization logic being constructed.</param>
        /// <param name="valueReference">Reference variable name.</param>
        /// <param name="responseVariable">Response variable name.</param>
        /// <returns>Deserialization logic for the given <paramref name="type"/> as string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        public string GetDeserializationString(IType type, string valueReference = "result", string responseVariable = "parsed_response")
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var builder = new IndentedStringBuilder("  ");
            if (type is CompositeType)
            {
                builder.AppendLine("result_mapper = {0}.mapper()", type.Name);
            }
            else
            {
                builder.AppendLine("result_mapper = {{{0}}}", type.ConstructMapper(responseVariable, null, false));
            }
            if (Group == null)
            {
                builder.AppendLine("{1} = self.deserialize(result_mapper, {0}, '{1}')", responseVariable, valueReference);
            }
            else
            {
                builder.AppendLine("{1} = @client.deserialize(result_mapper, {0}, '{1}')", responseVariable, valueReference);
            }
             
            return builder.ToString();
        }

        /// <summary>
        /// Builds null check expression for the given <paramref name="transformation"/>.
        /// </summary>
        /// <param name="transformation">ParameterTransformation for which to build null check expression.</param>
        /// <returns></returns>
        private static string BuildNullCheckExpression(ParameterTransformation transformation)
        {
            if (transformation == null)
            {
                throw new ArgumentNullException("transformation");
            }
            if (transformation.ParameterMappings.Count == 1)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0}.nil?",transformation.ParameterMappings[0].InputParameter.Name);
            }
            else
            {
                return string.Join(" || ",
                transformation.ParameterMappings.Select(m =>
                    string.Format(CultureInfo.InvariantCulture,
                    "({0}.nil?)", m.InputParameter.Name)));
            }
        }
    }
}