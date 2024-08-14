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
"CreatePortableTerrariaTask.cs"
"GuiForm.cs"
"GuiTerrariaDllPanel.cs"
"Program.cs"
"TerrariaDllResolver.cs"
"../Sahlaysta.PortableTerrariaCommon/Cecil.cs"
"../Sahlaysta.PortableTerrariaCommon/DotNetZip.cs"
"../Sahlaysta.PortableTerrariaCommon/GuiFileDialogs.cs"
"../Sahlaysta.PortableTerrariaCommon/GuiPanelBuilder.cs"
"../Sahlaysta.PortableTerrariaCommon/ReflectionHelper.cs"
"../Sahlaysta.PortableTerrariaCommon/StreamConcatenator.cs"
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