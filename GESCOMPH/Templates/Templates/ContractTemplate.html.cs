namespace Templates.Templates
{
    public static class ContractTemplate
    {
        public static readonly string Html = @"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"">
  <style>
    /* Configuración de página y estilo general */
    @page { size: Letter; margin: 15mm 20mm; }
    body { font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.4; color: #000; margin: 0; padding: 0; }
    
    /* Header con logo y banner */
    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 10px;
      position: relative;
    }
    
    .header-left {
      display: flex;
      align-items: center;
      gap: 15px;
      flex: 1;
    }
    
    .logo {
      width: 80px;
      height: auto;
    }
    
    .header-text {
      display: flex;
      flex-direction: column;
    }
    
    .header-title {
      font-size: 14pt;
      color: #2c5f8d;
      font-weight: bold;
      margin: 0;
      text-transform: uppercase;
    }
    
    .header-subtitle {
      font-size: 18pt;
      color: #2c5f8d;
      font-weight: bold;
      margin: 0;
      text-transform: uppercase;
    }
    
    .header-nit {
      font-size: 10pt;
      color: #333;
      margin: 2px 0 0 0;
    }
    
    .header-right {
      text-align: right;
      font-size: 9pt;
      line-height: 1.3;
    }
    
    .wave-banner {
      width: 100%;
      height: 40px;
      background: linear-gradient(90deg, #a8d5a8 0%, #fff9b3 50%, #ffd9a8 100%);
      border-radius: 0 0 50% 50% / 0 0 100% 100%;
      margin-bottom: 15px;
    }
    
    /* Título del contrato */
    .contract-title {
      text-align: center;
      font-size: 14pt;
      font-weight: bold;
      text-transform: uppercase;
      margin: 15px 0 10px 0;
    }
    
    .contract-subtitle {
      text-align: center;
      font-size: 11pt;
      font-weight: bold;
      text-transform: uppercase;
      margin: 0 0 20px 0;
    }
    
    /* Tabla de información principal */
    .info-table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 20px;
      border: 2px solid #000;
    }
    
    .info-table td {
      border: 1px solid #000;
      padding: 8px 10px;
      vertical-align: top;
    }
    
    .info-table .label-cell {
      width: 35%;
      font-weight: bold;
      background-color: #f5f5f5;
    }
    
    .info-table .value-cell {
      width: 65%;
    }
    
    /* Secciones del contrato */
    h1, h2 { text-align: center; text-transform: uppercase; }
    p { text-align: justify; text-justify: inter-word; hyphens: auto; margin: 8px 0; }
    .section { margin-top: 15px; page-break-inside: avoid; }
    .section h2 { font-size: 12pt; margin: 10px 0 8px 0; }
    
    ul { padding-left: 20px; margin: 8px 0; }
    li { text-align: justify; hyphens: auto; margin: 4px 0; }
    
    /* Firmas */
    .signature-section { 
      margin-top: 40px; 
      display: grid; 
      grid-template-columns: 1fr 1fr; 
      gap: 30px;
      page-break-inside: avoid;
    }
    .signature { margin-top: 50px; text-align: center; font-size: 10pt; }
    .signature-line { border-top: 1px solid #000; display: inline-block; width: 200px; margin-bottom: 5px; }
  </style>
</head>
<body>
  <!-- Header -->
  <div class=""header"">
    <div class=""header-left"">
      <img src=""data:image/png;base64,{{LOGO_BASE64}}"" alt=""Logo Palermo"" class=""logo"" />
      <div class=""header-text"">
        <div class=""header-title"">Alcaldía Municipal de</div>
        <div class=""header-subtitle"">PALERMO</div>
        <div class=""header-nit"">NIT: 891.180.021-9</div>
      </div>
    </div>
    <div class=""header-right"">
      <strong>COMUNICACIÓN</strong><br />
      <strong>OFICIAL DESPACHADA</strong><br />
      Codigo: FOR -GP-05<br />
      Versión:05<br />
      Fecha: 02-04-2024<br />
      Página No. 1
    </div>
  </div>
  
  <!-- Wave banner -->
  <div class=""wave-banner""></div>
  
  <!-- Título del contrato -->
  <div class=""contract-title"">Contrato de Arrendamiento Nº @Model.ContractNumber de @Model.ContractYear</div>
  <div class=""contract-subtitle"">@Model.PremisesLeased[0].EstablishmentName, @Model.PremisesLeased[0].PlazaName</div>
  
  <!-- Tabla de información principal -->
  <table class=""info-table"">
    <tr>
      <td class=""label-cell"">ARRENDADOR:</td>
      <td class=""value-cell"">
        <strong>MUNICIPIO DE PALERMO (H)</strong>, identificado con Nit. No. 891.180.021-9, representado legalmente por <strong>KLEYVER OVIEDO FARFAN</strong> identificado con Cedula de ciudadanía No. 7.717.624, en calidad de Alcalde Municipal.
      </td>
    </tr>
    <tr>
      <td class=""label-cell"">ARRENDATARIO(A):</td>
      <td class=""value-cell"">
        <strong>@Model.FullName</strong> identificado con cédula de ciudadanía No. @Model.Document<br />
        Dirección: @Model.Address<br />
        Teléfono: @Model.Phone<br />
        Email: @Model.Email
      </td>
    </tr>
    <tr>
      <td class=""label-cell"">OBJETO:</td>
      <td class=""value-cell"">
        Arrendamiento de <strong>@Model.PremisesLeased[0].EstablishmentName</strong> ubicado en @Model.PremisesLeased[0].Address de la <strong>@Model.PremisesLeased[0].PlazaName</strong>
      </td>
    </tr>
    <tr>
      <td class=""label-cell"">VALOR DEL CANON DE ARRENDAMIENTO:</td>
      <td class=""value-cell"">
        <strong>@Model.MonthlyRentAmount</strong> PESOS (@Model.MonthlyRentAmount) M/CTE correspondiente a @Model.UVTValue U.V.T. vigente conforme al año @Model.ContractYear Por los locales, más I.V.A. El valor se ajustará anualmente al U.V.T.
      </td>
    </tr>
    <tr>
      <td class=""label-cell"">TÉRMINO /DURACIÓN DEL CONTRATO:</td>
      <td class=""value-cell""><strong>@Model.DurationMonths MESES</strong></td>
    </tr>
    <tr>
      <td class=""label-cell"">FECHA DE INICIO DEL CONTRATO:</td>
      <td class=""value-cell""><strong>@Model.StartDate.ToString(""dd/MM/yyyy"")</strong></td>
    </tr>
  </table>

  <!-- Contenido del contrato -->
  <p>Entre los suscritos a saber: por una parte el <strong>MUNICIPIO DE PALERMO (H)</strong>, identificado con NIT <strong>891.180.021-9</strong>, representado legalmente por <strong>KLEYVER OVIEDO FARFAN</strong>, identificado con cédula de ciudadanía No. <strong>7.717.624</strong>, en calidad de <strong>Alcalde Municipal</strong>, quien en adelante se denominará EL ARRENDADOR; y por otra parte <strong>@Model.FullName</strong>, identificado(a) con cédula de ciudadanía No. <strong>@Model.Document</strong>, en adelante EL ARRENDATARIO; se celebra el presente contrato de arrendamiento que se regirá por las siguientes estipulaciones:</p>

  <div class=""section"">
    <h2>Primera: Objeto</h2>
    <p>EL ARRENDADOR da en arrendamiento a EL ARRENDATARIO el inmueble/local denominado <strong>@Model.PremisesLeased[0].EstablishmentName</strong>, ubicado en <strong>@Model.PremisesLeased[0].Address</strong>, con un área aproximada de <strong>@Model.PremisesLeased[0].AreaM2</strong> m², perteneciente a la plaza <strong>@Model.PremisesLeased[0].PlazaName</strong>. El local se destinará exclusivamente a las actividades comerciales permitidas por el reglamento del centro comercial y la normatividad vigente.</p>
  </div>

  <div class=""section"">
    <h2>Segunda: Canon de Arrendamiento y Pagos</h2>
    <p>El canon mensual de arrendamiento será de <strong>@Model.MonthlyRentAmount PESOS</strong> (@Model.MonthlyRentAmountWords) más IVA cuando aplique. El pago se efectuará por mensualidades vencidas en los medios de pago autorizados por EL ARRENDADOR. La mora causará intereses a la tasa máxima legal permitida.</p>
  </div>

  <div class=""section"">
    <h2>Tercera: Plazo</h2>
    <p>El contrato tendrá vigencia de <strong>@Model.DurationMonths MESES</strong>, desde el <strong>@Model.StartDate.ToString(""dd/MM/yyyy"")</strong> hasta el <strong>@Model.EndDate.ToString(""dd/MM/yyyy"")</strong>. Podrá renovarse por períodos iguales salvo preaviso en los términos pactados entre las partes.</p>
  </div>

  <div class=""section"">
    <h2>Cuarta: Entrega e Inventario</h2>
    <p>La entrega del inmueble se realizará mediante acta que deje constancia del estado del local, dotaciones y elementos recibidos. Al finalizar, EL ARRENDATARIO restituirá el inmueble en condiciones similares, salvo deterioro natural por el uso normal.</p>
  </div>

  <div class=""section"">
    <h2>Quinta: Uso y Reglamento</h2>
    <p>EL ARRENDATARIO se obliga a dar al inmueble el uso convenido y a cumplir el reglamento interno del centro comercial, horarios, manuales de imagen y demás políticas comunicadas por EL ARRENDADOR.</p>
  </div>

  <div class=""section"">
    <h2>Sexta: Mantenimiento y Servicios</h2>
    <p>Serán a cargo de EL ARRENDATARIO los gastos ordinarios de mantenimiento locativo y el pago de los servicios públicos, administración y demás expensas inherentes a la operación del local. Las reparaciones estructurales o de fuerza mayor serán asumidas por EL ARRENDADOR, salvo culpa del arrendatario.</p>
  </div>

  <div class=""section"">
    <h2>Séptima: Incrementos</h2>
    <p>El canon podrá ser ajustado anualmente con base en la Unidad de Valor Tributario (U.V.T.) vigente u otro índice que las partes acuerden por escrito, aplicable a partir del aniversario de inicio del contrato.</p>
  </div>

  <div class=""section"">
    <h2>Octava: Garantías</h2>
    <p>Cuando aplique, EL ARRENDATARIO mantendrá vigente la garantía acordada (depósito, póliza o codeudor) para asegurar el cumplimiento de sus obligaciones.</p>
  </div>

  <div class=""section"">
    <h2>Novena: Cesión y Subarriendo</h2>
    <p>EL ARRENDATARIO no podrá ceder el contrato ni subarrendar total o parcialmente el inmueble sin autorización previa y escrita de EL ARRENDADOR.</p>
  </div>

  <div class=""section"">
    <h2>Décima: Terminación</h2>
    <p>Son causales de terminación, entre otras, el incumplimiento de obligaciones esenciales, el uso indebido del inmueble, la falta de pago reiterada y la vulneración del reglamento. La parte cumplida podrá declarar la terminación y exigir la restitución del local y las indemnizaciones a que haya lugar.</p>
  </div>

  <div class=""section"">
    <h2>Cláusulas Especiales</h2>
    <ul>
      {{CLAUSES}}
    </ul>
  </div>


  <div class=""signature-section"">
    <div class=""signature"">
      <div class=""signature-line""></div><br />
      <strong>EL ARRENDADOR</strong><br />
      MUNICIPIO DE PALERMO (H)<br />
      Rep. Legal: KLEYVER OVIEDO FARFAN<br />
      Alcalde Municipal<br />
      CC 7.717.624
    </div>
    <div class=""signature"">
      <div class=""signature-line""></div><br />
      <strong>EL ARRENDATARIO</strong><br />
      @Model.FullName<br />
      CC @Model.Document
    </div>
  </div>
</body>
</html>";
    }
}