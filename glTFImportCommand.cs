using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace glTFImport
{

    //we don't need this command at the moment, but keeping the boilerplate around in case this changes.

    [CommandStyle(Style.Hidden)]
    public class glTFImportCommand : Command
    {
        public glTFImportCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static glTFImportCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "glTFImportCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Usually commands in import plug-ins are used to modify settings and behavior.
            // The import work itself is performed by the RhinoglTFPlugIn class.

            return Result.Success;
        }
    }
}
