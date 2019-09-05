using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace glTFExport
{
    public class ModelExporter
    {
        public static string Serialize(Rhino.RhinoDoc doc)
        {
            var model = new glTFLoader.Schema.Gltf();

            #region Iterate through objects in doc

            foreach(Rhino.DocObjects.RhinoObject o in doc.Objects)
            {
                var mesh = new Rhino.Geometry.Mesh();
                var glTFMesh = new glTFLoader.Schema.Mesh();

                switch (o.ObjectType)
                {
                    case Rhino.DocObjects.ObjectType.Extrusion:
                    case Rhino.DocObjects.ObjectType.SubD:
                    case Rhino.DocObjects.ObjectType.Brep:
                        mesh.Append(o.GetMeshes(Rhino.Geometry.MeshType.Default));
                        break;
                    case Rhino.DocObjects.ObjectType.Mesh:
                        mesh = o.Geometry as Rhino.Geometry.Mesh;
                        break;
                    default:
                        Rhino.RhinoApp.WriteLine("Exporting {0} is not supported.", o.ObjectType);
                        break;
                }

                // do something with mesh

            }

            #endregion

            return glTFLoader.Interface.SerializeModel(model);
        }

    }
}
