include(../CSharp_Project.cmake)
CSharp_Project(

PROJECT_NAME
"Sahlaysta.PortableTerrariaCreator"

PROJECT_APPLICATIONTYPE
"Application"

PROJECT_DEPENDS
"Sahlaysta.PortableTerrariaLauncher"

PROJECT_APPLICATIONICON
"../Sahlaysta.PortableTerrariaResources/PortableTerrariaCreatorIcon.ico"

PROJECT_SOURCEFILES
"AssemblyInfo.cs"
"GuiForm.cs"
"GuiLauncherAssemblyWriter.cs"
"GuiTerrariaDllPanel.cs"
"Program.cs"
"../Sahlaysta.PortableTerrariaCommon/Cecil.cs"
"../Sahlaysta.PortableTerrariaCommon/DotNetZip.cs"
"../Sahlaysta.PortableTerrariaCommon/GuiFileDialogs.cs"
"../Sahlaysta.PortableTerrariaCommon/GuiProgressDialog.cs"
"../Sahlaysta.PortableTerrariaCommon/ManifestResources.cs"
"../Sahlaysta.PortableTerrariaCommon/PanelBuilder.cs"
"../Sahlaysta.PortableTerrariaCommon/PathRelativity.cs"
"../Sahlaysta.PortableTerrariaCommon/ReflectionHelper.cs"
"../Sahlaysta.PortableTerrariaCommon/StreamConcatenator.cs"
"../Sahlaysta.PortableTerrariaCommon/TerrariaDllResolver.cs"
"../Sahlaysta.PortableTerrariaCommon/ThreadStreamPiper.cs"

PROJECT_VSDOTNETREFERENCES
"Microsoft.CSharp"
"System"
"System.Core"
"System.Data"
"System.Drawing"
"System.Windows.Forms"

PROJECT_EMBEDDEDRESOURCEFILES
"../Sahlaysta.PortableTerrariaResources/DotNetZip.dll"
"../Sahlaysta.PortableTerrariaResources/Mono.Cecil.dll"
"${CMAKE_CURRENT_BINARY_DIR}/../Sahlaysta.PortableTerrariaLauncher/binout/Sahlaysta.PortableTerrariaLauncher.exe"

PROJECT_POSTBUILDCOMMANDS
COMMAND ${CMAKE_COMMAND} -E copy "${CMAKE_CURRENT_BINARY_DIR}/binout/Sahlaysta.PortableTerrariaCreator.exe" "${CMAKE_CURRENT_BINARY_DIR}/../../../PortableTerrariaCreator/PortableTerrariaCreator.exe"

)