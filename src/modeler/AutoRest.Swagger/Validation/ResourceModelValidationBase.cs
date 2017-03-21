// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AutoRest.Core.Logging;
using AutoRest.Core.Properties;
using AutoRest.Swagger.Model;
using AutoRest.Swagger.Model.Utilities;
using AutoRest.Swagger.Validation.Core;
using System.Collections.Generic;

namespace AutoRest.Swagger.Validation
{
    /// <summary>
    /// Validates the structure of Resource Model that it must contain id,
    /// name, type with readonly: true
    /// </summary>
    public abstract class ResourceModelValidationBase<T>: TypedRule<T>
    {
        protected static readonly List<string> RequiredProperties = new List<string> { "id", "name", "type" };

        /// <summary>
        /// Id of the Rule.
        /// </summary>
        public override string Id => "M3001";

        /// <summary>
        /// Violation category of the Rule.
        /// </summary>
        public override ValidationCategory ValidationCategory => ValidationCategory.RPCViolation;

        /// <summary>
        /// The template message for this Rule. 
        /// </summary>
        /// <remarks>
        /// This may contain placeholders '{0}' for parameterized messages.
        /// </remarks>
        public override string MessageTemplate => Resources.ResourceModelIsNotValid;

        /// <summary>
        /// The severity of this message (ie, debug/info/warning/error/fatal, etc)
        /// </summary>
        public override Category Severity => Category.Error;

        /// <summary>
        /// Validates resource models for requirements of readOnly: true for id, name and type properties
        /// </summary>
        /// <param name="resourceModels">models to validate</param>
        /// <param name="definitions">Dictionary of definitions</param>
        /// <param name="context">Rule context</param>
        /// <returns></returns>
        protected IEnumerable<ValidationMessage> ValidateResourceModels(IEnumerable<string> resourceModels, Dictionary<string, Schema> definitions, RuleContext context)
        {
            foreach (string resourceModel in resourceModels)
            {
                if (!ValidationUtilities.ContainsReadOnlyProperties(resourceModel, definitions, RequiredProperties))
                {
                    yield return new ValidationMessage(new FileObjectPath(context.File, context.Path), this, resourceModel);
                }
            }
        }
    }
}
