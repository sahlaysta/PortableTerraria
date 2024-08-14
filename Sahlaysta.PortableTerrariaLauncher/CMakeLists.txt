include(../CSharp_Project.cmake)
CSharp_Project(

PROJECT_NAME
"Sahlaysta.PortableTerrariaLauncher"

PROJECT_APPLICATIONTYPE
"Application"

PROJECT_APPLICATIONICON
"../Sahlaysta.PortableTerrariaResources/PortableTerrariaLauncherIcon.ico"

PROJECT_SOURCEFILES
"AssemblyInfo.cs"
"GuiForm.cs"
"Program.cs"
"TerrariaLauncher.cs"
"../Sahlaysta.PortableTerrariaCommon/GuiFileDialogs.cs"
"../Sahlaysta.PortableTerrariaCommon/GuiPanelBuilder.cs"

PROJECT_VSDOTNETREFERENCES
"Microsoft.CSharp"
"System"
"System.Core"
"System.Data"
"System.Drawing"
"System.Windows.Forms"

PROJECT_EMBEDDEDRESOURCEFILES
"../Sahlaysta.PortableTerrariaResources/DotNetZip.dll"

)