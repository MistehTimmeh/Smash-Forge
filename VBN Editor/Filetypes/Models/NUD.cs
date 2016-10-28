﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace VBN_Editor
{
    public class NUD : FileBase
    {
        public NUD()
        {
            GL.GenBuffers(1, out vbo_position);
            GL.GenBuffers(1, out vbo_color);
            GL.GenBuffers(1, out vbo_nrm);
            GL.GenBuffers(1, out vbo_uv);
            GL.GenBuffers(1, out vbo_bone);
            GL.GenBuffers(1, out vbo_weight);
            GL.GenBuffers(1, out ibo_elements);
        }
        public NUD(string fname) : this()
        {
            Read(fname);
            PreRender();
        }

        // gl buffer objects
        int vbo_position;
        int vbo_color;
        int vbo_nrm;
        int vbo_uv;
        int vbo_weight;
        int vbo_bone;
        int ibo_elements;

        Vector2[] uvdata;
        Vector3[] vertdata, nrmdata;
        int[] facedata;
        Vector4[] bonedata, coldata, weightdata;

        public const int SMASH = 0;
        public const int POKKEN = 1;
        public int type = SMASH;
        public int boneCount = 0;
        public bool hasBones = false;
        public List<Mesh> mesh = new List<Mesh>();

        public override Endianness Endian { get; set; }


        public void Destroy()
        {
            GL.DeleteBuffer(vbo_position);
            GL.DeleteBuffer(vbo_color);
            GL.DeleteBuffer(vbo_nrm);
            GL.DeleteBuffer(vbo_uv);
            GL.DeleteBuffer(vbo_weight);
            GL.DeleteBuffer(vbo_bone);
        }

        /*
		 * Not sure if update is needed here
		*/
        private void PreRender()
        {
            List<Vector3> vert = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<Vector4> col = new List<Vector4>();
            List<Vector3> nrm = new List<Vector3>();
            List<Vector4> bone = new List<Vector4>();
            List<Vector4> weight = new List<Vector4>();
            List<int> face = new List<int>();

            int i = 0;

            foreach (Mesh m in mesh)
            {
                foreach (Polygon p in m.polygons)
                {
                    if (p.faces.Count <= 3)
                        continue;
                    foreach (Vertex v in p.vertices)
                    {
                        vert.Add(v.pos);
                        col.Add(v.col);
                        nrm.Add(v.nrm);
                        uv.AddRange(v.tx);
                        while (v.node.Count < 4)
                        {
                            v.node.Add(0);
                            v.weight.Add(0);
                        }
                        bone.Add(new Vector4(v.node[0], v.node[1], v.node[2], v.node[3]));
                        weight.Add(new Vector4(v.weight[0], v.weight[1], v.weight[2], v.weight[3]));
                    }

                    // rearrange faces
                    int[] ia = p.faces.ToArray();
                    for (int j = 0; j < ia.Length; j++)
                    {
                        ia[j] += i;
                    }
                    face.AddRange(ia);
                    i += p.vertices.Count;
                }
            }

            vertdata = vert.ToArray();
            coldata = col.ToArray();
            nrmdata = nrm.ToArray();
            uvdata = uv.ToArray();
            facedata = face.ToArray();
            bonedata = bone.ToArray();
            weightdata = weight.ToArray();

        }

        public void Render(Shader shader)
        {

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_position);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertdata.Length * Vector3.SizeInBytes), vertdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shader.getAttribute("vPosition"), 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_color);
            GL.BufferData<Vector4>(BufferTarget.ArrayBuffer, (IntPtr)(coldata.Length * Vector4.SizeInBytes), coldata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shader.getAttribute("vColor"), 4, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_nrm);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(nrmdata.Length * Vector3.SizeInBytes), nrmdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shader.getAttribute("vNormal"), 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_uv);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(uvdata.Length * Vector2.SizeInBytes), uvdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shader.getAttribute("vUV"), 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_bone);
            GL.BufferData<Vector4>(BufferTarget.ArrayBuffer, (IntPtr)(bonedata.Length * Vector4.SizeInBytes), bonedata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shader.getAttribute("vBone"), 4, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_weight);
            GL.BufferData<Vector4>(BufferTarget.ArrayBuffer, (IntPtr)(weightdata.Length * Vector4.SizeInBytes), weightdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shader.getAttribute("vWeight"), 4, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo_elements);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(facedata.Length * sizeof(int)), facedata, BufferUsageHint.StaticDraw);


            //mesh [0].polygons [0].isVisible = false;

            //GL.Uniform1(shader.getAttribute("hasBones"), hasBones ? 1 : 0);

            int indiceat = 0;
            foreach (Mesh m in mesh)
            {
                foreach (Polygon p in m.polygons)
                {
                    if (p.faces.Count <= 3)
                        continue;

                    foreach (NUT nut in Runtime.TextureContainers)
                    {
                        int tex = -1;
                        nut.draw.TryGetValue(p.materials[0].textures[0].hash, out tex);

                        if (tex != 0)
                        {
                            GL.BindTexture(TextureTarget.Texture2D, tex);
                            GL.Uniform1(shader.getAttribute("tex"), 0);
                            break;
                        }
                    }
                    GL.Uniform4(shader.getAttribute("colorSamplerUV"), new Vector4(1,1,0,0));

                    if (p.isVisible && m.isVisible)
                    {
                        GL.DrawElements((p.strip>>4) == 4 ? PrimitiveType.Triangles : PrimitiveType.TriangleStrip, p.faces.Count, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    }
                    indiceat += p.faces.Count;
                }
            }

            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }


        public void applyMTA(MTA m, int frame)
        {
            foreach (VisEntry e in m.visEntries)
            {
                int state = e.getState(frame);
                foreach (Mesh me in mesh)
                {
                    if (me.name.Equals(e.name))
                    {
                        Console.WriteLine("Set " + me.name + " to " + state);
                        if (state == 0)
                        {
                            me.isVisible = false;
                        }
                        else
                        {
                            me.isVisible = true;
                        }
                        break;
                    }
                }
            }

        }



        //------------------------------------------------------------------------------------------------------------------------
        /*
		 * Reads the contents of the nud file into this class
		 * Not all info will be saved, so the file will be different on export
		 */
        //------------------------------------------------------------------------------------------------------------------------
        public override void Read(string filename)
        {
            FileData d = new FileData(filename);
            d.Endian = Endianness.Big;
            d.seek(0);

            // read header
            d.seek(0xA);
            int polysets = d.readShort();
            boneCount = d.readShort();
            d.skip(2);  // somethingsets
            int polyClumpStart = d.readInt() + 0x30;
            int polyClumpSize = d.readInt();
            int vertClumpStart = polyClumpStart + polyClumpSize;
            int vertClumpSize = d.readInt();
            int vertaddClumpStart = vertClumpStart + vertClumpSize;
            int vertaddClumpSize = d.readInt();
            int nameStart = vertaddClumpStart + vertaddClumpSize;
            d.skip(16); // some floats

            // object descriptors

            _s_Object[] obj = new _s_Object[polysets];
            for (int i = 0; i < polysets; i++)
            {
                d.skip(32);
                int temp = d.pos() + 4;
                d.seek(nameStart + d.readInt());
                obj[i].name = (d.readString());
                // read name string
                d.seek(temp);
                obj[i].id = d.readInt();
                obj[i].singlebind = d.readShort();
                obj[i].polyamt = d.readShort();
                obj[i].positionb = d.readInt();
            }

            // reading polygon data
            foreach (var o in obj)
            {
                Mesh m = new Mesh();
                m.name = o.name;
                mesh.Add(m);
                m.singlebind = (short)o.singlebind;

                for (int i = 0; i < o.polyamt; i++)
                {
                    _s_Poly p = new _s_Poly();

                    p.polyStart = d.readInt() + polyClumpStart;
                    p.vertStart = d.readInt() + vertClumpStart;
                    p.verAddStart = d.readInt() + vertaddClumpStart;
                    p.vertamt = d.readShort();
                    p.vertSize = d.readByte();
                    p.UVSize = d.readByte();
                    p.texprop1 = d.readInt();
                    p.texprop2 = d.readInt();
                    p.texprop3 = d.readInt();
                    p.texprop4 = d.readInt();
                    p.polyamt = d.readShort();
                    p.polsize = d.readByte();
                    p.polflag = d.readByte();
                    d.skip(0xC);

                    int temp = d.pos();

                    // read vertex
                    Polygon pol = readVertex(d, p, o);
                    m.polygons.Add(pol);

                    pol.materials = readMaterial(d, p, nameStart);

                    d.seek(temp);
                }
            }
        }

        //VERTEX TYPES----------------------------------------------------------------------------------------

        private static List<Material> readMaterial(FileData d, _s_Poly p, int nameOffset)
        {
            int propoff = p.texprop1;
            List<Material> mats = new List<Material>();

            while (propoff != 0)
            {
                d.seek(propoff);

                Material m = new Material();
                mats.Add(m);

                m.flags = d.readInt();
                d.skip(4);
                m.data1 = d.readShort();

                int propCount = d.readShort();
                m.trans1 = d.readInt();
                m.trans2 = d.readInt();
                d.skip(12); // dunno what this part is padding

                for (int i = 0; i < propCount; i++)
                {
                    Mat_Texture tex = new Mat_Texture();
                    tex.hash = d.readInt();
                    d.skip(8); // padding?
                    tex.data1 = d.readInt();
                    d.skip(4); // padding?
                    tex.data2 = d.readInt();
                    m.textures.Add(tex);
                }

                int head = 0x20;


                while (head != 0)
                {
                    head = d.readInt();
                    int nameStart = d.readInt();

                    string name = d.readString(nameOffset + nameStart, -1);
                
                    int pos = d.pos();
                    int c = d.readInt();
                    d.skip(4);
                    float[] f = new float[c];
                    for (int i = 0; i < c; i++)
                    {
                        f[i] = d.readFloat();
                    }

                    m.entries.Add(name, f);

                    d.seek(pos);

                    if (head == 0)
                        d.skip(0x20 - 8);
                    else
                        d.skip(head - 8);
                }

                if(propoff == p.texprop1)
                    propoff = p.texprop2;
                else
                if(propoff == p.texprop2)
                    propoff = p.texprop3;
                else
                if(propoff == p.texprop3)
                    propoff = p.texprop4;
            }

            return mats;
        }

        private static Polygon readVertex(FileData d, _s_Poly p, _s_Object o)
        {
            Polygon m = new Polygon();
            m.vertSize = p.vertSize;
            m.UVSize = p.UVSize;
            m.strip = p.polsize;

            readVertex(d, p, o, m);

            // faces
            d.seek(p.polyStart);

            //if (p.polsize == 0x40)
            //{
                for (int x = 0; x < p.polyamt / 3; x++)
                {
                    m.faces.Add(d.readShort());
                    m.faces.Add(d.readShort());
                    m.faces.Add(d.readShort());
                }
            //}

            /*if (p.polsize == 0x00 || p.polsize == 0x04)
            {
                int faceCount = p.polyamt;
                int faceStart = d.pos();
                int verStart = (faceCount * 2) + faceStart;

                int startDirection = 1;
                int f1 = d.readShort();
                int f2 = d.readShort();
                int faceDirection = startDirection;
                int f3;
                do
                {
                    f3 = d.readShort();
                    if (f3 == 0xFFFF)
                    {
                        f1 = d.readShort();
                        f2 = d.readShort();
                        faceDirection = startDirection;
                    }
                    else
                    {
                        faceDirection *= -1;
                        if ((f1 != f2) && (f2 != f3) && (f3 != f1))
                        {
                            if (faceDirection > 0)
                            {
                                m.faces.Add(f3);
                                m.faces.Add(f2);
                                m.faces.Add(f1);
                            }
                            else
                            {
                                m.faces.Add(f2);
                                m.faces.Add(f3);
                                m.faces.Add(f1);
                            }
                        }
                        f1 = f2;
                        f2 = f3;
                    }
                } while (d.pos() != (verStart));

            }*/

            return m;
        }

        //VERTEX TYPES----------------------------------------------------------------------------------------
        private static void readUV(FileData d, _s_Poly p, _s_Object o, Polygon m, Vertex[] v)
        {
            int uvCount = (p.UVSize >> 4);
            int uvType = (p.UVSize) & 0xF;

            for (int i = 0; i < p.vertamt; i++)
            {
                v[i] = new Vertex();
                if (uvType == 0x2)
                {
                    v[i].col = new Vector4(d.readByte(), d.readByte(), d.readByte(), d.readByte());
                    for(int j = 0; j < uvCount ; j++)
                        v[i].tx.Add(new Vector2(d.readHalfFloat(), d.readHalfFloat()));
                }
                else
                    throw new NotImplementedException("UV type not supported");
            }
        }

        private static void readVertex(FileData d, _s_Poly p, _s_Object o, Polygon m)
        {
            int weight = p.vertSize >> 4;
            int nrm = p.vertSize & 0xF;

            Vertex[] v = new Vertex[p.vertamt];

            d.seek(p.vertStart);

            if (weight > 0)
            {
                readUV(d, p, o, m, v);
                d.seek(p.verAddStart);
            }
            else
            {
                for (int i = 0; i < p.vertamt; i++)
                {
                    v[i] = new Vertex();
                }
            }

            for (int i = 0; i < p.vertamt; i++)
            {
                v[i].pos.X = d.readFloat();
                v[i].pos.Y = d.readFloat();
                v[i].pos.Z = d.readFloat();

                if (nrm > 0)
                {
                    v[i].nrm.X = d.readHalfFloat();
                    v[i].nrm.Y = d.readHalfFloat();
                    v[i].nrm.Z = d.readHalfFloat();
                    d.skip(2); // n1?
                }
                else
                    d.skip(4);

                if (nrm == 7)
                    d.skip(16); // bn and tan half floats

                if (weight == 0)
                {
                    if (p.UVSize >= 18)
                    {
                        v[i].col.X = (int)d.readByte();
                        v[i].col.Y = (int)d.readByte();
                        v[i].col.Z = (int)d.readByte();
                        v[i].col.W = (int)d.readByte();
                        //v.a = (int) (d.readByte());
                    }

                    for(int j = 0; j < (p.UVSize >> 4) ; j++)
                        v[i].tx.Add(new Vector2(d.readHalfFloat(), d.readHalfFloat()));

                    // UV layers
                    //d.skip(4 * ((p.UVSize >> 4) - 1));
                }

                if (weight == 4)
                {
                    v[i].node.Add(d.readByte());
                    v[i].node.Add(d.readByte());
                    v[i].node.Add(d.readByte());
                    v[i].node.Add(d.readByte());
                    v[i].weight.Add((float)d.readByte() / 255f);
                    v[i].weight.Add((float)d.readByte() / 255f);
                    v[i].weight.Add((float)d.readByte() / 255f);
                    v[i].weight.Add((float)d.readByte() / 255f);
                }
                else if (weight == 0)
                {
                    v[i].node.Add((short)o.singlebind);
                    v[i].weight.Add(1);
                }
            }

            foreach (Vertex vi in v)
                m.vertices.Add(vi);
        }

        //Creating---------------------------------------------------------
        public override byte[] Rebuild()
        {
            FileOutput d = new FileOutput(); // data
            d.Endian = Endianness.Big;

            // mesh optimize

            d.writeString("NDP3");
            d.writeInt(0); //FileSize
            d.writeShort(0x200); //  version num
            d.writeShort(mesh.Count); // polysets
            d.writeShort(2); // type
            d.writeShort(boneCount - 1); // Number of bones

            d.writeInt(0); // polyClump start
            d.writeInt(0); // polyClump size
            d.writeInt(0); // vertexClumpsize
            d.writeInt(0); // vertexaddcump size

            // some floats.. TODO: I dunno what these are for
            d.writeFloat(0);
            d.writeFloat(0);
            d.writeFloat(0);
            d.writeFloat(0);

            // other sections....
            FileOutput obj = new FileOutput(); // data
            obj.Endian = Endianness.Big;
            FileOutput tex = new FileOutput(); // data
            tex.Endian = Endianness.Big;

            FileOutput poly = new FileOutput(); // data
            poly.Endian = Endianness.Big;
            FileOutput vert = new FileOutput(); // data
            vert.Endian = Endianness.Big;
            FileOutput vertadd = new FileOutput(); // data
            vertadd.Endian = Endianness.Big;

            FileOutput str = new FileOutput(); // data
            str.Endian = Endianness.Big;


            // obj descriptor

            FileOutput tempstring = new FileOutput(); // data
            for (int i = 0; i < mesh.Count; i++)
            {
                str.writeString(mesh[i].name);
                str.writeByte(0);
                str.align(16);
            }

            int polyCount = 0; // counting number of poly
            foreach (Mesh m in mesh)
                polyCount += m.polygons.Count;

            for (int i = 0; i < mesh.Count; i++)
            {
                // more floats TODO: I dunno what these are for
                d.writeFloat(0);
                d.writeFloat(0);
                d.writeFloat(0);
                d.writeFloat(0);

                d.writeFloat(0);
                d.writeFloat(0);
                d.writeFloat(0);
                d.writeFloat(0);

                d.writeInt(tempstring.size());

                tempstring.writeString(mesh[i].name);
                tempstring.writeByte(0);
                tempstring.align(16);

                d.writeInt(0x04); // ID
                d.writeShort(mesh[i].singlebind); // Single Bind 
                d.writeShort(mesh[i].polygons.Count); // poly count
                d.writeInt(obj.size() + 0x30 + mesh.Count * 0x30); // position start for obj

                // write obj info here...
                for (int k = 0; k < mesh[i].polygons.Count; k++)
                {
                    obj.writeInt(poly.size());
                    obj.writeInt(vert.size());
                    obj.writeInt(mesh[i].polygons[k].vertSize>>4 > 0 ? vertadd.size() : 0);
                    obj.writeShort(mesh[i].polygons[k].vertices.Count);
                    obj.writeByte(mesh[i].polygons[k].vertSize); // type of vert

                    int maxUV = mesh[i].polygons[k].vertices[0].tx.Count; // TODO: multi uv stuff  mesh[i].polygons[k].maxUV() + 

                    obj.writeByte((maxUV << 4) | 2); // type of UV 0x12 for vertex color

                    // MATERIAL SECTION 

                    FileOutput te = new FileOutput();
                    te.Endian = Endianness.Big;

                    int[] texoff = writeMaterial(tex, mesh[i].polygons[k], str);
                    //tex.writeOutput(te);

                    //obj.writeInt(tex.size() + 0x30 + mesh.Count * 0x30 + polyCount * 0x30); // Tex properties... This is tex offset
                    obj.writeInt(texoff[0] + 0x30 + mesh.Count * 0x30 + polyCount * 0x30);
                    obj.writeInt(texoff[1] > 0 ? texoff[1] + 0x30 + mesh.Count * 0x30 + polyCount * 0x30 : 0);
                    obj.writeInt(texoff[2] > 0 ? texoff[2] + 0x30 + mesh.Count * 0x30 + polyCount * 0x30 : 0);
                    obj.writeInt(texoff[3] > 0 ? texoff[3] + 0x30 + mesh.Count * 0x30 + polyCount * 0x30 : 0);

                    obj.writeShort(mesh[i].polygons[k].faces.Count); // polyamt
                    obj.writeByte(0x40); // polysize 0x04 is strips and 0x40 is easy
                                         // :D
                    obj.writeByte(0x04); // polyflag

                    obj.writeInt(0); // idk, nothing padding??
                    obj.writeInt(0);
                    obj.writeInt(0);

                    // Write the poly...
                    foreach (int face in mesh[i].polygons[k].faces)
                        poly.writeShort(face);

                    // Write the vertex....

                    writeVertex(vert, vertadd, mesh[i].polygons[k]);

                }
            }

            //
            d.writeOutput(obj);
            d.writeOutput(tex);
            d.align(16);

            d.writeIntAt(d.size() - 0x30, 0x10);
            d.writeIntAt(poly.size(), 0x14);
            d.writeIntAt(vert.size(), 0x18);
            d.writeIntAt(vertadd.size(), 0x1c);

            d.writeOutput(poly);

            int s = d.size();
            d.align(16);
            s = d.size() - s;
            d.writeIntAt(poly.size() + s, 0x14);

            d.writeOutput(vert);

            s = d.size();
            d.align(16);
            s = d.size() - s;
            d.writeIntAt(vert.size() + s, 0x18);

            d.writeOutput(vertadd);

            s = d.size();
            d.align(16);
            s = d.size() - s;
            d.writeIntAt(vertadd.size() + s, 0x1c);

            d.writeOutput(str);

            d.writeIntAt(d.size(), 0x4);

            return d.getBytes();
        }

        private static void writeUV(FileOutput d, Polygon m)
        {
            for (int i = 0; i < m.vertices.Count; i++)
            {
                if ((m.UVSize&0xF) == 0x2)
                {
                    d.writeByte((int)m.vertices[i].col.X);
                    d.writeByte((int)m.vertices[i].col.Y);
                    d.writeByte((int)m.vertices[i].col.Z);
                    d.writeByte((int)m.vertices[i].col.W);
                    for (int j = 0; j < m.vertices[i].tx.Count; j++)
                    {
                        d.writeHalfFloat(m.vertices[i].tx[j].X);
                        d.writeHalfFloat(m.vertices[i].tx[j].Y);
                    }
                }
                else
                    throw new NotImplementedException("Unsupported UV format");
            }
        }

        private static void writeVertex(FileOutput d, FileOutput add, Polygon m)
        {
            int weight = m.vertSize >> 4;
            int nrm = m.vertSize & 0xF;

            //d.seek(p.vertStart);
            if (weight > 0)
            {
                writeUV(d, m);
                //d.seek(p.verAddStart);
                d = add;
            }

            for (int i = 0; i < m.vertices.Count; i++)
            {
                Vertex v = m.vertices[i];
                d.writeFloat(v.pos.X);
                d.writeFloat(v.pos.Y);
                d.writeFloat(v.pos.Z);

                if (nrm > 0)
                {
                    d.writeHalfFloat(v.nrm.X);
                    d.writeHalfFloat(v.nrm.Y);
                    d.writeHalfFloat(v.nrm.Z);
                    d.writeHalfFloat(1);
                }
                else
                    d.writeInt(0);

                if (nrm == 7){
                    // bn and tan half floats
                    d.writeInt(0);
                    d.writeInt(0);
                    d.writeInt(0);
                    d.writeInt(0);
                }

                if (weight == 0)
                {
                    if (m.UVSize >= 18)
                    {
                        d.writeByte((int)m.vertices[i].col.X);
                        d.writeByte((int)m.vertices[i].col.Y);
                        d.writeByte((int)m.vertices[i].col.Z);
                        d.writeByte((int)m.vertices[i].col.W);
                    }

                    for (int j = 0; j < m.vertices[i].tx.Count; j++)
                    {
                        d.writeHalfFloat(m.vertices[i].tx[j].X);
                        d.writeHalfFloat(m.vertices[i].tx[j].Y);
                    }

                    // UV layers
                    //d.skip(4 * ((m.UVSize >> 4) - 1));
                }

                if (weight == 4)
                {
                    d.writeByte(v.node[0]);
                    d.writeByte(v.node[1]);
                    d.writeByte(v.node[2]);
                    d.writeByte(v.node[3]);
                    d.writeByte((int)(v.weight[0] * 255f));
                    d.writeByte((int)(v.weight[1] * 255f));
                    d.writeByte((int)(v.weight[2] * 255f));
                    d.writeByte((int)(v.weight[3] * 255f));
                }
            }
        }

        private static int[] writeMaterial(FileOutput d, Polygon p, FileOutput str)
        {
            int[] offs = new int[4];
            int c = 0;
            foreach(Material mat in p.materials)
            {
                offs[c++] = d.size();
                d.writeInt(mat.flags);
                d.writeInt(0);//padding
                d.writeShort(mat.data1);
                d.writeShort(mat.textures.Count);
                d.writeInt(mat.trans1);
                d.writeInt(mat.trans2);
                d.writeInt(0);
                d.writeInt(0);
                d.writeInt(0);

                foreach (Mat_Texture tex in mat.textures)
                {
                    d.writeInt(tex.hash);
                    d.writeInt(0);
                    d.writeInt(0);
                    d.writeInt(tex.data1);
                    d.writeInt(0);
                    d.writeInt(tex.data2);
                }

                for (int i = 0; i < mat.entries.Count; i++)
                {
                    float[] data;
                    mat.entries.TryGetValue(mat.entries.ElementAt(i).Key, out data);
                    d.writeInt(i == mat.entries.Count-1 ? 0 : 16+4*data.Length);
                    d.writeInt(str.size());

                    str.writeString(mat.entries.ElementAt(i).Key);
                    str.writeByte(0);
                    str.align(16);

                    d.writeInt(data.Length);
                    d.writeInt(0);
                    foreach (float f in data)
                        d.writeFloat(f);
                }
            }
            return offs;
        }


        // HELPERS FOR READING
        /*private struct header{
			char[] magic;
			public int fileSize;
			public short unknown;
			public int polySetCount;
		}*/
        private struct _s_Object
        {
            public int id;
            //public int polynamestart;
            public int singlebind;
            public int polyamt;
            public int positionb;
            public string name;
        }

        private struct _s_Poly
        {
            public int polyStart;
            public int vertStart;
            public int verAddStart;
            public int vertamt;
            public int vertSize;
            public int UVSize;
            public int polyamt;
            public int polsize;
            public int polflag;
            public int texprop1;
            public int texprop2;
            public int texprop3;
            public int texprop4;
        }

        public class Vertex
        {
            public Vector3 pos = new Vector3(0, 0, 0), nrm = new Vector3(0, 0, 0);
            public Vector4 col = new Vector4(1, 1, 1, 1);
            public List<Vector2> tx = new List<Vector2>();
            public List<int> node = new List<int>();
            public List<float> weight = new List<float>();

            public Vertex()
            {
            }

            public Vertex(float x, float y, float z)
            {
                pos = new Vector3(x, y, z);
            }
        }


        public class Mat_Texture
        {
            public int hash;
            public int data1, data2;
        }

        public class Material
        {
            public Dictionary<string, float[]> entries = new Dictionary<string, float[]>();
            public Dictionary<string, float[]> anims = new Dictionary<string, float[]>();
            public List<Mat_Texture> textures = new List<Mat_Texture>();

            public int flags;
            public int data1;
            public int trans1, trans2;

            public Material()
            {
            }
        }

        public class Polygon
        {
            public List<Vertex> vertices = new List<Vertex>();
            public List<int> faces = new List<int>();

            // Material
            public List<Material> materials = new List<Material>();

            public bool isVisible = true;

            // for nud stuff
            public int vertSize = 0x46; // defaults to a basic bone weighted vertex format
            public int UVSize = 0x12;
            public int strip = 0x40;

            public void AddVertex(Vertex v)
            {
                vertices.Add(v);
            }
        }

        // typically a mesh will just have 1 polygon
        // but you can just use the mesh class without polygons
        public class Mesh
        {
            public string name;
            public List<Polygon> polygons = new List<Polygon>();
            public short singlebind = -1;

            public bool isVisible = true;

            public void addVertex(Vertex v)
            {
                if (polygons.Count == 0)
                    polygons.Add(new Polygon());

                polygons[0].AddVertex(v);
            }
        }
    }
}

