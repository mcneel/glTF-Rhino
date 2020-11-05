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

                glTFMesh.Name = o.Name;

                var primitive = new glTFLoader.Schema.MeshPrimitive();

                // Faces

                var accessor = new glTFLoader.Schema.Accessor();
                accessor.Type = glTFLoader.Schema.Accessor.TypeEnum.SCALAR;

                var indices = new List<int>();

                foreach (var face in mesh.Faces)
                {
                    if (face.IsTriangle)
                    {
                        indices.Add(face.A);
                        indices.Add(face.B);
                        indices.Add(face.C);
                    }
                    if (face.IsQuad)
                    {
                        indices.Add(face.A);
                        indices.Add(face.B);
                        indices.Add(face.C);
                        indices.Add(face.D);
                    }
                }

                accessor.Count = indices.Count;

                int min = 0;
                int max = 0;

                foreach (var id in indices)
                {
                    if (id < min) min = id;
                    if (id > max) max = id;
                }

                accessor.Min = new float [] { min };
                accessor.Max = new float [] { max };



                /*
                    var faceIds = accessorData[mp.Indices.Value];
                    var faces = new List<MeshFace>();

                    for (int i = 0; i <= faceIds.Count - 3; i = i + 3)
                        faces.Add(new MeshFace(faceIds[i], faceIds[i + 1], faceIds[i + 2]));

                    meshPart.Faces.AddFaces(faces);
                */


            }

            #endregion

            return glTFLoader.Interface.SerializeModel(model);
        }

    }
}
