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
"DirectXAudioRegistry.cs"
"GuiForm.cs"
"GuiLauncherAssemblyReader.cs"
"GuiTerrariaInstaller.cs"
"GuiTerrariaLauncher.cs"
"Program.cs"
"../Sahlaysta.PortableTerrariaCommon/DotNetZip.cs"
"../Sahlaysta.PortableTerrariaCommon/GuiFileDialogs.cs"
"../Sahlaysta.PortableTerrariaCommon/GuiProgressDialog.cs"
"../Sahlaysta.PortableTerrariaCommon/ManifestResources.cs"
"../Sahlaysta.PortableTerrariaCommon/PanelBuilder.cs"
"../Sahlaysta.PortableTerrariaCommon/ReflectionHelper.cs"
"../Sahlaysta.PortableTerrariaCommon/StreamConcatenator.cs"
"../Sahlaysta.PortableTerrariaCommon/TerrariaDllResolver.cs"

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