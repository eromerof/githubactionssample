using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Enums;
using FakeXrmEasy.Middleware;
using FakeXrmEasy.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using System;

namespace PluginsDataverse.FakeXrmTests
{
    /// <summary>
    /// Tests de integración del plugin usando FakeXrmEasy.
    /// A diferencia de PluginsDataverse.Tests (que llama ValidateDni directamente),
    /// estos tests ejecutan el plugin completo a través del contexto simulado de Dataverse,
    /// incluyendo la comprobación de mensaje, entidad y stage.
    /// </summary>
    [TestClass]
    public class DniValidationPluginFakeTests
    {
        private IXrmFakedContext _context = null!;

        [TestInitialize]
        public void Init()
        {
            _context = MiddlewareBuilder
                .New()
                .SetLicense(FakeXrmEasyLicense.RPL_1_5)
                .Build();
        }

        private XrmFakedPluginExecutionContext CreatePluginContext(Entity target, string messageName = "Create", int stage = 10, string entityName = "erf_tablasparaexportar")
        {
            return new XrmFakedPluginExecutionContext
            {
                MessageName = messageName,
                Stage = stage,
                PrimaryEntityName = entityName,
                InputParameters = new ParameterCollection { { "Target", target } }
            };
        }

        [TestMethod]
        public void Execute_DNIValido_NoLanzaExcepcion()
        {
            var target = new Entity("erf_tablasparaexportar") { Id = Guid.NewGuid() };
            target["erf_name"] = "test 1";
            target["erf_dni"] = "12345678Z";

            _context.ExecutePluginWith<DniValidationPlugin>(CreatePluginContext(target));
        }

        [TestMethod]
        public void Execute_SinAtributoDNI_LanzaObligatorio()
        {
            var target = new Entity("erf_tablasparaexportar") { Id = Guid.NewGuid() };
            target["erf_name"] = "test sin dni";
            // erf_dni ausente intencionalmente

            var ex = Assert.ThrowsException<InvalidPluginExecutionException>(() =>
                _context.ExecutePluginWith<DniValidationPlugin>(CreatePluginContext(target)));

            StringAssert.Contains(ex.Message, "obligatorio");
        }

        [TestMethod]
        public void Execute_DNIFormatoInvalido_LanzaError()
        {
            var target = new Entity("erf_tablasparaexportar") { Id = Guid.NewGuid() };
            target["erf_name"] = "test formato";
            target["erf_dni"] = "ABC123";

            var ex = Assert.ThrowsException<InvalidPluginExecutionException>(() =>
                _context.ExecutePluginWith<DniValidationPlugin>(CreatePluginContext(target)));

            StringAssert.Contains(ex.Message, "formato");
        }

        [TestMethod]
        public void Execute_DNILetraIncorrecta_LanzaError()
        {
            var target = new Entity("erf_tablasparaexportar") { Id = Guid.NewGuid() };
            target["erf_name"] = "test letra";
            target["erf_dni"] = "12345678A"; // Letra incorrecta, debería ser Z

            var ex = Assert.ThrowsException<InvalidPluginExecutionException>(() =>
                _context.ExecutePluginWith<DniValidationPlugin>(CreatePluginContext(target)));

            StringAssert.Contains(ex.Message, "letra");
        }

        [TestMethod]
        public void Execute_MensajeUpdate_PluginNoActua()
        {
            // El plugin solo actúa en Create; con Update no debe validar el DNI
            var target = new Entity("erf_tablasparaexportar") { Id = Guid.NewGuid() };
            target["erf_name"] = "test update";
            // Sin erf_dni: si el plugin actuase, lanzaría excepción

            _context.ExecutePluginWith<DniValidationPlugin>(
                CreatePluginContext(target, messageName: "Update"));
        }

        [TestMethod]
        public void Execute_EntidadDistinta_PluginNoActua()
        {
            // El plugin solo actúa en erf_tablasparaexportar; con otra entidad no debe validar
            var target = new Entity("account") { Id = Guid.NewGuid() };

            _context.ExecutePluginWith<DniValidationPlugin>(
                CreatePluginContext(target, entityName: "account"));
        }

        [TestMethod]
        public void Execute_StageDistinto_PluginNoActua()
        {
            // El plugin solo actúa en stage 10 (PreValidation); con otro stage no debe validar
            var target = new Entity("erf_tablasparaexportar") { Id = Guid.NewGuid() };
            target["erf_name"] = "test stage";
            // Sin erf_dni: si el plugin actuase, lanzaría excepción

            _context.ExecutePluginWith<DniValidationPlugin>(
                CreatePluginContext(target, stage: 20)); // PostOperation
        }
    }
}
