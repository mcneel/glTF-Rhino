using System.Reflection;
using System.Runtime.InteropServices;
using Rhino.PlugIns;

// Plug-in Description Attributes - all of these are optional.
// These will show in Rhino's option dialog, in the tab Plug-ins.
[assembly: PlugInDescription(DescriptionType.Address, "-")]
[assembly: PlugInDescription(DescriptionType.Country, "-")]
[assembly: PlugInDescription(DescriptionType.Email, "-")]
[assembly: PlugInDescription(DescriptionType.Phone, "-")]
[assembly: PlugInDescription(DescriptionType.Fax, "-")]
[assembly: PlugInDescription(DescriptionType.Organization, "-")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "-")]
[assembly: PlugInDescription(DescriptionType.WebSite, "http://rhino3d.com")]

// Icons should be Windows .ico files and contain 32-bit images in the following sizes: 16, 24, 32, 48, and 256.
// This is a Rhino 6-only description.
[assembly: PlugInDescription(DescriptionType.Icon, "glTFImport.EmbeddedResources.plugin-import.ico")]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("glTFImport")]

// This will be used also for the plug-in description.
[assembly: AssemblyDescription("glTF Import import plug-in")]

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("glTF Import")]
[assembly: AssemblyCopyright("Copyright © Robert McNeel & Associates  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d5c378a1-393f-4c39-b641-02a54f094cfa")] // This will also be the Guid of the Rhino plug-in

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("0.0.1.0")]
[assembly: AssemblyFileVersion("0.0.1.0")]

// Make compatible with Rhino Installer Engine
[assembly: AssemblyInformationalVersion("2")]
