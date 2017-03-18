// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AutoRest.Core.Properties;
using AutoRest.Swagger.Validation.Core;
using System.Collections.Generic;
using AutoRest.Swagger.Model;
using AutoRest.Swagger.Model.Utilities;

namespace AutoRest.Swagger.Validation
{
    /// <summary>
    /// Validates the structure of Resource Model marked with x-ms-azure-resource extension then it must contain id,
    /// name, type with readonly: true
    /// </summary>
    public class XmsAzureResourceModelValidation : ResourceModelValidationBase<Dictionary<string, Schema>>
    {
        /// <summary>
        /// Validates resource models for required properties
        /// </summary>
        /// <param name="definitions">Dictionary of definitions</param>
        /// <param name="context">Rule context</param>
        /// <returns>List of validation messages for violating models</returns>
        public override IEnumerable<ValidationMessage> GetValidationMessages(Dictionary<string, Schema> definitions, RuleContext context)
        {
            return ValidateResourceModels(ValidationUtilities.GetXmsAzureResourceModels(definitions), definitions, context);
        }
    }
}
