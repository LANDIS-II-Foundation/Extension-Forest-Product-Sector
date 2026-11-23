; LANDIS-II Extension infomation
#define CoreRelease "LANDIS-II-V8"
#define ExtensionName "Forest Product Sector Extension"
#define AppVersion "1.1"
#define AppPublisher "LANDIS-II Foundation"
#define AppURL "http://www.landis-ii.org/"

; Build directory
#define BuildDir "..\..\bin\Release\net8.0"

; LANDIS-II installation directories
#define ExtDir "C:\Program Files\LANDIS-II-v8\extensions"
#define AppDir "C:\Program Files\LANDIS-II-v8"
#define SetupDir "C:\FPS"
#define ExampleDir "C:\FPS\examples"
#define LandisPlugInDir "C:\Program Files\LANDIS-II-v8\plug-ins-installer-files"
#define ExtensionsCmd AppDir + "\commands\landis-ii-extensions.cmd"
#define Example1Dir "C:\FPS\examples\ComplexTier3Ex"
#define Example2Dir "C:\FPS\examples\SimpleTier1Ex"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{42DD4663-54B1-4A9A-9FB5-6EE556ACC1D2}
AppName={#CoreRelease} {#ExtensionName}
AppVersion={#AppVersion}
; Name in "Programs and Features"
AppVerName={#CoreRelease} {#ExtensionName} v{#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={pf}\{#ExtensionName}
DisableDirPage=yes
DefaultGroupName={#ExtensionName}
DisableProgramGroupPage=yes
LicenseFile=LANDIS-II_Binary_license.rtf
OutputDir={#SourcePath}
OutputBaseFilename={#CoreRelease} {#ExtensionName} {#AppVersion}-setup
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"


[Files]
; This .dll IS the extension (ie, the extension's assembly)
; NB: Do not put an additional version number in the file name of this .dll
; (The name of this .dll is defined in the extension's \src\*.csproj file)
Source: {#BuildDir}\Landis.Extension.FPS-v1.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Landis.Extension.FPS-v1.exe; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Landis.Extension.FPS-v1.runtimeconfig.json; DestDir: {#SetupDir}; Flags: replacesameversion
; The dependency manifest tells .NET where the native GDAL libraries are (runtimes\ below).
; With it installed, every assembly it lists must be installed too.
Source: {#BuildDir}\Landis.Extension.FPS-v1.deps.json; DestDir: {#SetupDir}; Flags: replacesameversion
Source: LANDIS-II ForestProducts v1.1 User Guide.pdf; DestDir: {#SetupDir}; Flags: replacesameversion


; Requisite auxiliary libraries
; NB. These libraries are used by other extensions and thus are never uninstalled.
Source: {#BuildDir}\Landis.Core.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Landis.Library.Metadata-v2.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Landis.Library.Parameters-v2.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Landis.SpatialModeling.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Landis.Utilities.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\log4net.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\MathNet.Numerics.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\System.Configuration.ConfigurationManager.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\System.Security.Cryptography.ProtectedData.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\System.Security.Permissions.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Troschuetz.Random.dll; DestDir: {#SetupDir}; Flags: replacesameversion

; Reading the ManagementUnits map: LANDIS-II's raster library and GDAL. FPSM loads the
; Windows or Linux driver at run time; the manifest lists both, so both are installed.
Source: {#BuildDir}\Landis.RasterIO.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Landis.RasterIO.Gdal.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Landis.RasterIO.Gdal.Linux.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\Gdal.Core.dll; DestDir: {#SetupDir}; Flags: replacesameversion
Source: {#BuildDir}\runtimes\win10-x64\*; DestDir: {#SetupDir}\runtimes\win10-x64; Flags: replacesameversion recursesubdirs createallsubdirs
Source: {#BuildDir}\runtimes\win\*; DestDir: {#SetupDir}\runtimes\win; Flags: replacesameversion recursesubdirs createallsubdirs

; Complete examples for testing the extension
Source: ..\examples\ComplexTier3Ex\*; DestDir: {#Example1Dir}; Flags: replacesameversion
Source: ..\examples\SimpleTier1Ex\*; DestDir: {#Example2Dir}; Flags: replacesameversion

; LANDIS-II identifies the extension with the info in this .txt file
; NB. New releases must modify the name of this file and the info in it
#define InfoTxt "FPS 1.1.txt"
Source: {#InfoTxt}; DestDir: {#LandisPlugInDir}
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Code]
procedure CreateDirectory();
begin
 ForceDirectories('C:\FPS\examples\ComplexTier3Ex');
 ForceDirectories('C:\FPS\examples\SimpleTier1Ex');
end;

[Run]
Filename: {#ExtensionsCmd}; Parameters: "remove ""Forest Product Sector Extension"" "; WorkingDir: {#LandisPlugInDir}; BeforeInstall: CreateDirectory
Filename: {#ExtensionsCmd}; Parameters: "add ""{#InfoTxt}"" "; WorkingDir: {#LandisPlugInDir} 


[UninstallRun]
; Remove "Age-Only Succession" from "extensions.xml" file.
Filename: {#ExtensionsCmd}; Parameters: "remove ""Forest Product Sector Extension"" "; WorkingDir: {#LandisPlugInDir}


