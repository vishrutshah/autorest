﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AutoRest.Core.Properties;
using AutoRest.Swagger.Validation.Core;
using System.Collections.Generic;
using AutoRest.Swagger.Model;

namespace AutoRest.Swagger.Validation
{
    /// <summary>
    /// Validates the structure of all Resource Model that it must contain id,
    /// name, type with readonly: true
    /// </summary>
    public class ResourceModelValidation: ResourceModelValidationBase<Dictionary<string, Schema>>
    {
        /// <summary>
        /// Validates resource models for required properties
        /// </summary>
        /// <param name="definitions">Dictionary of definitions</param>
        /// <param name="context">Rule context</param>
        /// <returns>List of validation messages for violating models</returns>
        public override IEnumerable<ValidationMessage> GetValidationMessages(Dictionary<string, Schema> definitions, RuleContext context)
        {
            return ValidateResourceModels(context.ResourceModels, definitions, context);
        }
    }
}
