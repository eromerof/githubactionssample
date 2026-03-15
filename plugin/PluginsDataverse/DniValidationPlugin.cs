using System;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

namespace PluginsDataverse
{
    public class DniValidationPlugin : IPlugin
    {
        private const string DniLetters = "TRWAGMYFPDXBNJZSQVHLCKE";

        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context == null) return;

            // Only run on Create PreValidation for the target entity
            if (!string.Equals(context.MessageName, "Create", StringComparison.OrdinalIgnoreCase)) return;
            if (!string.Equals(context.PrimaryEntityName, "erf_tablasparaexportar", StringComparison.OrdinalIgnoreCase)) return;
            // Pre-validation stage is 10
            if (context.Stage != 10) return;

            if (!context.InputParameters.Contains("Target")) return;

            var target = context.InputParameters["Target"] as Entity;
            if (target == null) return;

            // Only validate if attribute exists on the target
            if (!target.Attributes.Contains("erf_dni"))
            {
                throw new InvalidPluginExecutionException("El campo DNI es obligatorio.");
            }

            var dniValue = target.GetAttributeValue<string>("erf_dni");
            ValidateDni(dniValue);
        }

        // Public static for easier unit testing
        public static void ValidateDni(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                throw new InvalidPluginExecutionException("El campo DNI es obligatorio.");
            }

            dni = dni.Trim().ToUpperInvariant();

            if (!Regex.IsMatch(dni, "^\\d{8}[A-Z]$"))
            {
                throw new InvalidPluginExecutionException("El formato del DNI no es válido. Debe contener 8 dígitos seguidos de una letra.");
            }

            var numberPart = dni.Substring(0, 8);
            var letterPart = dni[8];

            if (!int.TryParse(numberPart, out var number))
            {
                throw new InvalidPluginExecutionException("El formato del DNI no es válido. Debe contener 8 dígitos seguidos de una letra.");
            }

            var expectedLetter = DniLetters[number % 23];
            if (expectedLetter != letterPart)
            {
                throw new InvalidPluginExecutionException("La letra del DNI no es correcta.");
            }
        }
    }
}
