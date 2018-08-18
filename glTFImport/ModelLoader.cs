using glTFLoader;
using glTFLoader.Schema;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace glTFImport
{
    public static class ModelLoader
    {

        public static void Load(string filename, RhinoDoc doc)
        {
            var model = Interface.LoadModel(filename);

            if (model != null)
            {
                // dictionary with deserialized buffer data
                var bufferData = new Dictionary<int, byte[]>();

                // accessorData contains a dictionary to the buffer data meant to be accessed by an accessor via a bufferView
                var accessorData = new Dictionary<int, dynamic>();

                // material data contains a dictionary with the material index and the Rhino material ID
                var materialData = new Dictionary<int, int>();

                // mesh data
                var meshData = new Dictionary<int, List<Rhino.Geometry.Mesh>>();

                var nodeXformData = new Dictionary<int, Transform>();

                var dir = Path.GetDirectoryName(filename);

                #region Read Buffers

                for (int i = 0; i < model.Buffers.Length; i++)
                {
                    var data = Interface.LoadBinaryBuffer(model, i, filename);
                    bufferData.Add(i, data);
                }

                #endregion

                #region Process Images

                //go through images in model
                //save them to disk if necessary

                const string EMBEDDEDPNG = "data:image/png;base64,";
                const string EMBEDDEDJPEG = "data:image/jpeg;base64,";

                var imageData = new Dictionary<int, string>(); //image index, image path

                if (model.Images != null)
                {
                    for (int i = 0; i < model.Images.Length; i++)
                    {
                        var image = model.Images[i];
                        var name = image.Name ?? "embeddedImage_"+i;
                        var extension = string.Empty;
                        var imageStream = Stream.Null;

                        if (image.BufferView.HasValue)
                        {
                            imageStream = Interface.OpenImageFile(model, i, filename);
                            if (image.MimeType.HasValue)
                                if (image.MimeType == glTFLoader.Schema.Image.MimeTypeEnum.image_jpeg)
                                    extension = ".jpg";
                                else if (image.MimeType == glTFLoader.Schema.Image.MimeTypeEnum.image_png)
                                    extension = ".png";

                            var imgPath = Path.Combine(dir, "EmbeddedImages");
                            if (!Directory.Exists(imgPath))
                                Directory.CreateDirectory(imgPath);
                            imgPath = Path.Combine(imgPath, name + extension);
                            
                            using (var fileStream = File.Create(imgPath))
                            {
                                imageStream.Seek(0, SeekOrigin.Begin);
                                imageStream.CopyTo(fileStream);
                                imageData.Add(i, imgPath);
                            }

                        }

                        if (image.Uri != null && image.Uri.StartsWith("data:image/"))
                        {
                            if (image.Uri.StartsWith(EMBEDDEDPNG)) extension = ".png";
                            if (image.Uri.StartsWith(EMBEDDEDJPEG)) extension = ".jpg";

                            imageStream = Interface.OpenImageFile(model, i, filename);

                            var imgPath = Path.Combine(dir, "EmbeddedImages");
                            if (!Directory.Exists(imgPath))
                                Directory.CreateDirectory(imgPath);
                            imgPath = Path.Combine(imgPath, name + extension);

                            using (var fileStream = File.Create(imgPath))
                            {
                                imageStream.Seek(0, SeekOrigin.Begin);
                                imageStream.CopyTo(fileStream);
                                imageData.Add(i, imgPath);
                            }
                        }
                        
                        if (image.Uri != null && File.Exists(Path.Combine(dir, image.Uri)))
                        {
                            imageData.Add(i, Path.Combine(dir, image.Uri));
                        }
                    }
                }

                #endregion

                #region Process Materials

                if (model.Materials != null)
                {

                    for (int i = 0; i < model.Materials.Length; i++)
                    {
                        var mat = model.Materials[i];
                        var rhinoMat = new Rhino.DocObjects.Material();

                        var texId = -1;
                        int? sourceId = null;

                        if (mat.NormalTexture != null) { }
                        if (mat.OcclusionTexture != null) { }
                        if (mat.EmissiveTexture != null) { }

                        if (mat.PbrMetallicRoughness.BaseColorTexture != null)
                        {
                            texId = mat.PbrMetallicRoughness.BaseColorTexture.Index;
                            sourceId = model.Textures[texId].Source.Value;
                            rhinoMat.SetBitmapTexture(imageData[sourceId.Value]);
                        }

                        if (mat.PbrMetallicRoughness.MetallicRoughnessTexture != null)
                        {
                            texId = mat.PbrMetallicRoughness.MetallicRoughnessTexture.Index;
                            sourceId = model.Textures[texId].Source.Value;
                            rhinoMat.SetBumpTexture(imageData[sourceId.Value]);
                        }

                        rhinoMat.Name = mat.Name;

                        materialData.Add(i, doc.Materials.Add(rhinoMat));

                    }
                }

                #endregion

                #region Access Buffers

                for (int i = 0; i < model.Accessors.Length; i++)
                {
                    var accessor = model.Accessors[i];

                    //process, afterwards, check if sparse

                    if (accessor.BufferView != null)
                    {

                        var bufferView = model.BufferViews[accessor.BufferView.Value];

                        var buffer = bufferData[bufferView.Buffer]; //byte[]

                        //calculate byte length
                        var elementBytes = GetTypeMultiplier(accessor.Type) * GetComponentTypeMultiplier(accessor.ComponentType);
                        var stride = bufferView.ByteStride != null ? bufferView.ByteStride.Value : 0;
                        var strideDiff = stride > 0 ? stride - elementBytes : 0;
                        var count = (elementBytes + strideDiff) * accessor.Count;

                        var arr = new byte[count];

                        System.Buffer.BlockCopy(buffer, bufferView.ByteOffset + accessor.ByteOffset, arr, 0, count);

                        var res = AccessBuffer(accessor.Type, accessor.Count, accessor.ComponentType, stride, arr);

                        accessorData.Add(i, res);
                    }

                    // if accessor is sparse, need to modify the data. accessorData index is i

                    if (accessor.Sparse != null)
                    {
                        //TODO
                        //If a BufferView is specified in the accessor, sparse acts a a way to replace  values
                        //if a BufferView does not exist in the accessor, just process the sparse data

                        //access construct data

                        var bufferViewI = model.BufferViews[accessor.Sparse.Indices.BufferView];

                        var bufferI = bufferData[bufferViewI.Buffer]; //byte[]

                        var sparseComponentType = (Accessor.ComponentTypeEnum)accessor.Sparse.Indices.ComponentType;

                        //calculate count
                        var elementBytesI = GetTypeMultiplier(Accessor.TypeEnum.SCALAR) * GetComponentTypeMultiplier(sparseComponentType);
                        var strideI = bufferViewI.ByteStride != null ? bufferViewI.ByteStride.Value : 0;
                        var strideDiffI = strideI > 0 ? strideI - elementBytesI : 0;
                        var countI = (elementBytesI + strideDiffI) * accessor.Sparse.Count;

                        var arrI = new byte[countI];
                        System.Buffer.BlockCopy(bufferI, accessor.Sparse.Indices.ByteOffset + bufferViewI.ByteOffset, arrI, 0, countI);

                        var resIndices = AccessBuffer(Accessor.TypeEnum.SCALAR, accessor.Sparse.Count, sparseComponentType, strideI, arrI);

                        /////////

                        var bufferViewV = model.BufferViews[accessor.Sparse.Values.BufferView];

                        var bufferV = bufferData[bufferViewV.Buffer]; //byte[]

                        //calculate count
                        var elementBytesV = GetTypeMultiplier(accessor.Type) * GetComponentTypeMultiplier(accessor.ComponentType);
                        var strideV = bufferViewV.ByteStride != null ? bufferViewV.ByteStride.Value : 0;
                        var strideDiffV = strideV > 0 ? strideV - elementBytesV : 0;
                        var countV = (elementBytesV + strideDiffV) * accessor.Sparse.Count;

                        var arrV = new byte[countV];
                        System.Buffer.BlockCopy(bufferV, accessor.Sparse.Values.ByteOffset + bufferViewV.ByteOffset, arrV, 0, countV);

                        var resValues = AccessBuffer(accessor.Type, accessor.Sparse.Count, accessor.ComponentType, strideV, arrV);

                        //mod accessorData
                        var valueCnt = 0;

                        for (int j = 0; j < accessor.Sparse.Count; j++)
                        {
                            var index = resIndices[j];
                            var mult = GetTypeMultiplier(accessor.Type);
                            var indexAccessorData = index * mult;

                            for (int k = 0; k < mult; k++)
                            {
                                accessorData[i][indexAccessorData + k] = resValues[valueCnt];
                                valueCnt++;
                            }

                        }

                    }

                }

                #endregion

                #region Process Meshes

                //foreach (var m in model.Meshes)
                for(int j = 0; j < model.Meshes.Length; j++)
                {

                    var m = model.Meshes[j];

                    var groupId = doc.Groups.Add(m.Name);

                    var meshes = new List<Rhino.Geometry.Mesh>();

                    foreach (var mp in m.Primitives)
                    {

                        //Do I need to treat different MeshPrimitive.ModeEnum differently? Yes because if we get POINTS, LINES, LINE_LOOP, LINE_STRIP then it won't be a mesh

                        var meshPart = new Rhino.Geometry.Mesh();

                        foreach (var att in mp.Attributes)
                        {
                            var attributeData = accessorData[att.Value];

                            switch (att.Key)
                            {
                                case "POSITION":

                                    var pts = new List<Point3d>();

                                    for (int i = 0; i <= attributeData.Count - 3; i = i + 3)
                                        pts.Add(new Point3d(attributeData[i], attributeData[i + 1], attributeData[i + 2]));

                                    meshPart.Vertices.AddVertices(pts);

                                    break;

                                case "TEXCOORD_0":

                                    var uvs = new List<Point2f>();

                                    for (int i = 0; i <= attributeData.Count - 2; i = i + 2)
                                        uvs.Add(new Point2f(attributeData[i], attributeData[i + 1]));

                                    meshPart.TextureCoordinates.AddRange(uvs.ToArray());

                                    break;

                                case "NORMAL":

                                    var normals = new List<Vector3f>();

                                    for (int i = 0; i <= attributeData.Count - 3; i = i + 3)
                                        normals.Add(new Vector3f(attributeData[i], attributeData[i + 1], attributeData[i + 2]));

                                    meshPart.Normals.AddRange(normals.ToArray());

                                    break;

                                case "COLOR_0":

                                    var colors = new List<Color>();

                                    for (int i = 0; i <= attributeData.Count - 3; i = i + 3)
                                        colors.Add(ColorFromSingle(attributeData[i], attributeData[i + 1], attributeData[i + 2]));

                                    meshPart.VertexColors.AppendColors(colors.ToArray());

                                    break;

                                default:

                                    RhinoApp.WriteLine("Rhino glTF Importer: Attribute {0} not supported in Rhino. Skipping.", att.Key);

                                    /* NOT SUPPORTED IN RHINO ... yet

                                    - TANGENT
                                    - TEXCOORD_1 //might be supported with multiple mapping channels?
                                    - JOINTS_0
                                    - WEIGHTS_0

                                    */

                                    break;
                            }
 
                        }

                        if (mp.Indices != null)
                        {
                            // Indices can be defined as UNSIGNED_BYTE 5121 or UNSIGNED_SHORT 5123, maybe even as UNSIGNED_INT 5125

                            var faceIds = accessorData[mp.Indices.Value];
                            var faces = new List<MeshFace>();

                            for (int i = 0; i <= faceIds.Count - 3; i = i + 3)
                                faces.Add(new MeshFace(faceIds[i], faceIds[i + 1], faceIds[i + 2]));

                            meshPart.Faces.AddFaces(faces);
                        }

                        //meshPart.Weld(Math.PI);

                        var oa = new ObjectAttributes
                        {
                            MaterialSource = ObjectMaterialSource.MaterialFromObject,
                            //MaterialIndex = (mp.Material!=null) ? materials[mp.Material.Value] : 0,
                            Name = m.Name
                        };

                        if (mp.Material != null)
                            oa.MaterialIndex = materialData[mp.Material.Value];

                        meshPart.Compact();

#if DEBUG
                        if (!meshPart.IsValid)
                        {
                            //meshPart.Weld(Math.PI);
                            //meshPart.Vertices.Align(0.0001);
                            //meshPart.Vertices.CombineIdentical(true, true);
                            //meshPart.Vertices.CullUnused();
                            //meshPart.FaceNormals.ComputeFaceNormals();
                            //meshPart.Normals.ComputeNormals();
                            
                            if (!meshPart.IsValid)
                            {
                                for (int i = 0; i < meshPart.Vertices.Count; i++)
                                {
                                    doc.Objects.AddTextDot(i.ToString(), meshPart.Vertices[i]);
                                    var ptD = new Point3d(meshPart.Vertices[i]);
                                    ptD.Transform(Transform.Translation(meshPart.Normals[i]));
                                    doc.Objects.AddLine(meshPart.Vertices[i], ptD);
                                }
                                foreach (var mf in meshPart.Faces)
                                    RhinoApp.WriteLine("Rhino glTF: Mesh Face - {0}", mf);
                                doc.Objects.AddPoints(meshPart.Vertices.ToPoint3dArray());
                            }
                            else
                                doc.Objects.AddMesh(meshPart, oa);

                        }
#endif

                        // var guid = doc.Objects.AddMesh(meshPart, oa);
                        // doc.Groups.AddToGroup(groupId, guid);

                        meshes.Add(meshPart);
                        
                    }

                    meshData.Add(j, meshes);
                }

                #endregion

                #region Process Nodes Transforms

                for (int i = 0; i < model.Nodes.Length; i++)
                    nodeXformData.Add(i , ProcessNode(model.Nodes[i]));

                //for (int i = 0; i < model.Nodes.Length; i++)
                //TraverseNode(model, model.Nodes[i], Transform.Unset, meshData);

                TraverseNode(model, model.Nodes[model.Scenes[model.Scene.Value].Nodes[0]], Transform.Identity, meshData);

                #endregion
              
                #region Add to doc

                for(int i = 0; i < model.Nodes.Length; i++)
                {
                    var n = model.Nodes[i];
                    if (n.Mesh.HasValue)
                    {
                        //should be doing the orientation here
                        for (int j = 0; j < meshData.Values.ElementAt(i).Count ; j++)
                        {
                            var meshes = meshData.Values.ElementAt(i);
                            var group = doc.Groups.Add(n.Name);
                            foreach (var m in meshes)
                            {
                                var oa = new ObjectAttributes
                                {
                                    Name = model.Meshes[j].Name
                                };
                                var guid = doc.Objects.AddMesh(m, oa);
                                doc.Groups.AddToGroup(group, guid);
                            }


                        }

                    }
                }

                


                #endregion


            }
        }

        
        public static Transform TraverseNode(Gltf model, Node node, Transform parentXform, Dictionary<int, List<Rhino.Geometry.Mesh>> meshDict)
        {
            //process Node

            var xform = ProcessNode(node);

            //if (parentXform != Transform.Unset)
                //xform *= parentXform;
                //xform = Transform.Multiply(xform, parentXform);

            ProcessNodeElements(model,node, meshDict, xform, parentXform);

            //process children
            if (node.Children != null)
                foreach (var n in node.Children)
                    TraverseNode(model, model.Nodes[n], xform, meshDict);

            return xform;
        }

        public static void ProcessNodeElements(Gltf model, Node node, Dictionary<int, List<Rhino.Geometry.Mesh>> meshDict, Transform xform, Transform parentXform)
        {
            if (node.Mesh.HasValue)
                foreach (var m in meshDict[node.Mesh.Value])
                {
                    m.Transform(xform);
                    if (!parentXform.IsIdentity)
                        m.Transform(parentXform);
                }
        }

        public static Transform ProcessNode(Node n)
        {
            var xform = Transform.Identity;
            xform.M00 = n.Matrix[0]; xform.M10 = n.Matrix[4]; xform.M20 = n.Matrix[8];    xform.M30 = n.Matrix[12];
            xform.M01 = n.Matrix[1]; xform.M11 = n.Matrix[5]; xform.M21 = n.Matrix[9];    xform.M31 = n.Matrix[13];
            xform.M02 = n.Matrix[2]; xform.M12 = n.Matrix[6]; xform.M22 = n.Matrix[10];   xform.M32 = n.Matrix[14];
            xform.M03 = n.Matrix[3]; xform.M13 = n.Matrix[7]; xform.M23 = n.Matrix[11];   xform.M33 = n.Matrix[15];

            var translation = Transform.Translation(n.Translation[0], n.Translation[1], n.Translation[2]);
            var rotationQ = Quaternion.Identity;//;new Quaternion(n.Rotation[3], n.Rotation[0], n.Rotation[1], n.Rotation[2]);
            rotationQ.A = n.Rotation[3];
            rotationQ.B = n.Rotation[0];
            rotationQ.C = n.Rotation[1];
            rotationQ.D = n.Rotation[2];

            var axis = Vector3d.Unset;
            rotationQ.Conjugate.GetRotation(out double angle, out axis);
            //axis.Reverse();
            var rotation = Transform.Rotation(angle, axis, Point3d.Origin);
            var scale = Transform.Scale(Plane.WorldXY, n.Scale[0], n.Scale[1], n.Scale[2]);
            
            if (!(n.Translation[0] == 0 && n.Translation[1] == 0 && n.Translation[2] == 0))
                xform *= translation;
                //xform = Transform.Multiply(xform, translation);

            if (!(n.Rotation[0] == 0 && n.Rotation[1] == 0 && n.Rotation[2] == 0 && n.Rotation[3] == 1))
                xform *= rotation;
                //xform = Transform.Multiply(xform, rotation);

            if (!(n.Scale[0] == 1 && n.Scale[1] == 1 && n.Scale[2] == 1))
                xform *= scale;
                //xform = Transform.Multiply(xform, scale);

            return xform;
        }

        public static dynamic AccessBuffer(Accessor.TypeEnum accessorType, int count, Accessor.ComponentTypeEnum componentType, int stride, byte[] arr)
        {
            dynamic result = null;

            var elementCount = count;                                   //how many times do we need to do this?
            var componentCount = GetTypeMultiplier(accessorType); ;     //each time we do this, how many times do I need to read the buffer?
            var byteCount = GetComponentTypeMultiplier(componentType);  //how many bytes is each component from ComponentTypeEnum
            var elementBytes = componentCount * byteCount;

            var strideDiff = stride > 0 ? stride - elementBytes : 0;

            using (var memoryStream = new MemoryStream(arr))
            using (var reader = new BinaryReader(memoryStream))
            {
                // TODO: clean this up
                switch (componentType)
                {
                    case glTFLoader.Schema.Accessor.ComponentTypeEnum.BYTE:

                        var listSByte = new List<sbyte>();

                        //loop through element count

                        //loop through component count
                        //if stride, position the reader appropriately
                        // element bytes should be byteCount * componentCount
                        // stride diff should be stride - elementbytes

                        for (int i = 0; i < elementCount; i++)
                        {
                            for (int j = 0; j < componentCount; j++)
                            {
                                listSByte.Add(reader.ReadSByte());
                            }
                            reader.BaseStream.Position += strideDiff;
                        }

                        result = listSByte;

                        break;
                    case glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT:

                        var listSingle = new List<Single>();

                        for (int i = 0; i < elementCount; i++)
                        {
                            for (int j = 0; j < componentCount; j++)
                            {
                                listSingle.Add(reader.ReadSingle());
                            }
                            reader.BaseStream.Position += strideDiff;
                        }

                        result = listSingle;

                        break;

                    case glTFLoader.Schema.Accessor.ComponentTypeEnum.SHORT:

                        var listShort = new List<Int16>();

                        for (int i = 0; i < elementCount; i++)
                        {
                            for (int j = 0; j < componentCount; j++)
                            {
                                listShort.Add(reader.ReadInt16());
                            }
                            reader.BaseStream.Position += strideDiff;
                        }

                        result = listShort;

                        break;

                    case glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_BYTE:

                        var listByte = new List<byte>();

                        for (int i = 0; i < elementCount; i++)
                        {
                            for (int j = 0; j < componentCount; j++)
                            {
                                listByte.Add(reader.ReadByte());
                            }
                            reader.BaseStream.Position += strideDiff;
                        }

                        result = listByte;

                        break;
                    case glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_INT:

                        var listUInt = new List<uint>();

                        for (int i = 0; i < elementCount; i++)
                        {
                            for (int j = 0; j < componentCount; j++)
                            {
                                listUInt.Add(reader.ReadUInt32());
                            }
                            reader.BaseStream.Position += strideDiff;
                        }

                        result = listUInt;

                        break;
                    case glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_SHORT:

                        var listUInt16 = new List<UInt16>();

                        for (int i = 0; i < elementCount; i++)
                        {
                            for (int j = 0; j < componentCount; j++)
                            {
                                listUInt16.Add(reader.ReadUInt16());
                            }

                            reader.BaseStream.Position += strideDiff;
                        }

                        result = listUInt16;

                        break;
                }

            }

            return result;
        }

        public static int GetTypeMultiplier(Accessor.TypeEnum accessorType)
        {

            var mult = 0;

            switch (accessorType)
            {
                case glTFLoader.Schema.Accessor.TypeEnum.SCALAR:
                    mult = 1;
                    break;
                case glTFLoader.Schema.Accessor.TypeEnum.VEC2:
                    mult = 2;
                    break;
                case glTFLoader.Schema.Accessor.TypeEnum.VEC3:
                    mult = 3;
                    break;
                case glTFLoader.Schema.Accessor.TypeEnum.VEC4:
                case glTFLoader.Schema.Accessor.TypeEnum.MAT2:
                    mult = 4;
                    break;
                case glTFLoader.Schema.Accessor.TypeEnum.MAT3:
                    mult = 9;
                    break;
                case glTFLoader.Schema.Accessor.TypeEnum.MAT4:
                    mult = 16;
                    break;

            }

            return mult;
        }

        public static int GetComponentTypeMultiplier(Accessor.ComponentTypeEnum componentType)
        {

            var mult = 0;

            switch (componentType)
            {
                case Accessor.ComponentTypeEnum.BYTE:
                case Accessor.ComponentTypeEnum.UNSIGNED_BYTE:
                    mult = 1;
                    break;
                case Accessor.ComponentTypeEnum.SHORT:
                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                    mult = 2;
                    break;
                case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                case Accessor.ComponentTypeEnum.FLOAT:
                    mult = 4;
                    break;
            }

            return mult;
        }

        public static Color ColorFromSingle(Single red, Single green, Single blue)
        {

            var r = (int) red * 255;
            var g = (int) green * 255;
            var b = (int) blue * 255;

            return Color.FromArgb(r,g,b);
        }
    }
}
