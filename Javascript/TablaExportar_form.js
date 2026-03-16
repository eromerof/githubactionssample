/**********************************************************************************************************
* Javascript para agregar lógica al formulario de tabla exportar
* @author enriqueromero.es
* @current version : 1.0
***********************************************************************************************************/
if (typeof (FormTablaExportar) === "undefined")
    FormTablaExportar = { __namespace: true };
var formContext = null;



FormTablaExportar.Events =
{
    OnLoad: function (executionContext) {
        "use strict";
        formContext = executionContext.getFormContext();
       



    },

    OnSave: function (executionContext) {
        "use strict";
    }
};

FormTablaExportar.Functions =
{
   ShowDialog: function () {
        "use strict";
        Common.Utilities.OpenNavigateTo("erf_tablasparaexportar","cd_custompageconfirmacin_c9245","Calificar")
       



    },
};
