using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using PluginsDataverse;

namespace PluginsDataverse.Tests
{
    [TestClass]
    public class DniValidationPluginTests
    {
        [TestMethod]
        public void ValidateDni_ValidDni_DoesNotThrow()
        {
            // Example valid DNI: 12345678Z -> 12345678 % 23 = 14 -> DniLetters[14] = Z
            DniValidationPlugin.ValidateDni("12345678Z");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ValidateDni_NullOrEmpty_ThrowsMandatoryException()
        {
            DniValidationPlugin.ValidateDni(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ValidateDni_WrongFormat_ThrowsFormatException()
        {
            DniValidationPlugin.ValidateDni("ABC123");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ValidateDni_WrongLetter_ThrowsLetterException()
        {
            // 12345678 -> expected letter Z, so use A to fail
            DniValidationPlugin.ValidateDni("12345678A");
        }
    }
}
